using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IEmailVerificationTokenRepository
{
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    Task<EmailVerificationToken?> GetValidByHashAsync(
        string tokenHash,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task InvalidateAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
