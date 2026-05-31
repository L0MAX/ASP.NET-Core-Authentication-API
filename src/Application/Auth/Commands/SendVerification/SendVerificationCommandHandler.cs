using Application.Auth.Commands.SendVerification;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.SendVerification;

public sealed class SendVerificationCommandHandler : IRequestHandler<SendVerificationCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenProvider _verificationTokenProvider;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendVerificationCommandHandler> _logger;

    public SendVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenProvider verificationTokenProvider,
        IEmailService emailService,
        ILogger<SendVerificationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _verificationTokenProvider = verificationTokenProvider;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(SendVerificationCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation(
                "Verification email requested for unknown address {Email}. Returning success to prevent enumeration.",
                normalizedEmail);
            return;
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation(
                "Verification email requested for already verified user {UserId}. Returning success to prevent enumeration.",
                user.Id);
            return;
        }

        var verificationToken = await _verificationTokenProvider.GenerateAndStoreTokenAsync(user.Id, cancellationToken);
        await _emailService.SendEmailConfirmationAsync(user.Email, verificationToken, cancellationToken);

        _logger.LogInformation("Verification email sent to user {UserId}", user.Id);
    }
}
