using MareLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Fishing3;

[Block]
public class BlockHeatPipe : Block
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world is IServerWorldAccessor serverWorld)
        {
            // Get heat pipe block entity, raise temperature.
            if (serverWorld.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHeatPipe be)
            {
                be.heatPipeInstance.ChangeTemperature(100f);
            }
        }

        return true;
    }
}