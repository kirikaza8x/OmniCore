namespace OmniCore.Services.Auth.Domain.Repositories;

using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Repositories;

public interface IRoleRepository : IRepository<Role, RoleId>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> IsNameUniqueAsync(string name, CancellationToken cancellationToken = default);
}