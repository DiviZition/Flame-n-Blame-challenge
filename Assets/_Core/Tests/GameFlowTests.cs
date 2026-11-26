using Flame_n_Blame_Challenge.UseCases;
using NUnit.Framework;

namespace Flame_n_Blame_Challenge.Tests
{
    /// The launch flow as rules: who may move the state, and what a legal move raises (D-053). The gate is the
    /// state, never the request, so a request outside its state is a no-op.
    public class GameFlowTests
    {
        [Test]
        public void Start_BriefingEntersPlaying_OutsideBriefingIsNoOp()
        {
            var flow = new GameFlow();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Briefing));

            flow.RequestStart();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));

            flow.RequestStart();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing), "the gate eats the second press");
        }

        [Test]
        public void Pause_OnlyFromPlaying()
        {
            var flow = new GameFlow();

            flow.RequestPause();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Briefing), "the briefing window cannot be paused");

            flow.RequestStart();
            flow.RequestPause();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Paused));
        }

        [Test]
        public void Resume_OnlyFromPaused()
        {
            var flow = new GameFlow();

            flow.RequestResume();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Briefing));

            flow.RequestStart();
            flow.RequestResume();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing), "live play is not resumed by a press");

            flow.RequestPause();
            flow.RequestResume();
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Playing));
        }

        [Test]
        public void Restart_RaisesTheLoadIntentAndReturnsToBriefing()
        {
            var flow = new GameFlow();
            int restartCount = 0;
            flow.RestartRequested += () => restartCount++;

            flow.RequestRestart();
            Assert.That(restartCount, Is.EqualTo(0), "there is nothing to restart in Briefing");

            flow.RequestStart();
            flow.RequestRestart();
            Assert.That(restartCount, Is.EqualTo(1));
            Assert.That(flow.State, Is.EqualTo(GameFlowState.Briefing), "the new load lands on the briefing window");

            flow.RequestRestart();
            Assert.That(restartCount, Is.EqualTo(1), "a restart while briefing is no-op");
        }

        [Test]
        public void StateChanged_FiresTheNewStateOncePerLegalRequest()
        {
            var flow = new GameFlow();
            var seen = new System.Collections.Generic.List<GameFlowState>();
            flow.StateChanged += state => seen.Add(state);

            flow.RequestRestart();
            flow.RequestStart();
            flow.RequestResume();
            flow.RequestPause();

            Assert.That(seen, Is.EqualTo(new[] { GameFlowState.Playing, GameFlowState.Paused }));
        }
    }
}
