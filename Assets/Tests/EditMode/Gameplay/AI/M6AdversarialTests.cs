using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M6 (Rune system) adversarial cold-eye tests. Per Docs/QUD-PARITY.md §3.9.
    ///
    /// Companion to M6CoverageGapTests — that file pinned observed
    /// contracts after reading production line-by-line; THIS file
    /// deliberately probes untested edges with predictions made
    /// before re-reading.
    ///
    /// Each test commits a PREDICTION and CONFIDENCE label; failures
    /// get classified test-wrong / code-wrong / setup-wrong in the
    /// commit message.
    ///
    /// Surfaces probed:
    ///   - MovementSystem: multi-occupant dispatch ordering; mover-pushed-off-cell mid-dispatch
    ///   - TriggerOnStepPart: faction case-sensitivity; rune-already-consumed re-fire safety
    ///   - LayRuneGoal: at-construction at-target shortcut; rune blueprint missing TriggerOnStepPart
    ///   - AILayRunePart: two-cultist race for same target; cache invalidation on RuneBlueprints reassign;
    ///                    blueprint list with internal empties; reassignment with different reference but same content
    /// </summary>
    [TestFixture]
    public class M6AdversarialTests
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
                    ""Name"": ""TestRune"",
                    ""Inherits"": ""PhysicalObject"",
                    ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [
                            { ""Key"": ""DisplayName"", ""Value"": ""rune"" },
                            { ""Key"": ""RenderString"", ""Value"": ""*"" }
                        ]},
                        { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""false"" }] },
                        { ""Name"": ""RuneFlameTrigger"", ""Params"": [
                            { ""Key"": ""Damage"", ""Value"": ""4"" }
                        ]}
                    ],
                    ""Tags"": [
                        { ""Key"": ""Rune"", ""Value"": """" }
                    ]
                },
                {
                    ""Name"": ""PlainRune"",
                    ""Inherits"": ""PhysicalObject"",
                    ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [
                            { ""Key"": ""DisplayName"", ""Value"": ""plain rune"" },
                            { ""Key"": ""RenderString"", ""Value"": ""*"" }
                        ]},
                        { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""false"" }] }
                    ],
                    ""Tags"": [
                        { ""Key"": ""Rune"", ""Value"": """" }
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
            LayRuneGoal.Factory = null;
            LayRuneGoal.FactoryNullWarned = false;
            LayRuneGoal.BlueprintMissingWarned = false;
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static EntityFactory MakeFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(TestBlueprintsJson);
            return f;
        }

        private static Entity MakeMover(Zone zone, int x, int y, int hp = 20, string id = "mover-1")
        {
            var e = new Entity { BlueprintName = "Mover", ID = id };
            e.AddPart(new RenderPart { DisplayName = "mover" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeRune(Zone zone, int x, int y, TriggerOnStepPart part, string id = null)
        {
            var e = new Entity { BlueprintName = "TestRune", ID = id ?? $"rune-{x}-{y}" };
            e.AddPart(new RenderPart { DisplayName = "rune" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.SetTag("Rune", "");
            e.AddPart(part);
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeCultist(Zone zone, int x, int y, string id = null,
            int chance = 100, int searchRadius = 3, string runes = "TestRune")
        {
            var e = new Entity { BlueprintName = "RuneCultist", ID = id ?? "cult-" + x };
            e.Tags["Creature"] = "";
            e.Tags["Faction"] = "Cultists";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "cultist" });
            e.AddPart(new PhysicsPart { Solid = true });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(0) };
            e.AddPart(brain);
            e.AddPart(new AILayRunePart
            {
                Chance = chance,
                MaxRunesPerZone = 50,
                SearchRadius = searchRadius,
                RuneBlueprints = runes
            });
            zone.AddEntity(e, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return e;
        }

        /// <summary>Receipt that captures dispatch order across multiple invocations.</summary>
        private class OrderRecorder : Part
        {
            public string Name;
            public System.Collections.Generic.List<string> Sequence;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "EntityEnteredCell") Sequence.Add(Name);
                return true;
            }
        }

        // ============================================================
        // MovementSystem adversarial probes
        // ============================================================

        /// <summary>
        /// PRED: Multiple non-mover occupants of the destination cell
        /// receive EntityEnteredCell in the order they appear in
        /// `cell.Objects`. CONFIDENCE: high — production iterates the
        /// scratch list (populated by a forward loop over Objects)
        /// in index order.
        /// </summary>
        [Test]
        public void MultipleOccupants_ReceiveEventInInsertionOrder()
        {
            var zone = new Zone("TestZone");
            var seq = new System.Collections.Generic.List<string>();

            var w1 = new Entity { BlueprintName = "W1", ID = "w1" };
            w1.AddPart(new PhysicsPart { Solid = false });
            w1.AddPart(new OrderRecorder { Name = "W1", Sequence = seq });
            zone.AddEntity(w1, 5, 5);

            var w2 = new Entity { BlueprintName = "W2", ID = "w2" };
            w2.AddPart(new PhysicsPart { Solid = false });
            w2.AddPart(new OrderRecorder { Name = "W2", Sequence = seq });
            zone.AddEntity(w2, 5, 5);

            var w3 = new Entity { BlueprintName = "W3", ID = "w3" };
            w3.AddPart(new PhysicsPart { Solid = false });
            w3.AddPart(new OrderRecorder { Name = "W3", Sequence = seq });
            zone.AddEntity(w3, 5, 5);

            var mover = MakeMover(zone, 4, 5);
            MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);

            CollectionAssert.AreEqual(new[] { "W1", "W2", "W3" }, seq,
                "Dispatch order must match insertion order in cell.Objects.");
        }

        /// <summary>
        /// PRED: When a listener's payload moves the mover OUT of the
        /// destination cell mid-dispatch, the loop's
        /// `!occupants.Contains(mover)` guard breaks and subsequent
        /// listeners do NOT receive the event. Same class as the CR-01
        /// mover-died-mid-sweep break, but for teleport-out, not death.
        /// CONFIDENCE: high — the guard is symmetric (it just checks
        /// presence, not death-state).
        /// </summary>
        [Test]
        public void MoverPushedOffCell_StopsRemainingDispatches()
        {
            var zone = new Zone("TestZone");

            // Listener 1: when fired, removes the mover from the zone (simulates teleport-out).
            var seq = new System.Collections.Generic.List<string>();
            var teleport = new Entity { BlueprintName = "Teleport", ID = "tp" };
            teleport.AddPart(new PhysicsPart { Solid = false });
            teleport.AddPart(new TestPushOffPart { Sequence = seq, Tag = "L1-fire" });
            zone.AddEntity(teleport, 5, 5);

            // Listener 2: should NOT fire because the mover is gone.
            var laggard = new Entity { BlueprintName = "Laggard", ID = "lag" };
            laggard.AddPart(new PhysicsPart { Solid = false });
            laggard.AddPart(new OrderRecorder { Name = "L2-fire", Sequence = seq });
            zone.AddEntity(laggard, 5, 5);

            var mover = MakeMover(zone, 4, 5);
            MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);

            CollectionAssert.AreEqual(new[] { "L1-fire" }, seq,
                "After listener 1 removes the mover, listener 2 must NOT fire (Contains-guard break).");
        }

        private class TestPushOffPart : Part
        {
            public System.Collections.Generic.List<string> Sequence;
            public string Tag;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "EntityEnteredCell")
                {
                    Sequence.Add(Tag);
                    var actor = e.GetParameter<Entity>("Actor");
                    var cell = e.GetParameter<Cell>("Cell");
                    cell?.ParentZone?.RemoveEntity(actor);
                }
                return true;
            }
        }

        // ============================================================
        // TriggerOnStepPart adversarial probes
        // ============================================================

        /// <summary>
        /// PRED: TriggerFaction comparison is exact-string (case-sensitive).
        /// Layer faction "Cultists" + actor faction "cultists" (lower)
        /// → strings differ → trigger FIRES. CONFIDENCE: high — production
        /// uses `==` on strings, no ToLower.
        /// Why probe: future "make faction lookup case-insensitive"
        /// refactor would silently change rune behavior.
        /// </summary>
        [Test]
        public void TriggerFaction_ComparisonIsCaseSensitive()
        {
            var zone = new Zone("TestZone");
            MakeRune(zone, 5, 5, new RuneFlameTriggerPart
            {
                Damage = 5,
                TriggerFaction = "Cultists"
            });
            var actor = MakeMover(zone, 4, 5);
            actor.SetTag("Faction", "cultists");  // lowercase variant

            MovementSystem.TryMove(actor, zone, dx: 1, dy: 0);

            // Trigger fired → HP dropped by 5.
            Assert.AreEqual(15, actor.Statistics["Hitpoints"].BaseValue,
                "Case mismatch ('cultists' vs 'Cultists') must NOT match — trigger fires.");
        }

        /// <summary>
        /// PRED: After ConsumeOnTrigger removes the rune, a SECOND
        /// move into the same cell finds no rune — second mover gets
        /// no payload. Trivial per-se but pins the consume contract
        /// end-to-end: not just "rune removed from cell" but
        /// "subsequent steppers unaffected." CONFIDENCE: high.
        /// </summary>
        [Test]
        public void ConsumedRune_DoesNotFireOnSecondStepper()
        {
            var zone = new Zone("TestZone");
            MakeRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 5 });

            var first = MakeMover(zone, 4, 5, hp: 20, id: "first");
            MovementSystem.TryMove(first, zone, dx: 1, dy: 0);
            Assert.AreEqual(15, first.Statistics["Hitpoints"].BaseValue, "First stepper triggered rune.");

            var second = MakeMover(zone, 6, 5, hp: 20, id: "second");
            MovementSystem.TryMove(second, zone, dx: -1, dy: 0);
            Assert.AreEqual(20, second.Statistics["Hitpoints"].BaseValue,
                "Second stepper enters consumed rune cell — must take no damage.");
        }

        // ============================================================
        // LayRuneGoal adversarial probes
        // ============================================================

        /// <summary>
        /// PRED: When the actor's position equals the target at the
        /// FIRST TakeAction (no walking needed), LayRune fires
        /// immediately and _done flips true on the same tick. No
        /// MoveToGoal child pushed, no MoveTries increment.
        /// CONFIDENCE: high — production line 125-130 short-circuits
        /// before MoveTries increment.
        /// </summary>
        [Test]
        public void LayRuneGoal_AtTargetAtConstruction_LaysImmediately()
        {
            LayRuneGoal.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);

            var goal = new LayRuneGoal(targetX: 5, targetY: 5, runeBlueprint: "TestRune");
            npc.GetPart<BrainPart>().PushGoal(goal);

            goal.TakeAction();

            Assert.IsTrue(goal.Finished(), "At-target construction → _done after first TakeAction.");
            Assert.AreEqual(0, goal.MoveTries, "No walk needed → MoveTries must remain 0.");

            Entity rune = null;
            foreach (var obj in zone.GetCell(5, 5).Objects)
                if (obj.HasTag("Rune")) { rune = obj; break; }
            Assert.IsNotNull(rune, "Rune must be spawned at target.");
        }

        /// <summary>
        /// PRED: When the spawned rune's blueprint has no
        /// TriggerOnStepPart, the faction-stamp branch is skipped
        /// safely (production line 188-193, `if (trigger != null)`).
        /// The rune still spawns at target and the goal completes
        /// normally — no NPE, no hang. CONFIDENCE: high.
        /// Why probe: not all rune-tagged entities will be triggers
        /// (think: decorative shrine markers) and the goal's faction
        /// stamp must tolerate that.
        /// </summary>
        [Test]
        public void LayRuneGoal_RuneWithoutTriggerPart_SpawnsCleanly_NoStamp()
        {
            LayRuneGoal.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);

            var goal = new LayRuneGoal(targetX: 5, targetY: 5, runeBlueprint: "PlainRune");
            npc.GetPart<BrainPart>().PushGoal(goal);
            Assert.DoesNotThrow(() => goal.TakeAction());

            Assert.IsTrue(goal.Finished());
            Entity rune = null;
            foreach (var obj in zone.GetCell(5, 5).Objects)
                if (obj.HasTag("Rune")) { rune = obj; break; }
            Assert.IsNotNull(rune, "PlainRune (no TriggerOnStepPart) should still spawn.");
            Assert.IsNull(rune.GetPart<TriggerOnStepPart>(),
                "Confirming the test setup — no TriggerOnStepPart was attached.");
        }

        // ============================================================
        // AILayRunePart adversarial probes
        // ============================================================

        /// <summary>
        /// PRED: M6 ships WITHOUT a target-cell reservation system
        /// (different from M5's DepositCorpsesReserve). Two cultists
        /// bored on the same tick can both pick the same target cell
        /// and both push goals — both walk there, the first to arrive
        /// places the rune, the second arrives, sees the cell already
        /// runed (line 177 cell.HasObjectWithTag("Rune") check would
        /// reject it on a SUBSEQUENT pick — but the goal is already
        /// pushed targeting that cell). CONFIDENCE: medium — the
        /// outcome of "two goals targeting same cell" depends on what
        /// LayRuneGoal does when at-target-but-cell-already-runed.
        ///
        /// Probe specifically: do BOTH cultists push goals targeting
        /// the same cell? If yes, the M6 audit comment about "no
        /// reservation" is honest and the contract holds.
        /// </summary>
        [Test]
        public void TwoCultists_SameBoredTick_NoReservation_BothPushGoals()
        {
            var zone = new Zone("TestZone");
            // Both cultists adjacent to a single 1-cell pocket so candidate set is tiny.
            // We don't need to force same cell — just confirm both push SOMETHING
            // when no reservation is present.
            var c1 = MakeCultist(zone, 5, 5, id: "c1");
            var c2 = MakeCultist(zone, 6, 5, id: "c2");

            AIBoredEvent.Check(c1);
            AIBoredEvent.Check(c2);

            Assert.IsTrue(c1.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Cultist 1 should push LayRuneGoal.");
            Assert.IsTrue(c2.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Cultist 2 should also push LayRuneGoal — no reservation system in M6.");
        }

        /// <summary>
        /// PRED: When the RuneBlueprints field is reassigned to a
        /// different string (different content), the cached
        /// `_runeList` must invalidate on the next PickRuneBlueprint
        /// call. Production line 211 checks `_runeListSource != RuneBlueprints`
        /// — different reference triggers re-parse.
        /// CONFIDENCE: high.
        /// </summary>
        [Test]
        public void AILayRune_RuneBlueprintsChanged_CacheInvalidates()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5, runes: "TestRune");
            var part = npc.GetPart<AILayRunePart>();

            // First bored tick: cache populated with ["TestRune"].
            AIBoredEvent.Check(npc);
            var goal1 = npc.GetPart<BrainPart>().FindGoal<LayRuneGoal>();
            Assert.IsNotNull(goal1);
            Assert.AreEqual("TestRune", goal1.RuneBlueprint);

            // Reassign RuneBlueprints + clear goal stack.
            npc.GetPart<BrainPart>().RemoveGoal(goal1);
            part.RuneBlueprints = "PlainRune";

            AIBoredEvent.Check(npc);
            var goal2 = npc.GetPart<BrainPart>().FindGoal<LayRuneGoal>();
            Assert.IsNotNull(goal2);
            Assert.AreEqual("PlainRune", goal2.RuneBlueprint,
                "Cache must invalidate when RuneBlueprints field is reassigned.");
        }

        /// <summary>
        /// PRED: A blueprint list with internal empty entries
        /// ("A,,B") parses to 2 entries (A and B), with the empty
        /// in-between skipped. Production line 224-225 trims and
        /// only adds entries with `s.Length > 0`. CONFIDENCE: high.
        /// </summary>
        [Test]
        public void AILayRune_BlueprintList_SkipsInternalEmpties()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5, runes: "TestRune,,PlainRune");
            // Determinism: with seed 0, rng.Next(2) is deterministic — we just
            // need to verify the picked blueprint is one of the two non-empty
            // entries (NEVER an empty string that would later FailToParent).
            AIBoredEvent.Check(npc);
            var goal = npc.GetPart<BrainPart>().FindGoal<LayRuneGoal>();
            Assert.IsNotNull(goal);
            Assert.IsTrue(goal.RuneBlueprint == "TestRune" || goal.RuneBlueprint == "PlainRune",
                $"Picked blueprint must be one of two non-empty entries — got '{goal.RuneBlueprint}'.");
        }

        /// <summary>
        /// PRED: List with only delimiters and whitespace ("  ,  ,  ")
        /// trims to empty array → PickRuneBlueprint returns null →
        /// HandleBored returns true (no goal pushed). CONFIDENCE: high.
        /// </summary>
        [Test]
        public void AILayRune_BlueprintList_AllWhitespace_DoesNothing()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5, runes: "  ,  ,  ");

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Whitespace-only blueprint list → empty array → no goal.");
        }

        /// <summary>
        /// PRED: When CountRunesInZone returns exactly MaxRunesPerZone
        /// (boundary), the gate at line 106 (`>= cap`) trips — no
        /// goal pushed. Pinning the exact `>=` boundary, not `>`.
        /// CONFIDENCE: high.
        /// </summary>
        [Test]
        public void AILayRune_QuotaAtExactCap_BlocksGoalPush()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            var part = npc.GetPart<AILayRunePart>();
            part.MaxRunesPerZone = 3;

            // Place exactly 3 rune-tagged entities in zone.
            for (int i = 0; i < 3; i++)
            {
                var rune = new Entity { BlueprintName = "PreExisting", ID = "p-" + i };
                rune.SetTag("Rune", "");
                rune.AddPart(new RenderPart { DisplayName = "rune" });
                rune.AddPart(new PhysicsPart { Solid = false });
                zone.AddEntity(rune, 10 + i, 0);  // far from cultist's search radius
            }

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "CountRunesInZone == cap should trigger >= gate → no goal pushed.");
        }

        /// <summary>
        /// PRED: The static `_candidateScratch` list is shared across
        /// ALL AILayRunePart instances. If two NPCs run HandleBored
        /// concurrently in the same tick, the second call's Clear
        /// erases the first call's working set. AI turns are SERIAL
        /// (one HandleBored finishes before the next), so this is
        /// safe in production — but the test pins the contract:
        /// after each AIBoredEvent.Check, the scratch list is logically
        /// consumed and the next call starts fresh.
        /// CONFIDENCE: high.
        /// </summary>
        [Test]
        public void AILayRune_StaticScratch_ResetsBetweenNpcCalls()
        {
            var zone = new Zone("TestZone");
            var c1 = MakeCultist(zone, 3, 5, id: "c1", searchRadius: 2);
            var c2 = MakeCultist(zone, 15, 5, id: "c2", searchRadius: 2);

            AIBoredEvent.Check(c1);
            var g1 = c1.GetPart<BrainPart>().FindGoal<LayRuneGoal>();
            Assert.IsNotNull(g1);
            // Goal target must be within c1's search radius (Cheb dist <= 2 from (3,5)).
            int d1 = Math.Max(Math.Abs(g1.TargetX - 3), Math.Abs(g1.TargetY - 5));
            Assert.LessOrEqual(d1, 2, "c1's target must be in c1's radius.");

            AIBoredEvent.Check(c2);
            var g2 = c2.GetPart<BrainPart>().FindGoal<LayRuneGoal>();
            Assert.IsNotNull(g2);
            // c2's target must be in c2's radius (Cheb dist <= 2 from (15,5)),
            // NOT some leftover from c1's scan in the static scratch.
            int d2 = Math.Max(Math.Abs(g2.TargetX - 15), Math.Abs(g2.TargetY - 5));
            Assert.LessOrEqual(d2, 2,
                "c2's target must be in c2's radius — proves scratch was reset, not carried over.");
        }
    }
}
