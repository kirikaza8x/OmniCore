namespace OmniCore.Services.Auth.Domain.Repositories;

using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Enums;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Repositories;

public interface IAccountRepository : IRepository<Account, AccountId>
{
    /// <summary>
    /// Retrieves an account entity currently tracked in EF Core local memory (unsaved) by ID.
    /// </summary>
    Account? GetLocalById(AccountId accountId);
    Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>Finds an account by matching username OR email address.</summary>
    Task<Account?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>Finds an account associated with the given refresh token string.</summary>
    Task<Account?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Loads an account with its security tokens (password reset, e-mail verification) by e-mail.</summary>
    Task<Account?> GetByEmailWithSecurityTokensAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Loads an account with its security tokens by id.</summary>
    Task<Account?> GetByIdWithSecurityTokensAsync(AccountId id, CancellationToken cancellationToken = default);

    /// <summary>Finds the account that owns an unused security token of the given type, by the token's hash.</summary>
    Task<Account?> GetBySecurityTokenAsync(string codeHash, SecurityTokenType type, CancellationToken cancellationToken = default);

    /// <summary>Loads several accounts by id (accounts that do not exist are simply missing from the result).</summary>
    Task<IReadOnlyList<Account>> GetByIdsAsync(IReadOnlyCollection<AccountId> ids, CancellationToken cancellationToken = default);

    Task<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);

    Task<(bool IsEmailTaken, bool IsUsernameTaken)> CheckUniquenessAsync(
        string email, 
        string username, 
        CancellationToken cancellationToken = default);
}