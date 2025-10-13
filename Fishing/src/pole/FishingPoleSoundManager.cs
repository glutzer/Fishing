using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Fishing;

public class TrackedSound
{
    public Entity entity;
    public ILoadedSound sound;
    public string soundPath;
    public Action<float> updateCallback;

    public TrackedSound(Entity entity, ILoadedSound sound, string soundPath, Action<float> updateCallback)
    {
        this.entity = entity;
        this.sound = sound;
        this.soundPath = soundPath;
        this.updateCallback = updateCallback;
    }
}

[GameSystem(forSide = EnumAppSide.Client)]
public class FishingPoleSoundManager : GameSystem, IRenderer
{
    private readonly Dictionary<string, TrackedSound> playerSounds = [];
    public static FishingPoleSoundManager Instance { get; private set; } = null!;

    public FishingPoleSoundManager(bool isServer, ICoreAPI api) : base(isServer, api)
    {

    }

    public double RenderOrder => 1;
    public int RenderRange => 0;

    public void OnRenderFrame(float dt, EnumRenderStage stage)
    {
        foreach (TrackedSound sound in playerSounds.Values)
        {
            Vector3 pos = RenderTools.PlayerRelativePosition(sound.entity.Pos.ToVector());
            Vec3f vecPos = new(pos.X, pos.Y, pos.Z);
            sound.sound.SetPosition(vecPos);
            sound.updateCallback(dt);
        }
    }

    public override void Initialize()
    {
        Instance = this;
    }

    private void OnFirstSoundAdded()
    {
        MainAPI.Capi.Event.RegisterRenderer(this, EnumRenderStage.Before);
    }

    private void OnLastSoundRemoved()
    {
        MainAPI.Capi.Event.UnregisterRenderer(this, EnumRenderStage.Before);
    }

    /// <summary>
    /// Start a sound (like a fishing reel), takes a callback to do every frame.
    /// "fishing:sounds/reel"
    /// </summary>
    public void StartSound(EntityPlayer entity, string soundPath, Action<float> updateCallback, bool looping = true)
    {
        if (playerSounds.ContainsKey(entity.PlayerUID))
        {
            StopSound(entity);
        }

        Vector3 pos = RenderTools.PlayerRelativePosition(entity.Pos.ToVector());
        ILoadedSound sound = MainAPI.Capi.World.LoadSound(new SoundParams()
        {
            Location = new AssetLocation(soundPath),
            ShouldLoop = looping,
            Position = new Vec3f(pos.X, pos.Y, pos.Z),
            DisposeOnFinish = true,
            RelativePosition = true,
            Volume = 0.5f,
            SoundType = EnumSoundType.Entity
        });

        sound.Start();

        TrackedSound trackedSound = new(entity, sound, soundPath, updateCallback);

        playerSounds.Add(entity.PlayerUID, trackedSound);

        if (playerSounds.Count == 1)
        {
            OnFirstSoundAdded();
        }
    }

    public void StopSound(EntityPlayer entity)
    {
        if (playerSounds.TryGetValue(entity.PlayerUID, out TrackedSound? sound))
        {
            sound.sound.Stop();
            playerSounds.Remove(entity.PlayerUID);
            if (playerSounds.Count == 0)
            {
                OnLastSoundRemoved();
            }
        }
    }

    public void UpdatePitchVolume(EntityPlayer player, float pitch, float volume)
    {
        if (playerSounds.TryGetValue(player.PlayerUID, out TrackedSound? sound))
        {
            sound.sound.SetPitch(pitch);
            sound.sound.SetVolume(volume);
        }
    }

    public override void OnClose()
    {
        Instance = null!;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}