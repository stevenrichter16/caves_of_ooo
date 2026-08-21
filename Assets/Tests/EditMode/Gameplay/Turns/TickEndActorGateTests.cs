using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Wx optimization review §0 (Docs/FELLING-WX-OPTIMIZATION-REVIEW.md)
    /// — the TickEnd per-actor seam.
    ///
    /// <para><c>TurnManager.EndTurn</c> fires TickEnd once per ACTOR.
    /// <c>ZoneTileStateSystem.cs:25-30</c> documents the trap ("duration
    /// would become a function of local population") and routes through
    /// the player-turn seam instead — but <c>GasSystemPart</c> and its
    /// byte-for-byte mirror <c>CropSystemPart</c> consumed raw TickEnd:
    /// gas decayed, spread, and DOSED per actor-turn, and crops grew per
    /// actor-turn. In a zone with A actors, standing in poison gas dealt
    /// A× the authored per-turn dose.</para>
    ///
    /// <para>The fix: TurnManager stamps the ending actor onto the
    /// TickEnd event; the gas/crop forwarder Parts only forward when the
    /// stamped actor is the player (or when no actor is stamped — the
    /// compatibility contract for benches and tests that fire bare
    /// TickEnd events, e.g. NarrativeReactorTests). The player's EndTurn
    /// fires exactly once per round — including blocked/stunned rounds,
    /// where TurnManager's playerAlreadyBlocked path still calls
    /// EndTurn(player) — so per-round semantics hold even while the
    /// player is held stunned inside a cloud (the case the
    /// InputHandler-seam alternative would have frozen).</para>
    /// </summary>
    public class TickEndActorGateTests
    {
        private TurnManager _turnManager;
        private Entity _player;
        private Entity _npc;

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""DisplayName"":""poison vapor"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" } ] }");
            GasPoisonPart.TestRng = new System.Random(42);

            var world = new Entity { BlueprintName = "World" };
            world.AddPart(new GasSystemPart());
            world.AddPart(new CropSystemPart());
            TurnManager.World = world;

            _turnManager = new TurnManager();
            _player = new Entity { ID = "player", BlueprintName = "Player" };
            _player.Tags["Player"] = "";
            _npc = new Entity { ID = "npc", BlueprintName = "Bystander" };
            _npc.Tags["Creature"] = "";
            _turnManager.AddEntity(_player);
            _turnManager.AddEntity(_npc);
        }

        [TearDown]
        public void TearDown()
        {
            Diag.SetChannel("gas-verbose", false);
            TurnManager.World = null;
            GasRegistry.ResetForTests();
            GasPoisonPart.TestRng = null;
            SettlementRuntime.Reset();
        }

        private Zone MakeActiveZoneWithGas()
        {
            var zone = new Zone("TickGate");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            SettlementRuntime.ActiveZone = zone;
            Diag.ResetAll(); // drop the spawn-time records; count tick-driven only
            // Wx §1b: Dispersed rides the off-by-default gas-verbose
            // channel — enable it (AFTER ResetAll, which restores
            // channel defaults) so the tick counter sees it. Disabled
            // again in TearDown.
            Diag.SetChannel("gas-verbose", true);
            return zone;
        }

        private static int DispersedCount() => DiagQuery.Apply(new DiagQuery.Filter
        { Category = "gas-verbose", Kind = "Dispersed", Limit = 50 }).Records.Count;

        private static Entity MakeCrop(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "crop", BlueprintName = "TestCrop" };
            e.Tags["Crop"] = "";
            e.AddPart(new RenderPart { DisplayName = "crop" });
            e.AddPart(new CropPart { MoistureTicks = 5, TicksInStage = 0, TicksPerStage = 20 });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeVictim(Zone zone, int x, int y)
        {
            // Stat block mirrors GasPoisonPartTests.MakeCreature so the
            // filter chain computes the same intake (=100 → dose 5).
            var e = new Entity { ID = "victim_" + x, BlueprintName = "Victim" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", 200, 200); S("Toughness", 12);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            S("AcidResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "victim" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   Gas — the dispersal pass runs per ROUND, not per actor
        // ════════════════════════════════════════════════════════════

        [Test]
        public void NpcTickEnd_DoesNotDriveGasDispersal()
        {
            MakeActiveZoneWithGas();
            _turnManager.EndTurn(_npc);
            Assert.AreEqual(0, DispersedCount(),
                "an NPC's turn ending must not advance gas — dispersal per " +
                "actor makes cloud lifetime a function of zone population");
        }

        [Test]
        public void PlayerTickEnd_DrivesGasDispersal()
        {
            // Counter-check: the same setup with the flag flipped — the
            // player's turn ending IS the round boundary and must tick gas.
            MakeActiveZoneWithGas();
            _turnManager.EndTurn(_player);
            Assert.AreEqual(1, DispersedCount(),
                "the player's turn end is the round boundary; gas ticks once");
        }

        [Test]
        public void BareTickEnd_WithoutActorStamp_StillDrivesGas()
        {
            // Compatibility pin: benches and tests fire TickEnd directly
            // with no Actor parameter (NarrativeReactorTests.cs:120). A
            // bare TickEnd means "a round passed", not "an NPC acted".
            MakeActiveZoneWithGas();
            var e = GameEvent.New("TickEnd");
            TurnManager.World.FireEvent(e);
            e.Release();
            Assert.AreEqual(1, DispersedCount(),
                "an unstamped TickEnd keeps the old contract for direct drivers");
        }

        // ════════════════════════════════════════════════════════════
        //   Crops — growth runs per ROUND, not per actor
        // ════════════════════════════════════════════════════════════

        [Test]
        public void NpcTickEnd_DoesNotAdvanceCrops()
        {
            var zone = new Zone("CropGate");
            var crop = MakeCrop(zone, 3, 3);
            SettlementRuntime.ActiveZone = zone;

            _turnManager.EndTurn(_npc);

            Assert.AreEqual(0, crop.GetPart<CropPart>().TicksInStage,
                "crops must not grow faster in populated zones");
            Assert.AreEqual(5, crop.GetPart<CropPart>().MoistureTicks,
                "nor dry out faster");
        }

        [Test]
        public void PlayerTickEnd_AdvancesCrops()
        {
            // Counter-check for the crop gate.
            var zone = new Zone("CropGate2");
            var crop = MakeCrop(zone, 3, 3);
            SettlementRuntime.ActiveZone = zone;

            _turnManager.EndTurn(_player);

            Assert.AreEqual(1, crop.GetPart<CropPart>().TicksInStage,
                "one round, one growth tick");
            Assert.AreEqual(4, crop.GetPart<CropPart>().MoistureTicks);
        }

        // ════════════════════════════════════════════════════════════
        //   The user-visible invariant that motivated the fix
        // ════════════════════════════════════════════════════════════

        [Test]
        public void PoisonDose_IsIndependentOfZonePopulation()
        {
            // Hypothesis (review §0): a creature standing in poison gas
            // beside N bystanders took (N+1)× the authored immediate dose
            // per round, because every actor's EndTurn re-ran the gas
            // apply pass. One round = one dose, however crowded the zone.
            var zone = new Zone("DoseGate");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            var victim = MakeVictim(zone, 5, 5); // in the cloud; NOT a turn actor
            SettlementRuntime.ActiveZone = zone;

            int hpBefore = victim.GetStatValue("Hitpoints", -1);

            // One full round: the player and three bystander NPC turns.
            var npc2 = new Entity { ID = "npc2", BlueprintName = "B2" };
            var npc3 = new Entity { ID = "npc3", BlueprintName = "B3" };
            _turnManager.AddEntity(npc2);
            _turnManager.AddEntity(npc3);
            _turnManager.EndTurn(_player);
            _turnManager.EndTurn(_npc);
            _turnManager.EndTurn(npc2);
            _turnManager.EndTurn(npc3);

            int lost = hpBefore - victim.GetStatValue("Hitpoints", -1);
            // Immediate exposure dose: (intake=100 + 1) / 20 = 5, applied
            // exactly once per round. Pre-fix this read 20 — one dose per
            // actor whose turn ended.
            Assert.AreEqual(5, lost,
                "one round in the cloud is ONE dose, not one per actor in the zone");
        }
    }
}
