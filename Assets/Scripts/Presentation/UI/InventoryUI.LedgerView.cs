using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        enum LedgerHitKind
        {
            Tab, Equipment, Item, TinkerRow, AbilitySlot, CraftRow, TinkerMode, CraftMode,
            ClearPicks, CraftCommit, ClearAbility, PopupRow, DisplaceYes, DisplaceNo,
            Cancel, Details, Close
        }
        struct LedgerHit { public Rect Bounds; public LedgerHitKind Kind; public int Index; }
        readonly List<LedgerHit> _ledgerHits = new List<LedgerHit>(48);
        readonly List<Image> _ledgerImages = new List<Image>(96);
        readonly List<SkillScreenText> _ledgerLabels = new List<SkillScreenText>(96);
        Canvas _ledgerCanvas;
        RectTransform _ledgerContent;
        GameObject _ledgerRoot;
        RawImage _ledgerPortrait;
        int _ledgerImageUsed, _ledgerLabelUsed;
        bool _ledgerHidden;
        InventoryScreenData.ScreenState _ledgerSelectionState;
        Entity _ledgerSelectionOwner, _ledgerSelectionViewer;
        Zone _ledgerSelectionZone;
        ExaminablePart _ledgerSelectionDescription;
        AnnouncementDocument _ledgerSelectionDocument;
        string _ledgerSelectionText;
        const int LedgerListRows = 12;
        static readonly Color LBackground = new Color(.018f, .044f, .040f);
        static readonly Color LInk = new Color(.86f, .88f, .76f);
        static readonly Color LMuted = new Color(.57f, .69f, .60f);
        static readonly Color LGold = new Color(.97f, .77f, .33f);
        static readonly Color LBorder = new Color(.21f, .38f, .31f);
        static readonly Color LPanel = new Color(.028f, .073f, .065f);
        static readonly Color LSelected = new Color(.085f, .14f, .105f);
        static readonly Color LRed = new Color(.96f, .55f, .44f);
        static readonly Color LGreen = new Color(.48f, .86f, .57f);

        void EnsureLedgerView()
        {
            if (_ledgerRoot == null)
            {
                _ledgerRoot = new GameObject("FullScreenInventory", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                _ledgerRoot.layer = 0;
                _ledgerRoot.transform.SetParent(transform, false);
                _ledgerCanvas = _ledgerRoot.GetComponent<Canvas>();
                _ledgerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                _ledgerCanvas.sortingOrder = 100;
                var scaler = _ledgerRoot.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 800);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                var backdrop = LedgerObject("OpaqueBackground", _ledgerRoot.transform, typeof(Image)).GetComponent<Image>();
                backdrop.color = LBackground;
                backdrop.raycastTarget = false;
                backdrop.rectTransform.anchorMin = Vector2.zero;
                backdrop.rectTransform.anchorMax = Vector2.one;
                backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
                _ledgerContent = LedgerObject("InventoryLedger", _ledgerRoot.transform).GetComponent<RectTransform>();
                _ledgerContent.anchorMin = _ledgerContent.anchorMax = new Vector2(.5f, .5f);
                _ledgerContent.pivot = new Vector2(.5f, .5f);
                _ledgerContent.sizeDelta = new Vector2(1280, 800);
                _ledgerPortrait = LedgerObject("SelectedItemWorldArt", _ledgerContent, typeof(RawImage)).GetComponent<RawImage>();
                _ledgerPortrait.raycastTarget = false;
                _ledgerPortrait.gameObject.SetActive(false);
            }
            // Camera.main is resolved on entry/paint, never by a per-frame view loop.
            // A view-only fixture may have no camera; normal InputHandler binds one.
            _ledgerCanvas.worldCamera = Camera.main;
            if (_ledgerCanvas.worldCamera != null)
                _ledgerCanvas.planeDistance = Mathf.Clamp(1f, _ledgerCanvas.worldCamera.nearClipPlane + .01f,
                    _ledgerCanvas.worldCamera.farClipPlane - .01f);
        }

        static GameObject LedgerObject(string name, Transform parent, params Type[] components)
        {
            var types = new Type[components.Length + 1]; types[0] = typeof(RectTransform);
            Array.Copy(components, 0, types, 1, components.Length);
            var child = new GameObject(name, types); child.layer = 0;
            child.transform.SetParent(parent, false); return child;
        }

        void SetLedgerVisible(bool visible)
        {
            _ledgerHidden = !visible;
            if (visible) EnsureLedgerView();
            if (_ledgerRoot != null) _ledgerRoot.SetActive(visible);
            if (!visible) { _ledgerHits.Clear(); ClearLedgerSelection(); }
        }

        bool LedgerPoint(Vector2 screen, out Vector2 point)
        {
            point = default;
            if (_ledgerContent == null || _ledgerHidden || _ledgerRoot == null || !_ledgerRoot.activeInHierarchy) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_ledgerContent, screen, _ledgerCanvas.worldCamera, out var local)) return false;
            Rect rect = _ledgerContent.rect;
            point = new Vector2(local.x - rect.xMin, rect.yMax - local.y);
            return point.x >= 0 && point.y >= 0 && point.x <= 1280 && point.y <= 800;
        }

        static void LedgerPlace(RectTransform transform, Rect bounds)
        {
            transform.anchorMin = transform.anchorMax = new Vector2(0, 1);
            transform.pivot = new Vector2(0, 1);
            transform.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            transform.sizeDelta = bounds.size;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        Image LBox(Rect bounds, Color color)
        {
            if (_ledgerImageUsed == _ledgerImages.Count)
            {
                var image = LedgerObject("Panel", _ledgerContent, typeof(Image)).GetComponent<Image>();
                image.raycastTarget = false; _ledgerImages.Add(image);
            }
            var result = _ledgerImages[_ledgerImageUsed++]; result.gameObject.SetActive(true);
            result.transform.SetAsLastSibling(); LedgerPlace(result.rectTransform, bounds); result.color = color;
            return result;
        }
        void LFrame(Rect bounds, Color border, Color fill)
        {
            LBox(bounds, border);
            LBox(new Rect(bounds.x + 1, bounds.y + 1, bounds.width - 2, bounds.height - 2), fill);
        }
        void LLabel(Rect bounds, string text, Color color, float height = 20)
        {
            if (_ledgerLabelUsed == _ledgerLabels.Count)
            {
                var label = LedgerObject("GlyphText", _ledgerContent, typeof(SkillScreenText)).GetComponent<SkillScreenText>();
                label.raycastTarget = false; _ledgerLabels.Add(label);
            }
            var result = _ledgerLabels[_ledgerLabelUsed++]; result.gameObject.SetActive(true);
            result.transform.SetAsLastSibling(); LedgerPlace(result.rectTransform, bounds);
            if (result.GlyphHeight != height) { result.GlyphHeight = height; result.SetVerticesDirty(); }
            result.Content = text; result.color = color;
        }
        void LHit(Rect bounds, LedgerHitKind kind, int index = 0)
            => _ledgerHits.Add(new LedgerHit { Bounds = bounds, Kind = kind, Index = index });
        void EndLedgerPools()
        {
            for (int i = _ledgerImageUsed; i < _ledgerImages.Count; i++) _ledgerImages[i].gameObject.SetActive(false);
            for (int i = _ledgerLabelUsed; i < _ledgerLabels.Count; i++) _ledgerLabels[i].gameObject.SetActive(false);
        }

        void RenderLedger()
        {
            if (!_isOpen || _ledgerHidden) return;
            EnsureLedgerView();
            _ledgerRoot.SetActive(true);
            _ledgerImageUsed = _ledgerLabelUsed = 0; _ledgerHits.Clear();
            _ledgerPortrait.gameObject.SetActive(false);
            LLabel(new Rect(24, 20, 560, 34), "PACK & EQUIPMENT", LInk, 28);
            if (_state != null)
                LLabel(new Rect(630, 28, 626, 24), "Weight " + _state.CarriedWeight + "/" + _state.MaxCarryWeight
                    + " lb   Drams " + _state.Drams + "   Ink " + _state.Ink, LMuted, 18);
            LBox(new Rect(24, 65, 1232, 1), LBorder);
            string[] tabs = { "Equipment", "Inventory", "Tinkering", "Abilities", "Crafting" };
            for (int i = 0; i < tabs.Length; i++)
            {
                var rect = new Rect(24 + i * 248, 76, 240, 40);
                bool selected = _panel == i;
                LFrame(rect, selected ? LGold : LBorder, selected ? LSelected : LPanel);
                LLabel(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, 20), tabs[i], selected ? LGold : LMuted, 20);
                LHit(rect, LedgerHitKind.Tab, i);
            }
            if (_panel == PANEL_TINKERING) RenderLedgerTinkering();
            else if (_panel == PANEL_ABILITIES) RenderLedgerAbilities();
            else if (_panel == PANEL_CRAFTING) RenderLedgerCrafting();
            else
            {
                RenderLedgerEquipment();
                RenderLedgerPack();
                RenderLedgerSelection();
            }
            RenderLedgerFooter();
            RenderLedgerPopup();
            // A refused action leaves its modal open for retry. Keep the reason
            // above that modal's dimmer, in its own non-overlapping footer row.
            if (!string.IsNullOrWhiteSpace(_actionStatus))
                LLabel(new Rect(24, 712, 1232, 24), SkillScreenText.Fit(_actionStatus, 123, 1), _actionStatusSuccess ? LGreen : LRed, 20);
            EndLedgerPools();
        }

        void RenderLedgerEquipment()
        {
            LFrame(new Rect(24, 132, 312, 572), _panel == PANEL_EQUIPMENT ? LGold : LBorder, LPanel);
            LLabel(new Rect(38, 145, 280, 22), "STATUS", LGold, 18);
            var stats = _state?.PlayerStats?.OrderBy(stat => stat.Name == "Hitpoints" ? 0
                : stat.Name == "AV" ? 1 : stat.Name == "DV" ? 2 : 3).ToList();
            int shown = stats == null ? 0 : Mathf.Min(stats.Count, 14);
            bool overflow = stats != null && stats.Count > 14;
            if (overflow) shown = 13;
            for (int i = 0; i < shown; i++)
            {
                var stat = stats[i]; float x = 38 + (i / 7) * 146, y = 174 + (i % 7) * 20;
                LLabel(new Rect(x, y, 42, 18), SkillScreenText.Fit((stat.Label ?? stat.Name ?? "?").ToUpperInvariant(), 4, 1), LMuted, 18);
                LLabel(new Rect(x + 46, y, 94, 18), SkillScreenText.Fit(stat.Value, 10, 1), LInk, 18);
            }
            if (overflow) LLabel(new Rect(184, 294, 138, 18), "+" + (stats.Count - shown) + " more stats", LMuted, 14);
            LBox(new Rect(38, 316, 284, 1), LBorder);
            LLabel(new Rect(38, 328, 280, 22), "EQUIPMENT", _panel == PANEL_EQUIPMENT ? LGold : LMuted, 18);
            int count = _equipSlots?.Count ?? 0;
            const int capacity = 7;
            int start = Mathf.Clamp(_equipCursorIndex - capacity + 1, 0, Mathf.Max(0, count - capacity));
            for (int line = 0; line < capacity && start + line < count; line++)
            {
                int index = start + line; var slot = _equipSlots[index];
                var rect = new Rect(32, 358 + line * 44, 296, 43);
                bool selected = _panel == PANEL_EQUIPMENT && _equipCursorIndex == index;
                if (selected) LBox(rect, LSelected);
                LLabel(new Rect(38, rect.y + 3, 284, 20), SkillScreenText.Fit(slot.BodyPartName ?? slot.SlotName, 31, 1), selected ? LGold : LMuted, 18);
                LLabel(new Rect(52, rect.y + 23, 270, 20), SkillScreenText.Fit(slot.ItemName ?? "(empty)", 30, 1),
                    slot.EquippedItem != null ? LInk : LMuted, 18);
                LHit(rect, LedgerHitKind.Equipment, index);
            }
            if (count > capacity)
                LLabel(new Rect(38, 679, 284, 18), (start + 1) + "-" + Mathf.Min(start + capacity, count) + " / " + count + " body slots", LMuted, 14);
        }

        void RenderLedgerPack()
        {
            LFrame(new Rect(352, 132, 904, 330), _panel == PANEL_INVENTORY ? LGold : LBorder, LPanel);
            string heading = string.IsNullOrEmpty(_inventoryQuery) ? "ITEM" : "FIND: " + _inventoryQuery;
            if (_inventorySearchActive) heading += "_";
            LLabel(new Rect(368, 145, 404, 20), SkillScreenText.Fit(heading, 40, 1), _inventorySearchActive ? LGold : LMuted, 18);
            LLabel(new Rect(788, 145, 96, 20), "QTY", LMuted, 18);
            LLabel(new Rect(896, 145, 120, 20), "WT", LMuted, 18);
            LLabel(new Rect(1026, 145, 176, 20), "DMG / DEF", LMuted, 18);
            LBox(new Rect(368, 170, 872, 1), LBorder);
            if (_cursorIndex < _scrollOffset) _scrollOffset = _cursorIndex;
            else if (_cursorIndex >= _scrollOffset + LedgerListRows) _scrollOffset = _cursorIndex - LedgerListRows + 1;
            _scrollOffset = Mathf.Clamp(_scrollOffset, 0, Mathf.Max(0, _rows.Count - LedgerListRows));
            if (_rows.Count == 0)
                LLabel(new Rect(370, 193, 850, 54), string.IsNullOrEmpty(_inventoryQuery) ? "Your pack is empty." : "No items match this search. Escape clears the filter.", LMuted, 18);
            for (int line = 0; line < LedgerListRows && _scrollOffset + line < _rows.Count; line++)
            {
                int index = _scrollOffset + line; var row = _rows[index]; float y = 177 + line * 22;
                if (row.IsHeader)
                {
                    LLabel(new Rect(370, y + 2, 404, 20), SkillScreenText.Fit(row.Text, 40, 1).ToUpperInvariant(), LMuted, 18);
                    continue;
                }
                if (row.Item == null) continue;
                bool selected = _panel == PANEL_INVENTORY && _cursorIndex == index;
                var rect = new Rect(360, y - 1, 888, 22);
                if (selected) { LBox(rect, LSelected); LBox(new Rect(rect.x, rect.y, 3, rect.height), LGold); }
                LLabel(new Rect(370, y, 404, 21), SkillScreenText.Fit(row.Item.Name, 40, 1), selected ? LGold : LInk, 20);
                LLabel(new Rect(788, y, 96, 21), row.Item.StackCount.ToString(), LInk, 18);
                LLabel(new Rect(896, y, 120, 21), row.Item.Weight + " lb", LInk, 18);
                LLabel(new Rect(1026, y, 176, 21), SkillScreenText.Fit(LedgerCombatValue(row.Item.Item), 19, 1), LInk, 18);
                if (row.Item.IsEquipped) LLabel(new Rect(1220, y, 28, 20), "[E]", LGold, 18);
                LHit(rect, LedgerHitKind.Item, index);
            }
            if (_rows.Count > LedgerListRows)
                LLabel(new Rect(370, 444, 850, 16), (_scrollOffset + 1) + "-" + Mathf.Min(_scrollOffset + LedgerListRows, _rows.Count)
                    + " / " + _rows.Count + " rows", LMuted, 14);
        }

        // Item contributions, not actor totals or predicted damage against a target.
        static string LedgerCombatValue(Entity item)
        {
            var weapon = item.GetPart<MeleeWeaponPart>();
            if (weapon != null) return weapon.BaseDamage ?? "1d2";
            var armor = item.GetPart<ArmorPart>();
            if (armor != null)
                return armor.AV + " AV " + (armor.DV > 0 ? "+" : "") + armor.DV + " DV";
            return "-";
        }

        void ClearLedgerSelection()
        {
            _ledgerSelectionState = null; _ledgerSelectionOwner = _ledgerSelectionViewer = null;
            _ledgerSelectionZone = null; _ledgerSelectionDescription = null;
            _ledgerSelectionDocument = null; _ledgerSelectionText = null;
        }

        AnnouncementDocument LedgerSelectionPresentation(out string text)
        {
            Entity selected = null;
            if (!_inventorySearchActive && _panel == PANEL_INVENTORY && _cursorIndex >= 0 && _cursorIndex < _rows.Count)
                selected = _rows[_cursorIndex].Item?.Item;
            else if (!_inventorySearchActive && _panel == PANEL_EQUIPMENT && _equipSlots != null
                && _equipCursorIndex >= 0 && _equipCursorIndex < _equipSlots.Count)
                selected = _equipSlots[_equipCursorIndex].EquippedItem;
            text = null;
            if (selected == null) { ClearLedgerSelection(); return null; }

            // The retained backdrop is an inspection snapshot. A modal, rejection,
            // queued reader or layout repaint must not invoke mutable description
            // providers again. Only a changed selection or explicit Rebuild refreshes it.
            bool changed = _ledgerSelectionState != _state || _ledgerSelectionOwner != selected
                || _ledgerSelectionViewer != PlayerEntity || _ledgerSelectionZone != CurrentZone;
            if (changed && !LedgerHasPopup && !MessageLog.HasPendingAnnouncement)
            {
                _ledgerSelectionState = _state; _ledgerSelectionOwner = selected;
                _ledgerSelectionViewer = PlayerEntity; _ledgerSelectionZone = CurrentZone;
                _ledgerSelectionDescription = selected.GetPart<ExaminablePart>();
                _ledgerSelectionDocument = SelectedDecisionPresentation(out _ledgerSelectionText);
            }
            var document = _ledgerSelectionDocument;
            if (document == null || document.Subject != selected || _ledgerSelectionState != _state
                || _ledgerSelectionViewer != PlayerEntity || _ledgerSelectionZone != CurrentZone
                || !ReferenceEquals(_ledgerSelectionDescription, selected.GetPart<ExaminablePart>())
                || (_ledgerSelectionDescription != null && _ledgerSelectionDescription.ParentEntity != selected)
                || !ExamineDocumentBuilder.IsCurrentSubject(document))
            {
                // Remember the attempted key, but discard invalid prose. Repainting
                // a refusal is not permission to retry a replacement provider.
                _ledgerSelectionDocument = null; _ledgerSelectionText = null;
                return null;
            }
            text = _ledgerSelectionText;
            return document;
        }

        void RenderLedgerSelection()
        {
            LFrame(new Rect(352, 478, 904, 226), LBorder, LPanel);
            var document = LedgerSelectionPresentation(out string decision);
            Entity subject = document?.Subject;
            if (subject == null || string.IsNullOrWhiteSpace(decision))
            {
                string text = _inventorySearchActive ? "Search the pack, then select an item to inspect it."
                    : _panel == PANEL_EQUIPMENT ? "Select equipment to inspect it. Enter chooses an item for the selected body slot."
                    : "Select a carried item to see its uses and current condition.";
                LLabel(new Rect(372, 506, 860, 96), SkillScreenText.Fit(text, 86, 4), LMuted, 20);
                return;
            }
            LLabel(new Rect(448, 495, 790, 52), SkillScreenText.Fit(subject.GetDisplayName(), 71, 2), LGold, 22);
            var lines = new List<string>();
            foreach (string line in decision.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed == "Equipment comparison" || trimmed.StartsWith("Candidate: ", StringComparison.Ordinal)
                    || trimmed == subject.GetDisplayName()) continue;
                lines.Add(trimmed);
            }
            string preview = SkillScreenText.Fit(string.Join("\n", lines), 87, 5);
            LLabel(new Rect(448, 553, 790, 108), preview, LInk, 18);
            // This is a current, authorized owner in the existing renderer, never
            // a blueprint guess or an offscreen model capture. Texture stays borrowed.
            var renderer = Tilemap != null ? Tilemap.GetComponent<ZoneRenderer>() : null;
            if (renderer != null && renderer.TryGetFlatExaminePortrait(document, out var portrait) && portrait.Texture != null && !portrait.IsModel)
            {
                _ledgerPortrait.texture = portrait.Texture; _ledgerPortrait.uvRect = portrait.Uv; _ledgerPortrait.color = portrait.Tint;
                LedgerPlace(_ledgerPortrait.rectTransform, new Rect(368, 504, 64, 96));
                _ledgerPortrait.gameObject.SetActive(true); _ledgerPortrait.transform.SetAsLastSibling();
            }
            else if (document.Glyph.HasValue)
                LLabel(new Rect(384, 522, 36, 48), document.Glyph.Value.ToString(), QudColorParser.Parse(document.GlyphColor), 44);
            var details = new Rect(1066, 670, 174, 26);
            LFrame(details, LBorder, LBackground);
            LLabel(new Rect(details.x + 8, details.y + 4, details.width - 16, 18), NativeGamepadInput.IsConnected ? "LT+RT Details" : "F1 Details", LMuted, 16);
            LHit(details, LedgerHitKind.Details);
        }

        void RenderLedgerFooter()
        {
            LBox(new Rect(24, 740, 1232, 1), LBorder);
            bool pad = NativeGamepadInput.IsConnected;
            string hint;
            if (_inventorySearchActive) hint = "Type to filter   Enter: accept   Esc: clear search";
            else if (pad) hint = "D-pad: browse  A: select  X: commands  B: back  Y: mode  LB/RB: mode  LT+RT: details";
            else if (_panel == PANEL_CRAFTING) hint = "Space: pick   Enter: craft   F/B: mode   C: clear   Tab: next   F1: details   Esc: close";
            else if (_panel == PANEL_TINKERING) hint = "Enter: select   B/M: build/mod   Tab: next   F1: details   Esc: close";
            else if (_panel == PANEL_ABILITIES) hint = "Arrows: slot   Enter: assign   X: clear   Tab: next   Esc: close";
            else if (_panel == PANEL_EQUIPMENT) hint = "Arrows: body slot   Enter: equip   E: unequip   Tab: next   F1: details   Esc: close";
            else hint = "Arrows: browse  Enter: actions  D: drop  /: find  Tab: mode  F1: details  F2: status  Esc: close";
            LLabel(new Rect(24, 755, 1232, 24), hint, LMuted, 18);
        }
    }
}
