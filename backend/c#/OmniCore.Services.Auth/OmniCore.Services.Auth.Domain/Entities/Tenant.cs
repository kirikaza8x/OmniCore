namespace OmniCore.Services.Auth.Domain.Entities;

using System.Text.RegularExpressions;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

/// <summary>
/// A tenant (ADR 0001): an isolated space whose members share data in the other services. Accounts stay global;
/// <see cref="TenantMembership"/> links them.
/// </summary>
public class Tenant : AggregateRoot<TenantId>
{
    /// <summary>Fixed id of the default tenant that absorbs all pre-tenancy data (created by the migration).</summary>
    public static readonly TenantId DefaultId = TenantId.From(new Guid("7e4a0001-0000-4000-8000-000000000001"));
    public const string DefaultSlug = "default";

    private static readonly Regex SlugPattern = new("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    private Tenant() { }

    private Tenant(TenantId id, string slug, string name, DateTime createdAtUtc) : base(id)
    {
        Slug = slug;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Tenant> Create(string slug, string name, DateTime utcNow)
    {
        string normalized = (slug ?? string.Empty).Trim().ToLowerInvariant();

        if (!SlugPattern.IsMatch(normalized))
        {
            return Result.Failure<Tenant>(Error.Validation(
                "Tenant.SlugInvalid", "Slug must be 1-63 characters: lowercase letters, digits and '-', not starting or ending with '-'."));
        }

        string trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > 100)
        {
            return Result.Failure<Tenant>(Error.Validation("Tenant.NameInvalid", "Name must be 1-100 characters."));
        }

        return new Tenant(TenantId.New(), normalized, trimmed, utcNow);
    }

    public Result Deactivate()
    {
        if (Id == DefaultId)
        {
            return Result.Failure(Error.Validation("Tenant.DefaultProtected", "The default tenant cannot be deactivated."));
        }

        IsActive = false;
        return Result.Success();
    }

    public void Activate() => IsActive = true;
}
