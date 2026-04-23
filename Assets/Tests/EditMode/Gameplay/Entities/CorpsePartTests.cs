using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M5.1 — CorpsePart (spawner on living creatures that listens to the
    /// <c>"Died"</c> event). Tests the spawn gate (CorpseChance), property
    /// propagation (CreatureName / SourceBlueprint / SourceID / KillerID /
    /// KillerBlueprint), the suppression tag, and the hook-ordering invariant
    /// (cell is resolvable when Died fires).
    ///
    /// Mirrors Qud's <c>XRL.World.Parts.Corpse</c>. See
    /// <c>Docs/QUD-PARITY.md</c> §M5.1 for the design-decisions table.
    ///
    /// Tests use the direct Zone + Entity construction pattern (same shape as
    /// <see cref="MoveToInteriorExteriorGoalTests"/>) plus a minimal inline
    /// blueprint JSON that includes <c>PhysicalObject</c> and
    /// <c>SnapjawCorpse</c> — enough for the CorpsePart to spawn a corpse
    /// without loading the full game Objects.json.
    /// </summary>
    [TestFixture]
    public class CorpsePartTests
    {
        private const string TestBlueprintsJson = @"{
            ""Objects"": [
                {
                    ""Name"": ""PhysicalObject"",
                    ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""?"" }] },
                        { ""Name"": ""Physics"", ""Params"": [] }
                    ],
                    ""Stats"": [],
                    ""Tags"": []
                },
                {
                    ""Name"": ""SnapjawCorpse"",
                    ""Inherits"": ""PhysicalObject"",
                    ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [
                            { ""Key"": ""DisplayName"", ""Value"": ""snapjaw corpse"" },
                            { ""Key"": ""RenderString"", ""Value"": ""%"" },
                            { ""Key"": ""ColorString"", ""Value"": ""&r"" }
                        ]},
                        { ""Name"": ""Physics"", ""Params"": [
                            { ""Key"": ""Takeable"", ""Value"": ""true"" },
                            { ""Key"": ""Weight"", ""Value"": ""10"" }
                        ]}
                    ],
                    ""Tags"": [
                        { ""Key"": ""Corpse"", ""Value"": """" }
                    ]
                }
            ]
        }";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprintsJson);
            CorpsePart.Factory = _factory;
        }

        [TearDown]
        public void Teardown()
        {
            // Prevent leakage into other test fixtures that rely on a clean
            // static Factory reference. Mirrors MaterialReactionResolver.Factory
            // hygiene in other test fixtures.
            CorpsePart.Factory = null;
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Build a minimal creature with a CorpsePart pre-wired. RNG is seeded
        /// deterministic so CorpseChance=100 always passes and CorpseChance=0
        /// always fails — no probabilistic flake.
        /// </summary>
        private Entity CreateCreatureWithCorpsePart(
            Zone zone, int x, int y,
            int corpseChance, string corpseBlueprint,
            string blueprintName = "TestSnapjaw")
        {
            var entity = new Entity { BlueprintName = blueprintName, ID = "TestSnapjaw-1" };
            entity.Tags["Creature"] = "";
            entity.AddPart(new RenderPart { DisplayName = blueprintName });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new CorpsePart
            {
                CorpseChance = corpseChance,
                CorpseBlueprint = corpseBlueprint,
                TestRng = new Random(0)
            });
            zone.AddEntity(entity, x, y);
            return entity;
        }

        /// <summary>
        /// Fire the "Died" event on the entity with Zone+Target+Killer params
        /// exactly as CombatSystem.HandleDeath would. Isolates CorpsePart's
        /// behavior from the full combat flow (equipment/inventory drop,
        /// witness broadcast). Does not remove the entity from the zone —
        /// tests that need post-removal state call zone.RemoveEntity directly.
        /// </summary>
        private void FireDied(Entity target, Entity killer, Zone zone)
        {
            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)target);
            died.SetParameter("Killer", (object)killer);
            died.SetParameter("Zone", (object)zone);
            target.FireEvent(died);
            died.Release();
        }

        private Entity FindCorpseAt(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return null;
            foreach (var obj in cell.Objects)
            {
                if (obj.HasTag("Corpse")) return obj;
            }
            return null;
        }

        // ====================================================================
        // Spawn gate
        // ====================================================================

        [Test]
        public void CorpsePart_SpawnsCorpseAtDeathCell_WhenChance100()
        {
            var zone = new Zone("TestZone");
            var snapjaw = CreateCreatureWithCorpsePart(zone, 10, 10, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");

            FireDied(snapjaw, killer: null, zone);

            var corpse = FindCorpseAt(zone, 10, 10);
            Assert.IsNotNull(corpse, "CorpsePart should spawn a SnapjawCorpse at the death cell when CorpseChance=100.");
            Assert.AreEqual("SnapjawCorpse", corpse.BlueprintName);
        }

        [Test]
        public void CorpsePart_DoesNotSpawn_WhenCorpseChanceZero()
        {
            var zone = new Zone("TestZone");
            var snapjaw = CreateCreatureWithCorpsePart(zone, 10, 10, corpseChance: 0, corpseBlueprint: "SnapjawCorpse");

            FireDied(snapjaw, killer: null, zone);

            var corpse = FindCorpseAt(zone, 10, 10);
            Assert.IsNull(corpse, "CorpsePart must NOT spawn a corpse when CorpseChance=0 (regression pin for the chance gate).");
        }

        [Test]
        public void CorpsePart_SuppressCorpseDropsTag_PreventsSpawn()
        {
            var zone = new Zone("TestZone");
            var snapjaw = CreateCreatureWithCorpsePart(zone, 10, 10, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");
            // Mirror Qud's SuppressCorpseDrops gate (Corpse.cs line 89).
            snapjaw.SetTag("SuppressCorpseDrops");

            FireDied(snapjaw, killer: null, zone);

            var corpse = FindCorpseAt(zone, 10, 10);
            Assert.IsNull(corpse, "SuppressCorpseDrops tag must bypass the spawn even when CorpseChance=100.");
        }

        // ====================================================================
        // Descriptor property propagation
        // ====================================================================

        [Test]
        public void CorpsePart_SpawnedCorpseCarries_CreatureName_And_SourceBlueprint()
        {
            var zone = new Zone("TestZone");
            var snapjaw = CreateCreatureWithCorpsePart(zone, 10, 10, corpseChance: 100, corpseBlueprint: "SnapjawCorpse",
                blueprintName: "Snapjaw");

            FireDied(snapjaw, killer: null, zone);

            var corpse = FindCorpseAt(zone, 10, 10);
            Assert.IsNotNull(corpse);
            Assert.AreEqual("Snapjaw", corpse.GetProperty("CreatureName"),
                "Spawned corpse should carry the deceased's display name (mirrors Qud Corpse.cs line 141).");
            Assert.AreEqual("Snapjaw", corpse.GetProperty("SourceBlueprint"),
                "Spawned corpse should carry the deceased's blueprint (mirrors Qud Corpse.cs line 155).");
        }

        [Test]
        public void CorpsePart_SpawnedCorpseCarries_KillerID_WhenKillerHasID()
        {
            var zone = new Zone("TestZone");
            var snapjaw = CreateCreatureWithCorpsePart(zone, 10, 10, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");

            var killer = new Entity { BlueprintName = "Warden", ID = "Warden-7" };
            killer.AddPart(new RenderPart { DisplayName = "warden" });
            // Killer does not need to be in zone — mirrors Qud passing killer via event param.

            FireDied(snapjaw, killer, zone);

            var corpse = FindCorpseAt(zone, 10, 10);
            Assert.IsNotNull(corpse);
            Assert.AreEqual("Warden-7", corpse.GetProperty("KillerID"),
                "KillerID property should carry over when the killer has a runtime ID (Qud Corpse.cs line 160).");
            Assert.AreEqual("Warden", corpse.GetProperty("KillerBlueprint"),
                "KillerBlueprint should carry over alongside the ID (Qud Corpse.cs line 162).");
        }

        [Test]
        public void CorpsePart_SelfKiller_NoKillerPropertiesAssigned()
        {
            // Qud filters out self-kills so a suicide doesn't report itself as the killer
            // (Corpse.cs line 156: `if (E.Killer != null && E.Killer != ParentObject)`).
            // Pin that behaviour explicitly.
            var zone = new Zone("TestZone");
            var snapjaw = CreateCreatureWithCorpsePart(zone, 10, 10, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");

            FireDied(snapjaw, killer: snapjaw, zone);

            var corpse = FindCorpseAt(zone, 10, 10);
            Assert.IsNotNull(corpse);
            Assert.IsNull(corpse.GetProperty("KillerID"),
                "KillerID must not be set when the deceased is their own killer.");
            Assert.IsNull(corpse.GetProperty("KillerBlueprint"),
                "KillerBlueprint must not be set when the deceased is their own killer.");
        }

        // ====================================================================
        // Hook-ordering invariant
        // ====================================================================

        [Test]
        public void CorpsePart_CellResolvable_WhenDiedFires_EvenDuringFullHandleDeath()
        {
            // Pins the critical M5.1 invariant: at the moment "Died" fires in
            // CombatSystem.HandleDeath, the deceased still occupies its cell
            // (RemoveEntity runs AFTER the event cascade at line 465). If
            // someone reorders HandleDeath and moves RemoveEntity above the
            // Died event, this test breaks loudly.
            var zone = new Zone("TestZone");
            var snapjaw = CreateCreatureWithCorpsePart(zone, 10, 10, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");
            // Full creature stats needed so HandleDeath drops equipment/inventory
            // without throwing (Body/Inventory null-checks tolerate absence,
            // but HP stat is read).
            snapjaw.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 0, Min = 0, Max = 15 };

            CombatSystem.HandleDeath(snapjaw, killer: null, zone);

            // After HandleDeath: snapjaw is gone from zone but the SnapjawCorpse
            // replaces it at the same cell.
            Assert.IsNull(zone.GetEntityCell(snapjaw), "Deceased entity should be removed from zone post-HandleDeath.");
            var corpse = FindCorpseAt(zone, 10, 10);
            Assert.IsNotNull(corpse,
                "CorpsePart must have spawned during the Died event's cell-still-valid window " +
                "(regression pin for HandleDeath ordering: Died fires BEFORE zone.RemoveEntity).");
        }
    }
}
