using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        private string _inventoryQuery = "";
        private bool _inventorySearchActive;
        private Entity _searchReturnItem;
        public bool IsSearching => _inventorySearchActive || _loadoutNameEditing || StorageLabelEditorOpen;
        public string SearchQuery => _inventoryQuery;
        public void HideForDecisionReader() { SetLedgerVisible(false); }
        public void RestoreAfterDecisionReader() { if (_isOpen) { SetLedgerVisible(true); Rebuild(); ClampCursor(); Render(); } }
        public string SelectedDecisionDetails() => BuildSelectedDecisionDetails(out _);

        /// <summary>Preserves the selected decision prose while borrowing current examine
        /// metadata for a single owned item. Composite previews remain neutral. An invalidated
        /// item returns neither a document nor stale fallback text.</summary>
        public AnnouncementDocument SelectedDecisionPresentation(out string text)
        {
            var viewer = PlayerEntity;
            var zone = CurrentZone;
            text = BuildSelectedDecisionSnapshot(out var subject, out var sections);
            if (string.IsNullOrWhiteSpace(text) || subject == null) return null;
            var identity = sections == null ? ExamineDocumentBuilder.BuildInventory(subject, viewer)
                : ExamineDocumentBuilder.BuildInventorySnapshot(subject, viewer, sections);
            if (identity == null || !_isOpen || _inventorySearchActive
                || PlayerEntity != viewer || CurrentZone != zone)
            {
                text = null;
                return null;
            }
            var visibleSections = sections != null ? identity.Sections : identity.Sections
                .Where(section => section.Heading == RepairExamine.Heading)
                .Append(new AnnouncementSection("Decision details", text)).ToArray();
            var repair = visibleSections.FirstOrDefault(section => section.Heading == RepairExamine.Heading);
            if (repair != null && text.IndexOf(repair.Heading.ToUpperInvariant() + "\n", StringComparison.Ordinal) < 0)
                text = repair.Heading.ToUpperInvariant() + "\n" + repair.Text + "\n\n" + text;
            return new AnnouncementDocument(identity.Title, identity.Kind,
                visibleSections, identity.Facts,
                identity.BlueprintName, identity.Glyph, identity.GlyphColor,
                identity.Subject, identity.SubjectZone, identity.SubjectCell, identity.SubjectViewer);
        }

        private string BuildSelectedDecisionDetails(out Entity subject)
            => BuildSelectedDecisionSnapshot(out subject, out _);

        private string BuildSelectedDecisionSnapshot(out Entity subject, out IReadOnlyList<AnnouncementSection> sections)
        {
            sections = null;
            subject = null;
            if (!_isOpen || _inventorySearchActive) return null;
            if (_panel == PANEL_LOADOUTS && !_loadoutNameEditing) return DescribeSelectedLoadout();
            if (_displaceConfirm != null)
            {
                var pending = _displaceConfirm;
                var candidate = pending.ItemToEquip;
                var body = PlayerEntity?.GetPart<Body>();
                var render = candidate?.GetPart<RenderPart>();
                if (!InventoryDecisionDetails.Owned(PlayerEntity, candidate)
                    || body?.ParentEntity != PlayerEntity || render?.ParentEntity != candidate || !render.Visible
                    || candidate.GetPart<EquippablePart>()?.ParentEntity != candidate
                    || pending.Displacements == null || pending.Displacements.Count == 0) return null;
                // This is a multi-owner preview, not the candidate item's portrait.
                // Revalidate the exact captured consequences without executing equip.
                var current = InventorySystem.PreviewDisplacements(PlayerEntity, candidate, pending.TargetPart);
                if (current.Count != pending.Displacements.Count) return null;
                foreach (var displacement in current)
                {
                    var part = displacement.BodyPart; var item = displacement.Item;
                    var itemRender = item?.GetPart<RenderPart>();
                    if (part == null || part.ParentBody != body || part._Equipped != item
                        || !InventoryDecisionDetails.Owned(PlayerEntity, item)
                        || itemRender?.ParentEntity != item || !itemRender.Visible
                        || !pending.Displacements.Any(saved => ReferenceEquals(saved.Item, item)
                            && ReferenceEquals(saved.BodyPart, part))) return null;
                }
                return "Equip " + candidate.GetDisplayName() + "?\n\nWill unequip:\n"
                    + string.Join("\n", current.Select(displacement => displacement.BodyPart.GetDisplayName()
                        + ": " + displacement.Item.GetDisplayName()))
                    + "\n\nChoose Replace to confirm, or Keep current to cancel.";
            }
            if (_grimoirePicker != null)
            {
                int index = _grimoirePicker.CursorIndex;
                if (index < 0 || index >= _grimoirePicker.Grimoires.Count) return null;
                var candidate = _grimoirePicker.Grimoires[index];
                var abilities = GetAbilitiesPart();
                // Picker entries are snapshots. Only describe the same learned power
                // still owned by this character; ability prose has no item portrait.
                if (candidate == null || abilities == null
                    || !ReferenceEquals(abilities.GetAbility(candidate.ID), candidate)
                    || !abilities.AbilityList.Contains(candidate)
                    || !GrimoireTooltipData.TryGet(candidate.SourcePowerClass, out var tooltip)) return null;
                return tooltip.DisplayName + "\n\n" + tooltip.Flavor + "\n\n" + tooltip.Mechanics
                    + "\n" + tooltip.Signature + "\n\nCooldown remaining: " + candidate.CooldownRemaining;
            }
            if (_itemActionPopup != null)
            {
                var item = _itemActionPopup.Item;
                if (!InventoryDecisionDetails.Owned(PlayerEntity, item)) return null;
                if (_itemActionPopup.CursorIndex >= 0 && _itemActionPopup.CursorIndex < _itemActionPopup.Actions.Count)
                {
                    var action = _itemActionPopup.Actions[_itemActionPopup.CursorIndex];
                    if (CompanionCareActions.IsCommand(action.Command))
                    {
                        subject = item;
                        return CompanionCareActions.DescribeSelection(PlayerEntity, item, CurrentZone, action.Command);
                    }
                    if (action.Command == "put_container") return InventoryDecisionDetails.Container(PlayerEntity, CurrentZone, action.Container, item);
                    if (action.Command == "disassemble")
                    {
                        subject = item;
                        return item.GetDisplayName() + "\n\n" + InventoryDecisionDetails.Disassembly(PlayerEntity, item);
                    }
                }
                subject = item;
                return DetailsForItemSnapshot(item, out sections);
            }
            if (_modTargetPopup != null && _modTargetPopup.CursorIndex >= 0 && _modTargetPopup.CursorIndex < _modTargetPopup.Targets.Count)
            {
                var item = _modTargetPopup.Targets[_modTargetPopup.CursorIndex];
                subject = item;
                return InventoryDecisionDetails.Owned(PlayerEntity, item) ? InventoryDecisionDetails.Modification(item, _modTargetPopup.Recipe) : null;
            }
            if (_equipPopup != null && _equipPopup.CursorItemIndex >= 0 && _equipPopup.CursorItemIndex < _equipPopup.Items.Count)
            {
                subject = _equipPopup.Items[_equipPopup.CursorItemIndex];
                return DetailsForItemSnapshot(subject, out sections);
            }
            if (_panel == PANEL_ABILITIES)
            {
                var ability = GetAbilitiesPart()?.GetAbilityBySlot(_abilitiesCursorSlot);
                return ability == null ? null : AbilityDetailsBuilder.BuildForAbility(PlayerEntity, ability.ID);
            }
            if (_panel == PANEL_CRAFTING)
            {
                // Current selection is re-read through the existing pure previews.
                BuildCraftingRows();
                if (_craftingMode == CraftingMode.Recipes) return SelectedPackRecipeDetails();
                string text = _craftingMode == CraftingMode.Forge ? InventoryDecisionDetails.Forge(_forgePreview) : InventoryDecisionDetails.Brew(_brewPreview);
                IEnumerable<Entity> picks = _craftingMode == CraftingMode.Forge
                    ? new[] { _pickedBlade, _pickedHaft, _pickedBinding, _pickedQuench } : _pickedReagents;
                text += "\n\nSelected inputs: " + string.Join(", ", picks.Where(p => p != null).Select(p => p.GetDisplayName())) + ".";
                text += "\n" + (_atStation ? "At the required station." : "Away from the required station: one result at a time.");
                if (_craftBatchMax > 1) text += " Batch available at station: " + _craftBatchMax + ".";
                return text;
            }
            if (_panel == PANEL_TINKERING)
            {
                var row = GetSelectedTinkeringRow(); if (!row.HasValue) return null;
                var recipe = row.Value.Recipe;
                return recipe.DisplayName + "\n\n" + recipe.Description + "\nCost: " + recipe.Cost
                    + "\nProduces: " + recipe.NumberMade + "\nIngredient: " + (recipe.Ingredient ?? "none")
                    + (row.Value.Affordable ? "\nCurrent prerequisites met; execution rechecks." : "\nMissing current prerequisites; see the recipe list.");
            }
            if (_panel == PANEL_INVENTORY && _cursorIndex >= 0 && _cursorIndex < _rows.Count)
            {
                if (TryGetSelectedStorageChild(out var child))
                {
                    string summary = DescribeStorageChild(child);
                    return summary == null ? null : child.Item.Item.GetDisplayName() + "\n\n" + summary;
                }
                subject = _rows[_cursorIndex].Item?.Item;
                return DetailsForItemSnapshot(subject, out sections);
            }
            if (_panel == PANEL_EQUIPMENT && _equipCursorIndex >= 0 && _equipCursorIndex < _equipSlots.Count)
            {
                subject = _equipSlots[_equipCursorIndex].EquippedItem;
                return DetailsForItemSnapshot(subject, out sections);
            }
            return null;
        }
        private string DetailsForItemSnapshot(Entity item, out IReadOnlyList<AnnouncementSection> sections)
        {
            sections = null;
            if (item == null || !InventoryDecisionDetails.Owned(PlayerEntity, item)) return null;
            if (CellStatusReadout.TryFireLines(item, out _, out _))
            {
                var fire = ExamineDocumentBuilder.BuildInventory(item, PlayerEntity);
                if (fire == null) return null;
                sections = fire.Sections;
                return string.Join("\n\n", sections.Select(section => section.Heading + "\n" + section.Text));
            }
            if (item.HasPart<FoodPart>() && item.GetPart<CookablePart>()?.ParentEntity == item)
            {
                var examinable = item.GetPart<ExaminablePart>();
                string details = DetailsForItem(item);
                string use = UseGuides.SectionText(item, null, null, PlayerEntity);
                if (!InventoryDecisionDetails.Owned(PlayerEntity, item)
                    || !ReferenceEquals(examinable, item.GetPart<ExaminablePart>())) return null;
                if (!string.IsNullOrWhiteSpace(use))
                    sections = new[] { new AnnouncementSection("Decision details", details),
                        new AnnouncementSection(UseGuides.Heading, use) };
                return details;
            }
            // Equipment captures full prose and compact overviews in one validated pass.
            if (!item.HasPart<EquippablePart>() || item.HasPart<FoodPart>()) return DetailsForItem(item);
            var description = item.GetPart<ExaminablePart>();
            string flavor = description?.GetDescriptionText();
            if (!EquipmentComparisonService.TryDescribeSections(PlayerEntity, item, out var equipment, out _, () =>
            {
                var context = new List<AnnouncementSection>();
                var state = new List<string>();
                string publicState = _state?.Categories.SelectMany(c => c.Items).FirstOrDefault(d => d.Item == item)?.State;
                if (!string.IsNullOrWhiteSpace(publicState)) state.Add("State: " + publicState);
                var effects = item.GetPart<StatusEffectsPart>()?.GetAllEffects();
                if (effects != null)
                    foreach (var effect in effects)
                        if (effect != null && effect.Owner == item && effect.Duration != 0) state.Add(EffectDescriber.Describe(effect));
                // The torch section owns its validated flame/fuel reading. Keep
                // other attached state providers without publishing fuel twice.
                bool torch = item.HasPart<TorchLightPart>();
                StateReader.Collect(item, true, null, null, PlayerEntity, state,
                    part => (!torch || !(part is FuelPart)) && !(part is RepairablePart && RepairExamine.Section(item) != null));
                if (state.Count > 0)
                    context.Add(new AnnouncementSection("Current condition", string.Join("\n", state),
                        AnnouncementTone.Normal, "Condition: see full details."));
                string use = UseGuides.SectionText(item, null, null, PlayerEntity);
                if (!string.IsNullOrWhiteSpace(use))
                    context.Add(new AnnouncementSection(UseGuides.Heading, use, AnnouncementTone.Normal, "How to use: see full details."));
                return context.AsReadOnly();
            })) return null;
            var captured = new List<AnnouncementSection>(equipment);
            string handling = InventoryDecisionDetails.Handling(PlayerEntity, item);
            if (!string.IsNullOrWhiteSpace(handling))
                captured.Add(new AnnouncementSection("Handling", handling, AnnouncementTone.Normal,
                    "Carrying and throwing: see full details."));
            string disassembly = InventoryDecisionDetails.Disassembly(PlayerEntity, item);
            if (!string.IsNullOrWhiteSpace(disassembly))
                captured.Add(new AnnouncementSection("Disassembly", disassembly, AnnouncementTone.Normal, ""));
            if (!string.IsNullOrWhiteSpace(flavor))
                captured.Add(new AnnouncementSection("About this item", flavor.Trim(), AnnouncementTone.Muted, ""));
            if (!InventoryDecisionDetails.Owned(PlayerEntity, item)
                || !ReferenceEquals(description, item.GetPart<ExaminablePart>())) return null;
            sections = captured.AsReadOnly();
            return string.Join("\n\n", captured.Select(section => section.Heading + "\n" + section.Text));
        }

        private string DetailsForItem(Entity item)
        {
            if (item == null || !InventoryDecisionDetails.Owned(PlayerEntity, item)) return null;
            string state = _state?.Categories.SelectMany(c => c.Items).FirstOrDefault(d => d.Item == item)?.State;
            string details = DetailsForItemBody(item);
            return string.IsNullOrEmpty(state) ? details : "State: " + state + "\n\n" + details;
        }
        private string DetailsForItemBody(Entity item)
        {
            if (item.HasPart<FoodPart>()) return InventoryDecisionDetails.Food(PlayerEntity, item) + "\n\n" + InventoryDecisionDetails.Handling(PlayerEntity, item);
            if (item.HasPart<EquippablePart>() && EquipmentComparisonService.TryDescribe(PlayerEntity, item, out var comparison, out _))
                return comparison + "\n\n" + InventoryDecisionDetails.Handling(PlayerEntity, item) + "\n" + InventoryDecisionDetails.Disassembly(PlayerEntity, item);
            if (MaterialUseDescription.TryDescribe(PlayerEntity, item, EntityFactory, out var material))
            {
                // F1 must retain live flask contents before the longer field-use guidance.
                string contents = item.HasPart<LiquidVesselPart>() && ItemExamineService.TryDescribeDetails(item, out var mechanics)
                    ? mechanics + "\n\n" : "";
                return contents + material + "\n\n" + InventoryDecisionDetails.Handling(PlayerEntity, item);
            }
            return InventoryDecisionDetails.Item(PlayerEntity, item);
        }
        private void BeginInventorySearch()
        {
            _panel = PANEL_INVENTORY; _inventorySearchActive = true;
            _searchReturnItem = _cursorIndex >= 0 && _cursorIndex < _rows.Count ? _rows[_cursorIndex].Item?.Item : null;
            Render();
        }
        private void SetInventoryQuery(string query)
        {
            var selected = _cursorIndex >= 0 && _cursorIndex < _rows.Count ? _rows[_cursorIndex].Item?.Item : null;
            if (string.IsNullOrEmpty(query) && !string.IsNullOrEmpty(_inventoryQuery) && _searchReturnItem != null) selected = _searchReturnItem;
            int oldOrdinal = DenseOrdinal;
            _inventoryQuery = query ?? "";
            if (_state == null) Rebuild(); else RebuildInventoryRows(selected, oldOrdinal);
            int row = _rows.FindIndex(r => r.Item?.Item == selected && selected != null);
            _cursorIndex = row >= 0 ? row : _rows.FindIndex(r => r.Item != null);
            if (_cursorIndex < 0) _cursorIndex = 0;
            _scrollOffset = 0; ClampCursor(); Render();
        }
        private void HandleInventorySearchInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                _inventorySearchActive = false; SetInventoryQuery("");
                int row = _rows.FindIndex(r => _searchReturnItem != null && r.Item?.Item == _searchReturnItem);
                if (row >= 0) { _cursorIndex = row; ClampCursor(); } Render(); return;
            }
            if (InputHelper.GetKeyDown(KeyCode.Return)) { _inventorySearchActive = false; Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.Backspace)) { if (_inventoryQuery.Length > 0) SetInventoryQuery(_inventoryQuery.Substring(0, _inventoryQuery.Length - 1)); return; }
            if (_inventoryQuery.Length >= 48) return;
            for (int i = 0; i < 26; i++) if (InputHelper.GetKeyDown(KeyCode.A + i)) { SetInventoryQuery(_inventoryQuery + (char)('a' + i)); return; }
            for (int i = 0; i < 10; i++) if (InputHelper.GetKeyDown(KeyCode.Alpha0 + i)) { SetInventoryQuery(_inventoryQuery + (char)('0' + i)); return; }
            if (InputHelper.GetKeyDown(KeyCode.Space)) SetInventoryQuery(_inventoryQuery + " ");
            else if (InputHelper.GetKeyDown(KeyCode.Minus)) SetInventoryQuery(_inventoryQuery + "-");
            else if (InputHelper.GetKeyDown(KeyCode.Quote)) SetInventoryQuery(_inventoryQuery + "'");
        }
    }
}
