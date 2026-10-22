using System.Net;
using System.Net.Mail;
using IskurAuto.Service.Abstractions;

namespace IskurAuto.API.Infrastructure;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}

/// <summary>Ayarlanmış SMTP sunucusu üzerinden gönderir. Hata olursa kaydeder, fırlatmaz.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(SmtpOptions options, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.UseSsl };
            if (!string.IsNullOrEmpty(_options.User))
                client.Credentials = new NetworkCredential(_options.User, _options.Password);

            var from = string.IsNullOrEmpty(_options.From) ? _options.User : _options.From;
            using var message = new MailMessage(from, to, subject, body);
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi.");
        }
    }
}

/// <summary>
/// SMTP ayarı yoksa kullanılır. Geliştirmede e-postanın içeriğini (sıfırlama bağlantısı dahil) konsola yazar,
/// böylece gerçek posta sunucusu olmadan akış denenebilir. Üretimde içerik yazılmaz.
/// </summary>
public class LogEmailSender : IEmailSender
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(IHostEnvironment env, ILogger<LogEmailSender> logger)
    {
        _env = env;
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        if (_env.IsDevelopment())
            _logger.LogWarning("E-posta (SMTP ayarlı değil, gönderilmedi). Alıcı: {To}\n{Subject}\n{Body}", to, subject, body);
        else
            _logger.LogError("E-posta gönderilemedi: Smtp ayarları tanımlı değil.");
        return Task.CompletedTask;
    }
}
