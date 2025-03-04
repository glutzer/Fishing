using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Fishing3;

public delegate bool ItemAllowedDelegate(int slotId, ItemStack stackIn);

/// <summary>
/// Special inventory that will put items into/out of an item stack.
/// </summary>
public class ItemInventory : InventoryBase
{
    private readonly ItemSlotInventory[] slots;
    private readonly ItemSlot forSlot; // Usually the held item slot.
    private readonly ItemStack forStack;

    public ItemInventory(string className, string instanceId, ICoreAPI api, int slotCount, ItemAllowedDelegate itemAllowed, ItemSlot forSlot) : base(className, instanceId, api)
    {
        slots = new ItemSlotInventory[slotCount];

        this.forSlot = forSlot;
        forStack = forSlot.Itemstack; // Should not be null since opening with a held item, but do a check later.

        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = new ItemSlotInventory(this, i, itemAllowed);

            // Load stacks from item.
            if (forStack.Attributes.HasAttribute($"slot{i}"))
            {
                ItemStack stack = forStack.Attributes.GetItemstack($"slot{i}");
                if (stack == null) continue;
                stack.ResolveBlockOrItem(api.World);
                slots[i].Itemstack = stack;
            }
        }
    }

    public override ItemSlot this[int slotId] { get => slots[slotId]; set => slots[slotId] = (ItemSlotInventory)value; }

    public override int Count => slots.Length;

    public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
    {
        return base.CanContain(sinkSlot, sourceSlot) && CanTakeFrom();
    }

    public override bool CanPlayerModify(IPlayer player, EntityPos position)
    {
        return base.CanPlayerModify(player, position) && CanTakeFrom();
    }

    public override bool CanPlayerAccess(IPlayer player, EntityPos position)
    {
        return base.CanPlayerAccess(player, position) && CanTakeFrom();
    }

    public bool CanTakeFrom()
    {
        return forSlot.Itemstack == forStack;
    }

    public override void OnItemSlotModified(ItemSlot slot)
    {
        base.OnItemSlotModified(slot);
        ItemSlotInventory invSlot = (ItemSlotInventory)slot;

        forSlot.MarkDirty();

        if (invSlot.Itemstack == null)
        {
            forStack.Attributes.RemoveAttribute($"slot{invSlot.slotId}");
            return;
        }

        forStack.Attributes.SetItemstack($"slot{invSlot.slotId}", invSlot.Itemstack);
    }

    // This doesn't need to be set? Everything is saved in the item stack. This is just an interface.

    public override void FromTreeAttributes(ITreeAttribute tree)
    {

    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {

    }
}