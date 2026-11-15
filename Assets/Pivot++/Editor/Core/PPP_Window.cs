using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Hosts the Pivot++ panel as an ordinary editor window, so it can be docked
    /// anywhere in the layout instead of living inside the scene view. Shows the
    /// same <see cref="PPP_Panel"/> the overlay does.
    /// </summary>
    public class PPP_Window : EditorWindow, IPPP_PanelHost
    {
        // Matches the overlay's narrow band, which is what the panel's rows were
        // laid out against.
        private static readonly Vector2 MIN_SIZE = new Vector2(300f, 160f);

        // A destroyed window is still a live C# object until the GC gets to it,
        // and Unity's == is what knows the difference.
        bool IPPP_PanelHost.IsPanelVisible => this != null;

        [MenuItem("Tools/Pivot++/Panel", priority = 0)]
        internal static void Open()
        {
            // Opening the window is a request for the panel to live here, so it
            // takes the panel and the scene view overlays are put away. Claimed
            // here rather than in OnEnable, which also runs on every domain
            // reload - the panel should change hands when someone asks for it to,
            // not every time the scripts recompile.
            PPP_PanelHost.ClaimPanel(PPP_PanelMode.Window);

            PPP_Window window = GetWindow<PPP_Window>();
            window.titleContent = new GUIContent("Pivot++");
            window.minSize = MIN_SIZE;
            window.Show();
            window.Focus();
        }

        /// <summary>
        /// Closes the window if one is open. Callers defer this: the click that
        /// asks for it usually comes from a button inside the window.
        /// </summary>
        internal static void CloseIfOpen()
        {
            // Guarded, because GetWindow would otherwise create the very window
            // it is about to close.
            if (HasOpenInstances<PPP_Window>())
                GetWindow<PPP_Window>().Close();
        }

        private void OnEnable()
        {
            PPP_PanelHost.Register(this);
        }

        private void OnDisable()
        {
            PPP_PanelHost.Unregister(this);
            // Closing the last panel leaves nothing to turn the tool back on, so
            // let the scene views drop the handle override with it.
            SceneView.RepaintAll();
        }

        private void CreateGUI()
        {
            // A docked window can be made far shorter than the panel's natural
            // height, which the overlay is never asked to be, so the content
            // scrolls here.
            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.Add(new PPP_Panel(PPP_PanelMode.Window));

            rootVisualElement.Add(scroll);
        }
    }
}
