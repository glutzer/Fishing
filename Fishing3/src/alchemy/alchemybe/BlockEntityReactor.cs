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
public class BlockEntityReactor : BlockEntityHeatedAlchemyEquipment, IFluidSink, IFluidSource
{
    protected readonly FluidContainer containerLeft = new(1000);
    protected FluidRenderingInstance? renderInstanceLeft;

    protected readonly FluidContainer containerRight = new(1000);
    protected FluidRenderingInstance? renderInstanceRight;

    protected ILoadedSound? bubblingSound;

    private ReactorRecipe? selectedRecipe;
    private FluidStack? pendingOutput;
    private int recipeTicksLeft;

    public override AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = new[]
            {
                new AlchemyAttachPoint(new Vector3(0.25f, 1f, 0.65f), false),
                new AlchemyAttachPoint(new Vector3(0.75f, 1f, 0.65f), false),
                new AlchemyAttachPoint(new Vector3(0.5f, 1.1f, 0.5f), true)
            };

    public override FluidContainer? GetInputContainer(int inputIndex)
    {
        return inputIndex switch
        {
            0 => containerLeft,
            1 => containerRight,
            _ => null
        };
    }

    public override void OnTick(int tick)
    {
        if (Api.Side == EnumAppSide.Client) return;

        // No item.
        if (tick % 20 == 0)
        {
            UpdateRecipe();
            if (selectedRecipe == null) return;
        }

        // Nothing to process.
        if (selectedRecipe == null) return;

        // Not hot enough.
        if (!selectedRecipe.InTempRange(heatPipeInstance.celsius)) return;

        // Emit smoke randomly.
        if (Random.Shared.NextSingle() < 0.05f) EmitParticles(EnumAlchemyParticle.Smoke, new Vector3(0.5f, 1f, 0.5f), 3);

        recipeTicksLeft--;
        if (recipeTicksLeft < 0)
        {
            // Nothing to output to.
            FluidContainer? cont = GetOutputConnection(2);
            if (cont == null) return;

            if (pendingOutput == null)
            {
                FluidContainer[] containers = new FluidContainer[] { containerLeft, containerRight };
                pendingOutput = selectedRecipe.GetOutputStack(containers);
            }

            if (pendingOutput == null)
            {
                ResetRecipe();
                return;
            }

            if (cont.HasRoomFor(pendingOutput))
            {
                FluidContainer[] containers = new FluidContainer[] { containerLeft, containerRight };

                if (!selectedRecipe.Matches(containers, heatPipeInstance.celsius))
                {
                    UpdateRecipe();
                    return;
                }

                // Merge purity here?
                // Weight based on amount consumed from each input.

                selectedRecipe.ConsumeFluid(containers);

                int times = Math.Min(pendingOutput.Units, 5);

                FluidContainer.MoveFluids(pendingOutput, cont);

                EmitParticles(EnumAlchemyParticle.Drip, AlchemyAttachPoints[2].Position + AlchemyAttachPoints[2].CachedOffset, cont, 1f, times);

                MarkConnectionDirty(0);
                UpdateRecipe(true);
            }
        }
    }

    public override void OnClientInteract()
    {
        GuiAlchemyEquipment gui = new();
        gui.AddFluidMeter(containerLeft);
        gui.AddProcessingDisplay(() =>
        {
            return selectedRecipe != null && selectedRecipe.InTempRange(heatPipeInstance.celsius) && !containerLeft.Empty && !containerRight.Empty;
        });
        gui.AddFluidMeter(containerRight);
        gui.TryOpen();
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api.Side == EnumAppSide.Client)
        {
            renderInstanceLeft = new(containerLeft, RotateVectorToSide(new Vector3(0.15f, 0.25f, 0.4f)), RotateVectorToSide(new Vector3(0.35f, 0.95f, 0.6f)), Pos);
            FluidBlockRenderingSystem.Instance?.RegisterInstance(renderInstanceLeft);

            renderInstanceRight = new(containerRight, RotateVectorToSide(new Vector3(0.65f, 0.25f, 0.4f)), RotateVectorToSide(new Vector3(0.85f, 0.95f, 0.6f)), Pos);
            FluidBlockRenderingSystem.Instance?.RegisterInstance(renderInstanceRight);

            bubblingSound = MainAPI.Capi.World.LoadSound(new SoundParams()
            {
                Location = "fishing:sounds/bubbling",
                ShouldLoop = true,
                Position = new Vec3f(Pos.X + 0.5f, Pos.Y + 0.5f, Pos.Z + 0.5f),
                DisposeOnFinish = true,
                RelativePosition = false,
                Volume = 0f,
                SoundType = EnumSoundType.Sound,
                Range = 16f,
                Pitch = 1.5f
            });

            bubblingSound.Start();
        }
    }

    public override void OnBlockGone()
    {
        base.OnBlockGone();

        if (Api.Side == EnumAppSide.Client)
        {
            if (renderInstanceLeft != null) FluidBlockRenderingSystem.Instance?.UnregisterInstance(renderInstanceLeft);
            if (renderInstanceRight != null) FluidBlockRenderingSystem.Instance?.UnregisterInstance(renderInstanceRight);
            bubblingSound?.Stop();
            bubblingSound?.Dispose();
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        byte[] inputData = containerLeft.SaveStack();
        if (inputData.Length > 0) tree.SetBytes("leftStack", inputData);

        byte[] outputData = containerRight.SaveStack();
        if (outputData.Length > 0) tree.SetBytes("rightStack", outputData);

        tree.SetInt("recipe", selectedRecipe?.Id ?? -1);
        tree.SetInt("ticksLeft", recipeTicksLeft);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        byte[]? inputData = tree.GetBytes("leftStack");
        if (inputData == null)
        {
            containerLeft.EmptyContainer();
        }
        else
        {
            containerLeft.LoadStack(inputData, worldAccessForResolve.Side);
        }

        byte[]? outputData = tree.GetBytes("rightStack");
        if (outputData == null)
        {
            containerRight.EmptyContainer();
        }
        else
        {
            containerRight.LoadStack(outputData, worldAccessForResolve.Side);
        }

        // Sync current recipe.
        selectedRecipe = MainAPI.GetGameSystem<AlchemyRecipeRegistry>(worldAccessForResolve.Side).GetById<ReactorRecipe>(tree.GetInt("recipe", -1));
        recipeTicksLeft = tree.GetInt("ticksLeft", 0);
    }

    public FluidContainer GetSink(int index)
    {
        return index switch
        {
            0 => containerLeft,
            1 => containerRight,
            _ => containerLeft
        };
    }

    public FluidContainer GetSource(int index)
    {
        return index switch
        {
            0 => containerLeft,
            1 => containerRight,
            _ => containerLeft
        };
    }

    public void MarkContainerDirty()
    {
        MarkDirty();
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        dsc.AppendLine($"Left: {containerLeft.RoomUsed}/{containerLeft.Capacity}mL");
        dsc.AppendLine($"Right: {containerRight.RoomUsed}/{containerRight.Capacity}mL");
        base.GetBlockInfo(forPlayer, dsc);

        if (containerLeft.HeldStack != null)
        {
            dsc.AppendLine();
            dsc.AppendLine("Left:");
            containerLeft.HeldStack.GetFluidInfo(dsc);
        }

        if (containerRight.HeldStack != null)
        {
            dsc.AppendLine();
            dsc.AppendLine("Right:");
            containerRight.HeldStack.GetFluidInfo(dsc);
        }
    }

    public void ResetRecipe()
    {
        pendingOutput = null;
        selectedRecipe = null;
        recipeTicksLeft = 0;
    }

    public void UpdateRecipe(bool onCompleted = false)
    {
        if (containerLeft.Empty || containerRight.Empty)
        {
            ResetRecipe();
            return;
        }

        FluidContainer[] containers = new FluidContainer[] { containerLeft, containerRight };

        if (onCompleted && selectedRecipe?.Matches(containers, heatPipeInstance.celsius) == true)
        {
            pendingOutput = null;
            recipeTicksLeft = selectedRecipe.Ticks;
            return;
        }

        AlchemyRecipeRegistry registry = MainAPI.GetServerSystem<AlchemyRecipeRegistry>();

        ReactorRecipe? foundRecipe = null;

        foreach (ReactorRecipe recipe in registry.AllRecipes<ReactorRecipe>())
        {
            if (recipe.Matches(containers, heatPipeInstance.celsius))
            {
                foundRecipe = recipe;
                break;
            }
        }

        if (foundRecipe == null)
        {
            ResetRecipe();
            return;
        }

        if (foundRecipe == selectedRecipe) return;

        selectedRecipe = foundRecipe;
        recipeTicksLeft = foundRecipe.Ticks;
        MarkDirty();
    }
}