using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>Opt-in tag for saved bookkeeping owners which occupy an
        /// anchor solely for persistence, never a player-facing world object.
        /// Unrendered ordinary owners retain their existing query behavior.</summary>
        public const string WorldMetadataTag = "WorldMetadata";
        private static bool IsQueryable(Entity entity) => entity != null && !entity.HasTag(WorldMetadataTag);
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
        /// Physical occupants can merge anchored entities with multi-cell
        /// owners, so render priority is selected explicitly. Equal layers
        /// retain the last-occupant preference of canonical cell stacks.
        /// </summary>
        public static Entity ResolveTarget(Cell cell)
            => ResolveTarget(cell, null);

        /// <summary>Resolve the world owner beneath the acting entity rather
        /// than selecting that actor as its own interaction target. Only the
        /// exact actor reference is excluded from the cell's physical occupants;
        /// other creatures and Player-tagged owners keep their normal priority.</summary>
        public static Entity ResolveTarget(Cell cell, Entity actor)
        {
            if (cell == null) return null;
            if (cell.Occupants.Count == 0) return null;

            Entity topTerrain = null;
            Entity topObject = null;
            for (int i = 0; i < cell.Occupants.Count; i++)
            {
                var e = cell.Occupants[i];
                if (!IsQueryable(e) || ReferenceEquals(e, actor)) continue;
                if (IsTerrain(e))
                {
                    if (topTerrain == null || RenderLayer(e) >= RenderLayer(topTerrain)) topTerrain = e;
                    continue;
                }
                if (topObject == null || RenderLayer(e) >= RenderLayer(topObject)) topObject = e;
            }
            return topObject ?? topTerrain;
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
            if (!IsQueryable(target)) return new List<InventoryAction>(0);

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

            string ground = CellStatusReadout.GroundSummary(zone, cell);
            if (string.IsNullOrEmpty(ground))
                return baseText;

            if (baseText == "You see nothing here.")
                return "You see " + ground + " on the ground.";
            return baseText + " (" + ground + " underfoot)";
        }

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
            if (cell == null || cell.Occupants.Count == 0)
                return "You see nothing here.";

            var nonTerrain = new List<Entity>();
            Entity topTerrain = null;
            foreach (var e in cell.Occupants)
            {
                if (!IsQueryable(e)) continue;
                // B.1 (Docs/BREADTH-PASS.md): the player is the one looking, never the thing
                // described — a title on your own cell names what is underfoot, not "you".
                if (e.HasTag("Player")) continue;
                if (IsTerrain(e))
                {
                    if (topTerrain == null || RenderLayer(e) >= RenderLayer(topTerrain)) topTerrain = e;
                    continue;
                }
                InsertByRenderLayer(nonTerrain, e);
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
            => IsPileCell(cell, null);

        /// <summary>The acting entity standing on a single world object does
        /// not make that object a pile. Genuine non-terrain stacks still do.</summary>
        public static bool IsPileCell(Cell cell, Entity actor)
        {
            if (cell == null) return false;
            int count = 0;
            foreach (var e in cell.Occupants)
            {
                if (!IsQueryable(e) || ReferenceEquals(e, actor)) continue;
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
            AppendUnderfootActions(rows.Actions, cell, actor);
            AppendToolActions(rows.Actions, cell, actor);
            return rows.Actions;
        }

        /// <summary>Expose explicitly marked underfoot terrain and existing-save beds without
        /// replacing the actor or loot target. Only the actor's occupied cell
        /// qualifies. Picker execution re-resolves the entity by its ID.</summary>
        public static void AppendUnderfootActions(List<InventoryAction> rows, Cell cell, Entity actor, Entity target = null)
        {
            if (rows == null || cell == null || actor == null || !cell.Occupants.Contains(actor)) return;
            foreach (var e in cell.Occupants)
                if (IsQueryable(e) && e != target && (e.HasTag("UnderfootInteractable") || e.HasPart<BedPart>()) && !string.IsNullOrEmpty(e.ID))
                    rows.Add(new InventoryAction("Underfoot", e.GetDisplayName() + " underfoot",
                        PickTargetCommandPrefix + e.ID, '\0', 0));
        }

        /// <summary>Adds the "light it" row for a cell beside the actor where a spark would catch (an oil slick), run by the torch or flint striker the actor carries, so lighting never
        /// needs a trip through the pack menu. Nothing is added for any other cell.</summary>
        public static void AppendIgnitionActions(List<InventoryAction> rows, Cell cell, Entity actor) => FireStarting.AddCellRows(rows, cell, actor);

        /// <summary>
        /// The event a carried tool answers to offer rows for one cell that its pack rows do not already tag (Actor, Zone, Cell, Actions). A tool whose pack rows already name the cell they act on does not need it: it tags
        /// those rows (<see cref="InventoryAction.AtCell"/>) and <see cref="AppendToolActions"/> reads them. A tool that works on whatever is in the cell (a scraper on a vent) answers this event for the cell asked about.
        /// </summary>
        public const string WorldCellActionsEvent = "GetWorldCellActions";

        /// <summary>
        /// Adds what the tools the actor carries (or holds) can do to <paramref name="cell"/>, so using a tool on the environment never needs the pack opened: the rows the pack menu would show for each item that act on this
        /// cell, plus any the item offers for it through <see cref="WorldCellActionsEvent"/>. Each row is run by the item that offered it (<see cref="InventoryAction.Via"/>) with the command and checks the pack row has;
        /// a row that declares no turn cost costs one action, as the pack charges for these; hotkeys are dropped so none can clash with the menu's own; a label that does not say which item is told which; identical rows from identical items collapse to one; and rows come
        /// after the cell's own, most prominent first, so the first row of a menu is never a tool. Rows under one heading (<see cref="InventoryAction.Group"/>) collapse into a single row that opens them once there
        /// are <see cref="ToolGroupAt"/> of them, so a loaded pack gives a short menu. Nothing is added for an actor who cannot act, and nothing about the actor, pack or world changes by asking.
        /// </summary>
        public static void AppendToolActions(List<InventoryAction> rows, Cell cell, Entity actor)
        {
            if (rows == null) return;
            var offered = CollectToolRows(cell, actor); if (offered == null) return;
            // Rows that are the same kind of work (a heading) collapse into one row that opens them once there are enough to make a wall; a row with no heading, or a heading with few rows, stays where it is.
            var crowded = offered.Where(o => !string.IsNullOrEmpty(o.row.Group)).GroupBy(o => o.row.Group).Where(g => g.Count() >= ToolGroupAt).ToDictionary(g => g.Key, g => g.ToList());
            var opened = new HashSet<string>();
            foreach (var (row, _) in offered)
            {
                if (!string.IsNullOrEmpty(row.Group) && crowded.TryGetValue(row.Group, out var members)) { if (opened.Add(row.Group)) rows.Add(GroupRow(row.Group, members)); continue; }
                rows.Add(row);
            }
        }

        /// <summary>How many rows under one heading it takes before the menu shows one row that opens them instead of the rows themselves.</summary>
        public const int ToolGroupAt = 3;

        /// <summary>The command of a grouped row (this prefix and the heading), which the input handler turns into the list of that group's rows; and of that list's "back" row.</summary>
        public const string ToolGroupPrefix = "ToolGroup|", ToolGroupBackPrefix = "ToolGroupBack|";

        /// <summary>Whether a command is a grouped row or the back row of its list: menu navigation, not an action on the world.</summary>
        public static bool IsToolGroupCommand(string command) => command != null
            && (command.StartsWith(ToolGroupPrefix, StringComparison.Ordinal) || command.StartsWith(ToolGroupBackPrefix, StringComparison.Ordinal));

        /// <summary>The heading a grouped row or its back row names; null for any other command.</summary>
        public static string ToolGroupHeading(string command) => IsToolGroupCommand(command) ? command.Substring(command.IndexOf('|') + 1) : null;

        /// <summary>What the opened list is titled: its heading, capitalised.</summary>
        public static string ToolGroupTitle(string heading) => string.IsNullOrEmpty(heading) ? "" : char.ToUpperInvariant(heading[0]) + heading.Substring(1);

        /// <summary>
        /// The rows a grouped row opens: every tool row of the cell under that heading, in menu order, then a row that goes back. Built from the same collection as the menu itself, so the list always agrees with the
        /// count the grouped row showed, and a row that is no longer possible is simply not in it.
        /// </summary>
        public static List<InventoryAction> BuildToolGroupActions(Cell cell, Entity actor, string heading)
        {
            var result = new List<InventoryAction>(); var offered = CollectToolRows(cell, actor);
            if (offered != null) foreach (var (row, _) in offered) if (row.Group == heading) result.Add(row);
            result.Add(new InventoryAction("ToolGroupBack", "<< back", ToolGroupBackPrefix + heading, '\0', 0));
            return result;
        }

        static InventoryAction GroupRow(string heading, List<(InventoryAction row, Entity item)> members)
        {
            var names = members.Select(m => ToolName(m.item)).Distinct().ToList();
            string hint = "Choose from: " + string.Join(", ", names.Take(4)) + (names.Count > 4 ? " and " + (names.Count - 4) + " more" : "") + ".";
            return new InventoryAction("ToolGroup", heading + " (" + members.Count + ") >>", ToolGroupPrefix + heading, '\0', members.Max(m => m.row.Priority)) { Hint = hint };
        }

        /// <summary>The finished, ordered tool rows of one cell (each row run by the item that offered it, named, told apart by what its item holds, duplicates collapsed), or null when there are none.</summary>
        static List<(InventoryAction row, Entity item)> CollectToolRows(Cell cell, Entity actor)
        {
            var zone = cell?.ParentZone; var pack = actor?.GetPart<InventoryPart>();
            if (zone == null || pack == null || !WorldResourceActions.ActorCurrent(actor, zone)) return null;
            var offered = new List<(InventoryAction row, Entity item)>();
            foreach (var item in pack.Objects.Concat(pack.EquippedItems.Values).Where(i => i != null).Distinct().ToArray())
            {
                foreach (var row in InventorySystem.GetActions(actor, item)) if (row != null && !row.FireOnActor && row.ActsOnCell(cell.X, cell.Y)) offered.Add((row, item));
                var list = new InventoryActionList(); var e = GameEvent.New(WorldCellActionsEvent);
                e.SetParameter("Actor", (object)actor); e.SetParameter("Zone", (object)zone); e.SetParameter("Cell", (object)cell); e.SetParameter("Actions", (object)list);
                item.FireEventAndRelease(e);
                foreach (var row in list.Actions) if (row != null && !row.FireOnActor) offered.Add((row, item));
            }
            if (offered.Count == 0) return null;
            foreach (var (row, item) in offered)
            {
                row.Via ??= item; row.Key = '\0';
                if (row.Time == ActionTime.Unspecified && !row.IsDisabled) row.Time = ActionTime.OneAction;
                string name = ToolName(item);
                if (!row.Shared && (row.Display == null || row.Display.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)) row.Display = (row.Display ?? "") + " [" + name + "]";
            }
            offered.Sort((a, b) =>
            {
                int c = b.row.Priority.CompareTo(a.row.Priority); if (c != 0) return c;
                c = string.CompareOrdinal(a.row.Display, b.row.Display); if (c != 0) return c;
                c = a.row.IsDisabled.CompareTo(b.row.IsDisabled); if (c != 0) return c;   // of two rows that read alike, the one you can use is the one kept
                return string.CompareOrdinal(a.item.ID, b.item.ID);
            });
            // An empty flask and a full one read alike ("fill with water (11,10) [liquid flask]") but do different things: where items of the same name differ in what they hold, each row says what its item holds.
            foreach (var group in offered.Where(o => !o.row.Shared).GroupBy(o => o.row.Display).ToList())
            {
                if (group.Select(o => ToolState(o.item)).Distinct().Count() < 2) continue;
                foreach (var o in group) { string state = ToolState(o.item); if (state.Length > 0) o.row.Display += " (" + state + ")"; }
            }
            // Four flasks of the same mire all read "pour bog mire east (12) [liquid flask]": the same choice four times. Rows that read alike collapse to the first (lowest id), so a full pack does not multiply a menu. A shared
            // row ("make camp here") is offered by every kit and is one choice whichever runs it.
            var seen = new HashSet<string>(); var kept = new List<(InventoryAction row, Entity item)>();
            foreach (var pair in offered) if (seen.Add(pair.row.Shared ? "shared|" + pair.row.Name + "|" + pair.row.Display : pair.row.Display)) kept.Add(pair);
            return kept;
        }

        /// <summary>What an item holds, when that decides what its rows do: what a flask or waterskin contains (<see cref="IToolMenuState"/>) and how many are in a stack ("x3"), or an empty string.</summary>
        static string ToolState(Entity item)
        {
            string state = "";
            foreach (var part in item.Parts) if (part is IToolMenuState held) { string text = held.ToolMenuState(); if (!string.IsNullOrEmpty(text)) { state = text; break; } }
            int count = item.GetPart<StackerPart>()?.StackCount ?? 1;
            return count > 1 ? (state.Length == 0 ? "x" + count : state + ", x" + count) : state;
        }

        /// <summary>What a tool is called in a menu row: its plain name, without a stack count or a charge counter.</summary>
        static string ToolName(Entity item) => item.GetPart<RenderPart>()?.DisplayName is string name && name.Length > 0 ? name : item.BlueprintName ?? "tool";

        /// <summary>
        /// The menu the interact (C) key opens for one cell's target: the target's own rows, then the rows that belong to the cell and the actor rather than to any one object there (what is underfoot, lighting an oil slick,
        /// what the tools you carry can do to the cell). The input handler builds the live menu only through this, so a row added here reaches the player and a row added anywhere else does not.
        /// </summary>
        public static List<InventoryAction> GatherCellActions(Entity target, Cell cell, Entity actor)
        {
            var actions = GatherActions(target, actor);
            AppendUnderfootActions(actions, cell, actor, target);
            AppendIgnitionActions(actions, cell, actor);
            AppendToolActions(actions, cell, actor);
            return actions;
        }

        public static List<InventoryAction> BuildTargetPickerActions(Cell cell)
            => BuildTargetPickerActions(cell, null);

        /// <summary>Keep every selectable world owner, including terrain, but
        /// omit the acting entity when it physically occupies this cell.</summary>
        public static List<InventoryAction> BuildTargetPickerActions(Cell cell, Entity actor)
        {
            var rows = new List<InventoryAction>();
            if (cell == null) return rows;

            var ordered = new List<Entity>();
            foreach (var entity in cell.Occupants)
                if (IsQueryable(entity) && !ReferenceEquals(entity, actor)) InsertByRenderLayer(ordered, entity);

            for (int pass = 0; pass < 2; pass++)
            {
                bool terrainPass = pass == 1;
                for (int i = ordered.Count - 1; i >= 0; i--)
                {
                    Entity e = ordered[i];
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
            for (int i = 0; i < cell.Occupants.Count; i++)
            {
                Entity e = cell.Occupants[i];
                if (IsQueryable(e) && e.ID == id)
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

        private static int RenderLayer(Entity entity) => entity.GetPart<RenderPart>()?.RenderLayer ?? 0;

        private static void InsertByRenderLayer(List<Entity> ordered, Entity entity)
        {
            int layer = RenderLayer(entity);
            int index = ordered.Count;
            while (index > 0 && RenderLayer(ordered[index - 1]) > layer) index--;
            ordered.Insert(index, entity);
        }

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
