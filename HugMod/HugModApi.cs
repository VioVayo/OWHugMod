using OWML.Common;
using System;
using UnityEngine;

namespace HugMod
{
    public class HugModApi : IHugModApi
    {
        public void AddHugComponent(GameObject hugObject, bool initialiseImmediately) 
        {
            if (hugObject.GetComponent<HugComponent>() == null) 
            { 
                var hug = hugObject.AddComponent<HugComponent>();
                if (initialiseImmediately) hug.Initialise();
            }
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"\"{hugObject.name}\" already has HugComponent.", MessageType.Error);
        }

        public void InitialiseHugComponent(GameObject hugObject)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.Initialise();
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void RemoveHugComponent(GameObject hugObject)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) GameObject.Destroy(hug);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }


        //-----Set stuff-----
        public void SetHugEnabled(GameObject hugObject, bool enable)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetHugEnabled(enable);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void SetLookAtPlayerEnabled(GameObject hugObject, bool enable)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetLookAtPlayerEnabled(enable);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void ForceLookAtPlayer(GameObject hugObject, bool enable)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.ForceLookAtPlayer(enable);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void SetPrompt(GameObject hugObject, string name)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetPrompt(name);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void SetFocusPoint(GameObject hugObject, Vector3 focusPoint)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetFocusPoint(focusPoint);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void SetAnimationTrigger(GameObject hugObject, int hugTrigger)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetAnimationTrigger((HugTrigger)hugTrigger);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void SetAnimationMasks(GameObject hugObject, bool fullbodyReact, bool keepRightFootPosition, bool keepLeftFootPosition, bool keepRightHandPose, bool keepLeftHandPose)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetAnimationMasks(fullbodyReact, keepRightFootPosition, keepLeftFootPosition, keepRightHandPose, keepLeftHandPose);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void SetUnderlayTransition(GameObject hugObject, string transitionClipName, int transitionHash, float transitionTime)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetUnderlayTransition(transitionClipName, transitionHash, transitionTime);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void SetUnderlayTransition(GameObject hugObject, AnimationClip transitionClip, int transitionHash, float transitionTime)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.SetUnderlayTransition(transitionClip, transitionHash, transitionTime);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }


        //-----Get stuff-----
        public InteractReceiver GetHugReceiver(GameObject hugObject)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) return hug.HugReceiver;
            else
            {
                HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
                return null;
            }
        }

        public Animator GetHugAnimator(GameObject hugObject) 
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) return hug.HugAnimator;
            else
            {
                HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
                return null;
            }
        }

        public Animator GetSecondaryAnimator(GameObject hugObject) 
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) return hug.SecondaryAnimator;
            else
            {
                HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
                return null;
            }
        }

        public void GetUnderlayTransitionData(GameObject hugObject, out float transitionTime, out int transitionHash, out AnimationClip transitionClip)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.GetUnderlayTransitionData(out transitionTime, out transitionHash, out transitionClip);
            else
            {
                HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
                transitionTime = 0;
                transitionHash = 0;
                transitionClip = null;
            }
        }

        public bool IsSequenceInProgress(GameObject hugObject)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) return hug.IsSequenceInProgress();
            else
            {
                HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
                return false;
            }
        }

        public bool IsAnimatorControllerSwapped(GameObject hugObject)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) return hug.IsAnimatorControllerSwapped();
            else
            {
                HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
                return false;
            }
        }

        public Action CancelHugSequence(GameObject hugObject)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) return hug.CancelHugSequence;
            else
            {
                HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
                return null;
            }
        }


        //-----Event subs-----
        public void OnHugStart(GameObject hugObject, Action action)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.OnHugStart += action;
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void OnUnderlayTransitionStart(GameObject hugObject, Action action)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.OnUnderlayTransitionStart += action;
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void OnConcludingHug(GameObject hugObject, Action action)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.OnConcludingHug += action;
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void OnHugFinish(GameObject hugObject, Action action)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.OnHugFinish += action;
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void OnDestroyEvent(GameObject hugObject, Action action)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.OnDestroyEvent += action;
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }


        //-----Change yoinkables-----
        public void ChangeInteractReceiver(GameObject hugObject, InteractReceiver newReceiver, bool resetTriggerCollider)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.ChangeInteractReceiver(newReceiver, resetTriggerCollider);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void ChangeTriggerCollider(GameObject hugObject, Collider newCompareCollider)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.ChangeTriggerCollider(newCompareCollider);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void ChangePrimaryAnimator(GameObject hugObject, Animator newAnimator, RuntimeAnimatorController newAnimatorController)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.ChangePrimaryAnimator(newAnimator, newAnimatorController);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void ChangeSecondaryAnimator(GameObject hugObject, Animator newAnimator)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.ChangeSecondaryAnimator(newAnimator);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }

        public void ChangeCharacterAnimController(GameObject hugObject, CharacterAnimController newAnimController)
        {
            if (hugObject.TryGetComponent(out HugComponent hug)) hug.ChangeCharacterAnimController(newAnimController);
            else HugMod.HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugComponent on \"{hugObject.name}\".", MessageType.Error);
        }
    }
}
