using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using static HugMod.HugMod;
using static HugMod.Targets.TargetManager;

namespace HugMod.Targets
{
    [HarmonyPatch]
    public class GabbroPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(GabbroPatches)).Patch(); }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(TornadoDialogueHandler), nameof(TornadoDialogueHandler.OnEnterTornado))]
        public static void TornadoDialogueHandler_OnEnterTornado_Postfix()
        {
            GabbroHug.Exists()?.CancelHugSequence();
        }

        [HarmonyPrefix] //keep animators from crossfading when they shouldn't
        [HarmonyPatch(typeof(GabbroTravelerController), nameof(GabbroTravelerController.StartConversation))]
        public static bool GabbroTravelerController_StartConversation_Prefix()
        {
            if (GabbroHug == null || !GabbroHug.IsSequenceInProgress()) return true;
            Locator.GetTravelerAudioManager().StopAllTravelerAudio();
            return false;
        }
    }


    [HarmonyPatch]
    public class SolanumPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(SolanumPatches)).Patch(); }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.PlayRaiseCairns))]
        public static void SolanumAnimController_PlayRaiseCairns_Postfix()
        {
            SolanumHug.Exists()?.SetHugEnabled(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.Audio_ExitRaiseCairn))]
        public static void SolanumAnimController_Audio_ExitRaiseCairn_Postfix()
        {
            SolanumHug.Exists()?.SetHugEnabled(true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.StartWritingMessage))]
        public static void SolanumAnimController_StartWritingMessage_Postfix()
        {
            SolanumHug.Exists()?.SetHugEnabled(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.StopWritingMessage))]
        public static void SolanumAnimController_StopWritingMessage_Postfix()
        {
            SolanumHug.Exists()?.SetHugEnabled(true);
        }

        [HarmonyTranspiler] //make IK work without required parameters
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.OnAnimatorIK))]
        public static IEnumerable<CodeInstruction> SolanumAnimController_OnAnimatorIK_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator).MatchForward(false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SolanumAnimController), nameof(SolanumAnimController._animator))),
                new CodeMatch(OpCodes.Ldstr),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Animator), nameof(Animator.GetFloat), new Type[] { typeof(string) }))
            );

            matcher.Repeat(matcher => {
                matcher.CreateLabel(out Label doGetFloat);
                matcher.CreateLabelAt(matcher.Pos + 4, out Label skipGetFloat);

                matcher.InsertAndAdvance( //move pointer past inserted code
                    Transpilers.EmitDelegate(() => { return SolanumHug != null && SolanumHug.IsSequenceInProgress(); }),
                    new CodeInstruction(OpCodes.Brfalse, doGetFloat),
                    new CodeInstruction(OpCodes.Ldc_R4, 1f),
                    new CodeInstruction(OpCodes.Br, skipGetFloat)
                ).Advance(4); //move pointer past matched code, to not match with the same block again on repeat
            });

            return matcher.InstructionEnumeration();
        }

        [HarmonyTranspiler] //remove attempts to get AnimatorStateEvents during hug sequence to avoid NREs
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.LateUpdate))]
        public static IEnumerable<CodeInstruction> SolanumAnimController_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator).MatchForward(false, new CodeMatch(
                OpCodes.Callvirt, AccessTools.Method(typeof(AnimatorStateEvents), "AnimatorStateEvents.add_OnEnterStateEvents", new Type[] { typeof(AnimatorStateEvents.StateEvent) })
            )).Advance(1);

            matcher.CreateLabel(out Label skipGetAnimatorStateEvents);

            matcher.MatchBack(true, 
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SolanumAnimController), nameof(SolanumAnimController._animatorStateEvents))),
                new CodeMatch(OpCodes.Ldnull),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "Object.op_Equality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) }))
            ).Insert(
                Transpilers.EmitDelegate(() => { return SolanumHug != null && SolanumHug.IsSequenceInProgress(); }),
                new CodeInstruction(OpCodes.Brtrue, skipGetAnimatorStateEvents)
            );

            return matcher.InstructionEnumeration();
        }

        [HarmonyPrefix] //freeze processes during hug sequence to prevent unaccounted for state alterations
        [HarmonyPatch(typeof(NomaiConversationManager), nameof(NomaiConversationManager.Update))]
        public static bool NomaiConversationManager_Update_Prefix()
        {
            return (SolanumHug == null || !SolanumHug.IsSequenceInProgress());
        }
    }


    [HarmonyPatch]
    public class PrisonerPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(PrisonerPatches)).Patch(); }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PrisonerBrain), nameof(PrisonerBrain.OnArriveAtPosition))]
        public static bool PrisonerBrain_OnArriveAtPosition_Prefix(PrisonerBrain __instance)
        {
            if (PrisonerHug == null || __instance._currentBehavior != PrisonerBehavior.MoveToElevatorDoor) return true;

            PrisonerHug.SetHugEnabled(false);
            if (PrisonerHug.IsSequenceInProgress()) PrisonerHug.OnHugFinish += DelayedArrival;
            return !PrisonerHug.IsSequenceInProgress();

            void DelayedArrival() //does the things that would normally happen the moment the Prisoner reached the elevator
            {
                PrisonerHug.OnHugFinish -= DelayedArrival;
                __instance._controller.StopFacing();
                __instance._effects.Play180TurnAnimation();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PrisonerDirector), nameof(PrisonerDirector.OnPrisonerReadyToReceiveTorch))]
        public static void PrisonerDirector_OnPrisonerReadyToReceiveTorch_Postfix()
        {
            PrisonerHug.Exists()?.SetHugEnabled(true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PrisonerEffects), nameof(PrisonerEffects.PlayFarewellBowAnimation))]
        public static void PrisonerEffects_PlayFarewellBowAnimation_Postfix()
        {
            PrisonerHug.Exists()?.SetHugEnabled(false);
        }

        [HarmonyPrefix] //don't unpause movement during hugs, pt.1
        [HarmonyPatch(typeof(PrisonerBrain), nameof(PrisonerBrain.FixedUpdate))]
        public static void PrisonerBrain_FixedUpdate_Prefix(PrisonerBrain __instance, out bool __state)
        {
            __state = __instance._controller._movementPaused;
        }

        [HarmonyPostfix] //don't unpause movement during hugs, pt.2
        [HarmonyPatch(typeof(PrisonerBrain), nameof(PrisonerBrain.FixedUpdate))]
        public static void PrisonerBrain_FixedUpdate_Postfix(PrisonerBrain __instance, bool __state)
        {
            if (PrisonerHug != null && PrisonerHug.IsSequenceInProgress()) __instance._controller.SetMovementPaused(__state);
        }
    }


    [HarmonyPatch]
    public class GhostPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(GhostPatches)).Patch(); }


        [HarmonyPrefix] //prevent owls from rotating when they shouldn't
        [HarmonyPatch(typeof(GhostController), nameof(GhostController.TurnTowardsLocalDirection))]
        public static bool GhostController_TurnTowardsLocalDirection_Prefix(GhostController __instance)
        {
            return !__instance._movementPaused;
        }

        [HarmonyPostfix] //prevent the turning animation from playing when it shouldn't
        [HarmonyPatch(typeof(GhostEffects), nameof(GhostEffects.Update_Effects))]
        public static void GhostEffects_Update_Effects_Postfix(GhostEffects __instance)
        {
            if (__instance._controller._movementPaused) __instance._animator.SetFloat(GhostEffects.AnimatorKeys.Float_TurnSpeed, 0);
        }
    }
}
