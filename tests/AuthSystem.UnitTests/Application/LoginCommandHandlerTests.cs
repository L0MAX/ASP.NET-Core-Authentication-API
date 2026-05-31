using Application.Auth.Commands.Login;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using AuthSystem.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSystem.UnitTests.Application;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _logger = new();

    private LoginCommandHandler CreateHandler() =>
        new(
            _userRepository.Object,
            _passwordService.Object,
            _jwtService.Object,
            _logger.Object);

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthResponseWithTokens()
    {
        var command = TestDataFactory.CreateLoginCommand();
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        _userRepository
            .Setup(r => r.GetByEmailWithRolesAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _passwordService
            .Setup(p => p.GetUpgradedHashIfNeeded(command.Password, user.PasswordHash))
            .Returns((string?)null);

        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
        _jwtService.Setup(j => j.GetRefreshTokenExpiry()).Returns(refreshExpiry);
        _jwtService.Setup(j => j.GenerateAccessToken(user)).Returns("access-token");
        _jwtService.Setup(j => j.GetAccessTokenExpiry()).Returns(accessExpiry);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
        result.AccessTokenExpiresAt.Should().Be(accessExpiry);
        result.User.Email.Should().Be("user@example.com");

        user.RefreshTokens.Should().ContainSingle(t => t.Token == "new-refresh-token");
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeLookup()
    {
        var command = new LoginCommand("  User@Example.COM  ", TestDataFactory.ValidPassword);

        _userRepository
            .Setup(r => r.GetByEmailWithRolesAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.User?)null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();

        _userRepository.Verify(
            r => r.GetByEmailWithRolesAsync("user@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsUnauthorizedException()
    {
        var command = TestDataFactory.CreateLoginCommand();

        _userRepository
            .Setup(r => r.GetByEmailWithRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.User?)null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ThrowsUnauthorizedException()
    {
        var command = TestDataFactory.CreateLoginCommand();
        var user = TestDataFactory.CreateVerifiedUserWithRole();

        _userRepository
            .Setup(r => r.GetByEmailWithRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WhenEmailNotConfirmed_ThrowsForbiddenException()
    {
        var command = TestDataFactory.CreateLoginCommand();
        var user = TestDataFactory.CreateUser(emailConfirmed: false);

        _userRepository
            .Setup(r => r.GetByEmailWithRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Please verify your email address before logging in.");
    }

    [Fact]
    public async Task Handle_WhenPasswordRehashNeeded_UpgradesStoredHash()
    {
        var command = TestDataFactory.CreateLoginCommand();
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        var originalHash = user.PasswordHash;
        const string upgradedHash = "upgraded-hash";

        _userRepository
            .Setup(r => r.GetByEmailWithRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordService
            .Setup(p => p.VerifyPassword(command.Password, originalHash))
            .Returns(true);

        _passwordService
            .Setup(p => p.GetUpgradedHashIfNeeded(command.Password, originalHash))
            .Returns(upgradedHash);

        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
        _jwtService.Setup(j => j.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));
        _jwtService.Setup(j => j.GenerateAccessToken(user)).Returns("access");
        _jwtService.Setup(j => j.GetAccessTokenExpiry()).Returns(DateTime.UtcNow.AddMinutes(15));

        await CreateHandler().Handle(command, CancellationToken.None);

        user.PasswordHash.Should().Be(upgradedHash);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
