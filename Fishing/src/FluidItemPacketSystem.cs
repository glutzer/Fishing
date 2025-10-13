using ProtoBuf;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Fishing;

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

/// <summary>
/// Logs pending connections on client, dispatches packets.
/// </summary>
[GameSystem]
public class FluidItemPacketSystem : NetworkedGameSystem
{
    public FluidItemPacketSystem(bool isServer, ICoreAPI api) : base(isServer, api, "fips")
    {

    }

    protected override void RegisterMessages(INetworkChannel channel)
    {
        channel.RegisterMessageType<AlchemyMarkerPacket>();
        channel.RegisterMessageType<AlchemyFlaskNamePacket>();
    }

    protected override void RegisterClientMessages(IClientNetworkChannel channel)
    {

    }

    protected override void RegisterServerMessages(IServerNetworkChannel channel)
    {
        channel.SetMessageHandler<AlchemyMarkerPacket>((player, packet) =>
        {
            ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack == null || slot.Itemstack.Collectible is not ItemFluidStorage fluidStorage) return;

            FluidContainer cont = fluidStorage.GetContainer(slot.Itemstack);

            int mark = Math.Clamp(packet.mark, 0, cont.Capacity);

            slot.Itemstack.Attributes.SetInt("mark", mark);
            slot.MarkDirty();
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