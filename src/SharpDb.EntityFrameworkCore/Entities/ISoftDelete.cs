namespace SharpDb.EntityFrameworkCore.Entities;

/// <summary>
/// Indicates that the entity supports soft deletion.
/// </summary>
public interface ISoftDelete
{
    DateTime? DeletedAt { get; set; }
    bool IsDeleted => DeletedAt.HasValue;
}
