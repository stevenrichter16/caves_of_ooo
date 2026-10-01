using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Pure state of the new-game "pick a build" screen: which card is
    /// highlighted. Kept free of Unity so it is unit-tested; the popup in
    /// Presentation only draws it and maps keys onto these calls.
    /// </summary>
    public sealed class StartingBuildMenuModel
    {
        private readonly IReadOnlyList<StartingBuildDef> _options;

        public StartingBuildMenuModel(IReadOnlyList<StartingBuildDef> options)
        {
            _options = options ?? new List<StartingBuildDef>();
        }

        public IReadOnlyList<StartingBuildDef> Options => _options;
        public int SelectedIndex { get; private set; }
        public StartingBuildDef Selected => _options.Count == 0 ? null : _options[SelectedIndex];

        /// <summary>Move by <paramref name="delta"/> rows, wrapping at both ends.</summary>
        public void Move(int delta)
        {
            if (_options.Count == 0) return;
            int n = _options.Count;
            SelectedIndex = ((SelectedIndex + delta) % n + n) % n;
        }

        /// <summary>Jump to an index; an out-of-range index leaves the selection alone.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= _options.Count) return;
            SelectedIndex = index;
        }

        /// <summary>The highlighted build's card, wrapped to <see cref="StartingBuildCard.CardWidth"/>.</summary>
        public List<string> CardLines(EntityFactory factory)
            => Selected == null ? new List<string>() : StartingBuildCard.Lines(Selected, factory);
    }

    /// <summary>When the new-game picker appears at all.</summary>
    public static class StartingBuildSelection
    {
        /// <summary>Master switch; tests and tools set false.</summary>
        public static bool Enabled = true;

        /// <summary>
        /// True only for an ordinary interactive new game. Never for DevMode (it
        /// has its own sandbox kit), a scenario launch (the scenario shapes the
        /// player), a native audit (private save root, scripted input) or a
        /// batch run, and never when no builds are loaded (a missing file must
        /// fall back to the Classic start, not open a modal nobody can dismiss).
        /// </summary>
        public static bool ShouldPrompt(bool devMode, bool scenarioPending, bool auditSaveRoot, bool batchMode)
        {
            if (!Enabled) return false;
            if (devMode || scenarioPending || auditSaveRoot || batchMode) return false;
            return StartingBuildRegistry.All.Count > 0;
        }
    }

    /// <summary>The text of one build card.</summary>
    public static class StartingBuildCard
    {
        /// <summary>Columns available to the card (the popup's right-hand pane).</summary>
        public const int CardWidth = 50;

        public static List<string> Lines(StartingBuildDef b, EntityFactory factory)
        {
            var lines = new List<string>();
            if (b == null) return lines;

            lines.Add(b.Name ?? b.Id ?? "");
            Wrap(lines, "\"" + (b.Tagline ?? "") + "\"", CardWidth);
            lines.Add("");

            var a = b.Attributes ?? new StartingBuildAttributes();
            lines.Add("HP 40   DV " + DerivedDv(b, factory) + "   Speed 100");
            lines.Add("Str " + a.Strength + "  Agi " + a.Agility + "  Tou " + a.Toughness + "  Ego " + a.Ego);
            lines.Add("");

            var worn = new List<string>();
            var carried = new List<string>();
            foreach (var it in ItemsOf(b))
            {
                string name = ItemLabel(it.Blueprint, factory);
                int n = it.EffectiveCount;
                string label = n > 1 ? n + " " + name : name;
                (it.Equip ? worn : carried).Add(label);
            }
            if (worn.Count > 0) Wrap(lines, "Wearing: " + string.Join(", ", worn), CardWidth);
            if (carried.Count > 0) Wrap(lines, "Carrying: " + string.Join(", ", carried), CardWidth);

            var skills = new List<string>();
            foreach (var s in SkillsOf(b)) skills.Add(SkillLabel(s));
            if (skills.Count > 0) Wrap(lines, "Skills: " + string.Join(", ", skills), CardWidth);

            lines.Add("");
            if (!string.IsNullOrWhiteSpace(b.Weakness)) Wrap(lines, "Weakness: " + b.Weakness, CardWidth);
            if (!string.IsNullOrWhiteSpace(b.FirstBuy)) Wrap(lines, "First buy: " + b.FirstBuy, CardWidth);
            return lines;
        }

        /// <summary>Starting DV from the card's own numbers: 6 + Agility modifier
        /// + the DV of every worn item. Same formula as <c>CombatSystem.GetDV</c>.</summary>
        public static int DerivedDv(StartingBuildDef b, EntityFactory factory)
        {
            int agility = b?.Attributes?.Agility ?? 16;
            int dv = 6 + (int)Math.Floor((agility - 16) / 2.0);
            foreach (var it in ItemsOf(b))
            {
                if (!it.Equip || factory == null) continue;
                if (factory.Blueprints.TryGetValue(it.Blueprint ?? "", out var bp)
                    && bp.Parts.TryGetValue("Armor", out var armor)
                    && armor.TryGetValue("DV", out var raw)
                    && int.TryParse(raw, out int itemDv))
                    dv += itemDv;
            }
            return dv;
        }

        /// <summary>"Pyromancy_Kindle" becomes "Kindle"; "ShortBladesSkill" becomes
        /// "Short Blades"; "Cudgel_GroundPound" becomes "Ground Pound".</summary>
        public static string SkillLabel(string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return "";
            string core = className;
            int underscore = core.IndexOf('_');
            if (underscore >= 0) core = core.Substring(underscore + 1);
            else if (core.EndsWith("Skill") && core.Length > 5) core = core.Substring(0, core.Length - 5);

            var sb = new StringBuilder();
            for (int i = 0; i < core.Length; i++)
            {
                if (i > 0 && char.IsUpper(core[i]) && !char.IsUpper(core[i - 1])) sb.Append(' ');
                sb.Append(core[i]);
            }
            return sb.ToString();
        }

        // Classic's card is metadata in the JSON; its contents come from the legacy
        // kit so the card cannot drift from what Classic actually grants.
        private static IEnumerable<StartingBuildItem> ItemsOf(StartingBuildDef b)
        {
            if (b == null) yield break;
            if (b.IsClassic)
            {
                foreach (var e in NewGameLoadout.Items)
                    yield return new StartingBuildItem { Blueprint = e.Blueprint, Count = e.Count, Equip = e.Blueprint == "Dagger" };
                yield break;
            }
            foreach (var it in b.Items ?? new List<StartingBuildItem>())
                if (it != null) yield return it;
        }

        private static IEnumerable<string> SkillsOf(StartingBuildDef b)
        {
            if (b == null) yield break;
            if (b.IsClassic)
            {
                foreach (var s in CavesOfOoo.Skills.StartingSpellKit.SpellClasses) yield return s;
                yield break;
            }
            foreach (var s in b.Skills ?? new List<string>()) yield return s;
        }

        private static string ItemLabel(string blueprint, EntityFactory factory)
        {
            if (factory != null && factory.Blueprints.TryGetValue(blueprint ?? "", out var bp)
                && bp.Parts.TryGetValue("Render", out var render)
                && render.TryGetValue("DisplayName", out var display) && !string.IsNullOrWhiteSpace(display))
                return display;
            return SkillLabel("_" + blueprint).ToLowerInvariant();
        }

        private static void Wrap(List<string> lines, string text, int width)
        {
            if (string.IsNullOrEmpty(text)) { lines.Add(""); return; }
            var line = new StringBuilder();
            foreach (var word in text.Split(' '))
            {
                if (line.Length == 0) { line.Append(word); continue; }
                if (line.Length + 1 + word.Length > width)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                    line.Append("  ").Append(word);   // hanging indent
                }
                else line.Append(' ').Append(word);
            }
            if (line.Length > 0) lines.Add(line.ToString());
        }
    }
}
