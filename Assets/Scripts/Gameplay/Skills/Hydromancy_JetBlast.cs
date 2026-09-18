using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Hydromancy active: a pressurised blast of water across a short
    /// cone, soaking everything it touches and shoving it back.
    ///
    /// <para>SPELLCRAFT SM5 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.3) —
    /// the request's jet blast, verbatim: <i>"a jet blast skill (water,
    /// soaks enemy which sets it up to be electrocuted)"</i>. This is the
    /// cast that makes the soak-then-shock loop self-serve.</para>
    ///
    /// <para><b>What the soaking buys.</b> Moisture above 0.2 makes
    /// <see cref="ElectrifiedEffect"/> double its charge and last a turn
    /// longer; above 0.35 it suppresses ignition
    /// (<see cref="PyroIgnition"/>). One cast therefore sets a whole
    /// clump up for lightning AND shields it from fire — the elemental
    /// grammar in a single action.</para>
    ///
    /// <para>Damage is deliberately minimal. Water is not a way to kill
    /// things; it is a way to make other things lethal.</para>
    /// </summary>
    public class Hydromancy_JetBlast : SpellSkillPart
    {
        public override string Name => nameof(Hydromancy_JetBlast);

        public const int COOLDOWN = 20;
        public const int BLAST_LENGTH = 2;

        /// <summary>Token damage. The soaking is the point.</summary>
        public const int BLAST_DAMAGE = 2;

        /// <summary>Comfortably above BOTH interaction thresholds (0.2
        /// for charge doubling, 0.35 for fire suppression) with room to
        /// evaporate for several turns before either lapses.</summary>
        public const float BLAST_MOISTURE = 0.8f;

        public const int BLAST_PUSH_CELLS = 1;

        /// <summary>Liquid written to the ground. Reuses the shipped
        /// `water` LiquidDefinition rather than inventing an id.</summary>
        public const string GroundLiquid = "water";

        /// <summary>Turns the puddle lasts. Long enough to still be
        /// there when a lightning follow-up comes off cooldown, which is
        /// the entire tactical point of leaving it.</summary>
        public const int GroundTurns = 6;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Jet Blast",
                Command = "CommandJetBlast",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = BLAST_LENGTH,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            List<Entity> targets = SpellTargeting.GetCreaturesInCone(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, BLAST_LENGTH);

            if (targets.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                // PALIMPSEST P2 — a miss is not a nothing. The water
                // still lands, which is the whole point of writing to
                // the world instead of only to creatures.
                WetTheGround(ctx, actor, actorPos.x, actorPos.y, dx, dy);
                MessageLog.Add(actor.GetDisplayName() + "'s jet blast splashes across bare ground.");
                return true; // the ground got wet — a real cast
            }

            float moisture = HydromancySkill.ApplyMoistureBonus(actor, BLAST_MOISTURE);
            int soaked = 0, survivors = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                SpellFxCapture.TargetInAffectedCells(ctx.Zone, target);

                var dmg = new Damage(BLAST_DAMAGE);
                // NOTE: "Water" is a descriptive tag only — it maps to
                // DamageAttributeFlags.None (Damage.cs:144-173), so this
                // damage is untyped and no elemental resistance reduces
                // it. That is intended: water is not an element you
                // resist here, it is a setup. Pinned by
                // HydromancyJetBlastTests.WaterDamage_IsUntyped.
                dmg.AddAttribute("Water");
                CombatSystem.ApplyDamage(target, dmg, actor, ctx.Zone);

                if (target.GetStatValue("Hitpoints") <= 0) continue;
                survivors++;
                target.ApplyEffect(new WetEffect(moisture), actor, ctx.Zone);
                soaked++;
            }

            // Furthest-first, the ordering rule SM3's audit established.
            int shoved = 0;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target.GetStatValue("Hitpoints") <= 0) continue;
                if (SkillCombatHelpers.TryPush(actor, target, ctx.Zone, BLAST_PUSH_CELLS))
                    shoved++;
            }

            // PALIMPSEST P2 — water lands where the target ENDS UP.
            // Read each position back rather than computing it: the push
            // above may have been refused for some of them.
            for (int i = 0; i < targets.Count; i++)
            {
                var pos = ctx.Zone.GetEntityPosition(targets[i]);
                if (pos.x < 0) continue;
                ZoneTileStateSystem.WriteCoating(ctx.Zone, pos.x, pos.y,
                    GroundLiquid, GroundTurns, actor, Name);
            }
            WetTheGround(ctx, actor, actorPos.x, actorPos.y, dx, dy);

            if (shoved == 0 && survivors > 0)
                EmitSkillRejectedDiag(ctx, "push_blocked");
            else if (survivors == 0)
                EmitSkillRejectedDiag(ctx, "all_targets_died");

            // P3 — resolve what the writes just created, now rather than
            // at end of turn: lightning into a puddle must electrify it
            // on the cast, or the rule reads as a bug.
            ZoneTileStateSystem.ResolveAfterAbility(ctx.Zone, actor);

            MessageLog.Add(actor.GetDisplayName() + "'s jet blast drenches "
                + soaked + " target" + (soaked == 1 ? "" : "s") + "!");
        
            return true;
        }

        /// <summary>Lays water along the cone's centre line. A
        /// simplification of the cone's full widening geometry — the
        /// spray soaks what it passes over, not the entire fan — kept
        /// deliberately cheap because P2 is about proving marks matter,
        /// not about puddle shape.</summary>
        private void WetTheGround(SkillEventContext ctx, Entity actor,
            int fromX, int fromY, int dx, int dy)
        {
            var cells = SkillLine.CollectCells(
                ctx.Zone, actor, fromX, fromY, dx, dy, BLAST_LENGTH);
            for (int i = 0; i < cells.Count; i++)
                ZoneTileStateSystem.WriteCoating(ctx.Zone, cells[i].X, cells[i].Y,
                    GroundLiquid, GroundTurns, actor, Name);
        }
    }
}
