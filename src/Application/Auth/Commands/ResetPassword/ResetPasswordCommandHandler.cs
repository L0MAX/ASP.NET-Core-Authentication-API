using Application.Auth.Commands.ResetPassword;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private const string InvalidTokenMessage = "Invalid or expired password reset token.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IPasswordResetTokenProvider _tokenProvider;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IPasswordResetTokenProvider tokenProvider,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var userId = await _tokenProvider.ValidateAndConsumeTokenAsync(
            request.Token,
            user.Id,
            cancellationToken);

        if (userId is null)
        {
            _logger.LogWarning("Invalid password reset attempt for user {UserId}", user.Id);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        user.UpdatePassword(_passwordService.HashPassword(request.NewPassword));
        await _userRepository.RevokeAllRefreshTokensForUserAsync(user.Id, cancellationToken);

        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}. All sessions revoked.", user.Id);
    }
}
