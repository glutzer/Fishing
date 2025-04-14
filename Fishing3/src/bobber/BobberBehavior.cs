using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Fishing3;

public class BobberBehavior
{
    public readonly EntityBobber bobber;
    public readonly bool isServer;
    public virtual bool CanFloat => true;

    public BobberBehavior(EntityBobber bobber, bool isServer)
    {
        this.bobber = bobber;
        this.isServer = isServer;
    }

    /// <summary>
    /// Initialize once on the server with attributes from a stack when casting.
    /// Save to bytes if needed.
    /// </summary>
    public virtual void ServerInitialize(ItemStack bobberStack, ItemStack rodSlot, JsonObject properties)
    {

    }

    public virtual void OnServerPhysicsTick(float dt)
    {

    }

    public virtual void OnClientTick(float dt)
    {

    }

    public virtual void OnCollided()
    {

    }

    /// <summary>
    /// Called when using a pole while a bobber is active.
    /// </summary>
    public virtual void OnUseStart(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {

    }

    /// <summary>
    /// Called when stopping using a pole while a bobber is active.
    /// </summary>
    public virtual void OnUseEnd(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {

    }

    /// <summary>
    /// Called when attacking with pole while a bobber is active.
    /// </summary>
    public virtual void OnAttackStart(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {

    }

    /// <summary>
    /// Called when stopping attacking with a pole while a bobber is active.
    /// </summary>
    public virtual void OnAttackEnd(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {

    }

    public virtual void Dispose(EntityDespawnData? despawn)
    {

    }

    public virtual void ToBytes(BinaryWriter writer, bool forClient)
    {

    }

    public virtual void FromBytes(BinaryReader reader, bool forClient)
    {

    }
}