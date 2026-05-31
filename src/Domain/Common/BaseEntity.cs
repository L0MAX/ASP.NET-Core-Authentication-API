namespace Domain.Common;

/// <summary>
/// Base type for auditable entities with soft-delete support.
/// </summary>
public abstract class BaseEntity : Entity
{
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    internal void ApplyCreated(DateTime timestamp) => CreatedAt = timestamp;

    internal void ApplyUpdated(DateTime timestamp) => UpdatedAt = timestamp;

    public void MarkDeleted()
    {
        IsDeleted = true;
        ApplyUpdated(DateTime.UtcNow);
    }
}
