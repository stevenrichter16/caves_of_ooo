using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Builds the pure-data <see cref="SkillsScreenSnapshot"/> consumed
    /// by the skills-screen UI renderer. Pure function — same testability
    /// shape as <see cref="QuestLogStateBuilder"/> + <c>HotbarStateBuilder</c>
    /// + <c>SidebarStateBuilder</c>.
    ///
    /// <para>Per Docs/SKILL-TREE-QUD-PARITY.md ST.7a. The renderer side
    /// (MonoBehaviour + Tilemap + 'x' hotkey wiring) is ST.7b.</para>
    ///
    /// <para>Iterates the <see cref="SkillRegistry"/> in registration
    /// order, emitting one row per skill tree (tree-root) and one row per
    /// power within each tree. Per-row state (Owned / Buyable /
    /// InsufficientSP / RequirementsNotMet) is computed against the
    /// actor's current SP, owned skills, and stat values using the same
    /// gating logic as <see cref="BuySkillAction"/> — duplicated
    /// inline here to avoid coupling the state-builder to a side-effect-
    /// having action class. The duplication is small (~30 lines) and
    /// will be refactored to a shared <c>SkillGating</c> helper if a
    /// third consumer materializes.</para>
    /// </summary>
    public static class SkillsScreenStateBuilder
    {
        /// <summary>
        /// Build a snapshot from the actor's <see cref="SkillsPart"/>
        /// + <see cref="Stat"/> "SP". Returns an empty snapshot when
        /// the actor lacks either — callers can render an empty-state
        /// message without null checks.
        /// </summary>
        public static SkillsScreenSnapshot Build(Entity actor)
        {
            if (actor == null)
                return new SkillsScreenSnapshot(null, 0);

            var skills = actor.GetPart<SkillsPart>();
            int sp = actor.GetStatValue("SP", 0);
            if (skills == null)
                return new SkillsScreenSnapshot(null, sp);

            var rows = new List<SkillsScreenRow>();
            foreach (var skill in SkillRegistry.GetAllSkills())
            {
                if (skill == null) continue;

                bool ownsTree = skills.HasSkill(skill.Class);
                var (treeState, treeObfuscated) = EvaluateRowState(
                    actor, skills, sp,
                    @class: skill.Class,
                    cost: skill.Cost,
                    attribute: skill.Attribute,
                    minimum: "",   // skill trees don't carry Minimum in Qud
                    requires: "",
                    exclusion: "",
                    flagsBits: skill.Flags,
                    isOwned: ownsTree);

                rows.Add(new SkillsScreenRow(
                    @class: skill.Class,
                    displayName: treeObfuscated ? "???" : skill.Name,
                    description: skill.Description ?? string.Empty,
                    isTreeRoot: true,
                    parentSkillName: string.Empty,
                    cost: skill.Cost,
                    state: treeState,
                    isObfuscated: treeObfuscated));

                if (skill.Powers == null) continue;
                for (int i = 0; i < skill.Powers.Count; i++)
                {
                    var p = skill.Powers[i];
                    if (p == null) continue;

                    bool ownsPower = skills.HasSkill(p.Class);
                    var (powerState, powerObfuscated) = EvaluateRowState(
                        actor, skills, sp,
                        @class: p.Class,
                        cost: p.Cost,
                        attribute: p.Attribute,
                        minimum: p.Minimum,
                        requires: p.Requires,
                        exclusion: p.Exclusion,
                        flagsBits: p.Flags,
                        isOwned: ownsPower);

                    rows.Add(new SkillsScreenRow(
                        @class: p.Class,
                        displayName: powerObfuscated ? "???" : p.Name,
                        description: p.Description ?? string.Empty,
                        isTreeRoot: false,
                        parentSkillName: skill.Name,
                        cost: p.Cost,
                        state: powerState,
                        isObfuscated: powerObfuscated));
                }
            }

            return new SkillsScreenSnapshot(rows, sp);
        }

        // ────────────────────────────────────────────────────────────────────
        // Gating evaluator. Returns (state, isObfuscated). Inlined from
        // BuySkillAction's gating helpers to avoid coupling. Order matches
        // BuySkillAction.Execute so what the UI shows == what the action
        // would do. Returns:
        //   Owned              if isOwned (no further checks needed)
        //   InsufficientSP     if requirements met but cost > sp
        //   Buyable            if all gates pass
        //   RequirementsNotMet otherwise (stat min OR missing prereq OR
        //                                  exclusion blocking)
        // isObfuscated = (FLAG_OBFUSCATED set) AND (state == RequirementsNotMet).
        // ────────────────────────────────────────────────────────────────────

        private static (SkillsScreenRowState state, bool isObfuscated) EvaluateRowState(
            Entity actor, SkillsPart skills, int sp,
            string @class, int cost,
            string attribute, string minimum, string requires, string exclusion,
            int flagsBits, bool isOwned)
        {
            if (isOwned)
                return (SkillsScreenRowState.Owned, false);

            // Check requirements (stat min + Requires + Exclusion). If ANY
            // fail, state = RequirementsNotMet. Cost only matters once
            // requirements pass — Qud orders it the same way.
            bool reqsMet = MeetsAttributeMinimum(actor, attribute, minimum)
                        && MeetsRequires(skills, requires)
                        && !HasAnyExclusion(skills, exclusion);

            bool obfuscatedFlag = (flagsBits & SkillData.FLAG_OBFUSCATED) != 0;

            if (!reqsMet)
                return (SkillsScreenRowState.RequirementsNotMet, obfuscatedFlag);

            if (sp < cost)
                return (SkillsScreenRowState.InsufficientSP, false);

            return (SkillsScreenRowState.Buyable, false);
        }

        private static bool MeetsAttributeMinimum(Entity actor, string attribute, string minimum)
        {
            if (string.IsNullOrWhiteSpace(attribute) || string.IsNullOrWhiteSpace(minimum))
                return true;

            string[] orGroupsAttr = attribute.Split('|');
            string[] orGroupsMin  = minimum.Split('|');
            int n = orGroupsAttr.Length < orGroupsMin.Length ? orGroupsAttr.Length : orGroupsMin.Length;

            for (int g = 0; g < n; g++)
            {
                string[] attrs = orGroupsAttr[g].Split(',');
                string[] mins  = orGroupsMin[g].Split(',');
                int gn = attrs.Length < mins.Length ? attrs.Length : mins.Length;

                bool groupPasses = true;
                for (int i = 0; i < gn; i++)
                {
                    string attrName = attrs[i].Trim();
                    if (!int.TryParse(mins[i].Trim(), out int minValue)) continue;
                    if (actor.GetStatValue(attrName, 0) < minValue) { groupPasses = false; break; }
                }
                if (groupPasses) return true;
            }
            return false;
        }

        private static bool MeetsRequires(SkillsPart skills, string requires)
        {
            if (string.IsNullOrWhiteSpace(requires)) return true;
            foreach (var raw in requires.Split(','))
            {
                string cls = raw.Trim();
                if (cls.Length == 0) continue;
                if (!skills.HasSkill(cls)) return false;
            }
            return true;
        }

        private static bool HasAnyExclusion(SkillsPart skills, string exclusion)
        {
            if (string.IsNullOrWhiteSpace(exclusion)) return false;
            foreach (var raw in exclusion.Split(','))
            {
                string cls = raw.Trim();
                if (cls.Length == 0) continue;
                if (skills.HasSkill(cls)) return true;
            }
            return false;
        }
    }
}
