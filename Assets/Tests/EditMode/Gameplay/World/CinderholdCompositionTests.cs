using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>CoO-original working-town contract: the layout must lead to
    /// native work, trade and the pruning errand, rather than simulate them in art.</summary>
    public class CinderholdCompositionTests
    {
        public const string Id="Overworld.6.6.0";
        public static EntityFactory Factory()=>GrovelandsCompositionTests.Factory();
        public static ZoneGenerationPipeline Pipeline(OverworldZoneManager m,string id)=>
            (ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,new object[]{id});
        public static IEnumerable<(int x,int y)> Neighbors(int x,int y)
        {yield return(x-1,y);yield return(x+1,y);yield return(x,y-1);yield return(x,y+1);}
        // One real border entry, not seeding every interior pocket independently.
        public static bool[,] Flood(Zone z)
        {
            var seen=new bool[80,25];var q=new Queue<(int x,int y)>();
            for(int y=0;y<25;y++)if(!z.GetCell(0,y).BlocksMovement()){q.Enqueue((0,y));seen[0,y]=true;break;}
            Assert.Greater(q.Count,0,"A west entry must exist.");
            while(q.Count>0){var c=q.Dequeue();foreach(var n in Neighbors(c.x,c.y))if(z.InBounds(n.x,n.y)&&!seen[n.x,n.y]&&!z.GetCell(n.x,n.y).BlocksMovement()){seen[n.x,n.y]=true;q.Enqueue(n);}}
            return seen;
        }
        public static void AssertConnected(Zone z)
        {
            var seen=Flood(z);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(seen[x,y],"Unreachable open cell "+x+","+y);
            Assert.IsTrue(Enumerable.Range(0,80).Any(x=>seen[x,0]));Assert.IsTrue(Enumerable.Range(0,80).Any(x=>seen[x,24]));Assert.IsTrue(Enumerable.Range(0,25).Any(y=>seen[79,y]));
        }
        public static void LoadLoot()=>LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/Loot/LootTables.json")));

        [Test] public void ExactTownHasRepeatableButSeedDependentSemanticArchitecture()
        {
            Assert.IsTrue(CinderholdCompositionPlan.IsSupportedZone(Id));
            Assert.AreEqual(CinderholdCompositionPlan.Create(Id,64).Signature(),CinderholdCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(CinderholdCompositionPlan.Create(Id,64).Signature(),CinderholdCompositionPlan.Create(Id,1729).Signature());
            Assert.AreEqual(BiomeType.Grovelands,WorldMapAuthoring.BiomeAt(6,6));Assert.AreEqual(3,WorldMapAuthoring.TierAt(6,6));
            Assert.IsTrue(WorldMapAuthoring.IsRoad(6,6));Assert.IsFalse(WorldMapAuthoring.IsRiver(6,6));
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.6.6.1")] [TestCase("Overworld.6.6.-1")]
        [TestCase("Overworld.06.6.0")] [TestCase("Overworld.6.6.00")] [TestCase(" Overworld.6.6.0")]
        [TestCase("Overworld.6.6.0 ")] [TestCase("overworld.6.6.0")] [TestCase("Overworld.5.6.0")]
        [TestCase("Overworld.18.18.0")] [TestCase("Overworld.999999999999.6.0")]
        public void NoncanonicalAndForeignAddressesCannotAuthorTheNamedTown(string id)
        {Assert.IsFalse(CinderholdCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>CinderholdCompositionPlan.Create(id,64));}

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FourUnequalStoneRoomsServeWorkPublicBusinessAndQuietHomes(int seed)
        {
            var f=Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(seed);Assert.AreEqual(1000,b.Priority);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;Assert.NotNull(p);
            Assert.AreEqual(4,p.Rooms.Count);Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Role).Distinct().Count(),3);
            Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Width*r.Height).Distinct().Count(),3);
            foreach(var r in p.Rooms)
            {
                Assert.GreaterOrEqual(r.Width,8);Assert.GreaterOrEqual(r.Height,5);
                Assert.IsTrue(p.IsApproach(r.DoorX,r.DoorY));Assert.IsFalse(z.GetCell(r.DoorX,r.DoorY).BlocksMovement());
                for(int y=r.Y+1;y<r.Y+r.Height-1;y++)for(int x=r.X+1;x<r.X+r.Width-1;x++)
                {Assert.IsTrue(p.IsInterior(x,y));Assert.IsTrue(z.GetCell(x,y).IsInterior);Assert.AreEqual("StoneFloor",p.GroundAt(x,y));}
            }
            foreach(var c in new[]{(40,12),(39,12),(41,12),(40,11),(40,13)})
            {Assert.IsFalse(z.GetCell(c.Item1,c.Item2).BlocksMovement());Assert.IsFalse(p.IsInterior(c.Item1,c.Item2));Assert.IsTrue(p.IsReserved(c.Item1,c.Item2));}
            AssertConnected(z);
            int trees=z.GetAllEntities().Count(e=>e.BlueprintName=="Tree");Assert.That(trees,Is.InRange(8,45));
            Assert.Greater(z.GetAllEntities().Count(e=>e.BlueprintName=="Bed"),0);Assert.Greater(z.GetAllEntities().Count(e=>e.BlueprintName=="Chair"),0);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                var c=z.GetCell(x,y);Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                Assert.IsFalse(c.Objects.GroupBy(e=>e.BlueprintName).Any(g=>g.Count()>1));
                if(p.IsApproach(x,y)){Assert.IsFalse(c.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
                if(!p.IsInterior(x,y))Assert.AreNotEqual("StoneFloor",p.GroundAt(x,y),"Outdoor work and road surfaces must not masquerade as native indoor population cells.");
            }
        }

        [Test] public void LateProfilePreservesThePostAndAddsARealDistinctWorkshopOnce()
        {
            LoadLoot();try
            {
                var f=Factory();var z=new Zone(Id);var b=new CinderholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                var expected=new[]{"ConcordFactor","CinderholdNoticeBoard","Chest","Campfire","TinkersForge","SmithAnvil","Weaponsmith"};
                CollectionAssert.AreEquivalent(expected,b.Plan.Profile.Select(p=>p.Blueprint));
                foreach(var bp in expected)Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName==bp));
                var late=new CinderholdProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(2)));
                foreach(var bp in expected)Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
                foreach(var p in b.Plan.Profile){var e=z.GetCell(p.X,p.Y).Objects.Single(a=>a.BlueprintName==p.Blueprint);Assert.AreEqual(Id,e.GetProperty("SettlementId"));}
                var chest=z.GetAllEntities().Single(e=>e.BlueprintName=="Chest");Assert.Greater(chest.GetPart<ContainerPart>().Contents.Count,0);
                var forge=z.GetAllEntities().Single(e=>e.BlueprintName=="TinkersForge");var anvil=z.GetAllEntities().Single(e=>e.BlueprintName=="SmithAnvil");
                Assert.NotNull(forge.GetPart<ForgePart>());Assert.IsFalse(forge.GetPart<PhysicsPart>().Solid);Assert.IsFalse(forge.HasPart<LightSourcePart>());
                Assert.IsFalse(anvil.HasPart<ForgePart>());Assert.IsTrue(anvil.GetPart<PhysicsPart>().Solid);Assert.AreEqual(120,anvil.GetPart<HandlingPart>().Weight);
                Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="LastCounterSign"));
                var before=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(3)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
            }finally{LootTableRegistry.ResetForTests();}
        }

        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void RealVillagePipelineKeepsResidentsAndServicesWithoutTheUnmappedRiver(int seed)
        {
            LoadLoot();try
            {
                var m=new OverworldZoneManager(Factory(),seed);var pipeline=Pipeline(m,Id);
                Assert.AreEqual(1,pipeline.Builders.OfType<CinderholdCompositionBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<CinderholdProfileBuilder>().Count());
                Assert.AreEqual(1,pipeline.Builders.OfType<CinderholdArrivalReservationBuilder>().Count());Assert.IsFalse(pipeline.Builders.OfType<RiverChunkBuilder>().Any());
                Assert.IsTrue(pipeline.Builders.OfType<VillagePopulationBuilder>().Any());Assert.IsTrue(pipeline.Builders.OfType<CaveEntranceBuilder>().Any());Assert.IsTrue(pipeline.Builders.OfType<TradeStockBuilder>().Any());
                var z=m.GetZone(Id);foreach(var bp in new[]{"ConcordFactor","CinderholdNoticeBoard","Weaponsmith","TinkersForge","SmithAnvil","Elder","Merchant","Quartermaster","Scribe","Innkeeper"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
                Assert.AreEqual(1,z.GetCell(40,12).Objects.Count(e=>e.BlueprintName=="Well"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));
                Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Shrine"));
                Assert.IsNull(m.SettlementManager.GetSite(Id,SettlementSiteDefinitions.MainWellSiteId),"This composition does not expand Wellmeet's repair economy.");
                Assert.IsTrue(Pipeline(m,"Overworld.18.18.0").Builders.OfType<RiverChunkBuilder>().Any(),"An unrelated village keeps its native pipeline.");
            }finally{LootTableRegistry.ResetForTests();}
        }

        [TestCase(true)] [TestCase(false)]
        public void ActualWeaponsmithStockCanBeBoughtOnlyWithPaymentAndRefills(bool funded)
        {
            var oldTrader=TraderPart.Factory;var oldRestock=TraderRestockSystem.Factory;var oldRng=TraderPart.Rng;
            LoadLoot();try
            {
                var f=Factory();TraderPart.Factory=f;TraderRestockSystem.Factory=f;TraderPart.Rng=new Random(64);
                var z=new OverworldZoneManager(f,64).GetZone(Id);var smith=z.GetAllEntities().Single(e=>e.BlueprintName=="Weaponsmith");
                Assert.AreEqual("WeaponsmithStock",smith.GetPart<TraderPart>().StockTable);Assert.AreEqual("WeaponsmithStock",smith.GetProperty("ShopStockTable"));
                var shelf=smith.GetPart<InventoryPart>();Assert.Greater(shelf.Objects.Count,0);var item=shelf.Objects.First(e=>e.HasPart<MeleeWeaponPart>());
                var player=Player();TradeSystem.SetDrams(player,funded?10000:0);int purse=TradeSystem.GetDrams(smith);
                Assert.AreEqual(funded,TradeSystem.BuyFromTrader(player,smith,item));Assert.AreEqual(funded,player.GetPart<InventoryPart>().Objects.Contains(item));Assert.AreEqual(!funded,shelf.Objects.Contains(item));
                Assert.AreEqual(funded,TradeSystem.GetDrams(smith)>purse);
                foreach(var e in shelf.Objects.ToArray())shelf.RemoveObject(e);
                TraderRestockSystem.RestockZone(z,TraderRestockSystem.RestockIntervalTurns+1);Assert.Greater(shelf.Objects.Count,0);
            }finally{TraderPart.Factory=oldTrader;TraderRestockSystem.Factory=oldRestock;TraderPart.Rng=oldRng;LootTableRegistry.ResetForTests();}
        }

        [Test] public void NativeFactorErrandPostsAtTheChoirThenReportsForPaymentWithoutPrematureCompletion()
        {
            WithQuest((f,z,player)=>
            {
                var factor=z.GetAllEntities().Single(e=>e.BlueprintName=="ConcordFactor");
                ConversationManager.StartConversation(factor,player);Assert.IsFalse(ChoiceVisible("[Report]"));Assert.IsTrue(Choose("Is there work?"));Assert.IsTrue(Choose("I will post it."));
                Assert.IsTrue(StoryletPart.Current.IsQuestActive("PruningContract"));Assert.IsTrue(HasItem(player,"PruningWrit"));Assert.AreEqual(0,TradeSystem.GetDrams(player));
                ConversationManager.EndConversation();var tendril=f.CreateEntity("ChoirTendril");
                ConversationManager.StartConversation(tendril,player);Assert.IsTrue(Choose("[Post]"));Assert.IsFalse(HasItem(player,"PruningWrit"));Assert.AreEqual(-9,PlayerReputation.Get("RotChoir"));
                StoryletPart.Current.OnTickEnd(NarrativeStatePart.Current);StoryletPart.Current.OnTickEnd(NarrativeStatePart.Current);Assert.IsTrue(StoryletPart.Current.IsQuestActive("PruningContract"));
                ConversationManager.EndConversation();ConversationManager.StartConversation(factor,player);Assert.IsTrue(Choose("[Report]"));
                Assert.AreEqual(20,TradeSystem.GetDrams(player));Assert.AreEqual(10,PlayerReputation.Get("SaccharineConcord"));Assert.IsTrue(StoryletPart.Current.IsQuestCompleted("PruningContract"));
                ConversationManager.EndConversation();ConversationManager.StartConversation(factor,player);Assert.IsFalse(ChoiceVisible("[Report]"));Assert.IsFalse(ChoiceVisible("Is there work?"));
            });
        }
        [Test] public void RefusalCostsConcordStandingOnceAndNeverHandsOutAContract()
        {
            WithQuest((f,z,p)=>
            {
                var factor=z.GetAllEntities().Single(e=>e.BlueprintName=="ConcordFactor");
                for(int i=0;i<2;i++)
                {ConversationManager.StartConversation(factor,p);Assert.IsTrue(Choose("Is there work?"));Assert.IsTrue(Choose(i==0?"No -- and I say it aloud.":"Still no."));ConversationManager.EndConversation();}
                Assert.AreEqual(-5,PlayerReputation.Get("SaccharineConcord"));Assert.IsFalse(HasItem(p,"PruningWrit"));Assert.IsFalse(StoryletPart.Current.IsQuestActive("PruningContract"));Assert.AreEqual(0,TradeSystem.GetDrams(p));
            });
        }
        public static Entity Player()
        {
            var p=new Entity{BlueprintName="Player"};p.SetTag("Player");p.SetTag("Creature");p.AddPart(new RenderPart());p.AddPart(new InventoryPart{MaxWeight=1000});
            p.Statistics["Hitpoints"]=new Stat{Owner=p,Name="Hitpoints",BaseValue=40,Min=0,Max=40};p.Statistics["Strength"]=new Stat{Owner=p,Name="Strength",BaseValue=20};return p;
        }
        private static bool HasItem(Entity p,string bp)=>p.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName==bp);
        public static bool ChoiceVisible(string prefix)=>ConversationManager.VisibleChoices.Any(c=>c.Text.StartsWith(prefix));
        public static bool Choose(string prefix)
        {for(int i=0;i<ConversationManager.VisibleChoices.Count;i++)if(ConversationManager.VisibleChoices[i].Text.StartsWith(prefix)){ConversationManager.SelectChoice(i);return true;}return false;}
        public static void WithQuest(Action<EntityFactory,Zone,Entity> run)
        {
            var oldFactory=ConversationActions.Factory;LoadLoot();
            try
            {
                ConversationActions.Reset();ConversationPredicates.Reset();ConversationLoader.Reset();StoryletRegistry.Reset();ConversationManager.EndConversation();
                StoryletPart.Current=new StoryletPart();StoryletPart.LocalPlayer=null;NarrativeStatePart.Current=new NarrativeStatePart();PlayerReputation.Reset();
                foreach(var file in new[]{"FriendlyNPCs","RotChoir"})ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Conversations/"+file+".json")));
                StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/Storylets/PruningContract.json")));
                var f=Factory();ConversationActions.Factory=f;run(f,new OverworldZoneManager(f,64).GetZone(Id),Player());
            }
            finally{ConversationManager.EndConversation();ConversationLoader.Reset();StoryletRegistry.Reset();StoryletPart.Current=null;StoryletPart.LocalPlayer=null;NarrativeStatePart.Current=null;PlayerReputation.Reset();ConversationActions.Factory=oldFactory;LootTableRegistry.ResetForTests();}
        }
    }
}
