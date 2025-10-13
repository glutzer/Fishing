using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Fishing3;

public enum EnumAlchemyParticle
{
    Smoke,
    Drip
}

/// <summary>
/// Base block entity for all alchemy equipment.
/// </summary>
public class BlockEntityAlchemyEquipment : BlockEntity
{
    protected const int OPEN_INVENTORY_PACKET = 5555;
    protected const int CLOSE_INVENTORY_PACKET = 5556;

    private long listenerId;

    public Cuboidf[] NewSelectionBoxes { get; private set; } = Array.Empty<Cuboidf>();

    /// <summary>
    /// Override this with an array of every attachment point for this.
    /// </summary>
    public virtual AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = Array.Empty<AlchemyAttachPoint>();

    private static readonly SimpleParticleProperties smokeParticles;
    private static readonly SimpleParticleProperties dripParticles;
    static BlockEntityAlchemyEquipment()
    {
        smokeParticles = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinVelocity = new Vec3f(0f, 0f, 0f),
            AddVelocity = new Vec3f(0f, 1f, 0f),
            MinQuantity = 1f,
            AddQuantity = 2f,
            GravityEffect = 0f,
            SelfPropelled = false,
            MinSize = 0.125f,
            MaxSize = 0.5f,
            Color = ColorUtil.ToRgba(100, 200, 0, 0),
            OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -20)
        };

        dripParticles = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinVelocity = new Vec3f(0f, 0f, 0f),
            AddVelocity = new Vec3f(0f, 0f, 0f),
            MinQuantity = 1f,
            AddQuantity = 0f,
            GravityEffect = 0.5f,
            SelfPropelled = false,
            MinSize = 0.25f,
            MaxSize = 0.35f,
            Color = ColorUtil.ToRgba(100, 200, 0, 0),
            OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -10)
        };
    }

    public virtual void OnTick(int tick)
    {

    }

    private void RemakeSelectionBoxes()
    {
        if (Block.SelectionBoxes == null) return;

        List<Cuboidf> selectionCuboids = [.. Block.SelectionBoxes];

        foreach (AlchemyAttachPoint attachPoint in AlchemyAttachPoints)
        {
            float radius = 0.1f;

            Cuboidf cuboid = new(
                new Vec3f(attachPoint.Position.X - radius, attachPoint.Position.Y - radius, attachPoint.Position.Z - radius),
                new Vec3f(attachPoint.Position.X + radius, attachPoint.Position.Y + radius, attachPoint.Position.Z + radius)
            );

            selectionCuboids.Add(cuboid);
        }

        NewSelectionBoxes = selectionCuboids.ToArray();
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        listenerId = MainAPI.GetGameSystem<TickSystem>(api.Side).RegisterTicker(OnTick);

        if (api.Side == EnumAppSide.Client)
        {
            MainAPI.Capi.BlockTextureAtlas.GetOrInsertTexture("fishing:glasslong", out int subId, out TextureAtlasPosition texPos);
        }
    }

    /// <summary>
    /// Tries to get the container an output is connected to.
    /// </summary>
    protected FluidContainer? GetOutputConnection(int outputIndex, bool markOtherDirty = false)
    {
        if (outputIndex < 0 || outputIndex >= AlchemyAttachPoints.Length) return null;

        AlchemyAttachPoint point = AlchemyAttachPoints[outputIndex];
        if (!point.IsOutput || !point.Connected) return null; // Only output from outputs.

        if (Api.World.BlockAccessor.GetBlockEntity(point.ConnectedToPos.AsBlockPos) is not BlockEntityAlchemyEquipment targetBe) return null;

        if (markOtherDirty) targetBe.MarkDirty();

        return targetBe.GetInputContainer(point.ConnectedToIndex);
    }

    /// <summary>
    /// Mark a connection be dirty after moving it.
    /// </summary>
    protected void MarkConnectionDirty(int outputIndex)
    {
        if (outputIndex < 0 || outputIndex >= AlchemyAttachPoints.Length) return;

        AlchemyAttachPoint point = AlchemyAttachPoints[outputIndex];
        if (!point.IsOutput || !point.Connected) return; // Only output from outputs.

        if (Api.World.BlockAccessor.GetBlockEntity(point.ConnectedToPos.AsBlockPos) is not BlockEntityAlchemyEquipment targetBe) return;

        targetBe.MarkDirty();
    }

    /// <summary>
    /// Get a specific alchemy block entity's input container at an index.
    /// </summary>
    public virtual FluidContainer? GetInputContainer(int inputIndex)
    {
        return null;
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        byte[] bytes = SerializerUtil.Serialize(AlchemyAttachPoints);
        tree.SetBytes("apap", bytes);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        byte[]? data = tree.GetBytes("apap");
        if (data == null) return;

        AlchemyAttachPoint[]? apap = SerializerUtil.Deserialize<AlchemyAttachPoint[]>(data);
        if (apap == null) return;

        AlchemyAttachPoints = apap;

        if (NewSelectionBoxes.Length == 0)
        {
            RemakeSelectionBoxes();
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        MeshInfo<StandardVertex> meshInfo = new(6, 6);

        TextureAtlasPosition texPos = MainAPI.Capi.BlockTextureAtlas[$"fishing:glasslong"];

        foreach (AlchemyAttachPoint point in AlchemyAttachPoints)
        {
            if (!point.IsOutput || point.ConnectedToIndex == -1) continue; // Not connected to anything, only outputs tessellate.

            Vector3 start = point.Position;
            Vector3 offset = point.CachedOffset;

            float distance = offset.Length;

            Vector3 middle = start + (offset * 0.5f);

            Quaternion quat = QuaternionUtility.FromToRotation(Vector3.UnitY, offset.Normalized());

            Matrix4 translation = Matrix4.CreateScale(0.1f, distance, 0.1f) * Matrix4.CreateFromQuaternion(quat) * Matrix4.CreateTranslation(middle);
            Matrix3 rotation = new Matrix3(translation).Inverted();
            rotation.Transpose();

            CubeMeshUtility.AddCenteredCubeData(meshInfo, v =>
            {
                v.uv.Y *= distance / 2f;
                if (v.uv.Y > 1f) v.uv.Y = 1f;

                Vector4 newPos = new Vector4(v.position, 1f) * translation;
                Vector3 newNormal = v.normal * rotation;
                newNormal.Normalize();

                return new StandardVertex(newPos.Xyz, v.uv, newNormal, Vector4.One);
            });
        }

        TessellatorTools.MapUvToAtlasTexture(meshInfo, texPos);
        MeshData meshData = TessellatorTools.ConvertToMeshData(meshInfo, texPos.atlasTextureId, Vector4.One, 3, ColorSpace.GBRA);

        mesher.AddMeshData(meshData);

        return base.OnTesselation(mesher, tessThreadTesselator);
    }

    /// <summary>
    /// Override to add client interactions.
    /// </summary>
    public virtual void OnClientInteract()
    {

    }

    /// <summary>
    /// Override this to open guis and inventories.
    /// Client will only receive it for himself.
    /// </summary>
    public virtual void ToggleInventory(IPlayer player, bool open)
    {

    }

    /// <summary>
    /// Send a packet from the client.
    /// </summary>
    public void SendClientPacket(int packetId)
    {
        if (Api is ICoreClientAPI capi)
        {
            capi.Network.SendBlockEntityPacket(Pos, packetId);
        }
    }

    /// <summary>
    /// Send a packet from the server.
    /// </summary>
    public void SendServerPacket(int packetId, IPlayer player)
    {
        if (Api is ICoreServerAPI sapi)
        {
            sapi.Network.SendBlockEntityPacket((IServerPlayer)player, Pos, packetId);
        }
    }

    public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetId, byte[] data)
    {
        base.OnReceivedClientPacket(fromPlayer, packetId, data);

        // Inventory open request.
        if (packetId == OPEN_INVENTORY_PACKET)
        {
            ToggleInventory(fromPlayer, true);
            SendServerPacket(OPEN_INVENTORY_PACKET, fromPlayer);
        }

        // Inventory close request.
        if (packetId == CLOSE_INVENTORY_PACKET)
        {
            ToggleInventory(fromPlayer, false);
            //SendServerPacket(CLOSE_INVENTORY_PACKET, fromPlayer);
            // Should be done by the client, first.
        }
    }

    public override void OnReceivedServerPacket(int packetId, byte[] data)
    {
        base.OnReceivedServerPacket(packetId, data);

        // Inventory open request.
        if (packetId == OPEN_INVENTORY_PACKET)
        {
            ToggleInventory(MainAPI.Capi.World.Player, true);
        }

        // Inventory close request.
        if (packetId == CLOSE_INVENTORY_PACKET)
        {
            ToggleInventory(MainAPI.Capi.World.Player, false);
        }
    }

    /// <summary>
    /// Emit colored particles based on the fluid.
    /// Can be called on server or client, not both.
    /// </summary>
    protected void EmitParticles(EnumAlchemyParticle particleType, Vector3 localPosition, FluidContainer container, float lifeLengthMultiplier = 1f, int times = 1, float emitRad = 0.2f)
    {
        if (particleType == EnumAlchemyParticle.Smoke)
        {
            SimpleParticleProperties toEmit = smokeParticles;

            Vector3 emitRadius = Vector3.One * emitRad;

            // Set particles to the color of the block.
            Vector4 color = container.HeldStack?.fluid.GetColor(container.HeldStack) ?? new Vector4(0.4f, 0.4f, 0.4f, 0.5f); // Smoke if null.
            float glow = container.HeldStack?.fluid.GetGlowLevel(container.HeldStack) ?? 0;

            toEmit.MinPos.Set(Pos.X + localPosition.X - emitRadius.X, Pos.Y + localPosition.Y - emitRadius.Y, Pos.Z + localPosition.Z - emitRadius.Z);
            toEmit.AddPos.Set(emitRadius.X * 2f, emitRadius.Y * 2f, emitRadius.Z * 2f);
            toEmit.LifeLength = 0.5f * lifeLengthMultiplier;
            toEmit.addLifeLength = 3f * lifeLengthMultiplier;

            for (int i = 0; i < times; i++)
            {
                float brightness = 0.5f + (Random.Shared.NextSingle() * 0.5f);
                toEmit.Color = ColorUtil.ToRgba((int)(color.W * 255), (int)(color.X * 255 * brightness), (int)(color.Y * 255 * brightness), (int)(color.Z * 255 * brightness));

                toEmit.VertexFlags = (int)(glow * 255 * brightness);

                Api.World.SpawnParticles(toEmit);
            }
        }

        if (particleType == EnumAlchemyParticle.Drip)
        {
            SimpleParticleProperties toEmit = dripParticles;

            Vector4 color = container.HeldStack?.fluid.GetColor(container.HeldStack) ?? new Vector4(0.4f, 0.4f, 0.4f, 0.5f); // Default drip color if null.
            float glow = container.HeldStack?.fluid.GetGlowLevel(container.HeldStack) ?? 0;
            toEmit.MinPos.Set(Pos.X + localPosition.X, Pos.Y + localPosition.Y, Pos.Z + localPosition.Z);
            toEmit.LifeLength = 1f * lifeLengthMultiplier;
            toEmit.addLifeLength = 1f * lifeLengthMultiplier;
            for (int i = 0; i < times; i++)
            {
                float brightness = 0.5f + (Random.Shared.NextSingle() * 0.5f);
                toEmit.Color = ColorUtil.ToRgba((int)(color.W * 255), (int)(color.X * 255 * brightness), (int)(color.Y * 255 * brightness), (int)(color.Z * 255 * brightness));

                toEmit.VertexFlags = (int)(glow * 255 * brightness);

                Api.World.SpawnParticles(toEmit);
            }
        }
    }

    /// <summary>
    /// Emit colored particles based on the item stack.
    /// Can be called on server or client, not both.
    /// </summary>
    protected void EmitParticles(EnumAlchemyParticle particleType, Vector3 localPosition, float lifeLengthMultiplier = 1f, int times = 1)
    {
        if (particleType == EnumAlchemyParticle.Smoke)
        {
            SimpleParticleProperties toEmit = smokeParticles;

            Vector3 emitRadius = Vector3.One * 0.2f;

            // Color is RGBA.
            Vector4 color = new(0.5f, 0.5f, 0.5f, 0.8f);

            toEmit.MinPos.Set(Pos.X + localPosition.X - emitRadius.X, Pos.Y + localPosition.Y - emitRadius.Y, Pos.Z + localPosition.Z - emitRadius.Z);
            toEmit.AddPos.Set(emitRadius.X * 2f, emitRadius.Y * 2f, emitRadius.Z * 2f);
            toEmit.LifeLength = 0.5f * lifeLengthMultiplier;
            toEmit.addLifeLength = 3f * lifeLengthMultiplier;

            for (int i = 0; i < times; i++)
            {
                float brightness = 0.5f + (Random.Shared.NextSingle() * 0.5f);
                toEmit.Color = ColorUtil.ToRgba((int)(color.W * 255), (int)(color.X * 255 * brightness), (int)(color.Y * 255 * brightness), (int)(color.Z * 255 * brightness));

                Api.World.SpawnParticles(toEmit);
            }
        }
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        OnBlockGone();
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        OnBlockGone();
    }

    /// <summary>
    /// If this variant is rotatable, get the rotation radians of it.
    /// </summary>
    public float GetRotationRadians()
    {
        string? variant = Block.Variant["side"];

        return variant == null
            ? 0f
            : variant switch
            {
                "north" => 0f,
                "east" => MathF.PI / 2f,
                "south" => MathF.PI,
                "west" => 3f * MathF.PI / 2f,
                _ => 0f
            };
    }

    public Vector3 RotateVectorToSide(Vector3 vec)
    {
        float rotation = GetRotationRadians();

        Matrix4 rotationMatrix = Matrix4.CreateTranslation(-0.5f, -0.5f, -0.5f) *
                                 Matrix4.CreateRotationY(-rotation) *
                                 Matrix4.CreateTranslation(0.5f, 0.5f, 0.5f);

        Vector4 rotatedVec = new Vector4(vec, 1) * rotationMatrix;
        return rotatedVec.Xyz;
    }

    private AlchemyAttachPoint[] RotateAttachmentPoints(AlchemyAttachPoint[] points)
    {
        AlchemyAttachPoint[] newArray = new AlchemyAttachPoint[points.Length];
        float rotation = GetRotationRadians();
        if (rotation == 0) return points;

        Matrix4 rotationMatrix = Matrix4.CreateTranslation(-0.5f, -0.5f, -0.5f) *
                                 Matrix4.CreateRotationY(-rotation) *
                                 Matrix4.CreateTranslation(0.5f, 0.5f, 0.5f);

        for (int i = 0; i < points.Length; i++)
        {
            Vector4 point = new(points[i].Position, 1);
            point *= rotationMatrix;
            newArray[i] = new AlchemyAttachPoint(point.Xyz, points[i].IsOutput);
        }

        return newArray;
    }

    public virtual void OnBlockGone()
    {
        MainAPI.TryGetGameSystem(Api.Side, out TickSystem? tickSystem);
        tickSystem?.UnregisterTicker(listenerId);
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        if (Api.Side == EnumAppSide.Server)
        {
            AlchemyAttachPoints = RotateAttachmentPoints(AlchemyAttachPoints);
            RemakeSelectionBoxes();
        }

        base.OnBlockPlaced(byItemStack);
    }
}