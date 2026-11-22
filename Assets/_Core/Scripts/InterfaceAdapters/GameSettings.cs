using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// The authored facade the session is built from: two config assets, each read through its own port
    /// (D-037). `GameSettings` holds no numbers of its own - every number in the project has a home in the
    /// port that reads it, and a session rule joins this file when its reader exists (D-033). The Inspector
    /// shows two object slots; each asset is tuned on its own screen, grouped by what the numbers decide.
    [CreateAssetMenu(fileName = "Game Settings", menuName = "Configs/Game Settings", order = 99)]
    public class GameSettings : ScriptableObject, IGameSettings
    {
        [field: Header("Configs")]
        [field: Tooltip("The body and the contact: mass, gravity, friction, damping, the push cap, the jump "
            + "carry. Authored in `Physics Settings.asset`."), SerializeField]
        private PhysicsSettings _physics;

        [field: Tooltip("What the player commands: the three speeds, the airborne steering rate, the look. "
            + "Authored in `Player Control Settings.asset`."), SerializeField]
        private PlayerControlSettings _control;

        public IPhysicsSettings Physics => _physics;
        public IControlSettings Control => _control;
    }
}
