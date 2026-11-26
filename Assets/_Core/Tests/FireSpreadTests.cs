using Flame_n_Blame_Challenge.Domain;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// The spread rule (D-008, D-043, D-052): a burning neighbour feeds heat, and the heat a body gets is the
    /// sum of what every neighbour inside **its own** reach gives, with the quadratic falloff, integrated over
    /// the time since that body's own last visit. One rule per test (D-050): the visit delivers the full-sweep
    /// sum through the shell's stream door, and it names the neighbour that gives the most.
    public class FireSpreadTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerance = 0.00001f;

        private StubBurnablePreset _preset;
        private FireSpreadSystem _system;
        private readonly List<StubHeatSink> _sinks = new List<StubHeatSink>();

        [SetUp]
        public void SetUp()
        {
            _preset = new StubBurnablePreset();
            _system = new FireSpreadSystem();
            _sinks.Clear();
        }

        /// The scenario: the target at the origin, added first so the first visit is its own, and the given
        /// neighbours burning. Every neighbour is heated to 0.4, so each burns at the same intensity and only
        /// the distance separates their contributions — the expectation is the distances.
        private StubHeatSink BuildScenario(params Vector3[] neighbourPositions)
        {
            SetUp();

            var targetSink = new StubHeatSink();
            _system.Add(new BurnableObject(_preset, Vector3.zero), targetSink);
            _sinks.Add(targetSink);

            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                var neighbour = new BurnableObject(_preset, neighbourPositions[i]);
                neighbour.AffectIgnitionStrength(0.4f);

                var sink = new StubHeatSink();
                _system.Add(neighbour, sink);
                _sinks.Add(sink);
            }

            return targetSink;
        }

        // The analytic expectation, read off the authored numbers (threshold 0.3, intensity 0.1, reach 3 m): a
        // body heated to 0.4 burns at FireLevel (0.4 − 0.3) / (1 − 0.3) = 1/7, so its intensity is 0.1/7 and a
        // neighbour at d metres gets 0.1/7 × (1 − d/3)². The 1.5 m neighbour gives 0.25 of that intensity, the
        // 2.4 m one 0.04, and the 3.5 m one nothing — he is outside the 3 m reach, so his contribution is 0 in
        // the neighbour table and in a full sweep alike.
        [Test]
        public void SpreadVisit_FeedsTheFullSweepSum_AndNamesTheStrongestNeighbour()
        {
            StubHeatSink target = BuildScenario(
                new Vector3(1.5f, 0f, 0f),
                new Vector3(0f, 0f, 2.4f),
                new Vector3(0f, 0f, 3.5f));

            _system.UpdateLogic(Dt, Dt);

            float expected = 0.1f / 7f * (0.25f + 0.04f) * Dt;
            Assert.AreEqual(expected, target.LastAmount, Tolerance,
                "the visit must feed the sum over the neighbours, integrated over its own elapsed");

            Assert.IsTrue(target.LastContact.HasValue, "the heat must say where it came from");

            Vector3 named = target.LastContact.Value;
            Assert.AreEqual(1.5f, named.x, Tolerance,
                "the named source is the strongest in range, not the closest one");
            Assert.AreEqual(0f, named.y, Tolerance, "the named source is the 1.5 m neighbour (y)");
            Assert.AreEqual(0f, named.z, Tolerance, "the named source is the 1.5 m neighbour (z)");
        }
    }
}
