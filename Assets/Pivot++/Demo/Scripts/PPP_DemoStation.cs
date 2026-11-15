#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using PivotPlusPlus.EditorStuff;

namespace PivotPlusPlus.Demo
{
    public enum DemoTool
    {
        Unchanged = 0,
        Move = 1,
        Rotate = 2,
        Scale = 3,
        Rect = 4,
        Transform = 5,
        View = 6
    }

    public class PPP_DemoStation : MonoBehaviour
    {
        [field: SerializeField] public int Index { get; private set; }
        [field: SerializeField] public string Name { get; private set; }
        [SerializeField, TextArea(3, 12)] private string _description;
        [SerializeField] private bool _isNewInV11;
        [SerializeField] private Pose _cameraPose;
        [SerializeField] private float _cameraSize;

        [Header("Lights")]
        [SerializeField] private GameObject[] _activeWithPanel;

        [SerializeField] private MeshRenderer[] _handleGlowMesh;
        [SerializeField] private Material _emissiveMaterial;
        [SerializeField] private Material _diffuseMaterial;

        [Header("Pivot++ Settings")]
        [SerializeField] private GameObject _selectObject;

        [Tooltip("Transform tool to put in the user's hand at this station, e.g. Rotate when the " +
                 "point of the station is to rotate something. Unchanged leaves their current tool alone.")]
        [SerializeField] private DemoTool _activeTool = DemoTool.Unchanged;

        [SerializeField] private SettingsRef _settings;

        public string Description => _description;
        public bool IsNewInV11 => _isNewInV11;
        public Vector3 CameraPivot => transform.position + _cameraPose.position;
        public Quaternion CameraRotation => _cameraPose.rotation;
        public float CameraSize => _cameraSize;

        [ContextMenu("Save Camera Settings")]
        private void SaveCameraSettings()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null)
            {
                sv = EditorWindow.GetWindow<SceneView>();
                if (sv == null)
                {
                    Debug.LogWarning("[Pivot++ Demo] No Scene view is open to save camera settings.");
                    return;
                }
            }

            _cameraPose = new Pose(sv.pivot - transform.position, sv.rotation);
            _cameraSize = sv.size;

            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Capture Object Pivots")]
        private void CaptureObjectPivots()
        {
            _settings.multiPivotEntries.Clear();

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                GameObject target = child.gameObject;

                Pose[] offsets = PPP_Settings.GetMultiPivotOffsets(target);
                if (offsets.Length == 0)
                    continue;

                // GetMultiPivotOffsets hands back the settings' own array, so the station takes a
                // copy rather than a window onto state the user keeps editing.
                _settings.multiPivotEntries.Add(new MultiPivotEntryRef
                {
                    target = target,
                    offsets = (Pose[])offsets.Clone(),
                    pivotMode = PPP_Settings.GetMultiPivotMode(target)
                });
            }

            EditorUtility.SetDirty(this);
            Debug.Log($"[Pivot++ Demo] Station {Index}: captured {_settings.multiPivotEntries.Count} " +
                      "object pivot entr(ies).", this);
        }

        public void Activate()
        {
            gameObject.SetActive(true);
            EditorUtility.SetDirty(gameObject);

            foreach (GameObject go in _activeWithPanel)
            {
                if (go == null) continue;

                go.SetActive(true);
                EditorUtility.SetDirty(go);
            }

            foreach (MeshRenderer meshRenderer in _handleGlowMesh)
            {
                if (meshRenderer == null) continue;
                
                meshRenderer.material = _emissiveMaterial;
                EditorUtility.SetDirty(meshRenderer);
            }

            PPP_Settings.ApplySettings(_settings);
            Selection.activeGameObject = _selectObject;
            ApplyTool();
        }

        public void Deactivate(PPP_DemoStation arrived)
        {
            gameObject.SetActive(false);
            EditorUtility.SetDirty(gameObject);

            foreach (GameObject go in _activeWithPanel)
            {
                if (go == null) continue;
                if (arrived._activeWithPanel.Contains(go)) continue;

                go.SetActive(false);
                EditorUtility.SetDirty(go);
            }

            foreach (MeshRenderer meshRenderer in _handleGlowMesh)
            {
                if (meshRenderer == null) continue;
                if (arrived._handleGlowMesh.Contains(meshRenderer)) continue;

                meshRenderer.material = _diffuseMaterial;
                EditorUtility.SetDirty(meshRenderer);
            }
        }

        /// <summary>
        /// Re-selects this station's object and re-arms its tool, without touching the Pivot++
        /// settings. Called once the camera has arrived, so a click in the Scene view during the
        /// flight does not leave the wrong object under the handles - and so a slider the user has
        /// already grabbed is not reset.
        /// </summary>
        public void RestoreSelection()
        {
            if (_selectObject != null)
                Selection.activeGameObject = _selectObject;

            ApplyTool();
        }

        /// <summary>
        /// Puts the station's tool in the user's hand. A station that says nothing about tools
        /// leaves whatever the user last picked, so this stays out of the way by default.
        /// </summary>
        private void ApplyTool()
        {
            switch (_activeTool)
            {
                case DemoTool.Move: Tools.current = Tool.Move; break;
                case DemoTool.Rotate: Tools.current = Tool.Rotate; break;
                case DemoTool.Scale: Tools.current = Tool.Scale; break;
                case DemoTool.Rect: Tools.current = Tool.Rect; break;
                case DemoTool.Transform: Tools.current = Tool.Transform; break;
                case DemoTool.View: Tools.current = Tool.View; break;
                default: return;
            }

            // Tools.current does not repaint the scene view on its own, so without this the new
            // handle only appears once something else happens to trigger a redraw.
            SceneView.RepaintAll();
        }
    }
}
#endif
