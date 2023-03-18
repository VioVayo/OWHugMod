using UnityEngine;

namespace HugMod.ProxyComponents
{
    public class HugTriggerCollider : MonoBehaviour
    {
        private HugComponent hugComponent;

        public void SetHugComponent(HugComponent hugComponent) { this.hugComponent = hugComponent; }

        private void Start()
        {
            if (hugComponent == null) Remove();
            else hugComponent.OnDestroyEvent += Remove; 
        }
        private void OnTriggerStay(Collider collider) { hugComponent.OnTriggerStay(collider); }

        public void Remove() 
        { 
            this.gameObject.transform.SetParent(null);
            Destroy(this.gameObject); 
        }
    }
}
