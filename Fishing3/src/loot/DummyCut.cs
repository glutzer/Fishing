using Fishing3;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Fishing;

[Item]
public class DummyCut : Item
{
    public FishSpeciesSystem fishSpeciesSystem = null!;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        fishSpeciesSystem = MainAPI.GetGameSystem<FishSpeciesSystem>(api.Side);
    }

    public override bool ConsumeCraftingIngredients(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe)
    {
        ItemSlot? fishSlot = slots.FirstOrDefault((s) => s.Itemstack?.Item is ItemFish);
        bool isCleaver = slots.Any((s) => s.Itemstack?.Item is ItemCleaver cleaver);

        if (fishSlot != null)
        {
            ItemFish fish = (ItemFish)fishSlot.Itemstack.Item;
            FishSpecies? species = fish.GetSpecies(fishSlot.Itemstack);
            if (species == null) return base.ConsumeCraftingIngredients(slots, outputSlot, matchingRecipe);

            float kgPerCut = MathF.Round(1f / species.satietyMultiplier, 2);
            double kg = ItemFish.GetWeight(fishSlot.Itemstack);
            int maxCuts = (int)Math.Ceiling(kg / kgPerCut);
            maxCuts = Math.Clamp(maxCuts, 0, isCleaver ? 16 : 4);

            bool exists = ItemFish.ConsumeWeight(fishSlot.Itemstack, kgPerCut * maxCuts);
            if (!exists)
            {
                ItemStack boneStack = new(api.World.GetItem(new AssetLocation("game:bone")))
                {
                    StackSize = 2
                }; // Should be initial kg.
                boneStack.ResolveBlockOrItem(api.World);

                fishSlot.TakeOutWhole();
                fishSlot.Itemstack = boneStack;
            }
            fishSlot.MarkDirty();

            if (api is ICoreServerAPI sapi)
            {
                IPlayer player = sapi.World.PlayerByUid(fishSlot.Inventory.openedByPlayerGUIds.FirstOrDefault("unknown"));
                if (player != null) sapi.World.PlaySoundAt(new AssetLocation("fishing:sounds/stab"), player.Entity, null, true, 16);
            }

            string cutCode = fishSlot.Itemstack.Attributes.GetBool("smoked") ? "game:fish-cooked" : "game:fish-raw";

            CollectibleObject cut = api.World.GetItem(new AssetLocation(cutCode));
            cut ??= api.World.GetBlock(new AssetLocation(cutCode));

            if (cut != null)
            {
                ItemStack cutStack = new(cut)
                {
                    StackSize = maxCuts
                };

                outputSlot.TakeOutWhole();
                outputSlot.Itemstack = cutStack;
                outputSlot.MarkDirty();
            }
        }

        return base.ConsumeCraftingIngredients(slots, outputSlot, matchingRecipe);
    }
}