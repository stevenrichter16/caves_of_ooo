using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Shared damage-application helper for spell-like skills — the
    /// skill-side home of the former <c>MutationDamageHelpers</c>
    /// (moved+renamed in the mutations→skills migration, M4).
    /// Centralizes:
    /// <list type="bullet">
    ///   <item>Tagging spell damage with the <c>"Spell"</c> attribute
    ///         and the elemental attribute (Heat/Cold/Electric/Acid/Light)
    ///         so <see cref="CombatSystem.ApplyResistances"/> applies the
    ///         right resistance stat — without the tags, resistances are
    ///         silently bypassed.</item>
    ///   <item>Querying skill-driven damage modifiers via
    ///         <see cref="SkillEventDispatcher.GetSpellDamageModifier"/>
    ///         — Spellcraft and the elemental trees hook here.</item>
    /// </list>
    /// </summary>
    public static class SpellDamageHelpers
    {
        /// <summary>
        /// Apply spell damage to a target, tagged as elemental + Spell,
        /// with skill-based damage modifiers folded in. Returns the
        /// actual damage landed (post-resistance, post-skill-bonus).
        /// </summary>
        /// <param name="target">The damaged entity.</param>
        /// <param name="baseDamage">Pre-modifier damage roll from the
        /// spell's DamageDice.</param>
        /// <param name="elementAttribute">"Heat" / "Cold" / "Electric"
        /// / "Acid" / "Light" — feeds both the resistance lookup and
        /// the element-specific skill hooks. Empty string = untyped
        /// magic damage (still tagged Spell, no resistance).</param>
        /// <param name="attacker">The casting entity (skill source).
        /// May be null for environmental spell damage.</param>
        /// <param name="zone">The zone, for downstream event firing.</param>
        public static int ApplySpellDamage(Entity target, int baseDamage,
            string elementAttribute, Entity attacker, Zone zone)
        {
            if (target == null || baseDamage <= 0) return 0;

            // W2.5 — a spell at a person is a swing like any other
            // (see CombatSystem.PerformMeleeAttack's twin call).
            UnderTheClothEffect.Break(attacker, target);

            // Skill modifier — Spellcraft_Empower returns +1 universally,
            // PyromancySkill returns +damage when Heat hits a Burning
            // target, etc. The dispatcher iterates owned skills and sums
            // their contributions (additive across skills).
            int skillBonus = SkillEventDispatcher.GetSpellDamageModifier(
                attacker, target, elementAttribute, baseDamage);
            int finalDamage = baseDamage + skillBonus;
            if (finalDamage <= 0) return 0;

            // Build typed Damage with Spell attribute + element attribute.
            // The element string here is what AddAttribute consumes —
            // "Fire" maps to the Heat flag, "Cold" maps to Cold flag, etc.
            // (see DamageAttributeFlags aliases in Damage.cs:22-28).
            var dmg = new Damage(finalDamage);
            dmg.AddAttribute("Spell");
            if (!string.IsNullOrEmpty(elementAttribute))
                dmg.AddAttribute(elementAttribute);

            // RouteDamage, not ApplyDamage: a non-living target has no
            // Hitpoints stat, so ApplyDamage early-outs on it and a fire
            // bolt aimed at a hedgerow did nothing at all. Structural HP is
            // a separate pool with a separate death path.
            return DestructionSystem.RouteDamage(target, dmg, attacker, zone);
        }
    }
}
