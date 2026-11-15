using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// The Pivot++ panel itself: enable toggle, shared pivot offset controls
    /// (bounded sliders or unbounded fields), the offsetted transform fields and
    /// the object pivot actions.
    ///
    /// Deliberately a plain VisualElement rather than an Overlay: the same panel
    /// is hosted both by <see cref="PPP_Overlay"/> in the scene view and by
    /// <see cref="PPP_Window"/> as a dockable window, and neither host should own
    /// a second copy of this UI. What the two hosts differ on is passed in - the
    /// scene view fits the panel to its own width, a docked window does not.
    /// </summary>
    internal class PPP_Panel : VisualElement
    {
        const float SLIDER_MIN = -1f;
        const float SLIDER_MAX = 1f;

        // Responsive panel sizing. The panel targets a fraction of the scene
        // view's width, clamped to a sane band, so it neither stretches on
        // wide layouts (e.g. a size saved by another project) nor overflows
        // tiny scene views.
        const float MIN_PANEL_WIDTH = 300f;
        const float PREFERRED_PANEL_WIDTH = 300f;
        const float MAX_PANEL_WIDTH = 400f;
        const float PANEL_WIDTH_RATIO = 0.30f;   // fraction of scene view width
        const float PANEL_EDGE_MARGIN = 24f;     // breathing room on tiny views

        // Value change per pixel when dragging an axis label.
        const float NORMALIZED_DRAG_SENSITIVITY = 0.005f;
        const float ABSOLUTE_DRAG_SENSITIVITY = 0.01f;
        const float POSITION_DRAG_SENSITIVITY = 0.01f;
        const float ROTATION_DRAG_SENSITIVITY = 0.2f;   // degrees
        const float SCALE_DRAG_SENSITIVITY = 0.01f;
        const float VECTOR_LABEL_WIDTH = 56f;

        // The offsetted transform fields (and whether a parent can be created)
        // mirror live scene state, so they are polled instead of waiting for a
        // selection / settings event.
        const long OFFSET_TRANSFORM_POLL_MS = 100;

        const string CREATE_PARENT_TOOLTIP =
            "Create an empty parent at the shared pivot and move the selection into it.";

        // Quick-set buttons at the end of each bounded axis row.
        const float ORIGIN_RANGE_EPSILON = 0.0001f;

        const string ORIGIN_TOOLTIP =
            "Move the pivot onto the object's own origin on this axis (the yellow dot in the scene).";

        const string ORIGIN_OUT_OF_BOUNDS_TOOLTIP =
            "The object's origin sits outside its bounds on this axis, so Bounded mode cannot reach it. " +
            "Switch to Unbounded.";

        private static readonly float[] QuickSetValues = { SLIDER_MIN, 0f, SLIDER_MAX };

        private static readonly string[] QuickSetTooltips =
        {
            "Move the pivot to the minimum edge of the bounds on this axis.",
            "Move the pivot to the center of the bounds on this axis.",
            "Move the pivot to the maximum edge of the bounds on this axis."
        };

        private Slider _sliderX;
        private Slider _sliderY;
        private Slider _sliderZ;

        private FloatField _fieldX;
        private FloatField _fieldY;
        private FloatField _fieldZ;

        private VisualElement _sharedPivotConatiner;
        private VisualElement _normalizedRowsContainer;
        private VisualElement _absoluteRowsContainer;
        private Button _normalizedModeButton;
        private Button _absoluteModeButton;
        private EnumField _canvasBoundsField;
        private FloatField _absoluteFieldX;
        private FloatField _absoluteFieldY;
        private FloatField _absoluteFieldZ;
        private Toggle _useObjectScaleToggle;
        private readonly Button[] _originButtons = new Button[3];
        private Button _createParentButton;
        private Label _messageLabel;
        private PPP_OverlayWidgets.Switch _enabledSwitch;
        private Button _panelModeButton;
        private PPP_PanelMode? _shownPanelMode;
        private VisualElement _offsetTransformCard;
        private readonly FloatField[] _positionFields = new FloatField[3];
        private readonly FloatField[] _rotationFields = new FloatField[3];
        private readonly FloatField[] _scaleFields = new FloatField[3];
        private IVisualElementScheduledItem _offsetTransformPollItem;
        private VisualElement _pivotActionsCard;
        private VisualElement _modeButtonsRow;
        private Button _defaultModeButton;
        private Button _editModeButton;
        private Button _addPivotButton;
        private Label _pivotCountBadge;
        private IVisualElementScheduledItem _addFeedbackItem;
        private bool _addFeedbackActive;

        private const long ADD_FEEDBACK_MS = 2000; // "Added at ..." duration

        private VisualElement _sceneViewRoot; // watched for scene view resizes

        private readonly PPP_PanelMode _hostMode;
        private readonly Func<float> _hostWidthProvider;

        /// <param name="hostMode">Which chrome this copy of the panel lives in.</param>
        /// <param name="hostWidthProvider">
        /// Width of the host window, consulted only while the scene view's own
        /// layout has not resolved yet. Overlay hosts only.
        /// </param>
        internal PPP_Panel(PPP_PanelMode hostMode, Func<float> hostWidthProvider = null)
        {
            _hostMode = hostMode;
            _hostWidthProvider = hostWidthProvider;

            style.paddingLeft = 7;
            style.paddingRight = 7;
            style.paddingTop = 7;
            // Cards carry their own bottom margin, so the panel only needs a
            // hairline of padding here to balance the top.
            style.paddingBottom = 1;

            Add(CreateEnabledCard());
            Add(CreateSharedPivotCard());
            Add(CreateOffsetTransformCard());
            Add(CreateObjectPivotsCard());

            // Everything that reaches outside this element is wired on attach and
            // dropped again on detach, so a panel Unity has taken off screen stops
            // polling and stops listening - whichever host it belongs to.
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            TotalRefresh();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Selection.selectionChanged -= TotalRefresh;
            Selection.selectionChanged += TotalRefresh;
            PPP_Settings.OnSettingsChanged -= TotalRefresh;
            PPP_Settings.OnSettingsChanged += TotalRefresh;
            PPP_Handles.OnStateChanged -= OnHandlesStateChanged;
            PPP_Handles.OnStateChanged += OnHandlesStateChanged;

            _offsetTransformPollItem = schedule
                .Execute(() =>
                {
                    RefreshOffsetTransformValues();
                    RefreshCreateParentButton();
                    RefreshOriginButtons();
                    RefreshPanelModeButton();
                })
                .Every(OFFSET_TRANSFORM_POLL_MS);

            AttachSceneViewTracking();
            TotalRefresh();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Selection.selectionChanged -= TotalRefresh;
            PPP_Settings.OnSettingsChanged -= TotalRefresh;
            PPP_Handles.OnStateChanged -= OnHandlesStateChanged;

            _offsetTransformPollItem?.Pause();
            _offsetTransformPollItem = null;

            DetachSceneViewTracking();
        }

        /// <summary>
        /// Watches the scene view's root so the panel re-fits whenever the window
        /// is resized, not just when it is first created. A docked window sizes
        /// the panel by itself, so it opts out entirely.
        /// </summary>
        private void AttachSceneViewTracking()
        {
            DetachSceneViewTracking();

            if (_hostMode != PPP_PanelMode.Overlay)
                return;

            _sceneViewRoot = panel?.visualTree;
            _sceneViewRoot?.RegisterCallback<GeometryChangedEvent>(OnSceneViewGeometryChanged);
            UpdatePanelWidth();
        }

        private void DetachSceneViewTracking()
        {
            _sceneViewRoot?.UnregisterCallback<GeometryChangedEvent>(OnSceneViewGeometryChanged);
            _sceneViewRoot = null;
        }

        private void OnSceneViewGeometryChanged(GeometryChangedEvent evt)
        {
            UpdatePanelWidth();
        }

        // Fits the panel to the current scene view: roughly a third of the
        // view's width, never wider than MAX_PANEL_WIDTH, never spilling out
        // of a small window.
        private void UpdatePanelWidth()
        {
            if (_hostMode != PPP_PanelMode.Overlay)
                return;

            float sceneViewWidth = _sceneViewRoot?.layout.width ?? 0f;
            if (sceneViewWidth <= 0f || float.IsNaN(sceneViewWidth))
                sceneViewWidth = _hostWidthProvider?.Invoke() ?? 0f;

            if (sceneViewWidth <= 0f || float.IsNaN(sceneViewWidth))
            {
                // Layout not resolved yet; fall back to the preferred width.
                style.minWidth = PREFERRED_PANEL_WIDTH;
                style.maxWidth = MAX_PANEL_WIDTH;
                return;
            }

            float fitted = Mathf.Clamp(sceneViewWidth * PANEL_WIDTH_RATIO, MIN_PANEL_WIDTH, MAX_PANEL_WIDTH);
            fitted = Mathf.Min(fitted, Mathf.Max(MIN_PANEL_WIDTH, sceneViewWidth - PANEL_EDGE_MARGIN));

            style.minWidth = Mathf.Min(PREFERRED_PANEL_WIDTH, fitted);
            style.maxWidth = fitted;
        }

        private void OnHandlesStateChanged(PPP_Handles.State newState)
        {
            TotalRefresh();
        }
        

        private VisualElement CreateEnabledCard()
        {
            VisualElement enabledCard = PPP_OverlayWidgets.CreateCard();
            enabledCard.style.paddingTop = 5;
            enabledCard.style.paddingBottom = 5;

            _enabledSwitch = new PPP_OverlayWidgets.Switch("Enabled", "Disabled",
                "Toggle Pivot++ on / off", OnEnabledButtonClicked);
            _enabledSwitch.style.flexGrow = 1;

            _panelModeButton = PPP_OverlayWidgets.CreateIconButton(
                PPP_OverlayWidgets.CreateDockedPanelIcon(), string.Empty, OnPanelModeButtonClicked);

            // Pinned right by its own margin rather than by the switch's flex, so
            // it does not slide left on the refreshes that hide the switch.
            _panelModeButton.style.marginLeft = StyleKeyword.Auto;

            // The row itself always stays up, even when TotalRefresh hides the
            // switch behind a blocking message: where the panel lives is chrome,
            // and it should not become unreachable just because the selection
            // went away.
            VisualElement enabledRow = PPP_OverlayWidgets.CreateRow();
            enabledRow.Add(_enabledSwitch);
            enabledRow.Add(_panelModeButton);
            enabledCard.Add(enabledRow);

            RefreshPanelModeButton();

            _messageLabel = PPP_OverlayWidgets.CreateMessageLabel();
            _messageLabel.style.marginTop = 5;
            _messageLabel.style.marginBottom = 2;
            enabledCard.Add(_messageLabel);

            return enabledCard;
        }

        private VisualElement CreateSharedPivotCard()
        {
            _sharedPivotConatiner = new PPP_OverlayWidgets.CollapsibleCard("SHARED PIVOT");

            _normalizedModeButton = PPP_OverlayWidgets.CreateSegment("Bounded",
                "Offset the pivot within object's Mesh Bounds.", () => SetUnitMode(UnitMode.Bounded));
            _absoluteModeButton = PPP_OverlayWidgets.CreateSegment("Unbounded",
                "Offset the pivot in object's local space.", () => SetUnitMode(UnitMode.Unbounded));

            VisualElement unitModeRow = PPP_OverlayWidgets.CreateSegmentedRow(_normalizedModeButton, _absoluteModeButton);
            unitModeRow.style.marginBottom = PPP_OverlayWidgets.BLOCK_GAP;
            _sharedPivotConatiner.Add(unitModeRow);

            _canvasBoundsField = new EnumField(CanvasBoundsMode.RectTransform);
            PPP_OverlayWidgets.HideFieldLabel(_canvasBoundsField);
            PPP_OverlayWidgets.StyleField(_canvasBoundsField);
            _canvasBoundsField.RegisterValueChangedCallback(evt => SetCanvasBoundsMode((CanvasBoundsMode)evt.newValue));

            VisualElement canvasBoundsRow = PPP_OverlayWidgets.CreateLabeledRow("Canvas Bounds",
                "How UI / layout elements are measured:\n" +
                "RectTransform: the RectTransform's own rect.\n" +
                "Preferred: the layout element's preferred size.\n" +
                "Minimum: the layout element's minimum size.",
                _canvasBoundsField);
            canvasBoundsRow.style.marginBottom = PPP_OverlayWidgets.BLOCK_GAP;
            _sharedPivotConatiner.Add(canvasBoundsRow);

            _normalizedRowsContainer = new VisualElement();
            _normalizedRowsContainer.Add(CreateAxisRow(0, out _sliderX, out _fieldX));
            _normalizedRowsContainer.Add(CreateAxisRow(1, out _sliderY, out _fieldY));
            _normalizedRowsContainer.Add(CreateAxisRow(2, out _sliderZ, out _fieldZ));
            _sharedPivotConatiner.Add(_normalizedRowsContainer);

            _absoluteRowsContainer = new VisualElement();
            _absoluteRowsContainer.Add(CreateAbsoluteRow(0, out _absoluteFieldX));
            _absoluteRowsContainer.Add(CreateAbsoluteRow(1, out _absoluteFieldY));
            _absoluteRowsContainer.Add(CreateAbsoluteRow(2, out _absoluteFieldZ));
            _absoluteRowsContainer.Add(CreateUseObjectScaleToggle());
            _sharedPivotConatiner.Add(_absoluteRowsContainer);

            _createParentButton = PPP_OverlayWidgets.CreateButton("Create Parent at Pivot",
                CREATE_PARENT_TOOLTIP, OnCreateParentClicked);
            _createParentButton.style.marginTop = PPP_OverlayWidgets.BLOCK_GAP;
            _sharedPivotConatiner.Add(_createParentButton);

            return _sharedPivotConatiner;
        }

        private void OnCreateParentClicked()
        {
            // The button is only greyed out (so its tooltip can explain why), so
            // clicks still arrive while it is unavailable.
            if (!PPP_CreateParent.CanCreateAtSharedPivot(out _))
                return;

            PPP_CreateParent.CreateAtSharedPivot();
            SceneView.RepaintAll();
        }

        private void RefreshCreateParentButton()
        {
            if (_createParentButton == null)
                return;

            bool isAvailable = PPP_CreateParent.CanCreateAtSharedPivot(out string reason);
            PPP_OverlayWidgets.SetButtonAvailable(_createParentButton, isAvailable);
            _createParentButton.tooltip = isAvailable ? CREATE_PARENT_TOOLTIP : reason;
        }

        private VisualElement CreateOffsetTransformCard()
        {
            _offsetTransformCard = new PPP_OverlayWidgets.CollapsibleCard("OFFSETTED TRANSFORM");

            _offsetTransformCard.Add(CreateVectorSection("Position",
                "World position of the offset pivot. Typing here moves the selection " +
                "so the pivot lands exactly on these coordinates.",
                _positionFields, POSITION_DRAG_SENSITIVITY, OnPositionFieldChanged));

            VisualElement rotation = CreateVectorSection("Rotation",
                "World rotation of the active object, in degrees. Typing here rotates " +
                "the selection around the offset pivot.",
                _rotationFields, ROTATION_DRAG_SENSITIVITY, OnRotationFieldChanged);
            rotation.style.marginTop = PPP_OverlayWidgets.ROW_GAP;
            rotation.style.marginBottom = PPP_OverlayWidgets.ROW_GAP;

            _offsetTransformCard.Add(rotation);

            _offsetTransformCard.Add(CreateVectorSection("Scale",
                "Local scale of the active object. Typing here scales the selection " +
                "around the offset pivot.",
                _scaleFields, SCALE_DRAG_SENSITIVITY, OnScaleFieldChanged));

            return _offsetTransformCard;
        }

        /// <summary>
        /// A "Position"-style label followed by three axis fields on one row. The
        /// fields share the remaining width so the row survives the panel's narrow band.
        /// </summary>
        private VisualElement CreateVectorSection(string label, string tooltip, FloatField[] fields, float dragSensitivity, Action<int> applyAxis)
        {
            VisualElement row = PPP_OverlayWidgets.CreateRow();
            row.tooltip = tooltip;

            Label caption = PPP_OverlayWidgets.CreateFieldLabel(label);
            caption.style.width = VECTOR_LABEL_WIDTH;
            caption.style.minWidth = VECTOR_LABEL_WIDTH;
            caption.style.flexShrink = 0;
            row.Add(caption);

            for (int i = 0; i < 3; i++)
            {
                int axisIndex = i;

                VisualElement axisGroup = PPP_OverlayWidgets.CreateRow();
                axisGroup.style.flexGrow = 1;
                axisGroup.style.flexShrink = 1;
                axisGroup.style.flexBasis = 0;
                axisGroup.style.marginLeft = axisIndex == 0 ? 0 : 4;

                Label axisChip = PPP_OverlayWidgets.CreateAxisChip(axisIndex);

                // Live: an edit is an absolute target, so re-applying it on every
                // keystroke lands exactly where a single commit would, and the
                // scene follows along while the user types. What does need holding
                // back is text the field parses into something never asked for -
                // a half-typed "-" (0), or an expression like "2*3" that is only
                // whole once it is finished - so those wait for the commit below.
                FloatField field = new FloatField { isDelayed = false };
                PPP_OverlayWidgets.StyleField(field);
                field.style.flexGrow = 1;
                field.style.flexShrink = 1;
                field.style.flexBasis = 0;
                field.style.minWidth = 0;
                field.style.marginLeft = 3;

                bool hasHeldBackEdit = false;
                int undoGroup = -1;

                field.RegisterValueChangedCallback(evt =>
                {
                    if (!IsCompleteNumber(field.text))
                    {
                        hasHeldBackEdit = true;
                        return;
                    }

                    hasHeldBackEdit = false;
                    applyAxis(axisIndex);
                });

                // One typed value should be one undo step, not one per keystroke,
                // so the whole edit is collapsed back to where it started.
                field.RegisterCallback<FocusInEvent>(_ =>
                {
                    Undo.IncrementCurrentGroup();
                    undoGroup = Undo.GetCurrentGroup();
                });

                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                        CommitHeldBackEdit();
                });

                field.RegisterCallback<FocusOutEvent>(_ =>
                {
                    CommitHeldBackEdit();

                    if (undoGroup >= 0)
                    {
                        Undo.CollapseUndoOperations(undoGroup);
                        undoGroup = -1;
                    }
                });

                void CommitHeldBackEdit()
                {
                    if (!hasHeldBackEdit)
                        return;

                    // The field has already parsed the text into its value, so the
                    // held-back edit applies from exactly where the live path left off.
                    hasHeldBackEdit = false;
                    applyAxis(axisIndex);
                }

                PPP_OverlayWidgets.RegisterLabelDrag(axisChip, dragSensitivity, delta =>
                {
                    field.SetValueWithoutNotify(field.value + delta);
                    applyAxis(axisIndex);
                });

                axisGroup.Add(axisChip);
                axisGroup.Add(field);
                row.Add(axisGroup);

                fields[axisIndex] = field;
            }

            return row;
        }

        private void OnPositionFieldChanged(int axisIndex)
        {
            PPP_OffsetTransform.SetPosition(ReadVector(_positionFields));
            RefreshOffsetTransformValues();
            SceneView.RepaintAll();
        }

        private void OnRotationFieldChanged(int axisIndex)
        {
            PPP_OffsetTransform.SetRotation(ReadVector(_rotationFields));
            RefreshOffsetTransformValues();
            SceneView.RepaintAll();
        }

        private void OnScaleFieldChanged(int axisIndex)
        {
            PPP_OffsetTransform.SetScale(ReadVector(_scaleFields), axisIndex);
            RefreshOffsetTransformValues();
            SceneView.RepaintAll();
        }

        private static Vector3 ReadVector(FloatField[] fields)
        {
            return new Vector3(fields[0].value, fields[1].value, fields[2].value);
        }

        /// <summary>
        /// Pushes the live pivot / rotation / scale into the fields, leaving the
        /// one the user is currently typing in alone so it is not overwritten
        /// mid-edit.
        /// </summary>
        private void RefreshOffsetTransformValues()
        {
            if (_offsetTransformCard == null)
                return;

            if (GetBlockingMessage() != null || !PPP_OffsetTransform.TryGetSnapshot(out PPP_OffsetTransform.Snapshot snapshot))
            {
                _offsetTransformCard.style.display = DisplayStyle.None;
                return;
            }

            _offsetTransformCard.style.display = DisplayStyle.Flex;

            WriteVector(_positionFields, snapshot.pivotPosition);
            WriteVector(_rotationFields, snapshot.rotationEuler);
            WriteVector(_scaleFields, snapshot.scale);
        }

        private static void WriteVector(FloatField[] fields, Vector3 value)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!HasFocus(fields[i]))
                    fields[i].SetValueWithoutNotify(value[i]);
            }
        }

        /// <summary>
        /// Whether the raw text in a field is a finished plain number, and so safe
        /// to apply on the keystroke that produced it. Anything else still parses
        /// to a value - "-" to 0, "2*3" to 6 - but not to one the user has finished
        /// asking for, so it is left for the commit.
        /// </summary>
        private static bool IsCompleteNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
                   float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out _);
        }

        private static bool HasFocus(VisualElement element)
        {
            if (element.panel?.focusController?.focusedElement is not VisualElement focused)
                return false;

            return focused == element || element.Contains(focused);
        }

        private VisualElement CreateObjectPivotsCard()
        {
            _pivotCountBadge = PPP_OverlayWidgets.CreateBadge("", PPP_OverlayWidgets.AccentColor);
            _pivotCountBadge.style.display = DisplayStyle.None;

            _pivotActionsCard = new PPP_OverlayWidgets.CollapsibleCard("OBJECT PIVOTS", _pivotCountBadge);

            _defaultModeButton = PPP_OverlayWidgets.CreateSegment("Default",
                "Move object using pivots.", () => SetPivotMode(PivotMode.Default));
            _editModeButton = PPP_OverlayWidgets.CreateSegment("Edit Pivots",
                "Move/Rotate/Delete the extra pivots.", () => SetPivotMode(PivotMode.Edit));

            _modeButtonsRow = PPP_OverlayWidgets.CreateSegmentedRow(_defaultModeButton, _editModeButton);
            _modeButtonsRow.style.marginBottom = PPP_OverlayWidgets.CARD_GAP;
            _pivotActionsCard.Add(_modeButtonsRow);

            _addPivotButton = PPP_OverlayWidgets.CreateButton("+  Add New Pivot",
                "Create a new pivot for this specific object.", OnAddPivotClicked, true);
            _pivotActionsCard.Add(_addPivotButton);
            return _pivotActionsCard;
        }

        private VisualElement CreateAxisRow(int axisIndex, out Slider slider, out FloatField field)
        {
            VisualElement row = PPP_OverlayWidgets.CreateRow(PPP_OverlayWidgets.ROW_GAP);

            Label axisChip = PPP_OverlayWidgets.CreateAxisChip(axisIndex);

            Slider localSlider = new Slider(SLIDER_MIN, SLIDER_MAX);
            PPP_OverlayWidgets.StyleSlider(localSlider, axisIndex);
            localSlider.style.flexGrow = 1;
            localSlider.style.flexShrink = 1;
            localSlider.style.marginLeft = 6;
            localSlider.style.marginRight = 6;

            FloatField localField = new FloatField();
            PPP_OverlayWidgets.StyleField(localField);
            localField.style.width = 48;
            localField.style.flexShrink = 0;

            localSlider.RegisterValueChangedCallback(evt =>
            {
                localField.SetValueWithoutNotify(evt.newValue);
                OnSliderChanged(axisIndex, evt.newValue);
            });

            localField.RegisterValueChangedCallback(evt =>
            {
                float clamped = Mathf.Clamp(evt.newValue, SLIDER_MIN, SLIDER_MAX);
                localSlider.SetValueWithoutNotify(clamped);
                OnSliderChanged(axisIndex, clamped);
            });

            PPP_OverlayWidgets.RegisterLabelDrag(axisChip, NORMALIZED_DRAG_SENSITIVITY, delta =>
            {
                float clamped = Mathf.Clamp(localSlider.value + delta, SLIDER_MIN, SLIDER_MAX);
                localSlider.SetValueWithoutNotify(clamped);
                localField.SetValueWithoutNotify(clamped);
                OnSliderChanged(axisIndex, clamped);
            });

            row.Add(axisChip);
            row.Add(localSlider);
            row.Add(localField);
            row.Add(CreateQuickSetGroup(axisIndex, localSlider, localField));

            slider = localSlider;
            field = localField;
            return row;
        }

        /// <summary>
        /// The quick-set cluster at the end of an axis row: send the pivot to the
        /// object's own origin, or to the minimum / center / maximum of its bounds.
        ///
        /// The buttons carry glyphs rather than numbers. "-1 / 0 / 1" only means
        /// something once you already know the offset is normalized against the
        /// bounds, while a dot at one end of a short track says where the pivot
        /// lands and echoes the slider it belongs to. The origin button wears the
        /// same yellow dot the scene view draws on the object's real pivot.
        /// </summary>
        private VisualElement CreateQuickSetGroup(int axisIndex, Slider slider, FloatField field)
        {
            VisualElement group = PPP_OverlayWidgets.CreateRow();
            group.style.marginLeft = 6;
            group.style.flexShrink = 0;

            Button originButton = PPP_OverlayWidgets.CreateIconButton(
                PPP_OverlayWidgets.CreateDotIcon(PPP_Handles.OriginDotColor),
                ORIGIN_TOOLTIP,
                () => OnOriginButtonClicked(axisIndex, slider, field));

            _originButtons[axisIndex] = originButton;
            group.Add(originButton);

            for (int i = 0; i < QuickSetValues.Length; i++)
            {
                float value = QuickSetValues[i];

                Button button = PPP_OverlayWidgets.CreateIconButton(
                    PPP_OverlayWidgets.CreateTrackDotIcon(value),
                    QuickSetTooltips[i],
                    () => ApplySliderValue(axisIndex, slider, field, value));

                // A wider gap before the first one separates "go to the object's
                // pivot" from the three bounds positions.
                button.style.marginLeft = i == 0 ? 5 : 2;
                group.Add(button);
            }

            return group;
        }

        private void ApplySliderValue(int axisIndex, Slider slider, FloatField field, float value)
        {
            slider.SetValueWithoutNotify(value);
            field.SetValueWithoutNotify(value);
            OnSliderChanged(axisIndex, value);
        }

        private void OnOriginButtonClicked(int axisIndex, Slider slider, FloatField field)
        {
            // Greyed out rather than disabled, so clicks still arrive while the
            // origin is out of reach.
            if (!TryGetOriginSliderValue(axisIndex, out float value, out _))
                return;

            ApplySliderValue(axisIndex, slider, field, Mathf.Clamp(value, SLIDER_MIN, SLIDER_MAX));
        }

        /// <summary>
        /// The bounded value for one axis that puts the pivot on the active
        /// object's origin, or false plus the reason it cannot be reached.
        /// </summary>
        private static bool TryGetOriginSliderValue(int axisIndex, out float value, out string unavailableReason)
        {
            value = 0f;

            Transform[] targets = Selection.transforms;
            Transform activeTransform = Selection.activeTransform;

            if (targets.Length == 0 || activeTransform == null)
            {
                unavailableReason = "No object selected.";
                return false;
            }

            if (!PPP_Handles.TryGetOriginOffsetValues(targets, activeTransform, out Vector3 originValues))
            {
                unavailableReason = "Selected object does not have any component to draw bounds from.";
                return false;
            }

            value = originValues[axisIndex];

            // A tolerance around the ends: an origin sitting exactly on the bounds
            // (a character standing on its own feet, say) must not be rejected by
            // a float hair past 1.
            if (value < SLIDER_MIN - ORIGIN_RANGE_EPSILON || value > SLIDER_MAX + ORIGIN_RANGE_EPSILON)
            {
                unavailableReason = ORIGIN_OUT_OF_BOUNDS_TOOLTIP;
                return false;
            }

            unavailableReason = null;
            return true;
        }

        private void RefreshOriginButtons()
        {
            for (int axisIndex = 0; axisIndex < _originButtons.Length; axisIndex++)
            {
                Button button = _originButtons[axisIndex];
                if (button == null)
                    continue;

                bool isAvailable = TryGetOriginSliderValue(axisIndex, out _, out string reason);
                PPP_OverlayWidgets.SetButtonAvailable(button, isAvailable);
                button.tooltip = isAvailable ? ORIGIN_TOOLTIP : reason;
            }
        }

        private VisualElement CreateAbsoluteRow(int axisIndex, out FloatField field)
        {
            VisualElement row = PPP_OverlayWidgets.CreateRow(PPP_OverlayWidgets.ROW_GAP);

            Label axisChip = PPP_OverlayWidgets.CreateAxisChip(axisIndex);

            FloatField localField = new FloatField();
            PPP_OverlayWidgets.StyleField(localField);
            localField.style.flexGrow = 1;
            localField.style.marginLeft = 6;

            localField.RegisterValueChangedCallback(evt => OnAbsoluteFieldChanged(axisIndex, evt.newValue));

            PPP_OverlayWidgets.RegisterLabelDrag(axisChip, ABSOLUTE_DRAG_SENSITIVITY, delta =>
            {
                float newValue = localField.value + delta;
                localField.SetValueWithoutNotify(newValue);
                OnAbsoluteFieldChanged(axisIndex, newValue);
            });

            row.Add(axisChip);
            row.Add(localField);

            field = localField;
            return row;
        }

        private VisualElement CreateUseObjectScaleToggle()
        {
            _useObjectScaleToggle = new Toggle
            {
                text = "Use Object Scale",
                tooltip = "On: the offset scales with the object (2x scale = 2x distance).\n" +
                          "Off: the offset is a fixed distance, independent of object scale."
            };
            PPP_OverlayWidgets.StyleToggle(_useObjectScaleToggle);
            _useObjectScaleToggle.style.marginTop = PPP_OverlayWidgets.BLOCK_GAP;

            _useObjectScaleToggle.RegisterValueChangedCallback(evt => OnUseObjectScaleChanged(evt.newValue));
            return _useObjectScaleToggle;
        }

        private void OnUseObjectScaleChanged(bool newValue)
        {
            GameObject target = Selection.activeGameObject;
            PPP_Settings.SetUseObjectScale(target, newValue);
            SceneView.RepaintAll();
        }

        private void SetUnitMode(UnitMode mode)
        {
            GameObject target = Selection.activeGameObject;
            PPP_Settings.SetUnitMode(target, mode);
            TotalRefresh();
            SceneView.RepaintAll();
        }

        private void SetCanvasBoundsMode(CanvasBoundsMode mode)
        {
            PPP_Settings.CanvasBoundsMode = mode;
            PPP_Bounds.InvalidateAll();
            TotalRefresh();
            SceneView.RepaintAll();
        }

        private void OnAbsoluteFieldChanged(int axisIndex, float newValue)
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
                return;

            Vector3 current = PPP_Settings.GetAbsoluteValues(target);
            current[axisIndex] = newValue;
            PPP_Settings.RecordUndo("Change Shared Pivot");
            PPP_Settings.SetAbsoluteValues(target, current);
            SceneView.RepaintAll();
        }

        private void OnSliderChanged(int axisIndex, float newValue)
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
                return;

            Vector3 current = PPP_Settings.GetSliderValues(target);
            current[axisIndex] = newValue;
            PPP_Settings.RecordUndo("Change Shared Pivot");
            PPP_Settings.SetSliderValues(target, current);
            SceneView.RepaintAll();
        }

        private void OnAddPivotClicked()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null || Selection.gameObjects.Length != 1)
                return;

            // The new pivot spawns where the shared pivot currently sits.
            Vector3 localPosition = Vector3.zero;
            if (PPP_Handles.TryGetSharedPivotWorldPosition(target.transform, out Vector3 sharedWorldPosition))
                localPosition = target.transform.InverseTransformPoint(sharedWorldPosition);

            Pose[] offsets = PPP_Settings.GetMultiPivotOffsets(target);
            int offsetCount = offsets.Length;
            System.Array.Resize(ref offsets, offsetCount + 1);
            offsets[offsetCount] = new Pose(localPosition, Quaternion.identity);
            PPP_Settings.SetMultiPivotOffsets(target, offsets);

            TotalRefresh();
            ShowAddFeedback(localPosition);
            SceneView.RepaintAll();
        }

        // Briefly turns the add button into a confirmation, then restores it.
        private void ShowAddFeedback(Vector3 offset)
        {
            _addFeedbackActive = true;
            _addPivotButton.SetEnabled(false);
            _addPivotButton.text = $"✓  Added at [{offset.x:0.##}, {offset.y:0.##}, {offset.z:0.##}]";

            _addFeedbackItem?.Pause();
            _addFeedbackItem = _addPivotButton.schedule.Execute(ResetAddButton).StartingIn(ADD_FEEDBACK_MS);
        }

        private void ResetAddButton()
        {
            _addFeedbackItem?.Pause();
            _addFeedbackItem = null;
            _addFeedbackActive = false;
            _addPivotButton.SetEnabled(true);
            _addPivotButton.text = "+  Add New Pivot";
        }

        private void SetPivotMode(PivotMode mode)
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
                return;

            PPP_Settings.SetMultiPivotMode(target, mode);

            TotalRefresh();
            SceneView.RepaintAll();
        }

        private void TotalRefresh()
        {
            // Ahead of the early returns below: the mode can be changed from a
            // different copy of this panel, and this one should not be left
            // advertising the wrong destination.
            RefreshPanelModeButton();

            GameObject target = Selection.activeGameObject;
            bool isSingleSelection = Selection.gameObjects.Length == 1 && target != null;

            string message = GetBlockingMessage();

            if (message != null)
            {
                _messageLabel.text = message;
                _messageLabel.style.display = DisplayStyle.Flex;
                _sharedPivotConatiner.style.display = DisplayStyle.None;
                _pivotActionsCard.style.display = DisplayStyle.None;
                _offsetTransformCard.style.display = DisplayStyle.None;
                _enabledSwitch.style.display = DisplayStyle.None;
                return;
            }

            _messageLabel.style.display = DisplayStyle.None;
            _enabledSwitch.style.display = DisplayStyle.Flex;
            _enabledSwitch.SetState(PPP_Settings.Enabled);

            if (!PPP_Settings.Enabled)
            {
                _sharedPivotConatiner.style.display = DisplayStyle.None;
                _pivotActionsCard.style.display = DisplayStyle.None;
                _offsetTransformCard.style.display = DisplayStyle.None;
                return;
            }

            RefreshOffsetTransformValues();

            int pivotCount = isSingleSelection ? PPP_Settings.GetMultiPivotOffsets(target).Length : 0;
            PivotMode mode = pivotCount > 0 ? PPP_Settings.GetMultiPivotMode(target) : PivotMode.Default;
            bool inEditMode = mode == PivotMode.Edit;
            bool showModeToggle = pivotCount > 0;

            // A pending "Added" confirmation belongs to the previous refresh's
            // state; clear it so it never lingers after a selection change.
            if (_addFeedbackActive)
                ResetAddButton();

            _pivotActionsCard.style.display = isSingleSelection ? DisplayStyle.Flex : DisplayStyle.None;

            // Add/remove the mode row instead of toggling its display. Unity
            // 2022's layout engine doesn't re-measure a subtree when display
            // flips None -> Flex, so its flex-grow buttons keep a collapsed
            // height (a thin bar). Re-inserting takes the same fresh-layout
            // path as initial creation, which lays out correctly everywhere.
            if (showModeToggle)
            {
                if (_modeButtonsRow.parent == null)
                    _pivotActionsCard.Insert(_pivotActionsCard.IndexOf(_addPivotButton), _modeButtonsRow);
            }
            else
            {
                _modeButtonsRow.RemoveFromHierarchy();
            }

            // The add button is hidden while editing the object's own pivots.
            _addPivotButton.style.display = inEditMode ? DisplayStyle.None : DisplayStyle.Flex;

            _pivotCountBadge.style.display = pivotCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (pivotCount > 0)
                _pivotCountBadge.text = pivotCount == 1 ? "1 PIVOT" : $"{pivotCount} PIVOTS";

            if (showModeToggle)
            {
                PPP_OverlayWidgets.SetButtonActive(_defaultModeButton, !inEditMode, PPP_OverlayWidgets.AccentColor);
                PPP_OverlayWidgets.SetButtonActive(_editModeButton, inEditMode, PPP_OverlayWidgets.EditModeColor);
            }

            // The shared pivot (and its offset controls) is hidden while editing
            // the object's own pivots.
            if (inEditMode)
            {
                _sharedPivotConatiner.style.display = DisplayStyle.None;
                return;
            }

            _sharedPivotConatiner.style.display = DisplayStyle.Flex;
            RefreshCreateParentButton();
            RefreshOriginButtons();

            UnitMode unitMode = PPP_Settings.GetUnitMode(target);
            bool isAbsolute = unitMode == UnitMode.Unbounded;

            PPP_OverlayWidgets.SetButtonActive(_normalizedModeButton, !isAbsolute, PPP_OverlayWidgets.AccentColor);
            PPP_OverlayWidgets.SetButtonActive(_absoluteModeButton, isAbsolute, PPP_OverlayWidgets.AccentColor);

            _canvasBoundsField.SetValueWithoutNotify(PPP_Settings.CanvasBoundsMode);

            _normalizedRowsContainer.style.display = isAbsolute ? DisplayStyle.None : DisplayStyle.Flex;
            _absoluteRowsContainer.style.display = isAbsolute ? DisplayStyle.Flex : DisplayStyle.None;

            if (isAbsolute)
            {
                Vector3 absoluteValues = PPP_Settings.GetAbsoluteValues(target);
                _absoluteFieldX.SetValueWithoutNotify(absoluteValues.x);
                _absoluteFieldY.SetValueWithoutNotify(absoluteValues.y);
                _absoluteFieldZ.SetValueWithoutNotify(absoluteValues.z);
            }
            else
            {
                Vector3 values = PPP_Settings.GetSliderValues(target);

                _sliderX.SetValueWithoutNotify(values.x);
                _sliderY.SetValueWithoutNotify(values.y);
                _sliderZ.SetValueWithoutNotify(values.z);

                _fieldX.SetValueWithoutNotify(values.x);
                _fieldY.SetValueWithoutNotify(values.y);
                _fieldZ.SetValueWithoutNotify(values.z);
            }
        }

        private string GetBlockingMessage()
        {
            return PPP_Handles.CurrentState switch
            {
                PPP_Handles.State.NotInitialized => "Loading",
                PPP_Handles.State.Visible => null,
                PPP_Handles.State.NotEnabled => null,
                // Only reachable for a frame while the panel is coming back:
                // the handles haven't redrawn yet, but there is nothing to warn
                // about, so show the normal controls.
                PPP_Handles.State.PanelHidden => null,
                PPP_Handles.State.EditPivotMode => null,
                PPP_Handles.State.NoBounds => "Selected object does not have any component to draw bounds from.",
                PPP_Handles.State.NoTargets => "No object selected.",
                PPP_Handles.State.UnsupportedTool => "The current tool is not supported.\nUse Move, Rotate, Scale, or Transform.",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnEnabledButtonClicked()
        {
            PPP_Settings.Enabled = !PPP_Settings.Enabled;
            TotalRefresh();
            SceneView.RepaintAll();
        }

        private void OnPanelModeButtonClicked()
        {
            PPP_PanelHost.SwitchTo(PPP_PanelHost.Mode == PPP_PanelMode.Overlay
                ? PPP_PanelMode.Window
                : PPP_PanelMode.Overlay);

            // The move itself is deferred (this very panel is usually the one
            // being put away), so repaint the button now rather than waiting for
            // a refresh that this copy may not live to see.
            RefreshPanelModeButton();
        }

        /// <summary>
        /// The button shows where the panel would go, not where it is: its glyph
        /// is the chrome a click switches to.
        ///
        /// Polled rather than driven by an event, because the mode can also change
        /// from the other host entirely (opening the window, or ticking the overlay
        /// on from the scene view's own menu). The glyph is only rebuilt when it
        /// would actually differ.
        /// </summary>
        private void RefreshPanelModeButton()
        {
            if (_panelModeButton == null)
                return;

            PPP_PanelMode mode = PPP_PanelHost.Mode;
            if (_shownPanelMode == mode)
                return;

            _shownPanelMode = mode;
            bool isOverlay = mode == PPP_PanelMode.Overlay;

            PPP_OverlayWidgets.SetIconButtonGlyph(_panelModeButton, isOverlay
                ? PPP_OverlayWidgets.CreateDockedPanelIcon()
                : PPP_OverlayWidgets.CreateFloatingPanelIcon());

            _panelModeButton.tooltip = isOverlay
                ? "Dock Pivot++ as its own window instead of a scene view overlay."
                : "Show Pivot++ as a scene view overlay instead of a docked window.";
        }
    }
}
