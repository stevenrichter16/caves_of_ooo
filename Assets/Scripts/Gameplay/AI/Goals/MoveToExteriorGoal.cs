namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that seeks the nearest reachable exterior (outdoor) cell and
    /// walks to it via a child MoveToGoal. Symmetric twin of
    /// <see cref="MoveToInteriorGoal"/>. Finishes when the NPC stands on
    /// a non-IsInterior cell, when Age exceeds MaxTurns, or when the
    /// child MoveToGoal fails (no path to any reachable exterior).
    ///
    /// <para>CoO-adapted counterpart of Qud's
    /// <c>MoveToExterior</c> goal. Qud walks to a specific
    /// <c>InteriorPortal</c> (the door out of the current InteriorZone);
    /// we BFS to any reachable non-IsInterior cell.</para>
    ///
    /// <para>MaxTurns is a CoO safety net — Qud has no timeout.</para>
    ///
    /// <para>Typical callers (future work): dawn/curfew triggers sending
    /// NPCs outside, fire evacuation, open-air crafting routines.</para>
    /// </summary>
    public class MoveToExteriorGoal : GoalHandler
    {
        /// <summary>Maximum BFS radius in cells.</summary>
        public int MaxSearchRadius = 40;

        /// <summary>Max turns before giving up (safety net; Qud has no equivalent).</summary>
        public int MaxTurns = 50;

        public MoveToExteriorGoal(int maxSearchRadius = 40, int maxTurns = 50)
        {
            MaxSearchRadius = maxSearchRadius;
            MaxTurns = maxTurns;
        }

        public override bool IsBusy() => true;
        public override bool CanFight() => false;

        public override bool Finished()
        {
            if (Age > MaxTurns) return true;
            var pos = CurrentZone?.GetEntityPosition(ParentEntity) ?? (-1, -1);
            if (pos.x < 0) return true;
            var cell = CurrentZone.GetCell(pos.x, pos.y);
            return cell != null && !cell.IsInterior;
        }

        public override string GetDetails() => $"age={Age}/{MaxTurns}";

        public override void TakeAction()
        {
            var pos = CurrentZone.GetEntityPosition(ParentEntity);
            if (pos.x < 0) { FailToParent(); return; }

            // Already outside — Finished() will pop the goal next tick.
            var here = CurrentZone.GetCell(pos.x, pos.y);
            if (here != null && !here.IsInterior) return;

            Think("heading outside");

            var target = AIHelpers.FindNearestCellWhere(
                CurrentZone, pos.x, pos.y,
                c => !c.IsInterior && c.IsPassable(),
                MaxSearchRadius);
            if (target == null) { FailToParent(); return; }

            int remainingTurns = System.Math.Max(1, MaxTurns - Age);
            PushChildGoal(new MoveToGoal(target.Value.x, target.Value.y, remainingTurns));
        }

        public override void Failed(GoalHandler child)
        {
            FailToParent();
        }

        public override void OnPop()
        {
            // Mirrors MoveToInteriorGoal.OnPop — writes a terminal thought
            // so the Phase 10 inspector reflects the completed state
            // instead of the sticky "heading outside" TakeAction message.
            // See MoveToInteriorGoal.OnPop for the full rationale.
            var pos = CurrentZone?.GetEntityPosition(ParentEntity) ?? (-1, -1);
            var cell = (pos.x >= 0 && CurrentZone != null)
                ? CurrentZone.GetCell(pos.x, pos.y)
                : null;
            Think(cell != null && !cell.IsInterior ? "outside" : null);
        }
    }
}
