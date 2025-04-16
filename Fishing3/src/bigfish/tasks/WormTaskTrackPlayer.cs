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

    private Vector3 facing = new();

    /// <summary>
    /// 1 when underground for a time.
    /// </summary>
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

        // 1 underground power when underground for a duration, back to 0 when above ground for a duration.
        if (!Head.CollidingWithGround)
        {
            undergroundPower = Math.Clamp(undergroundPower - (dt / UNDERGROUND_MAX_POWER_SECONDS), 0, 1);
        }
        else
        {
            undergroundPower = Math.Clamp(undergroundPower + (dt / UNDERGROUND_MAX_POWER_SECONDS), 0, 1);
        }

        // Move.
        float speed = 50f;
        Head.ServerPos.Add(facing.X * dt * speed, facing.Y * dt * speed, facing.Z * dt * speed);

        // Don't go below world.
        if (Head.ServerPos.Y < 0) Head.ServerPos.Y = 0;

        TargetNewPlayer();

        if (currentTarget != null)
        {
            Vector3d playerPos = currentTarget.ServerPos.ToVector();
            Vector3d pos = Head.ServerPos.ToVector();

            Vector3d normal = playerPos - pos;
            normal.Normalize();

            Quaternion currentQuat = QuaternionUtility.FromToRotation(new Vector3(1f, 0f, 0f), facing);
            Quaternion targetQuat = QuaternionUtility.FromToRotation(new Vector3(1f, 0f, 0f), (Vector3)normal);

            // Slerp slightly.
            Quaternion newQuat = Quaternion.Slerp(currentQuat, targetQuat, dt);

            Quaternion downQuat = QuaternionUtility.FromToRotation(new Vector3(1f, 0f, 0f), -Vector3.UnitY);

            newQuat = Quaternion.Slerp(newQuat, downQuat, dt * (1f - undergroundPower) * 2f);

            facing = newQuat * new Vector3(1f, 0f, 0f);
        }

        Vector3d facingNormal = facing.Normalized();
        Head.ServerPos.Yaw = (float)Math.Atan2(-facingNormal.X, -facingNormal.Z);
        Head.ServerPos.Pitch = (float)Math.Asin(facingNormal.Y);
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
}