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
        /// creature (mirrors Cudgel_Slam's CellHasOtherCreature gate).
        /// No-op if target is already adjacent (no need to drag).
        /// </summary>
        private void DragTowardHooker(Entity target, Zone zone)
        {
            var targetPos = zone.GetEntityPosition(target);
            var hookerPos = zone.GetEntityPosition(Hooker);
            if (targetPos.x < 0 || hookerPos.x < 0) return;

            int dx = hookerPos.x - targetPos.x;
            int dy = hookerPos.y - targetPos.y;
            // Already adjacent — no drag needed.
            if (System.Math.Abs(dx) <= 1 && System.Math.Abs(dy) <= 1) return;

            // Pick a direction matching the existing 8-dir convention
            // in Zone.GetCellInDirection (0=N, 1=NE, 2=E, ..., 7=NW).
            // Step toward hooker on each axis (-1, 0, +1).
            int stepX = System.Math.Sign(dx);
            int stepY = System.Math.Sign(dy);
            int dir = DirectionFromStep(stepX, stepY);
            if (dir < 0) return;

            var nextCell = zone.GetCellInDirection(targetPos.x, targetPos.y, dir);
            if (nextCell == null) return;
            if (nextCell.IsSolid()) return;
            if (CellHasOtherCreature(nextCell, target)) return;

            zone.MoveEntity(target, nextCell.X, nextCell.Y);
            MessageLog.Add(target.GetDisplayName() + " is dragged toward " + Hooker.GetDisplayName() + ".");
        }

        /// <summary>
        /// Convert a unit step (-1/0/+1, -1/0/+1) into the 8-dir index
        /// used by <see cref="Zone.GetCellInDirection"/>.
        /// Returns -1 if the step is (0, 0) — no movement.
        /// </summary>
        private static int DirectionFromStep(int dx, int dy)
        {
            // 0=N(0,-1), 1=NE(+1,-1), 2=E(+1,0), 3=SE(+1,+1),
            // 4=S(0,+1), 5=SW(-1,+1), 6=W(-1,0), 7=NW(-1,-1)
            if (dx ==  0 && dy == -1) return 0;
            if (dx ==  1 && dy == -1) return 1;
            if (dx ==  1 && dy ==  0) return 2;
            if (dx ==  1 && dy ==  1) return 3;
            if (dx ==  0 && dy ==  1) return 4;
            if (dx == -1 && dy ==  1) return 5;
            if (dx == -1 && dy ==  0) return 6;
            if (dx == -1 && dy == -1) return 7;
            return -1;
        }

        /// <summary>Iterates the cell looking for a Creature-tagged
        /// entity that isn't the excluded one. Mirrors
        /// <see cref="CavesOfOoo.Skills.Cudgel_Slam"/>'s helper.</summary>
        private static bool CellHasOtherCreature(Cell cell, Entity exclude)
        {
            if (cell == null) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (e == null || e == exclude) continue;
                if (e.Tags.ContainsKey("Creature")) return true;
            }
            return false;
        }

        public override bool OnStack(Effect incoming)
        {
            // Stacking refreshes the duration and re-stamps the hooker —
            // re-hooking an already-hooked target gives the new attacker
            // control. Mirrors Qud's "RemoveAllEffects<Hooked>" + reapply
            // pattern from various dismember sites (the new hook always
            // wins).
            if (incoming is HookedEffect newHook)
            {
                Duration = newHook.Duration;
                Hooker = newHook.Hooker;
                SaveTarget = newHook.SaveTarget;
                Rng = newHook.Rng;
                return true;
            }
            return false;
        }
    }
}
