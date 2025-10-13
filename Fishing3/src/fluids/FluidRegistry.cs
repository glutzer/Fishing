using Newtonsoft.Json.Linq;
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

    private readonly Dictionary<string, Fluid> fluidsByCode = [];
    private readonly Dictionary<string, Type> fluidTypeMapping = [];

    private readonly Dictionary<string, Type> fluidBehaviorTypeMapping = [];

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
        TreeAttribute.RegisterAttribute(234, typeof(Vector3Attribute));
        TreeAttribute.RegisterAttribute(235, typeof(Vector4Attribute));
    }

    public override void OnAssetsLoaded()
    {
        RegisterFluidBehaviors();
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

    private void RegisterFluidBehaviors()
    {
        (Type, FluidBehaviorAttribute)[] types = AttributeUtilities.GetAllAnnotatedClasses<FluidBehaviorAttribute>();
        foreach ((Type type, _) in types)
        {
            fluidBehaviorTypeMapping.Add(type.Name, type);
        }
    }

    private void AddFluidBehaviors(Fluid fluid, System.Text.Json.Nodes.JsonObject fluidJson)
    {
        if (fluidJson.TryGetPropertyValue("Behaviors", out JsonNode? behaviorsNode) && behaviorsNode is System.Text.Json.Nodes.JsonObject behaviors)
        {
            foreach (KeyValuePair<string, JsonNode?> behavior in behaviors)
            {
                string behaviorName = behavior.Key;
                if (behavior.Value is not System.Text.Json.Nodes.JsonObject behaviorObject) continue; // Json incorrect.

                fluidBehaviorTypeMapping.TryGetValue(behaviorName, out Type? behaviorType);
                if (behaviorType == null) continue; // Behavior does not exist.

                // Pass the object assigned to the behavior.
                FluidBehavior fluidBehavior = (FluidBehavior)Activator.CreateInstance(behaviorType, behaviorObject)!;
                fluid.AddBehavior(fluidBehavior);
                fluidBehavior.RegisterEvents(fluid);
            }
        }
    }

    /// <summary>
    /// Registers all fluid classes, then loads all fluid assets.
    /// </summary>
    private void RegisterFluids()
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

                    fluid.RegisterEvents();

                    AddFluidBehaviors(fluid, variant);

                    Fluids[id++] = fluid;
                    fluidsByCode[fluidJson.Code] = fluid;
                });
            }
        }
    }

    /// <summary>
    /// Get creative stacks for this item, in the fishing-fluids category.
    /// </summary>
    public CreativeTabAndStackList[] GetCreativeStacks(ItemFluidStorage fluidStorageItem)
    {
        CreativeTabAndStackList[] array = new CreativeTabAndStackList[1];
        CreativeTabAndStackList tab = new();
        array[0] = tab;

        tab.Tabs = new string[] { "fishing-fluids" };
        List<JsonItemStack> stacks = [];

        JsonItemStack emptyStack = new()
        {
            Type = EnumItemClass.Item,
            Code = fluidStorageItem.Code,
            Attributes = new(JToken.Parse("{}"))
        };
        emptyStack.Resolve(api.World, "", false);

        stacks.Add(emptyStack);

        foreach (Fluid type in Fluids)
        {
            if (type == null) break; // Reached end.
            Vintagestory.API.Datastructures.JsonObject attributes = new(JToken.Parse("{ \"fillWith\": \"" + type.code + "\" }"));

            // Special type of fluid where units can't be set, don't display.
            FluidStack stack = type.CreateFluidStack(100);
            if (stack.Units == 0) continue;

            JsonItemStack jsonStack = new()
            {
                Type = EnumItemClass.Item,
                Code = fluidStorageItem.Code,
                Attributes = attributes
            };

            jsonStack.Resolve(api.World, "", false);

            stacks.Add(jsonStack);
        }

        tab.Stacks = stacks.ToArray();

        return array;
    }
}