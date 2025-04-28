using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Fishing3;

/// <summary>
/// Context for the current catch.
/// Has all rarity/weather modifiers.
/// Only exists on the server.
/// </summary>
public class FishingContext
{
    /// <summary>
    /// Liquid being fished.
    /// </summary>
    public readonly Block liquid;

    /// <summary>
    /// Is the bobber in a river?
    /// </summary>
    public readonly bool isRiver;

    /// <summary>
    /// Current precipitation from 0-1
    /// </summary>
    public readonly float precipitation;

    /// <summary>
    /// Current degrees in celsius at the bobber.
    /// </summary>
    public readonly float temperature;

    /// <summary>
    /// World gen humidity from 0-1.
    /// </summary>
    public readonly float humidity;

    /// <summary>
    /// How many blocks above or below sea level.
    /// </summary>
    public readonly int seaLevelOffset;

    /// <summary>
    /// Is a temporal storm occurring right now?
    /// </summary>
    public readonly bool temporalStorm;

    /// <summary>
    /// Current moon phase.
    /// </summary>
    public readonly EnumMoonPhase moonPhase;

    /// <summary>
    /// Is it night?
    /// </summary>
    public readonly bool night;

    /// <summary>
    /// Volume of the liquid in blocks.
    /// </summary>
    public readonly int volume;

    public float RarityMultiplier { get; private set; } = 1f;
    public float QuantityMultiplier { get; private set; } = 1f;

    /// <summary>
    /// Multipliers set by catch system.
    /// </summary>
    public Dictionary<string, float> tagMultipliers = new();

    public FishingContext(ICoreServerAPI sapi, Vector3d position, Entity? caster)
    {
        BlockPos blockPos = new((int)position.X, (int)(position.Y - 0.5), (int)position.Z);

        SystemTemporalStability systemTemporalStability = sapi.ModLoader.GetModSystem<SystemTemporalStability>();
        temporalStorm = systemTemporalStability.StormData.nowStormActive;
        moonPhase = sapi.World.Calendar.MoonPhase;

        int hourOfDay = (int)sapi.World.Calendar.HourOfDay;
        night = hourOfDay is > 18 or < 6;

        int seaLevel = Climate.Sealevel;
        seaLevelOffset = blockPos.Y - seaLevel;

        // Liquid 1-2 below bobber. Does not roll anything in shallow water.
        liquid = sapi.World.BlockAccessor.GetBlock(blockPos.AddCopy(0, 0, 0));

        // Climate.
        ClimateCondition climate = sapi.World.BlockAccessor.GetClimateAt(blockPos);

        temperature = climate.Temperature;
        precipitation = climate.Rainfall;
        humidity = climate.WorldgenRainfall;

        // Check if on a river.
        ServerChunk chunk = (ServerChunk)sapi.World.BlockAccessor.GetChunk(blockPos.X / 32, 0, blockPos.Z / 32);
        float[]? flowVectors = chunk.GetModdata<float[]>("flowVectors");
        if (flowVectors != null && flowVectors.Length == 2048)
        {
            int localX = blockPos.X % 32;
            int localZ = blockPos.Z % 32;

            int chunkIndex = ChunkMath.ChunkIndex2d(localX, localZ);

            float xFlowVector = flowVectors[chunkIndex];
            float zFlowVector = flowVectors[chunkIndex + 1024];

            isRiver = xFlowVector != 0 || zFlowVector != 0;
        }

        // Volume.
        bool isLava = liquid.Code.FirstCodePart() == "lava";
        volume = CheckVolumeAtBobber(blockPos.ToVec3d(), isLava ? 800 : 8000); // 10x less lava needed.

        CalculateRarityQuantity(caster, isLava ? 200f : 2000f);
    }

    private void CalculateRarityQuantity(Entity? caster, float liquidBaseLine)
    {
        RarityMultiplier = DRUtility.CalculateDR(volume, liquidBaseLine, 1.7f) / liquidBaseLine;
        QuantityMultiplier = DRUtility.CalculateDR(volume, liquidBaseLine, 1.7f) / liquidBaseLine;

        // 30% more quantity in stormy weather.
        QuantityMultiplier *= 1f + (precipitation * 0.3f);

        // 50% rarity and quantity multiplier in negative weather.
        float tempMultiplier = 1f + (Math.Clamp(-temperature, 0, 20) / 20f * 0.5f);
        RarityMultiplier *= tempMultiplier;
        QuantityMultiplier *= tempMultiplier;

        if (caster is EntityPlayer player)
        {
            RarityMultiplier *= caster.Stats.GetBlended("fishRarity");
            QuantityMultiplier *= caster.Stats.GetBlended("fishQuantity");

            ItemSlot hotbarSlot = player.Player.InventoryManager.ActiveHotbarSlot;

            if (hotbarSlot.Itemstack != null && hotbarSlot.Itemstack.Collectible is ItemFishingPole)
            {
                if (ItemFishingPole.ReadStack(2, hotbarSlot.Itemstack, MainAPI.Sapi, out ItemStack? baitStack))
                {
                    CollectibleBehaviorBait? baitBehavior = baitStack.Collectible.GetBehavior<CollectibleBehaviorBait>();

                    if (baitBehavior != null)
                    {
                        RarityMultiplier *= baitBehavior.rarityMultiplier;
                        QuantityMultiplier *= baitBehavior.quantityMultiplier;

                        // Flat bonuses.
                        RarityMultiplier += baitBehavior.extraRarity;
                        QuantityMultiplier += baitBehavior.extraQuantity;
                    }
                }
                else
                {
                    // No bait.
                    QuantityMultiplier *= 0.2f;
                    RarityMultiplier *= 0.2f;
                }
            }
        }
    }

    private static int CheckVolumeAtBobber(Vec3d pos, int max)
    {
        FVec3i blockPos = new((int)pos.X, (int)pos.Y, (int)pos.Z);

        int blockId = MainAPI.Sapi.World.BlockAccessor.GetBlock(blockPos.X, blockPos.Y, blockPos.Z, BlockLayersAccess.Fluid)?.BlockId ?? 0;
        if (blockId == 0) return 0;

        int volume = 0;
        HashSet<FVec3i> visited = new();
        Queue<FVec3i> toVisit = new();

        toVisit.Enqueue(blockPos);
        while (toVisit.Count > 0)
        {
            if (volume >= max) return volume;
            RecursiveFlood(toVisit.Dequeue(), visited, toVisit, blockId, ref volume);
        }

        return volume;
    }

    private static void RecursiveFlood(FVec3i current, HashSet<FVec3i> visited, Queue<FVec3i> toVisit, int waterId, ref int volume)
    {
        if (visited.Contains(current)) return;
        // Get block id at current.
        int blockId = MainAPI.Sapi.World.BlockAccessor.GetBlock(current.X, current.Y, current.Z, BlockLayersAccess.Fluid)?.BlockId ?? 0;
        visited.Add(current);
        if (blockId != waterId) return;

        volume++;

        for (int i = 0; i < 6; i++)
        {
            FVec3i next = current.GetFaceOffset(i);
            toVisit.Enqueue(next);
        }
    }
}

public struct FVec3i : IEquatable<FVec3i>
{
    public int X;
    public int Y;
    public int Z;

    public FVec3i(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Set(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public readonly FVec3i GetFaceOffset(int face)
    {
        return face switch
        {
            0 => new FVec3i(X, Y - 1, Z),
            1 => new FVec3i(X, Y + 1, Z),
            2 => new FVec3i(X, Y, Z - 1),
            3 => new FVec3i(X, Y, Z + 1),
            4 => new FVec3i(X - 1, Y, Z),
            5 => new FVec3i(X + 1, Y, Z),
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }

    public readonly bool Equals(FVec3i other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is FVec3i other && Equals(other);
    }

    public static bool operator ==(FVec3i left, FVec3i right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FVec3i left, FVec3i right)
    {
        return !left.Equals(right);
    }
}