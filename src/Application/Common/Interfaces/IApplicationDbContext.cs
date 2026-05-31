using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<User> Users { get; }

    IQueryable<Role> Roles { get; }

    IQueryable<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
