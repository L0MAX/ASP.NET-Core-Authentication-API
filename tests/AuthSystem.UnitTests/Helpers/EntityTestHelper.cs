using System.Reflection;
using Domain.Entities;

namespace AuthSystem.UnitTests.Helpers;

public static class EntityTestHelper
{
    public static void ExpireRefreshToken(RefreshToken refreshToken, DateTime? expiredAt = null)
    {
        var property = typeof(RefreshToken).GetProperty(
            nameof(RefreshToken.ExpiresAt),
            BindingFlags.Instance | BindingFlags.Public)!;

        property.SetValue(refreshToken, expiredAt ?? DateTime.UtcNow.AddMinutes(-1));
    }

    public static RefreshToken GetLatestRefreshToken(User user) =>
        user.RefreshTokens.Last();
}
