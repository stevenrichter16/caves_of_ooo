namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.1.5 — abstract base for enhancements that
    /// apply ONLY to melee weapons. Mirrors Qud's
    /// <c>IMeleeModification</c>.
    ///
    /// <para><b>Filter:</b> overrides <see cref="IItemEnhancement.Applicable"/>
    /// to reject any item that doesn't have a <see cref="MeleeWeaponPart"/>.
    /// Subclasses can ADD further filtering (e.g. "cutting weapons only")
    /// by overriding <see cref="Applicable"/> and calling
    /// <c>base.Applicable(item)</c> first.</para>
    ///
    /// <para><b>Usage:</b> concrete melee enhancements (E.2.2
    /// EnhancementSerrated, E.2.3 EnhancementLacquered for shields, etc.)
    /// inherit from this instead of <see cref="IItemEnhancement"/>
    /// directly. The melee-only veto becomes free.</para>
    /// </summary>
    public abstract class IMeleeEnhancement : IItemEnhancement
    {
        /// <summary>Base Applicable: target must be non-null AND have a
        /// <see cref="MeleeWeaponPart"/>. Subclasses can chain via
        /// <c>base.Applicable(item) &amp;&amp; (own check)</c>.</summary>
        public override bool Applicable(Entity item)
        {
            if (!base.Applicable(item)) return false;
            return item.GetPart<MeleeWeaponPart>() != null;
        }
    }
}
