using Application.Auth.Commands.ConfirmEmail;
using Application.Auth.DTOs.Requests;
using FluentValidation;

namespace Application.Auth.Validators;

public sealed class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.");
    }
}

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.");
    }
}
