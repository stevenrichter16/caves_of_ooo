namespace CavesOfOoo.Core
{
    /// <summary>
    /// WSP8.3 — Rooted: the actor can act (attack, cast, use abilities)
    /// but cannot move from their current cell. Distinct from Stunned
    /// (blocks all action) and from Frozen (Ice-class action-block).
    /// Applied by <see cref="Skills.Cryomancy_Frostbind"/> on a chosen
    /// adjacent target.
    ///
    /// <para><b>Mechanic:</b> overrides <see cref="Effect.AllowMovement"/>
    /// to false, leaves <see cref="Effect.AllowAction"/> at the default
    /// true. <see cref="StatusEffectsPart.HandleBeforeMove"/> consults
    /// AllowMovement so the BeforeMove event is rejected while Rooted
    /// is active. The actor can still take other action types (the
    /// turn loop's BeginTakeAction gate calls AllowAction, which we
    /// don't override).</para>
    ///
    /// <para>Stacks: extends duration (mirrors Stunned/Confused). Does
    /// NOT accumulate magnitude — there's only one "you can't move"
    /// state. Multiple Frostbinds in a row simply lock the target for
    /// longer.</para>
    /// </summary>
    public class RootedEffect : Effect
    {
        public override string DisplayName => "rooted";

        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        public RootedEffect(int duration = 4)
        {
            Duration = duration;
        }

        /// <summary>The defining override: the entity cannot move while
        /// Rooted. AllowAction is left at the default (true) so the
        /// rooted entity can still attack adjacent foes / cast spells
        /// / activate abilities.</summary>
        public override bool AllowMovement(Entity target) => false;

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is rooted in place!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " can move again.");
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is RootedEffect root)
            {
                Duration += root.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&C";
    }
}
