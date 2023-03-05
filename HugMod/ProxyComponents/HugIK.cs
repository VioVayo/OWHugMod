using UnityEngine;

namespace HugMod.ProxyComponents
{
    public class HugIK : MonoBehaviour
    {
        public HugComponent hugComponent;
        public void OnAnimatorIK() { hugComponent.OnAnimatorIK(); }
    }
}
