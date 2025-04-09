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
/// A retort heats items into gas, using retort recipes.
/// </summary>
[BlockEntity]
public class BlockEntityRetort : BlockEntityHeatedAlchemyEquipment
{
    protected ILoadedSound? bubblingSound;

    private readonly InventoryGeneric genericInventory = new(1, null, null);

    private RetortRecipe? selectedRecipe;
    private FluidStack? pendingOutput;
    private int recipeTicksLeft;
    private bool wasEmpty;

    public override AlchemyAttachPoint[] AlchemyAttachPoints { get; set; } = new[]
            {
                new AlchemyAttachPoint(new Vector3(0.5f, 1f, 0.5f), true)
            };

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        genericInventory.LateInitialize($"retort-{Pos.X}-{Pos.Y}-{Pos.Z}", api);

        if (api.Side == EnumAppSide.Client)
        {
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
                Pitch = 1.5f
            });

            bubblingSound.Start();
        }
        else
        {
            genericInventory.SlotModified += ServerInventoryChanged;
            UpdateRecipe();
        }
    }

    public override void OnTick(int tick)
    {
        if (Api.Side == EnumAppSide.Client) return;

        // Nothing to process.
        if (selectedRecipe == null) return;

        // Not hot enough.
        if (!selectedRecipe.InTempRange(heatPipeInstance.celsius)) return;

        // No item.
        if (genericInventory[0].Itemstack == null)
        {
            ResetRecipe();
            return;
        }

        if (recipeTicksLeft < 0)
        {
            // Nothing to output to.
            FluidContainer? cont = GetOutputConnection(0);
            if (cont == null) return;

            pendingOutput ??= selectedRecipe.GetOutputStack(genericInventory[0].Itemstack);
            if (pendingOutput == null)
            {
                ResetRecipe();
                return;
            }

            if (cont.HasRoomFor(pendingOutput) && cont.CanReceiveFluid(pendingOutput))
            {
                // The slot modified event may remove it.
                if (pendingOutput == null) return;

                int times = Math.Min(pendingOutput.Units, 5);

                FluidContainer.MoveFluids(pendingOutput, cont);

                selectedRecipe.ConsumeIngredients(genericInventory[0]);

                EmitParticles(EnumAlchemyParticle.Drip, AlchemyAttachPoints[0].Position + AlchemyAttachPoints[0].CachedOffset, cont, 1f, times);

                MarkConnectionDirty(0);
                UpdateRecipe(true);
            }

            return;
        }

        // Emit smoke randomly.
        recipeTicksLeft--;
        if (Random.Shared.NextSingle() < 0.05f) EmitParticles(EnumAlchemyParticle.Smoke, new Vector3(0.5f, 1f, 0.5f), 3f);
    }

    public void ResetRecipe()
    {
        pendingOutput = null;
        selectedRecipe = null;
        recipeTicksLeft = 0;
    }

    public void UpdateRecipe(bool onCompleted = false)
    {
        if (genericInventory[0].Itemstack == null)
        {
            ResetRecipe();
            return;
        }

        if (onCompleted && selectedRecipe?.Matches(genericInventory[0].Itemstack) == true)
        {
            pendingOutput = null;
            recipeTicksLeft = selectedRecipe.Ticks;
            return;
        }

        AlchemyRecipeRegistry registry = MainAPI.GetServerSystem<AlchemyRecipeRegistry>();

        RetortRecipe? foundRecipe = null;

        foreach (RetortRecipe recipe in registry.AllRecipes<RetortRecipe>())
        {
            if (recipe.Matches(genericInventory[0].Itemstack))
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
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);

        // Sync current recipe.
        selectedRecipe = MainAPI.GetGameSystem<AlchemyRecipeRegistry>(worldAccessForResolve.Side).GetById<RetortRecipe>(tree.GetInt("recipe", -1));
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
            gui.AddItemGrid(genericInventory[0]);
            gui.AddProcessingDisplay(() =>
            {
                return selectedRecipe != null && selectedRecipe.InTempRange(heatPipeInstance.celsius);
            });
            gui.TryOpen();
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        if (genericInventory[0].Itemstack != null)
        {
            dsc.AppendLine($"{genericInventory[0].Itemstack.StackSize}x {genericInventory[0].Itemstack.Collectible.GetHeldItemName(genericInventory[0].Itemstack)}");
        }
        base.GetBlockInfo(forPlayer, dsc);
    }

    public override void OnBlockGone()
    {
        base.OnBlockGone();

        bubblingSound?.Stop();
        bubblingSound?.Dispose();
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