using MediatR;

namespace Application.Auth.Commands.SendVerification;

public sealed record SendVerificationCommand(string Email) : IRequest;
