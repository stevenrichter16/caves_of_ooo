using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Parses Qud-style color codes (e.g. "&amp;Y", "&amp;c", "&amp;K") into Unity Colors.
    /// Qud uses a DOS/CGA-inspired 16-color palette with &amp; prefix for foreground.
    /// Lowercase = dark variant, uppercase = bright variant.
    /// </summary>
    public static class QudColorParser
    {
        // CGA 16-color palette
        public static readonly Color Black       = new Color(0.00f, 0.00f, 0.00f); // k
        public static readonly Color DarkRed     = new Color(0.67f, 0.00f, 0.00f); // r
        public static readonly Color DarkGreen   = new Color(0.00f, 0.67f, 0.00f); // g
        public static readonly Color DarkYellow  = new Color(0.67f, 0.67f, 0.00f); // w (brown/dark yellow)
        public static readonly Color DarkBlue    = new Color(0.00f, 0.00f, 0.67f); // b
        public static readonly Color DarkMagenta = new Color(0.67f, 0.00f, 0.67f); // m
        public static readonly Color DarkCyan    = new Color(0.00f, 0.67f, 0.67f); // c
        public static readonly Color Gray        = new Color(0.67f, 0.67f, 0.67f); // y (light gray)

        public static readonly Color DarkGray    = new Color(0.33f, 0.33f, 0.33f); // K
        public static readonly Color BrightRed   = new Color(1.00f, 0.33f, 0.33f); // R
        public static readonly Color BrightGreen = new Color(0.33f, 1.00f, 0.33f); // G
        public static readonly Color BrightYellow= new Color(1.00f, 1.00f, 0.33f); // W
        public static readonly Color BrightBlue  = new Color(0.33f, 0.33f, 1.00f); // B
        public static readonly Color BrightMagenta=new Color(1.00f, 0.33f, 1.00f); // M
        public static readonly Color BrightCyan  = new Color(0.33f, 1.00f, 1.00f); // C
        public static readonly Color White       = new Color(1.00f, 1.00f, 1.00f); // Y

        /// <summary>
        /// Parse a Qud color string like "&amp;Y" or "&amp;c" into a Unity Color.
        /// Returns white if unparseable.
        /// </summary>
        public static Color Parse(string colorString)
        {
            if (string.IsNullOrEmpty(colorString) || colorString.Length < 2)
                return Gray;

            // Find the color code after '&'
            char code = ' ';
            for (int i = 0; i < colorString.Length - 1; i++)
            {
                if (colorString[i] == '&')
                {
                    code = colorString[i + 1];
                    break;
                }
            }

            return CharToColor(code);
        }

        /// <summary>
        /// Convert a single color character to a Unity Color.
        /// </summary>
        public static Color CharToColor(char c)
        {
            switch (c)
            {
                case 'k': return Black;
                case 'r': return DarkRed;
                case 'g': return DarkGreen;
                case 'w': return DarkYellow;
                case 'b': return DarkBlue;
                case 'm': return DarkMagenta;
                case 'c': return DarkCyan;
                case 'y': return Gray;

                case 'K': return DarkGray;
                case 'R': return BrightRed;
                case 'G': return BrightGreen;
                case 'W': return BrightYellow;
                case 'B': return BrightBlue;
                case 'M': return BrightMagenta;
                case 'C': return BrightCyan;
                case 'Y': return White;

                default:  return Gray;
            }
        }
    }
}
