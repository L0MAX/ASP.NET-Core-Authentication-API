using Application.Auth.Commands.Register;
using Application.Auth.DTOs.Responses;
using Application.Auth.Mappings;
using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IEmailVerificationTokenProvider _verificationTokenProvider;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IEmailVerificationTokenProvider verificationTokenProvider,
        IEmailService emailService,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _verificationTokenProvider = verificationTokenProvider;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            _logger.LogInformation(
                "Registration attempt for existing email {Email}. Returning generic success to prevent enumeration.",
                normalizedEmail);

            return BuildGenericRegistrationResponse(request, normalizedEmail);
        }

        var defaultRole = await _userRepository.GetRoleByNameAsync(RoleNames.User, cancellationToken)
            ?? throw new InvalidOperationException($"Default role '{RoleNames.User}' is not seeded.");

        var user = User.Create(
            request.FirstName,
            request.LastName,
            normalizedEmail,
            _passwordService.HashPassword(request.Password));

        user.AssignRole(defaultRole);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var verificationToken = await _verificationTokenProvider.GenerateAndStoreTokenAsync(user.Id, cancellationToken);

        await _emailService.SendEmailConfirmationAsync(user.Email, verificationToken, cancellationToken);

        _logger.LogInformation("User {UserId} registered. Verification email sent to {Email}", user.Id, user.Email);

        return new RegisterResponse
        {
            User = user.ToResponse()
        };
    }

    private static RegisterResponse BuildGenericRegistrationResponse(RegisterCommand request, string normalizedEmail) =>
        new()
        {
            User = new UserResponse
            {
                Id = Guid.Empty,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                EmailConfirmed = false,
                Roles = []
            }
        };
}
