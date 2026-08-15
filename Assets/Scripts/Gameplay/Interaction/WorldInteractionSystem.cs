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
            return GatherActions(target, null);
        }

        /// <summary>
        /// Actor-aware overload (M3-L3): when <paramref name="actor"/> is
        /// non-null it rides the event as the "Actor" parameter, letting
        /// listeners declare actor-contextual rows (the crafting stations use
        /// this to offer in-menu mix/kit toggles for the actor's carried
        /// items). Declaring parts must tolerate a missing Actor — the
        /// single-arg form remains actor-less.
        /// </summary>
        public static List<InventoryAction> GatherActions(Entity target, Entity actor)
        {
            if (target == null) return new List<InventoryAction>(0);

            var list = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", list);
            if (actor != null)
                e.SetParameter("Actor", (object)actor);
            // Listeners populate `list` directly; event object is safe to release.
            target.FireEventAndRelease(e);
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
        /// <summary>
        /// Zone-aware overload: whatever the cell names, the GROUND
        /// speaks too (Jet Blast water, oil, embers) — the 'c' menu on a
        /// visibly wet tile saying nothing about the water was half of
        /// the bug that triggered the status-system study.
        ///
        /// <para>The suffix applies on EVERY branch, not just empty
        /// cells: terrain (grass, floor) exists on nearly every cell in
        /// the shipped world, so an empty-only append never fires in
        /// real play — caught live on the first playtest ("You see the
        /// grass." over visibly blue water).</para>
        /// </summary>
        public static string DescribeCell(Cell cell, Zone zone)
        {
            string baseText = DescribeCell(cell);
            if (zone == null || cell == null)
                return baseText;

            string ground = CellStatusReadout.GroundSummary(zone, cell, cell.X, cell.Y);
            if (string.IsNullOrEmpty(ground))
                return baseText;

            if (baseText == "You see nothing here.")
                return "You see " + ground + " on the ground.";
            return baseText + " (" + ground + " underfoot)";
        }

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
        /// Command prefix for the look-mode "what's here" picker rows; the
        /// suffix is the target entity's ID. InputHandler intercepts these
        /// and reopens the menu showing THAT entity's actions.
        /// </summary>
        public const string PickTargetCommandPrefix = "PickTarget:";

        /// <summary>Command that reopens the "what's here" picker.</summary>
        public const string PickCellCommand = "PickCell";

        /// <summary>
        /// Command that opens the pile's takeable items in the pickup list —
        /// the persistent one, where taking an item leaves the rest on
        /// screen. This is what a loot pile is FOR, so it is the row the
        /// pile menu leads with.
        /// </summary>
        public const string ViewPileCommand = "ViewPile";

        /// <summary>
        /// Build the "what's here" picker for a clicked cell: one row per
        /// object, non-terrain first (top-most leading, matching what the
        /// player visually clicked), terrain (the floor) last so it stays
        /// reachable. Each row's command is PickTarget:&lt;ID&gt;.
        /// </summary>
        /// <summary>
        /// The menu a PILE cell opens with: what you can do about the pile
        /// as a whole, rather than a wall of one-row-per-object.
        ///
        /// <para>Ordered by what a player actually wants from a heap of
        /// loot: take the items, look at them, then — for the things that
        /// are not simple loot (a chest, a corpse, a signpost sharing the
        /// cell) — the full per-object picker.</para>
        ///
        /// <para>The "take items" row is omitted when nothing in the cell is
        /// takeable, so a cell holding two pieces of scenery does not offer
        /// an empty list.</para>
        /// </summary>
        public static List<InventoryAction> BuildPileSummaryActions(Cell cell, Entity actor)
        {
            // InventoryActionList, not a bare List — InventoryAction is not
            // IComparable, so List.Sort() would throw. The list type owns the
            // priority/hotkey comparer that GatherActions already uses.
            var rows = new InventoryActionList();
            if (cell == null) return rows.Actions;

            int takeable = InventorySystem.GetTakeableItemsInCell(cell, actor).Count;
            if (takeable > 0)
            {
                rows.AddAction("ViewPile",
                    takeable == 1 ? "take item (1)" : $"take items ({takeable})",
                    ViewPileCommand, 'g', 40);
            }

            rows.AddAction("Examine", "examine", "Examine", 'x', 0);
            rows.AddAction("PickCell", "<< everything here", PickCellCommand, '\0', -1);

            rows.Sort();
            return rows.Actions;
        }

        public static List<InventoryAction> BuildTargetPickerActions(Cell cell)
        {
            var rows = new List<InventoryAction>();
            if (cell == null) return rows;

            for (int pass = 0; pass < 2; pass++)
            {
                bool terrainPass = pass == 1;
                for (int i = cell.Objects.Count - 1; i >= 0; i--)
                {
                    Entity e = cell.Objects[i];
                    if (e == null || string.IsNullOrEmpty(e.ID)) continue;
                    if (IsTerrain(e) != terrainPass) continue;

                    rows.Add(new InventoryAction(
                        "PickTarget", e.GetDisplayName(),
                        PickTargetCommandPrefix + e.ID, '\0', 0));
                }
            }

            return rows;
        }

        /// <summary>Resolve an entity in the cell by its ID (null-safe).</summary>
        public static Entity FindInCell(Cell cell, string id)
        {
            if (cell == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity e = cell.Objects[i];
                if (e != null && e.ID == id)
                    return e;
            }
            return null;
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
