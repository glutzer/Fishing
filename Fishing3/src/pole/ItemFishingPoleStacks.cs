using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;

namespace Fishing3;

public partial class ItemFishingPole : Item, IItemWithInventory
{
    public int SlotCount => 4;

    /// <summary>
    /// Implementation of IsItemAllowed delegate for the fishing pole inventory.
    /// </summary>
    public virtual bool IsAllowedInSlot(int slotId, ItemStack stackIn)
    {
        // Line (0).
        if (slotId == 0)
        {
            return stackIn.Collectible.Attributes["lineType"].Exists;
        }

        // Bobber (1).
        if (slotId == 1)
        {
            return stackIn.Collectible.GetBehavior<CollectibleBehaviorBobber>() != null;
        }

        // Bait (2)
        if (slotId == 2)
        {
            return stackIn.Collectible.GetBehavior<CollectibleBehaviorBait>() != null;
        }

        if (slotId == 3)
        {
            return false; // Catch slot, can't put items in.
        }

        return true;
    }

    /// <summary>
    /// Read an item stack from the inventory.
    /// </summary>
    public static bool ReadStack(int slotId, ItemStack? poleStack, ICoreAPI api, [NotNullWhen(true)] out ItemStack? readStack)
    {
        if (poleStack == null)
        {
            readStack = null;
            return false;
        }
        readStack = poleStack.Attributes.GetItemstack($"slot{slotId}");
        if (readStack == null) return false;
        readStack.ResolveBlockOrItem(api.World);
        return true;
    }

    /// <summary>
    /// Returns if stack was set successfully.
    /// </summary>
    public static void SetStack(int slotId, ItemStack poleStack, ItemStack? toSet)
    {
        if (toSet == null)
        {
            poleStack.Attributes.RemoveAttribute($"slot{slotId}");
            return;
        }

        poleStack.Attributes.SetItemstack($"slot{slotId}", toSet);
        return;
    }

    /// <summary>
    /// Damage a stack in a slot.
    /// Returns true if destroyed.
    /// </summary>
    public static bool DamageStack(int slotId, ItemSlot poleSlot, ICoreAPI api, int damage)
    {
        if (!ReadStack(slotId, poleSlot.Itemstack, api, out ItemStack? readStack)) return false;

        // Infinite durability item.
        if (readStack.Collectible.GetMaxDurability(readStack) == 1) return false;

        int remainingDurability = readStack.Collectible.GetRemainingDurability(readStack);
        remainingDurability -= damage;

        if (remainingDurability <= 0)
        {
            poleSlot.Itemstack.Attributes.RemoveAttribute($"slot{slotId}");
            poleSlot.MarkDirty();
            return true;
        }

        readStack.Attributes.SetInt("durability", remainingDurability);
        poleSlot.Itemstack.Attributes.SetItemstack($"slot{slotId}", readStack);

        poleSlot.MarkDirty();

        return false;
    }

    public static EntityBobber? TryGetBobber(ItemSlot rodSlot, ICoreAPI api)
    {
        if (rodSlot.Itemstack == null) return null;
        long bobberId = rodSlot.Itemstack.Attributes.GetLong("bobber", 0);
        if (bobberId == 0) return null;
        if (api.World.GetEntityById(bobberId) is not EntityBobber bobber || !bobber.Alive) return null;
        return bobber;
    }

    public static bool HasBobber(ItemSlot rodSlot)
    {
        return rodSlot.Itemstack != null && rodSlot.Itemstack.Attributes.HasAttribute("bobber");
    }

    public static void SetBobber(EntityBobber bobber, ItemSlot rodSlot)
    {
        if (rodSlot.Itemstack == null) return;

        rodSlot.Itemstack.Attributes.SetLong("bobber", bobber.EntityId);
        rodSlot.MarkDirty();
    }

    public static void RemoveBobber(ItemSlot rodSlot)
    {
        if (rodSlot.Itemstack == null) return;

        rodSlot.Itemstack.Attributes.RemoveAttribute("bobber");
        rodSlot.MarkDirty();
    }
}