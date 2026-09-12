using Microsoft.Extensions.Logging;

namespace CronTaskRunner.Sample.Services;

/// <summary>
/// Заглушка для демонстрации — пишет "письмо" в консоль вместо реальной отправки.
/// Замените на реализацию поверх SMTP / SendGrid / любого email-провайдера.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken, bool isHtml = false)
    {
        _logger.LogInformation("EMAIL -> {To} | {Subject} | isHtml={IsHtml} | {Body}", to, subject, isHtml, body);
        return Task.CompletedTask;
    }
}
