using Application.Auth.Commands.Login;
using Application.Auth.Commands.RefreshToken;
using Application.Auth.Commands.Register;
using Application.Auth.Mappings;
using Application.Auth.Validators;
using AuthSystem.UnitTests.Helpers;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace AuthSystem.UnitTests.Application;

public class AuthValidatorTests
{
    private readonly RegisterCommandValidator _registerValidator = new();
    private readonly LoginCommandValidator _loginValidator = new();
    private readonly RefreshTokenCommandValidator _refreshTokenValidator = new();

    [Fact]
    public void RegisterCommandValidator_WithValidCommand_PassesValidation()
    {
        var command = TestDataFactory.CreateRegisterCommand();

        var result = _registerValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Last", "user@example.com", "Password1!", "First name is required.")]
    [InlineData("First", "", "user@example.com", "Password1!", "Last name is required.")]
    [InlineData("First", "Last", "not-an-email", "Password1!", "A valid email address is required.")]
    [InlineData("First", "Last", "user@example.com", "short", "Password must be at least 8 characters.")]
    public void RegisterCommandValidator_WithInvalidInput_FailsValidation(
        string firstName,
        string lastName,
        string email,
        string password,
        string expectedMessage)
    {
        var command = new RegisterCommand(firstName, lastName, email, password);

        var result = _registerValidator.TestValidate(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void LoginCommandValidator_WithValidCommand_PassesValidation()
    {
        var command = TestDataFactory.CreateLoginCommand();

        var result = _loginValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginCommandValidator_WithEmptyEmail_FailsValidation()
    {
        var command = new LoginCommand("", TestDataFactory.ValidPassword);

        var result = _loginValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Fact]
    public void RefreshTokenCommandValidator_WithEmptyToken_FailsValidation()
    {
        var command = new RefreshTokenCommand("");

        var result = _refreshTokenValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token is required.");
    }
}

public class UserMapperTests
{
    [Fact]
    public void ToResponse_MapsUserPropertiesAndRoles()
    {
        var user = TestDataFactory.CreateVerifiedUserWithRole("mapper@example.com");

        var response = user.ToResponse();

        response.Id.Should().Be(user.Id);
        response.FirstName.Should().Be("Jane");
        response.LastName.Should().Be("Doe");
        response.Email.Should().Be("mapper@example.com");
        response.EmailConfirmed.Should().BeTrue();
        response.Roles.Should().ContainSingle().Which.Should().Be("User");
    }
}
