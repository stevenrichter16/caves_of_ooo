using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM8 — Storm Anvil's twin, and the demonstration that a
    /// rite's payoff need not be damage.
    ///
    /// <para>It resonates on the same Electric table and spends the same
    /// statuses, but converts every mark it consumes into
    /// <b>no-save Paralysis</b> instead of a damage multiplier. Same
    /// setup, entirely different answer: Storm Anvil removes a pack,
    /// Hanging Bolt removes a single elite from the fight while you deal
    /// with everything else.</para>
    ///
    /// <para>That is the point of building resonance as a shared system:
    /// two rites can read the identical status table and disagree
    /// completely about what it is FOR.</para>
    /// </summary>
    public class HangingBoltMutation : BaseMutation
    {
        public const string COMMAND = "CommandHangingBolt";
        public const int RANGE = 6;
        public const int COOLDOWN = 30;
        public const string ELEMENT = "Electric";
        public const int SLOTS = 2;

        /// <summary>Token damage — this rite is control, not a kill.</summary>
        public const int BASE_DAMAGE = 2;

        /// <summary>Paralysis turns granted per status consumed. Zero
        /// consumed means zero paralysis: cast cold, this rite does
        /// almost nothing, which is the same discipline Storm Anvil
        /// follows.</summary>
        public const int PARALYSIS_PER_MARK = 2;

        public override string Name => "HangingBolt";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Hanging Bolt";

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
            if (caster == null) return false;
            if (zone == null || (dx == 0 && dy == 0))
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = zone == null ? "no_zone" : "no_direction" });
                return false;
            }

            var pos = zone.GetEntityPosition(caster);
            if (pos.x < 0) return false;

            Entity target = RiteTargeting.FirstCreatureInLine(zone, caster, pos.x, pos.y, dx, dy, RANGE);
            if (target == null)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_target" });
                MessageLog.Add(caster.GetDisplayName() + " finds nothing to pin.");
                return false;
            }

            var grimoire = StormAnvilMutation.FindInkedGrimoire(caster);
            if (grimoire == null)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_ink" });
                MessageLog.Add("The pages are dry — no ink to spend.");
                return false;
            }
            grimoire.TrySpend();

            var res = ResonanceSystem.Spend(target, ELEMENT, SLOTS, caster, zone);

            var dmg = new Damage(BASE_DAMAGE);
            dmg.AddAttribute("Electric");
            dmg.AddAttribute("Lightning");
            CombatSystem.ApplyDamage(target, dmg, caster, zone);

            int turns = res.Consumed.Count * PARALYSIS_PER_MARK;
            if (target.GetStatValue("Hitpoints", 0) > 0 && turns > 0)
                target.ApplyEffect(new ParalyzedEffect(turns), caster, zone);

            Diag.Record("spell", "RiteCast", caster, target,
                new
                {
                    rite = Name, element = ELEMENT,
                    statusesConsumed = res.Consumed.Count,
                    paralysisTurns = turns,
                    inkLeft = grimoire.Charges,
                });

            MessageLog.Add(turns > 0
                ? caster.GetDisplayName() + " pins " + target.GetDisplayName()
                  + " under a hanging bolt for " + turns + " turns!"
                : caster.GetDisplayName() + "'s hanging bolt fizzles — "
                  + target.GetDisplayName() + " carried nothing to spend.");

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
