using MareLib;
using Vintagestory.API.Common;

namespace Fishing3;

[Block]
public class BlockFlotsam : Block
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (api.Side == EnumAppSide.Client && api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFlotsam bea)
        {
            bea.OnClientInteract();
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}