using SharpDb.Entities;

namespace SharpDb.EntityFrameworkCore.Entities;

/// <summary>
/// Indicates that the entity tracks the user who created it.
/// </summary>
public interface ITrackUserC
{
    IUser? CreatedByUser { get; set; }
}
