using MareLib;
using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Fishing3;

public static class AlchemyTessellationUtility
{
    public static void TessellateSolid(ITerrainMeshPool mesher, float height, float rad)
    {
        MeshInfo<StandardVertex> meshInfo = new(6, 6);

        CubeMeshUtility.AddRangeCubeData(meshInfo, v =>
        {
            v = TessellatorTools.SetUvsBasedOnPosition(v);

            return new StandardVertex(v.position, v.uv, v.normal, Vector4.One);
        }, new Vector3(rad, height, rad), new Vector3(1f - rad, height + 0.15f, 1f - rad));

        TextureAtlasPosition cokeTex = MainAPI.Capi.BlockTextureAtlas[$"game:block/coal/coke"];
        TessellatorTools.MapUvToAtlasTexture(meshInfo, cokeTex);
        MeshData meshData = TessellatorTools.ConvertToMeshData(meshInfo, cokeTex.atlasTextureId, Vector4.One, 0, ColorSpace.GBRA);
        mesher.AddMeshData(meshData);
    }

    public static void TessellateSolid(ITerrainMeshPool mesher, Vector3 from, Vector3 to)
    {
        MeshInfo<StandardVertex> meshInfo = new(6, 6);

        CubeMeshUtility.AddRangeCubeData(meshInfo, v =>
        {
            v = TessellatorTools.SetUvsBasedOnPosition(v);

            return new StandardVertex(v.position, v.uv, v.normal, Vector4.One);
        }, from, to);

        TextureAtlasPosition cokeTex = MainAPI.Capi.BlockTextureAtlas[$"game:block/coal/coke"];
        TessellatorTools.MapUvToAtlasTexture(meshInfo, cokeTex);
        MeshData meshData = TessellatorTools.ConvertToMeshData(meshInfo, cokeTex.atlasTextureId, Vector4.One, 0, ColorSpace.GBRA);
        mesher.AddMeshData(meshData);
    }
}