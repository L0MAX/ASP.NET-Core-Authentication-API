using Application.Auth.Commands.Login;
using Application.Auth.Validators;
using Application.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace AuthSystem.UnitTests.Application;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<LoginCommand, string>([]);
        var called = false;

        RequestHandlerDelegate<string> next = () =>
        {
            called = true;
            return Task.FromResult("success");
        };

        var result = await behavior.Handle(new LoginCommand("a@b.com", "pass"), next, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be("success");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var validator = new LoginCommandValidator();
        var behavior = new ValidationBehavior<LoginCommand, string>([validator]);

        RequestHandlerDelegate<string> next = () => Task.FromResult("success");

        var act = () => behavior.Handle(new LoginCommand("", ""), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var validator = new LoginCommandValidator();
        var behavior = new ValidationBehavior<LoginCommand, string>([validator]);

        RequestHandlerDelegate<string> next = () => Task.FromResult("success");

        var result = await behavior.Handle(
            new LoginCommand("user@example.com", "password"),
            next,
            CancellationToken.None);

        result.Should().Be("success");
    }
}
