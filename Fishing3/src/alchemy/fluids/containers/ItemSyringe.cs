using MareLib;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

public enum SyringeState
{
    InjectingSelf,
    InjectingEntity,
    Injected
}

[Item]
public class ItemSyringe : ItemFluidStorage
{
    public override int ContainerCapacity => 1000;

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (!firstEvent) return;

        /*
        if (api.Side == EnumAppSide.Server)
        {
            ItemStack parchment = MainAPI.GetServerSystem<AlchemyRecipeRegistry>().GenerateRandomParchment();
            api.World.SpawnItemEntity(parchment, byEntity.ServerPos.AsBlockPos);
        }
        */

        if (byEntity.Controls.Sneak && api.Side == EnumAppSide.Client)
        {
            new GuiFluidMarker(slot.Itemstack).TryOpen();
            return;
        }

        handling = EnumHandHandling.Handled;

        if (!InteractWithSelection(blockSel, true, slot, GetMark(slot.Itemstack)))
        {
            if (api.Side == EnumAppSide.Server) TryPickUpGroundFluid(slot, blockSel);
        }

        if (entitySel != null && byEntity.Pos.DistanceTo(entitySel.Entity.ServerPos) < 20)
        {
            SetState(slot, SyringeState.InjectingEntity);
            byEntity.AnimManager.StartAnimation("EntityInject");
        }
        else
        {
            SetState(slot, SyringeState.InjectingSelf);
            byEntity.AnimManager.StartAnimation("Inject");
        }
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        SyringeState state = GetState(slot);

        FluidContainer container = GetContainer(slot.Itemstack);

        if (state == SyringeState.InjectingSelf)
        {
            if (secondsUsed > 1f)
            {
                SetState(slot, SyringeState.Injected);

                if (api.Side == EnumAppSide.Server)
                {
                    api.World.PlaySoundAt(new AssetLocation("fishing:sounds/stab"), byEntity, null, false);
                    AlchemyEffectSystem.ApplyFluid(container, GetMark(slot.Itemstack), byEntity, byEntity, ApplicationMethod.Blood);
                    slot.MarkDirty();
                }
            }
        }

        if (state == SyringeState.InjectingEntity)
        {
            if (secondsUsed > 0.5f)
            {
                SetState(slot, SyringeState.Injected);

                if (api.Side == EnumAppSide.Server && entitySel != null && byEntity.Pos.DistanceTo(entitySel.Entity.ServerPos) < 20)
                {
                    api.World.PlaySoundAt(new AssetLocation("fishing:sounds/stab"), byEntity, null, false);
                    AlchemyEffectSystem.ApplyFluid(container, GetMark(slot.Itemstack), byEntity, entitySel.Entity, ApplicationMethod.Blood);
                    slot.MarkDirty();
                }
            }
        }

        return true;
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

        byEntity.AnimManager.StopAnimation("Inject");
        byEntity.AnimManager.StopAnimation("EntityInject");
    }

    public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        SyringeState state = GetState(slot);
        float minTime = state == SyringeState.Injected ? 3f : 0f;
        return secondsUsed > minTime;
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        handling = EnumHandHandling.PreventDefaultAction;

        InteractWithSelection(blockSel, false, slot, GetMark(slot.Itemstack));

        if (entitySel != null && byEntity.Pos.DistanceTo(entitySel.Entity.ServerPos) < 20)
        {
            SetState(slot, SyringeState.InjectingEntity);
            byEntity.AnimManager.StartAnimation("EntityInject");
        }
        else
        {
            SetState(slot, SyringeState.InjectingSelf);
            byEntity.AnimManager.StartAnimation("Inject");
        }
    }

    public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        SyringeState state = GetState(slot);
        FluidContainer container = GetContainer(slot.Itemstack);

        Fluid blood = MainAPI.GetGameSystem<FluidRegistry>(api.Side).GetFluid("blood");
        int toMove = Math.Min(container.RoomLeft, GetMark(slot.Itemstack));

        if (state == SyringeState.InjectingSelf)
        {
            if (secondsUsed > 1f)
            {
                SetState(slot, SyringeState.Injected);

                if (api.Side == EnumAppSide.Server)
                {
                    api.World.PlaySoundAt(new AssetLocation("fishing:sounds/stab"), byEntity, null, false);

                    if (container.HeldStack == null || container.HeldStack.fluid == blood)
                    {
                        FluidStack bloodStack = blood.CreateFluidStack();
                        bloodStack.Units = toMove;
                        bloodStack.Attributes.SetLong("entityId", byEntity.EntityId);
                        bloodStack.Attributes.SetString("entityType", byEntity.Code.FirstCodePart());

                        FluidContainer.MoveFluids(bloodStack, container);

                        byEntity.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Player,
                            SourceEntity = byEntity,
                            Type = EnumDamageType.PiercingAttack,
                            KnockbackStrength = 0f
                        }, toMove * 0.01f);

                        if (!byEntity.Alive) byEntity.StopAnimation("Inject");

                        slot.MarkDirty();
                    }
                }
            }
        }

        if (state == SyringeState.InjectingEntity)
        {
            if (secondsUsed > 0.5f)
            {
                SetState(slot, SyringeState.Injected);

                if (api.Side == EnumAppSide.Server && entitySel != null && byEntity.Pos.DistanceTo(entitySel.Entity.ServerPos) < 20)
                {
                    api.World.PlaySoundAt(new AssetLocation("fishing:sounds/stab"), byEntity, null, false);

                    if (container.HeldStack == null || container.HeldStack.fluid == blood)
                    {
                        FluidStack bloodStack = blood.CreateFluidStack();

                        // Limit amount for entities.
                        toMove = Math.Min(toMove, 100);

                        bloodStack.Units = toMove;
                        bloodStack.Attributes.SetLong("entityId", entitySel.Entity.EntityId);
                        bloodStack.Attributes.SetString("entityType", entitySel.Entity.Code.FirstCodePart());

                        FluidContainer.MoveFluids(bloodStack, container);

                        entitySel.Entity.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Player,
                            SourceEntity = byEntity,
                            Type = EnumDamageType.PiercingAttack,
                            KnockbackStrength = 0f
                        }, toMove * 0.01f);

                        slot.MarkDirty();
                    }
                }
            }
        }

        return true;
    }

    public override void OnHeldAttackStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        base.OnHeldAttackStop(secondsUsed, slot, byEntity, blockSel, entitySel);

        byEntity.AnimManager.StopAnimation("Inject");
        byEntity.AnimManager.StopAnimation("EntityInject");
    }

    public override bool OnHeldAttackCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
    {
        SyringeState state = GetState(slot);
        float minTime = state == SyringeState.Injected ? 3f : 0f;
        return secondsUsed > minTime;
    }

    public static void SetState(ItemSlot slot, SyringeState state)
    {
        slot.Itemstack.Attributes.SetInt("state", (int)state);
    }

    public static SyringeState GetState(ItemSlot slot)
    {
        return (SyringeState)slot.Itemstack.Attributes.GetInt("state", (int)SyringeState.Injected);
    }

    public override string? GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        return null;
    }

    public override string? GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
    {
        return null;
    }

    public override string? GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
    {
        return null;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        int mark = GetMark(inSlot.Itemstack);
        dsc.AppendLine($"Mark: {mark}mL");

        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        FluidContainer cont = GetContainer(itemStack);

        string baseName = base.GetHeldItemName(itemStack);

        if (cont.HeldStack != null)
        {
            baseName += $" ({cont.HeldStack.fluid.GetName(cont.HeldStack)})";
        }

        return baseName;
    }
}