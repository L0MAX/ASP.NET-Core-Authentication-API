using Application.Auth.Models;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users
            .Include("_roles")
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .Include("_roles")
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByIdWithRefreshTokensAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users
            .Include("_refreshTokens")
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<RefreshTokenLookup?> GetByRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return null;
        }

        var user = await _context.Users
            .Include("_roles")
            .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId, cancellationToken);

        return user is null ? null : new RefreshTokenLookup(user, refreshToken);
    }

    public Task<int> RevokeActiveRefreshTokenByIdAsync(
        Guid refreshTokenId,
        CancellationToken cancellationToken = default) =>
        _context.RefreshTokens
            .Where(token =>
                token.Id == refreshTokenId
                && !token.IsRevoked
                && token.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.IsRevoked, true),
                cancellationToken);

    public Task RevokeAllRefreshTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.IsRevoked, true),
                cancellationToken);

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
