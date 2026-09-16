using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class FoundingCampRenderingAdversarialTests
    {
        private static SpawnRing3DCatalog Catalog=>Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
        [TestCase("Well","well",typeof(WellSitePart),RepairableSiteType.Well)]
        [TestCase("Oven","oven",typeof(OvenSitePart),RepairableSiteType.HeatStone)]
        [TestCase("WatchLantern","lantern",typeof(LanternSitePart),RepairableSiteType.LightBeacon)]
        public void RealNativeSiteStageSelectsGeometryWithoutLookingAtAnotherWorld(string bp,string family,Type type,RepairableSiteType siteType)
        {
            var f=GrovelandsCompositionTests.Factory();var zone=new Zone("Overworld.8.16.0");var e=f.CreateEntity(bp);zone.AddEntity(e,10,10);
            var part=(Part)Activator.CreateInstance(type);e.AddPart(part);
            foreach(var row in new[]{(RepairStage.Fouled,0),(RepairStage.TemporarilyPurified,1),(RepairStage.StableRepair,2),(RepairStage.ImprovedWithCaretaker,3)})
            {
                SettlementSiteVisuals.ApplyToEntity(e,new RepairableSiteState{Stage=row.Item1,SiteType=siteType});
                Assert.AreEqual("wellmeet-"+family+"-"+row.Item2,SpawnRing3DRecipes.Resolve(zone,e,Catalog).ModelId);
                Assert.AreEqual(row.Item1,type.GetProperty("VisualStage",BindingFlags.Public|BindingFlags.Instance)?.GetValue(part));
            }
            var plain=f.CreateEntity(bp);zone.AddEntity(plain,11,10);
            Assert.AreEqual("wellmeet-"+family+"-2",SpawnRing3DRecipes.Resolve(zone,plain,Catalog).ModelId,"An ordinary fixture cannot inherit a repair site's state.");
        }
        [TestCase("Overworld.4.6.2","FoundingListener")]
        [TestCase("Overworld.4.6.2","FoundingPlaqueTender")]
        [TestCase("Overworld.8.16.0","TentRightHost")]
        [TestCase("Overworld.8.16.0","SaltMaster")]
        [TestCase("Overworld.8.16.0","Villager")]
        public void NativeActorMovesWithoutChangingBodyAndUnrelatedReskinsFallBack(string id,string bp)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(id);var e=f.CreateEntity(bp);z.AddEntity(e,10,10);
            string model=SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId;Assert.NotNull(model);
            z.RemoveEntity(e);z.AddEntity(e,20,10);Assert.AreEqual(model,SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<RenderPart>().RenderString="?";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [TestCase("Overworld.4.6.2","TheRooted")]
        [TestCase("Overworld.4.6.2","FoundingPlume")]
        [TestCase("Overworld.8.16.0","TentWall")]
        [TestCase("Overworld.8.16.0","GuestClothPole")]
        public void StaticNativeOwnersAreNeverRecreatedAfterRemoval(string id,string bp)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(id);var e=f.CreateEntity(bp);z.AddEntity(e,10,10);
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);Assert.IsTrue(r.Batched);Assert.IsFalse(r.Transient);
            z.RemoveEntity(e);int v=z.EntityVersion;Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);Assert.AreEqual(v,z.EntityVersion);
        }
        [TestCase("Well","well","MainWell",RepairMethodId.ManualRepair,SettlementRepairDefinitions.WellMaintenanceManualBlueprint,SettlementRepairDefinitions.SilverSandBlueprint)]
        [TestCase("Oven","oven","VillageOven",RepairMethodId.OvenRebuild,SettlementRepairDefinitions.OvenBuildersGuideBlueprint,SettlementRepairDefinitions.FireClayBlueprint)]
        [TestCase("WatchLantern","lantern","VillageLantern",RepairMethodId.LanternReforge,SettlementRepairDefinitions.LanternOilRecipeBlueprint,SettlementRepairDefinitions.WardOilBlueprint)]
        public void PaidNativeRepairChangesTheActualBatchedModelAndCannotSpendAgain(string bp,string family,string siteId,RepairMethodId method,string manual,string supply)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.8.16.0"))
            {
                var site=f.Zone.GetAllEntities().Single(e=>e.BlueprintName==bp&&e.GetProperty("SettlementSiteId")==siteId);
                var m=f.Manager.SettlementManager;var inventory=f.Player.GetPart<InventoryPart>();
                Assert.IsFalse(m.ApplyRepairMethod(f.Zone.ZoneID,siteId,method,f.Player));
                Assert.IsTrue(inventory.AddObject(f.Factory.CreateEntity(manual)));var item=f.Factory.CreateEntity(supply);Assert.IsTrue(inventory.AddObject(item));
                Assert.IsTrue(f.Find(site,out _,out var original));Assert.AreEqual("wellmeet-"+family+"-0",original);
                int builds=f.Get<int>("GroundBuildCount");Assert.IsTrue(m.ApplyRepairMethod(f.Zone.ZoneID,siteId,method,f.Player));
                Assert.IsFalse(inventory.Objects.Contains(item));Assert.IsTrue(inventory.Objects.Any(e=>e.BlueprintName==manual));
                m.RefreshActiveZonePresentation(f.Zone);f.Refresh(new System.Collections.Generic.HashSet<int>());
                Assert.IsTrue(f.Find(site,out _,out var repaired));Assert.AreEqual("wellmeet-"+family+"-2",repaired);
                Assert.Greater(f.Get<int>("GroundBuildCount"),builds);builds=f.Get<int>("GroundBuildCount");
                Assert.IsFalse(m.ApplyRepairMethod(f.Zone.ZoneID,siteId,method,f.Player));f.Refresh(new System.Collections.Generic.HashSet<int>());
                Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));
            }
        }
        [TestCase("Well",typeof(WellSitePart))] [TestCase("Oven",typeof(OvenSitePart))] [TestCase("WatchLantern",typeof(LanternSitePart))]
        public void DetachedRepairOwnersCannotEmitAurasIntoThePlayersCurrentZone(string bp,Type type)
        {
            var prior=SettlementRuntime.ActiveZone;AsciiFxBus.Clear();
            try
            {
                var zone=new Zone("Overworld.8.16.0");SettlementRuntime.ActiveZone=zone;
                var owner=GrovelandsCompositionTests.Factory().CreateEntity(bp);var part=(Part)Activator.CreateInstance(type);owner.AddPart(part);
                var change=type.GetMethod("OnStageChanged");change.Invoke(part,new object[]{RepairStage.Fouled});
                Assert.AreEqual(0,AsciiFxBus.PendingCount,"A detached generated owner does not belong to the active world.");
                zone.AddEntity(owner,10,10);change.Invoke(part,new object[]{RepairStage.StableRepair});
                Assert.AreEqual(1,AsciiFxBus.PendingCount,"A real current owner still starts its native aura.");
            }
            finally{SettlementRuntime.ActiveZone=prior;AsciiFxBus.Clear();}
        }
        [Test] public void DisposableNativePreviewManagerNeverPublishesAnotherSettlementsRegistry()
        {
            var factory=GrovelandsCompositionTests.Factory();var live=new OverworldZoneManager(factory,64);
            var old=SettlementManager.Current;
            var method=typeof(OverworldZoneManager).GetMethod("CreateDetached",BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method);
            var preview=(OverworldZoneManager)method.Invoke(null,new object[]{factory,1729});
            Assert.AreSame(old,SettlementManager.Current);Assert.AreNotSame(live.SettlementManager,preview.SettlementManager);
            preview.GetZone("Overworld.8.16.0");Assert.AreSame(old,SettlementManager.Current);
            var normal=new OverworldZoneManager(factory,1);Assert.AreSame(normal.SettlementManager,SettlementManager.Current);
        }
        [TestCase("GinFrog")] [TestCase("PrickleBrowGecko")]
        public void ARealCaveFollowerKeepsItsPortableBodyAndReskinGuardInWellmeet(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();var home=new Zone("Overworld.4.6.1");var camp=new Zone("Overworld.8.16.0");
            var actor=f.CreateEntity(bp);home.AddEntity(actor,20,10);var native=SpawnRing3DRecipes.Resolve(home,actor,Catalog);Assert.IsTrue(native.Transient);
            home.RemoveEntity(actor);camp.AddEntity(actor,30,11);var arrived=SpawnRing3DRecipes.Resolve(camp,actor,Catalog);
            Assert.IsTrue(arrived.Transient);Assert.IsFalse(arrived.Batched);Assert.AreEqual(native.ModelId,arrived.ModelId);
            actor.GetPart<RenderPart>().RenderString="?";Assert.IsNull(SpawnRing3DRecipes.Resolve(camp,actor,Catalog).ModelId);
            var ledge=f.CreateEntity("DescentLedge");camp.AddEntity(ledge,20,10);var still=SpawnRing3DRecipes.Resolve(camp,ledge,Catalog);
            Assert.IsFalse(still.Transient);Assert.IsTrue(still.Batched);
        }
        [TestCase("Overworld.4.6.2","FoundingPlume")]
        [TestCase("Overworld.8.16.0","TentWall")]
        public void ActualNativeDestructionRemovesOnlyTheOwnersBatchedGeometry(string id,string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                f.Zone=new Zone(id);f.Bind(f.Zone);f.Set("FullReveal",true);
                f.Add("StoneFloor",20,10);f.Add("StoneFloor",21,10);
                var victim=f.Add(blueprint,20,10);var neighbor=f.Add(blueprint,21,10);f.Refresh();
                Assert.IsTrue(f.Find(victim,out _,out _));Assert.IsTrue(f.Find(neighbor,out _,out _));int revision=f.Revision(20,10);
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(victim,1000,null,f.Zone));
                f.Refresh(new System.Collections.Generic.HashSet<int>());Assert.IsFalse(f.Find(victim,out _,out _));Assert.IsTrue(f.Find(neighbor,out _,out _));
                Assert.Greater(f.Revision(20,10),revision);Assert.IsNull(f.Zone.GetEntityCell(victim));
                revision=f.Revision(20,10);f.Refresh(new System.Collections.Generic.HashSet<int>());Assert.AreEqual(revision,f.Revision(20,10));
            }
        }
        [TestCase("WellGroundMarker")] [TestCase("OvenGroundMarker")] [TestCase("LanternGroundMarker")] [TestCase("CampfireGroundMarker")]
        public void ActivityGroundMarksRemainLowCurrentNativePads(string bp)
        {
            var z=new Zone("Overworld.8.16.0");var e=GrovelandsCompositionTests.Factory().CreateEntity(bp);z.AddEntity(e,10,10);
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);StringAssert.StartsWith("wellmeet-marker-",r.ModelId);
            Assert.IsTrue(r.Batched);Assert.IsFalse(r.Transient);
            var mesh=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).FindModel(r.ModelId).GetComponent<MeshFilter>().sharedMesh;
            Assert.LessOrEqual(mesh.bounds.max.y,.046f);
            z.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [Test] public void FoundingRoomUsesLowCutawayStoneWhileDescentKeepsItsCliff()
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(var id in new[]{"Overworld.4.6.2","Overworld.4.6.1"})
            {
                var z=new Zone(id);var wall=f.CreateEntity("SandstoneWall");z.AddEntity(wall,10,10);
                var r=SpawnRing3DRecipes.Resolve(z,wall,Catalog);
                Assert.NotNull(r.ModelId);var prefab=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).FindModel(r.ModelId);
                var height=prefab.GetComponent<MeshFilter>().sharedMesh.bounds.max.y;
                if(id.EndsWith(".2",StringComparison.Ordinal)){Assert.LessOrEqual(height,1.11f);StringAssert.StartsWith("olderdeep-wall-",r.ModelId);}else Assert.Greater(height,1.4f);
                Assert.IsTrue(wall.GetPart<PhysicsPart>().Solid);Assert.AreEqual(1,z.EntityCount);
            }
        }
        [TestCase(1,0,0,-1,0)] [TestCase(1,0,0,1,1)]
        [TestCase(-1,0,0,1,2)] [TestCase(-1,0,0,-1,3)]
        public void NativeTentCornersJoinBothExistingArms(int dx1,int dy1,int dx2,int dy2,int turns)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var wall=f.CreateEntity("TentWall");z.AddEntity(wall,20,10);
            z.AddEntity(f.CreateEntity("TentWall"),20+dx1,10+dy1);z.AddEntity(f.CreateEntity("TentWall"),20+dx2,10+dy2);
            var r=SpawnRing3DRecipes.Resolve(z,wall,Catalog);StringAssert.StartsWith("wellmeet-corner-",r.ModelId);Assert.AreEqual(turns,r.QuarterTurns);
        }
        [Test] public void TentRunsFollowCurrentNativeNeighborsAndReorientAfterDestruction()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");
            var wall=f.CreateEntity("TentWall");var n=f.CreateEntity("TentWall");var south=f.CreateEntity("TentWall");
            z.AddEntity(wall,20,10);z.AddEntity(n,20,9);z.AddEntity(south,20,11);
            Assert.AreEqual(1,SpawnRing3DRecipes.Resolve(z,wall,Catalog).QuarterTurns);
            z.RemoveEntity(n);z.RemoveEntity(south);var east=f.CreateEntity("TentWall");z.AddEntity(east,21,10);
            Assert.AreEqual(0,SpawnRing3DRecipes.Resolve(z,wall,Catalog).QuarterTurns);
        }
        [TestCase(true)] [TestCase(false)]
        public void PilgrimAppearanceRequiresItsNativeQuest(bool nativeQuest)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var e=f.CreateEntity("Villager");z.AddEntity(e,10,10);
            e.GetPart<RenderPart>().RenderString="p";
            e.AddPart(new CavesOfOoo.Storylets.QuestBeaconPart{Quest=nativeQuest?"HiddenShrine":"different-quest"});
            Assert.AreEqual(nativeQuest,SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId!=null);
        }
        [TestCase(true)] [TestCase(false)]
        public void OnlyTheRealPanickedQuestVillagerMayUseThePGlyph(bool quest)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var e=f.CreateEntity("Villager");z.AddEntity(e,10,10);
            e.GetPart<RenderPart>().RenderString="p";
            if(quest)e.AddPart(new CavesOfOoo.Storylets.QuestBeaconPart{Quest="StrongestInOoo"});
            Assert.AreEqual(quest,SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId!=null);
        }
        [TestCase(0,-1,2)] [TestCase(0,1,0)] [TestCase(-1,0,1)] [TestCase(1,0,3)]
        public void NicheOpeningFollowsActualAdjacentBackingWall(int dx,int dy,int turn)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.4.6.2");var niche=f.CreateEntity("NicheHome");z.AddEntity(niche,20,10);
            var wall=f.CreateEntity("SandstoneWall");z.AddEntity(wall,20+dx,10+dy);
            Assert.AreEqual(turn,SpawnRing3DRecipes.Resolve(z,niche,Catalog).QuarterTurns);
            z.RemoveEntity(wall);Assert.AreEqual(0,SpawnRing3DRecipes.Resolve(z,niche,Catalog).QuarterTurns);
        }
    }
}
