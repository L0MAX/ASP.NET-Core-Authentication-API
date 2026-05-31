using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
