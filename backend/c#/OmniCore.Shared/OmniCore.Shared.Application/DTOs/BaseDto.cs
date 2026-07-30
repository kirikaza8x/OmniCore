namespace OmniCore.Shared.Application.DTOs;

public abstract record BaseDto<TId>
{
    public TId Id { get; init; } = default!;
}

public abstract record AuditableDto<TId> : BaseDto<TId>
{
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}