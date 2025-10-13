using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;

namespace Fishing;

public class AlchemyRecipeTypeAttribute : ClassAttribute
{
    public string path;

    /// <param name="path">Path used to get assets in alchemyrecipes/.</param>
    public AlchemyRecipeTypeAttribute(string path)
    {
        this.path = path;
    }
}

public interface IAlchemyRecipe
{
    /// <summary>
    /// Global recipe id, for equating.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Called after created and id set.
    /// </summary>
    void Initialize();
}

public interface IParchmentable
{
    void WriteParchmentData(StringBuilder dsc, ICoreAPI api);
    string Title { get; }
}

[GameSystem]
public class AlchemyRecipeRegistry : GameSystem
{
    /// <summary>
    /// Dictionary of recipe type (like retort) to a list of all recipes it has.
    /// </summary>
    private readonly Dictionary<string, List<IAlchemyRecipe>> recipes = [];
    private readonly Dictionary<string, Type> recipeTypeMap = [];

    private readonly List<IAlchemyRecipe> allRecipes = [];

    private readonly string[] validNames = new string[] { "Xethyr", "Posh", "Floyd" };

    /// <summary>
    /// Event where you can call AddRecipe.
    /// </summary>
    public event Action<AlchemyRecipeRegistry>? EventRegisterRecipes;

    private int currentId;

    public AlchemyRecipeRegistry(bool isServer, ICoreAPI api) : base(isServer, api)
    {
        // Recipe example.

        //EventRegisterRecipes += reg =>
        //{
        //    reg.AddRecipe<ReactorRecipe>(new ReactorRecipe()
        //    {
        //        Ingredients = new FluidIngredient[]
        //        {
        //            new() { Code = "water", Units = 1000 },
        //            new() { Code = "ethanol", Units = 500 }
        //        },
        //        Ticks = 100,
        //        OutputFluid = new FluidIngredient { Code = "steam", Units = 2000 }
        //    });
        //};
    }

    /// <summary>
    /// Enumerate over every recipe of a type.
    /// </summary>
    public IEnumerable<T> AllRecipes<T>() where T : IAlchemyRecipe
    {
        List<IAlchemyRecipe> recipes = this.recipes[InnerClass<T>.Name];

        foreach (IAlchemyRecipe recipe in recipes)
        {
            yield return (T)recipe;
        }
    }

    /// <summary>
    /// Add a recipe, not loaded from json, of the type.
    /// Manually for adding custom handlers.
    /// </summary>
    public void AddRecipe<T>(T recipe) where T : IAlchemyRecipe
    {
        recipes[InnerClass<T>.Name].Add(recipe);
        allRecipes.Add(recipe);
        recipe.Id = currentId++;
        recipe.Initialize();
    }

    /// <summary>
    /// Try to get a recipe as T, by id.
    /// </summary>
    public T? GetById<T>(int id) where T : IAlchemyRecipe
    {
        if (id < 0 || id >= allRecipes.Count) return default;
        IAlchemyRecipe recipe = allRecipes[id];
        return recipe is T typedRecipe ? typedRecipe : default;
    }

    public override void PreInitialize()
    {
        (Type, AlchemyRecipeTypeAttribute)[] attribs = AttributeUtilities.GetAllAnnotatedClasses<AlchemyRecipeTypeAttribute>();

        foreach ((Type type, AlchemyRecipeTypeAttribute _) in attribs)
        {
            recipes[type.Name] = [];
            recipeTypeMap[type.Name] = type;
        }
    }

    public ItemStack GenerateRandomParchment()
    {
        int length = allRecipes.Count;
        int index = Random.Shared.Next(length);

        while (true)
        {
            IAlchemyRecipe recipe = allRecipes[index];
            if (recipe is not IParchmentable parchmentable)
            {
                index++;
                continue;
            }

            StringBuilder builder = new();
            parchmentable.WriteParchmentData(builder, api);

            string parchmentData = builder.ToString();
            string author = validNames[Random.Shared.Next(validNames.Length)];

            Item paperParchment = api.World.GetItem("game:paper-parchment");
            ItemStack stack = new(paperParchment, 1);

            stack.Attributes.SetString("text", parchmentData);
            stack.Attributes.SetString("title", parchmentable.Title);
            stack.Attributes.SetString("signedby", author);

            return stack;
        }

        throw new Exception("No recipes are parchmentable.");
    }

    public override void OnAssetsLoaded()
    {
        string alchemyRecipeAssetPath = "config/alchemyrecipes";

        // For each key, get all recipe jsons.
        foreach (string key in recipes.Keys)
        {
            if (!recipeTypeMap.TryGetValue(key, out Type? recipeType)) continue;
            string? attribPath = recipeType.GetCustomAttribute<AlchemyRecipeTypeAttribute>()?.path;
            if (attribPath == null) continue;

            string path = $"{alchemyRecipeAssetPath}/{attribPath}";

            List<IAsset> assets = api.Assets.GetMany(path);
            assets.Sort((a, b) => a.Name.CompareTo(b.Name));

            foreach (IAsset asset in assets)
            {
                string? assetJson = asset.ToText();
                if (assetJson == null) continue;

                try
                {
                    JsonObject[]? objects = JsonSerializer.Deserialize<JsonObject[]>(assetJson);
                    if (objects == null) continue;

                    foreach (JsonObject obj in objects)
                    {
                        JsonUtilities.ForEachVariantNoCode(obj, variant =>
                        {
                            if (variant.Deserialize(recipeType) is not IAlchemyRecipe recipe) return;
                            recipe.Id = currentId++;
                            recipe.Initialize();
                            recipes[key].Add(recipe);
                            allRecipes.Add(recipe);
                        });
                    }
                }
                catch
                {
                    api.World.Logger.Error($"Failed to deserialize recipe from asset {asset.Name}: {assetJson}");
                    continue;
                }
            }
        }
    }
}