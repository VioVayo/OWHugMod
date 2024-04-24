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

        private static OWScene currentScene;

        private static GameObject owlColliderObj;
        private static GameObject proxyOwlColliderObj;


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
                if (PlayerData.GetPersistentCondition("MET_SOLANUM")) AddHugComponent(friends["Solanum_Eye"]);
                if (!PlayerHasDLC || !PlayerData.GetPersistentCondition("MET_PRISONER")) return;
                AddHugComponent(friends["Prisoner_Eye_1"]);
                AddHugComponent(friends["Prisoner_Eye_2"]);
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
            Individualise_PreInit(target, hugComponent);
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
            var receiver = hugComponent.HugReceiver;
            if (receiver == null || receiver._owCollider._active) return; //this is meant to reenable the receiver without reenabling dialogue, so don't do the thing if everything is still enabled
            receiver.OnPressInteract -= hugComponent.gameObject.GetComponentInChildren<CharacterDialogueTree>().OnPressInteract;
            receiver.OnGainFocus += () => { Locator.GetPromptManager().RemoveScreenPrompt(receiver._screenPrompt); };
            receiver.EnableInteraction();
        }


        private static void Individualise_PreInit(Target target, HugComponent hugComponent)
        {
            if (target.isAtEye && currentScene == OWScene.EyeOfTheUniverse) return; //some object names are shared between scenes, this ensures special treatment for those present in both at the Eye only
            if (target == travellers["Gabbro"]) GabbroHug = hugComponent;
            if (target == friends["Solanum"]) SolanumHug = hugComponent;
            if (target == friends["Prisoner"])
            {
                PrisonerHug = hugComponent;
                Individualise_Prisoner_PreInit(target, hugComponent);
            }
            if (target.isPartyOwl) Individualise_PartyOwls_PreInit(target, hugComponent);
        }
        private static void Individualise(Target target, HugComponent hugComponent)
        {
            if (target.isAtEye && currentScene == OWScene.EyeOfTheUniverse)
            {
                Individualise_AtEye(target, hugComponent);
                return;
            }
            if (target.isTraveller) Individualise_Travellers(target, hugComponent);
            if (target == travellers["Riebeck"]) Individualise_Riebeck(target, hugComponent);
            if (target == travellers["Chert"]) Individualise_Chert(target, hugComponent);
            if (target == travellers["Gabbro"]) Individualise_Gabbro(target, hugComponent);
            if (target == travellers["Esker"]) Individualise_Esker(target, hugComponent);
            if (target == villagers["Hal_1"]) Individualise_Hal_1(target, hugComponent);
            if (target == villagers["Gneiss"]) Individualise_Gneiss(target, hugComponent);
            if (target == villagers["Spinel"]) Individualise_Spinel(target, hugComponent);
            if (target == villagers["Tuff"]) Individualise_Tuff(target, hugComponent);
            if (target == villagers["Tektite"]) Individualise_Tektite(target, hugComponent);
            if (target == villagers["Arkose"]) Individualise_Arkose(target, hugComponent);
            if (target == friends["Solanum"]) Individualise_Solanum(target, hugComponent);
            if (target.isOwl) Individualise_Owls(target, hugComponent);
            if (target.isPartyOwl) Individualise_PartyOwls(target, hugComponent);
        }

        private static void Individualise_AtEye(Target target, HugComponent hugComponent)
        {
            AnimationClip nullClip = null;
            hugComponent.SetUnderlayTransition(nullClip, 0);

            TravelerEyeController eyeController = null;
            var go = hugComponent.gameObject;
            while (eyeController == null && go.name != "Campsite")
            {
                go.TryGetComponent(out eyeController); //some controllers are on the same GO, some on a GO higher, choice dialogue Prisoner doesn't have one
                go = go.transform.parent?.gameObject; //so we're checking up to the GO all of the travellers are children of
            }
            if (eyeController != null)
            {
                var isPlaying = false;
                eyeController.OnStartPlaying += () => { isPlaying = true; };
                eyeController.OnStopPlaying += () => { isPlaying = false; };
                hugComponent.OnHugFinish += () =>
                {
                    if (!isPlaying) return;
                    hugComponent.HugAnimator.SetBool("Playing", true); //if music was triggered during the out transition, the controller swap back will have overridden the music play trigger
                    hugComponent.HugAnimator.CrossFadeInFixedTime("PlayingInstrument", 0.25f, -1, eyeController._signal.GetOWAudioSource().time);
                };
            }

            if (target == travellers["Riebeck"]) Individualise_Riebeck(target, hugComponent);
            if (target == travellers["Chert"]) Individualise_Chert_Eye(target, hugComponent);
            if (target == travellers["Gabbro"]) Individualise_Gabbro_Eye(target, hugComponent);
            if (target == travellers["Esker"]) Individualise_Esker_Eye(target, hugComponent);
            if (target == friends["Solanum_Eye"]) Individualise_Solanum_Eye(target, hugComponent);
            if (target == friends["Prisoner_Eye_1"]) Individualise_Prisoner_Eye_1(target, hugComponent);
            if (target == friends["Prisoner_Eye_2"]) Individualise_Prisoner_Eye_2(target, hugComponent);
        }
        private static void Individualise_Travellers(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.TryGetComponentInChildren(out TravelerController controller))
            {
                hugComponent.OnHugStart += controller.StartConversation;
                hugComponent.OnHugFinish += () =>
                {
                    controller._animator.Exists()?.SetTrigger("Playing"); //Esker's Animator isn't assigned to their TravelerController
                    Locator.GetTravelerAudioManager().PlayAllTravelerAudio(target == travellers["Riebeck"] || target == travellers["Chert"] ? 0 : controller._delayToRestartAudio);
                };
            }
        }
        private static void Individualise_Riebeck(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.transform.Find("CapsuleCollider")?.gameObject.TryGetComponent(out CapsuleCollider collider) ?? false)
            {
                collider.radius += 0.17f;
                hugComponent.ChangeTriggerCollider(collider);
            }
        }
        private static void Individualise_Chert(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.TryGetComponentInChildren(out ChertTravelerController controller))
            {
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
        }
        private static void Individualise_Chert_Eye(Target target, HugComponent hugComponent)
        {
            if (hugComponent.transform.Find("Collider")?.gameObject.TryGetComponent(out Collider collider) ?? false) hugComponent.ChangeTriggerCollider(collider);
        }
        private static void Individualise_Gabbro(Target target, HugComponent hugComponent)
        {
            if (hugComponent.HugReceiver.Exists()?.gameObject.transform.Find("Hug_TriggerCollider")?.TryGetComponent(out SphereCollider collider) ?? false)
            {
                collider.center = collider.gameObject.transform.InverseTransformPoint(hugComponent.gameObject.transform.TransformPoint(target.GetFocusPoint()));
                collider.radius = 1;
            }

            if (hugComponent.gameObject.TryGetComponentInChildren(out GabbroTravelerController controller))
            {
                hugComponent.ChangePrimaryAnimator(controller._animator);
                hugComponent.ChangeSecondaryAnimator(controller._hammockAnimator);

                hugComponent.OnHugStart += () => { controller._hammockAnimator.ResetTrigger("Playing"); };
                hugComponent.OnUnderlayTransitionStart += () =>
                {
                    hugComponent.GetUnderlayTransitionData(out float time, out int _, out AnimationClip _);
                    controller._hammockAnimator.CrossFadeInFixedTime(-414000389, time, 0, -time);
                };
                hugComponent.OnHugFinish += () => { controller._hammockAnimator.SetTrigger("Playing"); };
            }
        }
        private static void Individualise_Gabbro_Eye(Target target, HugComponent hugComponent)
        {
            hugComponent.SetFocusPoint(new(0.06f, 0.75f, -1.1f));
            if (hugComponent.transform.parent.Find("Collider (1)")?.gameObject.TryGetComponent(out Collider collider) ?? false) hugComponent.ChangeTriggerCollider(collider);
        }
        private static void Individualise_Esker(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.TryGetComponentInChildren(out EskerAnimController controller))
            {
                hugComponent.OnHugStart += () => { controller._isWhistling = false; };
                hugComponent.OnHugFinish += () => { controller._isWhistling = true; };
            }
        }
        private static void Individualise_Esker_Eye(Target target, HugComponent hugComponent)
        {
            if (hugComponent.transform.parent.Find("Collider (1)")?.gameObject.TryGetComponent(out Collider collider) ?? false) hugComponent.ChangeTriggerCollider(collider);
        }
        private static void Individualise_Hal_1(Target target, HugComponent hugComponent)
        {
            var effects = hugComponent.gameObject.transform.Find("Effects_HEA_HalCarving")?.gameObject;
            var audio = hugComponent.gameObject.transform.Find("AudioSource_HalCarving")?.gameObject;
            hugComponent.OnHugStart += () =>
            {
                effects.Exists()?.SetActive(false);
                audio.Exists()?.SetActive(false);
            };
            hugComponent.OnHugFinish += () =>
            {
                effects.Exists()?.SetActive(true);
                audio.Exists()?.SetActive(true);
            };

            if (hugComponent.gameObject.TryGetComponentInChildren(out CharacterDialogueTree dialogue))
            {
                dialogue.OnEndConversation += Enable;
                hugComponent.SetHugEnabled(false);
            }

            void Enable()
            {
                dialogue.OnEndConversation -= Enable;
                hugComponent.SetHugEnabled(true);
            }
        }
        private static void Individualise_Gneiss(Target target, HugComponent hugComponent)
        {
            var audio = hugComponent.gameObject.FindInDescendants("AudioSource_BanjoTuning")?.gameObject;

            hugComponent.OnHugStart += () => { audio.Exists()?.SetActive(false); };
            hugComponent.OnHugFinish += () => { audio.Exists()?.SetActive(true); };
        }
        private static void Individualise_Spinel(Target target, HugComponent hugComponent)
        {
            var rodObj = hugComponent.gameObject.transform.Find("Villager_HEA_Spinel_ANIM_Fishing/Villager_HEA_Spinel_ANIM_Fishing_ROD");
            var rodParent = rodObj?.parent;
            var rodAttach = hugComponent.gameObject.FindInDescendants("Spinel_Skin:Short_Rig_V01:LF_Arm_Wrist_Jnt")?.CreateChild("FishingRod_Attach");
            var rodAnimator = hugComponent.SecondaryAnimator;
            if (rodParent == null || rodAttach == null || rodAnimator == null) return;

            hugComponent.OnHugStart += () =>
            {
                if (hugComponent.IsAnimatorControllerSwapped()) return;
                rodAnimator.enabled = false;
                rodAttach.transform.position = rodParent.position;
                rodAttach.transform.rotation = rodParent.rotation;
                rodObj.SetParent(rodAttach.transform, false);
            };
            hugComponent.OnHugFinish += () =>
            {
                rodObj.SetParent(rodParent, false);
                rodAnimator.enabled = true;
            };
        }
        private static void Individualise_Tuff(Target target, HugComponent hugComponent)
        {
            var animator = hugComponent.HugAnimator;

            hugComponent.OnHugStart += () => { animator.Exists()?.SetTrigger("Talking"); };
            hugComponent.OnHugFinish += () => { animator.Exists()?.SetTrigger("Idle"); };
        }
        private static void Individualise_Tektite(Target target, HugComponent hugComponent)
        {
            var pickObj = hugComponent.gameObject.FindInDescendants("Props_HEA_Pickaxe");
            var pickParent = pickObj?.parent;
            var pickAttach = hugComponent.gameObject.CreateChild("Pickaxe_Attach");
            if (pickParent == null || pickAttach == null) return;

            hugComponent.OnHugStart += () =>
            {
                if (hugComponent.IsAnimatorControllerSwapped()) return;
                pickAttach.transform.position = pickParent.position;
                pickAttach.transform.rotation = pickParent.rotation;
                pickObj.SetParent(pickAttach.transform, false);
            };
            hugComponent.OnHugFinish += () => { pickObj.SetParent(pickParent, false); };
        }
        private static void Individualise_Arkose(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.TryGetComponentInChildren(out KidRockController controller))
            {
                hugComponent.OnHugStart += () => { controller._throwingRock = true; };
                hugComponent.OnHugFinish += () => { controller._throwingRock = false; };
            }
        }
        private static void Individualise_Solanum(Target target, HugComponent hugComponent)
        {
            hugComponent.SetLookAtPlayerEnabled(false);

            if (hugComponent.gameObject.TryGetComponentInChildren(out SolanumAnimController controller))
            {
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
        }
        private static void Individualise_Solanum_Eye(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.transform.Find("Collider (2)")?.gameObject.TryGetComponent(out Collider collider) ?? false) hugComponent.ChangeTriggerCollider(collider);
        }
        private static void Individualise_Prisoner_PreInit(Target target, HugComponent hugComponent)
        {
            var prisonerColliderTransform = hugComponent.gameObject.FindInDescendants("Collider_Prisoner", false);
            var prisonerCollider = prisonerColliderTransform?.gameObject.GetComponent<CapsuleCollider>();
            var receiverTransform = hugComponent.transform.Find("InteractReceiver");
            var receiverCollider = receiverTransform?.gameObject.GetComponent<CapsuleCollider>();

            if (prisonerCollider == null || receiverCollider == null) return; 
            var lookTarget = receiverTransform.Find("ConversationLookTarget");
            lookTarget?.SetParent(receiverTransform.parent, true);

            receiverTransform.SetParent(prisonerColliderTransform.parent);
            receiverTransform.localPosition = prisonerColliderTransform.localPosition;
            receiverTransform.localRotation = prisonerColliderTransform.localRotation;
            receiverTransform.localScale = prisonerColliderTransform.localScale;

            receiverCollider.center = prisonerCollider.center;
            receiverCollider.radius = prisonerCollider.radius;
            receiverCollider.height = prisonerCollider.height;
            receiverCollider.direction = prisonerCollider.direction;
        }
        private static void Individualise_Prisoner_Eye_1(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.TryGetComponentInChildren(out CharacterDialogueTree prisonerEvent)) prisonerEvent.OnEndConversation += () => { HugReenable(target); };
        }
        private static void Individualise_Prisoner_Eye_2(Target target, HugComponent hugComponent)
        {
            var colliderObj = hugComponent.transform.Find("Collider (2)");
            if (colliderObj != null && colliderObj.gameObject.TryGetComponent(out CapsuleCollider collider))
            {
                colliderObj.localPosition = Vector3.zero;
                collider.center = new(0, 1.5f, 0);
                collider.height = 3;
                collider.radius = 0.8f;

                hugComponent.ChangeTriggerCollider(collider);
            }
        }
        private static void Individualise_Owls(Target target, HugComponent hugComponent)
        {
            if (hugComponent.gameObject.TryGetComponentInChildren(out GhostController controller))
            {
                hugComponent.OnHugStart += () => { controller.SetMovementPaused(true); };
                hugComponent.OnHugFinish += () => { controller.SetMovementPaused(false); };
            }

            if (target == friends["Prisoner"]) return;
            if (!Settings.SillyGhostNames) hugComponent.SetPrompt("Ghost");
            HuggableFrightsMain.HFOwlSetup(hugComponent);
        }
        private static void Individualise_PartyOwls_PreInit(Target target, HugComponent hugComponent)
        {
            if (owlColliderObj == null) owlColliderObj = GameObject.Find("Prefab_IP_GhostBird_Micolash/Collider_Ghost");
            if (owlColliderObj != null) proxyOwlColliderObj = GameObject.Instantiate(owlColliderObj, hugComponent.gameObject.transform);
            //every target runs through all of AddHugComponent as part of the foreach so proxyOwlColliderObj is the same per target pre- and post-initialisation
        }
        private static void Individualise_PartyOwls(Target target, HugComponent hugComponent)
        {
            proxyOwlColliderObj.Exists()?.transform.SetParent(proxyOwlColliderObj.transform.parent.Find("Ghostbird_IP_ANIM"), true);
        }
    }
}
