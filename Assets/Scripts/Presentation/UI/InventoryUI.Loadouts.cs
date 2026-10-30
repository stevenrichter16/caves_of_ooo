using System;
using System.Linq;
using System.Text;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        Entity _loadoutKeeper;
        string _selectedLoadoutID;
        bool _loadoutNameEditing, _loadoutNameUntouched;
        string _loadoutName = "";
        const int LoadoutRows = 10;

        void ResetLoadoutView()
        {
            _loadoutKeeper = null;
            _selectedLoadoutID = null;
            _loadoutNameEditing = false;
            _loadoutName = "";
        }

        /// <summary>One panel for field use and an explicitly selected keeper.
        /// The keeper reference is a convenience, never authorization.</summary>
        public void OpenLoadouts(Entity keeper = null)
        {
            if (!_isOpen) Open();
            if (!_isOpen) return;
            _loadoutKeeper = keeper;
            _panel = PANEL_LOADOUTS;
            _selectedLoadoutID = PlayerLoadoutService.GetState(PlayerEntity)?.Saved.FirstOrDefault()?.ID;
            ClearActionStatus();
            Render();
        }

        SavedEquipmentLoadout SelectedLoadout()
        {
            var saved = PlayerLoadoutService.GetState(PlayerEntity)?.Saved;
            if (saved == null || saved.Count == 0) return null;
            return saved.FirstOrDefault(value => value.ID == _selectedLoadoutID) ?? saved[0];
        }

        bool SelectLoadout(int index)
        {
            var saved = PlayerLoadoutService.GetState(PlayerEntity)?.Saved;
            if (saved == null || index < 0 || index >= saved.Count) return false;
            if (_selectedLoadoutID == saved[index].ID) return false;
            _selectedLoadoutID = saved[index].ID;
            ClearActionStatus();
            return true;
        }

        void MoveLoadoutSelection(int delta)
        {
            var saved = PlayerLoadoutService.GetState(PlayerEntity)?.Saved;
            if (saved == null || saved.Count == 0) return;
            int current = saved.FindIndex(value => value.ID == SelectedLoadout()?.ID);
            SelectLoadout(Mathf.Clamp(current + delta, 0, saved.Count - 1));
        }

        void HandleLoadoutsInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.H))
            { _panel = PANEL_CRAFTING; Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.L)
                || InputHelper.GetKeyDown(KeyCode.Tab))
            { _panel = PANEL_EQUIPMENT; Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            { MoveLoadoutSelection(-1); Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
            { MoveLoadoutSelection(1); Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.Return)) { OpenLedgerCommands(); return; }
            if (InputHelper.GetKeyDown(KeyCode.S)) { BeginSaveLoadout(); return; }
            if (InputHelper.GetKeyDown(KeyCode.Alpha1)) { UsePreparedLoadout(0, KeyCode.Alpha1); return; }
            if (InputHelper.GetKeyDown(KeyCode.Alpha2)) { UsePreparedLoadout(1, KeyCode.Alpha2); return; }
        }

        void AddLoadoutCommands()
        {
            AddLedgerCommand("Save current equipment as a new loadout", BeginSaveLoadout);
            AddLedgerCommand("Use prepared A", () => UsePreparedLoadout(0));
            AddLedgerCommand("Use prepared B", () => UsePreparedLoadout(1));
            if (SelectedLoadout() == null) return;
            AddLedgerCommand("Prepare selected as A (outfitter)", () => PrepareSelectedLoadout(0));
            AddLedgerCommand("Prepare selected as B (outfitter)", () => PrepareSelectedLoadout(1));
            AddLedgerCommand("Delete selected saved loadout", ConfirmDeleteLoadout);
        }

        void ExecuteLoadoutAction(int action)
        {
            switch (action)
            {
                case 0: BeginSaveLoadout(); break;
                case 1: UsePreparedLoadout(0); break;
                case 2: UsePreparedLoadout(1); break;
                case 3: PrepareSelectedLoadout(0); break;
                case 4: PrepareSelectedLoadout(1); break;
                case 5: ConfirmDeleteLoadout(); break;
                case 6: SaveNamedLoadout(); break;
            }
        }

        void BeginSaveLoadout()
        {
            if (!PlayerLoadoutService.CanSaveCurrent(PlayerEntity, out string reason))
            { ShowActionFailure(reason); Render(); return; }
            var state = PlayerLoadoutService.GetState(PlayerEntity);
            int number = 1;
            while (state?.Saved.Any(value => string.Equals(value.Name, "Loadout " + number, StringComparison.OrdinalIgnoreCase)) == true)
                number++;
            _loadoutName = "Loadout " + number;
            _loadoutNameUntouched = true;
            _loadoutNameEditing = true;
            ClearActionStatus();
            Render();
        }

        void HandleLoadoutNameInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            { _loadoutNameEditing = false; Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.Return)) { SaveNamedLoadout(); return; }
            if (InputHelper.GetKeyboardKeyDown(KeyCode.Backspace))
            {
                _loadoutNameUntouched = false;
                if (_loadoutName.Length > 0) _loadoutName = _loadoutName.Substring(0, _loadoutName.Length - 1);
                Render(); return;
            }
            char typed = '\0';
            for (int i = 0; i < 26; i++)
                if (InputHelper.GetKeyboardKeyDown(KeyCode.A + i)) { typed = (char)('a' + i); break; }
            for (int i = 0; i < 10 && typed == '\0'; i++)
                if (InputHelper.GetKeyboardKeyDown(KeyCode.Alpha0 + i)) typed = (char)('0' + i);
            if (InputHelper.GetKeyboardKeyDown(KeyCode.Space)) typed = ' ';
            else if (InputHelper.GetKeyboardKeyDown(KeyCode.Minus)) typed = '-';
            else if (InputHelper.GetKeyboardKeyDown(KeyCode.Quote)) typed = '\'';
            if (typed == '\0') return;
            if (_loadoutNameUntouched) _loadoutName = "";
            _loadoutNameUntouched = false;
            if (_loadoutName.Length < 40) _loadoutName += typed;
            Render();
        }

        void SaveNamedLoadout()
        {
            if (!PlayerLoadoutService.SaveCurrent(PlayerEntity, _loadoutName, out var saved, out string reason))
            { ShowActionFailure(reason); Render(); return; }
            _selectedLoadoutID = saved.ID;
            _loadoutNameEditing = false;
            SetActionSuccess("Saved " + saved.Name + ". Equipment was not changed.");
            Render();
        }

        void UsePreparedLoadout(int slot, KeyCode activationKey = KeyCode.None)
        {
            ClearActionStatus();
            var result = PlayerLoadoutService.ApplyPrepared(PlayerEntity, slot, CurrentZone);
            if (!result.Success)
            { ShowActionFailure(result.ErrorMessage); Rebuild(); Render(); return; }
            if (result.ActionCommitted)
            {
                _pendingEverydayTurn = true;
                _closingActivationKey = activationKey != KeyCode.None ? activationKey
                    : InputHelper.GetKeyDown(KeyCode.Return) ? KeyCode.Return : KeyCode.None;
                Close();
                return;
            }
            SetActionSuccess("That loadout is already equipped.");
            Rebuild(); Render();
        }

        void PrepareSelectedLoadout(int slot)
        {
            var selected = SelectedLoadout();
            if (selected == null) return;
            if (!PlayerLoadoutService.Prepare(PlayerEntity, selected.ID, slot, _loadoutKeeper, out string reason))
                ShowActionFailure(reason);
            else SetActionSuccess(selected.Name + " is prepared as " + (slot == 0 ? "A" : "B") + ".");
            Render();
        }

        void ConfirmDeleteLoadout()
        {
            var selected = SelectedLoadout();
            if (selected == null) return;
            string id = selected.ID;
            _ledgerCommands.Clear(); _ledgerCommandIndex = 0;
            AddLedgerCommand("Keep " + selected.Name, () => { });
            AddLedgerCommand("Delete " + selected.Name, () =>
            {
                if (!PlayerLoadoutService.Delete(PlayerEntity, id, _loadoutKeeper, out string reason))
                    ShowActionFailure(reason);
                else
                {
                    _selectedLoadoutID = PlayerLoadoutService.GetState(PlayerEntity)?.Saved.FirstOrDefault()?.ID;
                    SetActionSuccess("Deleted the saved loadout. Your equipment was not changed.");
                }
            });
            _ledgerCommandsOpen = true;
            Render();
        }

        string DescribeSelectedLoadout()
        {
            var selected = SelectedLoadout();
            if (selected == null) return "No saved loadouts. Save your current equipment to begin.";
            var text = new StringBuilder(selected.Name);
            var preparedA = PlayerLoadoutService.GetPrepared(PlayerEntity, 0);
            var preparedB = PlayerLoadoutService.GetPrepared(PlayerEntity, 1);
            int slot = preparedA?.ID == selected.ID ? 0 : preparedB?.ID == selected.ID ? 1 : -1;
            if (slot >= 0)
            {
                text.Append("\n\nPrepared ").Append(slot == 0 ? "A" : "B").Append('.');
                if (!PlayerLoadoutService.CanApplyPrepared(PlayerEntity, slot, out string reason))
                    text.Append("\nCannot use now: ").Append(reason);
                else text.Append(PlayerLoadoutService.MatchesCurrent(PlayerEntity, selected)
                    ? " Already equipped." : " Switching costs one turn.");
            }
            else text.Append("\n\nSaved only. An outfitter must prepare it before you can use it in the field.");
            text.Append("\n\nRequires the exact saved items in your pack or on your body.");
            text.Append("\nF1 / Full details shows all ").Append(selected.Items.Count).Append(" item entries.");
            var inventory = PlayerEntity?.GetPart<InventoryPart>();
            var available = inventory == null ? Enumerable.Empty<Entity>()
                : inventory.Objects.Concat(inventory.GetAllEquipped()).Distinct();
            foreach (var item in selected.Items)
                text.Append("\n").Append(available.Any(value => value.ID == item.ItemID) ? "+ " : "MISSING: ")
                    .Append(item.ItemName).Append(" (").Append(string.Join(", ", item.BodyPartTypes)).Append(')');
            if (selected.Items.Count == 0) text.Append("\nNo worn items; using this loadout empties all equipment slots.");
            return text.ToString();
        }

        void LoadoutButton(Rect bounds, string label, int action, bool available = true)
        {
            LFrame(bounds, LBorder, LPanel);
            LLabel(new Rect(bounds.x + 10, bounds.y + 11, bounds.width - 20, 24), label, available ? LInk : LMuted, 18);
            // Refused actions remain selectable so the service can explain the live reason.
            LHit(bounds, LedgerHitKind.LoadoutAction, action);
        }

        void RenderLedgerLoadouts()
        {
            var state = PlayerLoadoutService.GetState(PlayerEntity);
            var selected = SelectedLoadout();
            LFrame(new Rect(24, 132, 398, 572), LGold, LPanel);
            LLabel(new Rect(40, 148, 364, 24), "SAVED LOADOUTS", LGold, 20);
            LLabel(new Rect(40, 178, 364, 42), "Save equipment snapshots.\nOnly two are prepared for field use.", LMuted, 16);
            int count = state?.Saved.Count ?? 0;
            int cursor = selected == null ? 0 : state.Saved.IndexOf(selected);
            int start = Mathf.Clamp(cursor - LoadoutRows + 1, 0, Mathf.Max(0, count - LoadoutRows));
            for (int row = 0; row < LoadoutRows && start + row < count; row++)
            {
                int index = start + row; var saved = state.Saved[index];
                Rect bounds = new Rect(32, 230 + row * 35, 382, 34);
                bool active = selected?.ID == saved.ID;
                if (active) LBox(bounds, LSelected);
                string prefix = state.PreparedA == saved.ID ? "[A] " : state.PreparedB == saved.ID ? "[B] " : "    ";
                LLabel(new Rect(42, bounds.y + 7, 362, 22), SkillScreenText.Fit(prefix + saved.Name, 36, 1), active ? LGold : LInk, 18);
                LHit(bounds, LedgerHitKind.LoadoutRow, index);
            }
            if (count == 0) LLabel(new Rect(40, 244, 350, 66), "No saved loadouts.\nEquip your gear, then choose\nSave current.", LMuted, 18);
            if (count > LoadoutRows) LLabel(new Rect(40, 590, 350, 22), (start + 1) + "-" + Math.Min(start + LoadoutRows, count) + " / " + count, LMuted, 16);
            LoadoutButton(new Rect(40, 632, 364, 46), "Save current as new...", 0);

            LFrame(new Rect(438, 132, 818, 120), LBorder, LPanel);
            for (int slot = 0; slot < 2; slot++)
            {
                float x = 454 + slot * 400;
                var saved = PlayerLoadoutService.GetPrepared(PlayerEntity, slot);
                LLabel(new Rect(x, 148, 378, 22), (slot == 0 ? "A  " : "B  ") + SkillScreenText.Fit(saved?.Name ?? "Not prepared", 32, 1), LGold, 18);
                LoadoutButton(new Rect(x, 185, 378, 46), "Use " + (slot == 0 ? "A" : "B") + " (one turn if changed)", slot + 1, saved != null);
            }
            LFrame(new Rect(438, 268, 818, 274), LBorder, LPanel);
            LLabel(new Rect(454, 284, 786, 246), SkillScreenText.Fit(DescribeSelectedLoadout(), 85, 12), LInk, 18);
            bool canPrepare = LoadoutKeeperPart.CanServe(PlayerEntity, _loadoutKeeper, out string keeperReason);
            string context = canPrepare ? "Preparing with " + _loadoutKeeper.GetDisplayName() + "."
                : keeperReason;
            LLabel(new Rect(454, 556, 786, 50), SkillScreenText.Fit(context, 85, 2), canPrepare ? LGreen : LMuted, 18);
            LoadoutButton(new Rect(454, 618, 252, 46), "Prepare selected as A", 3, canPrepare && selected != null);
            LoadoutButton(new Rect(718, 618, 252, 46), "Prepare selected as B", 4, canPrepare && selected != null);
            LoadoutButton(new Rect(982, 618, 258, 46), "Delete selected...", 5, selected != null);
            string initial = (state?.InitialPreparations ?? 0) < 2
                ? "Your first two saves fill the initial prepared slots." : "Saved snapshots stay fixed; save a new one to record changes.";
            LLabel(new Rect(454, 678, 786, 20), initial, LMuted, 16);
        }

        void RenderLoadoutNamePrompt()
        {
            if (!_loadoutNameEditing) return;
            _ledgerHits.Clear();
            LBox(new Rect(0, 0, 1280, 800), new Color(0, 0, 0, .8f));
            LFrame(new Rect(250, 225, 780, 310), LGold, LPanel);
            LLabel(new Rect(274, 249, 732, 28), "SAVE CURRENT EQUIPMENT", LGold, 24);
            LLabel(new Rect(274, 295, 732, 50), "Type a name, or accept the suggested name.\nThis creates a new snapshot and does not change your equipment.", LMuted, 18);
            LFrame(new Rect(274, 365, 732, 42), LGold, LSelected);
            LLabel(new Rect(286, 377, 708, 24), _loadoutName + "_", LInk, 20);
            LoadoutButton(new Rect(274, 450, 348, 46), "Enter / A  Save", 6);
            LedgerChoiceButton(new Rect(646, 450, 360, 46), "Esc / B  Cancel", LedgerHitKind.Cancel, false);
        }
    }
}
