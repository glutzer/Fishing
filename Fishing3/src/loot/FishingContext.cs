using MareLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

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
    /// 0-8_000 volume of the liquid.
    /// </summary>
    public readonly int volume;

    /// <summary>
    /// Rarity multiplier set by catch system.
    /// </summary>
    public float rarityMultiplier = 1f;

    /// <summary>
    /// Multipliers set by catch system.
    /// </summary>
    public Dictionary<string, float> tagMultipliers = new();

    public FishingContext(ICoreServerAPI sapi, BlockPos blockPos)
    {
        SystemTemporalStability systemTemporalStability = sapi.ModLoader.GetModSystem<SystemTemporalStability>();
        temporalStorm = systemTemporalStability.StormData.nowStormActive;
        moonPhase = sapi.World.Calendar.MoonPhase;

        int hourOfDay = (int)sapi.World.Calendar.HourOfDay;
        night = hourOfDay is > 18 or < 6;

        int seaLevel = Climate.Sealevel;
        seaLevelOffset = blockPos.Y - seaLevel;

        liquid = sapi.World.BlockAccessor.GetBlock(blockPos.AddCopy(0, -1, 0));

        Stopwatch sw = Stopwatch.StartNew();

        volume = CheckVolumeAtBobber(blockPos.ToVec3d(), 8000);

        Console.WriteLine($"Volume check took {sw.Elapsed.TotalMilliseconds}ms");

        // Climate.
        ClimateCondition climate = sapi.World.BlockAccessor.GetClimateAt(blockPos);

        temperature = climate.Temperature;
        precipitation = climate.Rainfall;
        humidity = climate.WorldgenRainfall;
    }

    private static int CheckVolumeAtBobber(Vec3d pos, int max)
    {
        FVec3i blockPos = new((int)pos.X, (int)(pos.Y - 0.5), (int)pos.Z);

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