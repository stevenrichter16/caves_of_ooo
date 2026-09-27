using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>Read-only local dialogue. Uses current owners/map and existing
    /// quest progress; never generates a destination, records a note or changes
    /// shared conversation data. IDs and quest actions remain authored.</summary>
    public static class FirstHourGuidance
    {
        public static string Describe(Entity speaker, Entity listener, string conversation, string node, string fallback)
        {
            if (!TryContext(speaker, listener, conversation, out var zone)) return fallback;
            if (IsGladeWarden(speaker, conversation, zone))
            {
                switch (node)
                {
                    case "Start": return "I keep watch over this clearing. Passing through, or looking for somewhere to go?";
                    case "PassingThrough": return "Keep your eyes on the path. " + GeographicLead(speaker);
                    case "AboutVillage": return "This is a clearing among the old walls. " + GeographicLead(speaker);
                    case "Threats": return "Leave yourself room to turn back. The walls can hide what is on the other side.";
                    case "OfferHelp": return "Keep your blade for your own need. I have no paid work to offer you.";
                    case "Creatures": return "Watch what is in front of you. A clear path now may not stay clear.";
                }
                return fallback;
            }
            if (!IsQuestSpeaker(speaker, listener, conversation, zone)) return fallback;
            if (conversation == "BMO_Quest" && (node == "Start" || node == "Accepted" || node == "Looking"))
            {
                if (StoryletPart.Current?.IsQuestCompleted("BmoCartridge") == true) return "It sings again. Thank you for finding the pith.";
                if (EllunReached()) return "You found the old stump. That's where the pith fell. Come tell me about it.";
                var marker = FindMarker(zone, listener);
                if (marker == null) return "The pith fell near the old stump, but I can't point you to it from here now.";
                var from = zone.GetEntityCell(speaker); var to = zone.GetEntityCell(marker);
                string direction = Direction(to.X - from.X, to.Y - from.Y);
                string lead = direction == null ? "The old stump is right here." : "The old stump is " + direction + " of where I'm standing.";
                return (node == "Start" ? "I lost the pith out of my tell. It's a little bone pin that makes it sing. " : "It's small and pale, like a tooth. ") + lead;
            }
            if (conversation == "RootBeerGuy_Quest" && (node == "Start" || node == "Accepted" || node == "Searching"))
            {
                if (StoryletPart.Current?.IsQuestCompleted("RootBeerGuyCase") == true) return "The witness-book is accounted for and the gremlin is gone. Nothing else on this case, partner.";
                HallunProgress(listener, out bool book, out bool gremlin);
                if (book && gremlin) return "The witness-book and the gremlin are both accounted for. Let's close the case, partner.";
                return "Here's what remains, partner. " + (book ? "The witness-book is accounted for. " : "Find my witness-book here in the village. ")
                    + (gremlin ? "The soot gremlin is already dealt with." : "Drive off the soot gremlin. Then report back to me.");
            }
            return fallback;
        }

        /// <summary>Return the original choice unless its label needs local
        /// wording. A copy retains the exact predicates/actions/target; authored
        /// data is never edited and selecting it follows the existing route.</summary>
        internal static ChoiceData PresentChoice(Entity speaker, Entity listener, string conversation, string node, ChoiceData choice)
        {
            if (choice == null || !TryContext(speaker, listener, conversation, out var zone)) return choice;
            string label = null;
            if (IsGladeWarden(speaker, conversation, zone))
            {
                if (node == "Start" && choice.Target == "AboutVillage") label = "What is this place?";
                if (node == "AboutVillage" && choice.Target == "Start") label = "Thanks for the directions.";
                if (node == "OfferHelp" && choice.Target == "End") label = "I'll keep that in mind.";
            }
            else if (IsQuestSpeaker(speaker, listener, conversation, zone) && node == "Start" && choice.Target == "Accepted")
            {
                if (conversation == "BMO_Quest" && EllunReached()) label = "[Accept] I've found the old stump.";
                if (conversation == "RootBeerGuy_Quest")
                {
                    HallunProgress(listener, out bool book, out bool gremlin);
                    label = book && gremlin ? "[Accept] Both are already accounted for." : book ? "[Accept] I'll deal with the gremlin."
                        : gremlin ? "[Accept] I'll find your witness-book." : "[Accept] I'll find the book and deal with the gremlin.";
                }
            }
            return label == null ? choice : new ChoiceData { Text = label, Target = choice.Target, Actions = choice.Actions, Predicates = choice.Predicates };
        }
        private static bool TryContext(Entity speaker, Entity listener, string conversation, out Zone zone)
        {
            zone = speaker?.SpatialZone;
            var manager = WorldLocationContext.For(zone);
            return zone != null && manager != null && manager.CachedZones.TryGetValue(zone.ZoneID, out var cached) && ReferenceEquals(zone, cached)
                && Live(speaker, zone) && Live(listener, zone) && listener.HasTag("Player")
                && speaker.GetPart<ConversationPart>() is ConversationPart part && ReferenceEquals(part.ParentEntity, speaker) && part.ConversationID == conversation
                && !FactionManager.IsHostile(speaker, listener) && !FactionManager.IsHostile(listener, speaker);
        }
        private static bool Live(Entity actor, Zone zone) => actor != null && ReferenceEquals(actor.SpatialZone, zone) && zone.GetEntityCell(actor) != null
            && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
            && actor.GetPart<PhysicsPart>() is PhysicsPart physics && ReferenceEquals(physics.ParentEntity, actor) && physics.InInventory == null && physics.Equipped == null;
        private static bool IsGladeWarden(Entity speaker, string conversation, Zone zone) => conversation == "Warden_1" && speaker.BlueprintName == "Warden" && ReferenceGladePlan.IsActive(zone);
        private static bool IsQuestSpeaker(Entity speaker, Entity listener, string conversation, Zone zone)
        {
            if (!ReferenceEquals(StoryletPart.LocalPlayer, listener) || zone.ZoneID != WorldMap.StartingZoneID || speaker.BlueprintName != "Villager") return false;
            var at = WorldMap.FromZoneID(zone.ZoneID);
            if (WorldLocationContext.For(zone).WorldMap.GetPOI(at.x, at.y)?.Type != POIType.Village) return false;
            var beacon = speaker.GetPart<QuestBeaconPart>();
            return beacon != null && ReferenceEquals(beacon.ParentEntity, speaker)
                && ((conversation == "BMO_Quest" && beacon.Quest == "BmoCartridge") || (conversation == "RootBeerGuy_Quest" && beacon.Quest == "RootBeerGuyCase"));
        }
        private static string GeographicLead(Entity speaker)
        {
            var lead = RegionalGuidance.BuildGeographicDirections(speaker).FirstOrDefault();
            return lead == null ? "I have no current directions to give you." : lead.Text + " I can't say who you'll find there.";
        }
        private static bool PastSearch(string quest) => StoryletPart.Current?.IsQuestCompleted(quest) == true || (StoryletPart.Current?.GetQuestState(quest)?.CurrentStageIndex ?? 0) >= 1;
        private static bool EllunReached() => PastSearch("BmoCartridge") || StoryletPart.Current?.IsObjectiveFinished("BmoCartridge", "reach_stump") == true || (NarrativeStatePart.Current?.GetFact("bmo_stump_reached") ?? 0) >= 1;
        private static void HallunProgress(Entity listener, out bool book, out bool gremlin)
        {
            bool report = PastSearch("RootBeerGuyCase");
            var inventory = listener.GetPart<InventoryPart>();
            book = report || StoryletPart.Current?.IsObjectiveFinished("RootBeerGuyCase", "find_notebook") == true
                || (inventory != null && ReferenceEquals(inventory.ParentEntity, listener) && inventory.Objects != null && inventory.Objects.Any(e => e != null && e.BlueprintName == "DetectiveNotebook" && e.SpatialZone == null
                    && e.GetPart<PhysicsPart>() is PhysicsPart p && ReferenceEquals(p.ParentEntity, e) && ReferenceEquals(p.InInventory, listener) && p.Equipped == null && inventory.CanConsumeOne(e)));
            gremlin = report || StoryletPart.Current?.IsObjectiveFinished("RootBeerGuyCase", "drive_off_gremlin") == true || (NarrativeStatePart.Current?.GetFact("rbg_gremlin_routed") ?? 0) >= 1;
        }
        private static Entity FindMarker(Zone zone, Entity listener)
        {
            Entity found = null;
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.BlueprintName != "OldStump" || entity.ID != "OldStump" || !ReferenceEquals(entity.SpatialZone, zone) || zone.GetEntityCell(entity) == null) continue;
                var marker = entity.GetPart<QuestMarkerTriggerPart>(); var physics = entity.GetPart<PhysicsPart>();
                if (marker == null || !ReferenceEquals(marker.ParentEntity, entity) || marker.Fact != "bmo_stump_reached" || marker.Value < 1
                    || physics == null || !ReferenceEquals(physics.ParentEntity, entity) || physics.InInventory != null || physics.Equipped != null || physics.Takeable || physics.Solid || entity.HasTag("Solid")
                    || (!string.IsNullOrEmpty(marker.TriggerFaction) && marker.TriggerFaction == FactionManager.GetFaction(listener))) continue;
                if (found != null) return null;
                found = entity;
            }
            return found;
        }
        private static string Direction(int dx, int dy)
        { string vertical = dy < 0 ? "north" : dy > 0 ? "south" : ""; string horizontal = dx < 0 ? "west" : dx > 0 ? "east" : ""; return dx == 0 && dy == 0 ? null : vertical + horizontal; }
    }
}
