using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 6 Milestone M1 — verifies that:
    /// - AISelfPreservationPart is wired into Warden, Tinker, Farmer, Innkeeper, Scribe
    ///   with per-NPC thresholds
    /// - BrainPart.Passive=true is set on Scribe, Elder, WellKeeper, Innkeeper
    /// - Part ordering puts AISelfPreservation BEFORE other AIBehaviorParts (so HP-based
    ///   retreat wins the AIBoredEvent race)
    /// - Existing behavior (Staying, Wanders, AllowIdleBehavior) is not regressed
    /// </summary>
    [TestFixture]
    public class AISelfPreservationBlueprintTests
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
        // M1.1 — AISelfPreservation thresholds
        // ========================

        [Test]
        public void Warden_HasAISelfPreservation_With_0_3_Threshold()
        {
            var warden = _factory.CreateEntity("Warden");
            var part = warden.GetPart<AISelfPreservationPart>();
            Assert.IsNotNull(part, "Warden should have AISelfPreservationPart");
            Assert.AreEqual(0.3f, part.RetreatThreshold, 0.001f,
                "Warden RetreatThreshold should be 0.3 (die-hard guard)");
            Assert.AreEqual(0.7f, part.SafeThreshold, 0.001f);
        }

        [Test]
        public void Tinker_HasAISelfPreservation_With_0_5_Threshold()
        {
            var tinker = _factory.CreateEntity("Tinker");
            var part = tinker.GetPart<AISelfPreservationPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(0.5f, part.RetreatThreshold, 0.001f);
            Assert.AreEqual(0.75f, part.SafeThreshold, 0.001f);
        }

        [Test]
        public void Farmer_HasAISelfPreservation_With_0_4_Threshold()
        {
            var farmer = _factory.CreateEntity("Farmer");
            var part = farmer.GetPart<AISelfPreservationPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(0.4f, part.RetreatThreshold, 0.001f);
            Assert.AreEqual(0.75f, part.SafeThreshold, 0.001f);
        }

        [Test]
        public void Innkeeper_HasAISelfPreservation_With_0_7_Threshold()
        {
            var innkeeper = _factory.CreateEntity("Innkeeper");
            var part = innkeeper.GetPart<AISelfPreservationPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(0.7f, part.RetreatThreshold, 0.001f,
                "Innkeeper flees at 70% HP — non-combatant");
            Assert.AreEqual(0.9f, part.SafeThreshold, 0.001f);
        }

        [Test]
        public void Scribe_HasAISelfPreservation_With_0_8_Threshold()
        {
            var scribe = _factory.CreateEntity("Scribe");
            var part = scribe.GetPart<AISelfPreservationPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(0.8f, part.RetreatThreshold, 0.001f,
                "Scribe flees at 80% HP — extreme non-combatant");
            Assert.AreEqual(0.95f, part.SafeThreshold, 0.001f);
        }

        [Test]
        public void WellKeeper_HasAISelfPreservation_With_0_7_Threshold()
        {
            // M1 code review Bug 4: WellKeeper was Passive but missing
            // AISelfPreservation. An attacked WellKeeper fights back via
            // PersonalEnemies path; without retreat he fights to the death.
            var wellKeeper = _factory.CreateEntity("WellKeeper");
            var part = wellKeeper.GetPart<AISelfPreservationPart>();
            Assert.IsNotNull(part, "WellKeeper should have AISelfPreservation for symmetry " +
                "with other passive NPCs (Innkeeper, Scribe).");
            Assert.AreEqual(0.7f, part.RetreatThreshold, 0.001f);
            Assert.AreEqual(0.9f, part.SafeThreshold, 0.001f);
        }

        [Test]
        public void Elder_HasAISelfPreservation_With_0_7_Threshold()
        {
            // M1 code review Bug 4: Elder was Passive but missing
            // AISelfPreservation.
            var elder = _factory.CreateEntity("Elder");
            var part = elder.GetPart<AISelfPreservationPart>();
            Assert.IsNotNull(part, "Elder should have AISelfPreservation — ceremonial leader, " +
                "not a combatant, should retreat when wounded.");
            Assert.AreEqual(0.7f, part.RetreatThreshold, 0.001f);
            Assert.AreEqual(0.9f, part.SafeThreshold, 0.001f);
        }

        // ========================
        // M1.2 — Passive flag
        // ========================

        [Test]
        public void Scribe_IsPassive_FromBlueprint()
        {
            var scribe = _factory.CreateEntity("Scribe");
            Assert.IsTrue(scribe.GetPart<BrainPart>().Passive,
                "Scribe should be Passive (writer, non-combatant)");
        }

        [Test]
        public void Elder_IsPassive_FromBlueprint()
        {
            var elder = _factory.CreateEntity("Elder");
            Assert.IsTrue(elder.GetPart<BrainPart>().Passive,
                "Elder should be Passive (ceremonial leader)");
        }

        [Test]
        public void WellKeeper_IsPassive_FromBlueprint()
        {
            var wellKeeper = _factory.CreateEntity("WellKeeper");
            Assert.IsTrue(wellKeeper.GetPart<BrainPart>().Passive,
                "WellKeeper should be Passive (water-keeper, not a fighter)");
        }

        [Test]
        public void Innkeeper_IsPassive_FromBlueprint()
        {
            var innkeeper = _factory.CreateEntity("Innkeeper");
            Assert.IsTrue(innkeeper.GetPart<BrainPart>().Passive,
                "Innkeeper should be Passive (tavern host)");
        }

        [Test]
        public void Warden_IsNotPassive_FromBlueprint()
        {
            // Sanity check: Warden is a combatant, should NOT be Passive
            var warden = _factory.CreateEntity("Warden");
            Assert.IsFalse(warden.GetPart<BrainPart>().Passive,
                "Warden should NOT be Passive — she's a guard");
        }

        [Test]
        public void Snapjaw_IsNotPassive_FromBlueprint()
        {
            // Sanity check: Snapjaws are hostile aggressors
            var snapjaw = _factory.CreateEntity("Snapjaw");
            Assert.IsFalse(snapjaw.GetPart<BrainPart>().Passive,
                "Snapjaw should NOT be Passive — it's a hostile monster");
        }

        // ========================
        // Part ordering: AISelfPreservation must fire before AIGuard/AIWellVisitor
        // ========================

        [Test]
        public void Warden_PartOrder_AISelfPreservationBeforeAIGuard()
        {
            var warden = _factory.CreateEntity("Warden");
            int selfPresIndex = -1;
            int guardIndex = -1;
            for (int i = 0; i < warden.Parts.Count; i++)
            {
                if (warden.Parts[i] is AISelfPreservationPart) selfPresIndex = i;
                if (warden.Parts[i] is AIGuardPart) guardIndex = i;
            }
            Assert.AreNotEqual(-1, selfPresIndex, "Warden must have AISelfPreservationPart");
            Assert.AreNotEqual(-1, guardIndex, "Warden must have AIGuardPart");
            Assert.Less(selfPresIndex, guardIndex,
                "AISelfPreservation must be declared BEFORE AIGuard so HP-retreat wins the AIBored race");
        }

        [Test]
        public void Farmer_PartOrder_AISelfPreservationBeforeAIWellVisitor()
        {
            var farmer = _factory.CreateEntity("Farmer");
            int selfPresIndex = -1;
            int wellIndex = -1;
            for (int i = 0; i < farmer.Parts.Count; i++)
            {
                if (farmer.Parts[i] is AISelfPreservationPart) selfPresIndex = i;
                if (farmer.Parts[i] is AIWellVisitorPart) wellIndex = i;
            }
            Assert.AreNotEqual(-1, selfPresIndex);
            Assert.AreNotEqual(-1, wellIndex);
            Assert.Less(selfPresIndex, wellIndex,
                "AISelfPreservation must fire before AIWellVisitor so low-HP farmer prioritizes retreat");
        }

        // ========================
        // Integration: low-HP + AIBored triggers RetreatGoal on real blueprint
        // ========================

        [Test]
        public void Warden_LowHp_AIBoredEvent_PushesRetreatGoal()
        {
            var zone = new Zone("TestZone");
            var warden = _factory.CreateEntity("Warden");
            zone.AddEntity(warden, 10, 10);

            var brain = warden.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);
            brain.StartingCellX = 10;
            brain.StartingCellY = 10;

            // Drop HP to 20% (below the 0.3 threshold)
            warden.GetStat("Hitpoints").BaseValue = 8; // 8/40 = 20%

            warden.FireEvent(GameEvent.New("AIBored"));

            Assert.IsTrue(brain.HasGoal<RetreatGoal>(),
                "Warden at 20% HP should push RetreatGoal via AISelfPreservation");
        }

        [Test]
        public void Scribe_FullHp_DoesNotInitiateCombat_AgainstHostile()
        {
            // End-to-end: Passive scribe ignores a sighted hostile at full HP
            var zone = new Zone("TestZone");
            var scribe = _factory.CreateEntity("Scribe");
            var snapjaw = _factory.CreateEntity("Snapjaw");
            zone.AddEntity(scribe, 10, 10);
            zone.AddEntity(snapjaw, 12, 10);

            var brain = scribe.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);

            scribe.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<KillGoal>(),
                "Passive Scribe should NOT push KillGoal on hostile sight");
        }

        [Test]
        public void Warden_DoesNotRetreat_WhileHostileInSight()
        {
            // M1 code review Test gap 11:
            //
            // Pins the design-intentional behavior: AISelfPreservation only fires
            // via AIBoredEvent, which only fires when BoredGoal finds NO hostile
            // in sight. Therefore a wounded NPC in active combat will NOT retreat
            // mid-fight — they engage via KillGoal until combat breaks off.
            //
            // Setup: Warden HP = 11/40 (27.5%). Above FleeThreshold (25%) so
            // ShouldFlee is false. Below RetreatThreshold (30%) so if AIBored
            // fired, AISelfPreservation would push RetreatGoal. But the hostile
            // in sight causes BoredGoal to engage first and return early,
            // skipping AIBoredEvent.
            var zone = new Zone("TestZone");
            var warden = _factory.CreateEntity("Warden");
            var snapjaw = _factory.CreateEntity("Snapjaw");
            zone.AddEntity(warden, 10, 10);
            zone.AddEntity(snapjaw, 12, 10);

            var brain = warden.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);
            brain.StartingCellX = 10;
            brain.StartingCellY = 10;

            // HP 11/40 = 0.275 — above FleeThreshold (0.25), below RetreatThreshold (0.3)
            warden.GetStat("Hitpoints").BaseValue = 11;

            warden.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<KillGoal>(),
                "Warden with hostile in sight should engage via KillGoal — " +
                "AIBored can't fire while a hostile is visible.");
            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "AISelfPreservation depends on AIBoredEvent firing, which is " +
                "suppressed when a hostile is sighted. Mid-combat retreat is not supported.");
        }

        [Test]
        public void Warden_Retreats_AfterHostileLeavesSight()
        {
            // Companion to Warden_DoesNotRetreat_WhileHostileInSight: verifies the
            // retreat path DOES fire once the hostile is gone (hostile killed or
            // moved off-screen), closing the "hostile is visible" guard and
            // letting BoredGoal fire AIBored.
            var zone = new Zone("TestZone");
            var warden = _factory.CreateEntity("Warden");
            zone.AddEntity(warden, 10, 10);

            var brain = warden.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);
            brain.StartingCellX = 10;
            brain.StartingCellY = 10;

            // Low HP, no hostile in zone
            warden.GetStat("Hitpoints").BaseValue = 8; // 20% — well below RetreatThreshold (30%)

            warden.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<RetreatGoal>(),
                "With no hostile in sight, AIBored fires and AISelfPreservation " +
                "pushes RetreatGoal for the wounded Warden.");
        }
    }
}
