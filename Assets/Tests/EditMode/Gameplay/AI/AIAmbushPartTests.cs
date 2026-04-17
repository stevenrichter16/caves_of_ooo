using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 6 Milestone M1.3 — verifies AIAmbushPart pushes DormantGoal on
    /// first TakeTurn, that the SleepingTroll/MimicChest/AmbushBandit blueprints
    /// load correctly with their per-creature wake-trigger configurations,
    /// and that the ambush push is idempotent across repeated TakeTurns.
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
        public void AIAmbush_PushesDormantGoal_OnFirstTakeTurn()
        {
            var zone = new Zone("TestZone");
            var creature = CreateAmbushCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            Assert.IsFalse(brain.HasGoal<DormantGoal>(),
                "Before first TakeTurn, there should be no DormantGoal");

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<DormantGoal>(),
                "After first TakeTurn, AIAmbushPart should have pushed DormantGoal");
        }

        [Test]
        public void AIAmbush_DoesNotPushDormantGoalTwice_AcrossRepeatedTurns()
        {
            var zone = new Zone("TestZone");
            var creature = CreateAmbushCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            for (int i = 0; i < 5; i++)
                creature.FireEvent(GameEvent.New("TakeTurn"));

            // Exactly ONE DormantGoal should exist. Pop the first and ensure no second.
            var first = brain.FindGoal<DormantGoal>();
            Assert.IsNotNull(first, "A DormantGoal should be present");
            brain.RemoveGoal(first);
            Assert.IsNull(brain.FindGoal<DormantGoal>(),
                "AIAmbushPart must push DormantGoal exactly once, not per-turn");
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
        public void MimicChest_Blueprint_Loads_WithDamageOnlyWake()
        {
            var mimic = _factory.CreateEntity("MimicChest");
            Assert.IsNotNull(mimic);

            var ambush = mimic.GetPart<AIAmbushPart>();
            Assert.IsNotNull(ambush);
            Assert.IsTrue(ambush.WakeOnDamage,
                "MimicChest wakes when attacked (someone tries to 'open' it)");
            Assert.IsFalse(ambush.WakeOnHostileInSight,
                "MimicChest must stay dormant when player is merely nearby — surprise is the point");
            Assert.AreEqual(0, ambush.SleepParticleInterval,
                "MimicChest has no 'z' particles — it's disguised, not obviously asleep");
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
        public void MimicChest_StaysDormantWhenHostileInSight()
        {
            // A player walking past a mimic should NOT trigger it (no WakeOnHostileInSight)
            var zone = new Zone("TestZone");
            var mimic = _factory.CreateEntity("MimicChest");
            zone.AddEntity(mimic, 10, 10);

            var brain = mimic.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);

            // Spawn hostile in sight
            var snapjaw = new Entity();
            snapjaw.Tags["Creature"] = "";
            snapjaw.Tags["Faction"] = "Villagers"; // hostile to Snapjaws faction
            snapjaw.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            snapjaw.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            snapjaw.AddPart(new RenderPart());
            snapjaw.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(snapjaw, 11, 10); // adjacent

            // Push dormant first
            mimic.FireEvent(GameEvent.New("TakeTurn"));

            // Then advance the dormant goal a few times; it should NOT wake on sight
            var dormant = brain.FindGoal<DormantGoal>();
            Assert.IsNotNull(dormant);
            dormant.TakeAction();
            dormant.TakeAction();
            Assert.IsFalse(dormant.Finished(),
                "MimicChest with WakeOnHostileInSight=false must stay dormant despite adjacent hostile");
        }
    }
}
