namespace OmniCore.Services.Auth.Domain.Entities;

using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

public class Role : AggregateRoot<RoleId>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public ICollection<AccountRole> AccountRoles { get; private set; } = new List<AccountRole>();

    private Role() { }

    private Role(RoleId id, string name, string? description) : base(id)
    {
        Name = name.Trim();
        Description = description?.Trim();
    }

    /// <summary>
    /// Creates a new role with a validated name and optional description.
    /// </summary>
    public static Result<Role> Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(Error.Validation("Role.NameEmpty", "Role name cannot be empty."));
        }

        return new Role(RoleId.New(), name, description);
    }

    /// <summary>
    /// Updates the role name and description ensuring domain rules are maintained.
    /// </summary>
    public Result Update(string newName, string? newDescription = null)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(Error.Validation("Role.NameEmpty", "Role name cannot be empty."));
        }

        Name = newName.Trim();
        Description = newDescription?.Trim();
        return Result.Success();
    }
}