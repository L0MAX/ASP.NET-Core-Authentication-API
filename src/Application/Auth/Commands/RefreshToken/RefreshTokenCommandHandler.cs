using Application.Auth.Commands.RefreshToken;
using Application.Auth.DTOs.Responses;
using Application.Auth.Mappings;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        user.RevokeRefreshToken(request.RefreshToken);

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        user.IssueRefreshToken(newRefreshToken, _jwtService.GetRefreshTokenExpiry());

        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = _jwtService.GetAccessTokenExpiry(),
            User = user.ToResponse()
        };
    }
}
