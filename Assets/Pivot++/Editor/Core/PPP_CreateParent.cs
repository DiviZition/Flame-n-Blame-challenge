using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// "Create Parent at Pivot": inserts an empty GameObject at a pivot and moves
    /// the selection into it. The insertion itself is simple; most of this class
    /// resolves the situations where it can't just be done - prefab instances,
    /// objects spread over several parents, components that rely on the hierarchy -
    /// by asking the user through <see cref="PPP_Popup"/>.
    /// </summary>
    internal static class PPP_CreateParent
    {
        private const string DEFAULT_NAME = "New Parent";
        private const string UNDO_NAME = "Create Parent at Pivot";
        private const int MAX_LISTED_NAMES = 3;

        // Resolved by name so projects with the Tilemap / Grid modules disabled still compile.
        private static readonly Type TilemapType = Type.GetType("UnityEngine.Tilemaps.Tilemap, UnityEngine.TilemapModule");
        private static readonly Type GridType = Type.GetType("UnityEngine.Grid, UnityEngine.GridModule");

        private enum PrefabChoice
        {
            Cancel,
            Unpack,
            OpenPrefab,
            ParentPrefabRoot
        }

        /// <summary>One parent to create: where it goes and what moves into it.</summary>
        private class ParentRequest
        {
            public List<Transform> children;
            public Transform destination; // null = scene root
            public Scene scene;
            public Vector3 position;
            public Quaternion rotation;
        }

        // ---------------------------------------------------------- availability

        internal static bool CanCreateAtSharedPivot(out string reason)
        {
            return CanCreate(Selection.transforms, out reason);
        }

        internal static bool CanCreateAtObjectPivot(Transform target, out string reason)
        {
            return CanCreate(new[] { target }, out reason);
        }

        /// <summary>
        /// Hard blockers only. Everything that can be resolved by a choice (prefab
        /// instances, different parents...) is asked about when the action runs.
        /// </summary>
        private static bool CanCreate(IReadOnlyList<Transform> targets, out string reason)
        {
            reason = null;

            if (targets.Count == 0 || targets[0] == null)
            {
                reason = "No object selected.";
                return false;
            }

            Scene scene = targets[0].gameObject.scene;
            foreach (Transform target in targets)
            {
                if (target.gameObject.scene != scene)
                {
                    reason = "Objects are in different scenes.";
                    return false;
                }

                if ((target.gameObject.hideFlags & HideFlags.NotEditable) != 0)
                {
                    reason = $"'{target.name}' is not editable.";
                    return false;
                }

                if (IsPrefabStageRoot(target))
                {
                    reason = "The root of the prefab being edited can't have a parent.";
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------------------ entry points

        /// <summary>Creates a parent at the shared pivot and moves the selection into it.</summary>
        internal static void CreateAtSharedPivot()
        {
            Transform[] targets = Selection.transforms;
            Transform activeTransform = Selection.activeTransform;
            if (activeTransform == null || !CanCreate(targets, out _))
                return;

            if (!PPP_Handles.TryGetSharedPivotPosition(targets, activeTransform, out Vector3 pivotPosition))
                return;

            Quaternion rotation = PPP_SharedPivotHandles.GetHandleRotation(activeTransform);
            Run(targets.ToList(), pivotPosition, rotation);
        }

        /// <summary>Creates a parent at one of the object's own pivots, oriented like that pivot.</summary>
        internal static void CreateAtObjectPivot(Transform target, int pivotIndex)
        {
            if (target == null || !CanCreate(new[] { target }, out _))
                return;

            Pose[] offsets = PPP_Settings.GetMultiPivotOffsets(target.gameObject);
            if (pivotIndex < 0 || pivotIndex >= offsets.Length)
                return;

            Pose pose = offsets[pivotIndex];
            Vector3 position = target.TransformPoint(pose.position);
            Quaternion rotation = PPP_PivotMath.GetSafeRotation(target.rotation * pose.rotation);
            Run(new List<Transform> { target }, position, rotation);
        }

        // ------------------------------------------------------------------ flow

        /// <summary>
        /// Asks every question first and only then touches the scene, so cancelling
        /// any popup leaves everything exactly as it was.
        /// </summary>
        private static void Run(List<Transform> targets, Vector3 pivotPosition, Quaternion rotation)
        {
            bool unpack = false;

            List<Transform> locked = targets.Where(IsLockedByPrefab).ToList();
            if (locked.Count > 0)
            {
                switch (AskPrefabResolution(targets, locked))
                {
                    case PrefabChoice.Unpack:
                        unpack = true;
                        break;
                    case PrefabChoice.OpenPrefab:
                        OpenPrefabAndRun(targets, pivotPosition, rotation);
                        return;
                    case PrefabChoice.ParentPrefabRoot:
                        targets = ReplaceLockedWithPrefabRoots(targets);
                        break;
                    default:
                        return;
                }
            }

            List<ParentRequest> requests = BuildRequests(targets, pivotPosition, rotation);
            if (requests == null)
                return;

            List<string> warnings = CollectWarnings(requests);
            if (warnings.Count > 0 && !PPP_Popup.Confirm("This may break something",
                    "Adding a parent changes the hierarchy that these objects rely on:",
                    warnings, "Create Anyway"))
                return;

            Execute(requests, unpack);
        }

        private static List<ParentRequest> BuildRequests(List<Transform> targets, Vector3 pivotPosition, Quaternion rotation)
        {
            Scene scene = targets[0].gameObject.scene;

            HashSet<Transform> parents = new HashSet<Transform>(targets.Select(t => t.parent));
            if (parents.Count == 1)
                return new List<ParentRequest> { CreateRequest(targets, targets[0].parent, scene, pivotPosition, rotation) };

            Transform ancestor = FindCommonAncestor(targets);
            string ancestorText = ancestor != null ? $"under '{ancestor.name}', their closest common parent" : "at the scene root";

            int choice = PPP_Popup.Choose("Selected objects have different parents",
                "The selected objects don't share the same parent, so there are two ways to add a parent.",
                new PPP_Popup.Option
                {
                    title = "A new parent for each object",
                    description = "Each object gets its own new parent, placed at that object's own pivot. " +
                                  "Every object keeps its current place in the hierarchy."
                },
                new PPP_Popup.Option
                {
                    title = "One parent for all objects",
                    description = $"Creates a single parent at the current pivot, {ancestorText}, " +
                                  "and moves all selected objects into it."
                });

            switch (choice)
            {
                case 0:
                    return targets.Select(t => CreateOwnPivotRequest(t, scene, rotation)).ToList();
                case 1:
                    return new List<ParentRequest> { CreateRequest(targets, ancestor, scene, pivotPosition, rotation) };
                default:
                    return null;
            }
        }

        private static ParentRequest CreateRequest(List<Transform> children, Transform destination, Scene scene,
            Vector3 position, Quaternion rotation)
        {
            return new ParentRequest
            {
                children = children,
                destination = destination,
                scene = scene,
                position = position,
                rotation = rotation
            };
        }

        /// <summary>A parent at the object's own pivot, oriented like its handle would be.</summary>
        private static ParentRequest CreateOwnPivotRequest(Transform target, Scene scene, Quaternion globalRotation)
        {
            Vector3 position = PPP_Handles.TryGetSharedPivotWorldPosition(target, out Vector3 pivot)
                ? pivot
                : target.position;
            Quaternion rotation = Tools.pivotRotation == PivotRotation.Local ? target.rotation : globalRotation;

            return CreateRequest(new List<Transform> { target }, target.parent, scene, position, rotation);
        }

        // --------------------------------------------------------------- prefabs

        /// <summary>
        /// True when Unity won't let the object be reparented: it is part of a
        /// prefab instance without being that instance's outermost root.
        /// </summary>
        private static bool IsLockedByPrefab(Transform target)
        {
            GameObject gameObject = target.gameObject;
            return PrefabUtility.IsPartOfPrefabInstance(gameObject) &&
                   !PrefabUtility.IsOutermostPrefabInstanceRoot(gameObject);
        }

        private static PrefabChoice AskPrefabResolution(List<Transform> targets, List<Transform> locked)
        {
            GameObject firstRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(locked[0].gameObject);
            bool involvesModel = locked.Any(IsFromModel);
            string kind = involvesModel ? "model" : "prefab";

            string subject = locked.Count == 1
                ? $"'{locked[0].name}' is inside the {kind} instance '{firstRoot.name}'."
                : $"{locked.Count} of the selected objects are inside {kind} instances.";

            // The root of the prefab being edited can be neither unpacked nor parented.
            string stageRootReason = locked.Any(t => IsPrefabStageRoot(PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject).transform))
                ? "The root of the prefab being edited can't be unpacked or parented."
                : null;

            string openBlocker = GetOpenPrefabBlocker(targets, out GameObject ownerRoot, out _);
            string ownerName = ownerRoot != null ? $"'{ownerRoot.name}'" : "the prefab";

            int choice = PPP_Popup.Choose($"Object is part of a {kind}",
                subject + " Unity doesn't allow changing the hierarchy inside a prefab instance, " +
                "so the parent can't be inserted there directly.",
                new PPP_Popup.Option
                {
                    title = "Unpack and restructure",
                    description = involvesModel
                        ? "Turns the instance into regular GameObjects (only as far as needed), then adds the parent. " +
                          "The link to the model file is lost, so re-importing the model won't update this copy."
                        : "Turns the instance into regular GameObjects (only as far as needed), then adds the parent. " +
                          "The link to the prefab is lost, so later changes to the prefab won't reach this copy.",
                    unavailableReason = stageRootReason,
                    destructive = true
                },
                new PPP_Popup.Option
                {
                    title = "Open the prefab and do it there",
                    description = $"Opens {ownerName} in Prefab Mode and adds the parent inside the prefab itself, " +
                                  "so every instance of it gets the change.",
                    unavailableReason = openBlocker
                },
                new PPP_Popup.Option
                {
                    title = "Parent the prefab root instead",
                    description = locked.Count == 1
                        ? $"Keeps the prefab intact and moves the whole instance '{firstRoot.name}' into the new parent. " +
                          "The parent is still placed at the current pivot."
                        : "Keeps the prefabs intact and moves each whole instance into the new parent. " +
                          "The parent is still placed at the current pivot.",
                    unavailableReason = stageRootReason
                });

            return choice switch
            {
                0 => PrefabChoice.Unpack,
                1 => PrefabChoice.OpenPrefab,
                2 => PrefabChoice.ParentPrefabRoot,
                _ => PrefabChoice.Cancel
            };
        }

        private static bool IsFromModel(Transform target)
        {
            GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject);
            GameObject owner = GetOwnerRoot(target);
            return (outermost != null && PrefabUtility.GetPrefabAssetType(outermost) == PrefabAssetType.Model) ||
                   (owner != null && PrefabUtility.GetPrefabAssetType(owner) == PrefabAssetType.Model);
        }

        /// <summary>
        /// The instance root of the prefab whose hierarchy has to change to reparent
        /// the target. A nested instance root is placed by the prefab around it; any
        /// other object belongs to the prefab it comes from.
        /// </summary>
        private static GameObject GetOwnerRoot(Transform target)
        {
            Transform from = PrefabUtility.IsAnyPrefabInstanceRoot(target.gameObject) ? target.parent : target;
            return from == null ? null : PrefabUtility.GetNearestPrefabInstanceRoot(from.gameObject);
        }

        /// <summary>Why the selection can't be handled in Prefab Mode, or null when it can.</summary>
        private static string GetOpenPrefabBlocker(List<Transform> targets, out GameObject ownerRoot, out string assetPath)
        {
            ownerRoot = null;
            assetPath = null;

            foreach (Transform target in targets)
            {
                GameObject owner = GetOwnerRoot(target);
                if (owner == null)
                    return "Some selected objects are not part of a prefab.";

                if (ownerRoot == null)
                    ownerRoot = owner;
                else if (owner != ownerRoot)
                    return "The selected objects belong to different prefabs.";
            }

            if (PrefabUtility.IsPrefabAssetMissing(ownerRoot))
                return "The prefab asset is missing.";

            if (PrefabUtility.GetPrefabAssetType(ownerRoot) == PrefabAssetType.Model)
                return "Models can't be edited in Prefab Mode.";

            if (PrefabUtility.IsPartOfImmutablePrefab(ownerRoot))
                return "The prefab is read-only (for example, it is inside a package).";

            assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(ownerRoot);
            foreach (Transform target in targets)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSourceAtPath(target.gameObject, assetPath) == null)
                    return "Some selected objects were added to this instance and don't exist in the prefab.";
            }

            return null;
        }

        /// <summary>
        /// Opens the owning prefab and runs the whole flow again on the matching
        /// objects there. The pivot is carried over relative to the instance root, so
        /// the parent lands on the same spot of the prefab in either Prefab Mode.
        /// </summary>
        private static void OpenPrefabAndRun(List<Transform> targets, Vector3 pivotPosition, Quaternion rotation)
        {
            if (GetOpenPrefabBlocker(targets, out GameObject ownerRoot, out string assetPath) != null)
                return;

            Transform instanceRoot = ownerRoot.transform;
            Vector3 localPivot = instanceRoot.InverseTransformPoint(pivotPosition);
            Quaternion localRotation = Quaternion.Inverse(instanceRoot.rotation) * rotation;

            // Asset objects mirror the Prefab Mode contents one to one, so their
            // sibling paths find the matching objects once the stage is open.
            List<int[]> paths = targets
                .Select(t => GetSiblingPath(((GameObject)PrefabUtility.GetCorrespondingObjectFromSourceAtPath(t.gameObject, assetPath)).transform))
                .ToList();

            PrefabStage stage = PrefabStageUtility.OpenPrefab(assetPath, ownerRoot);
            if (stage == null)
                return;

            Transform stageRoot = stage.prefabContentsRoot.transform;
            List<Transform> stageTargets = paths.Select(path => ResolveSiblingPath(stageRoot, path)).ToList();
            if (stageTargets.Any(t => t == null))
            {
                Debug.LogWarning("Pivot++: Could not find the selected objects in the opened prefab.");
                return;
            }

            Selection.objects = stageTargets.Select(t => (Object)t.gameObject).ToArray();
            Run(stageTargets, stageRoot.TransformPoint(localPivot), stageRoot.rotation * localRotation);
        }

        private static List<Transform> ReplaceLockedWithPrefabRoots(List<Transform> targets)
        {
            List<Transform> result = new List<Transform>();
            foreach (Transform target in targets)
            {
                Transform movable = IsLockedByPrefab(target)
                    ? PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject).transform
                    : target;

                if (!result.Contains(movable))
                    result.Add(movable);
            }

            // A root can swallow other selected objects; those move along with it.
            result.RemoveAll(t => result.Any(other => other != t && t.IsChildOf(other)));
            return result;
        }

        private static void UnpackLockedTargets(IEnumerable<Transform> targets)
        {
            foreach (Transform target in targets)
            {
                // Unpack one level at a time: only as much as it takes to free the target.
                for (int guard = 0; guard < 32 && IsLockedByPrefab(target); guard++)
                {
                    GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject);
                    PPP_Settings.PreserveEntriesAcross(root.transform,
                        () => PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction),
                        UNDO_NAME);
                }
            }
        }

        private static bool IsPrefabStageRoot(Transform target)
        {
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(target.gameObject);
            return stage != null && stage.prefabContentsRoot == target.gameObject;
        }

        // -------------------------------------------------------------- warnings

        /// <summary>Components that silently break when a parent is inserted above an object.</summary>
        private static List<string> CollectWarnings(List<ParentRequest> requests)
        {
            List<string> warnings = new List<string>();
            Dictionary<Component, List<string>> animated = new Dictionary<Component, List<string>>();

            foreach (Transform child in requests.SelectMany(r => r.children))
            {
                // Clips address objects by their path from the Animator, so a new
                // parent in between makes those paths point at nothing.
                Component animator = FindAnimatingAncestor(child);
                if (animator != null)
                {
                    if (!animated.TryGetValue(animator, out List<string> names))
                        animated[animator] = names = new List<string>();
                    names.Add(child.name);
                }

                if (TilemapType != null && GridType != null && child.parent != null &&
                    child.GetComponent(TilemapType) != null && child.parent.GetComponent(GridType) != null)
                {
                    warnings.Add($"'{child.name}' is a Tilemap. A Tilemap needs its Grid ('{child.parent.name}') " +
                                 "as its direct parent, so it will stop lining up with it.");
                }
            }

            foreach (KeyValuePair<Component, List<string>> pair in animated)
            {
                warnings.Add($"{FormatNames(pair.Value)} {(pair.Value.Count == 1 ? "is" : "are")} animated by the " +
                             $"{pair.Key.GetType().Name} on '{pair.Key.name}'. Animation clips find objects by their " +
                             "path in the hierarchy, so animations on these objects may stop working.");
            }

            return warnings;
        }

        private static Component FindAnimatingAncestor(Transform target)
        {
            for (Transform current = target.parent; current != null; current = current.parent)
            {
                if (current.TryGetComponent(out Animator animator))
                    return animator;
                if (current.TryGetComponent(out Animation animation))
                    return animation;
            }

            return null;
        }

        private static string FormatNames(List<string> names)
        {
            IEnumerable<string> listed = names.Take(MAX_LISTED_NAMES).Select(n => $"'{n}'");
            string text = string.Join(", ", listed);
            return names.Count > MAX_LISTED_NAMES ? $"{text} and {names.Count - MAX_LISTED_NAMES} more" : text;
        }

        // ------------------------------------------------------------- execution

        private static void Execute(List<ParentRequest> requests, bool unpack)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UNDO_NAME);

            if (unpack)
                UnpackLockedTargets(requests.SelectMany(r => r.children));

            List<GameObject> created = new List<GameObject>();
            foreach (ParentRequest request in requests)
                created.Add(CreateParent(request));

            Undo.CollapseUndoOperations(undoGroup);

            if (!Application.isPlaying)
            {
                foreach (Scene scene in created.Select(c => c.scene).Distinct())
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            Selection.objects = created.Select(c => (Object)c).ToArray();
            RevealInHierarchy(created);
        }

        private static GameObject CreateParent(ParentRequest request)
        {
            List<Transform> children = SortByHierarchy(request.children);
            Transform destination = request.destination;
            bool isUI = destination is RectTransform;

            string name = GameObjectUtility.GetUniqueNameForSibling(destination, DEFAULT_NAME);
            GameObject parent = isUI ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);

            // Fully set up before registering the creation, so undo / redo restore
            // the finished object.
            if (destination == null)
                MoveToScene(parent, request.scene);

            parent.transform.SetParent(destination, false);
            parent.transform.SetSiblingIndex(GetInsertIndex(children, destination));

            if (isUI)
                PlaceRectTransform((RectTransform)parent.transform, (RectTransform)destination, request.position, request.rotation);
            else
                parent.transform.SetPositionAndRotation(request.position, request.rotation);

            CopyCommonTraits(parent, children);

            // A layout group would take over the new parent's position and shove the
            // children away from where they are.
            if (destination != null && destination.TryGetComponent(out LayoutGroup _))
                parent.AddComponent<LayoutElement>().ignoreLayout = true;

            Undo.RegisterCreatedObjectUndo(parent, UNDO_NAME);

            foreach (Transform child in children)
                Undo.SetTransformParent(child, parent.transform, UNDO_NAME);

            return parent;
        }

        private static void MoveToScene(GameObject gameObject, Scene scene)
        {
            if (!scene.IsValid() || gameObject.scene == scene)
                return;

            // The DontDestroyOnLoad scene can't be targeted directly; objects only
            // get there through DontDestroyOnLoad itself.
            if (Application.isPlaying && scene.buildIndex == -1 && scene.name == "DontDestroyOnLoad")
                Object.DontDestroyOnLoad(gameObject);
            else
                SceneManager.MoveGameObjectToScene(gameObject, scene);
        }

        /// <summary>
        /// Stretches the new parent over its parent's rect, so the children's anchors
        /// resolve against the exact same rect as before and nothing resizes, then
        /// moves the rect's pivot onto the requested point.
        /// </summary>
        private static void PlaceRectTransform(RectTransform rectTransform, RectTransform destination,
            Vector3 position, Quaternion rotation)
        {
            Vector3 localPoint = destination.InverseTransformPoint(position);
            Rect rect = destination.rect;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(
                rect.width != 0f ? (localPoint.x - rect.xMin) / rect.width : 0.5f,
                rect.height != 0f ? (localPoint.y - rect.yMin) / rect.height : 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition3D = new Vector3(0f, 0f, localPoint.z);
            rectTransform.localScale = Vector3.one;
            rectTransform.rotation = rotation;

            // A no-op for any rect with a size; lands the pivot for zero-sized ones.
            rectTransform.position = position;
        }

        /// <summary>
        /// The parent inherits tag, layer and static flags only where all its
        /// children agree, so tools filtering on them treat it like its contents.
        /// </summary>
        private static void CopyCommonTraits(GameObject parent, List<Transform> children)
        {
            GameObject first = children[0].gameObject;

            if (children.All(c => c.tag == first.tag))
                parent.tag = first.tag;

            if (children.All(c => c.gameObject.layer == first.layer))
                parent.layer = first.layer;

            StaticEditorFlags flags = (StaticEditorFlags)~0;
            foreach (Transform child in children)
                flags &= GameObjectUtility.GetStaticEditorFlags(child.gameObject);
            GameObjectUtility.SetStaticEditorFlags(parent, flags);
        }

        /// <summary>Right where the first moved object sits under the destination.</summary>
        private static int GetInsertIndex(List<Transform> children, Transform destination)
        {
            int index = int.MaxValue;
            foreach (Transform child in children)
            {
                Transform current = child;
                while (current.parent != destination)
                    current = current.parent;

                index = Mathf.Min(index, current.GetSiblingIndex());
            }

            return index == int.MaxValue ? 0 : index;
        }

        // ------------------------------------------------------------- hierarchy

        private static Transform FindCommonAncestor(List<Transform> targets)
        {
            if (targets.Any(t => t.parent == null))
                return null;

            for (Transform candidate = targets[0].parent; candidate != null; candidate = candidate.parent)
            {
                if (targets.All(t => t.parent.IsChildOf(candidate)))
                    return candidate;
            }

            return null;
        }

        private static List<Transform> SortByHierarchy(List<Transform> transforms)
        {
            List<Transform> sorted = new List<Transform>(transforms);
            sorted.Sort((a, b) => CompareSiblingPaths(GetSiblingPath(a), GetSiblingPath(b)));
            return sorted;
        }

        private static int[] GetSiblingPath(Transform target)
        {
            List<int> path = new List<int>();
            for (Transform current = target; current != null; current = current.parent)
                path.Add(current.GetSiblingIndex());

            path.Reverse();
            return path.ToArray();
        }

        private static Transform ResolveSiblingPath(Transform root, int[] path)
        {
            // path[0] is the root's own index; the walk starts below it.
            Transform current = root;
            for (int i = 1; i < path.Length; i++)
            {
                if (path[i] >= current.childCount)
                    return null;
                current = current.GetChild(path[i]);
            }

            return current;
        }

        private static int CompareSiblingPaths(int[] a, int[] b)
        {
            for (int i = 0; i < Mathf.Min(a.Length, b.Length); i++)
            {
                if (a[i] != b[i])
                    return a[i].CompareTo(b[i]);
            }

            return a.Length.CompareTo(b.Length);
        }

        /// <summary>
        /// Expands the new parents in the Hierarchy and, for a single one, starts
        /// a rename so the user can type a name right away. The Hierarchy window
        /// has no public API for either, so failures are silently ignored.
        /// </summary>
        private static void RevealInHierarchy(List<GameObject> created)
        {
            // The Hierarchy only picks up the new objects on its next update.
            EditorApplication.delayCall += () =>
            {
                EditorWindow hierarchy = GetHierarchyWindow();
                if (hierarchy == null)
                    return;

                try
                {
                    MethodInfo setExpanded = hierarchy.GetType().GetMethod("SetExpanded",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(int), typeof(bool) }, null);

                    foreach (GameObject parent in created.Where(c => c != null))
                        setExpanded?.Invoke(hierarchy, new object[] { parent.GetEntityId(), true });
                }
                catch (Exception)
                {
                    // Expanding is a convenience; the parents are created regardless.
                }

                if (created.Count != 1)
                    return;

                hierarchy.Focus();
                EditorApplication.delayCall += () =>
                {
                    if (hierarchy != null && Selection.activeGameObject == created[0])
                        hierarchy.SendEvent(EditorGUIUtility.CommandEvent("Rename"));
                };
            };
        }

        private static EditorWindow GetHierarchyWindow()
        {
            Type hierarchyType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (hierarchyType == null)
                return null;

            PropertyInfo lastInteracted = hierarchyType.GetProperty("lastInteractedHierarchyWindow",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (lastInteracted?.GetValue(null) is EditorWindow window && window != null)
                return window;

            Object[] windows = Resources.FindObjectsOfTypeAll(hierarchyType);
            return windows.Length > 0 ? windows[0] as EditorWindow : null;
        }
    }
}
