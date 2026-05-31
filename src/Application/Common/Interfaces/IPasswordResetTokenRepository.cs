using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetValidByHashAsync(
        string tokenHash,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task InvalidateAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
