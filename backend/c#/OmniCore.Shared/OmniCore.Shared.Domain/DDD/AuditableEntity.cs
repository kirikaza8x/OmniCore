namespace OmniCore.Shared.Domain.DDD;

public interface IAuditableEntity
{
    DateTime? CreatedAt { get; }
    string? CreatedBy { get; }
    DateTime? ModifiedAt { get; }
    string? ModifiedBy { get; }
}

public interface ISoftDeletable
{
    bool IsActive { get; }
}