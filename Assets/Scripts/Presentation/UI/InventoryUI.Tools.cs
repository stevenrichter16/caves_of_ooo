using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        private const int ToolListStart = 192;
        private const int ToolListEnd = 674;
        private const int ToolRowHeight = 35;
        private const int ToolHeadingHeight = 22;
        private const int ToolMaxRows = (ToolListEnd - ToolListStart - ToolHeadingHeight) / ToolRowHeight;
        private const int ToolDetailLinesPerPage = 15;
        private const int ToolDetailGlyphHeight = 18;
        private const int ToolDetailLineHeight = 22;
        private static readonly Rect ToolListPane = new Rect(24, 132, 488, 572);
        private static readonly Rect ToolDetailPane = new Rect(528, 132, 728, 572);
        private static readonly Rect ToolDetailText = new Rect(546, 250, 692, ToolDetailLinesPerPage * ToolDetailLineHeight);
        private readonly List<ToolDetailLine> _toolDetails = new List<ToolDetailLine>();
        private ToolSelection[] _toolEntries = Array.Empty<ToolSelection>();
        private InventoryScreenData.ScreenState _toolState;
        private Entity _toolActor;
        private Zone _toolZone;
        private Entity _selectedTool;
        private int _toolCursorIndex, _toolScroll, _toolDetailPage;
        private bool _toolsBuilt, _toolsUngrouped;

        private readonly struct ToolDetailLine
        {
            public readonly string Text;
            public readonly Color Color;
            public ToolDetailLine(string text, Color color) { Text = text; Color = color; }
        }

        private readonly struct ToolSelection
        {
            public readonly InventoryToolsData.Entry Entry;
            public readonly Entity Actor;
            public readonly Zone Zone;
            public readonly string Id;
            public readonly PhysicsPart Physical;
            public readonly ContainerPart Container;
            public readonly long Units;
            public readonly bool OtherCopyHasRecipe;

            public ToolSelection(InventoryToolsData.Entry entry, Entity actor, Zone zone, long units = 1, bool otherCopyHasRecipe = false)
            {
                Entry = entry;
                Actor = actor;
                Zone = zone;
                Id = entry.Item.ID;
                Physical = entry.Item.GetPart<PhysicsPart>();
                Container = entry.StorageOwner?.GetPart<ContainerPart>();
                Units = units;
                OtherCopyHasRecipe = otherCopyHasRecipe;
            }
        }

        private void ResetTools()
        {
            _toolEntries = Array.Empty<ToolSelection>();
            _toolDetails.Clear();
            _toolState = null;
            _toolActor = _selectedTool = null;
            _toolZone = null;
            _toolCursorIndex = _toolScroll = _toolDetailPage = 0;
            _toolsBuilt = false;
        }

        private void EnsureToolsData()
        {
            if (_toolsBuilt && _toolsUngrouped == _inventoryUngrouped
                && ReferenceEquals(_toolState, _state) && ReferenceEquals(_toolActor, PlayerEntity)
                && ReferenceEquals(_toolZone, CurrentZone)) return;

            bool sameContext = ReferenceEquals(_toolActor, PlayerEntity) && ReferenceEquals(_toolZone, CurrentZone);
            Entity preferred = sameContext ? _selectedTool : null;
            int priorIndex = sameContext ? _toolCursorIndex : 0;
            _toolState = _state;
            _toolActor = PlayerEntity;
            _toolZone = CurrentZone;
            _toolsBuilt = true;
            _toolsUngrouped = _inventoryUngrouped;
            var entries = InventoryToolsData.Build(_toolActor, _toolZone, _toolState);
            var carried = entries.Where(entry => entry.Display != null).ToDictionary(entry => entry.Display);
            var groups = InventoryItemGroups.Project(carried.Keys, preferred, !_toolsUngrouped);
            _toolEntries = groups.Select(group => new ToolSelection(carried[group.Selected], _toolActor, _toolZone, group.TotalUnits,
                    group.Members.Any(member => member != group.Selected && QuotedToolRecipe(carried[member]) != null)))
                .Concat(entries.Where(entry => entry.Display == null).Select(entry => new ToolSelection(entry, _toolActor, _toolZone)))
                .OrderBy(tool => tool.Entry.Section)
                .ThenBy(tool => tool.Entry.StorageOwner != null ? 1 : tool.Entry.Cell != null ? 2 : 0)
                .ThenBy(tool => tool.Entry.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(tool => tool.Entry.Item.ID, StringComparer.Ordinal)
                .ToArray();

            int match = Array.FindIndex(_toolEntries, tool => ReferenceEquals(tool.Entry.Item, preferred));
            _toolCursorIndex = match >= 0 ? match : Mathf.Clamp(priorIndex, 0, Mathf.Max(0, _toolEntries.Length - 1));
            _selectedTool = _toolEntries.Length > 0 ? _toolEntries[_toolCursorIndex].Entry.Item : null;
            if (!ReferenceEquals(preferred, _selectedTool)) _toolDetailPage = 0;
            BuildToolDetails();
        }

        private bool TrySelectedTool(out ToolSelection selection)
        {
            // Activation uses the selection the player actually saw. If the
            // owner changed, the action guard refuses before the next rebuild.
            selection = _toolEntries.Length > 0 ? _toolEntries[_toolCursorIndex] : default;
            return _toolEntries.Length > 0;
        }

        private bool SelectToolRow(int index)
        {
            if (index < 0 || index >= _toolEntries.Length || index == _toolCursorIndex) return false;
            _toolCursorIndex = index;
            _selectedTool = _toolEntries[index].Entry.Item;
            _toolDetailPage = 0;
            ClearActionStatus();
            BuildToolDetails();
            return true;
        }

        private void ActivateToolRow(int index)
        {
            if (SelectToolRow(index)) Render();
        }

        private void MoveToolsSelection(int delta)
        {
            EnsureToolsData();
            if (_toolEntries.Length > 0)
                SelectToolRow(Mathf.Clamp(_toolCursorIndex + delta, 0, _toolEntries.Length - 1));
        }

        private void MoveToolDetailsPage(int delta)
        {
            int page = Mathf.Clamp(_toolDetailPage + delta, 0, ToolDetailPageCount - 1);
            if (page == _toolDetailPage) return;
            _toolDetailPage = page;
            Render();
        }

        private void HandleToolsInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.H))
            { MoveToolDetailsPage(-1); return; }
            if (InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.L))
            { MoveToolDetailsPage(1); return; }
            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.E))
            { ActivateToolActions(); return; }
            if (InputHelper.GetKeyDown(KeyCode.R))
            { OpenToolRecipes(); return; }
            if (InputHelper.GetKeyDown(KeyCode.G))
            { ToggleInventoryGrouping(); return; }

            int count = _toolEntries.Length;
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K)) MoveToolsSelection(-1);
            else if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J)) MoveToolsSelection(1);
            else if (InputHelper.GetKeyDown(KeyCode.PageUp)) MoveToolsSelection(_toolScroll - ToolPageEnd(_toolScroll));
            else if (InputHelper.GetKeyDown(KeyCode.PageDown)) MoveToolsSelection(ToolPageEnd(_toolScroll) - _toolScroll);
            else if (InputHelper.GetKeyDown(KeyCode.Home)) SelectToolRow(0);
            else if (InputHelper.GetKeyDown(KeyCode.End)) SelectToolRow(count - 1);
            else return;
            Render();
        }

        private void RenderLedgerTools()
        {
            EnsureToolsData();
            LFrame(ToolListPane, LBorder, LPanel);
            LFrame(ToolDetailPane, LBorder, LPanel);
            LLabel(new Rect(40, 152, 330, 24), "Tools by use", LGold, 24);
            LSecondaryButton(new Rect(384, 145, 114, 28), _inventoryUngrouped ? "G: All copies" : "G: Grouped", LedgerHitKind.PackGrouping, 0);
            LBox(new Rect(36, 181, 464, 1), LBorder);
            if (_toolEntries.Length == 0)
            {
                PaintToolText(new Rect(40, 204, 450, 72), "You have no tools in your pack, in carried storage, or nearby.", LMuted, 20, 3);
                return;
            }

            _toolScroll = Mathf.Clamp(_toolScroll, Mathf.Max(0, _toolCursorIndex - ToolMaxRows + 1), _toolCursorIndex);
            while (ToolPageEnd(_toolScroll) <= _toolCursorIndex) _toolScroll++;
            while (_toolScroll > 0 && ToolPageEnd(_toolScroll - 1) == _toolEntries.Length) _toolScroll--;

            int end = ToolPageEnd(_toolScroll);
            int y = ToolListStart;
            for (int i = _toolScroll; i < end; i++)
            {
                if (ToolStartsSection(i, _toolScroll))
                {
                    RenderToolHeading(_toolEntries[i].Entry.Section, y);
                    y += ToolHeadingHeight;
                }
                RenderToolRow(i, y);
                y += ToolRowHeight;
            }
            if (_toolScroll > 0 || end < _toolEntries.Length)
                LSecondaryRange(new Rect(40, 677, 456, 18), _toolScroll, end - _toolScroll, _toolEntries.Length);

            RenderSelectedTool(_toolEntries[_toolCursorIndex].Entry);
        }

        private bool ToolStartsSection(int index, int first)
            => index == first || _toolEntries[index].Entry.Section != _toolEntries[index - 1].Entry.Section;

        private int ToolPageEnd(int first)
        {
            int i = first;
            int y = ToolListStart;
            for (; i < _toolEntries.Length; i++)
            {
                // Reserve space for the next heading along with its first row.
                y += ToolRowHeight + (ToolStartsSection(i, first) ? ToolHeadingHeight : 0);
                if (y > ToolListEnd) break;
            }
            return i;
        }

        private void RenderToolHeading(InventoryToolsData.ToolSection section, int y)
        {
            LLabel(new Rect(48, y + 1, 442, 18), InventoryToolsData.SectionHeading(section), LGold, 18);
            LBox(new Rect(36, y + ToolHeadingHeight - 2, 464, 1), LBorder);
        }

        private void RenderToolRow(int index, int y)
        {
            var tool = _toolEntries[index];
            var entry = tool.Entry;
            var row = new Rect(36, y, 464, ToolRowHeight - 2);
            bool selected = index == _toolCursorIndex;
            LBox(row, selected ? LSelected : LBackground);
            if (selected) LBox(new Rect(row.x, row.y, 3, row.height), LGold);

            var icon = InventoryItemIcons.For(entry.Item);
            float x = row.x + 8;
            if (icon != null)
            {
                LBox(new Rect(x, row.y + 4, 24, 24), Color.white, icon);
                x += 31;
            }
            float statusX = row.xMax - 124;
            float nameWidth = statusX - x - 8;
            string name = tool.Units > 1 ? entry.Name + " x" + tool.Units : entry.Name;
            var nameColor = selected ? LGold : LInk;
            if (entry.Display?.State is { Length: > 0 } state)
            {
                PaintToolText(new Rect(x, row.y, nameWidth, 21), name, nameColor, 19, 1);
                PaintToolText(new Rect(x, row.y + 18, nameWidth, 14), state, LMuted, 14, 1);
            }
            else
                PaintToolText(new Rect(x, row.y + 5, nameWidth, 24), name, nameColor, 21, 1);
            ToolReadiness(entry, out string status, out Color statusColor);
            PaintToolText(new Rect(statusX, row.y + 1, 116, 16), status, statusColor, 15, 1);
            string location = entry.Cell != null
                ? (HeldFixtureActions.IsPlayerHeld(entry.Item) ? "Placed nearby" : "Nearby station")
                : entry.StorageOwner != null ? InventoryPart.GetUnitDisplayName(entry.StorageOwner)
                : entry.Display.IsEquipped ? "Equipped" : "In pack";
            PaintToolText(new Rect(statusX, row.y + 17, 116, 15), location, LMuted, 14, 1);
            LHit(row, LedgerHitKind.ToolRow, index);
        }

        private void RenderSelectedTool(InventoryToolsData.Entry entry)
        {
            PaintToolText(new Rect(546, 150, 690, 61), entry.Name, LGold, 28, 2);
            PaintToolText(new Rect(546, 217, 500, 19), entry.Location, LMuted, 18, 1);
            ToolReadiness(entry, out string status, out Color statusColor);
            PaintToolText(new Rect(1070, 217, 167, 19), status, statusColor, 18, 1);
            LBox(new Rect(545, 240, 694, 1), LBorder);

            int first = _toolDetailPage * ToolDetailLinesPerPage;
            int count = Math.Min(ToolDetailLinesPerPage, _toolDetails.Count - first);
            for (int i = 0; i < count; i++)
            {
                var line = _toolDetails[first + i];
                if (line.Text.Length == 0) continue;
                var bounds = new Rect(ToolDetailText.x, ToolDetailText.y + i * ToolDetailLineHeight,
                    ToolDetailText.width, ToolDetailLineHeight);
                LLabel(bounds, line.Text, line.Color, ToolDetailGlyphHeight);
            }

            if (ToolDetailPageCount > 1)
            {
                float y = 609;
                if (_toolDetailPage > 0)
                    LSecondaryButton(new Rect(929, y, 110, 32), "Previous", LedgerHitKind.ToolDetailsPage, -1);
                LLabel(new Rect(1053, y + 7, 60, 20), $"{_toolDetailPage + 1}/{ToolDetailPageCount}", LMuted, 16);
                if (_toolDetailPage < ToolDetailPageCount - 1)
                    LSecondaryButton(new Rect(1126, y, 110, 32), "Next", LedgerHitKind.ToolDetailsPage, 1);
            }

            LBox(new Rect(545, 648, 694, 1), LBorder);
            var actionKind = entry.Cell != null ? LedgerHitKind.ToolWork : LedgerHitKind.ToolActions;
            LSecondaryButton(new Rect(546, 658, 204, 32), ToolActionLabel(entry), actionKind, 0);
            if (entry.HasRecipes)
                LSecondaryButton(new Rect(760, 658, 208, 32), ToolRecipeLabel(entry), LedgerHitKind.ToolRecipes, 0);
        }

        private void PaintToolText(Rect bounds, string text, Color color, int glyphHeight, int lines)
        {
            int columns = Math.Max(3, (int)(2 * bounds.width / glyphHeight));
            LLabel(bounds, SkillScreenText.Fit(text, columns, lines), color, glyphHeight);
        }

        private static void ToolReadiness(InventoryToolsData.Entry entry, out string label, out Color color)
        {
            if (entry.StorageOwner != null) { label = "Stowed"; color = LMuted; }
            else if (entry.Operations.Any(operation => operation.IsReady && !operation.IsSetup)) { label = "Ready here"; color = LGreen; }
            else if (PackableFixturePart.IsInertKit(entry.Item))
            {
                label = entry.Operations.Any(operation => operation.IsReady && operation.IsSetup) ? "Can set down" : "Needs setup";
                color = LGold;
            }
            else if (entry.Operations.Any(operation => !operation.IsSetup)) { label = "Needs attention"; color = LGold; }
            else { label = "See needs"; color = LMuted; }
        }

        private int ToolDetailPageCount => Math.Max(1, (_toolDetails.Count + ToolDetailLinesPerPage - 1) / ToolDetailLinesPerPage);

        private void BuildToolDetails()
        {
            _toolDetails.Clear();
            if (_toolEntries.Length == 0) { _toolDetailPage = 0; return; }
            var tool = _toolEntries[_toolCursorIndex];
            var entry = tool.Entry;

            var operations = entry.Operations.OrderBy(operation => operation.IsSetup).ToArray();
            if (tool.OtherCopyHasRecipe && QuotedToolRecipe(entry) == null)
                AddToolDetail("Another copy has a matching recipe. Show all copies to choose it.", LMuted);
            int introduction = Math.Min(2, operations.Length);
            for (int i = 0; i < introduction; i++) AddToolOperation(operations[i]);
            foreach (string guidance in entry.Guidance) AddToolDetail(guidance, LMuted);
            if (introduction < operations.Length) _toolDetails.Add(new ToolDetailLine(string.Empty, LInk));
            for (int i = introduction; i < operations.Length; i++) AddToolOperation(operations[i]);
            while (_toolDetails.Count > 0 && _toolDetails[_toolDetails.Count - 1].Text.Length == 0)
                _toolDetails.RemoveAt(_toolDetails.Count - 1);
            _toolDetailPage = Mathf.Clamp(_toolDetailPage, 0, ToolDetailPageCount - 1);
        }

        private void AddToolDetail(string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            int columns = (int)(2 * ToolDetailText.width / ToolDetailGlyphHeight);
            foreach (string line in SkillScreenText.Wrap(text, columns))
                _toolDetails.Add(new ToolDetailLine(line, color));
        }

        private void AddToolOperation(InventoryToolsData.Operation operation)
        {
            string prefix = operation.IsReady ? operation.IsSetup ? "Setup: " : "Ready: " : "Needs: ";
            AddToolDetail(prefix + operation.Title, operation.IsReady && !operation.IsSetup ? LGreen : LGold);
            AddToolDetail(operation.Detail, LInk);
            _toolDetails.Add(new ToolDetailLine(string.Empty, LInk));
        }

        private static string ToolActionLabel(InventoryToolsData.Entry entry)
            => entry.StorageOwner != null ? "Take to pack" : entry.Cell != null ? "World actions" : "Item actions";

        private static InventoryToolsData.Operation QuotedToolRecipe(InventoryToolsData.Entry entry)
            => entry.Operations.FirstOrDefault(operation => operation.IsReady && operation.Workpiece != null);

        private static string ToolRecipeLabel(InventoryToolsData.Entry entry)
            => QuotedToolRecipe(entry) == null ? "Browse recipes" : "Open matching recipe";

        private void AddToolsCommands()
        {
            if (!TrySelectedTool(out var tool)) return;
            AddLedgerCommand(ToolActionLabel(tool.Entry), () => ActivateToolActions(tool), tool.Entry.Item);
            AddLedgerCommand(_inventoryUngrouped ? "Group identical tools" : "Show all copies", ToggleInventoryGrouping);
            if (tool.Entry.HasRecipes) AddLedgerCommand(ToolRecipeLabel(tool.Entry), () => OpenToolRecipes(tool));
            if (_toolDetailPage > 0) AddLedgerCommand("Previous details page", () => MoveToolDetailsPage(-1));
            if (_toolDetailPage < ToolDetailPageCount - 1) AddLedgerCommand("Next details page", () => MoveToolDetailsPage(1));
        }

        private void OpenToolRecipes()
        {
            if (TrySelectedTool(out var tool)) OpenToolRecipes(tool);
        }

        private void OpenToolRecipes(ToolSelection tool)
        {
            if (!tool.Entry.HasRecipes) return;
            if (!CurrentToolSelection(tool) || !CurrentCarriedTool(tool)) { RejectToolsSelection(); return; }
            var quoted = QuotedToolRecipe(tool.Entry);
            _panel = PANEL_CRAFTING;
            SwitchCraftingMode(CraftingMode.Recipes);
            if (quoted == null) return;

            // A write cannot silently move to a different input or tool if the
            // rebuilt recipe list has lost the original quoted operation.
            int index = _craftRows.FindIndex(row => ReferenceEquals(row.Item, quoted.Workpiece)
                && row.RecipeAction is { IsDisabled: false } recipe && recipe.Command == quoted.Command
                && (ReferenceEquals(recipe.RecipeTool, tool.Entry.Item) || ReferenceEquals(recipe.RecipeAssistTool, tool.Entry.Item)));
            if (index < 0)
            {
                _panel = PANEL_TOOLS;
                SetActionFailure("That recipe no longer uses the selected tool and material.");
            }
            else _craftCursorIndex = index;
            Render();
        }

        private void ActivateToolWork() => ActivateToolActions();

        private void ActivateToolActions()
        {
            if (TrySelectedTool(out var tool)) ActivateToolActions(tool);
        }

        private void ActivateToolActions(ToolSelection tool)
        {
            var entry = tool.Entry;
            if (!CurrentToolSelection(tool)) { RejectToolsSelection(); return; }

            if (entry.Cell != null) { QueueToolWorldAction(entry); return; }
            if (entry.StorageOwner != null)
            {
                if (tool.Container == null || !CurrentStorageView(entry.StorageOwner, tool.Container)
                    || !tool.Container.Contents.Contains(entry.Item)) { RejectToolsSelection(); return; }
                if (CarriedStorage.TryWithdraw(tool.Actor, entry.StorageOwner, entry.Item, tool.Zone, out string reason))
                {
                    _selectedTool = entry.Item;
                    SetActionSuccess("Moved " + entry.Name + " to the pack.");
                }
                else SetActionFailure(reason);
                Rebuild();
                Render();
                return;
            }

            if (!CurrentCarriedTool(tool)) { RejectToolsSelection(); return; }
            OpenItemActionPopup(entry.Display);
        }

        private bool CurrentToolSelection(ToolSelection tool) => ReferenceEquals(PlayerEntity, tool.Actor)
            && ReferenceEquals(CurrentZone, tool.Zone) && tool.Entry.Item.ID == tool.Id
            && ReferenceEquals(tool.Entry.Item.GetPart<PhysicsPart>(), tool.Physical);

        private static bool CurrentCarriedTool(ToolSelection tool) => tool.Entry.Display is { } display
            && ReferenceEquals(display.Item, tool.Entry.Item) && InventoryDecisionDetails.Owned(tool.Actor, tool.Entry.Item)
            && (tool.Physical.Equipped == tool.Actor) == display.IsEquipped;

        private void RejectToolsSelection()
        {
            SetActionFailure("That tool is no longer in the selected location.");
            Rebuild();
            Render();
        }
    }
}
