using System;
using System.Collections;
using UnityEngine;
using static HugMod.HugMod;

namespace HugMod
{
    public class PlayerHugController : MonoBehaviour
    {
        public static ScreenPrompt HugPrompt = new(InputLibrary.interactSecondary, "");

        public static bool WalkingTowardsHugTarget = false;
        public static float WalkingTowardsHugSpeed = 0.7f;
        public static GameObject PlayerCamera { get; private set; }

        private static PlayerHugController playerHugInstance;
        private static GameObject playerObject;
        private static GameObject[] arms;
        private static OWAudioSource sound;
        private static PlayerAnimController playerAnimController;
        private static Animator playerAnimator;
        private static RuntimeAnimatorController playerRuntimeController;
        private static AnimatorOverrideController playerOverrider = new();
        private static AnimationClip idleClip, idleClipSuit;
        private static GameObject cameraParent, cameraAttach;
        private static float playerCrossfadeTime = 0.5f;
        private static bool cameraAttached, concludingHug;


        public static void SetUpPlayer()
        {
            if (playerHugInstance == null) playerHugInstance = GameObject.Find("Player_Body/Traveller_HEA_Player_v2").AddComponent<PlayerHugController>();
            playerHugInstance.StartCoroutine(SetupRoutine());
        }

        private static IEnumerator SetupRoutine()
        {
            cameraAttached = false; 
            concludingHug = false;

            playerObject = playerHugInstance.gameObject;
            var armR = playerObject.transform.Find("player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_RightArm").gameObject;
            var armL = playerObject.transform.Find("player_mesh_noSuit:Traveller_HEA_Player/player_mesh_noSuit:Player_LeftArm").gameObject;
            var armSuitR = playerObject.transform.Find("Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_RightArm").gameObject;
            var armSuitL = playerObject.transform.Find("Traveller_Mesh_v01:Traveller_Geo/Traveller_Mesh_v01:PlayerSuit_LeftArm").gameObject;
            arms = new[] { armR, armL, armSuitR, armSuitL };

            yield return new WaitForEndOfFrame();

            PlayerCamera = Locator.GetPlayerCamera().gameObject;
            cameraParent = PlayerCamera.transform.parent.gameObject;
            cameraAttach = playerObject.FindInDescendants("Traveller_Rig_v01:Traveller_Camera_01_Jnt").CreateChild("Hug_Camera", scaleMultiplier: 10);
            cameraAttach.transform.SetPositionAndRotation(cameraParent.transform.position, cameraParent.transform.rotation);

            playerAnimController = playerObject.GetComponent<PlayerAnimController>();
            playerAnimator = playerObject.GetComponent<Animator>();
            playerOverrider.runtimeAnimatorController = AltRuntimeController;

            playerObject.SetActive(false);
            sound = playerObject.AddComponent<OWAudioSource>();
            sound.SetTrack(OWAudioMixer.TrackName.Player_External);
            playerObject.SetActive(true);
            sound.AssignAudioLibraryClip(PlayerHasDLC ? AudioType.Ghost_Grab_Contact : AudioType.ImpactLowSpeed);
            sound.SetMaxVolume(PlayerHasDLC ? 0.6f : 0.3f);
            sound.playOnAwake = false;
        }


        public static Transform GetPlayerObjectTransform() => playerObject.transform;

        public static void LockPlayerControl(Transform baseTransform, Vector3 focus)
        {
            Locator.GetToolModeSwapper().UnequipTool();
            OWInput.ChangeInputMode(InputMode.None);
            Locator.GetPlayerTransform()?.GetRequiredComponent<PlayerLockOnTargeting>()?.LockOn(baseTransform, focus, 4.5f);
        }

        public static void UnlockPlayerControl()
        {
            Locator.GetPlayerTransform()?.GetRequiredComponent<PlayerLockOnTargeting>()?.BreakLock();
            OWInput.ChangeInputMode(InputMode.Character);
        }


        public static void PlayerStartHug(float height)
        {
            if (playerAnimator.runtimeAnimatorController != playerOverrider)
            {
                playerRuntimeController = playerAnimator.runtimeAnimatorController; //regrab every time because it differs between suited and unsuited
                if (PlayerState._isWearingSuit) idleClipSuit ??= Array.Find(playerRuntimeController.animationClips, element => element.name == "Idle_Hold_Suit");
                else idleClip ??= Array.Find(playerRuntimeController.animationClips, element => element.name == "Idle_Hold");

                playerOverrider["clip_Placeholder01"] = playerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
                playerOverrider["clip_Placeholder02"] = PlayerState._isWearingSuit ? idleClipSuit : idleClip;
                playerAnimator.runtimeAnimatorController = playerOverrider;

                playerHugInstance.StartCoroutine(CameraAttached());
            }

            var animStateName = "hug_" + ((height > 15) ? "upright" : (height > 11) ? "hunched" : "crouching");
            playerAnimator.CrossFadeInFixedTime(animStateName, playerCrossfadeTime, 0);
            sound.PlayDelayed(playerCrossfadeTime);
        }

        public static void PlayerEndHug()
        {
            if (concludingHug) return;
            if (playerAnimator.runtimeAnimatorController != playerOverrider) ResetPlayer();
            else playerHugInstance.StartCoroutine(ResetPlayer(playerCrossfadeTime));
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
                    playerAnimController._rightArmHidden = !visible; //keep this wrong to trick PlayerAnimController into not undoing layer changes
                    visible = !visible;
                } 
                yield return null; 
            }
            if (!visible) foreach (var arm in arms) arm.layer = LayerMask.NameToLayer("Default");
            playerAnimController._rightArmHidden = false; //correct value here

            PlayerCamera.transform.SetParent(cameraParent.transform, true);
        }

        private static IEnumerator ResetPlayer(float crossfadeDuration)
        {
            concludingHug = true;

            while (playerAnimator.IsInTransition(0)) yield return null;
            yield return new WaitForSeconds(0.3f); //at the very least finish hug start transition and hold for this long

            playerAnimator.CrossFadeInFixedTime("Placeholder02", crossfadeDuration, 0, -(crossfadeDuration + 0.1f));

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


        private void Update() { HugPrompt.SetVisibility(OWInput.IsInputMode(InputMode.Character)); }
    }
}
