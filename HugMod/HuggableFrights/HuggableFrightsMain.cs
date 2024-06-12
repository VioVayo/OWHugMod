using OWML.Common;
using OWML.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static HugMod.HugMod;

namespace HugMod.HuggableFrights
{
    public class HuggableFrightsMain
    {
        public static GhostBrain[] OwlBrains { get; private set; }

        public static GhostAction.Name HuggedActionName { get; private set; }
        public static GhostAction.Name WitnessedHugActionName { get; private set; }
        public static GhostAction.Name ReturnActionName { get; private set; }

        private static bool setupComplete = false;
        private static PopupMenu popup;
        private static event Action<bool> OnHuggableFrightsToggle;

        private static float owlReceiverRange = 10;

        private static string optionText = "Friendmaker Mode";
        private static string optionTooltipText = "Enables the making of more friends.";
        private static string popupText = 
            "This option is recommended for those who've already experienced the Echoes of the Eye DLC as it was intended, as it significantly alters gameplay. Do you wish to continue?";


        public static void HFSetup()
        {
            if (HugModInstance.ModHelper.Interaction.ModExists("Leadpogrommer.PeacefulGhosts")) return;
            //HF only handles Owl AI stuff to make them non-hostile when hugged, so if they're not hostile from the start the hug mechanic works as is and we don't need any of this

            HuggedActionName = EnumUtils.Create<GhostAction.Name>("Hugged");
            WitnessedHugActionName = EnumUtils.Create<GhostAction.Name>("WitnessedHug");
            ReturnActionName = EnumUtils.Create<GhostAction.Name>("Return");
            HuggableFrightsPatches.Apply();

            AddHuggableFrightsOption(); //add to initial title screen scene menu
            LoadManager.OnCompleteSceneLoad += (_, loadScene) => 
            { 
                if (loadScene == OWScene.SolarSystem || loadScene == OWScene.TitleScreen || loadScene == OWScene.EyeOfTheUniverse) AddHuggableFrightsOption(); //and to every new scene with a menu
                if (loadScene == OWScene.SolarSystem) OwlBrains = GameObject.FindObjectsOfType<GhostBrain>().ToArray();
            };

            setupComplete = true;
        }

        public static void HFOwlSetup(HugComponent hugComponent)
        {
            if (!setupComplete) return;

            var brain = hugComponent.gameObject.GetComponent<GhostBrain>();
            brain._actions = brain._actions.AddToArray(HuggedActionName, WitnessedHugActionName, ReturnActionName);
            brain._effects.OnGrabComplete += () => { hugComponent.SetHugEnabled(false); }; //reenabling this after the player gets kicked out is done in patches
            brain._controller.gameObject.AddComponent<GhostNavigation>();

            hugComponent.HugReceiver.SetInteractRange(owlReceiverRange);
            hugComponent.OnHugStart += () => { brain.ChangeAction(HuggedActionName); };
            hugComponent.OnHugFinish += () => { if (brain._currentAction.GetName() == HuggedActionName) brain._currentAction._enterTime = Time.time; };

            if (!Settings.FriendmakerModeEnabled) hugComponent.SetHugEnabled(false);
            OnHuggableFrightsToggle += hugComponent.SetHugEnabled;
            hugComponent.OnDestroyEvent += () => { OnHuggableFrightsToggle -= hugComponent.SetHugEnabled; };
        }

        public static string HFEnabledMessage(bool wasChanged)
        {
            return setupComplete ? $"{optionText} is" + (wasChanged ? " now: " : ": ") + (Settings.FriendmakerModeEnabled ? "Enabled" : "Disabled") : "";
        }


        private static void AddHuggableFrightsOption()
        {
            var optionsCanvas = GameObject.Find("OptionsCanvas");

            //add checkbox to settings menu
            var menuObj = optionsCanvas.FindInDescendants("MenuGameplayBasic").gameObject;
            var settingsOptionObj = menuObj.FindInDescendants("UIElement-ReducedFrights").gameObject;
            var popupPrefab = settingsOptionObj.GetComponentInChildren<ReducedFrightsPopup>()._popupPrefab;
            var pos = settingsOptionObj.transform.GetSiblingIndex() + 1;
            settingsOptionObj = GameObject.Instantiate(settingsOptionObj, settingsOptionObj.transform.parent);
            settingsOptionObj.name = "UIElement-HuggableFrights";
            settingsOptionObj.transform.SetSiblingIndex(pos);
            GameObject.Destroy(settingsOptionObj.GetComponentInChildren<LocalizedText>());
            GameObject.Destroy(settingsOptionObj.GetComponentInChildren<ReducedFrightsPopup>());

            var toggleElement = settingsOptionObj.GetComponent<ToggleElement>();
            toggleElement.Initialize(Settings.FriendmakerModeEnabled);
            toggleElement.SetDisplayText(optionText);
            toggleElement.SetTooltipText(UITextType.None);
            toggleElement._overrideTooltipText = optionTooltipText;

            //make keyboard-selectable
            var menu = menuObj.GetComponentInChildren<Menu>();
            menu._menuOptions = menu._menuOptions.AddToArray(toggleElement);
            Menu.SetVerticalNavigation(menu, menu._menuOptions);

            //toggle
            var toggleButton = settingsOptionObj.GetComponent<Button>();
            toggleButton.onClick.AddListener(() =>
            {
                if (toggleElement._value != 1) return; //the value is changed before this action is called so this checks for what it's toggled *to*
                if (popup == null)
                {
                    var gameObject = GameObject.Instantiate<GameObject>(popupPrefab);
                    popup = gameObject.GetComponentInChildren<PopupMenu>(true);
                    popup.OnPopupCancel += toggleElement.Toggle;
                }
                ScreenPrompt screenPrompt1 = new ScreenPrompt(InputLibrary.menuConfirm, UITextLibrary.GetString(UITextType.MenuConfirm), 0, ScreenPrompt.DisplayState.Normal, false);
                ScreenPrompt screenPrompt2 = new ScreenPrompt(InputLibrary.cancel, UITextLibrary.GetString(UITextType.MenuCancel), 0, ScreenPrompt.DisplayState.Normal, false);
                popup.EnableMenu(true);
                popup.SetUpPopup(popupText, InputLibrary.menuConfirm, InputLibrary.cancel, screenPrompt1, screenPrompt2);
            });

            //seeming duplicates are for button click and controller command each
            var menuView = optionsCanvas.transform.Find("SettingsMenuManagers").GetComponent<SettingsMenuView>();
            menuView._resetSettingsAction.OnSubmitAction += () => { if (toggleElement.isActiveAndEnabled && toggleElement._value == 1) toggleElement.Toggle(); };
            menuView._resetSettingsActionByCommand.OnSubmitAction += () => { if (toggleElement.isActiveAndEnabled && toggleElement._value == 1) toggleElement.Toggle(); };
            menuView._closeMenuAction.OnSubmitAction += () => { HuggableFrightsToggle(toggleElement._value == 1); };
            menuView._confirmCancelAction.OnMenuCancel += (_, _) => { HuggableFrightsToggle(toggleElement._value == 1); };
        }

        private static void HuggableFrightsToggle(bool enable)
        {
            if (enable == Settings.FriendmakerModeEnabled) return;
            Settings.FriendmakerModeEnabled = enable;
            OnHuggableFrightsToggle?.Invoke(enable);
            HugModInstance.ModHelper.Console.WriteLine(HFEnabledMessage(true), MessageType.Success);
            UpdateSettings();
        }
    }
}
