using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        readonly Dictionary<Entity, ContainerPart> _expandedStorage = new Dictionary<Entity, ContainerPart>();

        bool CurrentStorageView(Entity vessel, ContainerPart container)
            => container != null && CarriedStorage.TryBrowse(PlayerEntity, vessel, out var current)
                && ReferenceEquals(current, container);

        bool IsStorageExpanded(Entity vessel, ContainerPart container)
            => _expandedStorage.TryGetValue(vessel, out var expanded) && ReferenceEquals(expanded, container);

        void SetStorageExpanded(Entity vessel, ContainerPart container, bool expanded)
        {
            if (!CurrentStorageView(vessel, container))
            {
                SetActionFailure("That vessel is no longer available in your pack.");
                Rebuild();
                Render();
                return;
            }

            ClearActionStatus();
            if (expanded) _expandedStorage[vessel] = container;
            else _expandedStorage.Remove(vessel);
            _itemActionPopup = null;
            _ledgerCommandsOpen = false;
            _panel = PANEL_INVENTORY;
            RebuildInventoryRows(vessel, DenseOrdinal);
            Render();
        }

        void AddInlineStorageRows(Entity vessel, ContainerPart container)
        {
            if (!IsStorageExpanded(vessel, container)) return;
            foreach (var item in container.Contents)
            {
                var display = new InventoryScreenData.ItemDisplay
                {
                    Item = item,
                    Name = item.GetDisplayName(),
                    Category = ItemCategory.GetCategory(item),
                    Weight = InventoryPart.GetItemWeight(item),
                    StackCount = item.GetPart<StackerPart>()?.StackCount ?? 1
                };
                _rows.Add(new DisplayRow
                {
                    Item = display,
                    Text = "    " + display.Name,
                    Color = LMuted,
                    StorageOwner = vessel,
                    StorageContainer = container
                });
            }
        }

        bool CurrentStorageChild(DisplayRow row)
            => row.StorageOwner != null && row.Item?.Item != null
                && IsStorageExpanded(row.StorageOwner, row.StorageContainer)
                && CurrentStorageView(row.StorageOwner, row.StorageContainer)
                && row.StorageContainer.Contents.Contains(row.Item.Item);

        bool TryGetSelectedStorageChild(out DisplayRow row)
        {
            row = _panel == PANEL_INVENTORY && !_inventorySearchActive && _cursorIndex >= 0 && _cursorIndex < _rows.Count
                ? _rows[_cursorIndex] : default;
            return row.StorageOwner != null;
        }

        string DescribeStorageChild(DisplayRow row)
        {
            if (!CurrentStorageChild(row)) return null;
            var item = row.Item.Item;
            var label = StorageLabels.Read(item);
            string text = "Stored in " + row.StorageOwner.GetDisplayName() + ".\nQuantity "
                + (item.GetPart<StackerPart>()?.StackCount ?? 1) + "\nWeight " + InventoryPart.GetItemWeight(item) + " lb";
            if (label.Mode != StorageLabelMode.None)
            {
                string mark = label.Mode == StorageLabelMode.Text ? label.Text : "color mark";
                text += "\nLabel " + StorageLabels.ColorNames[label.ColorIndex] + " " + mark;
            }
            return text;
        }

        void ActivatePackRow(int index, bool browseContents)
        {
            if (index < 0 || index >= _rows.Count) return;
            var row = _rows[index];
            if (row.IsHeader || row.Item == null) return;
            if (DensePack && index == _cursorIndex) RememberBrowseSelection();
            if (row.StorageOwner != null)
                OpenStoredItemActions(row);
            else if (browseContents && row.StorageContainer != null)
                SetStorageExpanded(row.Item.Item, row.StorageContainer, !IsStorageExpanded(row.Item.Item, row.StorageContainer));
            else
                OpenItemActionPopup(row.Item);
        }

        void OpenStoredItemActions(DisplayRow row)
        {
            if (!CurrentStorageChild(row))
            {
                SetActionFailure("That item is no longer in the selected storage.");
                Rebuild();
                Render();
                return;
            }

            ClearActionStatus();
            _ledgerCommands.Clear();
            _ledgerCommandIndex = 0;
            var actor = PlayerEntity;
            var zone = CurrentZone;
            var item = row.Item.Item;
            _ledgerCommandTitle = item.GetDisplayName() + " in " + row.StorageOwner.GetDisplayName();
            var tonic = CarriedTonicUse.Offer(actor, row.StorageOwner, item, zone);
            if (tonic != null)
                AddLedgerCommand(tonic.Label, () => UseStoredTonic(actor, zone, row, tonic), item);
            if (CarriedStorage.CanWithdraw(actor, row.StorageOwner, item, zone, out _))
                AddLedgerCommand("Take to pack", () => UseStoredItemAction(actor, zone, row, label: false));
            if (StorageLabels.CanEdit(actor, item))
                AddLedgerCommand("Label item", () => UseStoredItemAction(actor, zone, row, label: true));
            AddLedgerCommand("Back to pack", () => { });
            _ledgerCommandsOpen = true;
            Render();
        }

        void UseStoredTonic(Entity actor, Zone zone, DisplayRow row, CarriedTonicUse tonic)
        {
            if (PlayerEntity == actor && CurrentZone == zone && CurrentStorageChild(row))
            {
                var result = tonic.Execute();
                if (result.Success)
                {
                    _pendingEverydayTurn = true;
                    _closingActivationKey = KeyCode.None;
                    Close();
                    return;
                }
                SetActionFailure(result.ErrorMessage);
            }
            else SetActionFailure("That storage selection changed.");
            Rebuild();
            Render();
        }

        void UseStoredItemAction(Entity actor, Zone zone, DisplayRow row, bool label)
        {
            if (PlayerEntity != actor || CurrentZone != zone || !CurrentStorageChild(row))
            {
                SetActionFailure("That storage selection changed.");
                Rebuild();
                Render();
                return;
            }

            var item = row.Item.Item;
            if (label)
            {
                OpenStorageLabelEditor(item);
                return;
            }

            string name = item.GetDisplayName();
            if (CarriedStorage.TryWithdraw(actor, row.StorageOwner, item, zone, out string reason))
            {
                RebuildSelecting(row.StorageOwner);
                SetActionSuccess("Moved " + name + " to the pack.");
            }
            else
            {
                Rebuild();
                SetActionFailure(reason);
            }
            Render();
        }

        Rect IndentStorageRow(Rect bounds, DisplayRow row)
        {
            float indent;
            if (row.StorageOwner != null)
            {
                float midline = bounds.y + bounds.height / 2;
                LBox(new Rect(bounds.x + 5, bounds.y - 1, 1, bounds.height + 2), LBorder);
                LBox(new Rect(bounds.x + 5, midline, 10, 1), LBorder);
                indent = 42;
            }
            else if (row.StorageContainer != null)
            {
                LLabel(new Rect(bounds.x, bounds.y, 16, bounds.height),
                    IsStorageExpanded(row.Item.Item, row.StorageContainer) ? "v" : ">", LGold, 18);
                indent = 19;
            }
            else return bounds;
            return new Rect(bounds.x + indent, bounds.y, bounds.width - indent, bounds.height);
        }

        void PaintStorageRowActions(Rect bounds, int index, DisplayRow row)
        {
            LHit(bounds, LedgerHitKind.Item, index);
            if (row.StorageOwner != null || row.StorageContainer == null) return;
            var actions = new Rect(bounds.xMax - 31, bounds.y + 1, 29, bounds.height - 2);
            LFrame(actions, LBorder, LPanel);
            LLabel(new Rect(actions.x + 2, actions.y, actions.width - 4, actions.height), "...", LGold, 16);
            LHit(actions, LedgerHitKind.ItemActions, index);
        }

        string InlineStorageName(DisplayRow row, string name)
            => row.StorageOwner == null && row.StorageContainer != null
                && IsStorageExpanded(row.Item.Item, row.StorageContainer) && row.StorageContainer.Contents.Count == 0
                ? name + " (empty)" : name;

        bool RenderStoredItemSelection(Rect pane, DisplayRow row)
        {
            string contents = DescribeStorageChild(row);
            if (contents == null) return false;
            int columns = Mathf.Max(3, (int)((pane.width - 24) / 11));
            LLabel(new Rect(pane.x + 12, pane.y + 16, pane.width - 24, 52),
                SkillScreenText.Fit(row.Item.Item.GetDisplayName(), columns, 2), LGold, 22);
            LLabel(new Rect(pane.x + 12, pane.y + 72, pane.width - 24, pane.height - 72),
                SkillScreenText.Fit(contents, Mathf.Max(3, (int)((pane.width - 24) / 9)), 6), LMuted, 18);
            return true;
        }

        private void OpenCarriedStorageStow(Entity vessel, ContainerPart expected)
        {
            if (!CurrentStorageView(vessel, expected) || !CarriedStorage.IsCarried(PlayerEntity, vessel))
            {
                SetActionFailure("That vessel is no longer available in your pack.");
                Render();
                return;
            }

            _itemActionPopup = null;
            _ledgerCommands.Clear();
            _ledgerCommandIndex = 0;
            _ledgerCommandTitle = "STOW INTO " + vessel.GetDisplayName();
            var actor = PlayerEntity;
            var zone = CurrentZone;
            foreach (var item in actor.GetPart<InventoryPart>().Objects)
            {
                if (!CarriedStorage.CanDeposit(actor, vessel, item, zone, out _)) continue;
                AddLedgerCommand("Stow " + item.GetDisplayName(), () => StowInCarriedStorage(actor, vessel, expected, item, zone), item);
            }
            if (_ledgerCommands.Count == 0)
                AddLedgerCommand("No eligible items fit", () => OpenCarriedStorageStow(vessel, expected));
            AddLedgerCommand("View contents", () => SetStorageExpanded(vessel, expected, expanded: true));
            AddLedgerCommand("Back to pack", () => { });
            _ledgerCommandsOpen = true;
            Render();
        }

        private void StowInCarriedStorage(Entity actor, Entity vessel, ContainerPart container, Entity item, Zone zone)
        {
            if (PlayerEntity != actor || CurrentZone != zone || vessel.GetPart<ContainerPart>() != container)
            {
                SetActionFailure("That storage selection changed.");
                Render();
                return;
            }

            string name = item.GetDisplayName();
            if (CarriedStorage.TryDeposit(actor, vessel, item, zone, out string reason)) SetActionSuccess("Stowed " + name + ".");
            else SetActionFailure(reason);
            Rebuild();
            OpenCarriedStorageStow(vessel, container);
        }
    }
}
