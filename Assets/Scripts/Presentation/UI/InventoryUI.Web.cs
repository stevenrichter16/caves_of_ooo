using UnityEngine;
using System.Collections.Generic;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        internal string PhoneModalKey => "inventory:" + (_loadoutNameEditing ? "name"
            : _ledgerCommandsOpen ? "commands" : _displaceConfirm != null ? "replace"
            : _equipPopup != null ? "equipment" : _itemActionPopup != null ? (_itemActionPopup.InBodyPartPicker ? "item:body-part" : "item:actions")
            : _modTargetPopup != null ? "mod" : _grimoirePicker != null ? "grimoire" : "base:" + _panel);
        internal bool AppendPhoneRows(List<PhoneMenuMirror.Row> output)
        {
            if (!IsOpen || _ledgerHidden) return false;
            for (int i = _phoneModalLabelStart; i < _ledgerLabelUsed; i++)
            {
                var label = _ledgerLabels[i];
                if (!string.IsNullOrWhiteSpace(label.Content))
                    output.Add(new PhoneMenuMirror.Row { text = label.Content, color = "#" + ColorUtility.ToHtmlStringRGB(label.color) });
            }
            return true;
        }
        public string PhoneLoadoutName => IsOpen && !_ledgerHidden && _loadoutNameEditing ? _loadoutName : null;
        public void SubmitPhoneLoadoutName(string text)
        {
            if (!PhoneInput.Enabled || !IsOpen || !_loadoutNameEditing) return;
            _loadoutName = (text ?? "").Trim();
            if (_loadoutName.Length > 40) _loadoutName = _loadoutName.Substring(0, 40);
            _loadoutNameUntouched = false;
            SaveNamedLoadout();
        }
    }
}
