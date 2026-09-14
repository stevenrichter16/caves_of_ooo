using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent native-consumer hypotheses: habitat reservations
    /// must protect ecology without preventing the population that uses it.</summary>
    public class StumpCompositionAdversarialTests
    {
        private EntityFactory factory;
        private NarrativeStatePart oldState;
        private EntityFactory oldHarvest;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Setup()
        {
            factory=GrovelandsCompositionTests.Factory(); oldState=NarrativeStatePart.Current;
            NarrativeStatePart.Current=null; oldHarvest=HarvestablePart.Factory; HarvestablePart.Factory=factory;
        }
        [TearDown] public void Cleanup()
        { NarrativeStatePart.Current=oldState; HarvestablePart.Factory=oldHarvest; }

        [TestCase(null)] [TestCase("")] [TestCase("garbage")]
        [TestCase("Overworld.2.2.1")] [TestCase("Overworld.20.2.0")]
        public void Adversarial_InvalidAddressesFailBeforePlanning(string id)
            => Assert.Throws<ArgumentException>(()=>StumpCompositionPlan.Create(id,64));

        [TestCase("Overworld.3.3.0")] [TestCase("Overworld.3.5.0")]
        [TestCase("Overworld.2.4.0")] [TestCase("Overworld.3.7.0")]
        [TestCase("Overworld.8.4.0")] [TestCase("Overworld.2.2.1")]
        public void Adversarial_SpecialSitesAndForeignBiomesAreNotWilderness(string id)
            => Assert.IsFalse(StumpCompositionPlan.IsWildernessZone(id));

        [Test] public void Adversarial_EntireMapHasExactlyEighteenOrdinaryAddresses()
        {
            int total=0,feet=0,slopes=0,summit=0;
            for(int y=0;y<20;y++)for(int x=0;x<20;x++)
            {
                string id=WorldMap.ToZoneID(x,y);
                bool expected=StumpBands.BandAt(x,y)!=StumpBand.None
                    && !WorldMapAuthoring.PlaceAt(x,y).HasValue && !SinkholeSites.IsMouth(x,y)
                    && id!="Overworld.3.3.0" && id!="Overworld.3.5.0" && id!="Overworld.3.7.0";
                Assert.AreEqual(expected,StumpCompositionPlan.IsWildernessZone(id),id);
                if(!expected)continue; total++;
                switch(StumpBands.BandAt(x,y)){case StumpBand.Foothills:feet++;break;case StumpBand.Slopes:slopes++;break;case StumpBand.Summit:summit++;break;}
            }
            Assert.AreEqual(18,total); Assert.AreEqual(11,feet); Assert.AreEqual(3,slopes); Assert.AreEqual(4,summit);
        }

        [TestCase(StumpBand.Foothills,Formation.Grainfield)]
        [TestCase(StumpBand.Slopes,Formation.CascadeGorge)]
        [TestCase(StumpBand.Summit,Formation.ButtressRidge)]
        [TestCase(StumpBand.Summit,Formation.SaltPan)]
        public void Adversarial_OverridesCannotSmuggleForeignBandContent(StumpBand band,Formation form)
            => Assert.Throws<ArgumentException>(()=>StumpCompositionPlan.Create(Address(band),64,form));

        [TestCase(Formation.CascadeGorge)] [TestCase(Formation.Grainfield)]
        [TestCase(Formation.ButtressRidge)] [TestCase(Formation.SummitScrub)] [TestCase(Formation.RimForest)]
        public void Adversarial_EveryOpenCellAndDryPortalSurvivesNativeRealization(Formation form)
        {
            string id=Address(BandFor(form));
            for(int seed=0;seed<20;seed++)
            {
                var b=new StumpCompositionBuilder(seed){FormationOverride=form};var z=new Zone(id);
                Assert.IsTrue(b.BuildZone(z,factory,new Random(seed)));
                var reached=ConnectivityBuilder.FloodFill(z,0,b.Plan.WestY);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    var c=z.GetCell(x,y);
                    if(!c.BlocksMovement())Assert.IsTrue(reached[x,y],form+" seed "+seed+" pocket "+x+","+y);
                    if(!b.Plan.IsApproach(x,y))continue;
                    Assert.IsFalse(c.BlocksMovement()); Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
                    Assert.IsFalse(c.Objects.Any(e=>e.BlueprintName=="SprayPool"||e.HasPart<LiquidPoolPart>()));
                }
                foreach(var entry in new[]{(0,b.Plan.WestY),(79,b.Plan.EastY),(b.Plan.NorthX,0),(b.Plan.SouthX,24)})
                    Assert.IsTrue(reached[entry.Item1,entry.Item2]);
            }
        }

        [TestCase(Formation.SummitScrub,"SummitSinger")]
        [TestCase(Formation.SummitScrub,"BrocchiniaSentinel")]
        [TestCase(Formation.RimForest,"SummitSinger")]
        [TestCase(Formation.RimForest,"BrocchiniaSentinel")]
        public void Adversarial_BothEndemicsHaveProtectedWalkableHabitat(Formation form,string species)
        {
            for(int seed=0;seed<16;seed++)
            {
                var b=new StumpCompositionBuilder(seed){FormationOverride=form};var z=new Zone(Address(StumpBand.Summit));
                Assert.IsTrue(b.BuildZone(z,factory,new Random(1)));int candidates=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                    if(b.Plan.IsHabitat(x,y)&&!z.GetCell(x,y).BlocksMovement()&&StumpFaunaHabitat.Allows(species,z.GetCell(x,y)))
                    {candidates++;Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
                Assert.Greater(candidates,0,form+" "+species+" seed "+seed);
            }
        }

        // Exercise the actual manager's reservation window and real PopulationBuilder.
        // Force count, not habitat or state predicates; those remain production code.
        [TestCase("SummitSinger")] [TestCase("BrocchiniaSentinel")]
        [TestCase("CascadeFather")]
        public void Adversarial_RealPopulationWindowUsesHabitatThenRestoresProtection(string species)
        {
            string id=Address(species=="CascadeFather"?StumpBand.Foothills:StumpBand.Summit);
            var manager=new OverworldZoneManager(factory,64);var pipeline=Pipeline(manager,id);
            var pop=pipeline.Builders.OfType<StumpHabitatPopulationBuilder>().Single().Population;
            Assert.NotNull(pop.HabitatFilter);
            pop.Table=One(species);
            var z=new Zone(id);Assert.IsTrue(pipeline.Generate(z,factory,new Random(1)));
            var animal=z.GetAllEntities().Single(e=>e.BlueprintName==species);var cell=z.GetEntityCell(animal);
            Assert.IsTrue(StumpFaunaHabitat.Allows(species,cell));
            var plan=pipeline.Builders.OfType<StumpCompositionBuilder>().Single().Plan;
            Assert.IsTrue(plan.IsHabitat(cell.X,cell.Y));Assert.IsTrue(z.GenReservedCells.Contains((cell.X,cell.Y)));
            // With no reservation window the identical source habitat cannot spawn.
            var b=new StumpCompositionBuilder(64);var control=new Zone(id);Assert.IsTrue(b.BuildZone(control,factory,new Random(1)));
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)control.GenReservedCells.Add((x,y));
            new PopulationBuilder(One(species)){HabitatFilter=StumpFaunaHabitat.Allows}.BuildZone(control,factory,new Random(1));
            Assert.IsFalse(control.GetAllEntities().Any(e=>e.BlueprintName==species));
        }

        [TestCase("SkySari","UrquActive",true,StumpBand.Summit)]
        [TestCase("SariSnake","UrquActive",true,StumpBand.Slopes)]
        [TestCase("CascadeFather","EcologyDamaged",false,StumpBand.Foothills)]
        public void Adversarial_ReservationWindowDoesNotBypassNativeWorldFlags(string species,string flag,bool requires,StumpBand band)
        {
            string id=Address(band);var state=new NarrativeStatePart();NarrativeStatePart.Current=state;
            foreach(bool enabled in new[]{false,true})
            {
                state.SetFact(flag,enabled?1:0);var manager=new OverworldZoneManager(factory,64);var pipeline=Pipeline(manager,id);
                var row=PopulationTable.GetStumpTable(band,3).Entries.Single(e=>e.BlueprintName==species);
                Assert.AreEqual(flag,requires?row.RequiresWorldFlag:row.ForbidsWorldFlag);
                var table=One(species);table.Entries[0].RequiresWorldFlag=row.RequiresWorldFlag;table.Entries[0].ForbidsWorldFlag=row.ForbidsWorldFlag;
                pipeline.Builders.OfType<StumpHabitatPopulationBuilder>().Single().Population.Table=table;
                var z=new Zone(id);Assert.IsTrue(pipeline.Generate(z,factory,new Random(1)));
                Assert.AreEqual(requires?enabled:!enabled,z.GetAllEntities().Any(e=>e.BlueprintName==species));
            }
        }

        [Test] public void Adversarial_PopulationFailureRestoresEveryOpenedHabitatReservation()
        {
            var terrain=new StumpCompositionBuilder(64);
            var zone=new Zone(Address(StumpBand.Foothills));
            Assert.IsTrue(terrain.BuildZone(zone,factory,new Random(1)));
            var before=zone.GenReservedCells.ToArray();
            var wrapper=new StumpHabitatPopulationBuilder(terrain,One("CascadeFather"));
            int inspected=0;
            wrapper.Population.HabitatFilter=(bp,cell)=>
            {inspected++;throw new InvalidOperationException("Injected habitat consumer failure");};
            Assert.Throws<InvalidOperationException>(()=>wrapper.BuildZone(zone,factory,new Random(1)));
            Assert.Greater(inspected,0,"Failure must occur inside the real population consumer.");
            CollectionAssert.AreEquivalent(before,zone.GenReservedCells);
            Assert.IsFalse(zone.GetAllEntities().Any(e=>e.BlueprintName=="CascadeFather"));
            wrapper.Population.HabitatFilter=StumpFaunaHabitat.Allows;
            Assert.IsTrue(wrapper.BuildZone(zone,factory,new Random(1)));
            Assert.IsTrue(zone.GetAllEntities().Any(e=>e.BlueprintName=="CascadeFather"));
            CollectionAssert.AreEquivalent(before,zone.GenReservedCells);
        }

        [TestCase("TepuiStone",Formation.CascadeGorge)] [TestCase("SprayPool",Formation.CascadeGorge)]
        [TestCase("TepuiboneVein",Formation.Grainfield)] [TestCase("TankBrocchinia",Formation.SummitScrub)]
        public void Adversarial_MissingRequiredContentRejectsWithoutPartialState(string missing,Formation form)
        {
            Assert.IsTrue(factory.Blueprints.Remove(missing));var z=new Zone(Address(BandFor(form)));
            var b=new StumpCompositionBuilder(64){FormationOverride=form};Assert.IsFalse(b.BuildZone(z,factory,new Random(1)));
            Assert.IsNull(b.Plan);Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
            factory=GrovelandsCompositionTests.Factory();Assert.IsTrue(b.BuildZone(z,factory,new Random(1)));
        }

        [Test] public void Adversarial_RejectedRebuildKeepsPlayerRemovalAndClearsPlan()
        {
            var z=new Zone(Address(StumpBand.Slopes));var b=new StumpCompositionBuilder(64){FormationOverride=Formation.Grainfield};
            Assert.IsTrue(b.BuildZone(z,factory,new Random(1)));var vein=z.GetAllEntities().First(e=>e.BlueprintName=="TepuiboneVein");
            z.RemoveEntity(vein);var before=z.GetAllEntities().ToArray();Assert.IsFalse(b.BuildZone(z,factory,new Random(2)));
            Assert.IsNull(b.Plan);CollectionAssert.AreEquivalent(before,z.GetAllEntities());Assert.IsNull(z.GetEntityCell(vein));
        }

        [TestCase(0)] [TestCase(24)]
        public void Adversarial_ComposedVeinHarvestPreservesHeavyYieldAndNeighbor(int capacity)
        {
            var z=new Zone(Address(StumpBand.Slopes));Assert.IsTrue(new StumpCompositionBuilder(64){FormationOverride=Formation.Grainfield}.BuildZone(z,factory,new Random(1)));
            var veins=z.GetAllEntities().Where(e=>e.BlueprintName=="TepuiboneVein").Take(2).ToArray();Assert.AreEqual(2,veins.Length);
            var actor=new Entity();var pack=new InventoryPart{MaxWeight=capacity};actor.AddPart(pack);z.AddEntity(actor,0,0);
            var ev=GameEvent.New("InventoryAction");ev.SetParameter("Command","Harvest");ev.SetParameter("Actor",actor);ev.SetParameter("Zone",z);ev.SetParameter("Random",new Random(1));veins[0].FireEventAndRelease(ev);
            Assert.IsNull(z.GetEntityCell(veins[0]));Assert.NotNull(z.GetEntityCell(veins[1]));
            var goods=pack.Objects.Concat(z.GetAllEntities()).Where(e=>e.BlueprintName=="Tepuibone").ToArray();
            Assert.That(goods.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1),Is.InRange(1,2));
            foreach(var item in goods)Assert.AreEqual(12,item.GetPart<PhysicsPart>().Weight);
            if(capacity==0)Assert.AreEqual(0,pack.Objects.Count);else Assert.Greater(pack.Objects.Count,0);
        }

        [TestCase("SprayPool")] [TestCase("TankBrocchinia")] [TestCase("GrainRidge")]
        [TestCase("StoneDome")] [TestCase("TepuiboneVein")]
        public void Adversarial_RecipesRetainNativeMembershipAndRemoval(string blueprint)
        {
            var z=new Zone(Address(StumpBand.Summit));var e=factory.CreateEntity(blueprint);z.AddEntity(e,10,10);
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);Assert.NotNull(library);
            var recipe=SpawnRing3DRecipes.Resolve(z,e,library.Definition);Assert.IsNotEmpty(recipe.ModelId,recipe.Failure);Assert.AreSame(e,recipe.Owner);
            e.GetPart<RenderPart>().Visible=false;Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,library.Definition).ModelId);
            e.GetPart<RenderPart>().Visible=true;z.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,library.Definition).ModelId);
        }

        private sealed class LastEligibleRandom : Random
        { public override int Next(int maxValue)=>0; }

        [Test] public void Adversarial_LaterNativeStairNeverReopensAsFaunaHabitat()
        {
            var terrain=new StumpCompositionBuilder(64);var z=new Zone(Address(StumpBand.Foothills));
            Assert.IsTrue(terrain.BuildZone(z,factory,new Random(1)));
            // Population's reservoir picks the final eligible cell with this RNG.
            // Choose that same cell for the later cave mouth so the test cannot
            // pass merely because forty random draws happened to miss it.
            Cell last=null;
            z.ForEachCell((cell,x,y)=>
            {if(terrain.Plan.IsHabitat(x,y)&&!cell.BlocksMovement()&&StumpFaunaHabitat.Allows("CascadeFather",cell))last=cell;});
            Assert.NotNull(last);var stairs=factory.CreateEntity("StairsDown");Assert.NotNull(stairs);
            Assert.IsTrue(z.AddEntity(stairs,last.X,last.Y));Assert.IsFalse(last.BlocksMovement());
            var table=One("CascadeFather");table.Entries[0].MinCount=40;table.Entries[0].MaxCount=40;
            var wrapper=new StumpHabitatPopulationBuilder(terrain,table);
            Assert.IsTrue(wrapper.BuildZone(z,factory,new LastEligibleRandom()));
            Assert.IsFalse(last.Objects.Any(e=>e.BlueprintName=="CascadeFather"),"Native stairs must remain reserved even while their SprayPool remains valid habitat.");
            Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="CascadeFather"),"Ordinary spray habitat must still admit the same table.");
            Assert.IsTrue(z.GenReservedCells.Contains((last.X,last.Y)));
        }

        [Test] public void Adversarial_HabitatWindowRejectsDifferentNativeZoneWithSameAddress()
        {
            string id=Address(StumpBand.Foothills);var terrain=new StumpCompositionBuilder(64);
            var original=new Zone(id);Assert.IsTrue(terrain.BuildZone(original,factory,new Random(1)));
            var other=new Zone(id);Assert.IsTrue(new StumpCompositionBuilder(64).BuildZone(other,factory,new Random(1)));
            var wrapper=new StumpHabitatPopulationBuilder(terrain,One("CascadeFather"));
            var before=other.GenReservedCells.ToArray();int count=other.EntityCount;
            Assert.IsFalse(wrapper.BuildZone(other,factory,new Random(1)),"An address is not ownership of another native Zone graph.");
            Assert.AreEqual(count,other.EntityCount);CollectionAssert.AreEquivalent(before,other.GenReservedCells);
            Assert.IsTrue(wrapper.BuildZone(original,factory,new Random(1)));
            Assert.IsTrue(original.GetAllEntities().Any(e=>e.BlueprintName=="CascadeFather"));
        }

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void Adversarial_AllEighteenFinalNativeZonesKeepPortalsResourcesAndBothHabitats(int seed)
        {
            var manager=new OverworldZoneManager(factory,seed);int checkedZones=0,campOccupants=0;
            for(int wy=0;wy<20;wy++)for(int wx=0;wx<20;wx++)
            {
                string id=WorldMap.ToZoneID(wx,wy);if(!StumpCompositionPlan.IsWildernessZone(id))continue;
                Assert.IsNull(manager.WorldMap.GetPOI(wx,wy),id);
                var plan=StumpCompositionPlan.Create(id,seed);var zone=manager.GetZone(id);Assert.NotNull(zone);checkedZones++;
                campOccupants+=zone.GetAllEntities().Count(e=>e.BlueprintName=="CaveHermit"||e.BlueprintName=="SnapjawWarlord");
                var reached=ConnectivityBuilder.FloodFill(zone,0,plan.WestY);
                foreach(var p in new[]{(0,plan.WestY),(79,plan.EastY),(plan.NorthX,0),(plan.SouthX,24),(plan.FocalX,plan.FocalY)})
                {
                    var cell=zone.GetCell(p.Item1,p.Item2);
                    Assert.IsFalse(cell.BlocksMovement(),id+" seed "+seed+" obstructed "+p+" objects: "+string.Join(",",cell.Objects.Select(e=>e.BlueprintName+"[physicsSolid="+(e.GetPart<PhysicsPart>()?.Solid==true)+",solidTag="+e.HasTag("Solid")+"]")));
                    Assert.IsTrue(reached[p.Item1,p.Item2],id+" seed "+seed+" disconnected "+p);
                }
                var veins=zone.GetAllEntities().Where(e=>e.BlueprintName=="TepuiboneVein").ToArray();
                if(plan.Band==StumpBand.Slopes)
                {
                    int expected=0;for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(plan.ObjectAt(x,y)=="TepuiboneVein")expected++;
                    Assert.AreEqual(12,expected,id+" promised three four-cell seams after plan repair");
                    Assert.AreEqual(expected,veins.Length,id+" later stages changed seam supply");
                    foreach(var vein in veins)
                    {
                        var c=zone.GetEntityCell(vein);bool accessible=false;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                            if(zone.InBounds(c.X+dx,c.Y+dy)&&reached[c.X+dx,c.Y+dy])accessible=true;
                        Assert.IsTrue(accessible,id+" inaccessible resource "+c.X+","+c.Y);
                    }
                }
                else Assert.AreEqual(0,veins.Length,id+" off-slope mineral leak");
                if(plan.Band==StumpBand.Summit)
                    foreach(string species in new[]{"SummitSinger","BrocchiniaSentinel"})
                    {
                        int habitat=0;
                        for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                            if(plan.IsHabitat(x,y)&&StumpFaunaHabitat.Allows(species,zone.GetCell(x,y)))
                            {habitat++;Assert.IsTrue(zone.GenReservedCells.Contains((x,y)),id+" habitat reservation leaked");}
                        Assert.Greater(habitat,0,id+" lost "+species+" habitat");
                    }
                // The checks above exercise the actual populated zone. This
                // second audit isolates environmental topology: only actors
                // leave, while walls, resources, containers and haulables stay.
                // It does not excuse an actor blocking an arrival or resource.
                var actors=zone.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray();
                foreach(var actor in actors)zone.RemoveEntity(actor);
                var terrainReached=ConnectivityBuilder.FloodFill(zone,0,plan.WestY);
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    var cell=zone.GetCell(x,y);
                    if(cell.BlocksMovement())continue;
                    if(!terrainReached[x,y])Assert.Fail(id+" seed "+seed+" stranded environmental cell "+x+","+y
                        +" objects: "+string.Join(",",cell.Objects.Select(e=>e.BlueprintName))
                        +" neighbors: "+string.Join(";",new[]{(x-1,y),(x+1,y),(x,y-1),(x,y+1)}
                            .Where(p=>zone.InBounds(p.Item1,p.Item2))
                            .Select(p=>p+"="+string.Join(",",zone.GetCell(p.Item1,p.Item2).Objects.Select(e=>e.BlueprintName))))
                        +TopologyDiagnostic(zone,terrainReached,x,y));
                }
            }
            Assert.AreEqual(18,checkedZones);
            Assert.Greater(campOccupants,0,"Entrance clearance must not eliminate all optional inhabited landmarks.");
        }

        [Test] public void Adversarial_AllSummitAddressesKeepBothSpeciesHabitatAcrossThirtyTwoSeeds()
        {
            int addresses=0;
            for(int wy=0;wy<20;wy++)for(int wx=0;wx<20;wx++)
            {
                string id=WorldMap.ToZoneID(wx,wy);
                if(!StumpCompositionPlan.IsWildernessZone(id)||StumpBands.BandAt(wx,wy)!=StumpBand.Summit)continue;
                addresses++;
                for(int seed=0;seed<32;seed++)
                {
                    var b=new StumpCompositionBuilder(seed);var z=new Zone(id);Assert.IsTrue(b.BuildZone(z,factory,new Random(1)));
                    foreach(string species in new[]{"SummitSinger","BrocchiniaSentinel"})
                    {
                        int candidates=0;
                        for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                            if(b.Plan.IsHabitat(x,y)&&!z.GetCell(x,y).BlocksMovement()&&StumpFaunaHabitat.Allows(species,z.GetCell(x,y)))candidates++;
                        Assert.Greater(candidates,0,id+" seed "+seed+" no "+species+" habitat");
                    }
                }
            }
            Assert.AreEqual(4,addresses);
        }

        private static string TopologyDiagnostic(Zone zone,bool[,] reached,int sx,int sy)
        {
            var text=new System.Text.StringBuilder("\nTopology: .=reachable ?=stranded #=solid; row labels are Y\n");
            for(int y=0;y<Zone.Height;y++)
            {
                text.Append(y.ToString("D2")).Append(' ');
                for(int x=0;x<Zone.Width;x++)text.Append(zone.GetCell(x,y).BlocksMovement()?'#':reached[x,y]?'.':'?');
                text.AppendLine();
            }
            var component=ConnectivityBuilder.FloodFill(zone,sx,sy);
            var boundary=new HashSet<(int x,int y)>();
            text.AppendLine("Stranded component contents:");
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(component[x,y])
            {
                text.Append(x).Append(',').Append(y).Append(':').AppendLine(string.Join(",",zone.GetCell(x,y).Objects.Select(e=>e.BlueprintName)));
                foreach(var p in new[]{(x-1,y),(x+1,y),(x,y-1),(x,y+1)})
                    if(zone.InBounds(p.Item1,p.Item2)&&!component[p.Item1,p.Item2])boundary.Add(p);
            }
            text.AppendLine("Complete blocking frontier:");
            foreach(var p in boundary.OrderBy(p=>p.y).ThenBy(p=>p.x))
                text.Append(p.x).Append(',').Append(p.y).Append(':').AppendLine(string.Join(",",zone.GetCell(p.x,p.y).Objects
                    .Select(e=>e.BlueprintName+"[physicsSolid="+(e.GetPart<PhysicsPart>()?.Solid==true)+",solidTag="+e.HasTag("Solid")+"]")));
            return text.ToString();
        }

        private static PopulationTable One(string species)
        {var t=new PopulationTable{Name="Review forced native row"};t.Entries.Add(new PopulationEntry{BlueprintName=species,MinCount=1,MaxCount=1});return t;}
        private static ZoneGenerationPipeline Pipeline(OverworldZoneManager manager,string id)
        {var m=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",Private);Assert.NotNull(m);return (ZoneGenerationPipeline)m.Invoke(manager,new object[]{id});}
        private static StumpBand BandFor(Formation form)=>form==Formation.CascadeGorge?StumpBand.Foothills:form==Formation.Grainfield||form==Formation.ButtressRidge?StumpBand.Slopes:StumpBand.Summit;
        private static string Address(StumpBand band)
        {
            for(int y=0;y<20;y++)for(int x=0;x<20;x++)
            {string id=WorldMap.ToZoneID(x,y);if(StumpBands.BandAt(x,y)==band&&StumpCompositionPlan.IsWildernessZone(id))return id;}
            throw new InvalidOperationException("No eligible Stump address for "+band);
        }
    }
}
