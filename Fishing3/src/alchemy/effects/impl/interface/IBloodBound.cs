namespace Fishing3;

/// <summary>
/// Blood meta effect applies to this one by setting the entity type and id, which should be -1 and "none" by default.
/// </summary>
public interface IBloodBound
{
    string EntityType { set; }
    long EntityId { set; }
}