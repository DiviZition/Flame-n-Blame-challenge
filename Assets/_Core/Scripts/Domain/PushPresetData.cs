using System;

namespace Flame_n_Blame_Challenge.Domain
{
    /// One push, authored on a shell/ScriptableObject and copied in as plain data. Domain never reads
    /// the Inspector (D-012). Public fields are legal here: this is a data-only DTO (STYLE.md).
    [Serializable]
    public class PushPresetData
    {
        public PushType Type;
        public PushKind Kind;
        public PushApplication Applies;

        /// Newtons for Sustained, newton-seconds for Impulse. Divided by Mass in the domain, so a
        /// spray shoves the player more than it shoves a wardrobe.
        public float Force;

        public Vector3CurveData Curve;

        /// How long the FORCE runs. It ends the adding, not the velocity - momentum survives the timer
        /// and is absorbed by contact (G-012).
        public float Duration;
    }
}
