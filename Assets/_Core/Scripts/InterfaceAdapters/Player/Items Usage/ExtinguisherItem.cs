using Flame_n_Blame_Challenge.Domain;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class ExtinguisherItem : ItemBase, IUsable
    {
        [field: SerializeField] public float UseCoolDown { get; private set; } = 0.2f;

        public void Use()
        {
            Debug.Log($"Item: {this.name} used");
        }
    }
}
