using MediatR;

namespace Application.Auth.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string Email, string Token) : IRequest;
