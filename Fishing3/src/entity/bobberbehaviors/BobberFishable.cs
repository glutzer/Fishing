using MareLib;
using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Fishing3;

[Bobber]
public class BobberFishable : BobberReelable
{
    public CaughtInstance? bitingFish;

    public const float FISH_MPS = 3f;
    public const float FISH_MASS = 10f;

    public BobberFishable(EntityBobber bobber, bool isServer) : base(bobber, isServer)
    {

    }

    public override void ServerInitialize(ItemStack bobberStack, ItemStack rodStack)
    {
        base.ServerInitialize(bobberStack, rodStack);

        bitingFish = MainAPI.GetGameSystem<CatchSystem>(EnumAppSide.Server).RollCatch(bobber.Pos.ToVector());
    }

    public override void TryCatch()
    {
        base.TryCatch();

        if (bobber.Api.World.GetEntityById(bobber.casterId) is not EntityPlayer player) return;

        ItemSlot rodSlot = player.Player.InventoryManager.ActiveHotbarSlot;
        if (rodSlot.Itemstack == null || rodSlot.Itemstack.Collectible is not ItemFishingPole) return;

        if (bitingFish == null) return;

        ItemFishingPole.SetStack(4, rodSlot.Itemstack, bitingFish.itemStack);
    }

    public Vector3d GetFishMovement(Vector3d currentPosition, Vector3d playerPos, float dt)
    {
        Vector3d normalToPlayer = currentPosition - playerPos;
        normalToPlayer.Y = 0;
        normalToPlayer.Normalize();

        double cos = Math.Cos(radianOffset);
        double sin = Math.Sin(radianOffset);

        normalToPlayer.X = (normalToPlayer.X * cos) - (normalToPlayer.Z * sin);
        normalToPlayer.Z = (normalToPlayer.X * sin) + (normalToPlayer.Z * cos);
        normalToPlayer.Normalize();

        float motionMulti = !bobber.CollidedVertically && !bobber.Swimming ? 0f : 1f;
        normalToPlayer *= dt * FISH_MPS * motionMulti;

        return normalToPlayer;
    }

    public double radianOffset;

    public override void OnServerPhysicsTick(float dt)
    {
        if (bobber.Api.World.GetEntityById(bobber.casterId) is not EntityPlayer player) return;

        Vector3d currentPosition = bobber.ServerPos.ToVector();
        Vector3d playerPos = player.ServerPos.ToVector();

        Vec3d pos = player.ServerPos.XYZ.Add(0, player.LocalEyePos.Y, 0);
        Vec3d targetNormal = (pos.AheadCopy(1, Math.PI, player.ServerPos.Yaw) - pos).Normalize();
        Vector3d normalVec = new(targetNormal.X, targetNormal.Y, targetNormal.Z);
        playerPos += normalVec * 3.5f;
        Vector3d diff = currentPosition - playerPos;

        // Weight the radian offset to make the normal of the diff point towards the normalVec.
        double angleToNormalVec = Math.Atan2(normalVec.Z, normalVec.X);
        double angleToDiff = Math.Atan2(diff.Z, diff.X);
        double angleDifference = angleToNormalVec - angleToDiff;

        radianOffset += angleDifference * 0.1; // Weighting factor of 0.1.
        radianOffset = Math.Clamp(radianOffset, -Math.PI * 0.2f, Math.PI * 0.2f);

        float maxDistance = bobber.WatchedAttributes.GetFloat("maxDistance");
        float oldDistance = maxDistance;

        // Fish.
        if (bitingFish != null)
        {
            Vector3d movement = GetFishMovement(currentPosition, playerPos, dt);

            currentPosition += movement * 3;

            diff = currentPosition - playerPos;

            // If the fish has moved past the max distance after multipliers, set new.
            if (diff.Length > maxDistance)
            {
                maxDistance = Math.Min(maxDistance + ((float)movement.Length * 0.2f), maxPossibleDistance);
            }
        }
        // Fish.

        if (releasing)
        {
            maxDistance = Math.Max(Math.Min((float)diff.Length + 0.0001f, maxPossibleDistance), maxDistance);
        }

        if (reeling)
        {
            if (bitingFish != null)
            {
                double dist = Math.Abs(radianOffset) / (Math.PI * 0.2);

                maxDistance -= REEL_METERS_PER_SECOND * dt * (0.2f + ((float)dist * 0.2f));
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

        // Update bobber position
        bobber.ServerPos.SetPos(currentPosition.X, currentPosition.Y, currentPosition.Z);
        bobber.Pos.SetPos(currentPosition.X, currentPosition.Y, currentPosition.Z);
    }
}