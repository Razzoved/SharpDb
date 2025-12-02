namespace SharpDb.Entities;

/// <summary>
/// This should be your goto interface for any entity that represents
/// a user in the system. Using this interface enables additional
/// library features such as automatic user tracking on entities.
/// </summary>
public interface IUser
{
    object GetID();
    string GetDisplayName();
}
