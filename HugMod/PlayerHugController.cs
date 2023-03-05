using System;
using System.Collections;
using UnityEngine;
using static HugMod.HugMod;

namespace HugMod
{
    public class PlayerHugController
    {
        public static bool WalkingTowardsHugTarget = false;
        public static float WalkingTowardsHugSpeed = 0.7f;

        private static GameObject playerObject;
        private static OWAudioSource sound;
        private static Animator playerAnimator;
        private static RuntimeAnimatorController playerRuntimeController;
        private static AnimatorOverrideController playerOverrider = new();
        private static AnimationClip idleClip, idleClipSuit;
        private static GameObject camera, cameraParent, cameraAttach;
        private static float playerCrossfadeTime = 0.5f;
        private static bool cameraAttached = false, concludingHug = false;


        public static IEnumerator SetUpPlayer()
        {
            playerObject = GameObject.Find("Player_Body/Traveller_HEA_Player_v2");
            yield return null;

            camera = Locator.GetPlayerCamera().gameObject;
            cameraParent = camera.transform.parent.gameObject;
            cameraAttach = CreateChild("Hug_Camera", FindInDescendants(playerObject, "Traveller_Rig_v01:Traveller_Camera_01_Jnt"), scaleMultiplier: 10);
            cameraAttach.transform.position = cameraParent.transform.position;
            cameraAttach.transform.rotation = cameraParent.transform.rotation;

            playerAnimator = playerObject.GetComponent<Animator>();
            playerOverrider.runtimeAnimatorController = AltRuntimeController;

            sound = playerObject.AddComponent<OWAudioSource>();
            sound.playOnAwake = false;
            sound.SetTrack(OWAudioMixer.TrackName.Player_External);
            sound.AssignAudioLibraryClip(PlayerHasDLC ? AudioType.Ghost_Grab_Contact : AudioType.ImpactLowSpeed);
            sound.SetMaxVolume(PlayerHasDLC ? 0.6f : 0.3f);
        }


        public static Transform GetPlayerTransform() { return playerObject.transform; }

        public static void LockPlayerControl(Transform baseTransform, Vector3 focus)
        {
            OWInput.ChangeInputMode(InputMode.None);
            Locator.GetPlayerTransform().GetRequiredComponent<PlayerLockOnTargeting>().LockOn(baseTransform, focus, 4.5f);
        }

        public static void UnlockPlayerControl()
        {
            Locator.GetPlayerTransform().GetRequiredComponent<PlayerLockOnTargeting>().BreakLock();
            OWInput.ChangeInputMode(InputMode.Character);
        }


        public static void PlayerStartHug(float height)
        {
            Locator.GetToolModeSwapper().UnequipTool();

            if (playerAnimator.runtimeAnimatorController != playerOverrider)
            {
                playerRuntimeController = playerAnimator.runtimeAnimatorController; //regrab every time because it differs between suited and unsuited
                if (PlayerState._isWearingSuit) idleClipSuit ??= Array.Find(playerRuntimeController.animationClips, element => element.name == "Idle_Hold_Suit");
                else idleClip ??= Array.Find(playerRuntimeController.animationClips, element => element.name == "Idle_Hold");

                playerOverrider["clip_Placeholder01"] = playerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
                playerOverrider["clip_Placeholder02"] = PlayerState._isWearingSuit ? idleClipSuit : idleClip;
                playerAnimator.runtimeAnimatorController = playerOverrider;

                HugModInstance.StartCoroutine(CameraAttached());
            }

            var animStateName = "hug_" + ((height > 15) ? "upright" : (height > 11) ? "hunched" : "crouching");
            playerAnimator.CrossFadeInFixedTime(animStateName, playerCrossfadeTime, 0);
            sound.PlayDelayed(playerCrossfadeTime);
        }

        public static void PlayerEndHug()
        {
            if (concludingHug) return;
            if (playerAnimator.runtimeAnimatorController != playerOverrider) ResetPlayer();
            else HugModInstance.StartCoroutine(ResetPlayer(playerCrossfadeTime));
        }


        private static IEnumerator CameraAttached()
        {
            camera.transform.SetParent(cameraAttach.transform, true);
            cameraAttached = true;
            while (cameraAttached) yield return null;
            camera.transform.SetParent(cameraParent.transform, true);
        }

        private static IEnumerator ResetPlayer(float crossfadeDuration)
        {
            concludingHug = true;

            while (playerAnimator.IsInTransition(0)) yield return null;
            yield return new WaitForSeconds(0.3f);

            playerAnimator.CrossFadeInFixedTime("Placeholder02", crossfadeDuration, 0, -crossfadeDuration);

            yield return new WaitForSeconds(crossfadeDuration + 0.1f);

            playerAnimator.runtimeAnimatorController = playerRuntimeController;
            playerAnimator.SetBool("Grounded", true);
            concludingHug = false;

            ResetPlayer();
        }

        private static void ResetPlayer()
        {
            cameraAttached = false;
            UnlockPlayerControl();
        }
    }
}
