using UnityEngine;

namespace HugMod.Targets
{
    public class Target
    {
        public string name = "", objectName = "";
        public HugTrigger hugTrigger = HugTrigger.None;
        public (float, float, float) focusPoint = (0, 0.1f, 0);
        public bool fullbodyReact = true, keepFootAnimRight = false, keepFootAnimLeft = false, keepHandAnimRight = false, keepHandAnimLeft = false;
        public float transitionTime = 0.5f;
        public int transitionHash = 0;
        public string transitionClipName = "";
        public bool isTraveller = false, isOwl = false, isAtEye = false;


        public GameObject GetTargetObject() { return GameObject.Find(objectName); }

        public HugComponent GetHugComponent() { return GameObject.Find(objectName).GetComponent<HugComponent>(); }

        public Vector3 GetFocusPoint() { return new(focusPoint.Item1, focusPoint.Item2, focusPoint.Item3); }
    }
}
