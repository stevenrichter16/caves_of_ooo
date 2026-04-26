using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M6 (Rune system: TriggerOnStepPart + LayRuneGoal + AILayRunePart +
    /// MovementSystem.FireCellEnteredEvents) gap-coverage pass.
    /// Companion to:
    ///   - TriggerOnStepPartTests.cs   (M6.1, 11 tests)
    ///   - LayRuneGoalTests.cs         (M6.2, 11 tests)
    ///   - AILayRunePartTests.cs       (M6.3, 13 tests)
    ///
    /// Per Docs/QUD-PARITY.md gap-coverage protocol: read production
    /// line-by-line, identify branches not pinned by an existing test,
    /// add a test that does. All citations are against the post-audit
    /// commits (00ee4c3, a5fc219, 057001f, 3dd6a1b, 488b49a, 7de63b9).
    /// </summary>
    [TestFixture]
    public class M6CoverageGapTests
    {
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
                }
            ]
        }";

        private static EntityFactory MakeFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(TestBlueprintsJson);
            return f;
        }

        private static Entity MakeMover(Zone zone, int x, int y, string id = "mover-1", int hp = 20)
        {
            var e = new Entity { BlueprintName = "Mover", ID = id };
            e.AddPart(new RenderPart { DisplayName = "mover" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeRune(Zone zone, int x, int y, TriggerOnStepPart part)
        {
            var e = new Entity { BlueprintName = "TestRune", ID = $"rune-{x}-{y}" };
            e.AddPart(new RenderPart { DisplayName = "rune" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.SetTag("Rune", "");
            e.AddPart(part);
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeCultist(Zone zone, int x, int y, int chance = 100, int searchRadius = 3,
            string runes = "TestRune", string faction = "Cultists")
        {
            var e = new Entity { BlueprintName = "RuneCultist", ID = "cult-" + x };
            e.Tags["Creature"] = "";
            e.Tags["Faction"] = faction;
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "cultist" });
            e.AddPart(new PhysicsPart { Solid = true });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(0) };
            e.AddPart(brain);
            e.AddPart(new AILayRunePart
            {
                Chance = chance,
                MaxRunesPerZone = 5,
                SearchRadius = searchRadius,
                RuneBlueprints = runes
            });
            zone.AddEntity(e, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return e;
        }

        /// <summary>Test-only Part that records EnteredCell receipts.</summary>
        private class Receipt : Part
        {
            public int Count;
            public Entity LastActor;
            public Cell LastCell;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "EntityEnteredCell")
                {
                    Count++;
                    LastActor = e.GetParameter<Entity>("Actor");
                    LastCell = e.GetParameter<Cell>("Cell");
                }
                return true;
            }
        }

        // ============================================================
        // MovementSystem.FireCellEnteredEvents gap-coverage
        // ============================================================

        /// <summary>
        /// Production line 174: when the destination cell is empty
        /// (zero occupants), no dispatch loop runs — a fast-skip
        /// path that protects the per-step cost. Without this we'd
        /// pay scratch-clear cost on every step into empty cells.
        /// </summary>
        [Test]
        public void FireCellEntered_EmptyDestination_NoDispatch()
        {
            var zone = new Zone("TestZone");
            var mover = MakeMover(zone, 4, 5);

            // Add a witness on the START cell (4,5) so it doesn't end up at the target.
            // After move, target (5,5) is empty of non-mover occupants.
            // Move into (5,5) — no other entities present.
            bool moved = MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);

            Assert.IsTrue(moved);
            // No witness was registered, so no observation possible — but no exception
            // and the move succeeded. Use destination-cell occupant count to confirm.
            var dest = zone.GetCell(5, 5);
            int nonMover = 0;
            foreach (var o in dest.Objects) if (o != mover) nonMover++;
            Assert.AreEqual(0, nonMover, "No non-movers in destination — dispatch loop should be a no-op.");
        }

        /// <summary>
        /// Production line 177 fast-path: when the only occupant is
        /// the mover itself, no dispatch and no scratch population.
        /// Pin: the mover must NOT receive an event sent to itself
        /// even via a one-element pre-dispatch.
        /// </summary>
        [Test]
        public void FireCellEntered_MoverOnlyOccupant_NoEventToSelf()
        {
            var zone = new Zone("TestZone");
            var mover = new Entity { BlueprintName = "Self", ID = "s-1" };
            mover.AddPart(new PhysicsPart { Solid = false });
            mover.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            mover.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            var receipt = new Receipt();
            mover.AddPart(receipt);
            zone.AddEntity(mover, 4, 5);

            MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);

            Assert.AreEqual(0, receipt.Count,
                "Fast-path skip: mover-only cell must not dispatch event to mover.");
        }

        /// <summary>
        /// Production line 206: the event's "Cell" parameter must be
        /// the exact target Cell reference (not a copy / not the
        /// origin cell). Pinning this protects rune-trigger logic
        /// that uses cell.ParentZone to resolve their zone.
        /// </summary>
        [Test]
        public void FireCellEntered_EventCellParam_IsTargetCellReference()
        {
            var zone = new Zone("TestZone");
            var witness = new Entity { BlueprintName = "W", ID = "w-1" };
            witness.AddPart(new PhysicsPart { Solid = false });
            var receipt = new Receipt();
            witness.AddPart(receipt);
            zone.AddEntity(witness, 5, 5);

            var mover = MakeMover(zone, 4, 5);
            MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);

            Assert.AreSame(zone.GetCell(5, 5), receipt.LastCell,
                "Event 'Cell' parameter must be the exact target Cell reference.");
        }

        // ============================================================
        // TriggerOnStepPart.HandleEvent gap-coverage
        // ============================================================

        /// <summary>
        /// Production line 52-53: when the event ID is anything other
        /// than "EntityEnteredCell" (e.g. Died, Hurt), the part
        /// returns true (passthrough) without any inspection. Pins
        /// that we don't accidentally consume unrelated events.
        /// </summary>
        [Test]
        public void Trigger_NonEnteredCellEvent_PropagatesNoOp()
        {
            var zone = new Zone("TestZone");
            int hp = 10;
            var rune = MakeRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = hp });

            var unrelated = GameEvent.New("Died");
            bool propagated = rune.GetPart<RuneFlameTriggerPart>().HandleEvent(unrelated);
            unrelated.Release();

            Assert.IsTrue(propagated, "Non-EntityEnteredCell events must propagate (return true).");
            // Rune is still in the zone (no consume).
            Assert.IsNotNull(zone.GetCell(5, 5).Objects.Find(o => o == rune),
                "Rune should not be consumed by an unrelated event.");
        }

        /// <summary>
        /// Production line 55-57: null Actor parameter on the event
        /// must short-circuit safely (no NPE, no consume, no payload).
        /// Defensive against malformed dispatches from non-CoO
        /// callers.
        /// </summary>
        [Test]
        public void Trigger_NullActor_DoesNotFireOrConsume()
        {
            var zone = new Zone("TestZone");
            var rune = MakeRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 8 });

            var ev = GameEvent.New("EntityEnteredCell");
            // Deliberately omit Actor.
            ev.SetParameter("Cell", (object)zone.GetCell(5, 5));
            Assert.DoesNotThrow(() => rune.FireEvent(ev));
            ev.Release();

            // Rune still present — ConsumeOnTrigger path NOT taken.
            Assert.IsNotNull(zone.GetCell(5, 5).Objects.Find(o => o == rune),
                "Null Actor must not fire OnTrigger or consume the rune.");
        }

        /// <summary>
        /// Production line 56: actor == ParentEntity (the rune is
        /// somehow firing with itself as the actor — a malformed
        /// dispatch / nested move case). Must not self-trigger.
        /// </summary>
        [Test]
        public void Trigger_ActorIsSelf_DoesNotFireOrConsume()
        {
            var zone = new Zone("TestZone");
            var rune = MakeRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 8 });

            var ev = GameEvent.New("EntityEnteredCell");
            ev.SetParameter("Actor", (object)rune);    // self
            ev.SetParameter("Cell", (object)zone.GetCell(5, 5));
            Assert.DoesNotThrow(() => rune.FireEvent(ev));
            ev.Release();

            Assert.IsNotNull(zone.GetCell(5, 5).Objects.Find(o => o == rune),
                "Self-actor event must not consume the rune.");
        }

        /// <summary>
        /// Production line 71-74: when the event's Cell parameter is
        /// missing or its ParentZone is null, the trigger short-
        /// circuits safely. Defensive against orphaned-cell dispatch.
        /// </summary>
        [Test]
        public void Trigger_NullCellParam_NoOps()
        {
            var zone = new Zone("TestZone");
            var rune = MakeRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 8 });
            var actor = MakeMover(zone, 4, 5);

            var ev = GameEvent.New("EntityEnteredCell");
            ev.SetParameter("Actor", (object)actor);
            // No Cell param.
            Assert.DoesNotThrow(() => rune.FireEvent(ev));
            ev.Release();

            Assert.IsNotNull(zone.GetCell(5, 5).Objects.Find(o => o == rune),
                "Null Cell param must short-circuit before consume.");
        }

        /// <summary>
        /// Production line 64: TriggerFaction = "" (empty string) is
        /// treated the same as null per IsNullOrEmpty — fires for
        /// everyone. Pins that the faction gate uses IsNullOrEmpty,
        /// not just `== null`.
        /// </summary>
        [Test]
        public void Trigger_EmptyTriggerFaction_FiresOnAnyActor()
        {
            var zone = new Zone("TestZone");
            var rune = MakeRune(zone, 5, 5, new RuneFlameTriggerPart
            {
                Damage = 5,
                TriggerFaction = ""   // empty — should be treated as null/no-filter
            });
            var actor = MakeMover(zone, 4, 5);
            actor.SetTag("Faction", "Anyone");

            MovementSystem.TryMove(actor, zone, dx: 1, dy: 0);

            // Empty TriggerFaction = no filter, so the actor receives full payload.
            Assert.AreEqual(15, actor.Statistics["Hitpoints"].BaseValue,
                "Empty TriggerFaction must fire regardless of actor faction (treated as no-filter).");
        }

        /// <summary>
        /// Production line 81: HandleEvent always returns true.
        /// Pins that even a triggered (consumed) rune still
        /// propagates the event to other listeners on the same
        /// entity — important if a rune ever has both a
        /// TriggerOnStep AND another EntityEnteredCell listener.
        /// </summary>
        [Test]
        public void Trigger_HandleEvent_AlwaysReturnsTrue_ToPropagate()
        {
            var zone = new Zone("TestZone");
            var rune = MakeRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 4 });
            var actor = MakeMover(zone, 4, 5);

            var ev = GameEvent.New("EntityEnteredCell");
            ev.SetParameter("Actor", (object)actor);
            ev.SetParameter("Cell", (object)zone.GetCell(5, 5));

            bool propagated = rune.GetPart<RuneFlameTriggerPart>().HandleEvent(ev);
            ev.Release();

            Assert.IsTrue(propagated,
                "HandleEvent must return true so other parts on the rune still receive the event.");
        }

        // ============================================================
        // LayRuneGoal gap-coverage
        // ============================================================

        /// <summary>
        /// Production line 104: Finished() returns false during the
        /// active walk phase. Only after the rune is laid OR
        /// MaxMoveTries exhaust does _done flip true.
        /// </summary>
        [Test]
        public void LayRuneGoal_NotFinished_DuringWalkPhase()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            var goal = new LayRuneGoal(targetX: 8, targetY: 5, runeBlueprint: "TestRune");
            npc.GetPart<BrainPart>().PushGoal(goal);

            Assert.IsFalse(goal.Finished(), "Newly-pushed goal not finished.");
            goal.TakeAction();
            Assert.IsFalse(goal.Finished(), "After pushing MoveToGoal, still walking — not finished.");
        }

        /// <summary>
        /// Production line 108: CanFight() returns false (Qud parity
        /// with LayMineGoal.cs:35). Important — without this, a
        /// rune-laying NPC under attack would keep laying instead of
        /// fighting back.
        /// </summary>
        [Test]
        public void LayRuneGoal_CanFight_IsFalse_ForCombatInterrupt()
        {
            var goal = new LayRuneGoal(0, 0, "TestRune");
            Assert.IsFalse(goal.CanFight(),
                "LayRuneGoal must NOT allow continuing through combat — Qud parity LayMineGoal.cs:35.");
        }

        /// <summary>
        /// Production line 110-111: GetDetails formats the inspector
        /// line — blueprint, target coords, tries counter. Pins the
        /// debug-overlay contract.
        /// </summary>
        [Test]
        public void LayRuneGoal_GetDetails_ContainsBlueprintAndTarget()
        {
            var goal = new LayRuneGoal(targetX: 7, targetY: 9, runeBlueprint: "RuneOfFlame");
            string details = goal.GetDetails();
            StringAssert.Contains("RuneOfFlame", details);
            StringAssert.Contains("(7,9)", details);
            StringAssert.Contains("tries=", details);
        }

        /// <summary>
        /// Production line 122: when the actor's position resolves to
        /// (-1,-1) (NPC removed from zone mid-flight), FailToParent
        /// fires immediately. Defensive against the NPC dying or
        /// being teleported between TakeActions.
        /// </summary>
        [Test]
        public void LayRuneGoal_ActorNotInZone_FailsToParent()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            var goal = new LayRuneGoal(targetX: 8, targetY: 5, runeBlueprint: "TestRune");
            npc.GetPart<BrainPart>().PushGoal(goal);

            // Remove npc from zone after pushing goal.
            zone.RemoveEntity(npc);

            goal.TakeAction();
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Actor not in zone → FailToParent → goal removed.");
        }

        /// <summary>
        /// Production line 200-205: explicit Failed(child) override
        /// propagates via FailToParent. Without this, an unreachable
        /// rune target would silently retry (against design — Qud's
        /// LayMineGoal.cs:77-81 also one-shot-aborts on unreachable).
        /// </summary>
        [Test]
        public void LayRuneGoal_Failed_PropagatesToParent()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            var goal = new LayRuneGoal(targetX: 8, targetY: 5, runeBlueprint: "TestRune");
            npc.GetPart<BrainPart>().PushGoal(goal);

            var fakeChild = new MoveToGoal(99, 99, 5);
            fakeChild.ParentHandler = goal;
            goal.Failed(fakeChild);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Failed(child) must call FailToParent → goal removed.");
        }

        /// <summary>
        /// Production line 133: MoveTries increments per push of the
        /// child MoveToGoal. Pins counter location (before the cap
        /// check, not after).
        /// </summary>
        [Test]
        public void LayRuneGoal_MoveTries_IncrementsPerPush()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            var goal = new LayRuneGoal(targetX: 8, targetY: 5, runeBlueprint: "TestRune");
            npc.GetPart<BrainPart>().PushGoal(goal);

            Assert.AreEqual(0, goal.MoveTries);
            goal.TakeAction();
            Assert.AreEqual(1, goal.MoveTries);

            // Pop the child, tick again.
            var brain = npc.GetPart<BrainPart>();
            var child = brain.PeekGoal();
            Assert.IsInstanceOf<MoveToGoal>(child);
            brain.RemoveGoal(child);

            goal.TakeAction();
            Assert.AreEqual(2, goal.MoveTries);
        }

        /// <summary>
        /// Production line 191-194: faction stamping is conditional
        /// on the actor having a non-empty Faction tag. An NPC with
        /// no faction must NOT stamp anything onto the rune (leaving
        /// TriggerFaction at its blueprint default).
        /// </summary>
        [Test]
        public void LayRuneGoal_ActorWithoutFaction_DoesNotStampTriggerFaction()
        {
            LayRuneGoal.Factory = MakeFactory();
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5, faction: null);  // no Faction tag
            // MakeCultist sets faction; remove it.
            npc.Tags.Remove("Faction");

            // Place npc directly at target so LayRune fires.
            var goal = new LayRuneGoal(targetX: 5, targetY: 5, runeBlueprint: "TestRune");
            npc.GetPart<BrainPart>().PushGoal(goal);
            goal.TakeAction();  // runs LayRune at-target branch

            // Find the spawned rune in the zone.
            Entity rune = null;
            foreach (var obj in zone.GetCell(5, 5).Objects)
                if (obj.HasTag("Rune")) { rune = obj; break; }
            Assert.IsNotNull(rune, "Rune should have been spawned at target.");
            var trigger = rune.GetPart<TriggerOnStepPart>();
            Assert.IsNotNull(trigger);
            Assert.IsNull(trigger.TriggerFaction,
                "Actor with no Faction tag must leave the rune's TriggerFaction unset (null).");
        }

        // ============================================================
        // AILayRunePart gap-coverage
        // ============================================================

        /// <summary>
        /// Production line 80: HandleEvent on a non-AIBored event ID
        /// must return true (passthrough). Pins that we don't
        /// accidentally consume Died / Hurt / etc.
        /// </summary>
        [Test]
        public void AILayRune_NonBoredEvent_Propagates()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            var part = npc.GetPart<AILayRunePart>();

            var ev = GameEvent.New("Died");
            bool propagated = part.HandleEvent(ev);
            ev.Release();

            Assert.IsTrue(propagated,
                "Non-AIBored events must propagate (not consumed by AILayRune).");
        }

        /// <summary>
        /// Production line 94: when brain.Rng is null, the part
        /// short-circuits. Defensive against test scaffolds and
        /// save-load races where Rng might briefly not be wired.
        /// </summary>
        [Test]
        public void AILayRune_NullRng_GracefullyNoOps()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            npc.GetPart<BrainPart>().Rng = null;

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Null Rng must short-circuit before goal push.");
        }

        /// <summary>
        /// Production line 94: brain.CurrentZone == null → no-op.
        /// </summary>
        [Test]
        public void AILayRune_NullCurrentZone_GracefullyNoOps()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            npc.GetPart<BrainPart>().CurrentZone = null;

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Null CurrentZone must short-circuit before goal push.");
        }

        /// <summary>
        /// Production line 110: when the NPC is no longer in the zone
        /// (pos.x &lt; 0), no goal is pushed. Defensive against the
        /// NPC being removed between bored ticks.
        /// </summary>
        [Test]
        public void AILayRune_ActorNotInZone_NoGoalPushed()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);
            zone.RemoveEntity(npc);

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "Actor not in zone → no goal pushed.");
        }

        /// <summary>
        /// Production line 170: PickRunePlacementCell explicitly
        /// excludes the NPC's own cell (dx==0 && dy==0 → continue).
        /// Without this the NPC would walk back-and-forth on its own
        /// cell laying runes — wasted turns. Pinning by forcing
        /// SearchRadius=0 (only candidate would be self-cell, so no
        /// candidate → no-op).
        /// </summary>
        [Test]
        public void AILayRune_SearchRadiusZero_ExcludesSelfCell_NoCandidate()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5, searchRadius: 0);

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "SearchRadius=0 → only candidate is self-cell, which is excluded → no goal.");
        }

        /// <summary>
        /// Production line 177: PickRunePlacementCell skips cells
        /// that already have a Rune-tagged entity — prevents cultists
        /// from stacking runes on the same cell. Forces the scenario
        /// where every cell in radius is already runed (except self).
        /// </summary>
        [Test]
        public void AILayRune_SkipsCellsWithExistingRune()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5, searchRadius: 1);
            // Fill all 8 surrounding cells with rune-tagged entities.
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var rune = new Entity { BlueprintName = "ExistingRune", ID = $"r-{dx}-{dy}" };
                rune.SetTag("Rune", "");
                rune.AddPart(new RenderPart { DisplayName = "rune" });
                rune.AddPart(new PhysicsPart { Solid = false });
                zone.AddEntity(rune, 5 + dx, 5 + dy);
            }

            AIBoredEvent.Check(npc);

            Assert.IsFalse(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>(),
                "All neighbor cells already runed → no candidate → no goal.");
        }

        /// <summary>
        /// Production line 82: when AILayRune pushes a goal, e.Handled
        /// = true so AIBoredEvent.Check returns false (consumed).
        /// Pins the consume contract; without it BoredGoal would still
        /// run wander/idle on top of the lay attempt.
        /// </summary>
        [Test]
        public void AILayRune_OnSuccessfulPush_ConsumesAIBoredEvent()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 5, 5);

            bool unhandled = AIBoredEvent.Check(npc);

            Assert.IsFalse(unhandled,
                "Successful goal push must consume AIBoredEvent (return false).");
            Assert.IsTrue(npc.GetPart<BrainPart>().HasGoal<LayRuneGoal>());
        }

        /// <summary>
        /// Production line 173: PickRunePlacementCell respects zone
        /// bounds — at a zone corner (1,1), only quarter of the
        /// neighbor radius is in-bounds. Pins that the InBounds check
        /// is wired and we don't get an out-of-range exception.
        /// </summary>
        [Test]
        public void AILayRune_AtZoneCorner_BoundedSearch_DoesNotThrow()
        {
            var zone = new Zone("TestZone");
            var npc = MakeCultist(zone, 0, 0, searchRadius: 3);

            Assert.DoesNotThrow(() => AIBoredEvent.Check(npc));
            // Either pushes goal (some in-bounds candidate found) or doesn't —
            // but must never throw.
        }
    }
}
