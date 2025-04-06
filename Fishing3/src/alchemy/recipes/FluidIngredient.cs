namespace Fishing3;

public class FluidIngredient
{
    public required string Code { get; set; }
    public int Units { get; set; } = 1;

    public bool ContainerContains(FluidContainer container)
    {
        return container.HeldStack != null &&
               container.HeldStack.fluid.code == Code &&
               container.HeldStack.Units >= Units;
    }

    public FluidStack? CreateStack(FluidRegistry fluidRegistry)
    {
        if (fluidRegistry.TryGetFluid(Code, out Fluid? fluid))
        {
            FluidStack stack = fluid.CreateFluidStack();
            stack.Units = Units;
            return stack;
        }

        return null;
    }
}