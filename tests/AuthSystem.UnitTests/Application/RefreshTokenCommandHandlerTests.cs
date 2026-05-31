using Application.Auth.Commands.RefreshToken;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using AuthSystem.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSystem.UnitTests.Application;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _logger = new();

    private RefreshTokenCommandHandler CreateHandler() =>
        new(_userRepository.Object, _jwtService.Object, _logger.Object);

    [Fact]
    public async Task Handle_WithValidToken_RotatesRefreshTokenAndReturnsNewAccessToken()
    {
        const string existingToken = "existing-refresh-token";
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        TestDataFactory.IssueRefreshToken(user, existingToken);

        var command = TestDataFactory.CreateRefreshTokenCommand(existingToken);
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);

        _userRepository
            .Setup(r => r.GetByRefreshTokenAsync(existingToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("rotated-refresh-token");
        _jwtService.Setup(j => j.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));
        _jwtService.Setup(j => j.GenerateAccessToken(user)).Returns("new-access-token");
        _jwtService.Setup(j => j.GetAccessTokenExpiry()).Returns(accessExpiry);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("rotated-refresh-token");
        result.AccessTokenExpiresAt.Should().Be(accessExpiry);

        user.RefreshTokens.Should().Contain(t => t.Token == existingToken && t.IsRevoked);
        user.RefreshTokens.Should().Contain(t => t.Token == "rotated-refresh-token" && t.IsActive);

        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenUnknown_ThrowsUnauthorizedException()
    {
        var command = TestDataFactory.CreateRefreshTokenCommand("unknown-token");

        _userRepository
            .Setup(r => r.GetByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.User?)null);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid or expired refresh token.");
    }

    [Fact]
    public async Task Handle_WhenTokenNotOnUser_ThrowsUnauthorizedException()
    {
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        var command = TestDataFactory.CreateRefreshTokenCommand("missing-on-user");

        _userRepository
            .Setup(r => r.GetByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid or expired refresh token.");
    }

    [Fact]
    public async Task Handle_WhenRevokedTokenReused_RevokesAllSessionsAndThrowsUnauthorizedException()
    {
        const string reusedToken = "reused-refresh-token";
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        TestDataFactory.IssueRefreshToken(user, reusedToken);
        TestDataFactory.IssueRefreshToken(user, "active-token");

        user.RevokeRefreshToken(reusedToken);

        var command = TestDataFactory.CreateRefreshTokenCommand(reusedToken);

        _userRepository
            .Setup(r => r.GetByRefreshTokenAsync(reusedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid or expired refresh token.");

        user.RefreshTokens.Should().OnlyContain(t => t.IsRevoked);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_RevokesTokenAndThrowsUnauthorizedException()
    {
        const string expiredToken = "expired-refresh-token";
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        TestDataFactory.IssueRefreshToken(user, expiredToken);

        var refreshToken = EntityTestHelper.GetLatestRefreshToken(user);
        EntityTestHelper.ExpireRefreshToken(refreshToken);

        var command = TestDataFactory.CreateRefreshTokenCommand(expiredToken);

        _userRepository
            .Setup(r => r.GetByRefreshTokenAsync(expiredToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid or expired refresh token.");

        refreshToken.IsRevoked.Should().BeTrue();
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
