using System.Linq;
using UnityEngine;

namespace HugMod
{
    public static class HugModUtilities
    {
        public static T[] AddToArray<T>(T[] array, params T[] toAdd)
        {
            var list = array.ToList();
            foreach (var addition in toAdd) list.Add(addition);
            return list.ToArray();
        }

        public static Transform FindInDescendants(this GameObject gameObject, string name, bool includeInactive = true)
        {
            return gameObject.GetComponentsInChildren<Transform>(includeInactive).Where(obj => obj.gameObject.name == name).First();
        }

        public static GameObject CreateChild(this Transform parentTransform, string name, Vector3 localPosition = default, Vector3 localEulerAngles = default, float scaleMultiplier = 1)
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
