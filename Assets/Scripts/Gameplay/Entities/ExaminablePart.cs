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
    /// entities without a Text override begin with a plain
    /// "You see a {display name}." line. Supported parts contribute current
    /// item mechanics, contextual directions, enhancements and afflictions.
    ///
    /// Intended consumers:
    /// - <c>GetInventoryActions</c> event — adds the Examine menu entry
    ///   (priority 0 so richer actions like Chat, Open sort above it)
    /// - <c>InventoryAction</c> event with <c>Command == "Examine"</c> —
    ///   logs the examine text.
    ///
    /// Presentation belongs to the consumer: world actions log the description,
    /// while supported inventory inspections show it in an announcement popup.
    /// Non-goals:
    /// - No Understood/Identified gating. Every entity is considered known.
    /// - No "Recall Story" sub-action (Qud-only flavor today).
    /// </summary>
    public class ExaminablePart : Part
    {
        public override string Name => "Examinable";

        /// <summary>
        /// Optional flavor text set by the blueprint. If non-empty, appended
        /// to the examine message. If empty, the opening line is simply
        /// "You see a {display name}."; other parts may still contribute detail.
        /// </summary>
        public string Text = "";

        /// <summary>
        /// Alias for <see cref="Text"/>. 40 shipped blueprints author their
        /// examine copy under the JSON key <c>"Description"</c> — a name
        /// that reads more naturally in content — but
        /// <c>EntityFactory.ApplyParameters</c> binds blueprint params by
        /// exact field/property name via reflection and silently no-ops on
        /// a miss, so every one of those 40 objects examined as a bare
        /// "You see a {name}." with the authored prose dropped on the
        /// floor. A property, not a rename, so blueprints already using
        /// either spelling keep working (Docs/FELLING-W1-W2-PLAN.md SM0).
        /// </summary>
        public string Description { get => Text; set => Text = value; }

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
        /// Compose the current description without displaying it. Preserves
        /// authored flavor and appends supported mechanics/contextual details.
        /// Does not apply effects, consume items or turns, record travel notes,
        /// or generate destination zones. Callers choose log or popup presentation.
        ///
        /// Indefinite-article handling is minimal on purpose — roguelikes
        /// often use natural-language polish that's out of scope here. If the
        /// display name already starts with "a ", "an ", "the ", or is a
        /// proper noun (starts uppercase), we leave it alone. Otherwise we
        /// prepend "a ".
        /// </summary>
        public string BuildExamineLine() => BuildDescription(false, null);

        /// <summary>World reader snapshot: immediate effects and visible ground precede long flavor.</summary>
        public string BuildWorldExamineLine(Zone zone, Cell cell, Entity viewer = null)
        {
            string description = BuildDescription(true, CellStatusReadout.GroundLine(zone, cell),
                ParentEntity?.GetPart<CampfirePart>()?.DescribeFiniteCooking(zone, cell),
                ParentEntity?.GetPart<CropPart>()?.DescribeGrowth(zone, cell));
            string warning = LocalGatheringClaims.WarningFor(viewer, ParentEntity, zone);
            return string.IsNullOrEmpty(warning) ? description : warning + "\n\n" + description;
        }

        private string BuildDescription(bool statusFirst, string ground, string cooking = null, string growth = null)
        {
            string name = ParentEntity?.GetDisplayName() ?? "something";
            string article = GetArticle(name);
            string baseLine = $"You see {article}{name}.";
            var batch = ParentEntity?.GetPart<KitchenBatchPart>()?.Describe();
            if (!string.IsNullOrEmpty(batch)) baseLine += "\n" + batch;
            var dressing = ParentEntity?.GetPart<SoddenPreparationPart>()?.Describe();
            if (!string.IsNullOrEmpty(dressing)) baseLine += "\n" + dressing;
            var repair = ParentEntity?.GetPart<RepairablePart>()?.Describe();
            if (!string.IsNullOrEmpty(repair)) baseLine += "\n" + repair;
            var trap = ParentEntity?.GetPart<TrapJammingPart>()?.DescribeJamming();
            if (!string.IsNullOrEmpty(trap)) baseLine += "\n" + trap;
            if (ParentEntity?.HasPart<CultivatedSoilPart>() == true)
                baseLine += "\nTilled growing bed. Plant a carried seed here when the bed is empty. Conjure Rain waters planted crops; watered crops keep growing while you travel and rest, but dry soil stops growth.";
            if (statusFirst)
            {
                baseLine += DescribeAfflictions();
                if (!string.IsNullOrEmpty(ground)) baseLine += "\n" + ground;
                if (!string.IsNullOrEmpty(cooking)) baseLine += "\n" + cooking;
                if (!string.IsNullOrEmpty(growth)) baseLine += "\n" + growth;
            }
            if (!string.IsNullOrWhiteSpace(Text))
                baseLine += (statusFirst ? "\n\n" : " ") + Text.Trim();

            var carriedReward = ParentEntity?.GetPart<LegendaryIdentityPart>()?.DescribeCarriedReward();
            if (!string.IsNullOrWhiteSpace(carriedReward))
                baseLine += "\n" + carriedReward;

            var directions = ParentEntity?.GetPart<RegionalSignpostPart>()?.GetDirectionsText();
            if (!string.IsNullOrWhiteSpace(directions))
                baseLine += "\n" + directions;

            var exploration = SpreadExplorationReadout.Describe(ParentEntity);
            if (!string.IsNullOrEmpty(exploration)) baseLine += "\n" + exploration;

            if (ItemExamineService.TryDescribeDetails(ParentEntity, out string itemDetails))
                baseLine += "\n" + itemDetails;

            // Item Enhancements (E.1–E.5): append per-enhancement effect
            // lines so the player sees what each attached IItemEnhancement
            // does. Tier-aware text comes from each enhancement's
            // GetEffectDescription override. Items with no enhancements
            // (the vast majority) produce no extra output here.
            if (ParentEntity?.Parts != null)
            {
                for (int i = 0; i < ParentEntity.Parts.Count; i++)
                {
                    if (ParentEntity.Parts[i] is IItemEnhancement enh)
                    {
                        // ASCII bullet — the bullet dot is not a CP437 glyph
                        // and rendered as '?' in the log.
                        baseLine += "\n  - " + enh.GetEffectDescription();
                    }
                }
            }

            if (!statusFirst) baseLine += DescribeAfflictions();
            return baseLine;
        }

        private string DescribeAfflictions()
        {
            var effects = ParentEntity?.GetPart<StatusEffectsPart>()?.GetAllEffects();
            if (effects == null || effects.Count == 0) return string.Empty;
            string result = "\nAfflicted:";
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null) result += "\n  - " + EffectDescriber.Describe(effects[i]);
            return result;
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
