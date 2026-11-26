using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell (D-012): a trigger is the only thing Unity lets this object see, and it forwards what
    /// it observed. No rules in it — the preset, the direction, the launch and the momentum are the
    /// domain's decision (D-016).
    ///
    /// The direction is the child Transform's **forward**, handed over as a Vector3 (D-028): a
    /// `Quaternion * curve` is what made a launch read as a diagonal — an empty curve times a rotation is
    /// the diagonal of the nozzle's space (G-018). Aim the child Transform where the burst should run
    /// (up-and-out for the catapult, D-008 pillar 3), and the vertical part of that direction becomes the
    /// launch. The numbers of the burst come from the shared presets asset, keyed by the PushType authored
    /// here, and the **type of the DTO** the author picked (Impulse / Sustained) says whether it is a
    /// one-shot shove or a force that runs for a Duration (D-031).
    [SelectionBase]
    public class HydrantPush : MonoBehaviour
    {
        [SerializeField] private PushPresets _presets;
        [SerializeField] private PushType _pushType;
        [SerializeField] private Transform _pushDirection;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerMotor playerMotor))
                playerMotor.ApplyForce(_presets.PresetOf(_pushType), _pushDirection.forward);
        }
    }
}
