namespace CavesOfOoo.Core
{
    /// <summary>
    /// What the player is told when a haul is refused.
    ///
    /// <para>One sentence per <see cref="DragVerdict"/>, kept apart from the
    /// rules so the vocabulary stays in one place and a new verdict cannot
    /// ship without someone deciding what it sounds like. Each line names
    /// the <i>specific</i> obstacle: "it does not budge" and "you are
    /// already carrying something" send the player to different next
    /// actions, and a generic "you can't do that" sends them nowhere.</para>
    /// </summary>
    public static class DragMessages
    {
        public static string Refusal(DragVerdict verdict, Entity target)
        {
            string it = target != null ? target.GetDisplayName() : "it";

            switch (verdict)
            {
                case DragVerdict.Living:
                    return $"{it} is not a thing to be dragged.";
                case DragVerdict.Rooted:
                    return $"{it} is part of the world here. It does not budge.";
                case DragVerdict.CarryInstead:
                    return $"You could simply pick {it} up.";
                case DragVerdict.TooHeavy:
                    return $"You set your shoulder against {it}. It does not care.";
                case DragVerdict.NotStrongEnough:
                    return $"There is nowhere on {it} to get a grip.";
                case DragVerdict.NotAdjacent:
                    return $"{it} is out of reach.";
                case DragVerdict.HandsFull:
                    return "You are already hauling something.";
                case DragVerdict.TakenByAnother:
                    return $"Someone else has hold of {it}.";
                case DragVerdict.NoActor:
                case DragVerdict.NoTarget:
                case DragVerdict.Ok:
                default:
                    return $"You cannot haul {it}.";
            }
        }
    }
}
