using OpenTK.Mathematics;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing;

[BlockEntity]
public class BlockEntityBeaker : BlockEntityHeatedAlchemyEquipment, IFluidSink, IFluidSource
{
    protected ILoadedSound? bubblingSound;

    private readonly FluidContainer inputContainer = new(1000);
    private readonly InventoryGeneric genericInventory = new(1, null, null);
    protected FluidRenderingInstance? renderInstance;

    private BeakerRecipe? selectedRecipe;
    private FluidStack? pendingOutput;
    private int recipeTicksLeft;
    private bool wasEmpty;

    public override AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = new[]
            {
                new AlchemyAttachPoint(new Vector3(0.5f, 1f, 0.5f), false),
                new AlchemyAttachPoint(new Vector3(0.5f, 0.3f, 0.9f), true)
            };

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        genericInventory.LateInitialize($"beaker-{Pos.X}-{Pos.Y}-{Pos.Z}", api);

        if (api.Side == EnumAppSide.Client)
        {
            float rad = 0.35f;
            renderInstance = new(inputContainer, new Vector3(rad, 0.2f, rad), new Vector3(1f - rad, 0.8f, 1f - rad), Pos);
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
        else
        {
            genericInventory.SlotModified += ServerInventoryChanged;
            UpdateRecipe();
        }
    }

    public override FluidContainer? GetInputContainer(int inputIndex)
    {
        return inputContainer;
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

    public override void OnTick(int tick)
    {
        if (Api.Side == EnumAppSide.Client) return;

        if (tick % 20 == 0)
        {
            UpdateRecipe();
        }

        // Nothing to process.
        if (selectedRecipe == null) return;

        // Not hot enough.
        if (!selectedRecipe.InTempRange(heatPipeInstance.celsius)) return;

        // No item.
        if (genericInventory[0].Itemstack == null || inputContainer.Empty)
        {
            ResetRecipe();
            return;
        }

        if (recipeTicksLeft < 0)
        {
            // Nothing to output to.
            FluidContainer? cont = GetOutputConnection(1);
            if (cont == null) return;

            pendingOutput ??= selectedRecipe.GetOutputStack(genericInventory[0].Itemstack, inputContainer);
            if (pendingOutput == null)
            {
                ResetRecipe();
                return;
            }

            if (cont.HasRoomFor(pendingOutput) && cont.CanReceiveFluid(pendingOutput))
            {
                if (!selectedRecipe.Matches(genericInventory[0].Itemstack, inputContainer))
                {
                    UpdateRecipe(false);
                    return;
                }

                MarkDirty();

                // The slot modified event may remove it.
                if (pendingOutput == null) return;

                int times = Math.Min(pendingOutput.Units, 5);

                FluidContainer.MoveFluids(pendingOutput, cont);

                // Consume after. Changing the item slot updates the recipe.
                selectedRecipe.ConsumeIngredients(genericInventory[0], inputContainer);

                EmitParticles(EnumAlchemyParticle.Drip, AlchemyAttachPoints[1].Position + AlchemyAttachPoints[1].CachedOffset, cont, 1f, times);

                MarkConnectionDirty(1);
                UpdateRecipe(true);
            }

            return;
        }

        // Emit smoke randomly.
        recipeTicksLeft--;

        // Beaker does not emit particles.
        //if (Random.Shared.NextSingle() < 0.05f) EmitParticles(EnumAlchemyParticle.Smoke, new Vector3(0.5f, 1f, 0.5f), 3f);
    }

    public void ResetRecipe()
    {
        pendingOutput = null;
        selectedRecipe = null;
        recipeTicksLeft = 0;
    }

    public void UpdateRecipe(bool onCompleted = false)
    {
        if (genericInventory[0].Itemstack == null || inputContainer.HeldStack == null)
        {
            if (selectedRecipe != null) MarkDirty();

            ResetRecipe();
            return;
        }

        if (selectedRecipe?.Matches(genericInventory[0].Itemstack, inputContainer) == true)
        {
            if (onCompleted)
            {
                pendingOutput = null;
                recipeTicksLeft = selectedRecipe.Ticks;
            }

            return;
        }

        AlchemyRecipeRegistry registry = MainAPI.GetServerSystem<AlchemyRecipeRegistry>();

        BeakerRecipe? foundRecipe = null;

        foreach (BeakerRecipe recipe in registry.AllRecipes<BeakerRecipe>())
        {
            if (recipe.Matches(genericInventory[0].Itemstack, inputContainer))
            {
                foundRecipe = recipe;
                break;
            }
        }

        if (foundRecipe == null)
        {
            if (selectedRecipe != null) MarkDirty();

            ResetRecipe();
            return;
        }

        if (foundRecipe == selectedRecipe) return;

        selectedRecipe = foundRecipe;
        recipeTicksLeft = foundRecipe.Ticks;
        MarkDirty();
    }

    /// <summary>
    /// Callback on server for slot modified.
    /// </summary>
    private void ServerInventoryChanged(int slotId)
    {
        UpdateRecipe();
    }

    public override void OnClientInteract()
    {
        SendClientPacket(OPEN_INVENTORY_PACKET);
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
        selectedRecipe = MainAPI.GetGameSystem<AlchemyRecipeRegistry>(worldAccessForResolve.Side).GetById<BeakerRecipe>(tree.GetInt("recipe", -1));
        recipeTicksLeft = tree.GetInt("ticksLeft", 0);

        // Change volume...
        if (worldAccessForResolve.Side == EnumAppSide.Client)
        {
            if (selectedRecipe != null && selectedRecipe.InTempRange(heatPipeInstance.celsius) && heatPipeInstance.celsius > 50)
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
                ToggleInventory(MainAPI.Capi.World.Player, false);
                SendClientPacket(CLOSE_INVENTORY_PACKET);
            });
            gui.AddItemGrid(genericInventory[0]);
            gui.AddFluidMeter(inputContainer);
            gui.AddProcessingDisplay(() =>
            {
                return selectedRecipe != null && selectedRecipe.InTempRange(heatPipeInstance.celsius);
            });
            gui.TryOpen();
        }
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

        genericInventory.CloseForAllPlayers();
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
}