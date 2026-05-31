using MediatR;

namespace Application.Auth.Commands.Logout;

public sealed record LogoutAllSessionsCommand(Guid UserId) : IRequest;
