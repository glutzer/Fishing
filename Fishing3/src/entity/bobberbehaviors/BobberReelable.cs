using MareLib;
using OpenTK.Mathematics;
using System;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Fishing3;

/// <summary>
/// Class for a bobber than can be reeled around, but doesn't catch anything.
/// </summary>
[Bobber]
public class BobberReelable : BobberBehavior
{
    public bool reeling;
    public bool releasing;
    public float maxPossibleDistance;
    public AnimationMetaData? currentAnimation;

    /// <summary>
    /// How many meters of line 1 rotation of the reel gives, for animation.
    /// </summary>
    protected const float REEL_METERS_PER_ROTATION = 2f;
    protected const float REEL_METERS_PER_SECOND = 10f;

    public BobberReelable(EntityBobber bobber, bool isServer) : base(bobber, isServer)
    {
        releasing = true; // Start released.
        if (!isServer)
        {
            if (bobber.Api.World.GetEntityById(bobber.casterId) is not EntityPlayer player) return;
            FishingPoleSoundManager.Instance.StartSound(player, "fishing:sounds/linereel", dt => { });
        }
    }

    public override void ServerInitialize(ItemStack bobberStack, ItemStack rodStack)
    {
        ItemFishingPole.ReadStack(0, rodStack, MainAPI.Sapi, out ItemStack? lineStack);
        int durability = lineStack?.Collectible.GetRemainingDurability(bobberStack) ?? 1;
        maxPossibleDistance = durability;

        // Begin at 20, maybe pass seconds used into this method to calculate initial length.
        bobber.WatchedAttributes.SetFloat("maxDistance", 1f);
        bobber.WatchedAttributes.SetFloat("distMps", 1f);
    }

    public override void FromBytes(BinaryReader reader, bool forClient)
    {
        base.FromBytes(reader, forClient);
        maxPossibleDistance = reader.ReadSingle();
    }

    public override void ToBytes(BinaryWriter writer, bool forClient)
    {
        base.ToBytes(writer, forClient);
        writer.Write(maxPossibleDistance);
    }

    public override void OnUseStart(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {
        if (releasing) OnAttackEnd(isServer, rodSlot, player);

        reeling = true;

        if (!isServer)
        {
            currentAnimation = player.Properties.Client.Animations.FirstOrDefault(a => a.Code == "LineReel")!.Clone();
            FishingPoleSoundManager.Instance.StartSound(player, "fishing:sounds/linereel", dt => { });
            player.AnimManager.StartAnimation(currentAnimation);
        }
    }

    public override void OnUseEnd(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {
        reeling = false;

        if (!isServer)
        {
            if (currentAnimation != null) currentAnimation.AnimationSpeed = 1; // For some reason, animation with 0 speed can't be stopped.
            currentAnimation = null;
            FishingPoleSoundManager.Instance.StopSound(player);
            player.AnimManager.StopAnimation("LineReel");
        }
    }

    public override void OnAttackStart(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {
        if (reeling) OnUseEnd(isServer, rodSlot, player);
        if (releasing)
        {
            OnAttackEnd(isServer, rodSlot, player);
            return;
        }

        releasing = true;

        if (!isServer)
        {
            FishingPoleSoundManager.Instance.StartSound(player, "fishing:sounds/linereel", dt => { });
        }
    }

    public override void OnAttackEnd(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {
        releasing = false;

        if (!isServer)
        {
            FishingPoleSoundManager.Instance.StopSound(player);
        }
    }

    public override void OnClientTick(float dt)
    {
        if (bobber.Api.World.GetEntityById(bobber.casterId) is not EntityPlayer player) return;

        float mps = Math.Abs(bobber.WatchedAttributes.GetFloat("distMps"));

        if (reeling && currentAnimation != null)
        {
            currentAnimation.AnimationSpeed = mps / REEL_METERS_PER_ROTATION * 2f;
        }

        if (reeling || releasing)
        {
            FishingPoleSoundManager.Instance.UpdatePitchVolume(player, mps / REEL_METERS_PER_ROTATION, mps * 0.2f);
        }
    }

    /// <summary>
    /// Try to catch an entity on the server.
    /// </summary>
    public virtual void TryCatch()
    {
        bobber.Die();

        if (bobber.Api.World.GetEntityById(bobber.casterId) is not EntityPlayer player) return;
        MainAPI.Sapi.World.PlaySoundAt("fishing:sounds/reelin", player, null, true, 16);
    }

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

        float maxDistance = bobber.WatchedAttributes.GetFloat("maxDistance");
        float oldDistance = maxDistance;

        if (releasing)
        {
            maxDistance = Math.Max(Math.Min((float)diff.Length + 0.0001f, maxPossibleDistance), maxDistance);
        }

        if (reeling) maxDistance -= REEL_METERS_PER_SECOND * dt;

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

    // Stop all animations/sounds.
    public override void Dispose(EntityDespawnData? despawn)
    {
        if (!isServer)
        {
            if (MainAPI.Capi.World.GetEntityById(bobber.casterId) is not EntityPlayer player) return;
            FishingPoleSoundManager.Instance.StopSound(player);
            player.AnimManager.StopAnimation("LineReel");

            if (reeling)
            {
                player.AnimManager.StartAnimation("RodCatch");
            }
        }
    }
}