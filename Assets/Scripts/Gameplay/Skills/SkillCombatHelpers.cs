using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// WSP3.3 — Shared helper utilities for skill combat-event handlers.
    /// Each helper is a single-responsibility static method that multiple
    /// skill classes can call from their event-override implementations
    /// without forcing inheritance hierarchies.
    ///
    /// <para>The cleave-target lookup lives here because both
    /// <see cref="Axe_Cleave"/> (gated by 30% chance) and
    /// <see cref="AxeSkill"/> (force-cleave on crit) need identical
    /// adjacent-Creature-pickup semantics. Extracting kept the previous
    /// <c>OnHitSkillEffects.cs</c> 22-line cleave loop from being
    /// duplicated between the two skills' virtual overrides — same
    /// rationale as the WSP.4b cold-eye-fix extraction.</para>
    /// </summary>
    public static class SkillCombatHelpers
    {
        /// <summary>
        /// Finds the first Creature entity adjacent to defender (in
        /// direction-iteration order N → NE → E → SE → S → SW → W → NW)
        /// that isn't the attacker themselves. Null if none found or
        /// if zone/position lookup fails.
        ///
        /// <para>Direction-iteration order is deterministic so seeded
        /// tests can pin the cleave victim. The first-found-Creature
        /// wins; no random target selection.</para>
        /// </summary>
        public static Entity FindAdjacentCleaveTarget(Entity defender,
            Entity attacker, Zone zone)
        {
            if (zone == null) return null;
            var defPos = zone.GetEntityPosition(defender);
            if (defPos.x < 0) return null;

            for (int dir = 0; dir < 8; dir++)
            {
                var cell = zone.GetCellInDirection(defPos.x, defPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == attacker || e == defender) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    return e;
                }
            }
            return null;
        }

        /// <summary>
        /// Convenience wrapper: find an adjacent cleave target and, if
        /// one exists, deal max(1, actualDamage/2) damage to it. Returns
        /// true if cleave landed on a target. Used by Axe_Cleave (gated
        /// by chance) and AxeSkill (force-cleave on crit).
        /// </summary>
        public static bool ExecuteCleave(int actualDamage, Entity defender,
            Entity attacker, Zone zone)
        {
            var target = FindAdjacentCleaveTarget(defender, attacker, zone);
            if (target == null) return false;

            int cleaveDamage = System.Math.Max(1, actualDamage / 2);
            CombatSystem.ApplyDamage(target, cleaveDamage, attacker, zone);
            return true;
        }

        /// <summary>
        /// Returns the first equipped <see cref="MeleeWeaponPart"/> on
        /// the actor whose Attributes string contains the given
        /// substring (e.g. "Cudgel", "Axe", "Piercing"). Null if none
        /// found. Used by active-ability skills (Cudgel_Conk,
        /// Axe_Berserk) to gate command invocation on the right weapon
        /// being wielded. (ShortBlades_Hobble used to be on this list,
        /// but it shipped as an on-hit passive only — no `OnCommand`
        /// override; WSP5.1 cleanup of cold-eye borderline (a).)
        /// </summary>
        public static MeleeWeaponPart FindEquippedWeaponOfClass(
            Entity actor, string requiredAttribute)
        {
            if (actor == null || string.IsNullOrEmpty(requiredAttribute)) return null;
            var body = actor.GetPart<Body>();
            if (body == null) return null;

            MeleeWeaponPart found = null;
            body.ForeachEquippedObject((item, bp) =>
            {
                if (found != null || item == null) return;
                var w = item.GetPart<MeleeWeaponPart>();
                if (w != null && !string.IsNullOrEmpty(w.Attributes)
                    && w.Attributes.Contains(requiredAttribute))
                {
                    found = w;
                }
            });
            return found;
        }

        /// <summary>
        /// Docs/COMBAT-AUDIT-BUGFIX-PLAN-2026-07.md SM8/B2. Deals guaranteed
        /// (no to-hit roll) damage exactly like <c>Cudgel_Slam</c>/
        /// <c>Cudgel_GroundPound</c>'s own custom damage formulas, but routes
        /// the landed hit through the same on-hit dispatch chain
        /// <see cref="CombatSystem.PerformSingleAttack"/> uses for a normal
        /// weapon swing -- previously both skills called the raw
        /// <c>CombatSystem.ApplyDamage(Entity,int,Entity,Zone)</c> overload
        /// directly, which wraps the amount in a zero-attribute
        /// <see cref="Damage"/> and skips <see cref="OnHitClassEffects"/>,
        /// <see cref="OnHitWeaponEffects"/>, <see cref="OnHitGasEmit"/>,
        /// <see cref="ItemEnhancementDispatch"/>, and skill
        /// <c>AttackerAfterAttack</c> dispatch entirely.
        ///
        /// <para>Deliberately does NOT replace either skill's custom
        /// guaranteed-hit/scaled-damage mechanic with a
        /// <see cref="CombatSystem.PerformSingleAttack"/> call (that would
        /// add a to-hit roll and normal weapon-dice damage, changing the
        /// core mechanic both skills are designed around — Slam's
        /// wall-hit-count-scaled damage, GroundPound's reduced-damage/
        /// AOE-knockback trade-off per its own doc-comment). This helper
        /// only threads the missing on-hit pipeline through the EXISTING
        /// guaranteed-hit path.</para>
        ///
        /// <para>Weapon-mod dispatchers (class/weapon/gas/enhancement) fire
        /// regardless of survival, mirroring SM7's fix to the normal melee
        /// pipeline. Skill dispatch (<c>AttackerAfterAttack</c>) stays
        /// gated on <c>hpAfter &gt; 0</c>, matching the same split.
        /// <c>WeaponMadeCriticalHit</c> and dismemberment are intentionally
        /// not wired here — neither skill has a crit-roll or hit-location
        /// concept (no to-hit roll exists in this guaranteed-hit design).
        /// </para>
        /// </summary>
        /// <returns>The actual post-resistance HP delta (0 if the hit was
        /// vetoed or fully resisted).</returns>
        public static int DealGuaranteedHitDamage(
            Entity attacker, Entity target, MeleeWeaponPart weapon,
            int rawDamage, Zone zone, System.Random rng)
        {
            if (target == null || rawDamage <= 0) return 0;

            var damage = new Damage(rawDamage);
            damage.AddAttribute("Melee");
            if (weapon != null && !string.IsNullOrEmpty(weapon.Attributes))
                damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints", 0);
            CombatSystem.ApplyDamage(target, damage, attacker, zone);
            int hpAfter = target.GetStatValue("Hitpoints", 0);
            int actualDamage = System.Math.Max(0, hpBefore - hpAfter);

            OnHitClassEffects.Apply(damage, actualDamage, target, attacker, zone, rng);
            OnHitWeaponEffects.Apply(weapon, damage, actualDamage, target, attacker, zone, rng);
            OnHitGasEmit.Apply(weapon, damage, actualDamage, target, attacker, zone, rng);
            ItemEnhancementDispatch.DispatchOnHit(
                weapon?.ParentEntity, target, attacker, damage, actualDamage, zone, rng);

            if (hpAfter > 0)
            {
                var hitCtx = new SkillEventContext
                {
                    Attacker = attacker, Defender = target,
                    Weapon = weapon, WeaponEntity = weapon?.ParentEntity,
                    Damage = damage, ActualDamage = actualDamage,
                    Zone = zone, Rng = rng,
                };
                SkillEventDispatcher.AttackerAfterAttack(attacker, hitCtx);
            }

            return actualDamage;
        }

        // ════════════════════════════════════════════════════════
        // Knockback
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Shoves <paramref name="target"/> directly away from
        /// <paramref name="actor"/>, up to <paramref name="cells"/>
        /// cells, and reports whether it actually moved.
        ///
        /// <para>SPELLCRAFT SM1 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §5,
        /// P1). Extracted verbatim in behaviour from
        /// <see cref="Cudgel_GroundPound"/>, which was the codebase's
        /// only knockback. The plan's Ground Surge, Jet Blast and
        /// Undertow all push, and duplicating the destination guards
        /// four times is how guards drift apart.</para>
        ///
        /// <para><b>Contract.</b> The push direction is resolved from
        /// the target's CURRENT position each step — a multi-target
        /// power may already have moved it. A step is refused by a solid
        /// cell, by another <i>creature</i> (items on the floor never
        /// block: after the loot overhaul the ground is covered in
        /// them), or by the zone edge. A multi-cell push stops at the
        /// first refusal and KEEPS the ground it gained, returning true
        /// — partial movement is still movement. Returns false only when
        /// nothing moved at all, which callers use to distinguish
        /// "shoved into open ground" from "slammed against a wall".</para>
        /// </summary>
        /// <param name="cells">Cells to shove. Zero or negative is a
        /// no-op returning false, so a caller may pass a computed
        /// distance without pre-checking it.</param>
        public static bool TryPush(Entity actor, Entity target, Zone zone, int cells = 1)
        {
            if (actor == null || target == null || zone == null) return false;
            if (cells <= 0) return false;

            var actorPos = zone.GetEntityPosition(actor);
            var targetPos = zone.GetEntityPosition(target);
            if (actorPos.x < 0 || targetPos.x < 0) return false;

            int dx = System.Math.Sign(targetPos.x - actorPos.x);
            int dy = System.Math.Sign(targetPos.y - actorPos.y);
            // Coincident actor and target give no direction to push
            // along. Decline rather than pick one arbitrarily.
            if (dx == 0 && dy == 0) return false;

            return DragAlong(target, zone, dx, dy, cells, stopBefore: null);
        }

        /// <summary>
        /// Drags <paramref name="target"/> TOWARD <paramref name="actor"/>,
        /// up to <paramref name="cells"/> cells, stopping adjacent — never
        /// onto the puller.
        ///
        /// <para>SPELLCRAFT SM5. The mirror of <see cref="TryPush"/>, for
        /// Hydromancy's Undertow: yank a back-line caster out of its
        /// rank and into your melee. Same guards, same movement
        /// pipeline; only the direction and the stop condition
        /// differ.</para>
        ///
        /// <para>The stop-adjacent rule is the one thing a pull needs
        /// that a push does not. Without it a long drag ends with the
        /// target standing in the caster's own cell, which no other
        /// movement in the game permits.</para>
        /// </summary>
        public static bool TryPull(Entity actor, Entity target, Zone zone, int cells = 1)
        {
            if (actor == null || target == null || zone == null) return false;
            if (cells <= 0) return false;

            var actorPos = zone.GetEntityPosition(actor);
            var targetPos = zone.GetEntityPosition(target);
            if (actorPos.x < 0 || targetPos.x < 0) return false;

            // Toward the actor — the sign is inverted relative to TryPush.
            int dx = System.Math.Sign(actorPos.x - targetPos.x);
            int dy = System.Math.Sign(actorPos.y - targetPos.y);
            if (dx == 0 && dy == 0) return false;

            return DragAlong(target, zone, dx, dy, cells,
                stopBefore: new Point(actorPos.x, actorPos.y));
        }

        /// <summary>
        /// The shared step loop behind <see cref="TryPush"/> and
        /// <see cref="TryPull"/>: walk the target one cell at a time
        /// along (dx, dy), refusing any step into stone, another
        /// creature, the zone edge, or <paramref name="stopBefore"/>.
        /// Stops at the first refusal and keeps the ground gained.
        ///
        /// <para>Extracted so the two directions cannot drift apart —
        /// the destination guards are exactly the thing that must stay
        /// identical, and SM3's audit showed how quickly a duplicated
        /// guard becomes a different guard.</para>
        ///
        /// <para>Movement goes through
        /// <see cref="MovementSystem.ForceMoveTo"/>, not a raw
        /// <c>Zone.MoveEntity</c>: a shove or a drag IS a move, so the
        /// target must fire AfterMove, run cell-entry (a creature
        /// dragged into a pool actually gets wet — the setup this whole
        /// feature is built around) and mark its cells dirty. ForceMoveTo
        /// rather than TryMoveTo because BeforeMove asks "may this
        /// creature move ITSELF", which Stunned answers no — routing
        /// through it let GroundPound's own stun cancel GroundPound's own
        /// knockback.</para>
        /// </summary>
        private static bool DragAlong(
            Entity target, Zone zone, int dx, int dy, int cells, Point? stopBefore)
        {
            bool movedAny = false;
            for (int step = 0; step < cells; step++)
            {
                // Re-resolve every step: the target is moving.
                var from = zone.GetEntityPosition(target);
                if (from.x < 0) break;

                int nx = from.x + dx;
                int ny = from.y + dy;

                // A pull must never deposit the target on the puller.
                //
                // For a CREATURE puller the occupancy guard below would
                // already refuse this step, so mutation-testing showed
                // this line was unreachable in every existing caller.
                // It is kept because the invariant must not depend on
                // the puller happening to be tagged Creature: a future
                // whirlpool fixture or tentacle prop would otherwise
                // drag its victim inside itself. Pinned by
                // SkillPushHelperTests.TryPull_StopsAdjacentEvenWhenThePullerIsNotACreature.
                if (stopBefore.HasValue
                    && stopBefore.Value.X == nx && stopBefore.Value.Y == ny) break;

                var dest = zone.GetCell(nx, ny);
                if (dest == null) break;                       // zone edge
                if (dest.IsSolid()) break;                     // wall
                if (CellHasOtherCreature(dest, target)) break; // occupied

                if (!MovementSystem.ForceMoveTo(target, zone, nx, ny)) break;
                movedAny = true;
            }

            return movedAny;
        }

        /// <summary>
        /// True when <paramref name="cell"/> holds a Creature other than
        /// <paramref name="exclude"/>. Non-creature occupants (dropped
        /// loot, corpses, scenery without the Solid tag) never block a
        /// shove.
        /// </summary>
        private static bool CellHasOtherCreature(Cell cell, Entity exclude)
        {
            if (cell == null) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (e == null || e == exclude) continue;
                if (e.Tags.ContainsKey("Creature")) return true;
            }
            return false;
        }
    }
}
