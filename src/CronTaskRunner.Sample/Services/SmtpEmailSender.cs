using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CronTaskRunner.Sample.Services;

/// <summary>
/// Реальная отправка почты через SMTP (System.Net.Mail).
/// Настройки — секция "Smtp" в appsettings.json (см. SmtpOptions).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken, bool isHtml = false)
    {
        using var message = new MailMessage(_options.From, to, subject, body)
        {
            IsBodyHtml = isHtml,
        };

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password),
        };

        _logger.LogInformation("Отправляем письмо -> {To} | {Subject}", to, subject);

        await client.SendMailAsync(message, cancellationToken);
    }
}
