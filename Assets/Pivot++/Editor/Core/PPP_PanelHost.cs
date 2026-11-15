using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PivotPlusPlus.EditorStuff
{
    /// <summary>
    /// Anything that can put a <see cref="PPP_Panel"/> on screen: the scene view
    /// overlay and the dockable window.
    /// </summary>
    internal interface IPPP_PanelHost
    {
        /// <summary>Whether this host's copy of the panel is on screen right now.</summary>
        bool IsPanelVisible { get; }
    }

    /// <summary>
    /// Knows where the Pivot++ panel is shown, and whether it is shown at all.
    ///
    /// Two jobs, both about chrome rather than plugin state:
    ///
    /// 1. Presence. The scene handles only take over the transform gizmo while a
    ///    panel is on screen - hiding the panel is how a user asks Pivot++ to
    ///    step aside, and without this the default gizmo would stay overridden
    ///    with nothing left to hand it back. Kept apart from
    ///    <see cref="PPP_Settings.Enabled"/>, which is persisted and means "the
    ///    user turned the tool off"; this state lives in memory only and is
    ///    rebuilt from the hosts' lifecycle callbacks after each domain reload.
    ///
    /// 2. The mode. The two hosts are exclusive, and the rule is simply that the
    ///    one opened last wins: opening either puts the other away. The mode is
    ///    therefore a record of what happened rather than a rule enforced against
    ///    the user - an earlier version policed it, and closing the window left
    ///    the mode stuck on Window, which slammed the overlay shut every time it
    ///    was opened afterwards. Which chrome won last is a per-user layout
    ///    preference, so it goes to EditorPrefs rather than the settings asset,
    ///    which is shared through source control and undoable.
    /// </summary>
    internal static class PPP_PanelHost
    {
        private const string MODE_PREF_KEY = "PivotPlusPlus.Panel.Mode";

        // One entry per scene view plus at most one window, so a handful at most;
        // a list is cheaper to walk than a set is to hash.
        private static readonly List<IPPP_PanelHost> Hosts = new List<IPPP_PanelHost>();

        // Mirrored in memory so the panel can poll it without hitting EditorPrefs
        // several times a second.
        private static PPP_PanelMode? _mode;

        /// <summary>
        /// True while at least one host shows the panel. A collapsed (icon-only)
        /// overlay still counts as shown.
        ///
        /// Read off the hosts on demand rather than cached from their change
        /// events, so a visibility Unity restores before this class hears about it
        /// can never leave the handles stuck on or off.
        /// </summary>
        internal static bool IsPanelVisible
        {
            get
            {
                foreach (IPPP_PanelHost host in Hosts)
                {
                    if (host != null && host.IsPanelVisible)
                        return true;
                }

                return false;
            }
        }

        internal static void Register(IPPP_PanelHost host)
        {
            if (host != null && !Hosts.Contains(host))
                Hosts.Add(host);
        }

        internal static void Unregister(IPPP_PanelHost host)
        {
            Hosts.Remove(host);
        }

        /// <summary>Which chrome most recently claimed the panel.</summary>
        internal static PPP_PanelMode Mode
        {
            get
            {
                _mode ??= (PPP_PanelMode)EditorPrefs.GetInt(MODE_PREF_KEY, (int)PPP_PanelMode.Overlay);
                return _mode.Value;
            }
        }

        /// <summary>
        /// Called by a host that has just come up, to take the panel and put the
        /// other host away.
        ///
        /// Note what is deliberately absent: nothing re-asserts this later. A host
        /// that is closed afterwards leaves the mode behind as a record only, so
        /// the other one is free to be opened again without being fought.
        /// </summary>
        internal static void ClaimPanel(PPP_PanelMode mode)
        {
            SetMode(mode);

            if (mode == PPP_PanelMode.Window)
            {
                SetOverlaysDisplayed(false);
                return;
            }

            // Deferred: this usually arrives from inside an overlay's own
            // displayed notification, and closing a docked window from there
            // means relaying the editor out mid-notification.
            EditorApplication.delayCall += PPP_Window.CloseIfOpen;
        }

        /// <summary>
        /// Asks for the panel to move to <paramref name="mode"/>, opening that
        /// host. The host itself claims the panel once it is up, which is what
        /// puts the current one away.
        ///
        /// Deferred: the request comes from a button inside the host that is about
        /// to be taken off screen, and tearing that panel down underneath its own
        /// event handler invites trouble.
        /// </summary>
        internal static void SwitchTo(PPP_PanelMode mode)
        {
            // Recorded up front so the button can redraw immediately, even though
            // the move itself lands a tick later.
            SetMode(mode);

            EditorApplication.delayCall += () =>
            {
                if (mode == PPP_PanelMode.Window)
                    PPP_Window.Open();
                else
                    SetOverlaysDisplayed(true);

                SceneView.RepaintAll();
            };
        }

        private static void SetMode(PPP_PanelMode mode)
        {
            if (_mode == mode)
                return;

            _mode = mode;
            EditorPrefs.SetInt(MODE_PREF_KEY, (int)mode);
        }

        /// <summary>
        /// Shows or hides the overlay in every scene view. Showing one makes it
        /// claim the panel in turn, which is what closes the window.
        /// </summary>
        private static void SetOverlaysDisplayed(bool isDisplayed)
        {
            // Snapshot: showing or hiding an overlay runs callbacks that touch the
            // registry.
            IPPP_PanelHost[] snapshot = Hosts.ToArray();

            foreach (IPPP_PanelHost host in snapshot)
            {
                if (host is PPP_Overlay overlay && overlay.displayed != isDisplayed)
                    overlay.displayed = isDisplayed;
            }
        }
    }
}
