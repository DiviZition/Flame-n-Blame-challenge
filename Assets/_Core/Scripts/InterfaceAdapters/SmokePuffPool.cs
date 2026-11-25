using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell: a fixed pool of one-shot puff systems, cycled in order (STYLE.md: pool anything that
    /// spawns repeatedly — a puff is the most repeated VFX event in this game, a strike per impact).
    /// Nothing is created at runtime, and the pool keeps no rules: it takes the position and the intensity
    /// the shell hands over, plays the next slot, and moves on. When the pool is full, the oldest slot is
    /// reused — a bounded budget, not a queue that grows.
    ///
    /// WebGL budget (TODO.md): 32 puffs alive, 12 particles each.
    public class SmokePuffPool : MonoBehaviour
    {
        [SerializeField] private GameObject _puffPrefab;
        [SerializeField] private int _capacity = 32;
        [SerializeField] private float _minScale = 0.4f;
        [SerializeField] private float _maxScale = 1.2f;

        private readonly List<VisualEffect> _slots = new(32);
        private int _next;

        /// Composition: the slots exist from the start, as children, inactive. The session calls this before
        /// any shell can ask for a puff.
        public void Construct()
        {
            _slots.Clear();
            _next = 0;

            for (int i = 0; i < _capacity; i++)
            {
                GameObject puff = Instantiate(_puffPrefab, transform);
                VisualEffect effect = puff.GetComponent<VisualEffect>();
                puff.SetActive(false);
                _slots.Add(effect);
            }
        }

        public void Spawn(Vector3 worldPosition, float intensity)
        {
            if (_slots.Count == 0)
                return;

            VisualEffect puff = _slots[_next];
            _next = (_next + 1) % _slots.Count;

            Transform slot = puff.transform;
            slot.position = worldPosition;
            slot.localScale = Vector3.one * Mathf.Lerp(_minScale, _maxScale, Mathf.Clamp01(intensity));

            // The slot is a child of the pool, inactive between plays. A slot is reused while its effect's
            // simulation may still be running, so the pool re-inits it before playing — the effect's Clear().
            slot.gameObject.SetActive(true);
            puff.Reinit();
            puff.Play();
        }
    }
}
