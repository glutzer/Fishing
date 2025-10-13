using OpenTK.Mathematics;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing;

[BlockEntity]
public class BlockEntityFurnace : BlockEntity
{
    public HeatPipeSystem heatPipeSystem = null!;
    public HeatPipeInstance heatPipeInstance = null!;

    // Seconds of fuel until the next fuel piece is removed.
    private float fuelSecondsLeft;

    // Max temperature of the current fuel piece.
    private float fuelTemperature;

    // Has this been ignited by something?
    private bool ignited;

    // 0 - Fuel slot.
    // 1 - Output slot (collectible soot).
    private readonly InventoryGeneric inventory = new(2, null, null);

    private long listenerId;

    /// <summary>
    /// Ignite on the server and mark dirty.
    /// </summary>
    public void Ignite()
    {
        if (ignited == false && Api.Side == EnumAppSide.Server)
        {
            ConsumeFuel(false);
        }
    }

    /// <summary>
    /// Consume fuel on the server.
    /// </summary>
    protected void ConsumeFuel(bool consumeItem)
    {
        ItemSlot fuelSlot = inventory[0]; // Fuel slot.

        if (consumeItem)
        {
            fuelSlot.TakeOut(1);
            fuelSlot.MarkDirty();
        }

        if (fuelSlot.Empty)
        {
            ignited = false;
            MarkDirty(true);
            return;
        }

        CollectibleObject fuelItem = fuelSlot.Itemstack.Collectible;
        CombustibleProperties? props = fuelItem.CombustibleProps;
        if (props == null) return;

        fuelTemperature = props.BurnTemperature;
        fuelSecondsLeft = props.BurnDuration;

        if (!ignited)
        {
            ignited = true;
        }

        MarkDirty(true);
    }

    public void OnInteractServer(ItemSlot slot, EntityAgent byEntity)
    {
        if (slot.Itemstack == null || slot.Itemstack.Collectible.CombustibleProps == null || slot.Itemstack.Collectible.CombustibleProps.BurnTemperature <= 0) return;
        if (inventory[0].Itemstack != null && inventory[0].Itemstack.Collectible != slot.Itemstack.Collectible) return;
        if (inventory[0].Itemstack != null && inventory[0].Itemstack.StackSize >= inventory[0].Itemstack.Collectible.MaxStackSize) return; // No room.

        if (inventory[0].Itemstack == null)
        {
            ItemStack newStack = slot.TakeOut(byEntity.Controls.Sneak ? 8 : 1);
            inventory[0].Itemstack = newStack;
        }
        else
        {
            int toTake = Math.Min(inventory[0].Itemstack.Collectible.MaxStackSize - inventory[0].Itemstack.StackSize, byEntity.Controls.Sneak ? 8 : 1);
            slot.TryPutInto(Api.World, inventory[0], toTake);
        }

        slot.MarkDirty();
        MarkDirty(true);
    }

    protected void OnServerTick(int tick)
    {
        if (tick % 10 == 0 && ignited)
        {
            float maxToAdd = fuelTemperature - heatPipeInstance.celsius;

            // Add up to 10 degrees?
            if (maxToAdd > 0) heatPipeInstance.ChangeTemperature(Math.Min(maxToAdd, 10));

            fuelSecondsLeft -= 0.5f;
            if (fuelSecondsLeft <= 0f) ConsumeFuel(true);
        }
    }

    public override void Initialize(ICoreAPI api)
    {
        inventory.LateInitialize($"{Pos.X}-{Pos.Y}-{Pos.Z}-furnace", api);

        base.Initialize(api);

        heatPipeSystem = MainAPI.GetGameSystem<HeatPipeSystem>(api.Side);

        float temperature = heatPipeInstance?.celsius ?? 15f;
        heatPipeInstance = new(new GridPos(Pos.X, Pos.Y, Pos.Z), temperature);

        heatPipeSystem.RegisterPipe(heatPipeInstance);

        if (api.Side == EnumAppSide.Server)
        {
            listenerId = TickSystem.Server!.RegisterTicker(OnServerTick);
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetFloat("celsius", heatPipeInstance?.celsius ?? 15f);

        tree.SetFloat("fuelSecondsLeft", fuelSecondsLeft);
        tree.SetFloat("fuelTemperature", fuelTemperature);
        tree.SetBool("ignited", ignited);

        ITreeAttribute invTree = tree.GetOrAddTreeAttribute("inv");
        inventory.ToTreeAttributes(invTree);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        heatPipeInstance ??= new(new GridPos(Pos.X, Pos.Y, Pos.Z), 15f);
        heatPipeInstance.celsius = tree.GetFloat("celsius", 15f);

        fuelSecondsLeft = tree.GetFloat("fuelSecondsLeft", 0f);
        fuelTemperature = tree.GetFloat("fuelTemperature", 15f);
        ignited = tree.GetBool("ignited", false);

        ITreeAttribute? invTree = tree.GetTreeAttribute("inv");
        if (invTree != null)
        {
            inventory.FromTreeAttributes(invTree);
        }
    }

    // Fundamental game bug - these sometimes do not get called.
    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);

        if (Api.Side == EnumAppSide.Server)
        {
            inventory.DropAll(Pos.ToVec3d().Add(0.5f));
        }
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        heatPipeSystem.UnregisterPipe(heatPipeInstance);

        if (Api.Side == EnumAppSide.Server)
        {
            TickSystem.Server?.UnregisterTicker(listenerId);
        }
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();

        heatPipeSystem.UnregisterPipe(heatPipeInstance);

        if (Api.Side == EnumAppSide.Server)
        {
            TickSystem.Server?.UnregisterTicker(listenerId);
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine($"Heat: {System.MathF.Round(heatPipeInstance.celsius, 2)}°C");

        if (inventory[0].Itemstack != null)
        {
            dsc.AppendLine($"{inventory[0].Itemstack.StackSize}x {inventory[0].Itemstack.Collectible.GetHeldItemName(inventory[0].Itemstack)}");
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        ItemSlot slot = inventory[0]; // Fuel slot.

        // No fuel.
        if (slot.Empty) return base.OnTesselation(mesher, tessThreadTesselator);

        float heldStackRatio = MathF.Round(slot.Itemstack.StackSize / (float)slot.Itemstack.Collectible.MaxStackSize, 2);

        MeshInfo<StandardVertex> meshInfo = new(6, 6);

        CubeMeshUtility.AddRangeCubeData(meshInfo, v =>
        {
            v = TessellatorTools.SetUvsBasedOnPosition(v);

            return new StandardVertex(v.position, v.uv, v.normal, Vector4.One);
        }, new Vector3(0.2f, 0.25f, 0.2f), new Vector3(0.8f, 0.25f + (0.4f * heldStackRatio), 0.8f)); // 0-1 for testing.

        if (ignited)
        {
            TextureAtlasPosition emberTex = MainAPI.Capi.BlockTextureAtlas[$"game:block/coal/ember"];
            TessellatorTools.MapUvToAtlasTexture(meshInfo, emberTex);
            MeshData meshData = TessellatorTools.ConvertToMeshData(meshInfo, emberTex.atlasTextureId, Vector4.One, 0, ColorSpace.GBRA, 1f); // 0 = opaque?
            mesher.AddMeshData(meshData);
        }
        else
        {
            TextureAtlasPosition cokeTex = MainAPI.Capi.BlockTextureAtlas[$"game:block/coal/coke"];
            TessellatorTools.MapUvToAtlasTexture(meshInfo, cokeTex);
            MeshData meshData = TessellatorTools.ConvertToMeshData(meshInfo, cokeTex.atlasTextureId, Vector4.One, 0, ColorSpace.GBRA);
            mesher.AddMeshData(meshData);
        }

        return base.OnTesselation(mesher, tessThreadTesselator);
    }
}