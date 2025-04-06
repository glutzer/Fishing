using MareLib;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing3;

/// <summary>
/// Has methods for interfacing with the heat pipe system.
/// </summary>
public class BlockEntityHeatedAlchemyEquipment : BlockEntityAlchemyEquipment
{
    public HeatPipeSystem heatPipeSystem = null!;
    public HeatPipeInstance heatPipeInstance = null!;

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

    public override void OnBlockGone()
    {
        base.OnBlockGone();
        heatPipeSystem.UnregisterPipe(heatPipeInstance);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine($"Heat: {System.MathF.Round(heatPipeInstance.celsius, 2)}°C");
    }
}