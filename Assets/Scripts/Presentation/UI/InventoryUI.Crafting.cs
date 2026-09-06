using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// The Crafting panel — forge weapons and brew alchemy straight from
    /// the pack, no station required
    /// (Docs/CRAFTING-FROM-THE-PACK.md §C3–C5).
    ///
    /// <para><b>Why a separate panel rather than more Tinkering modes.</b>
    /// Tinkering is recipe-driven: pick a known recipe from a list and
    /// spend bits. Forging and brewing are selection-driven: assemble a
    /// result out of whatever you happen to be carrying. Those are two
    /// different interaction models, and merging them would give one
    /// panel four modes and two grammars.</para>
    ///
    /// <para><b>The RESULT box is the point.</b> Both crafts can be
    /// resolved without consuming anything — <c>PreviewForge</c> and
    /// <c>PreviewBrew</c> — so the panel shows what you are about to make
    /// before you commit. Alchemy in particular stops being a slot
    /// machine: adding a reagent visibly changes the prediction while the
    /// reagents are still in your pack.</para>
    ///
    /// <para><b>Selection is not a new concept.</b> Rows toggle the same
    /// <see cref="CraftingMarkPart"/> marks the forge and still menus
    /// use, so a kit set aside at an anvil is still set aside when you
    /// open your pack a zone later.</para>
    /// </summary>
    public partial class InventoryUI
    {
        private enum CraftingMode { Forge, Brew }

        private CraftingMode _craftingMode = CraftingMode.Forge;
        private int _craftCursorIndex;
        private int _craftScrollOffset;
        // One map for drawing and hit testing, including blank section spacers.
        // Rebuilt only with rows/scroll changes; pointer frames allocate nothing.
        private readonly int[] _craftScreenRows = new int[CRAFT_LIST_END_Y - CRAFT_LIST_START_Y + 1];
        private bool _craftHasMoreBelow;
        private readonly Dictionary<string, Entity> _exclusiveCraftPicks = new Dictionary<string, Entity>(System.StringComparer.Ordinal);

        /// <summary>One rendered line. Headers and empty-notes are inert:
        /// the cursor skips them so it can never sit on something that
        /// does nothing.</summary>
        private struct CraftRow
        {
            public string Text;
            public bool IsHeader;
            public bool IsSelectable;
            public bool IsMarked;
            public int Count;
            public Entity Item;
        }

        private readonly List<CraftRow> _craftRows = new List<CraftRow>();

        // Previews are computed in Rebuild, never in Render — Render runs
        // per redraw and PreviewBrew allocates (PERF-FOUNDATION §Pattern 1).
        private ForgePreview _forgePreview;
        private BrewPreview _brewPreview;
        private Entity _pickedBlade, _pickedHaft, _pickedBinding, _pickedQuench;
        private readonly List<Entity> _pickedReagents = new List<Entity>();
        private int _craftBatchMax;
        private bool _atStation;

        // ── Layout (mirrors the Tinkering panel so they read as siblings) ──
        private const int CRAFT_DIVIDER_X = 49;
        private const int CRAFT_LIST_START_Y = 4;
        private const int CRAFT_LIST_END_Y = 40;
        private const int CRAFT_ANVIL_X = 51;
        private const int CRAFT_RESULT_W = 27;

        // ════════════════════════════════════════════════════════
        // BUILD
        // ════════════════════════════════════════════════════════

        private void BuildCraftingRows()
        {
            _craftRows.Clear();
            _pickedBlade = _pickedHaft = _pickedBinding = _pickedQuench = null;
            _pickedReagents.Clear();

            var inv = PlayerEntity?.GetPart<InventoryPart>();
            if (inv == null) { ClampCraftCursor(); return; }

            NormalizeExclusiveCraftPicks(inv);
            _atStation = _craftingMode == CraftingMode.Forge
                ? ForgePart.IsNearForge(PlayerEntity, CurrentZone)
                : AlchemyStillPart.IsNearStill(PlayerEntity, CurrentZone);

            if (_craftingMode == CraftingMode.Forge)
            {
                AddCraftSection("Blades", inv, item => SlotOf(item) == "Blade");
                AddCraftSection("Hafts", inv, item => SlotOf(item) == "Haft");
                AddCraftSection("Bindings", inv, item => SlotOf(item) == "Binding");
                AddCraftSection("Quench one weapon (optional)", inv, IsQuench);

                _forgePreview = WeaponForgingService.PreviewForge(
                    _pickedBlade?.GetPart<WeaponComponentPart>(),
                    _pickedHaft?.GetPart<WeaponComponentPart>(),
                    _pickedBinding?.GetPart<WeaponComponentPart>());

                _craftBatchMax = _forgePreview.IsComplete
                    ? WeaponForgingService.GetMaxBatchCount(_pickedBlade, _pickedHaft, _pickedBinding)
                    : 0;
            }
            else
            {
                AddCraftSection("Reagents", inv, item => item.HasPart<ReagentPart>());

                _brewPreview = BrewingService.PreviewBrew(_pickedReagents);
                _craftBatchMax = _pickedReagents.Count > 0
                    ? BrewingService.GetMaxBatchCount(_pickedReagents)
                    : 0;
            }

            ClampCraftCursor();
        }

        private void NormalizeExclusiveCraftPicks(InventoryPart inventory)
        {
            // Old panel selections could save multiple marks in an exclusive group.
            // Keep the last marked item and its exact marker (the station already
            // uses this order for component slots);
            // unrelated items and multi-pick reagents remain untouched.
            _exclusiveCraftPicks.Clear();
            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                Entity item = inventory.Objects[i];
                if (!CraftingMarkPart.IsMarked(item)) continue;
                string group = CraftingMarkPart.ExclusiveGroupOf(item);
                if (group == null) continue;
                if (_exclusiveCraftPicks.TryGetValue(group, out var previous)) CraftingMarkPart.Toggle(previous);
                _exclusiveCraftPicks[group] = item;
            }
            _exclusiveCraftPicks.Clear();
        }

        private static string SlotOf(Entity item)
            => item?.GetPart<WeaponComponentPart>()?.Slot ?? string.Empty;

        /// <summary>Any effect-carrying brew can quench — matches the
        /// forge menu's rule so the two surfaces agree on what counts.</summary>
        private static bool IsQuench(Entity item)
        {
            var brew = item?.GetPart<BrewItemPart>();
            return brew != null && brew.GetEffects().Count > 0;
        }

        private void AddCraftSection(string title, InventoryPart inv,
            System.Predicate<Entity> eligible)
        {
            _craftRows.Add(new CraftRow { Text = title, IsHeader = true });

            int added = 0;
            for (int i = 0; i < inv.Objects.Count; i++)
            {
                Entity item = inv.Objects[i];
                if (item == null || !eligible(item)) continue;

                bool marked = CraftingMarkPart.IsMarked(item);
                // Keep an invalid explicit pick visible and removable. Filtering it
                // out of _pickedReagents would silently change the selected recipe.
                if (!marked && !inv.CanConsumeOne(item)) continue;
                var stacker = item.GetPart<StackerPart>();

                _craftRows.Add(new CraftRow
                {
                    Text = item.GetDisplayName() + ((stacker?.StackCount ?? 1) <= 0 ? " (empty; remove pick)" : ""),
                    IsSelectable = true,
                    IsMarked = marked,
                    Count = stacker != null ? stacker.StackCount : 1,
                    Item = item,
                });
                added++;

                if (!marked) continue;

                // Remember what is picked so the preview and the craft
                // agree with what the list is showing.
                if (_craftingMode == CraftingMode.Brew) { _pickedReagents.Add(item); continue; }

                switch (SlotOf(item))
                {
                    case "Blade": _pickedBlade ??= item; break;
                    case "Haft": _pickedHaft ??= item; break;
                    case "Binding": _pickedBinding ??= item; break;
                    default: if (IsQuench(item)) _pickedQuench ??= item; break;
                }
            }

            if (added == 0)
                _craftRows.Add(new CraftRow { Text = "(none carried)" });
        }

        private void ClampCraftCursor()
        {
            if (_craftRows.Count == 0) { _craftCursorIndex = 0; _craftScrollOffset = 0; BuildCraftingLayout(); return; }

            if (_craftCursorIndex >= _craftRows.Count) _craftCursorIndex = _craftRows.Count - 1;
            if (_craftCursorIndex < 0) _craftCursorIndex = 0;

            // Never rest on an inert row.
            if (!_craftRows[_craftCursorIndex].IsSelectable)
            {
                int found = -1;
                for (int i = _craftCursorIndex; i < _craftRows.Count; i++)
                    if (_craftRows[i].IsSelectable) { found = i; break; }
                if (found < 0)
                    for (int i = _craftCursorIndex - 1; i >= 0; i--)
                        if (_craftRows[i].IsSelectable) { found = i; break; }
                if (found >= 0) _craftCursorIndex = found;
            }

            _craftScrollOffset = Mathf.Clamp(_craftScrollOffset, 0, _craftRows.Count - 1);
            if (_craftCursorIndex < _craftScrollOffset) _craftScrollOffset = _craftCursorIndex;
            BuildCraftingLayout();
            // Logical rows have variable drawn height because headers add spacers.
            // Keep the keyboard selection within the same map the player sees.
            while (_craftRows[_craftCursorIndex].IsSelectable && !CraftCursorIsVisible()
                && _craftScrollOffset < _craftCursorIndex)
            { _craftScrollOffset++; BuildCraftingLayout(); }
        }

        private bool CraftCursorIsVisible()
        {
            for (int i = 0; i < _craftScreenRows.Length; i++)
                if (_craftScreenRows[i] == _craftCursorIndex) return true;
            return false;
        }

        private void BuildCraftingLayout()
        {
            System.Array.Fill(_craftScreenRows, -1);
            int line = 0, row = _craftScrollOffset;
            for (; row < _craftRows.Count && line < _craftScreenRows.Length; row++)
            {
                if (_craftRows[row].IsHeader && line > 0) line++;
                if (line >= _craftScreenRows.Length) break;
                _craftScreenRows[line++] = row;
            }
            _craftHasMoreBelow = row < _craftRows.Count;
        }

        private int GetCraftingRowAtGrid(Vector2Int grid)
        {
            if (_panel != PANEL_CRAFTING || grid.x < 1 || grid.x >= CRAFT_DIVIDER_X - 2
                || grid.y < CRAFT_LIST_START_Y || grid.y > CRAFT_LIST_END_Y) return -1;
            int row = _craftScreenRows[grid.y - CRAFT_LIST_START_Y];
            return row >= 0 && row < _craftRows.Count && _craftRows[row].IsSelectable ? row : -1;
        }

        private void HandleCraftingClick(Vector2Int grid)
        {
            int row = GetCraftingRowAtGrid(grid);
            if (row < 0) return;
            _craftCursorIndex = row;
            ToggleCraftPickUnderCursor();
        }

        private void MoveCraftCursor(int delta)
        {
            if (_craftRows.Count == 0) return;

            int i = _craftCursorIndex;
            for (int guard = 0; guard < _craftRows.Count; guard++)
            {
                i += delta;
                if (i < 0 || i >= _craftRows.Count) return;   // stop at the ends
                if (_craftRows[i].IsSelectable) { _craftCursorIndex = i; break; }
            }
            ClampCraftCursor();
        }

        // ════════════════════════════════════════════════════════
        // INPUT
        // ════════════════════════════════════════════════════════

        private void HandleCraftingPanelInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.H))
            { _panel = PANEL_ABILITIES; Render(); return; }

            if (InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.L)
                || InputHelper.GetKeyDown(KeyCode.Tab))
            { _panel = PANEL_EQUIPMENT; Render(); return; }

            if (InputHelper.GetKeyDown(KeyCode.F) && _craftingMode != CraftingMode.Forge)
            { SwitchCraftingMode(CraftingMode.Forge); return; }

            if (InputHelper.GetKeyDown(KeyCode.B) && _craftingMode != CraftingMode.Brew)
            { SwitchCraftingMode(CraftingMode.Brew); return; }

            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            { MoveCraftCursor(-1); Render(); return; }

            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
            { MoveCraftCursor(1); Render(); return; }

            if (InputHelper.GetKeyDown(KeyCode.Space))
            { ToggleCraftPickUnderCursor(); return; }

            if (InputHelper.GetKeyDown(KeyCode.C))
            { ClearCraftPicks(); return; }

            if (InputHelper.GetKeyDown(KeyCode.Return))
            {
                bool batch = InputHelper.GetKey(KeyCode.LeftShift)
                    || InputHelper.GetKey(KeyCode.RightShift);
                ExecuteCraft(batch);
                return;
            }
        }

        private void SwitchCraftingMode(CraftingMode mode)
        {
            _craftingMode = mode;
            _craftCursorIndex = 0;
            _craftScrollOffset = 0;
            Rebuild();
            Render();
        }

        private void ToggleCraftPickUnderCursor()
        {
            if (_craftCursorIndex < 0 || _craftCursorIndex >= _craftRows.Count) return;
            CraftRow row = _craftRows[_craftCursorIndex];
            if (!row.IsSelectable || row.Item == null) return;

            // Use the same ownership validation and exclusive groups as station
            // selections; reagents deliberately remain multi-select.
            TryToggleCraftMarkViaCommand(row.Item);
            Rebuild();
            Render();
        }

        private void ClearCraftPicks()
        {
            ClearActionStatus();
            var inv = PlayerEntity?.GetPart<InventoryPart>();
            if (inv == null) return;

            for (int i = 0; i < inv.Objects.Count; i++)
            {
                Entity item = inv.Objects[i];
                if (item != null && CraftingMarkPart.IsMarked(item))
                    CraftingMarkPart.Toggle(item);
            }
            Rebuild();
            Render();
        }

        private void ExecuteCraft(bool batch)
        {
            ClearActionStatus();
            if (_craftingMode == CraftingMode.Forge) ExecuteForge(batch);
            else ExecuteBrew(batch);

            Rebuild();
            Render();
        }

        private void ExecuteForge(bool batch)
        {
            if (!_forgePreview.IsComplete)
            {
                ShowActionFailure("You need " + _forgePreview.Missing + " to forge that.");
                return;
            }

            var factory = ForgePart.Factory;
            if (factory == null)
            {
                ShowActionFailure("Forging is unavailable.");
                return;
            }

            int count = batch ? Mathf.Max(1, _craftBatchMax) : 1;
            var command = new ForgeWeaponCommand(
                _pickedBlade, _pickedHaft, _pickedBinding, factory, count);
            var result = InventorySystem.ExecuteCommand(command, PlayerEntity, CurrentZone);

            if (!result.Success)
            {
                ShowActionFailure(result.ErrorMessage);
                return;
            }

            // A picked quench tempers one unit from the first output recipient of the
            // anvil — same one-button behaviour the forge menu has.
            if (_pickedQuench != null && command.ForgedWeapons.Count > 0)
            {
                var quench = InventorySystem.ExecuteCommand(
                    new TemperWeaponCommand(command.ForgedWeapons[0], _pickedQuench),
                    PlayerEntity, CurrentZone);
                if (!quench.Success)
                    ShowActionFailure("Forged weapon; quench failed: " +
                        (string.IsNullOrWhiteSpace(quench.ErrorMessage) ? "quench unavailable." : quench.ErrorMessage));
            }
        }

        private void ExecuteBrew(bool batch)
        {
            if (_pickedReagents.Count == 0)
            {
                ShowActionFailure("Pick reagents to brew with.");
                return;
            }

            if (!_brewPreview.IsValid)
            {
                ShowActionFailure(string.IsNullOrEmpty(_brewPreview.Reason)
                    ? "These reagents make nothing." : _brewPreview.Reason);
                return;
            }

            var factory = AlchemyStillPart.Factory;
            if (factory == null)
            {
                ShowActionFailure("Brewing is unavailable.");
                return;
            }

            int count = batch ? Mathf.Max(1, _craftBatchMax) : 1;
            var result = InventorySystem.ExecuteCommand(
                new BrewReagentsCommand(new List<Entity>(_pickedReagents), factory, count),
                PlayerEntity, CurrentZone);

            if (!result.Success) ShowActionFailure(result.ErrorMessage);
        }

        // ════════════════════════════════════════════════════════
        // RENDER
        // ════════════════════════════════════════════════════════

        private void RenderCraftingPanel()
        {
            for (int y = 2; y < CONTENT_END; y++)
                DrawChar(CRAFT_DIVIDER_X, y, '|', QudColorParser.DarkGray);

            RenderCraftingModeHeader();
            RenderCraftingList();
            RenderCraftingAnvil();

            // The action bar at row H-2 is drawn once, centrally, by the
            // shared render block — not here. Two footers on one row
            // printed over each other.
        }

        private void RenderCraftingModeHeader()
        {
            bool forge = _craftingMode == CraftingMode.Forge;
            DrawText(2, 2, forge ? "> Forge" : "  Forge",
                forge ? QudColorParser.White : QudColorParser.Gray);
            DrawText(12, 2, forge ? "  Brew" : "> Brew",
                forge ? QudColorParser.Gray : QudColorParser.White);
        }

        private void RenderCraftingList()
        {
            if (_craftRows.Count == 0)
            {
                DrawText(2, CRAFT_LIST_START_Y, "(nothing to craft with)",
                    QudColorParser.DarkGray);
                return;
            }

            int listWidth = CRAFT_DIVIDER_X - 3;
            for (int line = 0; line < _craftScreenRows.Length; line++)
            {
                int i = _craftScreenRows[line];
                if (i < 0 || i >= _craftRows.Count) continue;
                CraftRow row = _craftRows[i];
                int rowY = CRAFT_LIST_START_Y + line;
                if (row.IsHeader) DrawSectionRule(1, rowY, row.Text, listWidth);
                else if (!row.IsSelectable) DrawText(4, rowY, row.Text, QudColorParser.DarkGray);
                else DrawPickRow(1, rowY, listWidth, row.Text, i == _craftCursorIndex, row.IsMarked, row.Count);
            }

            if (_craftScrollOffset > 0)
                DrawChar(CRAFT_DIVIDER_X - 2, CRAFT_LIST_START_Y, '^', QudColorParser.Gray);
            if (_craftHasMoreBelow)
                DrawChar(CRAFT_DIVIDER_X - 2, CRAFT_LIST_END_Y, 'v', QudColorParser.Gray);
        }

        private void RenderCraftingAnvil()
        {
            int x = CRAFT_ANVIL_X;
            DrawText(x, 2, _craftingMode == CraftingMode.Forge ? "ON THE ANVIL" : "IN THE MIX",
                QudColorParser.BrightYellow);

            int y = 4;
            if (_craftingMode == CraftingMode.Forge)
            {
                y = RenderForgeSelection(x, y);
                RenderForgeResult(x, y + 1);
            }
            else
            {
                y = RenderBrewSelection(x, y);
                RenderBrewResult(x, y + 1);
            }

            RenderBatchHint(x);
        }

        private int RenderForgeSelection(int x, int y)
        {
            DrawLabelled(x + 1, y, "Blade", x + 10, NameOrDash(_pickedBlade), SlotColor(_pickedBlade));
            DrawLabelled(x + 1, y + 1, "Haft", x + 10, NameOrDash(_pickedHaft), SlotColor(_pickedHaft));
            DrawLabelled(x + 1, y + 2, "Binding", x + 10, NameOrDash(_pickedBinding), SlotColor(_pickedBinding));
            DrawLabelled(x + 1, y + 3, "Quench", x + 10, NameOrDash(_pickedQuench), SlotColor(_pickedQuench));
            return y + 4;
        }

        private int RenderBrewSelection(int x, int y)
        {
            if (_pickedReagents.Count == 0)
            {
                DrawText(x + 1, y, "(nothing picked)", QudColorParser.DarkGray);
                return y + 1;
            }

            int row = y;
            for (int i = 0; i < _pickedReagents.Count && row < y + 6; i++, row++)
                DrawText(x + 1, row, Truncate(_pickedReagents[i].GetDisplayName(), 26),
                    QudColorParser.White);
            return row;
        }

        private void RenderForgeResult(int x, int y)
        {
            const int H = 9;
            DrawTitledBox(x, y, CRAFT_RESULT_W, H, "RESULT");
            int ix = x + 2, iy = y + 1;

            if (!_forgePreview.IsComplete)
            {
                DrawWrapped(ix, iy + 1, CRAFT_RESULT_W - 4, 4,
                    "Pick " + _forgePreview.Missing + " to finish the weapon.",
                    QudColorParser.Gray);
                return;
            }

            DrawText(ix, iy, Truncate(_forgePreview.DisplayName, CRAFT_RESULT_W - 4),
                QudColorParser.White);
            DrawLabelled(ix, iy + 2, "Damage", ix + 10, _forgePreview.BaseDamage, QudColorParser.White);
            DrawLabelled(ix, iy + 3, "Pen", ix + 10, _forgePreview.PenBonus.ToString(),
                _forgePreview.PenBonus > 0 ? QudColorParser.BrightGreen : QudColorParser.White);
            DrawLabelled(ix, iy + 4, "Hit", ix + 10, _forgePreview.HitBonus.ToString(),
                _forgePreview.HitBonus > 0 ? QudColorParser.BrightGreen : QudColorParser.White);

            if (!string.IsNullOrEmpty(_forgePreview.OnHitEffectsRaw))
                DrawText(ix, iy + 6, Truncate(_forgePreview.OnHitEffectsRaw, CRAFT_RESULT_W - 4),
                    QudColorParser.BrightRed);
            else if (_pickedQuench != null)
                DrawText(ix, iy + 6, "quench one weapon", QudColorParser.Gray);
        }

        private void RenderBrewResult(int x, int y)
        {
            const int H = 11;
            DrawTitledBox(x, y, CRAFT_RESULT_W, H, "RESULT");
            int ix = x + 2, iy = y + 1;

            if (!_brewPreview.IsValid)
            {
                DrawWrapped(ix, iy + 1, CRAFT_RESULT_W - 4, H - 4,
                    _brewPreview.Reason, QudColorParser.Gray);
                return;
            }

            DrawText(ix, iy, Truncate(_brewPreview.DisplayName, CRAFT_RESULT_W - 4),
                QudColorParser.White);

            int row = iy + 2;
            var effects = _brewPreview.Effects;
            for (int i = 0; i < effects.Count && row < y + H - 2; i++, row++)
            {
                DrawText(ix, row, Truncate(effects[i].Property, 14), QudColorParser.Gray);
                string potency = effects[i].Potency.ToString();
                DrawText(x + CRAFT_RESULT_W - 3 - potency.Length, row, potency,
                    QudColorParser.BrightGreen);
            }

            if (row < y + H - 1 && !string.IsNullOrEmpty(_brewPreview.Form))
                DrawLabelled(ix, row + 1, "form", ix + 7,
                    _brewPreview.Form.ToLowerInvariant(), QudColorParser.White);
        }

        private void RenderBatchHint(int x)
        {
            int y = CRAFT_LIST_END_Y;
            if (_craftBatchMax <= 1)
                return;

            if (_atStation)
                DrawText(x + 1, y,
                    "Batch up to " + _craftBatchMax + "  [shift+enter]",
                    QudColorParser.BrightYellow);
            else
                DrawText(x + 1, y,
                    (_craftingMode == CraftingMode.Forge ? "At a forge: " : "At a still: ")
                    + "batch up to " + _craftBatchMax,
                    QudColorParser.DarkGray);
        }

        private static string NameOrDash(Entity e)
            => e == null ? "-" : Truncate(e.GetDisplayName(), 24);

        private static Color SlotColor(Entity e)
            => e == null ? QudColorParser.DarkGray : QudColorParser.White;

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, System.Math.Max(1, max - 1)) + "~";
        }
    }
}
