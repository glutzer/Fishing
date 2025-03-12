using MareLib;
using ProtoBuf;
using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
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
    public BobberBehavior? behavior;
    public ItemSlot? rodSlot;
    public string? bobberClass;

    private long casterId;
    private EntityPlayer? caster;

    public EntityPlayer? Caster
    {
        get
        {
            caster ??= Api.World.GetEntityById(casterId) as EntityPlayer;
            return caster;
        }
    }

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        if (api.Side == EnumAppSide.Server)
        {
            MainAPI.Sapi.Server.AddPhysicsTickable(this);
        }
    }

    /// <summary>
    /// Called on the server when initializing when cast.
    /// </summary>
    public void SetPlayerAndBobber(EntityPlayer player, string bobberType, ItemStack bobberStack, ItemStack rodStack, JsonObject properties)
    {
        casterId = player.EntityId;
        bobberClass = bobberType;
        behavior = MainAPI.GetGameSystem<BobberRegistry>(Api.Side).TryCreateAndInitializeBobber(bobberClass, this, bobberStack, rodStack, properties);
        rodSlot = player.Player.InventoryManager.ActiveHotbarSlot;
    }

    public override void ToBytes(BinaryWriter writer, bool forClient)
    {
        base.ToBytes(writer, forClient);
        writer.Write(casterId);

        writer.Write(bobberClass ?? "NaN");
        behavior?.ToBytes(writer, forClient);
    }

    public override void FromBytes(BinaryReader reader, bool forClient)
    {
        base.FromBytes(reader, forClient);
        casterId = reader.ReadInt64();

        string bClass = reader.ReadString();
        if (bClass == "NaN") return;

        if (bobberClass != bClass)
        {
            bobberClass = bClass;
            behavior?.Dispose(null);
            behavior = MainAPI.GetGameSystem<BobberRegistry>(forClient ? EnumAppSide.Client : EnumAppSide.Server).TryCreateAndInitializeBobber(bobberClass, this, null, null, null);
        }

        behavior?.FromBytes(reader, forClient);
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
                    behavior?.OnUseStart(false, hotbarSlot, player);
                    break;
                case RodUseType.UseEnd:
                    behavior?.OnUseEnd(false, hotbarSlot, player);
                    break;
                case RodUseType.AttackStart:
                    behavior?.OnAttackStart(false, hotbarSlot, player);
                    break;
                case RodUseType.AttackEnd:
                    behavior?.OnAttackEnd(false, hotbarSlot, player);
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
        behavior?.Dispose(despawn);
        if (Api.Side == EnumAppSide.Server)
        {
            MainAPI.Sapi.Server.RemovePhysicsTickable(this);
        }
    }

    public bool IsValid()
    {
        EntityPlayer? caster = Caster;
        if (caster == null) return false;

        ItemSlot hotbarSlot = caster.Player.InventoryManager.ActiveHotbarSlot;
        if (hotbarSlot.Itemstack == null || hotbarSlot.Itemstack.Collectible is not ItemFishingPole || hotbarSlot != rodSlot) return false;

        EntityBobber? bobber = ItemFishingPole.TryGetBobber(hotbarSlot, Api);
        if (bobber == null || bobber.EntityId != EntityId) return false;

        return true;
    }

    public void OnPhysicsTick(float dt)
    {
        if (!IsValid() && Alive)
        {
            if (Caster != null)
            {
                EntityPlayer player = Caster;
                MainAPI.Sapi.World.PlaySoundAt("fishing:sounds/linesnap", player.Pos.X, player.Pos.Y, player.Pos.Z, null, true, 16);
            }

            Die();
            return;
        }

        behavior?.OnServerPhysicsTick(dt);
    }

    public override void OnCollided()
    {
        base.OnCollided();
        behavior?.OnCollided();
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
            behavior?.OnClientTick(dt);
        }
    }

    public void OnPhysicsTickDone()
    {

    }

    public bool Ticking { get; set; } = true;

    public override double SwimmingOffsetY => 0.6f;
    public override float MaterialDensity => behavior != null ? behavior.CanFloat ? 800f : 2000f : 800f;
}