using Application.Auth.Commands.Logout;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.Logout;

public sealed class LogoutAllSessionsCommandHandler : IRequestHandler<LogoutAllSessionsCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<LogoutAllSessionsCommandHandler> _logger;

    public LogoutAllSessionsCommandHandler(
        IUserRepository userRepository,
        ILogger<LogoutAllSessionsCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRefreshTokensAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Logout-all requested for unknown user {UserId}", request.UserId);
            return;
        }

        await _userRepository.RevokeAllRefreshTokensForUserAsync(user.Id, cancellationToken);

        _logger.LogInformation("All sessions revoked for user {UserId}", request.UserId);
    }
}
