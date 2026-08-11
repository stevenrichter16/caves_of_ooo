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
            if (inv == null) return;

            _atStation = _craftingMode == CraftingMode.Forge
                ? ForgePart.IsNearForge(PlayerEntity, CurrentZone)
                : AlchemyStillPart.IsNearStill(PlayerEntity, CurrentZone);

            if (_craftingMode == CraftingMode.Forge)
            {
                AddCraftSection("Blades", inv, item => SlotOf(item) == "Blade");
                AddCraftSection("Hafts", inv, item => SlotOf(item) == "Haft");
                AddCraftSection("Bindings", inv, item => SlotOf(item) == "Binding");
                AddCraftSection("Quench (optional)", inv, IsQuench);

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
                var stacker = item.GetPart<StackerPart>();

                _craftRows.Add(new CraftRow
                {
                    Text = item.GetDisplayName(),
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
            if (_craftRows.Count == 0) { _craftCursorIndex = 0; _craftScrollOffset = 0; return; }

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

            int visible = CRAFT_LIST_END_Y - CRAFT_LIST_START_Y + 1;
            if (_craftCursorIndex < _craftScrollOffset)
                _craftScrollOffset = _craftCursorIndex;
            else if (_craftCursorIndex >= _craftScrollOffset + visible)
                _craftScrollOffset = _craftCursorIndex - visible + 1;
            if (_craftScrollOffset < 0) _craftScrollOffset = 0;
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

            // Routed through the shared mark so the station menus see the
            // same selection.
            CraftingMarkPart.Toggle(row.Item);
            Rebuild();
            Render();
        }

        private void ClearCraftPicks()
        {
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
            if (_craftingMode == CraftingMode.Forge) ExecuteForge(batch);
            else ExecuteBrew(batch);

            Rebuild();
            Render();
        }

        private void ExecuteForge(bool batch)
        {
            if (!_forgePreview.IsComplete)
            {
                MessageLog.Add("You need " + _forgePreview.Missing + " to forge that.");
                return;
            }

            var factory = ForgePart.Factory;
            if (factory == null)
            {
                MessageLog.Add("(Forging is not wired to a factory.)");
                return;
            }

            int count = batch ? Mathf.Max(1, _craftBatchMax) : 1;
            var command = new ForgeWeaponCommand(
                _pickedBlade, _pickedHaft, _pickedBinding, factory, count);
            var result = InventorySystem.ExecuteCommand(command, PlayerEntity, CurrentZone);

            if (!result.Success)
            {
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    MessageLog.Add(result.ErrorMessage);
                return;
            }

            // A picked quench tempers the weapon that just came off the
            // anvil — same one-button behaviour the forge menu has.
            if (_pickedQuench != null && command.ForgedWeapons.Count > 0)
            {
                var quench = InventorySystem.ExecuteCommand(
                    new TemperWeaponCommand(command.ForgedWeapons[0], _pickedQuench),
                    PlayerEntity, CurrentZone);
                if (!quench.Success && !string.IsNullOrEmpty(quench.ErrorMessage))
                    MessageLog.Add(quench.ErrorMessage);
            }
        }

        private void ExecuteBrew(bool batch)
        {
            if (_pickedReagents.Count == 0)
            {
                MessageLog.Add("Pick reagents to brew with.");
                return;
            }

            if (!_brewPreview.IsValid)
            {
                MessageLog.Add(string.IsNullOrEmpty(_brewPreview.Reason)
                    ? "These reagents make nothing." : _brewPreview.Reason);
                return;
            }

            var factory = AlchemyStillPart.Factory;
            if (factory == null)
            {
                MessageLog.Add("(Brewing is not wired to a factory.)");
                return;
            }

            int count = batch ? Mathf.Max(1, _craftBatchMax) : 1;
            var result = InventorySystem.ExecuteCommand(
                new BrewReagentsCommand(new List<Entity>(_pickedReagents), factory, count),
                PlayerEntity, CurrentZone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        // ════════════════════════════════════════════════════════
        // RENDER
        // ════════════════════════════════════════════════════════

        private void RenderCraftingPanel()
        {
            for (int y = 1; y < CONTENT_END; y++)
                DrawChar(CRAFT_DIVIDER_X, y, '│', QudColorParser.DarkGray);

            RenderCraftingModeHeader();
            RenderCraftingList();
            RenderCraftingAnvil();

            DrawKeyHints(CONTENT_END + 1,
                "space", _craftingMode == CraftingMode.Forge ? "pick" : "pick",
                "enter", _craftingMode == CraftingMode.Forge ? "craft" : "brew",
                "C", "clear",
                "F/B", "mode",
                "tab", "panel");
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
            int rowY = CRAFT_LIST_START_Y;

            for (int i = _craftScrollOffset; i < _craftRows.Count && rowY <= CRAFT_LIST_END_Y; i++)
            {
                CraftRow row = _craftRows[i];

                if (row.IsHeader)
                {
                    // A blank line above every section but the first, so
                    // the groups read as blocks rather than one long list.
                    if (rowY > CRAFT_LIST_START_Y) rowY++;
                    if (rowY > CRAFT_LIST_END_Y) break;
                    DrawSectionRule(1, rowY, row.Text, listWidth);
                }
                else if (!row.IsSelectable)
                {
                    DrawText(4, rowY, row.Text, QudColorParser.DarkGray);
                }
                else
                {
                    DrawPickRow(1, rowY, listWidth, row.Text,
                        i == _craftCursorIndex, row.IsMarked, row.Count);
                }

                rowY++;
            }

            if (_craftScrollOffset > 0)
                DrawChar(CRAFT_DIVIDER_X - 2, CRAFT_LIST_START_Y, '^', QudColorParser.Gray);
            if (rowY > CRAFT_LIST_END_Y)
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
                DrawText(ix, iy + 1, "Pick " + Truncate(_forgePreview.Missing, 20),
                    QudColorParser.Gray);
                DrawText(ix, iy + 2, "to finish the weapon.", QudColorParser.Gray);
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
                DrawText(ix, iy + 6, "quench ready", QudColorParser.Gray);
        }

        private void RenderBrewResult(int x, int y)
        {
            const int H = 11;
            DrawTitledBox(x, y, CRAFT_RESULT_W, H, "RESULT");
            int ix = x + 2, iy = y + 1;

            if (!_brewPreview.IsValid)
            {
                DrawText(ix, iy + 1, Truncate(_brewPreview.Reason, CRAFT_RESULT_W - 4),
                    QudColorParser.Gray);
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
            => e == null ? "—" : Truncate(e.GetDisplayName(), 24);

        private static Color SlotColor(Entity e)
            => e == null ? QudColorParser.DarkGray : QudColorParser.White;

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, System.Math.Max(1, max - 1)) + "~";
        }
    }
}
