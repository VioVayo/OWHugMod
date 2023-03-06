using HugMod.ProxyComponents;
using OWML.Common;
using System;
using System.Collections;
using UnityEngine;
using static HugMod.HugMod;
using static HugMod.PlayerHugController;

namespace HugMod
{
    public class HugComponent : MonoBehaviour
    {
        //yoinked
        public InteractReceiver HugReceiver { get; private set; }
        public Animator HugAnimator { get; private set; }
        public Animator SecondaryAnimator { get; private set; }

        private RuntimeAnimatorController initialRuntimeController;
        private CharacterAnimController characterAnimController;
        private DampedSpring3D lookSpring;
        private AnimatorOverrideController hugOverrider = new();

        //tabled
        private ScreenPrompt hugPrompt = new(InputLibrary.interactSecondary, "<CMD> " + "Hug");
        private Vector3 focusPoint = new(0, 0.1f, 0);
        private string hugTrigger = "react_None";
        private bool fullbodyReact = true, keepFootAnimRight = false, keepFootAnimLeft = false, keepHandAnimRight = false, keepHandAnimLeft = false;

        private float transitionTime = 0.5f;
        private int transitionHash;
        private AnimationClip transitionClip;

        //working variables
        private Vector3 currentLookTarget;
        private float currentLookWeight;
        private int stateHash;
        private bool isInitialised = false, isAnimated = false, canHug = false, sequenceInProgress = false;

        private static Coroutine unstickRoutine;
        private static float unstickTime = 3;

        //proxies
        private HugTriggerCollider hugTriggerCollider;
        private HugIK hugIK;
        private bool forceLookAtPlayer = false;

        //events
        public event Action OnInitComplete;
        public event Action OnHugStart;
        public event Action OnUnderlayTransitionStart;
        public event Action OnConcludingHug;
        public event Action OnHugFinish;
        public event Action OnDestroyEvent;


        public void Start()
        {
            HugReceiver = gameObject.GetComponentInChildren<InteractReceiver>();
            if (HugReceiver == null)
            {
                var receiverCollider = gameObject.GetComponentInChildren<OWCollider>();
                if (receiverCollider == null)
                {
                    HugModInstance.ModHelper.Console.WriteLine($"Couldn't find OWCollider in children of object \"{gameObject.name}\". HugComponent will be disabled.", MessageType.Error);
                    enabled = false;
                    return;
                }
                if (receiverCollider.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) receiverCollider.gameObject.layer = LayerMask.NameToLayer("Default");
                HugReceiver = receiverCollider.gameObject.AddComponent<InteractReceiver>();
                HugReceiver.OnGainFocus += () => { Locator.GetPromptManager().RemoveScreenPrompt(HugReceiver._screenPrompt); };
                HugReceiver.SetInteractionEnabled(true);
            }
            if (!AddTriggerCollider(HugReceiver._owCollider._collider)) return; //only progress if both a valid InteractReceiver and trigger Collider exist

            HugReceiver.OnGainFocus += EnableHug;
            HugReceiver.OnLoseFocus += DisableHug;

            characterAnimController = gameObject.GetComponentInChildren<CharacterAnimController>();
            if (characterAnimController != null)
            {
                HugAnimator = characterAnimController._animator;
                SecondaryAnimator = characterAnimController._secondaryAnimator;
                lookSpring = characterAnimController.lookSpring;
            }
            else 
            { 
                HugAnimator = gameObject.GetComponentInChildren<Animator>(true);
                lookSpring = new(50, 14, 1);
            }

            isAnimated = HugAnimator != null && HugAnimator.avatar.isHuman;
            if (isAnimated)
            {
                initialRuntimeController = HugAnimator.runtimeAnimatorController;
                hugIK = HugAnimator.gameObject.AddComponent<HugIK>();
                hugIK.hugComponent = this;
            }

            hugOverrider.runtimeAnimatorController = AltRuntimeController;
            isInitialised = true;
            OnInitComplete?.Invoke();
        }

        private bool AddTriggerCollider(Collider compareCollider)
        {
            var triggerObj = CreateChild("Hug_TriggerCollider", compareCollider.gameObject.transform);
            triggerObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            if (compareCollider is CapsuleCollider capsuleCollider) AddCapsuleTrigger(capsuleCollider, triggerObj);
            else if (compareCollider is SphereCollider sphereCollider) AddSphereTrigger(sphereCollider, triggerObj);
            else if (compareCollider is BoxCollider boxCollider) AddBoxTrigger(boxCollider, triggerObj);
            else if (compareCollider is MeshCollider meshCollider) AddMeshTrigger(meshCollider, triggerObj);
            else
            {
                Destroy(triggerObj);
                HugModInstance.ModHelper.Console.WriteLine($"Collider type associated with object \"{gameObject.name}\" is not supported. HugComponent will be disabled.", MessageType.Error);
                enabled = false;
                return false;
            }

            hugTriggerCollider = triggerObj.AddComponent<HugTriggerCollider>();
            hugTriggerCollider.hugComponent = this;
            return true;
        }

        private void AddCapsuleTrigger(CapsuleCollider compareCollider, GameObject triggerObject)
        {
            var triggerCollider = triggerObject.AddComponent<CapsuleCollider>();
            triggerCollider.center = triggerObject.transform.InverseTransformPoint(compareCollider.transform.TransformPoint(compareCollider.center));
            triggerCollider.radius = compareCollider.radius + 0.15f;
            triggerCollider.height = compareCollider.height;
            triggerCollider.direction = compareCollider.direction;
            triggerCollider.isTrigger = true;
        }

        private void AddSphereTrigger(SphereCollider compareCollider, GameObject triggerObject)
        {
            var triggerCollider = triggerObject.AddComponent<SphereCollider>();
            triggerCollider.center = triggerObject.transform.InverseTransformPoint(compareCollider.transform.TransformPoint(compareCollider.center));
            triggerCollider.radius = compareCollider.radius + 0.15f;
            triggerCollider.isTrigger = true;
        }

        private void AddBoxTrigger(BoxCollider compareCollider, GameObject triggerObject)
        {
            var triggerCollider = triggerObject.AddComponent<BoxCollider>();
            triggerCollider.center = triggerObject.transform.InverseTransformPoint(compareCollider.transform.TransformPoint(compareCollider.center));
            triggerCollider.size = compareCollider.size + 0.3f * Vector3.one;
            triggerCollider.isTrigger = true;
        }

        private void AddMeshTrigger(MeshCollider compareCollider, GameObject triggerObject)
        {
            var triggerCollider = triggerObject.AddComponent<MeshCollider>();
            triggerObject.transform.localScale = 1.05f * Vector3.one;
            triggerCollider.sharedMesh = compareCollider.sharedMesh;
            triggerCollider.convex = true;
            triggerCollider.isTrigger = true;
        }


        // Set, gets, changes
        public void SetHugEnabled(bool enable)
        {
            if (!enable && HugReceiver != null && HugReceiver._focused) DisableHug();
            enabled = enable;
            if (enable && HugReceiver != null && HugReceiver._focused) EnableHug();
        }
        public void SetLookAtPlayerEnabled(bool enable) 
        { 
            if (hugIK != null) hugIK.enabled = enable; 
            else HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugIK Component on on object \"{HugAnimator.gameObject.name}\".", MessageType.Error);
        }
        public void ForceLookAtPlayer(bool enable) { forceLookAtPlayer = enable; }
        public void SetPrompt(string name) { hugPrompt = new(InputLibrary.interactSecondary, "<CMD> " + "Hug " + name); }
        public void SetFocusPoint(Vector3 focusPoint) { this.focusPoint = focusPoint; }
        public void SetAnimationTrigger(HugTrigger hugTrigger) { this.hugTrigger = "react_" + hugTrigger.ToString();}
        public void SetAnimationMasks(bool fullbodyReact = true, bool keepRightFootPosition = false, bool keepLeftFootPosition = false, bool keepRightHandPose = false, bool keepLeftHandPose = false)
        {
            this.fullbodyReact = fullbodyReact;
            this.keepFootAnimRight = keepRightFootPosition;
            this.keepFootAnimLeft = keepLeftFootPosition;
            this.keepHandAnimRight = keepRightHandPose;
            this.keepHandAnimLeft = keepLeftHandPose;
        }
        public void SetUnderlayTransition(string transitionClipName, int transitionHash, float transitionTime = 0.5f) 
        {
            if (isInitialised) SetByName();
            else OnInitComplete += SetByName;
            void SetByName()
            {
                if (!isAnimated) return;
                this.transitionTime = transitionTime;
                this.transitionHash = transitionHash;
                this.transitionClip = Array.Find(initialRuntimeController.animationClips, element => element.name == transitionClipName); 
            }
        }
        public void SetUnderlayTransition(AnimationClip transitionClip, int transitionHash, float transitionTime = 0.5f)
        {
            this.transitionTime = transitionTime;
            this.transitionHash = transitionHash;
            this.transitionClip = transitionClip;
        }

        public void GetUnderlayTransitionData(out float transitionTime, out int transitionHash, out AnimationClip transitionClip) 
        {
            transitionTime = this.transitionTime;
            transitionHash = this.transitionHash; 
            transitionClip = this.transitionClip; 
        }

        public bool IsSequenceInProgress() { return sequenceInProgress; }
        public bool IsAnimatorControllerSwapped() { return HugAnimator.runtimeAnimatorController == hugOverrider; }

        public void ChangeInteractReceiver(InteractReceiver newReceiver, bool resetTriggerCollider = false)
        {
            if (HugReceiver != null)
            {
                HugReceiver.OnGainFocus -= EnableHug;
                HugReceiver.OnLoseFocus -= DisableHug;
            }
            HugReceiver = newReceiver;
            newReceiver.OnGainFocus += EnableHug;
            newReceiver.OnLoseFocus += DisableHug;

            if (!resetTriggerCollider) return;
            ChangeTriggerCollider(newReceiver._owCollider._collider);
        }
        public void ChangeTriggerCollider(Collider newCompareCollider)
        {
            if (hugTriggerCollider != null) Destroy(hugTriggerCollider.gameObject);
            AddTriggerCollider(newCompareCollider);
        }
        public void ChangePrimaryAnimator(Animator newAnimator, RuntimeAnimatorController newAnimatorController)
        {
            HugAnimator = newAnimator;
            initialRuntimeController = newAnimatorController;
            isAnimated = newAnimator != null && newAnimator.avatar.isHuman;

            if ((hugIK == null && newAnimator == null) || (hugIK != null && newAnimator != null && hugIK.gameObject == newAnimator.gameObject)) return;
            if (hugIK != null) Destroy(hugIK);
            if (newAnimator == null) return;
            hugIK = newAnimator.gameObject.AddComponent<HugIK>();
            hugIK.hugComponent = this;
        }
        public void ChangeSecondaryAnimator(Animator newAnimator) { SecondaryAnimator = newAnimator; }
        public void ChangeCharacterAnimController(CharacterAnimController newAnimController)
        {
            characterAnimController = newAnimController;
            lookSpring = newAnimController.lookSpring;
        }


        //Hug stuff
        private void EnableHug()
        {
            if (!enabled) return;
            Locator.GetPromptManager().AddScreenPrompt(hugPrompt, PromptPosition.Center, true);
            canHug = true;
        }

        private void DisableHug()
        {
            if (!enabled) return;
            Locator.GetPromptManager().RemoveScreenPrompt(hugPrompt);
            canHug = false;
        }

        public void OnAnimatorIK()
        {
            var position = Locator.GetPlayerCamera().transform.position;
            var currentWeightTarget = (sequenceInProgress || forceLookAtPlayer) ? 1f : 0f;
            currentLookTarget = lookSpring.Update(currentLookTarget, position, Time.deltaTime);
            currentLookWeight = Mathf.Lerp(currentLookWeight, currentWeightTarget, Time.deltaTime * 2.5f);
            if (characterAnimController == null || characterAnimController._currentLookWeight < currentLookWeight)
            {
                HugAnimator.SetLookAtPosition(currentLookTarget);
                HugAnimator.SetLookAtWeight(currentLookWeight);
            }
        }

        public void Update()
        {
            if (OWInput.GetInputMode() == InputMode.Character) hugPrompt.SetVisibility(true);
            else hugPrompt.SetVisibility(false);

            if (OWInput.GetInputMode() == InputMode.Character && canHug && OWInput.IsNewlyPressed(InputLibrary.interactSecondary)) BeginHugSequence();
        }


        //-----Hug sequence-----
        private void BeginHugSequence()
        {
            CancelPending();
            sequenceInProgress = true;
            LockPlayerControl(gameObject.transform, focusPoint);
            WalkingTowardsHugTarget = true;
            unstickRoutine = StartCoroutine(Unstick(unstickTime));
        }

        private IEnumerator Unstick(float delay)
        {
            yield return new WaitForSeconds(delay);
            Unstick();
        }

        private void Unstick()
        {
            WalkingTowardsHugTarget = false;
            UnlockPlayerControl();
            sequenceInProgress = false;
            HugModInstance.ModHelper.Console.WriteLine("Hug attempt failed: Target could not be reached.", MessageType.Warning);
        }

        public void OnTriggerStay(Collider collider) 
        { 
            if (collider.gameObject.CompareTag("Player") && WalkingTowardsHugTarget && sequenceInProgress) StartCoroutine(HugSequenceMain()); 
        }

        public IEnumerator HugSequenceMain()
        {
            CommenceHug();
            yield return null;
            if (isAnimated && HugAnimator.enabled)
            {
                var wasHugging = IsAnimatorControllerSwapped();
                while (HugAnimator.GetCurrentAnimatorClipInfo(0).Length == 0) yield return null;
                PlayHugAnimation(wasHugging);
                if (transitionHash != 0 && transitionClip != null && !wasHugging)
                {
                    yield return new WaitForSeconds(0.1f);
                    UnderlayTransition();
                }
            }
            while (OWInput.IsPressed(InputLibrary.interactSecondary)) yield return null;
            ConcludeHug();
            if (!isAnimated || hugTrigger == "react_None" || !HugAnimator.enabled) yield break;

            var hugLayer = HugAnimator.GetLayerIndex("Hug Layer");
            while (!HugAnimator.GetNextAnimatorStateInfo(hugLayer).IsName("end_hug")) yield return null;
            if (unstickRoutine != null) StopCoroutine(unstickRoutine);
            StartCoroutine(ResetAnimator(0.7f)); //min 0.5f
        }

        private void CommenceHug()
        {
            if (unstickRoutine != null) StopCoroutine(unstickRoutine);
            var height = GetPlayerTransform().InverseTransformPoint(gameObject.transform.TransformPoint(focusPoint)).y;
            if (height < 5 || height > 22)
            {
                CancelHugSequence();
                return;
            }
            WalkingTowardsHugTarget = false;
            OnHugStart?.Invoke();
            PlayerStartHug(height);
        }

        private void PlayHugAnimation(bool wasHugging)
        {
            var info = HugAnimator.GetCurrentAnimatorStateInfo(0);
            var time = info.normalizedTime;
            if (!wasHugging)
            {
                var clip = HugAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
                stateHash = info.shortNameHash;
                hugOverrider["clip_Placeholder01"] = clip;
                HugAnimator.runtimeAnimatorController = hugOverrider;
                HugAnimator.Play("Placeholder01", 0, time);

                HugAnimator.SetLayerWeight(HugAnimator.GetLayerIndex("Fullbody"), fullbodyReact ? 1 : 0);
                HugAnimator.SetLayerWeight(HugAnimator.GetLayerIndex("Keep Right Foot Pose"), keepFootAnimRight ? 1 : 0);
                HugAnimator.SetLayerWeight(HugAnimator.GetLayerIndex("Keep Left Foot Pose"), keepFootAnimLeft ? 1 : 0);
                HugAnimator.SetLayerWeight(HugAnimator.GetLayerIndex("Keep Right Hand Pose"), keepHandAnimRight ? 1 : 0);
                HugAnimator.SetLayerWeight(HugAnimator.GetLayerIndex("Keep Left Hand Pose"), keepHandAnimLeft ? 1 : 0);
            }
            SecondaryAnimator?.Play(SecondaryAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, time);
            HugAnimator.SetTrigger(hugTrigger);
        }

        private void UnderlayTransition()
        {
            OnUnderlayTransitionStart?.Invoke();
            stateHash = transitionHash;
            hugOverrider["clip_Placeholder02"] = transitionClip;
            HugAnimator.CrossFadeInFixedTime("Placeholder02", transitionTime, 0, -transitionTime);
        }

        private void ConcludeHug()
        {
            PlayerEndHug();
            if (!isAnimated || !HugAnimator.enabled)
            {
                OnHugFinish?.Invoke();
                sequenceInProgress = false;
                return;
            }
            HugAnimator.SetTrigger("end_hug");
            unstickRoutine = StartCoroutine(ResetAnimator(hugTrigger == "react_None" ? transitionTime + 0.2f : unstickTime));
            OnConcludingHug?.Invoke();
        }

        private IEnumerator ResetAnimator(float delay)
        {
            yield return new WaitForSeconds(delay);
            while (PlayerState._inConversation) yield return null;

            var time = HugAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            HugAnimator.runtimeAnimatorController = initialRuntimeController;
            HugAnimator.Play(stateHash, 0, time);
            if (HugAnimator.layerCount > 1 && HugAnimator.GetCurrentAnimatorStateInfo(1).shortNameHash == stateHash) HugAnimator.Play(stateHash, 1, time);
            SecondaryAnimator?.Play(SecondaryAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, time);
            OnHugFinish?.Invoke();
            sequenceInProgress = false;
        }


        public void CancelHugSequence()
        {
            if (!sequenceInProgress) return;
            CancelPending();
            if (WalkingTowardsHugTarget)
            {
                Unstick();
                return;
            }
            PlayerEndHug();
            if (isAnimated && IsAnimatorControllerSwapped())
            {
                var hugLayer = HugAnimator.GetLayerIndex("Hug Layer");
                if (!HugAnimator.GetNextAnimatorStateInfo(hugLayer).IsName("end_hug")) HugAnimator.CrossFadeInFixedTime("end_hug", transitionTime, hugLayer);
                StartCoroutine(ResetAnimator(transitionTime));
            }
            else
            {
                OnHugFinish?.Invoke();
                sequenceInProgress = false;
            }
        }

        private void CancelPending()
        {
            StopAllCoroutines();
            if (isAnimated)
            {
                if (!IsAnimatorControllerSwapped())
                {
                    foreach (var parameter in HugAnimator.parameters)
                    { if (parameter.type == AnimatorControllerParameterType.Trigger) HugAnimator.ResetTrigger(parameter.name); }
                }
                HugAnimator.ResetTrigger(hugTrigger);
                HugAnimator.ResetTrigger("end_hug");
            }
        }


        public void Remove()
        {
            if (hugTriggerCollider != null) Destroy(hugTriggerCollider.gameObject);
            if (hugIK != null) Destroy(hugIK);
            Destroy(this);
        }

        public void OnDestroy() { OnDestroyEvent?.Invoke(); }
    }
}
