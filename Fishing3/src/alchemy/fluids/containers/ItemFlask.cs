using MareLib;
using OpenTK.Mathematics;
using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Fishing3;

[Item]
public class ItemFlask : ItemFluidStorage, IContainedMeshSource
{
    protected int flaskCapacity;
    public override int ContainerCapacity => flaskCapacity;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        flaskCapacity = Attributes["maxMl"].AsInt(100);

        if (Code.Path == "flask-large-unlabeled" && api.Side == EnumAppSide.Server)
        {
            FluidRegistry registry = MainAPI.GetGameSystem<FluidRegistry>(api.Side);
            if (CreativeInventoryStacks == null)
            {
                CreativeInventoryStacks = registry.GetCreativeStacks(this);
            }
            else
            {
                CreativeInventoryStacks = CreativeInventoryStacks.Concat(registry.GetCreativeStacks(this)).ToArray();
            }
        }
    }

    /// <summary>
    /// I don't know how to do this for the creative tab without doing it on before render.
    /// </summary>
    public void CheckFill(ItemStack thisStack)
    {
        // Check if a flask has fillWith when comparing, set container.
        if (thisStack.Attributes.HasAttribute("fillWith"))
        {
            string? fillCode = thisStack.Attributes.GetString("fillWith");

            if (fillCode != null)
            {
                FluidContainer container = GetContainer(thisStack);
                MainAPI.GetGameSystem<FluidRegistry>(api.Side).TryGetFluid(fillCode, out Fluid? fluid);

                if (fluid != null)
                {
                    FluidStack stack = fluid.CreateFluidStack(ContainerCapacity);
                    container.SetStack(stack);
                }
            }

            thisStack.Attributes.RemoveAttribute("fillWith");
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        CheckFill(thisStack);
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemStack, EnumItemRenderTarget target, ref ItemRenderInfo renderInfo)
    {
        CheckFill(itemStack);
        base.OnBeforeRender(capi, itemStack, target, ref renderInfo);
    }

    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack? extractedStack = null)
    {
        CheckFill(slot.Itemstack);
        base.OnModifiedInInventorySlot(world, slot, extractedStack);
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (!firstEvent) return;

        if (byEntity.Controls.Sneak && api.Side == EnumAppSide.Client)
        {
            new GuiFluidMarker(slot.Itemstack).TryOpen();
            return;
        }

        if (!InteractWithSelection(blockSel, true, slot, GetMark(slot.Itemstack)))
        {
            if (api.Side == EnumAppSide.Server) TryPickUpGroundFluid(slot, blockSel);
        }

        if (blockSel == null || api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is not IFluidSource)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        SetState(slot, SyringeState.InjectingSelf);
        byEntity.AnimManager.StartAnimation("Eat");

        handling = EnumHandHandling.Handled;
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        handling = EnumHandHandling.PreventDefaultAction;

        InteractWithSelection(blockSel, false, slot, GetMark(slot.Itemstack));
    }

    public static void SetState(ItemSlot slot, SyringeState state)
    {
        slot.Itemstack.Attributes.SetInt("state", (int)state);
    }

    public static SyringeState GetState(ItemSlot slot)
    {
        return (SyringeState)slot.Itemstack.Attributes.GetInt("state", (int)SyringeState.Injected);
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        SyringeState state = GetState(slot);

        if (state == SyringeState.InjectingSelf)
        {
            if (secondsUsed > 1f)
            {
                // Play potion sound.
                SetState(slot, SyringeState.Injected);

                FluidContainer container = GetContainer(slot.Itemstack);

                if (container != null && api.Side == EnumAppSide.Server)
                {
                    int mark = GetMark(slot.Itemstack);
                    byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/player/drink1"), byEntity, null, true, 8, 5);

                    AlchemyEffectSystem.ApplyFluid(container, mark, byEntity, byEntity, ApplicationMethod.Consume);

                    slot.MarkDirty();
                }
            }
        }

        return true;
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

        byEntity.AnimManager.StopAnimation("Eat");

        slot.Itemstack.Attributes.RemoveAttribute("state");
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

        if (inSlot.Itemstack.Attributes.HasAttribute("fillWith"))
        {
            string? fillCode = inSlot.Itemstack.Attributes.GetString("fillWith");
            dsc.AppendLine($"Fill with: {Lang.Get($"fluid-{fillCode}")}");
        }
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

    // Chunks do not store RGB so these are white.

    public MeshData GenMesh(ItemStack itemStack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        FluidContainer container = GetContainer(itemStack);

        if (container.HeldStack == null)
        {
            return FluidItemRenderingSystem.CreateFluidItemModel(this, Vector4.Zero, 0, 0);
        }

        float glow = container.HeldStack.fluid.GetGlowLevel(container.HeldStack);
        float fill = MathF.Round(container.FillPercent, 2);

        return FluidItemRenderingSystem.CreateFluidItemModel(this, Vector4.One, glow, fill);
    }

    public string GetMeshCacheKey(ItemStack itemStack)
    {
        FluidContainer container = GetContainer(itemStack);

        if (container.HeldStack == null)
        {
            return $"{Code}-empty";
        }

        float glow = container.HeldStack.fluid.GetGlowLevel(container.HeldStack);
        float fill = MathF.Round(container.FillPercent, 2);

        return $"{Code}-{glow}-{fill}";
    }
}