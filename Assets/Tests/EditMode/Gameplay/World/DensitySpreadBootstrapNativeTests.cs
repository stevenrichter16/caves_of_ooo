using System;
using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Calls real bootstrap/boot-menu seams. Full scene startup remains a
    // separate native Play gate; this fixture does not call DoStart itself.
    public sealed class DensitySpreadBootstrapNativeTests
    {
        [Test]
        public void UnconfiguredBootstrapAndSavedSceneUseOrdinaryDefaultSentinel()
        {
            string scene = File.ReadAllText(Path.Combine(Application.dataPath, "Scenes/Main/SampleScene.unity"));
            StringAssert.Contains("FreshGameZoneID:\n", scene);
            var go = new GameObject("fresh-start-default");
            try { Assert.AreEqual("", go.AddComponent<GameBootstrap>().FreshGameZoneID); }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void EmptySettingGeneratesCurrentActualSpreadWithoutChangingAuthoredVillageIdentity()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.Bootstrap.FreshGameZoneID = "";
                f.Generate();
                var position = WorldMap.FromZoneID(f.Zone.ZoneID);
                Assert.AreEqual(BiomeType.Spread, f.Manager.WorldMap.GetBiome(position.x, position.y));
                Assert.AreEqual(ReferenceGladePlan.ZoneID, f.Zone.ZoneID);
                Assert.AreSame(f.Zone, f.Manager.ActiveZone);
                Assert.AreEqual("Overworld.10.10.0", WorldMap.StartingZoneID);
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void ActualBootstrapHonorsPhysicalCenterOwner(bool solid)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone("Overworld.12.10.0");
                f.Bootstrap.FreshGameZoneID = f.Zone.ZoneID;
                var owner = new Entity(); owner.AddPart(new PhysicsPart { Solid = solid });
                f.Zone.AddEntity(owner, 40, 12); f.Place();
                Assert.AreEqual(!solid, f.Zone.GetEntityPosition(f.Player) == (40, 12));
                Assert.AreEqual((40, 12), f.Zone.GetEntityPosition(owner));
                Assert.False(f.Zone.GetEntityCell(f.Player).BlocksMovement(f.Player));
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void ActualBootstrapNeverStartsOnANonSolidCreature(bool creature)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone("Overworld.12.10.0");
                f.Bootstrap.FreshGameZoneID = f.Zone.ZoneID;
                var owner = new Entity(); owner.AddPart(new PhysicsPart());
                if (creature) owner.SetTag("Creature");
                f.Zone.AddEntity(owner, 40, 12); f.Place();
                Assert.AreEqual(!creature, f.Zone.GetEntityPosition(f.Player) == (40, 12));
                Assert.AreEqual((40, 12), f.Zone.GetEntityPosition(owner));
            }
        }

        [Test]
        public void FailedExplicitPlacementDoesNotForceOverlapOrOverwriteThePreviousSave()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone("Overworld.12.10.0");
                f.Bootstrap.FreshGameZoneID = f.Zone.ZoneID;
                f.Zone.ForEachCell((cell, x, y) => { var wall = new Entity(); wall.SetTag("Solid"); f.Zone.AddEntity(wall, x, y); });
                string active = f.Save.ActiveID;
                f.Place();
                Assert.IsNull(f.Zone.GetEntityCell(f.Player));
                Assert.AreEqual(2000, f.Zone.EntityCount);
                Assert.AreEqual(active, f.Save.ActiveID);
                Assert.False(Directory.Exists(Path.Combine(f.Save.Root, f.Save.NewID)));
                f.Save.OldUnchanged();
            }
        }

        [Test]
        public void RealBootNCheckpointsNewSpreadGraphAndPreservesPreviousSaveBytes()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.Bootstrap.FreshGameZoneID = ""; f.Generate(); f.Place();
                string id = f.Player.ID, zoneId = f.Zone.ZoneID;
                var at = f.Zone.GetEntityPosition(f.Player); int seed = f.Manager.WorldSeed;
                f.RegisterFresh(); Assert.False(f.Save.Choose(KeyCode.N).IsActive);
                Assert.AreEqual(f.Save.NewID, f.Save.ActiveID); Assert.True(SaveGameService.QuickLoad());
                Assert.AreEqual(id, f.Player.ID); Assert.AreEqual(zoneId, f.Zone.ZoneID);
                Assert.AreEqual(at, f.Zone.GetEntityPosition(f.Player)); Assert.AreEqual(seed, f.Manager.WorldSeed);
                var world = WorldMap.FromZoneID(zoneId);
                Assert.AreEqual(BiomeType.Spread, f.Manager.WorldMap.GetBiome(world.x, world.y));
                f.Save.OldUnchanged();
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void NativeContinueRestoresOldForeignLocationDespiteDefaultSpreadFreshGraph(bool alsoNew)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone("Overworld.19.19.0"); f.Zone.AddEntity(f.Player, 7, 8);
                string oldID = f.Player.ID; int oldSeed = f.Manager.WorldSeed;
                HotbarSaveFixture.Set(f.Bootstrap, "_gameID", f.Save.OldID);
                SaveGameService.RegisterRuntime(f.Runtime.Capture, f.Bootstrap.ApplyLoadedGame, f.Save.OldID);
                SaveGameService.SetActiveGameID(f.Save.OldID); Assert.True(SaveGameService.QuickSave());
                string path = Path.Combine(f.Save.Root, f.Save.OldID, "Quick.sav.gz"); byte[] bytes = File.ReadAllBytes(path);
                f.Bootstrap.FreshGameZoneID = ""; f.Generate(); f.Place(); f.RegisterFresh();
                Assert.AreEqual(ReferenceGladePlan.ZoneID, f.Zone.ZoneID);
                Assert.False(f.Save.Choose(alsoNew ? new[] { KeyCode.C, KeyCode.N } : new[] { KeyCode.C }).IsActive);
                Assert.AreEqual(oldID, f.Player.ID); Assert.AreEqual("Overworld.19.19.0", f.Zone.ZoneID);
                Assert.AreEqual((7, 8), f.Zone.GetEntityPosition(f.Player)); Assert.AreEqual(oldSeed, f.Manager.WorldSeed);
                Assert.AreEqual(17, f.Capture().TurnManager.TickCount);
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
                Assert.False(Directory.Exists(Path.Combine(f.Save.Root, f.Save.NewID)));
            }
        }
    }
}
