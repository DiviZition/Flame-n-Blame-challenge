using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// What the player commands, authored here and copied into the domain through `IControlSettings` (D-012).
    /// Commands, not forces: the air speed lives in a model with no reaction force (D-016), and in flight
    /// control never adds to a shove (D-036). Unity's own attributes only, so nothing extra ships to WebGL
    /// (D-007).
    [CreateAssetMenu(fileName = "Player Control Settings", menuName = "Configs/Player Control Settings",
        order = 97)]
    public class PlayerControlSettings : ScriptableObject, IControlSettings
    {
        [field: Header("Locomotion (m/s)")]
        [field: Tooltip("Walk speed. The control channel's target on the ground."), SerializeField]
        public float PlayerSpeedBase { get; private set; } = 5f;

        [field: Tooltip("Sprint speed, while grounded and running. The jump's forward carry is measured "
            + "against this, so it is the reach ceiling of a run-up jump (D-034)."), SerializeField]
        public float PlayerSpeedSprint { get; private set; } = 8f;

        [field: Tooltip("The control channel's target in flight. Lower than the ground speed on purpose: "
            + "steering in the air is weak, and a launch's momentum is carried in the push channel, not here "
            + "(D-016)."), SerializeField]
        public float PlayerSpeedInAir { get; private set; } = 2f;

        [field: Header("Steering (approach rate per second)")]
        [field: Tooltip("How fast the ground control reaches the input speed. 20 is about a quarter second "
            + "from rest to full walk speed. Above `GroundFriction` is unphysical: the legs cannot push "
            + "harder than the ground's reaction."), SerializeField]
        public float GroundAcceleration { get; private set; } = 20f;

        [field: Tooltip("The same approach in flight. Smaller than GroundAcceleration: a player cannot steer "
            + "as hard in the air as on the ground, and in flight control never adds to a shove (D-036)."),
            SerializeField]
        public float AirAcceleration { get; private set; } = 6f;

        [field: Header("Look")]
        [field: Tooltip("Mouse units to degrees."), SerializeField]
        public float LookSensitivity { get; private set; } = 0.17f;

        [field: Tooltip("Pitch clamp, degrees: x is down, y is up. Tighter than -90/90 keeps the camera from "
            + "the floor and the ceiling."), SerializeField]
        public Vector2 LookPitchBounds { get; private set; } = new Vector2(-90f, 90f);
    }
}
