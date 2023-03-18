﻿using HarmonyLib;
using OWML.Logging;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using static HugMod.HugMod;
using static HugMod.PlayerHugController;

namespace HugMod
{
    [HarmonyPatch]
    public class HugPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(HugPatches)).Patch(); }


        [HarmonyTranspiler] //simulate player input for walking towards hug target
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateMovement))]
        public static IEnumerable<CodeInstruction> PlayerCharacterController_UpdateMovement_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions).MatchForward(false, 
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(OWInput), "OWInput.GetAxisValue", new Type[] { typeof(IInputCommands), typeof(InputMode) })),
                new CodeMatch(OpCodes.Stloc_0)
            ).Advance(2);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldloc_0),
                Transpilers.EmitDelegate<Func<Vector2, Vector2>>((input) =>
                {
                    if (WalkingTowardsHugTarget) return new Vector2(0, WalkingTowardsHugSpeed);
                    return input;
                }),
                new CodeInstruction(OpCodes.Stloc_0)
            );

            return matcher.InstructionEnumeration();
        }


        [HarmonyPrefix] //yoinked from NH, thank you JohnCorby!
        [HarmonyPatch(typeof(UnityLogger), "OnLogMessageReceived")]
        public static bool UnityLogger_OnLogMessageReceived(string message)
        {
            // Filter out goofy error that doesn't actually break anything
            return !message.EndsWith(") is out of bounds (size=0)");
        }


        //ONLY FOR DEBUG
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.CheckForCompletionAchievement))]
        public static bool ShipLogManager_CheckForCompletionAchievement_Prefix()
        {
            return false;
        }
    }
}
