using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using HugMod.HuggableFrights;
using HugMod.Targets;

namespace HugMod
{
    public class HugMod : ModBehaviour
    {
        public static HugMod HugModInstance;
        public static Harmony HarmonyInstance;
        public static HugModSettings Settings = new();
        public static RuntimeAnimatorController AltRuntimeController;
        public static bool PlayerHasDLC;


        public override object GetApi() { return new HugModApi(); }

        private void Awake()
        {
            HugModInstance = this;
            HarmonyInstance = new Harmony("VioVayo.HugMod");
        }

        private bool MurderCheck()
        {
            if (ModHelper.Interaction.ModExists("Cleric.ArtifactLaser")) return true;
            if (ModHelper.Interaction.ModExists("xen.GhostBuster")) return true;
            return false;
        }

        private void Start()
        {
            if (MurderCheck()) 
            {
                ModHelper.Console.WriteLine("Murder is bad >:C", MessageType.Error);
                this.enabled = false;
                return;
            }

            var settings = HugModInstance.ModHelper.Storage.Load<HugModSettings>("settings.json");
            if (settings != null) Settings = settings;
            var hugBundle = ModHelper.Assets.LoadBundle("Assets/hug_bundle");
            AltRuntimeController = hugBundle.LoadAsset<GameObject>("Assets/HugModAssets/HugAnims.prefab").GetComponent<Animator>().runtimeAnimatorController;

            PlayerHasDLC = EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned;
            if (PlayerHasDLC) HuggableFrightsMain.HFSetup();

            TargetManager.ApplyTargetPatches();
            TargetManager.LoadHugTargetData();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;

                PlayerHugController.SetUpPlayer();
                TargetManager.SetUpHugTargets(loadScene);
            };

            HugPatches.Apply();
            ModHelper.Console.WriteLine($"{nameof(HugMod)} is loaded! " + (PlayerHasDLC ? HuggableFrightsMain.HFEnabledMessage(false) : ""), MessageType.Success);
        }

        public static void UpdateSettings() { HugModInstance.ModHelper.Storage.Save(Settings, "settings.json"); }

        public class HugModSettings
        {
            public bool FriendmakerModeEnabled = false;
            public bool SillyGhostNames = false;
        }
    }
}