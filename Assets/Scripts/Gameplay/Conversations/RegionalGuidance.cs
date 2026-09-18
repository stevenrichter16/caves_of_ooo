using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Storylets;
using UnityEngine;

namespace CavesOfOoo.Core
{
    [Serializable]
    public sealed class RegionalTravelLead
    {
        public string DestinationZoneID, DestinationName, OriginZoneID, QuestID;
        public string Text => RegionalGuidance.Describe(this);
    }

    /// <summary>Read-only map-derived leads. No zone generation or service invention.</summary>
    public static class RegionalGuidance
    {
        public const string ActionName="RememberRegionalDirection";
        public static IReadOnlyList<RegionalTravelLead> Build(Zone zone,Entity speaker,Entity player)
        {
            var result=new List<RegionalTravelLead>();
            var manager=WorldLocationContext.For(zone);
            if(manager==null||zone==null||!manager.CachedZones.TryGetValue(zone.ZoneID,out var live)||!ReferenceEquals(live,zone)
                ||speaker==null||player==null||!player.HasTag("Player")||zone.GetEntityCell(speaker)==null||zone.GetEntityCell(player)==null
                ||speaker.GetStatValue("Hitpoints",0)<=0||player.GetStatValue("Hitpoints",0)<=0||CombatSystem.IsDeathHandled(speaker)||CombatSystem.IsDeathHandled(player))return result;
            if(FactionManager.IsHostile(speaker,player)&&!speaker.HasTag("SpeaksToHostiles"))return result;
            string conversation=speaker.GetPart<ConversationPart>()?.ConversationID;
            bool morrowfastClerk=conversation=="Morrowfast_Vennit"&&zone.ZoneID==MorrowfastSceneRuntime.ZoneID
                &&ReferenceEquals(speaker,MorrowfastSceneRuntime.FindOwner(zone,"east-robed-resident"));
            if(conversation!="Scribe_1"&&conversation!="Innkeeper_1"&&!morrowfastClerk)return result;
            var origin=WorldMap.FromZoneID(zone.ZoneID);
            if(!zone.ZoneID.StartsWith("Overworld.",StringComparison.Ordinal)||origin.x<0||origin.x>=20||origin.y<0||origin.y>=20||origin.z!=0)return result;
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                var poi=manager.WorldMap.GetPOI(x,y);if(poi?.Type!=POIType.Village||string.IsNullOrWhiteSpace(poi.Name)||(x==origin.x&&y==origin.y))continue;
                string id=WorldMap.ToZoneID(x,y,0);
                // A known empty/ruined destination must not be sold as living work.
                if(manager.CachedZones.TryGetValue(id,out var cached)&&!cached.GetAllEntities().Any(e=>e.HasPart<ConversationPart>()&&e.GetStatValue("Hitpoints",0)>0&&!CombatSystem.IsDeathHandled(e)))continue;
                string quest=VillagePopulationBuilder.PickVillageQuest(id);
                if(!Available(quest))quest=null;
                if(quest!=null&&cached!=null&&!cached.GetAllEntities().Any(e=>e.GetPart<QuestBeaconPart>()?.Quest==quest&&e.GetStatValue("Hitpoints",0)>0&&!CombatSystem.IsDeathHandled(e)))quest=null;
                result.Add(new RegionalTravelLead{DestinationZoneID=id,DestinationName=poi.Name,OriginZoneID=zone.ZoneID,QuestID=quest});
            }
            return result.OrderBy(l=>Distance(origin.x,origin.y,l)).ThenBy(l=>WorldMap.FromZoneID(l.DestinationZoneID).y).ThenBy(l=>WorldMap.FromZoneID(l.DestinationZoneID).x).Take(4).ToArray();
        }
        private static int Distance(int x,int y,RegionalTravelLead l){var p=WorldMap.FromZoneID(l.DestinationZoneID);return Math.Abs(p.x-x)+Math.Abs(p.y-y);}
        internal static bool Available(string quest)=>!string.IsNullOrEmpty(quest)&&!(StoryletPart.Current?.IsQuestActive(quest)??false)&&!(StoryletPart.Current?.IsQuestCompleted(quest)??false);
        internal static string Describe(RegionalTravelLead lead)
        {
            var from=WorldMap.FromZoneID(lead.OriginZoneID);var to=WorldMap.FromZoneID(lead.DestinationZoneID);
            var directions=new List<string>();int dx=to.x-from.x,dy=to.y-from.y;
            if(dx!=0)directions.Add(Math.Abs(dx)+" "+(dx>0?"east":"west"));
            if(dy!=0)directions.Add(Math.Abs(dy)+" "+(dy>0?"south":"north"));
            string text=lead.DestinationName+" ("+to.x+","+to.y+"): from ("+from.x+","+from.y+"), "+string.Join(", ",directions)+" world-map cells.";
            if(Available(lead.QuestID))text+=" Local lead: "+StoryletPart.QuestDisplayName(lead.QuestID)+".";
            return text;
        }
        public static string TryRemember(Zone zone,Entity speaker,Entity player,string destinationID)
        {
            var lead=Build(zone,speaker,player).FirstOrDefault(l=>l.DestinationZoneID==destinationID);
            if(lead==null){MessageLog.Add("Those directions are no longer available.");return "guidance_unavailable";}
            if(!RegionalTravelNotes.Remember(player,lead))return "travel_notes_full";
            MessageLog.Add(lead.Text+" Recorded in [Q], [Tab] travel notes.");return null;
        }
        public static void AppendChoices(List<CavesOfOoo.Data.ChoiceData> choices)
        {
            string conversation=ConversationManager.CurrentConversation?.ID,node=ConversationManager.CurrentNode?.ID;
            if(!(((conversation=="Scribe_1"||conversation=="Morrowfast_Vennit")&&node=="RegionOverview")||(conversation=="Innkeeper_1"&&node=="Rumors")))return;
            foreach(var lead in Build(SettlementRuntime.ActiveZone,ConversationManager.Speaker,ConversationManager.Listener))
            {
                var at=WorldMap.FromZoneID(lead.DestinationZoneID);
                choices.Add(new CavesOfOoo.Data.ChoiceData{Text="Note "+lead.DestinationName+" ("+at.x+","+at.y+")"+(lead.QuestID!=null?" - local work":""),Target="",
                    Actions=new List<CavesOfOoo.Data.ConversationParam>{new CavesOfOoo.Data.ConversationParam{Key=ActionName,Value=lead.DestinationZoneID}}});
            }
        }
    }
    public static class RegionalTravelNotes
    {
        private const string Prefix="RegionalTravelNote:";
        public const int MaximumNotes=17;
        internal static bool Remember(Entity player,RegionalTravelLead lead)
        {
            string key=Prefix+lead.DestinationZoneID;
            if(!player.Properties.ContainsKey(key)&&player.Properties.Keys.Count(k=>k.StartsWith(Prefix,StringComparison.Ordinal))>=MaximumNotes)return false;
            player.Properties[key]=JsonUtility.ToJson(lead);return true;
        }
        public static IReadOnlyList<string> Read(Entity player)
        {
            var notes=new List<string>();if(player==null)return notes;
            foreach(var pair in player.Properties.Where(p=>p.Key.StartsWith(Prefix,StringComparison.Ordinal)).OrderBy(p=>p.Key,StringComparer.Ordinal).Take(MaximumNotes))
            {
                try{var lead=JsonUtility.FromJson<RegionalTravelLead>(pair.Value);if(lead!=null&&!string.IsNullOrEmpty(lead.DestinationName)&&!string.IsNullOrEmpty(lead.OriginZoneID)&&!string.IsNullOrEmpty(lead.DestinationZoneID))notes.Add(lead.Text);}
                catch(ArgumentException){/* Corrupt optional note must not break the quest journal. */}
            }
            return notes;
        }
    }
}
