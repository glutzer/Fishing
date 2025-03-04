using Vintagestory.API.Common;

namespace Fishing3.src;

/// <summary>
/// Used for bags, fishing pole, tackle box.
/// </summary>
public interface IItemWithInventory
{
    public int SlotCount { get; }
    public bool IsAllowedInSlot(int slotId, ItemStack stackIn);
}