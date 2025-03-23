using MareLib;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Fishing3;

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

        AlchemyAttachPoint fromPoint = lastSelectedEquipment.AlchemyAttachPoints[lastSelectedIndex];
        AlchemyAttachPoint toPoint = equipment.AlchemyAttachPoints[index];

        // Invalid.
        if (!fromPoint.IsOutput || toPoint.IsOutput)
        {
            lastSelectedEquipment = null;
            lastSelectedIndex = -1;

            MainAPI.Capi.TriggerIngameError(null, "alchemy-connection-invalid", "Must connect output to input.");

            return;
        }

        AlchemyConnectionPacket packet = new(
            lastSelectedEquipment.Pos.X,
            lastSelectedEquipment.Pos.Y,
            lastSelectedEquipment.Pos.Z,
            equipment.Pos.X,
            equipment.Pos.Y,
            equipment.Pos.Z,
            lastSelectedIndex,
            index
        );

        lastSelectedEquipment = null;
        lastSelectedIndex = -1;

        MainAPI.Capi.TriggerIngameError(null, "alchemy-connection-success", "Connected.");
        SendPacket(packet);
    }

    protected override void RegisterMessages(INetworkChannel channel)
    {
        channel.RegisterMessageType<AlchemyConnectionPacket>();
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
    }
}