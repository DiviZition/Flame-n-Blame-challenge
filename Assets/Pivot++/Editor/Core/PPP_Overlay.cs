using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Hosts the Pivot++ panel as a scene view overlay. All of the UI lives in
    /// <see cref="PPP_Panel"/>, which the dockable <see cref="PPP_Window"/> shows
    /// just the same; this class is only the scene view's half of the
    /// arrangement.
    /// </summary>
    // Shown by default: the scene handles only take over the transform gizmo
    // while this panel is on screen, so a layout that has never seen Pivot++
    // would otherwise get neither the panel nor the handles. Layouts that
    // already have a saved state for this overlay keep it.
    [Overlay(typeof(SceneView), "Pivot++", defaultDisplay: true)]
    public class PPP_Overlay : Overlay, IPPP_PanelHost
    {
        // Responsive panel sizing. The panel targets a fraction of the scene
        // view's width, clamped to a sane band, so it neither stretches on
        // wide layouts (e.g. a size saved by another project) nor overflows
        // tiny scene views.
        const float MIN_PANEL_WIDTH = 300f;
        const float MAX_PANEL_WIDTH = 400f;

        // False until the editor has ticked once past this overlay's creation.
        private bool _settled;

        bool IPPP_PanelHost.IsPanelVisible => displayed;

        public override void OnCreated()
        {
            PPP_PanelHost.Register(this);
            displayedChanged += OnDisplayedChanged;

            // Whatever visibility Unity gives this overlay as part of building the
            // scene view - the saved state, or the attribute's default - is not
            // someone asking for the panel to move here, so it must not close a
            // window the user has docked. Opening a second scene view would
            // otherwise take the panel away from them. Claims only count once
            // creation has settled.
            EditorApplication.delayCall += () => _settled = true;
        }

        private void OnDisplayedChanged(bool isDisplayed)
        {
            // Turning the overlay on is a request for the panel to live here, so
            // it takes the panel and the window is closed. Nothing is enforced in
            // the other direction: being hidden says nothing about where the panel
            // should go, only that this scene view does not want it.
            if (isDisplayed && _settled)
                PPP_PanelHost.ClaimPanel(PPP_PanelMode.Overlay);

            // The handles read the panel's visibility while drawing, so every
            // scene view has to redraw for the default tool gizmo to be handed
            // back (or taken over again) right away instead of on the next
            // incidental repaint.
            SceneView.RepaintAll();
        }

        public override VisualElement CreatePanelContent()
        {
            // Registering here as well as in OnCreated keeps an on-screen panel
            // accounted for even if only one of the two callbacks runs.
            PPP_PanelHost.Register(this);

            // Clamp the overlay itself. Overlay sizes are saved per project, so
            // a stretched size carried over from another layout gets clamped
            // back into this band automatically.
            minSize = new Vector2(MIN_PANEL_WIDTH, 0f);
            maxSize = new Vector2(MAX_PANEL_WIDTH, float.MaxValue);

            // The scene view's own layout is the panel's width reference; the
            // container window is only consulted before that layout resolves.
            return new PPP_Panel(PPP_PanelMode.Overlay,
                () => containerWindow != null ? containerWindow.position.width : 0f);
        }

        public override void OnWillBeDestroyed()
        {
            displayedChanged -= OnDisplayedChanged;
            PPP_PanelHost.Unregister(this);
            // The last panel going away leaves nothing to turn the tool back on,
            // so let the scene views drop the handle override with it.
            SceneView.RepaintAll();
        }
    }
}
