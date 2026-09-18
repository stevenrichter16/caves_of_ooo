using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Hearthwarm — anchor a gentle warming aura on an adjacent cell for
    /// a few turns. Port of <c>HearthwarmMutation</c>; taught by
    /// HearthwarmGrimoire, buyable in Pyromancy (no new utility tree —
    /// user decision, plan §5).
    ///
    /// <para>Verbatim: cooldown 4; requires a ThermalPart occupant in
    /// the chosen cell (warming empty ground means nothing); applies
    /// <c>HearthAuraEffect(x, y, duration 3, 60J/pulse)</c> to the
    /// caster. Refusal — no thermal target — is free.</para>
    /// </summary>
    public class Pyromancy_Hearthwarm : SpellSkillPart
    {
        public override string Name => nameof(Pyromancy_Hearthwarm);

        public const int COOLDOWN = 4;
        public const int AURA_DURATION = 3;
        public const float JOULES_PER_PULSE = 60f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Hearthwarm",
                Command = "CommandHearthwarm",
                Class = "Pyromancy",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.TargetCell == null) { EmitSkillRejectedDiag(ctx, "no_target_cell"); return false; }

            bool hasThermalTarget = false;
            for (int i = 0; i < ctx.TargetCell.Occupants.Count; i++)
            {
                if (ctx.TargetCell.Occupants[i].HasPart<ThermalPart>())
                {
                    hasThermalTarget = true;
                    break;
                }
            }
            if (!hasThermalTarget)
            {
                EmitSkillRejectedDiag(ctx, "no_thermal_target");
                return false; // free — nothing there to warm
            }

            SpellFxCapture.AffectCell(ctx.Zone, ctx.TargetCell.X, ctx.TargetCell.Y);
            ctx.Attacker.ApplyEffect(
                new HearthAuraEffect(ctx.TargetCell.X, ctx.TargetCell.Y,
                    duration: AURA_DURATION, joulesPerPulse: JOULES_PER_PULSE),
                ctx.Attacker,
                ctx.Zone);
            return true;
        }
    }
}
