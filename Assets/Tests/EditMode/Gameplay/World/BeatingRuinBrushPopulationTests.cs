using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Fresh real passable ruin cover; no new loot, routes or encounter rolls.
    public sealed class BeatingRuinBrushPopulationTests
    {
        const string Id="Overworld.19.12.0";
        DensityLootTestScope content;
        [SetUp] public void Setup()=>content=new DensityLootTestScope();
        [TearDown] public void Cleanup()=>content.Dispose();

        // Frozen before runtime edits; 2000 lines x,y:ground:object:approach:road, each ending LF.
        [TestCase(64,"2d55795a166f7ee1440dac49e866af29da854c9a39be14e7c244cfd501097301")]
        [TestCase(65,"91cea2bbb1f1bb4782638045455800d4625d07704cff2c33e21182dd41395458")]
        public void RuinCoverCannotChangePreexistingStructureGroundPavingOrApproaches(int seed,string expected)
        {
            var plan=Plan(seed);var text=new StringBuilder();
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                string body=plan.ObjectAt(x,y);if(body=="DryBrush")body=null;
                text.Append(x).Append(',').Append(y).Append(':').Append(plan.GroundAt(x,y)).Append(':').Append(body)
                    .Append(':').Append(plan.IsApproach(x,y)).Append(':').Append(plan.IsRoad(x,y)).Append('\n');
            }
            using(var hash=SHA256.Create())Assert.AreEqual(expected,BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","").ToLowerInvariant());
        }
        [TestCase(0)][TestCase(17)][TestCase(64)][TestCase(65)]
        public void RuinsGainUnequalCompactShelteredDriftsWithoutFillingTheApproaches(int seed)
        {
            var plan=Plan(seed);var cells=BrushCells(plan);Assert.That(cells.Count,Is.InRange(12,24));
            var groups=Components(cells);Assert.That(groups.Count,Is.InRange(2,3));
            Assert.Greater(groups.Select(g=>g.Count).Distinct().Count(),1,"Avoid equal repeated stamps");
            foreach(var g in groups)
            {
                Assert.That(g.Count,Is.InRange(3,12));
                Assert.LessOrEqual(g.Max(c=>c.x)-g.Min(c=>c.x)+1,10);
                Assert.LessOrEqual(g.Max(c=>c.y)-g.Min(c=>c.y)+1,8);
            }
            foreach(var c in cells)
            {
                Assert.That(c.x,Is.InRange(2,77));Assert.That(c.y,Is.InRange(2,22));Assert.False(plan.IsRoad(c.x,c.y));
                bool shelter=false;
                for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
                {
                    if(plan.ObjectAt(c.x+dx,c.y+dy)=="SandstoneWall")shelter=true;
                    if(Math.Abs(dx)<=1&&Math.Abs(dy)<=1)Assert.False(plan.IsApproach(c.x+dx,c.y+dy),"Keep an approach margin at "+c);
                }
                Assert.True(shelter,"Each bush must belong to surviving masonry, not an isolated floor patch");
                for(int i=0;i<plan.RuinCount;i++)
                {
                    var r=plan.GetRuin(i);int centre=r.X+r.Width/2;
                    bool entrance=Math.Abs(c.x-centre)<=1&&(Math.Abs(c.y-r.Y)<=1||Math.Abs(c.y-(r.Y+r.Height-1))<=1);
                    Assert.False(entrance,"Keep both existing three-cell gates and their immediate approach clear");
                }
            }
            Assert.AreEqual(plan.Signature(),Plan(seed).Signature());
            var salt=BeatingCompositionPlan.Create(Id,seed,Formation.SaltPan);
            Assert.IsEmpty(BrushCells(salt),"This does not spread ruin brush into open salt pans");
        }
        [TestCase(64)][TestCase(65)]
        public void BuilderProducesRealPassableFuelOwnersAndNoLootOrRouteReservation(int seed)
        {
            var zone=Build(seed,out var builder);var brush=zone.GetAllEntities().Where(e=>e.BlueprintName=="DryBrush").ToArray();
            Assert.That(brush.Length,Is.InRange(12,24));
            CollectionAssert.AreEquivalent(BrushCells(builder.Plan),brush.Select(e=>{var c=zone.GetEntityCell(e);return(c.X,c.Y);}));
            foreach(var e in brush)
            {
                var c=zone.GetEntityCell(e);Assert.False(e.GetPart<PhysicsPart>().Solid);Assert.False(c.BlocksMovement());
                Assert.AreEqual(1,c.Objects.Count(o=>o.BlueprintName=="DryBrush"));Assert.False(zone.GenReservedCells.Contains((c.X,c.Y)));
                Assert.AreEqual("DryPlant",e.GetPart<MaterialPart>().MaterialID);Assert.AreEqual(4,e.GetPart<DestructiblePart>().HP);
                Assert.IsNull(e.GetPart<HarvestablePart>());Assert.IsNull(e.GetPart<InventoryPart>());Assert.IsNull(e.GetPart<BrainPart>());
                Assert.True(string.IsNullOrEmpty(e.GetPart<DestructiblePart>().WreckageBlueprint));
                Assert.AreEqual(1,c.Objects.Count(o=>o.BlueprintName==builder.Plan.GroundAt(c.X,c.Y)));
            }
            Assert.True(FormationReachability.FullyReached(zone,FormationReachability.FloodFromWest(zone)));
        }
        [TestCase(64)][TestCase(65)]
        public void OrdinaryEncounterRollsAndLocationsMatchWithCoverPresentOrRemoved(int seed)
        {
            var withCover=Build(seed,out _);var without=Build(seed,out _);
            var removed=without.GetAllEntities().Where(e=>e.BlueprintName=="DryBrush").ToArray();Assert.IsNotEmpty(removed);
            foreach(var e in removed)Assert.True(without.RemoveEntity(e));
            CollectionAssert.AreEquivalent(withCover.GenReservedCells,without.GenReservedCells);
            var beforeWith=new HashSet<Entity>(withCover.GetAllEntities());var beforeWithout=new HashSet<Entity>(without.GetAllEntities());
            content.Seed(17);Assert.True(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Beating,1)).BuildZone(withCover,content.Factory,new Random(17)));
            content.Seed(17);Assert.True(new PopulationBuilder(PopulationTable.GetBiomeTable(BiomeType.Beating,1)).BuildZone(without,content.Factory,new Random(17)));
            var actual=Added(withCover,beforeWith);Assert.IsNotEmpty(actual);CollectionAssert.AreEqual(Added(without,beforeWithout),actual);
        }
        [TestCase(false)][TestCase(true)]
        public void SavedBareOrPlayerEditedRuinsAreNotRepopulatedOnLoad(bool retainCover)
        {
            var zone=Build(64,out var builder);var brush=zone.GetAllEntities().Where(e=>e.BlueprintName=="DryBrush").ToArray();Assert.IsNotEmpty(brush);
            if(retainCover){zone.RemoveEntity(brush[0]);brush[1].GetPart<DestructiblePart>().HP=1;}
            else foreach(var e in brush)zone.RemoveEntity(e);
            var player=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,0,builder.Plan.WestY));
            var expected=Owners(zone);var manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);manager.SetActiveZone(zone);
            var turns=new TurnManager();turns.AddEntity(player);var session=GameSessionState.Capture("ruin-brush-test","test",manager,turns,player);
            GameSessionState loaded;using(var stream=new MemoryStream())
            {session.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,content.Factory));}
            var restored=loaded.ZoneManager.GetZone(Id);CollectionAssert.AreEqual(expected,Owners(restored));
            Assert.AreSame(restored,loaded.ZoneManager.GetZone(Id));
            Assert.False(new BeatingCompositionBuilder(64){FormationOverride=Formation.RuinField}.BuildZone(restored,content.Factory,new NoDrawRandom()));
            CollectionAssert.AreEqual(expected,Owners(restored));
        }
        [Test] public void MissingBrushRejectsFreshRuinsBeforeMutation()
        {
            Assert.True(content.Factory.Blueprints.Remove("DryBrush"));var zone=new Zone(Id);
            Assert.False(new BeatingCompositionBuilder(64){FormationOverride=Formation.RuinField}.BuildZone(zone,content.Factory,new NoDrawRandom()));
            Assert.AreEqual(0,zone.EntityCount);Assert.IsEmpty(zone.GenReservedCells);
        }
        static BeatingCompositionPlan Plan(int seed)=>BeatingCompositionPlan.Create(Id,seed,Formation.RuinField);
        Zone Build(int seed,out BeatingCompositionBuilder builder)
        {
            var zone=new Zone(Id);builder=new BeatingCompositionBuilder(seed){FormationOverride=Formation.RuinField};
            Assert.True(builder.BuildZone(zone,content.Factory,new NoDrawRandom()));return zone;
        }
        static List<(int x,int y)> BrushCells(BeatingCompositionPlan p)
        {
            var result=new List<(int,int)>();for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(p.ObjectAt(x,y)=="DryBrush")result.Add((x,y));return result;
        }
        static List<List<(int x,int y)>> Components(IEnumerable<(int x,int y)> cells)
        {
            var left=new HashSet<(int x,int y)>(cells);var result=new List<List<(int x,int y)>>();var queue=new Queue<(int x,int y)>();
            while(left.Count>0)
            {
                var first=left.First();left.Remove(first);queue.Enqueue(first);var group=new List<(int x,int y)>();
                while(queue.Count>0){var c=queue.Dequeue();group.Add(c);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {var n=(c.x+dx,c.y+dy);if(left.Remove(n))queue.Enqueue(n);}}
                result.Add(group);
            }
            return result;
        }
        static string[] Added(Zone zone,HashSet<Entity> old)=>zone.GetAllEntities().Where(e=>!old.Contains(e)).Select(e=>
            {var c=zone.GetEntityCell(e);return e.BlueprintName+"@"+c.X+","+c.Y;}).OrderBy(s=>s).ToArray();
        static string[] Owners(Zone zone)=>zone.GetAllEntities().Select(e=>
            {var c=zone.GetEntityCell(e);return e.ID+":"+e.BlueprintName+"@"+c.X+","+c.Y+":"+e.GetPart<DestructiblePart>()?.HP;}).OrderBy(s=>s).ToArray();
        sealed class NoDrawRandom:Random
        {
            public override int Next()=>throw new InvalidOperationException("Composition consumed the pipeline RNG");
            public override int Next(int max)=>Next();public override int Next(int min,int max)=>Next();public override double NextDouble()=>Next();
        }
    }
}
