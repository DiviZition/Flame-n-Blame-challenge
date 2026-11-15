using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// The Pivot++ design system: a small palette, a set of spacing / radius
    /// constants and the element factory built on top of them. Everything the
    /// overlay and the welcome window show is created here, so the two stay
    /// visually identical and a token change lands everywhere at once.
    /// </summary>
    public static class PPP_OverlayWidgets
    {
        // ---------------------------------------------------------------- palette

        public static readonly Color AccentColor = new Color(0.26f, 0.53f, 0.96f);
        public static readonly Color EnabledColor = new Color(0.20f, 0.70f, 0.45f);
        public static readonly Color EditModeColor = new Color(0.93f, 0.60f, 0.20f);
        public static readonly Color DangerColor = new Color(0.90f, 0.36f, 0.34f);

        private static readonly Color XAxisColor = new Color(0.94f, 0.38f, 0.36f);
        private static readonly Color YAxisColor = new Color(0.48f, 0.80f, 0.31f);
        private static readonly Color ZAxisColor = new Color(0.32f, 0.60f, 0.98f);

        // ---------------------------------------------------------------- metrics

        /// <summary>Gap between two stacked cards.</summary>
        public const float CARD_GAP = 6f;
        /// <summary>Gap between two blocks inside the same card.</summary>
        public const float BLOCK_GAP = 8f;
        /// <summary>Gap between two rows of the same block.</summary>
        public const float ROW_GAP = 3f;

        public const float CARD_RADIUS = 5f;
        public const float CONTROL_RADIUS = 3f;

        public const float FIELD_HEIGHT = 20f;
        public const float BUTTON_HEIGHT = 24f;
        public const float SEGMENT_HEIGHT = 21f;
        public const float AXIS_CHIP_SIZE = 16f;
        /// <summary>A square button holding a glyph instead of a caption.</summary>
        public const float ICON_BUTTON_SIZE = 18f;

        private static bool Dark => EditorGUIUtility.isProSkin;

        // ------------------------------------------------------------- tokens

        public static Color GetPrimaryTextColor() => Dark
            ? new Color(0.85f, 0.86f, 0.88f)
            : new Color(0.13f, 0.14f, 0.16f);

        public static Color GetMutedTextColor() => Dark
            ? new Color(0.58f, 0.60f, 0.64f)
            : new Color(0.38f, 0.40f, 0.44f);

        private static Color GetCardColor() => Dark
            ? new Color(0.145f, 0.152f, 0.168f, 0.96f)
            : new Color(0.935f, 0.943f, 0.955f, 0.96f);

        private static Color GetCardBorderColor() => Dark
            ? new Color(1f, 1f, 1f, 0.055f)
            : new Color(0f, 0f, 0f, 0.10f);

        private static Color GetSunkenColor() => Dark
            ? new Color(0.085f, 0.090f, 0.100f, 1f)
            : new Color(0.855f, 0.865f, 0.880f, 1f);

        private static Color GetFieldColor() => Dark
            ? new Color(0.105f, 0.110f, 0.122f, 1f)
            : new Color(1f, 1f, 1f, 1f);

        private static Color GetFieldBorderColor() => Dark
            ? new Color(1f, 1f, 1f, 0.09f)
            : new Color(0f, 0f, 0f, 0.16f);

        public static Color GetDividerColor() => Dark
            ? new Color(1f, 1f, 1f, 0.07f)
            : new Color(0f, 0f, 0f, 0.09f);

        private static Color GetNeutralButtonColor() => Dark
            ? new Color(0.225f, 0.235f, 0.255f)
            : new Color(0.845f, 0.855f, 0.875f);

        /// <summary>A barely-there wash used for hover on transparent surfaces.</summary>
        private static Color GetHoverWashColor() => Dark
            ? new Color(1f, 1f, 1f, 0.06f)
            : new Color(0f, 0f, 0f, 0.05f);

        public static Color GetAxisColor(int axisIndex)
        {
            return axisIndex == 0 ? XAxisColor : axisIndex == 1 ? YAxisColor : ZAxisColor;
        }

        /// <summary>Lifts a color towards the skin's "hovered" direction.</summary>
        private static Color Lift(Color color, float amount = 0.09f)
        {
            if (color.a <= 0.01f)
                return GetHoverWashColor();

            // Translucent surfaces (axis chips) read a hover better as a bump in
            // opacity; opaque ones (buttons) as a shift towards the skin's edge.
            float target = Dark ? 1f : 0f;
            float alpha = color.a >= 0.9f ? color.a : Mathf.Min(1f, color.a + 0.12f);
            return new Color(
                Mathf.Lerp(color.r, target, amount),
                Mathf.Lerp(color.g, target, amount),
                Mathf.Lerp(color.b, target, amount),
                alpha);
        }

        private static Color Tint(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // ------------------------------------------------------- style helpers

        public static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        public static void SetBorder(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        public static void SetPadding(VisualElement element, float value)
        {
            element.style.paddingTop = value;
            element.style.paddingRight = value;
            element.style.paddingBottom = value;
            element.style.paddingLeft = value;
        }

        public static void SetMargin(VisualElement element, float value)
        {
            element.style.marginTop = value;
            element.style.marginRight = value;
            element.style.marginBottom = value;
            element.style.marginLeft = value;
        }

        public static VisualElement CreateRow(float topMargin = 0f)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = topMargin;
            return row;
        }

        /// <summary>A one pixel hairline that eats the remaining row width.</summary>
        public static VisualElement CreateDivider()
        {
            VisualElement divider = new VisualElement();
            divider.style.flexGrow = 1;
            divider.style.flexShrink = 1;
            divider.style.height = 1;
            divider.style.backgroundColor = GetDividerColor();
            return divider;
        }

        // --------------------------------------------------------------- cards

        public static VisualElement CreateCard()
        {
            VisualElement card = new VisualElement();
            SetPadding(card, 9);
            card.style.marginBottom = CARD_GAP;
            card.style.backgroundColor = GetCardColor();
            SetRadius(card, CARD_RADIUS);
            SetBorder(card, 1, GetCardBorderColor());
            return card;
        }

        /// <summary>
        /// Section caption followed by a hairline that runs to the card's edge,
        /// with room for an optional trailing element (a badge, a small action).
        /// </summary>
        public static VisualElement CreateSectionTitle(string text)
        {
            return CreateSectionTitle(text, null);
        }

        public static VisualElement CreateSectionTitle(string text, VisualElement trailing)
        {
            VisualElement header = CreateRow();
            header.style.marginBottom = 7;

            Label title = new Label(text);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 9;
            title.style.color = GetMutedTextColor();
            title.style.flexShrink = 0;
            header.Add(title);

            VisualElement divider = CreateDivider();
            divider.style.marginLeft = 7;
            divider.style.marginRight = trailing == null ? 0 : 7;
            header.Add(divider);

            if (trailing != null)
                header.Add(trailing);

            return header;
        }

        /// <summary>
        /// A card whose section header doubles as a foldout. Children added to it
        /// land in the collapsible body, so it is a drop-in for a plain card plus
        /// its section title.
        /// </summary>
        public class CollapsibleCard : VisualElement
        {
            // Chrome state, not plugin state, so it lives per user in EditorPrefs
            // rather than in the settings asset (which is shared and undoable).
            private const string PREF_PREFIX = "PivotPlusPlus.Overlay.Expanded.";

            private const string ARROW_EXPANDED = "▾";
            private const string ARROW_COLLAPSED = "▸";

            private readonly VisualElement _header;
            private readonly VisualElement _body;
            private readonly Label _arrow;
            private readonly string _prefKey;
            private bool _isExpanded;

            public override VisualElement contentContainer => _body;

            public CollapsibleCard(string title, VisualElement trailing = null)
            {
                _prefKey = PREF_PREFIX + title;

                SetPadding(this, 9);
                style.marginBottom = CARD_GAP;
                style.backgroundColor = GetCardColor();
                SetRadius(this, CARD_RADIUS);
                SetBorder(this, 1, GetCardBorderColor());

                _arrow = new Label();
                _arrow.style.width = 11;
                _arrow.style.flexShrink = 0;
                _arrow.style.fontSize = 9;
                _arrow.style.color = GetMutedTextColor();
                _arrow.style.unityTextAlign = TextAnchor.MiddleLeft;

                _header = CreateSectionTitle(title, trailing);
                _header.Insert(0, _arrow);
                _header.tooltip = "Click to collapse / expand";

                // Bleed into the card's padding so the whole strip is clickable,
                // then pad it back so the caption does not move.
                _header.style.marginTop = -3;
                _header.style.marginLeft = -5;
                _header.style.marginRight = -5;
                _header.style.paddingLeft = 5;
                _header.style.paddingRight = 5;
                _header.style.paddingTop = 3;
                _header.style.paddingBottom = 3;
                SetRadius(_header, CONTROL_RADIUS);

                _header.RegisterCallback<PointerEnterEvent>(_ => _header.style.backgroundColor = GetHoverWashColor());
                _header.RegisterCallback<PointerLeaveEvent>(_ => _header.style.backgroundColor = Color.clear);
                _header.AddManipulator(new Clickable(() => SetExpanded(!_isExpanded, true)));

                _body = new VisualElement();

                hierarchy.Add(_header);
                hierarchy.Add(_body);

                SetExpanded(EditorPrefs.GetBool(_prefKey, true), false);
            }

            public void SetExpanded(bool isExpanded, bool remember)
            {
                _isExpanded = isExpanded;

                _arrow.text = isExpanded ? ARROW_EXPANDED : ARROW_COLLAPSED;
                _body.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;

                // Collapsed, the header is the whole card, so it should not keep
                // the gap that separates it from the body.
                _header.style.marginBottom = isExpanded ? 4 : -3;

                if (remember)
                    EditorPrefs.SetBool(_prefKey, isExpanded);
            }
        }

        /// <summary>A small pill, used for counts and other inline status.</summary>
        public static Label CreateBadge(string text, Color color)
        {
            Label badge = new Label(text);
            badge.style.fontSize = 9;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.color = color;
            badge.style.backgroundColor = Tint(color, Dark ? 0.16f : 0.14f);
            badge.style.paddingLeft = 6;
            badge.style.paddingRight = 6;
            badge.style.paddingTop = 2;
            badge.style.paddingBottom = 2;
            badge.style.flexShrink = 0;
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetRadius(badge, 7);
            return badge;
        }

        /// <summary>The label that sits above / beside a control.</summary>
        public static Label CreateFieldLabel(string text)
        {
            Label label = new Label(text);
            label.style.fontSize = 11;
            label.style.color = GetMutedTextColor();
            return label;
        }

        /// <summary>
        /// A notice block: muted text on a tinted background with an accent bar
        /// down the left edge.
        /// </summary>
        public static Label CreateMessageLabel()
        {
            Label label = new Label();
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 11;
            label.style.color = GetMutedTextColor();
            label.style.paddingLeft = 9;
            label.style.paddingRight = 9;
            label.style.paddingTop = 7;
            label.style.paddingBottom = 7;
            label.style.backgroundColor = Tint(EditModeColor, Dark ? 0.10f : 0.13f);
            SetRadius(label, CONTROL_RADIUS);
            label.style.borderLeftWidth = 2;
            label.style.borderLeftColor = Tint(EditModeColor, 0.85f);
            return label;
        }

        // ------------------------------------------------------------- buttons

        /// <summary>
        /// A button that keeps its own colors, because inline background styling
        /// otherwise kills Unity's built-in hover feedback.
        /// </summary>
        private class ThemedButton : Button
        {
            private Color _baseColor;
            private Color _hoverColor;

            public bool IsSegment;
            public bool IsAvailable = true;

            public ThemedButton(Action clickEvent) : base(clickEvent)
            {
                RegisterCallback<PointerEnterEvent>(_ =>
                {
                    if (enabledSelf && IsAvailable)
                        style.backgroundColor = _hoverColor;
                });
                RegisterCallback<PointerLeaveEvent>(_ => style.backgroundColor = _baseColor);
            }

            public void SetAvailable(bool isAvailable)
            {
                IsAvailable = isAvailable;
                style.opacity = isAvailable ? 1f : 0.45f;
                style.backgroundColor = _baseColor;
            }

            public void SetColors(Color background, Color text, bool bold)
            {
                _baseColor = background;
                _hoverColor = Lift(background);
                style.backgroundColor = background;
                style.color = text;
                style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        public static Button CreateButton(string text, string tooltip, Action clickEvent, bool primary = false)
        {
            ThemedButton button = new ThemedButton(clickEvent) { text = text, tooltip = tooltip };
            button.style.height = BUTTON_HEIGHT;
            button.style.fontSize = 11;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetRadius(button, CONTROL_RADIUS);
            SetBorder(button, 1, Color.clear);

            if (primary)
                button.SetColors(AccentColor, Color.white, true);
            else
                button.SetColors(GetNeutralButtonColor(), GetPrimaryTextColor(), false);

            return button;
        }

        /// <summary>
        /// A compact secondary button. Several of them in a row read as a
        /// toolbar; use <see cref="CreateSegmentedRow"/> for exclusive choices.
        /// </summary>
        public static Button CreateModeButton(string text, string tooltip, Action clickEvent)
        {
            Button button = CreateButton(text, tooltip, clickEvent);
            button.style.height = SEGMENT_HEIGHT + 1;
            button.style.fontSize = 10;
            button.style.flexGrow = 1;
            button.style.flexShrink = 1;
            button.style.flexBasis = 0;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            return button;
        }

        /// <summary>
        /// A square button carrying a drawn glyph instead of a caption. The glyph
        /// is inert so the button itself stays the element under the pointer, and
        /// its hover and greyed-out states keep working.
        /// </summary>
        public static Button CreateIconButton(VisualElement icon, string tooltip, Action clickEvent)
        {
            Button button = CreateButton(string.Empty, tooltip, clickEvent);
            button.style.width = ICON_BUTTON_SIZE;
            button.style.minWidth = ICON_BUTTON_SIZE;
            button.style.height = ICON_BUTTON_SIZE;
            button.style.flexShrink = 0;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            SetPadding(button, 0);

            SetIconButtonGlyph(button, icon);
            return button;
        }

        /// <summary>Swaps the glyph of a button made by <see cref="CreateIconButton"/>.</summary>
        public static void SetIconButtonGlyph(Button button, VisualElement icon)
        {
            button.Clear();
            MakeInert(icon);
            button.Add(icon);
        }

        private static void MakeInert(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            foreach (VisualElement child in element.Children())
                MakeInert(child);
        }

        /// <summary>A filled dot, sized and coloured to stand in for a scene marker.</summary>
        public static VisualElement CreateDotIcon(Color color, float size = 6f)
        {
            VisualElement dot = new VisualElement();
            dot.style.width = size;
            dot.style.height = size;
            dot.style.backgroundColor = color;
            SetRadius(dot, size * 0.5f);
            return dot;
        }

        /// <summary>
        /// A dot sitting somewhere along a short track: the miniature of a slider,
        /// used to say where on an axis a value lands. <paramref name="normalized"/>
        /// runs from -1 (start of the track) through 0 (middle) to 1 (end).
        /// </summary>
        public static VisualElement CreateTrackDotIcon(float normalized)
        {
            const float TRACK_WIDTH = 10f;
            const float TRACK_HEIGHT = 2f;
            const float DOT_SIZE = 4f;

            VisualElement icon = new VisualElement();
            icon.style.width = TRACK_WIDTH;
            icon.style.height = DOT_SIZE;

            VisualElement track = new VisualElement();
            track.style.position = Position.Absolute;
            track.style.left = 0;
            track.style.right = 0;
            track.style.top = (DOT_SIZE - TRACK_HEIGHT) * 0.5f;
            track.style.height = TRACK_HEIGHT;
            // Drawn from the text colour rather than the sunken token: the track
            // sits on a button, where the sunken grey is nearly invisible in the
            // light skin.
            track.style.backgroundColor = Tint(GetPrimaryTextColor(), 0.32f);
            SetRadius(track, TRACK_HEIGHT * 0.5f);
            icon.Add(track);

            VisualElement dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.top = 0;
            dot.style.left = (TRACK_WIDTH - DOT_SIZE) * 0.5f * (Mathf.Clamp(normalized, -1f, 1f) + 1f);
            dot.style.width = DOT_SIZE;
            dot.style.height = DOT_SIZE;
            dot.style.backgroundColor = GetPrimaryTextColor();
            SetRadius(dot, DOT_SIZE * 0.5f);
            icon.Add(dot);

            return icon;
        }

        /// <summary>
        /// A panel floating over a view: the outline is the view, the filled
        /// block tucked into its corner is the panel sitting on top of it.
        /// Stands for the scene view overlay.
        /// </summary>
        public static VisualElement CreateFloatingPanelIcon()
        {
            VisualElement icon = CreateChromeFrame();

            VisualElement floater = new VisualElement();
            floater.style.position = Position.Absolute;
            floater.style.right = 1;
            floater.style.bottom = 1;
            floater.style.width = 6;
            floater.style.height = 4;
            floater.style.backgroundColor = GetPrimaryTextColor();
            SetRadius(floater, 1);
            icon.Add(floater);

            return icon;
        }

        /// <summary>
        /// A panel docked into a window: the filled block runs flush to one edge
        /// of the frame rather than floating inside it. Stands for the editor
        /// window.
        /// </summary>
        public static VisualElement CreateDockedPanelIcon()
        {
            VisualElement icon = CreateChromeFrame();

            VisualElement pane = new VisualElement();
            pane.style.position = Position.Absolute;
            pane.style.right = 0;
            pane.style.top = 0;
            pane.style.bottom = 0;
            pane.style.width = 4;
            pane.style.backgroundColor = GetPrimaryTextColor();
            icon.Add(pane);

            return icon;
        }

        /// <summary>The outlined rectangle both chrome glyphs are built on.</summary>
        private static VisualElement CreateChromeFrame()
        {
            VisualElement frame = new VisualElement();
            frame.style.width = 12;
            frame.style.height = 10;
            SetBorder(frame, 1, Tint(GetPrimaryTextColor(), 0.55f));
            SetRadius(frame, 1);
            return frame;
        }

        /// <summary>One option of a segmented control. Flat until it is active.</summary>
        public static Button CreateSegment(string text, string tooltip, Action clickEvent)
        {
            ThemedButton button = (ThemedButton)CreateButton(text, tooltip, clickEvent);
            button.IsSegment = true;
            button.style.height = SEGMENT_HEIGHT;
            button.style.fontSize = 10;
            button.style.flexGrow = 1;
            button.style.flexShrink = 1;
            button.style.flexBasis = 0;
            button.SetColors(Color.clear, GetMutedTextColor(), false);
            return button;
        }

        /// <summary>
        /// Wraps segments in a sunken track so the active one reads as a
        /// selection rather than as two separate buttons.
        /// </summary>
        public static VisualElement CreateSegmentedRow(params Button[] segments)
        {
            VisualElement track = new VisualElement();
            track.style.flexDirection = FlexDirection.Row;
            track.style.backgroundColor = GetSunkenColor();
            SetPadding(track, 2);
            SetRadius(track, CARD_RADIUS);
            SetBorder(track, 1, GetFieldBorderColor());

            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].style.marginLeft = i == 0 ? 0 : 2;
                track.Add(segments[i]);
            }

            return track;
        }

        /// <summary>Styles a toggle-like button as active (highlighted) or inactive (muted).</summary>
        public static void SetButtonActive(Button button, bool isActive, Color activeColor)
        {
            if (button is not ThemedButton themed)
                return;

            if (isActive)
                themed.SetColors(activeColor, Color.white, true);
            else if (themed.IsSegment)
                themed.SetColors(Color.clear, GetMutedTextColor(), false);
            else
                themed.SetColors(GetNeutralButtonColor(), GetMutedTextColor(), false);
        }

        /// <summary>
        /// Greys a button out without disabling it. A disabled element gets no
        /// tooltip, and the tooltip is exactly where an unavailable button explains
        /// why, so the button stays live and its click handler must re-check.
        /// </summary>
        public static void SetButtonAvailable(Button button, bool isAvailable)
        {
            if (button is ThemedButton themed)
                themed.SetAvailable(isAvailable);
        }

        /// <summary>
        /// One choice of a popup: a clickable card with a bold title, an explanation
        /// of what the choice does, and an accent bar. An unavailable option is
        /// dimmed, not clickable, and says why underneath.
        /// </summary>
        public static VisualElement CreateOptionCard(string title, string description, Color accent,
            string unavailableReason, Action onClick)
        {
            bool isAvailable = string.IsNullOrEmpty(unavailableReason);

            VisualElement card = new VisualElement();
            SetPadding(card, 9);
            card.style.paddingLeft = 11;
            card.style.marginBottom = CARD_GAP;
            SetRadius(card, CARD_RADIUS);
            SetBorder(card, 1, GetCardBorderColor());
            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = isAvailable ? accent : GetFieldBorderColor();

            Color background = GetCardColor();
            card.style.backgroundColor = background;

            Label titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 12;
            titleLabel.style.color = GetPrimaryTextColor();
            titleLabel.style.whiteSpace = WhiteSpace.Normal;
            card.Add(titleLabel);

            Label descriptionLabel = new Label(description);
            descriptionLabel.style.fontSize = 11;
            descriptionLabel.style.marginTop = 3;
            descriptionLabel.style.color = GetMutedTextColor();
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            card.Add(descriptionLabel);

            if (!isAvailable)
            {
                titleLabel.style.opacity = 0.55f;
                descriptionLabel.style.opacity = 0.55f;

                Label reasonLabel = new Label("Unavailable: " + unavailableReason);
                reasonLabel.style.fontSize = 11;
                reasonLabel.style.marginTop = 5;
                reasonLabel.style.color = EditModeColor;
                reasonLabel.style.whiteSpace = WhiteSpace.Normal;
                card.Add(reasonLabel);
                return card;
            }

            Color hoverBackground = Lift(background, 0.05f);
            Color hoverBorder = Tint(accent, 0.55f);
            card.RegisterCallback<PointerEnterEvent>(_ =>
            {
                card.style.backgroundColor = hoverBackground;
                card.style.borderTopColor = hoverBorder;
                card.style.borderRightColor = hoverBorder;
                card.style.borderBottomColor = hoverBorder;
            });
            card.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                card.style.backgroundColor = background;
                card.style.borderTopColor = GetCardBorderColor();
                card.style.borderRightColor = GetCardBorderColor();
                card.style.borderBottomColor = GetCardBorderColor();
            });
            card.AddManipulator(new Clickable(onClick));

            return card;
        }

        // -------------------------------------------------------------- switch

        /// <summary>
        /// The master on/off control: an iOS-style track plus a caption, sitting
        /// in its own clickable row.
        /// </summary>
        public class Switch : VisualElement
        {
            private readonly VisualElement _track;
            private readonly VisualElement _knob;
            private readonly Label _caption;
            private readonly string _onText;
            private readonly string _offText;

            public Switch(string onText, string offText, string tooltip, Action clickEvent)
            {
                _onText = onText;
                _offText = offText;

                this.tooltip = tooltip;
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
                style.height = 24;
                style.paddingLeft = 3;
                style.paddingRight = 3;
                SetRadius(this, CONTROL_RADIUS);

                _track = new VisualElement();
                _track.style.width = 28;
                _track.style.height = 15;
                _track.style.flexShrink = 0;
                _track.style.flexDirection = FlexDirection.Row;
                _track.style.alignItems = Align.Center;
                _track.style.paddingLeft = 2;
                _track.style.paddingRight = 2;
                SetRadius(_track, 8);

                _knob = new VisualElement();
                _knob.style.width = 11;
                _knob.style.height = 11;
                _knob.style.flexShrink = 0;
                SetRadius(_knob, 6);
                _track.Add(_knob);

                _caption = new Label();
                _caption.style.marginLeft = 8;
                _caption.style.fontSize = 12;
                _caption.style.unityFontStyleAndWeight = FontStyle.Bold;

                Add(_track);
                Add(_caption);

                RegisterCallback<PointerEnterEvent>(_ => style.backgroundColor = GetHoverWashColor());
                RegisterCallback<PointerLeaveEvent>(_ => style.backgroundColor = Color.clear);
                this.AddManipulator(new Clickable(clickEvent));

                SetState(false);
            }

            public void SetState(bool isOn)
            {
                _track.style.backgroundColor = isOn ? EnabledColor : GetSunkenColor();
                _track.style.justifyContent = isOn ? Justify.FlexEnd : Justify.FlexStart;
                SetBorder(_track, 1, isOn ? Tint(EnabledColor, 0.9f) : GetFieldBorderColor());

                _knob.style.backgroundColor = isOn ? Color.white : GetMutedTextColor();

                _caption.text = isOn ? _onText : _offText;
                _caption.style.color = isOn ? GetPrimaryTextColor() : GetMutedTextColor();
            }
        }

        // --------------------------------------------------------------- axes

        /// <summary>
        /// The X / Y / Z marker: a tinted square holding the axis letter. Also
        /// the drag handle, which is why it is deliberately bigger than the text.
        /// </summary>
        public static Label CreateAxisChip(int axisIndex)
        {
            Color color = GetAxisColor(axisIndex);

            Label chip = new Label(axisIndex == 0 ? "X" : axisIndex == 1 ? "Y" : "Z");
            chip.style.width = AXIS_CHIP_SIZE;
            chip.style.minWidth = AXIS_CHIP_SIZE;
            chip.style.height = AXIS_CHIP_SIZE;
            chip.style.flexShrink = 0;
            chip.style.fontSize = 10;
            chip.style.unityFontStyleAndWeight = FontStyle.Bold;
            chip.style.unityTextAlign = TextAnchor.MiddleCenter;
            chip.style.color = color;
            chip.style.backgroundColor = Tint(color, Dark ? 0.16f : 0.15f);
            SetMargin(chip, 0);
            SetPadding(chip, 0);
            SetRadius(chip, CONTROL_RADIUS);
            return chip;
        }

        // ------------------------------------------------------------- fields

        /// <summary>
        /// Repaints a built-in field's input box with the panel's own field
        /// tokens so Unity's default (much lighter) chrome doesn't show through.
        /// </summary>
        public static void StyleField(VisualElement field)
        {
            field.style.height = FIELD_HEIGHT;
            field.style.marginLeft = 0;
            field.style.marginRight = 0;
            field.style.marginTop = 0;
            field.style.marginBottom = 0;

            VisualElement input = FindInput(field);
            if (input == null)
                return;

            input.style.backgroundColor = GetFieldColor();
            input.style.color = GetPrimaryTextColor();
            input.style.fontSize = 11;
            input.style.paddingLeft = 5;
            input.style.paddingRight = 4;
            SetRadius(input, CONTROL_RADIUS);
            SetBorder(input, 1, GetFieldBorderColor());
        }

        private static VisualElement FindInput(VisualElement field)
        {
            return field.Q("unity-text-input")
                   ?? field.Q(className: "unity-base-field__input");
        }

        /// <summary>Left-labelled row: caption of a fixed width, control filling the rest.</summary>
        public static VisualElement CreateLabeledRow(string label, string tooltip, VisualElement control, float labelWidth = 86f)
        {
            VisualElement row = CreateRow();
            row.tooltip = tooltip;

            Label caption = CreateFieldLabel(label);
            caption.style.width = labelWidth;
            caption.style.minWidth = labelWidth;
            caption.style.flexShrink = 0;
            row.Add(caption);

            control.style.flexGrow = 1;
            control.style.flexShrink = 1;
            row.Add(control);

            return row;
        }

        /// <summary>Strips the built-in label so a field can live inside our own row.</summary>
        public static void HideFieldLabel(VisualElement field)
        {
            Label label = field.Q<Label>(className: "unity-base-field__label");
            if (label == null)
                return;

            label.style.display = DisplayStyle.None;
            label.style.minWidth = 0;
            label.style.width = 0;
        }

        public static void StyleToggle(Toggle toggle)
        {
            toggle.labelElement.style.fontSize = 11;
            toggle.labelElement.style.color = GetMutedTextColor();
            toggle.labelElement.style.minWidth = 0;
            toggle.labelElement.style.flexGrow = 1;
            toggle.style.marginLeft = 0;
            toggle.style.marginRight = 0;

            // The label takes the slack so the box lands hard against the right
            // edge, matching the sliders and fields above it.
            VisualElement input = toggle.Q(className: "unity-base-field__input");
            if (input != null)
            {
                input.style.flexGrow = 0;
                input.style.flexShrink = 0;
            }

            // Trailing text (Toggle.text) sits right of the box: "[ ] Label".
            Label trailingText = toggle.Q<Label>(className: "unity-toggle__text");
            if (trailingText != null)
            {
                trailingText.style.fontSize = 11;
                trailingText.style.color = GetMutedTextColor();
                trailingText.style.marginLeft = 5;
            }

            VisualElement checkmark = toggle.Q(className: "unity-toggle__checkmark");
            if (checkmark == null)
                return;

            checkmark.style.width = 14;
            checkmark.style.height = 14;
            checkmark.style.backgroundColor = GetFieldColor();
            checkmark.style.unityBackgroundImageTintColor = AccentColor;
            SetRadius(checkmark, CONTROL_RADIUS);
            SetBorder(checkmark, 1, GetFieldBorderColor());
        }

        /// <summary>Flattens the slider into a thin track with an axis-colored dragger.</summary>
        public static void StyleSlider(Slider slider, int axisIndex)
        {
            slider.style.height = FIELD_HEIGHT;
            slider.style.marginLeft = 0;
            slider.style.marginRight = 0;
            slider.style.marginTop = 0;
            slider.style.marginBottom = 0;

            VisualElement tracker = slider.Q(className: "unity-base-slider__tracker");
            if (tracker != null)
            {
                tracker.style.height = 3;
                tracker.style.backgroundColor = GetSunkenColor();
                SetRadius(tracker, 2);
                SetBorder(tracker, 0, Color.clear);
            }

            VisualElement dragger = slider.Q(className: "unity-base-slider__dragger");
            if (dragger == null)
                return;

            dragger.style.backgroundColor = GetAxisColor(axisIndex);
            SetRadius(dragger, 6);
            SetBorder(dragger, 0, Color.clear);

            VisualElement draggerBorder = slider.Q(className: "unity-base-slider__dragger-border");
            if (draggerBorder != null)
                draggerBorder.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Makes a label act like a horizontal drag area (Unity's "scrub" fields):
        /// dragging left/right feeds a delta, scaled by sensitivity, to the callback.
        /// </summary>
        public static void RegisterLabelDrag(VisualElement label, float sensitivity, Action<float> applyDelta)
        {
            bool dragging = false;
            float lastX = 0f;

            // A drag handle should look like one, so it also lights up on hover.
            StyleColor baseBackground = label.style.backgroundColor;
            StyleColor hoverBackground = Lift(baseBackground.value, 0.30f);

            label.RegisterCallback<PointerEnterEvent>(_ => label.style.backgroundColor = hoverBackground);
            label.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (!dragging)
                    label.style.backgroundColor = baseBackground;
            });

            label.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                dragging = true;
                lastX = evt.position.x;
                label.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            label.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging)
                    return;

                float delta = evt.position.x - lastX;
                lastX = evt.position.x;
                if (!Mathf.Approximately(delta, 0f))
                    applyDelta(delta * sensitivity);

                evt.StopPropagation();
            });

            label.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dragging)
                    return;

                dragging = false;
                label.ReleasePointer(evt.pointerId);
                label.style.backgroundColor = baseBackground;
                evt.StopPropagation();
            });
        }
    }
}
