using System;
using UnityEngine;

namespace HugMod
{
    public interface IHugModApi
    {
        /// <summary>
        /// Returns an array of all GameObjects that have a hug script attached<br/>
        /// This may falsely return null if used directly on scene load, so it's good to wait a frame first
        /// </summary>
        /// <returns>The array of GameObjects, including deactivated ones</returns>
        public GameObject[] GetAllHuggables();


        /// <summary>
        /// Makes a suitable GameObject huggable
        /// </summary>
        /// <param name="hugObject">The GameObject, requires an OWCollider component on it or one of its children</param>
        /// <param name="initialiseImmediately">If true, the hug script is automatically initialised upon being attached to the GameObject<br/>
        /// If false, InitialiseHugComponent has to be called manually<br/>
        /// Set this to false if you have code that needs to run after the script is attached but before it can call GetComponentInChildren to set its fields</param>
        void AddHugComponent(GameObject hugObject, bool initialiseImmediately = true);

        /// <summary>
        /// Initialises the hug script attached to a given GameObject by searching for required components in its children and adding new components if needed
        /// </summary>
        /// <param name="hugObject">The huggable GameObject, requires an OWCollider component on it or one of its children</param>
        void InitialiseHugComponent(GameObject hugObject);

        /// <summary>
        /// Destroys a given GameObject's attached hug script, along with all proxy components added by it<br/>
        /// Does not affect InteractReceivers even if they were added by the hug script
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        void RemoveHugComponent(GameObject hugObject);


        //-----Set stuff-----

        /// <summary>
        /// Toggles huggability of a huggable GameObject<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="enable">False to disable hug script, true to reenable</param>
        void SetHugEnabled(GameObject hugObject, bool enable);

        /// <summary>
        /// Toggles whether a target looks at the player during a hug<br/>
        /// Requires the target to be animated and humanoid<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="enable">False to disable Look IK script, true to reenable</param>
        void SetLookAtPlayerEnabled(GameObject hugObject, bool enable);

        /// <summary>
        /// Forces a target to look at the player even outside of a hug sequence<br/>
        /// Requires the target to be animated and humanoid<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="enable">If true, Look IK has maximum weight regardless of whether a sequence is ongoing or not<br/>
        /// False to reset</param>
        void ForceLookAtPlayer(GameObject hugObject, bool enable);

        /// <summary>
        /// Sets the name displayed in the hug interact prompt
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="name">The name to be featured in the prompt</param>
        void SetPrompt(GameObject hugObject, string name);

        /// <summary>
        /// Sets what point the player camera should focus on during a hug<br/>
        /// This point's position in relation to the player also decides whether a target is out of reach, and what animation is used for the player
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="focusPoint">The point relative to the huggable GameObject's Transform</param>
        void SetFocusPoint(GameObject hugObject, Vector3 focusPoint);

        /// <summary>
        /// Sets what reaction animation is played when the target is hugged<br/>
        /// Requires the target to be animated and humanoid<br/><br/>
        /// If you have an animated but nonhumanoid target with custom animations you want to use,<br/>
        /// use the events provided by the hug sequence to trigger your animations instead
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="hugTrigger">Best set as (int)HugTrigger, using the enum included in the IHugModApi.cs file<br/><br/>
        /// If you have an animated humanoid target with its own custom animations you want to use,<br/>
        /// leave the trigger as None and use the events provided by the hug sequence to trigger your animations</param>
        void SetAnimationTrigger(GameObject hugObject, int hugTrigger);

        /// <summary>
        /// Decides how much of the original animation to override and which parts to keep during a hug<br/>
        /// Requires the target to be animated and humanoid
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="fullbodyReact">If false, limits the animation override to the upper body while leaving hips and legs untouched</param>
        /// <param name="keepRightFootPosition">If true, keeps the original animation on the right foot</param>
        /// <param name="keepLeftFootPosition">If true, keeps the original animation on the left foot</param>
        /// <param name="keepRightHandPose">If true, keeps the original animation on the right hand fingers</param>
        /// <param name="keepLeftHandPose">If true, keeps the original animation on the left hand fingers</param>
        void SetAnimationMasks(GameObject hugObject, bool fullbodyReact = true, bool keepRightFootPosition = false, bool keepLeftFootPosition = false, bool keepRightHandPose = false, bool keepLeftHandPose = false);

        /// <summary>
        /// Makes it so an animation transition happens during a hug sequence, causing it to always exit into a specific AnimatorState<br/>
        /// Requires the target to be animated and humanoid, and doesn't work if HugTrigger is set to None
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="transitionClip">An AnimationClip associated with the new AnimatorState</param>
        /// <param name="transitionHash">The new AnimatorState's hashed name</param>
        /// <param name="transitionTime">The time in which the transition towards the new state shall take place</param>
        void SetUnderlayTransition(GameObject hugObject, AnimationClip transitionClip, int transitionHash, float transitionTime = 0.5f);

        /// <summary>
        /// Makes it so an animation transition happens during a hug sequence, causing it to always exit into a specific AnimatorState<br/>
        /// Requires the target to be animated and humanoid, and doesn't work if HugTrigger is set to None<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="transitionClipName">The name of an AnimationClip associated with the new AnimatorState, the clip must be on the RuntimeAnimatorController</param>
        /// <param name="transitionHash">The new AnimatorState's hashed name</param>
        /// <param name="transitionTime">The time in which the transition towards the new state shall take place</param>
        void SetUnderlayTransition(GameObject hugObject, string transitionClipName, int transitionHash, float transitionTime = 0.5f);


        //-----Get stuff-----

        /// <summary>
        /// Returns the InteractReceiver associated with the hug script<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>The currently set InteractReceiver</returns>
        InteractReceiver GetInteractReceiver(GameObject hugObject);

        /// <summary>
        /// Returns the primary Animator used by the hug script<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>The currently set primary Animator</returns>
        Animator GetPrimaryAnimator(GameObject hugObject);

        /// <summary>
        /// Returns the secondary Animator used by the hug script<br/>
        /// Requires hug script to be initialised
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
        /// Checks if animations are currently being altered by HugMod<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>If true, the Animator's RuntimeAnimatorController has been temporarily set to HugMod's AnimatorOverrideController</returns>
        bool IsAnimatorControllerSwapped(GameObject hugObject);

        /// <summary>
        /// Returns a method to cut an ongoing hug sequence involving a given GameObject short
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <returns>The method that stops the sequence for that GameObject, can be called by itself or subscribed to an event</returns>
        Action CancelHugSequence(GameObject hugObject);


        //-----Event subs-----

        /// <summary>
        /// Subscribes to an event that fires at the start of a hug sequence involving a given GameObject
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnHugStart(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires before an underlay transition, only if transitionHash and transitionClip are both defined<br/>
        /// Requires the target to be animated and humanoid, and doesn't work if HugTrigger is set to None
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnUnderlayTransitionStart(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires at the start of the transition out of the hug<br/>
        /// Requires the target to be animated and humanoid
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnConcludingHug(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires at the end of a hug sequence involving a given GameObject
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnHugFinish(GameObject hugObject, Action action);

        /// <summary>
        /// Subscribes to an event that fires when the hug script attached to a given GameObject is destroyed
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="action">The method to call</param>
        void OnDestroyEvent(GameObject hugObject, Action action);


        //-----Change yoinkables-----

        /// <summary>
        /// Sets or changes the associated InteractReceiver<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newReceiver">The new InteractReceiver</param>
        /// <param name="resetTriggerCollider">If true, creates a new trigger Collider using the new InteractReceiver's associated Collider for reference</param>
        void ChangeInteractReceiver(GameObject hugObject, InteractReceiver newReceiver, bool resetTriggerCollider = false);

        /// <summary>
        /// Creates a new trigger Collider of the same type and position as a given Collider, but with slightly increased scale<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newCompareCollider">The Collider to reference from</param>
        void ChangeTriggerCollider(GameObject hugObject, Collider newCompareCollider);

        /// <summary>
        /// Sets or changes the associated primary Animator<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newAnimator">The new Animator</param>
        /// <param name="resetAnimatorController">If true, automatically sets RuntimeAnimatorController to the one associated with the new Animator</param>
        void ChangePrimaryAnimator(GameObject hugObject, Animator newAnimator, bool resetAnimatorController = true);

        /// <summary>
        /// Sets or changes the RuntimeAnimatorController used when resetting animations after a hug sequence<br/>
        /// Requires the target to be animated and humanoid, and doesn't work if HugTrigger is set to None<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newAnimatorController">The new RuntimeAnimatorController</param>
        void ChangeAnimatorController(GameObject hugObject, RuntimeAnimatorController newAnimatorController);

        /// <summary>
        /// Sets or changes the associated secondary Animator<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newAnimator">The new Animator</param>
        void ChangeSecondaryAnimator(GameObject hugObject, Animator newAnimator);

        /// <summary>
        /// Sets or changes the CharacterAnimController the Look IK script coordinates with<br/>
        /// Requires the hug script to be initialised
        /// </summary>
        /// <param name="hugObject">The huggable GameObject</param>
        /// <param name="newCharacterAnimController">The new CharacterAnimController</param>
        void ChangeCharacterAnimController(GameObject hugObject, CharacterAnimController newCharacterAnimController);
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
