using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class CathedralCompositionAdversarialTests
    {
        // H1: malformed coordinates are accidentally clamped onto a real
        // wall or expose a route bit outside the playable cell domain.
        [TestCase(-1,0)] [TestCase(80,24)] [TestCase(0,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void OutOfBoundsQueriesDoNotCreateTerrainOrApproaches(int x,int y)
        {
            for(int depth=0;depth<3;depth++)
            {var p=CathedralCompositionPlan.Create(CathedralCompositionTests.Id(depth),64);
                Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsApproach(x,y));}
        }

        // H2: shared pipeline random consumption changes the town's geometry
        // even though the player loaded the same world and address.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CallerRandomConsumptionDoesNotRearrangeThePlace(int depth)
        {
            var f=GrovelandsCompositionTests.Factory();var a=new Zone(CathedralCompositionTests.Id(depth));var b=new Zone(a.ZoneID);
            var one=new CathedralCompositionBuilder(64);var two=new CathedralCompositionBuilder(64);
            var rng=new Random(987);for(int i=0;i<300;i++)rng.Next();
            Assert.IsTrue(one.BuildZone(a,f,rng));Assert.IsTrue(two.BuildZone(b,f,new Random(1)));
            Assert.AreEqual(one.Plan.Signature(),two.Plan.Signature());
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                CollectionAssert.AreEquivalent(a.GetCell(x,y).Objects.Select(e=>e.BlueprintName),b.GetCell(x,y).Objects.Select(e=>e.BlueprintName));
        }

        // H3: retry regenerates a player's removed wall or publishes a stale
        // successful plan after rejection. A nonempty native graph is sacred.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void RebuildPreservesRemovedOwnersAndExistingReservations(int depth)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(CathedralCompositionTests.Id(depth));var b=new CathedralCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var victim=z.GetAllEntities().First(e=>e.BlueprintName==(depth==0?"Tree":"SandstoneWall"));
            z.RemoveEntity(victim);var owners=z.GetAllEntities().ToArray();var reservations=z.GenReservedCells.ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);
            CollectionAssert.AreEquivalent(owners,z.GetAllEntities());CollectionAssert.AreEquivalent(reservations,z.GenReservedCells);
            Assert.IsNull(z.GetEntityCell(victim));
        }

        // H4: factories fail soft. Missing art-independent gameplay content
        // must be caught before the first floor/owner/reservation is committed.
        [TestCase("Grass",0)] [TestCase("Tree",0)] [TestCase("Bush",0)]
        [TestCase("SandstoneFloor",1)] [TestCase("SandstoneWall",1)]
        [TestCase("DescentLedge",1)] [TestCase("RopeAnchor",1)] [TestCase("Sack",1)]
        [TestCase("Bones",1)] [TestCase("Torch",1)] [TestCase("DriedMeat",1)] [TestCase("HealingTonic",1)]
        public void MissingNativeContentRejectsWithoutPartialRealization(string bp,int depth)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));
            var z=new Zone(CathedralCompositionTests.Id(depth));z.GenReservedCells.Add((79,24));var b=new CathedralCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }

        // H5: a non-null entity alone is insufficient. Broken native collision,
        // visibility, supply verbs, or destructibility must fail staging too.
        [TestCase("Grass","Physics","Solid","true",0)]
        [TestCase("Grass","Render","Visible","false",0)]
        [TestCase("SandstoneFloor","Physics","Takeable","true",1)]
        [TestCase("SandstoneFloor","Render","RenderString","?",1)]
        [TestCase("Tree","Destructible",null,null,0)]
        [TestCase("Bush","Material",null,null,0)]
        [TestCase("SandstoneWall","Destructible","HP","0",1)]
        [TestCase("DescentLedge","Physics","Solid","true",1)]
        [TestCase("RopeAnchor","Examinable",null,null,1)]
        [TestCase("Sack","Container",null,null,1)]
        [TestCase("Torch","LightSource",null,null,1)]
        [TestCase("DriedMeat","Food",null,null,1)]
        [TestCase("HealingTonic","Tonic",null,null,1)]
        public void MalformedNativeContractRejectsBeforeMutatingAnyCell(string bp,string part,string key,string value,int depth)
        {
            var f=GrovelandsCompositionTests.Factory();
            if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            var z=new Zone(CathedralCompositionTests.Id(depth));z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new CathedralCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }

        // H6: creating a floor through a debug jump before its parent must
        // still produce actual paired stair entities, never a one-way promise.
        [TestCase(1)] [TestCase(2)]
        public void LowerFirstLoadingKeepsTheActualNativeReturnStair(int depth)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);var lower=m.GetZone(CathedralCompositionTests.Id(depth));
            var upper=m.GetZone(CathedralCompositionTests.Id(depth-1));
            var edge=m.GetConnections(upper.ZoneID).Single(c=>c.SourceZoneID==upper.ZoneID&&c.TargetZoneID==lower.ZoneID&&c.Type=="StairsDown");
            Assert.IsTrue(upper.GetCell(edge.SourceX,edge.SourceY).Objects.Any(e=>e.HasPart<StairsDownPart>()));
            Assert.IsTrue(lower.GetCell(edge.TargetX,edge.TargetY).Objects.Any(e=>e.HasPart<StairsUpPart>()));
            Assert.IsFalse(upper.GetCell(edge.SourceX,edge.SourceY).BlocksMovement());Assert.IsFalse(lower.GetCell(edge.TargetX,edge.TargetY).BlocksMovement());
        }

        // H7: native late stairs/hazards/population/containers can sever an
        // otherwise good base. Strip only mobile actors for a static flood;
        // separately keep the real actors and prove accessible talking cells.
        [TestCase(2048)] [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalPipelineLeavesReciprocalTravelAndReachableEldersAndMerchants(int seed)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed);
            for(int depth=0;depth<3;depth++)
            {
                var z=m.GetZone(CathedralCompositionTests.Id(depth));
                foreach(var stair in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()||e.HasPart<StairsUpPart>()))
                {var c=z.GetEntityCell(stair);Assert.IsFalse(c.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)),"Native stairs need late-pass protection.");}
                if(depth==1)
                {
                    Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="RopeAnchor"));
                    Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="Sack"&&e.GetPart<ContainerPart>()?.Contents.Any(i=>i.BlueprintName=="HealingTonic")==true));
                }
                if(depth==2)
                {
                    Assert.That(z.GetAllEntities().Count(e=>e.BlueprintName=="EncasedElder"),Is.InRange(2,3));
                    Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="ChoirTendril"));
                    var reached=FormationReachability.FloodFromWest(z);
                    foreach(var e in z.GetAllEntities().Where(e=>e.BlueprintName=="EncasedElder"||e.BlueprintName=="ChoirTendril"))
                    {
                        Assert.NotNull(e.GetPart<ConversationPart>());var c=z.GetEntityCell(e);
                        Assert.IsTrue(CathedralCompositionTests.Neighbors(c.X,c.Y).Any(n=>z.InBounds(n.x,n.y)&&!z.GetCell(n.x,n.y).BlocksMovement()&&reached[n.x,n.y]),"Talk frontage lost for "+e.BlueprintName+" seed"+seed);
                    }
                }
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)),z.ZoneID+" seed"+seed+" static pocket");
            }
        }

        // H8: a stale address must not override the live world's replacement
        // POI, and another cathedral archetype is not this authored sanctuary.
        [TestCase(POIType.Village)] [TestCase(POIType.Sinkhole)]
        public void ReplacedRuntimePlaceVetoesTheNamedComposition(POIType type)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            Assert.IsTrue(CathedralCompositionTests.Pipeline(m,CathedralCompositionTests.Id(2)).Builders.Any(b=>b is CathedralCompositionBuilder));
            m.WorldMap.SetPOI(5,4,new PointOfInterest(type,"another place",tier:3));
            Assert.IsFalse(CathedralCompositionTests.Pipeline(m,CathedralCompositionTests.Id(2)).Builders.Any(b=>b is CathedralCompositionBuilder));
        }

        // H9: art must not turn a talking trader into scenery. Use its actual
        // ObjectCreated stock and existing renewals, not a mock inventory.
        [Test] public void NativeTendrilKeepsCreatureConversationAndRenewingChoirStock()
        {
            var oldFactory=TraderPart.Factory;var oldRng=TraderPart.Rng;var oldRestock=TraderRestockSystem.Factory;
            try
            {
                var f=GrovelandsCompositionTests.Factory();
                LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/Loot/LootTables.json")));
                TraderPart.Factory=f;TraderPart.Rng=new Random(64);TraderRestockSystem.Factory=f;
                var z=new OverworldZoneManager(f,64).GetZone(CathedralCompositionTests.Id(2));
                var trader=z.GetAllEntities().First(e=>e.BlueprintName=="ChoirTendril");
                Assert.IsTrue(trader.HasTag("Creature"));Assert.NotNull(trader.GetPart<BrainPart>());Assert.NotNull(trader.GetPart<ConversationPart>());
                Assert.AreEqual("ChoirStock",trader.GetPart<TraderPart>().StockTable);
                Assert.AreEqual("ChoirStock",trader.GetProperty(TraderRestockSystem.ShopStockTableProp));
                Assert.Greater(TradeSystem.GetDrams(trader),0);var inv=trader.GetPart<InventoryPart>();Assert.Greater(inv.Objects.Count,0);
                foreach(var item in inv.Objects.ToArray())inv.RemoveObject(item);
                trader.SetIntProperty(TraderRestockSystem.LastRestockProp,0);
                TraderRestockSystem.RestockZone(z,TraderRestockSystem.RestockIntervalTurns);Assert.AreEqual(0,inv.Objects.Count);
                TraderRestockSystem.RestockZone(z,TraderRestockSystem.RestockIntervalTurns+1);Assert.Greater(inv.Objects.Count,0);
                Assert.NotNull(z.GetEntityCell(trader));
            }
            finally{TraderPart.Factory=oldFactory;TraderPart.Rng=oldRng;TraderRestockSystem.Factory=oldRestock;LootTableRegistry.ResetForTests();}
        }

        // H9 countercontrols: invented stock properties or an empty Trader
        // declaration cannot turn every Choir creature into a renewing shop.
        [TestCase(false)] [TestCase(true)]
        public void NonmerchantChoirCreatureCannotRenewForgedShopProperties(bool emptyTrader)
        {
            var old=TraderRestockSystem.Factory;
            try
            {
                var f=GrovelandsCompositionTests.Factory();TraderRestockSystem.Factory=f;
                var e=f.CreateEntity("EncasedElder");Assert.IsNull(e.GetPart<TraderPart>());
                if(emptyTrader)e.AddPart(new TraderPart{StockTable=""});
                e.Properties[TraderRestockSystem.ShopStockTableProp]="ChoirStock";
                TradeSystem.SetDrams(e,5);e.SetIntProperty(TraderRestockSystem.LastRestockProp,0);
                var z=new Zone(CathedralCompositionTests.Id(2));z.AddEntity(e,12,12);
                Assert.AreEqual(0,TraderRestockSystem.RestockZone(z,301));
                Assert.AreEqual(5,TradeSystem.GetDrams(e));Assert.AreEqual(0,e.GetPart<InventoryPart>().Objects.Count);
                Assert.AreEqual(0,e.GetIntProperty(TraderRestockSystem.LastRestockProp));
            }
            finally{TraderRestockSystem.Factory=old;}
        }

        [Test] public void LegacyVillagerPurseStillRenewsWithoutANewTraderDeclaration()
        {
            var old=TraderRestockSystem.Factory;
            try
            {
                TraderRestockSystem.Factory=null;var e=GrovelandsCompositionTests.Factory().CreateEntity("EncasedElder");
                Assert.IsNull(e.GetPart<TraderPart>());e.SetTag("Faction","Villagers");TradeSystem.SetDrams(e,5);
                e.SetIntProperty(TraderRestockSystem.LastRestockProp,0);var z=new Zone(CathedralCompositionTests.Id(2));z.AddEntity(e,12,12);
                Assert.AreEqual(0,TraderRestockSystem.RestockZone(z,300));Assert.AreEqual(5,TradeSystem.GetDrams(e));
                Assert.AreEqual(1,TraderRestockSystem.RestockZone(z,301));Assert.AreEqual(TraderRestockSystem.DramsFloor,TradeSystem.GetDrams(e));
                Assert.AreEqual(301,e.GetIntProperty(TraderRestockSystem.LastRestockProp));
            }
            finally{TraderRestockSystem.Factory=old;}
        }

        [TestCase(false)] [TestCase(true)]
        public void ExplicitChoirMerchantStillRespectsFactoryAndCurrencyGates(bool missingPurse)
        {
            var oldFactory=TraderPart.Factory;var oldRestock=TraderRestockSystem.Factory;
            try
            {
                TraderPart.Factory=null;TraderRestockSystem.Factory=null;
                var e=GrovelandsCompositionTests.Factory().CreateEntity("ChoirTendril");
                Assert.AreEqual("ChoirStock",e.GetPart<TraderPart>().StockTable);
                if(missingPurse)e.IntProperties.Remove(TradeSystem.CURRENCY_PROP);else TradeSystem.SetDrams(e,5);
                e.SetIntProperty(TraderRestockSystem.LastRestockProp,0);
                var z=new Zone(CathedralCompositionTests.Id(2));z.AddEntity(e,12,12);
                TraderRestockSystem.RestockZone(z,301);
                Assert.AreEqual(missingPurse?-1:TraderRestockSystem.DramsFloor,e.GetIntProperty(TradeSystem.CURRENCY_PROP,-1));
                Assert.AreEqual(0,e.GetPart<InventoryPart>().Objects.Count,"A missing factory still disables physical shelf refill.");
                Assert.AreEqual(missingPurse?0:301,e.GetIntProperty(TraderRestockSystem.LastRestockProp));
            }
            finally{TraderPart.Factory=oldFactory;TraderRestockSystem.Factory=oldRestock;}
        }

        // H10: renderer scale cannot replace native destruction. Ordinary
        // stone remains removable; the old grown vault exception stays native.
        [Test] public void OrdinaryCliffDestructionOpensOnlyItsOwnedCellAndLeavesNativeRubble()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(CathedralCompositionTests.Id(1));
            Assert.IsTrue(new CathedralCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            var walls=z.GetAllEntities().Where(e=>e.BlueprintName=="SandstoneWall").Take(2).ToArray();var c=z.GetEntityCell(walls[0]);
            var old=DestructionSystem.EntityFactoryRef;
            try
            {
            DestructionSystem.EntityFactoryRef=f;
            Assert.IsTrue(c.BlocksMovement());Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(walls[0],1000,null,z));
            Assert.IsNull(z.GetEntityCell(walls[0]));Assert.NotNull(z.GetEntityCell(walls[1]));Assert.IsFalse(c.BlocksMovement());
            Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName=="Rubble"));
            Assert.IsNull(f.CreateEntity("SubstrateVault").GetPart<DestructiblePart>(),"Composition does not silently rewrite the older native exception.");
            }
            finally{DestructionSystem.EntityFactoryRef=old;}
        }
    }
}
