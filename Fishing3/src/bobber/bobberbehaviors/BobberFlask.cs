using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Fishing;

[Bobber]
public class BobberFlask : BobberReelable
{
    private static readonly SimpleParticleProperties splashParticles;
    static BobberFlask()
    {
        splashParticles = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinVelocity = new Vec3f(-10f, -10f, -10f),
            AddVelocity = new Vec3f(20f, 20f, 20f),
            MinQuantity = 10f,
            AddQuantity = 20f,
            GravityEffect = 4f,
            SelfPropelled = false,
            MinSize = 0.125f,
            MaxSize = 0.5f,
            Color = ColorUtil.ToRgba(100, 200, 0, 0),
            OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, -20),
            LifeLength = 1f,
            addLifeLength = 1f
        };
    }

    public BobberFlask(EntityBobber bobber, bool isServer) : base(bobber, isServer)
    {
    }

    protected void EmitFlaskParticles(FluidContainer container)
    {
        if (container.HeldStack == null) return;

        splashParticles.MinPos = bobber.ServerPos.XYZ;

        float particleCount = container.HeldStack.Units / 100f;

        splashParticles.MinQuantity = particleCount;
        splashParticles.AddQuantity = particleCount;

        Vector4 color = container.HeldStack.fluid.GetColor(container.HeldStack);
        float glow = container.HeldStack.fluid.GetGlowLevel(container.HeldStack);

        for (int i = 0; i < 5; i++)
        {
            float brightness = 0.5f + (Random.Shared.NextSingle() * 0.5f);
            splashParticles.Color = ColorUtil.ToRgba((int)(color.W * 255), (int)(color.X * 255 * brightness), (int)(color.Y * 255 * brightness), (int)(color.Z * 255 * brightness));
            splashParticles.VertexFlags = (int)(glow * 255 * brightness);
            bobber.Api.World.SpawnParticles(splashParticles);
        }
    }

    public override void OnCollided()
    {
        if (!isServer) return; // Does this even get called on client?

        EntityPlayer? caster = bobber.Caster;
        ItemSlot? rodSlot = bobber.rodSlot;
        if (caster == null || rodSlot == null) return;

        bobber.Die();
        MainAPI.Sapi.World.PlaySoundAt("sounds/block/glass", bobber, null, true, 32);

        ItemFishingPole.ReadStack(1, rodSlot.Itemstack, MainAPI.Sapi, out ItemStack? bobberStack);
        if (bobberStack == null || bobberStack.Collectible is not ItemFlask flask) return;

        FluidContainer cont = flask.GetContainer(bobberStack);

        EmitFlaskParticles(cont);

        // Get entities within 5m radius of bobber.
        Entity[] entities = MainAPI.Sapi.World.GetEntitiesAround(bobber.ServerPos.XYZ, 4, 4, (e) => e.HasBehavior<EntityBehaviorEffects>());

        foreach (Entity entity in entities)
        {
            // Apply at halved effect.
            AlchemyEffectSystem.ApplyFluid(cont.Copy(EnumAppSide.Server), int.MaxValue, caster, entity, ApplicationMethod.Skin, 0.5f);
        }

        EmitFlaskParticles(cont);

        // Remove flask.
        ItemFishingPole.SetStack(1, rodSlot.Itemstack, null);
        rodSlot.MarkDirty();
    }
}