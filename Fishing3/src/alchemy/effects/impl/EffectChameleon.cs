using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

[Effect]
public class EffectChameleon : AlchemyEffect
{
    public override float BaseDuration => 30f;
    public override EffectType Type => EffectType.Duration;
    private long distortionId;
    private OverlayRenderInfo? deferredInfo;

    public override void OnLoaded()
    {
        if (IsServer)
        {
            Entity.Stats.Set("animalSeekingRange", "chameleon", -1000, true);

            EffectBehavior.onDamaging += OnDamaging;
        }
        else
        {
            EntityOverlaySystem.Instance?.Register(Entity, (OnRender, 10), true);

            if (MainAPI.TryGetGameSystem(EnumAppSide.Client, out DistortionSystem? distortionSystem))
            {
                distortionId = distortionSystem.RegisterAnimatedRenderer(OnDistortion);
            }
        }
    }

    private void OnDamaging(ref float damage, DamageSource source, Entity toEntity)
    {
        Entity.Stats.Remove("animalSeekingRange", "chameleon");
    }

    public override void OnUnloaded()
    {
        if (IsServer)
        {
            Entity.Stats.Remove("animalSeekingRange", "chameleon");

            EffectBehavior.onDamaging -= OnDamaging;
        }
        else
        {
            EntityOverlaySystem.Instance?.Unregister(Entity, OnRender, true);

            if (MainAPI.TryGetGameSystem(EnumAppSide.Client, out DistortionSystem? distortionSystem))
            {
                distortionSystem.UnregisterAnimatedRenderer(distortionId);
            }
        }
    }

    public void OnDistortion(float dt, MareShader shader)
    {
        if (deferredInfo == null || deferredInfo.mesh.Disposed) return;

        UBORef? ubo = Patches.RendererPatch.AnimationUbo;
        if (ubo == null) return;

        shader.Uniform("addRenderFlags", deferredInfo.renderFlags);
        shader.Uniform("modelMatrix", deferredInfo.modelMatrix);

        ubo.Update(deferredInfo.entityAnimator.Matrices, 0, deferredInfo.entityAnimator.MaxJointId * 16 * 4);

        for (int i = 0; i < deferredInfo.mesh.meshrefs.Length; i++)
        {
            MeshRef vao = deferredInfo.mesh.meshrefs[i];
            MainAPI.Capi.Render.RenderMesh(vao);
        }

        deferredInfo = null;
    }

    public void OnRender(OverlayRenderInfo renderInfo)
    {
        deferredInfo = renderInfo;
    }
}