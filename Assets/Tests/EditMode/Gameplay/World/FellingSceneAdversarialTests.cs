using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Deliberate player-flow and parser hypotheses: a repeated clear, forged
    // owner, dead/detached actor or stale saved marker must not change scenery.
    public sealed class FellingSceneAdversarialTests
    {
        private EntityFactory factory;
        [SetUp] public void Setup()
        {factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));}
        [TestCase(null)] [TestCase("")] [TestCase(" ")] [TestCase("{}")] [TestCase("[]")]
        public void EmptyOrIncompleteDefinitionIsRejected(string json)
            =>Assert.Throws<ArgumentException>(()=>FellingSceneDefinition.Parse(json));
        [TestCase("canvas")] [TestCase("ppu")] [TestCase("origin")] [TestCase("offset")]
        [TestCase("missing-layer")] [TestCase("missing-cell")] [TestCase("mutable-count")]
        [TestCase("anchor")] [TestCase("bounds")] [TestCase("contact")]
        [TestCase("landmark")] [TestCase("missing-landmark")]
        public void MalformedDomainDataCannotPartiallyInstall(string field)
        {
            var d=JsonUtility.FromJson<FellingSceneDefinition>(JsonUtility.ToJson(FellingSceneDefinition.Load()));
            switch(field)
            {
                case "canvas":d.canvasWidth=0;break;case "ppu":d.pixelsPerCell=16;break;case "origin":d.originX=0;break;case "offset":d.groundOffset=0;break;
                case "missing-layer":d.layers=d.layers.Skip(1).ToArray();break;case "missing-cell":d.cells=d.cells.Skip(1).ToArray();break;
                case "mutable-count":d.layers.First(l=>l.mutable).mutable=false;break;case "anchor":d.layers[0].anchorX=80;break;
                case "bounds":d.layers[0].bounds=new[]{0,0,-1,1024};break;case "contact":d.layers[0].contactResource="SceneArt/FellingSite/../outside";break;
                case "landmark":d.landmarks[0].x=80;break;case "missing-landmark":d.landmarks=d.landmarks.Skip(1).ToArray();break;
            }
            Assert.Throws<ArgumentException>(d.Validate);
        }
        [Test] public void FixedOwnersDoNotDeclareClearAndHaveNoDestructibleBackdoor()
        {
            var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);
            foreach(var layer in FellingSceneDefinition.Load().layers.Where(l=>!l.mutable))
            {var e=FellingSceneRuntime.FindOwner(z,layer.id);Assert.IsFalse(WorldInteractionSystem.GatherActions(e).Any(a=>a.Command==FellingScenePropPart.ClearCommand));Assert.IsFalse(e.HasPart<DestructiblePart>());Assert.IsFalse(e.GetPart<PhysicsPart>().Takeable);}
        }
        [Test] public void SourceOwnersRemainIneligibleForNormalBreakTakeHaulAndLivingKnockback()
        {
            var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);var actor=factory.CreateEntity("Player");
            foreach(var layer in FellingSceneDefinition.Load().layers)
            {
                var owner=FellingSceneRuntime.FindOwner(z,layer.id);
                Assert.IsFalse(DestructionSystem.IsBreakable(owner));Assert.AreEqual(DragVerdict.Rooted,DragRules.CanDrag(actor,owner));
                Assert.AreEqual(0,owner.GetStatValue("Hitpoints",0),"Shove spells guard on surviving Hitpoints before pushing.");Assert.IsFalse(owner.HasTag("Creature"));
                Assert.IsFalse(WorldInteractionSystem.GatherActions(owner,actor).Any(a=>a.Command=="Take"||a.Command=="Break"||a.Command=="Throw"));
            }
        }
        [Test] public void ForgedDuplicateComponentPartCannotClearTheRealOwner()
        {
            var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable);
            var fake=new Entity{ID=Guid.NewGuid().ToString("N")};fake.AddPart(new FellingScenePropPart{ComponentId=layer.id,Mutable=true});z.AddEntity(fake,40,20);
            var actor=factory.CreateEntity("Player");z.AddEntity(actor,40,20);
            Assert.IsFalse(fake.GetPart<FellingScenePropPart>().TryClear(actor,z));Assert.IsTrue(FellingSceneRuntime.IsPresent(z,layer.id));
        }
        [Test] public void OwnerInADifferentSceneInstanceCannotBeClearedBySameCoordinates()
        {
            var a=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);var b=new OverworldZoneManager(factory,65).GetZone(FellingSiteBuilder.ZoneID);
            var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable);var owner=FellingSceneRuntime.FindOwner(a,layer.id);var actor=factory.CreateEntity("Player");b.AddEntity(actor,layer.anchorX,layer.anchorY);
            Assert.IsFalse(owner.GetPart<FellingScenePropPart>().TryClear(actor,b));Assert.IsTrue(FellingSceneRuntime.IsPresent(a,layer.id));Assert.IsTrue(FellingSceneRuntime.IsPresent(b,layer.id));
        }
        [Test] public void SavedMutableFlagCannotTurnTheMonumentIntoAClearableProp()
        {
            var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);var layer=FellingSceneDefinition.Load().layers.First(l=>!l.mutable);var owner=FellingSceneRuntime.FindOwner(z,layer.id);owner.GetPart<FellingScenePropPart>().Mutable=true;
            var actor=factory.CreateEntity("Player");z.AddEntity(actor,layer.anchorX,layer.anchorY);
            Assert.IsFalse(owner.GetPart<FellingScenePropPart>().TryClear(actor,z));Assert.IsTrue(FellingSceneRuntime.IsPresent(z,layer.id));
        }
        [Test] public void DisplacedOwnerCannotBecomeARemoteControlForItsUnmovedArtwork()
        {
            var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable);var owner=FellingSceneRuntime.FindOwner(z,layer.id);z.MoveEntity(owner,79,24);
            var actor=factory.CreateEntity("Player");z.AddEntity(actor,79,24);
            Assert.IsFalse(owner.GetPart<FellingScenePropPart>().TryClear(actor,z));Assert.IsTrue(FellingSceneRuntime.IsPresent(z,layer.id));
        }
        [Test] public void SceneStateIsCarriedByOrdinaryGroundAndDoesNotExposeAnImplementationObject()
        {
            var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);var marker=FellingSceneRuntime.GetState(z).ParentEntity;
            Assert.IsTrue(marker.HasPart<ExaminablePart>());Assert.IsTrue(marker.HasTag(FellingSceneRuntime.TerrainTag));
            Assert.IsFalse(WorldInteractionSystem.BuildTargetPickerActions(z.GetCell(0,0)).Any(a=>a.Display.Contains("FellingScene")));
        }
        [Test] public void NullAndUnknownLookupsAreSafeAndDoNotInventPresence()
        {
            Assert.IsFalse(FellingSceneRuntime.IsActive(null));Assert.IsFalse(FellingSceneRuntime.IsPresent(null,"missing"));Assert.IsNull(FellingSceneRuntime.FindOwner(null,null));
            var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);Assert.IsFalse(FellingSceneRuntime.IsPresent(z,"missing"));Assert.IsNull(FellingSceneRuntime.FindOwner(z,"missing"));
        }
        [Test] public void RejectedClearEmitsOnlyRejectionAndDoesNotDirtyTheCell()
        {
            bool previous=Diag.IsChannelEnabled("furniture");Diag.SetChannel("furniture",true);int dirties=0;var old=ZoneRenderHooks.CellDirtyCallback;ZoneRenderHooks.CellDirtyCallback=(x,y,s)=>dirties++;
            try
            {
                var z=new OverworldZoneManager(factory,64).GetZone(FellingSiteBuilder.ZoneID);var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable);var owner=FellingSceneRuntime.FindOwner(z,layer.id);int before=DiagQuery.Count(new DiagQuery.Filter{Category="furniture",Kind="FellingClearRejected"}).Count;
                Assert.IsFalse(owner.GetPart<FellingScenePropPart>().TryClear(null,z));Assert.AreEqual(0,dirties);Assert.AreEqual(before+1,DiagQuery.Count(new DiagQuery.Filter{Category="furniture",Kind="FellingClearRejected"}).Count);
            }
            finally{ZoneRenderHooks.CellDirtyCallback=old;Diag.SetChannel("furniture",previous);}
        }
        [TestCase("TepuiStone")] [TestCase("FellingBarePosition")] [TestCase("SeventhPosition")]
        public void MissingFactoryContentCannotEraseLegacyEntities(string missing)
        {
            var z=new Zone(FellingSiteBuilder.ZoneID);var item=factory.CreateEntity("Tepuibone");z.AddEntity(item,40,20);factory.Blueprints.Remove(missing);
            Assert.IsFalse(FellingSceneRuntime.UpgradeCachedZone(z,factory));CollectionAssert.AreEqual(new[]{item},z.GetAllEntities());
        }
        [Test] public void DifferentSavedPoiIsNotSilentlyUpgraded()
        {
            var manager=new OverworldZoneManager(factory,64);manager.WorldMap.SetPOI(3,5,new PointOfInterest((POIType)999,"Saved place"));
            var zone=manager.GetZone(FellingSiteBuilder.ZoneID);Assert.IsFalse(FellingSceneRuntime.IsActive(zone));Assert.IsFalse(zone.GetAllEntities().Any(e=>e.HasPart<FellingScenePropPart>()));
        }
    }
}
