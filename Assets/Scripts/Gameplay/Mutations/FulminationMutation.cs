using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// PALIMPSEST P5 — the rite that writes and walks away.
    ///
    /// <para>Every rite before this one resolved its own payoff: read
    /// the target's statuses, compute a multiplier, apply the damage.
    /// Fulmination does almost nothing. It consumes the target's Wet and
    /// then <b>writes Charge 2 onto the tile</b> — and stops.</para>
    ///
    /// <para>What happens next is not its business. If the tile holds
    /// water, <c>electrify_water</c> fires. If the water runs, the
    /// charge runs with it. If it is standing on a metal grate, the
    /// charge crosses the room. If the floor is dry stone, nothing
    /// happens at all and that is a correct outcome. <b>The same cast
    /// produces different results in different rooms, and the rite
    /// contains no knowledge of why.</b></para>
    ///
    /// <para>That is the architectural claim of this whole prototype —
    /// spells emit simple events, the world owns the consequences — made
    /// concrete enough to test. The POC question for P5 is exactly:
    /// does one rite behave differently in two rooms without knowing
    /// about either?</para>
    /// </summary>
    public class FulminationMutation : BaseMutation
    {
        public const string COMMAND = "CommandFulmination";
        public const int RANGE = 5;
        public const int COOLDOWN = 25;
        public const string ELEMENT = "Electric";

        /// <summary>Only one slot: this rite is a detonator for a single
        /// setup, not a vacuum for every status on the target.</summary>
        public const int SLOTS = 1;

        /// <summary>Direct damage is deliberately small. What the rite
        /// buys you is the charge in the ground, not the hit.</summary>
        public const int BASE_DAMAGE = 3;

        /// <summary>Charge written to the tile. 2 so it survives being
        /// spent by one reaction and still has something left to
        /// propagate.</summary>
        public const int WRITTEN_CHARGE = 2;

        public override string Name => "Fulmination";
        public override string MutationType => "Rite";
        public override string DisplayName => "Fulmination";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName, COMMAND, "Rites",
                AbilityTargetingMode.DirectionLine, RANGE);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != COMMAND) return true;
            var zone = e.GetParameter<Zone>("Zone");
            int dx = e.GetParameter<int>("DirectionX");
            int dy = e.GetParameter<int>("DirectionY");
            if (!Cast(zone, dx, dy)) return true;
            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, int dx, int dy)
        {
            var caster = ParentEntity;
            if (caster == null || zone == null) return false;
            if (dx == 0 && dy == 0)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_direction" });
                return false;
            }

            var pos = zone.GetEntityPosition(caster);
            if (pos.x < 0) return false;

            var target = RiteTargeting.FirstCreatureInLine(
                zone, caster, pos.x, pos.y, dx, dy, RANGE);
            if (target == null)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_target" });
                MessageLog.Add(caster.GetDisplayName() + "'s fulmination finds no mark.");
                return false;
            }

            var grimoire = GrimoireInk.FindInked(caster);
            if (grimoire == null)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_ink" });
                MessageLog.Add("The pages are dry — no ink to spend.");
                return false;
            }
            grimoire.TrySpend();

            // Spend the target's own water, if it has any.
            var res = ResonanceSystem.Spend(target, ELEMENT, SLOTS, caster, zone);

            var dmg = new Damage(BASE_DAMAGE + (int)Math.Round(BASE_DAMAGE * (res.Multiplier - 1f)));
            dmg.AddAttribute("Electric");
            dmg.AddAttribute("Lightning");
            CombatSystem.ApplyDamage(target, dmg, caster, zone);

            // ── And now the whole point ──────────────────────────
            // Write charge to the ground and stop. Whether that means
            // electrified water, a charged grate carrying it across the
            // room, or nothing at all is decided by the tile layer, not
            // here. This rite has no idea what a puddle is.
            var tpos = zone.GetEntityPosition(target);
            if (tpos.x >= 0)
            {
                ZoneTileStateSystem.AddCharge(
                    zone, tpos.x, tpos.y, WRITTEN_CHARGE, caster, Name);
                ZoneTileStateSystem.ResolveAfterAbility(zone, caster);
            }

            Diag.Record("spell", "RiteCast", caster, target,
                new
                {
                    rite = Name, element = ELEMENT,
                    statusesConsumed = res.Consumed.Count,
                    wroteCharge = WRITTEN_CHARGE,
                    inkLeft = grimoire.Charges,
                });

            MessageLog.Add(caster.GetDisplayName() + "'s fulmination cracks into "
                + target.GetDisplayName() + ", and the charge goes looking for somewhere to run.");

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
