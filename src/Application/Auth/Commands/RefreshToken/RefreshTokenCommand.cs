using Application.Auth.DTOs.Responses;
using MediatR;

namespace Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
