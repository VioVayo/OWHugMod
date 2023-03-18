using HarmonyLib;
using UnityEngine;
using static HugMod.HugMod;
using static HugMod.Targets.TargetManager;

namespace HugMod.Targets
{
    [HarmonyPatch]
    public class GabbroPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(GabbroPatches)).Patch(); }


        [HarmonyPrefix] //keep hammock from receiving triggers it shouldn't
        [HarmonyPatch(typeof(GabbroTravelerController), nameof(GabbroTravelerController.StartConversation))]
        public static bool GabbroTravelerController_StartConversation_Prefix()
        {
            if (GabbroHug == null || !GabbroHug.IsSequenceInProgress()) return true;
            Locator.GetTravelerAudioManager().StopAllTravelerAudio();
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TornadoDialogueHandler), nameof(TornadoDialogueHandler.OnEnterTornado))]
        public static void TornadoDialogueHandler_OnEnterTornado_Postfix()
        {
            GabbroHug?.CancelHugSequence();
        }
    }


    [HarmonyPatch]
    public class SolanumPatches
    {
        public static void Apply() { new PatchClassProcessor(HarmonyInstance, typeof(SolanumPatches)).Patch(); }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.PlayRaiseCairns))]
        public static bool SolanumAnimController_PlayRaiseCairns_Prefix()
        {
            SolanumHug?.SetHugEnabled(false);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.Audio_ExitRaiseCairn))]
        public static void SolanumAnimController_Audio_ExitRaiseCairn_Postfix()
        {
            SolanumHug?.SetHugEnabled(true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.StartWritingMessage))]
        public static bool SolanumAnimController_StartWritingMessage_Prefix()
        {
            SolanumHug?.SetHugEnabled(false);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.StopWritingMessage))]
        public static void SolanumAnimController_StopWritingMessage_Postfix()
        {
            SolanumHug?.SetHugEnabled(true);
        }

        [HarmonyPrefix] //make IK work without required parameters
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.OnAnimatorIK))]
        public static bool SolanumAnimController_OnAnimatorIK_Prefix(SolanumAnimController __instance)
        {
            if (SolanumHug == null || !SolanumHug.IsSequenceInProgress()) return true;
            __instance._animator.SetLookAtPosition(__instance.gameObject.transform.TransformPoint(__instance._localLookPosition));
            __instance._animator.SetLookAtWeight(1, 0.5f, 0.9f, 0f);
            __instance._animator.SetIKPosition(AvatarIKGoal.LeftHand, __instance._leftHandTransform.position);
            __instance._animator.SetIKRotation(AvatarIKGoal.LeftHand, __instance._leftHandTransform.rotation * Quaternion.AngleAxis(-90f, Vector3.up));
            __instance._animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            __instance._animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            __instance._animator.SetIKPosition(AvatarIKGoal.RightHand, __instance._rightHandTransform.position);
            __instance._animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.15f);
            return false;
        }

        [HarmonyPrefix] //remove attempts to get AnimatorStateEvents during hug sequence
        [HarmonyPatch(typeof(SolanumAnimController), nameof(SolanumAnimController.LateUpdate))]
        public static bool SolanumAnimController_LateUpdate_Prefix(SolanumAnimController __instance)
        {
            if (SolanumHug == null || !SolanumHug.IsSequenceInProgress()) return true;
            Quaternion quaternion = Quaternion.LookRotation(__instance._playerCameraTransform.position - __instance._headBoneTransform.position, __instance.gameObject.transform.up);
            __instance._currentLookRotation = __instance._lookSpring.Update(__instance._currentLookRotation, quaternion, Time.deltaTime);
            Vector3 vector = __instance._headBoneTransform.position + __instance._currentLookRotation * Vector3.forward;
            __instance._localLookPosition = __instance.gameObject.transform.InverseTransformPoint(vector);
            return false;
        }

        [HarmonyPrefix]
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
            PrisonerHug?.SetHugEnabled(true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PrisonerEffects), nameof(PrisonerEffects.PlayFarewellBowAnimation))]
        public static bool PrisonerEffects_PlayFarewellBowAnimation_Prefix()
        {
            PrisonerHug?.SetHugEnabled(false);
            return true;
        }

        [HarmonyPrefix] //don't unpause movement during hugs
        [HarmonyPatch(typeof(PrisonerBrain), nameof(PrisonerBrain.FixedUpdate))]
        public static bool PrisonerBrain_FixedUpdate_Prefix(PrisonerBrain __instance)
        {
            if (PrisonerHug == null || !PrisonerHug.IsSequenceInProgress()) return true;
            __instance._controller.FixedUpdate_Controller();
            __instance._sensors.FixedUpdate_Sensors();
            __instance._data.FixedUpdate_Data(__instance._controller, __instance._sensors);
            return false;
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
