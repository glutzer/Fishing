using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing;

[BlockEntity]
public class BlockEntityHeatPipe : BlockEntity
{
    public HeatPipeSystem heatPipeSystem = null!;
    public HeatPipeInstance heatPipeInstance = null!;

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        return true;
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        heatPipeSystem = MainAPI.GetGameSystem<HeatPipeSystem>(api.Side);

        float temperature = heatPipeInstance?.celsius ?? 15f;
        heatPipeInstance = new(new GridPos(Pos.X, Pos.Y, Pos.Z), temperature);

        heatPipeSystem.RegisterPipe(heatPipeInstance);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetFloat("celsius", heatPipeInstance?.celsius ?? 15f);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        heatPipeInstance ??= new(new GridPos(Pos.X, Pos.Y, Pos.Z), 15f);
        heatPipeInstance.celsius = tree.GetFloat("celsius", 15f);
    }

    // Fundamental game bug - these sometimes do not get called.

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        heatPipeSystem.UnregisterPipe(heatPipeInstance);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();

        heatPipeSystem.UnregisterPipe(heatPipeInstance);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine($"Heat: {System.MathF.Round(heatPipeInstance.celsius, 2)}°C");
    }
}