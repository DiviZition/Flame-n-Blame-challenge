using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// The body and the contact, authored here and copied into the domain through `IPhysicsSettings` (D-012):
    /// defaults are field initializers, so a fresh asset is playable, and the order is the order a designer
    /// tunes them in. The launch itself lives in `Push Presets.asset` - this file owns only its damping and
    /// cap. Unity's own attributes only: nothing beyond `UnityEngine` is needed, so nothing extra enters the
    /// WebGL build (D-007).
    [CreateAssetMenu(fileName = "Physics Settings", menuName = "Configs/Physics Settings", order = 98)]
    public class PhysicsSettings : ScriptableObject, IPhysicsSettings
    {
        [field: Header("Body & gravity")]
        [field: Tooltip("kg. Force / Mass is the velocity a push leaves behind; Mass·Gravity is the weight the "
            + "ground can carry (= 1569 N at 80 kg), so a launch above it is what can lift you (D-030)."),
            SerializeField]
        public float Mass { get; private set; } = 80f;

        [field: Tooltip("m/s². Applied to the vertical channel every tick."), SerializeField]
        public float Gravity { get; private set; } = 19.62f;

        [field: Tooltip("m/s² the ground holds the body at - the negative rest value vertical falls back to "
            + "on landing. A launch's first ticks read negative from here, which is why the ground's rule is a "
            + "force comparison, not a sign (G-020)."), SerializeField]
        public float GroundedGravity { get; private set; } = -2f;

        [field: Tooltip("m/s, the fall speed cap. The vertical channel never exceeds it."), SerializeField]
        public float TerminalVelocity { get; private set; } = 40f;

        [field: Header("Contact & damping")]
        [field: Tooltip("The ground's reaction force, m/s². It decelerates the player's own control when the "
            + "input is released, and eats the momentum of a shove when the player runs against it (D-036). "
            + "One contact force, so one number."), SerializeField]
        public float GroundFriction { get; private set; } = 12f;

        [field: Tooltip("Grounded push decay per second. 4 drops a 10 m/s shove to walk speed in about an "
            + "eighth of a second and to rest inside a second; drop toward 1 for a long skid, raise past 8 for "
            + "a hard stop. One rate for ground and air is what makes the floor read as ice (G-012)."),
            SerializeField]
        public float GroundDrag { get; private set; } = 4f;

        [field: Tooltip("Airborne push decay per second - the inertia of a launch during flight, which is the "
            + "whole point of D-016. 0.5 keeps a shove alive for seconds."), SerializeField]
        public float AirDrag { get; private set; } = 0.5f;

        [field: Header("Push channel (cap)")]
        [field: Tooltip("The push channel's cap, m/s. 0 means no cap. A sustained source saturates here "
            + "instead of accelerating the player away forever (B-003 is this rule's gate). The jump's forward "
            + "carry is bounded by it too."), SerializeField]
        public float MaxPushSpeed { get; private set; } = 12f;

        [field: Header("Jump (the launch itself is in Push Presets.asset)")]
        [field: Tooltip("The fraction of the player's own speed carried into the jump as forward momentum. "
            + "1 = a sprint jump keeps all 8 m/s, a walk jump its 5, standing carries exactly zero. The "
            + "vertical launch is the jump preset, untouched: this chooses reach, never height (D-034)."),
            SerializeField]
        public float JumpCarry { get; private set; } = 1f;
    }
}
