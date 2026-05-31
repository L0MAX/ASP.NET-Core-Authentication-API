namespace Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default);

    Task SendEmailConfirmationAsync(
        string email,
        string confirmationToken,
        CancellationToken cancellationToken = default);
}
