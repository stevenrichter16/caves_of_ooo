using System.Collections.Generic;
using System.Linq;
using System.Text;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What it takes to repair one of a place's data-defined working structures (<see cref="PlaceSiteData"/>), read without changing anything: whether a repair would be refused and why,
    /// and what the player still lacks. <see cref="SettlementManager.RepairPlaceSite"/> validates through <see cref="Check"/>, so the refusal a menu shows and the refusal the repair gives
    /// are one rule; a world object for a site (see <see cref="PlaceSiteObjectPart"/>) reads the same.
    /// </summary>
    public static class PlaceSiteRepairs
    {
        public const string UnknownSite = "unknown_site", AlreadyRepaired = "already_repaired", NoPatch = "no_patch", AlreadyPatched = "already_patched", MissingMaterials = "missing_materials", MissingTool = "missing_tool", MissingPlayer = "missing_player";

        /// <summary>Null when the repair could be made now; otherwise the refusal code. The order is the one the repair has always used.</summary>
        public static string Check(PlaceSiteData data, RepairableSiteState site, bool lasting, Entity player)
        {
            if (player == null) return MissingPlayer;
            if (data == null || site == null) return UnknownSite;
            if (site.Stage >= RepairStage.StableRepair) return AlreadyRepaired;
            var fix = lasting ? data.Lasting : data.Patch;
            if (!lasting && !data.HasPatch) return NoPatch;
            if (!lasting && site.Stage == RepairStage.TemporarilyPurified) return AlreadyPatched;
            foreach (var m in fix.Materials) if (CarriedAmount(player, m) < m.Count) return MissingMaterials;
            if (!string.IsNullOrEmpty(fix.Tool) && ConcernItems.CountCarried(player, fix.Tool) < 1) return MissingTool;
            return null;
        }

        /// <summary>A material or tool in plain words with what the player carries: "iron spike x2 (you have 1)".</summary>
        public static string Line(string blueprint, int need, int have)
            => NameOf(blueprint) + (need > 1 ? " x" + need : "") + " (you have " + have + ")";

        internal static string MaterialName(PlaceSiteMaterial material) =>
            string.IsNullOrEmpty(material.SubstituteBlueprint) ? NameOf(material.Blueprint)
                : "sound " + NameOf(material.Blueprint) + " or " + NameOf(material.SubstituteBlueprint);

        internal static int CarriedAmount(Entity actor, PlaceSiteMaterial material)
        {
            if (string.IsNullOrEmpty(material.SubstituteBlueprint))
                return ConcernItems.CountCarried(actor, material.Blueprint);
            return PayableUnits(actor, material).Sum(choice => choice.item.GetPart<StackerPart>()?.StackCount ?? 1);
        }

        private static IEnumerable<(Entity item, MaterialProcessItemState payload)> PayableUnits(Entity actor, PlaceSiteMaterial material)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (pack == null) yield break;
            var factory = WorldLocationContext.For(actor.SpatialZone)?.Factory ?? HarvestablePart.Factory;
            bool comparePayload = !string.IsNullOrEmpty(material.SubstituteBlueprint);
            foreach (string blueprint in new[] { material.Blueprint, material.SubstituteBlueprint })
            {
                if (string.IsNullOrEmpty(blueprint)) continue;
                var template = comparePayload ? MaterialProcessTemplate.Capture(factory, blueprint) : null;
                if (comparePayload && template == null) continue;
                foreach (var item in pack.Objects)
                {
                    if (item?.BlueprintName != blueprint || !WorldResourceActions.Carried(actor, item, single: false)
                        || !pack.CanConsumeOne(item)) continue;
                    var payload = template?.Snapshot(item);
                    if (!comparePayload || payload != null) yield return (item, payload);
                }
            }
        }

        internal static bool TryPaySubstituteBill(Entity actor, Zone zone, PlaceSiteFixData fix, InventoryTransaction tx)
        {
            var pack = actor.GetPart<InventoryPart>();
            if (pack == null) return false;
            foreach (var material in fix.Materials)
            {
                if (material.Count != 1) return false;
                var (item, payload) = PayableUnits(actor, material).FirstOrDefault();
                if (item == null || !tx.TryClaim(item, actor, "PlaceSiteRepair")
                    || !MaterialProcessService.ReserveInput(item, tx)) return false;
                string id = item.ID, blueprint = item.BlueprintName;
                var physics = item.GetPart<PhysicsPart>();
                var stack = item.GetPart<StackerPart>();
                int before = stack?.StackCount ?? 1;
                bool PayloadCurrent() => item.ID == id && item.BlueprintName == blueprint
                    && item.GetPart<PhysicsPart>() == physics && item.GetPart<StackerPart>() == stack
                    && (payload == null || payload.Matches());
                bool HasUniqueGroundIdentity() => !zone.GetReadOnlyEntities().Any(other => other?.ID == id);
                if (!HasUniqueGroundIdentity() || !MaterialProcessPayment.Spend(actor, item, actor, zone, tx,
                    PayloadCurrent, refundAllowed: HasUniqueGroundIdentity)) return false;
                tx.BeforeCommit(() => PayloadCurrent() && HasUniqueGroundIdentity()
                    && (before > 1 ? stack.StackCount == before - 1 && WorldResourceActions.Carried(actor, item, single: false)
                        : item.SpatialZone == null && physics.InInventory == null && physics.Equipped == null
                            && !pack.Objects.Any(other => other?.ID == id)));
            }
            return true;
        }

        /// <summary>Everything one fix asks for, each with the player's count: "iron spike x2 (you have 1), oak haft (you have 0); tool: hatchet (you have 1)".</summary>
        public static string Needs(PlaceSiteFixData fix, Entity player)
        {
            var b = new StringBuilder();
            foreach (var m in fix.Materials) { if (b.Length > 0) b.Append(", "); b.Append(MaterialName(m) + (m.Count > 1 ? " x" + m.Count : "") + " (you have " + CarriedAmount(player, m) + ")"); }
            if (!string.IsNullOrEmpty(fix.Tool)) b.Append(b.Length > 0 ? "; tool: " : "tool: ").Append(Line(fix.Tool, 1, ConcernItems.CountCarried(player, fix.Tool)));
            return b.ToString();
        }

        /// <summary>What a fix asks for, without counts: "2 iron spike, 1 oak haft, 1 leather binding (and a hatchet to work with)".</summary>
        public static string NeedsPlain(PlaceSiteFixData fix)
        {
            var parts = new List<string>();
            foreach (var m in fix.Materials) parts.Add(m.Count + " " + MaterialName(m));
            string text = string.Join(", ", parts);
            if (!string.IsNullOrEmpty(fix.Tool)) text += (text == "" ? "" : " ") + "(and a " + NameOf(fix.Tool) + " to work with)";
            return text;
        }

        /// <summary>What is still missing, as one sentence: "You still need 1 more iron spike and 1 oak haft." Empty when nothing is.</summary>
        public static string Missing(PlaceSiteFixData fix, Entity player)
        {
            var parts = new List<string>();
            foreach (var m in fix.Materials)
            {
                int short_ = m.Count - CarriedAmount(player, m);
                if (short_ > 0) parts.Add(short_ + (short_ == m.Count ? " " : " more ") + MaterialName(m));
            }
            if (!string.IsNullOrEmpty(fix.Tool) && ConcernItems.CountCarried(player, fix.Tool) < 1) parts.Add("a " + NameOf(fix.Tool) + " to work with (it is not used up)");
            if (parts.Count == 0) return "";
            string joined = parts.Count == 1 ? parts[0] : string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " and " + parts[parts.Count - 1];
            return "You still need " + joined + ".";
        }

        /// <summary>"IronSpikeComponent" as "iron spike": split on capitals, lower case, a trailing "component" dropped.</summary>
        public static string NameOf(string blueprint)
        {
            if (string.IsNullOrEmpty(blueprint)) return "";
            if (blueprint == "FieldHaftComponent") return "rough field haft";
            var b = new StringBuilder();
            for (int i = 0; i < blueprint.Length; i++) { if (i > 0 && char.IsUpper(blueprint[i])) b.Append(' '); b.Append(char.ToLowerInvariant(blueprint[i])); }
            string text = b.ToString(); return text.EndsWith(" component") ? text.Substring(0, text.Length - " component".Length) : text;
        }
    }
}
