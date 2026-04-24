using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M6.3 — AILayRunePart (AIBoredEvent handler that pushes
    /// <see cref="LayRuneGoal"/>). Covers the probability gate, the
    /// stack-cleanliness gate (no double-push), the per-zone quota cap
    /// (MaxRunesPerZone), the null-zone / no-target graceful paths, and
    /// the blueprint-list random-pick.
    ///
    /// Mirrors Qud's <c>XRL.World.Parts.Miner</c> BeginTakeActionEvent
    /// tests (Miner.cs:95-128).
    /// </summary>
    [TestFixture]
    public class AILayRunePartTests
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
                        { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""test rune"" }]},
                        { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""false"" }]},
                        { ""Name"": ""RuneFlameTrigger"", ""Params"": [{ ""Key"": ""Damage"", ""Value"": ""5"" }]}
                    ],
                    ""Tags"": [{ ""Key"": ""Rune"", ""Value"": """" }]
                }
            ]
        }";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprintsJson);
            LayRuneGoal.Factory = _factory;
        }

        [TearDown]
        public void Teardown()
        {
            LayRuneGoal.Factory = null;
            // Reset faction state — RuneCultist_Faction_IsRegistered_AndHostileToPlayer
            // overrides static FactionManager state; subsequent fixtures expect
            // the default hardcoded init (Snapjaws/Villagers only).
            FactionManager.Initialize();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity CreateCultist(
            Zone zone, int x, int y,
            int chance = 100,
            int maxRunes = 5,
            int searchRadius = 4,
            string runeBlueprints = "TestRune",
            int seed = 42)
        {
            var e = new Entity { BlueprintName = "Cultist", ID = "cultist-1" };
            e.AddPart(new RenderPart { DisplayName = "cultist" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.SetTag("Faction", "Cultists");
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(seed) };
            e.AddPart(brain);
            e.AddPart(new AILayRunePart
            {
                Chance = chance,
                MaxRunesPerZone = maxRunes,
                SearchRadius = searchRadius,
                RuneBlueprints = runeBlueprints
            });
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>
        /// Fire AIBoredEvent.Check on the cultist the same way BoredGoal would.
        /// Returns the "unhandled" result (true = proceed with default idle,
        /// false = a behavior part consumed the event).
        /// </summary>
        private bool FireBored(Entity cultist)
        {
            return AIBoredEvent.Check(cultist);
        }

        // ====================================================================
        // Happy path
        // ====================================================================

        [Test]
        public void AILayRune_PushesLayRuneGoal_OnBoredTick()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsFalse(unhandled,
                "Chance=100 + empty stack + zero existing runes must consume the AIBored event.");
            Assert.IsTrue(brain.HasGoal<LayRuneGoal>(),
                "AILayRune must push LayRuneGoal on a successful bored tick.");
        }

        [Test]
        public void AILayRune_LayRuneGoal_TargetsPassableCellWithinRadius()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100, searchRadius: 3);
            var brain = cultist.GetPart<BrainPart>();

            FireBored(cultist);

            var goal = (LayRuneGoal)brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.IsNotNull(goal);
            int dx = System.Math.Abs(goal.TargetX - 10);
            int dy = System.Math.Abs(goal.TargetY - 10);
            Assert.LessOrEqual(System.Math.Max(dx, dy), 3,
                "Target cell must be within SearchRadius Chebyshev distance of the NPC.");
            Assert.IsFalse(dx == 0 && dy == 0,
                "Target must not be the NPC's own cell.");
        }

        // ====================================================================
        // Gates
        // ====================================================================

        [Test]
        public void AILayRune_ProbabilityGate_SkipsWhenUnlucky()
        {
            var zone = new Zone("TestZone");
            // Chance=0 means Rng.Next(100) >= 0 is always true → skip.
            var cultist = CreateCultist(zone, 10, 10, chance: 0);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "Chance=0 must not consume the AIBored event — other idle behaviors should still run.");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "No goal should be pushed when the probability gate fails.");
        }

        [Test]
        public void AILayRune_DoesNotDoublePush_WhenLayRuneGoalAlreadyOnStack()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100);
            var brain = cultist.GetPart<BrainPart>();

            // Pre-push a LayRuneGoal directly.
            brain.PushGoal(new LayRuneGoal(5, 5, "TestRune"));
            int initialCount = brain.GoalCount;

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "When a LayRuneGoal is already on the stack, AILayRune must let other behaviors try (don't consume).");
            Assert.AreEqual(initialCount, brain.GoalCount,
                "No second LayRuneGoal should be pushed while one is already active.");
        }

        [Test]
        public void AILayRune_ZoneQuota_SkipsWhenCapReached()
        {
            var zone = new Zone("TestZone");

            // Pre-populate with MaxRunesPerZone runes.
            for (int i = 0; i < 3; i++)
            {
                var rune = _factory.CreateEntity("TestRune");
                Assert.IsNotNull(rune);
                zone.AddEntity(rune, 2 + i, 2);
            }

            var cultist = CreateCultist(zone, 10, 10, chance: 100, maxRunes: 3);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "Once MaxRunesPerZone is hit, AILayRune must step aside so other idle behaviors run.");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "No goal should be pushed when the zone quota is saturated.");
        }

        [Test]
        public void AILayRune_NoTargetCell_GracefullyDoesNothing()
        {
            var zone = new Zone("TestZone");
            // Place cultist in a corner with radius 0 — no neighbors → no candidates.
            var cultist = CreateCultist(zone, 0, 0, chance: 100, searchRadius: 0);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "When no passable target cell is found, the part must let other behaviors run.");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "No goal should be pushed when no target is found.");
        }

        [Test]
        public void AILayRune_IsSteppable_RejectsCellsWithPhysicsSolidButNoSolidTag()
        {
            // M6 post-impl audit finding: AILayRune must use the stricter
            // IsSteppable predicate (PhysicsPart.Solid OR "Solid" tag) — NOT
            // Cell.IsPassable() alone, which only checks the tag. A chair /
            // CompassStone / chest has PhysicsPart.Solid=true but no "Solid"
            // tag, and would be selected as a target under the old rule,
            // burning LayRuneGoal's retry budget on unreachable cells.
            //
            // Regression: fill every radius-1 cell except (11,10) with a
            // PhysicsPart.Solid-but-no-tag entity. If IsSteppable is wired,
            // (11,10) is the ONLY valid target. If the old IsPassable rule
            // leaks back in, random selection would sometimes pick one of
            // the solid cells and this test would flake.
            var zone = new Zone("TestZone");

            // Place Physics-solid (but no "Solid" tag) blockers on 7 of the
            // 8 radius-1 cells around (10,10).
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (dx == 1 && dy == 0) continue; // leave (11,10) open
                    var blocker = new Entity { BlueprintName = "Chair", ID = $"chair-{dx}-{dy}" };
                    blocker.AddPart(new RenderPart { DisplayName = "chair" });
                    blocker.AddPart(new PhysicsPart { Solid = true });
                    // Intentionally NO SetTag("Solid") — this is the whole
                    // point of the IsSteppable-vs-IsPassable distinction.
                    zone.AddEntity(blocker, 10 + dx, 10 + dy);
                }
            }

            var cultist = CreateCultist(zone, 10, 10, chance: 100, searchRadius: 1);
            var brain = cultist.GetPart<BrainPart>();

            FireBored(cultist);

            var goal = (LayRuneGoal)brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.IsNotNull(goal, "A goal should still be pushed — (11,10) remains a valid target.");
            Assert.AreEqual(11, goal.TargetX,
                "IsSteppable must reject PhysicsPart.Solid cells — only (11,10) is reachable.");
            Assert.AreEqual(10, goal.TargetY);
        }

        // ====================================================================
        // Rune blueprint selection
        // ====================================================================

        [Test]
        public void AILayRune_PicksBlueprintFromList()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100,
                runeBlueprints: "TestRune");
            var brain = cultist.GetPart<BrainPart>();

            FireBored(cultist);

            var goal = (LayRuneGoal)brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.IsNotNull(goal);
            Assert.AreEqual("TestRune", goal.RuneBlueprint,
                "LayRuneGoal's blueprint must be one of the configured RuneBlueprints.");
        }

        [Test]
        public void AILayRune_EmptyBlueprintList_DoesNothing()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100,
                runeBlueprints: "");
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "Empty RuneBlueprints must not consume the event (graceful unconfigured path).");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>());
        }

        [Test]
        public void AILayRune_BlueprintListParses_CommaAndWhitespace()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100,
                runeBlueprints: " TestRune , TestRune ");
            var brain = cultist.GetPart<BrainPart>();

            FireBored(cultist);

            var goal = (LayRuneGoal)brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.IsNotNull(goal);
            Assert.AreEqual("TestRune", goal.RuneBlueprint,
                "Whitespace around comma-separated entries must be trimmed.");
        }

        // ====================================================================
        // Null-safety
        // ====================================================================

        [Test]
        public void AILayRune_NoBrain_DoesNothing()
        {
            var e = new Entity { BlueprintName = "Orphan" };
            e.AddPart(new AILayRunePart { Chance = 100 });
            // Deliberately no BrainPart.

            bool unhandled = AIBoredEvent.Check(e);

            Assert.IsTrue(unhandled,
                "AILayRune must be a safe no-op when the entity has no BrainPart.");
        }

        // ====================================================================
        // CR-02 regression — RuneCultist faction is registered & hostile
        // to the player (M6 review: "Cultists" missing from Factions.json
        // made the ambush scenario neutral).
        // ====================================================================

        // ====================================================================
        // P-03 regression — candidate scratch list doesn't leak between calls
        // ====================================================================

        [Test]
        public void AILayRune_CandidateScratch_DoesNotLeakBetweenCalls()
        {
            // PickRunePlacementCell now reuses a static List<(int,int)>.
            // Two cultists run HandleBored back-to-back; the second's
            // target must come from a scan centered on ITS position, not
            // contaminated by the first's candidate set.
            var zone = new Zone("TestZone");
            var cultistA = CreateCultist(zone, 3, 3, chance: 100, searchRadius: 2, seed: 1);
            var cultistB = CreateCultist(zone, 20, 20, chance: 100, searchRadius: 2, seed: 1);
            // Both have ID "cultist-1" from CreateCultist; distinguish by
            // giving cultistB a unique one so zone-add doesn't collide.
            cultistB.ID = "cultist-2";

            var brainA = cultistA.GetPart<BrainPart>();
            var brainB = cultistB.GetPart<BrainPart>();

            FireBored(cultistA);
            var goalA = (LayRuneGoal)brainA.PeekGoalAt(brainA.GoalCount - 1);
            Assert.IsNotNull(goalA);
            // Target must be within radius-2 of cultistA at (3,3).
            Assert.LessOrEqual(System.Math.Abs(goalA.TargetX - 3), 2);
            Assert.LessOrEqual(System.Math.Abs(goalA.TargetY - 3), 2);

            FireBored(cultistB);
            var goalB = (LayRuneGoal)brainB.PeekGoalAt(brainB.GoalCount - 1);
            Assert.IsNotNull(goalB);
            // If scratch leaked, cultistB's target would be near (3,3)
            // (cultistA's leftovers). It must be near (20,20) — ITS own position.
            Assert.LessOrEqual(System.Math.Abs(goalB.TargetX - 20), 2,
                "P-03 regression: scratch list contamination would make cultistB's target near cultistA's position.");
            Assert.LessOrEqual(System.Math.Abs(goalB.TargetY - 20), 2);
        }

        [Test]
        public void RuneCultist_Faction_IsRegistered_AndHostileToPlayer()
        {
            // Load real Factions.json + real Objects.json end-to-end. If
            // "Cultists" is absent from the faction registry, IsHostile
            // will return false and the regression re-surfaces.
            var factionAsset = UnityEngine.Resources
                .Load<UnityEngine.TextAsset>("Content/Data/Factions");
            if (factionAsset == null)
            {
                Assert.Ignore("Factions.json not loadable via Resources (editor path mismatch).");
                return;
            }
            FactionManager.Initialize(factionAsset.text);

            var blueprintAsset = UnityEngine.Resources
                .Load<UnityEngine.TextAsset>("Content/Blueprints/Objects");
            if (blueprintAsset == null)
            {
                Assert.Ignore("Objects.json not loadable via Resources.");
                return;
            }
            var realFactory = new EntityFactory();
            realFactory.LoadBlueprints(blueprintAsset.text);

            var cultist = realFactory.CreateEntity("RuneCultist");
            Assert.IsNotNull(cultist, "RuneCultist blueprint must exist.");
            Assert.AreEqual("Cultists", FactionManager.GetFaction(cultist),
                "RuneCultist must carry the Cultists faction tag (pins the Objects.json blueprint).");

            // Player stub with the Player tag (FactionManager.GetFaction
            // returns 'Player' for any entity carrying the Player tag).
            var player = new Entity { BlueprintName = "Player", ID = "p-1" };
            player.SetTag("Player");
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };

            Assert.IsTrue(FactionManager.IsHostile(cultist, player),
                "Cultists must be hostile to the player so RuneCultistAmbush actually ambushes. " +
                "CR-02 regression — if Factions.json lacks a 'Cultists' entry with negative " +
                "InitialPlayerReputation, IsHostile falls through to 0 (neutral).");
        }
    }
}
