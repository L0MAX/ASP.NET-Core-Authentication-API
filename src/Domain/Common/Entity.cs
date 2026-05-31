namespace Domain.Common;

/// <summary>
/// Base type for entities identified by a surrogate key.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }
}
