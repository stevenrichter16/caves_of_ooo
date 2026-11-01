namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B5 — the smallest noise model: moving, striking or casting stamps the actor
    /// with the current tick. An eyeless hunter (<see cref="HuntsBySoundPart"/>) perceives
    /// what made noise within its window, and nothing that stood still. One int property
    /// per actor; it saves with the entity.
    /// </summary>
    public static class Noise
    {
        public const string Property = "NoiseTick";

        public static void Mark(Entity actor)
        {
            // Written directly: a step must not fire IntPropertyChanged through every part.
            if (actor != null) actor.IntProperties[Property] = WorldClock.CurrentTick;
        }

        /// <summary>Made noise within the last <paramref name="window"/> ticks.</summary>
        public static bool Recent(Entity actor, int window)
        {
            if (actor == null) return false;
            int tick = actor.GetIntProperty(Property, int.MinValue);
            return tick != int.MinValue && WorldClock.CurrentTick - tick <= window;
        }
    }
}
