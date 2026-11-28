using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        LedgerModalView _throwLedger, _attackLedger;
        readonly List<Rect> _throwLedgerRows = new List<Rect>();
        static readonly Rect CancelAttackRow = new Rect(24, 133, 282, 43);
        static readonly Rect ConfirmAttackRow = new Rect(318, 133, 282, 43);

        bool UseAuxiliaryLedger => WorldActionMenuUI?.UseLedgerView == true;

        void RenderLedgerThrowPopup()
        {
            if (_throwPopup == null) return;
            ClampThrowPopupScroll();
            const int width = 760, step = 41;
            int count = Mathf.Min(10, _throwPopup.Options.Count);
            _throwLedger ??= LedgerModalView.Create(transform, "ThrowLedger");
            _throwLedger.Begin(WorldActionMenuUI.PopupCamera, width, 128 + Mathf.Max(1, count) * step);
            _throwLedger.Card(_throwPopup.Title ?? "Throw", NativeGamepadInput.IsConnected ? "A Select    B Back" : "Enter Select    Esc Back");
            _throwLedgerRows.Clear();
            if (count == 0) _throwLedger.Label(new Rect(28, 74, width - 56, 34), "No actions available", LMuted);
            for (int i = 0; i < count && _throwPopup.ScrollOffset + i < _throwPopup.Options.Count; i++)
            {
                int index = _throwPopup.ScrollOffset + i;
                var option = _throwPopup.Options[index];
                var row = new Rect(24, 74 + i * step, width - 48, step - 5);
                _throwLedgerRows.Add(row);
                _throwLedger.Row(row, option.Label, index == _throwPopup.CursorIndex,
                    icon: option.Item != null ? InventoryItemIcons.For(option.Item) : null);
            }
            _throwLedger.End();
        }

        void RenderLedgerAttackConfirmation(Entity target)
        {
            _attackLedger ??= LedgerModalView.Create(transform, "AttackConfirmationLedger");
            _attackLedger.Begin(WorldActionMenuUI.PopupCamera, 624, 231);
            _attackLedger.Card("Confirm attack", NativeGamepadInput.IsConnected ? "A Attack    B Cancel" : "Y Attack    N or Esc Cancel");
            string name = target != null ? target.GetDisplayName() : "this creature";
            _attackLedger.Label(new Rect(24, 74, 576, 47), $"Attack {name}?", LInk, 22, fit: true);
            _attackLedger.Row(CancelAttackRow, "Cancel", false, NativeGamepadInput.IsConnected ? "B" : "N");
            _attackLedger.Row(ConfirmAttackRow, "Attack", false, NativeGamepadInput.IsConnected ? "A" : "Y");
            _attackLedger.End();
        }

        bool HandleLedgerAttackClick()
        {
            if (!UseAuxiliaryLedger || _attackLedger == null || !LedgerModalView.ReadPointer(out var pointer)
                || !_attackLedger.TryPoint(pointer, out var point)) return false;
            bool attack = ConfirmAttackRow.Contains(point);
            if (!attack && !CancelAttackRow.Contains(point)) return false;
            ResolveAttackConfirmation(attack);
            return true;
        }
    }
}
