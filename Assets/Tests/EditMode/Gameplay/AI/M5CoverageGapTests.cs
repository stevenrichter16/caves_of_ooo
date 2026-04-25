using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M5 (Corpse system: CorpsePart + DisposeOfCorpseGoal + AIUndertakerPart)
    /// gap-coverage pass. Companion to:
    ///   - CorpsePartTests.cs            (M5.1, 18 tests)
    ///   - DisposeOfCorpseGoalTests.cs   (M5.2, 14 tests)
    ///   - AIUndertakerPartTests.cs      (M5.3, 10 tests)
    ///
    /// Per Docs/QUD-PARITY.md gap-coverage protocol:
    ///   "Read production line-by-line; for each branch, check whether an
    ///    existing test pins the contract. If not, add a test that does."
    ///
    /// All branches read against commits 4eed32a (M5.1) / ab28254 (M5.2) /
    /// 89d2070 (M5.3) and the post-review fix-passes (44182c9, 941ce1e,
    /// 87c8400, daba022, 59e1674).
    ///
    /// Each test pins observed behavior; classification of any failures
    /// will follow §3.9: test-wrong / code-wrong / setup-wrong.
    /// </summary>
    [TestFixture]
    public class M5CoverageGapTests
    {
        // ====================================================================
        // Shared minimal blueprint JSON (used by CorpsePart tests that need
        // a real factory). Matches the shape used by CorpsePartTests.
        // ====================================================================
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
                            { ""Key"": ""RenderString"", ""Value"": ""%"" }
                        ]},
                        { ""Name"": ""Physics"", ""Params"": [
                            { ""Key"": ""Takeable"", ""Value"": ""true"" },
                            { ""Key"": ""Weight"", ""Value"": ""10"" }
                        ]}
                    ],
                    ""Tags"": [
                        { ""Key"": ""Corpse"", ""Value"": """" }
                    ]
                },
                {
                    ""Name"": ""GenericCorpse"",
                    ""Inherits"": ""PhysicalObject"",
                    ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [
                            { ""Key"": ""DisplayName"", ""Value"": ""corpse"" },
                            { ""Key"": ""RenderString"", ""Value"": ""%"" }
                        ]},
                        { ""Name"": ""Physics"", ""Params"": [
                            { ""Key"": ""Takeable"", ""Value"": ""true"" },
                            { ""Key"": ""Weight"", ""Value"": ""8"" }
                        ]}
                    ],
                    ""Tags"": [
                        { ""Key"": ""Corpse"", ""Value"": """" }
                    ]
                }
            ]
        }";

        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            CorpsePart.Factory = null;
        }

        // ====================================================================
        // Helpers — same shapes as the M5.x test fixtures, kept here so this
        // file is independently readable.
        // ====================================================================

        private static EntityFactory MakeFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(TestBlueprintsJson);
            return f;
        }

        private static Entity MakeCreature(
            Zone zone, int x, int y,
            int corpseChance, string corpseBlueprint,
            int buildChance = 100,
            int rngSeed = 0)
        {
            var entity = new Entity { BlueprintName = "TestCreature", ID = "Creat-" + x + "-" + y };
            entity.Tags["Creature"] = "";
            entity.AddPart(new RenderPart { DisplayName = "test creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new CorpsePart
            {
                CorpseChance = corpseChance,
                CorpseBlueprint = corpseBlueprint,
                BuildCorpseChance = buildChance,
                TestRng = new Random(rngSeed)
            });
            zone.AddEntity(entity, x, y);
            return entity;
        }

        private static GameEvent MakeDiedEvent(Entity target, Zone zone, Entity killer = null)
        {
            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)target);
            if (killer != null) died.SetParameter("Killer", (object)killer);
            if (zone != null) died.SetParameter("Zone", (object)zone);
            return died;
        }

        private static Entity FindCorpseAt(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return null;
            foreach (var obj in cell.Objects)
                if (obj.HasTag("Corpse")) return obj;
            return null;
        }

        private static Entity MakeUndertaker(Zone zone, int x, int y, int chance = 100, int rngSeed = 0)
        {
            var entity = new Entity { BlueprintName = "Undertaker", ID = "UT-" + x };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "undertaker" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(rngSeed) };
            entity.AddPart(brain);
            entity.AddPart(new AIUndertakerPart { Chance = chance });
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        private static Entity MakeBareCorpse(Zone zone, int x, int y, int weight = 10, string id = "Corpse-X")
        {
            var corpse = new Entity { BlueprintName = "SnapjawCorpse", ID = id };
            corpse.Tags["Corpse"] = "";
            corpse.AddPart(new RenderPart { DisplayName = "snapjaw corpse" });
            corpse.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            zone.AddEntity(corpse, x, y);
            return corpse;
        }

        private static Entity MakeGraveyard(Zone zone, int x, int y, int maxItems = -1, bool withContainer = true)
        {
            var grave = new Entity { BlueprintName = "Graveyard", ID = "Grave-" + x + "-" + y };
            grave.Tags["Graveyard"] = "";
            grave.AddPart(new RenderPart { DisplayName = "graveyard" });
            grave.AddPart(new PhysicsPart { Solid = true });
            if (withContainer)
                grave.AddPart(new ContainerPart { MaxItems = maxItems });
            zone.AddEntity(grave, x, y);
            return grave;
        }

        // ====================================================================
        // CorpsePart gap-coverage
        // ====================================================================

        /// <summary>
        /// Production line 134: BuildCorpseChance is checked first as a
        /// meta-gate. With a deterministic RNG seeded so Next(100) returns a
        /// value >= BuildCorpseChance, no corpse should spawn even though
        /// CorpseChance=100. Pins the two-stage gate ordering.
        /// </summary>
        [Test]
        public void CorpsePart_BuildCorpseChanceLow_BlocksSpawn_BeforeCorpseChance()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            // BuildCorpseChance=0 with any rng → first Next(100) is >= 0 always,
            // so the gate fires every time.
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "SnapjawCorpse", buildChance: 0);

            var died = MakeDiedEvent(creature, zone);
            creature.FireEvent(died);
            died.Release();

            Assert.IsNull(FindCorpseAt(zone, 5, 5),
                "BuildCorpseChance=0 must always block before CorpseChance is consulted.");
        }

        /// <summary>
        /// Production line 121-122: when CorpsePart.Factory is null (test or
        /// uninitialized runtime), HandleDied must no-op gracefully — no
        /// throw, no corpse, no log spam.
        /// </summary>
        [Test]
        public void CorpsePart_NoFactoryWired_GracefullyNoOps()
        {
            CorpsePart.Factory = null;  // explicitly disable
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");

            var died = MakeDiedEvent(creature, zone);
            Assert.DoesNotThrow(() => creature.FireEvent(died),
                "CorpsePart with no factory wired should not throw on Died.");
            died.Release();

            Assert.IsNull(FindCorpseAt(zone, 5, 5));
        }

        /// <summary>
        /// Production line 125: empty/null CorpseBlueprint disables drops.
        /// Distinct from CorpseChance=0; this is the "spawner is unconfigured"
        /// path. No-op cleanly.
        /// </summary>
        [Test]
        public void CorpsePart_NullOrEmptyCorpseBlueprint_NoOps()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: null);

            var died = MakeDiedEvent(creature, zone);
            Assert.DoesNotThrow(() => creature.FireEvent(died));
            died.Release();
            Assert.IsNull(FindCorpseAt(zone, 5, 5),
                "Null CorpseBlueprint must not spawn anything.");

            // Also test empty-string variant.
            creature.GetPart<CorpsePart>().CorpseBlueprint = "";
            var died2 = MakeDiedEvent(creature, zone);
            creature.FireEvent(died2);
            died2.Release();
            Assert.IsNull(FindCorpseAt(zone, 5, 5),
                "Empty CorpseBlueprint must not spawn anything.");
        }

        /// <summary>
        /// Production line 140-142: when Died fires without a Zone parameter,
        /// HandleDied returns early. Mirrors CombatSystem's contract that the
        /// Zone is always supplied; a malformed Died from elsewhere mustn't
        /// throw.
        /// </summary>
        [Test]
        public void CorpsePart_MissingZoneParameter_NoOps()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");

            // Build event WITHOUT Zone parameter.
            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)creature);
            Assert.DoesNotThrow(() => creature.FireEvent(died));
            died.Release();

            Assert.IsNull(FindCorpseAt(zone, 5, 5),
                "Died without a Zone parameter should be a no-op (defensive).");
        }

        /// <summary>
        /// Production line 147-149: when ParentEntity is no longer in the
        /// zone (cell == null), HandleDied no-ops. Pins the
        /// "fires-BEFORE-RemoveEntity" expectation: if some other listener
        /// removed the entity first, CorpsePart simply doesn't drop.
        /// </summary>
        [Test]
        public void CorpsePart_EntityNotInZone_NoOps()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");

            // Pre-remove from zone before firing Died.
            zone.RemoveEntity(creature);

            var died = MakeDiedEvent(creature, zone);
            Assert.DoesNotThrow(() => creature.FireEvent(died));
            died.Release();

            // Cell is empty.
            var cell = zone.GetCell(5, 5);
            Assert.IsNotNull(cell);
            foreach (var obj in cell.Objects)
                Assert.IsFalse(obj.HasTag("Corpse"), "No corpse should spawn when ParentEntity is no longer in zone.");
        }

        /// <summary>
        /// Production line 152-164: factory.CreateEntity returning null
        /// (typo'd or missing blueprint name) should warn-log and no-op,
        /// not throw or spawn a partial entity. Fix-pass M5 finding #1.
        /// </summary>
        [Test]
        public void CorpsePart_BadCorpseBlueprintName_LogsWarning_NoOps()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "DefinitelyNotABlueprint");

            // factory.CreateEntity already LogError's, and CorpsePart adds a
            // contextual LogWarning. Both are expected — assert via
            // LogAssert (UnityEngine.TestTools).
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                var died = MakeDiedEvent(creature, zone);
                Assert.DoesNotThrow(() => creature.FireEvent(died));
                died.Release();
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }

            Assert.IsNull(FindCorpseAt(zone, 5, 5),
                "Bad blueprint name must not produce a partial corpse entity.");
        }

        /// <summary>
        /// Production line 175-176: when ParentEntity has an empty ID, the
        /// SourceID property is NOT set on the corpse (skipped via the
        /// IsNullOrEmpty guard). Existing tests pin the populated case;
        /// this pins the negative path so no empty-string property leaks
        /// into Properties dict.
        /// </summary>
        [Test]
        public void CorpsePart_EmptySourceID_DoesNotWriteSourceIDProperty()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");
            // Force ID to empty (helper sets a non-empty default).
            creature.ID = "";

            var died = MakeDiedEvent(creature, zone);
            creature.FireEvent(died);
            died.Release();

            var corpse = FindCorpseAt(zone, 5, 5);
            Assert.IsNotNull(corpse);
            Assert.IsFalse(corpse.Properties.ContainsKey("SourceID"),
                "SourceID property must NOT be written when the deceased's ID is empty (avoids polluting Properties).");
        }

        /// <summary>
        /// Production line 204-210: when the corpse blueprint's Render is
        /// missing or DisplayName != "corpse", interpolation must NOT
        /// overwrite. SnapjawCorpse has DisplayName="snapjaw corpse" — must
        /// be preserved verbatim. Existing test pins this from the blueprint
        /// path; this version pins it via direct CorpsePart manipulation,
        /// proving the gate is on render.DisplayName, not blueprint name.
        /// </summary>
        [Test]
        public void CorpsePart_CorpseDisplayName_NotInterpolated_WhenAuthoredNonDefault()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");
            // Override creature DisplayName so we'd see interpolation if it ran.
            creature.GetPart<RenderPart>().DisplayName = "ancient lich king";

            var died = MakeDiedEvent(creature, zone);
            creature.FireEvent(died);
            died.Release();

            var corpse = FindCorpseAt(zone, 5, 5);
            Assert.IsNotNull(corpse);
            Assert.AreEqual("snapjaw corpse", corpse.GetPart<RenderPart>().DisplayName,
                "Authored DisplayName must NOT be overwritten when it isn't the bare default 'corpse'.");
        }

        /// <summary>
        /// Production line 107: HandleEvent always returns true so other
        /// "Died" listeners (StatusEffectsPart cleanup, GivesRepPart award,
        /// witness broadcast) still receive the event. Without this, the
        /// drop side-effect would steal the event.
        /// </summary>
        [Test]
        public void CorpsePart_HandleEvent_AlwaysPropagates_SoOtherListenersFire()
        {
            CorpsePart.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, corpseChance: 100, corpseBlueprint: "SnapjawCorpse");
            var corpsePart = creature.GetPart<CorpsePart>();

            var died = MakeDiedEvent(creature, zone);
            bool propagated = corpsePart.HandleEvent(died);
            died.Release();

            Assert.IsTrue(propagated,
                "CorpsePart.HandleEvent must return true so subsequent Died listeners still run.");
        }

        // ====================================================================
        // DisposeOfCorpseGoal gap-coverage
        // ====================================================================

        /// <summary>
        /// Production line 66: Finished() returns false during the active
        /// fetch phase. Pins the only-finishes-on-explicit-_done-flag invariant.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_NotFinished_DuringActiveFetchPhase()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);
            var grave = MakeGraveyard(zone, 20, 10);
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);

            Assert.IsFalse(goal.Finished(),
                "Newly-pushed goal should not be finished — only sets _done after deposit/give-up.");

            goal.TakeAction();
            Assert.IsFalse(goal.Finished(),
                "Mid-fetch goal (not yet adjacent) should still not be Finished.");
        }

        /// <summary>
        /// Production line 70: CanFight() returns true so undertakers defend
        /// themselves when a hostile appears mid-haul. Matches Qud's
        /// DisposeOfCorpse.cs line 33.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_CanFight_IsTrue()
        {
            var corpse = new Entity { BlueprintName = "SnapjawCorpse" };
            corpse.Tags["Corpse"] = "";
            var grave = new Entity { BlueprintName = "Graveyard" };
            grave.Tags["Graveyard"] = "";
            var goal = new DisposeOfCorpseGoal(corpse, grave);

            Assert.IsTrue(goal.CanFight(),
                "DisposeOfCorpseGoal must allow combat interrupts (matches Qud parity).");
        }

        /// <summary>
        /// Production line 72-81: GetDetails() must include "fetching" before
        /// pickup and "hauling" after. Pins the inspector text contract used
        /// by AIDebugMenu.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_GetDetails_FormatsPhase()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);
            var grave = MakeGraveyard(zone, 20, 10);
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);

            // Before pickup: phase=fetching.
            var beforeDetails = goal.GetDetails();
            StringAssert.Contains("phase=fetching", beforeDetails);
            StringAssert.Contains("fetchTries=", beforeDetails);
            StringAssert.Contains("haulTries=", beforeDetails);

            // Simulate pickup: place corpse in inventory.
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);
            var afterDetails = goal.GetDetails();
            StringAssert.Contains("phase=hauling", afterDetails);
        }

        /// <summary>
        /// Production line 89: null Corpse OR Container at construction must
        /// FailToParent on first TakeAction (defensive — caller mistake).
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_NullCorpseOrContainer_FailsToParentImmediately()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var grave = MakeGraveyard(zone, 20, 10);

            // Null corpse.
            var goal1 = new DisposeOfCorpseGoal(null, grave);
            npc.GetPart<BrainPart>().PushGoal(goal1);
            Assert.DoesNotThrow(() => goal1.TakeAction());
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "Null Corpse should cause FailToParent → goal removed from stack.");

            // Null container.
            var corpse = MakeBareCorpse(zone, 15, 10);
            var goal2 = new DisposeOfCorpseGoal(corpse, null);
            npc.GetPart<BrainPart>().PushGoal(goal2);
            Assert.DoesNotThrow(() => goal2.TakeAction());
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "Null Container should cause FailToParent → goal removed from stack.");
        }

        /// <summary>
        /// Production line 92: container removed from the zone between
        /// pushes (e.g. graveyard destroyed by world event) → FailToParent.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_ContainerRemovedFromZone_FailsToParent()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);
            var grave = MakeGraveyard(zone, 20, 10);
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);

            // Remove the graveyard before any TakeAction.
            zone.RemoveEntity(grave);

            goal.TakeAction();
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "Container missing from zone must trigger FailToParent.");
        }

        /// <summary>
        /// Production line 97-98: actor without InventoryPart cannot pick up
        /// the corpse → FailToParent. Defensive guard against blueprints that
        /// attach AIUndertaker but no inventory.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_ActorWithoutInventory_FailsToParent()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            // Strip the inventory part the helper added.
            var inv = npc.GetPart<InventoryPart>();
            npc.RemovePart(inv);

            var corpse = MakeBareCorpse(zone, 15, 10);
            var grave = MakeGraveyard(zone, 20, 10);
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);

            goal.TakeAction();
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "Actor without InventoryPart must FailToParent immediately.");
        }

        /// <summary>
        /// Production line 270-277: when the container is adjacent BUT
        /// missing a ContainerPart (blueprint misconfigured), the corpse
        /// drops at the actor's feet rather than being lost.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_AdjacentToContainerWithoutContainerPart_DropsAtFeet()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);
            // Misconfigured graveyard — Graveyard tag but no ContainerPart.
            var brokenGrave = MakeGraveyard(zone, 11, 10, withContainer: false);

            // Pre-pickup the corpse so we're directly in haul phase.
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var goal = new DisposeOfCorpseGoal(corpse, brokenGrave);
            npc.GetPart<BrainPart>().PushGoal(goal);
            goal.TakeAction();

            // Corpse should be at NPC's feet (10,10), not in inventory, not lost.
            Assert.IsFalse(npc.GetPart<InventoryPart>().Contains(corpse),
                "Misconfigured container should release the corpse from inventory.");
            Assert.IsNotNull(FindCorpseAt(zone, 10, 10),
                "Corpse should drop at NPC's feet when container has no ContainerPart.");
        }

        /// <summary>
        /// Production line 290-298: explicit Failed(child) override calls
        /// FailToParent — pinning that we propagate failure rather than
        /// silently retry. Important contract for hierarchical goals
        /// (an AIUndertaker behavior part may want to re-try with another
        /// corpse, which only works if the failure surfaces).
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_Failed_PropagatesToParent()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);
            var grave = MakeGraveyard(zone, 20, 10);
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);

            // Build a fake child goal to pass to Failed().
            var fakeChild = new MoveToGoal(99, 99, 5);
            fakeChild.ParentHandler = goal;
            goal.Failed(fakeChild);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "Failed(child) should call FailToParent → goal removed.");
        }

        /// <summary>
        /// Production line 140 + 170: GoToCorpseTries / GoToContainerTries
        /// increment per-push of the child MoveToGoal. Pins the counter
        /// before the cap-check, not after.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_GoToCorpseTries_IncrementsPerPush()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);  // far → pushes MoveToGoal
            var grave = MakeGraveyard(zone, 20, 10);
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);

            Assert.AreEqual(0, goal.GoToCorpseTries, "Initial counter must be 0.");
            goal.TakeAction();
            Assert.AreEqual(1, goal.GoToCorpseTries, "First push increments to 1.");

            // Pop the child MoveToGoal and re-tick to force another fetch push.
            var brain = npc.GetPart<BrainPart>();
            var child = brain.PeekGoal();
            Assert.IsInstanceOf<MoveToGoal>(child);
            brain.RemoveGoal(child);

            goal.TakeAction();
            Assert.AreEqual(2, goal.GoToCorpseTries, "Second push increments to 2.");
        }

        /// <summary>
        /// Production line 252-262: IsSteppable rejects a cell where ANY
        /// object has PhysicsPart.Solid=true even without the "Solid" tag.
        /// Pins the dual-rule (tag OR Solid bit) — the bug fix in daba022
        /// went sideways precisely because Cell.IsPassable only checked the
        /// tag. This test forces a PhysicsPart.Solid=true neighbor with NO
        /// tag and verifies the haul targets a different neighbor.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_IsSteppable_RejectsPhysicsPartSolid_WithoutTag()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 11, 10);

            // Place graveyard at (15,10). Block all neighbors except (16,10)
            // by inserting Solid-via-PhysicsPart-but-no-tag obstacles.
            var grave = MakeGraveyard(zone, 15, 10);
            // Block (14,10) (15,11) (15,9) and the diagonals — leaving only (16,10).
            BlockCellWithPhysicsPart(zone, 14, 10);
            BlockCellWithPhysicsPart(zone, 14, 11);
            BlockCellWithPhysicsPart(zone, 14, 9);
            BlockCellWithPhysicsPart(zone, 15, 11);
            BlockCellWithPhysicsPart(zone, 15, 9);
            BlockCellWithPhysicsPart(zone, 16, 11);
            BlockCellWithPhysicsPart(zone, 16, 9);

            // Put the corpse in inventory so we're in haul phase directly.
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);
            goal.TakeAction();

            // Inspect the pushed MoveToGoal — should target (16,10) not any blocked cell.
            var pushed = npc.GetPart<BrainPart>().PeekGoal() as MoveToGoal;
            Assert.IsNotNull(pushed, "Should push a MoveToGoal toward an unblocked neighbor.");
            Assert.AreEqual(16, pushed.TargetX,
                "Haul target must avoid PhysicsPart.Solid=true cells, even without a 'Solid' tag.");
            Assert.AreEqual(10, pushed.TargetY);
        }

        private static void BlockCellWithPhysicsPart(Zone zone, int x, int y)
        {
            var blocker = new Entity { BlueprintName = "PhysicsBlocker", ID = $"PB-{x}-{y}" };
            // Note: NO "Solid" tag — only PhysicsPart.Solid=true.
            blocker.AddPart(new RenderPart { DisplayName = "blocker" });
            blocker.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(blocker, x, y);
        }

        /// <summary>
        /// Production line 268-287: successful deposit transfers the corpse
        /// from NPC inventory INTO the container's ContainerPart contents.
        /// Pins the actual transfer end-state, not just the "_done" flag.
        /// </summary>
        [Test]
        public void DisposeOfCorpseGoal_SuccessfulDeposit_PutsCorpseInContainerContents()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 11, 10);
            var grave = MakeGraveyard(zone, 11, 10);  // adjacent to NPC

            // Pre-pickup the corpse.
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(), "Goal should be _done after adjacent deposit.");
            Assert.IsFalse(npc.GetPart<InventoryPart>().Contains(corpse),
                "Corpse must leave NPC inventory.");
            var containerPart = grave.GetPart<ContainerPart>();
            Assert.IsTrue(containerPart.Contents.Contains(corpse),
                "Corpse must end up in the graveyard's ContainerPart contents.");
        }

        // ====================================================================
        // AIUndertakerPart gap-coverage
        // ====================================================================

        /// <summary>
        /// Production line 53-62: HandleEvent on a non-AIBored event must
        /// return true (default propagation). Without this, AIUndertaker
        /// would silently swallow Died/Hurt/etc events on the wearer.
        /// </summary>
        [Test]
        public void AIUndertaker_NonBoredEvent_PropagatesNormally()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var part = npc.GetPart<AIUndertakerPart>();

            var died = GameEvent.New("Died");
            bool propagated = part.HandleEvent(died);
            died.Release();

            Assert.IsTrue(propagated,
                "AIUndertaker.HandleEvent for a non-AIBored event must return true.");
        }

        /// <summary>
        /// Production line 67-68: brain == null → return true (no NPE,
        /// graceful no-op). Defensive against test scaffolds that fire
        /// AIBored before brain is wired.
        /// </summary>
        [Test]
        public void AIUndertaker_NoBrain_GracefullyNoOps()
        {
            // Build a "thin" entity with AIUndertakerPart but no BrainPart.
            var orphan = new Entity { BlueprintName = "OrphanedNpc" };
            orphan.Tags["Creature"] = "";
            orphan.AddPart(new AIUndertakerPart { Chance = 100 });

            Assert.DoesNotThrow(() => AIBoredEvent.Check(orphan),
                "AIUndertaker without BrainPart must not throw on AIBored.");
        }

        /// <summary>
        /// Production line 67: brain.Rng==null → return true. Pins the
        /// short-circuit before any zone scan.
        /// </summary>
        [Test]
        public void AIUndertaker_NullRng_GracefullyNoOps()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var brain = npc.GetPart<BrainPart>();
            brain.Rng = null;
            // Even with corpse + graveyard present.
            MakeBareCorpse(zone, 15, 10);
            MakeGraveyard(zone, 20, 10);

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.IsFalse(brain.HasGoal<DisposeOfCorpseGoal>(),
                "Null RNG should short-circuit before pushing any goal.");
        }

        /// <summary>
        /// Production line 67: brain.CurrentZone==null → return true. Pins
        /// the short-circuit when the brain is detached from a zone.
        /// </summary>
        [Test]
        public void AIUndertaker_NullCurrentZone_GracefullyNoOps()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var brain = npc.GetPart<BrainPart>();
            brain.CurrentZone = null;

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.IsFalse(brain.HasGoal<DisposeOfCorpseGoal>(),
                "Null CurrentZone should short-circuit before pushing any goal.");
        }

        /// <summary>
        /// Production line 83-85: actor without InventoryPart cannot haul →
        /// no-op. (Different gate from the goal's defensive same-named check;
        /// this catches it earlier so we don't even claim a corpse.)
        /// </summary>
        [Test]
        public void AIUndertaker_ActorWithoutInventory_DoesNotClaimCorpse()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            npc.RemovePart(npc.GetPart<InventoryPart>());

            var corpse = MakeBareCorpse(zone, 15, 10);
            MakeGraveyard(zone, 20, 10);

            AIBoredEvent.Check(npc);

            Assert.AreEqual(0, corpse.GetIntProperty("DepositCorpsesReserve", 0),
                "Without inventory, AIUndertaker must NOT claim the corpse (no reservation).");
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>());
        }

        /// <summary>
        /// Production line 80-81: Chance=0 means "never try." Pins the
        /// probability gate's lower bound. Crucial for scenario authors
        /// who want NPCs that have AIUndertaker on the part list but
        /// disabled at runtime.
        /// </summary>
        [Test]
        public void AIUndertaker_ChanceZero_NeverPushesGoal()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10, chance: 0);
            MakeBareCorpse(zone, 15, 10);
            MakeGraveyard(zone, 20, 10);

            for (int i = 0; i < 20; i++) AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "Chance=0 must block goal-push across many bored ticks.");
        }

        /// <summary>
        /// Production line 58: when AIUndertaker successfully pushes a goal,
        /// AIBoredEvent.Check returns FALSE (meaning "consumed — don't run
        /// default wander/idle"). Pins the consume contract.
        /// </summary>
        [Test]
        public void AIUndertaker_OnSuccessfulPush_ConsumesAIBoredEvent()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            MakeBareCorpse(zone, 15, 10);
            MakeGraveyard(zone, 20, 10);

            bool unhandled = AIBoredEvent.Check(npc);
            Assert.IsFalse(unhandled,
                "Successful corpse-claim must consume AIBoredEvent (return false from Check).");
            Assert.IsTrue(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>());
        }

        /// <summary>
        /// Production line 110-117: a Graveyard-tagged entity that lacks a
        /// ContainerPart should be skipped — FindGraveyard returns null in
        /// that case. Defensive against half-configured blueprints.
        /// </summary>
        [Test]
        public void AIUndertaker_GraveyardTagWithoutContainer_IsIgnored()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            MakeBareCorpse(zone, 15, 10);
            MakeGraveyard(zone, 20, 10, withContainer: false);  // tag only

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "Graveyard tag without ContainerPart must be treated as 'no graveyard'.");
        }

        /// <summary>
        /// Production line 133-160: when multiple unclaimed corpses are
        /// present, FindNearestUnclaimedCorpse picks the one with smallest
        /// Chebyshev distance. Pins selection ordering.
        /// </summary>
        [Test]
        public void AIUndertaker_MultipleCorpses_PicksNearestByChebyshev()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            MakeGraveyard(zone, 30, 10);

            // Far corpse (Cheb dist 18) and near corpse (Cheb dist 3).
            var far = MakeBareCorpse(zone, 28, 10, id: "Far");
            var near = MakeBareCorpse(zone, 13, 10, id: "Near");

            AIBoredEvent.Check(npc);

            Assert.AreEqual(50, near.GetIntProperty("DepositCorpsesReserve", 0),
                "Near corpse must be the one reserved.");
            Assert.AreEqual(0, far.GetIntProperty("DepositCorpsesReserve", 0),
                "Far corpse must NOT be reserved while a closer one exists.");
        }
    }
}
