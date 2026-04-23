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

    /// <summary>
    /// Fix-pass regression fixture for the "spawn-village NPCs don't drop
    /// corpses" report. Loads the real Objects.json to exercise blueprint
    /// inheritance and the CreatureCorpse + Creature.CorpsePart wiring.
    ///
    /// Context: M5.1 originally only attached CorpsePart to <c>Snapjaw</c>,
    /// which left every other creature (Villager, Scribe, Warden, Farmer,
    /// and ~40 more) silently corpse-less. Fix: attach a CorpsePart to the
    /// <c>Creature</c> parent pointing at a new <c>CreatureCorpse</c>
    /// blueprint; Snapjaw's override continues to point at SnapjawCorpse;
    /// <c>Player</c> and <c>MimicChest</c> opt out via the
    /// <c>SuppressCorpseDrops</c> tag.
    /// </summary>
    [TestFixture]
    public class CreatureCorpseBlueprintTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            var text = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(text, "Real Objects.json must be loadable.");
            _factory.LoadBlueprints(text.text);
            CorpsePart.Factory = _factory;
        }

        [TearDown]
        public void Teardown() { CorpsePart.Factory = null; }

        [Test]
        public void CreatureBlueprint_HasCorpsePart_PointingAtCreatureCorpse()
        {
            var villager = _factory.CreateEntity("Villager");
            Assert.IsNotNull(villager, "Villager blueprint must instantiate.");
            var cp = villager.GetPart<CorpsePart>();
            Assert.IsNotNull(cp,
                "Every Creature-derived blueprint must inherit a CorpsePart from the Creature parent.");
            Assert.AreEqual("CreatureCorpse", cp.CorpseBlueprint,
                "Default corpse blueprint for a generic NPC is CreatureCorpse.");
            Assert.AreEqual(100, cp.CorpseChance,
                "Default CorpseChance for a generic NPC is 100 (every kill drops).");
        }

        [Test]
        public void CreatureCorpseBlueprint_HasTakeableRenderAndCorpseTag()
        {
            var corpse = _factory.CreateEntity("CreatureCorpse");
            Assert.IsNotNull(corpse, "CreatureCorpse blueprint must exist.");
            Assert.IsTrue(corpse.HasTag("Corpse"),
                "CreatureCorpse must carry the Corpse tag so AIUndertakerPart can find it.");
            var physics = corpse.GetPart<PhysicsPart>();
            Assert.IsNotNull(physics);
            Assert.IsTrue(physics.Takeable, "Corpse must be Takeable for PickupCommand.");
            Assert.AreEqual(10, physics.Weight, "Default corpse weight=10 matches SnapjawCorpse convention.");
        }

        [Test]
        public void SnapjawBlueprint_OverridesCreatureCorpse_StillSpawnsSnapjawCorpse()
        {
            // Blueprint inheritance merges Parts by name with child-wins.
            // Snapjaw's Corpse part entry should OVERRIDE the Creature parent's
            // entry, pointing at SnapjawCorpse not CreatureCorpse.
            var snapjaw = _factory.CreateEntity("Snapjaw");
            var cp = snapjaw.GetPart<CorpsePart>();
            Assert.IsNotNull(cp, "Snapjaw should still have a CorpsePart.");
            Assert.AreEqual("SnapjawCorpse", cp.CorpseBlueprint,
                "Snapjaw's override must stick — not fall through to CreatureCorpse.");
            Assert.AreEqual(70, cp.CorpseChance,
                "Snapjaw's 70% corpse-chance override must stick.");
        }

        [Test]
        public void PlayerBlueprint_HasSuppressCorpseDropsTag()
        {
            var player = _factory.CreateEntity("Player");
            Assert.IsNotNull(player);
            Assert.IsTrue(player.HasTag("SuppressCorpseDrops"),
                "Player must opt out of corpse drops — dying-player corpse is unwanted semantics.");
        }

        [Test]
        public void MimicChestBlueprint_HasSuppressCorpseDropsTag()
        {
            var mimic = _factory.CreateEntity("MimicChest");
            Assert.IsNotNull(mimic);
            Assert.IsTrue(mimic.HasTag("SuppressCorpseDrops"),
                "MimicChest must opt out — a 'corpse of a mimic chest' is off-theme.");
        }

        // End-to-end integration: spawn a Villager, fire Died via HandleDeath,
        // confirm CreatureCorpse lands at the death cell. Mirrors the Snapjaw
        // integration test above, but exercises the inherited CorpsePart path.
        [Test]
        public void Villager_OnDied_SpawnsCreatureCorpseAtDeathCell()
        {
            FactionManager.Initialize();
            var zone = new Zone("TestZone");
            var villager = _factory.CreateEntity("Villager");
            Assert.IsNotNull(villager);
            zone.AddEntity(villager, 10, 10);

            CombatSystem.HandleDeath(villager, killer: null, zone);

            Assert.IsNull(zone.GetEntityCell(villager),
                "Deceased Villager should be removed post-HandleDeath.");
            CavesOfOoo.Core.Entity corpse = null;
            var cell = zone.GetCell(10, 10);
            foreach (var obj in cell.Objects)
            {
                if (obj.BlueprintName == "CreatureCorpse") { corpse = obj; break; }
            }
            Assert.IsNotNull(corpse,
                "Villager must drop a CreatureCorpse at their death cell (inherited Creature.CorpsePart).");
            Assert.AreEqual("villager", corpse.GetProperty("CreatureName"));
            Assert.AreEqual("Villager", corpse.GetProperty("SourceBlueprint"));
        }

        [Test]
        public void Player_OnDied_DoesNotSpawnCorpse()
        {
            FactionManager.Initialize();
            var zone = new Zone("TestZone");
            var player = _factory.CreateEntity("Player");
            Assert.IsNotNull(player);
            zone.AddEntity(player, 10, 10);

            CombatSystem.HandleDeath(player, killer: null, zone);

            var cell = zone.GetCell(10, 10);
            foreach (var obj in cell.Objects)
            {
                Assert.IsFalse(obj.HasTag("Corpse"),
                    "Player death must NOT spawn any Corpse-tagged entity — SuppressCorpseDrops gate.");
            }
        }
    }
}
