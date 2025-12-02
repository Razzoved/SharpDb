using SharpDb.Entities;

namespace SharpDb.EntityFrameworkCore.Entities;

/// <summary>
/// Indicates that the entity tracks the user who last updated it.
/// </summary>
public interface ITrackUserU
{
    IUser? UpdatedByUser { get; set; }
}
