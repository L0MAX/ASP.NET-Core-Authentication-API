using Application.Auth.DTOs.Responses;
using MediatR;

namespace Application.Auth.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserResponse>;
