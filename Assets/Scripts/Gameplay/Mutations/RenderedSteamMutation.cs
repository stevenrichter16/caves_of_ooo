using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM8 — the game's clearest lesson that TWO statuses
    /// beat one.
    ///
    /// <para>Rendered Steam is the only rite that wants two OPPOSED
    /// statuses on the same body. Water and fire cancel each other
    /// everywhere else in the game — moisture suppresses ignition
    /// (<c>PyroIgnition</c>), and burning boils moisture off — so a
    /// target carrying both at once is a deliberate, awkward,
    /// short-lived setup. This rite is the reward for arranging it.</para>
    ///
    /// <para><b>The bonus is explicit, not emergent.</b> Consuming Wet
    /// AND Burning together adds <see cref="PAIR_BONUS"/> on top of
    /// whatever resonance already paid, and blinds the whole radius with
    /// <see cref="ConfusedEffect"/>. Either status alone still resonates
    /// through the normal Heat table — it simply does not detonate.</para>
    /// </summary>
    public class RenderedSteamMutation : BaseMutation
    {
        public const string COMMAND = "CommandRenderedSteam";
        public const int RADIUS = 2;
        public const int COOLDOWN = 35;
        public const string ELEMENT = "Heat";
        public const int SLOTS = 2;
        public const int BASE_DAMAGE = 5;

        /// <summary>Extra multiplier when Wet and Burning are spent off
        /// the SAME target. The pair is the whole point of the rite.</summary>
        public const float PAIR_BONUS = 1.5f;

        public const int CONFUSE_TURNS = 3;

        public override string Name => "RenderedSteam";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of Rendered Steam";

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
            var zone = e.GetParameter<Zone>("Zone");
            var cell = e.GetParameter<Cell>("SourceCell");
            if (!Cast(zone, cell)) return true;
            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell)
        {
            var caster = ParentEntity;
            if (caster == null || zone == null || sourceCell == null) return false;

            var targets = SpellTargeting.GetCreaturesInRadius(
                zone, sourceCell.X, sourceCell.Y, RADIUS, exclude: caster);
            if (targets.Count == 0)
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "no_target" });
                MessageLog.Add(caster.GetDisplayName() + " holds the rite — nothing to render.");
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

            int detonations = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var res = ResonanceSystem.Spend(target, ELEMENT, SLOTS, caster, zone);

                bool pair = res.Consumed.Contains("Wet") && res.Consumed.Contains("Burning");
                float mult = res.Multiplier + (pair ? PAIR_BONUS : 0f);
                if (pair) detonations++;

                var dmg = new Damage((int)Math.Round(BASE_DAMAGE * mult));
                dmg.AddAttribute("Fire");
                dmg.AddAttribute("Heat");
                CombatSystem.ApplyDamage(target, dmg, caster, zone);

                if (target.GetStatValue("Hitpoints", 0) <= 0) continue;

                // Only a real steam burst blinds. A partial cast still
                // hurts, but the crowd control is what you paid the pair
                // for.
                if (pair)
                    target.ApplyEffect(new ConfusedEffect(CONFUSE_TURNS), caster, zone);
            }

            Diag.Record("spell", "RiteCast", caster, caster,
                new
                {
                    rite = Name, element = ELEMENT,
                    targets = targets.Count,
                    steamDetonations = detonations,
                    inkLeft = grimoire.Charges,
                });

            MessageLog.Add(detonations > 0
                ? caster.GetDisplayName() + "'s rite renders " + detonations
                  + " body" + (detonations == 1 ? "" : "ies") + " to scalding steam!"
                : caster.GetDisplayName() + "'s rite hisses — nothing was both soaked and burning.");

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
