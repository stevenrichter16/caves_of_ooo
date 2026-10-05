using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Trades current HP for three saved Fire/Heat damage-cast charges.
    /// Its positive effect lasts five owner actions after activation. Every
    /// direct hit in a qualifying cast receives the base-damage bonus, while
    /// SpellSkillPart spends only one charge after successful resolution.
    /// </summary>
    public class Pyromancy_HeartFlame : SpellSkillPart
    {
        public override string Name => nameof(Pyromancy_HeartFlame);

        public const int COOLDOWN = 100;
        public const int HP_SACRIFICE_PERCENT = 50;
        public const int BUFF_CHARGES = 3;
        public const int BUFF_DURATION = 5;
        public const int DAMAGE_BONUS_PERCENT = 100; // ×2 = +100% bonus

        public int ChargesRemaining => ParentEntity?.GetEffect<HeartFlameEffect>()?.ChargesRemaining ?? 0;
        public int TurnsRemaining => ParentEntity?.GetEffect<HeartFlameEffect>()?.Duration ?? 0;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Heart Flame",
                Command = "CommandHeartFlame",
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

            int sacrifice = (hp.BaseValue * HP_SACRIFICE_PERCENT) / 100;
            if (sacrifice < 1) sacrifice = 1;
            // Clamp so the actor doesn't suicide on HeartFlame.
            if (sacrifice >= hp.BaseValue) sacrifice = hp.BaseValue - 1;
            if (sacrifice < 1)
            {
                EmitSkillRejectedDiag(ctx, "insufficient_hp");
                return false;
            }
            if (!actor.ApplyEffect(new HeartFlameEffect(BUFF_CHARGES, BUFF_DURATION), actor, ctx.Zone))
            { EmitSkillRejectedDiag(ctx, "buff_refused"); return false; }
            SpellFxCapture.Target(ctx.Zone, actor);
            hp.BaseValue -= sacrifice;
            SpellFxCapture.RecordDamage(ctx.Zone, actor, sacrifice, resisted: false);

            MessageLog.Add(actor.GetDisplayName() + " burns own heart for power! "
                + sacrifice + " HP sacrificed; next " + BUFF_CHARGES
                + " fire damage casts deal +" + DAMAGE_BONUS_PERCENT
                + "% base damage within " + BUFF_DURATION + " of your turns.");

            return true;
        }

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            if (string.IsNullOrEmpty(elementAttribute)) return 0;
            // Match Heat or Fire element flavor.
            if (elementAttribute != "Heat" && elementAttribute != "Fire") return 0;
            if (!SpellDamageHelpers.HeartFlameReady(ParentEntity,
                TurnsRemaining > 0 && ChargesRemaining > 0)) return 0;
            int bonus = (baseDamage * DAMAGE_BONUS_PERCENT) / 100;
            return bonus;
        }
    }
}
