using Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace AuthSystem.UnitTests.Helpers;

public sealed class TestTokenHasher : ITokenHasher
{
    public string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
