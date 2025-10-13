using System.Linq;
using Vintagestory.API.Common;

namespace Fishing;

[Item]
public class ItemLabeledFlask : ItemFlask
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (api.Side == EnumAppSide.Client && byEntity.Controls.Sneak)
        {
            GuiNamedFluidMarker gui = new(slot.Itemstack);
            gui.TryOpen();
            return;
        }

        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }

    public override void OnCreatedByCrafting(ItemSlot[] inputSlots, ItemSlot outputSlot, GridRecipe byRecipe)
    {
        base.OnCreatedByCrafting(inputSlots, outputSlot, byRecipe);

        ItemSlot? flask = inputSlots.FirstOrDefault(slot => slot.Itemstack?.Item is ItemFlask);

        if (flask != null)
        {
            ItemFlask itemFlask = (ItemFlask)flask.Itemstack.Item;

            // Copy container.

            FluidContainer container = itemFlask.GetContainer(flask.Itemstack);
            outputSlot.Itemstack.Attributes.SetFluidContainer("flCont", container);
            outputSlot.MarkDirty();
        }
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        return itemStack.Attributes.GetString("label", base.GetHeldItemName(itemStack));
    }

    /// <summary>
    /// Open a different gui if off-handing a writing tool...
    /// </summary>
    public static bool IsWritingTool(ItemSlot slot)
    {
        ItemStack itemstack = slot.Itemstack;
        return itemstack != null && itemstack.Collectible.Attributes?.IsTrue("writingTool") == true;
    }
}