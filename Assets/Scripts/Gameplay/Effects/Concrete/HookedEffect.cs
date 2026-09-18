using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Hooked: the target is on the end of someone's hook (typically
    /// from <see cref="CavesOfOoo.Skills.Axe_HookAndDrag"/>). At end of
    /// each of the target's turns: roll a Strength save vs
    /// <see cref="SaveTarget"/>; on failure, the target is dragged 1
    /// cell toward the <see cref="Hooker"/> if a clear path exists.
    /// On save success, the hook breaks (effect removed with cause
    /// <see cref="Effect.CAUSE_SAVE_SUCCEEDED"/>). Default duration
    /// 9 turns (Qud parity).
    ///
    /// <para>Per Qud's <c>Hooked</c> effect — Qud's version tracks
    /// hook state through more elaborate cell-tracking (LeftCell on
    /// the hooker, dragged-to-2-cells trigger). CoO simplifies to the
    /// turn-end pull-1-cell pattern, which captures the gameplay feel
    /// (the hooked target is reeled in over time) without needing a
    /// global movement-tracking system. Documented per CLAUDE.md §4.2
    /// as Match (mechanic family) + Divergent (no movement-trigger
    /// drag — drag fires on turn-end instead).</para>
    /// </summary>
    public class HookedEffect : Effect
    {
        public override string DisplayName => "hooked";

        // WSP6.16 backfill — TYPE_NEGATIVE so Shank etc. count this
        // as a debuff. Standard for any negative effect.
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        public Entity Hooker;
        public int SaveTarget;
        public Random Rng;

        public HookedEffect(int duration = 9, Entity hooker = null,
            int saveTarget = 20, Random rng = null)
        {
            Duration = duration;
            Hooker = hooker;
            SaveTarget = saveTarget;
            Rng = rng ?? new Random();
        }

        public override void OnApply(Entity target)
        {
            if (target == null) return;
            MessageLog.Add(target.GetDisplayName() + " is hooked!");
        }

        public override void OnRemove(Entity target)
        {
            // No state to undo — the hook just lets go. The removal
            // event itself carries CAUSE_SAVE_SUCCEEDED or
            // CAUSE_DURATION_EXPIRED depending on which branch fired.
        }

        /// <summary>
        /// Per-turn-end logic: save check first, then drag-toward-hooker
        /// on failed save. Save success removes the effect; otherwise the
        /// default Duration decrement runs via base.OnTurnEnd.
        /// </summary>
        public override void OnTurnEnd(Entity target, GameEvent context)
        {
            if (target == null) return;

            // Hooker validity check. If the hooker died or left the
            // zone, the hook trivially breaks — no save needed.
            if (Hooker == null)
            {
                LastRemovalCause = CAUSE_EXTERNAL;
                Duration = 0;
                return;
            }

            // Strength save vs SaveTarget. On success the hook breaks
            // permanently (mirrors Qud's Strength-save resist).
            int strMod = StatUtils.GetModifier(target, "Strength");
            int roll = DiceRoller.Roll(20, Rng) + strMod;
            if (roll >= SaveTarget)
            {
                LastRemovalCause = CAUSE_SAVE_SUCCEEDED;
                Duration = 0;
                MessageLog.Add(target.GetDisplayName() + " breaks free of the hook!");
                return;
            }

            // Drag toward hooker. Get Zone from event context (mirrors
            // BleedingEffect.OnTurnStart line 43).
            Zone zone = context?.GetParameter<Zone>("Zone");
            if (zone != null)
                DragTowardHooker(target, zone);

            // Default Duration decrement (Effect base behavior).
            base.OnTurnEnd(target, context);
        }

        /// <summary>
        /// If the target and Hooker are both in the given zone, move the
        /// target 1 cell toward the Hooker — choosing the cardinal/
        /// diagonal direction that minimizes Chebyshev distance. Skip
        /// the move if the chosen cell is solid or contains another
        /// creature. The complete destination must fit, and physical
        /// adjacency to the hooker stops the pull before bodies overlap.
        /// </summary>
        private void DragTowardHooker(Entity target, Zone zone)
        {
            var targetPos = zone.GetEntityPosition(target);
            if (targetPos.x < 0 || zone.GetEntityCell(Hooker) == null) return;
            if (SpatialQuery.Distance(zone, target, Hooker) <= 1) return;

            Cell targetContact = null, hookContact = null;
            int bestDistance = int.MaxValue;
            foreach (var cell in zone.GetOccupiedCells(target))
            {
                var other = SpatialQuery.ClosestCell(zone, Hooker, cell.X, cell.Y);
                if (other == null) continue;
                int distance = Math.Max(Math.Abs(cell.X - other.X), Math.Abs(cell.Y - other.Y));
                if (distance < bestDistance) { bestDistance = distance; targetContact = cell; hookContact = other; }
            }
            if (targetContact == null) return;
            int nx = targetPos.x + Math.Sign(hookContact.X - targetContact.X);
            int ny = targetPos.y + Math.Sign(hookContact.Y - targetContact.Y);
            if (!zone.CanPlaceFootprint(target, nx, ny)
                || CavesOfOoo.Skills.MultiCellAbilityQueries.CreatureAtPlacement(zone, target, nx, ny) != null
                || SpatialQuery.DistanceAt(zone, target, nx, ny, Hooker) == 0) return;
            if (MovementSystem.ForceMoveTo(target, zone, nx, ny))
                MessageLog.Add(target.GetDisplayName() + " is dragged toward " + Hooker.GetDisplayName() + ".");
        }

    }
}
