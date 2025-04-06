using MareLib;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Fishing3;

/// <summary>
/// Item which may hold a fluid, such as a syringe or a flask.
/// </summary>
public class ItemFluidStorage : Item
{
    /// <summary>
    /// What capacity this item container will have when created.
    /// </summary>
    public virtual int ContainerCapacity => 100;
    public FluidItemRenderingSystem fluidRenderingSystem = null!;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api.Side == EnumAppSide.Client) fluidRenderingSystem = MainAPI.GetGameSystem<FluidItemRenderingSystem>(api.Side);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemStack, EnumItemRenderTarget target, ref ItemRenderInfo renderInfo)
    {
        renderInfo.ModelRef = fluidRenderingSystem.GetFluidItemModel(this, itemStack, renderInfo.dt) ?? renderInfo.ModelRef;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        FluidContainer container = GetContainer(inSlot.Itemstack);

        container.HeldStack?.GetFluidInfo(dsc);
    }

    /// <summary>
    /// Tries to interact with a selection, moving items to/from if it's an IFluidSink or Source.
    /// Returns true if something was selected.
    /// On the client, checks if the entity is a IFluidSink or Source.
    /// </summary>
    public bool InteractWithSelection(BlockSelection? selection, bool isInteract, ItemSlot slot, int amount)
    {
        if (selection == null || slot.Itemstack == null) return false;
        BlockEntity? entity = api.World.BlockAccessor.GetBlockEntity(selection.Position);
        if (entity == null) return false;

        if (api.Side == EnumAppSide.Client)
        {
            return (isInteract && entity is IFluidSource) || (!isInteract && entity is IFluidSink);
        }

        FluidContainer container = GetContainer(slot.Itemstack);

        if (isInteract && entity is IFluidSource source) // Move from.
        {
            FluidContainer.MoveFluids(source.GetSource(selection.SelectionBoxIndex), container, amount);
            slot.MarkDirty();
            source.MarkContainerDirty();
            return true;
        }
        else if (!isInteract && entity is IFluidSink sink) // Move to.
        {
            FluidContainer.MoveFluids(container, sink.GetSink(selection.SelectionBoxIndex), amount);
            slot.MarkDirty();
            sink.MarkContainerDirty();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get what fluid level this item is marked at, or the container capacity.
    /// </summary>
    public int GetMark(ItemStack stack)
    {
        return stack.Attributes.GetInt("mark", GetContainer(stack).Capacity);
    }

    /// <summary>
    /// Get this item's fluid container.
    /// </summary>
    public FluidContainer GetContainer(ItemStack stack)
    {
        FluidContainer? container = stack.Attributes.GetFluidContainer("flCont", api);
        if (container != null) return container;

        container = new FluidContainer(ContainerCapacity);
        stack.Attributes.SetFluidContainer("flCont", container);
        return container;
    }

    /// <summary>
    /// Tries to pick up fluid from a block selection, returns if successful.
    /// Server only.
    /// </summary>
    protected bool TryPickUpGroundFluid(ItemSlot slot, BlockSelection? selection)
    {
        if (slot.Itemstack == null || selection == null) return false;

        GridPos offset = BlockFaces.GetFaceOffset((EnumBlockFacing)selection.Face.Index);
        BlockPos newPos = selection.Position.AddCopy(offset.X, offset.Y, offset.Z);

        Block? fluidAtOffset = api.World.BlockAccessor.GetBlock(newPos, BlockLayersAccess.Fluid);
        if (fluidAtOffset.Id == 0) return false; // Air.

        string firstCodePart = fluidAtOffset.FirstCodePart();

        // I don't feel like making mappings for this right now.
        string? fluidCode = firstCodePart switch
        {
            "water" => "water",
            "saltwater" => "water",
            "lava" => "lava",
            "boilingwater" => "water",
            _ => null
        };

        if (fluidCode == null) return false;

        Fluid fluid = MainAPI.GetGameSystem<FluidRegistry>(api.Side).GetFluid(fluidCode);
        FluidContainer container = GetContainer(slot.Itemstack);
        if (container.HeldStack != null && container.HeldStack.fluid != fluid) return false; // Incompatible fluid.

        FluidStack newStack = fluid.CreateFluidStack(GetMark(slot.Itemstack));

        FluidContainer.MoveFluids(newStack, container);

        slot.MarkDirty();

        return true;
    }
}