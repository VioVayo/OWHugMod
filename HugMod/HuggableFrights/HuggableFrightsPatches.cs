﻿using HarmonyLib;
using static HugMod.HugMod;
using static HugMod.HuggableFrights.HuggableFrightsMain;

namespace HugMod.HuggableFrights
{
    [HarmonyPatch]
    public class HuggableFrightsPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(HuggableFrightsPatches)).Patch(); }


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
        [HarmonyPatch(typeof(GhostBrain), nameof(GhostBrain.TabulaRasa))]
        public static void GhostBrain_TabulaRasa_Postfix(GhostBrain __instance)
        {
            var current = __instance.GetCurrentActionName();
            if (current == HuggedActionName || current == WitnessedHugActionName) __instance.ChangeAction(null);
        }

        //the following two patched methods are analogues that both kick the player out of the DW, but only one is subscribed directly to an event, the other uses a timer
        //if there was events for both I would subscribe this change to those the same way disabling the HugComponent is to the grab, but alas, we're patching them both instead for consistency
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GhostGrabController), nameof(GhostGrabController.OnSnapPlayerNeck))]
        public static void GhostGrabController_OnSnapPlayerNeck_Postfix(GhostGrabController __instance)
        {
            if (Settings.FriendmakerModeEnabled) __instance._origParent.gameObject.GetComponent<HugComponent>().SetHugEnabled(true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GhostGrabController), nameof(GhostGrabController.CompleteExtinguish))]
        public static void GhostGrabController_CompleteExtinguish_Postfix(GhostGrabController __instance)
        {
            if (Settings.FriendmakerModeEnabled) __instance._origParent.gameObject.GetComponent<HugComponent>().SetHugEnabled(true);
        }
    }
}
