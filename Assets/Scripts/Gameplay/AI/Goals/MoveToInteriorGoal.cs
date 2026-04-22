namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that seeks the nearest reachable interior (roofed / indoors)
    /// cell and walks to it via a child MoveToGoal. Finishes when the NPC
    /// stands on an IsInterior cell, when Age exceeds MaxTurns, or when
    /// the child MoveToGoal fails (no path to the chosen interior).
    ///
    /// <para>CoO-adapted counterpart of Qud's
    /// <c>MoveToInterior</c> goal. Qud navigates to a specific
    /// <c>GameObject</c> with an <c>Interior</c> part and calls
    /// <c>Interior.TryEnter</c>; we BFS to the nearest
    /// <see cref="Cell.IsInterior"/> cell and use our flat-zone
    /// MoveToGoal.</para>
    ///
    /// <para>MaxTurns is a CoO safety net — Qud has no timeout, but
    /// without one a broken-path scenario could spin forever.</para>
    ///
    /// <para>Typical callers (future work): weather system on rain,
    /// curfew at dusk, evacuation from a fire.</para>
    /// </summary>
    public class MoveToInteriorGoal : GoalHandler
    {
        /// <summary>Maximum BFS radius in cells (enough to cover any 80-cell zone).</summary>
        public int MaxSearchRadius = 40;

        /// <summary>Max turns before giving up (safety net; Qud has no equivalent).</summary>
        public int MaxTurns = 50;

        public MoveToInteriorGoal(int maxSearchRadius = 40, int maxTurns = 50)
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
            return cell != null && cell.IsInterior;
        }

        public override string GetDetails() => $"age={Age}/{MaxTurns}";

        public override void TakeAction()
        {
            var pos = CurrentZone.GetEntityPosition(ParentEntity);
            if (pos.x < 0) { FailToParent(); return; }

            // Already inside — Finished() will pop the goal next tick.
            var here = CurrentZone.GetCell(pos.x, pos.y);
            if (here != null && here.IsInterior) return;

            Think("seeking shelter");

            // Find the nearest reachable passable interior cell.
            var target = AIHelpers.FindNearestCellWhere(
                CurrentZone, pos.x, pos.y,
                c => c.IsInterior && c.IsPassable(),
                MaxSearchRadius);
            if (target == null) { FailToParent(); return; }

            // Remaining MaxTurns budget so the child inherits the same
            // cap; mirrors FleeLocationGoal's pattern.
            int remainingTurns = System.Math.Max(1, MaxTurns - Age);
            PushChildGoal(new MoveToGoal(target.Value.x, target.Value.y, remainingTurns));
        }

        public override void Failed(GoalHandler child)
        {
            // Child MoveToGoal gave up (unreachable) — propagate.
            FailToParent();
        }
    }
}
