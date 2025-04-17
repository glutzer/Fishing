using System.IO;
using Vintagestory.API.Common;

namespace Fishing3;

/// <summary>
/// Server side worm tasks.
/// </summary>
public class WormTask
{
    public float Priority { get; private set; }
    public EntityLeviathanHead Head { get; private set; }
    public bool IsServer { get; private set; }
    public int Id { get; private set; }

    public void SetId(int id)
    {
        Id = id;
    }

    public WormTask(float priority, EntityLeviathanHead head)
    {
        Priority = priority;
        Head = head;
        IsServer = head.Api.Side == EnumAppSide.Server;
    }

    /// <summary>
    /// After the last task has ended, can this task start?
    /// </summary>
    public virtual bool CanStartTask(float dt)
    {
        return false;
    }

    /// <summary>
    /// When a new task is started, calls this.
    /// </summary>
    public virtual void OnTaskStarted()
    {

    }

    /// <summary>
    /// Physics tick a task, on both the client and server.
    /// </summary>
    public virtual void TickTask(float dt)
    {

    }

    /// <summary>
    /// After ticking, can this task still continue?
    /// Only called on the server.
    /// </summary>
    public virtual bool CanContinueTask(float dt)
    {
        return true;
    }

    /// <summary>
    /// When the task is stopped.
    /// </summary>
    public virtual void OnTaskStopped()
    {

    }

    /// <summary>
    /// Called when entity is unloaded for all tasks.
    /// </summary>
    public virtual void OnEntityUnloaded()
    {

    }

    public virtual void ToBytes(BinaryWriter writer)
    {

    }

    public virtual void FromBytes(BinaryReader reader)
    {

    }
}