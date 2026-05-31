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
    private readonly ITokenHasher _tokenHasher;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        ITokenHasher tokenHasher,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _tokenHasher = tokenHasher;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHasher.Hash(request.RefreshToken);
        var lookup = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);

        if (lookup is null)
        {
            _logger.LogWarning("Refresh attempt with unknown token");
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var user = lookup.User;
        var existingToken = lookup.RefreshToken;

        if (existingToken.IsRevoked)
        {
            await HandleReuseDetectedAsync(user, cancellationToken);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (existingToken.IsExpired)
        {
            await _userRepository.RevokeActiveRefreshTokenByIdAsync(existingToken.Id, cancellationToken);
            _logger.LogWarning("Refresh attempt with expired token for user {UserId}", user.Id);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var revokedRows = await _userRepository.RevokeActiveRefreshTokenByIdAsync(existingToken.Id, cancellationToken);

        if (revokedRows == 0)
        {
            await HandleReuseDetectedAsync(user, cancellationToken);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        return await IssueRotatedTokensAsync(user, cancellationToken);
    }

    private async Task HandleReuseDetectedAsync(User user, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Refresh token reuse detected for user {UserId}. Revoking all sessions.",
            user.Id);

        await _userRepository.RevokeAllRefreshTokensForUserAsync(user.Id, cancellationToken);
    }

    private async Task<AuthResponse> IssueRotatedTokensAsync(User user, CancellationToken cancellationToken)
    {
        var newRefreshTokenValue = _jwtService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenHasher.Hash(newRefreshTokenValue);
        user.IssueRefreshToken(newRefreshTokenHash, _jwtService.GetRefreshTokenExpiry());

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
