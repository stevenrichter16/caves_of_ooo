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
            string elementAttribute, Entity attacker, Zone zone, params string[] additionalAttributes)
            => ApplyDirectDamage(target, baseDamage, elementAttribute, attacker, zone,
                elementAttribute, additionalAttributes);

        /// <summary>Rites have a resonance school distinct from their damage
        /// attributes. Preserve their exact typing while applying the same bonuses.</summary>
        public static int ApplySpellDamageWithAttributes(Entity target, int baseDamage,
            string modifierElement, Entity attacker, Zone zone, string[] damageAttributes)
            => ApplyDirectDamage(target, baseDamage, modifierElement, attacker, zone,
                null, damageAttributes);

        private static int ApplyDirectDamage(Entity target, int baseDamage,
            string modifierElement, Entity attacker, Zone zone,
            string primaryAttribute, string[] additionalAttributes)
        {
            if (target == null || baseDamage <= 0) return 0;

            // An attempted direct hit spends a charge even at full resistance;
            // a stale/dead owner cannot spend it. Ground/status/retort damage
            // never passes through this helper.
            bool standing = target.GetStatValue("Hitpoints", 0) > 0
                || (!target.HasTag("Creature") && target.GetPart<DestructiblePart>()?.HP > 0);
            if (standing && _cast != null && _cast.Actor == attacker) _cast.Attempt(modifierElement);

            // W2.5 — a spell at a person is a swing like any other
            // (see CombatSystem.PerformMeleeAttack's twin call).
            UnderTheClothEffect.Break(attacker, target);

            // Skill modifier — Spellcraft_Empower returns +2 universally,
            // PyromancySkill returns +damage when Heat hits a Burning
            // target, etc. The dispatcher iterates owned skills and sums
            // their contributions (additive across skills).
            int skillBonus = SkillEventDispatcher.GetSpellDamageModifier(
                attacker, target, modifierElement, baseDamage);
            int finalDamage = baseDamage + skillBonus;
            if (finalDamage <= 0) return 0;

            // Build typed Damage with Spell attribute + element attribute.
            // The element string here is what AddAttribute consumes —
            // "Fire" maps to the Heat flag, "Cold" maps to Cold flag, etc.
            // (see DamageAttributeFlags aliases in Damage.cs:22-28).
            var dmg = new Damage(finalDamage);
            dmg.AddAttribute("Spell");
            if (!string.IsNullOrEmpty(primaryAttribute)) dmg.AddAttribute(primaryAttribute);
            if (additionalAttributes != null)
                for (int i = 0; i < additionalAttributes.Length; i++)
                    dmg.AddAttribute(additionalAttributes[i]);

            // RouteDamage, not ApplyDamage: a non-living target has no
            // Hitpoints stat, so ApplyDamage early-outs on it and a fire
            // bolt aimed at a hedgerow did nothing at all. Structural HP is
            // a separate pool with a separate death path.
            int landed = DestructionSystem.RouteDamage(target, dmg, attacker, zone);
            if (landed > 0 && _cast != null && _cast.Actor == attacker)
                _cast.RecordDirectTarget(target, zone);
            return landed;
        }

        [System.ThreadStatic] private static Cast _cast;
        internal static Cast BeginCast(Entity actor) => new Cast(actor);

        internal static int LeyTapBonus(Entity actor, int liveBonus)
            => _cast != null && _cast.Actor == actor ? _cast.LeyBonus : liveBonus;

        internal static bool HeartFlameReady(Entity actor, bool liveReady)
            => _cast != null && _cast.Actor == actor ? _cast.HeartReady : liveReady;

        /// <summary>One actual spell resolution, not a damage preview. The
        /// snapshot applies to every direct target; successful completion spends
        /// at most one charge per buff. Dispose restores nested/failed callers.</summary>
        internal sealed class Cast : System.IDisposable
        {
            internal readonly Entity Actor;
            internal readonly int LeyBonus;
            internal readonly bool HeartReady;
            private readonly Cast _previous;
            private readonly LeyTapEffect _ley;
            private readonly HeartFlameEffect _heart;
            private bool _usedLey, _usedHeart, _committed;
            private System.Collections.Generic.List<(Entity target, Zone zone)> _damagedTargets;

            internal Cast(Entity actor)
            {
                Actor = actor; _previous = _cast;
                _ley = actor?.GetEffect<LeyTapEffect>();
                _heart = actor?.GetEffect<HeartFlameEffect>();
                LeyBonus = _ley != null && _ley.Duration > 0 ? _ley.BonusDamage : 0;
                HeartReady = _heart != null && _heart.Duration > 0 && _heart.ChargesRemaining > 0;
                _cast = this;
            }

            internal void Attempt(string element)
            {
                if (LeyBonus > 0) _usedLey = true;
                if (HeartReady && (element == "Heat" || element == "Fire")) _usedHeart = true;
            }

            internal void RecordDirectTarget(Entity target, Zone zone)
            {
                // Only a player with a party needs this per-cast receipt. Keep
                // resolver order; each follower chooses its first eligible
                // witnessed survivor when the spell actually completes.
                if (Actor?.HasTag("Player") != true || Actor.GetPart<BrainPart>()?.PartyMembers.Count is not > 0
                    || target?.HasTag("Creature") != true) return;
                _damagedTargets ??= new System.Collections.Generic.List<(Entity, Zone)>();
                for (int i = 0; i < _damagedTargets.Count; i++)
                    if (_damagedTargets[i].target == target && _damagedTargets[i].zone == zone) return;
                _damagedTargets.Add((target, zone));
            }

            internal void Commit()
            {
                if (_committed) return;
                _committed = true;
                // Never remove a replacement effect installed by a callback.
                if (_usedLey && Actor?.GetEffect<LeyTapEffect>() == _ley)
                    Actor.RemoveEffect<LeyTapEffect>();
                if (_usedHeart && Actor?.GetEffect<HeartFlameEffect>() == _heart)
                {
                    _heart.ChargesRemaining = System.Math.Max(0, _heart.ChargesRemaining - 1);
                    if (_heart.ChargesRemaining == 0) Actor.RemoveEffect<HeartFlameEffect>();
                }
                if (_damagedTargets != null)
                    foreach (var hit in _damagedTargets)
                        CompanionCombat.AfterPlayerDirectDamage(Actor, hit.target, hit.zone, 1);
            }

            public void Dispose() { _cast = _previous; }
        }
    }
}
