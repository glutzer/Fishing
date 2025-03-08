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

namespace Fishing3;

public class FishSpecies
{
    public string code;

    public double baseKg;

    public Shape shape = null!; // Not on server.
    public Vector3 mouthOffset;

    // What direction the mouth is facing, rotate this to point towards what's needed.
    public Vector3 mouthFacing = new(-1, 0, 0);

    public int tier;
    public float weight;

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

        tier = json.tier;
        weight = json.weight;
    }

    public ItemStack CreateStack(ICoreAPI api, float weight)
    {
        ItemStack stack = new(api.World.GetItem(new AssetLocation("fishing:fish")));
        stack.Attributes.SetString("species", code);
        stack.Attributes.SetFloat("kg", weight);
        stack.StackSize = 1;
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

    // Texture overrides for this model (eye, fins, scales).
    public Dictionary<string, string> textures = new();

    public int tier = 0;
    public float weight = 1f;
}

/// <summary>
/// Handles all species definitions for fish, for the fish item.
/// </summary>
[GameSystem]
public class FishSpeciesSystem : GameSystem
{
    private readonly Dictionary<string, FishSpecies> types = new();

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