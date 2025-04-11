using HarmonyLib;
using MareLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

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
            if (EntityOverlaySystem.EntityHasRenderingDisabled(__instance.entity))
            {
                return false;
            }

            return true;
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
}