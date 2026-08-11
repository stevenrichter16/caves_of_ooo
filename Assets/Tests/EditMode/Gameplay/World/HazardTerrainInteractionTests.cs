using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The non-conductive hazard terrain — fire, cold, vapour — and what
    /// it does to the creatures standing in it.
    ///
    /// <para>Companion to <see cref="ElectrifiedConductorTests"/>, which
    /// covers the electrical half after two bugs were reported there. The
    /// question this file asks of every remaining object is the same one
    /// that caught those: <b>does it actually do the thing its own
    /// description claims?</b> A tile that says "breathes cold" and
    /// cannot chill anything is the same defect as a live pipe that
    /// shocks nobody.</para>
    ///
    /// <para>Each object is tested for: what it asserts onto its own
    /// tile, what it does to a NEIGHBOURING tile, and what a creature
    /// standing on it experiences. The third is the one that was missing
    /// everywhere.</para>
    /// </summary>
    public class HazardTerrainInteractionTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
            LiquidRegistry.ResetForTests();
            var liquids = new List<string>();
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

        private static Entity Creature(Zone zone, string name, int x, int y, int hp = 300)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            foreach (var r in new[] { "HeatResistance", "ColdResistance", "ElectricResistance", "AcidResistance" })
                e.Statistics[r] = new Stat { Owner = e, Name = r, BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new Body());
            zone.AddEntity(e, x, y);
            return e;
        }

        private Entity Place(Zone zone, string bp, int x, int y)
        {
            var e = _factory.CreateEntity(bp);
            Assert.IsNotNull(e, bp + " blueprint must exist");
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════
        // FIRE — tar, brush, and what burns a creature
        // ════════════════════════════════════════════════════════

        [Test]
        public void FireOnATarSeep_BurnsTheCreatureStandingInIt()
        {
            // ignite_oil_heat carries OccupantDamage 8. The seep lays its
            // own oil, so no spell is needed to prepare the ground.
            var zone = new Zone();
            Place(zone, "TarSeep", 8, 8);
            var victim = Creature(zone, "snapjaw", 8, 8);
            int hp = victim.GetStatValue("Hitpoints");

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.ApplyFireToTile(zone, 8, 8, null, "test");
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.Less(victim.GetStatValue("Hitpoints"), hp,
                "a creature standing in burning tar took nothing");
        }

        [Test]
        public void AnUnlitTarSeep_IsSafeToStandIn()
        {
            // Counter-check: the seep is a hazard only once something
            // lights it. Otherwise it would be an instant-death tile.
            var zone = new Zone();
            Place(zone, "TarSeep", 8, 8);
            var walker = Creature(zone, "walker", 8, 8);
            int hp = walker.GetStatValue("Hitpoints");

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.AreEqual(hp, walker.GetStatValue("Hitpoints"),
                "unlit tar hurt someone");
        }

        [Test]
        public void BurningOil_LeavesEmbers_WhichSpreadToTheNextOiledTile()
        {
            // A seep is a patch, not a cell. Lighting one end should run.
            var zone = new Zone();
            for (int x = 5; x <= 8; x++) Place(zone, "TarSeep", x, 5);

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.ApplyFireToTile(zone, 5, 5, null, "test");
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            bool spread = false;
            for (int x = 6; x <= 8; x++)
                if (zone.TileState.HasResidue(x, 5, "embers")
                    || !zone.TileState.HasCoating(x, 5, "oil")) { spread = true; break; }

            Assert.IsTrue(spread, "fire did not travel along a connected oil patch");
        }

        [Test]
        public void DryBrush_IsFlammableTerrain()
        {
            var zone = new Zone();
            Place(zone, "DryBrush", 4, 4);
            Assert.IsTrue(TilePropagationSystem.IsFlammable(zone, 4, 4),
                "dry brush must be fuel");
        }

        // ════════════════════════════════════════════════════════
        // COLD — the frost vent's own promise
        // ════════════════════════════════════════════════════════

        [Test]
        public void AFrostVent_FreezesTheWaterNextToIt()
        {
            // The blueprint's description: "Breathes cold. Standing water
            // near it will not stay liquid." A vent that cannot chill the
            // puddle beside it is scenery telling a lie.
            var zone = new Zone();
            Place(zone, "FrostVent", 10, 10);
            Place(zone, "BrinePool", 11, 10);

            for (int turn = 0; turn < 3; turn++)
                ZoneTileStateSystem.OnPlayerTurnEnd(zone);

            Assert.IsTrue(zone.TileState.HasCoating(11, 10, "ice")
                          || zone.TileState.Cold(11, 10) > 0,
                "the pool beside a frost vent never got cold");
        }

        [Test]
        public void AFrostVent_DoesNotChillTheWholeZone()
        {
            // Counter-check: "breathes cold" is a neighbour, not a
            // weather system.
            var zone = new Zone();
            Place(zone, "FrostVent", 10, 10);

            for (int turn = 0; turn < 3; turn++)
                ZoneTileStateSystem.OnPlayerTurnEnd(zone);

            Assert.AreEqual(0, zone.TileState.Cold(20, 20),
                "a frost vent chilled a tile ten cells away");
        }

        [Test]
        public void ACreatureOnFrozenWater_IsFrozen()
        {
            // freeze_water carries OccupantEffect Frozen. Reaching it
            // from terrain is the point.
            var zone = new Zone();
            Place(zone, "BrinePool", 6, 6);
            var victim = Creature(zone, "victim", 6, 6);

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.AddCold(zone, 6, 6, 2);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.IsTrue(victim.HasEffect<FrozenEffect>(),
                "standing in freezing water left no Frozen status");
        }

        // ════════════════════════════════════════════════════════
        // VAPOUR — ash, steam
        // ════════════════════════════════════════════════════════

        [Test]
        public void AnAshBed_SteamsTheWaterNextToIt()
        {
            // The blueprint's description: "Still hot. Water poured on it
            // comes back up as steam."
            var zone = new Zone();
            Place(zone, "AshBed", 12, 12);
            Place(zone, "BrinePool", 13, 12);

            for (int turn = 0; turn < 3; turn++)
                ZoneTileStateSystem.OnPlayerTurnEnd(zone);

            Assert.IsTrue(zone.TileState.Heat(13, 12) > 0
                          || !string.IsNullOrEmpty(zone.TileState.Cloud(13, 12)),
                "the pool beside a bed of hot ash never felt the heat");
        }

        [Test]
        public void ASteamVent_ProducesSteam_FromItsOwnWaterAndHeat()
        {
            // The one object that reacts entirely on its own.
            var zone = new Zone();
            Place(zone, "SteamVent", 9, 9);

            ZoneTileStateSystem.OnPlayerTurnEnd(zone);

            Assert.AreEqual("steam", zone.TileState.Cloud(9, 9),
                "a steam vent produced no steam");
        }

        // ════════════════════════════════════════════════════════
        // WHO gets affected — creatures only
        // ════════════════════════════════════════════════════════

        [Test]
        public void HazardsAffectCreatures_ButNotTheTerrainItself()
        {
            // The tar seep must not burn itself away the instant it is
            // lit, or a patch would vanish before the fire could spread.
            var zone = new Zone();
            var seep = Place(zone, "TarSeep", 7, 7);

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.ApplyFireToTile(zone, 7, 7, null, "test");
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.IsFalse(seep.HasEffect<BurningEffect>(),
                "the terrain object itself took a creature-only effect");
        }

        [Test]
        public void ADeadCreature_IsNotFurtherAfflicted()
        {
            // ApplyToOccupants gates the effect on HP > 0. A corpse
            // acquiring a status reads as a bug.
            var zone = new Zone();
            Place(zone, "TarSeep", 3, 3);
            var doomed = Creature(zone, "doomed", 3, 3, hp: 1);

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.ApplyFireToTile(zone, 3, 3, null, "test");
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            if (doomed.GetStatValue("Hitpoints") <= 0)
                Assert.IsFalse(doomed.HasEffect<BurningEffect>(),
                    "a corpse caught fire");
        }

        [Test]
        public void ARealNpcBlueprint_IsAffectedJustLikeATestFixture()
        {
            // Every other test here builds its own creature. If the real
            // bestiary lacked whatever ApplyToOccupants keys on, none of
            // this would reach an actual enemy.
            var zone = new Zone();
            Place(zone, "TarSeep", 15, 15);

            Entity npc = null;
            foreach (var bp in new[] { "Snapjaw", "SootGremlin", "Rotling", "DuneLurker" })
            {
                npc = _factory.CreateEntity(bp);
                if (npc != null) { zone.AddEntity(npc, 15, 15); break; }
            }
            Assert.IsNotNull(npc, "no bestiary blueprint resolved");
            Assert.IsTrue(npc.HasTag("Creature"),
                "a bestiary NPC is not tagged Creature — tile hazards key on that tag "
                + "and would skip every real enemy in the game");

            int hp = npc.GetStatValue("Hitpoints", 0);
            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.ApplyFireToTile(zone, 15, 15, null, "test");
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            if (hp > 0)
                Assert.Less(npc.GetStatValue("Hitpoints", 0), hp,
                    "a real NPC standing in burning tar took nothing");
        }

        // ════════════════════════════════════════════════════════
        // STATUS INTERACTIONS — the grammar the whole system is for
        // ════════════════════════════════════════════════════════

        [Test]
        public void AWetCreature_OnBurningTar_StillBurns_ButTheGroundIsTheSource()
        {
            // Moisture protects against IGNITION (PyroIgnition), not
            // against standing in a fire. The tile's damage is direct.
            var zone = new Zone();
            Place(zone, "TarSeep", 2, 9);
            var soaked = Creature(zone, "soaked", 2, 9);
            soaked.ApplyEffect(new WetEffect(1.0f), null, zone);
            int hp = soaked.GetStatValue("Hitpoints");

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.ApplyFireToTile(zone, 2, 9, null, "test");
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.Less(soaked.GetStatValue("Hitpoints"), hp,
                "being wet made a creature immune to standing in fire");
        }

        [Test]
        public void ColdTerrain_AndHeatTerrain_CancelRatherThanBothApplying()
        {
            // ZoneTileState cancels opposed energy. A tile that is both
            // hot and cold is incoherent, and a creature should not be
            // burned and frozen on the same turn.
            var zone = new Zone();
            zone.TileState.AddHeat(5, 5, 2);
            zone.TileState.AddCold(5, 5, 2);
            zone.TileState.CancelOpposedEnergy(5, 5);

            Assert.IsTrue(zone.TileState.Heat(5, 5) == 0 || zone.TileState.Cold(5, 5) == 0,
                "a tile is carrying heat and cold at the same time");
        }

        [Test]
        public void TerrainDamage_DoesNotEscalateAcrossTurns()
        {
            // The lesson from the electrical bug, applied to fire: a
            // hazard that ramps would quietly become a death trap.
            var zone = new Zone();
            Place(zone, "TarSeep", 6, 12);
            var victim = Creature(zone, "victim", 6, 12, hp: 100000);

            int first = 0, last = 0;
            for (int turn = 0; turn < 8; turn++)
            {
                int before = victim.GetStatValue("Hitpoints");
                ZoneTileStateSystem.ApplyFireToTile(zone, 6, 12, null, "test");
                ZoneTileStateSystem.OnPlayerTurnEnd(zone);
                int loss = before - victim.GetStatValue("Hitpoints");
                if (turn == 0) first = loss;
                last = loss;
            }

            Assert.LessOrEqual(last, first + 2,
                "burning-tar damage climbed from " + first + " to " + last);
        }

        [Test]
        public void EveryHazardBlueprint_LeavesAnUnoccupiedTileHarmless()
        {
            // Sweep: no hazard may damage a creature that is merely
            // NEARBY. Everything here is a tile you must stand in.
            string[] hazards =
            {
                "TarSeep", "DryBrush", "AshBed", "FrostVent",
                "IceSheet", "SteamVent", "BrinePool", "PeatBog",
            };

            foreach (string bp in hazards)
            {
                var zone = new Zone();
                Place(zone, bp, 10, 10);
                var bystander = Creature(zone, "bystander", 14, 14);
                int hp = bystander.GetStatValue("Hitpoints");

                for (int turn = 0; turn < 5; turn++)
                    ZoneTileStateSystem.OnPlayerTurnEnd(zone);

                Assert.AreEqual(hp, bystander.GetStatValue("Hitpoints"),
                    bp + " hurt someone standing four tiles away");
            }
        }

        [Test]
        public void EveryHazardBlueprint_SurvivesManyTurnsWithoutRunawayState()
        {
            // The generalised form of the Infinity bug: no hazard may
            // grow its own tile state without bound.
            string[] hazards =
            {
                "TarSeep", "AshBed", "FrostVent", "IceSheet",
                "SteamVent", "BrinePool", "PeatBog", "OilSlick",
            };

            foreach (string bp in hazards)
            {
                var zone = new Zone();
                Place(zone, bp, 9, 9);

                for (int turn = 0; turn < 30; turn++)
                    ZoneTileStateSystem.OnPlayerTurnEnd(zone);

                Assert.LessOrEqual(zone.TileState.Heat(9, 9), ZoneTileState.MaxEnergy,
                    bp + " heat exceeded the cap");
                Assert.LessOrEqual(zone.TileState.Cold(9, 9), ZoneTileState.MaxEnergy,
                    bp + " cold exceeded the cap");
                Assert.LessOrEqual(zone.TileState.Charge(9, 9), ZoneTileState.MaxEnergy,
                    bp + " charge exceeded the cap");
            }
        }
    }
}
