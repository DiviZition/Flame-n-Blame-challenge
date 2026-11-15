using System;
using System.Collections.Generic;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    public enum UnitMode
    {
        Bounded = 0,
        Unbounded = 1
    }

    public enum PivotMode
    {
        Default = 0,
        Edit = 1
    }

    /// <summary>Which chrome the Pivot++ panel is shown in.</summary>
    public enum PPP_PanelMode
    {
        /// <summary>A scene view overlay, dockable only inside the scene view.</summary>
        Overlay = 0,
        /// <summary>A real editor window, dockable anywhere in the layout.</summary>
        Window = 1
    }

    public enum CanvasBoundsMode
    {
        RectTransform = 0,
        PreferredSize = 1,
        MinimumSize = 2
    }
    
    [Serializable]
    public struct PivotEntry
    {
        public string objectId;
        public Vector3 sliderValues;
        public Vector3 absoluteValues;
        public UnitMode unitMode;
        // Unbounded mode only: when false, the offset ignores the object's scale.
        public bool useObjectScale;
    }

    [Serializable]
    public struct MultiPivotEntry
    {
        public string objectId;
        public Pose[] offsets;
        public PivotMode pivotMode;
    }

    [Serializable]
    public struct PivotEntryRef
    {
        public GameObject target;
        public Vector3 sliderValues;
        public Vector3 absoluteValues;
        public UnitMode unitMode;
        public bool useObjectScale;
    }

    [Serializable]
    public struct MultiPivotEntryRef
    {
        public GameObject target;
        public Pose[] offsets;
        public PivotMode pivotMode;
    }
    
    [Serializable]
    public class SettingsRef
    {
        public bool enabled;
        public Vector3 commonSliderValues;
        public Vector3 commonAbsoluteValues;
        public UnitMode commonUnitMode;
        public bool commonUseObjectScale = true;
        public List<PivotEntryRef> pivotEntries = new List<PivotEntryRef>();
        public List<MultiPivotEntryRef> multiPivotEntries = new List<MultiPivotEntryRef>();
    }
}