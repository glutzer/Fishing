using MareLib;
using OpenTK.Mathematics;
using ProtoBuf;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Fishing3;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FishingRegion
{
    public GridPos position;
    public float maxPopulation; // Catching a fish drains 1 population point.
    public float currentPopulation;
    public float rarity; // Rarity multiplier.
    public float growth; // Fish size multiplier.

    public FishingRegion(GridPos position, float maxPopulation, float rarity, float growth)
    {
        this.position = position;
        this.maxPopulation = maxPopulation;
        currentPopulation = maxPopulation;
        this.rarity = rarity;
        this.growth = growth;
    }

    public FishingRegion()
    {

    }

    /// <summary>
    /// Update region, return if fully regenerated and should be removed.
    /// </summary>
    public bool Update(float dt)
    {
        float amountToRegen = dt / 7200; // One hour to fully regen.
        currentPopulation += maxPopulation * amountToRegen;
        if (currentPopulation > maxPopulation) return true;
        return false;
    }

    /// <summary>
    /// Drains, returns amount left.
    /// </summary>
    public float Drain(float amount)
    {
        float amountLeft = Math.Max(0, currentPopulation - amount);
        currentPopulation -= amount;
        if (currentPopulation < 0) currentPopulation = 0;
        return amountLeft;
    }
}

/// <summary>
/// Holds regenerating fishing regions.
/// </summary>
[GameSystem]
public class FishingRegionSystem : NetworkedGameSystem
{
    public const int REGION_SIZE = 256;

    private readonly Dictionary<GridPos, FishingRegion> activeRegions = new();
    private readonly List<GridPos> regionsToRemove = new();

    public FishingRegionSystem(bool isServer, ICoreAPI api) : base(isServer, api, "fishregions")
    {
    }

    public override void Initialize()
    {
        if (isServer)
        {
            MainAPI.Sapi.Event.RegisterGameTickListener(TickRegions, 5000);
        }
    }

    protected override void RegisterClientMessages(IClientNetworkChannel channel)
    {

    }

    protected override void RegisterServerMessages(IServerNetworkChannel channel)
    {

    }

    /// <summary>
    /// Returns a new fishing region with lerped values.
    /// </summary>
    public FishingRegion GetLerpedRegion(Vector3d position)
    {
        FishingRegion topLeft = GetOrGenerateRegion((int)(position.X / REGION_SIZE), (int)(position.Z / REGION_SIZE));
        FishingRegion topRight = GetOrGenerateRegion((int)(position.X / REGION_SIZE) + 1, (int)(position.Z / REGION_SIZE));
        FishingRegion bottomLeft = GetOrGenerateRegion((int)(position.X / REGION_SIZE), (int)(position.Z / REGION_SIZE) + 1);
        FishingRegion bottomRight = GetOrGenerateRegion((int)(position.X / REGION_SIZE) + 1, (int)(position.Z / REGION_SIZE) + 1);

        double xLerp = position.X % REGION_SIZE / REGION_SIZE;
        double zLerp = position.Z % REGION_SIZE / REGION_SIZE;

        float rarity = GameMath.BiLerp(topLeft.rarity, topRight.rarity, bottomLeft.rarity, bottomRight.rarity, (float)xLerp, (float)zLerp);
        float growth = GameMath.BiLerp(topLeft.growth, topRight.growth, bottomLeft.growth, bottomRight.growth, (float)xLerp, (float)zLerp);
        float maxPopulation = GameMath.BiLerp(topLeft.maxPopulation, topRight.maxPopulation, bottomLeft.maxPopulation, bottomRight.maxPopulation, (float)xLerp, (float)zLerp);
        float currentPopulation = GameMath.BiLerp(topLeft.currentPopulation, topRight.currentPopulation, bottomRight.currentPopulation, bottomRight.currentPopulation, (float)xLerp, (float)zLerp);

        return new FishingRegion(new GridPos((int)(position.X / REGION_SIZE), 0, (int)(position.Z / REGION_SIZE)), maxPopulation, rarity, growth)
        {
            currentPopulation = currentPopulation
        };
    }

    /// <summary>
    /// Drain 1 fish.
    /// </summary>
    public void DrainFromPosition(Vector3d position)
    {
        FishingRegion topLeft = GetOrGenerateRegion((int)(position.X / REGION_SIZE), (int)(position.Z / REGION_SIZE));
        FishingRegion topRight = GetOrGenerateRegion((int)(position.X / REGION_SIZE) + 1, (int)(position.Z / REGION_SIZE));
        FishingRegion bottomLeft = GetOrGenerateRegion((int)(position.X / REGION_SIZE), (int)(position.Z / REGION_SIZE) + 1);
        FishingRegion bottomRight = GetOrGenerateRegion((int)(position.X / REGION_SIZE) + 1, (int)(position.Z / REGION_SIZE) + 1);

        double xLerp = position.X % REGION_SIZE / REGION_SIZE;
        double zLerp = position.Z % REGION_SIZE / REGION_SIZE;

        // Drain from each based on distance.
        topLeft.Drain((0.5f * (1f - (float)xLerp)) + (0.5f * (1f - (float)zLerp)));
        topRight.Drain((0.5f * (float)xLerp) + (0.5f * (1f - (float)zLerp)));
        bottomLeft.Drain((0.5f * (1f - (float)xLerp)) + (0.5f * (float)zLerp));
        bottomRight.Drain((0.5f * (float)xLerp) + (0.5f * (float)zLerp));
    }

    private FishingRegion GetOrGenerateRegion(int regionX, int regionZ)
    {
        GridPos gPos = new(regionX, 0, regionZ);
        if (activeRegions.TryGetValue(gPos, out FishingRegion? region))
        {
            return region;
        }

        MainAPI.Capi.World.BlockAccessor.GetMapRegion(regionX, regionZ);
        Vector3d pos = new(regionX * REGION_SIZE, Climate.Sealevel, regionZ * REGION_SIZE);

        ClimateCondition climate = api.World.BlockAccessor.GetClimateAt(new BlockPos((int)pos.X, (int)pos.Y, (int)pos.Z));

        float maxPopulation = 100f;
        float rarity = 1f;
        float growth = 1f;

        if (climate.WorldGenTemperature < 0)
        {
            // Large bonus to freezing areas.
            float growthMultiplier = GameMath.Lerp(0f, 3f, -climate.Temperature / 20f);
            growth *= growthMultiplier;
            maxPopulation *= growthMultiplier;
        }

        rarity *= 1f + (1f - climate.WorldgenRainfall);
        growth *= 1f + climate.WorldgenRainfall;

        Noise noise = new(0, 0.002f, 2);

        rarity *= noise.GetPosNoise(pos.X, pos.Z);
        growth *= noise.GetPosNoise(-pos.X, -pos.Z);

        region = new(new GridPos(regionX, 0, regionZ), maxPopulation, rarity, growth);

        return region;
    }

    private void TickRegions(float dt)
    {
        foreach (FishingRegion region in activeRegions.Values)
        {
            if (region.Update(dt))
            {
                regionsToRemove.Add(region.position);
            }
        }

        if (regionsToRemove.Count > 0)
        {
            foreach (GridPos pos in regionsToRemove)
            {
                activeRegions.Remove(pos);
            }
            regionsToRemove.Clear();
        }
    }
}