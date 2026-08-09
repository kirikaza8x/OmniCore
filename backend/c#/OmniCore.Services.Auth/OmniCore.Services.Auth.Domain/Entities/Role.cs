namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

public class Role : AggregateRoot<RoleId>
{
    public string Name { get; private set; } = string.Empty;

    public ICollection<AccountRole> AccountRoles { get; private set; } = new List<AccountRole>();

    private Role() { }

    private Role(RoleId id, string name) : base(id)
    {
        Name = name.Trim();
    }

    public static Result<Role> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(Error.Validation("Role.NameEmpty", "Role name cannot be empty."));
        }

        return new Role(RoleId.New(), name);
    }

    /// <summary>
    /// Updates the role name ensuring domain rules are maintained.
    /// </summary>
    public Result UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("Role.NameEmpty", "Role name cannot be empty."));
        }

        Name = newName.Trim();
        return Result.Success();
    }
}