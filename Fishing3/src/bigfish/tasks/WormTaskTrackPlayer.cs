using MareLib;
using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;

namespace Fishing3;

[WormTask(-1f)]
public class WormTaskTrackPlayer : WormTask
{
    private float duration;
    private EntityPlayer? currentTarget;

    private float undergroundPower;
    private const float UNDERGROUND_MAX_POWER_SECONDS = 5f;

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

    public override void TickTask(float dt)
    {
        if (!IsServer) return;

        undergroundPower = !Head.CollidingWithGround
            ? Math.Clamp(undergroundPower - (dt / UNDERGROUND_MAX_POWER_SECONDS), 0, 1)
            : Math.Clamp(undergroundPower + (dt / UNDERGROUND_MAX_POWER_SECONDS), 0, 1);

        float speed = 50f;
        TargetNewPlayer();

        // Track player and adjust facing
        if (currentTarget != null)
        {
            Vector3d playerPos = currentTarget.ServerPos.ToVector();
            Vector3d pos = Head.ServerPos.ToVector();

            Vector3 normal = (Vector3)(playerPos - pos);
            normal.Normalize();

            Head.LerpToFacing(normal, 0.1f);
        }

        Head.Move(speed * dt);
    }

    public override bool CanContinueTask(float dt)
    {
        duration -= dt;
        return duration > 0;
    }

    public override bool CanStartTask(float dt)
    {
        return true;
    }

    public override void OnTaskStarted()
    {
        duration = 5f;
    }

    public override void OnTaskStopped()
    {
    }
}
