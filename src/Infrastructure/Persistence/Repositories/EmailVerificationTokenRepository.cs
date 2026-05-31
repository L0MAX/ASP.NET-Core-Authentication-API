using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly ApplicationDbContext _context;

    public EmailVerificationTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default) =>
        await _context.Set<EmailVerificationToken>().AddAsync(token, cancellationToken);

    public Task<EmailVerificationToken?> GetValidByHashAsync(
        string tokenHash,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _context.Set<EmailVerificationToken>()
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash
                     && t.UserId == userId
                     && !t.IsUsed
                     && t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task InvalidateAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Set<EmailVerificationToken>()
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
