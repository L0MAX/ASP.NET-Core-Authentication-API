using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Password reset email sent to {Email}. Token: {Token}",
            email,
            resetToken);

        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(
        string email,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email confirmation sent to {Email}. Token: {Token}",
            email,
            confirmationToken);

        return Task.CompletedTask;
    }
}
