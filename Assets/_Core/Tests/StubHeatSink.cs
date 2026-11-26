using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// The shell's sink, as plain data: it remembers the last stream it was given. The spread system's rule is
    /// the amount it integrates and the neighbour it names, so a test reads both off this stub (D-052).
    public class StubHeatSink : IHeatSink
    {
        public float LastAmount;
        public Vector3? LastContact;

        public void ApplyStream(float amount, Vector3? contactPoint)
        {
            LastAmount = amount;
            LastContact = contactPoint;
        }
    }
}
