using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// The motion tuning, authored here and copied into the domain through this port. Domain never
    /// reads the Inspector (D-012); defaults are field initializers so a fresh asset is playable.
    /// The Inspector is grouped by what the numbers decide, in the order a designer tunes them: the
    /// speed the player asks for, then how fast it is reached and stopped, then the body, then the
    /// push channel (a launch is authored in `Push Presets.asset`, this file owns only its damping and
    /// cap), then the camera. No Unity package beyond `UnityEngine` is needed (headers and tooltips are
    /// Unity's own), so nothing enters the WebGL build (D-007).
    [CreateAssetMenu(fileName = "Game Settings", menuName = "Configs/Game Settings", order = 99)]
    public class GameSettings : ScriptableObject, IGameSettings
    {
        [field: Header("Locomotion (m/s)")]
        [field: Tooltip("Walk speed. The control channel's target on the ground."), SerializeField]
        public float PlayerSpeedBase { get; private set; } = 5f;

        [field: Tooltip("Sprint speed, while grounded and running. The jump's forward carry is measured "
            + "against this, so it is the reach ceiling of a run-up jump (D-034)."), SerializeField]
        public float PlayerSpeedSprint { get; private set; } = 8f;

        [field: Tooltip("The control channel's target in flight. Lower than the ground speed on purpose: "
            + "steering in the air is weak, and a launch's momentum is carried in the push channel, not "
            + "here (D-016)."), SerializeField]
        public float PlayerSpeedInAir { get; private set; } = 2f;

        [field: Header("Steering (approach rate per second)")]
        [field: Tooltip("How fast the ground control reaches the input speed. 20 is about a quarter second "
            + "from rest to full walk speed."), SerializeField]
        public float GroundAcceleration { get; private set; } = 20f;

        [field: Tooltip("The same approach in flight. Smaller than GroundAcceleration: a player cannot "
            + "steer as hard in the air as on the ground."), SerializeField]
        public float AirAcceleration { get; private set; } = 6f;

        [field: Tooltip("How fast ground control dies when input is released. Authored separately from "
            + "GroundDrag: a snappy stop-press must not drag a shove to a halt too (D-020)."), SerializeField]
        public float GroundFriction { get; private set; } = 12f;

        [field: Header("Body & gravity")]
        [field: Tooltip("kg. Force / Mass is the velocity a push leaves behind; Mass·Gravity is the weight "
            + "the ground can carry (= 1569 N at 80 kg), so a launch above it is what can lift you (D-030)."),
            SerializeField]
        public float Mass { get; private set; } = 80f;

        [field: Tooltip("m/s². Applied to the vertical channel every tick."), SerializeField]
        public float Gravity { get; private set; } = 19.62f;

        [field: Tooltip("m/s² the ground holds the body at — the negative rest value vertical falls back "
            + "to on landing. A launch's first ticks read negative from here, which is why the ground's rule "
            + "is a force comparison, not a sign (G-020)."), SerializeField]
        public float GroundedGravity { get; private set; } = -2f;

        [field: Tooltip("m/s, the fall speed cap. The vertical channel never exceeds it."), SerializeField]
        public float TerminalVelocity { get; private set; } = 40f;

        [field: Header("Jump (the launch itself is in Push Presets.asset)")]
        [field: Tooltip("The fraction of the player's own speed carried into the jump as forward momentum. "
            + "1 = a sprint jump keeps all 8 m/s, a walk jump its 5, standing carries exactly zero. The "
            + "vertical launch is the jump preset, untouched: this chooses reach, never height (D-034)."),
            SerializeField]
        public float JumpCarry { get; private set; } = 1f;

        [field: Header("Push channel (damping & cap)")]
        [field: Tooltip("Grounded push decay per second. 4 drops a 10 m/s shove to walk speed in about an "
            + "eighth of a second and to rest inside a second; drop toward 1 for a long skid, raise past 8 "
            + "for a hard stop. One rate for ground and air is what makes the floor read as ice (G-012)."),
            SerializeField]
        public float GroundDrag { get; private set; } = 4f;

        [field: Tooltip("Airborne push decay per second — the inertia of a launch during flight, which is "
            + "the whole point of D-016. 0.5 keeps a shove alive for seconds."), SerializeField]
        public float AirDrag { get; private set; } = 0.5f;

        [field: Tooltip("The push channel's cap, m/s. 0 means no cap. A sustained source saturates here "
            + "instead of accelerating the player away forever (B-003 is this rule's gate). The jump's "
            + "forward carry is bounded by it too."), SerializeField]
        public float MaxPushSpeed { get; private set; } = 12f;

        [field: Header("Look")]
        [field: Tooltip("Mouse units to degrees."), SerializeField]
        public float LookSensitivity { get; private set; } = 0.17f;

        [field: Tooltip("Pitch clamp, degrees: x is down, y is up. Tighter than -90/90 keeps the camera "
            + "from the floor and the ceiling."), SerializeField]
        public Vector2 LookPitchBounds { get; private set; } = new Vector2(-90f, 90f);
    }
}
