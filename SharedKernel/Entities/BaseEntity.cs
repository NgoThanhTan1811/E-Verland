namespace SharedKernel.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }
    // public byte[] RowVersion { get; private set; } = [];


}
