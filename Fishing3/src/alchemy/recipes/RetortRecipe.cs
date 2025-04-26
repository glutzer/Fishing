using MareLib;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

[AlchemyRecipeType("retort")]
public class RetortRecipe : IAlchemyRecipe, IParchmentable
{
    public int Id { get; set; }

    public required string InputItem { get; set; }
    public required FluidIngredient OutputFluid { get; set; }
    public float[] Temp { get; set; } = new float[] { 100, 1000 };

    protected string? regex;

    public int Ticks { get; set; } = 20;

    public string Title => "Retort Recipe";

    public virtual void Initialize()
    {
        if (InputItem.Contains('*') == true)
        {
            regex = BetterWildCard.ConvertToWildCard(InputItem);
        }

        if (Temp.Length != 2) Temp = new float[] { 100, 1000 };
    }

    public bool InTempRange(float temp)
    {
        return temp >= Temp[0] && temp <= Temp[1];
    }

    public virtual bool Matches(ItemStack input)
    {
        return regex != null ? BetterWildCard.Matches(input, regex) : input.Collectible.Code.ToString() == InputItem;
    }

    public virtual FluidStack? GetOutputStack(ItemStack input)
    {
        FluidRegistry reg = MainAPI.GetGameSystem<FluidRegistry>(EnumAppSide.Server);
        return OutputFluid.CreateStack(reg);
    }

    public virtual void ConsumeIngredients(ItemSlot slot)
    {
        slot.TakeOut(1);
        slot.MarkDirty();
    }

    public virtual void WriteParchmentData(StringBuilder dsc, ICoreAPI api)
    {
        if (regex == null)
        {
            CollectibleObject? obj = api.World.GetItem(InputItem);
            obj ??= api.World.GetBlock(InputItem);
            if (obj != null)
            {
                ItemStack stack = new(obj, 1);
                dsc.AppendLine($"Input: {obj.GetHeldItemName(stack)}");
            }
        }
        else
        {
            dsc.AppendLine($"Input: {InputItem}");
        }

        dsc.AppendLine();

        FluidStack? flStack = OutputFluid.CreateStack(MainAPI.GetGameSystem<FluidRegistry>(api.Side));
        if (flStack == null) return;

        dsc.AppendLine($"Melts into {OutputFluid.Units}mL of {flStack.fluid.GetName(flStack)} at {Temp[0]}°C-{Temp[1]}°C");
    }
}