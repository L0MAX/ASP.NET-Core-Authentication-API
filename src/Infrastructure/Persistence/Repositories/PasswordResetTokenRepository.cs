using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
        await _context.Set<PasswordResetToken>().AddAsync(token, cancellationToken);

    public Task<PasswordResetToken?> GetValidByHashAsync(
        string tokenHash,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _context.Set<PasswordResetToken>()
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash
                     && t.UserId == userId
                     && !t.IsUsed
                     && t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task InvalidateAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.MarkUsed();
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
