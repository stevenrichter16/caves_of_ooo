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
        InspectTarget
    }
}
