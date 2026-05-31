using Application.Auth.Commands.ForgotPassword;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetTokenProvider _tokenProvider;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IPasswordResetTokenProvider tokenProvider,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation(
                "Password reset requested for unknown email {Email}. Returning success to prevent enumeration.",
                normalizedEmail);
            return;
        }

        var resetToken = await _tokenProvider.GenerateAndStoreTokenAsync(user.Id, cancellationToken);
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, cancellationToken);

        _logger.LogInformation("Password reset token issued for user {UserId}", user.Id);
    }
}
