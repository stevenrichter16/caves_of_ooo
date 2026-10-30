using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        struct LedgerCommand { public string Label; public Action Run; }
        readonly List<LedgerCommand> _ledgerCommands = new List<LedgerCommand>(10);
        bool _ledgerCommandsOpen;
        int _ledgerCommandIndex;
        bool _ledgerDetailsRequested;
        bool _ledgerStatusRequested;
        /// <summary>Transfers a pointer request to InputHandler's existing free reader path once.</summary>
        public bool ConsumeDetailsRequest()
        { bool requested = _ledgerDetailsRequested; _ledgerDetailsRequested = false; return requested && _isOpen && !_inventorySearchActive; }
        /// <summary>Transfers the controller command to the same free character reader as F2, once.</summary>
        public bool ConsumeStatusRequest()
        { bool requested = _ledgerStatusRequested; _ledgerStatusRequested = false; return requested && _isOpen && !_inventorySearchActive; }

        void OpenLedgerCommands()
        {
            _ledgerCommands.Clear(); _ledgerCommandIndex = 0;
            if (_panel == PANEL_LOADOUTS)
                AddLoadoutCommands();
            else if (_panel == PANEL_CRAFTING)
            {
                AddLedgerCommand("Pick / unpick ingredient", ToggleCraftPickUnderCursor);
                AddLedgerCommand("Craft one", () => ExecuteCraft(false));
                if (_atStation && _craftBatchMax > 1) AddLedgerCommand("Craft batch", () => ExecuteCraft(true));
                AddLedgerCommand("Forge", () => SwitchCraftingMode(CraftingMode.Forge));
                AddLedgerCommand("Brew", () => SwitchCraftingMode(CraftingMode.Brew));
                AddLedgerCommand("Clear picks", ClearCraftPicks);
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
            else AddLedgerCommand("Item actions", () =>
            { if (_cursorIndex >= 0 && _cursorIndex < _rows.Count && _rows[_cursorIndex].Item != null) OpenItemActionPopup(_rows[_cursorIndex].Item); });
            if (_panel != PANEL_ABILITIES || GetAbilitiesPart()?.GetAbilityBySlot(_abilitiesCursorSlot) != null)
                AddLedgerCommand("Full details", () => _ledgerDetailsRequested = true);
            AddLedgerCommand("Character status", () => _ledgerStatusRequested = true);
            AddLedgerCommand("Close inventory", Close);
            _ledgerCommandsOpen = true; Render();
        }
        void AddLedgerCommand(string label, Action run) => _ledgerCommands.Add(new LedgerCommand { Label = label, Run = run });
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
            LBox(new Rect(0, 0, 1280, 800), new Color(0, 0, 0, .8f));
            const float x = 250, y = 142, width = 780, height = 556;
            LFrame(new Rect(x, y, width, height), LGold, LPanel);
            string title = "ITEM ACTIONS", owner = "";
            int count = 0, cursor = 0;
            Func<int, string> label = null;
            if (_ledgerCommandsOpen)
            { title = "COMMANDS"; count = _ledgerCommands.Count; cursor = _ledgerCommandIndex; label = i => _ledgerCommands[i].Label; }
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
                label = i => _equipPopup.HasRemoveOption && i == 0 ? "Remove current equipment"
                    : _equipPopup.Items[i - (_equipPopup.HasRemoveOption ? 1 : 0)].GetDisplayName();
            }
            else if (_itemActionPopup != null)
            {
                owner = _itemActionPopup.Item.GetDisplayName();
                if (_itemActionPopup.InBodyPartPicker)
                {
                    title = "EQUIP TO BODY PART"; count = _itemActionPopup.BodyParts?.Count ?? 0; cursor = _itemActionPopup.BodyPartCursor;
                    label = i => _itemActionPopup.BodyParts[i].GetDisplayName();
                }
                else { count = _itemActionPopup.Actions.Count; cursor = _itemActionPopup.CursorIndex; label = i => _itemActionPopup.Actions[i].Label; }
            }
            else if (_modTargetPopup != null)
            {
                title = "MODIFICATION TARGET"; owner = _modTargetPopup.Recipe.DisplayName;
                count = _modTargetPopup.TotalRows; cursor = _modTargetPopup.CursorIndex;
                label = i => _modTargetPopup.Targets[i].GetDisplayName();
            }
            else if (_grimoirePicker != null)
            {
                title = "ASSIGN GRIMOIRE"; owner = "Hotbar slot " + ((_grimoirePicker.TargetSlot + 1) % 10);
                count = _grimoirePicker.Grimoires.Count; cursor = _grimoirePicker.CursorIndex;
                label = i => _grimoirePicker.Grimoires[i].DisplayName;
            }
            LLabel(new Rect(x + 24, y + 22, width - 48, 30), title, LGold, 26);
            LLabel(new Rect(x + 24, y + 64, width - 48, 52), SkillScreenText.Fit(owner, 73, 2), LInk);
            LBox(new Rect(x + 24, y + 122, width - 48, 1), LBorder);
            if (_displaceConfirm == null)
            {
                const int capacity = 9;
                int start = Mathf.Clamp(cursor - capacity + 1, 0, Mathf.Max(0, count - capacity));
                for (int i = start; i < Mathf.Min(count, start + capacity); i++)
                {
                    var rect = new Rect(x + 16, y + 134 + (i - start) * 34, width - 32, 32);
                    bool selected = i == cursor;
                    if (selected) { LBox(rect, LSelected); LBox(new Rect(rect.x, rect.y, 3, rect.height), LGold); }
                    // Equip remove row is not part of its letter-indexed item array.
                    int shortcut = _equipPopup != null ? i - (_equipPopup.HasRemoveOption ? 1 : 0) : i;
                    string prefix = NativeGamepadInput.IsConnected || _ledgerCommandsOpen || shortcut < 0 || shortcut > 25 ? "" : ((char)('a' + shortcut)) + ") ";
                    LLabel(new Rect(rect.x + 10, rect.y + 6, rect.width - 20, 22), SkillScreenText.Fit(prefix + label(i), 72, 1), selected ? LGold : LInk);
                    LHit(rect, LedgerHitKind.PopupRow, i);
                }
                if (count == 0) LLabel(new Rect(x + 24, y + 148, width - 48, 72), SkillScreenText.Fit(_modTargetPopup?.EmptyReason ?? "No available choices.", 73, 3), LMuted);
                if (count > capacity) LLabel(new Rect(x + 24, y + 448, width - 48, 22), (start + 1) + "-" + Mathf.Min(count, start + capacity) + " / " + count + " choices - scroll to continue", LMuted, 18);
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
