using Flame_n_Blame_Challenge.Domain;
using System;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Thin shell (D-012): the preset goes out into the domain, the domain's numbers go into the material and
    /// the particles. It decides nothing.
    ///
    /// Every frame: `Charredness` → `_Burn` (the shader's char axis, D-041), `FireLevel` → `_Fire`. The fire
    /// axis is the band **above the ignition threshold**, so flame, fire particles and the char front all
    /// start the moment the body actually ignites, not the moment it gets warm. Smoke runs on the full heat
    /// axis (`IgnitionStrength` 0..1), so a warm, unignited body smokes from the first contact.
    ///
    /// `_BurnOrigin` and `_BurnExtent` (the farthest mesh corner from it, so `Burn` 1 covers the whole mesh at
    /// any size) are written **once, at the ignition**: the last fire contact point is carried until the body
    /// crosses its threshold, then locked — a moving origin re-centres the char pattern and the black that is
    /// already there recedes (G-035). The mesh lives in the shell, so the shader cannot derive that radius.
    /// Authored look numbers (`_BurnReach` = 1, `_EmissionStrength`, `_ShellHeight`) stay in the material.
    ///
    /// The shell is the sink the domain streams into (`IHeatSink`): the spray and the fire spread are the same
    /// stream, and every heat contact lands on the mesh's surface — a spray hit point is already on it, an
    /// off-body contact is clamped to the face turned to it; that point is the fire's origin and the splash's
    /// spawn (D-052).
    ///
    /// The material is shared by every burnable, so the numbers travel in a `MaterialPropertyBlock`: 24
    /// objects, one shader variant (D-040, the WebGL budget in TODO.md).
    ///
    /// Fire and smoke roots sit at the burn point and travel to the mesh centre as `Charredness` grows. The
    /// fire scales from `_fireMinScale` to `_fireMaxScale` on that same axis; the smoke scale peaks at
    /// `_smokeMaxScale` at heat 0.5 and settles to the midpoint at heat 1. The two chains own their ramps as
    /// their own Vector3s, fire and smoke authored apart (D-054). `Charredness` 0 is the ignition, so a
    /// warming body keeps its smoke at the point, growing it, and the fire chain is silent until ignition.
    /// Every transform under a root is written, so a nested sub-emitter follows its parent.
    [SelectionBase]
    public class BurnableShell : MonoBehaviour, IUpdatable, IHeatSink
    {
        private static readonly int BurnId = Shader.PropertyToID("_Burn");
        private static readonly int FireId = Shader.PropertyToID("_Fire");
        private static readonly int BurnOriginId = Shader.PropertyToID("_BurnOrigin");
        private static readonly int BurnExtentId = Shader.PropertyToID("_BurnExtent");

        [Header("Burnable Object Settings")]
        [field: SerializeField] public BurnablePreset Preset { get; private set; }
        [SerializeField] private MeshRenderer[] _surfaces;
        [SerializeField] private GameObject _burnRoot;   // hidden when the object burns out
        [SerializeField] private ParticleSystem _fireParticles;
        [SerializeField] private ParticleSystem _smokeParticles;

        // The shell's own look numbers, per prefab. Nothing here is a domain parameter (D-043).
        [Header("Look Settings")]
        [Tooltip("Fire particle emission at FireLevel 1: the rate is the shell's, the level is the domain's.")]
        [SerializeField] private float _fireParticlesPerSecond = 24f;
        [Tooltip("Smoke particle emission at IgnitionStrength 1, on the full heat axis.")]
        [SerializeField] private float _smokeParticlesPerSecond = 14f;
        [Tooltip("Fire particle scale at Charredness 0, per axis — the scale the fire starts at, as a share "
            + "of the authored scale."), SerializeField]
        private Vector2 _fireScaleRange = new Vector2(0.2f, 0.2f);

        [Tooltip("Smoke particle scale at heat 0, per axis."), SerializeField]
        private Vector2 _smokeScaleRange = new Vector2(0.2f, 0.2f);

        #region Readonly Info
#if UNITY_EDITOR
        [Header("Output Readonly Info")]
        [field: SerializeField] public float IgnitionStrength;
        [field: SerializeField] public float CurrentBurnDurability;
#endif
        #endregion

        private IBurnable _burnable;
        private SmokePuffPool _puffPool;
        private MaterialPropertyBlock _block;

        // The mesh in the shell's object space, read once (STYLE.md, G-027): `bounds` is the renderer's world
        // AABB, so a surface's own scale is in it. No surfaces means the shell's own origin is the centre —
        // not the world zero, which is what an unset mesh leaves.
        private Vector3 _centerLocal;
        private Vector3[] _cornersLocal = Array.Empty<Vector3>();

        // The same box as min and max in object space, so a fire contact is clamped to the mesh's surface.
        private Vector3 _meshMinLocal;
        private Vector3 _meshMaxLocal;

        // Particle roots with every transform under them, and the scale each one starts at. Inactive nodes are
        // collected too: a chain authored with a disabled root is the author's choice, and the shell switches
        // it on.
        private Transform[] _fireChain = Array.Empty<Transform>();
        private Transform[] _smokeChain = Array.Empty<Transform>();
        private Vector3[] _fireBaseScales = Array.Empty<Vector3>();
        private Vector3[] _smokeBaseScales = Array.Empty<Vector3>();

        private Vector3 _burnPointLocal;   // the last fire contact; the centre until one arrives
        private bool _originLocked;        // set at the ignition, the first moment the body burns (D-045)

        private void Awake()
        {
            Bounds union = default;
            bool hasBounds = false;

            for (int i = 0; i < _surfaces.Length; i++)
            {
                MeshRenderer surface = _surfaces[i];
                if (surface == null)
                    continue;

                if (hasBounds)
                    union.Encapsulate(surface.bounds);
                else
                {
                    union = surface.bounds;
                    hasBounds = true;
                }
            }

            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            _centerLocal = hasBounds ? worldToLocal.MultiplyPoint(union.center) : Vector3.zero;

            _cornersLocal = new Vector3[8];
            for (int c = 0; c < 8; c++)
                _cornersLocal[c] = worldToLocal.MultiplyPoint(union.center + new Vector3(
                    (c & 1) == 0 ? -union.extents.x : union.extents.x,
                    (c & 2) == 0 ? -union.extents.y : union.extents.y,
                    (c & 4) == 0 ? -union.extents.z : union.extents.z));

            _meshMinLocal = _cornersLocal[0];
            _meshMaxLocal = _cornersLocal[0];
            for (int c = 1; c < 8; c++)
            {
                _meshMinLocal = Vector3.Min(_meshMinLocal, _cornersLocal[c]);
                _meshMaxLocal = Vector3.Max(_meshMaxLocal, _cornersLocal[c]);
            }

            CacheChain(_fireParticles, out _fireChain, out _fireBaseScales);
            CacheChain(_smokeParticles, out _smokeChain, out _smokeBaseScales);
        }

        /// The system's root plus everything nested under it, with the authored scale of each. The root comes
        /// first, so `chain[0]` is the one the shell moves.
        private static void CacheChain(ParticleSystem system, out Transform[] chain, out Vector3[] scales)
        {
            if (system == null)
            {
                chain = Array.Empty<Transform>();
                scales = Array.Empty<Vector3>();
                return;
            }

            chain = system.GetComponentsInChildren<Transform>(true);
            scales = new Vector3[chain.Length];
            for (int i = 0; i < chain.Length; i++)
                scales[i] = chain[i].localScale;
        }

        /// A nested system under a disabled root is not rendered, so the whole chain is switched on.
        private static void ActivateChain(Transform[] chain)
        {
            for (int i = 0; i < chain.Length; i++)
                chain[i].gameObject.SetActive(true);
        }

        /// The blob's coverage number: the farthest corner from `originLocal`, in object units. At `Burn` 1 the
        /// front must pass every vertex. The 1-unit floor keeps a meshless shell from turning the blob into a
        /// dot.
        private float MaxDistanceFrom(Vector3 originLocal)
        {
            float reach = 1f;
            for (int i = 0; i < _cornersLocal.Length; i++)
                reach = Mathf.Max(reach, Vector3.Distance(_cornersLocal[i], originLocal));

            return reach;
        }

        /// Composition: the preset the Inspector carries becomes the domain object (D-012), called before the
        /// first tick. A recomposition is a restart: the origin unlocks with the char.
        public void Construct(IBurnable burnable, SmokePuffPool puffPool)
        {
            _burnable = burnable;
            _puffPool = puffPool;
            _block = new MaterialPropertyBlock();

            _burnPointLocal = _centerLocal;
            _originLocked = false;
            WriteGeometry(_burnPointLocal, MaxDistanceFrom(_burnPointLocal));

            if (_burnRoot != null)
                _burnRoot.SetActive(true);

            ActivateChain(_fireChain);
            ActivateChain(_smokeChain);
        }

        /// One stream contact, from the spray or from the fire spread (`IHeatSink`). **The sign picks which
        /// cosmetic fact it is, and that mapping is the shell's business** (the fire rules are the domain's,
        /// D-046, G-036): fire carries the contact to the ignition, water plays the splash at the contact.
        /// **The contact is clamped to the mesh's surface for both** — a spray hit point is already on it, an
        /// off-body contact lands the splash on the face turned to it, same clamp as the origin. No point
        /// (rain, a room-wide soak) means the centre. The origin itself is written at the ignition, in
        /// `UpdateLogic`. Puff count and capacity are the pool's business, and the splash plays at the pool's
        /// full scale: the axis has no water-strength number, the amount is heat, not volume.
        public void ApplyStream(float amount, Vector3? contactPoint)
        {
            if (_burnable == null)
                return;

            Vector3 worldPoint = contactPoint.HasValue ? contactPoint.Value : transform.position;
            Vector3 surfaceLocal = SurfacePointLocal(worldPoint);

            if (amount < 0f)
            {
                if (_puffPool != null && _burnable.IgnitionStrength > 0)
                    _puffPool.Spawn(transform.TransformPoint(surfaceLocal), 1f);
            }
            else if (contactPoint.HasValue && !_originLocked)
                _burnPointLocal = surfaceLocal;

            _burnable.AffectIgnitionStrength(amount);
        }

        /// The point the fire burns from, in object space: the contact clamped to the mesh's box. A spray hit
        /// point is already on the surface, so the clamp leaves it where it is; a distant burning neighbour
        /// lands the origin on the face turned to him, which is where a real fire starts eating a body.
        private Vector3 SurfacePointLocal(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);

            local.x = Mathf.Clamp(local.x, _meshMinLocal.x, _meshMaxLocal.x);
            local.y = Mathf.Clamp(local.y, _meshMinLocal.y, _meshMaxLocal.y);
            local.z = Mathf.Clamp(local.z, _meshMinLocal.z, _meshMaxLocal.z);

            return local;
        }

        public void UpdateLogic(float time, float deltaTime)
        {
            if (_burnable == null)
                return;

            // The ignition is the origin: the body crosses its threshold, and the char front is measured from
            // the contact that got it there.
            if (!_originLocked && _burnable.FireLevel > 0f)
            {
                _originLocked = true;
                WriteGeometry(_burnPointLocal, MaxDistanceFrom(_burnPointLocal));
            }

            // The copies, no filtering. `GetPropertyBlock` reads the geometry back, so the origin and the
            // extent reach the shader once.
            for (int i = 0; i < _surfaces.Length; i++)
            {
                MeshRenderer surface = _surfaces[i];
                if (surface == null)
                    continue;

                surface.GetPropertyBlock(_block);
                _block.SetFloat(BurnId, _burnable.Charredness);
                _block.SetFloat(FireId, _burnable.FireLevel);
                surface.SetPropertyBlock(_block);
            }

            CopyEmission(_fireParticles, _fireParticlesPerSecond, _burnable.FireLevel);
            CopyEmission(_smokeParticles, _smokeParticlesPerSecond,
                _burnable.IsBurnedOut ? 0f : _burnable.IgnitionStrength);

            MoveChain(_fireChain, _fireBaseScales, _burnable.Charredness, Mathf.Lerp(_fireScaleRange.x , _fireScaleRange.y, _burnable.Charredness));
            MoveChain(_smokeChain, _smokeBaseScales, _burnable.Charredness, SmokeScale(_burnable.IgnitionStrength));

            if (_burnable.IsBurnedOut && _burnRoot != null)
                _burnRoot.SetActive(false);

            #region Readonly Info
#if UNITY_EDITOR
            CurrentBurnDurability = _burnable.CurrentDurability;
            IgnitionStrength = _burnable.IgnitionStrength;
#endif
            #endregion
        }

        /// The copy: the 0..1 number the domain owns, written as an emission rate. A shell that filters or
        /// smooths an intensity has rules in it.
        private static void CopyEmission(ParticleSystem system, float maxRate, float intensity)
        {
            if (system == null)
                return;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = intensity > 0.01f;
            emission.rateOverTimeMultiplier = maxRate * intensity;
        }

        /// The root's position moves the whole chain on `Charredness`; every system scales by its own local
        /// scale, so the authored scale of each is multiplied by the ramp the caller gives, component by
        /// component. `Charredness` 0 is the ignition, so a warming body sits at the burn point at min scale
        /// and does not move.
        private void MoveChain(Transform[] chain, Vector3[] baseScales, float charredness, float scale)
        {
            if (chain.Length == 0)
                return;

            chain[0].localPosition = Vector3.Lerp(_burnPointLocal, _centerLocal, charredness);

            for (int i = 0; i < chain.Length; i++)
                chain[i].localScale = Vector3.Scale(baseScales[i], Vector3.one * scale);
        }

        /// The smoke ramp: the plume grows with the heat to `_smokeMaxScale` at heat 0.5, then settles to the
        /// midpoint at heat 1 — the thickest column is the mid-hot body eating, a burnt-out prop exhales at
        /// half, not at the peak (D-054).
        private float SmokeScale(float heat)
        {
            float ramp = heat < 0.5f ? heat * 2f : 1.5f - heat;
            return _smokeScaleRange.x + (_smokeScaleRange.y - _smokeScaleRange.x) * ramp;
        }

        /// Geometry travels once per object, never per frame.
        private void WriteGeometry(Vector3 originLocal, float extent)
        {
            for (int i = 0; i < _surfaces.Length; i++)
            {
                MeshRenderer surface = _surfaces[i];
                if (surface == null)
                    continue;

                surface.GetPropertyBlock(_block);
                _block.SetVector(BurnOriginId, originLocal);
                _block.SetFloat(BurnExtentId, extent);
                surface.SetPropertyBlock(_block);
            }
        }
    }
}
