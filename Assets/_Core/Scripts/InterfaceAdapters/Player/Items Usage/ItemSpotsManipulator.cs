using UnityEngine;

namespace Flame_n_Blame_Challenge.Adapters
{
    /// Drives one pivot per hand, authored as the parent of the hand spot and the item spot. Each weight adds
    /// one camera channel and the channels never cross. Position: the pivot blends its authored spot on the
    /// body toward the camera anchor. The camera anchor is NOT a rigid orbit around the camera - a rotating
    /// authored offset threw the hands out of IK reach when looking up and above the camera when looking down.
    /// It is the camera position plus the authored offset, shifted by two authored extreme deltas that lerp on
    /// the camera's pitch: top extreme, bottom extreme, nothing in between leaves the body. Rotation: the
    /// pivot blends the root's yaw toward the camera's rotation. Weight 0: the bone owns the hand, noise only;
    /// weight 1: the pivot rides the camera position and the authored extremes, the pose is the camera's.
    /// Cosmetic tertiary adapter: it decides no game rule, and the item parenting and the IK rigs stay
    /// untouched, because the spots are children of the pivot.
    /// Budget: 2 pivots per tick = 3 Perlin + four Lerps + two rotated offsets + one normalize, zero allocation.
    public class ItemSpotsManipulator : MonoBehaviour, IUpdatable
    {
        [Tooltip("Pivots to drive, right hand first. The wobble phase is spread by index, so the hands move out of sync.")]
        [SerializeField] private Transform[] _pivots;

        [Tooltip("Camera holder: its position is what the position weight follows, its pitch drives the two extremes, its rotation is what the rotation weight follows.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Position weight: 0 - the bone owns the spot, noise only; 1 - the spot rides the camera position and the pitch extremes.")]
        [SerializeField, Range(0f, 1f)] private float _positionFollow = 0.6f;

        [Tooltip("Rotation weight: 0 - the root's yaw owns the pose, camera rotation never touches it; 1 - the pivot takes the camera rotation exactly.")]
        [SerializeField, Range(0f, 1f)] private float _rotationFollow = 0.6f;

        [Tooltip("Camera pitch, in degrees, where the anchor reaches the top extreme; -this reaches the bottom. Full clamp, no orbit.")]
        [SerializeField] private float _pitchRange = 45f;

        [Tooltip("The distance the camera anchor keeps from the camera, in meters. The authored spot and the extremes set direction only; the pivot rides this sphere, so the IK reach never stretches.")]
        [SerializeField] private float _cameraDistance = 0.7f;

        [Tooltip("The authored spot shifted by this at the top extreme, in the camera frame (up / right / forward at rest).")]
        [SerializeField] private Vector3 _topExtremeDelta = new Vector3(0f, 0.2f, 0.15f);

        [Tooltip("The authored spot shifted by this at the bottom extreme, in the camera frame.")]
        [SerializeField] private Vector3 _bottomExtremeDelta = new Vector3(0f, -0.2f, -0.1f);

        [Tooltip("Wobble amplitude in meters, along world axes, so the camera never feeds the noise.")]
        [SerializeField] private float _noiseAmplitude = 0.03f;

        [Tooltip("Perlin coordinate speed, in coordinates per second.")]
        [SerializeField] private float _noiseFrequency = 1.2f;

        [Tooltip("Perlin coordinate distance between the two hands, so their wobble is out of phase.")]
        [SerializeField] private float _noisePhaseSpread = 10f;

        private Vector3 _cameraRestPosition;
        private Quaternion _cameraRestRotation;
        private Transform[] _bones;
        private Transform _root;
        private Vector3[] _restLocalPositions;
        private Vector3[] _cameraSpaceOffsets;

        public void Construct()
        {
            _cameraRestPosition = _cameraTransform.position;
            _cameraRestRotation = _cameraTransform.rotation;
            _root = _cameraTransform.parent;

            int count = _pivots.Length;
            _bones = new Transform[count];
            _restLocalPositions = new Vector3[count];
            _cameraSpaceOffsets = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                _bones[i] = _pivots[i].parent;
                _restLocalPositions[i] = _pivots[i].localPosition;

                // The authored placement, stored in the camera's frame, so the extremes shift the spot the
                // designer put on the bone - no second table of numbers to keep in sync.
                _cameraSpaceOffsets[i] = Quaternion.Inverse(_cameraRestRotation) * (_pivots[i].position - _cameraRestPosition);
            }
        }

        private const float MIN_DIR_SQ = 0.0001f;

        public void UpdateLogic(float time, float deltaTime)
        {
            if (_pivots == null || _pivots.Length == 0)
                return;

            float pitch = Mathf.Asin(Mathf.Clamp(_cameraTransform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            float blend = Mathf.InverseLerp(-_pitchRange, _pitchRange, pitch);

            Quaternion yaw = _root.rotation;

            for (int i = 0; i < _pivots.Length; i++)
            {
                // The body anchor is the bone's translation plus the authored spot turned by the root's yaw.
                // The bone's own rotation is the animator's limb pose and must never swing the authored spot:
                // the leg animation once rotated the offset and drove the hands under the floor (y = -2.3).
                Vector3 boneAnchor = _bones[i].position + yaw * _restLocalPositions[i];

                // Two authored extremes, lerped by pitch, set the direction from the camera; the distance is
                // one authored constant, so the pivot rides a sphere around the camera and the IK reach never
                // stretches. No offset orbit: the hands never swing out of reach on the look up, never rise
                // above the camera on the look down.
                Vector3 dir = _cameraSpaceOffsets[i] + Vector3.Lerp(_bottomExtremeDelta, _topExtremeDelta, blend);

                // An epsilon guard that swallows NaN is the standing trap (G-023): a degenerate direction
                // keeps the authored facing, a visible no-op, not a silent freeze.
                if (dir.sqrMagnitude < MIN_DIR_SQ)
                    dir = Vector3.forward;

                Vector3 cameraAnchor = _cameraTransform.position + yaw * dir.normalized * _cameraDistance;

                Vector3 position = Vector3.Lerp(boneAnchor, cameraAnchor, _positionFollow) + Wobble(i, time);

                // The pose base is the root yaw, not the bone rotation - same reason, the animated limb never
                // owns the hand spot.
                Quaternion rotation = Quaternion.Lerp(yaw, _cameraTransform.rotation, _rotationFollow);

                _pivots[i].SetPositionAndRotation(position, rotation);
            }
        }

        private Vector3 Wobble(int handIndex, float time)
        {
            float phase = handIndex * _noisePhaseSpread;
            float x = Mathf.PerlinNoise(phase, time * _noiseFrequency) - 0.5f;
            float y = Mathf.PerlinNoise(phase + 3.7f, time * _noiseFrequency) - 0.5f;
            float z = Mathf.PerlinNoise(phase + 7.1f, time * _noiseFrequency) - 0.5f;

            return new Vector3(x, y, z) * _noiseAmplitude;
        }
    }
}
