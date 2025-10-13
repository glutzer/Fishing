using OpenTK.Mathematics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing3;

[BlockEntity]
public class BlockEntityCauldron : BlockEntityHeatedAlchemyEquipment, IFluidSource, IFluidSink
{
    public readonly FluidContainer inputBuffer = new(1000);
    public readonly FluidContainer outputBuffer = new(4000);
    public FluidRenderingInstance? renderInstance;

    protected ILoadedSound? bubblingSound;

    public override AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = new[]
            {
                new AlchemyAttachPoint(new Vector3(0.5f, 0.9f, 0.5f), false)
            };

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api.Side == EnumAppSide.Client)
        {
            renderInstance = new(outputBuffer, new Vector3(0.15f, 0.2f, 0.15f), new Vector3(0.85f, 0.8f, 0.85f), Pos);
            FluidBlockRenderingSystem.Instance?.RegisterInstance(renderInstance);

            bubblingSound = MainAPI.Capi.World.LoadSound(new SoundParams()
            {
                Location = "fishing:sounds/bubbling",
                ShouldLoop = true,
                Position = new Vec3f(Pos.X + 0.5f, Pos.Y + 0.5f, Pos.Z + 0.5f),
                DisposeOnFinish = true,
                RelativePosition = false,
                Volume = 0f,
                SoundType = EnumSoundType.Sound,
                Range = 8f,
                Pitch = 0.5f
            });

            bubblingSound.Start();
        }
    }

    public override void OnClientInteract()
    {
        GuiAlchemyEquipment gui = new();
        gui.AddFluidMeter(inputBuffer);
        gui.AddProcessingDisplay(() =>
        {
            return heatPipeInstance.celsius > 200f && !inputBuffer.Empty;
        });
        gui.AddFluidMeter(outputBuffer);
        gui.TryOpen();
    }

    public override void OnTick(int tick)
    {
        if (Api.Side == EnumAppSide.Client) return;
        if (tick % 20 != 0) return;
        if (inputBuffer.Empty || heatPipeInstance.celsius < 100f) return;

        // Create a new potion stack.
        if (outputBuffer.Empty)
        {
            Fluid potion = MainAPI.GetServerSystem<FluidRegistry>().GetFluid("potion");
            FluidStack potionStack = potion.CreateFluidStack();
            outputBuffer.SetStack(potionStack);
        }

        FluidContainer.MoveFluids(inputBuffer, outputBuffer, 100);

        MarkDirty();

        EmitParticles(EnumAlchemyParticle.Smoke, new Vector3(0.5f, 0.5f, 0.5f), outputBuffer, 2f, 2);
    }

    public FluidContainer GetSink(int index)
    {
        return inputBuffer;
    }

    public FluidContainer GetSource(int index)
    {
        return outputBuffer;
    }

    public void MarkContainerDirty()
    {
        MarkDirty();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        byte[] inputData = inputBuffer.SaveStack();
        if (inputData.Length > 0) tree.SetBytes("inStack", inputData);

        byte[] outputData = outputBuffer.SaveStack();
        if (outputData.Length > 0) tree.SetBytes("outStack", outputData);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        byte[]? inputData = tree.GetBytes("inStack");
        if (inputData == null)
        {
            inputBuffer.EmptyContainer();
        }
        else
        {
            inputBuffer.LoadStack(inputData, worldAccessForResolve.Side);
        }

        byte[]? outputData = tree.GetBytes("outStack");
        if (outputData == null)
        {
            outputBuffer.EmptyContainer();
        }
        else
        {
            outputBuffer.LoadStack(outputData, worldAccessForResolve.Side);
        }

        if (worldAccessForResolve.Side == EnumAppSide.Client)
        {
            if (heatPipeInstance.celsius > 100f && !inputBuffer.Empty)
            {
                //bubblingSound?.SetVolume(0.1f);
                // No sound.
            }
            else
            {
                bubblingSound?.SetVolume(0f);
            }
        }
    }

    public override void OnBlockGone()
    {
        base.OnBlockGone();

        if (renderInstance != null)
        {
            FluidBlockRenderingSystem.Instance?.UnregisterInstance(renderInstance);
            renderInstance = null;
        }

        bubblingSound?.Stop();
        bubblingSound?.Dispose();
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        dsc.AppendLine($"Input: {inputBuffer.RoomUsed}/{inputBuffer.Capacity}mL");
        dsc.AppendLine($"Output: {outputBuffer.RoomUsed}/{outputBuffer.Capacity}mL");
        base.GetBlockInfo(forPlayer, dsc);

        if (inputBuffer.HeldStack != null)
        {
            dsc.AppendLine();
            dsc.AppendLine("Pending mixture:");
            inputBuffer.HeldStack.GetFluidInfo(dsc);
        }

        dsc.AppendLine();
        outputBuffer.HeldStack?.GetFluidInfo(dsc);
    }

    public override FluidContainer? GetInputContainer(int inputIndex)
    {
        return inputIndex == 0 ? inputBuffer : null;
    }
}