using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 6 Milestone M1.3 — verifies AIAmbushPart pushes DormantGoal on
    /// Part.Initialize (at construction time, before any TakeTurn), that the
    /// SleepingTroll/MimicChest/AmbushBandit blueprints load correctly with
    /// their per-creature wake-trigger configurations, and that the ambush
    /// push is idempotent across repeated TakeTurns.
    ///
    /// Push-timing note: DormantGoal is pushed in Initialize (Part.AddPart hook)
    /// to avoid the turn-1 ordering bug where BrainPart.HandleEvent would
    /// otherwise fire BoredGoal on an empty stack before AIAmbush got a chance
    /// to push DormantGoal. See Phase 6 M1 code review Bug 1 in QUD-PARITY.md.
    /// </summary>
    [TestFixture]
    public class AIAmbushPartTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        // ========================
        // Helper
        // ========================

        /// <summary>
        /// Create a minimal creature with BrainPart + AIAmbushPart for direct unit tests.
        /// Separate from blueprint loading so we can test the part in isolation.
        /// </summary>
        private Entity CreateAmbushCreature(Zone zone, int x, int y,
            bool wakeOnDamage = true, bool wakeOnHostileInSight = true)
        {
            var entity = new Entity { BlueprintName = "TestAmbusher" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Snapjaws";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart());
            entity.AddPart(new PhysicsPart { Solid = true });
            var brain = new BrainPart { CurrentZone = zone, Rng = new System.Random(1) };
            entity.AddPart(brain);
            entity.AddPart(new AIAmbushPart
            {
                WakeOnDamage = wakeOnDamage,
                WakeOnHostileInSight = wakeOnHostileInSight,
                SleepParticleInterval = 8
            });
            zone.AddEntity(entity, x, y);
            return entity;
        }

        // ========================
        // Part behavior tests
        // ========================

        [Test]
        public void AIAmbush_PushesDormantGoal_AtConstructionTime()
        {
            // AIAmbushPart.Initialize runs when the part is attached to the entity
            // (inside Entity.AddPart). DormantGoal should be on the stack immediately,
            // before any TakeTurn has fired.
            var zone = new Zone("TestZone");
            var creature = CreateAmbushCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            Assert.IsTrue(brain.HasGoal<DormantGoal>(),
                "AIAmbushPart.Initialize should push DormantGoal at construction time, " +
                "not on first TakeTurn (prevents BoredGoal from running before ambush).");
        }

        [Test]
        public void AIAmbush_DormantGoalOnTop_NotBoredGoal_AfterFirstTakeTurn()
        {
            // Regression test for M1 code review Bug 1: turn-1 ordering.
            //
            // Previously AIAmbushPart pushed DormantGoal on TakeTurn, which fired
            // AFTER BrainPart.HandleEvent (due to blueprint part-declaration order).
            // That meant BrainPart.HandleTakeTurn pushed BoredGoal onto the empty
            // stack and executed a tick of wandering/scanning BEFORE DormantGoal
            // ever reached the top.
            //
            // With the Initialize-based push fix, DormantGoal is already on the stack
            // when BrainPart.HandleTakeTurn runs. Stack-empty check fails, BoredGoal
            // never gets pushed, and the first tick executes DormantGoal.TakeAction.
            var zone = new Zone("TestZone");
            var creature = CreateAmbushCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual(1, brain.GoalCount,
                "Stack should contain exactly one goal (DormantGoal). BoredGoal must not leak in.");
            Assert.IsTrue(brain.PeekGoal() is DormantGoal,
                "Top goal should be DormantGoal, not BoredGoal.");
            Assert.IsFalse(brain.HasGoal<BoredGoal>(),
                "BoredGoal should never be pushed — DormantGoal was already on the stack " +
                "before BrainPart.HandleTakeTurn's empty-stack check.");
        }

        [Test]
        public void AIAmbush_DoesNotPushDormantGoalTwice_AcrossRepeatedTurns()
        {
            var zone = new Zone("TestZone");
            var creature = CreateAmbushCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            // DormantGoal is already pushed at construction (Initialize). Firing
            // TakeTurn repeatedly should NOT re-push via the HandleEvent fallback.
            for (int i = 0; i < 5; i++)
                creature.FireEvent(GameEvent.New("TakeTurn"));

            // Exactly ONE DormantGoal should exist. Pop the first and ensure no second.
            var first = brain.FindGoal<DormantGoal>();
            Assert.IsNotNull(first, "A DormantGoal should be present");
            brain.RemoveGoal(first);
            Assert.IsNull(brain.FindGoal<DormantGoal>(),
                "AIAmbushPart must push DormantGoal exactly once (via _dormantPushed flag).");
        }

        [Test]
        public void AIAmbush_Rearm_AllowsReAmbushAfterWake()
        {
            // Polish 5: Rearm() clears _dormantPushed so a woken creature can
            // re-enter ambush mode (e.g., via a Sleep status effect). Without
            // Rearm, the flag remains set and AIAmbush can't re-push.
            var zone = new Zone("TestZone");
            var creature = CreateAmbushCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();
            var ambush = creature.GetPart<AIAmbushPart>();

            // Pop initial DormantGoal (simulating wake)
            var initial = brain.FindGoal<DormantGoal>();
            brain.RemoveGoal(initial);
            Assert.IsFalse(brain.HasGoal<DormantGoal>(), "DormantGoal removed post-wake");

            // TakeTurn without Rearm — should NOT re-push
            creature.FireEvent(GameEvent.New("TakeTurn"));
            Assert.IsFalse(brain.HasGoal<DormantGoal>(),
                "Without Rearm, AIAmbush must not re-push after a wake");

            // Rearm + TakeTurn — should push again via fallback path
            ambush.Rearm();
            creature.FireEvent(GameEvent.New("TakeTurn"));
            Assert.IsTrue(brain.HasGoal<DormantGoal>(),
                "After Rearm, next TakeTurn fallback should re-push DormantGoal");
        }

        [Test]
        public void AIAmbush_FallbackPushOnTakeTurn_WhenBrainAddedAfter()
        {
            // Defensive: Initialize push requires BrainPart to already exist on the entity.
            // If someone adds AIAmbush before Brain (unusual but possible), Initialize
            // can't find a brain and skips. The HandleEvent fallback catches this
            // on the first TakeTurn.
            var entity = new Entity { BlueprintName = "TestOrderingEdgeCase" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Snapjaws";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart());
            entity.AddPart(new PhysicsPart { Solid = true });

            // Reversed order: AIAmbush FIRST (no brain yet), then Brain
            entity.AddPart(new AIAmbushPart { WakeOnDamage = true, WakeOnHostileInSight = true });
            var brain = new BrainPart();
            entity.AddPart(brain);

            Assert.IsFalse(brain.HasGoal<DormantGoal>(),
                "Initialize couldn't find brain — no push yet");

            // Fallback kicks in on TakeTurn
            var zone = new Zone("TestZone");
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);
            zone.AddEntity(entity, 5, 5);
            entity.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<DormantGoal>(),
                "TakeTurn fallback should push DormantGoal when Initialize couldn't");
        }

        [Test]
        public void AIAmbush_PassesWakeFlags_ToPushedDormantGoal()
        {
            var zone = new Zone("TestZone");
            var creature = CreateAmbushCreature(zone, 10, 10,
                wakeOnDamage: false, wakeOnHostileInSight: true);
            var brain = creature.GetPart<BrainPart>();
            creature.FireEvent(GameEvent.New("TakeTurn"));

            var dormant = brain.FindGoal<DormantGoal>();
            Assert.IsNotNull(dormant);
            Assert.IsFalse(dormant.WakeOnDamage,
                "WakeOnDamage=false on AIAmbush must propagate to the pushed DormantGoal");
            Assert.IsTrue(dormant.WakeOnHostileInSight,
                "WakeOnHostileInSight=true on AIAmbush must propagate to the pushed DormantGoal");
        }

        // ========================
        // Blueprint loading tests
        // ========================

        [Test]
        public void SleepingTroll_Blueprint_Loads_WithAIAmbushAndCorrectConfig()
        {
            var troll = _factory.CreateEntity("SleepingTroll");
            Assert.IsNotNull(troll, "SleepingTroll blueprint should exist in Objects.json");

            var ambush = troll.GetPart<AIAmbushPart>();
            Assert.IsNotNull(ambush, "SleepingTroll must have AIAmbushPart");
            Assert.IsTrue(ambush.WakeOnDamage, "SleepingTroll wakes on damage");
            Assert.IsTrue(ambush.WakeOnHostileInSight, "SleepingTroll wakes when enemy enters sight");
            Assert.AreEqual(8, ambush.SleepParticleInterval,
                "SleepingTroll has visible 'z' particles every 8 ticks");
        }

        [Test]
        public void MimicChest_Blueprint_Loads_WithWakeOnSameCell()
        {
            // M1 code review Bug 3: MimicChest is Physics.Solid=false so the player
            // can walk onto it (preserving chest-like interaction), and its Brain
            // SightRadius=0 + WakeOnHostileInSight=true means it only wakes when
            // a hostile is ON its cell. Walking ADJACENT to it does nothing —
            // the disguise holds until the player actually steps on it.
            var mimic = _factory.CreateEntity("MimicChest");
            Assert.IsNotNull(mimic);

            var ambush = mimic.GetPart<AIAmbushPart>();
            Assert.IsNotNull(ambush);
            Assert.IsTrue(ambush.WakeOnDamage,
                "MimicChest wakes when attacked");
            Assert.IsTrue(ambush.WakeOnHostileInSight,
                "MimicChest wakes when hostile enters sight — but SightRadius=0 " +
                "narrows that to the mimic's own cell only");
            Assert.AreEqual(0, ambush.SleepParticleInterval,
                "MimicChest has no 'z' particles — it's disguised, not obviously asleep");

            var brain = mimic.GetPart<BrainPart>();
            Assert.AreEqual(0, brain.SightRadius,
                "SightRadius=0 restricts wake-on-sight to same-cell hostiles");

            var physics = mimic.GetPart<PhysicsPart>();
            Assert.IsFalse(physics.Solid,
                "MimicChest must be non-solid so players can walk onto it (like a real Chest), " +
                "preserving the chest-disguise surface interaction.");
        }

        [Test]
        public void AmbushBandit_Blueprint_Loads_WithHostileSightWake()
        {
            var bandit = _factory.CreateEntity("AmbushBandit");
            Assert.IsNotNull(bandit);

            var ambush = bandit.GetPart<AIAmbushPart>();
            Assert.IsNotNull(ambush);
            Assert.IsTrue(ambush.WakeOnDamage);
            Assert.IsTrue(ambush.WakeOnHostileInSight,
                "AmbushBandit pops out of hiding when he spots prey");
            Assert.AreEqual(0, ambush.SleepParticleInterval,
                "AmbushBandit is hidden in grass — no sleep particle");
        }

        [Test]
        public void SleepingTroll_RendersAsUppercase_T_Green()
        {
            var troll = _factory.CreateEntity("SleepingTroll");
            var render = troll.GetPart<RenderPart>();
            Assert.AreEqual("T", render.RenderString);
            Assert.AreEqual("&g", render.ColorString);
        }

        [Test]
        public void MimicChest_RendersAsChestGlyph()
        {
            // Critical for gameplay: must be visually indistinguishable from a real Chest
            var mimic = _factory.CreateEntity("MimicChest");
            var render = mimic.GetPart<RenderPart>();
            Assert.AreEqual("=", render.RenderString,
                "Mimic must look like a real chest (glyph '=')");
        }

        [Test]
        public void AmbushCreatures_AreHostileFaction()
        {
            // All three ambushers must be Snapjaws-faction so they engage
            // player/villagers when they wake up
            string[] ambushBlueprints = { "SleepingTroll", "MimicChest", "AmbushBandit" };
            foreach (var bp in ambushBlueprints)
            {
                var entity = _factory.CreateEntity(bp);
                Assert.AreEqual("Snapjaws", entity.GetTag("Faction"),
                    $"{bp} must be in Snapjaws faction so it's hostile when awake");
            }
        }

        // ========================
        // End-to-end: blueprint → TakeTurn → DormantGoal pushed
        // ========================

        [Test]
        public void SleepingTroll_FromBlueprint_PushesDormantOnFirstTurn()
        {
            var zone = new Zone("TestZone");
            var troll = _factory.CreateEntity("SleepingTroll");
            zone.AddEntity(troll, 10, 10);

            var brain = troll.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);

            troll.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<DormantGoal>(),
                "A blueprint-spawned SleepingTroll should be dormant on turn 1");
        }

        [Test]
        public void MimicChest_StaysDormantWhenHostileAdjacent_ButWakesOnSameCell()
        {
            // M1 code review Bug 3: MimicChest uses SightRadius=0 so it wakes
            // ONLY when a hostile is on its cell (distance 0). Adjacent hostiles
            // (distance 1) do not wake it — the chest disguise holds.
            var zone = new Zone("TestZone");
            var mimic = _factory.CreateEntity("MimicChest");
            zone.AddEntity(mimic, 10, 10);

            var brain = mimic.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);

            var villager = new Entity();
            villager.Tags["Creature"] = "";
            villager.Tags["Faction"] = "Villagers"; // hostile to mimic's Snapjaws faction
            villager.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            villager.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            villager.AddPart(new RenderPart());
            villager.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(villager, 11, 10); // adjacent — distance 1

            var dormant = brain.FindGoal<DormantGoal>();
            Assert.IsNotNull(dormant, "DormantGoal is pushed at Initialize");

            // Adjacent hostile: should stay dormant (outside SightRadius=0)
            dormant.TakeAction();
            dormant.TakeAction();
            Assert.IsFalse(dormant.Finished(),
                "MimicChest with SightRadius=0 must not wake when hostile is only adjacent");

            // Move hostile onto mimic's cell (distance 0): wakes
            zone.RemoveEntity(villager);
            zone.AddEntity(villager, 10, 10); // same cell now
            dormant.TakeAction();
            Assert.IsTrue(dormant.Finished(),
                "MimicChest must wake when a hostile steps onto its cell — the 'open chest' moment");
        }
    }
}
