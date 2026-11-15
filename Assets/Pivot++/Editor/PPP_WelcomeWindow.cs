using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Onboarding window shown once after the package is imported (and again
    /// after a version update). First installs get the full onboarding (demo
    /// scene, quick start, feature overview); updates lead with What's New and
    /// shrink the onboarding content to stay out of its way.
    /// </summary>
    public class PPP_WelcomeWindow : EditorWindow
    {
        private const string VERSION = "1.1.0";
        private const string SUPPORT_EMAIL = "itsdevlogger@gmail.com";
        private const string STORE_URL = "https://assetstore.unity.com/packages/package/393670";
        private static readonly GUID DEMO_SCENE_GUIDE = new GUID("9b5ea1d5138c44a4bb68a8c2f2a4cc66");
        private static readonly GUID DOCUMENTATION_GUIDE = new GUID("d1e045ad2937cd14eabf1b03c34a34ae");
        private static readonly Vector2 WINDOW_SIZE = new Vector2(420, 600);
        private static readonly Vector2 MAX_WINDOW_SIZE = new Vector2(900, 1400);

        // Reading comfort is a per-user editor preference, not project data, so
        // it lives in EditorPrefs rather than in the settings asset.
        private const string FONT_SCALE_PREF_KEY = "PivotPlusPlus.WelcomeWindow.FontScale";
        private const float FONT_SCALE_MIN = 1f;
        private const float FONT_SCALE_MAX = 2f;
        private const float FONT_SCALE_STEP = 0.1f;

        /// <summary>
        /// Base font size (and, for buttons, base height) of every element as it
        /// was authored, so scaling stays absolute instead of compounding.
        /// </summary>
        private readonly Dictionary<VisualElement, Vector2> _baseMetrics = new Dictionary<VisualElement, Vector2>();

        private float _fontScale = 1f;
        private Label _fontScaleLabel;
        private VisualElement _textSizeBar;

        /// <summary>
        /// The version this project last saw the welcome window for. Empty on a
        /// fresh install, which is the one case where "What's New" is pointless.
        /// </summary>
        private static string _lastSeenVersion;

        /// <summary>
        /// True when an older Pivot++ was already in this project (or the window
        /// was reopened later), false on a fresh install.
        /// </summary>
        private static bool IsReturningUser => !string.IsNullOrEmpty(_lastSeenVersion);

        /// <summary>
        /// Small label beside a changelog entry's title. Keep new entries to this
        /// vocabulary so each color keeps one meaning.
        /// </summary>
        private enum ChangeTag
        {
            None,
            New,
            Improved,
            BugFix,
            MostRequested
        }

        // ------------------------------------------------------------- palette
        // One color anchor (the accent) and three neutral text tiers. The shared
        // muted color is tinted blue, which melts into the accent-washed card, so
        // this window uses neutral greys instead.

        private static bool Dark => EditorGUIUtility.isProSkin;

        private static Color TitleTextColor => Dark
            ? new Color(0.95f, 0.95f, 0.96f)
            : new Color(0.07f, 0.07f, 0.08f);

        private static Color BodyTextColor => Dark
            ? new Color(0.77f, 0.77f, 0.78f)
            : new Color(0.20f, 0.20f, 0.21f);

        /// <summary>Captions, hints and the thank-you line: legible, but last in line.</summary>
        private static Color FaintTextColor => Dark
            ? new Color(0.60f, 0.60f, 0.61f)
            : new Color(0.34f, 0.34f, 0.35f);

        /// <summary>A hair off the window background, so quiet cards still read as cards.</summary>
        private static Color RaisedSurfaceColor => Dark
            ? new Color(1f, 1f, 1f, 0.045f)
            : new Color(1f, 1f, 1f, 0.40f);


        [InitializeOnLoadMethod]
        private static void AutoOpenAfterImport()
        {
            // Runs after every domain reload; only actually open the first time
            // this version of the plugin lands in this project.
            if (!PPP_Settings.ShowWelcomeWindowOnStartup)
                return;

            if (PPP_Settings.WelcomeWindowShownVersion == VERSION)
                return;

            // Wait until the editor is idle so we don't fight the import/compile.
            EditorApplication.delayCall += () =>
            {
                _lastSeenVersion = PPP_Settings.WelcomeWindowShownVersion;
                PPP_Settings.WelcomeWindowShownVersion = VERSION;
                ShowWindow();
            };
        }

        [MenuItem("Tools/Pivot++/Welcome", priority = 101)]
        public static void ShowWindow()
        {
            _lastSeenVersion ??= PPP_Settings.WelcomeWindowShownVersion;

            bool alreadyOpen = HasOpenInstances<PPP_WelcomeWindow>();

            PPP_WelcomeWindow window = GetWindow<PPP_WelcomeWindow>(true);
            window.titleContent = new GUIContent("Welcome to Pivot++");
            window.minSize = WINDOW_SIZE;

            // Resizable rather than pinned: scaled-up text needs more room, and
            // the user is the one who decides how much.
            window.maxSize = MAX_WINDOW_SIZE;

            if (!alreadyOpen)
                CenterOnMainWindow(window);

            window.Show();
            window.Focus();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            _fontScale = Mathf.Clamp(EditorPrefs.GetFloat(FONT_SCALE_PREF_KEY, 1f), FONT_SCALE_MIN, FONT_SCALE_MAX);
            root.Add(CreateTextSizeBar());

            ScrollView scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            root.Add(scroll);

            VisualElement content = scroll.contentContainer;
            content.style.paddingLeft = 14;
            content.style.paddingRight = 14;
            content.style.paddingTop = 12;
            content.style.paddingBottom = 12;

            content.Add(CreateHeader());

            // Returning users came for the changelog: onboarding content steps
            // back so it doesn't bury what's actually new.
            if (IsReturningUser)
            {
                content.Add(CreateWhatsNewCard());
                content.Add(CreateCompactDemoCard());
                content.Add(CreateQuickStartCard());
                content.Add(CreateSafetySummaryCard());
            }
            else
            {
                content.Add(CreateDemoCard());
                content.Add(CreateQuickStartCard());
                content.Add(CreateFeaturesCard());
            }

            content.Add(CreateLinksCard());
            root.Add(CreateFooter());

            // Authored sizes are only readable once the first layout has resolved
            // them, so the initial scale pass waits for it.
            root.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
        }

        private void OnFirstLayout(GeometryChangedEvent evt)
        {
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

            if (!Mathf.Approximately(_fontScale, 1f))
                ApplyFontScale();
        }

        /// <summary>
        /// Always-visible strip above the scroll view: the control is useless if
        /// you have to read small text to scroll down to it.
        /// </summary>
        private VisualElement CreateTextSizeBar()
        {
            VisualElement bar = new VisualElement();
            _textSizeBar = bar;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.justifyContent = Justify.FlexEnd;
            bar.style.flexShrink = 0;
            bar.style.paddingLeft = 14;
            bar.style.paddingRight = 14;
            bar.style.paddingTop = 6;
            bar.style.paddingBottom = 6;
            bar.style.borderBottomWidth = 1;
            bar.style.borderBottomColor = PPP_OverlayWidgets.GetDividerColor();

            Label caption = new Label("Text size");
            caption.style.fontSize = 10;
            caption.style.color = FaintTextColor;
            caption.style.marginRight = 6;
            bar.Add(caption);

            Button smaller = PPP_OverlayWidgets.CreateButton("A-", "Make the text in this window smaller.",
                () => StepFontScale(-FONT_SCALE_STEP));
            StyleFontScaleButton(smaller);
            bar.Add(smaller);

            _fontScaleLabel = new Label();
            _fontScaleLabel.style.fontSize = 10;
            _fontScaleLabel.style.color = FaintTextColor;
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
        /// Scales every font size in the window, and the height of buttons along
        /// with them so captions don't get clipped by a fixed height.
        /// </summary>
        private void ApplyFontScale()
        {
            foreach (VisualElement child in rootVisualElement.Children())
            {
                // The bar itself stays put: it is chrome, not content.
                if (child == _textSizeBar)
                    continue;

                ScaleRecursive(child);
            }
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



        private VisualElement CreateHeader()
        {
            VisualElement header = new VisualElement();
            header.style.alignItems = Align.Center;
            header.style.paddingTop = 10;
            header.style.paddingBottom = 14;

            Label title = new Label("Pivot++");
            title.style.fontSize = 30;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = PPP_OverlayWidgets.AccentColor;
            header.Add(title);

            Label tagline = new Label("Non-destructive custom pivots for the Unity Editor");
            tagline.style.fontSize = 12;
            tagline.style.marginTop = 2;
            tagline.style.color = BodyTextColor;
            header.Add(tagline);

            Label version = new Label($"Version {VERSION}");
            version.style.fontSize = 10;
            version.style.marginTop = 4;
            version.style.color = FaintTextColor;
            header.Add(version);

            return header;
        }

        /// <summary>
        /// Release notes for this version. Only shown to projects that already had
        /// an older Pivot++ in them; a first-time user has nothing to compare against.
        /// </summary>
        private VisualElement CreateWhatsNewCard()
        {
            VisualElement card = CreateChangelogCard();



            VisualElement title = CreateSectionTitle($"WHAT'S NEW IN {VERSION}");
            title.Q<Label>().style.color = PPP_OverlayWidgets.AccentColor;
            card.Add(title);

            card.Add(CreateFeature("Create Parent At Pivot",
                "Wraps selection in a new parent at the shared pivot; works at runtime too", ChangeTag.MostRequested));
            card.Add(CreateFeature("Dockable Window",
                "Switch Pivot++ between overlay and dockable window anytime"));
            card.Add(CreateFeature("Quick Align Buttons",
                "Snap any axis to -1, 0, or 1, or reset to zero offset"));
            card.Add(CreateFeature("Offset Transform",
                "Live position/rotation/scale of the shared pivot; drag to nudge, or type exact values"));
            card.Add(CreateFeature("Support For More Components",
                "UI, colliders, and layout components now supported alongside MeshRenderer, SkinnedMeshRenderer, Sprite"));
            card.Add(CreateFeature("Scale Lock Respected",
                "Axes no longer scale independently when Transform's scale lock is on", ChangeTag.BugFix));
            card.Add(CreateFeature("Object's Real Pivot Shown",
                "Yellow dot always marks the object's true pivot"));

            return card;
        }

        /// <summary>
        /// Full-size demo prompt for first installs, where the demo scene is the
        /// best way in.
        /// </summary>
        private VisualElement CreateDemoCard()
        {
            VisualElement card = PPP_OverlayWidgets.CreateCard();
            card.style.marginBottom = 8;

            card.Add(CreateSectionTitle("TRY IT"));

            Label blurb = CreateBodyLabel(
                "The demo scene is a guided tour: small stations that each show one " +
                "Pivot++ feature on real objects. The best first stop.");
            card.Add(blurb);

            Button demoButton = PPP_OverlayWidgets.CreateButton("Open Demo Scene",
                "Opens the Pivot++ demo scene (you will be asked to save unsaved changes first).",
                OpenDemoScene, true);
            demoButton.style.height = 32;
            demoButton.style.marginTop = 6;
            card.Add(demoButton);

            return card;
        }

        /// <summary>
        /// One-row demo prompt for returning users, who most likely know the demo
        /// scene already but may want to see the new features in it.
        /// </summary>
        private VisualElement CreateCompactDemoCard()
        {
            VisualElement card = CreateReferenceCard();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;

            Label blurb = CreateLeadLabel("Demo scene:", "a guided tour of every feature");
            blurb.style.flexGrow = 1;
            blurb.style.flexShrink = 1;
            card.Add(blurb);

            Button demoButton = PPP_OverlayWidgets.CreateButton("Open Demo",
                "Opens the Pivot++ demo scene (you will be asked to save unsaved changes first).",
                OpenDemoScene);
            demoButton.style.height = PPP_OverlayWidgets.SEGMENT_HEIGHT + 1;
            demoButton.style.fontSize = 10;
            demoButton.style.paddingLeft = 10;
            demoButton.style.paddingRight = 10;
            demoButton.style.marginLeft = 8;
            demoButton.style.flexShrink = 0;
            card.Add(demoButton);

            return card;
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
            Close();
        }

        private VisualElement CreateQuickStartCard()
        {
            VisualElement card = CreateReferenceCard();

            card.Add(CreateSectionTitle("QUICK START"));
            card.Add(CreateStep("1", "In the Scene view, press  `  (backtick) or use the overlay menu, and enable the Pivot++ overlay under \"Custom\"."));
            card.Add(CreateStep("2", "Select any GameObject that has bounds — a renderer, sprite, collider, UI element or layout component, on itself or a child."));
            card.Add(CreateStep("3", "Adjust the transform handle using sliders or by entering exact values in the overlay. The yellow dot marks the object's real pivot (its transform position), which Pivot++ never changes."));
            card.Add(CreateStep("4", "Use the \"Add Pivot\" button to save current pivot position as a separate pivot specifically for the selected object."));

            return card;
        }

        private VisualElement CreateStep(string number, string text)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 5;

            Label numberLabel = new Label(number);
            numberLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            numberLabel.style.color = PPP_OverlayWidgets.AccentColor;
            numberLabel.style.minWidth = 16;
            row.Add(numberLabel);

            Label textLabel = CreateBodyLabel(text);
            textLabel.style.flexShrink = 1;
            row.Add(textLabel);

            return row;
        }

        private VisualElement CreateFeaturesCard()
        {
            VisualElement card = CreateReferenceCard();

            card.Add(CreateSectionTitle("WHAT YOU GET"));
            card.Add(CreateFeature("Shared Pivot",
                "Slide anywhere in bounds, or type exact offset"));
            card.Add(CreateFeature("Extra Pivots Per Object",
                "Save, reuse, rotate freely"));
            card.Add(CreateFeature("Multi-Object Support",
                "Move/rotate/scale several objects around one combined pivot"));
            card.Add(CreateFeature("Offset Transform Readout",
                "See and edit exact values, or drag to nudge"));
            card.Add(CreateFeature("Non-Destructive",
                "No mesh edits, no added components, nothing ships in builds"));
            card.Add(CreateFeature("Full Undo",
                "Every pivot edit is on Unity's Undo stack"));

            return card;
        }

        /// <summary>
        /// What You Get, boiled down to the one line a returning user still cares
        /// about: that the tool can't hurt their project.
        /// </summary>
        private VisualElement CreateSafetySummaryCard()
        {
            VisualElement card = CreateReferenceCard();

            Label summary = CreateLeadLabel("Why it's safe to use:",
                "no mesh edits, no added components, full Undo, nothing ships in builds");
            card.Add(summary);

            return card;
        }

        private VisualElement CreateFeature(string title, string description, ChangeTag tag = ChangeTag.None)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 8;

            Label bullet = new Label("•");
            bullet.style.color = PPP_OverlayWidgets.AccentColor;
            bullet.style.unityFontStyleAndWeight = FontStyle.Bold;
            bullet.style.minWidth = 12;
            row.Add(bullet);

            VisualElement column = new VisualElement();
            column.style.flexShrink = 1;

            // Wraps so a long title pushes the tag onto its own line instead of
            // squeezing it.
            VisualElement titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.flexWrap = Wrap.Wrap;
            titleRow.style.alignItems = Align.Center;

            Label titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 12;
            titleLabel.style.color = TitleTextColor;
            titleRow.Add(titleLabel);

            if (tag != ChangeTag.None)
                titleRow.Add(CreateTag(tag));

            column.Add(titleRow);

            Label descriptionLabel = CreateBodyLabel(description);
            descriptionLabel.style.marginTop = 2;
            column.Add(descriptionLabel);

            row.Add(column);
            return row;
        }

        private static Label CreateTag(ChangeTag tag)
        {
            string text;
            Color color;
            switch (tag)
            {
                case ChangeTag.Improved:
                    text = "IMPROVED";
                    color = PPP_OverlayWidgets.AccentColor;
                    break;
                case ChangeTag.BugFix:
                    text = "BUG FIX";
                    color = PPP_OverlayWidgets.DangerColor;
                    break;
                case ChangeTag.MostRequested:
                    text = "MOST REQUESTED";
                    color = PPP_OverlayWidgets.EditModeColor;
                    break;
                default:
                    text = "NEW";
                    color = PPP_OverlayWidgets.EnabledColor;
                    break;
            }

            // A soft fill alone reads as decoration next to the accent; a crisp
            // border and a denser fill keep tags as the card's loudest signal.
            Label badge = PPP_OverlayWidgets.CreateBadge(text, color);
            badge.style.fontSize = 9;
            badge.style.letterSpacing = 0.5f;
            badge.style.color = Dark ? Color.Lerp(color, Color.white, 0.25f) : Color.Lerp(color, Color.black, 0.30f);
            badge.style.backgroundColor = new Color(color.r, color.g, color.b, Dark ? 0.24f : 0.18f);
            PPP_OverlayWidgets.SetBorder(badge, 1, new Color(color.r, color.g, color.b, 0.90f));
            PPP_OverlayWidgets.SetRadius(badge, 3);
            badge.style.paddingTop = 1;
            badge.style.paddingBottom = 1;
            badge.style.marginLeft = 7;
            return badge;
        }

        private VisualElement CreateLinksCard()
        {
            VisualElement card = CreateReferenceCard();

            card.Add(CreateSectionTitle("MORE"));

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            Button docsButton = PPP_OverlayWidgets.CreateModeButton("Documentation",
                "Open the full Pivot++ documentation.", OpenDocumentation);
            Button reviewButton = PPP_OverlayWidgets.CreateModeButton("Rate Pivot++ ★",
                "A review on the Asset Store helps a lot!", () => Application.OpenURL(STORE_URL));
            Button supportButton = PPP_OverlayWidgets.CreateModeButton("Support",
                $"Questions or bugs? Write to {SUPPORT_EMAIL}",
                () => Application.OpenURL($"mailto:{SUPPORT_EMAIL}?subject=Pivot%2B%2B%20{VERSION}%20Support"));

            row.Add(docsButton);
            row.Add(reviewButton);
            row.Add(supportButton);
            card.Add(row);

            Label thanks = new Label("♥  Thank you for supporting a solo developer");
            // Least important line in the window: smallest size, faint tier, and
            // clear air above the buttons so it doesn't read as part of that row.
            thanks.style.fontSize = 9;
            thanks.style.marginTop = 10;
            thanks.style.unityTextAlign = TextAnchor.MiddleCenter;
            thanks.style.whiteSpace = WhiteSpace.Normal;
            thanks.style.color = FaintTextColor;
            card.Add(thanks);

            return card;
        }

        private void OpenDocumentation()
        {
            string path = AssetDatabase.GUIDToAssetPath(DOCUMENTATION_GUIDE);
            UnityEngine.Object asset = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            if (asset == null)
            {
                EditorUtility.DisplayDialog("Pivot++", "Documentation.md could not be found.", "OK");
                return;
            }

            AssetDatabase.OpenAsset(asset);
        }

        private VisualElement CreateFooter()
        {
            VisualElement footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.alignItems = Align.Center;

            // The scroll view is the one that should give up height; otherwise
            // scaled-up text crops the footer. Wrapping keeps both halves
            // visible when they no longer fit side by side.
            footer.style.flexShrink = 0;
            footer.style.flexWrap = Wrap.Wrap;
            footer.style.paddingLeft = 14;
            footer.style.paddingRight = 14;
            footer.style.paddingTop = 6;
            footer.style.paddingBottom = 6;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = PPP_OverlayWidgets.GetDividerColor();

            Toggle startupToggle = new Toggle("Show this window on updates");
            startupToggle.value = PPP_Settings.ShowWelcomeWindowOnStartup;
            PPP_OverlayWidgets.StyleToggle(startupToggle);
            startupToggle.labelElement.style.flexGrow = 0;
            startupToggle.RegisterValueChangedCallback(evt => PPP_Settings.ShowWelcomeWindowOnStartup = evt.newValue);
            footer.Add(startupToggle);

            Label hint = new Label("Reopen via Tool ▸ Pivot++ ▸ Welcome");
            hint.style.fontSize = 10;
            hint.style.color = FaintTextColor;
            footer.Add(hint);

            return footer;
        }

        /// <summary>
        /// Changelog cards (What's New) carry the window's one color anchor: an
        /// accent edge bar, header and bullets. The fill stays close to neutral so
        /// the text and tags inside keep their contrast.
        /// </summary>
        private static VisualElement CreateChangelogCard()
        {
            VisualElement card = PPP_OverlayWidgets.CreateCard();
            card.style.marginBottom = 8;
            card.style.paddingLeft = 11;

            Color accent = PPP_OverlayWidgets.AccentColor;
            card.style.backgroundColor = Dark
                ? new Color(accent.r, accent.g, accent.b, 0.05f)
                : new Color(1f, 1f, 1f, 0.55f);
            PPP_OverlayWidgets.SetBorder(card, 0, Color.clear);
            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = accent;

            return card;
        }

        /// <summary>
        /// Reference cards (Quick Start, What You Get, links) sit just off the
        /// window background: present, but a layer below the changelog card.
        /// </summary>
        private static VisualElement CreateReferenceCard()
        {
            VisualElement card = PPP_OverlayWidgets.CreateCard();
            card.style.marginBottom = 8;
            card.style.backgroundColor = RaisedSurfaceColor;
            return card;
        }

        /// <summary>
        /// Section caption in the neutral faint tier; the changelog card recolors
        /// its own to the accent.
        /// </summary>
        private static VisualElement CreateSectionTitle(string text, VisualElement trailing = null)
        {
            VisualElement title = PPP_OverlayWidgets.CreateSectionTitle(text, trailing);
            title.Q<Label>().style.color = FaintTextColor;
            return title;
        }

        /// <summary>
        /// Body text led by a short phrase in the title tier, so one-line cards
        /// still have something for the eye to land on.
        /// </summary>
        private static Label CreateLeadLabel(string lead, string rest)
        {
            string titleHex = ColorUtility.ToHtmlStringRGB(TitleTextColor);
            return CreateBodyLabel($"<b><color=#{titleHex}>{lead}</color></b> {rest}");
        }

        /// <summary>
        /// A secondary action styled as a link, for things that navigate away
        /// rather than do something in the editor.
        /// </summary>
        private static Label CreateTextLink(string text, string tooltip, Action onClick)
        {
            Label link = new Label(text) { tooltip = tooltip };
            link.style.fontSize = 10;
            link.style.flexShrink = 0;
            link.style.color = PPP_OverlayWidgets.AccentColor;

            link.RegisterCallback<PointerEnterEvent>(_ => link.text = $"<u>{text}</u>");
            link.RegisterCallback<PointerLeaveEvent>(_ => link.text = text);
            link.AddManipulator(new Clickable(onClick));

            return link;
        }

        private static Label CreateBodyLabel(string text)
        {
            Label label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 11;
            label.style.color = BodyTextColor;
            return label;
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
                // 4 == MainWindow show mode
                if ((int)showModeField.GetValue(win) == 4)
                    return (Rect)positionProperty.GetValue(win, null);
            }

            return new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
        }
    }
}
