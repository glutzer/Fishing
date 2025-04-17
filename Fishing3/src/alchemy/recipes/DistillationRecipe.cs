using MareLib;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

[AlchemyRecipeType("distillation")]
public class DistillationRecipe : IAlchemyRecipe, IParchmentable
{
    public int Id { get; set; }

    public string Title => "Distillation Recipe";

    public required FluidIngredient InputFluid { get; set; }
    public required FluidIngredient OutputFluid { get; set; }
    public float[] Temp { get; set; } = new float[] { 100, 1000 };

    /// <summary>
    /// Optional output residue.
    /// </summary>
    public string? OutputItem { get; set; }
    public int OutputItemQuantity { get; set; } = 1;
    public float OutputItemChance { get; set; } = 1f;

    public int Ticks { get; set; } = 20;

    public virtual void Initialize()
    {
        if (Temp.Length != 2) Temp = new float[] { 100, 1000 };
    }

    public bool InTempRange(float temp)
    {
        return temp >= Temp[0] && temp <= Temp[1];
    }

    public virtual bool Matches(FluidContainer container)
    {
        return InputFluid.ContainerContains(container);
    }

    public virtual void ConsumeFluids(FluidContainer container)
    {
        container.TakeOut(InputFluid.Units);
    }

    public virtual void WriteParchmentData(StringBuilder dsc, ICoreAPI api)
    {
        FluidRegistry reg = MainAPI.GetGameSystem<FluidRegistry>(api.Side);

        FluidStack? stack = InputFluid.CreateStack(reg);
        if (stack != null)
        {
            dsc.AppendLine($"{InputFluid.Units}mL of {stack.fluid.GetName(stack)} distills into:");
        }

        dsc.AppendLine();

        stack = OutputFluid.CreateStack(reg);
        if (stack != null)
        {
            dsc.AppendLine($"- {stack.fluid.GetName(stack)}: {OutputFluid.Units}mL");
        }

        if (OutputItem != null)
        {
            dsc.AppendLine($"- {OutputItem}: {OutputItemQuantity}");
        }

        dsc.AppendLine();
        dsc.AppendLine($"at {Temp[0]}°C-{Temp[1]}°C");
    }
}