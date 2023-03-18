using OWML.Common;
using OWML.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static HugMod.HugMod;
using static HugMod.HugModUtilities;

namespace HugMod.HuggableFrights
{
    public class HuggableFrightsMain
    {
        public static bool IsHuggableFrightsEnabled { get; private set; } = false;

        public static List<GhostBrain> OwlBrains { get; private set; } = new();

        public static GhostAction.Name HuggedActionName { get; private set; }
        public static GhostAction.Name WitnessedHugActionName { get; private set; }
        public static GhostAction.Name ReturnActionName { get; private set; }

        private static bool setupComplete = false;
        private static event Action<bool> OnHuggableFrightsToggle;

        private static float owlReceiverRange = 10;

        private static string optionText = "Friendmaker Mode";
        private static string optionTooltipText = "Enables the making of more friends.";
        private static string popupText = "This option is recommended for those who've already experienced the Echoes of the Eye DLC as it was intended, as it significantly alters gameplay. Do you wish to continue?";
        private static string popupConfirmText = "YES";
        private static string popupCancelText = "NO";


        public static void HFSetup()
        {
            if (HugModInstance.ModHelper.Interaction.ModExists("Leadpogrommer.PeacefulGhosts")) return;

            IsHuggableFrightsEnabled = HugModInstance.ModHelper.Storage.Load<bool>("settings.json");
            HuggedActionName = EnumUtils.Create<GhostAction.Name>("Hugged");
            WitnessedHugActionName = EnumUtils.Create<GhostAction.Name>("WitnessedHug");
            ReturnActionName = EnumUtils.Create<GhostAction.Name>("Return");
            HuggableFrightsPatches.Apply();

            AddHuggableFrightsOption();
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => 
            { 
                if (loadScene == OWScene.SolarSystem || loadScene == OWScene.TitleScreen || loadScene == OWScene.EyeOfTheUniverse) AddHuggableFrightsOption();
                if (loadScene == OWScene.SolarSystem) OwlBrains = GameObject.FindObjectsOfType<GhostBrain>().ToList();
            };

            setupComplete = true;
        }

        public static void HFOwlSetup(HugComponent hugComponent)
        {
            if (!setupComplete) return;

            var brain = hugComponent.gameObject.GetComponent<GhostBrain>();
            brain._actions = AddToArray(brain._actions, HuggedActionName, WitnessedHugActionName, ReturnActionName);
            brain._effects.OnGrabComplete += () => { hugComponent.SetHugEnabled(false); };

            hugComponent.gameObject.AddComponent<GhostNavigation>();
            hugComponent.HugReceiver._interactRange = owlReceiverRange;
            hugComponent.OnHugStart += () => { brain.ChangeAction(HuggedActionName); };
            hugComponent.OnHugFinish += () => { if (brain._currentAction.GetName() == HuggedActionName) brain._currentAction._enterTime = Time.time; };

            EnableOwlToggle(hugComponent);
        }

        public static string HFEnabledMessage(bool wasChanged)
        {
            return setupComplete ? $"{optionText} is" + (wasChanged ? " now: " : ": ") + (IsHuggableFrightsEnabled ? "Enabled" : "Disabled") : "";
        }


        private static void AddHuggableFrightsOption()
        {
            var optionsCanvas = GameObject.Find("OptionsCanvas");

            //add checkbox to settings menu
            var menuObj = optionsCanvas.FindInDescendants("MenuGameplayBasic").gameObject;
            var settingsOptionObj = menuObj.transform.Find("UIElement-ReducedFrights").gameObject;
            settingsOptionObj = GameObject.Instantiate(settingsOptionObj, settingsOptionObj.transform.parent);
            settingsOptionObj.name = "UIElement-HuggableFrights";
            GameObject.Destroy(settingsOptionObj.GetComponentInChildren<LocalizedText>());
            GameObject.Destroy(settingsOptionObj.GetComponentInChildren<ReducedFrightsPopup>());

            var toggleElement = settingsOptionObj.GetComponent<ToggleElement>();
            toggleElement.Initialize(IsHuggableFrightsEnabled);
            toggleElement.SetDisplayText(optionText);
            toggleElement.SetTooltipText(UITextType.None);
            toggleElement._overrideTooltipText = optionTooltipText;

            //make keyboard-selectable
            var menu = menuObj.GetComponentInChildren<Menu>();
            menu._menuOptions = AddToArray(menu._menuOptions, toggleElement);
            Menu.SetVerticalNavigation(menu, menu._menuOptions);

            //toggle
            var toggleButton = settingsOptionObj.GetComponent<Button>();
            toggleButton.onClick.AddListener(() =>
            {
                if (toggleElement._value != 1) return;
                var popup = HugModInstance.ModHelper.Menus.PopupManager.CreateMessagePopup(popupText, true, popupConfirmText, popupCancelText);
                popup.OnCancel += toggleElement.Toggle;
            });

            var resetButton = optionsCanvas.transform.Find("OptionsMenu-Panel/OptionsButtons/UIElement-ResetToDefaultsButton").GetComponent<Button>();
            resetButton.onClick.AddListener(() => { if (toggleElement._value == 1) toggleElement.Toggle(); });

            var saveButton = optionsCanvas.transform.Find("OptionsMenu-Panel/OptionsButtons/UIElement-SaveAndExit").GetComponent<Button>();
            saveButton.onClick.AddListener(() => { HuggableFrightsToggle(toggleElement._value == 1); });
        }

        private static void HuggableFrightsToggle(bool enable)
        {
            if (enable == IsHuggableFrightsEnabled) return;
            IsHuggableFrightsEnabled = enable;
            HugModInstance.ModHelper.Storage.Save(IsHuggableFrightsEnabled, "settings.json");
            OnHuggableFrightsToggle?.Invoke(enable);
            HugModInstance.ModHelper.Console.WriteLine(HFEnabledMessage(true), MessageType.Success);
        }

        private static void EnableOwlToggle(HugComponent hugComponent)
        {
            if (!IsHuggableFrightsEnabled) hugComponent.SetHugEnabled(false);
            OnHuggableFrightsToggle += hugComponent.SetHugEnabled;
            hugComponent.OnDestroyEvent += () => { OnHuggableFrightsToggle -= hugComponent.SetHugEnabled; };
        }
    }
}
