using System;
using System.Text;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        // These are presentation capacities, not the legacy tilemap line counts.
        // Rendering only clamps presentation scroll; it never rebuilds recipes,
        // changes crafting marks, consumes inventory, or assigns an ability.
        private const int LedgerTinkerVisibleRows = 13;
        private const int LedgerCraftVisibleRows = 12;

        private void RenderLedgerTinkering()
        {
            LFrame(new Rect(24, 132, 688, 572), LBorder, LPanel);
            LFrame(new Rect(728, 132, 528, 572), LBorder, LPanel);
            LSecondaryButton(new Rect(42, 148, 164, 34), "BUILD", LedgerHitKind.TinkerMode, 0,
                _tinkeringMode == TinkeringMode.Build);
            LSecondaryButton(new Rect(216, 148, 164, 34), "MODIFY", LedgerHitKind.TinkerMode, 1,
                _tinkeringMode == TinkeringMode.Mod);
            LLabel(new Rect(42, 196, 414, 20), "RECIPES", LGold);
            LLabel(new Rect(490, 196, 202, 20), "COST", LMuted);

            _tinkerScrollOffset = LSecondaryFirstIndex(_tinkerScrollOffset, _tinkerCursorIndex,
                _tinkerRows.Count, LedgerTinkerVisibleRows);
            for (int line = 0; line < LedgerTinkerVisibleRows; line++)
            {
                int index = _tinkerScrollOffset + line;
                if (index >= _tinkerRows.Count) break;
                var row = _tinkerRows[index];
                var bounds = new Rect(36, 220 + line * 32, 664, 30);
                bool selected = index == _tinkerCursorIndex;
                if (selected) LBox(bounds, LSelected);
                LSecondaryText(new Rect(44, bounds.y + 5, 430, 20),
                    (selected ? "> " : "  ") + row.Name, selected ? LInk : LMuted, 18);
                LSecondaryText(new Rect(490, bounds.y + 5, 202, 20),
                    string.IsNullOrEmpty(row.Cost) ? "-" : "<" + row.Cost + ">",
                    row.Affordable ? LGreen : LRed, 18);
                LHit(bounds, LedgerHitKind.TinkerRow, index);
            }

            var locker = PlayerEntity?.GetPart<BitLockerPart>();
            if (_tinkerRows.Count == 0)
                LSecondaryText(new Rect(44, 238, 644, 110), locker == null
                    ? "No tinkering access is available for this character."
                    : "No known recipes in this mode.", LMuted);
            LSecondaryRange(new Rect(44, 662, 644, 22), _tinkerScrollOffset,
                LedgerTinkerVisibleRows, _tinkerRows.Count);

            LLabel(new Rect(748, 152, 488, 22), "SELECTED RECIPE", LGold);
            var selectedRow = GetSelectedTinkeringRow();
            if (!selectedRow.HasValue || selectedRow.Value.Recipe == null)
            {
                LSecondaryText(new Rect(748, 194, 488, 112),
                    "Select a known recipe to inspect its cost and requirements.", LMuted);
                return;
            }

            var chosen = selectedRow.Value;
            var recipe = chosen.Recipe;
            LSecondaryText(new Rect(748, 190, 488, 52), chosen.Name, LInk, 22);
            var details = new StringBuilder();
            details.Append("Cost: ").Append(string.IsNullOrEmpty(chosen.Cost) ? "none" : "<" + chosen.Cost + ">");
            details.Append("\nBits: ").Append(chosen.HasBits ? "enough carried" : "missing");
            details.Append("\nIngredient: ").Append(chosen.HasIngredient ? "requirement met" : "missing");
            if (_tinkeringMode == TinkeringMode.Mod)
                details.Append("\nTarget: ").Append(chosen.HasTarget ? "compatible item available" : "none compatible");
            else
                details.Append("\nProduces: ").Append(Mathf.Max(1, recipe.NumberMade));
            if (!string.IsNullOrWhiteSpace(recipe.Description))
                details.Append("\n\n").Append(recipe.Description);
            if (!string.IsNullOrWhiteSpace(recipe.Ingredient))
                details.Append("\n\nIngredient options: ").Append(recipe.Ingredient);
            if (_tinkeringMode == TinkeringMode.Mod && !string.IsNullOrEmpty(chosen.TargetPreview))
                details.Append("\nSuggested target: ").Append(chosen.TargetPreview);
            LSecondaryText(new Rect(748, 256, 488, 292), details.ToString(), LInk, 18);

            LLabel(new Rect(748, 558, 488, 20), "BITS: HAVE / NEED", LMuted, 18);
            var costs = BitCost.ToCounts(chosen.Cost);
            for (int i = 0; i < TinkerBitOrder.Length; i++)
            {
                char bit = TinkerBitOrder[i];
                int have = locker?.GetBitCount(bit) ?? 0;
                int need = costs.TryGetValue(bit, out int required) ? required : 0;
                LSecondaryText(new Rect(748 + (i % 6) * 81, 584 + (i / 6) * 24, 80, 20),
                    bit + " " + have + "/" + need, have < need ? LRed : need > 0 ? LGreen : LMuted, 16);
            }
            LLabel(new Rect(748, 634, 488, 18), "F1: full recipe details", LMuted, 16);
            // CraftCommit deliberately dispatches according to the current panel.
            // Recipe row clicks only select, matching the existing transaction flow.
            LSecondaryButton(new Rect(748, 658, 488, 32),
                _tinkeringMode == TinkeringMode.Mod ? "CHOOSE TARGET" : "BUILD RECIPE",
                LedgerHitKind.CraftCommit, 0, false, chosen.Affordable);
        }

        private void RenderLedgerCrafting()
        {
            LFrame(new Rect(24, 132, 688, 572), LBorder, LPanel);
            LFrame(new Rect(728, 132, 528, 572), LBorder, LPanel);
            bool forge = _craftingMode == CraftingMode.Forge;
            LSecondaryButton(new Rect(42, 148, 164, 34), "FORGE", LedgerHitKind.CraftMode, 0, forge);
            LSecondaryButton(new Rect(216, 148, 164, 34), "BREW", LedgerHitKind.CraftMode, 1, !forge);
            LLabel(new Rect(42, 204, 570, 20), forge ? "CARRIED COMPONENTS" : "CARRIED REAGENTS", LGold);
            LLabel(new Rect(638, 204, 54, 20), "QTY", LMuted, 18);

            _craftScrollOffset = LSecondaryFirstIndex(_craftScrollOffset, _craftCursorIndex,
                _craftRows.Count, LedgerCraftVisibleRows);
            for (int line = 0; line < LedgerCraftVisibleRows; line++)
            {
                int index = _craftScrollOffset + line;
                if (index >= _craftRows.Count) break;
                var row = _craftRows[index];
                var bounds = new Rect(36, 236 + line * 34, 664, 32);
                bool selected = row.IsSelectable && index == _craftCursorIndex;
                if (selected) LBox(bounds, LSelected);
                if (row.IsHeader)
                {
                    LSecondaryText(new Rect(44, bounds.y + 6, 644, 20), row.Text, LGold, 18);
                    continue;
                }
                string mark = row.IsSelectable ? (row.IsMarked ? "[+] " : "[ ] ") : "    ";
                LSecondaryText(new Rect(44, bounds.y + 6, 576, 20), mark + row.Text,
                    selected ? LInk : row.IsMarked ? LGreen : LMuted, 18);
                if (!row.IsSelectable) continue;
                LSecondaryText(new Rect(638, bounds.y + 6, 54, 20), row.Count.ToString(), LMuted, 18);
                LHit(bounds, LedgerHitKind.CraftRow, index);
            }
            if (_craftRows.Count == 0)
                LSecondaryText(new Rect(44, 242, 644, 100), "Nothing carried can be used in this mode.", LMuted);
            LSecondaryButton(new Rect(42, 658, 200, 32), "CLEAR PICKS", LedgerHitKind.ClearPicks, 0);
            LSecondaryRange(new Rect(264, 664, 428, 20), _craftScrollOffset,
                LedgerCraftVisibleRows, _craftRows.Count);

            LLabel(new Rect(748, 152, 488, 22), "RESULT", LGold);
            // These prose helpers read the cached previews only. In particular,
            // do not call BuildSelectedDecisionDetails/BuildCraftingRows here:
            // the latter normalizes shared crafting marks as part of Rebuild.
            string result = forge ? InventoryDecisionDetails.Forge(_forgePreview)
                : InventoryDecisionDetails.Brew(_brewPreview);
            LSecondaryText(new Rect(748, 194, 488, 316), result, LInk, 18);
            string station = _atStation
                ? (forge ? "At a forge." : "At a still.")
                : "Away from a station: one result at a time.";
            if (_craftBatchMax > 1)
                station += _atStation ? " Batch up to " + _craftBatchMax + "."
                    : " At a station: batch up to " + _craftBatchMax + ".";
            LSecondaryText(new Rect(748, 526, 488, 62), station, LMuted, 18);
            if (forge && _pickedQuench != null)
                LSecondaryText(new Rect(748, 592, 488, 40),
                    "Selected quench tempers one forged weapon.", LGold, 18);
            LLabel(new Rect(748, 634, 488, 18), "F1: full result and selected inputs", LMuted, 16);
            bool ready = forge ? _forgePreview.IsComplete : _brewPreview.IsValid;
            bool batch = _atStation && _craftBatchMax > 1;
            LSecondaryButton(new Rect(748, 658, batch ? 238 : 488, 32),
                forge ? "FORGE ONE" : "BREW ONE", LedgerHitKind.CraftCommit, 0, false, ready);
            if (batch)
                LSecondaryButton(new Rect(998, 658, 238, 32), "MAKE BATCH " + _craftBatchMax,
                    LedgerHitKind.CraftCommit, 1, false, ready);
        }

        private void RenderLedgerAbilities()
        {
            LFrame(new Rect(24, 132, 688, 572), LBorder, LPanel);
            LFrame(new Rect(728, 132, 528, 572), LBorder, LPanel);
            LLabel(new Rect(42, 152, 644, 22), "HOTBAR", LGold);
            LLabel(new Rect(42, 184, 644, 18), "Select a slot to assign a learned grimoire.", LMuted, 18);
            var abilities = GetAbilitiesPart();
            for (int slot = 0; slot < ActivatedAbilitiesPart.SlotCount; slot++)
            {
                var bounds = new Rect(36, 218 + slot * 43, 664, 40);
                bool selected = slot == _abilitiesCursorSlot;
                if (selected) LBox(bounds, LSelected);
                var ability = abilities?.GetAbilityBySlot(slot);
                GrimoireTooltipData.TryGet(ability?.SourcePowerClass ?? "", out var tooltip);
                string name = ability == null ? "Empty"
                    : string.IsNullOrEmpty(tooltip.DisplayName) ? ability.DisplayName : tooltip.DisplayName;
                LLabel(new Rect(44, bounds.y + 10, 46, 20), "[" + SlotToHotkey(slot) + "]", selected ? LGold : LMuted, 18);
                LSecondaryText(new Rect(102, bounds.y + 10, 458, 20), name,
                    selected ? LInk : ability == null ? LMuted : LInk, 18);
                if (ability != null)
                    LSecondaryText(new Rect(574, bounds.y + 10, 118, 20),
                        ability.CooldownRemaining > 0 ? "CD " + ability.CooldownRemaining : "READY",
                        ability.CooldownRemaining > 0 ? LRed : LGreen, 18);
                LHit(bounds, LedgerHitKind.AbilitySlot, slot);
            }
            LLabel(new Rect(44, 668, 644, 18), "Bindings change the hotbar; they do not cast.", LMuted, 18);

            int selectedSlot = Mathf.Clamp(_abilitiesCursorSlot, 0, ActivatedAbilitiesPart.SlotCount - 1);
            var chosen = abilities?.GetAbilityBySlot(selectedSlot);
            LLabel(new Rect(748, 152, 488, 22), "SLOT " + SlotToHotkey(selectedSlot), LGold);
            if (chosen == null)
            {
                LSecondaryText(new Rect(748, 194, 488, 124),
                    "This slot is empty. Choose a learned grimoire to bind it to the hotbar.", LMuted);
            }
            else
            {
                GrimoireTooltipData.TryGet(chosen.SourcePowerClass ?? "", out var tooltip);
                string name = string.IsNullOrEmpty(tooltip.DisplayName) ? chosen.DisplayName : tooltip.DisplayName;
                LSecondaryText(new Rect(748, 190, 488, 56), name, LInk, 22);
                LSecondaryText(new Rect(748, 260, 488, 48), chosen.CooldownRemaining > 0
                    ? "Cooldown: " + chosen.CooldownRemaining + " turns remaining."
                    : "Off cooldown.", chosen.CooldownRemaining > 0 ? LRed : LGreen, 18);
                if (!string.IsNullOrWhiteSpace(tooltip.Flavor))
                    LSecondaryText(new Rect(748, 326, 488, 90), tooltip.Flavor, LMuted, 20);
                if (!string.IsNullOrWhiteSpace(tooltip.Mechanics))
                    LSecondaryText(new Rect(748, 436, 488, 78), tooltip.Mechanics, LInk, 20);
                if (!string.IsNullOrWhiteSpace(tooltip.Signature))
                    LSecondaryText(new Rect(748, 534, 488, 102), tooltip.Signature, LGold, 20);
                if (string.IsNullOrEmpty(tooltip.Flavor) && string.IsNullOrEmpty(tooltip.Mechanics)
                    && string.IsNullOrEmpty(tooltip.Signature))
                {
                    // Use the same current owned-ability facts as the full reader.
                    // Compact blank lines only; the reader retains all prose and facts.
                    string details = AbilityDetailsBuilder.BuildForAbility(PlayerEntity, chosen.ID);
                    var preview = new StringBuilder();
                    string[] lines = (details ?? "").Split('\n');
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length == 0) continue;
                        if (preview.Length > 0) preview.Append('\n');
                        preview.Append(line);
                    }
                    LSecondaryText(new Rect(748, 326, 488, 300), preview.ToString(), LInk, 18);
                }
                LLabel(new Rect(748, 636, 488, 20), NativeGamepadInput.IsConnected
                    ? "LT+RT: full ability details" : "F1: full ability details", LMuted, 18);
            }
            LSecondaryButton(new Rect(748, 658, chosen == null ? 488 : 238, 32),
                "ASSIGN GRIMOIRE", LedgerHitKind.AbilitySlot, selectedSlot);
            if (chosen != null)
                LSecondaryButton(new Rect(998, 658, 238, 32), "CLEAR SLOT", LedgerHitKind.ClearAbility, selectedSlot);
        }

        private static int LSecondaryFirstIndex(int requested, int cursor, int count, int capacity)
        {
            if (count <= 0) return 0;
            int first = Mathf.Clamp(requested, 0, Mathf.Max(0, count - capacity));
            cursor = Mathf.Clamp(cursor, 0, count - 1);
            if (cursor < first) first = cursor;
            if (cursor >= first + capacity) first = cursor - capacity + 1;
            return first;
        }

        private void LSecondaryRange(Rect bounds, int first, int capacity, int count)
        {
            string text = count == 0 ? "No entries" : (first + 1) + "-" + Mathf.Min(count, first + capacity) + " of " + count;
            if (count > capacity) text += "  |  Up / Down to browse";
            LSecondaryText(bounds, text, LMuted, 16);
        }

        private void LSecondaryText(Rect bounds, string text, Color color, float height = 20)
        {
            int columns = Mathf.Max(3, Mathf.FloorToInt(bounds.width / (height * .5f)));
            int rows = Mathf.Max(1, Mathf.FloorToInt((bounds.height + 4) / (height + 4)));
            LLabel(bounds, SkillScreenText.Fit(text ?? "", columns, rows), color, height);
        }

        private void LSecondaryButton(Rect bounds, string text, LedgerHitKind kind, int index,
            bool selected = false, bool available = true)
        {
            LFrame(bounds, selected ? LGold : LBorder, selected ? LSelected : LPanel);
            LSecondaryText(new Rect(bounds.x + 12, bounds.y + 7, bounds.width - 24, 18),
                text, available ? LInk : LMuted, 16);
            // An unavailable craft remains actionable to preserve the existing
            // command's explanatory rejection. This is not a local transaction gate.
            LHit(bounds, kind, index);
        }
    }
}
