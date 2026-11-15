using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Per-handle drag bookkeeping for rotate / transform handles. One instance
    /// backs the shared pivot handle, and each object pivot gets its own.
    ///
    /// The update methods MUST run every frame right after their handle is drawn
    /// (even when nothing changed), in the same order as the statements they
    /// replace: freeze on drag start, reset the global rotation gizmo on drag
    /// end, unfreeze on drag end, then latch the dragging flag. Reordering these
    /// makes the handles jump or drift mid-drag.
    /// </summary>
    internal sealed class PPP_PivotDragState
    {
        /// <summary>Accumulated gizmo rotation while in Global pivot rotation mode.</summary>
        public Quaternion globalRotationHandleState = Quaternion.identity;
        public bool wasDragging;

        /// <summary>
        /// While a drag is in progress the pivot is frozen at its drag-start
        /// position, so the handle doesn't chase the bounds it is itself moving.
        /// </summary>
        public bool pivotFrozen;
        public Vector3 frozenPivotPosition;

        /// <summary>
        /// Post-draw update for handles that freeze the pivot while dragging
        /// (rotate and transform). <paramref name="allowFreeze"/> is false for a
        /// single-target shared handle, whose pivot follows the object directly.
        /// </summary>
        public void UpdateAfterFreezableHandle(Vector3 pivotPosition, bool allowFreeze)
        {
            bool isDragging = GUIUtility.hotControl != 0;

            if (allowFreeze && !wasDragging && isDragging)
            {
                pivotFrozen = true;
                frozenPivotPosition = pivotPosition;
            }

            if (Tools.pivotRotation != PivotRotation.Local && wasDragging && !isDragging)
                globalRotationHandleState = Quaternion.identity;

            if (wasDragging && !isDragging)
                pivotFrozen = false;

            wasDragging = isDragging;
        }

        /// <summary>
        /// Post-draw update for pivot-edit handles, which never freeze but still
        /// reset the global rotation gizmo when a drag ends.
        /// </summary>
        public void UpdateAfterEditHandle()
        {
            bool isDragging = GUIUtility.hotControl != 0;

            if (Tools.pivotRotation != PivotRotation.Local && wasDragging && !isDragging)
                globalRotationHandleState = Quaternion.identity;

            wasDragging = isDragging;
        }
    }
}
