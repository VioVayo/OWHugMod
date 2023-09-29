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
        private Vector3 focusPoint = new(0, 0, 0);
        private string hugTrigger = "react_None";
        private bool fullbodyReact = true, keepFootAnimRight = false, keepFootAnimLeft = false, keepHandAnimRight = false, keepHandAnimLeft = false;

        private float transitionTime = 0.5f;
        private int transitionHash;
        private AnimationClip transitionClip;

        //working variables
        private Vector3 currentLookTarget;
        private float currentLookWeight;
        private int stateHash;
        private bool canHug = false, sequenceInProgress = false;

        private static Coroutine unstickRoutine;
        private static float unstickTime = 3;

        //proxies
        private HugTriggerCollider hugTriggerCollider;
        private HugIK hugIK;
        private bool forceLookAtPlayer = false;

        //events
        public event Action OnHugStart;
        public event Action OnUnderlayTransitionStart;
        public event Action OnConcludingHug;
        public event Action OnHugFinish;
        public event Action OnDestroyEvent;


        private void Awake() { SetHugEnabled(false); } //don't call Update before HugComponent is initialised in case it's not initialised immediately when added or initialisation fails

        public void Initialise()
        {
            var flag = gameObject.activeSelf;
            gameObject.SetActive(true); //avoid nullrefs on by default deactivated GOs (like the Collector)

            HugReceiver = gameObject.GetComponentInChildren<InteractReceiver>();
            if (HugReceiver == null)
            {
                var receiverCollider = gameObject.GetComponentInChildren<OWCollider>();
                if (receiverCollider == null)
                {
                    HugModInstance.ModHelper.Console.WriteLine($"HugComponent on object \"{gameObject.name}\" couldn't create InteractReceiver, failed to find OWCollider in children of object.", MessageType.Error);
                    gameObject.SetActive(flag);
                    return;
                }
                if (receiverCollider.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) receiverCollider.gameObject.layer = LayerMask.NameToLayer("Default");
                HugReceiver = receiverCollider.gameObject.AddComponent<InteractReceiver>();
                HugReceiver.OnGainFocus += () => { Locator.GetPromptManager().RemoveScreenPrompt(HugReceiver._screenPrompt); };
                HugReceiver.SetInteractionEnabled(true);
            }
            if (!AddTriggerCollider(HugReceiver._owCollider._collider)) //prints an error if it fails
            {
                gameObject.SetActive(flag);
                return;
            } 
            //only progress if both a valid InteractReceiver and trigger Collider exist, otherwise consider initialisation failed

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
                HugAnimator = gameObject.GetComponentInChildren<Animator>();
                lookSpring = new(50, 14, 1);
            }

            if (IsAnimated())
            {
                initialRuntimeController = HugAnimator.runtimeAnimatorController;
                hugIK = HugAnimator.gameObject.AddComponent<HugIK>();
                hugIK.SetHugComponent(this);
            }

            hugOverrider.runtimeAnimatorController = AltRuntimeController;
            SetHugEnabled(true);
            gameObject.SetActive(flag);
        }

        private bool AddTriggerCollider(Collider compareCollider)
        {
            if (compareCollider == null)
            {
                HugModInstance.ModHelper.Console.WriteLine($"HugComponent on object \"{gameObject.name}\" couldn't create trigger Collider, Collider used for comparison is Null.", MessageType.Error);
                return false; 
            }

            var triggerObj = compareCollider.gameObject.CreateChild("Hug_TriggerCollider");
            triggerObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            hugTriggerCollider = triggerObj.AddComponent<HugTriggerCollider>();
            hugTriggerCollider.SetHugComponent(this);

            if (compareCollider is CapsuleCollider capsuleCollider) AddCapsuleTrigger(capsuleCollider, triggerObj);
            else if (compareCollider is SphereCollider sphereCollider) AddSphereTrigger(sphereCollider, triggerObj);
            else if (compareCollider is BoxCollider boxCollider) AddBoxTrigger(boxCollider, triggerObj);
            else if (compareCollider is MeshCollider meshCollider) AddMeshTrigger(meshCollider, triggerObj);
            else
            {
                hugTriggerCollider.Remove();
                HugModInstance.ModHelper.Console.WriteLine($"HugComponent on object \"{gameObject.name}\" couldn't create trigger Collider, comparison Collider type is not supported.", MessageType.Error);
                return false;
            }
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
        public void SetHugEnabled(bool enable) { enabled = enable; }
        public void SetLookAtPlayerEnabled(bool enable) 
        {
            if (hugIK != null) hugIK.enabled = enable; 
            else HugModInstance.ModHelper.Console.WriteLine($"Couldn't find HugIK Component associated with object \"{gameObject.name}\".", MessageType.Error);
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
            if (initialRuntimeController != null)
            {
                this.transitionTime = transitionTime;
                this.transitionHash = transitionHash;
                this.transitionClip = Array.Find(initialRuntimeController.animationClips, element => element.name == transitionClipName);
            }
            else HugModInstance.ModHelper.Console.WriteLine($"Couldn't find RuntimeAnimatorController for object \"{HugAnimator.gameObject.name}\".", MessageType.Error);
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

        public bool IsAnimated() => HugAnimator != null && HugAnimator.avatar.isHuman;
        public bool IsSequenceInProgress() => sequenceInProgress;
        public bool IsAnimatorControllerSwapped() => (HugAnimator != null) && (HugAnimator.runtimeAnimatorController == hugOverrider);

        public void ChangeInteractReceiver(InteractReceiver newReceiver, bool resetTriggerCollider = false)
        {
            if (HugReceiver != null)
            {
                HugReceiver.OnGainFocus -= EnableHug;
                HugReceiver.OnLoseFocus -= DisableHug;
            }
            HugReceiver = newReceiver;
            if (newReceiver == null) return;
            newReceiver.OnGainFocus += EnableHug;
            newReceiver.OnLoseFocus += DisableHug;

            if (!resetTriggerCollider) return;
            ChangeTriggerCollider(newReceiver._owCollider._collider);
        }
        public void ChangeTriggerCollider(Collider newCompareCollider)
        {
            hugTriggerCollider.Exists()?.Remove();
            AddTriggerCollider(newCompareCollider);
        }
        public void ChangePrimaryAnimator(Animator newAnimator, bool resetAnimatorController = true)
        {
            HugAnimator = newAnimator;
            if (resetAnimatorController && IsAnimated()) initialRuntimeController = newAnimator.runtimeAnimatorController;

            if ((hugIK == null && !IsAnimated()) || (hugIK != null && IsAnimated() && hugIK.gameObject == newAnimator.gameObject)) return;
            hugIK.Exists()?.Remove();
            if (IsAnimated()) hugIK = newAnimator.gameObject.AddComponent<HugIK>();
            hugIK.Exists()?.SetHugComponent(this);
        }
        public void ChangeAnimatorController(RuntimeAnimatorController newAnimatorController) { initialRuntimeController = newAnimatorController; }
        public void ChangeSecondaryAnimator(Animator newAnimator) { SecondaryAnimator = newAnimator; }
        public void ChangeCharacterAnimController(CharacterAnimController newCharacterAnimController)
        {
            characterAnimController = newCharacterAnimController;
            lookSpring = newCharacterAnimController.Exists()?.lookSpring ?? new(50, 14, 1);
        }


        //Hug stuff
        private void OnEnable()
        {
            if (HugReceiver != null && HugReceiver._focused) EnableHug();
        }

        private void OnDisable()
        {
            if (HugReceiver != null && HugReceiver._focused) DisableHug(); //might be easier to check for canHug here but the symmetry with OnEnable is nice
            if (sequenceInProgress) CancelHugSequence();
        }

        private void EnableHug()
        {
            if (!enabled) return;
            Locator.GetPromptManager()?.AddScreenPrompt(hugPrompt, PromptPosition.Center, OWInput.IsInputMode(InputMode.Character));
            canHug = true;
        }

        private void DisableHug()
        {
            Locator.GetPromptManager()?.RemoveScreenPrompt(hugPrompt);
            canHug = false;
        }

        public void OnAnimatorIK() //called via proxy even if HugComponent is disabled (so no jumpy head animation when en- or disabling HugComponent)
        {
            if (!IsAnimated() || PlayerCamera == null) return;
            var position = PlayerCamera.transform.position;
            var currentWeightTarget = (enabled && (sequenceInProgress || forceLookAtPlayer)) ? 1f : 0f;
            currentLookTarget = lookSpring.Update(currentLookTarget, position, Time.deltaTime);
            currentLookWeight = Mathf.Lerp(currentLookWeight, currentWeightTarget, Time.deltaTime * 2.5f);
            if (characterAnimController == null || characterAnimController._currentLookWeight < currentLookWeight)
            {
                HugAnimator.SetLookAtPosition(currentLookTarget);
                HugAnimator.SetLookAtWeight(currentLookWeight);
            }
        }

        private void Update()
        {
            hugPrompt.SetVisibility(OWInput.IsInputMode(InputMode.Character));

            if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character) && canHug) BeginHugSequence();
        }


        //-----Hug sequence-----
        private void BeginHugSequence()
        {
            CancelPending();
            sequenceInProgress = true;
            LockPlayerControl(gameObject.transform, focusPoint);
            WalkingTowardsHugTarget = true;
            unstickRoutine = StartCoroutine(CancelHugSequence(unstickTime)); //force cancel sequence after a certain time in case target is unreachable
        }

        public void OnTriggerStay(Collider collider) //called via proxy even if HugComponent is disabled
        {
            if (!enabled) return;
            if (collider.gameObject.CompareTag("Player") && WalkingTowardsHugTarget && sequenceInProgress) StartCoroutine(HugSequenceMain()); 
        }

        private IEnumerator HugSequenceMain()
        {
            if (unstickRoutine != null) StopCoroutine(unstickRoutine); //no unstick necessary because target was successfully reached
            CommenceHug();
            yield return null;
            if (IsAnimated() && HugAnimator.enabled && hugTrigger != "react_None")
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
            if (!IsAnimatorControllerSwapped()) yield break; //everything after this has only to do with resetting the animator

            var hugLayer = HugAnimator.GetLayerIndex("Hug Layer");
            while (!HugAnimator.GetNextAnimatorStateInfo(hugLayer).IsName("end_hug")) //this checks for a specific upcoming animation state, it shouldn't change from next to current before this check -
            {                                                                       //just in case though unstick will force ResetAnimator() after a certain time
                if (!sequenceInProgress) yield break; //exit this coroutine if the animator was already reset by the unstick
                yield return null; 
            }
            if (unstickRoutine != null) StopCoroutine(unstickRoutine); //no unstick necessary because state check was successful
            StartCoroutine(ResetAnimator(0.7f)); //min 0.5f to give enough time for the animation transition to play
        }

        private void CommenceHug()
        {
            var height = GetPlayerObjectTransform().InverseTransformPoint(gameObject.transform.TransformPoint(focusPoint)).y;
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
            SecondaryAnimator.Exists()?.Play(SecondaryAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, time);
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
            if (!IsAnimatorControllerSwapped() && !(IsAnimated() && HugAnimator.enabled))
            {
                OnHugFinish?.Invoke();
                sequenceInProgress = false;
                return;
            }
            if (IsAnimatorControllerSwapped()) 
            { 
                HugAnimator.SetTrigger("end_hug");
                unstickRoutine = StartCoroutine(ResetAnimator(unstickTime)); //in case ResetAnimator() is not triggered by the animator state transition as it should be, see HugSequenceMain()
            } 
            else StartCoroutine(ResetAnimator(1.2f)); //if controller wasn't swapped but the target is still animated there should be a little delay for look IK to persist through
            OnConcludingHug?.Invoke();
        }

        private IEnumerator ResetAnimator(float delay)
        {
            yield return new WaitForSeconds(delay); //if conversation was started in the middle of the transition taking place during this delay -
            while (PlayerState._inConversation) yield return null; //keep animators swapped until the conversation concludes to avoid jumpy animation transitions
            // THIS CAN CURRENTLY CAUSE ANIMATION ISSUES AT THE EYE WHEN THE BAND STARTS PLAYING, note to self I'll try to fix this when I have the brain power
            var time = HugAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (IsAnimatorControllerSwapped())
            {
                HugAnimator.runtimeAnimatorController = initialRuntimeController;
                HugAnimator.Play(stateHash, 0, time);
                if (HugAnimator.layerCount > 1 && HugAnimator.GetCurrentAnimatorStateInfo(1).shortNameHash == stateHash) HugAnimator.Play(stateHash, 1, time); //some of them have two, idk
            }
            SecondaryAnimator.Exists()?.Play(SecondaryAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, time);
            OnHugFinish?.Invoke();
            sequenceInProgress = false;
        }


        private IEnumerator CancelHugSequence(float delay)
        {
            yield return new WaitForSeconds(delay);
            CancelHugSequence();
        }

        public void CancelHugSequence()
        {
            if (!sequenceInProgress) return;
            CancelPending();
            if (WalkingTowardsHugTarget)
            {
                WalkingTowardsHugTarget = false;
                UnlockPlayerControl();
                sequenceInProgress = false;
                HugModInstance.ModHelper.Console.WriteLine("Hug attempt failed: Target could not be reached.", MessageType.Warning);
                return;
            }
            PlayerEndHug();
            if (IsAnimated() && IsAnimatorControllerSwapped())
            {
                if (HugAnimator.gameObject.activeInHierarchy)
                {
                    var hugLayer = HugAnimator.GetLayerIndex("Hug Layer");
                    if (!HugAnimator.GetNextAnimatorStateInfo(hugLayer).IsName("end_hug")) HugAnimator.CrossFadeInFixedTime("end_hug", transitionTime, hugLayer);
                    hugIK.StartCoroutine(ResetAnimator(transitionTime)); //start this on a separate MonoBehaviour on the same GO as the animator, in case this method was triggered by HugComponent being destroyed
                    return;
                }
                else HugAnimator.runtimeAnimatorController = initialRuntimeController;
            }
            OnHugFinish?.Invoke();
            sequenceInProgress = false;
        }

        private void CancelPending()
        {
            StopAllCoroutines();
            if (IsAnimated())
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


        private void OnDestroy()
        {
            if (HugReceiver != null)
            {
                HugReceiver.OnGainFocus -= EnableHug;
                HugReceiver.OnLoseFocus -= DisableHug;
            }
            OnDestroyEvent?.Invoke(); 
        }
    }
}
