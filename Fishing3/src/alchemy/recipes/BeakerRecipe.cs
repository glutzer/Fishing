using MareLib;
using System.Text;
using Vintagestory.API.Common;

namespace Fishing3;

// I have pasted the retort 5 times.
[AlchemyRecipeType("beaker")]
public class BeakerRecipe : IAlchemyRecipe, IParchmentable
{
    public int Id { get; set; }

    public required FluidIngredient InputFluid { get; set; }
    public required string InputItem { get; set; }
    public required FluidIngredient OutputFluid { get; set; }
    public float[] Temp { get; set; } = new float[] { 0, 1000 };

    protected string? regex;

    public int Ticks { get; set; } = 20;

    public string Title => "Beaker Recipe";

    public virtual void Initialize()
    {
        if (InputItem.Contains('*') == true)
        {
            regex = BetterWildCard.ConvertToWildCard(InputItem);
        }

        if (Temp.Length != 2) Temp = new float[] { 0, 1000 };
    }

    public bool InTempRange(float temp)
    {
        return temp >= Temp[0] && temp <= Temp[1];
    }

    public virtual bool Matches(ItemStack input, FluidContainer inputContainer)
    {
        if (!InputFluid.ContainerContains(inputContainer)) return false;

        if (regex != null)
        {
            return BetterWildCard.Matches(input, regex);
        }

        return input.Collectible.Code.ToString() == InputItem;
    }

    public virtual FluidStack? GetOutputStack(ItemStack input, FluidContainer inputContainer)
    {
        FluidRegistry reg = MainAPI.GetGameSystem<FluidRegistry>(EnumAppSide.Server);
        return OutputFluid.CreateStack(reg);
    }

    public virtual void ConsumeIngredients(ItemSlot slot, FluidContainer inputContainer)
    {
        inputContainer.TakeOut(InputFluid.Units);
        slot.TakeOut(1);
        slot.MarkDirty();
    }

    public virtual void WriteParchmentData(StringBuilder dsc, ICoreAPI api)
    {
        if (regex == null)
        {
            CollectibleObject? obj = api.World.GetItem(InputItem);
            obj ??= api.World.GetBlock(InputItem);
            if (obj == null) return;

            ItemStack stack = new(obj, 1);
            dsc.AppendLine($"Input: {obj.GetHeldItemName(stack)}");
        }
        else
        {
            dsc.AppendLine($"Input: {InputItem}");
        }


        FluidStack? inputStack = InputFluid.CreateStack(MainAPI.GetGameSystem<FluidRegistry>(api.Side));
        if (inputStack == null) return;
        dsc.AppendLine($"Input: {inputStack.fluid.GetName(inputStack)}");

        dsc.AppendLine();

        FluidStack? flStack = OutputFluid.CreateStack(MainAPI.GetGameSystem<FluidRegistry>(api.Side));
        if (flStack == null) return;

        dsc.AppendLine($"Mixes into {OutputFluid.Units}mL of {flStack.fluid.GetName(flStack)} at {Temp[0]}°C-{Temp[1]}°C");
    }
}