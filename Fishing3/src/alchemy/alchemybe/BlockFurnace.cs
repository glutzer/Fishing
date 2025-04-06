using MareLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Fishing3;

[Block]
public class BlockFurnace : Block, IIgnitable
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (blockSel != null && api.Side == EnumAppSide.Server)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFurnace furnace)
            {
                furnace.OnInteractServer(slot, byPlayer.Entity);
            }
        }

        base.OnBlockInteractStart(world, byPlayer, blockSel);

        return true;
    }

    public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
    {
        return secondsIgniting > 1 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
    }

    public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
    {
        handling = EnumHandling.PreventDefault;

        // Get BE, try to ignite.
        if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityFurnace furnaceBe)
        {
            furnaceBe.Ignite();
            return;
        }
    }

    public EnumIgniteState OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
    {
        return secondsIgniting > 1 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
    }
}