using System;

namespace Flame_n_Blame_Challenge.UseCases
{
    /// Launch, pause and restart as rules only: which request is legal in which state, and what a legal
    /// request raises. No Unity types, no timers, no scene knowledge — RestartRequested is the fact the
    /// composition root acts on, and the new load lands on Briefing (D-053).
    public class GameFlow
    {
        public GameFlowState State { get; private set; } = GameFlowState.Briefing;

        public event Action<GameFlowState> StateChanged;
        public event Action RestartRequested;

        /// The only way out of Briefing.
        public void RequestStart()
        {
            if (State != GameFlowState.Briefing)
                return;

            EnterState(GameFlowState.Playing);
        }

        public void RequestPause()
        {
            if (State != GameFlowState.Playing)
                return;

            EnterState(GameFlowState.Paused);
        }

        public void RequestResume()
        {
            if (State != GameFlowState.Paused)
                return;

            EnterState(GameFlowState.Playing);
        }

        /// A restart is dispose + load again, so the flow returns to Briefing for the new scene.
        public void RequestRestart()
        {
            if (State == GameFlowState.Briefing)
                return;

            RestartRequested?.Invoke();
            EnterState(GameFlowState.Briefing);
        }

        private void EnterState(GameFlowState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
