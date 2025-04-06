using MareLib;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

[AlchemyRecipeType("reactor")]
public class ReactorRecipe : IAlchemyRecipe, IParchmentable
{
    public int Id { get; set; }

    public required FluidIngredient[] Ingredients { get; set; }
    public required FluidIngredient OutputFluid { get; set; }

    public double[] Temp { get; set; } = new double[] { 100, 1000 };
    public int Ticks { get; set; } = 20;

    public string Title => "Reactor Recipe";

    public virtual void Initialize()
    {
        if (Temp.Length != 2) Temp = new double[] { 100, 1000 };
    }

    public bool InTempRange(float temp)
    {
        return temp >= Temp[0] && temp <= Temp[1];
    }

    /// <summary>
    /// Can this recipe be crafted?
    /// </summary>
    public virtual bool Matches(FluidContainer[] containers, float temperature)
    {
        if (!InTempRange(temperature)) return false;

        // Check if all ingredients are present in the containers.
        foreach (FluidIngredient ingredient in Ingredients)
        {
            bool foundMatch = false;

            // Look for this ingredient in any container.
            foreach (FluidContainer container in containers)
            {
                // Skip empty containers.
                if (container.HeldStack == null) continue;

                // Check if this container holds the required fluid type.
                if (container.HeldStack.fluid.code == ingredient.Code)
                {
                    // Check if the container has enough fluid.
                    if (container.HeldStack.Units >= ingredient.Units)
                    {
                        foundMatch = true;
                        break;
                    }
                }
            }

            // This ingredient does not exist, can't craft.
            if (!foundMatch) return false;
        }

        return true;
    }

    /// <summary>
    /// Consume fluid needed for this recipe from the containers.
    /// </summary>
    public virtual void ConsumeFluid(FluidContainer[] containers)
    {
        foreach (FluidIngredient ingredient in Ingredients)
        {
            // Find the first container that has enough of this fluid.
            foreach (FluidContainer container in containers)
            {
                if (container.HeldStack == null) continue;

                if (container.HeldStack.fluid.code == ingredient.Code)
                {
                    container.TakeOut(ingredient.Units);
                    break; // Exit the loop after consuming from the first matching container.
                }
            }
        }
    }

    /// <summary>
    /// Get stack this recipe would output.
    /// </summary>
    public virtual FluidStack? GetOutputStack(FluidContainer[] containers)
    {
        FluidRegistry reg = MainAPI.GetGameSystem<FluidRegistry>(EnumAppSide.Server);
        return OutputFluid.CreateStack(reg);
    }

    public virtual void WriteParchmentData(StringBuilder dsc, ICoreAPI api)
    {
        FluidRegistry reg = MainAPI.GetGameSystem<FluidRegistry>(api.Side);

        dsc.AppendLine("Ingredients:");

        foreach (FluidIngredient ingredient in Ingredients)
        {
            FluidStack? stack = ingredient.CreateStack(reg);
            if (stack == null) continue;
            dsc.AppendLine($"- {stack.fluid.GetName(stack)}: {ingredient.Units}mL");
        }

        dsc.AppendLine();
        FluidStack? outputStack = OutputFluid.CreateStack(reg);
        if (outputStack == null) return;
        dsc.AppendLine($"Mixes into {OutputFluid.Units}mL of {outputStack.fluid.GetName(outputStack)} at {Temp[0]}°C-{Temp[1]}°C");
    }
}