using System;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tests for Tier 3b (AIGuardPart) and Tier 3c (AIWellVisitorPart).
    /// Validates that concrete AIBehaviorPart subclasses respond to AIBoredEvent
    /// and push the correct goals onto the brain's stack.
    ///
    /// Ported to the Phase 3 scenario-test infrastructure:
    /// <see cref="ScenarioTestHarness"/> (fixture setup),
    /// <see cref="ScenarioContextExtensions.AdvanceTurns"/> (turn advancement),
    /// and <see cref="ScenarioVerifier"/> (fluent assertions).
    /// </summary>
    [TestFixture]
    public class AIBehaviorPartTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void Setup()
        {
            // Clear static MessageLog between tests so snapshots don't leak
            // across fixtures. FactionManager is already initialized by the
            // harness in OneTimeSetUp — no need to re-init here.
            MessageLog.Clear();
        }

        // ========================
        // Tier 3b: AIGuardPart
        // ========================

        [Test]
        public void AIGuard_PushesGuardGoalOnBored()
        {
            var ctx = _harness.CreateContext();
            var warden = ctx.Spawn("Warden").At(10, 10);

            ctx.AdvanceTurns(1);

            ctx.Verify().Entity(warden).HasGoalOnStack<GuardGoal>();
        }

        [Test]
        public void AIGuard_GuardGoalScansForHostiles()
        {
            var ctx = _harness.CreateContext();
            var warden = ctx.Spawn("Warden").At(10, 10);
            // Observer — not registered, won't tick on its own.
            ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(12, 10);

            ctx.AdvanceTurns(1);

            // No Verify method for brain state — inline check. The rest of the
            // test chain uses Verify.
            Assert.AreEqual(AIState.Chase, warden.GetPart<BrainPart>().CurrentState,
                "Warden should chase hostile Snapjaw.");
        }

        [Test]
        public void AIGuard_ReturnsToPostAfterCombat()
        {
            var ctx = _harness.CreateContext();
            var warden = ctx.Spawn("Warden").At(10, 10);
            ctx.AdvanceTurns(1); // pushes GuardGoal

            // Displace warden from post.
            ctx.Zone.MoveEntity(warden, 15, 10);
            ctx.AdvanceTurns(15);

            ctx.Verify().Entity(warden).IsAt(10, 10);
        }

        [Test]
        public void AIGuard_DoesNotConsumeBoredWithoutStartingCell()
        {
            // Special-case: ctx.Spawn auto-sets StartingCell. This test needs an
            // entity with no StartingCell, so we manually build one.
            var ctx = _harness.CreateContext();
            var entity = BuildMinimalCreatureWithAIGuard(ctx, 10, 10);
            // StartingCell NOT set (default -1, -1).

            bool consumed = !AIBoredEvent.Check(entity);

            Assert.IsFalse(consumed, "AIGuard should not consume AIBored without a StartingCell");
            ctx.Verify().Entity(entity).HasNoGoalOnStack<GuardGoal>();
        }

        [Test]
        public void AIGuard_AtPost_ReactsToHostileImmediately()
        {
            // Verifies the GuardGoal reactivity fix — at post, GuardGoal doesn't
            // push WaitGoal, so it re-scans for hostiles every tick.
            var ctx = _harness.CreateContext();
            var warden = ctx.Spawn("Warden").At(10, 10);

            ctx.AdvanceTurns(1); // Warden at post with GuardGoal idling
            ctx.Verify().Entity(warden).HasGoalOnStack<GuardGoal>();

            ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(12, 10);
            ctx.AdvanceTurns(1);

            ctx.Verify().Entity(warden).HasGoalOnStack<KillGoal>();
        }

        [Test]
        public void AIGuard_GoalStackStable_AfterMultipleTurns()
        {
            // GuardGoal should stay on the stack without duplicating.
            // After N turns with no hostiles, stack should be [BoredGoal, GuardGoal].
            var ctx = _harness.CreateContext();
            var warden = ctx.Spawn("Warden").At(10, 10);

            ctx.AdvanceTurns(20);

            var brain = warden.GetPart<BrainPart>();
            Assert.AreEqual(2, brain.GoalCount,
                "Stack should be [BoredGoal, GuardGoal] — no duplication from repeated ticks");
            ctx.Verify()
                .Entity(warden)
                    .HasGoalOnStack<BoredGoal>()
                    .HasGoalOnStack<GuardGoal>()
                    .IsAt(10, 10);
        }

        [Test]
        public void AIGuard_FullCombatCycle_ChaseAndReturnToPost()
        {
            // Full integration: hostile appears, warden chases, hostile dies,
            // warden walks back to post.
            var ctx = _harness.CreateContext();
            var warden = ctx.Spawn("Warden").At(10, 10);
            // Real Warden blueprint has no built-in MeleeWeaponPart (combat
            // goes through equipped weapons). Add one with overpowered damage
            // so the kill happens within the loop.
            warden.AddPart(new MeleeWeaponPart { BaseDamage = "10d10" });

            ctx.AdvanceTurns(1); // pushes GuardGoal
            ctx.Verify().Entity(warden).HasGoalOnStack<GuardGoal>();

            // Fragile Snapjaw — 1 HP.
            var snapjaw = ctx.Spawn("Snapjaw")
                .WithStatMax("Hitpoints", 1)
                .WithHpAbsolute(1)
                .At(12, 10);

            // Chase + kill within 5 ticks.
            for (int i = 0; i < 5; i++)
            {
                ctx.AdvanceTurns(1);
                if (ctx.Zone.GetEntityCell(snapjaw) == null) break;
            }
            Assert.IsNull(ctx.Zone.GetEntityCell(snapjaw), "Snapjaw should be dead after warden attack");

            // Return to post.
            ctx.AdvanceTurns(15);
            ctx.Verify().Entity(warden).IsAt(10, 10);
        }

        // ========================
        // Tier 3c: AIWellVisitorPart
        // ========================

        [Test]
        public void AIWellVisitor_WalksTowardWell()
        {
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Well").At(20, 12);
            var farmer = BuildFarmerWithWellVisitor(ctx, 5, 5, chance: 100);

            ctx.AdvanceTurns(1);

            ctx.Verify().Entity(farmer).HasGoalOnStack<MoveToGoal>();
        }

        [Test]
        public void AIWellVisitor_TargetsAdjacentToWell()
        {
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Well").At(20, 12);
            var farmer = BuildFarmerWithWellVisitor(ctx, 5, 5, chance: 100);

            // Fire AIBored directly so the MoveToGoal stays on top of the stack
            // (TakeTurn's child-chain would execute and start walking).
            AIBoredEvent.Check(farmer);

            var brain = farmer.GetPart<BrainPart>();
            ctx.Verify().Entity(farmer).HasGoalOnStack<MoveToGoal>();

            // MoveToGoal coordinates need direct inspection — no verifier for that.
            var top = brain.PeekGoal() as MoveToGoal;
            Assert.IsNotNull(top, "Top goal should be MoveToGoal");
            int dist = AIHelpers.ChebyshevDistance(top.TargetX, top.TargetY, 20, 12);
            Assert.AreEqual(1, dist,
                $"MoveToGoal target ({top.TargetX},{top.TargetY}) should be 1 cell from well (20,12)");
        }

        [Test]
        public void AIWellVisitor_NoWellInZone_DoesNothing()
        {
            var ctx = _harness.CreateContext();
            var farmer = BuildFarmerWithWellVisitor(ctx, 5, 5, chance: 100);

            ctx.AdvanceTurns(1);

            ctx.Verify().Entity(farmer).HasNoGoalOnStack<MoveToGoal>();
        }

        [Test]
        public void AIWellVisitor_ProbabilityGate_WithZeroChance()
        {
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Well").At(20, 12);
            var farmer = BuildFarmerWithWellVisitor(ctx, 5, 5, chance: 0);

            ctx.AdvanceTurns(20);

            ctx.Verify().Entity(farmer).HasNoGoalOnStack<MoveToGoal>();
        }

        // ========================
        // Blueprint integration — no zone/context needed
        // ========================

        [Test]
        public void Warden_Blueprint_HasAIGuard()
        {
            var warden = _harness.Factory.CreateEntity("Warden");
            Assert.IsNotNull(warden);
            Assert.IsNotNull(warden.GetPart<AIGuardPart>(),
                "Warden blueprint should have AIGuardPart attached");
        }

        [Test]
        public void Farmer_Blueprint_HasAIWellVisitor()
        {
            var farmer = _harness.Factory.CreateEntity("Farmer");
            Assert.IsNotNull(farmer);
            var wellVisitor = farmer.GetPart<AIWellVisitorPart>();
            Assert.IsNotNull(wellVisitor, "Farmer blueprint should have AIWellVisitorPart attached");
            Assert.AreEqual(5, wellVisitor.Chance,
                "Farmer's AIWellVisitor should have 5% chance per tick");
        }

        // ========================
        // Phase 6 M3.1: AIPetterPart
        // ========================

        [Test]
        public void AIPetter_PushesPetGoalOnBored_WhenChance100()
        {
            // Deterministic positive: chance=100 always pushes. An ally is
            // present so PetGoal's FindAlly phase has something to find,
            // but we fire AIBoredEvent directly (not TakeTurn) so the goal
            // stays on the stack without executing its TakeAction.
            var ctx = _harness.CreateContext();
            var child = BuildVillageChildWithPetter(ctx, 10, 10, chance: 100);
            ctx.Spawn("Villager").NotRegisteredForTurns().At(12, 10); // ally

            AIBoredEvent.Check(child);

            ctx.Verify().Entity(child).HasGoalOnStack<PetGoal>();
        }

        [Test]
        public void AIPetter_DoesNotDoublePush_WhenPetGoalAlreadyOnStack()
        {
            // Idempotency: HasGoal("PetGoal") gate in AIPetterPart should
            // prevent stacking even at chance=100. Pre-push a PetGoal,
            // fire AIBoredEvent, verify GoalCount is unchanged.
            var ctx = _harness.CreateContext();
            var child = BuildVillageChildWithPetter(ctx, 10, 10, chance: 100);
            ctx.Spawn("Villager").NotRegisteredForTurns().At(12, 10);

            var brain = child.GetPart<BrainPart>();
            brain.PushGoal(new PetGoal());
            int preCount = brain.GoalCount;

            AIBoredEvent.Check(child);

            Assert.AreEqual(preCount, brain.GoalCount,
                "AIPetter must not stack a second PetGoal when one is already on the brain.");
        }

        [Test]
        public void AIPetter_DoesNotPushAtChanceZero()
        {
            // Counter-check (Methodology Template §3.4): chance=0 must NEVER
            // push PetGoal, even across many bored ticks. Rules out "pushes
            // regardless of Chance" regression.
            var ctx = _harness.CreateContext();
            var child = BuildVillageChildWithPetter(ctx, 10, 10, chance: 0);
            ctx.Spawn("Villager").NotRegisteredForTurns().At(12, 10);

            ctx.AdvanceTurns(20);

            ctx.Verify().Entity(child).HasNoGoalOnStack<PetGoal>();
        }

        // ========================
        // Phase 6 M3.3: AIFleeToShrinePart
        // ========================

        [Test]
        public void AIFleeToShrine_PushesFleeLocationGoal_WhenHpLow_AndShrineExists()
        {
            // Positive path: HP below threshold + shrine in zone → push
            // FleeLocationGoal targeting the shrine's cell + consume event.
            var ctx = _harness.CreateContext();
            var shrine = ctx.World.PlaceObject("Shrine").At(20, 12);
            var scribe = BuildFleeToShrineCreature(ctx, 10, 10, fleeThreshold: 0.8f, hpFraction: 0.5f);

            bool consumed = !AIBoredEvent.Check(scribe);

            Assert.IsTrue(consumed,
                "AIFleeToShrine must consume the AIBoredEvent when it pushes a FleeLocationGoal.");
            ctx.Verify().Entity(scribe).HasGoalOnStack<FleeLocationGoal>();

            // Verify the goal targets the shrine's cell (not some other arbitrary point).
            var goal = scribe.GetPart<BrainPart>().FindGoal<FleeLocationGoal>();
            Assert.IsNotNull(goal);
            Assert.AreEqual(20, goal.SafeX, "FleeLocationGoal.SafeX should match shrine cell.");
            Assert.AreEqual(12, goal.SafeY, "FleeLocationGoal.SafeY should match shrine cell.");
        }

        [Test]
        public void AIFleeToShrine_DoesNotFire_WhenHpAboveThreshold()
        {
            // Counter-check #1 (HP gate): HP above threshold → event passes
            // through unconsumed, no FleeLocationGoal push.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Shrine").At(20, 12);
            var scribe = BuildFleeToShrineCreature(ctx, 10, 10, fleeThreshold: 0.5f, hpFraction: 0.9f);

            bool consumed = !AIBoredEvent.Check(scribe);

            Assert.IsFalse(consumed,
                "AIFleeToShrine must NOT consume the event when HP is above FleeThreshold.");
            ctx.Verify().Entity(scribe).HasNoGoalOnStack<FleeLocationGoal>();
        }

        [Test]
        public void AIFleeToShrine_DoesNotFire_WhenNoShrineInZone()
        {
            // Counter-check #2 (graceful fallback): HP below threshold BUT
            // no shrine in zone → event passes through unconsumed so
            // AISelfPreservation (if present) can fall back to home retreat.
            var ctx = _harness.CreateContext();
            var scribe = BuildFleeToShrineCreature(ctx, 10, 10, fleeThreshold: 0.8f, hpFraction: 0.5f);

            bool consumed = !AIBoredEvent.Check(scribe);

            Assert.IsFalse(consumed,
                "AIFleeToShrine must NOT consume the event when no shrine is available — " +
                "allows AISelfPreservation fallback to fire.");
            ctx.Verify().Entity(scribe).HasNoGoalOnStack<FleeLocationGoal>();
        }

        [Test]
        public void AIFleeToShrine_DoesNotDoublePush_WhenFleeLocationGoalAlreadyOnStack()
        {
            // Idempotency: pre-push a FleeLocationGoal, verify second
            // trigger is a no-op (doesn't stack a second goal).
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Shrine").At(20, 12);
            var scribe = BuildFleeToShrineCreature(ctx, 10, 10, fleeThreshold: 0.8f, hpFraction: 0.5f);

            var brain = scribe.GetPart<BrainPart>();
            brain.PushGoal(new FleeLocationGoal(5, 5, 10)); // pre-existing goal, different target
            int countBefore = brain.GoalCount;

            AIBoredEvent.Check(scribe);

            Assert.AreEqual(countBefore, brain.GoalCount,
                "AIFleeToShrine must not stack a second FleeLocationGoal when one exists.");
            var goal = brain.FindGoal<FleeLocationGoal>();
            Assert.AreEqual(5, goal.SafeX,
                "Existing FleeLocationGoal target must be preserved (not overwritten by shrine scan).");
        }

        [Test]
        public void AIFleeToShrine_FindsNearestShrine_WhenMultiplePresent()
        {
            // Counter-check: with two shrines at different distances, the
            // part should target the NEAREST one. Pins the "nearest"
            // selection logic in FindNearestSanctuary.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Shrine").At(30, 30); // distance 20 (Chebyshev)
            ctx.World.PlaceObject("Shrine").At(12, 11); // distance 2 (nearest)
            var scribe = BuildFleeToShrineCreature(ctx, 10, 10, fleeThreshold: 0.8f, hpFraction: 0.5f);

            AIBoredEvent.Check(scribe);

            var goal = scribe.GetPart<BrainPart>().FindGoal<FleeLocationGoal>();
            Assert.IsNotNull(goal);
            Assert.AreEqual(12, goal.SafeX, "Should target the nearer shrine.");
            Assert.AreEqual(11, goal.SafeY, "Should target the nearer shrine.");
        }

        [Test]
        public void AIFleeToShrine_PriorityOverAISelfPreservation_ViaEventConsumption()
        {
            // Integration: when both parts are attached AND AIFleeToShrine
            // fires, it must consume the event before AISelfPreservation
            // runs. Blueprint order decides (AIFleeToShrine added FIRST).
            // Entity.FireEvent stops propagation on Handled=true.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Shrine").At(20, 12);

            var entity = new Entity { BlueprintName = "TestDualPart" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 5, Max = 20 }; // 25%
            entity.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                StartingCellX = 10, StartingCellY = 10,
                Passive = true
            });
            // AIFleeToShrine FIRST (per M3.3 priority note).
            entity.AddPart(new AIFleeToShrinePart { FleeThreshold = 0.5f });
            // AISelfPreservation SECOND.
            entity.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            ctx.Zone.AddEntity(entity, 10, 10);
            ctx.Turns.AddEntity(entity);

            AIBoredEvent.Check(entity);

            ctx.Verify().Entity(entity).HasGoalOnStack<FleeLocationGoal>();
            ctx.Verify().Entity(entity).HasNoGoalOnStack<RetreatGoal>();
            // ^ AISelfPreservation would have pushed RetreatGoal if it ran;
            // its absence proves AIFleeToShrine consumed the event first.
        }

        [Test]
        public void Scribe_Blueprint_HasAIFleeToShrine_BeforeAISelfPreservation()
        {
            // Blueprint integration: real Scribe should load with both
            // parts attached in the right order. Part-insertion order is
            // preserved by BlueprintLoader.Bake (M1 ⚪ architectural note
            // #14 dependency), so "FIRST" in JSON → first in Parts list
            // → first to receive the bored event.
            var scribe = _harness.Factory.CreateEntity("Scribe");
            Assert.IsNotNull(scribe);
            Assert.IsNotNull(scribe.GetPart<AIFleeToShrinePart>(),
                "Scribe blueprint should have AIFleeToShrinePart attached.");
            Assert.IsNotNull(scribe.GetPart<AISelfPreservationPart>(),
                "Scribe blueprint should retain AISelfPreservationPart as fallback.");

            // Find indices in the Parts list to verify ordering.
            int fleeIdx = -1, selfPresIdx = -1;
            for (int i = 0; i < scribe.Parts.Count; i++)
            {
                if (scribe.Parts[i] is AIFleeToShrinePart) fleeIdx = i;
                else if (scribe.Parts[i] is AISelfPreservationPart) selfPresIdx = i;
            }
            Assert.Greater(fleeIdx, -1, "AIFleeToShrine should be in Parts list.");
            Assert.Greater(selfPresIdx, -1, "AISelfPreservation should be in Parts list.");
            Assert.Less(fleeIdx, selfPresIdx,
                "AIFleeToShrine MUST come BEFORE AISelfPreservation in the Parts " +
                "list so event dispatch order gives shrine-fleeing priority.");
        }

        [Test]
        public void Elder_Blueprint_HasAIFleeToShrine_BeforeAISelfPreservation()
        {
            var elder = _harness.Factory.CreateEntity("Elder");
            Assert.IsNotNull(elder);
            Assert.IsNotNull(elder.GetPart<AIFleeToShrinePart>(),
                "Elder blueprint should have AIFleeToShrinePart attached.");
            Assert.IsNotNull(elder.GetPart<AISelfPreservationPart>(),
                "Elder blueprint should retain AISelfPreservationPart as fallback.");

            int fleeIdx = -1, selfPresIdx = -1;
            for (int i = 0; i < elder.Parts.Count; i++)
            {
                if (elder.Parts[i] is AIFleeToShrinePart) fleeIdx = i;
                else if (elder.Parts[i] is AISelfPreservationPart) selfPresIdx = i;
            }
            Assert.Less(fleeIdx, selfPresIdx,
                "AIFleeToShrine must precede AISelfPreservation in Elder's Parts list.");
        }

        [Test]
        public void Shrine_Blueprint_Loads_WithSanctuaryPart()
        {
            // Blueprint round-trip: Shrine blueprint should produce an
            // entity with SanctuaryPart (the marker AIFleeToShrine scans
            // for) and Furniture tag (so NPC placement skips it).
            var shrine = _harness.Factory.CreateEntity("Shrine");
            Assert.IsNotNull(shrine, "Shrine blueprint should load.");
            Assert.IsNotNull(shrine.GetPart<SanctuaryPart>(),
                "Shrine should carry SanctuaryPart — AIFleeToShrine scans for this.");
            Assert.IsTrue(shrine.HasTag("Furniture"),
                "Shrine should have Furniture tag so GatherInteriorCells excludes it.");
        }

        // ========================
        // Phase 6 M3.2: AIHoarderPart
        // ========================

        [Test]
        public void AIHoarder_PushesGoFetchGoal_ForNearestTaggedItem()
        {
            // Positive: Chance=100, ally has a Shiny item on the floor,
            // magpie (hoarder) is bored → push GoFetchGoal targeting it.
            var ctx = _harness.CreateContext();
            var coin = ctx.World.PlaceObject("GoldCoin").At(15, 10);
            var magpie = BuildHoarderCreature(ctx, 10, 10, tag: "Shiny", chance: 100);

            AIBoredEvent.Check(magpie);

            ctx.Verify().Entity(magpie).HasGoalOnStack<GoFetchGoal>();
            var goal = magpie.GetPart<BrainPart>().FindGoal<GoFetchGoal>();
            Assert.AreSame(coin, goal.Item, "GoFetchGoal should target the nearest Shiny item.");
            Assert.IsTrue(goal.ReturnHome,
                "Hoarder mode — item goes HOME (magpie's starting cell).");
        }

        [Test]
        public void AIHoarder_FindsNearestAmongMultipleTaggedItems()
        {
            // Counter-check: two Shiny items, must pick the nearer one.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("GoldCoin").At(30, 30); // far — Chebyshev 20
            var nearCoin = ctx.World.PlaceObject("GoldCoin").At(12, 10); // near — Chebyshev 2
            var magpie = BuildHoarderCreature(ctx, 10, 10, tag: "Shiny", chance: 100);

            AIBoredEvent.Check(magpie);

            var goal = magpie.GetPart<BrainPart>().FindGoal<GoFetchGoal>();
            Assert.IsNotNull(goal);
            Assert.AreSame(nearCoin, goal.Item, "Should target the nearer Shiny coin.");
        }

        [Test]
        public void AIHoarder_DoesNotFire_WhenNoTaggedItemsPresent()
        {
            // Counter-check: no Shiny items in zone → event passes through.
            var ctx = _harness.CreateContext();
            var magpie = BuildHoarderCreature(ctx, 10, 10, tag: "Shiny", chance: 100);

            bool consumed = !AIBoredEvent.Check(magpie);

            Assert.IsFalse(consumed,
                "AIHoarder must not consume the event when no tagged items exist.");
            ctx.Verify().Entity(magpie).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void AIHoarder_IgnoresItemsWithoutTargetTag()
        {
            // Counter-check: an item exists but doesn't have the Shiny tag.
            // Magpie should NOT fetch an untagged item.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("Torch").At(12, 10); // not Shiny
            var magpie = BuildHoarderCreature(ctx, 10, 10, tag: "Shiny", chance: 100);

            AIBoredEvent.Check(magpie);

            ctx.Verify().Entity(magpie).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void AIHoarder_DoesNotFire_AtChanceZero()
        {
            // Counter-check (§3.4): chance=0 never fires, even with a Shiny
            // item right next to the magpie across many ticks.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("GoldCoin").At(11, 10);
            var magpie = BuildHoarderCreature(ctx, 10, 10, tag: "Shiny", chance: 0);

            ctx.AdvanceTurns(20);

            ctx.Verify().Entity(magpie).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void AIHoarder_DoesNotDoublePush_WhenGoFetchAlreadyOnStack()
        {
            var ctx = _harness.CreateContext();
            var coin = ctx.World.PlaceObject("GoldCoin").At(15, 10);
            var magpie = BuildHoarderCreature(ctx, 10, 10, tag: "Shiny", chance: 100);

            var brain = magpie.GetPart<BrainPart>();
            brain.PushGoal(new GoFetchGoal(coin, returnHome: true));
            int preCount = brain.GoalCount;

            AIBoredEvent.Check(magpie);

            Assert.AreEqual(preCount, brain.GoalCount,
                "AIHoarder must not stack a second GoFetchGoal.");
        }

        [Test]
        public void AIHoarder_DoesNotFire_WithoutStartingCell()
        {
            // Gate pin: HasStartingCell is required because GoFetchGoal
            // with returnHome=true walks back to StartingCell. Without one,
            // the return leg silently no-ops and "fetch" becomes "walk
            // one way." The part bails rather than push a half-baked goal.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("GoldCoin").At(12, 10);

            // Build a hoarder without StartingCell deliberately — ctx.Spawn
            // auto-sets it, so construct manually.
            var entity = new Entity { BlueprintName = "TestHoarderNoHome" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Tags["AllowIdleBehavior"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 6, Max = 6 };
            entity.AddPart(new BrainPart { CurrentZone = ctx.Zone, Rng = new Random(42) });
            // StartingCell deliberately NOT set.
            entity.AddPart(new InventoryPart { MaxWeight = 20 });
            entity.AddPart(new AIHoarderPart { TargetTag = "Shiny", Chance = 100 });
            ctx.Zone.AddEntity(entity, 10, 10);

            bool consumed = !AIBoredEvent.Check(entity);

            Assert.IsFalse(consumed,
                "AIHoarder must not consume the event when StartingCell is missing.");
            ctx.Verify().Entity(entity).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void AIHoarder_DoesNotFire_WithoutInventory()
        {
            // Gate pin: no InventoryPart → GoFetchGoal's Pickup phase
            // would fail with no stow destination. AIHoarder skips the
            // scan entirely rather than walk to an item it can't carry.
            var ctx = _harness.CreateContext();
            ctx.World.PlaceObject("GoldCoin").At(12, 10);

            var entity = new Entity { BlueprintName = "TestHoarderNoInv" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Tags["AllowIdleBehavior"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 6, Max = 6 };
            entity.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                StartingCellX = 10, StartingCellY = 10
            });
            // InventoryPart deliberately NOT added.
            entity.AddPart(new AIHoarderPart { TargetTag = "Shiny", Chance = 100 });
            ctx.Zone.AddEntity(entity, 10, 10);

            bool consumed = !AIBoredEvent.Check(entity);

            Assert.IsFalse(consumed,
                "AIHoarder must not consume the event when InventoryPart is missing.");
            ctx.Verify().Entity(entity).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void Magpie_Blueprint_HasAIHoarder_AndInventory()
        {
            var magpie = _harness.Factory.CreateEntity("Magpie");
            Assert.IsNotNull(magpie, "Magpie blueprint should load.");
            var hoarder = magpie.GetPart<AIHoarderPart>();
            Assert.IsNotNull(hoarder, "Magpie blueprint should have AIHoarderPart.");
            Assert.AreEqual("Shiny", hoarder.TargetTag);
            Assert.AreEqual(15, hoarder.Chance);
            Assert.IsNotNull(magpie.GetPart<InventoryPart>(),
                "Magpie needs InventoryPart to stow fetched items.");
        }

        [Test]
        public void GoldCoin_Blueprint_HasShinyTag_AndIsTakeable()
        {
            var coin = _harness.Factory.CreateEntity("GoldCoin");
            Assert.IsNotNull(coin, "GoldCoin blueprint should load.");
            Assert.IsTrue(coin.HasTag("Shiny"),
                "GoldCoin should carry the Shiny tag so AIHoarder scans find it.");
            var physics = coin.GetPart<PhysicsPart>();
            Assert.IsNotNull(physics);
            Assert.IsTrue(physics.Takeable,
                "GoldCoin should be Takeable so GoFetchGoal's pickup phase succeeds.");
        }

        // ========================
        // Phase 6 M3.2: AIRetrieverPart + ItemLanded broadcast
        // ========================

        [Test]
        public void AIRetriever_PushesGoFetchGoal_OnItemLandedFromAlly()
        {
            var ctx = _harness.CreateContext();
            var dog = BuildRetrieverCreature(ctx, 10, 10, alliesOnly: true, noticeRadius: 10);
            var ally = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 9);
            var item = BuildFloorItem(ctx, "Bone");
            ctx.Zone.AddEntity(item, 12, 10);
            var landingCell = ctx.Zone.GetCell(12, 10);

            // Simulate ItemLanded event from the ally.
            var e = GameEvent.New(ItemLandedEvent.ID);
            e.SetParameter("Item", (object)item);
            e.SetParameter("Thrower", (object)ally);
            e.SetParameter("LandingCell", (object)landingCell);
            dog.FireEvent(e);
            e.Release();

            ctx.Verify().Entity(dog).HasGoalOnStack<GoFetchGoal>();
            var goal = dog.GetPart<BrainPart>().FindGoal<GoFetchGoal>();
            Assert.AreSame(item, goal.Item);
            Assert.IsFalse(goal.ReturnHome,
                "Retriever mode — item stays where pet is (no return home).");
        }

        [Test]
        public void AIRetriever_IgnoresEnemyThrow_WhenAlliesOnlyTrue()
        {
            // Counter-check: a thrower from a hostile faction must NOT
            // trigger the fetch. Pins the AlliesOnly filter branch.
            var ctx = _harness.CreateContext();
            var dog = BuildRetrieverCreature(ctx, 10, 10, alliesOnly: true, noticeRadius: 10);
            var enemy = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(10, 9);
            var item = BuildFloorItem(ctx, "Bone");
            ctx.Zone.AddEntity(item, 12, 10);
            var landingCell = ctx.Zone.GetCell(12, 10);

            var e = GameEvent.New(ItemLandedEvent.ID);
            e.SetParameter("Item", (object)item);
            e.SetParameter("Thrower", (object)enemy);
            e.SetParameter("LandingCell", (object)landingCell);
            dog.FireEvent(e);
            e.Release();

            ctx.Verify().Entity(dog).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void AIRetriever_IgnoresThrowBeyondNoticeRadius()
        {
            // Counter-check (radius gate): item lands 20 cells away, radius=8.
            // Even though thrower is an ally, distance kills the fetch.
            var ctx = _harness.CreateContext();
            var dog = BuildRetrieverCreature(ctx, 10, 10, alliesOnly: true, noticeRadius: 8);
            var ally = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 9);
            var item = BuildFloorItem(ctx, "Bone");
            ctx.Zone.AddEntity(item, 30, 10); // dist 20
            var landingCell = ctx.Zone.GetCell(30, 10);

            var e = GameEvent.New(ItemLandedEvent.ID);
            e.SetParameter("Item", (object)item);
            e.SetParameter("Thrower", (object)ally);
            e.SetParameter("LandingCell", (object)landingCell);
            dog.FireEvent(e);
            e.Release();

            ctx.Verify().Entity(dog).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void AIRetriever_DoesNotDoublePush_WhenGoFetchAlreadyOnStack()
        {
            var ctx = _harness.CreateContext();
            var dog = BuildRetrieverCreature(ctx, 10, 10, alliesOnly: false, noticeRadius: 10);
            var item = BuildFloorItem(ctx, "Bone");
            ctx.Zone.AddEntity(item, 12, 10);
            var landingCell = ctx.Zone.GetCell(12, 10);

            var brain = dog.GetPart<BrainPart>();
            brain.PushGoal(new GoFetchGoal(item, returnHome: false));
            int preCount = brain.GoalCount;

            var e = GameEvent.New(ItemLandedEvent.ID);
            e.SetParameter("Item", (object)item);
            e.SetParameter("Thrower", (object)dog); // self-throw edge — still blocked by idempotency
            e.SetParameter("LandingCell", (object)landingCell);
            dog.FireEvent(e);
            e.Release();

            Assert.AreEqual(preCount, brain.GoalCount,
                "AIRetriever must not stack a second GoFetchGoal.");
        }

        [Test]
        public void ItemLandedEvent_Broadcast_SkipsThrowerAndItem()
        {
            // Pin Broadcast's self-exclusion: thrower doesn't get an event
            // about their own throw; item doesn't get an event about its
            // own landing. To make this observable we attach a spy part
            // to the thrower AND a spy to a bystander. Only the bystander
            // should flip Received=true.
            var ctx = _harness.CreateContext();

            var thrower = new Entity { BlueprintName = "TestThrower" };
            thrower.Tags["Creature"] = "";
            thrower.AddPart(new ItemLandedSpyPart()); // Thrower has a spy
            ctx.Zone.AddEntity(thrower, 10, 10);

            var bystander = BuildEventProbeEntity(ctx, 11, 10);

            var item = BuildFloorItem(ctx, "Bone");
            ctx.Zone.AddEntity(item, 12, 10);
            var landingCell = ctx.Zone.GetCell(12, 10);

            ItemLandedEvent.Broadcast(ctx.Zone, thrower, item, landingCell);

            var throwerSpy = thrower.GetPart<ItemLandedSpyPart>();
            var bystanderSpy = bystander.GetPart<ItemLandedSpyPart>();

            Assert.IsFalse(throwerSpy.Received,
                "Thrower MUST be skipped by Broadcast — no self-notification.");
            Assert.IsTrue(bystanderSpy.Received,
                "Bystander should have received the broadcast.");
        }

        [Test]
        public void ThrowItemCommand_FiresItemLanded_OnZoneCreatures()
        {
            // Integration: wire the throw pipeline end-to-end and confirm
            // the ItemLanded event actually broadcasts from
            // ThrowItemCommand.Execute. Uses an ItemLandedSpyPart on a
            // nearby creature that flips a flag on receipt.
            //
            // Throw distance is 1 cell so the range check trivially passes
            // regardless of the thrower's Strength or the item's weight —
            // the test is about the BROADCAST, not the range algorithm.
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var inv = thrower.GetPart<InventoryPart>();
            if (inv == null)
            {
                inv = new InventoryPart { MaxWeight = 50 };
                thrower.AddPart(inv); // AddPart returns void; attach then keep the ref
            }
            var bone = BuildFloorItem(ctx, "Bone");
            // HandlingService.CanThrow requires a HandlingPart with Throwable=true.
            bone.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            inv.AddObject(bone);

            // Probe east of the throw line so it sees the broadcast.
            var probe = BuildEventProbeEntity(ctx, 13, 10);

            var throwCmd = new CavesOfOoo.Core.Inventory.Commands.ThrowItemCommand(bone, 11, 10);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);

            Assert.IsTrue(result.Success, "Throw should succeed. Error: " + result.ErrorMessage);
            var spy = probe.GetPart<ItemLandedSpyPart>();
            Assert.IsTrue(spy.Received,
                "Probe should receive ItemLanded broadcast from ThrowItemCommand.");
            Assert.AreSame(bone, spy.LastItem,
                "Broadcast's Item parameter should point at the thrown bone.");
            Assert.AreSame(thrower, spy.LastThrower,
                "Broadcast's Thrower parameter should point at the throw's actor.");
        }

        [Test]
        public void PetDog_Blueprint_HasRetrieverAndPetter()
        {
            var dog = _harness.Factory.CreateEntity("PetDog");
            Assert.IsNotNull(dog, "PetDog blueprint should load.");
            Assert.IsNotNull(dog.GetPart<AIRetrieverPart>(),
                "PetDog blueprint should have AIRetrieverPart.");
            Assert.IsNotNull(dog.GetPart<AIPetterPart>(),
                "PetDog blueprint should also have AIPetterPart (pets get petted too).");
            var brain = dog.GetPart<BrainPart>();
            Assert.IsTrue(brain.Passive, "PetDog should be Passive (won't initiate combat).");
        }

        [Test]
        public void VillageChild_Blueprint_HasAIPetter_AndWanderingPassiveBrain()
        {
            // Blueprint integration: the real VillageChild should load with
            // AIPetter attached at chance=5, a Passive wandering Brain, and
            // the Villagers faction tag.
            var child = _harness.Factory.CreateEntity("VillageChild");
            Assert.IsNotNull(child, "VillageChild blueprint missing from Objects.json.");

            var petter = child.GetPart<AIPetterPart>();
            Assert.IsNotNull(petter, "VillageChild should have AIPetterPart attached.");
            Assert.AreEqual(5, petter.Chance,
                "VillageChild's AIPetter should have 5% chance per tick.");

            var brain = child.GetPart<BrainPart>();
            Assert.IsNotNull(brain, "VillageChild should have a BrainPart.");
            Assert.IsTrue(brain.Passive, "VillageChild should be Passive (don't initiate combat).");
            Assert.IsTrue(brain.Wanders, "VillageChild should Wander.");
            Assert.IsTrue(brain.WandersRandomly, "VillageChild should WanderRandomly.");

            Assert.AreEqual("Villagers", child.GetTag("Faction"),
                "VillageChild should be on the Villagers faction.");
        }

        // ========================
        // Phase 6 M3.2: additional gate + consumed-on-impact pins
        // ========================

        [Test]
        public void AIRetriever_DoesNotFire_WithoutInventory()
        {
            // Gate pin: no InventoryPart → GoFetchGoal's Pickup phase can't
            // stow the fetched item. AIRetriever bails rather than push a
            // doomed goal. Mirrors the Hoarder-no-inventory case but on
            // the reactive event-driven branch.
            var ctx = _harness.CreateContext();

            var dog = new Entity { BlueprintName = "TestDogNoInv" };
            dog.Tags["Creature"] = "";
            dog.Tags["Faction"] = "Villagers";
            dog.Tags["AllowIdleBehavior"] = "";
            dog.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 15, Max = 15 };
            dog.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                StartingCellX = 10, StartingCellY = 10
            });
            // InventoryPart deliberately NOT added.
            dog.AddPart(new AIRetrieverPart { AlliesOnly = false, NoticeRadius = 20 });
            ctx.Zone.AddEntity(dog, 10, 10);

            var item = BuildFloorItem(ctx, "Bone");
            ctx.Zone.AddEntity(item, 12, 10);
            var landingCell = ctx.Zone.GetCell(12, 10);

            var e = GameEvent.New(ItemLandedEvent.ID);
            e.SetParameter("Item", (object)item);
            e.SetParameter("Thrower", (object)null);
            e.SetParameter("LandingCell", (object)landingCell);
            dog.FireEvent(e);
            e.Release();

            ctx.Verify().Entity(dog).HasNoGoalOnStack<GoFetchGoal>();
        }

        [Test]
        public void ThrowItemCommand_DoesNotBroadcast_WhenConsumedOnImpact()
        {
            // Counter-check for the !consumedOnImpact guard around
            // ItemLandedEvent.Broadcast: a tonic thrown at a hit target
            // gets TryApplyThrownTonic → consumedOnImpact=true → the item
            // is NOT added to the zone and the event is NOT fired.
            // Regression guard: a future refactor that moves the Broadcast
            // call outside the !consumedOnImpact branch would break here.
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var inv = thrower.GetPart<InventoryPart>();
            if (inv == null)
            {
                inv = new InventoryPart { MaxWeight = 50 };
                thrower.AddPart(inv);
            }

            // Hit target directly in the throw trajectory.
            var victim = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(11, 10);

            // Tonic with a throwable payload (Healing set → HasThrowablePayload true).
            var tonic = new Entity { BlueprintName = "TestTonic" };
            tonic.AddPart(new RenderPart { DisplayName = "test tonic" });
            tonic.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            tonic.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            tonic.AddPart(new TonicPart { Healing = "1d1" });
            inv.AddObject(tonic);

            // Probe on the far side of the victim. If the broadcast leaked,
            // the probe would flip Received=true.
            var probe = BuildEventProbeEntity(ctx, 13, 10);

            var throwCmd = new CavesOfOoo.Core.Inventory.Commands.ThrowItemCommand(tonic, 11, 10);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);

            Assert.IsTrue(result.Success, "Throw should succeed. Error: " + result.ErrorMessage);
            var spy = probe.GetPart<ItemLandedSpyPart>();
            Assert.IsFalse(spy.Received,
                "Tonic consumed on impact must NOT broadcast ItemLanded — " +
                "there's no landed item to fetch.");
        }

        // ========================
        // Local helpers (test-specific setups that can't go through ctx.Spawn)
        // ========================

        /// <summary>
        /// Builds a minimal creature with AIGuard attached but NO StartingCell
        /// set. Needed for the "AIGuard doesn't fire without StartingCell" test —
        /// ctx.Spawn auto-sets StartingCell so we can't use it here.
        /// </summary>
        private static Entity BuildMinimalCreatureWithAIGuard(ScenarioContext ctx, int x, int y)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 15, Max = 15 };
            entity.AddPart(new BrainPart { CurrentZone = ctx.Zone, Rng = new Random(42) });
            entity.AddPart(new AIGuardPart());
            ctx.Zone.AddEntity(entity, x, y);
            // StartingCell deliberately NOT set.
            return entity;
        }

        /// <summary>
        /// Builds a minimal farmer-shaped creature with AIWellVisitor. Can't use
        /// ctx.Spawn("Farmer") because the real Farmer has Chance=5 baked in —
        /// we need to test other Chance values (0, 100).
        /// </summary>
        private static Entity BuildFarmerWithWellVisitor(ScenarioContext ctx, int x, int y, int chance)
        {
            var entity = new Entity { BlueprintName = "TestFarmer" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Tags["AllowIdleBehavior"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 15, Max = 15 };
            entity.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                Wanders = false,
                WandersRandomly = false,
                Staying = true,
                StartingCellX = x,
                StartingCellY = y
            });
            entity.AddPart(new AIWellVisitorPart { Chance = chance });
            ctx.Zone.AddEntity(entity, x, y);
            ctx.Turns.AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Builds a minimal VillageChild-shaped creature with AIPetter. Can't
        /// use ctx.Spawn("VillageChild") because the real blueprint bakes
        /// Chance=5 — we need Chance=0 and Chance=100 to exercise the gate
        /// deterministically.
        /// </summary>
        private static Entity BuildVillageChildWithPetter(ScenarioContext ctx, int x, int y, int chance)
        {
            var entity = new Entity { BlueprintName = "TestChild" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Tags["AllowIdleBehavior"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Max = 10 };
            entity.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                Wanders = true,
                WandersRandomly = true,
                Staying = false,
                Passive = true,
                StartingCellX = x,
                StartingCellY = y
            });
            entity.AddPart(new AIPetterPart { Chance = chance });
            ctx.Zone.AddEntity(entity, x, y);
            ctx.Turns.AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Builds a minimal Scribe-shaped creature with AIFleeToShrine. Can't
        /// use ctx.Spawn("Scribe") — the real blueprint bakes FleeThreshold
        /// (0.8) AND pairs with AISelfPreservation, both of which this test
        /// fixture needs to vary independently (chance=0.5/0.9, HP-ratio
        /// 0.5/0.9, with/without companion AISelfPreservation).
        ///
        /// HP setup: Max stays at 20 (Creature default) so hpFraction of
        /// Max produces an integer >= 1. BaseValue gets set from fraction
        /// so ShouldFlee and the part's own HP gate see consistent
        /// fractional values.
        /// </summary>
        private static Entity BuildFleeToShrineCreature(ScenarioContext ctx, int x, int y,
            float fleeThreshold, float hpFraction)
        {
            var entity = new Entity { BlueprintName = "TestScribe" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Tags["AllowIdleBehavior"] = "";
            entity.Statistics["Hitpoints"] = new Stat
            {
                Name = "Hitpoints",
                Max = 20,
                BaseValue = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(20 * hpFraction))
            };
            entity.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                Wanders = false,
                WandersRandomly = false,
                Staying = true,
                Passive = true,
                StartingCellX = x,
                StartingCellY = y
            });
            entity.AddPart(new AIFleeToShrinePart { FleeThreshold = fleeThreshold });
            ctx.Zone.AddEntity(entity, x, y);
            ctx.Turns.AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Builds a minimal creature with AIHoarderPart + InventoryPart +
        /// StartingCell (required by the hoarder's HasStartingCell gate).
        /// Tests need tag + chance variance (0, 100) that the real Magpie
        /// blueprint bakes (Shiny, 15), so build manually.
        /// </summary>
        private static Entity BuildHoarderCreature(ScenarioContext ctx, int x, int y, string tag, int chance)
        {
            var entity = new Entity { BlueprintName = "TestMagpie" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Tags["AllowIdleBehavior"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 6, Max = 6 };
            entity.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                Wanders = true,
                WandersRandomly = true,
                Staying = false,
                Passive = true,
                StartingCellX = x,
                StartingCellY = y
            });
            entity.AddPart(new InventoryPart { MaxWeight = 20 });
            entity.AddPart(new AIHoarderPart { TargetTag = tag, Chance = chance });
            ctx.Zone.AddEntity(entity, x, y);
            ctx.Turns.AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Builds a minimal creature with AIRetrieverPart + InventoryPart.
        /// Tests need to vary AlliesOnly + NoticeRadius independently of
        /// the PetDog blueprint's baked defaults.
        /// </summary>
        private static Entity BuildRetrieverCreature(ScenarioContext ctx, int x, int y,
            bool alliesOnly, int noticeRadius)
        {
            var entity = new Entity { BlueprintName = "TestDog" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Tags["AllowIdleBehavior"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 15, Max = 15 };
            entity.AddPart(new BrainPart
            {
                CurrentZone = ctx.Zone,
                Rng = new Random(42),
                Wanders = true,
                WandersRandomly = true,
                Staying = false,
                Passive = true,
                StartingCellX = x,
                StartingCellY = y
            });
            entity.AddPart(new InventoryPart { MaxWeight = 10 });
            entity.AddPart(new AIRetrieverPart { AlliesOnly = alliesOnly, NoticeRadius = noticeRadius });
            ctx.Zone.AddEntity(entity, x, y);
            ctx.Turns.AddEntity(entity);
            return entity;
        }

        /// <summary>
        /// Minimal floor-item entity for use as a fetch target in retriever
        /// tests. <paramref name="displayName"/> is used for tracing; the
        /// tests that care about tags set them on the returned entity
        /// directly.
        /// </summary>
        private static Entity BuildFloorItem(ScenarioContext ctx, string displayName)
        {
            var item = new Entity { BlueprintName = displayName };
            item.AddPart(new RenderPart { DisplayName = displayName });
            item.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            return item;
        }

        /// <summary>
        /// Spawns a minimal entity with an ItemLandedSpyPart that records
        /// whether it received the ItemLanded event. Used for asserting
        /// the broadcast actually reaches nearby entities.
        /// </summary>
        private static Entity BuildEventProbeEntity(ScenarioContext ctx, int x, int y)
        {
            var entity = new Entity { BlueprintName = "TestProbe" };
            entity.Tags["Creature"] = ""; // so Broadcast doesn't skip as non-creature
            entity.AddPart(new ItemLandedSpyPart());
            ctx.Zone.AddEntity(entity, x, y);
            return entity;
        }

        /// <summary>
        /// Test-only probe part: listens for ItemLanded events and flips
        /// Received=true. Lets tests assert broadcast reachability without
        /// inferring from goal-stack side effects.
        /// </summary>
        private class ItemLandedSpyPart : Part
        {
            public bool Received;
            public Entity LastItem;
            public Entity LastThrower;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == ItemLandedEvent.ID)
                {
                    Received = true;
                    LastItem = e.GetParameter<Entity>("Item");
                    LastThrower = e.GetParameter<Entity>("Thrower");
                }
                return true;
            }
        }
    }
}
