using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM7 — the first RITE, and the proof that resonance
    /// works end to end.
    ///
    /// <para><b>What makes this a rite rather than a skill</b>
    /// (Docs/SPELLCRAFT-STATUS-SYNERGY.md §4):</para>
    /// <list type="number">
    /// <item><b>It CONSUMES statuses.</b> Skills apply; only a rite
    /// spends. Storm Anvil eats Wet, Electrified and Frozen off its
    /// targets and converts them into damage and riders.</item>
    /// <item><b>It costs INK.</b> A charge from the grimoire in your
    /// pack, not merely a cooldown.</item>
    /// <item><b>Its payoff is super-linear.</b> Two statuses are worth
    /// more than twice one.</item>
    /// </list>
    ///
    /// <para><b>Cast cold, it is deliberately weak.</b>
    /// <see cref="BASE_DAMAGE"/> against a target with nothing on it is
    /// worse than a skill of the same cooldown — and that is the point.
    /// If a rite were a good opener there would be no reason to prime
    /// first, and the whole prime-then-detonate rhythm would collapse
    /// into spamming the rite. A rite is a finisher; the damage lives in
    /// what you spend, not in the spell.</para>
    /// </summary>
    public class StormAnvilMutation : BaseMutation
    {
        public const string COMMAND = "CommandStormAnvil";
        public const int RADIUS = 2;
        public const int COOLDOWN = 25;

        /// <summary>The element this rite resonates on. Drives which
        /// Resonance.json table applies.</summary>
        public const string ELEMENT = "Electric";

        /// <summary>Statuses this rite may spend per target. Channelling
        /// (SM9) will raise this.</summary>
        public const int SLOTS = 2;

        /// <summary>Deliberately low. A rite that hits hard with nothing
        /// consumed would remove every reason to prime.</summary>
        public const int BASE_DAMAGE = 4;

        /// <summary>Turns of no-save stun granted by the "Stun" rider.</summary>
        public const int STUN_RIDER_TURNS = 3;

        public override string Name => "StormAnvil";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Storm Anvil";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName, COMMAND, "Rites",
                AbilityTargetingMode.SelfCentered, RADIUS);
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

            Zone zone = e.GetParameter<Zone>("Zone");
            Cell sourceCell = e.GetParameter<Cell>("SourceCell");
            if (!Cast(zone, sourceCell)) return true;

            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell)
        {
            var caster = ParentEntity;
            if (caster == null) return false;
            if (zone == null || sourceCell == null)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_zone_or_cell" });
                return false;
            }

            var targets = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: caster);
            if (targets.Count == 0)
            {
                // Refuse BEFORE spending ink. Wasting a charge on empty
                // air would be the single most annoying possible bug in
                // a resource-costed spell.
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_target" });
                MessageLog.Add(caster.GetDisplayName()
                    + " holds the rite — there is nothing to strike.");
                return false;
            }

            var grimoire = GrimoireInk.FindInked(caster);
            if (grimoire == null)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_ink" });
                MessageLog.Add("The pages are dry — " + caster.GetDisplayName()
                    + " has no ink to spend.");
                return false;
            }
            grimoire.TrySpend();

            int totalConsumed = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                // The rite tells the system its element and slot count;
                // the DATA decides what that is worth. No combo logic
                // lives here.
                var res = ResonanceSystem.Spend(
                    target, ELEMENT, SLOTS, caster, zone);
                totalConsumed += res.Consumed.Count;

                int damage = (int)Math.Round(BASE_DAMAGE * res.Multiplier);
                var dmg = new Damage(damage);
                dmg.AddAttribute("Electric");
                dmg.AddAttribute("Lightning");
                CombatSystem.ApplyDamage(target, dmg, caster, zone);

                if (target.GetStatValue("Hitpoints", 0) <= 0) continue;

                // Riders. The rite interprets the tags; the system that
                // produced them has no idea what they mean.
                for (int r = 0; r < res.Riders.Count; r++)
                {
                    switch (res.Riders[r])
                    {
                        case "Stun":
                            target.ApplyEffect(
                                new StunnedEffect(duration: STUN_RIDER_TURNS),
                                caster, zone);
                            break;
                        case "Arc":
                            // The charge jumps back on: spent water
                            // leaves the target primed again, which is
                            // what makes Wet the best thing to feed it.
                            target.ApplyEffect(
                                new ElectrifiedEffect(charge: 1.0f), caster, zone);
                            break;
                        case "Shatter":
                            target.ApplyEffect(new BrokenEffect(), caster, zone);
                            break;
                    }
                }
            }

            Diag.Record("spell", "RiteCast", caster, caster,
                new
                {
                    rite = Name,
                    element = ELEMENT,
                    targets = targets.Count,
                    statusesConsumed = totalConsumed,
                    inkLeft = grimoire.Charges,
                });

            MessageLog.Add(caster.GetDisplayName() + " brings down the storm anvil"
                + (totalConsumed > 0
                    ? ", spending " + totalConsumed + " mark"
                      + (totalConsumed == 1 ? "" : "s") + "!"
                    : " — but nothing was primed to spend."));

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
