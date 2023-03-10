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
            if (name != HuggedActionName && name != WitnessedHugActionName && name != ReturnActionName) return true;

            if (name == HuggedActionName) __result = new HuggedAction();
            else if (name == WitnessedHugActionName) __result = new WitnessedHugAction();
            else __result = new ReturnAction();

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
