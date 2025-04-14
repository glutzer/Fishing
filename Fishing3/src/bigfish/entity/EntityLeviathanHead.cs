using MareLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static OpenTK.Graphics.OpenGL.GL;

namespace Fishing3;

[Entity]
public class EntityLeviathanHead : EntityLeviathanBase, IPhysicsTickable
{
    public EntityLeviathanBase[] segments = new EntityLeviathanBase[MAX_SEGMENTS];
    public bool Ticking { get; set; } = true;

    private readonly List<WormTask> tasks = new();
    private WormTask? currentTask;

    public void StartTask<T>() where T : WormTask
    {
        currentTask?.OnTaskStopped();

        foreach (WormTask task in tasks)
        {
            if (task is T)
            {
                currentTask = task;
                task.OnTaskStarted();
                break;
            }
        }
    }

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        if (api.Side == EnumAppSide.Server)
        {
            GenerateSegments();
            MainAPI.Sapi.Server.AddPhysicsTickable(this);

            // Instantiate all tasks.
            (Type, WormTaskAttribute)[] attribs = AttributeUtilities.GetAllAnnotatedClasses<WormTaskAttribute>();
            foreach ((Type type, WormTaskAttribute attrib) in attribs)
            {
                WormTask task = (WormTask)Activator.CreateInstance(type, attrib.priority, this)!;
                tasks.Add(task);
            }

            // Sort list with higher priority being first.
            tasks.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
        else
        {
            MainAPI.GetGameSystem<BossSystem>(api.Side).RegisterEntity(this);
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
        }
    }

    public void OnPhysicsTick(float dt)
    {
        if (!Alive) return;

        UpdateTasks(dt);

        for (int i = 1; i < segments.Length; i++)
        {
            EntityLeviathanSegment? seg = (EntityLeviathanSegment?)segments[i];

            if (seg == null || !seg.Alive)
            {
                // A segment is broken for some reason, this kills the worm.
                Die(EnumDespawnReason.Expire);
                return;
            }

            seg?.CascadingPhysicsTick(dt);
        }

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
            MainAPI.GetGameSystem<BossSystem>(Api.Side).UnregisterEntity(this);
        }
    }
}