using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public interface IItemView : IHoldable
    {
        public Transform Transform { get; }

        public void DisableCollision();
        public void EnableCollision();

        public void StartHighlighting();
        public void StopHighlighting();
    }
}
