using Vintagestory.API.Common;

namespace Fishing;

[Block]
public class BlockFlotsam : Block
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (api.Side == EnumAppSide.Client && api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityFlotsam bea)
        {
            bea.OnClientInteract();
            api.World.PlaySoundAt(new AssetLocation("game:sounds/block/largedoor-close"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}