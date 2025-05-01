using MareLib;
using OpenTK.Mathematics;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing3;

[BlockEntity]
public class BlockEntityAlembic : BlockEntityHeatedAlchemyEquipment, IFluidSource, IFluidSink
{
    protected readonly FluidContainer container = new(1000);
    protected FluidRenderingInstance? renderInstance;

    protected ILoadedSound? bubblingSound;

    public override AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = new[]
            {
                new AlchemyAttachPoint(new Vector3(0.2f, 0.7f, 0.5f), false),
                new AlchemyAttachPoint(new Vector3(0.5f, 1f, 0.5f), true)
            };

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api.Side == EnumAppSide.Client)
        {
            renderInstance = new(container, new Vector3(0.2f, 0.26f, 0.2f), new Vector3(0.8f, 0.55f, 0.8f), Pos);
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
                Range = 8f
            });

            bubblingSound.Start();
        }
    }

    public override void OnTick(int tick)
    {
        if (Api.Side == EnumAppSide.Client) return;

        if (tick % 20 != 0 || heatPipeInstance.celsius < 200f) return;

        FluidContainer? cont = GetOutputConnection(1, true);
        if (cont == null || container.HeldStack == null || cont.RoomLeft <= 0) return;

        // Can the output receive this stack?
        if (!cont.CanReceiveFluid(container.HeldStack)) return;

        FluidStack? newStack = container.TakeOut(1); // Refine 1mL.
        if (newStack == null) return;

        // 50% chance to discard fluid, still emit smoke.
        EmitParticles(EnumAlchemyParticle.Smoke, new Vector3(0.5f, 1f, 0.5f), container, 3);
        if (Random.Shared.NextSingle() < 0.5f) return;

        if (newStack.fluid.HasBehavior<FluidBehaviorReagent>())
        {
            float purity = newStack.Attributes.GetFloat("purity");

            // Add a random value to the purity, before DR is applied, then convert it back with DR.
            purity = DrUtility.ReverseDr(purity, 1, 0.5f);
            purity += Random.Shared.NextSingle();
            purity = DrUtility.CalculateDr(purity, 1, 0.5f);

            newStack.Attributes.SetFloat("purity", purity);
        }

        FluidContainer.MoveFluids(newStack, cont);

        MarkDirty();

        // Emit drip.
        EmitParticles(EnumAlchemyParticle.Drip, AlchemyAttachPoints[1].Position + AlchemyAttachPoints[1].CachedOffset, container);
    }

    public override void OnClientInteract()
    {
        // Alembic has no inventory, just open a gui.
        GuiAlchemyEquipment gui = new();
        gui.AddFluidMeter(container);
        gui.AddProcessingDisplay(() =>
        {
            return heatPipeInstance.celsius > 200f && !container.Empty;
        });
        gui.TryOpen();
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
        }
        else
        {
            container.LoadStack(bytes, worldAccessForResolve.Side);
        }

        if (worldAccessForResolve.Side == EnumAppSide.Client)
        {
            if (heatPipeInstance.celsius > 200f && !container.Empty)
            {
                bubblingSound?.SetVolume(0.2f);
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

        if (Api.Side == EnumAppSide.Client)
        {
            bubblingSound?.Stop();
            bubblingSound?.Dispose();
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        dsc.AppendLine($"{container.RoomUsed}/{container.Capacity}mL");
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        container.HeldStack?.GetFluidInfo(dsc);
    }

    public void MarkContainerDirty()
    {
        MarkDirty();
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