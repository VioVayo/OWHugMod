using UnityEngine;

namespace HugMod.ProxyComponents
{
    public class HugIK : MonoBehaviour
    {
        private HugComponent hugComponent;

        public void SetHugComponent(HugComponent hugComponent) { this.hugComponent = hugComponent; }

        private void Start() 
        { 
            if (hugComponent == null) Remove();
            else hugComponent.OnDestroyEvent += Remove; 
        }
        private void OnAnimatorIK() { hugComponent.OnAnimatorIK(); }

        public void Remove() { Destroy(this); }
    }
}
