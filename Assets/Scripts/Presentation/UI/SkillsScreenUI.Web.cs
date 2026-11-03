using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class SkillsScreenUI
    {
        private int phoneModalLabelStart;
        internal string PhoneModalKey => "skills:" + (IsReviewingPurchase ? "review:" + SelectedClass + ":" + Model.PendingRank
            : assigning ? "assign:" + SelectedClass : "base");
        internal bool AppendPhoneRows(List<PhoneMenuMirror.Row> output)
        {
            if (!IsOpen || hidden) return false;
            for (int i = phoneModalLabelStart; i < labelUsed; i++)
            {
                var label = labels[i];
                if (!string.IsNullOrWhiteSpace(label.Content))
                    output.Add(new PhoneMenuMirror.Row { text = label.Content, color = "#" + ColorUtility.ToHtmlStringRGB(label.color) });
            }
            return true;
        }
    }
}
