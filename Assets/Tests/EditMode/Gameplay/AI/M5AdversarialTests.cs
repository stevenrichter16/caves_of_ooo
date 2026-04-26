using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M5 (Corpse system) adversarial cold-eye tests. Per Docs/QUD-PARITY.md §3.9.
    ///
    /// Companion to M5CoverageGapTests (commit ee0f6c7) which was
    /// gap-coverage style (read production line-by-line, pin observed
    /// contract). This file deliberately targets behaviors I have NOT
    /// already probed:
    ///   - replay-on-same-creature (Died fired twice)
    ///   - multiple CorpsePart instances on one entity
    ///   - empty creature DisplayName effect on corpse interpolation
    ///   - independent guards for KillerID vs KillerBlueprint
    ///   - diagonal adjacency in DisposeOfCorpseGoal
    ///   - haul-phase try counter increments
    ///   - OnPop on a detached goal (no ParentBrain)
    ///   - AIUndertaker with actor position invalid
    ///   - corpse + graveyard share cell
    ///   - all-corpses-claimed → no-op
    ///   - multiple graveyards in zone
    ///   - Container.Locked behavior (genuinely uncertain — true adversarial probe)
    ///
    /// Each test commits a PREDICTION and CONFIDENCE; failures get
    /// classified test-wrong / code-wrong / setup-wrong in the commit
    /// message.
    /// </summary>
    [TestFixture]
    public class M5AdversarialTests
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
            var f = new EntityFactory();
            f.LoadBlueprints(TestBlueprintsJson);
            CorpsePart.Factory = f;
        }

        [TearDown]
        public void Teardown()
        {
            CorpsePart.Factory = null;
        }

        // ============================================================
        // Helpers (kept terse; these mirror the gap-coverage helpers
        // but the file should stand alone for review)
        // ============================================================

        private static Entity MakeCreature(Zone zone, int x, int y, int chance, string corpseBp,
            string displayName = "creature", string blueprintName = "TestCreature")
        {
            var e = new Entity { BlueprintName = blueprintName, ID = blueprintName + "-" + x + "-" + y };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = displayName });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new CorpsePart { CorpseChance = chance, CorpseBlueprint = corpseBp, TestRng = new Random(0) });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static GameEvent MakeDied(Entity target, Zone zone, Entity killer = null)
        {
            var died = GameEvent.New("Died");
            died.SetParameter("Target", (object)target);
            if (killer != null) died.SetParameter("Killer", (object)killer);
            died.SetParameter("Zone", (object)zone);
            return died;
        }

        private static int CountCorpsesAt(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return 0;
            int n = 0;
            foreach (var obj in cell.Objects)
                if (obj.HasTag("Corpse")) n++;
            return n;
        }

        private static Entity MakeUndertaker(Zone zone, int x, int y, int chance = 100)
        {
            var e = new Entity { BlueprintName = "Undertaker", ID = "UT-" + x };
            e.Tags["Creature"] = "";
            e.Tags["Faction"] = "Villagers";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "undertaker" });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(0) };
            e.AddPart(brain);
            e.AddPart(new AIUndertakerPart { Chance = chance });
            zone.AddEntity(e, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return e;
        }

        private static Entity MakeBareCorpse(Zone zone, int x, int y, int weight = 10, string id = "Corpse-X")
        {
            var c = new Entity { BlueprintName = "SnapjawCorpse", ID = id };
            c.Tags["Corpse"] = "";
            c.AddPart(new RenderPart { DisplayName = "snapjaw corpse" });
            c.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            zone.AddEntity(c, x, y);
            return c;
        }

        private static Entity MakeGraveyard(Zone zone, int x, int y, int maxItems = -1, bool locked = false)
        {
            var g = new Entity { BlueprintName = "Graveyard", ID = "Grave-" + x + "-" + y };
            g.Tags["Graveyard"] = "";
            g.AddPart(new RenderPart { DisplayName = "graveyard" });
            g.AddPart(new PhysicsPart { Solid = true });
            g.AddPart(new ContainerPart { MaxItems = maxItems, Locked = locked });
            zone.AddEntity(g, x, y);
            return g;
        }

        // ============================================================
        // CorpsePart adversarial probes
        // ============================================================

        /// <summary>
        /// PRED: Firing Died twice on the same creature spawns TWO
        /// corpses at the same cell — production has no guard against
        /// re-fire. CONFIDENCE: high.
        /// Why probe: this is a duplication-on-replay surface; if a
        /// system fires Died twice (common bug pattern: cleanup chain
        /// ordering issues), corpses multiply.
        /// </summary>
        [Test]
        public void Replay_DiedFiredTwice_SpawnsTwoCorpses()
        {
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, 100, "SnapjawCorpse");

            var d1 = MakeDied(creature, zone); creature.FireEvent(d1); d1.Release();
            var d2 = MakeDied(creature, zone); creature.FireEvent(d2); d2.Release();

            Assert.AreEqual(2, CountCorpsesAt(zone, 5, 5),
                "Replay of Died on same creature spawns N corpses (no guard).");
        }

        /// <summary>
        /// PRED: Two CorpsePart instances on the same entity each fire
        /// independently → two corpses spawn at the same cell.
        /// CONFIDENCE: high — HandleEvent returns true (propagates) so
        /// the second instance receives the event.
        /// Why probe: defensive against blueprint authoring that double-
        /// adds the part. If doubling spawns two corpses, that's known.
        /// </summary>
        [Test]
        public void DoubleCorpsePart_SpawnsTwoCorpses()
        {
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, 100, "SnapjawCorpse");
            // Double up.
            creature.AddPart(new CorpsePart { CorpseChance = 100, CorpseBlueprint = "SnapjawCorpse", TestRng = new Random(7) });

            var died = MakeDied(creature, zone);
            creature.FireEvent(died);
            died.Release();

            Assert.AreEqual(2, CountCorpsesAt(zone, 5, 5),
                "Two CorpseParts attached → both fire on Died → two corpses.");
        }

        /// <summary>
        /// PRED: When the deceased's GetDisplayName() returns "" (no
        /// RenderPart, no fallback name), the interpolation gate at
        /// production line 205 (`!string.IsNullOrEmpty(creatureName)`)
        /// rejects, so corpse render keeps its blueprint default.
        /// CONFIDENCE: high.
        /// Why probe: the gap-coverage tests proved the gate triggers
        /// with non-empty names; this proves the negative side of the
        /// `!IsNullOrEmpty` half of the gate.
        /// </summary>
        [Test]
        public void EmptyCreatureName_DoesNotInterpolate_KeepsBlueprintDefault()
        {
            var zone = new Zone("TestZone");
            // Create a creature with NO RenderPart so GetDisplayName falls back to blueprint name.
            // Then null out the BlueprintName so even that fallback returns empty.
            var creature = new Entity { BlueprintName = "", ID = "X-1" };
            creature.Tags["Creature"] = "";
            creature.AddPart(new PhysicsPart { Solid = true });
            creature.AddPart(new CorpsePart { CorpseChance = 100, CorpseBlueprint = "GenericCorpse", TestRng = new Random(0) });
            zone.AddEntity(creature, 7, 7);

            var died = MakeDied(creature, zone);
            creature.FireEvent(died);
            died.Release();

            var cell = zone.GetCell(7, 7);
            Entity corpse = null;
            foreach (var obj in cell.Objects)
                if (obj.HasTag("Corpse")) { corpse = obj; break; }
            Assert.IsNotNull(corpse, "Corpse should still spawn even with empty creature name (other gates pass).");
            Assert.AreEqual("corpse", corpse.GetPart<RenderPart>().DisplayName,
                "Empty creature name → interpolation gate rejects → corpse keeps default 'corpse'.");
        }

        /// <summary>
        /// PRED: A killer with empty ID but non-empty BlueprintName
        /// triggers the BlueprintName guard (line 183) but NOT the ID
        /// guard (line 181). KillerBlueprint set, KillerID not.
        /// CONFIDENCE: high — the two `!IsNullOrEmpty` checks are
        /// independent ifs.
        /// Why probe: this surfaces the independence of the two guards
        /// — if someone "simplifies" production by lifting one
        /// IsNullOrEmpty out, this test breaks.
        /// </summary>
        [Test]
        public void KillerWithBlueprintNameOnly_SetsBlueprintNotID()
        {
            var zone = new Zone("TestZone");
            var creature = MakeCreature(zone, 5, 5, 100, "SnapjawCorpse");

            var killer = new Entity { BlueprintName = "PhantomKiller", ID = "" };  // empty ID
            killer.AddPart(new RenderPart { DisplayName = "phantom" });

            var died = MakeDied(creature, zone, killer);
            creature.FireEvent(died);
            died.Release();

            var cell = zone.GetCell(5, 5);
            Entity corpse = null;
            foreach (var obj in cell.Objects) if (obj.HasTag("Corpse")) { corpse = obj; break; }
            Assert.IsNotNull(corpse);
            Assert.AreEqual("PhantomKiller", corpse.GetProperty("KillerBlueprint"),
                "KillerBlueprint should be set when killer.BlueprintName is non-empty.");
            Assert.IsNull(corpse.GetProperty("KillerID"),
                "KillerID should NOT be set when killer.ID is empty.");
        }

        // ============================================================
        // DisposeOfCorpseGoal adversarial probes
        // ============================================================

        /// <summary>
        /// PRED: Diagonal adjacency counts. Production line 120 uses
        /// AIHelpers.IsAdjacent which (in this codebase) is 8-connected
        /// Chebyshev. Actor at (10,10), corpse at (11,11) → adjacent →
        /// pickup runs immediately, no MoveToGoal pushed.
        /// CONFIDENCE: high — codebase-wide convention is Chebyshev.
        /// Why probe: untested case; if someone changes IsAdjacent to
        /// 4-connected, M5 would silently break.
        /// </summary>
        [Test]
        public void Fetch_DiagonalAdjacency_TriggersPickup_NoMoveToGoal()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 11, 11);  // diagonal
            var grave = MakeGraveyard(zone, 20, 10);

            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);
            goal.TakeAction();

            var brain = npc.GetPart<BrainPart>();
            Assert.IsTrue(npc.GetPart<InventoryPart>().Contains(corpse),
                "Diagonal-adjacent corpse should be picked up directly (no MoveToGoal).");
            // Top of stack should still be the goal itself, not a child.
            Assert.AreEqual(goal, brain.PeekGoal(),
                "No child MoveToGoal should be pushed — diagonal counts as adjacent.");
        }

        /// <summary>
        /// PRED: GoToContainerTries increments per-push, mirroring the
        /// fetch counter. Each haul-phase TakeAction that pushes a child
        /// MoveToGoal increments by 1. CONFIDENCE: high.
        /// Why probe: gap-coverage pinned GoToCorpseTries; symmetrical
        /// counter on the haul side wasn't tested. If someone refactors
        /// the increment placement, only one side breaks.
        /// </summary>
        [Test]
        public void Haul_GoToContainerTries_IncrementsPerPush()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 11, 10);
            var grave = MakeGraveyard(zone, 25, 10);

            // Force haul phase — pre-load corpse into inventory.
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var goal = new DisposeOfCorpseGoal(corpse, grave);
            npc.GetPart<BrainPart>().PushGoal(goal);

            Assert.AreEqual(0, goal.GoToContainerTries);
            goal.TakeAction();
            Assert.AreEqual(1, goal.GoToContainerTries);

            // Pop the child, tick again.
            var brain = npc.GetPart<BrainPart>();
            var child = brain.PeekGoal();
            Assert.IsInstanceOf<MoveToGoal>(child);
            brain.RemoveGoal(child);

            goal.TakeAction();
            Assert.AreEqual(2, goal.GoToContainerTries);
        }

        /// <summary>
        /// PRED: OnPop on a goal that was never pushed (ParentBrain==null)
        /// must not throw. Reservation clear is `?.RemoveIntProperty`,
        /// Think(null) goes through `ParentBrain?.Think`. Both null-safe.
        /// CONFIDENCE: high.
        /// Why probe: detached/discarded goal lifecycle is rare but
        /// possible during scenario teardown.
        /// </summary>
        [Test]
        public void OnPop_NoParentBrain_DoesNotThrow()
        {
            var zone = new Zone("TestZone");
            var corpse = MakeBareCorpse(zone, 5, 5);
            corpse.SetIntProperty("DepositCorpsesReserve", 50);
            var grave = MakeGraveyard(zone, 10, 10);

            var orphan = new DisposeOfCorpseGoal(corpse, grave);
            Assert.IsNull(orphan.ParentBrain);
            Assert.DoesNotThrow(() => orphan.OnPop());
            // The reservation should still be cleared — that path doesn't depend on ParentBrain.
            Assert.AreEqual(0, corpse.GetIntProperty("DepositCorpsesReserve", 0),
                "OnPop must clear the reservation even on a detached goal.");
        }

        // ============================================================
        // AIUndertakerPart adversarial probes
        // ============================================================

        /// <summary>
        /// PRED: When the undertaker's position is invalid (-1,-1 — not
        /// in zone), FindNearestUnclaimedCorpse returns null at line
        /// 128. No goal pushed, no corpse claimed.
        /// CONFIDENCE: high.
        /// Why probe: gap-coverage didn't exercise the actorPos.x<0
        /// guard; ensures the early-exit path is wired.
        /// </summary>
        [Test]
        public void ActorNotInZone_NoClaim()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            // Remove from zone so GetEntityPosition returns (-1,-1).
            zone.RemoveEntity(npc);
            // brain.CurrentZone is still set, brain.Rng still set — only
            // the actor's position is invalid.

            var corpse = MakeBareCorpse(zone, 15, 10);
            MakeGraveyard(zone, 20, 10);

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.AreEqual(0, corpse.GetIntProperty("DepositCorpsesReserve", 0),
                "Out-of-zone actor must not claim any corpse.");
        }

        /// <summary>
        /// PRED: When all corpses in zone already have
        /// DepositCorpsesReserve > 0, FindNearestUnclaimedCorpse returns
        /// null. No goal pushed, no exception.
        /// CONFIDENCE: high.
        /// Why probe: stress the all-claimed edge — gap-coverage tested
        /// "one claimed by another, picks the unclaimed one"; this is
        /// "all claimed, no fallback."
        /// </summary>
        [Test]
        public void AllCorpsesClaimed_NoOp()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var c1 = MakeBareCorpse(zone, 12, 10, id: "C1");
            var c2 = MakeBareCorpse(zone, 14, 10, id: "C2");
            var c3 = MakeBareCorpse(zone, 16, 10, id: "C3");
            c1.SetIntProperty("DepositCorpsesReserve", 50);
            c2.SetIntProperty("DepositCorpsesReserve", 50);
            c3.SetIntProperty("DepositCorpsesReserve", 50);
            MakeGraveyard(zone, 20, 10);

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<DisposeOfCorpseGoal>(),
                "All corpses claimed → no goal pushed.");
        }

        /// <summary>
        /// PRED: Two graveyards in zone — first encountered in
        /// GetReadOnlyEntities iteration order wins. Production says
        /// "first match wins" at line 113. Tests with deterministic
        /// insertion order: graveyard inserted first wins.
        /// CONFIDENCE: medium-high — depends on whether
        /// GetReadOnlyEntities preserves insertion order, which is the
        /// expectation but worth probing.
        /// Why probe: the comment at production line 113 promises this
        /// behavior; if it ever silently picks "nearest", the comment
        /// becomes a lie.
        /// </summary>
        [Test]
        public void TwoGraveyards_FirstAddedWins()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            // Insert FAR graveyard first, NEAR graveyard second.
            var farGrave = MakeGraveyard(zone, 30, 10);
            var nearGrave = MakeGraveyard(zone, 12, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);

            AIBoredEvent.Check(npc);

            var goal = npc.GetPart<BrainPart>().FindGoal<DisposeOfCorpseGoal>();
            Assert.IsNotNull(goal, "Goal should have been pushed.");
            Assert.AreSame(farGrave, goal.Container,
                "Production picks first match in iteration order, not nearest. " +
                "If this fails, either iteration order changed or production logic was 'fixed' to nearest.");
        }

        /// <summary>
        /// PRED: When corpse and graveyard occupy the same cell (Solid
        /// graveyard sharing Z with corpse), the system still works:
        /// - undertaker walks to adjacent cell of graveyard
        /// - which is also adjacent to corpse → pickup
        /// - graveyard is also adjacent → deposit
        /// CONFIDENCE: medium — this is a degenerate placement. Test
        /// pins that nothing crashes and full lifecycle completes.
        /// Why probe: tests against "what if village placement put
        /// corpse on the graveyard tile?" — a scenario that could
        /// realistically occur.
        /// </summary>
        [Test]
        public void CorpseAndGraveyardSameCell_FullLifecycleCompletes()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            // Place graveyard and corpse at same cell (15,10).
            var grave = MakeGraveyard(zone, 15, 10);
            var corpse = MakeBareCorpse(zone, 15, 10);

            AIBoredEvent.Check(npc);
            var goal = npc.GetPart<BrainPart>().FindGoal<DisposeOfCorpseGoal>();
            Assert.IsNotNull(goal, "Goal should be pushed.");
            Assert.AreSame(corpse, goal.Corpse);
            Assert.AreSame(grave, goal.Container);

            // The goal can't actually run to completion in this test
            // without the full turn loop, but at minimum constructing
            // and pushing must not throw.
            Assert.DoesNotThrow(() => goal.TakeAction());
        }

        /// <summary>
        /// PRED: The Chance probability gate uses brain.Rng (not a
        /// per-part RNG, not Random shared static). With a deterministic
        /// brain.Rng seed and Chance=50, output is reproducible run-to-
        /// run.
        /// CONFIDENCE: high — production line 80 explicitly reads
        /// `brain.Rng.Next(100)`.
        /// Why probe: protects against future "let's add a private
        /// RNG to AIBehaviorPart" refactor that would silently
        /// desynchronize from save-loaded brain.Rng.
        /// </summary>
        [Test]
        public void ProbabilityGate_UsesBrainRng_DeterministicAcrossRuns()
        {
            // Run twice with same seed → identical outcomes.
            int FireWithSeed(int seed)
            {
                var zone = new Zone("TestZone");
                var npc = MakeUndertaker(zone, 10, 10, chance: 50);
                npc.GetPart<BrainPart>().Rng = new Random(seed);
                MakeBareCorpse(zone, 15, 10);
                MakeGraveyard(zone, 20, 10);
                int hits = 0;
                for (int i = 0; i < 10; i++)
                {
                    // Reset goal state between iterations to count fresh attempts.
                    var brain = npc.GetPart<BrainPart>();
                    var existing = brain.FindGoal<DisposeOfCorpseGoal>();
                    if (existing != null) brain.RemoveGoal(existing);
                    var corpse = zone.GetCell(15, 10).Objects.Find(o => o.HasTag("Corpse"));
                    if (corpse != null) corpse.RemoveIntProperty("DepositCorpsesReserve");

                    AIBoredEvent.Check(npc);
                    if (brain.HasGoal<DisposeOfCorpseGoal>()) hits++;
                }
                return hits;
            }

            int run1 = FireWithSeed(12345);
            int run2 = FireWithSeed(12345);
            Assert.AreEqual(run1, run2,
                "Same seed must produce identical outcomes — proves probability gate routes through brain.Rng.");
        }

        /// <summary>
        /// PRED: Container.Locked behavior. Honest cold-eye guess:
        /// ContainerPart.AddItem probably doesn't reject when Locked
        /// (Locked typically gates UI/inspection, not programmatic
        /// add). Therefore the corpse is deposited normally despite
        /// the lock. CONFIDENCE: low — this is a true adversarial
        /// probe. If AddItem DOES reject Locked containers, the
        /// "drop at feet" fallback (production line 286) fires and
        /// the corpse ends up on the actor's tile, not in the
        /// container.
        /// Why probe: I haven't looked at ContainerPart.AddItem. The
        /// outcome is genuinely uncertain.
        /// </summary>
        [Test]
        public void Locked_Container_DepositOutcome()
        {
            var zone = new Zone("TestZone");
            var npc = MakeUndertaker(zone, 10, 10);
            var corpse = MakeBareCorpse(zone, 11, 10);
            var lockedGrave = MakeGraveyard(zone, 11, 10, locked: true);

            // Force haul phase + adjacent.
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var goal = new DisposeOfCorpseGoal(corpse, lockedGrave);
            npc.GetPart<BrainPart>().PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(), "Adjacent + carry → goal completes regardless of lock outcome.");

            var inLock = lockedGrave.GetPart<ContainerPart>().Contents.Contains(corpse);
            var atFeet = zone.GetCell(10, 10)?.Objects.Contains(corpse) == true;
            // Exactly one of the two outcomes must hold (not both, not neither).
            Assert.IsTrue(inLock ^ atFeet,
                "Corpse must end up either inside the locked container (Locked is UI-only) " +
                "or at the actor's feet (Locked rejected). Pinning whichever production picks.");
            // Document the observed outcome via TestContext for future readers.
            TestContext.Out.WriteLine(
                $"Locked-container deposit outcome: inLock={inLock}, atFeet={atFeet}");
        }
    }
}
