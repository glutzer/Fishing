using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing3;

[GameSystem]
public class BobberRegistry : GameSystem
{
    public readonly Dictionary<string, Type> bobberTypes = [];

    public BobberRegistry(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public override void OnStart()
    {
        (Type, BobberAttribute)[] attribs = AttributeUtilities.GetAllAnnotatedClasses<BobberAttribute>();
        foreach ((Type type, _) in attribs)
        {
            bobberTypes[type.Name] = type;
        }
    }

    /// <summary>
    /// Tries to create and initialize a bobber, returns if type exists.
    /// </summary>
    public BobberBehavior? TryCreateAndInitializeBobber(string type, EntityBobber bobber, ItemStack? bobberStack, ItemStack? rodStack, JsonObject? properties)
    {
        if (bobberTypes.TryGetValue(type, out Type? bobberType))
        {
            BobberBehavior behavior = (BobberBehavior)Activator.CreateInstance(bobberType, bobber, api.Side == EnumAppSide.Server)!;

            if (bobberStack != null && rodStack != null && properties != null)
            {
                behavior.ServerInitialize(bobberStack, rodStack, properties);
            }

            return behavior;
        }

        Console.WriteLine($"Tried to create bobber of type {type}, but it does not exist.");

        return null;
    }
}