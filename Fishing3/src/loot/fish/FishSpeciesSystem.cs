using MareLib;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Fishing3;

public class FishSpecies
{
    public string code;

    public double baseKg;

    public Shape shape = null!; // Not on server.
    public Vector3 mouthOffset;

    // What direction the mouth is facing, rotate this to point towards what's needed.
    public Vector3 mouthFacing = new(-1, 0, 0);

    public Vector2 tempRange;

    public int tier;
    public float weight;

    public float satietyMultiplier;

    public HashSet<string> liquids = new();

    public string oilFluid;
    public float oilFluidPerKg = 100f;

    public FishSpecies(FishSpeciesJson json, ICoreAPI api)
    {
        code = json.code!;
        baseKg = json.baseKg;

        // Replace textures and set mouth offset for the client.
        if (api is ICoreClientAPI capi)
        {
            string path = json.model.Replace(":", ":shapes/") + ".json";
            shape = capi.Assets.TryGet(path).ToObject<Shape>();

            if (shape.Elements.Length > 0)
            {
                ShapeElement mainElement = shape.Elements[0];
                AttachmentPoint? ap = mainElement.AttachmentPoints?.Where(ap => ap.Code == "Mouth").FirstOrDefault();

                if (ap != null)
                {
                    mouthOffset.X = (float)(mainElement.From[0] + ap.PosX);
                    mouthOffset.Y = (float)(mainElement.From[1] + ap.PosY);
                    mouthOffset.Z = (float)(mainElement.From[2] + ap.PosZ);

                    mouthOffset /= 16;
                }
            }

            foreach (KeyValuePair<string, string> textureReplacement in json.textures)
            {
                shape.Textures[textureReplacement.Key] = textureReplacement.Value;
            }
        }

        tempRange = new Vector2((float)json.tempRange[0], (float)json.tempRange[1]);

        tier = json.tier;
        weight = json.weight;

        satietyMultiplier = json.satietyMultiplier;

        liquids.AddRange(json.liquids);

        oilFluid = json.oilFluid ?? "fishoil-generic";
        oilFluidPerKg = json.oilFluidPerKg;
    }

    public ItemStack CreateStack(ICoreAPI api, double weight)
    {
        ItemFish itemFish = (ItemFish)api.World.GetItem(new AssetLocation("fishing:fish"));

        ItemStack stack = new(itemFish);
        stack.Attributes.SetString("species", code);
        stack.Attributes.SetDouble("kg", weight);
        stack.StackSize = 1;

        if (oilFluid != null)
        {
            FluidRegistry fluidRegistry = MainAPI.GetGameSystem<FluidRegistry>(api.Side);
            if (fluidRegistry.TryGetFluid(oilFluid, out Fluid? fluid))
            {
                FluidStack oilStack = fluid.CreateFluidStack();
                oilStack.Units = (int)(weight * oilFluidPerKg);

                FluidContainer container = itemFish.GetContainer(stack);
                container.SetStack(oilStack);
            }
        }

        return stack;
    }
}

public class FishSpeciesJson
{
    public string? code;

    // Base weight (multiplied by catch size).
    public double baseKg = 1f;

    // Base model.
    public string model = "fishing:fish/salmon";

    // Valid everywhere by default.
    public double[] tempRange = new double[] { -25, 45 };

    // Texture overrides for this model (eye, fins, scales).
    public Dictionary<string, string> textures = new();

    public int tier = 0;
    public float weight = 1f;

    public float satietyMultiplier = 1f;

    public string[] liquids = new string[] { "water", "saltwater" };

    public string? oilFluid;
    public float oilFluidPerKg = 100f;
}

/// <summary>
/// Handles all species definitions for fish, for the fish item.
/// </summary>
[GameSystem]
public class FishSpeciesSystem : GameSystem
{
    private readonly Dictionary<string, FishSpecies> types = new();

    public IEnumerable<FishSpecies> SpeciesAlphabetical => types.Values.OrderBy(x => x.code);

    public FishSpeciesSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public bool TryGetSpecies(string code, [NotNullWhen(true)] out FishSpecies? species)
    {
        return types.TryGetValue(code, out species);
    }

    public override void OnAssetsLoaded()
    {
        List<IAsset> assets = api.Assets.GetMany("config/fish");

        foreach (IAsset asset in assets)
        {
            FishSpeciesJson? species = asset.ToObject<FishSpeciesJson>();
            if (species == null || species.code == null)
            {
                Console.WriteLine($"Failed to deserialize fish {asset.Location}");
                continue;
            }

            types[species.code] = new FishSpecies(species, api);
        }
    }

    public CreativeTabAndStackList[] GetCreativeStacks()
    {
        CreativeTabAndStackList[] array = new CreativeTabAndStackList[1];
        CreativeTabAndStackList tab = new();
        array[0] = tab;

        tab.Tabs = new string[] { "fishing" };
        List<JsonItemStack> stacks = new();

        foreach (FishSpecies type in types.Values)
        {
            for (int i = 0; i < 6; i++)
            {
                float scale = i * 2;
                if (i == 3) scale *= 2;
                if (i == 4) scale *= 4;
                if (i == 5) scale *= 8;

                JsonObject attributes = new(JToken.Parse("{ \"species\": \"" + type.code + "\", \"kg\": " + (1.5 + scale) + " }"));

                JsonItemStack jsonStack = new()
                {
                    Type = EnumItemClass.Item,
                    Code = "fishing:fish",
                    Attributes = attributes
                };

                jsonStack.Resolve(api.World, "", false);

                stacks.Add(jsonStack);
            }
        }

        tab.Stacks = stacks.ToArray();

        return array;
    }
}