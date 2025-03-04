using MareLib;
using ProtoBuf;
using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Fishing3;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class BobberPacket
{
    public RodUseType useType;
    public long playerEntityId;

    public BobberPacket()
    {
    }

    public BobberPacket(RodUseType useType, long playerEntityId)
    {
        this.useType = useType;
        this.playerEntityId = playerEntityId;
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public enum RodUseType
{
    UseStart,
    UseEnd,
    AttackStart,
    AttackEnd
}

/// <summary>
/// Base class for bobbers.
/// </summary>
[Entity]
public class EntityBobber : Entity, IPhysicsTickable
{
    public long playerId;

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        if (api.Side == EnumAppSide.Server)
        {
            MainAPI.Sapi.Server.AddPhysicsTickable(this);
        }
    }

    public void SetPlayerData(EntityPlayer player)
    {
        playerId = player.EntityId;
    }

    public override void ToBytes(BinaryWriter writer, bool forClient)
    {
        base.ToBytes(writer, forClient);
        writer.Write(playerId);
    }

    public override void FromBytes(BinaryReader reader, bool forClient)
    {
        base.FromBytes(reader, forClient);
        playerId = reader.ReadInt64();
    }

    /// <summary>
    /// Called when using a pole while a bobber is active.
    /// </summary>
    public virtual void OnUseStart(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {
        FishingPoleSoundManager.Instance.StartSound(player, "fishing:sounds/linereel", dt => { });
    }

    /// <summary>
    /// Called when stopping using a pole while a bobber is active.
    /// </summary>
    public virtual void OnUseEnd(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {
        FishingPoleSoundManager.Instance.StopSound(player);
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

    public override void OnReceivedServerPacket(int packetId, byte[] data)
    {
        base.OnReceivedServerPacket(packetId, data);

        if (packetId == 5000)
        {
            BobberPacket? packet = SerializerUtil.Deserialize<BobberPacket>(data);
            if (packet == null) return;

            if (MainAPI.Client.GetEntityById(packet.playerEntityId) is not EntityPlayer player) return;

            ItemSlot hotbarSlot = player.Player.InventoryManager.ActiveHotbarSlot;
            if (hotbarSlot.Itemstack == null || hotbarSlot.Itemstack.Collectible is not ItemFishingPole) return;

            // Switch use type.
            switch (packet.useType)
            {
                case RodUseType.UseStart:
                    OnUseStart(false, hotbarSlot, player);
                    break;
                case RodUseType.UseEnd:
                    OnUseEnd(false, hotbarSlot, player);
                    break;
                case RodUseType.AttackStart:
                    OnAttackStart(false, hotbarSlot, player);
                    break;
                case RodUseType.AttackEnd:
                    OnAttackEnd(false, hotbarSlot, player);
                    break;
            }
        }
    }

    // Mirror use to player.
    public void BroadcastPacket(RodUseType type, EntityPlayer player)
    {
        BobberPacket packet = new(type, player.EntityId);
        MainAPI.Server.BroadcastEntityPacket(EntityId, 5000, SerializerUtil.Serialize(packet));
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        base.OnEntityDespawn(despawn);
        Dispose(despawn);
        if (Api.Side == EnumAppSide.Server)
        {
            MainAPI.Sapi.Server.RemovePhysicsTickable(this);
        }
    }

    public virtual void Dispose(EntityDespawnData despawn)
    {

    }

    public virtual void OnPhysicsTick(float dt)
    {

    }

    public void AfterPhysicsTick(float dt)
    {

    }

    public bool CanProceedOnThisThread()
    {
        if (RuntimeEnv.MainThreadId == Environment.CurrentManagedThreadId)
        {
            return true;
        }
        return false;
    }

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);
        if (Api.Side == EnumAppSide.Client)
        {
            OnClientBobberTick(dt);
        }
    }

    public virtual void OnClientBobberTick(float dt)
    {

    }

    public void OnPhysicsTickDone()
    {

    }

    public bool Ticking { get; set; } = true;
}