using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Fishing3;

// It is calculated if: the entity is CollidedWithGround, or Swimming.

[Entity]
public class EntityLeviathanHead : EntityLeviathanBase, IPhysicsTickable
{
    public Vector3 Facing { get; private set; } = Vector3.UnitY;

    public EntityLeviathanBase[] segments = new EntityLeviathanBase[MAX_SEGMENTS];
    public bool Ticking { get; set; } = true;

    private readonly List<WormTask> tasks = new();
    private WormTask? currentTask;

    private readonly WormCollisionTester collTester = new();

    /// <summary>
    /// Is the head's hitbox colliding with a block?
    /// </summary>
    public bool CollidingWithGround { get; private set; }

    public Entity Entity => this;

    private Accumulator clientTicker = Accumulator.WithInterval(1 / 20f).Max(1f);

    /// <summary>
    /// Move the head towards it's current facing.
    /// </summary>
    public void Move(float distance)
    {
        ServerPos.Add(Facing.X * distance, Facing.Y * distance, Facing.Z * distance);
        if (ServerPos.Y < 1) ServerPos.Y = 1;

        EntityLeviathanBase lastSegment = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            EntityLeviathanBase segment = segments[i];

            segment.MoveToSegment(lastSegment);
            lastSegment = segment;
        }
    }

    /// <summary>
    /// Set the current facing and yaw/pitch.
    /// </summary>
    public void SetFacing(Vector3 facing)
    {
        Facing = facing.Normalized();
        ServerPos.Yaw = (float)Math.Atan2(-Facing.X, -Facing.Z);
        ServerPos.Pitch = (float)Math.Asin(Facing.Y);
    }

    /// <summary>
    /// Lerp to a facing and set it.
    /// </summary>
    public void LerpToFacing(Vector3 facing, float value)
    {
        Quaternion currentQuat = QuaternionUtility.FromToRotation(new Vector3(1f, 0f, 0f), Facing);
        Quaternion targetQuat = QuaternionUtility.FromToRotation(new Vector3(1f, 0f, 0f), facing);
        Quaternion lerpedQuat = Quaternion.Slerp(currentQuat, targetQuat, value);
        Facing = lerpedQuat * new Vector3(1f, 0f, 0f);
        ServerPos.Yaw = (float)Math.Atan2(-Facing.X, -Facing.Z);
        ServerPos.Pitch = (float)Math.Asin(Facing.Y);
    }

    /// <summary>
    /// Start a task and return it.
    /// </summary>
    public T StartTask<T>() where T : WormTask
    {
        currentTask?.OnTaskStopped();

        foreach (WormTask task in tasks)
        {
            if (task is T t)
            {
                currentTask = task;
                task.OnTaskStarted();
                return t;
            }
        }

        throw new Exception("Task not found.");
    }

    /// <summary>
    /// Get a task, execute a delegate, then start it.
    /// </summary>
    public void StartTask<T>(Action<T>? dele) where T : WormTask
    {
        currentTask?.OnTaskStopped();

        foreach (WormTask task in tasks)
        {
            if (task is T t)
            {
                currentTask = task;
                dele?.Invoke(t);
                task.OnTaskStarted();
            }
        }

        throw new Exception("Task not found.");
    }

    public void StopTask()
    {
        currentTask?.OnTaskStopped();
        currentTask = null;
        SendTaskUpdate();
    }

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        if (api.Side == EnumAppSide.Server)
        {
            GenerateSegments();
            MainAPI.Sapi.Server.AddPhysicsTickable(this);
        }
        else
        {
            MainAPI.GetGameSystem<BossSystem>(api.Side).RegisterEntity(this);
        }

        // Instantiate all tasks.
        (Type, WormTaskAttribute)[] attribs = AttributeUtilities.GetAllAnnotatedClasses<WormTaskAttribute>();
        foreach ((Type type, WormTaskAttribute attrib) in attribs)
        {
            WormTask task = (WormTask)Activator.CreateInstance(type, attrib.priority, this)!;
            tasks.Add(task);
        }

        // Sort list with higher priority being first.
        tasks.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        for (int i = 0; i < tasks.Count; i++)
        {
            tasks[i].SetId(i);
        }
    }

    /// <summary>
    /// Spawn and initialize every segment.
    /// </summary>
    public void GenerateSegments()
    {
        segments[0] = this;
        Head = this;

        EntityProperties type = Api.World.GetEntityType(new AssetLocation("fishing:leviathansegment"));

        for (int i = 1; i < segments.Length; i++)
        {
            EntityLeviathanSegment segment = (EntityLeviathanSegment)Api.World.ClassRegistry.CreateEntity(type);
            segment.SegmentId = i;
            segments[i] = segment;
            segment.Head = this;

            // Offset pos slightly to avoid initial z-fighting.
            segment.ServerPos.SetPos(ServerPos.XYZ.Add(0.1 * i, 0.1 * i, 0.1 * i));

            segment.ParentSegment = segments[i - 1];

            Api.World.SpawnEntity(segment);
        }
    }

    public void UpdateTasks(float dt)
    {
        if (currentTask == null)
        {
            foreach (WormTask task in tasks)
            {
                if (task.CanStartTask(dt))
                {
                    currentTask = task;
                    task.OnTaskStarted();
                    SendTaskUpdate();
                    break;
                }
            }

            // No task can be started right now.
            if (currentTask == null) return;
        }

        currentTask.TickTask(dt);
        if (!currentTask.CanContinueTask(dt))
        {
            currentTask.OnTaskStopped();
            currentTask = null;

            foreach (WormTask task in tasks)
            {
                if (task.CanStartTask(dt))
                {
                    currentTask = task;
                    task.OnTaskStarted();
                    break;
                }
            }

            SendTaskUpdate();
        }
    }

    public void OnPhysicsTick(float dt)
    {
        if (!Alive) return;

        CollidingWithGround = collTester.IsColliding(this, Api);
        Block fluidBlockAtPos = MainAPI.Sapi.World.BlockAccessor.GetBlock(ServerPos.AsBlockPos, BlockLayersAccess.Fluid);
        Swimming = fluidBlockAtPos.Id != 0;

        UpdateTasks(dt);

        // Every tick try to deal damage from every segment.
        TouchDamage();
    }

    /// <summary>
    /// Apply touch damage on the server.
    /// </summary>
    public void TouchDamage()
    {
        IPlayer[] players = MainAPI.Server.GetPlayersAround(ServerPos.XYZ, 500, 500);
        if (players.Length > 0)
        {
            Cuboidd playerCuboid = new();
            Cuboidd segmentCuboid = new();

            foreach (IPlayer player in players)
            {
                EntityPlayer playerEntity = player.Entity;
                playerCuboid.SetAndTranslate(playerEntity.CollisionBox, playerEntity.ServerPos.XYZ);

                foreach (EntityLeviathanBase segment in segments)
                {
                    segmentCuboid.SetAndTranslate(segment.CollisionBox, segment.ServerPos.XYZ);
                    if (playerCuboid.Intersects(segmentCuboid))
                    {
                        playerEntity.ReceiveDamage(new DamageSource()
                        {
                            Source = EnumDamageSource.Entity,
                            Type = EnumDamageType.BluntAttack,
                            SourceEntity = this
                        }, 1);

                        break;
                    }
                }
            }
        }
    }

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);

        if (Api.Side == EnumAppSide.Server || !Alive) return;

        clientTicker.Add(dt);
        while (clientTicker.Consume())
        {
            CollidingWithGround = collTester.IsColliding(this, Api);
            Block fluidBlockAtPos = MainAPI.Capi.World.BlockAccessor.GetBlock(Pos.AsBlockPos, BlockLayersAccess.Fluid);
            Swimming = fluidBlockAtPos.Id != 0;

            currentTask?.TickTask(clientTicker.interval);
        }
    }

    public void AfterPhysicsTick(float dt)
    {
    }

    public bool CanProceedOnThisThread()
    {
        return Environment.CurrentManagedThreadId == RuntimeEnv.ServerMainThreadId;
    }

    public void OnPhysicsTickDone()
    {
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        base.OnEntityDespawn(despawn);
        if (Api.Side == EnumAppSide.Server)
        {
            MainAPI.Sapi.Server.RemovePhysicsTickable(this);
        }
        else
        {
            MainAPI.TryGetGameSystem(Api.Side, out BossSystem? bossSystem);
            bossSystem?.UnregisterEntity(this);
        }

        foreach (WormTask task in tasks)
        {
            task.OnEntityUnloaded();
        }
    }

    /// <summary>
    /// On the server, sends a task update to the client.
    /// Suitable when a new task is started, or when the current task needs a data update.
    /// </summary>
    public void SendTaskUpdate()
    {
        if (Api.Side != EnumAppSide.Server) return;

        if (currentTask == null)
        {
            MainAPI.Sapi.Network.BroadcastEntityPacket(EntityId, 1011, null);
        }
        else
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(currentTask.Id);
            currentTask.ToBytes(writer);

            MainAPI.Sapi.Network.BroadcastEntityPacket(EntityId, 1010, stream.ToArray());
        }
    }

    public override void OnReceivedClientPacket(IServerPlayer player, int packetId, byte[]? data)
    {
        base.OnReceivedClientPacket(player, packetId, data);

        if (packetId == 1010)
        {
            if (data == null) return;

            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            try
            {
                int taskId = reader.ReadInt32();
                WormTask? task = tasks.Find(t => t.Id == taskId);

                if (task != null)
                {
                    task.FromBytes(reader);

                    if (task != currentTask)
                    {
                        currentTask?.OnTaskStopped();
                        currentTask = task;
                        currentTask.OnTaskStarted();
                    }
                }
            }
            catch
            {

            }
        }

        if (packetId == 1011)
        {
            currentTask?.OnTaskStopped();
            currentTask = null;
        }
    }
}