namespace CavesOfOoo.Core
{
    /// <summary>
    /// Event fired on an entity when its BoredGoal runs and no hostile is found.
    /// Parts can handle this to inject custom idle behavior.
    /// Returns true if no handler consumed it (default behavior should proceed).
    /// Mirrors Qud's AIBoredEvent.Check() pattern.
    /// </summary>
    public static class AIBoredEvent
    {
        public const string ID = "AIBored";

        /// <summary>
        /// Fire the AIBored event on the entity.
        /// Returns true if unhandled (default wander/idle should proceed).
        /// Returns false if a part consumed the event.
        /// </summary>
        public static bool Check(Entity entity)
        {
            if (entity == null) return true;
            var e = GameEvent.New(ID);
            bool result = entity.FireEvent(e);
            bool handled = e.Handled;
            e.Release();
            return result && !handled;
        }
    }
}
