using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Fishing3;

/// <summary>
/// Base block entity for all alchemy equipment.
/// </summary>
public class BlockEntityAlchemyEquipment : BlockEntity
{
    public Cuboidf[] NewSelectionBoxes { get; private set; } = null!;

    /// <summary>
    /// Override this with an array of every attachment point for this.
    /// </summary>
    public virtual AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = Array.Empty<AlchemyAttachPoint>();

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        List<Cuboidf> selectionCuboids = new();
        selectionCuboids.AddRange(Block.SelectionBoxes);

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

    ///// <summary>
    ///// Outputs to the point at the given index.
    ///// </summary>
    //public void TryOutputToPoint(int index)
    //{
    //    if (index < 0 || index >= AlchemyAttachPoints.Length) return;

    //    AlchemyAttachPoint point = AlchemyAttachPoints[index];
    //    if (!point.IsOutput) return;
    //}

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
        if (apap == null || apap.Length != AlchemyAttachPoints.Length) return;

        AlchemyAttachPoints = apap;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        MeshInfo<StandardVertex> meshInfo = new(6, 6);

        TextureAtlasPosition texPos = MainAPI.Capi.BlockTextureAtlas[new AssetLocation($"fishing:glass")];

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
}