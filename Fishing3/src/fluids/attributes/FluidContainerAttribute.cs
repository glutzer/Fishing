using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing;

public static class AttributeExtensions1
{
    public static FluidContainer? GetFluidContainer(this ITreeAttribute instance, string key, ICoreAPI api)
    {
        if (instance.TryGetAttribute(key, out IAttribute? attribute))
        {
            if (attribute is FluidContainerAttribute fluidStackAttribute)
            {
                FluidContainer? container = fluidStackAttribute.TryGetContainer(api);
                if (container != null) return container; // Remove broken attribute.
            }

            // Wrong type.
            instance.RemoveAttribute(key);
        }

        return null; // Not found.
    }

    public static void SetFluidContainer(this ITreeAttribute instance, string key, FluidContainer fluidContainer)
    {
        instance[key] = new FluidContainerAttribute(fluidContainer);
    }
}

public class FluidContainerAttribute : IAttribute
{
    // Set container.
    private FluidContainer fluidContainer;
    private byte[]? stackData;

    public FluidContainerAttribute(FluidContainer container)
    {
        fluidContainer = container;
    }

    public FluidContainerAttribute(FluidContainer fluidContainer, byte[]? stackData)
    {
        this.fluidContainer = fluidContainer;
        this.stackData = stackData;
    }

    public FluidContainerAttribute()
    {

    }

    /// <summary>
    /// Returns false if the data is broken.
    /// </summary>
    public FluidContainer? TryGetContainer(ICoreAPI api)
    {
        // Container can't contain.
        if (fluidContainer.Capacity == 0) return null;

        // Attempt to convert the stack data into a container, then remove it.
        if (stackData != null)
        {
            FluidStack? stack = FluidStack.Load(stackData, api.Side);

            if (stack != null)
            {
                fluidContainer.SetStack(stack);
            }

            stackData = null;
        }

        return fluidContainer;
    }

    public IAttribute Clone()
    {
        if (fluidContainer.HeldStack != null)
        {
            stackData = FluidStack.Save(fluidContainer.HeldStack);
        }

        return new FluidContainerAttribute(new FluidContainer(fluidContainer.Capacity), stackData);
    }

    public bool Equals(IWorldAccessor worldForResolve, IAttribute attribute)
    {
        // Compare type.
        if (attribute is not FluidContainerAttribute fluidAttribute) return false;

        FluidContainer? container1 = TryGetContainer(worldForResolve.Api);
        FluidContainer? container2 = fluidAttribute.TryGetContainer(worldForResolve.Api);

        if (container1 == null || container2 == null) return false;
        if (container1.Capacity != container2.Capacity) return false;

        // Compare stack.
        if (container1.HeldStack == null && container2.HeldStack == null) return true;
        if (container1.HeldStack == null || container2.HeldStack == null) return false;

        // Simply compare fluids.
        return container1.HeldStack.fluid == container2.HeldStack.fluid && container1.HeldStack.Units == container2.HeldStack.Units;
    }

    public object? GetValue()
    {
        return fluidContainer;
    }

    public void ToBytes(BinaryWriter writer)
    {
        // Capacity of this container.
        writer.Write(fluidContainer.Capacity);

        // Does this container have a stack, or is there stack data present?
        writer.Write(fluidContainer.HeldStack != null || stackData != null);

        if (fluidContainer.HeldStack != null)
        {
            // Reset stack data.
            byte[] data = FluidStack.Save(fluidContainer.HeldStack);
            stackData = data;
            writer.Write(data.Length);
            writer.Write(data);
        }
        else if (stackData != null)
        {
            writer.Write(stackData.Length);
            writer.Write(stackData);
        }
    }

    public void FromBytes(BinaryReader reader)
    {
        int containerCapacity = reader.ReadInt32();
        fluidContainer = new FluidContainer(containerCapacity);

        bool hasStack = reader.ReadBoolean();

        // Stack data to later resolve.
        if (hasStack)
        {
            int length = reader.ReadInt32();
            stackData = reader.ReadBytes(length);
        }
    }

    public string ToJsonToken()
    {
        return ""; // Breaks page code.

        //return $"{{ \"capacity\": {fluidContainer.Capacity}," +
        //    $" \"roomLeft\": {fluidContainer.RoomLeft}," +
        //    $" \"fluidType\": {fluidContainer.HeldStack?.fluid.code ?? "none"}," +
        //    $" \"fluidStackType\": {fluidContainer.HeldStack?.GetType().Name ?? "none"} }}";
    }

    public int GetAttributeId()
    {
        return 233;
    }
}