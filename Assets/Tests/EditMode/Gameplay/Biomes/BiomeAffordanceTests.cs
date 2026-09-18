using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-facing contracts: ripe grain can be gathered once, while
    /// cut rows remain visible; the summit's described water has real uses.</summary>
    public sealed class BiomeAffordanceTests
    {
        private EntityFactory factory, oldHarvestFactory;
        [SetUp] public void Setup()
        {
            factory=GrovelandsCompositionTests.Factory();
            oldHarvestFactory=HarvestablePart.Factory; HarvestablePart.Factory=factory;
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions/water.json")));
            MessageLog.Clear();
        }
        [TearDown] public void Cleanup()
        { HarvestablePart.Factory=oldHarvestFactory; LiquidRegistry.ResetForTests(); }

        internal static Entity Actor(Zone zone,int x=10,int y=10,int capacity=500)
        {
            var e=new Entity { ID="affordance-player-"+x+"-"+y };
            e.AddPart(new RenderPart { DisplayName="traveller" });
            e.AddPart(new PhysicsPart { Solid=true, Weight=60 });
            e.AddPart(new InventoryPart { MaxWeight=capacity });
            e.AddPart(new StatusEffectsPart()); e.Tags["Player"]=""; e.Tags["Creature"]="";
            foreach(string stat in new[]{"Strength","Agility","Toughness"})
                e.Statistics[stat]=new Stat { Owner=e,Name=stat,BaseValue=16 };
            e.Statistics["Hitpoints"]=new Stat { Owner=e,Name="Hitpoints",BaseValue=40,Max=40 };
            Assert.IsTrue(zone.AddEntity(e,x,y)); return e;
        }
        internal static bool Act(Entity target,Entity actor,Zone zone,string command="Harvest")
        {
            var e=GameEvent.New("InventoryAction"); e.SetParameter("Command",command);
            if(actor!=null)e.SetParameter("Actor",(object)actor);
            if(zone!=null)e.SetParameter("Zone",(object)zone);
            try { target.FireEvent(e); return e.Handled; } finally { e.Release(); }
        }
        internal static int Grain(Entity actor) => actor.GetPart<InventoryPart>().Objects
            .Where(e=>e.BlueprintName=="Emberwheat").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private Entity Row(Zone zone,string bp="RipeCropRow",int x=11,int y=10)
        { var e=factory.CreateEntity(bp);Assert.NotNull(e,bp+" is authored content"); Assert.IsTrue(zone.AddEntity(e,x,y));return e; }

        [Test] public void RipeRowOffersHarvestButDecorativeCutRowDoesNot()
        {
            var z=new Zone(SpreadCompositionTests.Id);var actor=Actor(z);
            var ripe=Row(z);var spent=Row(z,"CropRow",12,10);
            Assert.NotNull(ripe.GetPart<FieldHarvestPart>());
            Assert.IsNull(ripe.GetPart<CropPart>(),"Standing ripe grain must not also mature into duplicate produce.");
            Assert.IsTrue(WorldInteractionSystem.GatherActions(ripe,actor).Any(a=>a.Command=="Harvest"));
            Assert.IsFalse(WorldInteractionSystem.GatherActions(spent,actor).Any(a=>a.Command=="Harvest"));
            StringAssert.Contains("emberwheat",ripe.GetDisplayName().ToLowerInvariant());
            StringAssert.Contains("cut",spent.GetPart<ExaminablePart>().Text.ToLowerInvariant());
        }
        [Test] public void HarvestProducesOneUsableSheafAndLeavesTheSameSpentOwner()
        {
            var z=new Zone(SpreadCompositionTests.Id);var actor=Actor(z);var row=Row(z);
            string id=row.ID;var cell=z.GetEntityCell(row);
            Assert.IsTrue(Act(row,actor,z));Assert.AreEqual(1,Grain(actor));
            Assert.IsTrue(row.GetPart<FieldHarvestPart>().Harvested);
            Assert.AreSame(cell,z.GetEntityCell(row));Assert.AreEqual(id,row.ID);
            Assert.AreEqual(1,cell.Objects.Count(e=>e==row));
            Assert.IsFalse(WorldInteractionSystem.GatherActions(row,actor).Any(a=>a.Command=="Harvest"));
            StringAssert.Contains("cut",row.GetPart<ExaminablePart>().Text.ToLowerInvariant());
            Assert.NotNull(actor.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Emberwheat").GetPart<FoodPart>());
            Act(row,actor,z);Assert.AreEqual(1,Grain(actor),"A stale action may not mint another sheaf.");
        }
        [Test] public void FullPackLeavesTheOneHarvestOnTheActualRowCell()
        {
            var z=new Zone(SpreadCompositionTests.Id);var actor=Actor(z,capacity:0);var row=Row(z);
            Assert.IsTrue(Act(row,actor,z));Assert.AreEqual(0,Grain(actor));
            Assert.AreEqual(1,z.GetEntityCell(row).Objects.Count(e=>e.BlueprintName=="Emberwheat"));
            Assert.IsTrue(row.GetPart<FieldHarvestPart>().Harvested);
            Act(row,actor,z);Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="Emberwheat"));
        }
        [TestCase(0)] [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FieldStripsHaveBoundedDeterministicRipeGleaningsAmongSpentRows(int seed)
        {
            var a=new Zone(SpreadCompositionTests.Id);var b=new Zone(SpreadCompositionTests.Id);
            var first=new SpreadCompositionBuilder(seed){FormationOverride=Formation.FieldStrips};
            var second=new SpreadCompositionBuilder(seed){FormationOverride=Formation.FieldStrips};
            Assert.IsTrue(first.BuildZone(a,factory,new Random(1)));Assert.IsTrue(second.BuildZone(b,factory,new Random(99)));
            var ripe=a.GetAllEntities().Where(e=>e.BlueprintName=="RipeCropRow").ToArray();
            int expected=first.Plan.Condition=="tended"?3:first.Plan.Condition=="returning scrub"?2:1;
            Assert.AreEqual(expected,ripe.Length);
            Assert.Greater(a.GetAllEntities().Count(e=>e.BlueprintName=="CropRow"),ripe.Length*5);
            var cells=ripe.Select(e=>a.GetEntityPosition(e)).OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();
            CollectionAssert.AreEqual(cells,b.GetAllEntities().Where(e=>e.BlueprintName=="RipeCropRow")
                .Select(e=>b.GetEntityPosition(e)).OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray());
            foreach(var e in ripe)
            {
                var p=a.GetEntityPosition(e);Assert.AreEqual("CropRow",first.Plan.ObjectAt(p.x,p.y));
                Assert.IsFalse(first.Plan.IsApproach(p.x,p.y));Assert.IsFalse(a.GetCell(p.x,p.y).BlocksMovement());
                Assert.IsFalse(e.GetPart<FieldHarvestPart>().Harvested);
            }
        }
        [TestCase(Formation.Hedgerow)] [TestCase(Formation.FlowerMeadow)] [TestCase(Formation.Fallow)]
        public void OtherSpreadFormationsDoNotReceiveInventedGrain(Formation form)
        {
            var z=new Zone(SpreadCompositionTests.Id);
            Assert.IsTrue(new SpreadCompositionBuilder(64){FormationOverride=form}.BuildZone(z,factory,new Random(1)));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<FieldHarvestPart>()));
        }
        [TestCase(false)] [TestCase(true)] public void ReadyAndSpentRowsRoundTripWithoutRenewingTheirYield(bool spent)
        {
            var z=new Zone(SpreadCompositionTests.Id);var actor=Actor(z);var row=Row(z);
            if(spent)Assert.IsTrue(Act(row,actor,z));
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(row);
            Assert.AreEqual(spent,loaded.GetPart<FieldHarvestPart>().Harvested);
            Assert.AreEqual("Emberwheat",loaded.GetPart<FieldHarvestPart>().YieldBlueprint);
            Assert.AreEqual(1,loaded.GetPart<FieldHarvestPart>().YieldCount);
            Assert.AreEqual(!spent,WorldInteractionSystem.GatherActions(loaded,actor).Any(a=>a.Command=="Harvest"));
            Assert.AreEqual(row.GetPart<RenderPart>().DisplayName,loaded.GetPart<RenderPart>().DisplayName);
            Assert.AreEqual(row.GetPart<ExaminablePart>().Text,loaded.GetPart<ExaminablePart>().Text);
        }
        [Test] public void BromeliadOffersExistingWaterActionAndCuresParchedWithoutSoakingItsCell()
        {
            var z=new Zone("Overworld.3.2.0");var actor=Actor(z);var tank=Row(z,"TankBrocchinia");
            actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new ParchedEffect());
            Assert.IsTrue(actor.HasEffect<ParchedEffect>());
            Assert.IsTrue(WorldInteractionSystem.GatherActions(tank,actor).Any(a=>a.Command=="DrawWaterAtWell"));
            Assert.IsTrue(Act(tank,actor,z,"DrawWaterAtWell"));Assert.IsFalse(actor.HasEffect<ParchedEffect>());
            Assert.AreEqual(16,actor.GetStatValue("Agility"));Assert.AreEqual(16,actor.GetStatValue("Strength"));
            Assert.IsNull(tank.GetPart<LiquidPoolPart>());Assert.IsFalse(z.TileState.HasCoating(11,10,"water"));
            var stone=Row(z,"TepuiStone",12,10);
            Assert.IsFalse(WorldInteractionSystem.GatherActions(stone,actor).Any(a=>a.Command=="DrawWaterAtWell"));
        }
        [TestCase("SprayPool",true)] [TestCase("TepuiStone",false)] [TestCase("TankBrocchinia",false)]
        public void SteppingIntoActualSprayWaterCoatsTheTravellerButDryStoneAndTankDoNot(string bp,bool wet)
        {
            var z=new Zone("Overworld.2.1.0");var actor=Actor(z);var source=Row(z,bp);
            Assert.AreEqual(wet,z.TileState.HasCoating(11,10,"water"));
            Assert.IsTrue(MovementSystem.TryMove(actor,z,1,0));
            var coat=actor.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            if(wet){Assert.NotNull(coat);Assert.AreEqual("water",coat.LiquidId);Assert.Greater(coat.Amount,0);}
            else Assert.IsNull(coat);
            Assert.AreSame(z.GetCell(11,10),z.GetEntityCell(source));
        }
        [Test] public void RemovingLastSprayPoolRemovesItsProjectionButPreservesAnotherOwnersWater()
        {
            var z=new Zone("Overworld.2.1.0");var a=Row(z,"SprayPool");var b=Row(z,"SprayPool");
            Assert.AreEqual(ZoneTileState.Permanent,z.TileState.CoatingTurns(11,10,"water"));
            z.RemoveEntity(a);Assert.IsTrue(z.TileState.HasCoating(11,10,"water"));
            z.RemoveEntity(b);Assert.IsFalse(z.TileState.HasCoating(11,10,"water"));
        }
        [TestCase("TankBrocchinia")] [TestCase("SprayPool")]
        public void NewWaterAffordancesUseExistingSavedParts(string bp)
        {
            var e=factory.CreateEntity(bp);var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(e);
            if(bp=="TankBrocchinia")Assert.NotNull(loaded.GetPart<WellPart>());
            else{Assert.NotNull(loaded.GetPart<LiquidPoolPart>());Assert.AreEqual("water",loaded.GetPart<LiquidPoolPart>().LiquidId);Assert.AreEqual(40,loaded.GetPart<LiquidPoolPart>().Volume);}
        }
    }
}
