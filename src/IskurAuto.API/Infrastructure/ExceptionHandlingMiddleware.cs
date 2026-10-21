using IskurAuto.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.API.Infrastructure;

/// <summary>
/// Tüm istisnaları tek noktada standart HTTP yanıtına (RFC 7807 ProblemDetails) çevirir.
/// Neden: controller'larda try/catch tekrarı biter; servis katmanı HTTP'yi bilmeden
/// "ne yanlış gitti" der, bu sınıf "hangi durum koduyla" sorusunu çözer. Frontend her hatayı aynı biçimde alır.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // İstemci bağlantıyı kendisi kesti; hata değil, yanıt yazmaya gerek yok.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        ProblemDetails problem = ex switch
        {
            DomainValidationException v => new ValidationProblemDetails(v.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Doğrulama hatası",
                Detail = v.Message
            },
            NotFoundException e => Simple(StatusCodes.Status404NotFound, "Bulunamadı", e.Message),
            ConflictException e => Simple(StatusCodes.Status409Conflict, "Çakışma", e.Message),
            ForbiddenException e => Simple(StatusCodes.Status403Forbidden, "Erişim reddedildi", e.Message),
            AuthenticationFailedException e => Simple(StatusCodes.Status401Unauthorized, "Kimlik doğrulanamadı", e.Message),

            // Uygulama kontrolünden kaçan yarış durumu (iki istek aynı anda aynı benzersiz değeri yazarsa)
            // veritabanı benzersiz indeksi yakalar; bunu kullanıcıya 500 değil 409 olarak göstermek doğrudur.
            DbUpdateException => Simple(StatusCodes.Status409Conflict, "Çakışma",
                "Kayıt, eş zamanlı başka bir işlemle çakıştı veya benzersizlik kuralını ihlal etti. Lütfen tekrar deneyin."),

            _ => Simple(StatusCodes.Status500InternalServerError, "Sunucu hatası",
                _env.IsDevelopment() ? ex.Message : "Beklenmeyen bir hata oluştu.")
        };

        if (problem.Status >= 500)
            _logger.LogError(ex, "Beklenmeyen hata: {Path}", context.Request.Path);
        else if (ex is DbUpdateException)
            _logger.LogWarning(ex, "Veritabanı çakışması: {Path}", context.Request.Path);

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // Çalışma zamanı tipiyle (problem.GetType()) yazılır. Statik tip olan ProblemDetails ile yazılsaydı
        // ValidationProblemDetails'e özgü "errors" alanı yanıttan sessizce düşerdi.
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }

    private static ProblemDetails Simple(int status, string title, string detail)
        => new() { Status = status, Title = title, Detail = detail };
}
