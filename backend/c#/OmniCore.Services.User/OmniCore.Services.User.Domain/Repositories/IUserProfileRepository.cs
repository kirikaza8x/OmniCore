// Repositories/IUserProfileRepository.cs
namespace OmniCore.Services.User.Domain.Repositories;

using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Repositories;
using OmniCore.Shared.Domain.Specifications;

public interface IUserProfileRepository : IRepository<UserProfile, UserId>
{
    /// <summary>
    /// Retrieves a user profile entity currently tracked in EF Core local memory (unsaved) by ID.
    /// </summary>
    UserProfile? GetLocalById(UserId userId);

    Task<UserProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);

    Task<(bool IsEmailTaken, bool IsUsernameTaken)> CheckUniquenessAsync(
        string email, 
        string username, 
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetWithSpecificationAsync(
        ISpecification<UserProfile> specification, 
        CancellationToken cancellationToken = default);
}