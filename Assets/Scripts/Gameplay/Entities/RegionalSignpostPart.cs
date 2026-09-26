using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Read-only, current-location directions for an authored signpost.
    /// Computed only when examined; no destination cache or saved map authority.
    /// ExaminablePart appends this text after the sign's ordinary flavor.</summary>
    public sealed class RegionalSignpostPart : Part
    {
        public override string Name => "RegionalSignpost";

        /// <summary>Up to four existing named surface villages, with map-cell
        /// bearings from this sign. Returns empty when no valid live surface
        /// context or destination exists. Does not advertise work, record notes,
        /// reveal map cells, or generate destination zones.</summary>
        public string GetDirectionsText()
        {
            var leads = RegionalGuidance.BuildSignpost(ParentEntity);
            if (leads.Count == 0) return string.Empty;
            var text = new StringBuilder("Carved directions:");
            foreach (var lead in leads) text.Append("\n  - ").Append(lead.Text);
            return text.ToString();
        }
    }
}
