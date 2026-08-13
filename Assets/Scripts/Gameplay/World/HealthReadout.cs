namespace CavesOfOoo.Core
{
    /// <summary>
    /// "How much is left in it" — for anything the player can point at.
    ///
    /// <para>Creatures keep their hitpoints in a <c>Hitpoints</c> stat;
    /// scenery keeps structural HP on a <see cref="DestructiblePart"/>
    /// instead (see that Part's docstring for why the two are separate
    /// pools). Every surface that wants to show "how much before it
    /// breaks" would otherwise have to know about both, and would show
    /// nothing at all for objects — which is how look mode behaved before
    /// this existed.</para>
    ///
    /// <para>Shared by look mode (<c>LookQueryService</c>) and the interact
    /// menu (<c>WorldActionMenuUI</c>) so the two cannot drift into
    /// disagreeing about the same barrel.</para>
    /// </summary>
    public static class HealthReadout
    {
        /// <summary>Shown instead of a number for things that never break.</summary>
        public const string UnbreakableLabel = "unbreakable";

        /// <summary>
        /// A short readout — <c>"HP 6/10"</c>, or
        /// <see cref="UnbreakableLabel"/> — or the empty string when this
        /// entity has no health of any kind to report (a coin, a puddle).
        /// </summary>
        public static string Describe(Entity target)
        {
            if (target == null) return string.Empty;

            // Creatures first: a mimic has both a Creature tag and a
            // destructible-looking shape, and its real health is the stat.
            if (target.HasTag("Creature"))
            {
                var hp = target.GetStat("Hitpoints");
                return hp != null ? "HP " + hp.Value + "/" + hp.Max : string.Empty;
            }

            var structural = target.GetPart<DestructiblePart>();
            if (structural == null) return string.Empty;

            // Saying "HP 40/40" about a staircase would be a lie of
            // omission — the player would keep hitting it.
            if (structural.Indestructible) return UnbreakableLabel;

            return "HP " + structural.HP + "/" + structural.MaxHP;
        }

        /// <summary>True when <see cref="Describe"/> would return something.</summary>
        public static bool Has(Entity target) => Describe(target).Length > 0;
    }
}
