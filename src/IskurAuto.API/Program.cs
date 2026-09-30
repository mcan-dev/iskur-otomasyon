using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IskurAuto.API.Infrastructure;
using IskurAuto.Automation;
using IskurAuto.Core.Interfaces;
using IskurAuto.Data.Context;
using IskurAuto.Data.Repositories;
using IskurAuto.Service.Implementations;
using IskurAuto.Service.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. Veritabanı Bağlantısı ─────────────────────────────────────────────
builder.Services.AddDbContext<IskurAutoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("IskurAutoDb"),
        sqlOptions => sqlOptions.MigrationsAssembly("IskurAuto.Data")
    )
);

// ─── 2. Repository ve Servis Kayıtları (DI) ──────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<IIskurBotService, IskurBotService>(); // Playwright otomasyon botu

// ─── 3. JWT Kimlik Doğrulama ve Yetkilendirme ────────────────────────────
var jwtSection  = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey yapılandırması eksik.");
var issuer      = jwtSection["Issuer"]   ?? "IskurAutoAPI";
var audience    = jwtSection["Audience"] ?? "IskurAutoClients";
var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

// Gelen JWT claim'lerini Microsoft'un uzun şema isimlerine (http://schemas...) dönüştürmesini engelle
// Aksi takdirde kendi verdiğimiz "role" claim'i kaybolur ve [Authorize(Roles = "...")] çalışmaz!
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKey         = signingKey,
            ClockSkew                = TimeSpan.Zero,          // Token süresini tam dakikada kes
            RoleClaimType            = ClaimTypes.Role,        // Rol claim'ini standart olarak oku
            NameClaimType            = AppClaimTypes.UserId    // Name claim'ini UserId'den oku
        };
    });

builder.Services.AddAuthorization();

// ─── 4. Controller'lar ────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── 5. Swagger / OpenAPI (JWT Authorize Butonu ile) ─────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "İŞKUR Otomasyon Sistemi API",
        Version     = "v1",
        Description = "Üniversiteler için kısmi zamanlı öğrenci puantaj yönetimi ve " +
                      "İŞKUR sistemine otonom veri girişi sağlayan çoklu kiracı (Multi-Tenant) API.\n\n" +
                      "**Kimlik Doğrulama:** Sağ üst köşedeki 🔒 Authorize butonuna " +
                      "`Bearer <token>` formatında JWT giriniz.",
        Contact = new OpenApiContact
        {
            Name = "SKS / Öğrenci İşleri Daire Başkanlığı"
        }
    });

    // ── JWT Güvenlik Tanımı ──
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme       = "bearer",
        BearerFormat = "JWT",
        Name         = "Authorization",
        In           = ParameterLocation.Header,
        Type         = SecuritySchemeType.Http,
        Description  = "JWT token'ınızı aşağıya **sadece token metnini** girin (Bearer prefix gerekmez).",
        Reference = new OpenApiReference
        {
            Id   = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    // Tüm endpoint'lere global olarak JWT gereksinimini uygula
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });

    // XML dokümantasyon yorumlarını Swagger UI'a ekle
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // Swagger UI uyumsuzluğunu önlemek için OpenAPI belge sürümünü normalize et
    options.DocumentFilter<IskurAuto.API.Infrastructure.OpenApiVersionFilter>();
});

// ─── 6. Uygulama Pipeline ─────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "İŞKUR Otomasyon API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// Sıralama kritik: Authentication → Authorization
app.UseAuthentication();
app.UseAuthorization();

// ─── Veritabanı Seed İşlemi ───────────────────────────────────────────────
await IskurAuto.API.Infrastructure.SeedData.InitializeAsync(app.Services);

app.MapControllers();

app.Run();
