namespace CavesOfOoo.Core
{
    /// <summary>
    /// Where a rite finds the ink it is about to spend.
    ///
    /// <para>This lived on <see cref="StormAnvilMutation"/> because that
    /// was the first rite written, and by SM9 six other rites — plus
    /// <see cref="ConsumingRiteBase"/> itself — were reaching into a
    /// concrete sibling for a helper that has nothing to do with storms.
    /// A base class depending on one of its own siblings is backwards;
    /// this is the same code with the dependency pointing the right
    /// way.</para>
    /// </summary>
    public static class GrimoireInk
    {
        /// <summary>
        /// Finds an inked grimoire in the caster's pack. A rite is cast
        /// FROM the book, so a depleted or absent grimoire is a real
        /// refusal, not an error.
        /// </summary>
        public static GrimoireChargePart FindInked(Entity caster)
        {
            var inv = caster?.GetPart<InventoryPart>();
            if (inv == null) return null;
            for (int i = 0; i < inv.Objects.Count; i++)
            {
                var item = inv.Objects[i];
                if (item == null) continue;
                var charge = item.GetPart<GrimoireChargePart>();
                if (charge != null && charge.HasCharge) return charge;
            }
            return null;
        }
    }
}
