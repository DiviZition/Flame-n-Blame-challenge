using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    [Serializable]
    public class ViewHandData
    {
        [field: SerializeField] public Transform ItemSpot { get; private set; }
        [field: SerializeField] public Transform HandIKSpot { get; private set; }
        [field: SerializeField] public IItemView ItemView { get; private set; }
    }
}
