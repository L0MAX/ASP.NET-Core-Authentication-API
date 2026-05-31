using Application.Common.Interfaces;
using Infrastructure.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public sealed class EmailService : IEmailService
{
    private readonly PasswordResetSettings _passwordResetSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<PasswordResetSettings> passwordResetSettings,
        ILogger<EmailService> logger)
    {
        _passwordResetSettings = passwordResetSettings.Value;
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var resetLink = BuildPasswordResetLink(email, resetToken);

        _logger.LogInformation(
            "Password reset email sent to {Email}. Link: {ResetLink}",
            email,
            resetLink);

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

    private string BuildPasswordResetLink(string email, string token)
    {
        var baseUrl = _passwordResetSettings.ResetLinkBaseUrl.TrimEnd('/');
        var query = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return $"{baseUrl}?{query}";
    }
}
