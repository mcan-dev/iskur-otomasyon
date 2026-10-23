using IskurAuto.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using IskurAuto.API.Infrastructure;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data;
using IskurAuto.Service;
using IskurAuto.Service.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Katmanların bağımlılık kayıtları ──────────────────────────────────
// Program.cs yalnızca "hangi katman var" der; her katmanın içini kendi DependencyInjection.cs'i bilir.
var connectionString = builder.Configuration.GetConnectionString("IskurAutoDb")
    ?? throw new InvalidOperationException("ConnectionStrings:IskurAutoDb yapılandırması eksik.");

builder.Services.AddDataLayer(connectionString);
var workPlanSettings = builder.Configuration.GetSection(WorkPlanSettings.SectionName).Get<WorkPlanSettings>()
    ?? new WorkPlanSettings();
builder.Services.AddServiceLayer(workPlanSettings);
builder.Services.AddSingleton<IDocumentStore>(new LocalDocumentStore(
    builder.Configuration["Storage:DocumentsPath"] ?? "App_Data/documents"));

// ─── 2. API katmanına özgü servisler ──────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// Parola sıfırlama e-postası: SMTP ayarı varsa gerçekten gönderilir, yoksa kayda yazılır.
var smtp = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
builder.Services.AddSingleton(smtp);
if (string.IsNullOrWhiteSpace(smtp.Host))
    builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton(
    builder.Configuration.GetSection(PasswordResetSettings.SectionName).Get<PasswordResetSettings>() ?? new PasswordResetSettings());

// ─── 3. JWT kimlik doğrulama ──────────────────────────────────────────────
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

// Fail-fast: zayıf ya da örnek anahtarla uygulama başlamaz. Sessizce güvensiz çalışmaktansa açıkça durmak iyidir.
if (jwt.SecretKey.Length < 32)
    throw new InvalidOperationException("JwtSettings:SecretKey en az 32 karakter olmalıdır.");
if (!builder.Environment.IsDevelopment() && jwt.SecretKey == JwtOptions.DevelopmentPlaceholderKey)
    throw new InvalidOperationException(
        "Üretimde örnek JWT anahtarı kullanılamaz. 'dotnet user-secrets' veya ortam değişkeniyle JwtSettings:SecretKey tanımlayın.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// Gelen "role" claim'ini Microsoft'un uzun şema adına (http://schemas.../role) çevirmesini engelle;
// aksi halde [Authorize(Roles = "...")] kısa "role" claim'ini bulamaz ve 403 verir.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.Zero,                 // süre dolunca tam o anda geçersiz
            RoleClaimType = AppClaimTypes.Role,
            NameClaimType = AppClaimTypes.UserId
        };
    });

builder.Services.AddAuthorization();

// ─── 4. CORS ──────────────────────────────────────────────────────────────
// Tarayıcı, farklı adresten (örn. React: localhost:5173) gelen istekleri sunucu izin vermedikçe engeller.
// İzinli adresler yapılandırmadan okunur; "herkese izin ver" (AllowAnyOrigin) bilerek kullanılmaz.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ─── 5. Controller'lar ────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'lar JSON'da sayı (0,1,2) yerine okunur metin ("Student") olarak gider.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ─── 6. Swagger ───────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Teknoloji Fakültesi İŞKUR Otomasyonu API",
        Version = "v1",
        Description = "İŞKUR öğrenci programı süreçlerinin (kayıt, çalışma planı, izin, devam, görev, rapor) yönetimi.\n\n" +
                      "**Kimlik doğrulama:** `POST /api/auth/login` ile token alın, sağ üstteki **Authorize** düğmesine " +
                      "yalnızca token metnini yapıştırın."
    });

    var bearer = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Yalnızca token metnini girin (başına 'Bearer' yazmayın).",
        Reference = new OpenApiReference { Id = JwtBearerDefaults.AuthenticationScheme, Type = ReferenceType.SecurityScheme }
    };
    options.AddSecurityDefinition(bearer.Reference.Id, bearer);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearer, Array.Empty<string>() } });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

    options.DocumentFilter<OpenApiVersionFilter>();
});

// ─── Uygulama hattı (pipeline) — SIRA ÖNEMLİDİR ───────────────────────────
var app = builder.Build();

// En dışta: sonraki her aşamadan fırlayan hatayı yakalayıp standart yanıta çevirir.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "İŞKUR Otomasyon API v1");
        c.RoutePrefix = string.Empty;   // Swagger ana sayfada açılır
    });
}
else
{
    // Geliştirmede kapalı: http→https yönlendirmesi, tarayıcının CORS ön kontrol (preflight) isteğini bozar.
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");           // CORS, kimlik doğrulamadan ÖNCE olmalı
app.UseAuthentication();           // "Sen kimsin?"  (token'ı çöz)
app.UseAuthorization();            // "Yetkin var mı?" (rol kontrolü)

app.MapControllers();

// ─── Veritabanı hazırlığı ─────────────────────────────────────────────────
var logger = app.Services.GetRequiredService<ILogger<Program>>();
// Yalnızca geliştirmede: Development:ResetDatabase=true ise veritabanı silinip demo verisiyle yeniden kurulur.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Development:ResetDatabase"))
{
    using var resetScope = app.Services.CreateScope();
    var resetDb = resetScope.ServiceProvider.GetRequiredService<IskurAuto.Data.Context.IskurAutoDbContext>();
    await resetDb.Database.EnsureDeletedAsync();
    logger.LogWarning("Geliştirme veritabanı silindi; demo verisiyle yeniden oluşturulacak.");
}

await app.Services.InitializeDatabaseAsync();
if (app.Environment.IsDevelopment())
    await DatabaseSeeder.SeedDemoDataAsync(app.Services, logger);
await DatabaseSeeder.EnsureBootstrapAdminAsync(app.Services, app.Configuration, logger);

app.Run();

// WebApplicationFactory ile entegrasyon testi yazabilmek için (sonraki aşamalar).
public partial class Program { }
