using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders the inventory UI on a tilemap with three tabs:
    /// Equipment, Inventory, and Tinkering.
    /// Equipment/Inventory preserve the paperdoll + list layout.
    /// Tinkering uses a Qud-inspired build/mod split with recipe list + bit locker panel.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;
        public Zone CurrentZone;
        public EntityFactory EntityFactory;

        // Layout constants (80x45 grid)
        private const int W = 80;
        private const int H = 45;
        private const int DIVIDER_X = 30;
        private const int RIGHT_START = 31;
        private const int RIGHT_W = W - RIGHT_START; // 49
        private const int CONTENT_START = 2;
        private const int CONTENT_END = 42;
        private const int VISIBLE_ROWS = CONTENT_END - CONTENT_START;
        private const int BOX_W = 5;
        private const int BOX_H = 3;
        private const int PANEL_EQUIPMENT = 0;
        private const int PANEL_INVENTORY = 1;
        private const int PANEL_TINKERING = 2;

        private const int TINKER_DIVIDER_X = 50;
        private const int TINKER_LIST_START_Y = 3;
        private const int TINKER_LIST_END_Y = 15;
        private const int TINKER_DESC_START_Y = 18;
        private static readonly char[] TinkerBitOrder = { 'R', 'G', 'B', 'C', 'r', 'g', 'b', 'c', 'K', 'W', 'Y', 'M' };

        // Equip popup constants
        private const int POPUP_W = 46;
        private const int POPUP_MAX_VISIBLE = 25;

        private bool _isOpen;
        private int _panel; // 0 = equipment, 1 = inventory, 2 = tinkering
        private InventoryScreenData.ScreenState _state;

        // Inventory list state (right panel)
        private int _cursorIndex;
        private int _scrollOffset;
        private List<DisplayRow> _rows = new List<DisplayRow>();

        // Equipment paperdoll state (left panel)
        private List<InventoryScreenData.EquipmentSlot> _equipSlots;
        private int _equipCursorIndex;

        // Tinkering tab state
        private enum TinkeringMode { Build, Mod }
        private TinkeringMode _tinkeringMode = TinkeringMode.Build;
        private int _tinkerCursorIndex;
        private int _tinkerScrollOffset;
        private List<TinkeringRecipeRow> _tinkerRows = new List<TinkeringRecipeRow>();

        // Equip-from-slot popup (equipment panel)
        private EquipPopupState _equipPopup;

        // Displacement confirmation popup
        private DisplaceConfirmState _displaceConfirm;

        // Item action popup (inventory panel)
        private ItemActionPopupState _itemActionPopup;

        public bool IsOpen => _isOpen;

        private struct DisplayRow
        {
            public bool IsHeader;
            public string Text;
            public Color Color;
            public InventoryScreenData.ItemDisplay Item;
        }

        private struct TinkeringRecipeRow
        {
            public TinkerRecipe Recipe;
            public string Name;
            public string Cost;
            public bool HasBits;
            public bool HasIngredient;
            public bool Affordable;
        }

        private class EquipPopupState
        {
            public InventoryScreenData.EquipmentSlot TargetSlot;
            public List<Entity> Items;
            public int CursorIndex;
            public int ScrollOffset;
            public bool HasRemoveOption;

            public int TotalRows => (HasRemoveOption ? 1 : 0) + Items.Count;
            public bool CursorOnRemove => HasRemoveOption && CursorIndex == 0;
            public int CursorItemIndex => HasRemoveOption ? CursorIndex - 1 : CursorIndex;
        }

        private class DisplaceConfirmState
        {
            public Entity ItemToEquip;
            public BodyPart TargetPart; // null for auto-equip
            public List<InventorySystem.Displacement> Displacements;
            public bool CursorOnYes; // true = Yes selected, false = No selected

            // Which popup to close after confirming (tracks origin)
            public bool FromEquipPopup;
            public bool FromItemAction;
        }

        private class ItemActionPopupState
        {
            public Entity Item;
            public InventoryScreenData.ItemDisplay ItemDisplay;
            public List<ItemAction> Actions;
            public int CursorIndex;

            // Body part picker sub-state (for manual equip)
            public List<BodyPart> BodyParts;
            public int BodyPartCursor;
            public bool InBodyPartPicker;
        }

        private struct ItemAction
        {
            public string Label;
            public string Command;
            public Entity Container;
        }

        public void Open()
        {
            if (PlayerEntity == null) return;
            _isOpen = true;
            _panel = PANEL_EQUIPMENT;
            _cursorIndex = 0;
            _scrollOffset = 0;
            _equipCursorIndex = 0;
            _tinkerCursorIndex = 0;
            _tinkerScrollOffset = 0;
            _equipPopup = null;
            _displaceConfirm = null;
            _itemActionPopup = null;
            Rebuild();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
            _equipPopup = null;
            _displaceConfirm = null;
            _itemActionPopup = null;
        }

        // ===== Input =====

        public bool HandleInput()
        {
            if (!_isOpen) return false;

            // Update selection to follow mouse hover
            UpdateMouseHover();

            // Popups intercept all input when active
            if (_displaceConfirm != null)
            {
                HandleDisplaceConfirmInput();
                return true;
            }
            if (_equipPopup != null)
            {
                HandleEquipPopupInput();
                return true;
            }
            if (_itemActionPopup != null)
            {
                HandleItemActionPopupInput();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.I))
            {
                Close();
                return true;
            }

            // Mouse click on equipment square (works from either panel)
            if (Input.GetMouseButtonDown(0))
            {
                if (_panel == PANEL_TINKERING)
                {
                    int clickedTinkerRow = GetTinkeringRowAtMouse();
                    if (clickedTinkerRow >= 0)
                    {
                        _tinkerCursorIndex = clickedTinkerRow;
                        Render();
                    }
                    return true;
                }

                int clickedSlot = GetEquipSlotAtMouse();
                if (clickedSlot >= 0)
                {
                    _panel = PANEL_EQUIPMENT;
                    _equipCursorIndex = clickedSlot;
                    OpenEquipPopup(_equipSlots[clickedSlot]);
                    return true;
                }

                // Mouse click on inventory item row → open item action popup
                int clickedRow = GetInventoryRowAtMouse();
                if (clickedRow >= 0)
                {
                    _panel = PANEL_INVENTORY;
                    _cursorIndex = clickedRow;
                    var clickedItem = _rows[clickedRow];
                    if (clickedItem.Item != null)
                        OpenItemActionPopup(clickedItem.Item);
                    else
                        Render();
                    return true;
                }
            }

            if (_panel == PANEL_EQUIPMENT)
                HandleEquipPanelInput();
            else if (_panel == PANEL_INVENTORY)
                HandleInventoryPanelInput();
            else
                HandleTinkeringPanelInput();

            return true;
        }

        private void UpdateMouseHover()
        {
            var grid = MouseToGrid();
            if (grid.x < 0) return;

            if (_displaceConfirm != null)
            {
                int button = GetDisplaceConfirmButtonAtMouse();
                if (button == 0 && !_displaceConfirm.CursorOnYes)
                {
                    _displaceConfirm.CursorOnYes = true;
                    Render();
                }
                else if (button == 1 && _displaceConfirm.CursorOnYes)
                {
                    _displaceConfirm.CursorOnYes = false;
                    Render();
                }
                return;
            }

            if (_equipPopup != null)
            {
                int row = GetPopupRowAtMouse();
                if (row >= 0 && row != _equipPopup.CursorIndex)
                {
                    _equipPopup.CursorIndex = row;
                    Render();
                }
                return;
            }

            if (_itemActionPopup != null)
            {
                int row = GetItemActionPopupRowAtMouse();
                if (row >= 0)
                {
                    if (_itemActionPopup.InBodyPartPicker)
                    {
                        if (row != _itemActionPopup.BodyPartCursor)
                        {
                            _itemActionPopup.BodyPartCursor = row;
                            Render();
                        }
                    }
                    else
                    {
                        if (row != _itemActionPopup.CursorIndex)
                        {
                            _itemActionPopup.CursorIndex = row;
                            Render();
                        }
                    }
                }
                return;
            }

            if (_panel == PANEL_TINKERING)
            {
                int hoveredTinkerRow = GetTinkeringRowAtMouse();
                if (hoveredTinkerRow >= 0 && hoveredTinkerRow != _tinkerCursorIndex)
                {
                    _tinkerCursorIndex = hoveredTinkerRow;
                    Render();
                }
                return;
            }

            // Main panels — equipment slots
            int hoveredSlot = GetEquipSlotAtMouse();
            if (hoveredSlot >= 0)
            {
                if (_panel != PANEL_EQUIPMENT || _equipCursorIndex != hoveredSlot)
                {
                    _panel = PANEL_EQUIPMENT;
                    _equipCursorIndex = hoveredSlot;
                    Render();
                }
                return;
            }

            // Main panels — inventory rows
            int hoveredRow = GetInventoryRowAtMouse();
            if (hoveredRow >= 0)
            {
                if (_panel != PANEL_INVENTORY || _cursorIndex != hoveredRow)
                {
                    _panel = PANEL_INVENTORY;
                    _cursorIndex = hoveredRow;
                    Render();
                }
                return;
            }
        }

        private void HandleEquipPanelInput()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                _panel = PANEL_TINKERING;
                Render();
                return;
            }

            // Up/Down: spatial navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                MoveEquipCursor(0, -1);
                Render();
                return;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                MoveEquipCursor(0, 1);
                Render();
                return;
            }

            // Left: spatial navigation within paperdoll
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.H))
            {
                MoveEquipCursor(-1, 0);
                Render();
                return;
            }

            // Right: spatial navigation, or switch to inventory panel if no slot to the right
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.L))
            {
                int prev = _equipCursorIndex;
                MoveEquipCursor(1, 0);
                if (_equipCursorIndex == prev)
                {
                    // No slot to the right — switch to inventory panel
                    _panel = PANEL_INVENTORY;
                }
                Render();
                return;
            }

            // Tab: switch to inventory panel
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _panel = PANEL_INVENTORY;
                Render();
                return;
            }

            if (_equipSlots != null && _equipSlots.Count > 0
                && _equipCursorIndex < _equipSlots.Count)
            {
                var slot = _equipSlots[_equipCursorIndex];

                // Unequip
                if (slot.EquippedItem != null && Input.GetKeyDown(KeyCode.E))
                {
                    TryUnequipViaCommand(slot.EquippedItem);
                    Rebuild();
                    if (_equipSlots != null && _equipCursorIndex >= _equipSlots.Count)
                        _equipCursorIndex = _equipSlots.Count > 0 ? _equipSlots.Count - 1 : 0;
                    Render();
                    return;
                }

                // Open equip popup
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    OpenEquipPopup(slot);
                    return;
                }
            }
        }

        private void HandleInventoryPanelInput()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                _panel = PANEL_TINKERING;
                Render();
                return;
            }

            // Up/Down: list navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                MoveCursor(-1);
                Render();
                return;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                MoveCursor(1);
                Render();
                return;
            }

            // Left: switch to equipment panel
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.H))
            {
                _panel = PANEL_EQUIPMENT;
                Render();
                return;
            }

            // Right: switch to tinkering tab
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.L))
            {
                _panel = PANEL_TINKERING;
                Render();
                return;
            }

            // Tab: switch to tinkering tab
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _panel = PANEL_TINKERING;
                Render();
                return;
            }

            if (_rows.Count > 0 && _cursorIndex < _rows.Count)
            {
                var row = _rows[_cursorIndex];
                if (row.Item != null)
                {
                    // Drop (quick shortcut)
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        TryDropViaCommand(row.Item.Item);
                        Rebuild();
                        ClampCursor();
                        Render();
                        return;
                    }

                    // Open item action popup (equip/unequip/use/etc.)
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
                    {
                        OpenItemActionPopup(row.Item);
                        return;
                    }
                }
            }
        }

        private void HandleTinkeringPanelInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.H))
            {
                _panel = PANEL_INVENTORY;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _panel = PANEL_EQUIPMENT;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                _tinkeringMode = TinkeringMode.Build;
                _tinkerCursorIndex = 0;
                _tinkerScrollOffset = 0;
                Rebuild();
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                _tinkeringMode = TinkeringMode.Mod;
                _tinkerCursorIndex = 0;
                _tinkerScrollOffset = 0;
                Rebuild();
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                MoveTinkeringCursor(-1);
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                MoveTinkeringCursor(1);
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                TryCraftSelectedRecipeViaCommand();
                Rebuild();
                Render();
                return;
            }
        }

        // ===== Equip Popup =====

        private void OpenEquipPopup(InventoryScreenData.EquipmentSlot slot)
        {
            var compatible = BuildCompatibleItems(slot);
            _equipPopup = new EquipPopupState
            {
                TargetSlot = slot,
                Items = compatible,
                CursorIndex = 0,
                ScrollOffset = 0,
                HasRemoveOption = slot.EquippedItem != null
            };
            Render();
        }

        private List<Entity> BuildCompatibleItems(InventoryScreenData.EquipmentSlot slot)
        {
            var inventory = PlayerEntity.GetPart<InventoryPart>();
            if (inventory == null) return new List<Entity>();

            string slotType = slot.SlotName;
            var compatible = new List<Entity>();

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                var item = inventory.Objects[i];
                var equippable = item.GetPart<EquippablePart>();
                if (equippable == null) continue;

                string[] slots = equippable.GetSlotArray();
                for (int j = 0; j < slots.Length; j++)
                {
                    if (slots[j].Trim() == slotType)
                    {
                        compatible.Add(item);
                        break;
                    }
                }
            }

            compatible.Sort((a, b) =>
            {
                string catA = ItemCategory.GetCategory(a);
                string catB = ItemCategory.GetCategory(b);
                int catOrdA = ItemCategory.GetSortOrder(catA);
                int catOrdB = ItemCategory.GetSortOrder(catB);
                int cmp = catOrdA.CompareTo(catOrdB);
                if (cmp != 0) return cmp;
                return string.Compare(a.GetDisplayName(), b.GetDisplayName(), StringComparison.Ordinal);
            });

            return compatible;
        }

        private void HandleEquipPopupInput()
        {
            int totalRows = _equipPopup.TotalRows;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _equipPopup = null;
                Render();
                return;
            }

            // Mouse click: act on popup row or close if outside
            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetPopupRowAtMouse();
                if (clickedRow >= 0 && totalRows > 0)
                {
                    if (_equipPopup.HasRemoveOption && clickedRow == 0)
                    {
                        UnequipFromPopup();
                        return;
                    }
                    int itemIdx = clickedRow - (_equipPopup.HasRemoveOption ? 1 : 0);
                    if (itemIdx >= 0 && itemIdx < _equipPopup.Items.Count)
                    {
                        EquipFromPopup(itemIdx);
                        return;
                    }
                }
                else if (clickedRow < 0)
                {
                    _equipPopup = null;
                    Render();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (totalRows > 0 && _equipPopup.CursorIndex > 0)
                {
                    _equipPopup.CursorIndex--;
                    ScrollPopupIntoView();
                }
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (totalRows > 0 && _equipPopup.CursorIndex < totalRows - 1)
                {
                    _equipPopup.CursorIndex++;
                    ScrollPopupIntoView();
                }
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) && totalRows > 0)
            {
                if (_equipPopup.CursorOnRemove)
                {
                    UnequipFromPopup();
                    return;
                }
                EquipFromPopup(_equipPopup.CursorItemIndex);
                return;
            }

            int itemCount = _equipPopup.Items.Count;
            for (int i = 0; i < 26 && i < itemCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + i))
                {
                    EquipFromPopup(i);
                    return;
                }
            }
        }

        private void ScrollPopupIntoView()
        {
            int visible = Mathf.Min(_equipPopup.TotalRows, POPUP_MAX_VISIBLE);
            if (visible <= 0) return;
            if (_equipPopup.CursorIndex < _equipPopup.ScrollOffset)
                _equipPopup.ScrollOffset = _equipPopup.CursorIndex;
            else if (_equipPopup.CursorIndex >= _equipPopup.ScrollOffset + visible)
                _equipPopup.ScrollOffset = _equipPopup.CursorIndex - visible + 1;
        }

        private void EquipFromPopup(int index)
        {
            if (index < 0 || index >= _equipPopup.Items.Count) return;

            var item = _equipPopup.Items[index];
            var slot = _equipPopup.TargetSlot;

            BodyPart targetBodyPart = null;
            var body = PlayerEntity.GetPart<Body>();
            if (body != null && slot.BodyPartID > 0)
            {
                var parts = body.GetParts();
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].ID == slot.BodyPartID)
                    {
                        targetBodyPart = parts[i];
                        break;
                    }
                }
            }

            // Check for displacements and show confirmation if needed
            var displacements = InventorySystem.PreviewDisplacements(PlayerEntity, item, targetBodyPart);
            if (displacements.Count > 0)
            {
                _displaceConfirm = new DisplaceConfirmState
                {
                    ItemToEquip = item,
                    TargetPart = targetBodyPart,
                    Displacements = displacements,
                    CursorOnYes = true,
                    FromEquipPopup = true,
                    FromItemAction = false
                };
                Render();
                return;
            }

            TryEquipViaCommand(item, targetBodyPart);

            _equipPopup = null;
            Rebuild();
            Render();
        }

        private void UnequipFromPopup()
        {
            var slot = _equipPopup.TargetSlot;
            if (slot.EquippedItem != null)
                TryUnequipViaCommand(slot.EquippedItem);

            _equipPopup = null;
            Rebuild();
            Render();
        }

        // ===== Displacement Confirm Popup =====

        private void HandleDisplaceConfirmInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.N))
            {
                _displaceConfirm = null;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
                || Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.L)
                || Input.GetKeyDown(KeyCode.Tab))
            {
                _displaceConfirm.CursorOnYes = !_displaceConfirm.CursorOnYes;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                ConfirmDisplacement();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (_displaceConfirm.CursorOnYes)
                    ConfirmDisplacement();
                else
                {
                    _displaceConfirm = null;
                    Render();
                }
                return;
            }

            // Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                int button = GetDisplaceConfirmButtonAtMouse();
                if (button == 0) // Yes
                {
                    ConfirmDisplacement();
                    return;
                }
                else if (button == 1) // No
                {
                    _displaceConfirm = null;
                    Render();
                    return;
                }
            }
        }

        private void ConfirmDisplacement()
        {
            var item = _displaceConfirm.ItemToEquip;
            var target = _displaceConfirm.TargetPart;
            bool fromEquipPopup = _displaceConfirm.FromEquipPopup;

            _displaceConfirm = null;

            TryEquipViaCommand(item, target);

            if (fromEquipPopup)
                _equipPopup = null;
            else
                _itemActionPopup = null;

            Rebuild();
            ClampCursor();
            Render();
        }

        private int GetDisplaceConfirmButtonAtMouse()
        {
            var grid = MouseToGrid();
            if (grid.x < 0) return -1;

            var dc = _displaceConfirm;
            int contentRows = dc.Displacements.Count + 1; // +1 for "Will unequip:" header
            int totalRows = contentRows + 2; // +1 blank line, +1 button row
            int popupH = totalRows + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;
            int buttonY = popupY + 3 + contentRows + 1;

            if (grid.y != buttonY) return -1;

            // Yes button area
            int yesX = popupX + 4;
            if (grid.x >= yesX && grid.x < yesX + 5) return 0;

            // No button area
            int noX = popupX + 12;
            if (grid.x >= noX && grid.x < noX + 4) return 1;

            return -1;
        }

        // ===== Item Action Popup =====

        private void OpenItemActionPopup(InventoryScreenData.ItemDisplay itemDisplay)
        {
            var item = itemDisplay.Item;
            var actions = new List<ItemAction>();

            var equippable = item.GetPart<EquippablePart>();

            if (itemDisplay.IsEquipped)
            {
                actions.Add(new ItemAction { Label = "Unequip", Command = "unequip" });
            }
            else if (equippable != null)
            {
                actions.Add(new ItemAction { Label = "Equip (auto)", Command = "equip_auto" });
                actions.Add(new ItemAction { Label = "Equip (manual)", Command = "equip_manual" });
            }

            // Add item-specific actions from the event system
            if (itemDisplay.Actions != null)
            {
                for (int i = 0; i < itemDisplay.Actions.Count; i++)
                {
                    var a = itemDisplay.Actions[i];
                    actions.Add(new ItemAction { Label = a.Display, Command = a.Command });
                }
            }

            // Tinkering seam: disassemble carried tinkering items via inventory command pipeline.
            var tinkerItem = item.GetPart<TinkerItemPart>();
            bool canDisassemble = !itemDisplay.IsEquipped
                && ((tinkerItem != null && tinkerItem.CanDisassemble)
                    || item.HasPart<MeleeWeaponPart>());
            if (canDisassemble && TinkeringService.CanDisassemble(item, out _))
            {
                actions.Add(new ItemAction { Label = "Disassemble", Command = "disassemble" });
            }

            // Container interaction seam: put selected item in a specific nearby container.
            if (CurrentZone != null)
            {
                var containersAtFeet = InventorySystem.GetContainersAtFeet(PlayerEntity, CurrentZone);
                for (int i = 0; i < containersAtFeet.Count; i++)
                {
                    var container = containersAtFeet[i];
                    string label = $"Put in {container.GetDisplayName()}";
                    actions.Add(new ItemAction
                    {
                        Label = label,
                        Command = "put_container",
                        Container = container
                    });
                }
            }

            actions.Add(new ItemAction { Label = "Drop", Command = "drop" });

            _itemActionPopup = new ItemActionPopupState
            {
                Item = item,
                ItemDisplay = itemDisplay,
                Actions = actions,
                CursorIndex = 0,
                InBodyPartPicker = false
            };

            Render();
        }

        private void HandleItemActionPopupInput()
        {
            if (_itemActionPopup.InBodyPartPicker)
            {
                HandleBodyPartPickerInput();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _itemActionPopup = null;
                Render();
                return;
            }

            // Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetItemActionPopupRowAtMouse();
                if (clickedRow >= 0 && clickedRow < _itemActionPopup.Actions.Count)
                {
                    ExecuteItemAction(clickedRow);
                    return;
                }
                else if (clickedRow < 0)
                {
                    _itemActionPopup = null;
                    Render();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (_itemActionPopup.CursorIndex > 0)
                    _itemActionPopup.CursorIndex--;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (_itemActionPopup.CursorIndex < _itemActionPopup.Actions.Count - 1)
                    _itemActionPopup.CursorIndex++;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                ExecuteItemAction(_itemActionPopup.CursorIndex);
                return;
            }

            // Hotkeys a-z
            for (int i = 0; i < 26 && i < _itemActionPopup.Actions.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + i))
                {
                    ExecuteItemAction(i);
                    return;
                }
            }
        }

        private void ExecuteItemAction(int index)
        {
            if (index < 0 || index >= _itemActionPopup.Actions.Count) return;

            var action = _itemActionPopup.Actions[index];
            var item = _itemActionPopup.Item;

            switch (action.Command)
            {
                case "equip_auto":
                {
                    var displacements = InventorySystem.PreviewDisplacements(PlayerEntity, item);
                    if (displacements.Count > 0)
                    {
                        _displaceConfirm = new DisplaceConfirmState
                        {
                            ItemToEquip = item,
                            TargetPart = null,
                            Displacements = displacements,
                            CursorOnYes = true,
                            FromEquipPopup = false,
                            FromItemAction = true
                        };
                        Render();
                        break;
                    }
                    TryEquipViaCommand(item);
                    _itemActionPopup = null;
                    Rebuild();
                    ClampCursor();
                    Render();
                    break;
                }

                case "equip_manual":
                    OpenBodyPartPicker();
                    break;

                case "unequip":
                    TryUnequipViaCommand(item);
                    _itemActionPopup = null;
                    Rebuild();
                    ClampCursor();
                    Render();
                    break;

                case "drop":
                    TryDropViaCommand(item);
                    _itemActionPopup = null;
                    Rebuild();
                    ClampCursor();
                    Render();
                    break;

                case "disassemble":
                    TryDisassembleViaCommand(item);
                    _itemActionPopup = null;
                    Rebuild();
                    ClampCursor();
                    Render();
                    break;

                case "put_container":
                    TryPutInContainerViaCommand(item, action.Container);
                    _itemActionPopup = null;
                    Rebuild();
                    ClampCursor();
                    Render();
                    break;

                default:
                    // Item-specific action (eat, drink, apply, etc.)
                    TryPerformActionViaCommand(item, action.Command);
                    _itemActionPopup = null;
                    Rebuild();
                    ClampCursor();
                    Render();
                    break;
            }
        }

        /// <summary>
        /// Command-first drop seam.
        /// </summary>
        private bool TryDropViaCommand(Entity item)
        {
            if (item == null)
                return false;

            var result = InventorySystem.ExecuteCommand(
                new DropCommand(item),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;
            LogCommandFailure("Drop", result);
            return false;
        }

        /// <summary>
        /// Command-first equip seam.
        /// </summary>
        private bool TryEquipViaCommand(Entity item, BodyPart targetBodyPart = null)
        {
            if (item == null)
                return false;

            var result = InventorySystem.ExecuteCommand(
                new EquipCommand(item, targetBodyPart),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;
            LogCommandFailure("Equip", result);
            return false;
        }

        /// <summary>
        /// Command-first unequip seam.
        /// </summary>
        private bool TryUnequipViaCommand(Entity item)
        {
            if (item == null)
                return false;

            var result = InventorySystem.ExecuteCommand(
                new UnequipCommand(item),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;
            LogCommandFailure("Unequip", result);
            return false;
        }

        /// <summary>
        /// Command-first item action seam.
        /// </summary>
        private bool TryPerformActionViaCommand(Entity item, string actionCommand)
        {
            if (item == null || string.IsNullOrEmpty(actionCommand))
                return false;

            var result = InventorySystem.ExecuteCommand(
                new PerformInventoryActionCommand(item, actionCommand),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;
            LogCommandFailure($"PerformAction[{actionCommand}]", result);
            return false;
        }

        /// <summary>
        /// Tinkering seam: disassemble selected item through command execution.
        /// </summary>
        private bool TryDisassembleViaCommand(Entity item)
        {
            if (item == null)
                return false;

            var result = InventorySystem.ExecuteCommand(
                new DisassembleCommand(item),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;
            LogCommandFailure("Disassemble", result);
            return false;
        }

        private bool TryCraftSelectedRecipeViaCommand()
        {
            if (_tinkeringMode == TinkeringMode.Mod)
            {
                MessageLog.Add("Item modding tab is not implemented yet.");
                return false;
            }

            if (_tinkerRows.Count == 0 || _tinkerCursorIndex < 0 || _tinkerCursorIndex >= _tinkerRows.Count)
                return false;

            var selected = _tinkerRows[_tinkerCursorIndex];
            if (selected.Recipe == null || string.IsNullOrWhiteSpace(selected.Recipe.ID))
                return false;

            if (EntityFactory == null)
            {
                MessageLog.Add("Cannot craft: missing EntityFactory on InventoryUI.");
                return false;
            }

            if (!selected.HasIngredient)
            {
                MessageLog.Add("Missing required ingredient.");
                return false;
            }

            if (!selected.HasBits)
            {
                MessageLog.Add("Not enough bits.");
                return false;
            }

            var result = InventorySystem.ExecuteCommand(
                new CraftFromRecipeCommand(selected.Recipe.ID, EntityFactory),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;

            LogCommandFailure("CraftFromRecipe", result);
            return false;
        }

        /// <summary>
        /// Container flow seam: put selected item into a chosen nearby container
        /// through command execution.
        /// </summary>
        private bool TryPutInContainerViaCommand(Entity item, Entity container)
        {
            if (item == null)
                return false;

            if (container == null || container.GetPart<ContainerPart>() == null)
            {
                MessageLog.Add("There is no container here.");
                return false;
            }

            var result = InventorySystem.ExecuteCommand(
                new PutInContainerCommand(container, item),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;
            LogCommandFailure("PutInContainer", result);
            return false;
        }

        private static void LogCommandFailure(string commandName, InventoryCommandResult result)
        {
            var validation = result?.Validation;
            string validationCode = validation == null
                ? "None"
                : validation.ErrorCode.ToString();

            Debug.LogWarning(
                "[Inventory/Refactor] " +
                $"{commandName} command failed. " +
                $"Code={result?.ErrorCode}, Validation={validationCode}, Message={result?.ErrorMessage}");
        }

        private void OpenBodyPartPicker()
        {
            var item = _itemActionPopup.Item;
            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null) return;

            var body = PlayerEntity.GetPart<Body>();
            if (body == null) return;

            string[] slotTypes = equippable.GetSlotArray();
            string primarySlot = slotTypes[0].Trim();
            var parts = body.GetEquippableSlots(primarySlot);

            if (parts.Count == 0) return;

            _itemActionPopup.BodyParts = parts;
            _itemActionPopup.BodyPartCursor = 0;
            _itemActionPopup.InBodyPartPicker = true;
            Render();
        }

        private void HandleBodyPartPickerInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _itemActionPopup.InBodyPartPicker = false;
                Render();
                return;
            }

            // Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetItemActionPopupRowAtMouse();
                if (clickedRow >= 0 && clickedRow < _itemActionPopup.BodyParts.Count)
                {
                    EquipToBodyPart(clickedRow);
                    return;
                }
                else if (clickedRow < 0)
                {
                    _itemActionPopup.InBodyPartPicker = false;
                    Render();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (_itemActionPopup.BodyPartCursor > 0)
                    _itemActionPopup.BodyPartCursor--;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (_itemActionPopup.BodyPartCursor < _itemActionPopup.BodyParts.Count - 1)
                    _itemActionPopup.BodyPartCursor++;
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                EquipToBodyPart(_itemActionPopup.BodyPartCursor);
                return;
            }

            // Hotkeys a-z
            for (int i = 0; i < 26 && i < _itemActionPopup.BodyParts.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + i))
                {
                    EquipToBodyPart(i);
                    return;
                }
            }
        }

        private void EquipToBodyPart(int index)
        {
            if (index < 0 || index >= _itemActionPopup.BodyParts.Count) return;

            var part = _itemActionPopup.BodyParts[index];
            var item = _itemActionPopup.Item;

            // Check for displacements and show confirmation if needed
            var displacements = InventorySystem.PreviewDisplacements(PlayerEntity, item, part);
            if (displacements.Count > 0)
            {
                _displaceConfirm = new DisplaceConfirmState
                {
                    ItemToEquip = item,
                    TargetPart = part,
                    Displacements = displacements,
                    CursorOnYes = true,
                    FromEquipPopup = false,
                    FromItemAction = true
                };
                Render();
                return;
            }

            TryEquipViaCommand(item, part);

            _itemActionPopup = null;
            Rebuild();
            ClampCursor();
            Render();
        }

        // ===== Rebuild =====

        private void Rebuild()
        {
            _state = InventoryScreenData.Build(PlayerEntity);

            // Always rebuild both panels
            _rows.Clear();
            BuildItemRows();
            BuildTinkeringRows();

            _equipSlots = _state.Equipment;
            if (_equipSlots.Count > 0 && _equipCursorIndex >= _equipSlots.Count)
                _equipCursorIndex = _equipSlots.Count - 1;
        }

        private void BuildItemRows()
        {
            for (int i = 0; i < _state.Categories.Count; i++)
            {
                var cat = _state.Categories[i];
                _rows.Add(new DisplayRow
                {
                    IsHeader = true,
                    Text = cat.CategoryName,
                    Color = QudColorParser.BrightYellow
                });

                for (int j = 0; j < cat.Items.Count; j++)
                {
                    var item = cat.Items[j];
                    string name = item.Name;

                    string equipped = item.IsEquipped ? " [E]" : "";
                    string line = "  " + name;

                    // Pad/truncate for right panel width (49 cols, with weight/value at end)
                    int nameWidth = RIGHT_W - 16; // reserve 16 for weight/value/equipped
                    if (line.Length < nameWidth)
                        line = line + new string(' ', nameWidth - line.Length);
                    else
                        line = line.Substring(0, nameWidth);

                    line += item.Weight.ToString().PadLeft(4) + "lb ";
                    line += item.Value.ToString().PadLeft(4) + "$";
                    line += equipped;

                    _rows.Add(new DisplayRow
                    {
                        IsHeader = false,
                        Text = line,
                        Color = item.IsEquipped ? QudColorParser.BrightCyan : QudColorParser.Gray,
                        Item = item
                    });
                }
            }
        }

        private void BuildTinkeringRows()
        {
            _tinkerRows.Clear();

            if (PlayerEntity == null)
                return;

            var bitLocker = PlayerEntity.GetPart<BitLockerPart>();
            if (bitLocker == null)
                return;

            foreach (var recipe in TinkerRecipeRegistry.GetAllRecipes())
            {
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.ID))
                    continue;

                bool isBuild = string.Equals(recipe.Type, "Build", StringComparison.OrdinalIgnoreCase);
                bool isMod = string.Equals(recipe.Type, "Mod", StringComparison.OrdinalIgnoreCase);

                if (_tinkeringMode == TinkeringMode.Build && !isBuild)
                    continue;
                if (_tinkeringMode == TinkeringMode.Mod && !isMod)
                    continue;

                if (!bitLocker.KnowsRecipe(recipe.ID))
                    continue;

                string cost = BitCost.Normalize(recipe.Cost);
                bool hasBits = bitLocker.HasBits(cost);
                bool hasIngredient = HasAnyIngredient(recipe.Ingredient);

                _tinkerRows.Add(new TinkeringRecipeRow
                {
                    Recipe = recipe,
                    Name = !string.IsNullOrWhiteSpace(recipe.DisplayName) ? recipe.DisplayName : recipe.Blueprint,
                    Cost = cost,
                    HasBits = hasBits,
                    HasIngredient = hasIngredient,
                    Affordable = hasBits && hasIngredient
                });
            }

            _tinkerRows.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            if (_tinkerRows.Count == 0)
            {
                _tinkerCursorIndex = 0;
                _tinkerScrollOffset = 0;
                return;
            }

            if (_tinkerCursorIndex >= _tinkerRows.Count)
                _tinkerCursorIndex = _tinkerRows.Count - 1;
            if (_tinkerCursorIndex < 0)
                _tinkerCursorIndex = 0;

            int visibleRows = TINKER_LIST_END_Y - TINKER_LIST_START_Y + 1;
            if (_tinkerScrollOffset > _tinkerCursorIndex)
                _tinkerScrollOffset = _tinkerCursorIndex;
            if (_tinkerCursorIndex >= _tinkerScrollOffset + visibleRows)
                _tinkerScrollOffset = _tinkerCursorIndex - visibleRows + 1;
            if (_tinkerScrollOffset < 0)
                _tinkerScrollOffset = 0;
        }

        private bool HasAnyIngredient(string ingredientSpec)
        {
            if (string.IsNullOrWhiteSpace(ingredientSpec))
                return true;

            var inventory = PlayerEntity?.GetPart<InventoryPart>();
            if (inventory == null)
                return false;

            string[] options = ingredientSpec.Split(',');
            for (int i = 0; i < options.Length; i++)
            {
                string blueprint = options[i].Trim();
                if (string.IsNullOrEmpty(blueprint))
                    continue;

                if (PlayerHasIngredientBlueprint(inventory, blueprint))
                    return true;
            }

            return false;
        }

        private static bool PlayerHasIngredientBlueprint(InventoryPart inventory, string blueprint)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(blueprint))
                return false;

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                var item = inventory.Objects[i];
                if (string.Equals(item.BlueprintName, blueprint, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        // ===== Navigation =====

        private void MoveCursor(int delta)
        {
            if (_rows.Count == 0) return;

            int next = _cursorIndex + delta;

            while (next >= 0 && next < _rows.Count && _rows[next].IsHeader)
                next += delta;

            if (next >= 0 && next < _rows.Count)
                _cursorIndex = next;

            int viewIndex = _cursorIndex - _scrollOffset;
            if (viewIndex < 0)
                _scrollOffset = _cursorIndex;
            else if (viewIndex >= VISIBLE_ROWS)
                _scrollOffset = _cursorIndex - VISIBLE_ROWS + 1;
        }

        private void MoveTinkeringCursor(int delta)
        {
            if (_tinkerRows.Count == 0)
                return;

            _tinkerCursorIndex += delta;
            if (_tinkerCursorIndex < 0)
                _tinkerCursorIndex = 0;
            if (_tinkerCursorIndex >= _tinkerRows.Count)
                _tinkerCursorIndex = _tinkerRows.Count - 1;

            int visibleRows = TINKER_LIST_END_Y - TINKER_LIST_START_Y + 1;
            if (_tinkerCursorIndex < _tinkerScrollOffset)
                _tinkerScrollOffset = _tinkerCursorIndex;
            else if (_tinkerCursorIndex >= _tinkerScrollOffset + visibleRows)
                _tinkerScrollOffset = _tinkerCursorIndex - visibleRows + 1;

            if (_tinkerScrollOffset < 0)
                _tinkerScrollOffset = 0;
        }

        private void MoveEquipCursor(int dx, int dy)
        {
            if (_equipSlots == null || _equipSlots.Count <= 1) return;

            var current = _equipSlots[_equipCursorIndex];
            int cx = current.GridX;
            int cy = current.GridY;

            int bestIdx = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _equipSlots.Count; i++)
            {
                if (i == _equipCursorIndex) continue;
                var s = _equipSlots[i];
                int sx = s.GridX;
                int sy = s.GridY;

                if (dx < 0 && sx >= cx) continue;
                if (dx > 0 && sx <= cx) continue;
                if (dy < 0 && sy >= cy) continue;
                if (dy > 0 && sy <= cy) continue;

                float primaryDist, crossDist;
                if (dx != 0)
                {
                    primaryDist = Mathf.Abs(sx - cx);
                    crossDist = Mathf.Abs(sy - cy);
                }
                else
                {
                    primaryDist = Mathf.Abs(sy - cy);
                    crossDist = Mathf.Abs(sx - cx);
                }

                float dist = primaryDist + crossDist * 2f;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
                _equipCursorIndex = bestIdx;
        }

        private void ClampCursor()
        {
            if (_rows.Count == 0)
            {
                _cursorIndex = 0;
                return;
            }

            if (_cursorIndex >= _rows.Count)
                _cursorIndex = _rows.Count - 1;

            while (_cursorIndex > 0 && _rows[_cursorIndex].IsHeader)
                _cursorIndex--;

            if (_scrollOffset > _cursorIndex)
                _scrollOffset = _cursorIndex;
        }

        // ===== Rendering =====

        private void Render()
        {
            if (Tilemap == null) return;

            Tilemap.ClearAllTiles();

            // Tab bar
            DrawText(1, 0, "Equipment", _panel == PANEL_EQUIPMENT ? QudColorParser.White : QudColorParser.DarkGray);
            DrawText(13, 0, "Inventory", _panel == PANEL_INVENTORY ? QudColorParser.White : QudColorParser.DarkGray);
            DrawText(25, 0, "Tinkering", _panel == PANEL_TINKERING ? QudColorParser.White : QudColorParser.DarkGray);

            // Weight and drams on right side of header
            string info = "Wt:" + _state.CarriedWeight + "/" + _state.MaxCarryWeight
                        + " $" + _state.Drams;
            DrawText(W - info.Length - 1, 0, info, QudColorParser.Gray);

            // Horizontal separators
            DrawHLine(0, 1, W, QudColorParser.DarkGray);
            DrawHLine(0, CONTENT_END, W, QudColorParser.DarkGray);

            if (_panel == PANEL_TINKERING)
            {
                RenderTinkeringPanel();
            }
            else
            {
                // Vertical divider
                for (int y = 0; y < H; y++)
                    DrawChar(DIVIDER_X, y, '|', QudColorParser.DarkGray);

                // Left panel: paperdoll
                RenderPaperdoll();

                // Right panel: inventory list
                RenderInventoryList();

                // Popup overlays
                if (_equipPopup != null)
                    RenderEquipPopup();
                if (_itemActionPopup != null)
                    RenderItemActionPopup();
                if (_displaceConfirm != null)
                    RenderDisplaceConfirm();
            }

            // Action bar
            string actions;
            if (_displaceConfirm != null)
                actions = " [Y]es  [N]o  [Esc]cancel";
            else if (_equipPopup != null || _itemActionPopup != null)
                actions = " [Enter]select  [a-z]quick select  [Esc]cancel";
            else if (_panel == PANEL_EQUIPMENT)
                actions = " [Enter]equip [e]unequip [>]inventory [Esc]close";
            else if (_panel == PANEL_TINKERING)
                actions = " [Enter]craft [B]/[M]mode [<]inventory [Tab]cycle [Esc]close";
            else
                actions = " [d]rop [Enter]actions [<]equipment [>]tinkering [Esc]close";

            DrawText(0, H - 2, actions, QudColorParser.Gray);

            // Detail line
            RenderDetailLine();
        }

        private void RenderPaperdoll()
        {
            if (_equipSlots == null) return;

            for (int i = 0; i < _equipSlots.Count; i++)
            {
                bool selected = (i == _equipCursorIndex) && _panel == PANEL_EQUIPMENT && _equipPopup == null;
                DrawEquipmentSquare(_equipSlots[i], selected);
            }
        }

        private void RenderInventoryList()
        {
            int x0 = RIGHT_START;

            // Item count hint
            string countText = _state.TotalItems + " items";
            DrawText(x0 + 1, CONTENT_START, countText, QudColorParser.DarkGray);

            // Content rows
            int y = CONTENT_START + 1;
            int visibleRows = CONTENT_END - CONTENT_START - 1;
            for (int i = _scrollOffset; i < _rows.Count && y < CONTENT_END; i++, y++)
            {
                var row = _rows[i];
                bool selected = (i == _cursorIndex) && _panel == PANEL_INVENTORY;

                if (selected && !row.IsHeader)
                {
                    DrawChar(x0, y, '>', QudColorParser.White);
                    DrawText(x0 + 1, y, row.Text, QudColorParser.White);
                }
                else
                {
                    DrawText(x0 + 1, y, row.Text, row.Color);
                }
            }

            // Scroll indicators
            if (_scrollOffset > 0)
                DrawChar(W - 2, CONTENT_START + 1, '^', QudColorParser.Gray);
            if (_scrollOffset + visibleRows < _rows.Count)
                DrawChar(W - 2, CONTENT_END - 1, 'v', QudColorParser.Gray);
        }

        private void RenderTinkeringPanel()
        {
            for (int y = 0; y < CONTENT_END; y++)
                DrawChar(TINKER_DIVIDER_X, y, '|', QudColorParser.DarkGray);

            DrawHLine(0, 16, TINKER_DIVIDER_X, QudColorParser.DarkGray);
            DrawText(2, 2,
                _tinkeringMode == TinkeringMode.Build
                    ? "> Build    Mod"
                    : "  Build  > Mod",
                QudColorParser.White);

            RenderTinkeringRecipeList();
            RenderBitLockerPanel();
            RenderTinkeringDescription();
            RenderTinkeringIngredientPanel();
        }

        private void RenderTinkeringRecipeList()
        {
            int visibleRows = TINKER_LIST_END_Y - TINKER_LIST_START_Y + 1;
            int maxNameWidth = TINKER_DIVIDER_X - 12;

            if (_tinkeringMode == TinkeringMode.Mod)
            {
                DrawText(2, TINKER_LIST_START_Y, "Modding recipes: coming next phase.", QudColorParser.DarkGray);
                return;
            }

            if (_tinkerRows.Count == 0)
            {
                DrawText(2, TINKER_LIST_START_Y, "(no known build recipes)", QudColorParser.DarkGray);
                return;
            }

            int rowY = TINKER_LIST_START_Y;
            for (int i = _tinkerScrollOffset; i < _tinkerRows.Count && rowY <= TINKER_LIST_END_Y; i++, rowY++)
            {
                var row = _tinkerRows[i];
                bool selected = i == _tinkerCursorIndex;

                string name = row.Name ?? "(unnamed recipe)";
                if (name.Length > maxNameWidth)
                    name = name.Substring(0, maxNameWidth - 1) + "~";

                string cost = "<" + (string.IsNullOrEmpty(row.Cost) ? "-" : row.Cost) + ">";
                int costX = TINKER_DIVIDER_X - cost.Length - 2;

                Color lineColor = row.Affordable ? QudColorParser.Gray : QudColorParser.BrightRed;
                if (selected)
                {
                    DrawChar(1, rowY, '>', QudColorParser.White);
                    lineColor = QudColorParser.White;
                }

                DrawText(3, rowY, name, lineColor);
                DrawText(costX, rowY, cost, selected ? QudColorParser.BrightYellow : lineColor);
            }

            if (_tinkerScrollOffset > 0)
                DrawChar(TINKER_DIVIDER_X - 2, TINKER_LIST_START_Y, '^', QudColorParser.Gray);
            if (_tinkerScrollOffset + visibleRows < _tinkerRows.Count)
                DrawChar(TINKER_DIVIDER_X - 2, TINKER_LIST_END_Y, 'v', QudColorParser.Gray);
        }

        private void RenderBitLockerPanel()
        {
            int x0 = TINKER_DIVIDER_X + 2;
            DrawText(x0, 2, "Bit Locker", QudColorParser.BrightYellow);

            var bitLocker = PlayerEntity?.GetPart<BitLockerPart>();
            var selected = GetSelectedTinkeringRow();
            Dictionary<char, int> required = selected.HasValue
                ? BitCost.ToCounts(selected.Value.Cost)
                : new Dictionary<char, int>();

            for (int i = 0; i < TinkerBitOrder.Length; i++)
            {
                char bit = TinkerBitOrder[i];
                int count = bitLocker != null ? bitLocker.GetBitCount(bit) : 0;
                int y = 3 + i;
                if (y >= CONTENT_END - 1)
                    break;

                Color bitColor = QudColorParser.CharToColor(bit);
                DrawText(x0, y, bit.ToString(), bitColor);

                if (required.TryGetValue(bit, out int need) && need > 0)
                {
                    bool hasEnough = count >= need;
                    DrawText(x0 + 2, y, hasEnough ? "v" : "x", hasEnough ? QudColorParser.BrightGreen : QudColorParser.BrightRed);
                    DrawText(W - 9, y, count.ToString().PadLeft(3) + "/" + need.ToString().PadRight(3), QudColorParser.Gray);
                }
                else
                {
                    DrawText(x0 + 2, y, "-", QudColorParser.DarkGray);
                    DrawText(W - 6, y, count.ToString().PadLeft(4), QudColorParser.Gray);
                }
            }
        }

        private void RenderTinkeringDescription()
        {
            var selected = GetSelectedTinkeringRow();
            if (!selected.HasValue || selected.Value.Recipe == null)
                return;

            int x0 = 2;
            int y = TINKER_DESC_START_Y;
            var recipe = selected.Value.Recipe;

            DrawText(x0, y, selected.Value.Name, QudColorParser.BrightYellow);
            y++;
            DrawText(x0, y, "Blueprint: " + recipe.Blueprint, QudColorParser.Gray);
            y++;
            DrawText(x0, y, "Cost: <" + (string.IsNullOrEmpty(selected.Value.Cost) ? "-" : selected.Value.Cost) + ">", QudColorParser.Gray);
            y++;
            DrawText(x0, y, "Produces: " + Mathf.Max(1, recipe.NumberMade), QudColorParser.Gray);
        }

        private void RenderTinkeringIngredientPanel()
        {
            var selected = GetSelectedTinkeringRow();
            if (!selected.HasValue || selected.Value.Recipe == null || string.IsNullOrWhiteSpace(selected.Value.Recipe.Ingredient))
                return;

            int x0 = TINKER_DIVIDER_X + 2;
            int y = 16;
            DrawText(x0, y, "Ingredient", QudColorParser.BrightYellow);
            y++;

            var inventory = PlayerEntity?.GetPart<InventoryPart>();
            string[] options = selected.Value.Recipe.Ingredient.Split(',');
            for (int i = 0; i < options.Length && y < CONTENT_END - 1; i++)
            {
                string blueprint = options[i].Trim();
                if (string.IsNullOrEmpty(blueprint))
                    continue;

                bool owned = PlayerHasIngredientBlueprint(inventory, blueprint);
                DrawText(x0, y, owned ? "v " : "x ", owned ? QudColorParser.BrightGreen : QudColorParser.BrightRed);
                DrawText(x0 + 2, y, blueprint, QudColorParser.Gray);
                y++;
            }
        }

        private TinkeringRecipeRow? GetSelectedTinkeringRow()
        {
            if (_tinkerRows.Count == 0 || _tinkerCursorIndex < 0 || _tinkerCursorIndex >= _tinkerRows.Count)
                return null;
            return _tinkerRows[_tinkerCursorIndex];
        }

        private void DrawEquipmentSquare(InventoryScreenData.EquipmentSlot slot, bool selected)
        {
            int x = slot.GridX;
            int y = slot.GridY;

            Color borderColor = selected ? QudColorParser.White : QudColorParser.DarkGray;

            // Top border: +---+
            DrawChar(x, y, '+', borderColor);
            for (int i = 1; i < BOX_W - 1; i++)
                DrawChar(x + i, y, '-', borderColor);
            DrawChar(x + BOX_W - 1, y, '+', borderColor);

            // Middle row: |   |
            DrawChar(x, y + 1, '|', borderColor);
            DrawChar(x + BOX_W - 1, y + 1, '|', borderColor);

            // Bottom border: +---+
            DrawChar(x, y + 2, '+', borderColor);
            for (int i = 1; i < BOX_W - 1; i++)
                DrawChar(x + i, y + 2, '-', borderColor);
            DrawChar(x + BOX_W - 1, y + 2, '+', borderColor);

            // Item glyph centered in box
            if (slot.EquippedItem != null)
            {
                var render = slot.EquippedItem.GetPart<RenderPart>();
                if (render != null && !string.IsNullOrEmpty(render.RenderString))
                {
                    char glyph = render.RenderString[0];
                    Color glyphColor = QudColorParser.Parse(render.ColorString);
                    DrawChar(x + 2, y + 1, glyph, glyphColor);
                }
            }

            // Label below box, centered on box width
            string label = slot.ShortLabel ?? slot.BodyPartName;
            int labelStart = x + (BOX_W - label.Length) / 2;
            if (labelStart < 0) labelStart = 0;
            Color labelColor = selected ? QudColorParser.BrightYellow : QudColorParser.Gray;
            DrawText(labelStart, y + BOX_H, label, labelColor);

            // Stats line below label (with 1 row gap after label)
            Entity statsEntity = slot.EquippedItem ?? slot.DefaultBehavior;
            if (statsEntity != null)
            {
                string stats = BuildSlotStats(statsEntity);
                if (stats.Length > 0)
                {
                    int statsStart = x + (BOX_W - stats.Length) / 2;
                    if (statsStart < 0) statsStart = 0;
                    DrawText(statsStart, y + BOX_H + 2, stats, QudColorParser.DarkCyan);
                }
            }
        }

        private string BuildSlotStats(Entity item)
        {
            var parts = new List<string>();

            var weapon = item.GetPart<MeleeWeaponPart>();
            if (weapon != null)
            {
                parts.Add(weapon.BaseDamage);
                if (weapon.HitBonus > 0) parts.Add("+" + weapon.HitBonus);
                else if (weapon.HitBonus < 0) parts.Add(weapon.HitBonus.ToString());
                if (weapon.PenBonus > 0) parts.Add(weapon.PenBonus + "PV");
                else if (weapon.PenBonus < 0) parts.Add(weapon.PenBonus + "PV");
            }

            var armor = item.GetPart<ArmorPart>();
            if (armor != null)
            {
                if (armor.AV != 0) parts.Add(armor.AV + "AV");
                if (armor.DV != 0) parts.Add(armor.DV + "DV");
            }

            if (parts.Count == 0)
            {
                var equippable = item.GetPart<EquippablePart>();
                if (equippable != null && !string.IsNullOrEmpty(equippable.EquipBonuses))
                {
                    string[] bonuses = equippable.EquipBonuses.Split(',');
                    for (int i = 0; i < bonuses.Length; i++)
                    {
                        string b = bonuses[i].Trim();
                        int colon = b.IndexOf(':');
                        if (colon < 0) continue;
                        string stat = b.Substring(0, colon);
                        string val = b.Substring(colon + 1);
                        if (stat.Length > 3) stat = stat.Substring(0, 3);
                        if (!val.StartsWith("-")) val = "+" + val;
                        parts.Add(stat + val);
                    }
                }
            }

            return string.Join(" ", parts);
        }

        private void RenderDetailLine()
        {
            if (_equipPopup != null)
            {
                if (_equipPopup.CursorOnRemove)
                {
                    string detail = "Remove " + (_equipPopup.TargetSlot.ItemName ?? "item")
                                  + " from " + _equipPopup.TargetSlot.BodyPartName;
                    DrawText(1, H - 1, detail, QudColorParser.BrightRed);
                }
                else if (_equipPopup.CursorItemIndex >= 0
                    && _equipPopup.CursorItemIndex < _equipPopup.Items.Count)
                {
                    var item = _equipPopup.Items[_equipPopup.CursorItemIndex];
                    var equip = item.GetPart<EquippablePart>();
                    string detail = item.GetDisplayName();
                    if (equip != null && !string.IsNullOrEmpty(equip.EquipBonuses))
                        detail += "  (" + equip.EquipBonuses + ")";
                    DrawText(1, H - 1, detail, QudColorParser.BrightCyan);
                }
            }
            else if (_itemActionPopup != null)
            {
                if (_itemActionPopup.InBodyPartPicker)
                {
                    if (_itemActionPopup.BodyParts != null
                        && _itemActionPopup.BodyPartCursor < _itemActionPopup.BodyParts.Count)
                    {
                        var part = _itemActionPopup.BodyParts[_itemActionPopup.BodyPartCursor];
                        string detail = part.GetDisplayName();
                        if (part._Equipped != null)
                            detail += ": " + part._Equipped.GetDisplayName() + " (will be unequipped)";
                        else
                            detail += ": (empty)";
                        DrawText(1, H - 1, detail, QudColorParser.BrightCyan);
                    }
                }
                else if (_itemActionPopup.CursorIndex < _itemActionPopup.Actions.Count)
                {
                    string detail = _itemActionPopup.Item.GetDisplayName();
                    var equip = _itemActionPopup.Item.GetPart<EquippablePart>();
                    if (equip != null && !string.IsNullOrEmpty(equip.EquipBonuses))
                        detail += "  (" + equip.EquipBonuses + ")";
                    DrawText(1, H - 1, detail, QudColorParser.BrightCyan);
                }
            }
            else if (_panel == PANEL_EQUIPMENT && _equipSlots != null && _equipCursorIndex < _equipSlots.Count)
            {
                var slot = _equipSlots[_equipCursorIndex];
                string detail = slot.BodyPartName + ": " + slot.ItemName;
                DrawText(1, H - 1, detail, QudColorParser.BrightCyan);
            }
            else if (_panel == PANEL_TINKERING)
            {
                var selected = GetSelectedTinkeringRow();
                if (!selected.HasValue)
                {
                    DrawText(1, H - 1, "No recipe selected.", QudColorParser.DarkGray);
                    return;
                }

                if (_tinkeringMode == TinkeringMode.Mod)
                {
                    DrawText(1, H - 1, "Modding flow is not implemented yet.", QudColorParser.DarkGray);
                    return;
                }

                if (!selected.Value.HasIngredient)
                {
                    DrawText(1, H - 1, "Missing ingredient for recipe.", QudColorParser.BrightRed);
                    return;
                }

                if (!selected.Value.HasBits)
                {
                    DrawText(1, H - 1, "Missing required bits.", QudColorParser.BrightRed);
                    return;
                }

                DrawText(1, H - 1, "Ready to craft. Press Enter.", QudColorParser.BrightGreen);
            }
            else if (_panel == PANEL_INVENTORY && _cursorIndex < _rows.Count && _rows[_cursorIndex].Item != null)
            {
                var item = _rows[_cursorIndex].Item;
                var itemActions = item.Actions;
                if (itemActions != null && itemActions.Count > 0)
                {
                    string actionStr = " Actions:";
                    for (int i = 0; i < itemActions.Count && i < 4; i++)
                    {
                        actionStr += " [" + itemActions[i].Display + "]";
                    }
                    DrawText(0, H - 1, actionStr, QudColorParser.DarkCyan);
                }
            }
        }

        private void RenderEquipPopup()
        {
            int totalRows = _equipPopup.TotalRows;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int popupH = visibleCount + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;

            ClearRegion(popupX, popupY, POPUP_W, popupH);

            // Top border
            DrawChar(popupX, popupY, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(popupX + i, popupY, '-', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, popupY, '+', QudColorParser.Gray);

            // Title row sides
            DrawChar(popupX, popupY + 1, '|', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, popupY + 1, '|', QudColorParser.Gray);

            // Separator under title
            int sepY = popupY + 2;
            DrawChar(popupX, sepY, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(popupX + i, sepY, '-', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, sepY, '+', QudColorParser.Gray);

            // Content row sides
            for (int r = 0; r < visibleCount; r++)
            {
                int ry = popupY + 3 + r;
                DrawChar(popupX, ry, '|', QudColorParser.Gray);
                DrawChar(popupX + POPUP_W - 1, ry, '|', QudColorParser.Gray);
            }

            // Bottom border
            int botY = popupY + 3 + visibleCount;
            DrawChar(popupX, botY, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(popupX + i, botY, '-', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, botY, '+', QudColorParser.Gray);

            // Title text
            string titleText = "Equip to: " + _equipPopup.TargetSlot.BodyPartName;
            if (titleText.Length > POPUP_W - 4)
                titleText = titleText.Substring(0, POPUP_W - 4);
            DrawText(popupX + 2, popupY + 1, titleText, QudColorParser.BrightYellow);

            // Content rows
            int contentY = popupY + 3;
            if (totalRows == 0)
            {
                DrawText(popupX + 2, contentY, "(no items available)", QudColorParser.DarkGray);
            }
            else
            {
                int removeOffset = _equipPopup.HasRemoveOption ? 1 : 0;

                for (int vi = 0; vi < visibleCount; vi++)
                {
                    int idx = _equipPopup.ScrollOffset + vi;
                    if (idx >= totalRows) break;

                    int rowY = contentY + vi;
                    bool selected = (idx == _equipPopup.CursorIndex);

                    if (selected)
                        DrawChar(popupX + 1, rowY, '>', QudColorParser.White);

                    if (_equipPopup.HasRemoveOption && idx == 0)
                    {
                        string removeName = _equipPopup.TargetSlot.ItemName ?? "item";
                        string removeText = "- Remove " + removeName;
                        int maxLen = POPUP_W - 5;
                        if (removeText.Length > maxLen)
                            removeText = removeText.Substring(0, maxLen - 1) + "~";
                        Color removeColor = selected ? QudColorParser.White : QudColorParser.BrightRed;
                        DrawText(popupX + 2, rowY, removeText, removeColor);
                    }
                    else
                    {
                        int itemIdx = idx - removeOffset;
                        var item = _equipPopup.Items[itemIdx];

                        if (itemIdx < 26)
                        {
                            char hotkey = (char)('a' + itemIdx);
                            string hk = hotkey + ")";
                            DrawText(popupX + 2, rowY, hk,
                                selected ? QudColorParser.White : QudColorParser.Gray);
                        }

                        var render = item.GetPart<RenderPart>();
                        if (render != null && !string.IsNullOrEmpty(render.RenderString))
                        {
                            char glyph = render.RenderString[0];
                            Color glyphColor = QudColorParser.Parse(render.ColorString);
                            DrawChar(popupX + 5, rowY, glyph, glyphColor);
                        }

                        string name = item.GetDisplayName();
                        int maxNameLen = POPUP_W - 9;
                        if (name.Length > maxNameLen)
                            name = name.Substring(0, maxNameLen - 1) + "~";
                        Color nameColor = selected ? QudColorParser.White : QudColorParser.Gray;
                        DrawText(popupX + 7, rowY, name, nameColor);
                    }
                }

                if (_equipPopup.ScrollOffset > 0)
                    DrawChar(popupX + POPUP_W - 2, contentY, '^', QudColorParser.Gray);
                if (_equipPopup.ScrollOffset + visibleCount < totalRows)
                    DrawChar(popupX + POPUP_W - 2, contentY + visibleCount - 1, 'v', QudColorParser.Gray);
            }
        }

        // ===== Item Action Popup Rendering =====

        private void RenderItemActionPopup()
        {
            if (_itemActionPopup.InBodyPartPicker)
            {
                RenderBodyPartPicker();
                return;
            }

            int totalRows = _itemActionPopup.Actions.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int popupH = visibleCount + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;

            ClearRegion(popupX, popupY, POPUP_W, popupH);
            DrawPopupBorder(popupX, popupY, POPUP_W, popupH, visibleCount);

            // Title: item name
            string titleText = _itemActionPopup.Item.GetDisplayName();
            if (titleText.Length > POPUP_W - 4)
                titleText = titleText.Substring(0, POPUP_W - 4);
            DrawText(popupX + 2, popupY + 1, titleText, QudColorParser.BrightYellow);

            // Content rows
            int contentY = popupY + 3;
            if (totalRows == 0)
            {
                DrawText(popupX + 2, contentY, "(no actions available)", QudColorParser.DarkGray);
            }
            else
            {
                for (int i = 0; i < visibleCount && i < totalRows; i++)
                {
                    int rowY = contentY + i;
                    bool selected = (i == _itemActionPopup.CursorIndex);

                    if (selected)
                        DrawChar(popupX + 1, rowY, '>', QudColorParser.White);

                    if (i < 26)
                    {
                        char hotkey = (char)('a' + i);
                        string hk = hotkey + ")";
                        DrawText(popupX + 2, rowY, hk,
                            selected ? QudColorParser.White : QudColorParser.Gray);
                    }

                    string label = _itemActionPopup.Actions[i].Label;
                    int maxLen = POPUP_W - 7;
                    if (label.Length > maxLen)
                        label = label.Substring(0, maxLen - 1) + "~";
                    Color labelColor = selected ? QudColorParser.White : QudColorParser.Gray;
                    DrawText(popupX + 5, rowY, label, labelColor);
                }
            }
        }

        private void RenderBodyPartPicker()
        {
            var parts = _itemActionPopup.BodyParts;
            int totalRows = parts.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int popupH = visibleCount + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;

            ClearRegion(popupX, popupY, POPUP_W, popupH);
            DrawPopupBorder(popupX, popupY, POPUP_W, popupH, visibleCount);

            // Title
            string titleText = "Equip to which body part?";
            DrawText(popupX + 2, popupY + 1, titleText, QudColorParser.BrightYellow);

            int contentY = popupY + 3;
            for (int i = 0; i < visibleCount && i < totalRows; i++)
            {
                int rowY = contentY + i;
                var part = parts[i];
                bool selected = (i == _itemActionPopup.BodyPartCursor);

                if (selected)
                    DrawChar(popupX + 1, rowY, '>', QudColorParser.White);

                if (i < 26)
                {
                    char hotkey = (char)('a' + i);
                    string hk = hotkey + ")";
                    DrawText(popupX + 2, rowY, hk,
                        selected ? QudColorParser.White : QudColorParser.Gray);
                }

                string partName = part.GetDisplayName();
                string equippedInfo = "";
                if (part._Equipped != null)
                    equippedInfo = " [" + part._Equipped.GetDisplayName() + "]";

                string line = partName + equippedInfo;
                int maxLen = POPUP_W - 7;
                if (line.Length > maxLen)
                    line = line.Substring(0, maxLen - 1) + "~";

                Color lineColor;
                if (selected)
                    lineColor = QudColorParser.White;
                else if (part._Equipped != null)
                    lineColor = QudColorParser.BrightCyan;
                else
                    lineColor = QudColorParser.Gray;

                DrawText(popupX + 5, rowY, line, lineColor);
            }
        }

        private void RenderDisplaceConfirm()
        {
            var dc = _displaceConfirm;
            int itemCount = dc.Displacements.Count;
            // Content: "Will unequip:" header + one line per displaced item + blank + button row
            int contentRows = itemCount + 1 + 1 + 1;
            int popupH = contentRows + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;

            ClearRegion(popupX, popupY, POPUP_W, popupH);
            DrawPopupBorder(popupX, popupY, POPUP_W, popupH, contentRows);

            // Title: "Equip <item name>?"
            string title = "Equip " + dc.ItemToEquip.GetDisplayName() + "?";
            if (title.Length > POPUP_W - 4)
                title = title.Substring(0, POPUP_W - 5) + "~?";
            DrawText(popupX + 2, popupY + 1, title, QudColorParser.BrightYellow);

            int cy = popupY + 3;

            // "Will unequip:" header
            DrawText(popupX + 2, cy, "Will unequip:", QudColorParser.Gray);
            cy++;

            // Displaced items
            for (int i = 0; i < itemCount; i++)
            {
                var d = dc.Displacements[i];
                string partName = d.BodyPart.GetDisplayName();
                string itemName = d.Item.GetDisplayName();
                string line = "  " + itemName + " in " + partName;
                int maxLen = POPUP_W - 4;
                if (line.Length > maxLen)
                    line = line.Substring(0, maxLen - 1) + "~";

                // Draw item glyph
                var render = d.Item.GetPart<RenderPart>();
                if (render != null && !string.IsNullOrEmpty(render.RenderString))
                {
                    Color glyphColor = QudColorParser.Parse(render.ColorString);
                    DrawChar(popupX + 3, cy, render.RenderString[0], glyphColor);
                }

                DrawText(popupX + 5, cy, line.Substring(2), QudColorParser.BrightRed);
                cy++;
            }

            // Blank line
            cy++;

            // Yes/No buttons
            bool yesSelected = dc.CursorOnYes;
            string yesText = yesSelected ? "[Yes]" : " Yes ";
            string noText = !yesSelected ? "[No]" : " No ";
            Color yesColor = yesSelected ? QudColorParser.White : QudColorParser.Gray;
            Color noColor = !yesSelected ? QudColorParser.White : QudColorParser.Gray;

            if (yesSelected)
                DrawChar(popupX + 3, cy, '>', QudColorParser.White);
            else
                DrawChar(popupX + 10, cy, '>', QudColorParser.White);

            DrawText(popupX + 4, cy, yesText, yesColor);
            DrawText(popupX + 12, cy, noText, noColor);
        }

        private void DrawPopupBorder(int x, int y, int w, int h, int contentRows)
        {
            // Top border
            DrawChar(x, y, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, y, '+', QudColorParser.Gray);

            // Title row sides
            DrawChar(x, y + 1, '|', QudColorParser.Gray);
            DrawChar(x + w - 1, y + 1, '|', QudColorParser.Gray);

            // Separator under title
            DrawChar(x, y + 2, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y + 2, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, y + 2, '+', QudColorParser.Gray);

            // Content row sides
            for (int r = 0; r < contentRows; r++)
            {
                DrawChar(x, y + 3 + r, '|', QudColorParser.Gray);
                DrawChar(x + w - 1, y + 3 + r, '|', QudColorParser.Gray);
            }

            // Bottom border
            int botY = y + 3 + contentRows;
            DrawChar(x, botY, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, botY, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, botY, '+', QudColorParser.Gray);
        }

        // ===== Mouse Helpers =====

        private Vector2Int MouseToGrid()
        {
            var cam = Camera.main;
            if (cam == null) return new Vector2Int(-1, -1);

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            int tx = Mathf.FloorToInt(world.x);
            int ty = Mathf.FloorToInt(world.y);
            int gx = tx;
            int gy = H - 1 - ty;

            if (gx < 0 || gx >= W || gy < 0 || gy >= H)
                return new Vector2Int(-1, -1);
            return new Vector2Int(gx, gy);
        }

        private int GetEquipSlotAtMouse()
        {
            if (_equipSlots == null) return -1;
            var grid = MouseToGrid();
            if (grid.x < 0) return -1;

            for (int i = 0; i < _equipSlots.Count; i++)
            {
                var s = _equipSlots[i];
                if (grid.x >= s.GridX && grid.x < s.GridX + BOX_W
                    && grid.y >= s.GridY && grid.y <= s.GridY + BOX_H)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the inventory list row index under the mouse, or -1 if none.
        /// </summary>
        private int GetInventoryRowAtMouse()
        {
            var grid = MouseToGrid();
            if (grid.x < 0 || grid.x < RIGHT_START) return -1;

            int contentStart = CONTENT_START + 1;
            if (grid.y < contentStart || grid.y >= CONTENT_END) return -1;

            int vi = grid.y - contentStart;
            int rowIdx = _scrollOffset + vi;
            if (rowIdx >= 0 && rowIdx < _rows.Count && !_rows[rowIdx].IsHeader)
                return rowIdx;
            return -1;
        }

        private int GetTinkeringRowAtMouse()
        {
            if (_panel != PANEL_TINKERING)
                return -1;

            var grid = MouseToGrid();
            if (grid.x < 0)
                return -1;

            if (grid.x < 1 || grid.x >= TINKER_DIVIDER_X)
                return -1;
            if (grid.y < TINKER_LIST_START_Y || grid.y > TINKER_LIST_END_Y)
                return -1;

            int index = _tinkerScrollOffset + (grid.y - TINKER_LIST_START_Y);
            if (index >= 0 && index < _tinkerRows.Count)
                return index;

            return -1;
        }

        private int GetPopupRowAtMouse()
        {
            var grid = MouseToGrid();
            if (grid.x < 0) return -1;

            int totalRows = _equipPopup.TotalRows;
            int visibleCount = Mathf.Min(totalRows, POPUP_MAX_VISIBLE);
            int popupH = visibleCount + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;
            int contentY = popupY + 3;

            if (grid.x > popupX && grid.x < popupX + POPUP_W - 1
                && grid.y >= contentY && grid.y < contentY + visibleCount)
            {
                int vi = grid.y - contentY;
                int rowIdx = _equipPopup.ScrollOffset + vi;
                if (rowIdx < totalRows)
                    return rowIdx;
            }
            return -1;
        }

        /// <summary>
        /// Returns the row index under the mouse in the item action popup, or -1 if outside.
        /// Works for both the action list and body part picker states.
        /// </summary>
        private int GetItemActionPopupRowAtMouse()
        {
            var grid = MouseToGrid();
            if (grid.x < 0) return -1;

            int totalRows;
            if (_itemActionPopup.InBodyPartPicker)
                totalRows = _itemActionPopup.BodyParts != null ? _itemActionPopup.BodyParts.Count : 0;
            else
                totalRows = _itemActionPopup.Actions.Count;

            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int popupH = visibleCount + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;
            int contentY = popupY + 3;

            if (grid.x > popupX && grid.x < popupX + POPUP_W - 1
                && grid.y >= contentY && grid.y < contentY + visibleCount)
            {
                int vi = grid.y - contentY;
                if (vi < totalRows)
                    return vi;
            }
            return -1;
        }

        // ===== Drawing Helpers =====

        private void ClearRegion(int x, int y, int width, int height)
        {
            for (int fy = y; fy < y + height; fy++)
            {
                for (int fx = x; fx < x + width; fx++)
                {
                    if (fx < 0 || fx >= W || fy < 0 || fy >= H) continue;
                    var tilePos = new Vector3Int(fx, H - 1 - fy, 0);
                    Tilemap.SetTile(tilePos, null);
                }
            }
        }

        private void DrawChar(int x, int y, char c, Color color)
        {
            if (Tilemap == null || x < 0 || x >= W || y < 0 || y >= H) return;
            var tilePos = new Vector3Int(x, H - 1 - y, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawText(int x, int y, string text, Color color)
        {
            if (text == null || Tilemap == null) return;

            for (int i = 0; i < text.Length && x + i < W; i++)
            {
                char c = text[i];
                if (c == ' ') continue;

                var tilePos = new Vector3Int(x + i, H - 1 - y, 0);
                var tile = CP437TilesetGenerator.GetTile(c);
                if (tile == null) continue;

                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
            }
        }

        private void DrawHLine(int x, int y, int width, Color color)
        {
            var tilePos = new Vector3Int(0, H - 1 - y, 0);
            var tile = CP437TilesetGenerator.GetTile('-');
            if (tile == null) return;

            for (int i = x; i < x + width && i < W; i++)
            {
                tilePos.x = i;
                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
            }
        }
    }
}
