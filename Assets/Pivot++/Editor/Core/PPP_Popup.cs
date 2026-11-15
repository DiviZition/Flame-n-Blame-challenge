using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Pivot++'s replacement for EditorUtility.DisplayDialog: a modal window in
    /// the plugin's own theme, where every choice is a card that explains what it
    /// does. Like DisplayDialog it blocks until the user answers, so callers can
    /// stay sequential.
    /// </summary>
    internal class PPP_Popup : EditorWindow
    {
        internal struct Option
        {
            public string title;
            public string description;
            /// <summary>When set, the option is shown dimmed with this explanation.</summary>
            public string unavailableReason;
            /// <summary>Marks a choice with a lasting side effect (e.g. breaking a prefab link).</summary>
            public bool destructive;
        }

        private const float WIDTH = 440f;
        private const float INITIAL_HEIGHT = 260f;
        private const int CANCELLED = -1;
        private const double ATTENTION_PULSE_DURATION = 1.2;

        // Static because the window is destroyed by the time the modal loop returns.
        private static int _result = CANCELLED;

        private string _heading;
        private string _message;
        private Option[] _options;
        private IList<string> _bullets;
        private string _confirmLabel;
        private string _cancelLabel;
        private VisualElement _content;

        /// <summary>Asks the user to pick one of the options. Returns its index, or -1 when cancelled.</summary>
        internal static int Choose(string heading, string message, params Option[] options)
        {
            return Open(heading, message, options, null, null, "Cancel");
        }

        /// <summary>Asks the user to confirm after reading a list of consequences.</summary>
        internal static bool Confirm(string heading, string message, IList<string> bullets, string confirmLabel)
        {
            return Open(heading, message, null, bullets, confirmLabel, "Cancel") == 0;
        }

        private static int Open(string heading, string message, Option[] options, IList<string> bullets,
            string confirmLabel, string cancelLabel)
        {
            _result = CANCELLED;

            PPP_Popup popup = CreateInstance<PPP_Popup>();
            popup._heading = heading;
            popup._message = message;
            popup._options = options;
            popup._bullets = bullets;
            popup._confirmLabel = confirmLabel;
            popup._cancelLabel = cancelLabel;
            popup.titleContent = new GUIContent("Pivot++");

            // Sized to its content once laid out (see FitToContent).
            popup.CenterOnMainWindow(INITIAL_HEIGHT);

            popup.ShowModalUtility();
            return _result;
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            _content = new VisualElement();
            _content.style.flexShrink = 0;
            _content.style.paddingLeft = 14;
            _content.style.paddingRight = 14;
            _content.style.paddingTop = 12;
            _content.style.paddingBottom = 12;
            root.Add(_content);

            Label brand = new Label("PIVOT++");
            brand.style.fontSize = 9;
            brand.style.unityFontStyleAndWeight = FontStyle.Bold;
            brand.style.color = PPP_OverlayWidgets.AccentColor;
            _content.Add(brand);

            Label heading = new Label(_heading);
            heading.style.fontSize = 15;
            heading.style.marginTop = 2;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = PPP_OverlayWidgets.GetPrimaryTextColor();
            heading.style.whiteSpace = WhiteSpace.Normal;
            _content.Add(heading);

            if (!string.IsNullOrEmpty(_message))
            {
                Label message = CreateBodyLabel(_message);
                message.style.marginTop = 6;
                message.style.marginBottom = 10;
                _content.Add(message);
            }

            if (_bullets != null && _bullets.Count > 0)
                _content.Add(CreateBulletCard(_bullets));

            if (_options != null)
            {
                for (int i = 0; i < _options.Length; i++)
                {
                    int index = i;
                    Option option = _options[i];
                    Color accent = option.destructive ? PPP_OverlayWidgets.DangerColor : PPP_OverlayWidgets.AccentColor;
                    _content.Add(PPP_OverlayWidgets.CreateOptionCard(option.title, option.description, accent,
                        option.unavailableReason, () => Finish(index)));
                }
            }

            _content.Add(CreateFooter());
            _content.RegisterCallback<GeometryChangedEvent>(_ => FitToContent());

            root.schedule.Execute(() =>
            {
                // A modal window is placed by the OS when it opens, ignoring the
                // position set beforehand, so it is centered again once shown.
                CenterOnMainWindow(position.height);

                // Focus the root so Escape / Enter work without clicking first.
                root.Focus();
                PulseAttention();
            });
        }

        /// <summary>A few fading accent flashes, so the popup is noticed right away.</summary>
        private void PulseAttention()
        {
            VisualElement overlay = new VisualElement();
            overlay.pickingMode = PickingMode.Ignore;
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            rootVisualElement.Add(overlay);

            double start = EditorApplication.timeSinceStartup;
            IVisualElementScheduledItem pulse = null;
            pulse = overlay.schedule.Execute(() =>
            {
                float t = (float)((EditorApplication.timeSinceStartup - start) / ATTENTION_PULSE_DURATION);
                if (t >= 1f)
                {
                    overlay.RemoveFromHierarchy();
                    pulse.Pause();
                    return;
                }

                float alpha = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * (1f - t) * 0.35f;
                Color accent = PPP_OverlayWidgets.AccentColor;
                overlay.style.backgroundColor = new Color(accent.r, accent.g, accent.b, alpha);
            }).Every(16);
        }

        private void CenterOnMainWindow(float height)
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            position = new Rect(
                main.x + (main.width - WIDTH) * 0.5f,
                main.y + (main.height - height) * 0.5f,
                WIDTH, height);
        }

        private VisualElement CreateBulletCard(IList<string> bullets)
        {
            VisualElement card = PPP_OverlayWidgets.CreateCard();
            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = PPP_OverlayWidgets.EditModeColor;

            for (int i = 0; i < bullets.Count; i++)
            {
                VisualElement row = PPP_OverlayWidgets.CreateRow(i == 0 ? 0 : 5);
                row.style.alignItems = Align.FlexStart;

                Label bullet = new Label("•");
                bullet.style.minWidth = 12;
                bullet.style.unityFontStyleAndWeight = FontStyle.Bold;
                bullet.style.color = PPP_OverlayWidgets.EditModeColor;
                row.Add(bullet);

                Label text = CreateBodyLabel(bullets[i]);
                text.style.flexShrink = 1;
                row.Add(text);

                card.Add(row);
            }

            return card;
        }

        private VisualElement CreateFooter()
        {
            VisualElement footer = PPP_OverlayWidgets.CreateRow(4);
            footer.style.justifyContent = Justify.FlexEnd;

            Button cancel = PPP_OverlayWidgets.CreateButton(_cancelLabel, "Close without changing anything (Esc)",
                () => Finish(CANCELLED));
            cancel.style.minWidth = 90;
            footer.Add(cancel);

            if (!string.IsNullOrEmpty(_confirmLabel))
            {
                Button confirm = PPP_OverlayWidgets.CreateButton(_confirmLabel, "Continue (Enter)",
                    () => Finish(0), true);
                confirm.style.minWidth = 110;
                confirm.style.marginLeft = 6;
                footer.Add(confirm);
            }

            return footer;
        }

        private void FitToContent()
        {
            float height = Mathf.Ceil(_content.layout.height);
            if (height <= 0f || float.IsNaN(height) || Mathf.Approximately(height, position.height))
                return;

            minSize = new Vector2(WIDTH, height);
            maxSize = new Vector2(WIDTH, height);
            CenterOnMainWindow(height);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Finish(CANCELLED);
                evt.StopPropagation();
            }
            else if (!string.IsNullOrEmpty(_confirmLabel) &&
                     (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
            {
                Finish(0);
                evt.StopPropagation();
            }
        }

        private void Finish(int result)
        {
            _result = result;
            Close();
        }

        private static Label CreateBodyLabel(string text)
        {
            Label label = new Label(text);
            label.style.fontSize = 11;
            label.style.color = PPP_OverlayWidgets.GetMutedTextColor();
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }
    }
}
