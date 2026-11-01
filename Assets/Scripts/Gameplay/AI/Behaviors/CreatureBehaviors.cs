using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 (Docs/UPPER-TIER-BESTIARY.md §3.2): a creature behaviour consulted by
    /// <c>KillGoal</c> before the ordinary attack, in part order. True means the
    /// creature's whole action was spent.
    /// </summary>
    public interface ICombatBehavior
    {
        bool TryAct(Entity target, Zone zone, Random rng, bool targetVisible);
    }

    /// <summary>
    /// P5: an action already under way (a stoop in the air, a drag) that owns the
    /// creature's turn before any goal runs, like <c>CommittedMeleePart.AdvancePendingAction</c>.
    /// </summary>
    public interface IPendingBehavior
    {
        bool AdvancePending(Zone zone, Random rng);
    }

    /// <summary>
    /// P5: perception other than sight. Consulted wherever an AI asks whether it can
    /// perceive a hostile; true replaces the line-of-sight answer for this observer.
    /// </summary>
    public interface IPerceptionBehavior
    {
        bool TryPerceive(Entity other, Zone zone, out bool perceived);
    }

    public static class CreatureBehaviors
    {
        /// <summary>Ask every <see cref="ICombatBehavior"/> part, in order. No allocation.</summary>
        public static bool TryAct(Entity actor, Entity target, Zone zone, Random rng, bool visible)
        {
            var parts = actor?.Parts;
            if (parts == null) return false;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is ICombatBehavior b && b.TryAct(target, zone, rng, visible)) return true;
            return false;
        }

        public static bool AdvancePending(Entity actor, Zone zone, Random rng)
        {
            var parts = actor?.Parts;
            if (parts == null) return false;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is IPendingBehavior b && b.AdvancePending(zone, rng)) return true;
            return false;
        }

        /// <summary>The observer's perception override, if any (looked up once per scan).</summary>
        public static IPerceptionBehavior PerceptionOf(Entity observer)
        {
            var parts = observer?.Parts;
            if (parts == null) return null;
            for (int i = 0; i < parts.Count; i++) if (parts[i] is IPerceptionBehavior p) return p;
            return null;
        }

        /// <summary>Null when no part overrides perception (the caller uses sight).</summary>
        public static bool? Perceives(Entity observer, Entity other, Zone zone)
        {
            var parts = observer?.Parts;
            if (parts == null) return null;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is IPerceptionBehavior p && p.TryPerceive(other, zone, out bool seen)) return seen;
            return null;
        }
    }
}
