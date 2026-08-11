using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Terrain that holds a status, and the four spells the player now
    /// starts with.
    ///
    /// <para>The point of both halves is the same: the game's central
    /// idea is that two statuses beat one, and until now that idea was
    /// only reachable if the player had bought two schools AND cast both
    /// themselves. The world could not set anything up, and a starting
    /// character could not combine anything.</para>
    /// </summary>
    public class TerrainStatusSourceTests
    {
        private EntityFactory _factory;

        private static readonly string[] Conductors =
            { "CopperPipe", "BrinePool", "RustedRailing", "IceSheet", "SteamVent", "PeatBog" };
        private static readonly string[] Fuels =
            { "TarSeep", "DryBrush", "PeatBog" };

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
            LiquidRegistry.ResetForTests();
            var liquids = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                liquids.Add(File.ReadAllText(f));
            LiquidRegistry.InitializeFromJsonSources(liquids);

            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [TearDown]
        public void TearDown()
        {
            TileReactionSystem.ResetForTests();
            LiquidRegistry.ResetForTests();
        }

        // ════════════════════════════════════════════════════════
        // Terrain asserts its own status
        // ════════════════════════════════════════════════════════

        [Test]
        public void ABrinePool_MakesItsOwnTileWet_WithoutAnyoneCastingAnything()
        {
            // The whole premise. Before this, ground was only ever wet
            // because the player had just soaked it.
            var zone = new Zone();
            var pool = _factory.CreateEntity("BrinePool");
            Assert.IsNotNull(pool, "BrinePool must exist");
            zone.AddEntity(pool, 10, 10);

            Assert.IsFalse(zone.TileState.HasCoating(10, 10, "water"), "dry to start");

            ZoneTileStateSystem.SeedTerrainSources(zone);

            Assert.IsTrue(zone.TileState.HasCoating(10, 10, "water"),
                "the pool is a puddle the world authored");
        }

        [Test]
        public void ATileWithNoSource_StaysDry()
        {
            // Counter-check: seeding must not wet the whole zone.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("BrinePool"), 10, 10);

            ZoneTileStateSystem.SeedTerrainSources(zone);

            Assert.IsFalse(zone.TileState.HasCoating(11, 10, "water"),
                "only the pool's own cell is wet");
        }

        [Test]
        public void ATarSeep_LaysOilRatherThanWater()
        {
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("TarSeep"), 5, 5);

            ZoneTileStateSystem.SeedTerrainSources(zone);

            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "oil"));
            Assert.IsFalse(zone.TileState.HasCoating(5, 5, "water"));
        }

        [Test]
        public void HotAsh_KeepsItsTileHot_AndFrostVentKeepsItCold()
        {
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("AshBed"), 3, 3);
            zone.AddEntity(_factory.CreateEntity("FrostVent"), 20, 3);

            ZoneTileStateSystem.SeedTerrainSources(zone);

            Assert.Greater(zone.TileState.Heat(3, 3), 0, "ash is hot");
            Assert.Greater(zone.TileState.Cold(20, 3), 0, "the vent is cold");
        }

        [Test]
        public void TerrainReAssertsEachTurn_SoAPoolDoesNotDryOut()
        {
            // Coatings decay. A pool that seeded once would evaporate and
            // stop being a pool, which is the bug this design avoids.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("BrinePool"), 8, 8);

            for (int turn = 0; turn < 12; turn++)
                ZoneTileStateSystem.OnPlayerTurnEnd(zone);

            Assert.IsTrue(zone.TileState.HasCoating(8, 8, "water"),
                "twelve turns later the pool is still a pool");
        }

        // ════════════════════════════════════════════════════════
        // The greenhouse vignette, made mechanical
        // ════════════════════════════════════════════════════════

        [Test]
        public void LightningIntoAWorldAuthoredPuddle_Electrifies_WithoutThePlayerWettingIt()
        {
            // Docs/STATUS-SYSTEM-MODULAR-LADDER.md §5: "Cast lightning
            // into the water… the room explains itself." This is that,
            // reduced to an assertion: the player supplies only the
            // charge; the world supplied the water.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("BrinePool"), 12, 12);

            ZoneTileStateSystem.SeedTerrainSources(zone);
            zone.TileState.AddCharge(12, 12, 2);
            int chargeBefore = zone.TileState.Charge(12, 12);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            // The reaction CONSUMES charge — that is the observable. The
            // original assertion allowed "charge still sitting there",
            // which passes just as well when nothing reacts at all.
            Assert.Less(zone.TileState.Charge(12, 12), chargeBefore,
                "charge met water and nothing consumed it — no reaction fired");
        }

        [Test]
        public void FireOnATarSeep_Ignites_BecauseTheSeepLaidItsOwnOil()
        {
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("TarSeep"), 6, 6);

            ZoneTileStateSystem.SeedTerrainSources(zone);
            Assert.IsTrue(zone.TileState.HasCoating(6, 6, "oil"), "precondition");

            ZoneTileStateSystem.ApplyFireToTile(zone, 6, 6, null, "test");
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.IsFalse(zone.TileState.HasCoating(6, 6, "oil"),
                "the oil burned — terrain fed the reaction, not a spell");
        }

        // ════════════════════════════════════════════════════════
        // Propagation reach — the objects' actual job
        // ════════════════════════════════════════════════════════

        [Test]
        public void EveryConductorBlueprint_ActuallyCarriesCharge()
        {
            // A "conductive" object whose Conductivity sits below the
            // threshold is decoration wearing a costume.
            foreach (string bp in Conductors)
            {
                var e = _factory.CreateEntity(bp);
                Assert.IsNotNull(e, bp);
                var zone = new Zone();
                zone.AddEntity(e, 4, 4);

                Assert.IsTrue(TilePropagationSystem.IsConductive(zone, 4, 4),
                    bp + " is described as a conductor but does not conduct");
            }
        }

        [Test]
        public void EveryFuelBlueprint_ActuallyBurns()
        {
            foreach (string bp in Fuels)
            {
                var e = _factory.CreateEntity(bp);
                Assert.IsNotNull(e, bp);
                var zone = new Zone();
                zone.AddEntity(e, 4, 4);

                Assert.IsTrue(TilePropagationSystem.IsFlammable(zone, 4, 4),
                    bp + " is described as fuel but will not catch");
            }
        }

        [Test]
        public void CopperPipe_IsNotAlsoFuel()
        {
            // Counter-check on the two lists above: if everything were
            // both, neither assertion would mean anything.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("CopperPipe"), 4, 4);

            Assert.IsTrue(TilePropagationSystem.IsConductive(zone, 4, 4));
            Assert.IsFalse(TilePropagationSystem.IsFlammable(zone, 4, 4),
                "metal pipe must not be flammable");
        }

        [Test]
        public void PeatBog_IsTheOneTileThatAnswersToBothSchools()
        {
            // Wet AND flammable — deliberately contradictory, and the
            // most interesting tile in the set.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("PeatBog"), 7, 7);

            Assert.IsTrue(TilePropagationSystem.IsConductive(zone, 7, 7), "conducts");
            Assert.IsTrue(TilePropagationSystem.IsFlammable(zone, 7, 7), "and burns");
        }

        [Test]
        public void ChargeRunsAlongAPipeRun_FurtherThanBareGround()
        {
            // The routing half of the greenhouse: metal carries it past
            // where it would otherwise stop.
            var piped = new Zone();
            for (int x = 5; x <= 12; x++)
                piped.AddEntity(_factory.CreateEntity("CopperPipe"), x, 9);
            piped.TileState.AddCharge(5, 9, 2);
            TilePropagationSystem.PropagateCharge(piped, null);

            var bare = new Zone();
            bare.TileState.AddCharge(5, 9, 2);
            TilePropagationSystem.PropagateCharge(bare, null);

            int pipedReach = 0, bareReach = 0;
            for (int x = 5; x <= 12; x++)
            {
                if (piped.TileState.Charge(x, 9) > 0) pipedReach++;
                if (bare.TileState.Charge(x, 9) > 0) bareReach++;
            }

            Assert.Greater(pipedReach, bareReach,
                "the pipe run carries charge further than bare ground");
        }

        // ════════════════════════════════════════════════════════
        // Starting spells
        // ════════════════════════════════════════════════════════

        private static Entity Caster()
        {
            var e = new Entity { ID = "player", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.AddPart(new SkillsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            return e;
        }

        [Test]
        public void ThePlayerStartsWithOneSpellFromEachElement()
        {
            var player = Caster();

            int granted = StartingSpellKit.GrantAll(player);

            Assert.AreEqual(StartingSpellKit.SpellClasses.Length, granted);
            var skills = player.GetPart<SkillsPart>();
            foreach (string cls in StartingSpellKit.SpellClasses)
                Assert.IsTrue(skills.HasSkill(cls), cls + " was not granted");
        }

        [Test]
        public void TheStartingSetCoversFireWaterElectricAndCold()
        {
            // Naming the elements out loud: a set of four fire spells
            // would satisfy the count and defeat the entire purpose.
            string joined = string.Join(",", StartingSpellKit.SpellClasses);
            StringAssert.Contains("Pyromancy", joined, "fire");
            StringAssert.Contains("Hydromancy", joined, "water");
            StringAssert.Contains("Galvanism", joined, "electric");
            StringAssert.Contains("Cryomancy", joined, "cold");
        }

        [Test]
        public void EveryStartingSpell_ResolvesToARealCastableSkill()
        {
            var player = Caster();
            StartingSpellKit.GrantAll(player);
            var skills = player.GetPart<SkillsPart>();

            foreach (string cls in StartingSpellKit.SpellClasses)
            {
                var skill = skills.GetSkill(cls);
                Assert.IsNotNull(skill, cls + " did not resolve to a skill instance");
                Assert.IsNotNull(skill.DeclareActivatedAbility(player),
                    cls + " is not an activated ability — the player could never cast it");
            }
        }

        [Test]
        public void GrantingTwice_DoesNotDuplicate()
        {
            // Bootstrap can run this on a loaded save.
            var player = Caster();
            StartingSpellKit.GrantAll(player);

            int second = StartingSpellKit.GrantAll(player);

            Assert.AreEqual(0, second, "re-granting is a no-op, not a duplicate");
            Assert.IsEmpty(StartingSpellKit.MissingFrom(player));
        }

        [Test]
        public void AFreshCharacter_IsMissingAllFour()
        {
            // Counter-check for MissingFrom: if it always returned empty
            // the test above would pass for the wrong reason.
            Assert.AreEqual(StartingSpellKit.SpellClasses.Length,
                StartingSpellKit.MissingFrom(Caster()).Count);
        }

        [Test]
        public void TheStartingSetIsPrimersNotFinishers()
        {
            // These should be the cheap entry actives. If a future edit
            // swaps in Flame Jet or Pyroclasm, the early game stops
            // teaching and starts handing over the payoff.
            var player = Caster();
            StartingSpellKit.GrantAll(player);
            var skills = player.GetPart<SkillsPart>();

            foreach (string cls in StartingSpellKit.SpellClasses)
            {
                var spec = skills.GetSkill(cls).DeclareActivatedAbility(player);
                Assert.LessOrEqual(spec.Cooldown, 35,
                    cls + " has a " + spec.Cooldown + "-turn cooldown — that is a "
                    + "finisher, not a starting primer");
            }
        }
    }
}
