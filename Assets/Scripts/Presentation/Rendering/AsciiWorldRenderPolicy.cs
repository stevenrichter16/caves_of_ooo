using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Canonical rules for world-space ASCII rendering.
    /// World rendering is glyph-first: RenderString, ColorString, and RenderLayer
    /// are authoritative. Tile/TileColor/DetailColor are ignored by the world renderer.
    /// </summary>
    public static class AsciiWorldRenderPolicy
    {
        public const char FallbackGlyph = '?';
        public const char EmptyGlyph = ' ';
        public const string FallbackColorString = "&y";

        public static char GetGlyphOrFallback(RenderPart render, out string issue)
        {
            issue = null;

            if (render == null)
            {
                issue = "missing Render part";
                return FallbackGlyph;
            }

            if (!IsValidGlyphString(render.RenderString, out string glyphIssue))
            {
                issue = glyphIssue;
                return FallbackGlyph;
            }

            return render.RenderString[0];
        }

        public static string GetColorOrFallback(RenderPart render, out string issue)
        {
            issue = null;

            if (render == null)
            {
                issue = "missing Render part";
                return FallbackColorString;
            }

            if (!IsValidColorString(render.ColorString))
            {
                issue = "missing ColorString";
                return FallbackColorString;
            }

            return render.ColorString;
        }

        public static bool IsValidGlyphString(string renderString, out string issue)
        {
            issue = null;

            if (string.IsNullOrEmpty(renderString))
            {
                issue = "missing RenderString";
                return false;
            }

            if (renderString.Length != 1)
            {
                issue = $"RenderString '{renderString}' must be exactly one printable glyph";
                return false;
            }

            if (!IsPrintableGlyph(renderString[0]))
            {
                issue = $"RenderString glyph 0x{(int)renderString[0]:X2} is not printable";
                return false;
            }

            return true;
        }

        public static bool IsValidColorString(string colorString)
        {
            return !string.IsNullOrWhiteSpace(colorString);
        }

        public static bool IsPrintableGlyph(char glyph)
        {
            return !char.IsControl(glyph);
        }

        public static List<string> ValidateBlueprintRenderParameters(
            string blueprintName,
            Dictionary<string, string> renderParameters,
            bool requireDisplayName)
        {
            var issues = new List<string>();
            if (renderParameters == null)
            {
                issues.Add($"{blueprintName}: missing Render parameters");
                return issues;
            }

            renderParameters.TryGetValue("DisplayName", out string displayName);
            renderParameters.TryGetValue("RenderString", out string renderString);
            renderParameters.TryGetValue("ColorString", out string colorString);
            renderParameters.TryGetValue("RenderLayer", out string renderLayer);

            if (requireDisplayName && string.IsNullOrWhiteSpace(displayName))
                issues.Add($"{blueprintName}: missing Render.DisplayName");

            if (!IsValidGlyphString(renderString, out string glyphIssue))
                issues.Add($"{blueprintName}: {glyphIssue}");

            if (!IsValidColorString(colorString))
                issues.Add($"{blueprintName}: missing Render.ColorString");

            if (!string.IsNullOrWhiteSpace(renderLayer) && !int.TryParse(renderLayer, out _))
                issues.Add($"{blueprintName}: RenderLayer '{renderLayer}' is not a valid integer");

            return issues;
        }
    }
}
