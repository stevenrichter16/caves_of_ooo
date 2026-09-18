using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class MultiCellPilotRuntimeTests
    {
        private EntityFactory factory;
        [OneTimeSetUp] public void LoadNativeBlueprints()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }

        private Zone Build()
        {
            var zone = new Zone(MultiCellPilotRuntime.ZoneID);
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone, factory));
            return zone;
        }

        [Test]
        public void GenerationInstallsExactSouthChunkWithEveryAuthoredOwnerAndGroundCell()
        {
            var zone = new OverworldZoneManager(factory, 64).GetZone(MultiCellPilotRuntime.ZoneID);
            Assert.NotNull(zone); Assert.IsTrue(MultiCellPilotRuntime.IsActive(zone));
            var owners = zone.GetAllEntities().Where(e => e.HasPart<MultiCellPilotPropPart>()).ToArray();
            Assert.AreEqual(149, owners.Length);
            Assert.AreEqual(149, owners.Select(e => e.GetPart<MultiCellPilotPropPart>().OwnerId).Distinct().Count());
            Assert.AreEqual(2000, zone.GetAllEntities().Count(e => e.BlueprintName == "TepuiStone"));
            int solid = 0;
            for (int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++)
            {
                if (zone.GetCell(x,y).BlocksMovement()) solid++;
                Assert.AreEqual(1, zone.GetCell(x,y).Objects.Count(e => e.BlueprintName == "TepuiStone"));
            }
            Assert.AreEqual(421, solid);
            Assert.IsFalse(zone.GetCell(40,0).BlocksMovement());
            Assert.IsFalse(zone.GetCell(40,24).BlocksMovement());
            Assert.AreEqual("Overworld.3.7.0", MultiCellPilotRuntime.ZoneID);
        }

        [Test]
        public void OtherZonesAndUndergroundAreNotEligibleForPilotInstall()
        {
            foreach (string id in new[]{"Overworld.3.6.0","Overworld.4.7.0","Overworld.3.7.1"})
            {
                var zone=new Zone(id); var sentinel=new Entity{ID="sentinel"}; zone.AddEntity(sentinel,10,10);
                Assert.IsFalse(MultiCellPilotRuntime.Ensure(zone,factory));
                Assert.AreEqual(1,zone.EntityCount); Assert.AreSame(sentinel,zone.GetCell(10,10).Objects[0]);
            }
        }

        [Test]
        public void EverySceneryOwnerHasOnePhysicalDestructibleMaterialBody()
        {
            var zone=Build();
            foreach(var owner in zone.GetAllEntities().Where(e=>e.HasPart<MultiCellPilotPropPart>()))
            {
                var part=owner.GetPart<MultiCellPilotPropPart>();
                Assert.IsNotEmpty(part.OwnerId); Assert.IsNotEmpty(part.ModelId);
                Assert.IsNotNull(owner.GetPart<SpatialFootprintPart>());
                if(part.Role=="actor") { Assert.IsTrue(owner.HasTag("Creature")); continue; }
                Assert.IsNotNull(owner.GetPart<DestructiblePart>(),part.OwnerId);
                Assert.IsNotNull(owner.GetPart<MaterialPart>(),part.OwnerId);
                Assert.IsNotNull(owner.GetPart<ThermalPart>(),part.OwnerId);
                Assert.IsFalse(owner.GetPart<DestructiblePart>().Indestructible,part.OwnerId);
                Assert.IsFalse(owner.GetPart<PhysicsPart>().Takeable,part.OwnerId);
            }
            var ridge=MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2");
            Assert.IsFalse(zone.GetCell(6,2).Occupants.Contains(ridge),"authored anchor hole stays empty");
            Assert.IsTrue(zone.GetCell(8,2).Occupants.Contains(ridge));
            Assert.AreEqual(10,zone.GetOccupiedCells(ridge).Count);
        }

        [Test]
        public void DestroyedOwnersAndAllOwnersGoneCannotBeResurrectedByEnsure()
        {
            var zone=Build();var ridge=MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2");
            Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Destroy(ridge,null,zone,"test"));
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,factory));
            Assert.IsNull(MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2"));
            foreach(var owner in zone.GetAllEntities().Where(e=>e.HasPart<MultiCellPilotPropPart>()).ToArray()) zone.RemoveEntity(owner);
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,factory));
            Assert.IsTrue(MultiCellPilotRuntime.IsActive(zone));
            Assert.AreEqual(0,zone.GetAllEntities().Count(e=>e.HasPart<MultiCellPilotPropPart>()));
            Assert.AreEqual(2000,zone.EntityCount,"state rides protected ground, never an invisible picker entity");
        }

        [Test]
        public void MissingBlueprintFailsBeforeMutatingLegacyWorld()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var old=new Entity{ID="old"};zone.AddEntity(old,10,10);
            Assert.IsFalse(MultiCellPilotRuntime.Ensure(zone,new EntityFactory(),preserveExisting:true));
            Assert.AreEqual(1,zone.EntityCount);Assert.AreSame(old,zone.GetCell(10,10).Objects[0]);
            Assert.IsFalse(MultiCellPilotRuntime.IsActive(zone));
        }

        [Test]
        public void UpgradePreservesActorsLootAndConflictingContainerWithoutMovingThem()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);
            var actor=factory.CreateEntity("Wardline");var loot=factory.CreateEntity("Dagger");
            var chest=factory.CreateEntity("Crate");var floor=factory.CreateEntity("Grass");
            Assert.NotNull(actor);Assert.NotNull(loot);Assert.NotNull(chest);Assert.NotNull(floor);
            zone.AddEntity(actor,40,12);zone.AddEntity(loot,40,12);zone.AddEntity(chest,8,2);zone.AddEntity(floor,41,12);
            var actorId=actor.ID;var lootId=loot.ID;var chestId=chest.ID;
            Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.AreEqual((40,12),zone.GetEntityPosition(actor));Assert.AreEqual(actorId,actor.ID);
            Assert.AreEqual((40,12),zone.GetEntityPosition(loot));Assert.AreEqual(lootId,loot.ID);
            Assert.AreEqual((8,2),zone.GetEntityPosition(chest));Assert.AreEqual(chestId,chest.ID);
            Assert.IsNull(zone.GetEntityCell(floor));
            Assert.IsNull(MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2"),"saved content wins an authored footprint conflict");
            zone.RemoveEntity(chest);Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,factory));
            Assert.IsNull(MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2"),"skipped owners are not respawned on the next visit");
        }

        [Test]
        public void UpgradeAdoptsExistingHermitIdentityInventoryAndConversation()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var hermit=factory.CreateEntity("CaveHermit");
            string id=hermit.ID;var inventory=hermit.GetPart<InventoryPart>();
            var keepsake=factory.CreateEntity("Dagger");inventory.AddObject(keepsake);zone.AddEntity(hermit,40,12);
            Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.AreSame(hermit,MultiCellPilotRuntime.FindOwner(zone,"ridge-hermit"));
            Assert.AreEqual(id,hermit.ID);Assert.AreEqual((40,12),zone.GetEntityPosition(hermit));
            Assert.IsTrue(inventory.Objects.Contains(keepsake));Assert.NotNull(hermit.GetPart<ConversationPart>());
            Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="CaveHermit"));
        }

        [Test]
        public void MovableCopperPipeCanBeHauledButCannotEnterInventoryOrThrowFlow()
        {
            var zone=Build();var pipe=MultiCellPilotRuntime.FindOwner(zone,"movable-copper-pipe");
            var actor=factory.CreateEntity("Wardline");actor.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=16,Owner=actor};
            Assert.IsFalse(pipe.GetPart<PhysicsPart>().Takeable);
            var handling=pipe.GetPart<HandlingPart>();Assert.NotNull(handling);
            Assert.IsFalse(handling.Carryable);Assert.IsFalse(handling.Throwable);
            Assert.AreEqual(DragVerdict.Ok,DragRules.CanDrag(actor,pipe));
            Assert.AreEqual(3,zone.GetOccupiedCells(pipe).Count);
            var ridge=MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2");
            Assert.AreEqual(DragVerdict.Rooted,DragRules.CanDrag(actor,ridge));
        }

        [Test]
        public void LargeMawToadPreservesOneNativeCombatBrainAndAmbushBehavior()
        {
            var zone=Build();var toad=MultiCellPilotRuntime.FindOwner(zone,"large-maw-toad");
            Assert.AreEqual("MawToad",toad.BlueprintName);Assert.AreEqual(4,zone.GetOccupiedCells(toad).Count);
            Assert.AreEqual(1,toad.Parts.Count(p=>p is BrainPart));
            Assert.IsTrue(toad.GetPart<BrainPart>().Staying);Assert.IsFalse(toad.GetPart<BrainPart>().Wanders);
            Assert.AreEqual(22,toad.GetStatValue("Hitpoints"));Assert.NotNull(toad.GetPart<CorpsePart>());
            Assert.IsTrue(EnvironmentSpriteRenderer.CreatureSprites.Any(r=>r.Blueprint=="MawToad"&&r.File=="maw_toad"));
            var sprite=Resources.Load<Sprite>("Sprites/Environment/maw_toad");Assert.NotNull(sprite);
            Assert.AreEqual(16,sprite.rect.width);Assert.AreEqual(16,sprite.rect.height);
        }

        [Test]
        public void PilotPartAndStateRoundTripWithTheNativePrimitiveFieldSerializer()
        {
            var zone=Build();var pipe=MultiCellPilotRuntime.FindOwner(zone,"movable-copper-pipe");
            pipe.GetPart<DestructiblePart>().HP=17;
            var loaded=PartRoundTripHelper.RoundTripEntity(pipe);
            Assert.AreEqual("movable-copper-pipe",loaded.GetPart<MultiCellPilotPropPart>().OwnerId);
            Assert.AreEqual("PilotPipe_0",loaded.GetPart<MultiCellPilotPropPart>().ModelId);
            Assert.AreEqual("movable",loaded.GetPart<MultiCellPilotPropPart>().Role);
            Assert.AreEqual(17,loaded.GetPart<DestructiblePart>().HP);
            var state=zone.GetCell(0,0).Objects.Single(e=>e.HasPart<MultiCellPilotStatePart>());
            var stateCopy=PartRoundTripHelper.RoundTripEntity(state);
            Assert.AreEqual(1,stateCopy.GetPart<MultiCellPilotStatePart>().Revision);
        }

        [Test]
        public void WorldMapAndElevationLabelOnlyTheSouthSpurAsFoothills()
        {
            Assert.AreEqual(BiomeType.Stump,WorldMapAuthoring.BiomeAt(3,7));
            Assert.AreEqual(StumpBand.Foothills,StumpBands.BandAt(3,7));
            Assert.AreEqual(3,WorldMapAuthoring.TierAt(3,7));
            foreach(var point in new[]{(3,6),(2,7),(4,7),(3,8)})
            {
                Assert.AreEqual(BiomeType.Grovelands,WorldMapAuthoring.BiomeAt(point.Item1,point.Item2));
                Assert.AreEqual(StumpBand.None,StumpBands.BandAt(point.Item1,point.Item2));
            }
        }

        [Test]
        public void AccessRepairsLegacyFenMapLabelWithoutChangingNeighborOrSavedPoi()
        {
            var manager=new OverworldZoneManager(factory,64);
            manager.WorldMap.Tiles[3,7]=BiomeType.Grovelands;
            var poi=new PointOfInterest(POIType.Lair,"saved landmark");manager.WorldMap.SetPOI(3,7,poi);
            var neighbor=manager.WorldMap.GetBiome(3,8);
            Assert.IsNotNull(manager.GetZone(MultiCellPilotRuntime.ZoneID));
            Assert.AreEqual(BiomeType.Stump,manager.WorldMap.GetBiome(3,7));
            Assert.AreEqual(neighbor,manager.WorldMap.GetBiome(3,8));
            Assert.AreSame(poi,manager.WorldMap.GetPOI(3,7));
        }

        [TestCase("CopperPipe","copper_pipe")]
        [TestCase("TarSeep","tar_seep")]
        [TestCase("SteamVent","steam_vent")]
        public void PreviouslyGlyphOnlyPilotFixturesHaveNativeSixteenPixelArt(string blueprint,string file)
        {
            Assert.IsTrue(EnvironmentSpriteRenderer.FixtureSprites.Any(row=>row.Blueprint==blueprint&&row.File==file));
            var sprite=Resources.Load<Sprite>("Sprites/Environment/"+file);Assert.NotNull(sprite);
            Assert.AreEqual(16,sprite.rect.width);Assert.AreEqual(16,sprite.rect.height);
        }

        [Test]
        public void NewDurabilityAndHandlingAreLocalToPilotInstances()
        {
            Build();
            Assert.IsNull(factory.CreateEntity("GrainRidge").GetPart<DestructiblePart>());
            Assert.IsNull(factory.CreateEntity("CopperPipe").GetPart<DestructiblePart>());
            Assert.IsNull(factory.CreateEntity("CopperPipe").GetPart<HandlingPart>());
        }
    }
}
