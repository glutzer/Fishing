using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Fishing3;

public class RecipeFlotsam : FlotsamLoot
{
    public override ItemStack? CreateItem(ICoreServerAPI sapi)
    {
        AlchemyRecipeRegistry registry = MainAPI.GetGameSystem<AlchemyRecipeRegistry>(sapi.Side);
        return registry.GenerateRandomParchment();
    }
}

public class FlotsamLoot : IWeightable, ITierable
{
    // Item code.
    public required string Code { get; set; }

    public double[] TempRange { get; set; } = new double[] { -25, 45 };
    public float Weight { get; set; } = 1f;
    public int Tier { get; set; } = 0;

    public int StackSize { get; set; } = 1;
    public int StackAdd { get; set; } = 0;

    public System.Text.Json.Nodes.JsonObject? Attributes { get; set; }

    public virtual ItemStack? CreateItem(ICoreServerAPI sapi)
    {
        CollectibleObject? thing = sapi.World.GetItem(Code);
        thing ??= sapi.World.GetBlock(Code);
        if (thing == null) return null;
        int size = StackSize + Random.Shared.Next(StackAdd + 1);

        ItemStack stack = new(thing, size);

        if (Attributes != null && Attributes.ToString() is string jsonText)
        {
            // Parse the json text into a newtonsoft json object.
            JObject node = JObject.Parse(jsonText);

            Vintagestory.API.Datastructures.JsonObject vanillaObject = new(node);
            stack.Attributes = vanillaObject.ToAttribute() as ITreeAttribute ?? new TreeAttribute();
        }

        return stack;
    }
}

[Catchable]
public class CatchableFlotsam : Catchable
{
    public TierChooser tierChooser = new(0.1f);
    public List<FlotsamLoot> flotsamList = [];

    public CatchableFlotsam(ICoreServerAPI sapi) : base(sapi)
    {
        RecipeFlotsam recipeFlotsam = new() { Code = "recipe" };
        flotsamList.Add(recipeFlotsam);

        List<IAsset> assets = sapi.Assets.GetMany("config/flotsamitems");
        foreach (IAsset asset in assets)
        {
            string jsonText = asset.ToText();
            // Convert to json with System.Text.Json.
            JsonNode? json = JsonNode.Parse(jsonText);

            if (json is JsonArray jsonArray)
            {
                // Loop over every jsonObject in the jsonArray.
                foreach (JsonNode? arrayObj in jsonArray)
                {
                    if (arrayObj is not System.Text.Json.Nodes.JsonObject jsonObj) continue;

                    System.Text.Json.Nodes.JsonObject? jsonObject = JsonUtilities.HandleExtends(jsonObj, sapi);

                    JsonUtilities.ForEachVariant(jsonObject, variant =>
                    {
                        // Deserialize JsonObject to FluidJson.
                        FlotsamLoot? flotsamVariant = JsonSerializer.Deserialize<FlotsamLoot>(variant);
                        if (flotsamVariant == null) return; // Deserialization failed.

                        flotsamList.Add(flotsamVariant);
                    });
                }
            }
        }

        flotsamList.Sort((a, b) => a.Code.CompareTo(b.Code));
    }

    public override CaughtInstance Catch(FishingContext context, WeightedCatch weightedCatch, ICoreServerAPI sapi)
    {
        int tier = int.Parse(weightedCatch.code[^1].ToString());
        ItemStack flotsamStack = CreateFlotsamStack(out int itemCount, tier);
        return new CaughtInstance(flotsamStack, 10f + (itemCount * 5f), 0f, 0f);
    }

    public override IEnumerable<WeightedCatch> GetCatches(FishingContext context, ICoreServerAPI sapi)
    {
        string liquid = context.liquid.FirstCodePart();
        if (liquid is not "water" and not "saltwater") yield break; // No lava.

        for (int i = 1; i < 4; i++)
        {
            WeightedCatch weightedCatch = new(this, 0.5f, i, "flotsamT" + i.ToString());

            // Add the flotsam tag to this.
            weightedCatch.WithTag("flotsam");

            yield return weightedCatch;
        }
    }

    public ItemStack CreateFlotsamStack(out int itemCount, int tier)
    {
        Block flotsamBlock = sapi.World.GetBlock("fishing:flotsam-normal");
        ItemStack stack = new(flotsamBlock, 1);

        InventoryGeneric dummyInventory = new(9, "dummy", "dummy", sapi);
        itemCount = 0;

        // Roll possible items in flotsam based on fishing rarity, location.
        // Some items can only roll in certain areas.
        for (int i = 0; i < 9; i++)
        {
            if (Random.Shared.NextSingle() > 0.5f) continue;

            ItemSlot slot = dummyInventory[i];

            FlotsamLoot? loot = tierChooser.RollItem(flotsamList, tier);
            if (loot == null) continue;

            ItemStack? lootStack = loot.CreateItem(sapi);
            if (lootStack == null) continue;

            slot.Itemstack = lootStack;
        }

        dummyInventory.ToTreeAttributes(stack.Attributes);
        return stack;
    }
}