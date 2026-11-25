using System;
using System.Linq;
using UnityEngine;

namespace Flame_n_Blame_Challenge
{
    public class SpawnPoints : MonoBehaviour
    {
        [field: SerializeField] public SpawnPointData[] Points { get; private set; }

        public bool GetPoint(SpawnPointType type, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            var pointData = Points.First(point => point.Type == type);

            if (pointData.Point == null)
                throw new NullReferenceException($"SpawnPoint of type {type} was not found.");

            pos = pointData.Point.position; 
            rot = pointData.Point.rotation;
            return true;
        }

        [Serializable]
        public struct SpawnPointData
        {
            public Transform Point;
            public SpawnPointType Type;
        }

        public enum SpawnPointType
        {
            Player,
        }
    }
}
