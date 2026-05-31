using Application.Auth.Commands.Logout;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        ILogger<LogoutCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
        _logger = logger;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedException("Refresh token is required.");
        }

        var tokenHash = _tokenHasher.Hash(request.RefreshToken);
        var lookup = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);

        if (lookup is null)
        {
            _logger.LogWarning("Logout attempt with unknown refresh token");
            return;
        }

        if (lookup.RefreshToken.IsActive)
        {
            await _userRepository.RevokeActiveRefreshTokenByIdAsync(lookup.RefreshToken.Id, cancellationToken);
        }

        _logger.LogInformation("Session revoked for user {UserId}", lookup.User.Id);
    }
}
