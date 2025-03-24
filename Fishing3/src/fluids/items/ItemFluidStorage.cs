using MareLib;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

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

    public FluidContainer GetContainer(ItemStack stack)
    {
        FluidContainer? container = stack.Attributes.GetFluidContainer("flCont", api);
        if (container != null) return container;

        container = new FluidContainer(ContainerCapacity);
        stack.Attributes.SetFluidContainer("flCont", container);
        return container;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemStack, EnumItemRenderTarget target, ref ItemRenderInfo renderInfo)
    {
        renderInfo.ModelRef = fluidRenderingSystem.GetFluidItemModel(this, itemStack, renderInfo.dt) ?? renderInfo.ModelRef;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        //base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        FluidContainer container = GetContainer(inSlot.Itemstack);

        if (container.HeldStack != null)
        {
            container.HeldStack.GetFluidInfo(dsc);
        }
    }
}