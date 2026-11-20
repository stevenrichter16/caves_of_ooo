namespace CavesOfOoo.Core
{
    /// <summary>
    /// Cursor interaction modes. V1 uses Look, but the enum leaves space
    /// for future cursor-targeted actions without redesigning the state model.
    /// </summary>
    public enum WorldCursorMode
    {
        Look,
        AbilityTarget,
        TalkTarget,
        InspectTarget,
        ThrowTarget,
        /// <summary>Choosing a cell to put something in (moving a set-down kit): the outline is green where it fits and red where it does not.</summary>
        PlaceTarget
    }
}
