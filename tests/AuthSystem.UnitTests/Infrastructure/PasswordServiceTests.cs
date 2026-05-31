using AuthSystem.UnitTests.Helpers;
using FluentAssertions;
using Infrastructure.Identity;

namespace AuthSystem.UnitTests.Infrastructure;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsNonEmptyHash()
    {
        var hash = _sut.HashPassword(TestDataFactory.ValidPassword);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(TestDataFactory.ValidPassword);
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePassword()
    {
        var hash1 = _sut.HashPassword(TestDataFactory.ValidPassword);
        var hash2 = _sut.HashPassword(TestDataFactory.ValidPassword);

        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_WithInvalidPassword_ThrowsArgumentException(string? password)
    {
        var act = () => _sut.HashPassword(password!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.HashPassword(TestDataFactory.ValidPassword);

        var result = _sut.VerifyPassword(TestDataFactory.ValidPassword, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword(TestDataFactory.ValidPassword);

        var result = _sut.VerifyPassword("WrongPassword1!", hash);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyPassword_WithEmptyHash_ReturnsFalse(string? hash)
    {
        var result = _sut.VerifyPassword(TestDataFactory.ValidPassword, hash!);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyPassword_WithInvalidPassword_ThrowsArgumentException(string? password)
    {
        var hash = _sut.HashPassword(TestDataFactory.ValidPassword);

        var act = () => _sut.VerifyPassword(password!, hash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetUpgradedHashIfNeeded_WhenRehashNotNeeded_ReturnsNull()
    {
        var hash = _sut.HashPassword(TestDataFactory.ValidPassword);

        var upgraded = _sut.GetUpgradedHashIfNeeded(TestDataFactory.ValidPassword, hash);

        upgraded.Should().BeNull();
    }

    [Fact]
    public void HashAndVerify_RoundTrip_SucceedsForMultiplePasswords()
    {
        var passwords = new[] { "Password1!", "AnotherPass2@", "Str0ng#Pass" };

        foreach (var password in passwords)
        {
            var hash = _sut.HashPassword(password);
            _sut.VerifyPassword(password, hash).Should().BeTrue($"password '{password}' should verify");
        }
    }

    [Fact]
    public void RunDummyVerification_DoesNotThrow()
    {
        var act = () => _sut.RunDummyVerification(TestDataFactory.ValidPassword);

        act.Should().NotThrow();
    }
}
