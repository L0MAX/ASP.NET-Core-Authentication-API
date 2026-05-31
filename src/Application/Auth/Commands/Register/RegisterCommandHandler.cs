using Application.Auth.Commands.Register;
using Application.Auth.DTOs.Responses;
using Application.Auth.Mappings;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Entities;
using MediatR;

namespace Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var defaultRole = await _userRepository.GetRoleByNameAsync(RoleNames.User, cancellationToken)
            ?? throw new InvalidOperationException($"Default role '{RoleNames.User}' is not seeded.");

        var user = User.Create(
            request.FirstName,
            request.LastName,
            normalizedEmail,
            _passwordHasher.HashPassword(request.Password));

        user.AssignRole(defaultRole);

        var refreshTokenValue = _jwtService.GenerateRefreshToken();
        user.IssueRefreshToken(refreshTokenValue, _jwtService.GetRefreshTokenExpiry());

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresAt = _jwtService.GetAccessTokenExpiry(),
            User = user.ToResponse()
        };
    }
}
