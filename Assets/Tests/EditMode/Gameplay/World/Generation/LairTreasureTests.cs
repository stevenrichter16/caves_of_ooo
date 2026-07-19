using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M2.b — lairs pay. Pins the lock→container sync fix (pre-M2.b
    /// a "locked" chest's Open action worked keyless because the open path
    /// never consults LockPart), the boss key/sidearm carry, and the
    /// biome-keyed chest manifest.
    /// </summary>
    public class LairTreasureTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        // ====================================================================
        // Lock → Container sync (the keyless-open exploit fix).
        // ====================================================================

        private static (Entity chest, LockPart lockPart, ContainerPart container)
            BuildLockedChest()
        {
            var chest = new Entity { ID = "test-chest", BlueprintName = "TestBossChest" };
            chest.AddPart(new PhysicsPart { Solid = true });
            var lockPart = new LockPart { KeyId = "iron", IsLocked = true };
            var container = new ContainerPart { Locked = true, MaxItems = 10 };
            chest.AddPart(lockPart);
            chest.AddPart(container);
            return (chest, lockPart, container);
        }

        private static Entity BuildActorWithItems(params string[] keyIds)
        {
            var actor = new Entity { ID = "test-actor", BlueprintName = "TestActor" };
            var inv = new InventoryPart { MaxWeight = 50 };
            actor.AddPart(inv);
            foreach (var keyId in keyIds)
            {
                var key = new Entity { ID = "key-" + keyId, BlueprintName = "TestKey" };
                key.AddPart(new KeyPart { KeyId = keyId });
                inv.AddObject(key);
            }
            return actor;
        }

        private static void FireAttemptUnlock(Entity chest, Entity actor)
        {
            var e = GameEvent.New("AttemptUnlock");
            e.SetParameter("Actor", actor);
            chest.FireEventAndRelease(e);
        }

        [Test]
        public void Unlock_WithMatchingKey_AlsoUnlocksTheContainer()
        {
            var (chest, lockPart, container) = BuildLockedChest();
            var actor = BuildActorWithItems("iron");

            FireAttemptUnlock(chest, actor);

            Assert.IsFalse(lockPart.IsLocked, "Iron key must open the lock.");
            Assert.IsFalse(container.Locked,
                "Unlock must sync ContainerPart.Locked — the open/loot path " +
                "checks ONLY the container's own gate.");
        }

        [Test]
        public void Unlock_WithoutKey_LeavesContainerLocked()
        {
            // Counter-check: identical setup minus the key. If this fails,
            // the sync fired unconditionally and the chest is open-for-all
            // again — the exact exploit M2.b closes.
            var (chest, lockPart, container) = BuildLockedChest();
            var actor = BuildActorWithItems(/* no keys */);

            FireAttemptUnlock(chest, actor);

            Assert.IsTrue(lockPart.IsLocked, "No key: lock stays locked.");
            Assert.IsTrue(container.Locked, "No key: container stays locked.");
        }

        [Test]
        public void BossChestBlueprint_ShipsWithLockedContainer()
        {
            // The blueprint-level pin: LockedChest's latent bug was exactly
            // this field shipping false.
            var chest = _harness.Factory.CreateEntity("BossChest");
            Assert.IsNotNull(chest, "BossChest blueprint must exist.");
            Assert.IsTrue(chest.GetPart<ContainerPart>().Locked,
                "BossChest ships Container.Locked=true — keyless Open must fail.");
            Assert.IsTrue(chest.GetPart<LockPart>().IsLocked);
            Assert.AreEqual("iron", chest.GetPart<LockPart>().KeyId);
        }

        // ====================================================================
        // The treasure tables.
        // ====================================================================

        [Test]
        public void TreasureTables_EveryEntry_ResolvesToARealBlueprint()
        {
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                foreach (var name in LairTreasure.ChestManifest(biome))
                    Assert.IsNotNull(_harness.Factory.CreateEntity(name),
                        $"'{name}' in the {biome} chest manifest has no blueprint.");
            }
            foreach (var sidearm in LairTreasure.BossSidearmByBlueprint.Values)
                Assert.IsNotNull(_harness.Factory.CreateEntity(sidearm),
                    $"Sidearm '{sidearm}' has no blueprint.");
        }

        [Test]
        public void ChestManifest_EachBiome_HasWeaponGrimoireAndMineral()
        {
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                var manifest = LairTreasure.ChestManifest(biome).ToList();
                Assert.GreaterOrEqual(manifest.Count, 3,
                    $"{biome} chest must hold at least weapon + mineral + grimoire.");
                Assert.IsTrue(manifest.Any(n => n.EndsWith("Grimoire")),
                    $"{biome} chest must include its element grimoire.");
            }
        }

        // ====================================================================
        // Lair build wiring.
        // ====================================================================

        private Zone BuildLair(string bossBlueprint, BiomeType biome)
        {
            var poi = new PointOfInterest(POIType.Lair, "Test Lair", null, 1, bossBlueprint);
            var builder = new LairBuilder(biome, poi);
            var zone = new Zone("Overworld.3.3.0");
            Assert.IsTrue(builder.BuildZone(zone, _harness.Factory, new System.Random(7)));
            return zone;
        }

        private static Entity FindByBlueprint(Zone zone, string blueprint)
        {
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) return e;
            return null;
        }

        [Test]
        public void LairBuild_BossCarriesTheChestKey()
        {
            var zone = BuildLair("SnapjawChieftain", BiomeType.Cave);
            var boss = FindByBlueprint(zone, "SnapjawChieftain");
            Assert.IsNotNull(boss, "Boss must spawn.");
            Assert.IsTrue(boss.GetPart<InventoryPart>().Objects
                    .Any(o => o.BlueprintName == "IronKey"),
                "Boss must carry the chest key — death-drop delivers it on kill.");
        }

        [Test]
        public void LairBuild_PlacesLockedBossChest_WithBiomeManifest()
        {
            var zone = BuildLair("SnapjawChieftain", BiomeType.Cave);
            var chest = FindByBlueprint(zone, "BossChest");
            Assert.IsNotNull(chest, "Boss chest must be placed.");

            var cell = zone.GetEntityCell(chest);
            Assert.IsTrue(cell.X >= Zone.Width / 2 - 6 && cell.X < Zone.Width / 2 + 6
                       && cell.Y >= Zone.Height / 2 - 4 && cell.Y < Zone.Height / 2 + 4,
                "Chest must sit inside the 12x8 boss chamber.");

            var contents = chest.GetPart<ContainerPart>().Contents
                .Select(e => e.BlueprintName).ToList();
            CollectionAssert.AreEquivalent(
                LairTreasure.ChestManifest(BiomeType.Cave).ToList(), contents,
                "Chest contents must match the Cave manifest.");
        }

        [Test]
        public void LairBuild_AncientGuardian_CarriesSeveranceEdge()
        {
            var zone = BuildLair("AncientGuardian", BiomeType.Ruins);
            var boss = FindByBlueprint(zone, "AncientGuardian");
            Assert.IsNotNull(boss);
            Assert.IsTrue(boss.GetPart<InventoryPart>().Objects
                    .Any(o => o.BlueprintName == "SeveranceEdge"),
                "The Ruins boss drops the Curation's blade — SeveranceEdge's " +
                "first world spawn path.");
        }

        [Test]
        public void LairBuild_NoBossBlueprint_PlacesNoChest()
        {
            // Counter-check: chest placement rides the boss branch — a
            // bossless lair (null BossBlueprint) gets no free treasure.
            var zone = BuildLair(null, BiomeType.Cave);
            Assert.IsNull(FindByBlueprint(zone, "BossChest"),
                "No boss, no chest — treasure is guarded by construction.");
        }
    }
}
