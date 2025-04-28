using MareLib;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing3;

[Bobber]
public class BobberFishable : BobberReelable
{
    public CaughtInstance? bitingFish;

    // Reel strength / catch weight determines reel speed multiplier.
    public const float BASE_REEL_STRENGTH = 5f;
    protected float reelStrength = 1f;
    protected float durabilityDrainAccumulation;

    // Current time left for something to bite.
    protected float biteTimer = -1f;
    protected float poolBiteSpeed;

    public const float BASE_BITE_TIME = 60f;

    public BobberFishable(EntityBobber bobber, bool isServer) : base(bobber, isServer)
    {

    }

    public override void ServerInitialize(ItemStack bobberStack, ItemStack rodStack, JsonObject properties)
    {
        base.ServerInitialize(bobberStack, rodStack, properties);

        // This is only required on the server, not saved.
        float reelStrengthMulti = bobber.Caster?.Stats.GetBlended("reelStrength") ?? 1f;
        reelStrength = BASE_REEL_STRENGTH * reelStrengthMulti;
    }

    /// <summary>
    /// Drain catch weight/s durability base line.
    /// </summary>
    public void DrainDurabilityServer(float dt)
    {
        //durabilityDrainAccumulation += dt * 10f;
        durabilityDrainAccumulation += dt * bitingFish?.kg ?? 0f;

        int duraAccumInt = (int)durabilityDrainAccumulation;
        if (bobber.rodSlot == null || duraAccumInt == 0) return;
        if (ItemFishingPole.DamageStack(0, bobber.rodSlot, bobber.Api, duraAccumInt))
        {
            bobber.Die();
            if (bobber.Caster is not EntityPlayer player) return;
            MainAPI.Sapi.World.PlaySoundAt("fishing:sounds/linesnap", player.Pos.X, player.Pos.Y, player.Pos.Z, null, true, 16);
        }
        durabilityDrainAccumulation -= duraAccumInt;
    }

    public override void TryCatchServer()
    {
        base.TryCatchServer();

        if (bitingFish == null) return;
        if (bobber.rodSlot == null || bobber.rodSlot.Itemstack == null || bobber.rodSlot.Itemstack.Collectible is not ItemFishingPole) return;

        if (bitingFish.itemStack != null) ItemFishingPole.SetStack(3, bobber.rodSlot.Itemstack, bitingFish.itemStack);
        bitingFish.OnCaught?.Invoke(bobber.ServerPos.ToVector());

        DrainDurabilityServer(0.5f);

        bobber.rodSlot.MarkDirty();
    }

    public override unsafe void OnReceivedServerPacket(int packetId, byte[]? data)
    {
        if (packetId == 6000 && bobber.Caster?.IsSelf() == true)
        {
            if (!MainAPI.Capi.Forms.Window.IsFocused)
            {
                GLFW.RequestWindowAttention(MainAPI.Capi.Forms.Window.WindowPtr);
            }
        }
    }

    protected void UpdateBiting(float dt)
    {
        void ResetBiteTimer()
        {
            // 0.5x - 1.5x base time.
            biteTimer = (BASE_BITE_TIME * 0.5f) + (Random.Shared.NextSingle() * BASE_BITE_TIME);
        }

        if (!bobber.Swimming || bitingFish != null || bobber.Caster == null) return;

        if (biteTimer == -1f)
        {
            ResetBiteTimer();
            poolBiteSpeed = CatchSystem.GetBiteSpeedMultiplier(bobber.ServerPos.ToVector(), bobber.Caster);
        }

        float distMulti = CatchSystem.GetPlayerDistanceBiteSpeedMultiplier(bobber.ServerPos.ToVector(), bobber.Caster);
        biteTimer -= dt * distMulti * poolBiteSpeed;

        if (biteTimer < 0)
        {
            biteTimer = 0;
            bitingFish = MainAPI.GetGameSystem<CatchSystem>(EnumAppSide.Server).RollCatch(bobber.ServerPos.ToVector(), bobber.Caster);
            if (bitingFish == null)
            {
                ResetBiteTimer();
            }
            else
            {
                MainAPI.Server.BroadcastEntityPacket(bobber.EntityId, 6000);
                releasing = false;
                bobber.ServerPos.Y -= 1f;
            }
        }
    }

    /// <summary>
    /// Get the normal of the fish's movement * the fish's speed.
    /// X meters per second.
    /// </summary>
    public Vector3d GetFishMovement(Vector3d currentPosition, Vector3d playerPos, float dt)
    {
        if (bitingFish == null) return Vector3d.Zero;

        Vector3d normalToPlayer = currentPosition - playerPos;
        normalToPlayer.Y = 0;
        normalToPlayer.Normalize();

        float motionMulti = !bobber.CollidedVertically && !bobber.Swimming ? 0f : 1f;
        normalToPlayer *= dt * bitingFish.speed * motionMulti;

        return normalToPlayer;
    }

    public override void OnServerPhysicsTick(float dt)
    {
        if (bobber.Caster is not EntityPlayer player) return;

        UpdateBiting(dt);

        Vector3d currentPosition = bobber.ServerPos.ToVector();
        Vector3d playerPos = player.ServerPos.ToVector();

        Vec3d pos = player.ServerPos.XYZ.Add(0, player.LocalEyePos.Y, 0);
        Vec3d targetNormal = (pos.AheadCopy(1, Math.PI, player.ServerPos.Yaw) - pos).Normalize();
        Vector3d normalVec = new(targetNormal.X, targetNormal.Y, targetNormal.Z);

        playerPos += normalVec * 3.5f;
        Vector3d diff = currentPosition - playerPos;

        float maxDistance = bobber.WatchedAttributes.GetFloat("maxDistance");
        float oldDistance = maxDistance;

        if (bitingFish != null)
        {
            bitingFish.UpdateStamina(dt);

            Vector3d movement = GetFishMovement(currentPosition, playerPos, dt);

            if ((diff + movement).Length > maxDistance && !releasing)
            {
                movement *= 0.2f;
            }

            currentPosition += movement;

            diff = currentPosition - playerPos;

            // If the fish has moved past the max distance after multipliers, set new.
            // Max distance increases at a rate lower than the speed (placeholder for line drag).
            if (diff.Length > maxDistance && bitingFish.IsFighting && !reeling)
            {
                maxDistance = Math.Min(maxDistance + (float)movement.Length, maxPossibleDistance);
            }
        }

        if (releasing)
        {
            maxDistance = Math.Max(Math.Min((float)diff.Length + 0.0001f, maxPossibleDistance), maxDistance);
        }

        if (reeling)
        {
            if (bitingFish != null && diff.Length > maxDistance - 1f)
            {
                // Reel slower based on fish fighting.
                float reelSpeedMultiplier = reelStrength / bitingFish.kg;
                if (reelSpeedMultiplier > 1f) reelSpeedMultiplier = 1f;

                if (bitingFish.IsFighting)
                {
                    reelSpeedMultiplier *= 0.5f;
                    DrainDurabilityServer(dt);
                }

                maxDistance -= REEL_METERS_PER_SECOND * dt * reelSpeedMultiplier;
            }
            else
            {
                maxDistance -= REEL_METERS_PER_SECOND * dt;
            }
        }

        if (maxDistance < 2f)
        {
            maxDistance = 2f;

            if (reeling)
            {
                TryCatchServer();
                return;
            }
        }

        // Mps for client to calculate things like reel speed, reel sound, release sound. 20 tps.
        float mps = (maxDistance - oldDistance) * 20f;

        bobber.WatchedAttributes.SetFloat("distMps", mps);
        bobber.WatchedAttributes.SetFloat("maxDistance", maxDistance);

        if (diff.Length > maxDistance)
        {
            diff.Normalize();
            currentPosition = playerPos + (diff * maxDistance);

            Vec3d motion = bobber.ServerPos.Motion;
            Vector3d motionStruct = new(motion.X, motion.Y, motion.Z);

            double velocityProjection = Vector3d.Dot(motionStruct, diff);

            if (velocityProjection > 0) // Only correct velocity if it's moving outward
            {
                motionStruct -= velocityProjection * diff; // Remove outward velocity component.
                motionStruct *= 0.98f;
                motion.Set(motionStruct.X, motionStruct.Y, motionStruct.Z);
            }
        }

        Vector3d startPos = bobber.ServerPos.ToVector();
        currentPosition = collisionTester.DoCollision(startPos, currentPosition, bobber, bobber.Api);

        // Update bobber position
        bobber.ServerPos.SetPos(currentPosition.X, currentPosition.Y, currentPosition.Z);
        bobber.Pos.SetPos(currentPosition.X, currentPosition.Y, currentPosition.Z);
    }
}