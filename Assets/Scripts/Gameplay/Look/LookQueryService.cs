using System.Collections.Generic;
using System.Text;

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

            return new LookSnapshot(x, y, header, summary, details, primary, cell);
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
