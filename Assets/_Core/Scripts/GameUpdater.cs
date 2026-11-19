using Flame_n_Blame_Challenge.Adapters;
using System.Collections.Generic;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class GameUpdater : IUpdatable
    {
        private List<IUpdatable> _updatables;
        private Queue<IUpdatable> _updatablesToRemove;

        public GameUpdater()
        {
            _updatables = new List<IUpdatable>(16);
            _updatablesToRemove = new Queue<IUpdatable>(4);
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            for (int i = 0; i < _updatables.Count; i++)
                _updatables[i].UpdateLogic(time, deltaTime);

            RemoveStagedUpdatables();
        }

        public GameUpdater RegisterUpdatable(IUpdatable updatable)
        {
            if (updatable != null && _updatables.Contains(updatable) == false)
                _updatables.Add(updatable);

            return this;
        }

        public GameUpdater RemoveUpdatable(IUpdatable updatable)
        {
            if (updatable != null)
                _updatablesToRemove.Enqueue(updatable);

            return this;
        }

        private void RemoveStagedUpdatables()
        {
            while (_updatablesToRemove.Count > 0)
            {
                var updatableToRemove = _updatablesToRemove.Dequeue();
                int index = _updatables.FindIndex(element => ReferenceEquals(element, updatableToRemove));
                if (index < 0)
                    continue;

                int last = _updatables.Count - 1;
                _updatables[index] = _updatables[last];
                _updatables.RemoveAt(last);
            }
        }
    }
}
