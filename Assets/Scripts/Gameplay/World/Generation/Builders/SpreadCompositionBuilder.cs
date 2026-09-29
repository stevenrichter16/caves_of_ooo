using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>Realizes recovered-country land use once, using native entities.
    /// Explicitly selected only for ordinary Spread surface generation.</summary>
    public sealed class SpreadCompositionBuilder : IZoneBuilder
    {
        public string Name=>"SpreadComposition";
        public int Priority=>2000;
        public Formation FormationOverride=Formation.None;
        /// <summary>Opt-in new-world interior grammar; default preserves legacy saves.</summary>
        public SpreadExplorationTopology Topology=SpreadExplorationTopology.Legacy;
        public SpreadCompositionPlan Plan {get;private set;}
        /// <summary>Exact successful cold-build graph; cleared before every attempt.</summary>
        public Zone SourceZone {get;private set;}
        /// <summary>Opt-in exact Hedge provenance for new-world FieldPassage or HeavySalvage.
        /// This must be enabled before the cold build; it grants no live rewrite authority.</summary>
        public bool CapturePassageSources;
        readonly Dictionary<Entity,SpreadGenerationReceipt> passageSources=new Dictionary<Entity,SpreadGenerationReceipt>();
        EntityFactory passageFactory; SpreadCompositionPlan passagePlan; int passageRevision;
        internal IReadOnlyList<SpreadGenerationReceipt> PassageSources=>passageSources.Values.ToArray();
        internal bool OwnsPassageReceipt(SpreadGenerationReceipt receipt)=>CapturePassageSources&&receipt!=null
            &&SourceZone==receipt.Zone&&passageFactory==receipt.Factory&&passageRevision==receipt.Revision
            &&ReferenceEquals(Plan,passagePlan)&&receipt.Owners.Count==1
            &&passageSources.TryGetValue(receipt.Owners[0],out var current)&&ReferenceEquals(current,receipt);
        internal SpreadGenerationReceipt PassageSource(Entity hedge)=>hedge!=null&&passageSources.TryGetValue(hedge,out var receipt)&&OwnsPassageReceipt(receipt)?receipt:null;
        /// <summary>Exact original finite rows, captured only for a selected cold
        /// cooking patch. This does not authorize live source discovery or harvest.</summary>
        public bool CaptureCookingSources;
        readonly Dictionary<Entity,SpreadGenerationReceipt> cookingSources=new Dictionary<Entity,SpreadGenerationReceipt>();
        internal IReadOnlyList<SpreadGenerationReceipt> CookingSources=>cookingSources.Values.ToArray();
        internal bool OwnsCookingReceipt(SpreadGenerationReceipt receipt)=>CaptureCookingSources&&receipt!=null
            &&SourceZone==receipt.Zone&&passageFactory==receipt.Factory&&passageRevision==receipt.Revision
            &&ReferenceEquals(Plan,passagePlan)&&receipt.Owners.Count==1
            &&cookingSources.TryGetValue(receipt.Owners[0],out var current)&&ReferenceEquals(current,receipt);
        internal SpreadGenerationReceipt CookingSource(Entity row)=>row!=null&&cookingSources.TryGetValue(row,out var receipt)&&OwnsCookingReceipt(receipt)?receipt:null;
        private readonly int seed;
        public SpreadCompositionBuilder(int worldSeed){seed=worldSeed;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            Plan=null;SourceZone=null;passageSources.Clear();cookingSources.Clear();passageFactory=null;passagePlan=null;int revision=++passageRevision;
            if(zone==null||factory==null||zone.EntityCount!=0)
            {
                Diag.Record("worldgen","SpreadCompositionRejected",payload:new{reason=zone==null?"missing-zone":factory==null?"missing-factory":"nonempty-zone"});
                return false;
            }
            Plan=SpreadCompositionPlan.Create(zone.ZoneID,seed,FormationOverride,Topology);
            var ripe=SelectGleanings(Plan);
            if(ripe.Count>0&&!factory.Blueprints.ContainsKey("RipeCropRow"))return Reject("missing-ripe-row");
            int objects=0,water=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(BuilderSpawn.TryPlace(zone,factory,Plan.GroundAt(x,y),x,y)==null)return Reject("missing-ground");
                if(Plan.IsWater(x,y)||Plan.IsApproach(x,y))zone.GenReservedCells.Add((x,y));
                if(Plan.IsWater(x,y)){zone.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);water++;}
                string bp=Plan.ObjectAt(x,y);
                if(bp==null)continue;
                if(ripe.Contains(y*Zone.Width+x))bp="RipeCropRow";
                var created=BuilderSpawn.TryPlace(zone,factory,bp,x,y);
                if(created==null)return Reject("missing-object");
                if(CapturePassageSources&&bp=="Hedge"&&created.BlueprintName==bp)
                    passageSources.Add(created,new SpreadGenerationReceipt(this,zone,factory,revision,new[]{created},1));
                if(CaptureCookingSources&&bp=="RipeCropRow"&&created.BlueprintName==bp)
                    cookingSources.Add(created,new SpreadGenerationReceipt(this,zone,factory,revision,new[]{created},1));
                objects++;
            }
            SourceZone=zone;passageFactory=factory;passagePlan=Plan;
            Diag.Record("worldgen","SpreadCompositionPlanned",payload:new{zoneId=zone.ZoneID,seed,
                formation=Plan.Formation.ToString(),condition=Plan.Condition,objects,water});
            return true;
        }
        // Rank the existing planned rows, never alter their footprint or consume
        // caller RNG. The same seed retains its few gleanings after regeneration.
        private static HashSet<int> SelectGleanings(SpreadCompositionPlan plan)
        {
            var candidates=new List<int>();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(plan.ObjectAt(x,y)=="CropRow")candidates.Add(y*Zone.Width+x);
            candidates.Sort((a,b)=>
            {
                int rank=plan.Roll(a%Zone.Width,a/Zone.Width,409).CompareTo(plan.Roll(b%Zone.Width,b/Zone.Width,409));
                return rank!=0?rank:a.CompareTo(b);
            });
            int budget=plan.Condition=="tended"?3:plan.Condition=="returning scrub"?2:1;
            var result=new HashSet<int>();
            for(int i=0;i<Math.Min(budget,candidates.Count);i++)result.Add(candidates[i]);
            return result;
        }
        private static bool Reject(string reason)
        {Diag.Record("worldgen","SpreadCompositionRejected",payload:new{reason});return false;}
    }
}
