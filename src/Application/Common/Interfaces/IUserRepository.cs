using Application.Auth.Models;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRefreshTokensAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RefreshTokenLookup?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<int> RevokeActiveRefreshTokenByIdAsync(Guid refreshTokenId, CancellationToken cancellationToken = default);

    Task RevokeAllRefreshTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
