using Vintagestory.API.Common;

namespace Fishing3.src.regions;

public delegate bool ValidCondition(FishingConditions condition);

public class FishingRequirement
{
    public string code;
    public ValidCondition spawnDelegate;

    public FishingRequirement(string code, ValidCondition spawnDelegate)
    {
        this.code = code;
        this.spawnDelegate = spawnDelegate;
    }
}

// All fishing conditions in area.
public class FishingConditions
{
    /// <summary>
    /// Block 1 below the bobber.
    /// </summary>
    public Block? liquidBlock;

    /// <summary>
    /// Rain from 0-1.
    /// </summary>
    public float rain;

    /// <summary>
    /// Temperature from 0-1.
    /// </summary>
    public float temperature;

    /// <summary>
    /// Height from 0-1, mantle to limit.
    /// </summary>
    public float height;

    /// <summary>
    /// Is a storm active at the moment?
    /// </summary>
    public bool isTemporalStorm;

    /// <summary>
    /// Moon phase.
    /// </summary>
    public EnumMoonPhase moonPhase;

    /// <summary>
    /// Is night time?
    /// </summary>
    public bool isNight;
}