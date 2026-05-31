using Domain.Entities;

namespace Application.Auth.Models;

public sealed record RefreshTokenLookup(User User, RefreshToken RefreshToken);
