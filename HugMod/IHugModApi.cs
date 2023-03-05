using System;
using UnityEngine;

namespace HugMod
{
    public interface IHugModApi
    {
        /// <summary>
        /// Makes a suitable GameObject huggable
        /// </summary>
        /// <param name="hugObject">The GameObject, requires an OWCollider component on it or one of its children</param>
        void AddHugComponent(GameObject hugObject);

        /// <summary>
        /// Destroys a given GameObject's attached hug script along with all of the hug script's proxy components
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        void RemoveHugComponent(GameObject hugObject);


        /// <summary>
        /// Toggles huggability of a huggable GameObject, enabled by default
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="enable">False to disable hug script, true to reenable</param>
        void SetHugEnabled(GameObject hugObject, bool enable);

        /// <summary>
        /// Toggles a humanoid target looking towards the player during a hug on and off, enabled by default
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="enable">False to disable LookIK script, true to reenable</param>
        void SetLookAtPlayerEnabled(GameObject hugObject, bool enable);

        /// <summary>
        /// Makes the interact prompt display the right name
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="name">The name to be featured in the prompt</param>
        void SetPrompt(GameObject hugObject, string name);

        /// <summary>
        /// Makes the player look towards the right place during a hug
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="focusPoint">The point to look at relative to the GameObject's Transform</param>
        void SetFocusPoint(GameObject hugObject, Vector3 focusPoint);

        /// <summary>
        /// Change what kind of animation is played in response to a hug
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="hugTrigger">Best set as (int)HugTrigger</param>
        void SetAnimationTrigger(GameObject hugObject, int hugTrigger);

        /// <summary>
        /// Decides how much of the original animation to override and which parts to keep during a hug
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="fullbodyReact">If false, limits the animation override to the torso</param>
        /// <param name="keepRightFootPosition">If true, keeps the original animation on right foot</param>
        /// <param name="keepLeftFootPosition">If true, keeps the original animation on left foot</param>
        /// <param name="keepRightHandPose">If true, keeps the original animation on right fingers</param>
        /// <param name="keepLeftHandPose">If true, keeps the original animation on left fingers</param>
        void SetAnimationMasks(GameObject hugObject, bool fullbodyReact = true, bool keepRightFootPosition = false, bool keepLeftFootPosition = false, bool keepRightHandPose = false, bool keepLeftHandPose = false);

        /// <summary>
        /// (optional) Makes it so a transition happens during a hug sequence, causing it to always exit into a specific AnimatorState
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="transitionClip">An AnimationClip associated with the new AnimatorState</param>
        /// <param name="transitionHash">The new AnimatorState's hashed name</param>
        /// <param name="transitionTime">The time in which the transition towards the new state shall take place</param>
        void SetUnderlayTransition(GameObject hugObject, AnimationClip transitionClip, int transitionHash, float transitionTime = 0.5f);

        /// <summary>
        /// (optional) Makes it so a transition happens during a hug sequence, causing it to always exit into a specific AnimatorState
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="transitionClipName">The name of an AnimationClip associated with the new AnimatorState, the clip must be on the RuntimeAnimatorController</param>
        /// <param name="transitionHash">The new AnimatorState's hashed name</param>
        /// <param name="transitionTime">The time in which the transition towards the new state shall take place</param>
        void SetUnderlayTransition(GameObject hugObject, string transitionClipName, int transitionHash, float transitionTime = 0.5f);


        /// <summary>
        /// Returns the InteractReceiver associated with the hug script
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>The currently set Interactreceiver</returns>
        InteractReceiver GetHugReceiver(GameObject hugObject);

        /// <summary>
        /// Returns the primary Animator used by the hug script
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>The currently set primary Animator</returns>
        Animator GetHugAnimator(GameObject hugObject);

        /// <summary>
        /// Returns the secondary Animator used by the hug script
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>The currently set secondary Animator</returns>
        Animator GetSecondaryAnimator(GameObject hugObject);

        /// <summary>
        /// Returns everything pertaining to the underlay transition
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="transitionTime">The time in which the transition takes place</param>
        /// <param name="transitionHash">The hashed name of the AnimatorState being transitioned into</param>
        /// <param name="transitionClip">The AnimationClip being transitioned into</param>
        void GetUnderlayTransitionData(GameObject hugObject, out float transitionTime, out int transitionHash, out AnimationClip transitionClip);

        /// <summary>
        /// Checks if a given GameObject is involved in an ongoing hug sequence
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>If true, a hug sequence is currently in progress</returns>
        bool IsSequenceInProgress(GameObject hugObject);

        /// <summary>
        /// Checks if animations are currently being altered by HugMod
        /// </summary>
        /// <param name="hugObject"></param>
        /// <returns>If true, the RuntimeAnimatorController has been replaced with HugMod's AnimatorOverrideController</returns>
        bool IsAnimatorControllerSwapped(GameObject hugObject);

        /// <summary>
        /// Returns a method to cut an ongoing hug sequence involving a given GameObject short
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>The method that stops the sequence</returns>
        Action CancelHugSequence(GameObject hugObject);

        /// <summary>
        /// Forces a humanoid target to look towards the player even outside of a hug sequence
        /// </summary>
        /// <param name="gameObject">The huggable GameObject</param>
        /// <param name="enable">If true, Look IK has maximum weight regardless of whether a sequence is ongoing or not</param>
        void ForceLookAtPlayer(GameObject gameObject, bool enable);


        /// <summary>
        /// Subscribes to an event that fires at the end of the component's Start method 
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnInitComplete(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires at the start of a hug sequence
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnHugStart(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires before an underlay transition, only if transitionHash and transitionClip are both defined
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnUnderlayTransitionStart(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires at the start of the transition out of the hug, animated targets only
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnConcludingHug(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires at the end of a hug sequence
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnHugFinish(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires when the component is destroyed
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnDestroyEvent(GameObject hugObject, Action action);


        /// <summary>
        /// Sets or changes the associated InteractReceiver
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newReceiver">The new InteractReceiver</param>
        /// <param name="resetTriggerCollider">If true, creates a new trigger Collider using the new InteractReceiver's associated Collider as a reference</param>
        void ChangeInteractReceiver(GameObject hugObject, InteractReceiver newReceiver, bool resetTriggerCollider = false);

        /// <summary>
        /// Creates a new trigger Collider of the same type and position as a given Collider, but with slightly increased scale
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newCompareCollider">The Collider to reference from</param>
        void ChangeTriggerCollider(GameObject hugObject, Collider newCompareCollider);

        /// <summary>
        /// Sets or changes the associated primary Animator and related RuntimeAnimatorController
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newAnimator">The new Animator</param>
        /// <param name="newAnimatorController">The new RuntimeAnimatorController</param>
        void ChangePrimaryAnimator(GameObject hugObject, Animator newAnimator, RuntimeAnimatorController newAnimatorController);

        /// <summary>
        /// Sets or changes the associated secondary Animator
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newAnimator">The new Animator</param>
        void ChangeSecondaryAnimator(GameObject hugObject, Animator newAnimator);

        /// <summary>
        /// Sets or changes the associated CharacterAnimController
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newAnimController">The new CharacterAnimController</param>
        void ChangeCharacterAnimController(GameObject hugObject, CharacterAnimController newAnimController);
    }

    public enum HugTrigger
    {
        None,
        Default_High,
        Default_Medium,
        Default_Low,
        Reclining,
        HideAndSeek,
        Slate,
        Gneiss,
        Tektite,
        Nomai,
        Ghost
    }
}
