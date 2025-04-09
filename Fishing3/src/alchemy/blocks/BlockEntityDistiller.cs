using MareLib;
using OpenTK.Mathematics;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing3;

/// <summary>
/// Distills a fluid, leaving behind an item and outputting a distilled version.
/// </summary>
[BlockEntity]
public class BlockEntityDistiller : BlockEntityHeatedAlchemyEquipment, IFluidSink, IFluidSource
{
    protected ILoadedSound? bubblingSound;

    protected readonly FluidContainer inputContainer = new(1000);
    protected FluidRenderingInstance? renderInstance;

    private readonly InventoryGeneric genericInventory = new(1, null, null);

    private DistillationRecipe? selectedRecipe;
    private FluidStack? pendingFluidOutput;
    private ItemStack? pendingItemOutput;
    private int recipeTicksLeft;
    private bool wasEmpty;

    public override AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = new[]
            {
                new AlchemyAttachPoint(new Vector3(0.5f, 1f, 0.5f), true),
                new AlchemyAttachPoint(new Vector3(0.1f, 0.8f, 0.5f), false)
            };

    public override FluidContainer? GetInputContainer(int inputIndex)
    {
        return inputContainer;
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        genericInventory.LateInitialize($"distiller-{Pos.X}-{Pos.Y}-{Pos.Z}", api);

        if (api.Side == EnumAppSide.Client)
        {
            float rad = 0.35f;
            renderInstance = new(inputContainer, new Vector3(rad, 0.2f, rad), new Vector3(1f - rad, 0.6f, 1f - rad), Pos);
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
                Pitch = 1f
            });

            bubblingSound.Start();
        }
        else
        {
            UpdateRecipe();
        }
    }

    public void UpdateRecipe(bool onCompleted = false)
    {
        AlchemyRecipeRegistry registry = MainAPI.GetServerSystem<AlchemyRecipeRegistry>();
        DistillationRecipe? foundRecipe = null;

        if (inputContainer.Empty)
        {
            if (selectedRecipe != null) ResetRecipe();
            return;
        }

        if (selectedRecipe?.Matches(inputContainer) == true)
        {
            if (onCompleted)
            {
                recipeTicksLeft = selectedRecipe?.Ticks ?? 0;
            }

            // Still matches, no update needed.
            return;
        }

        foreach (DistillationRecipe recipe in registry.AllRecipes<DistillationRecipe>())
        {
            if (recipe.Matches(inputContainer))
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

        ResetRecipe();
        selectedRecipe = foundRecipe;
        recipeTicksLeft = selectedRecipe.Ticks;
    }

    public void ResetRecipe()
    {
        pendingFluidOutput = null;
        pendingItemOutput = null;
        selectedRecipe = null;
        recipeTicksLeft = 0;
    }

    public override void OnTick(int tick)
    {
        if (Api.Side == EnumAppSide.Client) return;

        if (tick % 20 == 0)
        {
            UpdateRecipe();
        }

        if (selectedRecipe == null || !selectedRecipe.InTempRange(heatPipeInstance.celsius)) return;

        if (recipeTicksLeft <= 0)
        {
            // Nothing to output to.
            FluidContainer? cont = GetOutputConnection(0);
            if (cont == null) return;
            FluidRegistry registry = MainAPI.GetServerSystem<FluidRegistry>();

            pendingFluidOutput ??= selectedRecipe.OutputFluid.CreateStack(registry);
            if (pendingFluidOutput == null)
            {
                ResetRecipe();
                return;
            }

            if (selectedRecipe.OutputItem != null && pendingItemOutput == null)
            {
                // Can be null?
                pendingItemOutput ??= new ItemStack(Api.World.GetItem(selectedRecipe.OutputItem));
            }

            // Check if fluid has room to output.
            if (cont.HasRoomFor(pendingFluidOutput) && cont.CanReceiveFluid(pendingFluidOutput))
            {
                if (pendingItemOutput != null)
                {
                    DummySlot slot = new()
                    {
                        Itemstack = pendingItemOutput
                    };

                    if (genericInventory[0].Itemstack != null)
                    {
                        // Incompatible items.
                        if (!genericInventory[0].Itemstack.Satisfies(pendingItemOutput)) return;

                        // Not enough space to output item.
                        int spaceLeft = genericInventory[0].Itemstack.Collectible.MaxStackSize - genericInventory[0].Itemstack.StackSize;
                        if (spaceLeft < slot.Itemstack.StackSize) return;
                    }

                    // Move the slot.
                    if (Random.Shared.NextSingle() <= selectedRecipe.OutputItemChance)
                    {
                        slot.TryPutInto(Api.World, genericInventory[0], slot.Itemstack.StackSize);
                        genericInventory[0].MarkDirty();
                    }
                }

                // Consume the input fluids.
                selectedRecipe.ConsumeFluids(inputContainer);

                FluidContainer.MoveFluids(pendingFluidOutput, cont);

                pendingFluidOutput = null;
                pendingItemOutput = null;

                EmitParticles(EnumAlchemyParticle.Drip, AlchemyAttachPoints[0].Position + AlchemyAttachPoints[0].CachedOffset, cont, 1f, 1);

                MarkConnectionDirty(0);
                MarkDirty();

                UpdateRecipe(true);
            }

            return;
        }

        recipeTicksLeft--;
        if (Random.Shared.NextSingle() < 0.05f) EmitParticles(EnumAlchemyParticle.Smoke, new Vector3(0.5f, 1f, 0.5f), inputContainer, 3f);
    }

    public override void OnBlockGone()
    {
        base.OnBlockGone();

        if (Api.Side == EnumAppSide.Client)
        {
            bubblingSound?.Stop();
            bubblingSound?.Dispose();
            if (renderInstance != null) FluidBlockRenderingSystem.Instance?.UnregisterInstance(renderInstance);
        }
    }

    public FluidContainer GetSink(int index)
    {
        return inputContainer;
    }

    public FluidContainer GetSource(int index)
    {
        return inputContainer;
    }

    public void MarkContainerDirty()
    {
        MarkDirty();
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        dsc.AppendLine($"{inputContainer.RoomUsed}/{inputContainer.Capacity}mL");
        if (genericInventory[0].Itemstack != null)
        {
            dsc.AppendLine($"{genericInventory[0].Itemstack.StackSize}x {genericInventory[0].Itemstack.Collectible.GetHeldItemName(genericInventory[0].Itemstack)}");
        }
        base.GetBlockInfo(forPlayer, dsc);

        dsc.AppendLine();
        inputContainer.HeldStack?.GetFluidInfo(dsc);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        genericInventory.ToTreeAttributes(tree);

        tree.SetInt("recipe", selectedRecipe?.Id ?? -1);
        tree.SetInt("ticksLeft", recipeTicksLeft);

        byte[] data = inputContainer.SaveStack();
        if (data.Length > 0) tree.SetBytes("contStack", data);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        // Sync current recipe.
        selectedRecipe = MainAPI.GetGameSystem<AlchemyRecipeRegistry>(worldAccessForResolve.Side).GetById<DistillationRecipe>(tree.GetInt("recipe", -1));
        recipeTicksLeft = tree.GetInt("ticksLeft", 0);

        // Change volume...
        if (worldAccessForResolve.Side == EnumAppSide.Client)
        {
            if (selectedRecipe != null && selectedRecipe.InTempRange(heatPipeInstance.celsius))
            {
                bubblingSound?.SetVolume(0.2f);
            }
            else
            {
                bubblingSound?.SetVolume(0f);
            }
        }

        byte[]? bytes = tree.GetBytes("contStack");
        if (bytes == null)
        {
            inputContainer.EmptyContainer();
        }
        else
        {
            inputContainer.LoadStack(bytes, worldAccessForResolve.Side);
        }

        genericInventory.FromTreeAttributes(tree);

        // Retessellate.
        if (worldAccessForResolve.Side == EnumAppSide.Client && wasEmpty != (genericInventory[0].Itemstack == null))
        {
            MarkDirty(true);
        }

        wasEmpty = genericInventory[0].Itemstack == null;
    }

    public override void ToggleInventory(IPlayer player, bool open)
    {
        if (genericInventory == null) return;

        if (open)
        {
            player.InventoryManager.OpenInventory(genericInventory);
        }
        else
        {
            player.InventoryManager.CloseInventory(genericInventory);
        }

        if (Api.Side == EnumAppSide.Client && open)
        {
            // Alembic has no inventory, just open a gui.
            GuiAlchemyEquipment gui = new(() =>
            {
                SendClientPacket(CLOSE_INVENTORY_PACKET);
            });
            gui.AddFluidMeter(inputContainer);
            gui.AddProcessingDisplay(() =>
            {
                return selectedRecipe != null && selectedRecipe.InTempRange(heatPipeInstance.celsius);
            });
            gui.AddItemGrid(genericInventory[0]);
            gui.TryOpen();
        }
    }

    public override void OnClientInteract()
    {
        SendClientPacket(OPEN_INVENTORY_PACKET);
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);

        if (Api.Side == EnumAppSide.Server)
        {
            genericInventory.DropAll(Pos.ToVec3d().Add(0.5f));
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if (!genericInventory[0].Empty)
        {
            AlchemyTessellationUtility.TessellateSolid(mesher, 0.15f, 0.38f);
        }

        return base.OnTesselation(mesher, tessThreadTesselator);
    }
}