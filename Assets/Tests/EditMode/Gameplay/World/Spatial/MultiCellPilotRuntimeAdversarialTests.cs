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
    /// <summary>Migration, ownership and persistence hypotheses beyond the authored layout's happy path.</summary>
    public class MultiCellPilotRuntimeAdversarialTests
    {
        private EntityFactory factory;
        [OneTimeSetUp] public void LoadNativeBlueprints()
        {
            factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
        }
        private Entity Native(string blueprint)
        { var result=factory.CreateEntity(blueprint);Assert.NotNull(result,blueprint);return result; }
        private Zone Build()
        { var zone=new Zone(MultiCellPilotRuntime.ZoneID);Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,factory));return zone; }
        private static Diag.Entry[] Records(string kind)
            => DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="MultiCellPilot"+kind,Limit=100}).Records.ToArray();

        [Test] public void SavedTerrainWithInventoryKeepsContentsAndIdentityWhileEmptyTerrainIsReplaced()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var occupied=Native("Rock");var empty=Native("Rock");
            var inventory=new InventoryPart();occupied.AddPart(inventory);var heirloom=Native("Dagger");inventory.AddObject(heirloom);
            Assert.IsTrue(zone.AddEntity(occupied,40,12));Assert.IsTrue(zone.AddEntity(empty,41,12));
            Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.AreSame(zone.GetCell(40,12),zone.GetEntityCell(occupied));Assert.Contains(heirloom,inventory.Objects);
            Assert.IsNull(zone.GetEntityCell(empty));
        }

        [Test] public void LegacyHermitStackedWithSavedSolidIsPreservedWithoutPartialAdoptionOrThrow()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var hermit=Native("CaveHermit");var chest=Native("Crate");
            chest.GetPart<PhysicsPart>().Solid=true;
            Assert.IsTrue(zone.AddEntity(hermit,40,12));Assert.IsTrue(zone.AddEntity(chest,40,12));
            Assert.IsFalse(zone.CanPlaceFootprint(hermit,40,12),"the legacy single-cell stack is a real migration conflict");
            Assert.DoesNotThrow(()=>Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory)));
            Assert.IsTrue(MultiCellPilotRuntime.IsActive(zone));Assert.AreEqual((40,12),zone.GetEntityPosition(hermit));
            Assert.AreEqual((40,12),zone.GetEntityPosition(chest));Assert.IsNull(hermit.GetPart<MultiCellPilotPropPart>());
            Assert.IsNull(hermit.GetPart<SpatialFootprintPart>());Assert.IsNull(MultiCellPilotRuntime.FindOwner(zone,"ridge-hermit"));
            StringAssert.Contains("ridge-hermit",MultiCellPilotRuntime.GetState(zone).SkippedOwnerIds);
        }

        [Test] public void MultipleSavedHermitsRemainDistinctAndOnlyOneIsAdopted()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var first=Native("CaveHermit");var second=Native("CaveHermit");
            Assert.IsTrue(zone.AddEntity(first,40,12));Assert.IsTrue(zone.AddEntity(second,42,12));
            Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.AreEqual(2,zone.GetAllEntities().Count(e=>e.BlueprintName=="CaveHermit"));
            Assert.AreEqual(1,new[]{first,second}.Count(e=>e.HasPart<MultiCellPilotPropPart>()));
            Assert.AreEqual((40,12),zone.GetEntityPosition(first));Assert.AreEqual((42,12),zone.GetEntityPosition(second));
            Assert.AreNotEqual(first.ID,second.ID);
        }

        [Test] public void ExistingWritingSurvivesUpgradeAndRepeatedEnsureWithoutRewettingClearedTiles()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);
            zone.TileState.WriteCoating(40,12,"water",90);zone.TileState.WriteCoating(41,12,"oil",17);zone.TileState.AddHeat(41,12,7);
            Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.AreEqual(90,zone.TileState.CoatingTurns(40,12,"water"));Assert.AreEqual(17,zone.TileState.CoatingTurns(41,12,"oil"));
            Assert.AreEqual(ZoneTileState.MaxEnergy,zone.TileState.Heat(41,12));zone.TileState.RemoveCoating(40,12,"water");
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,factory));Assert.IsFalse(zone.TileState.HasCoating(40,12,"water"));
        }

        [Test] public void FreshGroundHasNoAmbientFenWaterButNativeTarStartsOily()
        {
            var zone=Build();Assert.IsFalse(zone.TileState.HasCoating(40,12,"water"));
            var tar=zone.GetAllEntities().Where(e=>e.BlueprintName=="TarSeep").ToArray();Assert.AreEqual(2,tar.Length);
            foreach(var seep in tar) Assert.NotNull(seep.GetPart<TileStateSourcePart>());
            var wetCells=tar.SelectMany(seep=>zone.GetOccupiedCells(seep)).Distinct().ToArray();
            Assert.AreEqual(12,wetCells.Length,"Two native 2x3 tar bodies occupy twelve physical cells.");
            foreach(var cell in wetCells)
                Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(cell.X,cell.Y,"oil"),
                    "Native LiquidPool immediately projects every physical cell, not just its anchor.");
            int oily=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(zone.TileState.HasCoating(x,y,"oil")){oily++;Assert.Contains(zone.GetCell(x,y),wetCells);}
            Assert.AreEqual(wetCells.Length,oily);
        }

        [Test] public void OrphanOwnerWithoutInstallMarkerRefusesBeforeDeletingAnything()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var orphan=Native("Rock");
            orphan.AddPart(new MultiCellPilotPropPart{OwnerId="saved-owner",ModelId="PilotRock_0"});
            Assert.IsTrue(zone.AddEntity(orphan,40,12));var before=zone.EntityCount;
            Assert.IsFalse(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.AreEqual(before,zone.EntityCount);Assert.AreSame(zone.GetCell(40,12),zone.GetEntityCell(orphan));
            Assert.IsFalse(MultiCellPilotRuntime.IsActive(zone));
        }

        [Test] public void UnknownInstallRevisionRefusesWithoutRewritingOrRestampingOwners()
        {
            var zone=Build();var before=zone.EntityCount;var owner=MultiCellPilotRuntime.FindOwner(zone,"movable-copper-pipe");
            MultiCellPilotRuntime.GetState(zone).Revision=99;
            Assert.IsFalse(MultiCellPilotRuntime.Ensure(zone,factory));Assert.AreEqual(before,zone.EntityCount);
            Assert.AreEqual(99,MultiCellPilotRuntime.GetState(zone).Revision);Assert.IsNotNull(zone.GetEntityCell(owner));
            Assert.IsFalse(MultiCellPilotRuntime.IsActive(zone));
        }

        [Test] public void InstalledStatePreservesDamageEvenWhenFactoryNoLongerHasBlueprints()
        {
            var zone=Build();var ridge=MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2");ridge.GetPart<DestructiblePart>().HP=13;
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,new EntityFactory()));
            Assert.AreSame(ridge,MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2"));Assert.AreEqual(13,ridge.GetPart<DestructiblePart>().HP);
            Assert.IsFalse(MultiCellPilotRuntime.Ensure(new Zone(MultiCellPilotRuntime.ZoneID),new EntityFactory()));
        }

        [Test] public void OneMissingLateBlueprintRefusesBeforeReplacingSavedTerrain()
        {
            var incomplete=new EntityFactory();incomplete.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            Assert.IsTrue(incomplete.Blueprints.Remove("MawToad"));var zone=new Zone(MultiCellPilotRuntime.ZoneID);var floor=Native("Grass");
            Assert.IsTrue(zone.AddEntity(floor,40,12));
            Assert.IsFalse(MultiCellPilotRuntime.UpgradeCachedZone(zone,incomplete));
            Assert.AreEqual(1,zone.EntityCount);Assert.AreSame(zone.GetCell(40,12),zone.GetEntityCell(floor));
        }

        [Test] public void TwoPilotZoneInstancesNeverShareMutableOwnerState()
        {
            var first=Build();var second=Build();var a=MultiCellPilotRuntime.FindOwner(first,"movable-copper-pipe");
            var b=MultiCellPilotRuntime.FindOwner(second,"movable-copper-pipe");Assert.AreNotSame(a,b);
            a.GetPart<DestructiblePart>().HP=3;Assert.AreEqual(45,b.GetPart<DestructiblePart>().HP);
            Assert.IsTrue(first.RemoveEntity(a));Assert.AreSame(b,MultiCellPilotRuntime.FindOwner(second,"movable-copper-pipe"));
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(first,factory));Assert.IsNull(MultiCellPilotRuntime.FindOwner(first,"movable-copper-pipe"));
        }

        [Test] public void EnsurePreservesMovedPipeAnchorAndVacatedBodyCells()
        {
            var zone=Build();var pipe=MultiCellPilotRuntime.FindOwner(zone,"movable-copper-pipe");
            var old=zone.GetEntityPosition(pipe);var next=(-1,-1);
            for(int y=1;y<Zone.Height-1&&next.Item1<0;y++)for(int x=1;x<Zone.Width-3;x++)
                if(Math.Abs(x-old.x)+Math.Abs(y-old.y)>5&&zone.CanPlaceFootprint(pipe,x,y)){next=(x,y);break;}
            Assert.GreaterOrEqual(next.Item1,0,"real three-cell destination, not assumed empty coordinates");
            Assert.IsTrue(zone.MoveEntity(pipe,next.Item1,next.Item2));
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,factory));Assert.AreEqual(next,zone.GetEntityPosition(pipe));
            Assert.IsFalse(zone.GetCell(old.x,old.y).Occupants.Contains(pipe));
            for(int x=next.Item1;x<next.Item1+3;x++)Assert.IsTrue(zone.GetCell(x,next.Item2).Occupants.Contains(pipe));
        }

        [Test] public void SavedItemInAnchorHoleDoesNotSuppressTheSurroundingRidge()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var loot=Native("Dagger");Assert.IsTrue(zone.AddEntity(loot,6,2));
            Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));var ridge=MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2");
            Assert.NotNull(ridge);Assert.IsTrue(zone.GetCell(6,2).Occupants.Contains(loot));Assert.IsFalse(zone.GetCell(6,2).Occupants.Contains(ridge));
            Assert.IsTrue(zone.GetCell(8,2).Occupants.Contains(ridge));
        }

        [Test] public void SavedLargeBodyFarEdgeSuppressesConflictingOwnerButNotItsNeighbor()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var saved=Native("Crate");saved.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});
            Assert.IsTrue(zone.AddEntity(saved,7,2));Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.IsNull(MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2"));Assert.NotNull(MultiCellPilotRuntime.FindOwner(zone,"large-maw-toad"));
            Assert.IsTrue(zone.GetCell(8,2).Occupants.Contains(saved));
        }

        [Test] public void GoneOwnerIsNotReturnedEvenBeforeItsCanonicalRemovalCompletes()
        {
            var zone=Build();var owner=MultiCellPilotRuntime.FindOwner(zone,"movable-copper-pipe");Assert.NotNull(owner);
            owner.GetPart<DestructiblePart>().Gone=true;Assert.IsNull(MultiCellPilotRuntime.FindOwner(zone,"movable-copper-pipe"));
            Assert.NotNull(MultiCellPilotRuntime.FindOwner(zone,"buried-copper-pipe"));
        }

        [Test] public void InstallDiagnosticsDistinguishCommittedSkippedAndRepeatedCalls()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);var loot=Native("Dagger");Assert.IsTrue(zone.AddEntity(loot,8,2));Diag.ResetAll();
            Assert.IsTrue(MultiCellPilotRuntime.UpgradeCachedZone(zone,factory));
            Assert.AreEqual(1,Records("Installed").Length);Assert.AreEqual(0,Records("Rejected").Length);
            StringAssert.Contains("PilotRidgeNE-6-2",Records("Installed")[0].PayloadJson);
            Assert.IsTrue(MultiCellPilotRuntime.Ensure(zone,factory));Assert.AreEqual(1,Records("AlreadyInstalled").Length);
            Assert.AreEqual(1,Records("Installed").Length);
        }

        [Test] public void RejectionDiagnosticsNeverClaimInstallation()
        {
            var zone=new Zone(MultiCellPilotRuntime.ZoneID);Diag.ResetAll();
            Assert.IsFalse(MultiCellPilotRuntime.Ensure(zone,new EntityFactory()));
            Assert.AreEqual(1,Records("Rejected").Length);Assert.AreEqual(0,Records("Installed").Length);
            StringAssert.Contains("missing_ground_blueprint",Records("Rejected")[0].PayloadJson);
        }
    }
}
