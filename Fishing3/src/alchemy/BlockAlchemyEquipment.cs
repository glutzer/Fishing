using MareLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Fishing3;

[Block]
public class BlockAlchemyEquipment : Block
{
    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        if (blockAccessor.GetBlockEntity(pos) is not BlockEntityAlchemyEquipment be) return SelectionBoxes;

        return be.NewSelectionBoxes;
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
    {
        return true;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;

        if (index > 0 && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAlchemyEquipment be)
        {
            AlchemyConnectionSystem system = MainAPI.GetGameSystem<AlchemyConnectionSystem>(world.Side);
            system.AddConnection(be, index - 1); // Ignore main single selection box.
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
    {
        BlockSelection? sel = MainAPI.Capi.World.Player.CurrentBlockSelection;
        int index = sel?.SelectionBoxIndex ?? 0;

        if (index > 0 && capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityAlchemyEquipment be)
        {
            AlchemyAttachPoint point = be.AlchemyAttachPoints[index - 1];

            if (point.IsOutput)
            {
                return new Vec4f(1f, 0.5f, 0f, 1f);
            }
            else
            {
                return new Vec4f(0f, 0.5f, 1f, 1f);
            }
        }


        return base.GetSelectionColor(capi, pos);
    }
}