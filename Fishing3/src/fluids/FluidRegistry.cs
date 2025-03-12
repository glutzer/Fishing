using MareLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing3;

/// <summary>
/// Fluid registry, existing on both the client and the server.
/// </summary>
[GameSystem]
public class FluidRegistry : GameSystem
{
    public Fluid[] Fluids { get; private set; } = new Fluid[512];

    private readonly Dictionary<string, Fluid> fluidsByCode = new();
    private readonly Dictionary<string, Type> fluidTypeMapping = new();

    public FluidRegistry(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public override void PreInitialize()
    {
        AssetCategory.categories["fluidtypes"] = new AssetCategory("fluidtypes", true, EnumAppSide.Universal);
    }

    public override void Initialize()
    {
        TreeAttribute.RegisterAttribute(233, typeof(FluidContainerAttribute));
    }

    public override void OnAssetsLoaded()
    {
        RegisterFluids();
    }

    public Fluid GetFluid(string code)
    {
        return fluidsByCode[code];
    }

    public bool TryGetFluid(string code, [NotNullWhen(true)] out Fluid? fluid)
    {
        return fluidsByCode.TryGetValue(code, out fluid);
    }

    /// <summary>
    /// Registers all fluid classes, then loads all fluid assets.
    /// </summary>
    public void RegisterFluids()
    {
        // Register all types by their name.
        (Type, FluidAttribute)[] types = AttributeUtilities.GetAllAnnotatedClasses<FluidAttribute>();
        foreach ((Type type, _) in types)
        {
            fluidTypeMapping.Add(type.Name, type);
        }

        // Load all fluids from json.
        List<IAsset> fluidAssets = api.Assets.GetMany("fluidtypes");

        // Incremental id.
        int id = 0;

        foreach (IAsset item in fluidAssets)
        {
            string jsonText = item.ToText();

            // Convert to json with System.Text.Json.
            JsonNode? json = JsonNode.Parse(jsonText);

            if (json is System.Text.Json.Nodes.JsonObject jsonObject)
            {
                jsonObject = JsonUtilities.HandleExtends(jsonObject, api);

                JsonUtilities.ForEachVariant(jsonObject, variant =>
                {
                    // Deserialize JsonObject to FluidJson.
                    FluidJson? fluidJson = JsonSerializer.Deserialize<FluidJson>(variant);
                    if (fluidJson == null) return; // Deserialization failed.

                    // Instantiate the fluid.
                    Type type = fluidTypeMapping[fluidJson.Class];
                    Fluid fluid = (Fluid)Activator.CreateInstance(type, fluidJson, id, api)!;

                    Fluids[id++] = fluid;
                    fluidsByCode[fluidJson.Code] = fluid;
                });
            }
        }
    }
}