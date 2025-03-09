using MareLib;
using OpenTK.Mathematics;
using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing3;

[Bobber]
public class BobberFishable : BobberReelable
{
    public CaughtInstance? bitingFish;

    protected Accumulator accumulator = Accumulator.WithRandomInterval(5f, 30f);

    // Reel strength / catch weight determines reel speed multiplier.
    public const float BASE_REEL_STRENGTH = 10f;
    public float reelStrength;

    private float durabilityDrainAccumulation;

    public BobberFishable(EntityBobber bobber, bool isServer) : base(bobber, isServer)
    {

    }

    public override void ServerInitialize(ItemStack bobberStack, ItemStack rodStack, JsonObject properties)
    {
        base.ServerInitialize(bobberStack, rodStack, properties);

        EntityPlayer? player = bobber.GetCaster();
        if (player == null) return;

        float reelStrengthMulti = player.Stats.GetBlended("reelStrength");
        reelStrength = BASE_REEL_STRENGTH * reelStrengthMulti;
    }

    /// <summary>
    /// Drain 10 durability/s base line.
    /// </summary>
    public void DrainDurability(float dt)
    {
        durabilityDrainAccumulation += dt * 10f;
        int duraAccumInt = (int)durabilityDrainAccumulation;
        if (bobber.rodSlot == null) return;
        if (ItemFishingPole.DamageStack(0, bobber.rodSlot, bobber.Api, duraAccumInt))
        {
            bobber.Die();
            if (bobber.GetCaster() is not EntityPlayer player) return;
            MainAPI.Sapi.World.PlaySoundAt("fishing:sounds/linesnap", player.Pos.X, player.Pos.Y, player.Pos.Z, null, true, 16);
        }
        durabilityDrainAccumulation -= duraAccumInt;
    }

    public override void Dispose(EntityDespawnData? despawn)
    {
        base.Dispose(despawn);

        if (isServer)
        {
            if (bobber.rodSlot?.Itemstack == null) return;
        }
    }

    public override void ToBytes(BinaryWriter writer, bool forClient)
    {
        base.ToBytes(writer, forClient);
        writer.Write(reelStrength);
    }

    public override void FromBytes(BinaryReader reader, bool forClient)
    {
        base.FromBytes(reader, forClient);
        reelStrength = reader.ReadSingle();
    }

    public void FishBite()
    {
        if (bobber.GetCaster() is not EntityPlayer player) return;
        bitingFish = MainAPI.GetGameSystem<CatchSystem>(EnumAppSide.Server).RollCatch(bobber.ServerPos.ToVector(), player);
        bobber.ServerPos.Y -= 0.5f;
    }

    public override void TryCatch()
    {
        base.TryCatch();

        if (bitingFish == null) return;

        // Give the caught item to the player.
        if (bobber.GetCaster() is not EntityPlayer player) return;

        ItemSlot rodSlot = player.Player.InventoryManager.ActiveHotbarSlot;
        if (rodSlot.Itemstack == null || rodSlot.Itemstack.Collectible is not ItemFishingPole) return;
        ItemFishingPole.SetStack(3, rodSlot.Itemstack, bitingFish.itemStack);

        DrainDurability(0.5f);

        rodSlot.MarkDirty();
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
        if (bobber.GetCaster() is not EntityPlayer player) return;

        if (bobber.Swimming)
        {
            accumulator.Add(dt);
            if (accumulator.ConsumeAll())
            {
                if (bitingFish == null)
                {
                    FishBite();
                }
                accumulator.SetRandomInterval(5f, 30f);
            }
        }

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

            currentPosition += movement;
            diff = currentPosition - playerPos;

            // If the fish has moved past the max distance after multipliers, set new.
            // Max distance increases at a rate lower than the speed (placeholder for line drag).
            if (diff.Length > maxDistance && bitingFish.IsFighting && !reeling)
            {
                maxDistance = Math.Min(maxDistance + ((float)movement.Length * 0.2f), maxPossibleDistance);
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

                if (bitingFish.IsFighting)
                {
                    reelSpeedMultiplier *= 0.5f;
                    DrainDurability(dt);
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
                TryCatch();
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