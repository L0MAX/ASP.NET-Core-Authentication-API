using Application.Auth.Commands.ResetPassword;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordResetTokenProvider _tokenProvider;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordResetTokenProvider tokenProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = await _tokenProvider.ValidateTokenAsync(request.Token, cancellationToken);

        if (userId is null)
        {
            throw new UnauthorizedException("Invalid or expired password reset token.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || user.Id != userId)
        {
            throw new UnauthorizedException("Invalid or expired password reset token.");
        }

        user.UpdatePassword(_passwordHasher.HashPassword(request.NewPassword));
        user.RevokeAllRefreshTokens();

        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
