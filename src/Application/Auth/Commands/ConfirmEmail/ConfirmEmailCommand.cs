using MediatR;

namespace Application.Auth.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Email, string Token) : IRequest;
