using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class MarrowstyeCompositionAdversarialTests
    {
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ClearNativeLoot()=>LootTableRegistry.ResetForTests();

        [TestCase("RoadStone")] [TestCase("Tree")] [TestCase("Bush")]
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("SandstoneWall")]
        [TestCase("StoneCoffer")] [TestCase("SaltCuredBody")] [TestCase("FilerClerk")]
        public void MissingRequiredOwnerRejectsBeforeTerrainMutation(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(MarrowstyeCompositionTests.Id);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new MarrowstyeCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase("StoneCoffer","Handling")] [TestCase("SaltCuredBody","Handling")]
        [TestCase("FilerClerk","Conversation")] [TestCase("SandstoneWall","Destructible")]
        public void MalformedNativeContractRejectsAtomically(string bp,string part)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));var z=new Zone(MarrowstyeCompositionTests.Id);
            Assert.IsFalse(new MarrowstyeCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
        }
        [TestCase(false)] [TestCase(true)]
        public void ClerkRenderingContractRejectsReskinsBeforeTerrainOrLateBatchMutation(bool lateMutation)
        {
            var f=GrovelandsCompositionTests.Factory();
            Assert.AreEqual("@",f.CreateEntity("FilerClerk").GetPart<RenderPart>().RenderString);
            var z=new Zone(MarrowstyeCompositionTests.Id);var b=new MarrowstyeCompositionBuilder(64);
            if(lateMutation)Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var before=z.GetAllEntities().ToArray();var reservations=z.GenReservedCells.ToArray();
            f.Blueprints["FilerClerk"].Parts["Render"]["RenderString"]="?";
            Assert.AreEqual("?",f.CreateEntity("FilerClerk").GetPart<RenderPart>().RenderString);
            Assert.IsFalse(lateMutation?new MarrowstyeProfileBuilder(b).BuildZone(z,f,new Random(1)):b.BuildZone(z,f,new Random(1)));
            CollectionAssert.AreEquivalent(before,z.GetAllEntities());CollectionAssert.AreEquivalent(reservations,z.GenReservedCells);
            f.Blueprints["FilerClerk"].Parts["Render"]["RenderString"]="@";
            if(!lateMutation)Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            Assert.IsTrue(new MarrowstyeProfileBuilder(b).BuildZone(z,f,new Random(1)));
            Assert.AreEqual("@",z.GetAllEntities().Single(e=>e.BlueprintName=="FilerClerk").GetPart<RenderPart>().RenderString);
        }
        [Test] public void RejectedBuildKeepsLivePlanAndClearedRetryGetsFreshOwners()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(MarrowstyeCompositionTests.Id);var b=new MarrowstyeCompositionBuilder(64);var late=new MarrowstyeProfileBuilder(b);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));var p=b.Plan;var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));Assert.AreSame(p,b.Plan);CollectionAssert.AreEquivalent(before,z.GetAllEntities());
            Assert.IsFalse(late.BuildZone(new Zone(z.ZoneID),f,new Random(1)));
            foreach(var e in before)z.RemoveEntity(e);Assert.IsTrue(b.BuildZone(z,f,new Random(2)));Assert.IsTrue(late.BuildZone(z,f,new Random(2)));Assert.IsFalse(z.GetAllEntities().Any(before.Contains));
        }
        [Test] public void BlockedProfileCellRejectsEntireLateBatch()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(MarrowstyeCompositionTests.Id);var b=new MarrowstyeCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var p=b.Plan.Profile[0];z.AddEntity(f.CreateEntity("SandstoneWall"),p.X,p.Y);var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(new MarrowstyeProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        private static Entity Player(int strength)
        {
            var e=new Entity{BlueprintName="Player"};e.SetTag("Player");e.SetTag("Creature");e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new InventoryPart());e.AddPart(new RenderPart{DisplayName="you"});
            foreach(var s in new[]{("Strength",strength),("Speed",100),("Hitpoints",30)})e.Statistics[s.Item1]=new Stat{Owner=e,Name=s.Item1,BaseValue=s.Item2,Max=s.Item1=="Speed"?999:30};return e;
        }
        [TestCase("StoneCoffer",14,110)] [TestCase("SaltCuredBody",12,90)]
        public void ActualCargoHaulsAtItsThresholdAndBlockedMovementIsAtomic(string bp,int strength,int weight)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(MarrowstyeCompositionTests.Id);var b=new MarrowstyeCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new MarrowstyeProfileBuilder(b).BuildZone(z,f,new Random(1)));
            var cargo=z.GetAllEntities().First(e=>e.BlueprintName==bp);var c=z.GetEntityPosition(cargo);
            Assert.AreEqual(weight,HandlingService.GetWeight(cargo));Assert.AreEqual(DragVerdict.TooHeavy,DragRules.CanDrag(Player(strength-1),cargo));
            var d=new[]{(-1,0),(1,0),(0,-1),(0,1)}.First(v=>z.InBounds(c.x+v.Item1*3,c.y+v.Item2*3)&&Enumerable.Range(1,3).All(n=>!z.GetCell(c.x+v.Item1*n,c.y+v.Item2*n).BlocksMovement()));
            var player=Player(strength);Assert.AreEqual(100,player.GetStatValue("Speed"),"Harness hauler must use the native Speed range rather than Stat default Max30.");z.AddEntity(player,c.x+d.Item1,c.y+d.Item2);Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(player,cargo,z));
            Assert.AreEqual(100-weight*4/10,player.GetStatValue("Speed"));Assert.IsTrue(MovementSystem.TryMove(player,z,d.Item1,d.Item2));
            Assert.AreEqual((c.x+d.Item1,c.y+d.Item2),z.GetEntityPosition(cargo));
            var blocker=f.CreateEntity("SandstoneWall");z.AddEntity(blocker,c.x+d.Item1*3,c.y+d.Item2*3);var actorAt=z.GetEntityPosition(player);var loadAt=z.GetEntityPosition(cargo);
            Assert.IsFalse(MovementSystem.TryMove(player,z,d.Item1,d.Item2));Assert.AreEqual(actorAt,z.GetEntityPosition(player));Assert.AreEqual(loadAt,z.GetEntityPosition(cargo));
            Assert.IsTrue(DragSystem.Release(player));Assert.AreEqual(100,player.GetStatValue("Speed"));
        }
        [Test] public void ActualCaveRollsStayOutsideIntakeReservationsAndKeepClearArrivals()
        {
            var f=GrovelandsCompositionTests.Factory();int found=0;
            foreach(int seed in new[]{1,2,3,4,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(MarrowstyeCompositionTests.Id);
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>())){found++;var c=z.GetEntityCell(e);Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));Assert.IsFalse(c.BlocksMovement());}
            }
            Assert.Greater(found,0);
        }
        [Test] public void ManagerCaveFilterUsesRealizedPlanAndRejectsBlockedArrivalNeighbor()
        {
            var f=GrovelandsCompositionTests.Factory();var manager=new OverworldZoneManager(f,64);
            var method=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var pipeline=(ZoneGenerationPipeline)method.Invoke(manager,new object[]{MarrowstyeCompositionTests.Id});
            var terrain=pipeline.Builders.OfType<MarrowstyeCompositionBuilder>().Single();var entrance=pipeline.Builders.OfType<CaveEntranceBuilder>().Single();
            var z=new Zone(MarrowstyeCompositionTests.Id);Assert.IsTrue(terrain.BuildZone(z,f,new Random(1)));
            Cell candidate=null;for(int y=1;y<24&&candidate==null;y++)for(int x=1;x<79&&candidate==null;x++)if(terrain.CanPlaceCaveEntrance(z,z.GetCell(x,y)))candidate=z.GetCell(x,y);
            Assert.NotNull(candidate);Assert.NotNull(entrance.PlacementFilter);Assert.IsTrue(entrance.PlacementFilter(z,candidate));
            Assert.IsFalse(terrain.CanPlaceCaveEntrance(z,new Zone(z.ZoneID).GetCell(candidate.X,candidate.Y)));
            var blocker=f.CreateEntity("SandstoneWall");z.AddEntity(blocker,candidate.X+1,candidate.Y);
            Assert.IsFalse(entrance.PlacementFilter(z,candidate),"Center remains open, but native stair arrival cannot be blocked on an adjacent cell.");
            z.RemoveEntity(blocker);Assert.IsTrue(entrance.PlacementFilter(z,candidate));
        }
        [TestCase(false,"SealedBogTakenBody")] [TestCase(true,null)] [TestCase(true,"SaltCuredBody")]
        [TestCase(true,"SealedBogTakenBody")]
        public void PlacedClerkRequiresActivePaperAndExactParcelAndRewardsOnlyOnce(bool active,string bp)
        {
            ConversationManager.EndConversation();ConversationActions.Reset();ConversationPredicates.Reset();ConversationLoader.Reset();StoryletRegistry.Reset();PlayerReputation.Reset();
            StoryletPart.Current=new StoryletPart();NarrativeStatePart.Current=new NarrativeStatePart();
            var f=GrovelandsCompositionTests.Factory();ConversationActions.Factory=f;
            try
            {
                ConversationLoader.LoadFromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Conversations/FriendlyNPCs.json")),"FriendlyNPCs.json");
                StoryletRegistry.LoadFromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/Storylets/BogBodyCourier.json")));
                var z=new OverworldZoneManager(f,64).GetZone(MarrowstyeCompositionTests.Id);var clerk=z.GetAllEntities().Single(e=>e.BlueprintName=="FilerClerk");var player=Player(16);
                if(active)StoryletPart.Current.StartQuest(new QuestState{QuestId="BogBodyCourier",CurrentStageIndex=0});
                if(bp=="SaltCuredBody")Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==bp),"Ordinary cured intake cargo is present but is not the carried courier parcel.");
                else if(bp!=null)Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(f.CreateEntity(bp)));
                var c=z.GetEntityPosition(clerk);var d=new[]{(-1,0),(1,0),(0,-1),(0,1)}.First(v=>!z.GetCell(c.x+v.Item1,c.y+v.Item2).BlocksMovement());z.AddEntity(player,c.x+d.Item1,c.y+d.Item2);
                Assert.IsFalse(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"),"Arriving is not delivering.");
                ConversationManager.StartConversation(clerk,player);int index=-1;
                for(int i=0;i<ConversationManager.VisibleChoices.Count;i++)if(ConversationManager.VisibleChoices[i].Text.StartsWith("[Deliver]"))index=i;
                bool valid=active&&bp=="SealedBogTakenBody";Assert.AreEqual(valid,index>=0);
                if(valid)
                {
                    ConversationManager.SelectChoice(index);Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName==bp));
                    Assert.AreEqual(25,TradeSystem.GetDrams(player));Assert.AreEqual(10,PlayerReputation.Get("PaleCuration"));Assert.IsTrue(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
                    ConversationManager.EndConversation();player.GetPart<InventoryPart>().AddObject(f.CreateEntity("SealedBogTakenBody"));ConversationManager.StartConversation(clerk,player);
                    Assert.IsFalse(ConversationManager.VisibleChoices.Any(c2=>c2.Text.StartsWith("[Deliver]")));Assert.AreEqual(25,TradeSystem.GetDrams(player));Assert.AreEqual(10,PlayerReputation.Get("PaleCuration"));
                }
                else {Assert.AreEqual(0,TradeSystem.GetDrams(player));Assert.AreEqual(0,PlayerReputation.Get("PaleCuration"));}
            }
            finally
            {ConversationManager.EndConversation();ConversationLoader.Reset();StoryletRegistry.Reset();StoryletPart.Current=null;StoryletPart.LocalPlayer=null;NarrativeStatePart.Current=null;ConversationActions.Factory=null;PlayerReputation.Reset();}
        }
    }
}
