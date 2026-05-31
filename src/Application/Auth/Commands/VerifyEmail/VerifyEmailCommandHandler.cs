using Application.Auth.Commands.VerifyEmail;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private const string InvalidTokenMessage = "Invalid or expired email verification token.";

    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenProvider _verificationTokenProvider;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenProvider verificationTokenProvider,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _verificationTokenProvider = verificationTokenProvider;
        _logger = logger;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (user.EmailConfirmed)
        {
            return;
        }

        var userId = await _verificationTokenProvider.ValidateAndConsumeTokenAsync(
            request.Token,
            user.Id,
            cancellationToken);

        if (userId is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        user.ConfirmEmail();
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified for user {UserId}", user.Id);
    }
}
