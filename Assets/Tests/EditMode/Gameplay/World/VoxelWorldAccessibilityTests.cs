using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow audit: real native graph generation and movement,
    /// never preview recipes or manually stamped substitute settlements.</summary>
    public class VoxelWorldAccessibilityTests
    {
        private HotbarSaveFixture isolation;
        private Dictionary<string,LootTableData> priorLoot;
        private bool priorLootInitialized;
        private static readonly FieldInfo LootTablesField=typeof(LootTableRegistry).GetField("_byName",BindingFlags.Static|BindingFlags.NonPublic);
        private static readonly FieldInfo LootInitializedField=typeof(LootTableRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            priorLoot=new Dictionary<string,LootTableData>((Dictionary<string,LootTableData>)LootTablesField.GetValue(null));
            priorLootInitialized=LootTableRegistry.IsInitialized;
            isolation=new HotbarSaveFixture(false,false);new TurnManager();TurnManager.World=null;CinderholdCompositionTests.LoadLoot();
        }
        [TearDown] public void Teardown()
        {
            try
            {
                var tables=(Dictionary<string,LootTableData>)LootTablesField.GetValue(null);
                tables.Clear();foreach(var pair in priorLoot)tables.Add(pair.Key,pair.Value);
                LootInitializedField.SetValue(null,priorLootInitialized);
            }
            finally{isolation?.Dispose();}
        }

        [Test] public void SerializedForestSpawnHasActualMorrowfastOneEastwardBoundaryAway()
        {
            string scene=File.ReadAllText(Path.Combine(Application.dataPath,"Scenes/Main/SampleScene.unity"));
            StringAssert.Contains("FreshGameZoneID: Overworld.2.6.0",scene);
            var m=Manager();var z=m.GetZone("Overworld.2.6.0");var player=PlacePlayer(z);
            WalkTo(z,player,c=>c.X==79,"forest east edge");var at=z.GetEntityCell(player);
            Assert.AreEqual(TransitionDirection.East,ZoneTransitionSystem.GetTransitionDirection(at.X,at.Y,1,0));
            var result=ZoneTransitionSystem.TransitionPlayer(player,z,TransitionDirection.East,at.X,at.Y,m,m.WorldMap);
            AssertTransition(player,z,result,"Overworld.3.6.0");Assert.IsTrue(MorrowfastSceneRuntime.IsActive(result.NewZone));
            Assert.IsTrue(result.NewZone.GetAllEntities().Any(e=>e.HasPart<MorrowfastPropPart>()),"Destination must have actual authored owners, not just a town name.");
        }

        [TestCase(64)][TestCase(1729)]
        public void CurrentSavedAddressHasNativeSumpholdOneWestwardBoundaryAway(int seed)
        {
            var m=Manager(seed);var z=m.GetZone("Overworld.16.6.0");var p=PlacePlayer(z);
            WalkTo(z,p,c=>c.X==0,"Sodden west edge");var at=z.GetEntityCell(p);
            var result=ZoneTransitionSystem.TransitionPlayer(p,z,TransitionDirection.West,at.X,at.Y,m,m.WorldMap);
            AssertTransition(p,z,result,SumpholdCompositionPlan.ZoneID);
            Assert.AreEqual("Boatyard",m.WorldMap.GetPOI(15,6).Profile);
            foreach(var expected in new[]{("BoatFrame",2),("PeatCutter",2),("TollRolls",1)})Assert.AreEqual(expected.Item2,result.NewZone.GetAllEntities().Count(e=>e.BlueprintName==expected.Item1));
            Assert.IsTrue(result.NewZone.GetAllEntities().Any(e=>e.BlueprintName=="Merchant"));
            Assert.AreNotEqual(SumpholdCompositionPlan.ZoneID,WorldMap.GetAdjacentZoneID("Overworld.16.6.1",-1,0),"Horizontal travel preserves depth.");
        }

        [TestCase(7,8,"Gantry","GantryRegistrar")]
        [TestCase(13,7,"Tine","BoatFrame")]
        [TestCase(14,9,"Quillhold","Scribe")]
        [TestCase(10,14,"Tally","Quartermaster")]
        [TestCase(15,6,"Sumphold","PeatCutter")]
        [TestCase(17,5,"the Drowned Ledger","CurationSorter")]
        [TestCase(12,12,"Marrowstye","FilerClerk")]
        [TestCase(8,16,"Wellmeet","SaltMaster")]
        [TestCase(5,17,"the First Tent","TentRightHost")]
        [TestCase(18,18,"the Last Counter","LastCounterSign")]
        [TestCase(6,6,"Cinderhold","ConcordFactor")]
        public void VisibleVillageMarkerLeadsThroughRealWorldMapMovementToNativeDestination(int wx,int wy,string name,string owner)
        {
            var m=Manager();var from=m.GetZone("Overworld.16.6.0");var p=PlacePlayer(from);
            Assert.IsFalse(WorldMapTraversal.TryWorldMapVertical(p,from,true,m).Success,"Down from ordinary ground must not select a town.");
            var up=WorldMapTraversal.TryWorldMapVertical(p,from,false,m);Assert.IsTrue(up.Success,up.ErrorReason);
            Assert.AreEqual(WorldMap.WorldMapZoneID,up.NewZone.ZoneID);Assert.IsNull(from.GetEntityCell(p));
            var target=WorldMap.WorldCellToZoneCell(wx,wy);var marker=up.NewZone.GetCell(target.Item1,target.Item2).Objects.Single(e=>e.HasPart<WorldMapCellPart>());
            Assert.AreEqual("!",marker.GetPart<RenderPart>().RenderString);StringAssert.Contains(name,marker.GetDisplayName());
            WalkTo(up.NewZone,p,c=>c.X==target.Item1&&c.Y==target.Item2,"world map town marker");
            var down=WorldMapTraversal.TryWorldMapVertical(p,up.NewZone,true,m);
            AssertTransition(p,up.NewZone,down,WorldMap.ToZoneID(wx,wy,0));
            Assert.IsTrue(down.NewZone.GetAllEntities().Any(e=>e.BlueprintName==owner),name+" missing actual "+owner);
            Assert.IsTrue(down.NewZone.GetAllEntities().Any(e=>e.BlueprintName=="Merchant"),name+" missing native village services");
            Assert.AreSame(down.NewZone,m.GetZone(down.NewZone.ZoneID),"Revisit retains the native graph rather than generating a new scene.");
        }

        [TestCase(2,7,"GinFrog")][TestCase(5,4,"ChoirNode")]
        [TestCase(2,4,"SealedLibraryDoor")][TestCase(4,6,"TheRooted")]
        public void AuthoredStackHasTwoPhysicalDescentsAndAReciprocalReturn(int wx,int wy,string floorOwner)
        {
            var m=Manager();var mouth=m.GetZone(WorldMap.ToZoneID(wx,wy,0));
            var p=PlacePlayer(mouth);Zone current=mouth;
            for(int depth=1;depth<=2;depth++)
            {
                WalkTo(current,p,c=>c.Objects.Any(e=>e.HasPart<StairsDownPart>()),"physical stairs down at "+current.ZoneID);
                var at=current.GetEntityCell(p);var next=ZoneTransitionSystem.TransitionPlayerVertical(p,current,true,at.X,at.Y,m);
                AssertTransition(p,current,next,WorldMap.ToZoneID(wx,wy,depth));current=next.NewZone;
                Assert.IsTrue(current.GetAllEntities().Any(e=>e.HasPart<StairsUpPart>()));
                Assert.IsFalse(WorldMapTraversal.TryWorldMapVertical(p,current,false,m).Success,"Underground travel cannot skip the actual stairs.");
            }
            Assert.IsTrue(current.GetAllEntities().Any(e=>e.BlueprintName==floorOwner),"Floor destination content missing: "+floorOwner);
            for(int depth=1;depth>=0;depth--)
            {
                WalkTo(current,p,c=>c.Objects.Any(e=>e.HasPart<StairsUpPart>()),"physical stairs up at "+current.ZoneID);
                var at=current.GetEntityCell(p);var next=ZoneTransitionSystem.TransitionPlayerVertical(p,current,false,at.X,at.Y,m);
                AssertTransition(p,current,next,WorldMap.ToZoneID(wx,wy,depth));current=next.NewZone;
            }
            Assert.AreSame(mouth,current);
        }

        private static OverworldZoneManager Manager(int seed=64)=>new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed);
        private static Entity PlacePlayer(Zone z)
        {
            var cell=Enumerable.Range(0,25).SelectMany(y=>Enumerable.Range(0,80).Select(x=>z.GetCell(x,y)))
                .Where(c=>!c.BlocksMovement()).OrderBy(c=>Math.Abs(c.X-40)+Math.Abs(c.Y-12)).First();
            var p=CinderholdCompositionTests.Player();Assert.IsTrue(z.AddEntity(p,cell.X,cell.Y));return p;
        }
        private static void AssertTransition(Entity p,Zone old,ZoneTransitionResult result,string expected)
        {
            Assert.IsTrue(result.Success,result.ErrorReason);Assert.AreEqual(expected,result.NewZone.ZoneID);Assert.IsNull(old.GetEntityCell(p));
            Assert.NotNull(result.NewZone.GetEntityCell(p));Assert.AreEqual(1,result.NewZone.GetAllEntities().Count(e=>ReferenceEquals(e,p)));
        }
        private static void WalkTo(Zone z,Entity p,Func<Cell,bool> goal,string purpose)
        {
            var start=z.GetEntityCell(p);Assert.NotNull(start);var from=(x:start.X,y:start.Y);
            var queue=new Queue<(int x,int y)>();var previous=new Dictionary<(int x,int y),(int x,int y)>();queue.Enqueue(from);previous[from]=from;
            (int x,int y)? found=null;
            while(queue.Count>0)
            {
                var c=queue.Dequeue();if(goal(z.GetCell(c.x,c.y))){found=c;break;}
                foreach(var n in CinderholdCompositionTests.Neighbors(c.x,c.y))
                    if(z.InBounds(n.x,n.y)&&!previous.ContainsKey(n)&&!z.GetCell(n.x,n.y).BlocksMovement()){previous[n]=c;queue.Enqueue(n);}
            }
            Assert.IsTrue(found.HasValue,purpose+" unreachable from "+from+" in "+z.ZoneID);
            var path=new List<(int x,int y)>();for(var c=found.Value;c!=from;c=previous[c])path.Add(c);path.Reverse();
            foreach(var next in path){var current=z.GetEntityCell(p);Assert.NotNull(current,purpose+" lost actor membership");Assert.IsTrue(MovementSystem.TryMove(p,z,next.x-current.X,next.y-current.Y),purpose+" rejected native move to "+next);}
        }
    }
}
