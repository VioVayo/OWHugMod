using UnityEngine;

namespace HugMod.ProxyComponents
{
    public class HugTriggerCollider : MonoBehaviour
    {
        public HugComponent hugComponent;
        public void OnTriggerStay(Collider collider) { hugComponent.OnTriggerStay(collider); }
    }
}
