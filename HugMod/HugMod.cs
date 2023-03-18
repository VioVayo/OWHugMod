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
        public static RuntimeAnimatorController AltRuntimeController;
        public static bool PlayerHasDLC;


        public override object GetApi() { return new HugModApi(); }

        private void Awake()
        {
            HugModInstance = this;
            HarmonyInstance = new Harmony("VioVayo.HugMod");
            HugPatches.Apply();
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

            var hugBundle = ModHelper.Assets.LoadBundle("Assets/hug_bundle");
            AltRuntimeController = hugBundle.LoadAsset<GameObject>("Assets/HugModAssets/HugAnims.prefab").GetComponent<Animator>().runtimeAnimatorController;

            PlayerHasDLC = EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.Owned;
            if (PlayerHasDLC) HuggableFrightsMain.HFSetup();

            TargetManager.ApplyTargetPatches();
            TargetManager.LoadHugTargetData();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;

                StartCoroutine(PlayerHugController.SetUpPlayer());
                TargetManager.SetUpHugTargets(loadScene);
            };

            ModHelper.Console.WriteLine($"{nameof(HugMod)} is loaded! " + (PlayerHasDLC ? HuggableFrightsMain.HFEnabledMessage(false) : ""), MessageType.Success);
        }
    }
}