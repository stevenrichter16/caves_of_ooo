using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Acrobatics active ability: remove ONE active negative status
    /// effect from self. Priority order favors action-blocking effects:
    /// Stunned > Frozen > Paralyzed > Bleeding > others. Long cooldown
    /// keeps it tactical (one cleanse per fight, not on-demand
    /// healing). Distinct from every other ability — EvasiveRoll is
    /// the only SELF-CLEANSE.
    ///
    /// <para><b>Mechanic:</b> SelfCentered, no targeting. Iterates
    /// the actor's <see cref="StatusEffectsPart"/> effects, picks the
    /// highest-priority negative effect, removes it. If no negative
    /// effects: rejection (no_target).</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Acrobatics_EvasiveRoll</c>):
    /// "the only self-cleanse / status-removal active."</para>
    /// </summary>
    public class Acrobatics_EvasiveRoll : BaseSkillPart
    {
        public override string Name => nameof(Acrobatics_EvasiveRoll);

        public const int COOLDOWN = 60;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Evasive Roll",
                Command = "CommandEvasiveRoll",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 0,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;
            var sep = actor.GetPart<StatusEffectsPart>();
            if (sep == null)
            {
                EmitSkillRejectedDiag(ctx, "no_status_part");
                return;
            }

            // Priority order: action-blockers first (highest impact to
            // remove), then per-turn-damage effects, then rest.
            // Iterate the priority types one by one — first match wins.
            var prioritized = new System.Type[]
            {
                typeof(StunnedEffect),
                typeof(FrozenEffect),
                typeof(ParalyzedEffect),
                typeof(BleedingEffect),
                typeof(PoisonedEffect),
                typeof(BurningEffect),
                typeof(ConfusedEffect),
                typeof(HobbledEffect),
                typeof(RootedEffect),
            };
            for (int i = 0; i < prioritized.Length; i++)
            {
                if (sep.RemoveEffect(prioritized[i]))
                {
                    MessageLog.Add(actor.GetDisplayName() + " rolls free of "
                        + prioritized[i].Name.Replace("Effect", "").ToLower() + "!");
                    return;
                }
            }

            // No prioritized type — fall back to any TYPE_NEGATIVE effect.
            bool removed = sep.RemoveEffect(eff => eff.IsOfType(Effect.TYPE_NEGATIVE));
            if (!removed)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to roll free of.");
                EmitSkillRejectedDiag(ctx, "no_negative_effect");
                return;
            }

            MessageLog.Add(actor.GetDisplayName() + " rolls free of a negative effect!");
        }
    }
}
