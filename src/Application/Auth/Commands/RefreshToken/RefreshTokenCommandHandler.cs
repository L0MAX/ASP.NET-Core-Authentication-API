using Application.Auth.Commands.RefreshToken;
using Application.Auth.DTOs.Responses;
using Application.Auth.Mappings;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private const string InvalidTokenMessage = "Invalid or expired refresh token.";

    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Refresh attempt with unknown token");
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);

        if (existingToken is null)
        {
            _logger.LogWarning("Refresh token not found on user {UserId}", user.Id);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (existingToken.IsRevoked)
        {
            await HandleReuseDetectedAsync(user, cancellationToken);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (existingToken.IsExpired)
        {
            user.RevokeRefreshToken(existingToken.Token);
            await _userRepository.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Refresh attempt with expired token for user {UserId}", user.Id);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        return await RotateTokensAsync(user, existingToken, cancellationToken);
    }

    private async Task HandleReuseDetectedAsync(User user, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Refresh token reuse detected for user {UserId}. Revoking all sessions.",
            user.Id);

        user.RevokeAllRefreshTokens();
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> RotateTokensAsync(
        User user,
        Domain.Entities.RefreshToken existingToken,
        CancellationToken cancellationToken)
    {
        user.RevokeRefreshToken(existingToken.Token);

        var newRefreshTokenValue = _jwtService.GenerateRefreshToken();
        user.IssueRefreshToken(newRefreshTokenValue, _jwtService.GetRefreshTokenExpiry());

        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

        return new AuthResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = newRefreshTokenValue,
            AccessTokenExpiresAt = _jwtService.GetAccessTokenExpiry(),
            User = user.ToResponse()
        };
    }
}
