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
            .Include("_refreshTokens")
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .Include("_roles")
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (token is null)
        {
            return null;
        }

        return await _context.Users
            .Include("_roles")
            .Include("_refreshTokens")
            .FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
    }

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _context.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
