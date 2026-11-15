using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using Scene = UnityEngine.SceneManagement.Scene;
using PivotPlusPlus.Demo;


namespace PivotPlusPlus.EditorStuff
{
    public class PPP_DemoManagerWindow : EditorWindow
    {
        private static readonly GUID DEMO_SCENE_GUIDE = new GUID("9b5ea1d5138c44a4bb68a8c2f2a4cc66");
        private static readonly Vector2 WINDOW_SIZE = new Vector2(360, 520);
        private const double ATTENTION_PULSE_DURATION = 1.2;

        /// <summary>Slide 0 is the "how to use" slide; stations start at slide 1.</summary>
        private const int INTRO_SLIDE = 0;

        private const string FONT_SCALE_PREF_KEY = "PivotPlusPlus.DemoWindow.FontScale";
        private const float FONT_SCALE_MIN = 1f;
        private const float FONT_SCALE_MAX = 2f;
        private const float FONT_SCALE_STEP = 0.1f;

        /// <summary>Pixels of headroom above each tab, so a "new" tag can straddle its top edge.</summary>
        private const float TAB_TAG_OVERHANG = 6f;

        /// <summary>Space between two consecutive lines of a station description.</summary>
        private const float LINE_GAP = 8f;

        /// <summary>Extra space contributed by each blank line an author leaves in a description.</summary>
        private const float BLANK_LINE_GAP = 10f;

        private readonly Dictionary<VisualElement, Vector2> _baseMetrics = new Dictionary<VisualElement, Vector2>();
        private float _fontScale = 1f;
        private Label _fontScaleLabel;
        private VisualElement _textSizeBar;
        private VisualElement _content;
        private VisualElement _body;

        private PPP_DemoStation[] _stations = Array.Empty<PPP_DemoStation>();
        private int _slideIndex = INTRO_SLIDE;

        private int SlideCount => _stations.Length + 1;

        [MenuItem("Tools/Pivot++/Demo Window", priority = 100)]
        public static void ShowWindow()
        {
            bool alreadyOpen = HasOpenInstances<PPP_DemoManagerWindow>();

            var window = GetWindow<PPP_DemoManagerWindow>();
            window.titleContent = new GUIContent("Pivot++ Demo");
            window.minSize = new Vector2(300, 380);

            if (!alreadyOpen)
                CenterOnMainWindow(window);

            window.Show();
            window.Focus();
            window.PulseAttention();
        }

        [InitializeOnLoadMethod]
        private static void RegisterSceneHook()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (AssetDatabase.GUIDFromAssetPath(scene.path) == DEMO_SCENE_GUIDE)
            {
                LoadCameraSettings();
                ShowWindow();
            }
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneOpened += OnAnySceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneOpened -= OnAnySceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            PPP_DemoCameraTween.Stop();
        }

        private static void LoadCameraSettings()
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

            sv.pivot = new Vector3(550f, 250f, -250f);
            sv.rotation = Quaternion.identity;
            sv.size = 100f;
            sv.Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange _) => Rebuild();
        private void OnAnySceneOpened(Scene _, OpenSceneMode __) => Rebuild();
        private void OnActiveSceneChanged(Scene _, Scene __) => Rebuild();

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            _fontScale = Mathf.Clamp(EditorPrefs.GetFloat(FONT_SCALE_PREF_KEY, 1f), FONT_SCALE_MIN, FONT_SCALE_MAX);
            root.Add(CreateTextSizeBar());

            _content = new VisualElement();
            _content.style.flexGrow = 1;
            _content.style.paddingLeft = 12;
            _content.style.paddingRight = 12;
            _content.style.paddingTop = 10;
            _content.style.paddingBottom = 10;
            root.Add(_content);

            Rebuild();
        }

        private void Rebuild()
        {
            if (_content == null)
                return;

            _content.Clear();

            // The elements these were measured from are gone, so the recorded sizes are too.
            _baseMetrics.Clear();

            // Everything is built into a body element that is new on every rebuild. The rescale
            // below has to wait for a layout pass, and a brand new element always reports one as it
            // goes from a zero rect to its real size. Hanging that callback on _content instead
            // silently never fires: _content is created once and fills the window, so its rect is
            // identical before and after a rebuild, and a geometry event only reports a change.
            VisualElement body = new VisualElement();
            body.style.flexGrow = 1;
            _content.Add(body);
            _body = body;

            // _content survives rebuilds, so any font size left on it from a previous pass would be
            // read back as the authored size next time and scaled again - the scale compounding once
            // per station change. Nothing scales it any more, but an inline value from an older
            // build of this window can still be sitting there, so clear it.
            _content.style.fontSize = StyleKeyword.Null;

            body.Add(CreateHeader());

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                body.Add(CreatePlayModeWarning());
            }
            else if (!IsDemoSceneActive())
            {
                body.Add(CreateWrongSceneWarning());
            }
            else
            {
                RefreshStations();

                if (_stations.Length == 0)
                {
                    body.Add(CreateBodyLabel(
                        "No stations found in this scene. The demo scene should contain " +
                        "one or more PPP_DemoStation objects."));
                }
                else
                {
                    _slideIndex = Mathf.Clamp(_slideIndex, 0, SlideCount - 1);

                    body.Add(CreateStationTabs());
                    body.Add(CreateSlide());
                    body.Add(CreateNavRow());
                }
            }

            // Authored sizes only resolve once the new content has been laid out, so the rescale
            // waits for that rather than reading zeroes off elements that have never been measured.
            body.RegisterCallback<GeometryChangedEvent>(OnContentLaidOut);
        }

        private void OnContentLaidOut(GeometryChangedEvent evt)
        {
            if (evt.target is VisualElement body)
                body.UnregisterCallback<GeometryChangedEvent>(OnContentLaidOut);

            if (!Mathf.Approximately(_fontScale, 1f))
                ApplyFontScale();
        }

        // ------------------------------------------------------------- text size

        /// <summary>
        /// Always-visible strip above the slide: the control is useless if you have to read small
        /// text to find it. Matches the welcome window so the two behave the same way.
        /// </summary>
        private VisualElement CreateTextSizeBar()
        {
            VisualElement bar = new VisualElement();
            _textSizeBar = bar;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.justifyContent = Justify.FlexEnd;
            bar.style.flexShrink = 0;
            bar.style.paddingLeft = 12;
            bar.style.paddingRight = 12;
            bar.style.paddingTop = 6;
            bar.style.paddingBottom = 6;
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = PPP_OverlayWidgets.GetDividerColor();

            Label caption = new Label("Text size");
            caption.style.fontSize = 10;
            caption.style.color = PPP_OverlayWidgets.GetMutedTextColor();
            caption.style.marginRight = 6;
            bar.Add(caption);

            Button smaller = PPP_OverlayWidgets.CreateButton("A-", "Make the text in this window smaller.",
                () => StepFontScale(-FONT_SCALE_STEP));
            StyleFontScaleButton(smaller);
            bar.Add(smaller);

            _fontScaleLabel = new Label();
            _fontScaleLabel.style.fontSize = 10;
            _fontScaleLabel.style.color = PPP_OverlayWidgets.GetMutedTextColor();
            _fontScaleLabel.style.minWidth = 34;
            _fontScaleLabel.style.marginLeft = 4;
            _fontScaleLabel.style.marginRight = 4;
            _fontScaleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            bar.Add(_fontScaleLabel);

            Button larger = PPP_OverlayWidgets.CreateButton("A+", "Make the text in this window larger.",
                () => StepFontScale(FONT_SCALE_STEP));
            StyleFontScaleButton(larger);
            bar.Add(larger);

            UpdateFontScaleLabel();
            return bar;
        }

        private static void StyleFontScaleButton(Button button)
        {
            button.style.width = 26;
            button.style.minWidth = 26;
            button.style.height = 20;
            button.style.flexShrink = 0;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        private void StepFontScale(float delta)
        {
            float scaled = Mathf.Clamp(_fontScale + delta, FONT_SCALE_MIN, FONT_SCALE_MAX);

            // Keep the steps on clean tenths so repeated clicks don't drift.
            scaled = Mathf.Round(scaled * 10f) / 10f;

            if (Mathf.Approximately(scaled, _fontScale))
                return;

            _fontScale = scaled;
            EditorPrefs.SetFloat(FONT_SCALE_PREF_KEY, _fontScale);

            UpdateFontScaleLabel();
            ApplyFontScale();
        }

        private void UpdateFontScaleLabel()
        {
            if (_fontScaleLabel != null)
                _fontScaleLabel.text = $"{Mathf.RoundToInt(_fontScale * 100f)}%";
        }

        /// <summary>
        /// Scales every font size under the slide, and the height of buttons along with them so
        /// captions don't get clipped by a fixed height. The bar itself is chrome and stays put.
        /// </summary>
        private void ApplyFontScale()
        {
            // Deliberately the body and not _content: the body is rebuilt from authored styles every
            // time, so its recorded base metrics are always the real ones. Scaling an element that
            // outlives the rebuild makes each pass compound on the last.
            if (_body != null)
                ScaleRecursive(_body);
        }

        private void ScaleRecursive(VisualElement element)
        {
            if (!_baseMetrics.TryGetValue(element, out Vector2 metrics))
            {
                float fontSize = element.resolvedStyle.fontSize;
                if (fontSize <= 0f)
                    fontSize = 12f;

                metrics = new Vector2(fontSize, element is Button ? element.resolvedStyle.height : 0f);
                _baseMetrics[element] = metrics;
            }

            element.style.fontSize = metrics.x * _fontScale;

            if (metrics.y > 0f)
                element.style.height = metrics.y * _fontScale;

            foreach (VisualElement child in element.Children())
                ScaleRecursive(child);
        }

        // ---------------------------------------------------------------- slides

        private VisualElement CreateHeader()
        {
            VisualElement header = new VisualElement();
            header.style.alignItems = Align.Center;
            header.style.paddingBottom = 10;
            header.style.flexShrink = 0;

            Label title = new Label("Pivot++ Demo");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = PPP_OverlayWidgets.AccentColor;
            header.Add(title);
            return header;
        }

        private VisualElement CreateSlide()
        {
            ScrollView scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;

            VisualElement content = scroll.contentContainer;
            content.style.paddingRight = 4;

            if (_slideIndex == INTRO_SLIDE)
                CreateIntroSlide(content);
            else
                CreateStationSlide(content, _stations[_slideIndex - 1]);

            return scroll;
        }

        private void CreateIntroSlide(VisualElement slide)
        {
            slide.Add(CreateSlideHeading("How to Use", false, "GETTING STARTED"));

            slide.Add(CreateStep("1", "Walk the stations in order using Next, or pick one from the strip at the top."));
            slide.Add(CreateStep("2", "Each selects the relevant obects, and sets Pivot++ up for you. Read the slide, then try it in the Scene view."));
            slide.Add(CreateStep("3", "If you are asked to import the TMP Essentials, import it and reload the scene with the button below."));

            bool found = !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(DEMO_SCENE_GUIDE));
            string tooltip = found
                ? "Opens the Pivot++ demo scene (you'll be asked to save unsaved changes first)."
                : "The demo scene could not be found in this project.";

            Button open = PPP_OverlayWidgets.CreateButton("Reload the Scene", tooltip, OpenDemoScene);
            open.style.height = 30;
            open.style.marginTop = 14;
            open.style.unityTextAlign = TextAnchor.MiddleCenter;
            open.SetEnabled(found);
            slide.Add(open);
        }

        private void CreateStationSlide(VisualElement slide, PPP_DemoStation station)
        {
            slide.Add(CreateSlideHeading(
                station.Name,
                station.IsNewInV11,
                $"STATION {_slideIndex} OF {_stations.Length}"));

            string description = station.Description;
            if (string.IsNullOrWhiteSpace(description))
            {
                Label missing = CreateBodyLabel(
                    "This station has no description yet. Add one on its PPP_DemoStation component.");
                missing.style.color = PPP_OverlayWidgets.EditModeColor;
                slide.Add(missing);
            }
            else
            {
                // Every non-empty line becomes its own label, and a blank line in the description
                // adds real space rather than disappearing. Skipping blanks outright - which is what
                // this used to do - silently flattened every deliberate paragraph break in an
                // authored description down to the same single-line gap as everything else.
                int blankRun = 0;

                foreach (string line in description.Replace("\r\n", "\n").Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        blankRun++;
                        continue;
                    }

                    // TrimEnd, not Trim: leading whitespace is how an author indents a sub-point.
                    Label body = CreateBodyLabel(line.TrimEnd());
                    body.style.fontSize = 12;
                    body.style.marginTop = LINE_GAP + blankRun * BLANK_LINE_GAP;
                    slide.Add(body);

                    blankRun = 0;
                }
            }

            if (!station.gameObject.activeSelf)
            {
                Button load = PPP_OverlayWidgets.CreateButton("Go to this Station",
                    "Loads the station and flies the Scene view to it.",
                    () => GoToSlide(_slideIndex, true), true);
                load.style.height = 30;
                load.style.marginTop = 14;
                slide.Add(load);
            }
        }

        private VisualElement CreateSlideHeading(string title, bool isNew, string eyebrow)
        {
            VisualElement wrapper = new VisualElement();
            wrapper.style.marginBottom = 4;

            VisualElement topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems = Align.Center;
            topRow.style.justifyContent = Justify.SpaceBetween;

            topRow.Add(PPP_OverlayWidgets.CreateSectionTitle(eyebrow));

            if (isNew)
                topRow.Add(PPP_OverlayWidgets.CreateBadge("NEW", PPP_OverlayWidgets.EditModeColor));

            wrapper.Add(topRow);

            Label heading = new Label(title);
            heading.style.whiteSpace = WhiteSpace.Normal;
            heading.style.fontSize = 16;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = PPP_OverlayWidgets.GetPrimaryTextColor();
            heading.style.marginTop = 2;
            heading.style.marginBottom = 4;
            wrapper.Add(heading);

            return wrapper;
        }

        // ------------------------------------------------------------ navigation

        private VisualElement CreateNavRow()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 8;
            row.style.flexShrink = 0;

            Button previous = PPP_OverlayWidgets.CreateButton("‹  Back",
                "Go to the previous slide.",
                () => GoToSlide(_slideIndex - 1, true));
            previous.style.height = 28;
            previous.style.flexGrow = 1;
            previous.SetEnabled(_slideIndex > 0);
            row.Add(previous);

            Label counter = new Label($"{_slideIndex + 1} / {SlideCount}");
            counter.style.unityTextAlign = TextAnchor.MiddleCenter;
            counter.style.minWidth = 54;
            counter.style.fontSize = 11;
            counter.style.color = PPP_OverlayWidgets.GetMutedTextColor();
            row.Add(counter);

            bool isLast = _slideIndex >= SlideCount - 1;
            Button next = PPP_OverlayWidgets.CreateButton("Next  ›",
                isLast ? "You have reached the last station." : "Go to the next slide.",
                () => GoToSlide(_slideIndex + 1, true), true);
            next.style.height = 28;
            next.style.flexGrow = 1;
            next.SetEnabled(!isLast);
            row.Add(next);

            return row;
        }

        /// <summary>
        /// Every station as a tab at the top of the window, wrapping onto as many rows as it takes.
        /// Nothing scrolls out of reach, so which stations exist - and which are new - is readable at
        /// a glance rather than something you have to scrub a strip to discover.
        /// </summary>
        private VisualElement CreateStationTabs()
        {
            VisualElement wrapper = new VisualElement();
            wrapper.style.flexShrink = 0;
            wrapper.style.marginBottom = 4;

            VisualElement tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.flexWrap = Wrap.Wrap;
            tabs.style.flexShrink = 0;
            wrapper.Add(tabs);

            tabs.Add(CreateTab("How to Use", INTRO_SLIDE, false));

            for (int i = 0; i < _stations.Length; i++)
            {
                PPP_DemoStation station = _stations[i];
                if (station == null)
                    continue;

                tabs.Add(CreateTab($"{i + 1}. {station.Name}", i + 1, station.IsNewInV11));
            }

            VisualElement divider = PPP_OverlayWidgets.CreateDivider();
            divider.style.marginTop = 6;
            divider.style.marginBottom = 8;
            wrapper.Add(divider);

            return wrapper;
        }

        private VisualElement CreateTab(string label, int slideIndex, bool isNew)
        {
            bool isCurrent = slideIndex == _slideIndex;

            string tooltip = isNew
                ? (isCurrent ? "You are here. New in v1.1." : "New in v1.1. Jump to this station.")
                : (isCurrent ? "You are here." : "Jump to this station.");

            Button tab = PPP_OverlayWidgets.CreateButton(label, tooltip, () => GoToSlide(slideIndex, true));
            tab.style.height = 26;
            tab.style.marginTop = 0;
            tab.style.marginBottom = 0;
            tab.style.marginLeft = 0;
            tab.style.marginRight = 0;
            tab.style.paddingLeft = 10;
            tab.style.paddingRight = 10;
            tab.style.flexShrink = 0;
            tab.style.fontSize = 11;

            // A tab never shrinks, so in a narrow window at a large text scale a long label could
            // outgrow the row. Clipping it to an ellipsis keeps that contained inside the tab's own
            // box instead of painting across the next one.
            tab.style.overflow = Overflow.Hidden;
            tab.style.textOverflow = TextOverflow.Ellipsis;

            // Only the current tab is repainted. SetButtonActive's inactive branch swaps the label to
            // the muted text colour, which on a whole grid of tabs reads as "all disabled" - the
            // colours CreateButton already applied are the legible ones.
            if (isCurrent)
                PPP_OverlayWidgets.SetButtonActive(tab, true, PPP_OverlayWidgets.AccentColor);

            // The tag cannot be a child of the button. A Button is a TextElement and measures itself
            // from its text only while it is childless - give it any child and it sizes from that
            // instead, collapsing the tab to its padding while the text still draws, spilling over
            // the next tab. A wrapper keeps the button childless and lets the tag sit over its corner.
            VisualElement holder = new VisualElement();
            holder.style.flexShrink = 0;
            holder.style.maxWidth = Length.Percent(100f);
            holder.style.marginRight = 6;
            holder.style.marginBottom = 6;
            // Room for the tag to sit half over the button's top edge. Applied to every tab, new or
            // not, so all of them line up on the same baseline.
            holder.style.paddingTop = TAB_TAG_OVERHANG;
            holder.Add(tab);

            if (isNew)
            {
                Label tag = PPP_OverlayWidgets.CreateBadge("new", PPP_OverlayWidgets.EditModeColor);
                tag.style.backgroundColor = PPP_OverlayWidgets.EditModeColor;
                tag.style.color = Color.white;
                tag.style.paddingTop = 0;
                tag.style.paddingBottom = 0;
                tag.style.position = Position.Absolute;
                tag.style.top = 0;
                tag.style.left = 6;
                tag.pickingMode = PickingMode.Ignore;
                tag.tooltip = tooltip;

                // Added after the button so it paints over the corner rather than under it.
                holder.Add(tag);
            }

            return holder;
        }

        /// <summary>
        /// Moves to a slide. Station slides load their station and fly the Scene view to it;
        /// the intro slide leaves the scene exactly as it is.
        /// </summary>
        private void GoToSlide(int slideIndex, bool travel)
        {
            slideIndex = Mathf.Clamp(slideIndex, 0, SlideCount - 1);
            _slideIndex = slideIndex;

            if (travel && slideIndex != INTRO_SLIDE)
                ActivateStation(_stations[slideIndex - 1]);
            else
                Rebuild();
        }

        /// <summary>
        /// Shows the destination, flies to it, and only then hides everything else. Hiding the
        /// station you are standing on before the camera has left makes the trip start on an empty
        /// floor, which is what made the old instant jump feel abrupt.
        /// </summary>
        private void ActivateStation(PPP_DemoStation target)
        {
            if (target == null)
                return;

            target.Activate();

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
                sv.Focus();

            PPP_DemoCameraTween.FlyTo(
                target.CameraPivot,
                target.CameraRotation,
                target.CameraSize,
                () => OnArrived(target));

            Rebuild();
        }

        /// <summary>
        /// Runs once the camera has settled. Clearing the other stations here rather than up front
        /// also self-heals a flight that was interrupted: whichever station is arrived at last is
        /// the one left standing.
        /// </summary>
        private void OnArrived(PPP_DemoStation target)
        {
            if (target == null)
                return;

            foreach (PPP_DemoStation station in _stations)
            {
                if (station != null && station != target)
                    station.Deactivate(target);
            }

            // The selection is re-applied on arrival: activating the station already set it, but a
            // click in the Scene view during the flight would otherwise leave the wrong object
            // under the Pivot++ handles. The settings are deliberately not re-applied, so a slider
            // grabbed mid-flight is not reset.
            target.RestoreSelection();

            // No rebuild here on purpose: the window already shows this station, and rebuilding
            // would reset the slide's scroll position and the strip's scroll offset half a second
            // after the user arrived.
        }

        private void RefreshStations()
        {
            _stations = FindObjectsByType<PPP_DemoStation>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(s => s != null)
                .OrderBy(s => s.Index)
                .ToArray();
        }

        // -------------------------------------------------------------- warnings

        private VisualElement CreatePlayModeWarning()
        {
            VisualElement block = new VisualElement();
            block.Add(PPP_OverlayWidgets.CreateSectionTitle("EXIT PLAY MODE"));
            block.Add(CreateBodyLabel(
                "Pivot++ is an editor-only tool, so the demo runs in edit mode. " +
                "Stop play mode to continue the tour."));

            Button stop = PPP_OverlayWidgets.CreateButton("Exit Play Mode",
                "Stops play mode so you can run the demo.",
                () => EditorApplication.isPlaying = false, true);
            stop.style.height = 30;
            stop.style.marginTop = 10;
            block.Add(stop);

            return block;
        }

        private VisualElement CreateWrongSceneWarning()
        {
            VisualElement block = new VisualElement();
            block.Add(PPP_OverlayWidgets.CreateSectionTitle("OPEN THE DEMO SCENE"));
            block.Add(CreateBodyLabel(
                "This window is supposed to be used with the Pivot++ demo scene. " +
                "Would you like to open it now?"));

            bool found = !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(DEMO_SCENE_GUIDE));
            string tooltip = found
                ? "Opens the Pivot++ demo scene (you'll be asked to save unsaved changes first)."
                : "The demo scene could not be found in this project.";

            Button open = PPP_OverlayWidgets.CreateButton("Open Demo Scene", tooltip, OpenDemoScene, true);
            open.style.height = 30;
            open.style.marginTop = 10;
            open.SetEnabled(found);
            block.Add(open);
            return block;
        }

        private void OpenDemoScene()
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(DEMO_SCENE_GUIDE);
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Pivot++",
                    "The demo scene could not be found. It may have been deleted, " +
                    "or the Demo folder was not imported.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            _slideIndex = INTRO_SLIDE;
            Rebuild();
        }

        private static bool IsDemoSceneActive()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            return AssetDatabase.GUIDFromAssetPath(active.path) == DEMO_SCENE_GUIDE;
        }

        // ----------------------------------------------------------------- bits

        private VisualElement CreateStep(string number, string text)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 8;

            // An explicit size, never an inherited one: a label that inherits its font size takes
            // whatever a parent happens to be carrying, which is how this one ended up rendering at
            // ten times the body text. It keeps the row's default stretch alignment, so its box is
            // exactly as tall as the paragraph beside it however many lines that wraps to.
            Label numberLabel = new Label(number);
            numberLabel.style.fontSize = 12;
            numberLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            numberLabel.style.color = PPP_OverlayWidgets.AccentColor;
            numberLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            numberLabel.style.minWidth = 16;
            numberLabel.style.flexShrink = 0;
            row.Add(numberLabel);

            Label textLabel = CreateBodyLabel(text);
            textLabel.style.fontSize = 12;
            textLabel.style.flexShrink = 1;
            row.Add(textLabel);

            return row;
        }

        private static Label CreateBodyLabel(string text)
        {
            Label label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 11;
            label.style.color = PPP_OverlayWidgets.GetMutedTextColor();
            return label;
        }

        private void PulseAttention()
        {
            VisualElement root = rootVisualElement;
            if (root == null)
                return;

            VisualElement overlay = new VisualElement();
            overlay.pickingMode = PickingMode.Ignore;
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            root.Add(overlay);

            double start = EditorApplication.timeSinceStartup;
            IVisualElementScheduledItem item = null;
            item = overlay.schedule.Execute(() => {
                float t = (float)((EditorApplication.timeSinceStartup - start) / ATTENTION_PULSE_DURATION);
                if (t >= 1f)
                {
                    overlay.RemoveFromHierarchy();
                    item.Pause();
                    return;
                }

                float alpha = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * (1f - t) * 0.35f;
                Color c = PPP_OverlayWidgets.AccentColor;
                overlay.style.backgroundColor = new Color(c.r, c.g, c.b, alpha);
            }).Every(16);
        }

        private static void CenterOnMainWindow(EditorWindow window)
        {
            Rect main = GetEditorMainWindowPos();

            Rect pos = window.position;
            pos.size = WINDOW_SIZE;
            pos.x = main.x + (main.width - pos.width) * 0.5f;
            pos.y = main.y + (main.height - pos.height) * 0.5f;
            window.position = pos;
        }

        private static Rect GetEditorMainWindowPos()
        {
            Type containerWinType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "ContainerWindow");

            if (containerWinType == null)
                return new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);

            FieldInfo showModeField = containerWinType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo positionProperty = containerWinType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);

            if (showModeField == null || positionProperty == null)
                return new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(containerWinType);
            foreach (UnityEngine.Object win in windows)
            {
                if ((int)showModeField.GetValue(win) == 4)
                    return (Rect)positionProperty.GetValue(win, null);
            }

            return new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
        }
    }
}
