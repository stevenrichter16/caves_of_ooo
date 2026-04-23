using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M5.2 — <see cref="DisposeOfCorpseGoal"/>. Two-phase carry-and-deposit
    /// state machine: fetch corpse → haul to container → deposit.
    ///
    /// Ports Qud's <c>XRL.World.AI.GoalHandlers.DisposeOfCorpse</c>. See
    /// <c>Docs/QUD-PARITY.md</c> §M5.2 for the design-decisions table.
    ///
    /// Uses the direct Zone + Entity construction pattern (same shape as
    /// <see cref="MoveToInteriorExteriorGoalTests"/>). No EntityFactory —
    /// corpses and containers are built by hand so tests isolate the goal's
    /// state transitions without dragging in blueprint loading.
    /// </summary>
    [TestFixture]
    public class DisposeOfCorpseGoalTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Build an undertaker NPC: Brain, Inventory, Body (so PickupCommand's
        /// auto-equip path doesn't NPE), and solid-movable stats. Strength 16
        /// so weight-10 corpses lift fine (required lift = ceil(10/2) = 5).
        /// </summary>
        private Entity CreateUndertaker(Zone zone, int x, int y)
        {
            var entity = new Entity { BlueprintName = "Undertaker", ID = "UT-1" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 14, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 14, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "undertaker" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(0) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        /// <summary>Build a bare SnapjawCorpse-equivalent (no Stacker so adding to
        /// a container doesn't merge / stack).</summary>
        private Entity CreateCorpse(Zone zone, int x, int y)
        {
            var corpse = new Entity { BlueprintName = "SnapjawCorpse", ID = "Corpse-1" };
            corpse.Tags["Corpse"] = "";
            corpse.AddPart(new RenderPart { DisplayName = "snapjaw corpse", RenderString = "%", ColorString = "&r" });
            corpse.AddPart(new PhysicsPart { Takeable = true, Weight = 10, Solid = false });
            zone.AddEntity(corpse, x, y);
            return corpse;
        }

        /// <summary>Graveyard-equivalent container. MaxItems lets us force the
        /// "container full" fallback.</summary>
        private Entity CreateGraveyard(Zone zone, int x, int y, int maxItems = -1)
        {
            var grave = new Entity { BlueprintName = "Graveyard", ID = "Grave-1" };
            grave.Tags["Graveyard"] = "";
            grave.AddPart(new RenderPart { DisplayName = "graveyard", RenderString = "t", ColorString = "&K" });
            grave.AddPart(new PhysicsPart { Solid = true });
            grave.AddPart(new ContainerPart { MaxItems = maxItems });
            zone.AddEntity(grave, x, y);
            return grave;
        }

        private static void RunGoalOnce(DisposeOfCorpseGoal goal, BrainPart brain)
        {
            // Mimics BrainPart.HandleTakeTurn's core: increment age, invoke
            // TakeAction on the top goal. We don't run the child-chain loop;
            // tests that need the child MoveToGoal to execute drive that path
            // explicitly by advancing position manually.
            goal.Age++;
            goal.TakeAction();
        }

        // ====================================================================
        // Phase 1: fetch
        // ====================================================================

        [Test]
        public void FetchPhase_NotAdjacent_PushesMoveToGoalTowardCorpse()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 15, 10);
            var grave = CreateGraveyard(zone, 20, 10);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "When NPC is NOT adjacent to the corpse, goal should push a MoveToGoal child.");
            Assert.AreEqual(1, goal.GoToCorpseTries,
                "GoToCorpseTries should increment by 1 per fetch-phase push.");
        }

        [Test]
        public void FetchPhase_Adjacent_PicksUpCorpseIntoInventory()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 11, 10);   // adjacent
            var grave = CreateGraveyard(zone, 20, 10);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);

            var inv = npc.GetPart<InventoryPart>();
            Assert.IsTrue(inv.Contains(corpse),
                "Adjacent to corpse → Pickup via InventorySystem.Pickup. Corpse should end up in NPC inventory.");
            Assert.IsNull(zone.GetEntityCell(corpse),
                "Picked-up corpse must be removed from the zone (PickupCommand side-effect).");
            Assert.IsFalse(goal.Finished(),
                "Fetch pickup should NOT mark the goal Done — haul phase runs on next tick.");
        }

        // ====================================================================
        // Phase 2: haul
        // ====================================================================

        [Test]
        public void HaulPhase_CarryingNotAdjacent_PushesMoveToGoalTowardContainer()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 10, 10);     // start corpse at NPC feet
            var grave = CreateGraveyard(zone, 20, 10);
            // Put corpse directly in inventory so we're already in haul phase.
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "Carrying + not-adjacent should push a MoveToGoal toward the container.");
            Assert.AreEqual(1, goal.GoToContainerTries,
                "GoToContainerTries should increment by 1 per haul-phase push.");
        }

        [Test]
        public void HaulPhase_TargetsPassableNeighbor_NotSolidContainerCell()
        {
            // Regression pin for the M5 PlayMode-sweep finding: HaulPhase used
            // to push `new MoveToGoal(containerCell.X, containerCell.Y, ...)`
            // targeting the container cell directly. Graveyard is Solid=true,
            // so MovementSystem.TryMove blocks the final step; MoveToGoal
            // FailsToParent, DisposeOfCorpseGoal.Failed cascades FailToParent,
            // corpse stuck in NPC inventory forever. Fix: target the passable
            // cell closest to the actor rather than the container cell.
            // Mirrors AIWellVisitorPart.GetPassableAdjacentCells pattern.
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 10, 10);
            var grave = CreateGraveyard(zone, 15, 10); // Solid=true in helper
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);

            var move = brain.FindGoal<MoveToGoal>();
            Assert.IsNotNull(move, "HaulPhase should push a MoveToGoal when not adjacent.");
            Assert.IsFalse(move.TargetX == 15 && move.TargetY == 10,
                "MoveToGoal must NOT target the Graveyard cell (15,10) directly — Solid tiles are unreachable.");
            var targetCell = zone.GetCell(move.TargetX, move.TargetY);
            Assert.IsNotNull(targetCell, "Target cell must be in bounds.");
            Assert.IsTrue(targetCell.IsPassable(),
                $"MoveToGoal target ({move.TargetX},{move.TargetY}) must be passable, else the move fails forever.");
            int dx = System.Math.Abs(move.TargetX - 15);
            int dy = System.Math.Abs(move.TargetY - 10);
            Assert.LessOrEqual(System.Math.Max(dx, dy), 1,
                "Target should be Chebyshev-adjacent to the Graveyard so the next HaulPhase tick sees adjacency and deposits.");
        }

        [Test]
        public void HaulPhase_ContainerWalledIn_DropsAtFeetRatherThanLoop()
        {
            // Counter-check for the above fix: when NO passable neighbor exists
            // (container fully walled in), HaulPhase must drop at feet instead
            // of looping forever.
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 10, 10);
            var grave = CreateGraveyard(zone, 15, 10);
            // Wall in every cell adjacent to the graveyard with solid objects.
            for (int wx = 14; wx <= 16; wx++)
            {
                for (int wy = 9; wy <= 11; wy++)
                {
                    if (wx == 15 && wy == 10) continue; // skip grave cell itself
                    var wall = new Entity { BlueprintName = "TestWall" };
                    // "Solid" TAG drives Cell.IsPassable (line 91). PhysicsPart.Solid
                    // drives MovementSystem.TryMove blocking. Both are used inconsistently
                    // across CoO — set both for a fully-solid test wall.
                    wall.Tags["Solid"] = "";
                    wall.AddPart(new PhysicsPart { Solid = true });
                    zone.AddEntity(wall, wx, wy);
                }
            }

            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);

            Assert.IsTrue(goal.Finished(),
                "Walled-in container must cause HaulPhase to drop at feet and finish.");
            Assert.IsFalse(npc.GetPart<InventoryPart>().Contains(corpse),
                "Corpse must be dropped from inventory when container is unreachable.");
            Assert.IsNotNull(zone.GetEntityCell(corpse),
                "Corpse must land back in the zone at NPC's cell (not vanish).");
        }

        [Test]
        public void HaulPhase_AdjacentToContainer_DepositsCorpseAndSetsDone()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 10, 10);
            var grave = CreateGraveyard(zone, 11, 10);    // adjacent
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);

            var containerPart = grave.GetPart<ContainerPart>();
            Assert.IsTrue(containerPart.Contents.Contains(corpse),
                "Corpse should have been transferred into the graveyard's container.");
            Assert.IsFalse(npc.GetPart<InventoryPart>().Contains(corpse),
                "Corpse must not remain in NPC inventory after deposit.");
            Assert.IsTrue(goal.Finished(),
                "Successful deposit should mark the goal Done.");
        }

        // ====================================================================
        // Fallbacks
        // ====================================================================

        [Test]
        public void HaulPhase_ContainerFull_DropsCorpseAtFeet()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 10, 10);
            // Capacity-1 graveyard, pre-filled.
            var grave = CreateGraveyard(zone, 11, 10, maxItems: 1);
            var filler = new Entity { BlueprintName = "FillerCorpse", ID = "Filler-1" };
            filler.AddPart(new PhysicsPart { Takeable = true, Weight = 10 });
            grave.GetPart<ContainerPart>().AddItem(filler);

            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);

            var npcCell = zone.GetEntityCell(corpse);
            Assert.IsNotNull(npcCell,
                "Full-container fallback must place the corpse back in the zone.");
            Assert.AreEqual(10, npcCell.X, "Corpse should drop at NPC's cell X.");
            Assert.AreEqual(10, npcCell.Y, "Corpse should drop at NPC's cell Y.");
            Assert.IsFalse(grave.GetPart<ContainerPart>().Contents.Contains(corpse),
                "Full container should reject the corpse, not silently accept it.");
            Assert.IsTrue(goal.Finished(),
                "Container-full fallback still marks the goal Done (no infinite retry).");
        }

        [Test]
        public void FetchPhase_ExhaustedTries_SetsDoneQuietly()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 40, 10);
            var grave = CreateGraveyard(zone, 50, 10);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            // Simulate MaxMoveTries failed attempts by running TakeAction
            // directly without advancing position. Each call pushes a MoveToGoal
            // which we immediately pop so the next TakeAction re-runs.
            for (int i = 0; i < DisposeOfCorpseGoal.MaxMoveTries; i++)
            {
                RunGoalOnce(goal, brain);
                var child = brain.FindGoal<MoveToGoal>();
                if (child != null) brain.RemoveGoal(child);
                Assert.IsFalse(goal.Finished(),
                    $"Goal should not be done after {i + 1} retries (cap is {DisposeOfCorpseGoal.MaxMoveTries}).");
            }
            // One more push takes us PAST the cap → Done=true.
            RunGoalOnce(goal, brain);
            Assert.IsTrue(goal.Finished(),
                "Fetch phase must set Done after exhausting MaxMoveTries retries (Qud DisposeOfCorpse.cs line 87).");
        }

        [Test]
        public void HaulPhase_ExhaustedTries_DropsAtFeetAndSetsDone()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 10, 10);
            var grave = CreateGraveyard(zone, 50, 10);
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            for (int i = 0; i < DisposeOfCorpseGoal.MaxMoveTries; i++)
            {
                RunGoalOnce(goal, brain);
                var child = brain.FindGoal<MoveToGoal>();
                if (child != null) brain.RemoveGoal(child);
                Assert.IsFalse(goal.Finished(),
                    $"Haul retry {i + 1} should not yet terminate the goal.");
            }
            RunGoalOnce(goal, brain);

            Assert.IsTrue(goal.Finished(),
                "Haul phase must set Done after exhausting MaxMoveTries retries.");
            Assert.IsFalse(npc.GetPart<InventoryPart>().Contains(corpse),
                "Exhausted-haul fallback must empty the corpse out of the NPC's inventory.");
            Assert.IsNotNull(zone.GetEntityCell(corpse),
                "Exhausted-haul fallback must drop the corpse back into the zone (at NPC's feet).");
        }

        [Test]
        public void FetchPhase_CorpseRemovedFromZone_FailsToParent()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 15, 10);
            var grave = CreateGraveyard(zone, 20, 10);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            // Simulate the corpse being destroyed / teleported / eaten before
            // the NPC arrives. GetEntityCell(corpse) now returns null.
            zone.RemoveEntity(corpse);

            RunGoalOnce(goal, brain);

            Assert.IsFalse(brain.HasGoal<DisposeOfCorpseGoal>(),
                "Missing corpse mid-fetch must pop the goal via FailToParent (mirrors Qud's Validate fail path).");
        }

        // ====================================================================
        // Lifecycle
        // ====================================================================

        [Test]
        public void OnPop_ClearsReservationOnCorpse()
        {
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 11, 10);
            var grave = CreateGraveyard(zone, 12, 10);

            // Simulate AIUndertakerPart having claimed the corpse (M5.3).
            corpse.SetIntProperty("DepositCorpsesReserve", 50);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            // Force goal completion (single deposit tick) then pop.
            RunGoalOnce(goal, brain); // pickup
            RunGoalOnce(goal, brain); // deposit → Done

            // Brain auto-pops finished goals in TakeTurn, but we're not running
            // full ticks — pop manually, which invokes OnPop.
            brain.RemoveGoal(goal);

            Assert.IsFalse(corpse.IntProperties.ContainsKey("DepositCorpsesReserve"),
                "OnPop must clear the DepositCorpsesReserve property (matches Qud's " +
                "ModIntProperty decrement cycle in Corpse.cs line 96-97, compressed " +
                "into a single pop-time clear in CoO).");
        }

        [Test]
        public void UndertakerKilledMidHaul_CorpseDropsAtDeathCell()
        {
            // Fix-pass M5 post-review finding #8: pin the contract between
            // DisposeOfCorpseGoal and CombatSystem.HandleDeath. When the
            // undertaker dies carrying a corpse, DropInventoryOnDeath should
            // drop the corpse at the undertaker's cell — the corpse must NOT
            // stay orphaned in the dead NPC's inventory.
            //
            // KNOWN LIMITATION (deferred to M5 follow-ups): HandleDeath does
            // not invoke brain.ClearGoals(), so the DisposeOfCorpseGoal stays
            // attached to the dead NPC with its OnPop never firing. The
            // corpse's DepositCorpsesReserve property remains set, preventing
            // other undertakers from claiming it. Fixing this cleanly is
            // cross-cutting (affects every AI goal's cleanup on death) and
            // out of M5 scope. Documented in QUD-PARITY.md §M5 follow-ups.
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            npc.Statistics["Hitpoints"].BaseValue = 0; // mark as dead for HandleDeath
            var corpse = CreateCorpse(zone, 10, 10);
            var grave = CreateGraveyard(zone, 12, 10);
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            brain.PushGoal(new DisposeOfCorpseGoal(corpse, grave));

            CombatSystem.HandleDeath(npc, killer: null, zone);

            Assert.IsNull(zone.GetEntityCell(npc),
                "Dead undertaker should be removed from the zone.");
            var corpseCell = zone.GetEntityCell(corpse);
            Assert.IsNotNull(corpseCell,
                "The snapjaw corpse must be dropped back into the zone — not orphaned in the dead undertaker's inventory.");
            Assert.AreEqual(10, corpseCell.X, "Corpse should drop at the undertaker's cell X.");
            Assert.AreEqual(10, corpseCell.Y, "Corpse should drop at the undertaker's cell Y.");
        }

        [Test]
        public void OnPop_ClearsLastThought_OnSuccess_SoActivePhaseThoughtDoesNotStick()
        {
            // Regression pin for a user-reported sticky-thought bug: the
            // initial M5.2 OnPop wrote "buried" on success (mirroring the
            // M4 MoveToInteriorGoal "sheltered" pattern), and that value
            // persisted in LastThought indefinitely because subsequent
            // goals (BoredGoal / WaitGoal / MoveToGoal for Staying) don't
            // Think(). The fix is to clear LastThought unconditionally on
            // pop — the disappearance of "hauling corpse" is the terminal
            // signal; no separate "buried" marker is needed.
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 10, 10);
            var grave = CreateGraveyard(zone, 11, 10);
            zone.RemoveEntity(corpse);
            npc.GetPart<InventoryPart>().AddObject(corpse);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            RunGoalOnce(goal, brain);    // deposit — sets Done=true
            brain.RemoveGoal(goal);      // invokes OnPop

            Assert.IsNull(brain.LastThought,
                "OnPop must clear LastThought — not write a terminal \"buried\" value. " +
                "Past-tense terminal thoughts stick in the inspector because no subsequent " +
                "goal overwrites them. Regression for the user-reported sticky-'buried' bug.");
        }

        [Test]
        public void OnPop_ClearsActivePhaseThought_EvenOnFailure()
        {
            // Complement to the above: failure paths (corpse missing,
            // container gone, exhausted tries) ALSO must clear the
            // LastThought. Here we force a failure by removing the corpse
            // mid-fetch so DisposeOfCorpseGoal.TakeAction calls FailToParent.
            var zone = new Zone("TestZone");
            var npc = CreateUndertaker(zone, 10, 10);
            var corpse = CreateCorpse(zone, 15, 10);    // not adjacent
            var grave = CreateGraveyard(zone, 20, 10);

            var brain = npc.GetPart<BrainPart>();
            var goal = new DisposeOfCorpseGoal(corpse, grave);
            brain.PushGoal(goal);

            // First TakeAction sets LastThought to "fetching corpse" and
            // pushes a MoveToGoal child.
            RunGoalOnce(goal, brain);
            Assert.AreEqual("fetching corpse", brain.LastThought,
                "Precondition: fetch phase should set the active thought.");

            // Now remove the corpse so the next tick's validation fails.
            zone.RemoveEntity(corpse);
            // Clean the stale child MoveToGoal off so RunGoalOnce calls
            // our goal's TakeAction directly.
            var child = brain.FindGoal<MoveToGoal>();
            if (child != null) brain.RemoveGoal(child);
            // The goal's TakeAction sees corpse.CurrentCell==null → FailToParent → Pop → OnPop.
            goal.TakeAction();

            Assert.IsFalse(brain.HasGoal<DisposeOfCorpseGoal>(),
                "Goal should have popped via FailToParent.");
            Assert.IsNull(brain.LastThought,
                "OnPop must clear LastThought on failure too — not leave \"fetching corpse\" stuck.");
        }
    }
}
