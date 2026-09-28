using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>Historical reports from frozen eligible addresses, never remote owners.
    /// Only the current bounded offer can authorize an explicit note write.</summary>
    public static class SpreadDiscoveryReports
    {
        public const string ActionName = "RememberSpreadDiscovery";
        private static Offer[] offers = Array.Empty<Offer>();
        private static ulong revision;

        private sealed class Context
        {
            internal Zone Zone;
            internal OverworldZoneManager Manager;
            internal WorldMap Map;
            internal SpreadRareEncounterPlan Plan;
            internal SpreadWayhousePlan Wayhouse;
            internal Entity Speaker, Player;
            internal PhysicsPart SpeakerPhysics, PlayerPhysics;
            internal RenderPart SpeakerRender, PlayerRender;
            internal ConversationPart ConversationPart;
            internal ConversationData Conversation;
            internal NodeData Node;
            internal Cell SpeakerCell, PlayerCell;
            internal string SpeakerID, PlayerID, Name, ConversationID, NodeID;
        }

        private sealed class Offer
        {
            internal string Token;
            internal Context Source;
            internal SpreadDiscoveryNotes.Record Record;
        }

        public static void ClearOffers() => offers = Array.Empty<Offer>();

        public static void AppendChoices(List<ChoiceData> choices)
        {
            ClearOffers();
            if (choices == null || !TryContext(out var context)) return;
            var records = Build(context);
            var current = new List<Offer>(3);
            // This is a UI revision, not gameplay randomness or saved discovery state.
            revision = revision == ulong.MaxValue ? 1 : revision + 1;
            foreach (var record in records)
            {
                string token = record.Family + "|" + revision;
                current.Add(new Offer { Token = token, Source = context, Record = record });
                choices.Add(new ChoiceData
                {
                    Text = record.Family == SpreadDiscoveryNotes.Pair
                        ? "Remember ditch-cutters (unconfirmed)"
                        : record.Family == SpreadDiscoveryNotes.Viper ? "Remember chalk-ring vipers (unconfirmed)"
                        : "Remember Turnbank wayhouse (unconfirmed)",
                    Target = "",
                    Actions = new List<ConversationParam> { new ConversationParam { Key = ActionName, Value = token } }
                });
            }
            offers = current.ToArray();
        }

        public static string AppendText(string original)
        {
            if (!TryContext(out var context)) return original;
            string text = original ?? "";
            foreach (var record in Build(context))
                text += (text.Length == 0 ? "" : "\n\n") + SpreadDiscoveryNotes.Describe(record);
            return text;
        }

        public static string TryRemember(Entity speaker, Entity player, string token)
        {
            Offer offered = null;
            if (!string.IsNullOrEmpty(token))
                foreach (var offer in offers) if (offer.Token == token) { offered = offer; break; }
            if (offered == null || !ReferenceEquals(speaker, offered.Source.Speaker)
                || !ReferenceEquals(player, offered.Source.Player) || !TryContext(out var current)
                || !SameContext(offered.Source, current) || !StillSelected(current, offered.Record))
                return Refuse("discovery_offer_stale");
            if (!SpreadDiscoveryNotes.Remember(player, offered.Record)) return Refuse("discovery_note_invalid");
            MessageLog.Add("Report recorded in [Q], [Tab] travel notes; unconfirmed when heard.");
            return null;
        }

        private static string Refuse(string reason)
        {
            MessageLog.Add("That report is no longer available from this conversation.");
            return reason;
        }

        private static bool TryContext(out Context context)
        {
            context = null;
            var speaker = ConversationManager.Speaker;
            var player = ConversationManager.Listener;
            var conversation = ConversationManager.CurrentConversation;
            var node = ConversationManager.CurrentNode;
            string conversationID = conversation?.ID;
            if (!((conversationID == "Scribe_1" && node?.ID == "RegionOverview")
                || (conversationID == "Innkeeper_1" && node?.ID == "Rumors"))) return false;
            var zone = speaker?.SpatialZone;
            var manager = WorldLocationContext.For(zone);
            if (zone == null || manager?.WorldMap == null || !SpreadDiscoveryNotes.CanonicalSurface(zone.ZoneID)
                || !ReferenceEquals(manager.ActiveZone, zone) || !ReferenceEquals(SettlementRuntime.ActiveZone, zone)
                || !manager.CachedZones.TryGetValue(zone.ZoneID, out var cached) || !ReferenceEquals(cached, zone)
                || !Live(speaker, zone) || !Live(player, zone) || !player.HasTag("Player")
                || !ReferenceEquals(StoryletPart.LocalPlayer, player)) return false;
            var part = speaker.GetPart<ConversationPart>();
            var render = speaker.GetPart<RenderPart>();
            if (part == null || !ReferenceEquals(part.ParentEntity, speaker) || part.ConversationID != conversationID
                || render == null || !ReferenceEquals(render.ParentEntity, speaker)
                || !SpreadDiscoveryNotes.BoundedText(speaker.ID, 128) || !SpreadDiscoveryNotes.BoundedText(player.ID, 128)
                || !SpreadDiscoveryNotes.BoundedText(render.DisplayName, 160)
                || FactionManager.IsHostile(speaker, player) || FactionManager.IsHostile(player, speaker)) return false;
            context = new Context
            {
                Zone = zone, Manager = manager, Map = manager.WorldMap, Plan = manager.RareEncounters, Wayhouse = manager.Wayhouse,
                Speaker = speaker, Player = player, SpeakerID = speaker.ID, PlayerID = player.ID,
                SpeakerPhysics = speaker.GetPart<PhysicsPart>(), PlayerPhysics = player.GetPart<PhysicsPart>(),
                SpeakerRender = render, PlayerRender = player.GetPart<RenderPart>(), ConversationPart = part,
                Conversation = conversation, Node = node, ConversationID = conversationID, NodeID = node.ID,
                SpeakerCell = zone.GetEntityCell(speaker), PlayerCell = zone.GetEntityCell(player), Name = render.DisplayName
            };
            return true;
        }

        private static bool Live(Entity actor, Zone zone)
        {
            if (actor == null || !ReferenceEquals(actor.SpatialZone, zone) || zone.GetEntityCell(actor) == null
                || actor.GetStatValue("Hitpoints", 0) <= 0 || CombatSystem.IsDeathHandled(actor)) return false;
            var physics = actor.GetPart<PhysicsPart>();
            var render = actor.GetPart<RenderPart>();
            return physics != null && ReferenceEquals(physics.ParentEntity, actor)
                && physics.InInventory == null && physics.Equipped == null
                && (render == null || ReferenceEquals(render.ParentEntity, actor));
        }

        private static bool SameContext(Context a, Context b)
        {
            return ReferenceEquals(a.Zone, b.Zone) && ReferenceEquals(a.Manager, b.Manager)
                && ReferenceEquals(a.Map, b.Map) && ReferenceEquals(a.Plan, b.Plan)
                && ReferenceEquals(a.Wayhouse, b.Wayhouse)
                && ReferenceEquals(a.Speaker, b.Speaker) && ReferenceEquals(a.Player, b.Player)
                && ReferenceEquals(a.SpeakerPhysics, b.SpeakerPhysics) && ReferenceEquals(a.PlayerPhysics, b.PlayerPhysics)
                && ReferenceEquals(a.SpeakerRender, b.SpeakerRender) && ReferenceEquals(a.PlayerRender, b.PlayerRender)
                && ReferenceEquals(a.ConversationPart, b.ConversationPart)
                && ReferenceEquals(a.Conversation, b.Conversation) && ReferenceEquals(a.Node, b.Node)
                && ReferenceEquals(a.SpeakerCell, b.SpeakerCell) && ReferenceEquals(a.PlayerCell, b.PlayerCell)
                && a.SpeakerID == b.SpeakerID && a.PlayerID == b.PlayerID && a.Name == b.Name
                && a.ConversationID == b.ConversationID && a.NodeID == b.NodeID;
        }

        private static List<SpreadDiscoveryNotes.Record> Build(Context context)
        {
            var result = new List<SpreadDiscoveryNotes.Record>(3);
            if (context.Plan?.Initialized == true)
            {
                AddIfSelected(context, SpreadDiscoveryNotes.Pair, context.Plan.PairZoneID, result);
                AddIfSelected(context, SpreadDiscoveryNotes.Viper, context.Plan.ViperZoneID, result);
            }
            AddIfSelected(context, SpreadDiscoveryNotes.Wayhouse, context.Wayhouse?.ZoneID, result);
            return result;
        }

        private static void AddIfSelected(Context context, string family, string id, List<SpreadDiscoveryNotes.Record> result)
        {
            if (!Selected(context, family, id)) return;
            result.Add(new SpreadDiscoveryNotes.Record
            {
                Version = 1, Family = family, OriginZoneID = context.Zone.ZoneID, DestinationZoneID = id,
                InformantID = context.SpeakerID, InformantName = context.Name,
                Formation = FormationSelector.For(BiomeType.Spread, id).ToString()
            });
        }

        private static bool StillSelected(Context context, SpreadDiscoveryNotes.Record record)
        {
            return record.OriginZoneID == context.Zone.ZoneID && Selected(context, record.Family, record.DestinationZoneID)
                && record.Formation == FormationSelector.For(BiomeType.Spread, record.DestinationZoneID).ToString();
        }

        private static bool Selected(Context context, string family, string id)
        {
            if (!SpreadDiscoveryNotes.CanonicalSurface(id)) return false;
            if (family == SpreadDiscoveryNotes.Wayhouse) return context.Wayhouse?.Selects(context.Manager, id) == true;
            if (context.Plan?.Initialized != true) return false;
            if (family == SpreadDiscoveryNotes.Viper) return context.Plan.SelectsViper(context.Manager, id);
            if (family != SpreadDiscoveryNotes.Pair || context.Plan.PairZoneID != id
                || !SpreadRareEncounterPlan.IsEligible(context.Manager, id)) return false;
            var formation = FormationSelector.For(BiomeType.Spread, id);
            return formation == Formation.Hedgerow || formation == Formation.OldRoad;
        }
    }
}
