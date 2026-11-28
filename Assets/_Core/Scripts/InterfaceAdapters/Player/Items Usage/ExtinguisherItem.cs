using Flame_n_Blame_Challenge.Domain;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    public class ExtinguisherItem : ItemBase, IUsable
    {
        [field: SerializeField] public float UseCoolDown { get; private set; } = 0.2f;
        [SerializeField, Range(-1f, 1f)] private float _extinguishPower;
        [SerializeField] private float _maxResource;
        [SerializeField] private int _maxTargetsPerTime;

        [SerializeField] private PushPresets _pushPresets;
        [SerializeField] private PushType _pushType;
        [SerializeField] private ParticleSystem _SprayParticle;
        [SerializeField] private ParticleSystem _emptyResourceParticle;

        [SerializeField] private Vector3 _sprayCastStartPointOffset;
        [SerializeField] private float _sprayLength;
        [SerializeField] private float _sprayRadius;
        [SerializeField] private LayerMask _burnablesMask;

        [field: SerializeField] public float CurrentResource { get; private set; } = -100;
        private Collider[] _burnablesHit;

        //TODO: переписать на использование UseContext/UseEffectsOutput, вместо прямого обращения к домену.
        public void Use(Player playerDomain)
        {
            if (_burnablesHit == null)
                _burnablesHit = new Collider[_maxTargetsPerTime];

            if (CurrentResource == -100)
                CurrentResource = _maxResource;

            _emptyResourceParticle.Play();
            
            if (CurrentResource <= 0)
                return;

            CurrentResource -= Mathf.Abs(_extinguishPower);

            ExtinguishBurnables();
            _SprayParticle.Play();
            PushPlayer(playerDomain);
        }

        private void PushPlayer(Player playerDomain)
        {
            Vector3 pushDirection = Transform.forward * -1;
            playerDomain.ReceivePush(_pushPresets.PresetOf(_pushType), pushDirection);
        }

        private void ExtinguishBurnables()
        {
            //Vector3 castStartPoint = Transform.TransformPoint(_sprayCastStartPointOffset);
            Vector3 castStartPoint = Transform.position + Transform.rotation * _sprayCastStartPointOffset;
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                point0: castStartPoint, 
                point1: castStartPoint + Transform.forward * _sprayLength, 
                radius: _sprayRadius,
                results: _burnablesHit,
                layerMask: _burnablesMask);

            Debug.Log($"found {hitCount} burnables");
            for (int i = 0; i < hitCount; i++)
            {
                if(_burnablesHit[i].TryGetComponent(out BurnableShell burnableShell))
                {
                    Debug.Log($"Extinguishing: {burnableShell}");
                    burnableShell.ApplyStream(_extinguishPower, Transform.position);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (Transform == null)
                return;

            Gizmos.color = Color.yellow;
            Vector3 castStartPoint = Transform.position + Transform.rotation * _sprayCastStartPointOffset;
            Gizmos.DrawWireSphere(castStartPoint, _sprayRadius);
            Gizmos.DrawWireSphere(castStartPoint + Transform.forward * _sprayLength, _sprayRadius);
        }
    }
}
