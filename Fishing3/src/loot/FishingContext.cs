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

        // Climate.
        ClimateCondition climate = sapi.World.BlockAccessor.GetClimateAt(blockPos);

        temperature = climate.Temperature;
        precipitation = climate.Rainfall;
        humidity = climate.WorldgenRainfall;
    }
}