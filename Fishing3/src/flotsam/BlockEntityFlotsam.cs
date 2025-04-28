using MareLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Fishing3;

public static class InventoryExtensions
{
    public static void CloseForAllPlayers(this InventoryBase inv)
    {
        HashSet<string> openedByPlayers = inv.openedByPlayerGUIds;

        foreach (string uid in openedByPlayers)
        {
            IPlayer? player = inv.Api.World.PlayerByUid(uid);
            if (player == null) continue;
            player.InventoryManager.CloseInventory(inv);
        }
    }
}

[BlockEntity]
public class BlockEntityFlotsam : BlockEntity
{
    protected const int OPEN_INVENTORY_PACKET = 5555;
    protected const int CLOSE_INVENTORY_PACKET = 5556;
    public readonly InventoryGeneric genericInventory = new(9, null, null);

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        genericInventory.LateInitialize($"flotsam-{Pos.X}-{Pos.Y}-{Pos.Z}", api);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        genericInventory.ToTreeAttributes(tree);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        genericInventory.FromTreeAttributes(tree);
    }

    public void OnClientInteract()
    {
        if (Api is ICoreClientAPI capi)
        {
            capi.Network.SendBlockEntityPacket(Pos, OPEN_INVENTORY_PACKET);
        }
    }

    /// <summary>
    /// Call from gui to close inventory.
    /// </summary>
    public void SendClosePacketFromClient()
    {
        if (Api is ICoreClientAPI capi)
        {
            genericInventory.Close(MainAPI.Capi.World.Player);
            capi.Network.SendBlockEntityPacket(Pos, CLOSE_INVENTORY_PACKET);
        }
    }

    public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetId, byte[] data)
    {
        base.OnReceivedClientPacket(fromPlayer, packetId, data);

        if (Api is ICoreServerAPI sapi && fromPlayer is IServerPlayer servPlayer)
        {
            if (packetId == OPEN_INVENTORY_PACKET)
            {
                servPlayer.InventoryManager.OpenInventory(genericInventory);
                sapi.Network.SendBlockEntityPacket(servPlayer, Pos, OPEN_INVENTORY_PACKET);
            }

            if (packetId == CLOSE_INVENTORY_PACKET)
            {
                servPlayer.InventoryManager.CloseInventory(genericInventory);
            }
        }
    }

    public override void OnReceivedServerPacket(int packetId, byte[] data)
    {
        base.OnReceivedServerPacket(packetId, data);

        if (packetId == OPEN_INVENTORY_PACKET)
        {
            MainAPI.Capi.World.Player.InventoryManager.OpenInventory(genericInventory);
            new GuiFlotsam(this).TryOpen();
        }
    }

    public override void OnBlockPlaced(ItemStack? byItemStack)
    {
        base.OnBlockPlaced(byItemStack);

        // Initialize inventory on server.
        if (byItemStack == null || Api.Side == EnumAppSide.Client) return;

        genericInventory.FromTreeAttributes(byItemStack.Attributes.Clone());
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);

        if (Api.Side == EnumAppSide.Client) return;

        genericInventory.DropAll(Pos.ToVec3d().Add(0.5f, 0.5f, 0.5f));
        genericInventory.CloseForAllPlayers();
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        genericInventory.CloseForAllPlayers();
    }
}