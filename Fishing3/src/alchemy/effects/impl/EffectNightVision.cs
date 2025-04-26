using MareLib;
using Newtonsoft.Json;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Fishing3;

[Effect]
public class EffectNightVision : AlchemyEffect, IRenderer
{
    public double RenderOrder => 1;
    public int RenderRange => 1;
    public override EffectType Type => EffectType.Duration;

    public AmbientModifier darkMod = null!;
    public AmbientModifier lightMod = null!;
    public AmbientModifier lerpedMod = null!;

    [JsonProperty]
    public float totalDuration;

    public override float BaseDuration => 120f;

    public override void ApplyInstantEffect()
    {
        totalDuration = Duration;
    }

    public override void OnLoaded()
    {
        if (IsServer || !Entity.IsSelf()) return;

        // Set ambience to day.
        AmbientManager ambient = (AmbientManager)MainAPI.Capi.Ambient;

        darkMod = new()
        {
            SceneBrightness = new WeightedFloat(2f, 0f),
            AmbientColor = new WeightedFloatArray(new float[] { 0.4f, 0.4f, 1f }, 0.8f)
        };
        darkMod.EnsurePopulated();

        lightMod = new()
        {
            SceneBrightness = new WeightedFloat(2f, StrengthMultiplier * 0.5f),
            AmbientColor = new WeightedFloatArray(new float[] { 0.4f, 0.4f, 1f }, 0f),

        };
        lightMod.EnsurePopulated();

        lerpedMod = new()
        {
            SceneBrightness = new WeightedFloat(2f, 0f),
            AmbientColor = new WeightedFloatArray(new float[] { 0.4f, 0.4f, 1f }, 0.8f)
        };
        lerpedMod.EnsurePopulated();

        ambient.CurrentModifiers.Add("darkvision", lerpedMod);

        MainAPI.Capi.Event.RegisterRenderer(this, EnumRenderStage.Before);
        OnRenderFrame(0, EnumRenderStage.Before);
    }

    public void OnRenderFrame(float dt, EnumRenderStage stage)
    {
        float lerp = 1;

        if (Duration < 1)
        {
            lerp = Math.Clamp(Duration, 0, 1);
        }
        else if (Duration > totalDuration - 1)
        {
            lerp = Math.Clamp(totalDuration - Duration, 0, 1);
        }

        lerpedMod.SetLerped(darkMod, lightMod, lerp);
    }

    public override void OnUnloaded()
    {
        if (IsServer || !Entity.IsSelf()) return;

        // Set ambience to day.
        AmbientManager ambient = (AmbientManager)MainAPI.Capi.Ambient;

        ambient.CurrentModifiers.Remove("darkvision");

        MainAPI.Capi.Event.UnregisterRenderer(this, EnumRenderStage.Before);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}