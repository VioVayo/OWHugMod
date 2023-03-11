using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HugMod.HuggableFrights;
using HugMod.Targets;

namespace HugMod
{
    public class HugMod : ModBehaviour
    {
        public static HugMod HugModInstance;
        public static RuntimeAnimatorController AltRuntimeController;
        public static bool PlayerHasDLC;


        public override object GetApi() { return new HugModApi(); }

        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            HugModInstance = this;
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

            TargetManager.LoadHugTargetData();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;

                StartCoroutine(PlayerHugController.SetUpPlayer());
                TargetManager.SetUpHugTargets(loadScene);
            };

            ModHelper.Console.WriteLine($"{nameof(HugMod)} is loaded! " + (PlayerHasDLC ? HuggableFrightsMain.HFEnabledMessage(false) : ""), MessageType.Success);
        }



        //-----Useful stuff-----
        public static T[] AddToArray<T>(T[] array, T toAdd)
        {
            var list = array.ToList();
            list.Add(toAdd);
            return list.ToArray();
        }

        public static T[] AddToArray<T>(T[] array, T toAdd1, T toAdd2, T toAdd3)
        {
            var list = array.ToList();
            list.Add(toAdd1);
            list.Add(toAdd2);
            list.Add(toAdd3);
            return list.ToArray();
        }

        public static Transform FindInDescendants(GameObject gameObject, string name, bool includeInactive = true)
        {
            return gameObject.GetComponentsInChildren<Transform>(includeInactive).Where(obj => obj.gameObject.name == name).First();
        }

        public static GameObject CreateChild(string name, Transform parentTransform, Vector3 localPosition = default, Vector3 localEulerAngles = default, float scaleMultiplier = 1)
        {
            var childObj = new GameObject(name);
            childObj.transform.SetParent(parentTransform);
            childObj.transform.localPosition = localPosition;
            childObj.transform.localEulerAngles = localEulerAngles;
            childObj.transform.localScale = scaleMultiplier * Vector3.one;
            return childObj;
        }
    }
}