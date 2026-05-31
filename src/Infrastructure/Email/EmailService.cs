using Application.Common.Interfaces;
using Infrastructure.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

public sealed class EmailService : IEmailService
{
    private readonly PasswordResetSettings _passwordResetSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IHostEnvironment _environment;

    public EmailService(
        IOptions<PasswordResetSettings> passwordResetSettings,
        ILogger<EmailService> logger,
        IHostEnvironment environment)
    {
        _passwordResetSettings = passwordResetSettings.Value;
        _logger = logger;
        _environment = environment;
    }

    public Task SendPasswordResetEmailAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset email queued for {Email}", email);

        if (_environment.IsDevelopment())
        {
            var resetLink = BuildPasswordResetLink(email, resetToken);
            _logger.LogDebug("Dev-only password reset link generated for {Email}: {ResetLink}", email, resetLink);
        }

        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(
        string email,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email confirmation queued for {Email}", email);

        if (_environment.IsDevelopment())
        {
            _logger.LogDebug("Dev-only email confirmation token generated for {Email}", email);
        }

        return Task.CompletedTask;
    }

    private string BuildPasswordResetLink(string email, string token)
    {
        var baseUrl = _passwordResetSettings.ResetLinkBaseUrl.TrimEnd('/');
        var query = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        return $"{baseUrl}?{query}";
    }
}
