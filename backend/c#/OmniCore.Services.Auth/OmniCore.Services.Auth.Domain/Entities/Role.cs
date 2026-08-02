using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Domain.Entities;

/// <summary>
/// Aggregate Root representing a high-level authorization role claim (e.g., Admin, User).
/// </summary>
public class Role : AggregateRoot<RoleId>
{
    public string Name { get; private set; } = string.Empty;

    public ICollection<AccountRole> AccountRoles { get; private set; } = new List<AccountRole>();

    private Role() { }

    private Role(RoleId id, string name) : base(id)
    {
        Name = name.Trim();
    }

    /// <summary>
    /// Factory method to construct a system role.
    /// </summary>
    /// <param name="name">The unique system role name.</param>
    /// <returns>A <see cref="Result{Role}"/> instance.</returns>
    public static Result<Role> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(Error.Validation("Role.NameEmpty", "Role name cannot be empty."));
        }

        return new Role(RoleId.New(), name);
    }
}