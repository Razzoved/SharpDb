using SharpDb.Entities;

namespace SharpDb.EntityFrameworkCore.Entities;

/// <summary>
/// Indicates that the entity tracks the user who deleted it.
/// </summary>
public interface ITrackUserD : ISoftDelete
{
    IUser? DeletedByUser { get; set; }
}
