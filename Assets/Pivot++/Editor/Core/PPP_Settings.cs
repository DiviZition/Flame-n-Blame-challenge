using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Persistent plugin state: the common (fallback) pivot offset plus optional
    /// per-object overrides and per-object extra pivots, keyed by GlobalObjectId.
    /// </summary>
    [FilePath("UserSettings/PivotPlusPlusSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class PPP_Settings : ScriptableSingleton<PPP_Settings>
    {
        public static event Action OnSettingsChanged;

        [SerializeField] private bool _enabled;
        [SerializeField] private Vector3 _sliderValues;
        [SerializeField] private Vector3 _absoluteValues;
        [SerializeField] private UnitMode _commonUnitMode;
        [SerializeField] private CanvasBoundsMode _canvasBoundsMode;
        [SerializeField] private bool _useObjectScale = true;
        [SerializeField] private List<PivotEntry> _pivotEntries = new List<PivotEntry>();
        [SerializeField] private List<MultiPivotEntry> _multiPivotEntries = new List<MultiPivotEntry>();

        // Demo related flags
        [SerializeField] private bool _showWelcomeWindowOnStartup = true;
        [SerializeField] private string _welcomeWindowShownVersion = "";

        private Dictionary<string, int> _indexLookup;
        private Dictionary<string, int> _multiPivotIndexLookup;

        private delegate void PivotEntryMutator(ref PivotEntry entry);

        public static bool Enabled
        {
            get => instance._enabled;
            set
            {
                if (instance._enabled == value)
                    return;

                instance._enabled = value;
                instance.Save(true);
            }
        }

        public static Vector3 CommonSliderValues
        {
            get => instance._sliderValues;
            set
            {
                if (instance._sliderValues == value)
                    return;

                instance._sliderValues = value;
                instance.Save(true);
            }
        }

        public static Vector3 CommonAbsoluteValues
        {
            get => instance._absoluteValues;
            set
            {
                if (instance._absoluteValues == value)
                    return;

                instance._absoluteValues = value;
                instance.Save(true);
            }
        }

        public static UnitMode CommonUnitMode
        {
            get => instance._commonUnitMode;
            set
            {
                if (instance._commonUnitMode == value)
                    return;

                instance._commonUnitMode = value;
                instance.Save(true);
            }
        }
        
        public static CanvasBoundsMode CanvasBoundsMode
        {
            get => instance._canvasBoundsMode;
            set
            {
                if (instance._canvasBoundsMode == value)
                    return;

                instance._canvasBoundsMode = value;
                instance.Save(true);
            }
        }

        public static bool CommonUseObjectScale
        {
            get => instance._useObjectScale;
            set
            {
                if (instance._useObjectScale == value)
                    return;

                instance._useObjectScale = value;
                instance.Save(true);
            }
        }

        public static bool ShowWelcomeWindowOnStartup
        {
            get => instance._showWelcomeWindowOnStartup;
            set
            {
                if (instance._showWelcomeWindowOnStartup == value)
                    return;

                instance._showWelcomeWindowOnStartup = value;
                instance.Save(true);
            }
        }

        public static string WelcomeWindowShownVersion
        {
            get => instance._welcomeWindowShownVersion;
            set
            {
                if (instance._welcomeWindowShownVersion == value)
                    return;

                instance._welcomeWindowShownVersion = value;
                instance.Save(true);
            }
        }

        public static Vector3 GetSliderValues(UnityEngine.Object target)
        {
            return TryGetEntry(target, out PivotEntry entry) ? entry.sliderValues : instance._sliderValues;
        }

        public static void SetSliderValues(UnityEngine.Object target, Vector3 value)
        {
            if (!TrySetEntry(target, (ref PivotEntry entry) => entry.sliderValues = value))
                CommonSliderValues = value;
        }

        public static Vector3 GetAbsoluteValues(UnityEngine.Object target)
        {
            return TryGetEntry(target, out PivotEntry entry) ? entry.absoluteValues : instance._absoluteValues;
        }

        public static void SetAbsoluteValues(UnityEngine.Object target, Vector3 value)
        {
            if (!TrySetEntry(target, (ref PivotEntry entry) => entry.absoluteValues = value))
                CommonAbsoluteValues = value;
        }

        public static UnitMode GetUnitMode(UnityEngine.Object target)
        {
            return TryGetEntry(target, out PivotEntry entry) ? entry.unitMode : instance._commonUnitMode;
        }

        public static void SetUnitMode(UnityEngine.Object target, UnitMode value)
        {
            if (!TrySetEntry(target, (ref PivotEntry entry) => entry.unitMode = value))
                CommonUnitMode = value;
        }

        public static bool GetUseObjectScale(UnityEngine.Object target)
        {
            return TryGetEntry(target, out PivotEntry entry) ? entry.useObjectScale : instance._useObjectScale;
        }

        public static void SetUseObjectScale(UnityEngine.Object target, bool value)
        {
            if (!TrySetEntry(target, (ref PivotEntry entry) => entry.useObjectScale = value))
                CommonUseObjectScale = value;
        }

        /// <summary>
        /// Records the settings singleton onto Unity's Undo stack. Must be called
        /// *before* mutating pivot data, because GetMultiPivotOffsets hands back the
        /// internal array by reference and callers edit it in place. Repeated calls
        /// with the same name during a continuous drag collapse into one undo step.
        /// </summary>
        public static void RecordUndo(string operationName)
        {
            Undo.RecordObject(instance, operationName);
        }

        /// <summary>
        /// Restores runtime state after an undo/redo. The lookup dictionaries are not
        /// serialized, so they go stale when Undo swaps the entry lists back in.
        /// </summary>
        public static void OnUndoRedoPerformed()
        {
            instance.RebuildLookup();
            instance.RebuildMultiPivotLookup();
            OnSettingsChanged?.Invoke();
        }

        public static Pose[] GetMultiPivotOffsets(UnityEngine.Object target)
        {
            if (target == null)
                return Array.Empty<Pose>();

            int index = instance.FindMultiPivotEntryIndex(GetObjectId(target));
            if (index < 0 || instance._multiPivotEntries[index].offsets == null)
                return Array.Empty<Pose>();

            return instance._multiPivotEntries[index].offsets;
        }

        public static void SetMultiPivotOffsets(UnityEngine.Object target, Pose[] offsets)
        {
            if (offsets == null || offsets.Length == 0)
            {
                int existingIndex = instance.FindMultiPivotEntryIndex(GetObjectId(target));
                if (existingIndex >= 0)
                {
                    instance._multiPivotEntries.RemoveAt(existingIndex);
                    instance.RebuildMultiPivotLookup();
                    instance.Save(true);
                    OnSettingsChanged?.Invoke();
                }
                return;
            }

            instance.GetOrCreateMultiPivotEntry(target, out int index);
            MultiPivotEntry entry = instance._multiPivotEntries[index];
            int previousCount = entry.offsets != null ? entry.offsets.Length : 0;
            entry.offsets = offsets;
            instance._multiPivotEntries[index] = entry;
            instance.Save(true);

            // The overlay only needs to react to structural changes (pivot added or
            // removed), not to every drag frame while a pivot is repositioned.
            if (previousCount != offsets.Length)
                OnSettingsChanged?.Invoke();
        }

        public static PivotMode GetMultiPivotMode(UnityEngine.Object target)
        {
            int index = instance.FindMultiPivotEntryIndex(GetObjectId(target));
            if (index < 0)
                return PivotMode.Default;

            return instance._multiPivotEntries[index].pivotMode == PivotMode.Edit
                ? PivotMode.Edit
                : PivotMode.Default;
        }

        public static void SetMultiPivotMode(UnityEngine.Object target, PivotMode value)
        {
            instance.GetOrCreateMultiPivotEntry(target, out int index);
            MultiPivotEntry entry = instance._multiPivotEntries[index];
            entry.pivotMode = value;
            instance._multiPivotEntries[index] = entry;
            instance.Save(true);
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Runs a change that gives the objects under <paramref name="root"/> new
        /// GlobalObjectIds (unpacking a prefab instance does), then moves their
        /// Pivot++ data over to the new ids so their pivots survive it.
        /// </summary>
        public static void PreserveEntriesAcross(Transform root, Action change, string undoName)
        {
            List<(GameObject target, string oldId)> owned = instance.FindEntriesUnder(root);
            change();

            bool changed = false;
            foreach ((GameObject target, string oldId) in owned)
            {
                if (target == null)
                    continue;

                string newId = GetObjectId(target);
                if (newId == oldId)
                    continue;

                if (!changed)
                    RecordUndo(undoName);

                instance.ReplaceId(oldId, newId);
                changed = true;
            }

            if (!changed)
                return;

            instance.RebuildLookup();
            instance.RebuildMultiPivotLookup();
            instance.Save(true);
            OnSettingsChanged?.Invoke();
        }

        private List<(GameObject, string)> FindEntriesUnder(Transform root)
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (PivotEntry entry in _pivotEntries)
                ids.Add(entry.objectId);
            foreach (MultiPivotEntry entry in _multiPivotEntries)
                ids.Add(entry.objectId);

            List<(GameObject, string)> result = new List<(GameObject, string)>();
            foreach (string id in ids)
            {
                if (!GlobalObjectId.TryParse(id, out GlobalObjectId globalId))
                    continue;

                if (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) is GameObject target &&
                    target.transform.IsChildOf(root))
                    result.Add((target, id));
            }

            return result;
        }

        private void ReplaceId(string oldId, string newId)
        {
            for (int i = 0; i < _pivotEntries.Count; i++)
            {
                if (_pivotEntries[i].objectId != oldId)
                    continue;

                PivotEntry entry = _pivotEntries[i];
                entry.objectId = newId;
                _pivotEntries[i] = entry;
            }

            for (int i = 0; i < _multiPivotEntries.Count; i++)
            {
                if (_multiPivotEntries[i].objectId != oldId)
                    continue;

                MultiPivotEntry entry = _multiPivotEntries[i];
                entry.objectId = newId;
                _multiPivotEntries[i] = entry;
            }
        }

        public static void ApplySettings(SettingsRef settings)
        {
            if (settings == null)
                return;

            Enabled = settings.enabled;
            CommonSliderValues = settings.commonSliderValues;
            CommonAbsoluteValues = settings.commonAbsoluteValues;
            CommonUnitMode = settings.commonUnitMode;
            CommonUseObjectScale = settings.commonUseObjectScale;

            foreach (PivotEntryRef entry in settings.pivotEntries)
                instance.UpdateOrAdd(entry);

            foreach (MultiPivotEntryRef entry in settings.multiPivotEntries)
                instance.UpdateOrAdd(entry);

            instance.RebuildLookup();
            instance.RebuildMultiPivotLookup();
            instance.Save(true);
            OnSettingsChanged?.Invoke();
        }

        private static bool TryGetEntry(UnityEngine.Object target, out PivotEntry entry)
        {
            if (target != null)
            {
                int index = instance.FindEntryIndex(GetObjectId(target));
                if (index >= 0)
                {
                    entry = instance._pivotEntries[index];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// Mutates the target's per-object entry if it has one. Returns false when
        /// there is no entry, in which case the caller falls back to the common value.
        /// </summary>
        private static bool TrySetEntry(UnityEngine.Object target, PivotEntryMutator mutate)
        {
            if (target == null)
                return false;

            int index = instance.FindEntryIndex(GetObjectId(target));
            if (index < 0)
                return false;

            PivotEntry entry = instance._pivotEntries[index];
            mutate(ref entry);
            instance._pivotEntries[index] = entry;
            instance.Save(true);
            return true;
        }

        private void GetOrCreateMultiPivotEntry(UnityEngine.Object target, out int index)
        {
            string id = GetObjectId(target);
            index = FindMultiPivotEntryIndex(id);
            if (index >= 0)
                return;

            _multiPivotEntries.Add(new MultiPivotEntry { objectId = id, offsets = Array.Empty<Pose>() });
            index = _multiPivotEntries.Count - 1;
            RebuildMultiPivotLookup();
        }

        private int FindEntryIndex(string id)
        {
            if (_indexLookup == null)
                RebuildLookup();

            return _indexLookup.TryGetValue(id, out int index) ? index : -1;
        }

        private int FindMultiPivotEntryIndex(string id)
        {
            if (_multiPivotIndexLookup == null)
                RebuildMultiPivotLookup();

            return _multiPivotIndexLookup.TryGetValue(id, out int index) ? index : -1;
        }

        private void RebuildLookup()
        {
            _indexLookup = new Dictionary<string, int>();
            for (int i = 0; i < _pivotEntries.Count; i++)
                _indexLookup[_pivotEntries[i].objectId] = i;
        }

        private void RebuildMultiPivotLookup()
        {
            _multiPivotIndexLookup = new Dictionary<string, int>();
            for (int i = 0; i < _multiPivotEntries.Count; i++)
                _multiPivotIndexLookup[_multiPivotEntries[i].objectId] = i;
        }

        private void UpdateOrAdd(PivotEntryRef entryRef)
        {
            if (entryRef.target == null)
                return;

            string id = GetObjectId(entryRef.target);
            PivotEntry entry = new PivotEntry
            {
                objectId = id,
                sliderValues = entryRef.sliderValues,
                absoluteValues = entryRef.absoluteValues,
                unitMode = entryRef.unitMode,
                useObjectScale = entryRef.useObjectScale
            };

            int index = FindEntryIndex(id);
            if (index >= 0)
                _pivotEntries[index] = entry;
            else
                _pivotEntries.Add(entry);
        }

        private void UpdateOrAdd(MultiPivotEntryRef entryRef)
        {
            if (entryRef.target == null)
                return;

            string id = GetObjectId(entryRef.target);
            MultiPivotEntry entry = new MultiPivotEntry
            {
                objectId = id,
                offsets = entryRef.offsets,
                pivotMode = entryRef.pivotMode
            };

            int index = FindMultiPivotEntryIndex(id);
            if (index >= 0)
                _multiPivotEntries[index] = entry;
            else
                _multiPivotEntries.Add(entry);
        }

        private static string GetObjectId(UnityEngine.Object target)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
        }
    }
}
