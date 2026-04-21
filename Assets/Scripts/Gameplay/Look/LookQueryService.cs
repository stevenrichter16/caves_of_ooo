using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Read-only inspect queries for world cursor / look mode.
    /// Converts simulation state into lightweight presentation DTOs.
    /// </summary>
    public static class LookQueryService
    {
        public static LookSnapshot BuildSnapshot(Entity player, Zone zone, int x, int y)
        {
            if (zone == null || !zone.InBounds(x, y))
            {
                return new LookSnapshot(
                    x,
                    y,
                    "[" + x + "," + y + "] out of bounds",
                    "There is nothing there.",
                    new List<string>(),
                    null,
                    null);
            }

            Cell cell = zone.GetCell(x, y);
            Entity primary = cell?.GetTopVisibleObject();

            if (cell == null)
            {
                return new LookSnapshot(
                    x,
                    y,
                    "[" + x + "," + y + "] empty ground",
                    "There is nothing there.",
                    new List<string>(),
                    null,
                    null);
            }

            List<Entity> visibleObjects = GetVisibleObjects(cell);
            string header = primary != null
                ? "[" + x + "," + y + "] " + primary.GetDisplayName()
                : "[" + x + "," + y + "] empty ground";

            string summary = BuildSummary(primary, visibleObjects);
            List<string> details = new List<string>();

            string contents = BuildContentsLine(visibleObjects);
            if (!string.IsNullOrEmpty(contents))
                details.Add(contents);

            string subjectDetail = BuildPrimaryDetail(player, primary);
            if (!string.IsNullOrEmpty(subjectDetail))
                details.Add(subjectDetail);

            string flags = BuildFlagsLine(cell);
            if (!string.IsNullOrEmpty(flags))
                details.Add(flags);

            // Phase 10 — AI goal-stack + last-thought inspector fields. Populated
            // only when the debug toggle is on AND the primary is a Creature with
            // a BrainPart. All three gates must pass or both fields stay null.
            // LookOverlayRenderer / SidebarRenderer treat null as "don't render".
            BuildBrainInspection(primary, out var goalLines, out var lastThought);

            return new LookSnapshot(x, y, header, summary, details, primary, cell,
                goalStackLines: goalLines,
                lastThought: lastThought);
        }

        /// <summary>
        /// Phase 10 — populate goal-stack + last-thought fields for the primary
        /// entity when the inspector toggle is on. Emits goals TOP-DOWN (topmost
        /// / currently-executing first) and collapses consecutive-duplicate
        /// descriptions with <c>xN</c>, matching Qud's <c>GetDebugInternalsEvent</c>
        /// renderer's format.
        ///
        /// Both outs are null when:
        /// - <see cref="AIDebug.AIInspectorEnabled"/> is false, OR
        /// - primary is null / not Creature-tagged, OR
        /// - primary has no <see cref="BrainPart"/>.
        /// </summary>
        private static void BuildBrainInspection(
            Entity primary,
            out List<string> goalLines,
            out string lastThought)
        {
            goalLines = null;
            lastThought = null;

            if (!AIDebug.AIInspectorEnabled) return;
            if (primary == null) return;
            if (!primary.HasTag("Creature")) return;

            var brain = primary.GetPart<BrainPart>();
            if (brain == null) return;

            // LastThought is set even when the goal stack is empty (Brain.Think
            // can be called outside any goal). Use "none" placeholder when null
            // so the sidebar always renders a "Thought:" line instead of
            // silently omitting it when the creature just hasn't thought yet.
            lastThought = string.IsNullOrEmpty(brain.LastThought) ? "none" : brain.LastThought;

            int count = brain.GoalCount;
            if (count == 0) return;

            goalLines = new List<string>(count);
            // Iterate top-down so the first rendered line is what the NPC is
            // currently doing. Indices in BrainPart._goals are bottom-up (0 =
            // oldest), so we start at count-1 and walk down.
            int i = count - 1;
            while (i >= 0)
            {
                var goal = brain.PeekGoalAt(i);
                if (goal == null) { i--; continue; }

                string desc = goal.GetDescription();
                int run = 1;
                while (i - run >= 0)
                {
                    var next = brain.PeekGoalAt(i - run);
                    if (next == null || next.GetDescription() != desc) break;
                    run++;
                }
                goalLines.Add(run > 1 ? desc + " x" + run : desc);
                i -= run;
            }
        }

        private static List<Entity> GetVisibleObjects(Cell cell)
        {
            List<Entity> visible = new List<Entity>();
            if (cell == null)
                return visible;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity entity = cell.Objects[i];
                RenderPart render = entity?.GetPart<RenderPart>();
                if (render != null && render.Visible)
                    visible.Add(entity);
            }

            return visible;
        }

        private static string BuildSummary(Entity primary, List<Entity> visibleObjects)
        {
            if (primary != null)
            {
                if (visibleObjects.Count <= 1)
                    return "You see " + primary.GetDisplayName() + ".";

                return "You see " + primary.GetDisplayName() + " with " + (visibleObjects.Count - 1) + " other visible object(s).";
            }

            return visibleObjects.Count > 0
                ? "You notice lingering traces here."
                : "You see empty ground.";
        }

        private static string BuildContentsLine(List<Entity> visibleObjects)
        {
            if (visibleObjects == null || visibleObjects.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("Contents: ");
            for (int i = 0; i < visibleObjects.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(visibleObjects[i].GetDisplayName());
            }

            return sb.ToString();
        }

        private static string BuildPrimaryDetail(Entity player, Entity primary)
        {
            if (primary == null)
                return string.Empty;

            List<string> parts = new List<string>();

            if (primary.HasTag("Creature"))
            {
                Stat hp = primary.GetStat("Hitpoints");
                if (hp != null)
                    parts.Add("HP " + hp.Value + "/" + hp.Max);

                if (player != null && primary != player)
                    parts.Add(GetRelationLabel(player, primary));
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
        }

        private static string BuildFlagsLine(Cell cell)
        {
            if (cell == null)
                return string.Empty;

            bool solid = false;
            bool container = false;
            bool stairs = false;
            bool takeable = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity entity = cell.Objects[i];
                if (entity == null)
                    continue;

                if (entity.HasTag("Solid"))
                    solid = true;
                if (entity.GetPart<ContainerPart>() != null)
                    container = true;
                if (entity.GetPart<StairsDownPart>() != null || entity.GetPart<StairsUpPart>() != null)
                    stairs = true;

                PhysicsPart physics = entity.GetPart<PhysicsPart>();
                if (physics != null && physics.Takeable)
                    takeable = true;
            }

            List<string> flags = new List<string>();
            if (solid) flags.Add("solid");
            if (container) flags.Add("container");
            if (stairs) flags.Add("stairs");
            if (takeable) flags.Add("takeable");

            if (flags.Count == 0)
                return string.Empty;

            return "Flags: " + string.Join(", ", flags);
        }

        private static string GetRelationLabel(Entity player, Entity target)
        {
            if (player == null || target == null)
                return "neutral";

            // NoFightGoal (from M2.2 CalmMutation, M2.1 dialogue action, or any
            // future pacification source) takes precedence over the underlying
            // faction stance: the target is behaviorally non-hostile for the
            // goal's duration. Showing "hostile" on a pacified Snapjaw was the
            // reported bug — the player just cast Calm on it and can visually
            // confirm it's no longer pursuing, yet the readout claimed hostile.
            var brain = target.GetPart<BrainPart>();
            if (brain != null && brain.HasGoal<NoFightGoal>())
                return "pacified";

            if (FactionManager.IsHostile(player, target))
                return "hostile";
            if (FactionManager.IsAllied(player, target) || player == target)
                return "friendly";
            return "neutral";
        }
    }
}
