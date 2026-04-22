namespace CavesOfOoo.Core
{
    /// <summary>
    /// Applied to a Passive NPC who saw a creature die nearby. Pushes
    /// <see cref="WanderDurationGoal"/> so the NPC visibly paces / moves
    /// around for <see cref="Effect.Duration"/> turns — the Innkeeper
    /// pacing nervously after a tavern brawl, Scribes looking shaken
    /// after a fight in the square, etc.
    ///
    /// Applied by <c>CombatSystem.BroadcastDeathWitnessed</c> on every
    /// death event; only Passive NPCs within 8 cells + LOS of the death
    /// cell qualify. This consumes M1.2's <see cref="BrainPart.Passive"/>
    /// flag directly as the filter.
    ///
    /// Lifecycle:
    /// - OnApply: push <see cref="WanderDurationGoal"/> (tracked so it
    ///   can be removed later). Guarded against pushing a duplicate if
    ///   a WanderDurationGoal is already on the brain from another source.
    /// - OnRemove: remove the tracked goal. Safe if the goal already
    ///   popped naturally (BrainPart.RemoveGoal is a no-op on absent
    ///   items — BrainPart.cs:131).
    /// - OnStack: a second witness event while already shaken extends
    ///   the remaining duration if the new event has a longer one;
    ///   never adds a duplicate effect to the status list.
    /// </summary>
    public class WitnessedEffect : Effect
    {
        public override string DisplayName => "shaken";

        // Qud-style effect type bitmask. Matches the category set used by
        // qud_decompiled_project/XRL.World.Effects/Shaken.cs (value
        // 117440514 = TYPE_MENTAL | TYPE_MINOR | TYPE_NEGATIVE | TYPE_REMOVABLE).
        // TYPE_MENTAL in particular matters — mind-blank / psionic-immunity
        // queries filter on that bit, and a shaken NPC should register as
        // having a mental effect for future interactions.
        //
        // NOTE: the mechanical payload diverges from Qud's Shaken. Qud's
        // Shaken carries a Level field and applies `-Level DV` as a stat
        // shift; ours pushes WanderDurationGoal for a pacing animation.
        // The category/bitmask stays aligned so future parity work on the
        // stat-shift mechanic can ride the same classification.
        public override int GetEffectType()
            => TYPE_MENTAL | TYPE_MINOR | TYPE_NEGATIVE | TYPE_REMOVABLE;

        // The goal this effect pushed on OnApply. Kept as a reference so
        // OnRemove can surgically remove exactly the goal we pushed, not
        // any other WanderDurationGoal that happens to be on the brain.
        // Null when OnApply was a no-op (brain absent, or goal already
        // present from another source).
        private WanderDurationGoal _pushedGoal;

        // `Effect` has no constructor taking a duration — the base class
        // exposes `Duration` as a public field. Assign it in the body
        // rather than forwarding via `: base(duration)`.
        public WitnessedEffect(int duration = 20)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            if (target == null) return;
            var brain = target.GetPart<BrainPart>();
            if (brain == null) return;

            // Idempotent at the goal level too: if a WanderDurationGoal is
            // already on the brain (e.g. pushed by some future ambient
            // system), don't add a second. The effect itself sticks around
            // for its Duration, but _pushedGoal stays null so OnRemove
            // won't accidentally remove the OTHER system's goal.
            if (brain.HasGoal<WanderDurationGoal>()) return;

            _pushedGoal = new WanderDurationGoal(Duration);
            brain.PushGoal(_pushedGoal);
            MessageLog.Add(target.GetDisplayName() + " looks shaken.");
        }

        public override void OnRemove(Entity target)
        {
            if (_pushedGoal == null) return;
            var brain = target?.GetPart<BrainPart>();
            // RemoveGoal is safe if the goal already popped naturally
            // (WanderDurationGoal.Finished() returns true when
            // _ticksTaken >= Duration, at which point BrainPart's
            // cleanup loop removes it on the next TakeTurn). List.Remove
            // on absent = no-op; OnPop is only called on successful removal.
            brain?.RemoveGoal(_pushedGoal);
            _pushedGoal = null;
        }

        public override bool OnStack(Effect incoming)
        {
            // A second witness event while still shaken: don't add a
            // duplicate WitnessedEffect to the status list. Extend the
            // existing duration if the new one is longer — a fresh
            // horror resets the clock, but an older faint echo doesn't
            // shorten it.
            if (incoming is WitnessedEffect w && w.Duration > Duration)
                Duration = w.Duration;
            return true; // handled — caller will discard `incoming`
        }
    }
}
