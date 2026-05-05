using System;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// WS — Skill-driven on-hit effect hooks. Reads the attacker's
    /// owned skills (via <see cref="SkillsPart"/>) and applies the
    /// matching effects on a successful melee hit. Mirrors the shape
    /// of <see cref="OnHitClassEffects"/> exactly — same parameter
    /// list, same null-safety guards, same actualDamage&gt;0 gate.
    ///
    /// Order in <c>CombatSystem.PerformSingleAttack</c> (post-<c>ApplyDamage</c>,
    /// inside the <c>if (hpAfter &gt; 0)</c> block):
    /// <list type="number">
    /// <item><see cref="OnHitClassEffects.Apply"/> — universal class
    ///   tags (Bludgeoning→Stun, Cutting→Bleed, Piercing→Confuse).</item>
    /// <item><see cref="OnHitWeaponEffects.Apply"/> — per-weapon overrides
    ///   (FlamingSword→Burning, IceSword→Frozen, etc.).</item>
    /// <item><see cref="OnHitSkillEffects.Apply"/> — owner's skills
    ///   (Cudgel_Bludgeon→Stun, LongBlades_Lacerate→Bleed, etc.).</item>
    /// </list>
    ///
    /// <para>Stacks independently of the prior two: a Mace
    /// (Bludgeoning + Cudgel) wielded by an actor with
    /// <c>Cudgel_Bludgeon</c> rolls TWO stun chances on the same hit
    /// — 15% from class hooks, 35% from skill hooks. Stunned's
    /// <c>OnStack</c> extends duration, so a player can land
    /// multi-effect mauls without the second roll being wasted.</para>
    ///
    /// <para>Per-skill behavior is documented inline at the matching
    /// <c>TrySkillName</c> private method. The branch tree in
    /// <see cref="Apply"/> is intentionally kept FLAT (one if-block per
    /// skill) for grep-ability when a future content ship adds a 5th
    /// or 6th weapon-class power.</para>
    /// </summary>
    public static class OnHitSkillEffects
    {
        // ─────────────────────────────────────────────────────────────────
        // Per-skill tunables. WS.2-5 fill in. WS.1 ships the empty Apply.
        // ─────────────────────────────────────────────────────────────────

        // Cudgel_Bludgeon (WS.2; re-tuned WSP.2 to Qud-verbatim values):
        // Cudgel-class hit → 50% chance to apply Stunned for a random
        // 1-4T duration. Mirrors Qud's Cudgel_Bludgeon mechanic exactly
        // (50% base chance via "Skill Bludgeon"; Stat.Random(3,4) duration
        // — note Qud's value range is actually 3-4 turns; CoO uses 1-4
        // for player-readable variance). Stacks with the universal
        // Bludgeoning→Stun (15%, 2T) and the CudgelSkill crit (1-4T) on
        // the same hit — StunnedEffect.OnStack sums durations.
        public const int CUDGEL_BLUDGEON_CHANCE_PERCENT = 50;
        public const int CUDGEL_BLUDGEON_DURATION_MIN = 1;
        public const int CUDGEL_BLUDGEON_DURATION_MAX = 4;  // inclusive

        // Axe_Cleave (WS.3): Axe-class hit → chance to swing through
        // to one adjacent Creature for half the original damage.
        // Picks the first Creature in direction-iteration order
        // (N → NE → E → SE → S → SW → W → NW) — deterministic so
        // seeded tests can pin the target.
        public const int AXE_CLEAVE_CHANCE_PERCENT = 30;

        // LongBlades_Lacerate (WS.4): LongBlades-class hit → chance
        // to apply Bleeding with stronger damage dice ("1d3" vs the
        // class hook's "1d2"). Stacks on top of OnHitClassEffects'
        // 25% Cutting→Bleed roll (BleedingEffect's OnStack semantics
        // determine combination — duration extends, dice picks higher).
        public const int LONGBLADES_LACERATE_CHANCE_PERCENT = 35;
        public const int LONGBLADES_LACERATE_SAVE_TARGET = 15;
        public const string LONGBLADES_LACERATE_DAMAGE_DICE = "1d3";

        // ShortBlades_Jab (WS.5): Piercing-class hit → chance to apply
        // Confused for 3T. Stacks on top of the universal Piercing→Confused
        // class hook (10%, 2T) — a dagger-trained character disorients
        // harder. (Reframed from the genre-archetypal "extra off-hand
        // attack" mechanic since CoO doesn't have dual-wielding in v1.)
        public const int SHORTBLADES_JAB_CHANCE_PERCENT = 30;
        public const int SHORTBLADES_JAB_DURATION = 3;

        // ShortBlades_Bloodletter (WSP.3): Piercing-class hit → 50%
        // chance to apply Bleed with light dice. Mirrors Qud's
        // ShortBlades_Bloodletter "1d2-1" behavior — CoO uses "1d2"
        // (DiceRoller doesn't accept negative modifiers). Stacks
        // freely with the WSP.1 ShortBladesSkill crit-Bleed and the
        // ShortBlades_Jab Confused hook on the same hit.
        public const int SHORTBLADES_BLOODLETTER_CHANCE_PERCENT = 50;
        public const int SHORTBLADES_BLOODLETTER_SAVE_TARGET = 15;
        public const string SHORTBLADES_BLOODLETTER_DAMAGE_DICE = "1d2";

        // ─────────────────────────────────────────────────────────────────
        // WSP.1 — Tree-root crit-only behaviors. Each tree-root grants
        // an observable bonus when a critical hit lands with a matching
        // weapon class. Mirrors Qud's WeaponMadeCriticalHit overrides
        // (qud-decompiled-project/XRL.World.Parts.Skill/{Cudgel,Axe,
        // LongBlades,ShortBlades}.cs). Owning a tree-root for 1 SP is
        // now mechanically meaningful even before buying any powers.
        // ─────────────────────────────────────────────────────────────────

        // CudgelSkill on crit: random 1-4T Stunned (Qud uses Dazed; CoO
        // doesn't have Dazed, so map to Stunned with the same duration shape).
        public const int CUDGEL_CRIT_STUN_DURATION_MIN = 1;
        public const int CUDGEL_CRIT_STUN_DURATION_MAX = 4;  // inclusive

        // AxeSkill on crit: force-cleave at 100% chance (vs Axe_Cleave's
        // 30% gated chance). Reuses the same adjacent-Creature lookup +
        // half-damage shape — just bypasses the dice gate.

        // LongBladesSkill on crit: force Bleed with stronger dice than
        // Lacerate. Qud uses +2 penetration rolls (extra dice); CoO ports
        // as a forced Bleed since the post-damage hook signature can't
        // modify pre-damage rolls.
        public const int LONGBLADES_CRIT_BLEED_SAVE_TARGET = 15;
        public const string LONGBLADES_CRIT_BLEED_DAMAGE_DICE = "1d4";

        // ShortBladesSkill on crit: force Bleed with light dice. Qud's
        // Bleeding "1d2-1" doesn't translate cleanly (DiceRoller doesn't
        // accept negative modifiers); use "1d2" as the closest equivalent.
        public const int SHORTBLADES_CRIT_BLEED_SAVE_TARGET = 15;
        public const string SHORTBLADES_CRIT_BLEED_DAMAGE_DICE = "1d2";

        /// <summary>
        /// Apply skill-driven on-hit effects. Same contract as
        /// <see cref="OnHitClassEffects.Apply"/>: short-circuits if any
        /// argument is null, if actualDamage&lt;=0 (vetoed/fully-resisted
        /// hits don't trigger on-hit), or if the attacker has no
        /// <see cref="SkillsPart"/> (nobody to query for owned skills).
        /// </summary>
        /// <param name="damage">Damage object inspected for class attributes (Cudgel/Axe/etc.).</param>
        /// <param name="actualDamage">Real HP delta applied. 0 = vetoed/resisted; skip.</param>
        /// <param name="defender">The entity that took the hit.</param>
        /// <param name="attacker">The entity that swung — read its SkillsPart for owned skills.</param>
        /// <param name="zone">Live zone for effect application + adjacent-target lookups (Cleave).</param>
        /// <param name="rng">Deterministic RNG, shared with the attack pipeline.</param>
        public static void Apply(Damage damage, int actualDamage,
            Entity defender, Entity attacker, Zone zone, Random rng)
        {
            // Null-safety: any of these missing → silently no-op rather
            // than throw. Mirrors OnHitClassEffects.Apply contract exactly.
            if (damage == null || defender == null || attacker == null || rng == null) return;

            // Vetoed / fully-resisted hits don't trigger on-hit effects.
            // Same threshold as OnHitClassEffects: an attack that did
            // 0 actual damage (e.g. Glowmaw fully absorbing fire) shouldn't
            // also fire skill-tier effects on the same swing.
            if (actualDamage <= 0) return;

            // No SkillsPart on the attacker → no owned skills to check.
            // This is the common path for non-player attackers (creatures
            // don't have SkillsPart by default), so the early-out is fast.
            var skills = attacker.GetPart<SkillsPart>();
            if (skills == null) return;

            // Cudgel_Bludgeon (WS.2): Cudgel-attribute hit → chance to
            // Stun for 3T. Distinct from the OnHitClassEffects 15%
            // Bludgeoning→Stun roll: this fires on the Cudgel sub-class
            // attribute specifically (so a wholly-Bludgeoning weapon
            // like the basic Cudgel — yes the weapon is also named that
            // — won't trigger this branch). Mace / Warhammer / OldWorldPipe
            // carry both attributes and roll BOTH chances.
            if (skills.HasSkill(nameof(Cudgel_Bludgeon))
                && damage.HasAttribute("Cudgel"))
            {
                TryCudgelBludgeon(defender, attacker, zone, rng);
            }

            // Axe_Cleave (WS.3): Axe-attribute hit → chance to deal
            // half-damage to one adjacent Creature. Different shape from
            // a status apply: this damages a SECOND entity (the cleave
            // victim) rather than re-rolling effects on the defender.
            if (skills.HasSkill(nameof(Axe_Cleave))
                && damage.HasAttribute("Axe"))
            {
                TryAxeCleave(actualDamage, defender, attacker, zone, rng);
            }

            // LongBlades_Lacerate (WS.4): LongBlades-attribute hit →
            // chance to apply Bleeding (stronger dice than class hook).
            if (skills.HasSkill(nameof(LongBlades_Lacerate))
                && damage.HasAttribute("LongBlades"))
            {
                TryLongBladesLacerate(defender, attacker, zone, rng);
            }

            // ShortBlades_Jab (WS.5): Piercing-attribute hit → chance
            // to apply Confused. Stacks on top of OnHitClassEffects'
            // 10% Piercing→Confused class hook.
            if (skills.HasSkill(nameof(ShortBlades_Jab))
                && damage.HasAttribute("Piercing"))
            {
                TryShortBladesJab(defender, attacker, zone, rng);
            }

            // ShortBlades_Bloodletter (WSP.3): Piercing-attribute hit →
            // chance to apply Bleed (light dice). Stacks with Jab + the
            // WSP.1 crit-Bleed hook on the same swing.
            if (skills.HasSkill(nameof(ShortBlades_Bloodletter))
                && damage.HasAttribute("Piercing"))
            {
                TryShortBladesBloodletter(defender, attacker, zone, rng);
            }

            // ─────────────────────────────────────────────────────────────
            // WSP.1 — Tree-root crit behaviors. Gated on the "Critical"
            // damage attribute (set by CombatSystem when a natural-20
            // lands) + the matching weapon-class attribute + the actor
            // owning the tree-root skill. Fires AS WELL AS any matching
            // power branch above (e.g. a Mace crit by a fully-Cudgel-
            // trained character runs Cudgel_Bludgeon's 50% Stun roll AND
            // CudgelSkill's force-Stun-on-crit).
            // ─────────────────────────────────────────────────────────────

            bool isCrit = damage.HasAttribute("Critical");
            if (isCrit)
            {
                if (skills.HasSkill(nameof(CudgelSkill))
                    && damage.HasAttribute("Cudgel"))
                {
                    ApplyCudgelCrit(defender, attacker, zone, rng);
                }
                if (skills.HasSkill(nameof(AxeSkill))
                    && damage.HasAttribute("Axe"))
                {
                    ApplyAxeCritCleave(actualDamage, defender, attacker, zone);
                }
                if (skills.HasSkill(nameof(LongBladesSkill))
                    && damage.HasAttribute("LongBlades"))
                {
                    ApplyLongBladesCrit(defender, attacker, zone, rng);
                }
                if (skills.HasSkill(nameof(ShortBladesSkill))
                    && damage.HasAttribute("Piercing"))
                {
                    ApplyShortBladesCrit(defender, attacker, zone, rng);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Per-skill apply helpers
        // ─────────────────────────────────────────────────────────────────

        private static void TryCudgelBludgeon(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            int roll = rng.Next(100);
            if (roll >= CUDGEL_BLUDGEON_CHANCE_PERCENT) return;

            // Random 1-4T duration. rng.Next is exclusive on the upper
            // bound, so MAX+1 to make MAX inclusive.
            int duration = rng.Next(CUDGEL_BLUDGEON_DURATION_MIN,
                                    CUDGEL_BLUDGEON_DURATION_MAX + 1);
            var stun = new StunnedEffect(duration);
            defender.ApplyEffect(stun, attacker, zone);
        }

        // Axe_Cleave: roll the chance gate; if it passes, find the first
        // adjacent Creature (in direction-iteration order N → NE → E →
        // SE → S → SW → W → NW) that isn't the attacker themselves, and
        // damage it for max(1, actualDamage/2). Half-damage floor of 1
        // ensures even a low-damage cleave is non-trivial.
        //
        // Bails silently if zone is null (no adjacency lookup possible),
        // defender is not in the zone (position lookup returns -1), or
        // no adjacent Creature exists. Each is a normal-game path —
        // cleaving into empty space just doesn't connect, no error.
        //
        // RNG ordering: chance roll is intentionally consumed BEFORE the
        // adjacency lookup, even when there's no target. Rationale:
        //   1. Symmetry with the other 3 helpers (Bludgeon/Lacerate/Jab)
        //      which also roll-then-act — a future content-author can
        //      copy any helper's shape without reading a 4th variant.
        //   2. Models "attempted cleave that found nothing" rather than
        //      "free probe of the surroundings followed by gated roll" —
        //      the action-economy cost of the swing is uniform.
        // The asymmetry is invisible in normal play (1 RNG draw per swing
        // either way) but is documented here for save-replay determinism.
        private static void TryAxeCleave(int actualDamage, Entity defender,
            Entity attacker, Zone zone, Random rng)
        {
            if (zone == null) return;

            int roll = rng.Next(100);
            if (roll >= AXE_CLEAVE_CHANCE_PERCENT) return;

            var defPos = zone.GetEntityPosition(defender);
            if (defPos.x < 0) return;

            Entity cleaveTarget = null;
            for (int dir = 0; dir < 8 && cleaveTarget == null; dir++)
            {
                var cell = zone.GetCellInDirection(defPos.x, defPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == attacker || e == defender) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    cleaveTarget = e;
                    break;
                }
            }
            if (cleaveTarget == null) return;

            int cleaveDamage = System.Math.Max(1, actualDamage / 2);
            CombatSystem.ApplyDamage(cleaveTarget, cleaveDamage, attacker, zone);
        }

        // LongBlades_Lacerate: same shape as Cudgel_Bludgeon but applies
        // BleedingEffect with stronger dice. The rng is forwarded into
        // the BleedingEffect ctor so its tick rolls also deterministic
        // for tests (BleedingEffect's tick deals dice damage on
        // start-of-turn).
        private static void TryLongBladesLacerate(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            int roll = rng.Next(100);
            if (roll >= LONGBLADES_LACERATE_CHANCE_PERCENT) return;

            var bleed = new BleedingEffect(
                saveTarget: LONGBLADES_LACERATE_SAVE_TARGET,
                damageDice: LONGBLADES_LACERATE_DAMAGE_DICE,
                rng: rng);
            defender.ApplyEffect(bleed, attacker, zone);
        }

        // ShortBlades_Jab: same shape as Cudgel_Bludgeon but applies
        // ConfusedEffect for SHORTBLADES_JAB_DURATION turns.
        private static void TryShortBladesJab(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            int roll = rng.Next(100);
            if (roll >= SHORTBLADES_JAB_CHANCE_PERCENT) return;

            var confused = new ConfusedEffect(SHORTBLADES_JAB_DURATION);
            defender.ApplyEffect(confused, attacker, zone);
        }

        // ShortBlades_Bloodletter: same shape as LongBlades_Lacerate but
        // with light dice + a higher chance gate. The rng is forwarded
        // into BleedingEffect so its tick rolls are deterministic.
        private static void TryShortBladesBloodletter(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            int roll = rng.Next(100);
            if (roll >= SHORTBLADES_BLOODLETTER_CHANCE_PERCENT) return;

            var bleed = new BleedingEffect(
                saveTarget: SHORTBLADES_BLOODLETTER_SAVE_TARGET,
                damageDice: SHORTBLADES_BLOODLETTER_DAMAGE_DICE,
                rng: rng);
            defender.ApplyEffect(bleed, attacker, zone);
        }

        // ─────────────────────────────────────────────────────────────────
        // WSP.1 — tree-root crit helpers. No chance roll: every Crit-tagged
        // hit by an owner of the matching tree-root fires the effect.
        // ─────────────────────────────────────────────────────────────────

        // CudgelSkill on crit: random 1-4T Stunned. rng.Next(min, max+1)
        // since Random.Next is exclusive on the upper bound.
        private static void ApplyCudgelCrit(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            int duration = rng.Next(CUDGEL_CRIT_STUN_DURATION_MIN,
                                    CUDGEL_CRIT_STUN_DURATION_MAX + 1);
            var stun = new StunnedEffect(duration);
            defender.ApplyEffect(stun, attacker, zone);
        }

        // AxeSkill on crit: force-cleave at 100% chance (no rng roll).
        // Same adjacent-Creature lookup as TryAxeCleave; just executes
        // the cleave unconditionally. Bails if zone is null or there's
        // no adjacent Creature (a normal-game path: cleaving with no
        // target connected just doesn't do anything).
        private static void ApplyAxeCritCleave(int actualDamage, Entity defender,
            Entity attacker, Zone zone)
        {
            if (zone == null) return;

            var defPos = zone.GetEntityPosition(defender);
            if (defPos.x < 0) return;

            Entity cleaveTarget = null;
            for (int dir = 0; dir < 8 && cleaveTarget == null; dir++)
            {
                var cell = zone.GetCellInDirection(defPos.x, defPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == attacker || e == defender) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    cleaveTarget = e;
                    break;
                }
            }
            if (cleaveTarget == null) return;

            int cleaveDamage = System.Math.Max(1, actualDamage / 2);
            CombatSystem.ApplyDamage(cleaveTarget, cleaveDamage, attacker, zone);
        }

        // LongBladesSkill on crit: force Bleeding with stronger dice
        // ("1d4" vs Lacerate's "1d3"). No chance roll; every crit
        // applies Bleed.
        private static void ApplyLongBladesCrit(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            var bleed = new BleedingEffect(
                saveTarget: LONGBLADES_CRIT_BLEED_SAVE_TARGET,
                damageDice: LONGBLADES_CRIT_BLEED_DAMAGE_DICE,
                rng: rng);
            defender.ApplyEffect(bleed, attacker, zone);
        }

        // ShortBladesSkill on crit: force Bleeding with light dice
        // ("1d2"). No chance roll; every Piercing crit applies Bleed.
        private static void ApplyShortBladesCrit(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            var bleed = new BleedingEffect(
                saveTarget: SHORTBLADES_CRIT_BLEED_SAVE_TARGET,
                damageDice: SHORTBLADES_CRIT_BLEED_DAMAGE_DICE,
                rng: rng);
            defender.ApplyEffect(bleed, attacker, zone);
        }
    }
}
