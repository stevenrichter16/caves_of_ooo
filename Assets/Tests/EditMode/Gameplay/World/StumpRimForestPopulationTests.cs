using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public sealed class StumpRimForestPopulationTests
    {
        private const string Id = "Overworld.2.3.0";
        private DensityLootTestScope content;
        [Serializable] private sealed class Baselines { public Row[] rows; }
        [Serializable] private sealed class Row { public int seed; public string signature; }
        private static readonly int[] Seeds = {0,1,17,64,65,12345,int.MaxValue,int.MinValue};
        [SetUp] public void SetUp() => content = new DensityLootTestScope();
        [TearDown] public void TearDown() => content.Dispose();
        private static string[] Cells(string signature) => signature.Substring(signature.IndexOf(':',signature.IndexOf(':')+1)+1).Split('|').Take(Zone.Width*Zone.Height).ToArray();
        private static string[] Before(int seed)
        {
            string path = Path.Combine(Application.dataPath,"../Docs/Verification/GraphicsExecution/RimForest/protected-plan-baselines.json");
            return Cells(JsonUtility.FromJson<Baselines>(File.ReadAllText(path)).rows.Single(r=>r.seed==seed).signature);
        }
        private static List<(int x,int y)> Added(StumpCompositionPlan p)
        {
            var before=Before(p.Seed); var after=Cells(p.Signature()); var cells=new List<(int,int)>();
            for(int i=0;i<before.Length;i++) if(before[i]!=after[i])
            {
                Assert.AreEqual(".h",before[i],"Only originally empty reserved habitat can change at "+i);
                Assert.AreEqual("Bushh",after[i],"All original owners and masks must stay exact at "+i);
                cells.Add((i%Zone.Width,i/Zone.Width));
            }
            return cells;
        }
        [TestCaseSource(nameof(Seeds))]
        public void UnderstoryAddsSubstantialUnequalConnectedMassesWithoutChangingOriginalPlan(int seed)
        {
            var p=StumpCompositionPlan.Create(Id,seed);var added=Added(p);
            Assert.That(added.Count,Is.InRange(35,85),"A useful trial envelope, subject to actual visual acceptance.");
            if(seed==65)Assert.GreaterOrEqual(added.Count,50,"The observed baseline needs more than tiny collars.");
            var groups=Components(added);Assert.That(groups.Count,Is.InRange(3,6));
            Assert.Greater(groups.Select(g=>g.Count).Distinct().Count(),1);
            foreach(var g in groups)
            {
                Assert.That(g.Count,Is.InRange(5,24));
                Assert.LessOrEqual(g.Max(c=>c.x)-g.Min(c=>c.x),10);
                Assert.LessOrEqual(g.Max(c=>c.y)-g.Min(c=>c.y),6);
            }
            Assert.GreaterOrEqual(added.Max(c=>c.x)-added.Min(c=>c.x),35,"Distribute the forest patches across the chunk.");
            Assert.AreEqual(p.Signature(),StumpCompositionPlan.Create(Id,seed).Signature());
        }
        [TestCaseSource(nameof(Seeds))]
        public void AdditionsKeepTheCrackRoutesTankAccessAndOriginalTreeShelter(int seed)
        {
            var p=StumpCompositionPlan.Create(Id,seed);var old=Before(seed);var added=Added(p);Assert.IsNotEmpty(added);
            double phase=(double)typeof(StumpCompositionPlan).GetField("phase",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(p);
            foreach(var c in added)
            {
                Assert.IsTrue(p.IsHabitat(c.x,c.y));
                Assert.Greater(Math.Abs(c.y-(12+Math.Sin(c.x*.07+phase)*5)),2.25,"Keep a quiet winding crack.");
                bool tree=false;
                for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                {
                    int x=c.x+dx,y=c.y+dy;if(x<0||y<0||x>=80||y>=25)continue;
                    tree|=old[y*80+x].StartsWith("Tree");
                    if(Math.Abs(dx)<=1&&Math.Abs(dy)<=1)
                    {Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(old[y*80+x].StartsWith("TankBrocchinia"));}
                }
                Assert.IsTrue(tree,"Use existing tree shelter, not scattered arbitrary clutter.");
            }
        }
        [TestCase(64)] [TestCase(65)]
        public void RealOwnersArePassableAndReservationsAndPopulationRemainExact(int seed)
        {
            var with=Build(seed,out var terrain);var without=Build(seed,out var other);var added=Added(terrain.Plan);Assert.IsNotEmpty(added);
            foreach(var c in added)
            {
                var bush=with.GetCell(c.x,c.y).Objects.Single(e=>e.BlueprintName=="Bush");
                Assert.IsFalse(bush.GetPart<PhysicsPart>().Solid);Assert.NotNull(bush.GetPart<DestructiblePart>());
                without.RemoveEntity(without.GetCell(c.x,c.y).Objects.Single(e=>e.BlueprintName=="Bush"));
            }
            var expected=Before(seed).Select((s,i)=>(s,i)).Where(t=>t.s!=".").Select(t=>(t.i%80,t.i/80)).ToArray();
            CollectionAssert.AreEquivalent(expected,with.GenReservedCells);CollectionAssert.AreEquivalent(expected,without.GenReservedCells);
            var originalWith=new HashSet<Entity>(with.GetAllEntities());var originalWithout=new HashSet<Entity>(without.GetAllEntities());
            var table=PopulationTable.GetStumpTable(StumpBand.Summit,3);
            content.Seed(17);Assert.IsTrue(new StumpHabitatPopulationBuilder(terrain,table).BuildZone(with,content.Factory,new Random(17)));
            content.Seed(17);Assert.IsTrue(new StumpHabitatPopulationBuilder(other,table).BuildZone(without,content.Factory,new Random(17)));
            var actual=PopulationRows(with,originalWith);Assert.IsNotEmpty(actual);CollectionAssert.AreEqual(PopulationRows(without,originalWithout),actual);
        }
        [TestCase(false)] [TestCase(true)]
        public void CachedBareOrEditedForestRemainsUnchangedOnReload(bool edited)
        {
            var zone=Build(65,out var terrain);var added=Added(terrain.Plan);Assert.IsNotEmpty(added);
            foreach(var c in added){var bush=zone.GetCell(c.x,c.y).Objects.Single(e=>e.BlueprintName=="Bush");if(!edited||c==added[0])zone.RemoveEntity(bush);else bush.GetPart<DestructiblePart>().HP=2;}
            var player=content.Factory.CreateEntity("Player");Assert.IsTrue(zone.AddEntity(player,0,terrain.Plan.WestY));
            var expected=OwnerRows(zone);var manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);manager.SetActiveZone(zone);var turns=new TurnManager();turns.AddEntity(player);
            var state=GameSessionState.Capture("rim-test","test",manager,turns,player);GameSessionState loaded;
            using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,content.Factory));}
            var restored=loaded.ZoneManager.GetZone(Id);CollectionAssert.AreEqual(expected,OwnerRows(restored));
            Assert.IsFalse(new StumpCompositionBuilder(65).BuildZone(restored,content.Factory,new NoDrawRandom()));CollectionAssert.AreEqual(expected,OwnerRows(restored));
        }
        [Test] public void MissingBushFailsBeforeAnyPlacement()
        {
            content.Factory.Blueprints.Remove("Bush");var zone=new Zone(Id);Assert.IsFalse(new StumpCompositionBuilder(65).BuildZone(zone,content.Factory,new NoDrawRandom()));Assert.AreEqual(0,zone.EntityCount);Assert.IsEmpty(zone.GenReservedCells);
        }
        private Zone Build(int seed,out StumpCompositionBuilder builder){var z=new Zone(Id);builder=new StumpCompositionBuilder(seed);Assert.IsTrue(builder.BuildZone(z,content.Factory,new NoDrawRandom()));return z;}
        private static string[] OwnerRows(Zone z)=>z.GetAllEntities().Select(e=>{var c=z.GetEntityCell(e);return e.ID+":"+e.BlueprintName+"@"+c.X+","+c.Y+":"+e.GetPart<DestructiblePart>()?.HP;}).OrderBy(s=>s).ToArray();
        private static string[] PopulationRows(Zone z,HashSet<Entity> old)=>z.GetAllEntities().Where(e=>!old.Contains(e)).Select(e=>{var c=z.GetEntityCell(e);return e.BlueprintName+"@"+c.X+","+c.Y;}).OrderBy(s=>s).ToArray();
        private static List<List<(int x,int y)>> Components(IEnumerable<(int x,int y)> cells)
        {
            var left=new HashSet<(int x,int y)>(cells);var groups=new List<List<(int x,int y)>>();
            while(left.Count>0){var first=left.First();left.Remove(first);var q=new Queue<(int x,int y)>();q.Enqueue(first);var group=new List<(int x,int y)>();while(q.Count>0){var c=q.Dequeue();group.Add(c);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(left.Remove((c.x+dx,c.y+dy)))q.Enqueue((c.x+dx,c.y+dy));}groups.Add(group);}return groups;
        }
        private sealed class NoDrawRandom:Random{public override int Next()=>throw new InvalidOperationException("No pipeline RNG draws");public override int Next(int max)=>throw new InvalidOperationException("No pipeline RNG draws");public override int Next(int min,int max)=>throw new InvalidOperationException("No pipeline RNG draws");public override double NextDouble()=>throw new InvalidOperationException("No pipeline RNG draws");}
    }
}
