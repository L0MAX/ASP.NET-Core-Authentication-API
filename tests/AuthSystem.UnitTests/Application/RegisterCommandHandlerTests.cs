using Application.Auth.Commands.Register;
using Application.Common.Interfaces;
using AuthSystem.UnitTests.Helpers;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSystem.UnitTests.Application;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IEmailVerificationTokenProvider> _verificationTokenProvider = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<ILogger<RegisterCommandHandler>> _logger = new();

    private RegisterCommandHandler CreateHandler() =>
        new(
            _userRepository.Object,
            _passwordService.Object,
            _verificationTokenProvider.Object,
            _emailService.Object,
            _logger.Object);

    [Fact]
    public async Task Handle_WithValidRequest_RegistersUserAndSendsVerificationEmail()
    {
        var command = TestDataFactory.CreateRegisterCommand();
        var userRole = TestDataFactory.CreateUserRole();
        const string hashedPassword = "hashed-password";
        const string verificationToken = "verification-token";

        _userRepository
            .Setup(r => r.EmailExistsAsync("newuser@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.GetRoleByNameAsync(RoleNames.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRole);

        _passwordService
            .Setup(p => p.HashPassword(command.Password))
            .Returns(hashedPassword);

        _verificationTokenProvider
            .Setup(v => v.GenerateAndStoreTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationToken);

        User? capturedUser = null;
        _userRepository
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.User.Email.Should().Be("newuser@example.com");
        result.User.FirstName.Should().Be("Jane");
        result.User.LastName.Should().Be("Doe");
        result.User.EmailConfirmed.Should().BeFalse();
        result.User.Roles.Should().Contain(RoleNames.User);

        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().Be(hashedPassword);

        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(
            e => e.SendEmailConfirmationAsync("newuser@example.com", verificationToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NormalizesEmailToLowerCase()
    {
        var command = new RegisterCommand("Jane", "Doe", "  MixedCase@Example.COM  ", TestDataFactory.ValidPassword);

        _userRepository
            .Setup(r => r.EmailExistsAsync("mixedcase@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.GetRoleByNameAsync(RoleNames.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateUserRole());

        _passwordService
            .Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("hash");

        _verificationTokenProvider
            .Setup(v => v.GenerateAndStoreTokenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("token");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.User.Email.Should().Be("mixedcase@example.com");
        _userRepository.Verify(
            r => r.EmailExistsAsync("mixedcase@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsGenericSuccessWithoutCreatingUser()
    {
        var command = TestDataFactory.CreateRegisterCommand();

        _userRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.User.Email.Should().Be("newuser@example.com");
        result.User.Id.Should().Be(Guid.Empty);

        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailService.Verify(
            e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDefaultRoleMissing_ThrowsInvalidOperationException()
    {
        var command = TestDataFactory.CreateRegisterCommand();

        _userRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.GetRoleByNameAsync(RoleNames.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _passwordService
            .Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("hash");

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Default role '{RoleNames.User}' is not seeded.");
    }
}
