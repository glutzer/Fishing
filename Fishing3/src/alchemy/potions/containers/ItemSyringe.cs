using MareLib;
using Vintagestory.API.Common;

namespace Fishing3;

[Item]
public class ItemSyringe : ItemFluidStorage
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
                Fluid fluid1 = registry.GetFluid("potion");
                FluidStack stack1 = fluid1.CreateFluidStack();
                container.SetStack(stack1);
            }

            if (byEntity.Controls.Sprint)
            {
                AlchemyEffectSystem.ApplyFluid(container, 100, byEntity, byEntity);
                slot.MarkDirty();
                return;
            }

            Fluid fluid = registry.GetFluid(byEntity.Controls.Sneak ? "lava" : "water");
            FluidStack stack = fluid.CreateFluidStack();
            stack.Units = 10;

            FluidContainer.MoveFluids(stack, container);

            slot.MarkDirty();
        }
    }
}