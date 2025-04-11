using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Fishing3;

/// <summary>
/// This is the singleton of a fluid, similar to the singleton of a block or item.
/// </summary>
[Fluid]
public class Fluid
{
    public BlockEvent<(StringBuilder builder, FluidStack thisStack)> EventGetFluidInfo { get; private set; } = new();
    public BlockEvent<(FluidStack sourceStack, FluidStack thisStack, int toMove)> EventBeforeFluidAddedToOwnStack { get; private set; } = new();

    /// <summary>
    /// Check a fluid periodically, for things like spoilage.
    /// </summary>
    public BlockEvent<(FluidContainer container, object containingObject, ICoreAPI api)> EventCheckFluid { get; private set; } = new();

    /// <summary>
    /// Can this fluid stack take from another fluid stack?
    /// Called from the fluid stack class.
    /// </summary>
    public CheckBlockEvent<(FluidStack sourceStack, FluidStack thisStack)> EventCanTakeFrom { get; private set; } = new();

    /// <summary>
    /// Which fluid stack class will be used for this.
    /// Must extend FluidStack.
    /// StackTypes must use the same constructors.
    /// </summary>
    protected virtual Type StackType => typeof(FluidStack);

    private readonly SortedDictionary<string, FluidBehavior> behaviors = new();
    public IEnumerable<FluidBehavior> AllBehaviors => behaviors.Values;

    /// <summary>
    /// Creates a fluid stack of this fluid with 0 units.
    /// </summary>
    public virtual FluidStack CreateFluidStack()
    {
        FluidStack fluidStack = (FluidStack)Activator.CreateInstance(StackType, this)!;
        return fluidStack;
    }

    /// <summary>
    /// Creates a fluid stack of this fluid and sets the units.
    /// </summary>
    public FluidStack CreateFluidStack(int units)
    {
        FluidStack fluidStack = CreateFluidStack();
        fluidStack.Units = units;
        return fluidStack;
    }

    public readonly string code;
    public readonly int id;

    protected readonly float glowLevel;
    protected readonly Vector4 color;

    public readonly ICoreAPI api;

    public Fluid(FluidJson fluidJson, int id, ICoreAPI api)
    {
        code = fluidJson.Code;
        this.id = id;
        glowLevel = fluidJson.GlowLevel;

        float[] color = fluidJson.Color;
        this.color = new Vector4(color[0], color[1], color[2], color[3]);

        this.api = api;
    }

    /// <summary>
    /// Register events after this fluid is created.
    /// </summary>
    public virtual void RegisterEvents()
    {
        EventCanTakeFrom.Register(args =>
        {
            return args.sourceStack.fluid == args.thisStack.fluid;
        });
    }

    public virtual string GetName(FluidStack fluidStack)
    {
        return Lang.Get("fishing:fluid-" + code);
    }

    public virtual float GetGlowLevel(FluidStack fluidStack)
    {
        return glowLevel;
    }

    public virtual Vector4 GetColor(FluidStack fluidStack)
    {
        return color;
    }

    /// <summary>
    /// Try to add a new behavior from the fluid registry.
    /// </summary>
    public void AddBehavior(FluidBehavior behavior)
    {
        Type type = behavior.GetType();
        Type genericType = typeof(InnerClass<>).MakeGenericType(type);
        string id = (string)genericType.GetField("Name", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;

        behaviors.Add(id, behavior);
    }

    public T? GetBehavior<T>() where T : FluidBehavior
    {
        if (behaviors.TryGetValue(InnerClass<T>.Name, out FluidBehavior? behavior))
        {
            return (T)behavior;
        }

        return null;
    }

    public bool HasBehavior<T>() where T : FluidBehavior
    {
        return behaviors.ContainsKey(InnerClass<T>.Name);
    }
}