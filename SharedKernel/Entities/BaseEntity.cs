using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    // Soft delete support
    public bool IsDeleted { get; protected set; } = false;
    public DateTime? DeletedAt { get; protected set; }

    // Optimistic concurrency control (EF Core timestamp)
    [Timestamp]
    public byte[] RowVersion { get; protected set; } = [];
}
