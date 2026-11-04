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

/// <summary>
/// Ayarlanmış SMTP sunucusu (örn. Gmail: smtp.gmail.com, 587, SSL açık) üzerinden gönderir.
/// Gönderim hatası isteği düşürmez (parola sıfırlama yanıtı "kayıtlı mı?" bilgisini sızdırmamalı);
/// ama nedeni açıkça kayda yazılır.
/// </summary>
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
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20_000
            };
            if (!string.IsNullOrEmpty(_options.User))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_options.User, _options.Password);
            }

            var from = string.IsNullOrWhiteSpace(_options.From) ? _options.User : _options.From;
            using var message = new MailMessage
            {
                From = new MailAddress(from, "İŞKUR Otomasyonu", System.Text.Encoding.UTF8),
                Subject = subject,
                Body = body,
                SubjectEncoding = System.Text.Encoding.UTF8,
                BodyEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = false
            };
            message.To.Add(to);

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("E-posta gönderildi. Alıcı: {To}, konu: {Subject}", to, subject);
        }
        catch (SmtpException ex) when (ex.StatusCode is SmtpStatusCode.MustIssueStartTlsFirst or SmtpStatusCode.ClientNotPermitted
                                       || ex.Message.Contains("5.7.0") || ex.Message.Contains("5.7.8") || ex.Message.Contains("Authentication"))
        {
            _logger.LogError(ex,
                "E-posta gönderilemedi: posta sunucusu kimlik doğrulamayı kabul etmedi. Gmail için normal parola çalışmaz; " +
                "Google hesabında 2 adımlı doğrulamayı açıp 16 haneli 'Uygulama parolası' oluşturun ve Smtp:Password olarak onu girin. " +
                "Host={Host}, Port={Port}, User={User}", _options.Host, _options.Port, _options.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi. Host={Host}, Port={Port}, SSL={Ssl}, User={User}",
                _options.Host, _options.Port, _options.UseSsl, _options.User);
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
