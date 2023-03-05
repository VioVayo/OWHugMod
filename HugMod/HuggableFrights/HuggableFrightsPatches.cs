using HarmonyLib;
using static HugMod.HuggableFrights.HuggableFrightsMain;

namespace HugMod.HuggableFrights
{
    [HarmonyPatch]
    public class HuggableFrightsPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GhostAction), nameof(GhostAction.CreateAction))]
        public static bool GhostAction_CreateAction_Prefix(GhostAction.Name name, ref GhostAction __result)
        {
            if (name != HuggedActionName) return true;
            __result = new HuggedAction();
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GhostGrabController), nameof(GhostGrabController.OnSnapPlayerNeck))]
        public static void GhostGrabController_OnSnapPlayerNeck_Postfix(GhostGrabController __instance)
        {
            if (IsHuggableFrightsEnabled) __instance._origParent.gameObject.GetComponent<HugComponent>().SetHugEnabled(true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GhostGrabController), nameof(GhostGrabController.CompleteExtinguish))]
        public static void GhostGrabController_CompleteExtinguish_Postfix(GhostGrabController __instance)
        {
            if (IsHuggableFrightsEnabled) __instance._origParent.gameObject.GetComponent<HugComponent>().SetHugEnabled(true);
        }
    }
}
