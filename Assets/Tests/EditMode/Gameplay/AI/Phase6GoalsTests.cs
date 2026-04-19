using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 6 tests: new goal handlers that fill gaps against Qud's goal catalog.
    ///
    /// Covers:
    /// - Green goals (ready, no new infrastructure): FleeLocationGoal, WanderDurationGoal,
    ///   GoFetchGoal, PetGoal
    /// - Yellow goals (small infrastructure additions): NoFightGoal + BrainPart.Passive,
    ///   RetreatGoal + AISelfPreservationPart, DormantGoal (damage + hostile wake triggers)
    /// </summary>
    [TestFixture]
    public class Phase6GoalsTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateCreature(Zone zone, int x, int y, string faction = "Villagers", int hp = 20)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = faction ?? "creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            entity.AddPart(new ArmorPart());
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        /// <summary>
        /// Create a pickup-ready item. Weight drives the Strength requirement via
        /// HandlingService.GetLiftStrengthRequirement: ceil(weight/2) + 1 for OneHand.
        /// At weight=1 that's 2 required Strength; test creatures have Strength=16.
        /// </summary>
        private Entity CreateItem(Zone zone, int x, int y, string name = "rock", int weight = 1)
        {
            var item = new Entity { BlueprintName = name };
            item.AddPart(new RenderPart { DisplayName = name });
            item.AddPart(new PhysicsPart { Solid = false, Takeable = true, Weight = weight });
            zone.AddEntity(item, x, y);
            return item;
        }

        /// <summary>
        /// Place a wall blueprint on a cell. Walls tag the cell with "Solid", which
        /// Cell.IsPassable() and AIHelpers.HasLineOfSight both respect.
        /// </summary>
        private Entity CreateWall(Zone zone, int x, int y)
        {
            var wall = new Entity { BlueprintName = "Wall" };
            wall.Tags["Solid"] = "";
            wall.Tags["Wall"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall", RenderString = "#" });
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, x, y);
            return wall;
        }

        // ========================
        // FleeLocationGoal
        // ========================

        [Test]
        public void FleeLocationGoal_CanFight_IsFalse()
        {
            var goal = new FleeLocationGoal(5, 5);
            Assert.IsFalse(goal.CanFight(),
                "FleeLocationGoal should not be interrupted by combat acquisition");
        }

        [Test]
        public void FleeLocationGoal_Finished_WhenAtWaypoint()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            var brain = creature.GetPart<BrainPart>();

            var goal = new FleeLocationGoal(5, 5, endWhenNotFleeing: false);
            brain.PushGoal(goal);

            Assert.IsTrue(goal.Finished(),
                "FleeLocationGoal should finish immediately when NPC is already at the waypoint");
        }

        [Test]
        public void FleeLocationGoal_PushesMoveToChild_WhenNotAtWaypoint()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 1, 1);
            var brain = creature.GetPart<BrainPart>();

            var goal = new FleeLocationGoal(10, 10, endWhenNotFleeing: false);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "FleeLocationGoal should push a MoveToGoal child to route to the safe cell");
        }

        [Test]
        public void FleeLocationGoal_Finished_WhenAgeExceedsMaxTurns()
        {
            var goal = new FleeLocationGoal(99, 99, maxTurns: 5);
            goal.Age = 6;
            Assert.IsTrue(goal.Finished(), "Age > MaxTurns should terminate the goal");
        }

        // ========================
        // WanderDurationGoal
        // ========================

        [Test]
        public void WanderDurationGoal_Finished_AfterDurationTicks()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new WanderDurationGoal(duration: 3);
            brain.PushGoal(goal);

            Assert.IsFalse(goal.Finished(), "Fresh goal should not be finished");

            // Each TakeAction pushes a WanderRandomlyGoal child. After the child pops,
            // the next tick advances WanderDurationGoal's counter.
            for (int i = 0; i < 3; i++)
            {
                // Simulate one tick: increment age on parent, run TakeAction on parent,
                // run child, pop child.
                goal.Age++;
                goal.TakeAction();
                // The pushed WanderRandomlyGoal executes, finishes, and pops — we just
                // clean manually here.
                var child = brain.PeekGoal();
                if (child is WanderRandomlyGoal wr)
                {
                    wr.TakeAction();
                    brain.RemoveGoal(wr);
                }
            }

            Assert.IsTrue(goal.Finished(),
                "WanderDurationGoal should finish after Duration ticks of TakeAction");
        }

        [Test]
        public void WanderDurationGoal_IsBusy_IsFalse()
        {
            var goal = new WanderDurationGoal(10);
            Assert.IsFalse(goal.IsBusy(),
                "WanderDurationGoal should report IsBusy=false (idle behavior, combat can interrupt)");
        }

        // ========================
        // GoFetchGoal
        // ========================

        [Test]
        public void GoFetchGoal_PicksUpItem_WhenStandingOnItemCell()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            creature.AddPart(new InventoryPart());
            var brain = creature.GetPart<BrainPart>();

            var rock = CreateItem(zone, 5, 5);
            var goal = new GoFetchGoal(rock);
            brain.PushGoal(goal);

            goal.TakeAction(); // WalkToItem phase -> transitions to Pickup, calls DoPickup

            var inv = creature.GetPart<InventoryPart>();
            Assert.IsTrue(inv.Objects.Contains(rock),
                "GoFetchGoal should pick up the item when the NPC is on the item's cell");
        }

        [Test]
        public void GoFetchGoal_PopsWhenItemGone()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            creature.AddPart(new InventoryPart());
            var brain = creature.GetPart<BrainPart>();

            var rock = CreateItem(zone, 10, 10);
            var goal = new GoFetchGoal(rock);
            brain.PushGoal(goal);

            // Remove the item from the zone before the goal runs
            zone.RemoveEntity(rock);
            goal.TakeAction();

            Assert.AreEqual(0, brain.GoalCount,
                "GoFetchGoal should pop itself when the item is no longer in the zone");
        }

        [Test]
        public void GoFetchGoal_NullItem_PopsImmediately()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            creature.AddPart(new InventoryPart());
            var brain = creature.GetPart<BrainPart>();

            var goal = new GoFetchGoal(null);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.AreEqual(0, brain.GoalCount, "Null item should cause immediate pop");
        }

        // ========================
        // PetGoal
        // ========================

        [Test]
        public void PetGoal_PopsWhenAlreadyAdjacentToTarget()
        {
            var zone = new Zone("TestZone");
            var petter = CreateCreature(zone, 5, 5, "Villagers");
            var ally = CreateCreature(zone, 6, 5, "Villagers");
            var brain = petter.GetPart<BrainPart>();

            var goal = new PetGoal(ally);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(),
                "PetGoal should finish after emitting the pet effect when already adjacent to the ally");
        }

        [Test]
        public void PetGoal_FindsAlly_ByFaction()
        {
            var zone = new Zone("TestZone");
            var petter = CreateCreature(zone, 5, 5, "Villagers");
            var ally = CreateCreature(zone, 6, 5, "Villagers"); // same faction → allied
            var brain = petter.GetPart<BrainPart>();

            var goal = new PetGoal(); // no explicit target — search
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.AreSame(ally, goal.Target,
                "PetGoal should find the nearest allied Creature via faction");
        }

        [Test]
        public void PetGoal_PopsWhenNoAllyFound()
        {
            var zone = new Zone("TestZone");
            var petter = CreateCreature(zone, 5, 5, "Villagers");
            // No allies in zone
            var brain = petter.GetPart<BrainPart>();

            var goal = new PetGoal();
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.AreEqual(0, brain.GoalCount,
                "PetGoal should pop when no ally is found in sight radius");
        }

        [Test]
        public void PetGoal_IgnoresHostiles()
        {
            var zone = new Zone("TestZone");
            var petter = CreateCreature(zone, 5, 5, "Villagers");
            var hostile = CreateCreature(zone, 6, 5, "Snapjaws");
            var brain = petter.GetPart<BrainPart>();

            var goal = new PetGoal();
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsNull(goal.Target, "PetGoal should NOT target a hostile creature");
            Assert.AreEqual(0, brain.GoalCount, "No ally found → pop");
        }

        // ========================
        // BrainPart.Passive + BoredGoal gating
        // ========================

        [Test]
        public void Passive_DoesNotInitiateCombat_OnSightOfHostile()
        {
            var zone = new Zone("TestZone");
            var villager = CreateCreature(zone, 10, 10, "Villagers", hp: 100);
            var brain = villager.GetPart<BrainPart>();
            brain.Passive = true;
            brain.SightRadius = 12;

            var snapjaw = CreateCreature(zone, 12, 10, "Snapjaws");

            villager.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<KillGoal>(),
                "Passive creatures should NOT push KillGoal proactively on hostile sight");
            Assert.IsFalse(brain.HasGoal<FleeGoal>(),
                "With full HP, Passive creature should not flee either");
        }

        [Test]
        public void Passive_StillFlees_WhenHpLow()
        {
            var zone = new Zone("TestZone");
            var villager = CreateCreature(zone, 10, 10, "Villagers", hp: 100);
            var brain = villager.GetPart<BrainPart>();
            brain.Passive = true;
            brain.SightRadius = 12;

            // Drop HP below FleeThreshold (25%)
            villager.GetStat("Hitpoints").BaseValue = 10; // 10% of 100

            var snapjaw = CreateCreature(zone, 12, 10, "Snapjaws");
            villager.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<FleeGoal>(),
                "Passive creature with low HP should still flee from a threat");
        }

        [Test]
        public void Passive_StillFightsBack_WhenPersonallyHostile()
        {
            var zone = new Zone("TestZone");
            var villager = CreateCreature(zone, 10, 10, "Villagers", hp: 100);
            var brain = villager.GetPart<BrainPart>();
            brain.Passive = true;
            brain.SightRadius = 12;

            var snapjaw = CreateCreature(zone, 12, 10, "Snapjaws");
            brain.SetPersonallyHostile(snapjaw); // direct aggro

            villager.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<KillGoal>(),
                "Passive creature should fight back against entities in its PersonalEnemies list");
        }

        [Test]
        public void NonPassive_InitiatesCombat_OnSight()
        {
            var zone = new Zone("TestZone");
            var villager = CreateCreature(zone, 10, 10, "Villagers", hp: 100);
            var brain = villager.GetPart<BrainPart>();
            brain.Passive = false; // default, being explicit
            brain.SightRadius = 12;

            var snapjaw = CreateCreature(zone, 12, 10, "Snapjaws");
            villager.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<KillGoal>(),
                "Baseline behavior: non-passive creature pushes KillGoal on hostile sight");
        }

        // ========================
        // NoFightGoal
        // ========================

        [Test]
        public void NoFightGoal_CanFight_IsFalse()
        {
            var goal = new NoFightGoal();
            Assert.IsFalse(goal.CanFight(),
                "NoFightGoal exists to suppress combat — CanFight must be false");
        }

        [Test]
        public void NoFightGoal_InfiniteDuration_NeverFinishes()
        {
            var goal = new NoFightGoal(duration: 0); // 0 means infinite
            goal.Age = 1000;
            Assert.IsFalse(goal.Finished(),
                "NoFightGoal with duration=0 should stay active indefinitely");
        }

        [Test]
        public void NoFightGoal_FixedDuration_Finishes()
        {
            var goal = new NoFightGoal(duration: 5);
            goal.Age = 4;
            Assert.IsFalse(goal.Finished());
            goal.Age = 5;
            Assert.IsTrue(goal.Finished(),
                "NoFightGoal should finish once Age reaches Duration");
        }

        [Test]
        public void NoFightGoal_OnTopOfStack_BlocksBoredGoalCombatAcquisition()
        {
            var zone = new Zone("TestZone");
            var villager = CreateCreature(zone, 10, 10, "Villagers");
            var brain = villager.GetPart<BrainPart>();
            brain.SightRadius = 12;

            var snapjaw = CreateCreature(zone, 12, 10, "Snapjaws");

            // Push NoFightGoal on top — BoredGoal won't run, no KillGoal pushed.
            brain.PushGoal(new NoFightGoal());
            villager.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<KillGoal>(),
                "While NoFightGoal is on the stack, BoredGoal never runs — no combat initiation");
        }

        // ========================
        // RetreatGoal
        // ========================

        [Test]
        public void RetreatGoal_CanFight_IsFalse()
        {
            var goal = new RetreatGoal(5, 5);
            Assert.IsFalse(goal.CanFight(), "RetreatGoal disables combat acquisition during travel + recovery");
        }

        [Test]
        public void RetreatGoal_AtWaypointWithFullHp_Finishes()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            var brain = creature.GetPart<BrainPart>();

            var goal = new RetreatGoal(5, 5, safeHpFraction: 0.75f);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(),
                "At waypoint with full HP (> safe fraction), RetreatGoal should finish");
        }

        [Test]
        public void RetreatGoal_AtWaypointWithLowHp_StaysToRecover()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            creature.GetStat("Hitpoints").BaseValue = 5; // 25% of 20 max
            var brain = creature.GetPart<BrainPart>();

            var goal = new RetreatGoal(5, 5, safeHpFraction: 0.75f);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsFalse(goal.Finished(),
                "At waypoint with low HP, RetreatGoal should keep recovering (not done yet)");
        }

        [Test]
        public void RetreatGoal_NotAtWaypoint_PushesMoveTo()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new RetreatGoal(5, 5);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "RetreatGoal should push MoveToGoal toward the waypoint when not there yet");
        }

        [Test]
        public void RetreatGoal_Recovery_HealsHpPerTick_WithoutExternalRegen()
        {
            // M1 code review Bug 2 / Test gap 13:
            //
            // RetreatGoal.Recover phase must be self-healing so NPCs without
            // RegenerationMutation (most of them) can recover HP and exit
            // retreat. Without per-tick heal, Recover would block on
            // `hp/maxHp >= SafeFraction` forever and the goal would only
            // terminate via the MaxTurns safety cap.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5); // HP 20/20 max (from helper)
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 5; // 25% HP

            var goal = new RetreatGoal(5, 5, safeHpFraction: 0.75f, maxTurns: 200, healPerTick: 1);
            brain.PushGoal(goal);

            // First tick: Travel → already at waypoint → phase = Recover → heal first tick
            goal.TakeAction();
            Assert.IsFalse(goal.Finished(), "Should still be recovering");

            // Keep ticking; HP 5 → 15 takes 10 ticks at 1/tick, well within maxTurns
            int tickBudget = 20;
            while (tickBudget > 0 && !goal.Finished())
            {
                goal.TakeAction();
                tickBudget--;
            }

            Assert.IsTrue(goal.Finished(),
                "RetreatGoal.Recover should self-heal to SafeFraction within 20 ticks " +
                "at healPerTick=1 (regardless of whether the creature has RegenerationMutation).");
            Assert.GreaterOrEqual(creature.GetStatValue("Hitpoints", 0), 15,
                "Creature HP should reach at least 15/20 (=75% SafeFraction) before goal finishes.");
        }

        [Test]
        public void RetreatGoal_Recovery_ClampsHealToMaxHp()
        {
            // Recovery heal should not overflow Max HP.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 19; // 1 below max

            // SafeFraction 1.0 = must reach full HP to finish
            var goal = new RetreatGoal(5, 5, safeHpFraction: 1.0f, healPerTick: 5);
            brain.PushGoal(goal);

            goal.TakeAction(); // heals by 5 → but clamped to 20

            Assert.AreEqual(20, creature.GetStatValue("Hitpoints", 0),
                "Heal must clamp to Max HP, not overflow.");
            Assert.IsTrue(goal.Finished(),
                "At max HP with SafeFraction=1.0, goal should complete.");
        }

        [Test]
        public void RetreatGoal_Recovery_Exit_UsesBaseValue_NotPenalizedValue()
        {
            // Regression for M1 post-review finding M1.R-3:
            //
            // The Recover phase self-heals by writing to hpStat.BaseValue,
            // but the exit gate used to compare Stat.Value (= BaseValue +
            // Bonus - Penalty + Boost, clamped) against Max. If the NPC
            // carried a Penalty — bleed, poison, exhaustion, debuff aura —
            // BaseValue could heal to full while Value stayed pinned below
            // the SafeFraction forever. Recovery would deadlock and only
            // exit via the MaxTurns safety cap, then get re-pushed by
            // AISelfPreservation on the next AIBored tick.
            //
            // Fix: the gate now compares BaseValue / Max, which is the
            // quantity the heal actually moves.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5); // HP 20/20 max
            var brain = creature.GetPart<BrainPart>();
            var hp = creature.GetStat("Hitpoints");
            hp.BaseValue = 5;       // 25% of Max
            hp.Penalty = 18;        // heavy debuff — Value clamps near Min

            // Sanity check: with Penalty=18, Stat.Value is near Min, so a
            // Value-based gate would never clear SafeFraction=0.75 even if
            // BaseValue hits Max.
            Assert.LessOrEqual(hp.Value, 5,
                "Penalty should suppress Stat.Value; if this precondition " +
                "fails the test isn't exercising the bug.");

            var goal = new RetreatGoal(5, 5, safeHpFraction: 0.75f,
                                       maxTurns: 200, healPerTick: 5);
            brain.PushGoal(goal);

            // Tick until the goal finishes. HealPerTick=5 takes BaseValue
            // 5 → 10 → 15 → 20 → satisfied at 20/20 ≥ 0.75. Budget is
            // generous; the point is the goal MUST terminate despite the
            // persistent Penalty.
            int tickBudget = 20;
            while (tickBudget > 0 && !goal.Finished())
            {
                goal.TakeAction();
                tickBudget--;
            }

            Assert.IsTrue(goal.Finished(),
                "RetreatGoal.Recover should complete once BaseValue/Max " +
                "crosses SafeFraction, regardless of any persistent Penalty " +
                "on the stat. If this fails, the exit gate is comparing " +
                "Stat.Value again and debuffed NPCs will deadlock in retreat.");

            // Goal should have finished well before the MaxTurns=200 cap —
            // confirm that the exit path is the "healed to safe" path, not
            // the "gave up after MaxTurns" fallback.
            Assert.Less(goal.Age, 200,
                "Goal should exit via the SafeFraction gate, not MaxTurns. " +
                $"Age={goal.Age} suggests the MaxTurns fallback fired instead.");
        }

        [Test]
        public void RetreatGoal_Recovery_HealPerTickZero_FallsBackToMaxTurnsExit()
        {
            // If healPerTick=0 is explicitly configured (e.g., a creature using
            // external regen), the goal still has MaxTurns as a safety cap so
            // it never blocks forever. This pins that safety behavior.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 5; // 25% HP

            var goal = new RetreatGoal(5, 5, safeHpFraction: 0.75f, maxTurns: 3, healPerTick: 0);
            brain.PushGoal(goal);

            // Simulate ticks: no heal, HP stays at 5, SafeFraction never reached.
            // MaxTurns=3 → Age > 3 triggers Finished().
            for (int i = 0; i < 5; i++)
            {
                goal.Age++;
                goal.TakeAction();
            }

            Assert.IsTrue(goal.Finished(),
                "With healPerTick=0 and no external regen, MaxTurns cap must eventually " +
                "terminate the goal so the NPC isn't stuck forever.");
            Assert.AreEqual(5, creature.GetStatValue("Hitpoints", 0),
                "HP should not have changed when healPerTick=0.");
        }

        // ========================
        // AISelfPreservationPart
        // ========================

        [Test]
        public void AISelfPreservation_PushesRetreat_WhenHpLow()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            // Drop HP to 20% (below 50% threshold)
            creature.GetStat("Hitpoints").BaseValue = 4; // 4/20 = 20%

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<RetreatGoal>(),
                "AISelfPreservationPart should push RetreatGoal when HP is below the threshold");
        }

        [Test]
        public void AISelfPreservation_DoesNothing_WhenHpFull()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "AISelfPreservationPart should not push RetreatGoal when HP is above threshold");
        }

        [Test]
        public void AISelfPreservation_DoesNotDoublePushRetreat_EventPath()
        {
            // Directly verifies the HasGoal("RetreatGoal") gate in AISelfPreservationPart.
            // Firing AIBored twice on a brain that already has a RetreatGoal should not
            // result in a second RetreatGoal being pushed.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 4;

            // Pre-populate the stack with a RetreatGoal as if a previous tick pushed it.
            brain.PushGoal(new RetreatGoal(10, 10));
            int goalCountBefore = brain.GoalCount;

            // Fire AIBored directly. AISelfPreservation should see HasGoal("RetreatGoal")
            // and skip pushing another one.
            var e = GameEvent.New("AIBored");
            creature.FireEvent(e);

            // Pop the first RetreatGoal and confirm no second one was pushed.
            var first = brain.FindGoal<RetreatGoal>();
            Assert.IsNotNull(first, "Pre-existing RetreatGoal should still be on the stack");
            brain.RemoveGoal(first);
            var second = brain.FindGoal<RetreatGoal>();
            Assert.IsNull(second,
                "AISelfPreservationPart should not push a second RetreatGoal when one is already active");
            Assert.AreEqual(goalCountBefore - 1, brain.GoalCount,
                "Only the manually-pushed RetreatGoal existed; stack count should drop by exactly one");
        }

        [Test]
        public void AISelfPreservation_DoesNotDoublePushRetreat_FullTurnFlow()
        {
            // End-to-end version: drives the full TakeTurn flow across multiple ticks.
            // First tick pushes RetreatGoal via AIBored. Once RetreatGoal is the top
            // of the stack, BoredGoal doesn't run, so AIBored doesn't fire — meaning
            // AISelfPreservation cannot accidentally double-push. This test pins
            // that behavior so it survives future goal-loop refactors.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 4; // 20% HP — below retreat threshold

            // Tick 1: BoredGoal pushed, AIBored fires, RetreatGoal pushed, RetreatGoal
            // runs via child-chain (at waypoint, HP still low → stays in Recover).
            creature.FireEvent(GameEvent.New("TakeTurn"));
            Assert.IsTrue(brain.HasGoal<RetreatGoal>(), "Tick 1: RetreatGoal should be on the stack");
            int countAfterTick1 = brain.GoalCount;

            // Tick 2: RetreatGoal is top; BoredGoal does not run this tick.
            creature.FireEvent(GameEvent.New("TakeTurn"));

            // There should be exactly one RetreatGoal. Pop it and verify no second.
            var first = brain.FindGoal<RetreatGoal>();
            Assert.IsNotNull(first, "Tick 2: RetreatGoal should still be present");
            brain.RemoveGoal(first);
            Assert.IsNull(brain.FindGoal<RetreatGoal>(),
                "End-to-end: exactly one RetreatGoal across two ticks; no duplicate push");
        }

        [Test]
        public void AISelfPreservation_SwappedThresholds_DoesNotThrash()
        {
            // Regression: if a blueprint author configures RetreatThreshold >= SafeThreshold,
            // the old implementation would push RetreatGoal, immediately finish it
            // (HP already above "safe"), pop, push again next tick... forever.
            // EffectiveSafeThreshold clamps to RetreatThreshold + MinThresholdGap.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            creature.AddPart(new AISelfPreservationPart
            {
                RetreatThreshold = 0.6f,
                SafeThreshold = 0.3f // intentionally swapped
            });
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 50; // 50% — below RetreatThreshold (60%)

            // Fire AIBored directly so we can observe the pushed RetreatGoal without
            // the child-chain advancing it through Recover.
            var e = GameEvent.New("AIBored");
            creature.FireEvent(e);

            var retreat = brain.FindGoal<RetreatGoal>();
            Assert.IsNotNull(retreat, "RetreatGoal should be pushed");
            Assert.Greater(retreat.SafeHpFraction, 0.6f,
                "EffectiveSafeThreshold must clamp above RetreatThreshold to prevent thrashing");
        }

        // ========================
        // PetGoal attempt counter
        // ========================

        [Test]
        public void PetGoal_GivesUp_AfterMaxApproachAttempts()
        {
            // Regression: PetGoal could infinite-loop if MoveToGoal kept hitting its
            // Age-based timeout (which pops silently, not via FailToParent).
            // After MaxApproachAttempts pushes, PetGoal should mark itself Done.
            var zone = new Zone("TestZone");
            var petter = CreateCreature(zone, 5, 5, "Villagers");
            var ally = CreateCreature(zone, 20, 20, "Villagers"); // far away
            var brain = petter.GetPart<BrainPart>();

            var goal = new PetGoal(ally);
            brain.PushGoal(goal);

            // Simulate the loop: each TakeAction either pets (adjacent) or pushes MoveToGoal.
            // We clear any child MoveToGoal manually to simulate its silent timeout.
            for (int i = 0; i < PetGoal.MaxApproachAttempts + 2; i++)
            {
                if (goal.Finished()) break;
                goal.TakeAction();
                // Manually pop any MoveToGoal child to simulate its Age-based timeout.
                var child = brain.PeekGoal();
                if (child is MoveToGoal mt)
                    brain.RemoveGoal(mt);
            }

            Assert.IsTrue(goal.Finished(),
                "PetGoal must terminate within MaxApproachAttempts even if ally is unreachable");
        }

        // ========================
        // GoFetchGoal attempt counter
        // ========================

        [Test]
        public void GoFetchGoal_GivesUp_AfterMaxWalkAttempts()
        {
            // Regression: GoFetchGoal could infinite-loop when MoveToGoal timed out
            // silently. After MaxWalkAttempts unsuccessful pushes, the goal should pop.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            creature.AddPart(new InventoryPart());
            var brain = creature.GetPart<BrainPart>();

            var rock = CreateItem(zone, 25, 25); // far away
            var goal = new GoFetchGoal(rock);
            brain.PushGoal(goal);

            int iterations = 0;
            while (brain.GoalCount > 0 && iterations < GoFetchGoal.MaxWalkAttempts + 3)
            {
                goal.TakeAction();
                // Silently pop any MoveToGoal child to simulate timeout.
                var child = brain.PeekGoal();
                if (child is MoveToGoal mt)
                    brain.RemoveGoal(mt);
                iterations++;
            }

            Assert.AreEqual(0, brain.GoalCount,
                "GoFetchGoal must pop within MaxWalkAttempts when the item stays unreachable");
            var inv = creature.GetPart<InventoryPart>();
            Assert.IsFalse(inv.Objects.Contains(rock),
                "Far-away rock should not end up in inventory after give-up");
        }

        [Test]
        public void AISelfPreservation_NoStartingCell_DoesNotPush()
        {
            var zone = new Zone("TestZone");
            var creature = new Entity { BlueprintName = "TestCreature" };
            creature.Tags["Creature"] = "";
            creature.Tags["Faction"] = "Villagers";
            creature.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 4, Min = 0, Max = 20 };
            creature.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            creature.AddPart(new RenderPart());
            creature.AddPart(new PhysicsPart { Solid = true });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            zone.AddEntity(creature, 10, 10);
            // Explicitly ensure no StartingCell set yet
            brain.StartingCellX = -1;
            brain.StartingCellY = -1;

            // Avoid auto-setting StartingCell during TakeTurn by simulating only the
            // AIBoredEvent directly.
            var e = GameEvent.New("AIBored");
            creature.FireEvent(e);

            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "AISelfPreservationPart requires StartingCell — without one, it should not push RetreatGoal");
        }

        // ========================
        // DormantGoal
        // ========================

        [Test]
        public void DormantGoal_CanFight_IsFalse()
        {
            var goal = new DormantGoal();
            Assert.IsFalse(goal.CanFight(), "DormantGoal should suppress combat until woken");
        }

        [Test]
        public void DormantGoal_DoesNotFinish_WhenQuiet()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new DormantGoal(wakeOnDamage: true, wakeOnHostileInSight: false);
            brain.PushGoal(goal);
            goal.TakeAction();
            goal.TakeAction();

            Assert.IsFalse(goal.Finished(), "DormantGoal should stay asleep when no wake trigger fires");
        }

        [Test]
        public void DormantGoal_WakesOnDamage()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            var brain = creature.GetPart<BrainPart>();

            var goal = new DormantGoal(wakeOnDamage: true);
            brain.PushGoal(goal);
            goal.TakeAction(); // baseline HP captured

            // Drop HP — simulating being attacked
            creature.GetStat("Hitpoints").BaseValue = 80;
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(),
                "DormantGoal with WakeOnDamage should finish after HP decreases between ticks");
        }

        [Test]
        public void DormantGoal_WakesOnHostileInSight()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, "Villagers");
            var brain = creature.GetPart<BrainPart>();
            brain.SightRadius = 12;

            var goal = new DormantGoal(wakeOnDamage: false, wakeOnHostileInSight: true);
            brain.PushGoal(goal);
            goal.TakeAction();
            Assert.IsFalse(goal.Finished(), "No hostile yet — stays dormant");

            var snapjaw = CreateCreature(zone, 12, 10, "Snapjaws");
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(),
                "DormantGoal with WakeOnHostileInSight should finish when a hostile enters sight");
            Assert.AreSame(snapjaw, brain.Target,
                "Woken creature should have Target set to the triggering hostile");
        }

        [Test]
        public void DormantGoal_Wake_MarksFinished()
        {
            var goal = new DormantGoal();
            Assert.IsFalse(goal.Finished());
            goal.Wake();
            Assert.IsTrue(goal.Finished(), "Explicit Wake() should mark the goal finished");
        }

        [Test]
        public void DormantGoal_IgnoresDamageWhenFlagOff()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            var brain = creature.GetPart<BrainPart>();

            var goal = new DormantGoal(wakeOnDamage: false);
            brain.PushGoal(goal);
            goal.TakeAction();
            creature.GetStat("Hitpoints").BaseValue = 50;
            goal.TakeAction();

            Assert.IsFalse(goal.Finished(),
                "DormantGoal with WakeOnDamage=false should stay dormant even when HP drops");
        }

        [Test]
        public void DormantGoal_WakeOnHostileInSight_RespectsLineOfSight()
        {
            // Regression: wake-on-sight uses AIHelpers.FindNearestHostile, which
            // applies LOS filtering. A hostile blocked by a wall should NOT wake
            // a dormant creature.
            var zone = new Zone("TestZone");
            var sleeper = CreateCreature(zone, 10, 10, "Villagers");
            var brain = sleeper.GetPart<BrainPart>();
            brain.SightRadius = 12;

            // Wall between sleeper and hostile
            CreateWall(zone, 11, 10);
            var snapjaw = CreateCreature(zone, 12, 10, "Snapjaws");

            var goal = new DormantGoal(wakeOnDamage: false, wakeOnHostileInSight: true);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsFalse(goal.Finished(),
                "DormantGoal must respect LOS — wall should block the wake trigger");
            Assert.IsNull(brain.Target,
                "Target should stay null while the hostile is out of sight");
        }

        [Test]
        public void DormantGoal_EmitsSleepParticle_AtInterval()
        {
            // Verifies the Age-gated sleep particle path in TakeAction doesn't throw
            // and runs when Age % SleepParticleInterval == 0 (Age > 0).
            // AsciiFxBus queues a request rather than rendering synchronously, so we
            // primarily check that the code path executes without exception.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new DormantGoal(wakeOnDamage: false, sleepParticleInterval: 4);
            brain.PushGoal(goal);

            // Tick at Age=4 (multiple of interval) — particle emission branch runs.
            goal.Age = 4;
            Assert.DoesNotThrow(() => goal.TakeAction(),
                "Sleep particle emission path should not throw at interval-aligned Age");
            Assert.IsFalse(goal.Finished(),
                "Emitting the sleep particle shouldn't wake the goal");

            // Tick at Age=0: interval check is `Age > 0 && Age % interval == 0` so
            // the particle path is skipped on push-tick.
            var freshGoal = new DormantGoal(wakeOnDamage: false, sleepParticleInterval: 4);
            brain.PushGoal(freshGoal);
            Assert.DoesNotThrow(() => freshGoal.TakeAction(),
                "Push-tick (Age=0) should skip sleep particle without throwing");
        }

        [Test]
        public void DormantGoal_SleepParticleInterval_Zero_DisablesParticle()
        {
            // SleepParticleInterval = 0 → no emission, no modulo-by-zero.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new DormantGoal(wakeOnDamage: false, sleepParticleInterval: 0);
            brain.PushGoal(goal);
            goal.Age = 10;

            Assert.DoesNotThrow(() => goal.TakeAction(),
                "SleepParticleInterval=0 must not trigger a modulo-by-zero or emit");
        }

        // ========================
        // NoFightGoal Wander branch
        // ========================

        [Test]
        public void NoFightGoal_Wander_PushesWanderRandomly()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new NoFightGoal(duration: 0, wander: true);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(brain.HasGoal<WanderRandomlyGoal>(),
                "NoFightGoal with Wander=true should push WanderRandomlyGoal each tick");
        }

        [Test]
        public void NoFightGoal_NoWander_DoesNotPushChildren()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new NoFightGoal(duration: 0, wander: false);
            brain.PushGoal(goal);
            int before = brain.GoalCount;
            goal.TakeAction();

            Assert.AreEqual(before, brain.GoalCount,
                "NoFightGoal with Wander=false should not push any children (just idles)");
        }

        [Test]
        public void NoFightGoal_AlsoStarvesAISelfPreservation()
        {
            // Documents the known side-effect: while NoFightGoal is on top, AIBored
            // never fires, so AISelfPreservationPart cannot push RetreatGoal even
            // at critical HP. This is captured as expected behavior — the creature
            // must remove NoFightGoal via a higher-priority path if emergency
            // retreat is required.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 5; // critical

            brain.PushGoal(new NoFightGoal()); // pacified

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "While NoFightGoal blocks BoredGoal, AISelfPreservation cannot push RetreatGoal");
        }
    }
}
