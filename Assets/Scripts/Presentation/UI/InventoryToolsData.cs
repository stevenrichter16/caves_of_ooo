using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read model for the Tools tab. Its quotes belong to the same
    /// physical tools, workpieces and world action owners as the ordinary menus.</summary>
    internal static class InventoryToolsData
    {
        internal sealed class Entry
        {
            public Entity Item { get; }
            public InventoryScreenData.ItemDisplay Display { get; }
            public Cell Cell { get; }
            public Entity StorageOwner { get; }
            public string Name { get; }
            public string Location { get; }
            public IReadOnlyList<string> Guidance { get; }
            public IReadOnlyList<Operation> Operations { get; }
            public bool HasRecipes { get; }

            public Entry(Entity item, InventoryScreenData.ItemDisplay display, Cell cell, Entity storageOwner,
                string location, IReadOnlyList<string> guidance, IReadOnlyList<Operation> operations, bool hasRecipes)
            {
                Item = item;
                Display = display;
                Cell = cell;
                StorageOwner = storageOwner;
                Name = InventoryPart.GetUnitDisplayName(item);
                Location = location;
                Guidance = guidance;
                Operations = operations;
                HasRecipes = hasRecipes;
            }
        }

        internal sealed class Operation
        {
            public string Title { get; }
            public string Detail { get; }
            public bool IsReady { get; }
            public bool IsSetup { get; }
            public Entity Workpiece { get; }
            public string Command { get; }

            public Operation(InventoryAction action, Entity workpiece = null)
            {
                string title = action.RecipeTitle ?? action.Display;
                Title = workpiece == null ? title : title + " from " + InventoryPart.GetUnitDisplayName(workpiece);
                IsReady = !action.IsDisabled;
                IsSetup = HeldFixtureActions.IsCommand(action.Command)
                    || action.Name is "SetDownFixture" or "MakeCamp" or "MoveFixture"
                    || action.Command == RepairablePart.RepairCommand;
                Workpiece = workpiece;
                Command = action.Command;
                Detail = action.IsDisabled ? action.DisabledReason : FirstText(action.Hint, action.RecipeInstructions,
                    action.RecipeResult, action.Time == ActionTime.OneAction ? "Takes one action." : null);
            }
        }

        internal static IReadOnlyList<Entry> Build(Entity actor, Zone zone, InventoryScreenData.ScreenState state)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (pack == null || pack.ParentEntity != actor || state == null) return Array.Empty<Entry>();

            var owned = state.Categories.SelectMany(category => category.Items)
                .Where(display => InventoryScreenData.CanReadState(actor, display.Item))
                .ToArray();
            var workByTool = IndexWorkpieceRecipes(owned);
            var entries = new List<Entry>();
            var seen = new HashSet<Entity>();
            foreach (var display in owned)
            {
                var item = display.Item;
                if (IsTool(item) && seen.Add(item))
                {
                    workByTool.TryGetValue(item, out var work);
                    var operations = OwnOperations(display.Actions);
                    if (work != null) operations.AddRange(work);
                    string location = display.IsEquipped ? "Equipped" : "In pack";
                    entries.Add(new Entry(item, display, null, null, location,
                        Describe(item, zone, null, actor, display.State), operations, WorkStartsAtMaterials(item) || work?.Count > 0));
                }
            }

            foreach (var display in owned)
            {
                var vessel = display.Item;
                if (!CarriedStorage.TryBrowse(actor, vessel, out var storage)) continue;
                foreach (var item in storage.Contents)
                {
                    if (!IsTool(item) || !seen.Add(item)) continue;
                    string location = "Stowed in " + InventoryPart.GetUnitDisplayName(vessel);
                    var guidance = new[] { "Take it into your pack to see and use its available operations.", Description(item) };
                    entries.Add(new Entry(item, null, null, vessel, location, guidance, Array.Empty<Operation>(), false));
                }
            }

            if (WorldResourceActions.ActorCurrent(actor, zone))
            {
                var origin = zone.GetEntityCell(actor);
                for (int y = origin.Y - 1; y <= origin.Y + 1; y++)
                    for (int x = origin.X - 1; x <= origin.X + 1; x++)
                    {
                        var cell = zone.GetCell(x, y);
                        if (!CellStatusReadout.IsDisclosedCell(zone, cell)) continue;
                        foreach (var item in cell.Objects)
                        {
                            if (!CanInspectNearbyTool(actor, zone, item, cell) || !seen.Add(item)) continue;
                            var operations = OwnOperations(WorldOperations(item, actor, zone));
                            var location = HeldFixtureActions.IsPlayerHeld(item) ? "Placed nearby" : "Nearby station";
                            entries.Add(new Entry(item, null, cell, null, $"{location} ({x}, {y})",
                                Describe(item, zone, cell, actor, null), operations, false));
                        }
                    }
            }

            return entries.OrderBy(entry => entry.Cell != null ? 2 : entry.StorageOwner != null ? 1 : 0)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Item.ID, StringComparer.Ordinal).ToArray();
        }

        internal static bool CanInspectNearbyTool(Entity actor, Zone zone, Entity item, Cell cell)
            => IsInstalledTool(item) && CellStatusReadout.IsDisclosedCell(zone, cell)
                && WorldResourceActions.Nearby(actor, item, zone) && zone.GetEntityCell(item) == cell
                && !PackableFixturePart.IsInertKit(item) && !item.HasTag(WorldInteractionSystem.WorldMetadataTag)
                && item.GetPart<RenderPart>() is { Visible: true } render && render.ParentEntity == item;

        static bool IsTool(Entity item)
        {
            if (item == null || item.HasTag("Creature") || item.HasTag("Terrain")) return false;
            return ItemCategory.GetCategory(item) == ItemCategory.Tools || IsInstalledTool(item)
                || item.HasPart<LiquidVesselPart>() || item.HasPart<WaterskinPart>()
                || item.HasPart<FlintStrikerPart>() || item.HasPart<TorchLightPart>()
                || item.GetPart<CuttingToolPart>() is { WoodStock: true } cutter && cutter.ParentEntity == item
                || WorkStartsAtMaterials(item);
        }

        static bool IsInstalledTool(Entity item)
            => item != null && !item.HasTag("Creature") && !item.HasTag("Terrain")
                && item.Parts.Any(part => part.ParentEntity == item && (part is PackableFixturePart || part is IStationCapability));

        static bool WorkStartsAtMaterials(Entity item)
            => item.GetPart<ScreeningToolPart>() is { } sieve && sieve.ParentEntity == item
                || item.GetPart<CuttingToolPart>() is { FlexibleStock: true } blade && blade.ParentEntity == item
                || item.GetPart<DirectedAirPart>() is { } bellows && bellows.ParentEntity == item;

        static Dictionary<Entity, List<Operation>> IndexWorkpieceRecipes(IEnumerable<InventoryScreenData.ItemDisplay> owned)
        {
            var result = new Dictionary<Entity, List<Operation>>();
            foreach (var display in owned)
            {
                if (display.Actions == null) continue;
                foreach (var action in display.Actions)
                {
                    if (action == null || action.IsDisabled || action.RecipeCategory == PackRecipeCategory.None) continue;
                    if (action.RecipeTool == null && action.RecipeAssistTool == null) continue;
                    var operation = new Operation(action, display.Item);
                    Add(action.RecipeTool, operation);
                    if (action.RecipeAssistTool != action.RecipeTool) Add(action.RecipeAssistTool, operation);
                }
            }
            return result;

            void Add(Entity tool, Operation operation)
            {
                if (tool == null) return;
                if (!result.TryGetValue(tool, out var operations))
                    result.Add(tool, operations = new List<Operation>());
                operations.Add(operation);
            }
        }

        static List<Operation> OwnOperations(List<InventoryAction> actions)
        {
            var operations = new List<Operation>();
            if (actions == null) return operations;
            var seen = new HashSet<(string, string, bool)>();
            foreach (var action in actions)
            {
                if (action == null || action.Via != null || string.IsNullOrWhiteSpace(action.Display)
                    || action.Command == InventoryAction.InertCommand || IsOrdinaryHandling(action.Name)
                    || action.Command?.StartsWith(CraftingMarkPart.ToggleCommandPrefix, StringComparison.Ordinal) == true) continue;
                var operation = new Operation(action);
                if (seen.Add((operation.Title, operation.Detail, operation.IsReady))) operations.Add(operation);
            }
            return operations;
        }

        static bool IsOrdinaryHandling(string name)
            => name is "Look" or "Examine" or "Inspect" or "Take" or "Drop" or "Throw" or "Equip" or "Unequip";

        static List<InventoryAction> WorldOperations(Entity item, Entity actor, Zone zone)
        {
            var actions = WorldInteractionSystem.GatherActions(item, actor);
            var readiness = new InventoryActionList();
            // The world menu leaves these buttons present so the player can
            // assemble a pick. The station's reader owns their readiness quote.
            var still = item.GetPart<AlchemyStillPart>();
            if (still?.ParentEntity == item)
            {
                actions.RemoveAll(action => action.Command is "BrewMix" or "BrewMixBatch");
                still.AddUseRows(actor, zone, readiness);
            }
            var forge = item.GetPart<ForgePart>();
            if (forge?.ParentEntity == item)
            {
                actions.RemoveAll(action => action.Command == "CraftKit");
                forge.AddUseRows(actor, zone, readiness);
            }
            actions.AddRange(readiness.Actions);
            return actions;
        }

        static IReadOnlyList<string> Describe(Entity item, Zone zone, Cell cell, Entity actor, string state)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(state)) lines.Add(state);
            if (cell != null)
            {
                StateReader.Collect(item, false, zone, cell, actor, lines);
                StateReader.CollectMenuStatus(item, zone, cell, actor, lines);
            }
            if (!UseGuides.Collect(item, zone, cell, actor, lines))
            {
                string description = Description(item);
                if (!string.IsNullOrWhiteSpace(description)) lines.Add(description);
            }
            return lines;
        }

        static string Description(Entity item) => item.GetPart<ExaminablePart>()?.Text ?? "";

        static string FirstText(params string[] options)
            => options.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? "";
    }
}
