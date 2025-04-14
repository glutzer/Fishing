using MareLib;
using System.Text;
using Vintagestory.API.Common;
using JsonObject = System.Text.Json.Nodes.JsonObject;

namespace Fishing3;

[Effect]
public class EffectSatiety : AlchemyEffect, IEffectInfoProvider
{
    private EnumFoodCategory category; // Type.
    private float satiety; // Satiety per unit of fluid.
    private float nutritionGainMultiplier; // Multiplier to nutrition gained.

    public override void ApplyInstantEffect()
    {
        if (!IsServer) return;

        if (Entity is EntityAgent agent)
        {
            agent.ReceiveSaturation(satiety * Units * StrengthMultiplier, category, 10, nutritionGainMultiplier);
        }
    }

    public override void CollectDataFromFluidStack(FluidStack stack, ApplicationMethod method)
    {
        if (method == ApplicationMethod.Skin)
        {
            StrengthMultiplier *= 0.25f;
        }
    }

    public override void CollectDataFromReagent(JsonObject jsonObject)
    {
        category = jsonObject.Get("Category", "Fruit")!.ToLower() switch
        {
            "fruit" => EnumFoodCategory.Fruit,
            "vegetable" => EnumFoodCategory.Vegetable,
            "protein" => EnumFoodCategory.Protein,
            "grain" => EnumFoodCategory.Grain,
            "dairy" => EnumFoodCategory.Dairy,
            _ => EnumFoodCategory.Fruit
        };

        satiety = jsonObject.Get("Satiety", 1f);

        nutritionGainMultiplier = jsonObject.Get("NutritionGainMultiplier", 1f);
    }

    public void GetInfo(StringBuilder builder, FluidStack stack)
    {
        float strength = StrengthMultiplier * FluidBehaviorReagent.GetPurityMultiplier(stack);
        builder.AppendLine($"{(int)(satiety * stack.Units * strength)} {category} nutrition");
    }
}