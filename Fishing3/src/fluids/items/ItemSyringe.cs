using MareLib;
using Vintagestory.API.Common;

namespace Fishing3;

[Item]
public class ItemSyringe : FluidStorageItem
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        handling = EnumHandHandling.Handled;

        if (api.Side == EnumAppSide.Server)
        {
            FluidContainer container = GetContainer(slot.Itemstack);
            FluidRegistry registry = MainAPI.GetGameSystem<FluidRegistry>(api.Side);

            if (container.HeldStack == null)
            {
                Fluid fluid = registry.GetFluid("potion");
                FluidStack stack = fluid.CreateFluidStack();
                container.SetStack(stack);
            }
            else
            {
                Fluid fluid = registry.GetFluid(byEntity.Controls.Sneak ? "lava" : "water");
                FluidStack stack = fluid.CreateFluidStack();
                stack.Units = 10;

                FluidContainer.MoveFluids(stack, container);
            }

            slot.MarkDirty();
        }
    }
}