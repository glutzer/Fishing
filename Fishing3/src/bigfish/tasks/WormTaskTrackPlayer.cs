using MareLib;
using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;

namespace Fishing3;

[WormTask(-1f)]
public class WormTaskTrackPlayer : WormTask
{
    private float duration;
    public EntityPlayer? currentTarget;

    public WormTaskTrackPlayer(float priority, EntityLeviathanHead head) : base(priority, head)
    {
    }

    public void TargetNewPlayer()
    {
        IPlayer[] players = MainAPI.Server.GetPlayersAround(Head.ServerPos.XYZ, 200, 200);

        if (players.Length > 0)
        {
            currentTarget = players[0].Entity;
        }
    }

    public override bool CanContinueTask(float dt)
    {
        duration -= dt;
        if (duration <= 0) return false;
        return true;
    }

    public override bool CanStartTask(float dt)
    {
        return true;
    }

    public override void OnTaskStarted()
    {
        // Follow players for 5 seconds.
        duration = 5f;
    }

    public override void OnTaskStopped()
    {

    }

    public override void TickTask(float dt)
    {
        Head.ServerPos.Roll = 0;
        Head.ServerPos.Yaw = 0;
        Head.ServerPos.Pitch = 0;

        TargetNewPlayer();

        if (currentTarget == null) return;

        Vector3d playerPos = currentTarget.ServerPos.ToVector();
        Vector3d pos = Head.ServerPos.ToVector();

        Vector3d delta = playerPos - pos;
        Vector3d normal = delta.Normalized();
        Head.ServerPos.Yaw = (float)Math.Atan2(-normal.X, -normal.Z);
        Head.ServerPos.Pitch = (float)Math.Asin(normal.Y);

        if (delta.Length > 10)
        {
            normal *= 50;
            Head.ServerPos.Add(normal.X * dt, normal.Y * dt, normal.Z * dt);
        }
    }
}