using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Stillleaf Archive, SA.1 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): the
    /// sealed library's lock expects a key that existed nowhere, so the vault
    /// was unreachable in ordinary play. These pins state the player-visible
    /// contract: the keeper's key really opens the door through the ordinary
    /// bump path, nothing else does, and the one contested record lies inside,
    /// reachable only through that door.
    /// </summary>
    public sealed class StillleafArchiveTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            MessageLog.Clear();
        }

        private Zone Vault(int seed = 66) => new OverworldZoneManager(_factory, seed).GetZone(SealedLibraryBuilder.ZoneID);

        private static Entity Door(Zone z) => z.GetAllEntities().Single(e => e.BlueprintName == "SealedLibraryDoor");

        private Entity Player(Zone z, int x, int y)
        {
            var p = _factory.CreateEntity("Player");
            Assert.IsNotNull(p);
            // The exterior aisle cell west of the door is ordinary floor.
            Assert.IsTrue(z.AddEntity(p, x, y), "player placed beside the door");
            return p;
        }

        private static HashSet<(int x, int y)> Flood(Zone z, (int x, int y) start)
        {
            var seen = new HashSet<(int x, int y)> { start }; var q = new Queue<(int x, int y)>(); q.Enqueue(start);
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    var n = (p.x + dx, p.y + dy); var c = z.GetCell(n.Item1, n.Item2);
                    if (c != null && !c.BlocksMovement() && seen.Add(n)) q.Enqueue(n);
                }
            }
            return seen;
        }

        private static Entity Register(Zone z) => z.GetAllEntities().SingleOrDefault(e => e.ID == StillleafArchive.RegisterId);

        // ════════════════════════════════════════════════════════════
        //   The key
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheKeepersKey_MatchesTheDoorsLock()
        {
            // Anti-drift: the key's id is the builder's constant, which is
            // the id the shipped door blueprint's lock expects.
            var key = _factory.CreateEntity(StillleafArchive.KeyBlueprint);
            Assert.IsNotNull(key, "the key is real content");
            Assert.AreEqual(SealedLibraryBuilder.KeyID, key.GetPart<KeyPart>()?.KeyId);
            var door = _factory.CreateEntity("SealedLibraryDoor");
            Assert.AreEqual(door.GetPart<LockPart>().KeyId, key.GetPart<KeyPart>().KeyId);
            Assert.IsTrue(key.GetPart<PhysicsPart>().Takeable, "a key must be carryable");
        }

        [TestCase(1)] [TestCase(66)] [TestCase(913)]
        public void WithTheKey_ThePlayerWalksIntoTheVault(int seed)
        {
            // The real bump path: the first bump unlocks and still blocks the
            // turn; the next step walks through the doorway.
            var z = Vault(seed); var door = Door(z); var d = z.GetEntityPosition(door);
            var player = Player(z, d.x - 1, d.y);
            Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(_factory.CreateEntity(StillleafArchive.KeyBlueprint)));

            Assert.IsFalse(MovementSystem.TryMove(player, z, 1, 0), "unlocking is this turn's action");
            Assert.IsFalse(door.GetPart<LockPart>().IsLocked, "the keeper's key opened the lock");
            Assert.IsTrue(MovementSystem.TryMove(player, z, 1, 0), "the next step enters the doorway");
            Assert.AreEqual((d.x, d.y), z.GetEntityPosition(player));
        }

        [TestCase("IronKey")]
        [TestCase(null)]
        public void WithoutTheKeepersKey_TheDoorHolds(string otherKey)
        {
            // Counter-check: an ordinary iron key, or no key, never opens it.
            var z = Vault(); var door = Door(z); var d = z.GetEntityPosition(door);
            var player = Player(z, d.x - 1, d.y);
            if (otherKey != null)
                Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(_factory.CreateEntity(otherKey)));
            for (int i = 0; i < 3; i++) MovementSystem.TryMove(player, z, 1, 0);
            Assert.IsTrue(door.GetPart<LockPart>().IsLocked);
            Assert.AreEqual((d.x - 1, d.y), z.GetEntityPosition(player), "still outside");
        }

        // ════════════════════════════════════════════════════════════
        //   The register
        // ════════════════════════════════════════════════════════════

        [TestCase(1)] [TestCase(66)] [TestCase(913)]
        public void TheRegisterLiesInsideTheVault_ReachableOnlyThroughTheDoor(int seed)
        {
            var z = Vault(seed); var register = Register(z);
            Assert.IsNotNull(register, "one contested record is placed on fresh generation");
            var r = z.GetEntityPosition(register);
            Assert.IsTrue(z.GetCell(r.x, r.y).Objects.Any(e => e.BlueprintName == "SealedLibraryFloor"),
                "it lies on the archive's own floor");
            Assert.IsTrue(register.GetPart<PhysicsPart>().Takeable, "the player can carry it out");

            // The key is the gate: locked, the flood from the door's outside
            // cannot reach it; opened, it can.
            var door = Door(z); var d = z.GetEntityPosition(door);
            Assert.IsFalse(Flood(z, (d.x - 1, d.y)).Contains(r), "sealed: unreachable");
            door.GetPart<LockPart>().IsLocked = false; door.GetPart<PhysicsPart>().Solid = false;
            Assert.IsTrue(Flood(z, (d.x - 1, d.y)).Contains(r), "opened: reachable");
        }

        [Test]
        public void TheRegisterCarriesTheKeepersPlea()
        {
            // The third testimony travels with the record itself, so it is
            // read wherever the player first examines it.
            var register = Register(Vault());
            string text = register.GetPart<ExaminablePart>()?.Text ?? "";
            StringAssert.Contains("Stillleaf", text);
            StringAssert.Contains("sealed", text);
        }

        // ════════════════════════════════════════════════════════════
        //   Observability: every install outcome leaves a diag record
        // ════════════════════════════════════════════════════════════

        private static int Count(string kind) =>
            DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Limit = 10 }).Records.Count;

        [Test]
        public void FreshGeneration_EmitsExactlyOnePlacedRecord_AndNoRefusal()
        {
            // A debugger's first query for "where is the register?" answers
            // from the buffer, not from a code grep.
            Diag.ResetAll();
            Vault();
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "StillleafRegisterPlaced", Limit = 10 }).Records;
            Assert.AreEqual(1, records.Count, "exactly one placement per fresh floor");
            StringAssert.Contains(SealedLibraryBuilder.ZoneID, records[0].PayloadJson);
            Assert.AreEqual(0, Count("StillleafRegisterRefused"), "the happy path never also refuses");
        }

        [Test]
        public void ReinstallOnAnInstalledFloor_RefusesNamingTheGate_AndClaimsNoSuccess()
        {
            var z = Vault(); Diag.ResetAll();
            Assert.IsFalse(StillleafArchive.TryInstallVault(z, _factory));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "StillleafRegisterRefused", Limit = 10 }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("already_installed", records[0].PayloadJson);
            Assert.AreEqual(0, Count("StillleafRegisterPlaced"), "a refusal never also claims success");
        }

        [Test]
        public void AnotherFloor_RefusesAsNotStillleaf()
        {
            // Counter-check for the gate order: a foreign zone is refused
            // before any door or blueprint is consulted.
            var other = new OverworldZoneManager(_factory, 66).GetZone("Overworld.2.7.2"); Diag.ResetAll();
            Assert.IsFalse(StillleafArchive.TryInstallVault(other, _factory));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "StillleafRegisterRefused", Limit = 10 }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("not_stillleaf_floor", records[0].PayloadJson);
        }

        [Test]
        public void InstallIsIdempotent_AndARevisitNeverDuplicates()
        {
            var m = new OverworldZoneManager(_factory, 66);
            var z = m.GetZone(SealedLibraryBuilder.ZoneID);
            Assert.IsFalse(StillleafArchive.TryInstallVault(z, _factory), "a second install refuses");
            Assert.AreSame(z, m.GetZone(SealedLibraryBuilder.ZoneID));
            Assert.AreEqual(1, z.GetAllEntities().Count(e => e.BlueprintName == StillleafArchive.RegisterBlueprint));
        }

        [Test]
        public void TakenRegister_IsNotReplacedOnReinstall()
        {
            // A finite record: once removed, an install attempt must not
            // conjure another (the latch outlives the item).
            var z = Vault(); var register = Register(z);
            Assert.IsTrue(z.RemoveEntity(register));
            Assert.IsFalse(StillleafArchive.TryInstallVault(z, _factory));
            Assert.IsNull(Register(z));
        }

        [Test]
        public void OtherUndergroundZonesGetNoRegister()
        {
            // Counter-check: the hook is scoped to Stillleaf's floor.
            var m = new OverworldZoneManager(_factory, 66);
            foreach (var id in new[] { "Overworld.2.7.2", "Overworld.2.4.1", "Overworld.10.10.1" })
                Assert.IsFalse(m.GetZone(id).GetAllEntities().Any(e => e.BlueprintName == StillleafArchive.RegisterBlueprint), id);
        }
    }
}
