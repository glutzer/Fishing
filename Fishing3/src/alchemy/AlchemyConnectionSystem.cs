using ProtoBuf;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Fishing3;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AlchemyMarkerPacket
{
    public int mark;
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AlchemyFlaskNamePacket
{
    public string? name;
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AlchemyDisconnectPacket
{
    public int x;
    public int y;
    public int z;

    public int index;

    public AlchemyDisconnectPacket()
    {
    }

    public AlchemyDisconnectPacket(int x, int y, int z, int index)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.index = index;
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class AlchemyConnectionPacket
{
    public int fromX;
    public int fromY;
    public int fromZ;

    public int toX;
    public int toY;
    public int toZ;

    public int fromIndex;
    public int toIndex;

    public AlchemyConnectionPacket()
    {
    }

    public AlchemyConnectionPacket(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int fromIndex, int toIndex)
    {
        this.fromX = fromX;
        this.fromY = fromY;
        this.fromZ = fromZ;
        this.toX = toX;
        this.toY = toY;
        this.toZ = toZ;
        this.fromIndex = fromIndex;
        this.toIndex = toIndex;
    }
}

/// <summary>
/// Logs pending connections on client, dispatches packets.
/// </summary>
[GameSystem]
public class AlchemyConnectionSystem : NetworkedGameSystem
{
    private BlockEntityAlchemyEquipment? lastSelectedEquipment;
    private int lastSelectedIndex = -1;

    public AlchemyConnectionSystem(bool isServer, ICoreAPI api) : base(isServer, api, "alchemyconn")
    {
    }

    public void AddConnection(BlockEntityAlchemyEquipment equipment, int index)
    {
        if (lastSelectedEquipment == null || lastSelectedIndex < 0)
        {
            lastSelectedEquipment = equipment;
            lastSelectedIndex = index;

            MainAPI.Capi.TriggerIngameError(null, "alchemy-connection-another", "Select another attachment point.");

            return;
        }

        bool swap = false;
        AlchemyAttachPoint fromPoint = lastSelectedEquipment.AlchemyAttachPoints[lastSelectedIndex];
        AlchemyAttachPoint toPoint = equipment.AlchemyAttachPoints[index];

        if (toPoint.IsOutput && !fromPoint.IsOutput)
        {
            // Swap with tuple.
            (fromPoint, toPoint) = (toPoint, fromPoint);
            swap = true;
        }

        // Invalid.
        if (!fromPoint.IsOutput || toPoint.IsOutput)
        {
            lastSelectedEquipment = null;
            lastSelectedIndex = -1;

            MainAPI.Capi.TriggerIngameError(null, "alchemy-connection-invalid", "Must connect output to input.");

            return;
        }

        AlchemyConnectionPacket packet = swap
            ? new(
                equipment.Pos.X,
                equipment.Pos.Y,
                equipment.Pos.Z,
                lastSelectedEquipment.Pos.X,
                lastSelectedEquipment.Pos.Y,
                lastSelectedEquipment.Pos.Z,
                index,
                lastSelectedIndex
            )
            : new(
                lastSelectedEquipment.Pos.X,
                lastSelectedEquipment.Pos.Y,
                lastSelectedEquipment.Pos.Z,
                equipment.Pos.X,
                equipment.Pos.Y,
                equipment.Pos.Z,
                lastSelectedIndex,
                index
            );
        ClearClientConnections();

        MainAPI.Capi.TriggerIngameError(null, "alchemy-connection-success", "Connected.");
        SendPacket(packet);
    }

    public void ClearClientConnections()
    {
        lastSelectedEquipment = null;
        lastSelectedIndex = -1;
    }

    protected override void RegisterMessages(INetworkChannel channel)
    {
        channel.RegisterMessageType<AlchemyConnectionPacket>();
        channel.RegisterMessageType<AlchemyDisconnectPacket>();
        channel.RegisterMessageType<AlchemyMarkerPacket>();
        channel.RegisterMessageType<AlchemyFlaskNamePacket>();
    }

    protected override void RegisterClientMessages(IClientNetworkChannel channel)
    {

    }

    protected override void RegisterServerMessages(IServerNetworkChannel channel)
    {
        channel.SetMessageHandler<AlchemyConnectionPacket>((player, packet) =>
        {
            BlockPos startPos = new(packet.fromX, packet.fromY, packet.fromZ);
            BlockPos endPos = new(packet.toX, packet.toY, packet.toZ);

            if (
            player.Entity.World.BlockAccessor.GetBlockEntity(startPos) is not BlockEntityAlchemyEquipment start ||
            player.Entity.World.BlockAccessor.GetBlockEntity(endPos) is not BlockEntityAlchemyEquipment end) return;

            // Can't connect to same block entity.
            if (start == end) return;

            if (start.Pos.DistanceTo(end.Pos) > 3f) return;

            if (
            packet.fromIndex >= start.AlchemyAttachPoints.Length ||
            packet.toIndex >= end.AlchemyAttachPoints.Length) return;

            AlchemyAttachPoint fromPoint = start.AlchemyAttachPoints[packet.fromIndex];
            AlchemyAttachPoint toPoint = end.AlchemyAttachPoints[packet.toIndex];

            if (fromPoint.Connect(start, end, packet.toIndex))
            {
                start.MarkDirty(true);
            }
        });

        channel.SetMessageHandler<AlchemyMarkerPacket>((player, packet) =>
        {
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack == null || slot.Itemstack.Collectible is not ItemFluidStorage fluidStorage) return;

            FluidContainer cont = fluidStorage.GetContainer(slot.Itemstack);

            int mark = Math.Clamp(packet.mark, 0, cont.Capacity);

            slot.Itemstack.Attributes.SetInt("mark", mark);
            slot.MarkDirty();
        });

        channel.SetMessageHandler<AlchemyDisconnectPacket>((player, packet) =>
        {
            BlockPos pos = new(packet.x, packet.y, packet.z);
            if (player.Entity.World.BlockAccessor.GetBlockEntity(pos) is not BlockEntityAlchemyEquipment equipment) return;
            if (packet.index < 0 || packet.index >= equipment.AlchemyAttachPoints.Length) return;
            AlchemyAttachPoint point = equipment.AlchemyAttachPoints[packet.index];
            if (point.IsOutput)
            {
                point.Disconnect();
                equipment.MarkDirty(true);
            }
        });

        channel.SetMessageHandler<AlchemyFlaskNamePacket>((player, packet) =>
        {
            if (packet.name == null) return;

            ItemSlot hotbarSlot = player.Entity.ActiveHandItemSlot;
            if (hotbarSlot.Itemstack == null || hotbarSlot.Itemstack.Collectible is not ItemLabeledFlask labeledFlask) return;

            if (packet.name.Length > 100) return; // Too long.

            hotbarSlot.Itemstack.Attributes.RemoveAttribute("label");

            if (packet.name.Length > 0 && packet.name != hotbarSlot.Itemstack.GetName())
            {
                hotbarSlot.Itemstack.Attributes.SetString("label", packet.name);
            }

            hotbarSlot.MarkDirty();
        });
    }
}