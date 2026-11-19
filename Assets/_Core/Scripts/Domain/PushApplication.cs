namespace Flame_n_Blame_Challenge.Domain
{
    /// GroundedOnly forces stop applying the moment the player leaves the ground - a shove from a
    /// physics object dies at a ledge, the extinguisher spray (Always) keeps dragging them off it.
    /// Carries the surviving half of the old PushForceInteraptionType draft; its Override value is
    /// cut with the control-lock idea.
    public enum PushApplication
    {
        Always,
        GroundedOnly,
    }
}
