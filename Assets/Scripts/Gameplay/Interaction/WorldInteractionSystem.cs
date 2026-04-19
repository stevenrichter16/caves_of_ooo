using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Phase 4c of the world action menu plan — dispatcher / query layer for
    /// interactions in a zone cell. The UI (Phase 4d) asks this system:
    ///
    ///   1. "The player clicked cell (x, y). What's the interaction target?"
    ///      → <see cref="ResolveTarget"/>
    ///   2. "What actions can be performed on that target?"
    ///      → <see cref="GatherActions"/>
    ///   3. "Describe this cell in one line." (for the pile case or previews)
    ///      → <see cref="DescribeCell"/>
    ///
    /// Pure functions throughout — no logging or side effects. Safe to call
    /// in previews, tool-tips, or any context where you want to *peek* at
    /// cell state without firing events. Action execution (with its
    /// MessageLog side effects) is the caller's responsibility via the
    /// <c>InventoryAction</c> event.
    ///
    /// Target-resolution rule: top render-layer non-terrain entity wins. If
    /// the cell has only terrain (Wall / Floor / Terrain-tagged), the top
    /// terrain entity is the target. An empty cell returns null.
    /// </summary>
    public static class WorldInteractionSystem
    {
        // =========================================================
        // Target resolution
        // =========================================================

        /// <summary>
        /// Pick the most interactable entity in a cell.
        ///
        /// Rule (in order):
        ///   - Highest render-layer entity that is NOT terrain-tagged
        ///   - Otherwise, the highest render-layer terrain entity
        ///   - Otherwise (empty cell or null), null
        ///
        /// Cell.Objects is stored ascending by render layer, so this method
        /// iterates top-down (<c>i = Count - 1</c> down to <c>0</c>) to find
        /// the visual-top entity first.
        /// </summary>
        public static Entity ResolveTarget(Cell cell)
        {
            if (cell == null) return null;
            if (cell.Objects.Count == 0) return null;

            Entity topTerrain = null;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                var e = cell.Objects[i];
                if (e == null) continue;
                if (IsTerrain(e))
                {
                    if (topTerrain == null) topTerrain = e;
                    continue;
                }
                return e; // highest-layer non-terrain wins immediately
            }
            return topTerrain; // only terrain in the cell
        }

        // =========================================================
        // Action gathering
        // =========================================================

        /// <summary>
        /// Fire the <c>GetInventoryActions</c> event on <paramref name="target"/>,
        /// collect whatever actions its parts declare, sort by priority, and
        /// return the list.
        ///
        /// Returns an empty list (never null) for null targets so callers can
        /// safely iterate without a null check.
        /// </summary>
        public static List<InventoryAction> GatherActions(Entity target)
        {
            if (target == null) return new List<InventoryAction>(0);

            var list = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", list);
            target.FireEvent(e);
            list.Sort();
            return list.Actions;
        }

        // =========================================================
        // Cell-level description (for the "pile of items" case)
        // =========================================================

        /// <summary>
        /// Human-readable one-line description of what a player sees in a cell.
        ///   - 2+ non-terrain entities → "A pile of items, including: a, b, c."
        ///   - 1 non-terrain entity    → "You see a {name}."
        ///   - Only terrain            → "You see the {top terrain name}."
        ///   - Empty or null cell      → "You see nothing here."
        ///
        /// Pure function. No MessageLog side effects — use this for previews,
        /// tooltips, and the pile-summary fast path. For the side-effecting
        /// Examine command, fire an <c>InventoryAction</c> event with
        /// <c>Command = "Examine"</c>; ExaminablePart will log there.
        /// </summary>
        public static string DescribeCell(Cell cell)
        {
            if (cell == null || cell.Objects.Count == 0)
                return "You see nothing here.";

            var nonTerrain = new List<Entity>();
            Entity topTerrain = null;
            foreach (var e in cell.Objects)
            {
                if (e == null) continue;
                if (IsTerrain(e)) { topTerrain = e; continue; }
                nonTerrain.Add(e);
            }

            if (nonTerrain.Count >= 2)
            {
                var sb = new StringBuilder("A pile of items, including: ");
                for (int i = 0; i < nonTerrain.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(nonTerrain[i].GetDisplayName());
                }
                sb.Append('.');
                return sb.ToString();
            }

            if (nonTerrain.Count == 1)
            {
                string name = nonTerrain[0].GetDisplayName();
                return $"You see {GetArticle(name)}{name}.";
            }

            if (topTerrain != null)
                return $"You see the {topTerrain.GetDisplayName()}.";

            return "You see nothing here.";
        }

        // =========================================================
        // Predicates
        // =========================================================

        /// <summary>
        /// True if the cell has 2 or more non-terrain entities (the "pile"
        /// case). The UI uses this to decide whether to present a multi-item
        /// summary instead of a single-target Examine.
        /// </summary>
        public static bool IsPileCell(Cell cell)
        {
            if (cell == null) return false;
            int count = 0;
            foreach (var e in cell.Objects)
            {
                if (e == null) continue;
                if (!IsTerrain(e)) count++;
            }
            return count >= 2;
        }

        /// <summary>
        /// True for entities carrying the <c>Wall</c>, <c>Floor</c>, or
        /// <c>Terrain</c> tag. Matches the convention already used by
        /// ZoneBuilder for its cell-clearing safety rail.
        /// </summary>
        public static bool IsTerrain(Entity entity)
        {
            if (entity == null) return false;
            return entity.HasTag("Wall")
                || entity.HasTag("Floor")
                || entity.HasTag("Terrain");
        }

        // =========================================================
        // Private helpers
        // =========================================================

        /// <summary>
        /// Article selection — "a ", "an ", or "" for proper nouns / names
        /// that already carry a determiner. Duplicated from ExaminablePart
        /// rather than extracted to a shared utility because the two callers
        /// are so few. If a third caller emerges, factor out.
        /// </summary>
        private static string GetArticle(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (char.IsUpper(name[0])) return "";

            string lower = name.ToLowerInvariant();
            if (lower.StartsWith("a ") || lower.StartsWith("an ") ||
                lower.StartsWith("the ") || lower.StartsWith("some ") ||
                lower.StartsWith("your ") || lower.StartsWith("his ") ||
                lower.StartsWith("her ") || lower.StartsWith("their "))
                return "";

            char first = char.ToLowerInvariant(name[0]);
            bool vowel = first == 'a' || first == 'e' || first == 'i' ||
                         first == 'o' || first == 'u';
            return vowel ? "an " : "a ";
        }
    }
}
