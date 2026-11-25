using Flame_n_Blame_Challenge.Adapters;
using System;
using System.Collections.Generic;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class Updater : IUpdatable
    {
        private List<IUpdatable> _updatables;
        private Queue<IUpdatable> _updatablesToRemove;

        public Updater()
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

        public Updater RegisterUpdatable(IUpdatable updatable)
        { 
            if (updatable != null && _updatables.Contains(updatable) == false)
                _updatables.Add(updatable);

            return this;
        }

        public Updater RemoveUpdatable(IUpdatable updatable)
        { 
            if (updatable != null)
                _updatablesToRemove.Enqueue(updatable);

            return this;
        }

        private void RemoveStagedUpdatables()
        {
            if (_updatablesToRemove.Count >= _updatables.Count)
            {
                _updatables.Clear();
                _updatablesToRemove.Clear();
            }

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

        public void Clear()
        {
            _updatablesToRemove.Clear();
            foreach (var item in _updatables)
                _updatablesToRemove.Enqueue(item);
        }
    }
}
