using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Small persisted request ledger, separate from global storylets
    /// and destination-keyed directions. Text is recorded on interaction; journal opening reads the stored
    /// receipt without world queries. Cues read the integer completion key directly.</summary>
    public static class RegionalSituationNotes
    {
        private const string Prefix="RegionalSituationNote:";
        internal static string NoteKey(string instance)=>Prefix+instance;
        internal static string CompletionKey(string instance)=>"RegionalSituationComplete:"+instance;
        internal static string Record(Entity player,RegionalSituationDefinition definition,string instance,string state,
            Entity recipient,EntityFactory factory,bool localSupplyAvailable)
        {
            if(player==null||definition==null)return string.Empty;
            string text=Describe(definition,state,recipient,factory,localSupplyAvailable);
            player.Properties[NoteKey(instance)]=text;
            return text;
        }
        // Historical receipt composed from the successful transaction's own
        // outcome. Reading it never rechecks shelves, habitats or living NPCs.
        internal static void RecordCompletion(Entity player, RegionalSituationDefinition definition,
            string instance, Entity recipient, EntityFactory factory, int paidDrams, bool? habitatPreserved)
        {
            string who=recipient?.GetDisplayName()??"the request's recipient";
            string result=definition.Title+" [completed]. "
                +(definition.Kind==RegionalSituationKind.Recovery?"The recovered consignment supplied ":"Delivered ")
                +definition.ItemCount+" x "
                +ItemName(factory,definition.ItemBlueprint)+" to "+who+" in "+definition.RecipientName
                +" ("+definition.RecipientX+","+definition.RecipientY+"). Paid: "+paidDrams
                +" drams and one "+ItemName(factory,definition.RewardBlueprint)+".";
            if(habitatPreserved.HasValue)
                result+=habitatPreserved.Value
                    ?" The marked bank was preserved at delivery; the payment includes three extra drams."
                    :" The marked bank was not preserved at delivery; no preservation bonus was paid.";
            result+=" The goods joined the recipient's trade stock at delivery. If they remain available, "
                +"their conversation offers trade; the goods may since have been sold. This request is settled and cannot pay again.";
            player.Properties[NoteKey(instance)]=result;
        }
        // Definitions are already baked by the native factory. Read their labels
        // instead of creating throwaway items or printing internal blueprint IDs.
        internal static string ItemName(EntityFactory factory,string blueprint)
        {
            if(factory!=null&&blueprint!=null&&factory.Blueprints.TryGetValue(blueprint,out var data)
                &&data.Parts.TryGetValue("Render",out var render)&&render.TryGetValue("DisplayName",out var name)
                &&!string.IsNullOrWhiteSpace(name))return name;
            return "requested goods";
        }
        internal static string Describe(RegionalSituationDefinition d,string state,Entity recipient,
            EntityFactory factory,bool localSupplyAvailable)
        {
            string source=d.SourceX+","+d.SourceY,target=d.RecipientX+","+d.RecipientY;
            string who=recipient?.GetDisplayName()??"the request's recipient";
            if(state=="released")
                return d.Title+" [released]. You released this request with "+who+" in "+d.RecipientName
                    +" ("+target+"). No goods or payment changed hands. You may inquire there again, "
                    +(d.Kind==RegionalSituationKind.Recovery
                        ?"if the recipient and marked consignment remain available."
                        :"if the recipient remains available; outside goods are still accepted after reaccepting.")
                    +" This is not an active delivery.";
            string request=d.Kind==RegionalSituationKind.Supply
                ?"Bring "+d.ItemCount+" x "+ItemName(factory,d.ItemBlueprint)+"; bought or already carried goods are welcome."
                :"Recover the marked sealed consignment intact; ordinary loose goods do not replace it.";
            string availability=d.Kind==RegionalSituationKind.Supply&&!localSupplyAvailable
                ?" At this reading, the marked local supply is unavailable or exhausted. Outside goods still fulfill the request."
                :string.Empty;
            string warning=d.SourceBiome==BiomeType.Grovelands?" Choir law charges standing for digging local mineral veins."
                :d.SourceBiome==BiomeType.Sodden?" Follow the dry approach; heat makes peat release marsh gas. Leaving the marked bank intact earns three extra drams."
                :d.SourceBiome==BiomeType.Beating?" Height exposure parches bare heads; headgear or actual interior shelter protects you."
                :localSupplyAvailable?" The marked ripe rows can be harvested once, leaving stubble.":string.Empty;
            return d.Title+" ["+state+"]. Source ("+source+"); return to "+who+" in "+d.RecipientName+" ("+target+"). "
                +request+availability+warning+" Payment: "+d.RewardDrams+" drams and one "+ItemName(factory,d.RewardBlueprint)
                +". Read, deliver, or release through the recipient's [C] actions.";
        }
        public static IReadOnlyList<string> Read(Entity player)
        {
            var notes=new List<KeyValuePair<string,string>>();
            if(player!=null)foreach(var p in player.Properties)
                if(p.Key.StartsWith(Prefix,StringComparison.Ordinal)&&!string.IsNullOrWhiteSpace(p.Value))notes.Add(p);
            notes.Sort((a,b)=>string.CompareOrdinal(a.Key,b.Key));
            var result=new List<string>(notes.Count);foreach(var p in notes)result.Add(p.Value);return result;
        }
    }
}
