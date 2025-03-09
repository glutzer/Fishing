using HarmonyLib;
using Vintagestory.API.Common;

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
                .Register("reelStrength");
        }
    }
}