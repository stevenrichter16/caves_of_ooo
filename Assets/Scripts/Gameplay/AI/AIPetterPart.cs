namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part that occasionally pushes <see cref="PetGoal"/> on a
    /// bored tick. Wires Phase 6's PetGoal to a real trigger (M3.1). The
    /// goal finds a nearby ally, walks adjacent to them, and emits a magenta
    /// '*' particle — pure ambient flavor, zero combat impact.
    ///
    /// Mirrors <see cref="AIWellVisitorPart"/> exactly in shape: gate on
    /// <see cref="AIBoredEvent"/>, probability-sample via
    /// <see cref="BrainPart.Rng"/>, check idempotency with
    /// <see cref="BrainPart.HasGoal(string)"/>, push the goal, consume the
    /// event. Attached to NPCs via blueprint:
    ///
    ///   { "Name": "AIPetter", "Params": [{ "Key": "Chance", "Value": "5" }] }
    ///
    /// Intended wearer: VillageChild. Could also go on pets / companions.
    ///
    /// Side-effect warning: PetGoal pushes a MoveToGoal child to approach
    /// the ally. If the ally is unreachable, PetGoal caps the retry count
    /// at <see cref="PetGoal.MaxApproachAttempts"/> and pops — the child
    /// doesn't chase indefinitely. Safe ambient behavior.
    /// </summary>
    public class AIPetterPart : AIBehaviorPart
    {
        public override string Name => "AIPetter";

        /// <summary>Percent chance per bored tick to push a PetGoal (0-100).</summary>
        public int Chance = 3;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == AIBoredEvent.ID)
            {
                bool result = HandleBored();
                if (!result) e.Handled = true;
                return result;
            }
            return true;
        }

        private bool HandleBored()
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain?.Rng == null || brain.CurrentZone == null)
                return true;

            // Idempotency — don't stack PetGoals if one is already on the stack.
            // Uses the string variant so the check doesn't require the
            // generic parameter to be reachable from this assembly's test-only
            // paths. Mirrors AIWellVisitorPart / AIGuardPart idiom.
            if (brain.HasGoal("PetGoal"))
                return true;

            // Probability gate
            if (brain.Rng.Next(100) >= Chance)
                return true;

            brain.PushGoal(new PetGoal());
            return false; // consumed
        }
    }
}
