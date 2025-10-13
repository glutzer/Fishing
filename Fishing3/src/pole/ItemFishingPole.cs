using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Fishing;

[Item]
public partial class ItemFishingPole : Item
{
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
                slot.Itemstack.Attributes.SetBool("charge", true);
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
                slot.Itemstack.Attributes.SetBool("charge", true);
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

        bool charging = slot.Itemstack.Attributes.GetBool("charge");
        if (charging) slot.Itemstack.Attributes.RemoveAttribute("charge");

        if (currentBobber == null && HasBobber(slot))
        {
            // Bobber is dead, remove it.
            RemoveBobber(slot);
        }

        if (api.Side == EnumAppSide.Client)
        {
            if (currentBobber != null)
            {

            }
            else if (charging)
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
            else if (charging)
            {
                byEntity.AnimManager.StopAnimation("ChargeRod");
                byEntity.AnimManager.StartAnimation("CastRod");
                CastBobber(slot, secondsUsed, byEntity);
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
            }
        }
    }

    /// <summary>
    /// Cast a bobber on the server.
    /// Returns if entity spawned.
    /// </summary>
    public bool CastBobber(ItemSlot slot, float secondsUsed, Entity byEntity)
    {
        if (!ReadStack(1, slot.Itemstack, api, out ItemStack? bobberStack) || !ReadStack(0, slot.Itemstack, api, out ItemStack? _)) return false;

        // Get CollectibleBehavior.
        CollectibleBehaviorBobber? behavior = bobberStack.Collectible.GetBehavior<CollectibleBehaviorBobber>();
        if (behavior == null) return false;

        string bobberType = behavior.bobberType;
        if (bobberType == null) return false;

        // Max velocity reached at 2 seconds.
        float velocityMultiplier = Math.Clamp(secondsUsed / 2, 0, 1);

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

        newBobber.SetPlayerAndBobber((EntityPlayer)byEntity, bobberType, bobberStack, slot.Itemstack, behavior.properties);

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

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine("Sneak and interact to open editor");
    }
}