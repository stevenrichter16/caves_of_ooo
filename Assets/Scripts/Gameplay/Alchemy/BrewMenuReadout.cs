using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What a still's interact (C) menu says about the mix, as much as the pack's Crafting panel does: what is picked, what it would make (name, each effect with its potency, the
    /// form), why a mix makes nothing, and how many a full batch would brew. Read from the same marked reagents the brew rows use and from <see cref="BrewingService.PreviewBrew"/>,
    /// the one preview the pack panel draws, so the two can never disagree about what a mix makes. Nothing here consumes or changes anything.
    /// </summary>
    public static class BrewMenuReadout
    {
        /// <summary>The status lines for the actor's current mix.</summary>
        public static List<string> Lines(Entity actor)
        {
            var picks = CraftingMarkPart.CollectMarked(actor).Reagents;
            if (picks.Count == 0) return Format(picks, default, 0);
            var preview = BrewingService.PreviewBrew(picks);
            return Format(picks, preview, preview.IsValid ? BrewingService.GetMaxBatchCount(picks) : 0);
        }

        /// <summary>The lines for a mix, a preview of it and its full-batch size. Pure, so the wording is testable without a world.</summary>
        public static List<string> Format(IReadOnlyList<Entity> picks, BrewPreview preview, int batchMax)
        {
            var lines = new List<string>();
            if (picks == null || picks.Count == 0)
            {
                lines.Add("In the mix: nothing picked yet. Pick reagents below.");
                return lines;
            }
            var names = new List<string>(picks.Count);
            foreach (var pick in picks) if (pick != null) names.Add(pick.GetDisplayName());
            lines.Add("In the mix: " + string.Join(", ", names));
            if (!preview.IsValid)
            {
                lines.Add("Makes nothing: " + (string.IsNullOrWhiteSpace(preview.Reason) ? "These reagents make nothing." : preview.Reason));
                return lines;
            }
            lines.Add("Makes: " + preview.DisplayName);
            var effects = new List<string>();
            if (preview.Effects != null) foreach (var e in preview.Effects) if (e != null) effects.Add(e.Property + " " + e.Potency);
            string form = string.IsNullOrEmpty(preview.Form) ? "" : preview.Form.ToLowerInvariant();
            if (effects.Count > 0 || form != "") lines.Add("Effects: " + (effects.Count > 0 ? string.Join(", ", effects) : "none") + (form != "" ? " (" + form + ")" : ""));
            if (batchMax > 1) lines.Add("A full batch brews " + batchMax + ".");
            return lines;
        }
    }
}
