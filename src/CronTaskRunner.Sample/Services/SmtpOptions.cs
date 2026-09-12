namespace CronTaskRunner.Sample.Services;

/// <summary>Настройки SMTP — секция "Smtp" в appsettings.json.</summary>
public sealed class SmtpOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; } = 587;
    public string User { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string From { get; set; } = default!;
    public bool EnableSsl { get; set; } = true;
}
