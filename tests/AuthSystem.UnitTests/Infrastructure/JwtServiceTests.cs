using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common.Constants;
using AuthSystem.UnitTests.Helpers;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthSystem.UnitTests.Infrastructure;

public class JwtServiceTests
{
    private const string TestSecret = "ThisIsATestSecretKeyWith32Chars!!";

    private static JwtService CreateService(JwtSettings? settings = null)
    {
        var jwtSettings = settings ?? new JwtSettings
        {
            Secret = TestSecret,
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationInDays = 7
        };

        return new JwtService(Options.Create(jwtSettings));
    }

    [Fact]
    public void GenerateAccessToken_WithNullUser_ThrowsArgumentNullException()
    {
        var act = () => CreateService().GenerateAccessToken(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var user = TestDataFactory.CreateVerifiedUserWithRole("jwt-user@example.com");
        var service = CreateService();

        var token = service.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");

        jwt.Claims.Should().Contain(c => c.Type == JwtClaimTypes.UserId && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtClaimTypes.Email && c.Value == "jwt-user@example.com");
        jwt.Claims.Should().Contain(c => c.Type == JwtClaimTypes.Role && c.Value == RoleNames.User);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidSignedToken()
    {
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        var service = CreateService();
        var handler = new JwtSecurityTokenHandler();

        var token = service.GenerateAccessToken(user);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "test-issuer",
            ValidateAudience = true,
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var act = () => handler.ValidateToken(token, validationParameters, out _);

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateAccessToken_ExpiresWithinConfiguredLifetime()
    {
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        var service = CreateService();
        var before = DateTime.UtcNow;

        var token = service.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueBase64Strings()
    {
        var service = CreateService();

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);

        var act = () => Convert.FromBase64String(token1);
        act.Should().NotThrow();
    }

    [Fact]
    public void GetAccessTokenExpiry_ReturnsFutureUtcTime()
    {
        var service = CreateService();
        var before = DateTime.UtcNow;

        var expiry = service.GetAccessTokenExpiry();

        expiry.Should().BeAfter(before);
        expiry.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void GetRefreshTokenExpiry_ReturnsFutureUtcTime()
    {
        var service = CreateService();
        var before = DateTime.UtcNow;

        var expiry = service.GetRefreshTokenExpiry();

        expiry.Should().BeAfter(before);
        expiry.Should().BeCloseTo(before.AddDays(7), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void GenerateAccessToken_IncludesAllAssignedRoles()
    {
        var user = TestDataFactory.CreateVerifiedUserWithRole();
        user.AssignRole(Role.CreateForSeed(Guid.NewGuid(), RoleNames.Admin));

        var service = CreateService();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateAccessToken(user));

        var roles = jwt.Claims.Where(c => c.Type == JwtClaimTypes.Role).Select(c => c.Value).ToList();
        roles.Should().BeEquivalentTo([RoleNames.User, RoleNames.Admin]);
    }
}
