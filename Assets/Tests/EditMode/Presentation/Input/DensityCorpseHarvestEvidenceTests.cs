using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DensityCorpseHarvestEvidenceTests
    {
        static Entity Corpse(Zone zone)
        {
            var e = new Entity { ID = "corpse", BlueprintName = "CreatureCorpse" };
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new HarvestablePart { YieldBlueprint = "RawMeat", YieldMin = 1, YieldMax = 2, YieldChance = 100 });
            e.Properties["SourceID"] = "target"; e.Properties["SourceBlueprint"] = "SunStriker";
            e.Properties["KillerID"] = "player"; e.Properties["KillerBlueprint"] = "Player";
            Assert.True(zone.AddEntity(e, 11, 10)); return e;
        }
        static bool Match(Entity e, Zone z) => DensityCorpseHarvestEvidence.IsExactCorpse(e,z,"player","target",11,10);
        [Test] public void ActualDeathMetadataAndCurrentFiniteRecipeIdentifyTheCorpse()
        {var z=new Zone("audit");var e=Corpse(z);Assert.True(Match(e,z));Assert.False(e.GetPart<HarvestablePart>().Harvested);Assert.AreSame(z.GetCell(11,10),z.GetEntityCell(e));}
        [TestCase("source-id")][TestCase("killer-id")][TestCase("species")][TestCase("corpse-blueprint")]
        [TestCase("detached")][TestCase("foreign-zone")][TestCase("foreign-inventory")][TestCase("moved")]
        [TestCase("spent")][TestCase("missing-recipe")][TestCase("wrong-product")][TestCase("wrong-chance")]
        public void SimilarRemainsOrInvalidOwnershipCannotSubstitute(string mutation)
        {
            var z=new Zone("audit");var e=Corpse(z);var h=e.GetPart<HarvestablePart>();
            if(mutation=="source-id")e.Properties["SourceID"] = "other";
            if(mutation=="killer-id")e.Properties["KillerID"] = "warden";
            if(mutation=="species")e.Properties["SourceBlueprint"] = "MarlbackScrabbler";
            if(mutation=="corpse-blueprint")e.BlueprintName="MarlbackCorpse";
            if(mutation=="detached")z.RemoveEntity(e);
            if(mutation=="foreign-zone"){z.RemoveEntity(e);new Zone("foreign").AddEntity(e,11,10);}
            if(mutation=="foreign-inventory")e.GetPart<PhysicsPart>().InInventory=new Entity();
            if(mutation=="moved")z.MoveEntity(e,12,10);
            if(mutation=="spent")h.Harvested=true;
            if(mutation=="missing-recipe")e.RemovePart(h);
            if(mutation=="wrong-product")h.YieldBlueprint="Bone";
            if(mutation=="wrong-chance")h.YieldChance=75;
            Assert.False(Match(e,z));
        }
        static List<Diag.Entry> Rows(int packed=1,int dropped=0)
            =>new List<Diag.Entry>{new Diag.Entry{TraceId="start",Category="scenario",Kind=DensityCorpseHarvestEvidence.MarkerKind,ActorId="player",TargetId="corpse"},
                new Diag.Entry{TraceId="harvest",Category="loot",Kind="Harvested",ActorId="player",TargetId="corpse",
                    PayloadJson="{\"source\":\"CreatureCorpse\",\"yield\":\"RawMeat\",\"count\":"+packed+",\"dropped\":"+dropped+",\"rollPassed\":true}"}};
        static bool Harvest(List<Diag.Entry> rows,int packed=1,int floor=0,bool spent=true,bool absent=true)
            =>DensityCorpseHarvestEvidence.CommittedHarvest(rows,"start","player","corpse",3,3+packed,4,4+floor,spent,absent);
        [TestCase(1,0)][TestCase(2,0)][TestCase(0,2)][TestCase(1,1)]
        public void FreshCommitMustAccountForBothPackedAndOverflowUnits(int packed,int floor)
        {Assert.True(Harvest(Rows(packed,floor),packed,floor));}
        [TestCase("stale")][TestCase("duplicate-marker")][TestCase("foreign-actor")][TestCase("wrong-corpse")]
        [TestCase("duplicate-commit")][TestCase("unspent")][TestCase("still-present")][TestCase("extra-packed")]
        [TestCase("missing-overflow")][TestCase("out-of-range")][TestCase("malformed")][TestCase("wrong-product")]
        public void CountsOrUnrelatedLogsAloneDoNotProveNativeHarvest(string mutation)
        {
            var rows=Rows();var r=rows[1];
            if(mutation=="stale"){rows.RemoveAt(1);rows.Insert(0,r);}
            else if(mutation=="duplicate-marker")rows.Insert(0,rows[0]);
            else if(mutation=="duplicate-commit")rows.Add(r);
            else if(mutation=="foreign-actor"){r.ActorId="npc";rows[1]=r;}
            else if(mutation=="wrong-corpse"){r.TargetId="other";rows[1]=r;}
            else if(mutation=="malformed"){r.PayloadJson="{broken";rows[1]=r;}
            else if(mutation=="wrong-product"){r.PayloadJson=r.PayloadJson.Replace("RawMeat","Bone");rows[1]=r;}
            else if(mutation=="missing-overflow")rows=Rows(1,1);
            else if(mutation=="out-of-range")rows=Rows(3,0);
            Assert.False(Harvest(rows,mutation=="extra-packed"?2:mutation=="out-of-range"?3:1,0,mutation!="unspent",mutation!="still-present"));
        }
        sealed class SavedGraph
        {
            public readonly Zone Before = new Zone("source"), Restored = new Zone("source"), Other = new Zone("other");
            public readonly Entity Old = Player(), Actor = Player();
            public readonly Entity Yield = new Entity { ID="earned", BlueprintName="RawMeat" };
            public SavedGraph()
            {
                Before.AddEntity(Old,10,10); Restored.AddEntity(Actor,10,10);
                Yield.AddPart(new PhysicsPart { Takeable=true });
                Yield.AddPart(new StackerPart { StackCount=2 });
                Actor.GetPart<InventoryPart>().AddObject(Yield);
            }
            static Entity Player(){var p=new Entity{ID="player",BlueprintName="Player"};p.AddPart(new InventoryPart());return p;}
            public bool Verify(Entity player=null,Zone zone=null) => DensityCorpseHarvestEvidence.RestoredYieldGraph(Old,Before,player??Actor,zone??Restored,
                new[]{Restored,Other},"target","corpse","earned",2);
        }
        [Test] public void EquivalentReplacementGraphRetainsActualEarnedOwnerAndNoSpentSource()
        {var graph=new SavedGraph();Assert.True(graph.Verify());}
        [TestCase("same-player")][TestCase("same-zone")][TestCase("wrong-player-id")][TestCase("wrong-zone-id")]
        [TestCase("wrong-backlink")][TestCase("equipped-yield")][TestCase("wrong-quantity")][TestCase("resurrected-source")]
        [TestCase("resurrected-corpse")][TestCase("renamed-corpse")][TestCase("post-save-loose-yield")]
        public void InventoryCountDoesNotSubstituteForReplacementAndDepletedSavedGraph(string mutation)
        {
            var g=new SavedGraph();
            if(mutation=="wrong-player-id")g.Actor.ID="another";
            if(mutation=="wrong-zone-id")g.Restored.ZoneID="another";
            if(mutation=="wrong-backlink")g.Yield.GetPart<PhysicsPart>().InInventory=g.Old;
            if(mutation=="equipped-yield")g.Yield.GetPart<PhysicsPart>().Equipped=g.Actor;
            if(mutation=="wrong-quantity")g.Yield.GetPart<StackerPart>().StackCount=1;
            if(mutation=="resurrected-source")g.Other.AddEntity(new Entity{ID="target"},5,5);
            if(mutation=="resurrected-corpse")g.Other.AddEntity(new Entity{ID="corpse"},5,5);
            if(mutation=="renamed-corpse"){var e=new Entity{ID="renamed"};e.Properties["SourceID"]="target";g.Other.AddEntity(e,5,5);}
            if(mutation=="post-save-loose-yield")g.Other.AddEntity(new Entity{ID="earned",BlueprintName="RawMeat"},5,5);
            Assert.False(g.Verify(mutation=="same-player"?g.Old:null,mutation=="same-zone"?g.Before:null));
        }
    }
}
