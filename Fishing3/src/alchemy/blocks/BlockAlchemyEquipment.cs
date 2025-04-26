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
        return blockAccessor.GetBlockEntity(pos) is not BlockEntityAlchemyEquipment be ? SelectionBoxes : be.NewSelectionBoxes;
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
    {
        return true;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;

        if (api.Side == EnumAppSide.Client && byPlayer.Entity.Controls.Sneak && index < SelectionBoxes.Length && api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAlchemyEquipment bea)
        {
            bea.OnClientInteract();
        }

        if (world.Side == EnumAppSide.Client && index >= SelectionBoxes.Length && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAlchemyEquipment be)
        {
            AlchemyConnectionSystem system = MainAPI.GetGameSystem<AlchemyConnectionSystem>(world.Side);

            if (byPlayer.Entity.Controls.Sneak)
            {
                system.SendPacket(new AlchemyDisconnectPacket(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, index - SelectionBoxes.Length));
                system.ClearClientConnections();
            }
            else
            {
                system.AddConnection(be, index - SelectionBoxes.Length); // Ignore main single selection box.
            }
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
    {
        BlockSelection? sel = MainAPI.Capi.World.Player.CurrentBlockSelection;
        int index = sel?.SelectionBoxIndex ?? 0;

        if (index >= SelectionBoxes.Length && capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityAlchemyEquipment be)
        {
            AlchemyAttachPoint point = be.AlchemyAttachPoints[index - SelectionBoxes.Length];

            return point.IsOutput ? new Vec4f(1f, 0.5f, 0f, 1f) : new Vec4f(0f, 0.5f, 1f, 1f);
        }

        return base.GetSelectionColor(capi, pos);
    }
}