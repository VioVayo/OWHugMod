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
        public static GameObject PlayerCamera;

        private static GameObject playerObject;
        private static GameObject[] arms;
        private static OWAudioSource sound;
        private static Animator playerAnimator;
        private static RuntimeAnimatorController playerRuntimeController;
        private static AnimatorOverrideController playerOverrider = new();
        private static AnimationClip idleClip, idleClipSuit;
        private static GameObject cameraParent, cameraAttach;
        private static float playerCrossfadeTime = 0.5f;
        private static bool cameraAttached = false, concludingHug = false;


        public static IEnumerator SetUpPlayer()
        {
            playerObject = GameObject.Find("Player_Body/Traveller_HEA_Player_v2");
            var armR = playerObject.transform.Find("player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_RightArm").gameObject;
            var armL = playerObject.transform.Find("player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_LeftArm").gameObject;
            var armSuitR = playerObject.transform.Find("Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_RightArm").gameObject;
            var armSuitL = playerObject.transform.Find("Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_LeftArm").gameObject;
            arms = new[] { armR, armL, armSuitR, armSuitL };

            yield return null;

            PlayerCamera = Locator.GetPlayerCamera().gameObject;
            cameraParent = PlayerCamera.transform.parent.gameObject;
            cameraAttach = CreateChild("Hug_Camera", FindInDescendants(playerObject, "Traveller_Rig_v01:Traveller_Camera_01_Jnt"), scaleMultiplier: 10);
            cameraAttach.transform.SetPositionAndRotation(cameraParent.transform.position, cameraParent.transform.rotation);

            playerAnimator = playerObject.GetComponent<Animator>();
            playerOverrider.runtimeAnimatorController = AltRuntimeController;

            sound = playerObject.AddComponent<OWAudioSource>();
            sound.playOnAwake = false;
            sound.SetTrack(OWAudioMixer.TrackName.Player_External);
            sound.AssignAudioLibraryClip(PlayerHasDLC ? AudioType.Ghost_Grab_Contact : AudioType.ImpactLowSpeed);
            sound.SetMaxVolume(PlayerHasDLC ? 0.6f : 0.3f);
        }


        public static Transform GetPlayerObjectTransform() { return playerObject.transform; }

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
            PlayerCamera.transform.SetParent(cameraAttach.transform, true);
            cameraAttached = true;

            var visible = true;
            while (cameraAttached)
            {
                if ((visible && Locator.GetActiveCamera().gameObject == PlayerCamera) || (!visible && Locator.GetActiveCamera().gameObject != PlayerCamera))
                {
                    foreach (var arm in arms) arm.layer = LayerMask.NameToLayer(visible ? "VisibleToProbe" : "Default");
                    visible = !visible;
                } 
                yield return null; 
            }
            if (!visible) foreach (var arm in arms) arm.layer = LayerMask.NameToLayer("Default");

            PlayerCamera.transform.SetParent(cameraParent.transform, true);
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
