namespace SharpDb.EntityFrameworkCore.Entities;

/// <summary>
/// Indicates that the entity tracks its creation and last update timestamps.
/// </summary>
public interface ITrackSaveTime
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
