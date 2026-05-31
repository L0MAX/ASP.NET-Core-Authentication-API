using Application.Auth.DTOs.Responses;
using Domain.Entities;

namespace Application.Auth.Mappings;

public static class UserMapper
{
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = user.Roles.Select(r => r.Name).ToList()
        };
    }
}
