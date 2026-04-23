namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that wanders randomly for a fixed number of turns, then pops.
    /// Mirrors Qud's WanderDuration goal handler.
    ///
    /// Contrast with WanderRandomlyGoal (one-shot, single step) and WanderGoal
    /// (walk to one random cell then pop). This one keeps stepping randomly
    /// for Duration ticks — useful for "patrol for 20 turns," "wander while
    /// the alchemist is busy," or "pace during idle conversation."
    ///
    /// Each tick takes one random passable step via WanderRandomlyGoal as a child,
    /// so the stepping logic stays in one place.
    /// </summary>
    public class WanderDurationGoal : GoalHandler
    {
        public int Duration;

        /// <summary>
        /// Optional thought string written to LastThought each tick so the
        /// Phase 10 inspector shows why the NPC is pacing. Null (default)
        /// leaves LastThought untouched — callers that push this goal for
        /// reasons with no visible narrative can pass null. Consumers today:
        /// <see cref="WitnessedEffect"/> passes <c>"shaken"</c>.
        /// </summary>
        public string Thought;

        private int _ticksTaken;

        public WanderDurationGoal(int duration, string thought = null)
        {
            Duration = duration;
            Thought = thought;
        }

        public override bool IsBusy() => false;

        public override bool Finished() => _ticksTaken >= Duration;

        public override string GetDetails()
            => Thought != null
                ? $"thought={Thought} ticks={_ticksTaken}/{Duration}"
                : $"ticks={_ticksTaken}/{Duration}";

        public override void TakeAction()
        {
            _ticksTaken++;
            ParentBrain.CurrentState = AIState.Wander;
            // Re-assert the thought every tick so it shows in the inspector
            // for the whole pacing duration. Without this, LastThought would
            // only reflect whatever the outer context last wrote.
            if (Thought != null) Think(Thought);
            PushChildGoal(new WanderRandomlyGoal());
        }

        public override void OnPop()
        {
            // Clear the thought on pop so it doesn't stick — same pattern as
            // DisposeOfCorpseGoal.OnPop (M5.2 playtest lesson: sticky thoughts
            // on LastThought are bad UX because nothing else overwrites them).
            // Only clear if we were writing one; otherwise leave whatever's
            // in LastThought alone.
            if (Thought != null) Think(null);
        }
    }
}
