using Fishing3.src.rendering;
using MareLib;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Fishing3;

// Client sends to server to request opening item inventory of pole.
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FishingInventoryPacket
{
    public bool openInventory;
}

[GameSystem]
public class FishingGameSystem : NetworkedGameSystem
{
    public FishingGameSystem(bool isServer, ICoreAPI api) : base(isServer, api, "fishingchannel")
    {
    }

    public override void Initialize()
    {
        base.Initialize();

        if (!isServer)
        {
            MareShaderRegistry.AddShader("fishing:fishingline", "marelib:opaque", "fishingline");
        }
    }

    public override void OnAssetsLoaded()
    {
        if (!isServer)
        {
            FishingLineRenderer.OnStart();
        }
    }

    protected override void RegisterMessages(INetworkChannel channel)
    {
        channel.RegisterMessageType<FishingInventoryPacket>();
    }

    protected override void RegisterClientMessages(IClientNetworkChannel channel)
    {
        channel.SetMessageHandler<FishingInventoryPacket>(p =>
        {
            // Get hotbar slot.
            ItemSlot hotbarSlot = MainAPI.Capi.World.Player.InventoryManager.ActiveHotbarSlot;
            if (hotbarSlot.Itemstack == null || hotbarSlot.Itemstack.Collectible is not ItemFishingPole pole) return;

            ItemInventory inv = new("iteminv", $"fishingpole{MainAPI.Capi.World.Player.PlayerUID}", MainAPI.Capi, pole.SlotCount, pole.IsAllowedInSlot, hotbarSlot);
            MainAPI.Capi.World.Player.InventoryManager.OpenInventory(inv);

            GuiInWorldPoleEditor editor = new(inv);
            editor.TryOpen();
        });
    }

    protected override void RegisterServerMessages(IServerNetworkChannel channel)
    {
        channel.SetMessageHandler<FishingInventoryPacket>((player, p) =>
        {
            if (!p.openInventory)
            {
                // Close inventory.
                player.InventoryManager.GetInventory($"iteminv-fishingpole{player.PlayerUID}")?.Close(player);
                return;
            }

            // Get hotbar slot.
            ItemSlot hotbarSlot = player.InventoryManager.ActiveHotbarSlot;
            if (hotbarSlot.Itemstack == null || hotbarSlot.Itemstack.Collectible is not ItemFishingPole pole) return;

            ItemInventory inv = new("iteminv", $"fishingpole{player.PlayerUID}", MainAPI.Capi, pole.SlotCount, pole.IsAllowedInSlot, hotbarSlot);
            player.InventoryManager.OpenInventory(inv);

            // Send packet to client.
            channel.SendPacket(p, player);
        });
    }

    public override void OnClose()
    {
        FishingLineRenderer.OnEnd();
    }
}

public class Fishing3ModSystem : ModSystem
{
}