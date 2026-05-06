namespace CavesOfOoo.Core
{
    /// <summary>
    /// WSP2.1 — Broken: an item's identity-marker indicating it has been
    /// damaged/cracked by combat. Applied to ITEM entities (not creature
    /// entities) by <see cref="Skills.Cudgel_Hammer"/> on Cudgel hits.
    ///
    /// <para><b>Gameplay-impact divergence from Qud (documented):</b>
    /// In Qud, a Broken item is functionally disabled — weapons can't
    /// be wielded, armor doesn't protect, tonics aren't drinkable. CoO
    /// in v1 ships this as a <b>marker effect</b> with flavor messages
    /// only. The actual equip-blocking / use-blocking integration with
    /// <c>InventoryPart</c> + <c>EquippablePart</c> + <c>MeleeWeaponPart</c>
    /// is deferred to a follow-on milestone (would require adding
    /// <c>HasEffect&lt;BrokenEffect&gt;</c> checks at every gameplay
    /// path that consumes the item). The marker provides observability
    /// (player sees "X's mace is broken" in the message log) and the
    /// scaffolding for the future gameplay hook.</para>
    ///
    /// <para>Indefinite duration by default — Broken doesn't
    /// auto-recover (matches Qud where Broken needs explicit repair).
    /// Repair-via-effect-removal is a future-content path.</para>
    /// </summary>
    public class BrokenEffect : Effect
    {
        public override string DisplayName => "broken";

        public BrokenEffect(int duration = DURATION_INDEFINITE)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            // The "target" here is the ITEM, not the wearer. Use the
            // item's display name for the message rather than calling
            // GetDisplayName which works for both creatures and items.
            MessageLog.Add(target.GetDisplayName() + " is broken!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is repaired.");
        }

        public override bool OnStack(Effect incoming)
        {
            // Multiple Broken applies are no-ops — the item's already broken.
            // Do not extend duration; the first apply wins.
            return true;
        }

        public override string GetRenderColorOverride() => "&K";
    }
}
