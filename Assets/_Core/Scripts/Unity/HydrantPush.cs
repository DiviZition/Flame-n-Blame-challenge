using Flame_n_Blame_Challenge.Adapters;
using UnityEngine;

namespace Flame_n_Blame_Challenge
{
    public class HydrantPush : MonoBehaviour
    {
        public ForcePreset PushPreset;
        public Transform PushDirection;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerMotor playerMotor))
                playerMotor.ApplyForce(PushPreset, PushDirection.rotation);
        }
    }
}
