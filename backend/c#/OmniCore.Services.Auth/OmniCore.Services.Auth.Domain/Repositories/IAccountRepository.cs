namespace OmniCore.Services.Auth.Domain.Repositories;

using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Repositories;

public interface IAccountRepository : IRepository<Account, AccountId>
{
    Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>Finds an account by matching username OR email address.</summary>
    Task<Account?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    Task<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);

    Task<(bool IsEmailTaken, bool IsUsernameTaken)> CheckUniquenessAsync(
    string email, 
    string username, 
    CancellationToken cancellationToken = default);
}