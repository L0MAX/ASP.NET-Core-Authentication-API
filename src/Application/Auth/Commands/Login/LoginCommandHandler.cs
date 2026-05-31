using Application.Auth.Commands.Login;
using Application.Auth.DTOs.Responses;
using Application.Auth.Mappings;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtService jwtService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailWithRolesAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login failed: no account found for {Email}", normalizedEmail);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid password for {Email}", normalizedEmail);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!user.EmailConfirmed)
        {
            throw new ForbiddenException("Please verify your email address before logging in.");
        }

        var upgradedHash = _passwordService.GetUpgradedHashIfNeeded(request.Password, user.PasswordHash);

        if (upgradedHash is not null)
        {
            user.UpdatePassword(upgradedHash);
        }

        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        user.IssueRefreshToken(refreshTokenValue, _jwtService.GetRefreshTokenExpiry());

        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new AuthResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresAt = _jwtService.GetAccessTokenExpiry(),
            User = user.ToResponse()
        };
    }
}
