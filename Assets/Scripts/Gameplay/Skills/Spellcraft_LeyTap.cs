using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Trades current HP for one saved universal damage-cast charge.
    /// Its positive effect lasts three owner actions after activation; every
    /// direct hit in the next qualifying cast receives the same bonus.
    /// Modifier queries are pure; SpellSkillPart commits charge spending.
    /// </summary>
    public class Spellcraft_LeyTap : SpellSkillPart
    {
        public override string Name => nameof(Spellcraft_LeyTap);

        public const int COOLDOWN = 40;
        public const int HP_DRAIN_PERCENT = 15;
        public const int BUFF_DURATION = 3;
        public const int DAMAGE_BONUS_MULTIPLIER = 2;

        public int PendingBonus => ParentEntity?.GetEffect<LeyTapEffect>()?.BonusDamage ?? 0;
        public int TurnsRemaining => ParentEntity?.GetEffect<LeyTapEffect>()?.Duration ?? 0;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Ley Tap",
                Command = "CommandLeyTap",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 0,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            var hp = actor.GetStat("Hitpoints");
            if (hp == null) { EmitSkillRejectedDiag(ctx, "no_hitpoints"); return false; }

            int drain = (hp.BaseValue * HP_DRAIN_PERCENT) / 100;
            if (drain < 1) drain = 1;
            if (drain >= hp.BaseValue) drain = hp.BaseValue - 1;
            if (drain < 1)
            {
                EmitSkillRejectedDiag(ctx, "insufficient_hp");
                return false;
            }
            int bonus = drain * DAMAGE_BONUS_MULTIPLIER;
            if (!actor.ApplyEffect(new LeyTapEffect(bonus, BUFF_DURATION), actor, ctx.Zone))
            { EmitSkillRejectedDiag(ctx, "buff_refused"); return false; }
            SpellFxCapture.Target(ctx.Zone, actor);
            hp.BaseValue -= drain;
            SpellFxCapture.RecordDamage(ctx.Zone, actor, drain, resisted: false);

            MessageLog.Add(actor.GetDisplayName() + " taps the leylines! "
                + drain + " HP drained; next damaging cast deals +" + bonus
                + " per target within " + BUFF_DURATION + " of your turns.");

            return true;
        }

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            // This hook is also safe to query for a readout. Only the enclosing
            // successful direct-damage cast can consume the saved charge.
            return SpellDamageHelpers.LeyTapBonus(ParentEntity,
                TurnsRemaining > 0 ? PendingBonus : 0);
        }
    }
}
