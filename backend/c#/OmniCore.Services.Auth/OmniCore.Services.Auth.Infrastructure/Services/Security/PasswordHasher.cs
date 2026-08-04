namespace OmniCore.Services.Auth.Infrastructure.Services.Security;

using BCrypt.Net;
using OmniCore.Services.Auth.Application.Abstractions.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Verify(password, hashedPassword);
    }
}