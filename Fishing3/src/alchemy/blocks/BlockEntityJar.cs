using OpenTK.Mathematics;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing;

[BlockEntity]
public class BlockEntityJar : BlockEntityAlchemyEquipment, IFluidSource, IFluidSink
{
    public readonly FluidContainer container = new(3000);
    public FluidRenderingInstance? renderInstance;

    public override AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = new[]
            {
                new AlchemyAttachPoint(new Vector3(0.5f, 0.7f, 0.5f), false)
            };

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api.Side == EnumAppSide.Client)
        {
            renderInstance = new(container, new Vector3(0.3f, 0.05f, 0.3f), new Vector3(0.7f, 0.6f, 0.7f), Pos);
            FluidBlockRenderingSystem.Instance?.RegisterInstance(renderInstance);
        }
    }

    public void MarkContainerDirty()
    {
        MarkDirty();
    }

    public override FluidContainer? GetInputContainer(int inputIndex)
    {
        return inputIndex == 0 ? container : null;
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        byte[] data = container.SaveStack();
        if (data.Length > 0) tree.SetBytes("contStack", data);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        byte[]? bytes = tree.GetBytes("contStack");
        if (bytes == null)
        {
            container.EmptyContainer();
            return;
        }

        container.LoadStack(bytes, worldAccessForResolve.Side);
    }

    public override void OnBlockGone()
    {
        base.OnBlockGone();
        if (renderInstance != null)
        {
            FluidBlockRenderingSystem.Instance?.UnregisterInstance(renderInstance);
            renderInstance = null;
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        dsc.AppendLine($"{container.RoomUsed}/{container.Capacity}mL");
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        container.HeldStack?.GetFluidInfo(dsc);
    }

    public FluidContainer GetSource(int index)
    {
        return container;
    }

    public FluidContainer GetSink(int index)
    {
        return container;
    }
}