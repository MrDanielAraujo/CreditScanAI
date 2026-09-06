namespace CreditScanAI.Api.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}

/// <summary>
/// Fase 8 Parte 3: no real SMTP provider is configured for this project
/// (per the user's own choice), so "sending" an email just logs it - good
/// enough to exercise the whole forgot-password flow locally without any
/// external dependency. Swap for a real provider (SMTP, SendGrid, etc.)
/// before this system ever has real, non-dev users.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[EMAIL FALSO - dev only] Para: {ToEmail} | Assunto: {Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
