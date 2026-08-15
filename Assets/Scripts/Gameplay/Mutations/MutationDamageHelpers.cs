using CavesOfOoo.Skills;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Shared damage-application helpers for spell-like mutations.
    /// Centralizes:
    /// <list type="bullet">
    ///   <item>Tagging spell damage with the <c>"Spell"</c> attribute
    ///         and the elemental attribute (Heat/Cold/Electric/Acid/Light)
    ///         so <see cref="CombatSystem.ApplyResistances"/> applies the
    ///         right resistance stat (HeatResistance / ColdResistance /
    ///         etc.) — pre-WSP7 these were silently bypassed because the
    ///         int-overload of ApplyDamage built a Damage with no
    ///         attributes.</item>
    ///   <item>Querying skill-driven damage modifiers via
    ///         <see cref="SkillEventDispatcher.GetSpellDamageModifier"/>
    ///         — Spellcraft and elemental trees (Pyromancy, Cryomancy,
    ///         etc.) hook here.</item>
    /// </list>
    ///
    /// <para><b>Migration:</b> mutations that currently call
    /// <c>CombatSystem.ApplyDamage(target, damage, attacker, zone)</c>
    /// with a raw int should switch to
    /// <see cref="ApplySpellDamage"/> with the appropriate element
    /// attribute. The element string ("Heat" / "Cold" / "Electric" /
    /// "Acid" / "Light") feeds both the resistance pipeline and the
    /// element-specific skill hooks (e.g., Pyromancy_Conflagration's
    /// "+damage to Burning targets when casting Heat" gate).</para>
    ///
    /// <para><b>Why a static helper instead of putting the logic in
    /// BaseMutation:</b> not all spell-like damage flows through a
    /// single subclass (some via DirectionalProjectileMutationBase,
    /// some inline in mutation HandleEvent). A static helper keeps
    /// the migration uniform without coupling every mutation to a
    /// specific base class.</para>
    /// </summary>
    public static class MutationDamageHelpers
    {
        /// <summary>
        /// Apply spell damage to a target, tagged as elemental + Spell,
        /// with skill-based damage modifiers folded in. Returns the
        /// actual damage landed (post-resistance, post-skill-bonus).
        /// </summary>
        /// <param name="target">The damaged entity.</param>
        /// <param name="baseDamage">Pre-modifier damage roll from the
        /// mutation's DamageDice.</param>
        /// <param name="elementAttribute">"Heat" / "Cold" / "Electric"
        /// / "Acid" / "Light" — feeds both the resistance lookup and
        /// the element-specific skill hooks. Empty string = untyped
        /// magic damage (still tagged Spell, no resistance).</param>
        /// <param name="attacker">The casting entity (skill source).
        /// May be null for environmental spell damage.</param>
        /// <param name="zone">The zone, for ApplyDamage's downstream
        /// event firing.</param>
        public static int ApplySpellDamage(Entity target, int baseDamage,
            string elementAttribute, Entity attacker, Zone zone)
            => CavesOfOoo.Skills.SpellDamageHelpers.ApplySpellDamage(
                target, baseDamage, elementAttribute, attacker, zone);
    }
}
