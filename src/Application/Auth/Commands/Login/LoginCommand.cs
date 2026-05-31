using Application.Auth.DTOs.Responses;
using MediatR;

namespace Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<AuthResponse>;
