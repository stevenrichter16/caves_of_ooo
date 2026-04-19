namespace CavesOfOoo.Core
{
    /// <summary>
    /// Universal "Examine" action provider. Any entity carrying this part
    /// contributes an "Examine" action to the world-action menu and, when
    /// selected, logs a description of the entity via <see cref="MessageLog"/>.
    ///
    /// Mirrors Qud's <c>Description</c> part, which declares the "Look"
    /// action on every describable object. Attach via the <c>PhysicalObject</c>
    /// base blueprint so every item/creature/furniture entity inherits it —
    /// no per-blueprint wiring needed for the common case.
    ///
    /// Optional <see cref="Text"/> field lets individual blueprints supply
    /// flavor prose (e.g., "A sturdy wooden chest with iron bands.") while
    /// entities without a Text override fall back to a plain
    /// "You see a {display name}." line.
    ///
    /// Intended consumers:
    /// - <c>GetInventoryActions</c> event — adds the Examine menu entry
    ///   (priority 0 so richer actions like Chat, Open sort above it)
    /// - <c>InventoryAction</c> event with <c>Command == "Examine"</c> —
    ///   logs the examine text.
    ///
    /// Non-goals for v1:
    /// - No description popup. MessageLog only until the world action menu
    ///   UI lands and can host a richer description panel.
    /// - No Understood/Identified gating. Every entity is considered known.
    /// - No "Recall Story" sub-action (Qud-only flavor today).
    /// </summary>
    public class ExaminablePart : Part
    {
        public override string Name => "Examinable";

        /// <summary>
        /// Optional flavor text set by the blueprint. If non-empty, appended
        /// to the examine message. If empty, the examine line is just
        /// "You see a {display name}." with no extra detail.
        /// </summary>
        public string Text = "";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                // Priority 0: lowest-priority universal action. Entity-specific
                // actions (Chat at 10, Open at 30) rank above it so the menu's
                // default-selection still favors the meaningful interaction.
                // Hotkey 'x' to avoid colliding with 'o' (Open) / 'c' (Chat).
                actions?.AddAction("Examine", "examine", "Examine", 'x', 0);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == "Examine")
                {
                    MessageLog.Add(BuildExamineLine());
                    e.Handled = true;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compose the examine message: "You see a {name}. {flavor}" when Text
        /// is set, or plain "You see a {name}." otherwise.
        ///
        /// Indefinite-article handling is minimal on purpose — roguelikes
        /// often use natural-language polish that's out of scope here. If the
        /// display name already starts with "a ", "an ", "the ", or is a
        /// proper noun (starts uppercase), we leave it alone. Otherwise we
        /// prepend "a ".
        /// </summary>
        private string BuildExamineLine()
        {
            string name = ParentEntity?.GetDisplayName() ?? "something";
            string article = GetArticle(name);
            string baseLine = $"You see {article}{name}.";
            if (string.IsNullOrWhiteSpace(Text))
                return baseLine;
            return baseLine + " " + Text.Trim();
        }

        private static string GetArticle(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            // Proper nouns — skip article (e.g., "Asphodel", "Glimmer").
            // Heuristic: uppercase first letter.
            if (char.IsUpper(name[0])) return "";

            // Already has a determiner.
            string lower = name.ToLowerInvariant();
            if (lower.StartsWith("a ") || lower.StartsWith("an ") ||
                lower.StartsWith("the ") || lower.StartsWith("some ") ||
                lower.StartsWith("your ") || lower.StartsWith("his ") ||
                lower.StartsWith("her ") || lower.StartsWith("their "))
                return "";

            // Vowel-start → "an "; else "a ".
            char first = char.ToLowerInvariant(name[0]);
            bool vowel = first == 'a' || first == 'e' || first == 'i' ||
                         first == 'o' || first == 'u';
            return vowel ? "an " : "a ";
        }
    }
}
