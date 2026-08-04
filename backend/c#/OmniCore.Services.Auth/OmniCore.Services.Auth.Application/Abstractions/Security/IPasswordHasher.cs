namespace OmniCore.Services.Auth.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}