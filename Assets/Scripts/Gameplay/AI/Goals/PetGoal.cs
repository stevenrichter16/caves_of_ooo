namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that finds a nearby ally, walks up to them, and emits a "petting" flavor particle.
    /// Mirrors Qud's Pet goal handler (simplified — pure flavor / ambient life).
    ///
    /// Intended use: companion creatures, friendly villagers bonding with their pets,
    /// children playing with animals, etc. Adds "lived-in" ambient behavior.
    ///
    /// Finds the nearest allied Creature (FactionManager.IsAllied) within sight radius,
    /// walks adjacent to them (not onto their cell — allies are solid), emits a '*'
    /// particle, and pops. One-shot.
    ///
    /// Approach reliability: a moving or unreachable ally could otherwise cause
    /// infinite re-pushes of MoveToGoal (which pops silently on Age-timeout
    /// rather than via FailToParent). <see cref="MaxApproachAttempts"/> caps
    /// the number of MoveToGoal pushes before the goal gives up.
    /// </summary>
    public class PetGoal : GoalHandler
    {
        /// <summary>Max MoveToGoal pushes before PetGoal gives up on reaching the ally.</summary>
        public const int MaxApproachAttempts = 3;

        /// <summary>Ticks each inner MoveToGoal is allowed before being declared a timeout.</summary>
        public const int ApproachStepBudget = 20;

        public Entity Target;

        private enum Phase { FindAlly, Approach, Done }
        private Phase _phase = Phase.FindAlly;
        private int _approachAttempts;

        public PetGoal() { }

        /// <summary>Optional: pet a specific ally. If null, the goal searches at runtime.</summary>
        public PetGoal(Entity target)
        {
            Target = target;
            if (target != null) _phase = Phase.Approach;
        }

        public override bool IsBusy() => false;
        public override bool CanFight() => false;
        public override bool Finished() => _phase == Phase.Done;

        public override void TakeAction()
        {
            if (CurrentZone == null || ParentEntity == null) { Pop(); return; }

            switch (_phase)
            {
                case Phase.FindAlly:
                    FindNearestAlly();
                    break;
                case Phase.Approach:
                    ApproachTarget();
                    break;
            }
        }

        private void FindNearestAlly()
        {
            var selfCell = CurrentZone.GetEntityCell(ParentEntity);
            if (selfCell == null) { Pop(); return; }

            int radius = ParentBrain?.SightRadius ?? 10;
            Entity nearest = null;
            int nearestDist = int.MaxValue;

            foreach (var other in CurrentZone.GetReadOnlyEntities())
            {
                if (other == ParentEntity) continue;
                if (!other.HasTag("Creature")) continue;
                if (!FactionManager.IsAllied(ParentEntity, other)) continue;

                var otherCell = CurrentZone.GetEntityCell(other);
                if (otherCell == null) continue;

                int dist = AIHelpers.ChebyshevDistance(selfCell.X, selfCell.Y, otherCell.X, otherCell.Y);
                if (dist > radius) continue;

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = other;
                }
            }

            if (nearest == null) { Pop(); return; }

            Target = nearest;
            _phase = Phase.Approach;
            ApproachTarget();
        }

        private void ApproachTarget()
        {
            if (Target == null) { Pop(); return; }

            var targetCell = CurrentZone.GetEntityCell(Target);
            if (targetCell == null) { Pop(); return; } // target left zone

            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            if (AIHelpers.IsAdjacent(myPos.x, myPos.y, targetCell.X, targetCell.Y))
            {
                EmitPetEffect();
                _phase = Phase.Done;
                return;
            }

            // Give up if we've already pushed MoveToGoal MaxApproachAttempts times
            // without succeeding. Prevents infinite loop when the ally is
            // unreachable or keeps moving faster than we can catch up.
            // MoveToGoal's Age-based timeout pops silently (no FailToParent),
            // so we can't rely on Failed() to notice those cases.
            if (_approachAttempts >= MaxApproachAttempts)
            {
                _phase = Phase.Done;
                return;
            }

            _approachAttempts++;
            PushChildGoal(new MoveToGoal(targetCell.X, targetCell.Y, ApproachStepBudget));
        }

        private void EmitPetEffect()
        {
            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            if (myPos.x < 0) return;
            // Magenta particle above the petter. Use an ASCII glyph so it renders
            // correctly in the CP437 tileset (unicode '♥' U+2665 is not in CP437).
            AsciiFxBus.EmitParticle(CurrentZone, myPos.x, myPos.y - 1, '*', "&M", 0.5f);
        }

        public override void Failed(GoalHandler child)
        {
            // MoveToGoal called FailToParent (unreachable). Check if we're lucky
            // enough to be adjacent now, emit the effect, then mark done.
            if (Target != null)
            {
                var targetCell = CurrentZone?.GetEntityCell(Target);
                var myPos = CurrentZone?.GetEntityPosition(ParentEntity) ?? (-1, -1);
                if (targetCell != null && myPos.x >= 0
                    && AIHelpers.IsAdjacent(myPos.x, myPos.y, targetCell.X, targetCell.Y))
                {
                    EmitPetEffect();
                }
            }
            _phase = Phase.Done;
        }
    }
}
