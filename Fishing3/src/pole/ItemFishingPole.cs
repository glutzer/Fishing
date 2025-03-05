using MareLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Fishing3;

[Item]
public partial class ItemFishingPole : Item
{
    public MareShader? shader;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api is ICoreClientAPI)
        {
            shader = MareShaderRegistry.Get("debugfishing");
        }
    }

    // This is only called on the using client, then the server.

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        // Only do initial use.
        if (!firstEvent) return;
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (api.Side == EnumAppSide.Client)
        {
            if (currentBobber != null)
            {

            }
            else
            {
                if (byEntity.Controls.Sneak)
                {
                    FishingGameSystem sys = MainAPI.GetGameSystem<FishingGameSystem>(EnumAppSide.Client);

                    FishingInventoryPacket packet = new()
                    {
                        openInventory = true
                    };

                    sys.SendPacket(packet);
                    return;
                }

                byEntity.AnimManager.StartAnimation("ChargeRod");
            }
        }
        else
        {
            if (currentBobber != null)
            {
                currentBobber.behavior?.OnUseStart(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.UseStart, player);
            }
            else
            {
                byEntity.AnimManager.StartAnimation("ChargeRod");
            }
        }

        handling = EnumHandHandling.PreventDefaultAction;
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        return true;
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (currentBobber == null && HasBobber(slot))
        {
            // Bobber is dead, remove it.
            RemoveBobber(slot);
            //slot.MarkDirty();
            return;
        }

        if (api.Side == EnumAppSide.Client)
        {
            if (currentBobber != null)
            {

            }
            else
            {
                byEntity.AnimManager.StartAnimation("CastRod");
                byEntity.AnimManager.StopAnimation("ChargeRod");
            }
        }
        else
        {
            if (currentBobber != null)
            {
                currentBobber.behavior?.OnUseEnd(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.UseEnd, player);
            }
            else
            {
                byEntity.AnimManager.StartAnimation("CastRod");
                CastBobber(slot, secondsUsed, byEntity);
                byEntity.AnimManager.StopAnimation("ChargeRod");
            }
        }
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        // Only do initial use.
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (api.Side == EnumAppSide.Client)
        {
            // Nothing particular.
        }
        else
        {
            if (currentBobber != null)
            {
                currentBobber.behavior?.OnAttackStart(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.AttackStart, player);
            }
        }

        handling = EnumHandHandling.PreventDefaultAction;
    }

    public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        return true;
    }

    public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (api.Side == EnumAppSide.Client)
        {
            // Nothing particular.
        }
        else
        {
            if (currentBobber != null)
            {
                currentBobber.behavior?.OnAttackEnd(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.AttackEnd, player);

                if (byEntity.Controls.ShiftKey)
                {
                    currentBobber.Die();
                    RemoveBobber(slot);
                }
            }
        }
    }

    /// <summary>
    /// Cast a bobber on the server.
    /// Returns if entity spawned.
    /// </summary>
    public bool CastBobber(ItemSlot slot, float secondsUsed, Entity byEntity)
    {
        if (!ReadStack(1, slot.Itemstack, api, out ItemStack? bobberStack)) return false;

        string bobberType = bobberStack.Collectible.Attributes["bobberType"].AsString() ?? "BobberFishing";
        if (bobberType == null) return false;

        // Max velocity reached at 2 seconds.
        float velocityMultiplier = Math.Clamp(secondsUsed / 2, 0, 1) * /*velocity*/ 1;

        // Spawn entity.
        EntityProperties type = api.World.GetEntityType(new AssetLocation($"fishing:bobber"));
        EntityBobber newBobber = (EntityBobber)api.ClassRegistry.CreateEntity(type);

        Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y, 0);
        Vec3d targetNormal = (pos.AheadCopy(1, byEntity.ServerPos.Pitch, byEntity.ServerPos.Yaw) - pos).Normalize();

        newBobber.ServerPos.SetPos(pos);
        newBobber.Pos.SetPos(pos);

        newBobber.ServerPos.Motion.Set(targetNormal * velocityMultiplier);
        newBobber.Pos.Motion.Set(targetNormal * velocityMultiplier);

        api.World.SpawnEntity(newBobber);

        newBobber.SetPlayerAndBobber((EntityPlayer)byEntity, bobberType, bobberStack, slot.Itemstack);

        // Set the bobber to the rod.
        SetBobber(newBobber, slot);
        slot.MarkDirty();

        return true;
    }

    public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        return true;
    }

    public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        return true;
    }

    public override string? GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        EntityBobber? bobber = TryGetBobber(activeHotbarSlot, api);
        return bobber != null ? "RodIdle" : "HoldRod";
    }

    public override string? GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
    {
        EntityBobber? bobber = TryGetBobber(activeHotbarSlot, api);
        return bobber != null ? "RodIdle" : "HoldRod";
    }

    public override string? GetHeldTpHitAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
    {
        EntityBobber? bobber = TryGetBobber(activeHotbarSlot, api);
        return bobber != null ? "RodIdle" : "HoldRod";
    }
}