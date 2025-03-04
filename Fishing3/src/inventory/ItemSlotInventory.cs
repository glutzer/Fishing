using Vintagestory.API.Common;

namespace Fishing3;

public class ItemSlotInventory : ItemSlot
{
    public readonly int slotId;
    private readonly ItemAllowedDelegate itemAllowed;
    private readonly ItemInventory itemInventory;

    public ItemSlotInventory(ItemInventory inventory, int slotId, ItemAllowedDelegate itemAllowed) : base(inventory)
    {
        itemInventory = inventory;
        this.slotId = slotId;
        this.itemAllowed = itemAllowed;
    }

    public override bool CanHold(ItemSlot sourceSlot)
    {
        return base.CanHold(sourceSlot) && itemAllowed(slotId, sourceSlot.Itemstack);
    }

    public override bool CanTake()
    {
        return base.CanTake() && itemInventory.CanTakeFrom();
    }
}