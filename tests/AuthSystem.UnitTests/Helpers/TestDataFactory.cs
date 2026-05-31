using Application.Auth.Commands.Login;
using Application.Auth.Commands.Register;
using Application.Auth.Commands.RefreshToken;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Identity;

namespace AuthSystem.UnitTests.Helpers;

public static class TestDataFactory
{
    public const string ValidPassword = "Password1!";

    private static readonly PasswordService PasswordHasher = new();

    public static string HashPassword(string password = ValidPassword) =>
        PasswordHasher.HashPassword(password);

    public static Role CreateUserRole() =>
        Role.CreateForSeed(Guid.NewGuid(), RoleNames.User);

    public static User CreateUser(
        string email = "user@example.com",
        bool emailConfirmed = false,
        string? passwordHash = null)
    {
        var user = User.Create(
            "Jane",
            "Doe",
            email,
            passwordHash ?? HashPassword());

        if (emailConfirmed)
        {
            user.ConfirmEmail();
        }

        return user;
    }

    public static User CreateVerifiedUserWithRole(string email = "user@example.com")
    {
        var user = CreateUser(email, emailConfirmed: true);
        user.AssignRole(CreateUserRole());
        return user;
    }

    public static RegisterCommand CreateRegisterCommand(
        string email = "newuser@example.com",
        string password = ValidPassword) =>
        new("Jane", "Doe", email, password);

    public static LoginCommand CreateLoginCommand(
        string email = "user@example.com",
        string password = ValidPassword) =>
        new(email, password);

    public static RefreshTokenCommand CreateRefreshTokenCommand(string token = "refresh-token-value") =>
        new(token);

    public static string IssueRefreshToken(User user, string token = "refresh-token-value", int daysValid = 7)
    {
        user.IssueRefreshToken(token, DateTime.UtcNow.AddDays(daysValid));
        return token;
    }
}
