using OpenTK.Mathematics;
using System;

namespace Fishing;

[WormTask]
public class WormTaskArcTo : WormTask
{
    public Vector3 TargetVector { get; private set; }
    private Action? onArcReached;

    public WormTaskArcTo(float priority, EntityLeviathanHead head) : base(priority, head)
    {
    }

    /// <summary>
    /// Set a target when starting the task.
    /// </summary>
    public void SetTarget(Vector3 target, Action? onArcReached)
    {
        TargetVector = target;
        this.onArcReached = onArcReached;
    }

    public override void TickTask(float dt)
    {
        if (!IsServer) return;

        // Move at an arc untilat the vector then do the callback.
        Head.LerpToFacing(TargetVector, 0.1f);

        if (Vector3.Dot(TargetVector, Head.Facing) >= 0.9f)
        {
            Head.StopTask();

            // End, start new tasks.
            onArcReached?.Invoke();
            onArcReached = null;
        }

        Head.Move(50f * dt);
    }

    public override bool CanContinueTask(float dt)
    {
        return true;
    }
}