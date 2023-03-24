using System.Linq;
using UnityEngine;

namespace HugMod.Targets
{
    public class Target
    {
        public string name = "", objectName = "";
        public HugTrigger hugTrigger = HugTrigger.None;
        public (float, float, float) focusPoint = (0, 0, 0);
        public bool fullbodyReact = true, keepFootAnimRight = false, keepFootAnimLeft = false, keepHandAnimRight = false, keepHandAnimLeft = false;
        public float transitionTime = 0.5f;
        public int transitionHash = 0;
        public string transitionClipName = "";
        public bool isTraveller = false, isOwl = false, isPartyOwl = false, isAtEye = false;


        public GameObject GetTargetObject() 
        { 
            var objNames = objectName.Split('/');
            var obj = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(obj => obj.name == objNames[0]);
            if (obj != null && objNames.Length > 1) for (int n = 1; n < objNames.Length; ++n) obj = obj.transform.Find(objNames[n]).gameObject;
            return obj;
        }

        public HugComponent GetHugComponent() { return GetTargetObject().GetComponent<HugComponent>(); }

        public Vector3 GetFocusPoint() { return new(focusPoint.Item1, focusPoint.Item2, focusPoint.Item3); }
    }
}
