using System.Linq;
using UnityEngine;

namespace HugMod
{
    public static class HugModUtilities
    {
        public static T[] AddToArray<T>(this T[] array, params T[] toAdd)
        {
            var list = array.ToList();
            foreach (var addition in toAdd) list.Add(addition);
            return list.ToArray();
        }

        public static Transform FindInDescendants(this GameObject gameObject, string name, bool includeInactive = true)
            => gameObject.GetComponentsInChildren<Transform>(includeInactive).FirstOrDefault(obj => obj.gameObject.name == name);

        public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T component) where T : Component
        {
            component = gameObject.GetComponentInChildren<T>();
            return component != null;
        }

        public static GameObject CreateChild(this GameObject parentObject, string name, Vector3 localPosition = default, Vector3 localEulerAngles = default, float scaleMultiplier = 1)
            => parentObject.transform.CreateChild(name, localPosition, localEulerAngles, scaleMultiplier);

        public static GameObject CreateChild(this Transform parentTransform, string name, Vector3 localPosition = default, Vector3 localEulerAngles = default, float scaleMultiplier = 1)
        {
            var childObj = new GameObject(name);
            childObj.transform.SetParent(parentTransform);
            childObj.transform.localPosition = localPosition;
            childObj.transform.localEulerAngles = localEulerAngles;
            childObj.transform.localScale = scaleMultiplier * Vector3.one;
            return childObj;
        }

        public static T Exists<T>(this T obj) where T : Object 
            => (obj != null) ? obj : null; //Enable use of null conditional operators with destroyed Unity objects
    }
}
