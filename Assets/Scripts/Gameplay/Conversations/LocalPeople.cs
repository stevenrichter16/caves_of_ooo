using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Persistent ownership of five authored individuals, not a generic
    /// uniqueness tag. Stored in the existing World part graph.</summary>
    public sealed class LocalPeopleLedgerPart : Part, ISaveSerializable
    {
        public override string Name => "LocalPeopleLedger";
        internal OverworldZoneManager RuntimeOwner;
        internal readonly Dictionary<string, Entity> LiveOwners = new Dictionary<string, Entity>();
        internal readonly Dictionary<string, (string id, string zone)> Claims = new Dictionary<string, (string, string)>();
        public int Count => Claims.Count;
        public void Save(SaveWriter writer)
        {
            writer.Write(1); writer.Write(Claims.Count);
            foreach (var pair in Claims.OrderBy(p => p.Key, StringComparer.Ordinal))
            { writer.WriteString(pair.Key); writer.WriteString(pair.Value.id); writer.WriteString(pair.Value.zone); }
        }
        public void Load(SaveReader reader)
        {
            int version = reader.ReadInt(), count = reader.ReadInt();
            if (version != 1 || count < 0 || count > LocalPeople.UniqueBlueprints.Count)
                throw new InvalidDataException("Invalid local people ledger version/count.");
            var staged = new Dictionary<string, (string id, string zone)>();
            for (int i = 0; i < count; i++)
            {
                string blueprint = reader.ReadString(), id = reader.ReadString(), zone = reader.ReadString();
                if (!LocalPeople.UniqueBlueprints.Contains(blueprint) || string.IsNullOrEmpty(id) || id.Length > 128
                    || string.IsNullOrEmpty(zone) || zone.Length > 128 || staged.ContainsKey(blueprint))
                    throw new InvalidDataException("Invalid local people ownership record.");
                staged.Add(blueprint, (id, zone));
            }
            Claims.Clear(); foreach (var pair in staged) Claims.Add(pair.Key, pair.Value);
        }
    }

    /// <summary>Fresh-generation personal labels and read-only local speech.
    /// Uses no gameplay RNG and never rewrites cached people or shared dialogue.</summary>
    public static class LocalPeople
    {
        public static readonly IReadOnlyList<string> UniqueBlueprints = Array.AsReadOnly(new[] { "Mogu", "Grib", "Nam", "Sien", "Sopp" });
        private static readonly HashSet<string> Roles = new HashSet<string>(new[] {
            "Elder", "Villager", "Weaponsmith", "Armorer", "Apothecary", "Arcanist", "Provisioner", "Tinker", "Merchant", "Quartermaster",
            "Warden", "WellKeeper", "Farmer", "Innkeeper", "Undertaker", "Scribe", "PalimpsestEcho", "SaccharineEnvoy", "ConcordFactor",
            "PaleCurator", "TentRightHost", "SaltMaster", "RecensionScribe", "CurationSorter", "PeatCutter", "FilerClerk", "GantryRegistrar" });
        // Constructed CoO names. First/surname combinations are finite and are
        // reserved per settlement before role labels are appended.
        private static readonly Dictionary<string, string[][]> Pools = new Dictionary<string, string[][]> {
            ["Villagers"] = new[] { new[] { "Vellin", "Perret", "Dovrin", "Kesset", "Brinna", "Ostrel", "Yevven", "Nerrel" }, new[] { "Reedfall", "Lowbank", "Ternfold", "Mosswick", "Rillpost", "Wethand", "Sedgefold", "Driftbent" } },
            ["Palimpsest"] = new[] { new[] { "Esset", "Harlun", "Vessil", "Tovren", "Nerrit", "Calven", "Orriel", "Dremma" }, new[] { "Vell", "Tern", "Rusk", "Vask", "Senn", "Ollit", "Halm", "Vesk" } },
            ["SaccharineConcord"] = new[] { new[] { "Tavvit", "Rennel", "Ossik", "Veldra", "Darret", "Silven", "Istren", "Povra" }, new[] { "Tallmark", "Pennel", "Drav", "Fennit", "Colwick", "Settern", "Valmet", "Rosset" } },
            ["PaleCuration"] = new[] { new[] { "Nereth", "Vostel", "Halvra", "Tessel", "Orren", "Salvet", "Doreth", "Ivrin" }, new[] { "Kell", "Marn", "Vellat", "Stenn", "Sorre", "Pelv", "Tammet", "Ress" } },
            ["BowerFolk"] = new[] { new[] { "Velisse", "Tavelle", "Nerissa", "Ollive", "Perris", "Dovelle", "Kessil", "Ylvet" }, new[] { "Amberturn", "Reedcurve", "Palespan", "Softbend", "Brighthold", "Sidelight", "Stillarch", "Roseangle" } },
            ["TentRight"] = new[] { new[] { "Varrik", "Nesset", "Torven", "Elveth", "Parrit", "Sennel", "Orrava", "Dovrek" }, new[] { "Clothward", "Shadefold", "Saltstep", "Polehand", "Waterkept", "Westcloth", "Stillcup", "Reedtie" } },
            ["CatacombFolk"] = new[] { new[] { "Nellit", "Vossel", "Ternik", "Pavva", "Orrik", "Dessel", "Kelmra", "Issen" }, new[] { "Warmwall", "Thirdniche", "Greenward", "Lampfold", "Lowglow", "Patchside", "Stonekin", "Inward" } }
        };
        private static readonly ConditionalWeakTable<OverworldZoneManager, LocalPeopleLedgerPart> Ledgers = new ConditionalWeakTable<OverworldZoneManager, LocalPeopleLedgerPart>();
        private static LocalPeopleLedgerPart Ledger(OverworldZoneManager manager)
        {
            return Ledgers.GetValue(manager, m => { var ledger = new LocalPeopleLedgerPart { RuntimeOwner = m }; AdoptCached(m, ledger); return ledger; });
        }
        private static void AdoptCached(OverworldZoneManager manager, LocalPeopleLedgerPart ledger)
        {
            foreach (var pair in manager.CachedZones.OrderBy(p => p.Key, StringComparer.Ordinal))
                foreach (var actor in pair.Value.GetReadOnlyEntities().OrderBy(e => e.ID, StringComparer.Ordinal))
                    if (UniqueBlueprints.Contains(actor.BlueprintName) && !ledger.Claims.ContainsKey(actor.BlueprintName) && !string.IsNullOrEmpty(actor.ID))
                        { ledger.Claims.Add(actor.BlueprintName, (actor.ID, pair.Key)); ledger.LiveOwners[actor.BlueprintName] = actor; }
        }
        /// <summary>Attach this manager's claims before serialization. Keeps a null
        /// World null when there are no claims; refuses another manager's ledger.</summary>
        public static Entity BindForSave(OverworldZoneManager manager, Entity world)
        {
            if (manager == null) return world;
            var ledger = Ledger(manager); var existing = world?.GetPart<LocalPeopleLedgerPart>();
            if (existing != null && !ReferenceEquals(existing, ledger))
                throw new InvalidOperationException("World already owns another local people ledger.");
            if (ledger.Count == 0 && existing == null) return world;
            if (world == null) { world = new Entity { BlueprintName = "World" }; world.SetTag("WorldEntity"); }
            if (existing == null) world.AddPart(ledger);
            return world;
        }
        /// <summary>Bind only the loaded candidate manager; legacy graphs adopt
        /// cached owners without changing their names or removing duplicates.</summary>
        public static void Restore(OverworldZoneManager manager, Entity world)
        {
            if (manager == null) return;
            var saved = world?.GetPart<LocalPeopleLedgerPart>();
            if (saved != null)
            {
                if (saved.RuntimeOwner != null && !ReferenceEquals(saved.RuntimeOwner, manager))
                    throw new InvalidOperationException("Cannot borrow another world's resident claims.");
                saved.RuntimeOwner = manager; Ledgers.Remove(manager); Ledgers.Add(manager, saved);
            }
            Ledger(manager); // Old saves adopt cached ownership without altering people.
        }

        /// <summary>Process fresh overworld generation only. Cached zones are
        /// untouched; five authored individuals are claimed and generic roles named.</summary>
        public static void Apply(Zone zone, OverworldZoneManager manager)
        {
            if (zone == null || manager?.Factory == null || !WorldMap.IsOverworldZoneID(zone.ZoneID)) return;
            if (manager.CachedZones.TryGetValue(zone.ZoneID, out var cached) && ReferenceEquals(cached, zone)) return;
            var ledger = Ledger(manager);
            foreach (var actor in zone.GetReadOnlyEntities().ToArray())
            {
                if (!UniqueBlueprints.Contains(actor.BlueprintName) || string.IsNullOrEmpty(actor.ID)) continue;
                if (ledger.Claims.TryGetValue(actor.BlueprintName, out var claim))
                {
                    if (ledger.LiveOwners.TryGetValue(actor.BlueprintName, out var owner) && ReferenceEquals(owner, actor)) continue;
                    zone.RemoveEntity(actor);
                    Record("UniqueResidentSuppressed", actor, zone.ZoneID, "already-claimed");
                }
                else
                {
                    ledger.Claims.Add(actor.BlueprintName, (actor.ID, zone.ZoneID));
                    ledger.LiveOwners[actor.BlueprintName] = actor;
                    Record("UniqueResidentClaimed", actor, zone.ZoneID, "");
                }
            }
            var people = zone.GetReadOnlyEntities().Where(e => e.HasPart<RenderPart>()).ToArray();
            var used = new HashSet<string>(people.Select(e => e.GetProperty("LocalPersonalName", e.GetPart<RenderPart>().DisplayName)), StringComparer.OrdinalIgnoreCase);
            // All named authored people are reserved even when they live elsewhere.
            foreach (var bp in manager.Factory.Blueprints.Values)
                if (!Roles.Contains(bp.Name) && bp.Parts.TryGetValue("Conversation", out _) && bp.Parts.TryGetValue("Render", out var render)
                    && render.TryGetValue("DisplayName", out var label)) used.Add(label);
            foreach (var actor in people.OrderBy(e => zone.GetEntityPosition(e).y).ThenBy(e => zone.GetEntityPosition(e).x).ThenBy(e => e.BlueprintName, StringComparer.Ordinal))
            {
                if (!Roles.Contains(actor.BlueprintName) || actor.Properties.ContainsKey("LocalPersonalName") || !(Guid.TryParse(actor.ID, out _) || (int.TryParse(actor.ID, out int factoryId) && factoryId > 0))) continue;
                if (!manager.Factory.Blueprints.TryGetValue(actor.BlueprintName, out var blueprint)
                    || !blueprint.Parts.TryGetValue("Render", out var authoredRender) || !authoredRender.TryGetValue("DisplayName", out var role)
                    || actor.GetPart<RenderPart>().DisplayName != role
                    || !blueprint.Parts.TryGetValue("Conversation", out var conversation)
                    || !conversation.TryGetValue("ConversationID", out var conversationId)
                    || actor.GetPart<ConversationPart>()?.ConversationID != conversationId || actor.HasPart<CavesOfOoo.Storylets.QuestBeaconPart>()) continue;
                string culture = FactionManager.GetFaction(actor);
                if (!Pools.TryGetValue(culture ?? "", out var pool)) { Record("LocalResidentNameRejected", actor, zone.ZoneID, "unsupported-culture"); continue; }
                var pos = zone.GetEntityPosition(actor); int count = pool[0].Length * pool[1].Length;
                uint hash = StableHash(manager.WorldSeed.ToString(CultureInfo.InvariantCulture) + ":" + zone.ZoneID + ":" + culture + ":" + actor.BlueprintName + ":" + pos.x + ":" + pos.y);
                string personal = null;
                for (int offset = 0; offset < count; offset++)
                {
                    int slot = (int)((hash + (uint)offset) % (uint)count);
                    string candidate = pool[0][slot / pool[1].Length] + " " + pool[1][slot % pool[1].Length];
                    if (used.Add(candidate)) { personal = candidate; break; }
                }
                if (personal == null) { Record("LocalResidentNameRejected", actor, zone.ZoneID, "name-pool-exhausted"); continue; }
                actor.Properties["LocalPersonalName"] = personal; actor.Properties["LocalCulture"] = culture; actor.Properties["LocalRole"] = role;
                actor.GetPart<RenderPart>().DisplayName = personal + ", " + role;
                Record("LocalResidentNamed", actor, zone.ZoneID, "");
            }
        }
        private static uint StableHash(string text)
        { unchecked { uint hash = 2166136261; foreach (char c in text) { hash ^= c; hash *= 16777619; } return hash; } }
        private static void Record(string kind, Entity actor, string zone, string reason)
        { if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", kind, actor, null, new { zone, reason }); }

        /// <summary>Read live local context for three generic nodes. Returns the
        /// supplied authored text when inapplicable; never mutates shared dialogue.</summary>
        public static string DescribeConversation(Entity speaker, Entity listener, string conversation, string node, string fallback)
        {
            if (conversation != "Villager_1" || (node != "Start" && node != "PassingThrough" && node != "Dangers")
                || speaker == null || (speaker.BlueprintName != "Villager" && speaker.BlueprintName != "Undertaker") || listener == null) return fallback;
            var zone = speaker.SpatialZone; var manager = WorldLocationContext.For(zone);
            if (manager == null || listener.SpatialZone != zone || !manager.CachedZones.TryGetValue(zone.ZoneID, out var cached) || !ReferenceEquals(cached, zone)
                || speaker.GetStatValue("Hitpoints") <= 0 || listener.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(speaker)
                || CombatSystem.IsDeathHandled(listener) || FactionManager.IsHostile(speaker, listener)) return fallback;
            var loc = WorldMap.FromZoneID(zone.ZoneID); var poi = manager.WorldMap.GetPOI(loc.x, loc.y);
            if (loc.z != 0 || poi?.Type != POIType.Village || string.IsNullOrWhiteSpace(poi.Name)) return fallback;
            string culture = speaker.GetProperty("LocalCulture", FactionManager.GetFaction(speaker));
            if (!Pools.ContainsKey(culture ?? "")) return fallback;
            if (node == "Start") return Observation(culture, poi.Name);
            if (node == "PassingThrough")
            {
                Entity contact = null;
                foreach (string role in new[] { "Scribe", "Innkeeper", "WellKeeper", "Merchant" })
                {
                    contact = zone.GetReadOnlyEntities().FirstOrDefault(e => e.BlueprintName == role && e.GetStatValue("Hitpoints") > 0
                        && !CombatSystem.IsDeathHandled(e) && !string.IsNullOrEmpty(e.GetPart<ConversationPart>()?.ConversationID) && !FactionManager.IsHostile(e, listener));
                    if (contact != null) break;
                }
                return Lead(culture, contact?.GetDisplayName());
            }
            var state = manager.SettlementManager.GetAllSettlementsSnapshot();
            if (!state.TryGetValue(zone.ZoneID, out var settlement)) return "I have no current word on a well here.";
            var site = settlement.GetSite(SettlementSiteDefinitions.MainWellSiteId);
            if (site == null || !zone.GetReadOnlyEntities().Any(e => e.HasPart<WellSitePart>())) return "I have no current word on a well here.";
            return Water(culture, site.Stage);
        }
        private static string Observation(string culture, string place)
        {
            switch (culture)
            {
                case "Palimpsest": return "I live in " + place + ". That much I can attest. What account do you need?";
                case "SaccharineConcord": return place + ". Ask the rate before you agree to anything. It saves both sides a quarrel.";
                case "PaleCuration": return place + ". Arrival noted. Condition: on your feet. Tell me if that changes.";
                case "BowerFolk": return place + ". Half a step to your left. Yes, the light catches your sleeve there.";
                case "TentRight": return "There is shade here. Rest. This is " + place + "; your business can wait.";
                case "CatacombFolk": return "This is " + place + ". Stay by the light a moment. Your eyes will settle.";
                default: return "You're in " + place + ". Mind where you leave those boots; someone has to sweep.";
            }
        }
        private static string Lead(string culture, string contact)
        {
            if (contact == null) return "I have no local contact to send you to just now.";
            switch (culture)
            {
                case "Palimpsest": return contact + " is here. Ask them for their own account; I won't put words in it.";
                case "SaccharineConcord": return "Ask " + contact + ". What they can offer, and on what terms, is theirs to say.";
                case "PaleCuration": return "Local contact: " + contact + ". Take your questions there.";
                case "BowerFolk": return "Look for " + contact + ". Leave them room to turn when you approach.";
                case "TentRight": return "When you have rested, " + contact + " is here to speak with. You need not hurry.";
                case "CatacombFolk": return contact + " is here. Speak plainly; there's no need to raise your voice.";
                default: return "Try " + contact + ". They're here, and they'll tell you what they can.";
            }
        }
        private static string Water(string culture, RepairStage stage)
        {
            bool safe = stage == RepairStage.StableRepair || stage == RepairStage.ImprovedWithCaretaker;
            bool temporary = stage == RepairStage.TemporarilyPurified;
            switch (culture)
            {
                case "Palimpsest": return safe ? "The latest word is that the well repair is holding." : temporary ? "The well was cleared for now. That account does not promise it will hold." : "The latest word is that the well is fouled. I would not draw drinking water from it.";
                case "SaccharineConcord": return safe ? "The well repair is holding. Better to keep it that way than pay for another." : temporary ? "The well is clear for now. That is not the same bargain as repaired." : "The well needs work. Paying for a cup won't make that water fit to drink.";
                case "PaleCuration": return safe ? "Well condition: repaired. Status: holding." : temporary ? "Well condition: cleared. Duration: provisional." : "Well condition: fouled. Drinking: not advised.";
                case "BowerFolk": return safe ? "The well has been put right. Leave its rim as you found it." : temporary ? "The well is clear for now. It still needs attention." : "Leave the water in that well. It needs tending before anyone takes it.";
                case "TentRight": return safe ? "The well has been repaired. That is what I can tell you about it." : temporary ? "The well is clear for now. I will not promise you more than that." : "No, not water from that well. It needs tending first.";
                case "CatacombFolk": return safe ? "The well is holding since the repair. Good. One less thing to tend tonight." : temporary ? "The well is clear for now. We'll need to look at it again." : "The well needs tending. Leave that water be for now.";
                default: return safe ? "The well repair is holding. That's one less worry." : temporary ? "The well is clear for now. Don't mistake that for a lasting repair." : "The well needs tending. Don't drink from it yet.";
            }
        }
    }
}
