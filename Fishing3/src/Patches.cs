using HarmonyLib;
using MareLib;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.Server.Systems;

namespace Fishing3;

public class Patches
{
    [HarmonyPatch(typeof(EntityPlayer), MethodType.Constructor)]
    public static class AcquireClaimInProgressPatch
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayer __instance)
        {
            __instance.Stats
                .Register("flaskEffect")
                .Register("fishRarity")
                .Register("fishQuantity")
                .Register("reelStrength");
        }
    }

    // Entity renderer patches.

    [HarmonyPatch(typeof(EntityBehaviorNameTag), "OnRenderFrame")]
    public class RendererPatch2D
    {
        [HarmonyPrefix]
        public static bool Prefix(EntityBehaviorNameTag __instance)
        {
            return !EntityOverlaySystem.EntityHasRenderingDisabled(__instance.entity);
        }
    }

    [HarmonyPatch(typeof(EntityShapeRenderer), "DoRender3DOpaqueBatched")]
    public class RendererPatch
    {
        /// <summary>
        /// Animation UBO captured by player renderer.
        /// </summary>
        public static UBORef? AnimationUbo { get; private set; }
        private static Action? after;

        [HarmonyPrefix]
        public static bool Prefix(EntityShapeRenderer __instance, bool isShadowPass)
        {
            if (isShadowPass && EntityOverlaySystem.EntityHasRenderingDisabled(__instance.entity))
            {
                return false;
            }

            // Enqueue actual renderer.
            if (!isShadowPass && EntityOverlaySystem.EntityHasHandler(__instance.entity))
            {
                Entity entity = __instance.entity;
                AnimationUbo = MainAPI.Capi.Render.CurrentActiveShader.UBOs["Animation"];

                after = () =>
                {
                    MultiTextureMeshRef? meshRef = __instance.GetField<MultiTextureMeshRef>("meshRefOpaque");
                    if (meshRef == null) return;

                    float[] modelMat = __instance.ModelMat;

                    IAnimator? animator = __instance.entity.AnimManager.Animator;
                    if (animator == null) return;

                    OverlayRenderInfo renderInfo = new(meshRef, animator, modelMat)
                    {
                        renderFlags = __instance.AddRenderFlags
                    };

                    EntityOverlaySystem.Instance?.ExecuteHandlers(entity, renderInfo);
                };

                if (EntityOverlaySystem.EntityHasRenderingDisabled(entity))
                {
                    return false;
                }
            }

            // Returning false here will remove all rendering.
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (after != null)
            {
                after();
                after = null;
            }
        }
    }

    /// <summary>
    /// Allow tracking outside of range.
    /// </summary>
    [HarmonyPatch(typeof(PhysicsManager), "UpdateTrackedEntitiesStates")]
    public class TrackingRangePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PhysicsManager __instance, IDictionary<int, ConnectedClient> clients)
        {
            double[]? positions = __instance.GetField<double[]>("positions");
            CachingConcurrentDictionary<long, Entity> loadedEntities = __instance.GetField<CachingConcurrentDictionary<long, Entity>>("loadedEntities");
            ServerSystemEntitySimulation es = __instance.GetField<ServerSystemEntitySimulation>("es");
            int trackingRangeSq = es.GetField<int>("trackingRangeSq");
            EntityDespawnData outofRangeDespawnData = __instance.GetField<EntityDespawnData>("outofRangeDespawnData");
            ServerMain server = MainAPI.Server;
            ServerUdpNetwork udpNetwork = __instance.GetField<ServerUdpNetwork>("udpNetwork");

            if (positions == null || positions.Length != clients.Count * 3)
            {
                positions = new double[clients.Count * 3];
                __instance.SetField("positions", positions);
            }

            int i = 0;
            foreach (ConnectedClient client in clients.Values)
            {
                if (client.State is not EnumClientState.Connected and not EnumClientState.Playing)
                {
                    positions[i * 3] = double.MaxValue;
                    positions[(i * 3) + 1] = double.MaxValue;
                    positions[(i * 3) + 2] = double.MaxValue;
                    i++;
                }
                else
                {
                    EntityPos pos = client.Position;
                    positions[i * 3] = pos.X;
                    positions[(i * 3) + 1] = pos.Y;
                    positions[(i * 3) + 2] = pos.Z;
                    i++;
                }
            }
            foreach (Entity entity in loadedEntities.Values)
            {
                double x = entity.ServerPos.X;
                double y = entity.ServerPos.Y;
                double z = entity.ServerPos.Z;
                double minRangeSq = double.MaxValue;
                double trackRange = Math.Max(trackingRangeSq, entity.SimulationRange * entity.SimulationRange);
                bool isTracked = entity.IsTracked > 0;
                int j = 0;
                foreach (ConnectedClient client2 in clients.Values)
                {
                    double num = x - positions[j * 3];
                    double dy = y - positions[(j * 3) + 1];
                    double dz = z - positions[(j * 3) + 2];
                    j++;

                    double rangeSq = (num * num) + (dy * dy) + (dz * dz);

                    if (entity.AllowOutsideLoadedRange) rangeSq = 0;

                    if (rangeSq < minRangeSq)
                    {
                        minRangeSq = rangeSq;
                    }

                    if ((isTracked || rangeSq <= trackRange) && (client2.State == EnumClientState.Connected || client2.State == EnumClientState.Playing))
                    {
                        bool trackedByClient = client2.TrackedEntities.ContainsKey(entity.EntityId);
                        bool outOfLoadedRange = !client2.DidSendChunk(entity.InChunkIndex3d) && entity.EntityId != client2.Player.Entity.EntityId && !entity.AllowOutsideLoadedRange;
                        if (!outOfLoadedRange || trackedByClient)
                        {
                            bool inRange = rangeSq < trackRange && !outOfLoadedRange;
                            if (trackedByClient || inRange)
                            {
                                if (trackedByClient && !inRange && !entity.AllowOutsideLoadedRange) // Don't despawn if allowed outside the range.
                                {
                                    client2.TrackedEntities.Remove(entity.EntityId);
                                    client2.entitiesNowOutOfRange.Add(new EntityDespawn
                                    {
                                        ForClientId = client2.Id,
                                        DespawnData = outofRangeDespawnData,
                                        Entity = entity
                                    });
                                }
                                else if (!trackedByClient && inRange && client2.TrackedEntities.Count < MagicNum.TrackedEntitiesPerClient)
                                {
                                    bool within50Blocks = rangeSq < 2500.0;
                                    client2.TrackedEntities.Add(entity.EntityId, within50Blocks);
                                    client2.entitiesNowInRange.Add(new EntityInRange
                                    {
                                        ForClientId = client2.Id,
                                        Entity = entity
                                    });
                                }
                            }
                        }
                    }
                }
                entity.IsTracked = minRangeSq < trackRange ? (byte)((minRangeSq >= 2500.0) ? 1 : 2) : (byte)0;
            }

            using FastMemoryStream ms = new();
            foreach (ConnectedClient client3 in clients.Values)
            {
                if (client3.entitiesNowInRange.Count > 0)
                {
                    List<AnimationPacket> entityAnimPackets = new();
                    foreach (EntityInRange nowInRange in client3.entitiesNowInRange)
                    {
                        if (nowInRange.Entity is EntityPlayer entityPlayer)
                        {
                            server.PlayersByUid.TryGetValue(entityPlayer.PlayerUID, out ServerPlayer? value);
                            if (value != null)
                            {
                                server.SendPacket(nowInRange.ForClientId, ((ServerWorldPlayerData)value.WorldData).ToPacketForOtherPlayers(value));
                            }
                        }
                        ms.Reset();
                        BinaryWriter writer = new(ms);
                        server.SendPacket(nowInRange.ForClientId, ServerPackets.GetFullEntityPacket(nowInRange.Entity, ms, writer));
                        entityAnimPackets.Add(new AnimationPacket(nowInRange.Entity));
                    }
                    BulkAnimationPacket bulkAnimationPacket = new()
                    {
                        Packets = entityAnimPackets.ToArray()
                    };
                    udpNetwork.ServerNetworkChannel.SendPacket(bulkAnimationPacket, new IServerPlayer[] { client3.Player });
                    client3.entitiesNowInRange.Clear();
                }
                if (client3.entitiesNowOutOfRange.Count > 0)
                {
                    server.SendPacket(client3.Id, ServerPackets.GetEntityDespawnPacket(client3.entitiesNowOutOfRange));
                    client3.entitiesNowOutOfRange.Clear();
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(GameMain), "GetEntitiesAround")]
    public class TracePatch
    {
        [HarmonyPrefix]
        public static bool GetEntities(GameMain __instance, ref Entity[] __result, Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches)
        {
            float initialHorizontalRange = horRange;
            float initialVerticalRange = vertRange;

            // Add for bigger hitboxes.
            horRange += 10f;
            vertRange += 10f;

            int chunkStartX = (int)((position.X - (double)horRange) / 32.0);
            int chunkEndX = (int)((position.X + (double)horRange) / 32.0);

            int chunkStartY = (int)((position.Y - (double)vertRange) / 32.0);
            int chunkEndY = (int)((position.Y + (double)vertRange) / 32.0);

            int chunkStartZ = (int)((position.Z - (double)horRange) / 32.0);
            int chunkEndZ = (int)((position.Z + (double)horRange) / 32.0);

            List<Entity> list = new();
            matches ??= e => true;

            for (int i = chunkStartX; i <= chunkEndX; i++)
            {
                for (int j = chunkStartY; j <= chunkEndY; j++)
                {
                    for (int k = chunkStartZ; k <= chunkEndZ; k++)
                    {
                        IWorldChunk chunk = __instance.World.BlockAccessor.GetChunk(i, j, k);
                        if (chunk == null || chunk.Entities == null) continue;

                        for (int l = 0; l < chunk.Entities.Length; l++)
                        {
                            Entity entity = chunk.Entities[l];

                            if (entity == null)
                            {
                                if (l >= chunk.EntitiesCount)
                                {
                                    break;
                                }
                            }
                            else if (entity.State != EnumEntityState.Despawned && matches(entity))
                            {
                                float xDist = (float)Math.Abs(entity.SidedPos.X - position.X) - (entity.SelectionBox.XSize / 2f);
                                float yDist = (float)Math.Abs(entity.SidedPos.Y - position.Y) - (entity.SelectionBox.YSize / 2f);
                                float zDist = (float)Math.Abs(entity.SidedPos.Z - position.Z) - (entity.SelectionBox.ZSize / 2f);

                                if (xDist < initialHorizontalRange && yDist < initialVerticalRange && zDist < initialHorizontalRange)
                                {
                                    list.Add(entity);
                                }
                            }
                        }
                    }
                }
            }

            __result = list.ToArray();

            return false;
        }

        //public static MethodBase TargetMethod()
        //{
        //    return typeof(GameMain).GetMethod("RayTraceForSelection", new Type[] { typeof(IWorldIntersectionSupplier), typeof(Ray), typeof(BlockSelection).MakeByRefType(), typeof(EntitySelection).MakeByRefType(), typeof(BlockFilter), typeof(EntityFilter) })!;
        //}

        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    List<CodeInstruction> code = new(instructions);

        //    for (int i = 0; i < code.Count; i++)
        //    {

        //        if (code[i].opcode == OpCodes.Ldarg_S && code[i + 1].opcode == OpCodes.Ldc_I4_0 && code[i + 2].opcode == OpCodes.Callvirt && code[i + 3].opcode == OpCodes.Stind_Ref)
        //        {
        //            code.Insert(i + 4, new CodeInstruction(OpCodes.Ldloc_1));
        //            code.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_R4, 10f));
        //            code.Insert(i + 6, new CodeInstruction(OpCodes.Add));
        //            code.Insert(i + 7, new CodeInstruction(OpCodes.Stloc_1));

        //            break;
        //        }
        //    }

        //    return code;
        //}
    }
}