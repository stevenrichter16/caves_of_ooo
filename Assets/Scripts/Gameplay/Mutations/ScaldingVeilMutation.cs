using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM8 — the rite that spends a status on YOURSELF.
    ///
    /// <para>Every other rite reads an enemy. This one reads the caster:
    /// it consumes your own <see cref="WetEffect"/> and boils it into a
    /// <see cref="ScaldingVeilEffect"/>. Being soaked is normally a
    /// liability — it is precisely what sets you up to be shocked and
    /// frozen — so this is the one way to turn a debuff you are
    /// suffering into an asset.</para>
    ///
    /// <para>It refuses when you are dry, and says so. A rite that
    /// silently did nothing while eating a charge would be the worst
    /// possible feedback for a resource-costed spell.</para>
    /// </summary>
    public class ScaldingVeilMutation : BaseMutation
    {
        public const string COMMAND = "CommandScaldingVeil";
        public const int COOLDOWN = 30;
        public const string ELEMENT = "Heat";
        public const int SLOTS = 1;

        public const int VEIL_DURATION = 8;
        public const int VEIL_SCALD = 3;
        public const int VEIL_CONFUSE = 2;

        public override string Name => "ScaldingVeil";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Scalding Veil";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName, COMMAND, "Rites",
                AbilityTargetingMode.SelfCentered, 0);
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
            if (!Cast(e.GetParameter<Zone>("Zone"))) return true;
            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone)
        {
            var caster = ParentEntity;
            if (caster == null) return false;

            // Check BEFORE spending ink: a rite that eats a charge and
            // does nothing is the worst feedback a costed spell can give.
            var preview = ResonanceSystem.Preview(caster, ELEMENT, SLOTS);
            if (!preview.Consumed.Contains("Wet"))
            {
                Diag.Record("spell", "RiteRejected", caster, caster,
                    new { rite = Name, reason = "caster_not_wet" });
                MessageLog.Add(caster.GetDisplayName() + " is dry — there is nothing to boil.");
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

            // Resonate on the CASTER — the whole conceit of the rite.
            var res = ResonanceSystem.Spend(caster, ELEMENT, SLOTS, caster, zone);

            caster.ApplyEffect(
                new ScaldingVeilEffect(VEIL_DURATION, VEIL_SCALD, VEIL_CONFUSE),
                caster, zone);

            Diag.Record("spell", "RiteCast", caster, caster,
                new
                {
                    rite = Name, element = ELEMENT,
                    statusesConsumed = res.Consumed.Count,
                    selfTargeted = true,
                    inkLeft = grimoire.Charges,
                });

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
