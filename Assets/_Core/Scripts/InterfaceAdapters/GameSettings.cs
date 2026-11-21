using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// The motion tuning, authored here and copied into the domain through this port. Domain never
    /// reads the Inspector (D-012); defaults are field initializers so a fresh asset is playable.
    [CreateAssetMenu(fileName = "Game Settings", menuName = "Configs/Game Settings", order = 99)]
    public class GameSettings : ScriptableObject, IGameSettings
    {
        [field: SerializeField] public float PlayerSpeedBase { get; private set; } = 5f;
        [field: SerializeField] public float PlayerSpeedSprint { get; private set; } = 8f;
        [field: SerializeField] public float PlayerSpeedInAir { get; private set; } = 2f;

        [field: SerializeField] public float LookSensitivity { get; private set; } = 0.17f;
        [field: SerializeField] public Vector2 LookPitchBounds { get; private set; } = new Vector2(-90f, 90f);

        [field: SerializeField] public float Mass { get; private set; } = 80f;
        [field: SerializeField] public float Gravity { get; private set; } = 19.62f;
        [field: SerializeField] public float GroundedGravity { get; private set; } = -2f;
        [field: SerializeField] public float TerminalVelocity { get; private set; } = 40f;

        [field: SerializeField] public float GroundAcceleration { get; private set; } = 20f;
        [field: SerializeField] public float AirAcceleration { get; private set; } = 6f;
        [field: SerializeField] public float GroundFriction { get; private set; } = 12f;

        /// The push channel's decay while grounded: how fast the ground absorbs a shove, in units per
        /// second. 4 makes a 10 m/s launch fall to walking speed in about an eighth of a second and
        /// come to rest inside a second; drop it toward 1 for a long skid, raise it past 8 for a
        /// hard stop. It is authored separately from GroundFriction so a stop-press can stay snappy
        /// while a shove still outlives it.
        [field: SerializeField] public float GroundDrag { get; private set; } = 4f;
        [field: SerializeField] public float AirDrag { get; private set; } = 0.5f;

        /// Anti-accumulation guard on the push channel: a sustained source saturates here instead of
        /// accelerating the player away forever. 0 means no cap.
        [field: SerializeField] public float MaxPushSpeed { get; private set; } = 12f;
    }
}
