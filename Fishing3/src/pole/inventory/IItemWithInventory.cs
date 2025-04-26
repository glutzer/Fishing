using Vintagestory.API.Common;

namespace Fishing3;

/// <summary>
/// Used for bags, fishing pole, tackle box.
/// </summary>
public interface IItemWithInventory
{
    int SlotCount { get; }
    bool IsAllowedInSlot(int slotId, ItemStack stackIn);
}