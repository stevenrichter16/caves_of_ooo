using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        struct LedgerCommand { public string Label; public Action Run; public Entity Item; }
        readonly List<LedgerCommand> _ledgerCommands = new List<LedgerCommand>(10);
        bool _ledgerCommandsOpen;
        int _ledgerCommandIndex;
        bool _ledgerDetailsRequested;
        bool _ledgerStatusRequested;
        /// <summary>Transfers a pointer request to InputHandler's existing free reader path once.</summary>
        public bool ConsumeDetailsRequest()
        { bool requested = _ledgerDetailsRequested; _ledgerDetailsRequested = false; return requested && _isOpen && !_inventorySearchActive && !StorageLabelEditorOpen; }
        /// <summary>Transfers the controller command to the same free character reader as F2, once.</summary>
        public bool ConsumeStatusRequest()
        { bool requested = _ledgerStatusRequested; _ledgerStatusRequested = false; return requested && _isOpen && !_inventorySearchActive && !StorageLabelEditorOpen; }

        void OpenLedgerCommands()
        {
            _ledgerCommandTitle = "COMMANDS";
            _ledgerCommands.Clear(); _ledgerCommandIndex = 0;
            if (_panel == PANEL_LOADOUTS)
                AddLoadoutCommands();
            else if (_panel == PANEL_CRAFTING)
            {
                bool recipes = _craftingMode == CraftingMode.Recipes;
                if (recipes) AddLedgerCommand("Make selected recipe", () => ExecuteCraft(false));
                else
                {
                    AddLedgerCommand("Pick / unpick ingredient", ToggleCraftPickUnderCursor);
                    AddLedgerCommand("Craft one", () => ExecuteCraft(false));
                    if (_atStation && _craftBatchMax > 1) AddLedgerCommand("Craft batch", () => ExecuteCraft(true));
                }
                AddLedgerCommand("Forge", () => SwitchCraftingMode(CraftingMode.Forge));
                AddLedgerCommand("Brew", () => SwitchCraftingMode(CraftingMode.Brew));
                AddLedgerCommand("Recipes", () => SwitchCraftingMode(CraftingMode.Recipes));
                if (!recipes) AddLedgerCommand("Clear picks", ClearCraftPicks);
            }
            else if (_panel == PANEL_TINKERING)
            {
                AddLedgerCommand("Use selected recipe", () => ActivateLedgerHit(new LedgerHit { Kind = LedgerHitKind.CraftCommit }));
                AddLedgerCommand("Build", () => ActivateLedgerHit(new LedgerHit { Kind = LedgerHitKind.TinkerMode, Index = 0 }));
                AddLedgerCommand("Modify", () => ActivateLedgerHit(new LedgerHit { Kind = LedgerHitKind.TinkerMode, Index = 1 }));
            }
            else if (_panel == PANEL_ABILITIES)
            {
                AddLedgerCommand("Assign grimoire", () => OpenGrimoirePicker(_abilitiesCursorSlot));
                AddLedgerCommand("Clear slot", () => { GetAbilitiesPart()?.AssignAbilityToSlot(Guid.Empty, _abilitiesCursorSlot); Render(); });
            }
            else if (_panel == PANEL_EQUIPMENT)
            {
                AddLedgerCommand("Choose equipment", () =>
                { if (_equipSlots != null && _equipCursorIndex >= 0 && _equipCursorIndex < _equipSlots.Count) OpenEquipPopup(_equipSlots[_equipCursorIndex]); });
                AddLedgerCommand("Unequip selected", () =>
                {
                    if (_equipSlots != null && _equipCursorIndex >= 0 && _equipCursorIndex < _equipSlots.Count)
                    { var item = _equipSlots[_equipCursorIndex].EquippedItem; if (item != null) TryUnequipViaCommand(item); }
                    Rebuild(); Render();
                });
            }
            else AddLedgerCommand("Item actions", () => ActivatePackRow(_cursorIndex, browseContents: false));
            if (_panel != PANEL_ABILITIES || GetAbilitiesPart()?.GetAbilityBySlot(_abilitiesCursorSlot) != null)
                AddLedgerCommand("Full details", () => _ledgerDetailsRequested = true);
            AddLedgerCommand("Character status", () => _ledgerStatusRequested = true);
            if (DensePack)
            {
                AddLedgerCommand("Choose category", OpenCategoryChooser);
                AddLedgerCommand("Sort items", OpenSortChooser);
                AddLedgerCommand(_inventoryUngrouped ? "Group matching items" : "Show individual items", ToggleInventoryGrouping);
                AddLedgerCommand("Larger text", () => ChangeDenseTextScale(1));
                AddLedgerCommand("Smaller text", () => ChangeDenseTextScale(-1));
            }
            AddLedgerCommand("Close inventory", Close);
            _ledgerCommandsOpen = true; Render();
        }
        void AddLedgerCommand(string label, Action run, Entity item = null)
            => _ledgerCommands.Add(new LedgerCommand { Label = label, Run = run, Item = item });
        void ExecuteLedgerCommand(int index)
        {
            if (index < 0 || index >= _ledgerCommands.Count) return;
            var action = _ledgerCommands[index].Run; _ledgerCommandsOpen = false;
            action?.Invoke(); if (_isOpen) Render();
        }
        bool HandleLedgerCommandsInput()
        {
            if (!_ledgerCommandsOpen) return false;
            if (InputHelper.GetKeyDown(KeyCode.Escape)) { _ledgerCommandsOpen = false; Render(); }
            else if (InputHelper.GetKeyDown(KeyCode.UpArrow)) { MoveLedgerSelection(-1); Render(); }
            else if (InputHelper.GetKeyDown(KeyCode.DownArrow)) { MoveLedgerSelection(1); Render(); }
            else if (InputHelper.GetKeyDown(KeyCode.Return)) ExecuteLedgerCommand(_ledgerCommandIndex);
            return true;
        }

        void RenderLedgerPopup()
        {
            if (!LedgerHasPopup || _loadoutNameEditing) return;
            // Opaque modal wins every hit; the underlying pack cannot be activated.
            _ledgerHits.Clear();
            LBox(new Rect(0, 0, DensePack ? _denseLayout.Width : 1280, DensePack ? _denseLayout.Height : 800), new Color(0, 0, 0, .8f));
            if (DensePack)
            {
                _ledgerPaintScale = Mathf.Min(1, _denseLayout.Width / 1280, _denseLayout.Height / 800);
                _ledgerPaintOffset = new Vector2((_denseLayout.Width - 1280 * _ledgerPaintScale) / 2, (_denseLayout.Height - 800 * _ledgerPaintScale) / 2);
            }
            _ledgerModalBounds = LedgerPaintRect(new Rect(250, 142, 780, 556));
            const float x = 250, y = 142, width = 780, height = 556;
            LFrame(new Rect(x, y, width, height), LGold, LPanel);
            if (StorageLabelEditorOpen)
            {
                RenderStorageLabelEditor();
                return;
            }
            string title = "ITEM ACTIONS", owner = "", groupScope = "";
            int count = 0, cursor = 0;
            Func<int, string> label = null;
            Func<int, Entity> item = null;
            if (_ledgerCommandsOpen)
            {
                title = _ledgerCommandTitle; count = _ledgerCommands.Count; cursor = _ledgerCommandIndex;
                label = i => _ledgerCommands[i].Label;
                item = i => _ledgerCommands[i].Item;
            }
            else if (_displaceConfirm != null)
            {
                title = "REPLACE EQUIPMENT?"; owner = _displaceConfirm.ItemToEquip.GetDisplayName();
                LLabel(new Rect(x + 24, y + 134, width - 48, 24), "Will unequip:", LMuted);
                int line = 0;
                foreach (var displacement in _displaceConfirm.Displacements)
                {
                    if (line == 8) break;
                    string consequence = displacement.BodyPart.GetDisplayName() + ": " + displacement.Item.GetDisplayName();
                    LLabel(new Rect(x + 24, y + 170 + line++ * 26, width - 48, 24), SkillScreenText.Fit(consequence, 72, 1), LInk);
                }
                if (_displaceConfirm.Displacements.Count > 8) LLabel(new Rect(x + 24, y + 386, width - 48, 24), "+ " + (_displaceConfirm.Displacements.Count - 8) + " slots - " + (NativeGamepadInput.IsConnected ? "LT+RT" : "F1") + " for all replacements", LMuted, 18);
                LedgerChoiceButton(new Rect(x + 24, y + 424, 350, 40), "Replace", LedgerHitKind.DisplaceYes, _displaceConfirm.CursorOnYes);
                LedgerChoiceButton(new Rect(x + 406, y + 424, 350, 40), "Keep current", LedgerHitKind.DisplaceNo, !_displaceConfirm.CursorOnYes);
            }
            else if (_equipPopup != null)
            {
                title = "CHOOSE EQUIPMENT"; owner = _equipPopup.TargetSlot.BodyPartName;
                count = _equipPopup.TotalRows; cursor = _equipPopup.CursorIndex;
                item = i => _equipPopup.HasRemoveOption && i == 0 ? null
                    : _equipPopup.Items[i - (_equipPopup.HasRemoveOption ? 1 : 0)];
                label = i => _equipPopup.HasRemoveOption && i == 0 ? "Remove current equipment"
                    : _equipPopup.Items[i - (_equipPopup.HasRemoveOption ? 1 : 0)].GetDisplayName();
            }
            else if (_itemActionPopup != null)
            {
                owner = _itemActionPopup.Item.GetDisplayName();
                groupScope = GroupActionScope(_itemActionPopup.Item);
                if (_itemActionPopup.InBodyPartPicker)
                {
                    title = "EQUIP TO BODY PART"; count = _itemActionPopup.BodyParts?.Count ?? 0; cursor = _itemActionPopup.BodyPartCursor;
                    label = i => _itemActionPopup.BodyParts[i].GetDisplayName();
                }
                else
                {
                    count = _itemActionPopup.Actions.Count;
                    cursor = _itemActionPopup.CursorIndex;
                    label = i => _itemActionPopup.Actions[i].Label;
                    item = i => _itemActionPopup.Actions[i].Container;
                }
            }
            else if (_modTargetPopup != null)
            {
                title = "MODIFICATION TARGET"; owner = _modTargetPopup.Recipe.DisplayName;
                count = _modTargetPopup.TotalRows; cursor = _modTargetPopup.CursorIndex;
                item = i => _modTargetPopup.Targets[i];
                label = i => _modTargetPopup.Targets[i].GetDisplayName();
            }
            else if (_grimoirePicker != null)
            {
                title = "ASSIGN GRIMOIRE"; owner = "Hotbar slot " + ((_grimoirePicker.TargetSlot + 1) % 10);
                count = _grimoirePicker.Grimoires.Count; cursor = _grimoirePicker.CursorIndex;
                label = i => _grimoirePicker.Grimoires[i].DisplayName;
            }
            LLabel(new Rect(x + 24, y + 22, width - 48, 30), title, LGold, 26);
            if (groupScope.Length > 0)
            {
                LLabel(new Rect(x + 24, y + 64, width - 48, 24), SkillScreenText.Fit(groupScope, 81, 1), LGold, 18);
                LLabel(new Rect(x + 24, y + 90, width - 48, 26), SkillScreenText.Fit(owner, 73, 1), LInk);
            }
            else LLabel(new Rect(x + 24, y + 64, width - 48, 52), SkillScreenText.Fit(owner, 73, 2), LInk);
            LBox(new Rect(x + 24, y + 122, width - 48, 1), LBorder);
            if (_displaceConfirm == null)
            {
                Func<int, string> rowLabel = i =>
                {
                    int shortcut = _equipPopup != null ? i - (_equipPopup.HasRemoveOption ? 1 : 0) : i;
                    string prefix = NativeGamepadInput.IsConnected || _ledgerCommandsOpen || shortcut < 0 || shortcut > 25 ? "" : ((char)('a' + shortcut)) + ") ";
                    return prefix + label(i);
                };
                // Keep the row layout stable as selection moves. Any clipped row
                // reserves a preview for the selected label, including its costs.
                bool hasPreview = false;
                for (int i = 0; i < count; i++)
                    if (SkillScreenText.Wrap(rowLabel(i), InventoryIconSettings.Enabled && item?.Invoke(i) != null ? 69 : 72).Count > 1)
                    { hasPreview = true; break; }
                int capacity = hasPreview ? 6 : 9;
                int start = Mathf.Clamp(cursor - capacity + 1, 0, Mathf.Max(0, count - capacity));
                for (int i = start; i < Mathf.Min(count, start + capacity); i++)
                {
                    var rect = new Rect(x + 16, y + 134 + (i - start) * 34, width - 32, 32);
                    bool selected = i == cursor;
                    if (selected) { LBox(rect, LSelected); LBox(new Rect(rect.x, rect.y, 3, rect.height), LGold); }
                    var nameBounds = new Rect(rect.x + 10, rect.y + 6, rect.width - 20, 22);
                    var name = DrawItemRowIcon(nameBounds, item?.Invoke(i));
                    LLabel(name, SkillScreenText.Fit(rowLabel(i), name.width < nameBounds.width ? 69 : 72, 1), selected ? LGold : LInk);
                    LHit(rect, LedgerHitKind.PopupRow, i);
                }
                if (count == 0) LLabel(new Rect(x + 24, y + 148, width - 48, 72), SkillScreenText.Fit(_modTargetPopup?.EmptyReason ?? "No available choices.", 73, 3), LMuted);
                if (count > capacity) LLabel(new Rect(x + 24, y + (hasPreview ? 340 : 448), width - 48, 22), (start + 1) + "-" + Mathf.Min(count, start + capacity) + " / " + count + " choices - scroll to continue", LMuted, 18);
                if (hasPreview && cursor >= 0 && cursor < count)
                {
                    LBox(new Rect(x + 24, y + 368, width - 48, 1), LBorder);
                    LLabel(new Rect(x + 24, y + 382, width - 48, 92), SkillScreenText.Fit(label(cursor), 73, 4), LInk);
                }
            }
            LedgerChoiceButton(new Rect(x + 24, y + 492, 162, 40), NativeGamepadInput.IsConnected ? "B  Back" : "Esc  Back", LedgerHitKind.Cancel, false);
            bool candidateDetails = _grimoirePicker != null && count > 0;
            string controls = NativeGamepadInput.IsConnected
                ? (candidateDetails ? "D-pad: browse  A: select  LT+RT: candidate details" : "D-pad: browse   A: select")
                : (candidateDetails ? "Arrows: browse  Enter: select  F1: candidate details" : "Arrows: browse   Enter: select");
            LLabel(new Rect(x + 210, y + 502, width - 234, 24), controls, LMuted, 18);
        }
        void LedgerChoiceButton(Rect rect, string text, LedgerHitKind kind, bool selected)
        {
            LFrame(rect, selected ? LGold : LBorder, selected ? LSelected : LPanel);
            LLabel(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, 22), text, selected ? LGold : LInk);
            LHit(rect, kind);
        }
    }
}
