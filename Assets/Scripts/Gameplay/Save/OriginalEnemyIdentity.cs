using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Read-side identity migration for the retired prototype enemy roster.
    /// Changes only exact authored identities; never rebuilds a saved creature,
    /// grants gear/skills, rewrites arbitrary prose, or changes reference IDs.
    /// Kept at the save boundary so retired blueprints cannot spawn in new games.
    /// </summary>
    internal static class OriginalEnemyIdentity
    {
        internal static string Blueprint(string value)
        {
            switch (value)
            {
                case "Snapjaw": return "MarlbackScrabbler";
                case "SnapjawScavenger": return "MarlbackGleaner";
                case "SnapjawHunter": return "MarlbackTunnelguard";
                case "SnapjawChieftain": return "MarlbackWallkeeper";
                case "SnapjawWarlord": return "MarlbackBreacher";
                case "SnapjawCorpse": return "MarlbackCorpse";
                case "SnapjawClaw": return "MarlbackRake";
                case "SnapjawHunterClaw": return "MarlbackGuardRake";
                case "WarlordCleaver": return "BreacherCleaver";
                case "GlowMoth": return "GroveLanternMoth";
                default: return value;
            }
        }

        internal static string Faction(string value) => value == "Snapjaws" ? "OutlandRaiders" : value;
        internal static string PlaceName(string value) => value == "Snapjaw Lair" ? "Marlback Burrow" : value;

        internal static Dictionary<string, int> Reputation(Dictionary<string, int> values)
        {
            if (values.TryGetValue("Snapjaws", out int oldValue))
            {
                // A current identity already in a mixed-version save wins;
                // transferring a legacy entry must not overwrite later play.
                if (!values.ContainsKey("OutlandRaiders")) values["OutlandRaiders"] = oldValue;
                values.Remove("Snapjaws");
            }
            return values;
        }

        internal static void EntityBody(Entity entity)
        {
            string previous = entity.BlueprintName;
            var render = entity.GetPart<RenderPart>();
            string display = render?.DisplayName;
            entity.BlueprintName = Blueprint(previous);
            // Two old quest builders used a hidden prototype base. Preserve
            // their independent identities and quest parts, including named saves.
            var oldObjective = entity.GetPart<CavesOfOoo.Storylets.FinishObjectiveWhenSlain>();
            bool soot = previous == "Snapjaw" && (entity.GetPart<CavesOfOoo.Storylets.SetFactWhenSlain>()?.Fact == "rbg_gremlin_routed"
                || (oldObjective?.Quest == "CinnamonBunFavor" && oldObjective.Objective == "drive_off_gremlin"));
            bool gnome = previous == "Snapjaw" && entity.GetPart<CavesOfOoo.Storylets.AddFactWhenSlain>()?.Fact == "warren_gnomes_routed";
            bool quest = soot || gnome;
            if (quest) entity.BlueprintName = soot ? "SootGremlin" : "DirtGnome";
            if (render != null)
            {
                string oldDefault = DefaultDisplay(previous);
                if (oldDefault != null && display == oldDefault)
                    render.DisplayName = DefaultDisplay(entity.BlueprintName);
            }
            if (entity.Tags.TryGetValue("Faction", out string faction)) entity.Tags["Faction"] = Faction(faction);
            if (entity.Properties.TryGetValue("NaturalWeapon", out string weapon))
            {
                string current = Blueprint(weapon);
                entity.Properties["NaturalWeapon"] = quest && current == "MarlbackRake" ? "ScavengerClaw" : current;
            }
            if (quest)
            {
                var body = entity.GetPart<Body>();
                if (body != null)
                {
                    NormalizeQuestRecipe(body.GetBody());
                    if (body.DismemberedParts != null)
                        foreach (var lost in body.DismemberedParts) NormalizeQuestRecipe(lost.Part);
                }
            }
            if (entity.Tags.TryGetValue("KilledSnapjaw", out string killed))
            {
                if (!entity.Tags.ContainsKey("KilledMarlback")) entity.Tags["KilledMarlback"] = killed;
                entity.Tags.Remove("KilledSnapjaw");
            }
            var corpse = entity.GetPart<CorpsePart>();
            if (corpse != null)
                corpse.CorpseBlueprint = quest ? "CreatureCorpse" : Blueprint(corpse.CorpseBlueprint);
            // Exact old item flavor is identity-bearing, but custom descriptions
            // belong to the save and are not searched/replaced.
            var examine = entity.GetPart<ExaminablePart>();
            if (previous == "WarlordCleaver" && examine != null && examine.Description == OldCleaverDescription)
                examine.Description = "A salvaged wedge-blade lashed to a short haft. Shale grit fills its binding; the broad edge is worn from splitting roots and breaking packed banks.";
        }

        // Only the old base creature's recipe is genericized. Existing payload
        // instances, custom recipes, equipment and limb state remain untouched.
        private static void NormalizeQuestRecipe(BodyPart part)
        {
            if (part == null) return;
            if (part.DefaultBehaviorBlueprint == "MarlbackRake") part.DefaultBehaviorBlueprint = "ScavengerClaw";
            if (part.Parts != null)
                foreach (var child in part.Parts) NormalizeQuestRecipe(child);
        }

        private const string OldCleaverDescription = "A snapjaw warband banner-blade, notched from a hundred raids. Heavier than it looks; meaner than it needs to be.";

        private static string DefaultDisplay(string blueprint)
        {
            switch (blueprint)
            {
                case "Snapjaw": return "snapjaw";
                case "SnapjawScavenger": return "snapjaw scavenger";
                case "SnapjawHunter": return "snapjaw hunter";
                case "SnapjawChieftain": return "snapjaw chieftain";
                case "SnapjawWarlord": return "snapjaw warlord";
                case "SnapjawCorpse": return "snapjaw corpse";
                case "GlowMoth": return "glow-moth";
                case "WarlordCleaver": return "warlord's cleaver";
                case "MarlbackScrabbler": return "marlback scrabbler";
                case "MarlbackGleaner": return "marlback gleaner";
                case "MarlbackTunnelguard": return "marlback tunnelguard";
                case "MarlbackWallkeeper": return "marlback wallkeeper";
                case "MarlbackBreacher": return "marlback breacher";
                case "MarlbackCorpse": return "marlback remains";
                case "GroveLanternMoth": return "grove lantern-moth";
                case "BreacherCleaver": return "wedge-cleaver";
                case "SootGremlin": return "soot gremlin";
                case "DirtGnome": return "dirt gnome";
                default: return null;
            }
        }
    }
}
