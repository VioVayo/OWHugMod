using HugMod.HuggableFrights;
using OWML.Common;
using System.Collections.Generic;
using UnityEngine;
using static HugMod.HugMod;

namespace HugMod.Targets
{
    public class TargetManager
    {
        public static HugComponent GabbroHug, SolanumHug, PrisonerHug;

        private static Dictionary<string, Target> travellers, villagers, friends, owls, partyOwls;

        private static GameObject owlColliderObj;
        private static GameObject proxyColliderObj; //every target runs through all of AddHugComponent as part of the foreach so proxyColliderObj is the same pre- and post-initialisation

        private static OWScene currentScene;


        public static void ApplyTargetPatches()
        {
            GabbroPatches.Apply();
            SolanumPatches.Apply();
            PrisonerPatches.Apply();
            GhostPatches.Apply();
        }

        public static void LoadHugTargetData()
        {
            travellers = HugModInstance.ModHelper.Storage.Load<Dictionary<string, Target>>("Targets/Travellers.json");
            villagers = HugModInstance.ModHelper.Storage.Load<Dictionary<string, Target>>("Targets/Villagers.json");
            friends = HugModInstance.ModHelper.Storage.Load<Dictionary<string, Target>>("Targets/Friends.json");
            owls = HugModInstance.ModHelper.Storage.Load<Dictionary<string, Target>>("Targets/Ghosts.json");
            partyOwls = HugModInstance.ModHelper.Storage.Load<Dictionary<string, Target>>("Targets/Partyghosts.json");
        }

        public static void SetUpHugTargets(OWScene loadScene)
        {
            currentScene = loadScene;

            foreach (var entry in travellers) AddHugComponent(entry.Value);

            if (currentScene == OWScene.EyeOfTheUniverse)
            {
                if (PlayerData.GetPersistentCondition("MET_SOLANUM")) AddHugComponent(friends["Eye_Solanum"]);
                if (!PlayerHasDLC || !PlayerData.GetPersistentCondition("MET_PRISONER")) return;
                AddHugComponent(friends["Eye_Prisoner_1"]);
                AddHugComponent(friends["Eye_Prisoner_2"]);
                return;
            }

            foreach (var entry in villagers) AddHugComponent(entry.Value);

            if (PlayerData.GetPersistentCondition("MET_SOLANUM")) AddHugComponent(friends["Solanum"]);
            var solanumEvent = GameObject.Find("Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            if (solanumEvent != null) solanumEvent.OnTouchRock += (int rockIndex) => { HugReenable(friends["Solanum"]); };

            if (!PlayerHasDLC) return;
            if (PlayerData.GetPersistentCondition("MET_PRISONER")) AddHugComponent(friends["Prisoner"]);
            var prisonerEvent = GameObject.Find("Prefab_IP_GhostBird_Prisoner/Ghostbird_IP_ANIM").GetComponent<PrisonerEffects>();
            if (prisonerEvent != null) prisonerEvent.OnReactToVisionAnimationComplete += () => { HugReenable(friends["Prisoner"]); };

            owlColliderObj = GameObject.Find("Prefab_IP_GhostBird_Micolash/Collider_Ghost");
            foreach (var entry in partyOwls) AddHugComponent(entry.Value);
            foreach (var entry in owls) AddHugComponent(entry.Value);
        }


        private static HugComponent AddHugComponent(Target target)
        {
            var targetObj = target.GetTargetObject();
            if (targetObj == null)
            {
                HugModInstance.ModHelper.Console.WriteLine($"Could not find target object of name \"{target.objectName}\"", MessageType.Error);
                return null;
            }
            var hugComponent = targetObj.AddComponent<HugComponent>();
            PreInitIndividualise(target, hugComponent);
            hugComponent.Initialise();
            hugComponent.SetPrompt(target.name);
            hugComponent.SetFocusPoint(target.GetFocusPoint());
            hugComponent.SetAnimationTrigger(target.hugTrigger);
            hugComponent.SetAnimationMasks(target.fullbodyReact, target.keepFootAnimRight, target.keepFootAnimLeft, target.keepHandAnimRight, target.keepHandAnimLeft);
            if (target.transitionHash != 0) hugComponent.SetUnderlayTransition(target.transitionClipName, target.transitionHash, target.transitionTime);
            Individualise(target, hugComponent);
            return hugComponent;
        }

        private static void HugReenable(Target target) //prevent loss of huggability after dialogue with Solanum and the Prisoner
        {
            var hugComponent = target.GetHugComponent() ?? AddHugComponent(target);
            if (hugComponent == null) return;
            var receiver = hugComponent.gameObject.GetComponentInChildren<InteractReceiver>();
            receiver.OnPressInteract -= hugComponent.gameObject.GetComponentInChildren<CharacterDialogueTree>().OnPressInteract;
            receiver.OnGainFocus += () => { Locator.GetPromptManager().RemoveScreenPrompt(receiver._screenPrompt); };
            receiver.EnableInteraction();
        }


        private static void PreInitIndividualise(Target target, HugComponent hugComponent)
        {
            if (target.isOwl && partyOwls.ContainsValue(target))
            {
                if (owlColliderObj != null) proxyColliderObj = GameObject.Instantiate(owlColliderObj, hugComponent.gameObject.transform);
            }
            if (target == friends["Prisoner"])
            {
                var receiverTransform = hugComponent.transform.Find("InteractReceiver");
                var receiverCollider = receiverTransform.gameObject.GetComponent<CapsuleCollider>();
                var lookTarget = receiverTransform.Find("ConversationLookTarget");
                var prisonerColliderTransform = hugComponent.gameObject.FindInDescendants("Collider_Prisoner", false);
                var prisonerCollider = prisonerColliderTransform.gameObject.GetComponent<CapsuleCollider>();

                lookTarget.SetParent(receiverTransform.parent, true);

                receiverTransform.SetParent(prisonerColliderTransform.parent);
                receiverTransform.localPosition = prisonerColliderTransform.localPosition;
                receiverTransform.localRotation = prisonerColliderTransform.localRotation;
                receiverTransform.localScale = prisonerColliderTransform.localScale;

                receiverCollider.center = prisonerCollider.center;
                receiverCollider.radius = prisonerCollider.radius;
                receiverCollider.height = prisonerCollider.height;
                receiverCollider.direction = prisonerCollider.direction;
            }
        }


        private static void Individualise(Target target, HugComponent hugComponent)
        {
            if (target == travellers["Riebeck"])
            {
                var collider = hugComponent.gameObject.transform.Find("CapsuleCollider")?.gameObject.GetComponent<Collider>();
                hugComponent.ChangeTriggerCollider(collider);
            }
            if (target.isAtEye && currentScene == OWScene.EyeOfTheUniverse)
            {
                AnimationClip nullClip = null;
                hugComponent.SetUnderlayTransition(nullClip, 0);

                if (target == travellers["Gabbro"]) hugComponent.SetFocusPoint(new(0.06f, 0.75f, -1.1f));
                if (target == friends["Eye_Solanum"])
                {
                    var collider = hugComponent.gameObject.transform.Find("Collider (2)")?.gameObject.GetComponent<Collider>();
                    hugComponent.ChangeTriggerCollider(collider); 
                }
                if (target == friends["Eye_Prisoner_1"]) 
                {
                    var prisonerEvent = hugComponent.gameObject.GetComponentInChildren<CharacterDialogueTree>();
                    if (prisonerEvent != null) prisonerEvent.OnEndConversation += () => { HugReenable(target); }; 
                }
                return;
            }
            if (target.isTraveller)
            {
                var controller = hugComponent.gameObject.GetComponentInChildren<TravelerController>();
                hugComponent.OnHugStart += controller.StartConversation;
                hugComponent.OnHugFinish += () =>
                {
                    controller._animator?.SetTrigger("Playing"); //Esker's Animator isn't assigned to their TravelerController
                    Locator.GetTravelerAudioManager().PlayAllTravelerAudio(target == travellers["Riebeck"] || target == travellers["Chert"] ? 0 : controller._delayToRestartAudio);
                };
            }
            if (target == travellers["Chert"])
            {
                var controller = hugComponent.gameObject.GetComponentInChildren<ChertTravelerController>();
                var moodMemory = controller._mood;
                hugComponent.OnHugStart += () =>
                {
                    if (hugComponent.IsAnimatorControllerSwapped()) return;
                    var mood = controller._mood;
                    if (mood != moodMemory) hugComponent.SetUnderlayTransition(mood == ChertMood.Catatonia ? "Chert_Dream" : "Chert_Chatter_" + mood.ToString(), target.transitionHash);
                    moodMemory = mood;
                };
                hugComponent.OnHugFinish += () =>
                {
                    var weight = (float)moodMemory;
                    controller._moodWeight = weight;
                    controller._animator.SetFloat("Mood", weight);
                };
            }
            if (target == travellers["Gabbro"])
            {
                GabbroHug = hugComponent;
                var controller = hugComponent.gameObject.GetComponentInChildren<GabbroTravelerController>();

                hugComponent.ChangePrimaryAnimator(controller._animator, controller._animator.runtimeAnimatorController);
                hugComponent.ChangeSecondaryAnimator(controller._hammockAnimator);

                hugComponent.OnHugStart += () => { controller._hammockAnimator.ResetTrigger("Playing"); };
                hugComponent.OnUnderlayTransitionStart += () =>
                {
                    hugComponent.GetUnderlayTransitionData(out float time, out int _, out AnimationClip _);
                    controller._hammockAnimator.CrossFadeInFixedTime(-414000389, time, 0, -time);
                };
                hugComponent.OnHugFinish += () => { controller._hammockAnimator.SetTrigger("Playing"); };
            }
            if (target == travellers["Esker"])
            {
                var controller = hugComponent.gameObject.GetComponentInChildren<EskerAnimController>();
                hugComponent.OnHugStart += () => { controller._isWhistling = false; };
                hugComponent.OnHugFinish += () => { controller._isWhistling = true; };
            }
            if (target == villagers["Hal_1"])
            {
                var effects = hugComponent.gameObject.transform.Find("Effects_HEA_HalCarving")?.gameObject;
                var audio = hugComponent.gameObject.transform.Find("AudioSource_HalCarving")?.gameObject;
                hugComponent.OnHugStart += () =>
                {
                    effects.SetActive(false);
                    audio.SetActive(false);
                };
                hugComponent.OnHugFinish += () =>
                {
                    effects.SetActive(true);
                    audio.SetActive(true);
                };

                var dialogue = hugComponent.gameObject.GetComponentInChildren<CharacterDialogueTree>();
                dialogue.OnEndConversation += Enable;
                hugComponent.SetHugEnabled(false);

                void Enable()
                {
                    dialogue.OnEndConversation -= Enable;
                    hugComponent.SetHugEnabled(true);
                }
            }
            if (target == villagers["Gneiss"])
            {
                var audio = hugComponent.gameObject.FindInDescendants("AudioSource_BanjoTuning")?.gameObject;
                hugComponent.OnHugStart += () => { audio.SetActive(false); };
                hugComponent.OnHugFinish += () => { audio.SetActive(true); };
            }
            if (target == villagers["Spinel"])
            {
                var rodObj = hugComponent.gameObject.transform.Find("Villager_HEA_Spinel_ANIM_Fishing/Villager_HEA_Spinel_ANIM_Fishing_ROD");
                var rodParent = rodObj?.parent;
                var rodAttach = hugComponent.gameObject.FindInDescendants("Spinel_Skin:Short_Rig_V01:LF_Arm_Wrist_Jnt")?.CreateChild("FishingRod_Attach");
                hugComponent.OnHugStart += () =>
                {
                    if (hugComponent.IsAnimatorControllerSwapped()) return;
                    hugComponent.SecondaryAnimator.enabled = false;
                    rodAttach.transform.position = rodParent.position;
                    rodAttach.transform.rotation = rodParent.rotation;
                    rodObj.SetParent(rodAttach.transform, false);
                };
                hugComponent.OnHugFinish += () =>
                {
                    rodObj.SetParent(rodParent, false);
                    hugComponent.SecondaryAnimator.enabled = true;
                };
            }
            if (target == villagers["Tuff"])
            {
                hugComponent.OnHugStart += () => { hugComponent.HugAnimator.SetTrigger("Talking"); };
                hugComponent.OnHugFinish += () => { hugComponent.HugAnimator.SetTrigger("Idle"); };
            }
            if (target == villagers["Tektite"])
            {
                var pickObj = hugComponent.gameObject.FindInDescendants("Props_HEA_Pickaxe");
                var pickParent = pickObj.parent;
                var pickAttach = hugComponent.gameObject.transform.CreateChild("Pickaxe_Attach");
                hugComponent.OnHugStart += () =>
                {
                    if (hugComponent.IsAnimatorControllerSwapped()) return;
                    pickAttach.transform.position = pickParent.position;
                    pickAttach.transform.rotation = pickParent.rotation;
                    pickObj.SetParent(pickAttach.transform, false);
                };
                hugComponent.OnHugFinish += () => { pickObj.SetParent(pickParent, false); };
            }
            if (target == villagers["Arkose"])
            {
                var controller = hugComponent.gameObject.GetComponentInChildren<KidRockController>();
                hugComponent.OnHugStart += () => { controller._throwingRock = true; };
                hugComponent.OnHugFinish += () => { controller._throwingRock = false; };
            }
            if (target == friends["Solanum"])
            {
                SolanumHug = hugComponent;
                hugComponent.SetLookAtPlayerEnabled(false);

                var controller = hugComponent.gameObject.GetComponentInChildren<SolanumAnimController>();
                var parameter1 = false;
                var parameter2 = false;
                var parameter3 = false;
                var parameter4 = false;
                hugComponent.OnHugStart += () =>
                {
                    parameter1 = controller._animator.GetBool("WatchingPlayer");
                    parameter2 = controller._animator.GetBool("ListeningToPlayer");
                    parameter3 = controller._animator.GetBool("WritingResponse");
                    parameter4 = controller._animator.GetBool("GestureOnFinishWriting");
                };
                hugComponent.OnHugFinish += () =>
                {
                    controller._animator.SetBool("WatchingPlayer", parameter1);
                    controller._animator.SetBool("ListeningToPlayer", parameter2);
                    controller._animator.SetBool("WritingResponse", parameter3);
                    controller._animator.SetBool("GestureOnFinishWriting", parameter4);
                };
            }
            if (target.isOwl)
            {
                if (partyOwls.ContainsValue(target))
                {
                    proxyColliderObj?.transform.SetParent(proxyColliderObj.transform.parent.Find("Ghostbird_IP_ANIM"), true);
                    return;
                }

                var controller = hugComponent.gameObject.GetComponentInChildren<GhostController>();
                hugComponent.OnHugStart += () => { controller.SetMovementPaused(true); };
                hugComponent.OnHugFinish += () => { controller.SetMovementPaused(false); };

                if (target == friends["Prisoner"]) PrisonerHug = hugComponent;
                else
                {
                    hugComponent.SetPrompt("Ghost");
                    HuggableFrightsMain.HFOwlSetup(hugComponent);
                }
            }
        }
    }
}
