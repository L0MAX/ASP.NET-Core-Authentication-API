using Application.Auth.DTOs.Responses;
using MediatR;

namespace Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<RegisterResponse>;
