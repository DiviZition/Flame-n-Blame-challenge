using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell (D-012): a trigger is the only thing Unity lets this object see, and it forwards what
    /// it observed. No rules in it — the preset, the direction, the launch and the momentum are the
    /// domain's decision (D-016). Direction comes from the **source's** rotation, never the player's
    /// facing, so a rotated hydrant launches you along its nozzle.
    public class HydrantPush : MonoBehaviour
    {
        [SerializeField] private PushPresetData _pushPreset;
        [SerializeField] private Transform _pushDirection;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerMotor playerMotor))
                playerMotor.ApplyForce(_pushPreset, _pushDirection.rotation);
        }
    }
}
