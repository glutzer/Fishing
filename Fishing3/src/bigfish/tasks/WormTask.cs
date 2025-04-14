namespace Fishing3;

/// <summary>
/// Server side worm tasks.
/// </summary>
public abstract class WormTask
{
    public float Priority { get; private set; }
    public EntityLeviathanHead Head { get; private set; }

    public WormTask(float priority, EntityLeviathanHead head)
    {
        Priority = priority;
        Head = head;
    }

    /// <summary>
    /// After the last task has ended, can this task start?
    /// </summary>
    public abstract bool CanStartTask(float dt);

    /// <summary>
    /// When a new task is started, calls this.
    /// </summary>
    public abstract void OnTaskStarted();

    /// <summary>
    /// Physics tick a task.
    /// </summary>
    public abstract void TickTask(float dt);

    /// <summary>
    /// After ticking, can this task still continue?
    /// </summary>
    public abstract bool CanContinueTask(float dt);

    /// <summary>
    /// When the task is stopped.
    /// </summary>
    public abstract void OnTaskStopped();
}