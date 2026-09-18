using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Small persisted request ledger, separate from global storylets
    /// and destination-keyed directions. Text is composed on interaction/journal
    /// opening; cue queries read the integer completion key directly.</summary>
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
