using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Electrified conductors, end to end — two bugs reported from play:
    /// charge on a pipe growing without bound until it read "Infinity",
    /// and creatures standing on a live pipe taking nothing.
    ///
    /// <para><b>Bug 1 — the runaway.</b>
    /// <c>MaterialPart.HandleTryChainElectricity</c> computed
    /// <c>passCharge = charge * Conductivity</c>, treating Conductivity
    /// as a 0-1 efficiency. The field is authored 0-100 — CopperPipe is
    /// 100 — so every hop multiplied the charge by a HUNDRED. Two other
    /// consumers read the same field on the 0-100 scale
    /// (<c>TilePropagationSystem</c> compares against 50,
    /// <c>LiquidCoveredEffect</c> divides by 100), so the chain handler
    /// was the odd one out. A pipe RUN is a line of adjacent conductors,
    /// which is why laying pipe runs in worldgen is what finally made it
    /// visible.</para>
    ///
    /// <para><b>Bug 2 — the dead pipe.</b> The only rule that shocked
    /// anyone was <c>electrify_water</c>, which requires a WATER coating.
    /// A bare charged conductor has charge and no coating, so nothing
    /// matched and standing on it did nothing at all.</para>
    ///
    /// <para>Energy conservation is the invariant worth holding onto: a
    /// chain should ATTENUATE. Any arithmetic that can grow charge as it
    /// travels is wrong regardless of the constant involved.</para>
    /// </summary>
    public class ElectrifiedConductorTests
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

        private static Entity Creature(Zone zone, string name, int x, int y, int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["ElectricResistance"] = new Stat { Owner = e, Name = "ElectricResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new Body());
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>A run of copper pipe along one row.</summary>
        private List<Entity> PipeRun(Zone zone, int x0, int x1, int y)
        {
            var run = new List<Entity>();
            for (int x = x0; x <= x1; x++)
            {
                var p = _factory.CreateEntity("CopperPipe");
                Assert.IsNotNull(p, "CopperPipe blueprint");
                zone.AddEntity(p, x, y);
                run.Add(p);
            }
            return run;
        }

        private static float ChargeOn(Entity e)
            => e.GetPart<StatusEffectsPart>()?.GetEffect<ElectrifiedEffect>()?.Charge ?? 0f;

        private static void TickTurnEnd(Entity e, Zone zone)
        {
            var ev = GameEvent.New("EndTurn");
            ev.SetParameter("Zone", zone);
            e.GetPart<StatusEffectsPart>()?.HandleEvent(ev);
            ev.Release();
        }

        // ════════════════════════════════════════════════════════
        // BUG 1 — charge must never grow as it travels
        // ════════════════════════════════════════════════════════

        [Test]
        public void ChainedCharge_NeverExceedsTheChargeThatEnteredTheChain()
        {
            // The invariant. Whatever the conductivity constant, a chain
            // is a transmission line, not an amplifier.
            var zone = new Zone();
            var run = PipeRun(zone, 5, 11, 9);

            const float seed = 2.0f;
            run[0].ApplyEffect(new ElectrifiedEffect(charge: seed), null, zone);

            for (int turn = 0; turn < 8; turn++)
                foreach (var pipe in run)
                    TickTurnEnd(pipe, zone);

            foreach (var pipe in run)
            {
                float c = ChargeOn(pipe);
                Assert.IsFalse(float.IsInfinity(c), "charge reached Infinity");
                Assert.IsFalse(float.IsNaN(c), "charge went NaN");
                Assert.LessOrEqual(c, seed + 0.001f,
                    "a pipe carries " + c + " from a " + seed
                    + " source — the chain is amplifying, not conducting");
            }
        }

        [Test]
        public void ChargeAttenuatesAlongARun_RatherThanGrowing()
        {
            // Directional counter-check: the far end must not be hotter
            // than the near end.
            var zone = new Zone();
            var run = PipeRun(zone, 5, 12, 4);
            run[0].ApplyEffect(new ElectrifiedEffect(charge: 2.0f), null, zone);

            for (int turn = 0; turn < 6; turn++)
                foreach (var pipe in run)
                    TickTurnEnd(pipe, zone);

            float first = ChargeOn(run[0]);
            float last = ChargeOn(run[run.Count - 1]);

            Assert.LessOrEqual(last, first + 0.001f,
                "the far end (" + last + ") is hotter than the source ("
                + first + ")");
        }

        [Test]
        public void ALongRunOverManyTurns_StaysFinite()
        {
            // The reported symptom, reproduced directly: a long pipe run
            // left to tick showed "Infinity".
            var zone = new Zone();
            var run = PipeRun(zone, 2, 20, 12);
            run[0].ApplyEffect(new ElectrifiedEffect(charge: 1.5f), null, zone);

            for (int turn = 0; turn < 40; turn++)
                foreach (var pipe in run)
                    TickTurnEnd(pipe, zone);

            foreach (var pipe in run)
                Assert.IsTrue(float.IsFinite(ChargeOn(pipe)),
                    "charge on a pipe is no longer a finite number");
        }

        [Test]
        public void TwoAdjacentConductors_DoNotPingPongUpwards()
        {
            // The tightest possible feedback loop: A charges B, B charges
            // A. If either hop has gain, this diverges.
            var zone = new Zone();
            var a = _factory.CreateEntity("CopperPipe");
            var b = _factory.CreateEntity("CopperPipe");
            zone.AddEntity(a, 6, 6);
            zone.AddEntity(b, 7, 6);

            a.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), null, zone);

            for (int turn = 0; turn < 25; turn++)
            {
                TickTurnEnd(a, zone);
                TickTurnEnd(b, zone);
            }

            Assert.LessOrEqual(ChargeOn(a), 1.001f, "A ran away: " + ChargeOn(a));
            Assert.LessOrEqual(ChargeOn(b), 1.001f, "B ran away: " + ChargeOn(b));
        }

        [Test]
        public void ConductorThreshold_MatchesTheRestOfTheCodebase()
        {
            // The same 0-100 confusion made the conductor TEST wrong too:
            // `Conductivity > 0.5f` treats a material of conductivity 1
            // (essentially an insulator on this scale) as a conductor.
            // TilePropagationSystem's threshold is 50; they must agree or
            // "what conducts" depends on which system you ask.
            var zone = new Zone();

            var insulator = new Entity { ID = "insulator", BlueprintName = "Insulator" };
            insulator.AddPart(new RenderPart { DisplayName = "rubber mat" });
            insulator.AddPart(new MaterialPart { MaterialID = "Rubber", Conductivity = 5f });
            zone.AddEntity(insulator, 9, 9);

            Assert.IsFalse(TilePropagationSystem.IsConductive(zone, 9, 9),
                "conductivity 5 of 100 must not conduct (propagation)");

            // And the chain must agree: a source next to it leaves it cold.
            var pipe = _factory.CreateEntity("CopperPipe");
            zone.AddEntity(pipe, 8, 9);
            pipe.ApplyEffect(new ElectrifiedEffect(charge: 2.0f), null, zone);
            TickTurnEnd(pipe, zone);

            Assert.IsFalse(insulator.HasEffect<ElectrifiedEffect>(),
                "a near-insulator was electrified — the chain's conductor "
                + "threshold disagrees with TilePropagationSystem's");
        }

        [Test]
        public void ARealConductor_StillChains()
        {
            // Counter-check for the threshold test: tightening it must
            // not stop actual conductors from working.
            var zone = new Zone();
            var a = _factory.CreateEntity("CopperPipe");
            var b = _factory.CreateEntity("CopperPipe");
            zone.AddEntity(a, 3, 3);
            zone.AddEntity(b, 4, 3);

            a.ApplyEffect(new ElectrifiedEffect(charge: 2.0f), null, zone);
            TickTurnEnd(a, zone);

            Assert.IsTrue(b.HasEffect<ElectrifiedEffect>(),
                "copper next to copper did not chain at all");
            Assert.Greater(ChargeOn(b), 0f, "chained but with no charge");
        }

        // ════════════════════════════════════════════════════════
        // BUG 2 — standing on a live conductor must hurt
        // ════════════════════════════════════════════════════════

        [Test]
        public void ACreatureOnACHargedPipe_TakesAShock()
        {
            // The reported miss. electrify_water needs a water coating; a
            // bare charged pipe had none, so nothing fired and the
            // creature stood on a live rail unharmed.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("CopperPipe"), 10, 10);
            var victim = Creature(zone, "snapjaw", 10, 10);
            int hp = victim.GetStatValue("Hitpoints");

            ZoneTileStateSystem.AddCharge(zone, 10, 10, 2);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.Less(victim.GetStatValue("Hitpoints"), hp,
                "standing on a live conductor did no damage");
        }

        [Test]
        public void ACreatureOnACHargedPipe_IsElectrified()
        {
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("CopperPipe"), 10, 10);
            var victim = Creature(zone, "snapjaw", 10, 10);

            ZoneTileStateSystem.AddCharge(zone, 10, 10, 2);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.IsTrue(victim.HasEffect<ElectrifiedEffect>(),
                "the shock left no electrified status");
        }

        [Test]
        public void ACreatureOnAChargedNonConductor_IsUnharmed()
        {
            // Counter-check: charge on bare ground is not a hazard, or
            // every Fulmination would electrocute the caster's own tile
            // regardless of what it is standing on.
            var zone = new Zone();
            var bystander = Creature(zone, "bystander", 14, 14);
            int hp = bystander.GetStatValue("Hitpoints");

            ZoneTileStateSystem.AddCharge(zone, 14, 14, 2);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.AreEqual(hp, bystander.GetStatValue("Hitpoints"),
                "charge on plain ground should not shock anyone");
        }

        [Test]
        public void AnUnchargedPipe_IsSafeToStandOn()
        {
            // Counter-check: the pipe itself is not the hazard. A dead
            // rail must be walkable.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("CopperPipe"), 7, 7);
            var walker = Creature(zone, "walker", 7, 7);
            int hp = walker.GetStatValue("Hitpoints");

            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.AreEqual(hp, walker.GetStatValue("Hitpoints"),
                "an unpowered pipe hurt someone");
            Assert.IsFalse(walker.HasEffect<ElectrifiedEffect>());
        }

        [Test]
        public void TheShockConsumesTheCharge_SoItIsNotAPermanentKillField()
        {
            // A live rail that never discharges would grind any creature
            // that walked onto it to death over time.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("CopperPipe"), 11, 11);
            Creature(zone, "victim", 11, 11);

            ZoneTileStateSystem.AddCharge(zone, 11, 11, 1);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.AreEqual(0, zone.TileState.Charge(11, 11),
                "the shock did not draw the charge down");
        }

        [Test]
        public void TheWetRuleStillWins_OnAWetConductor()
        {
            // A conductive tile that is ALSO wet should still resolve as
            // electrified water — the older, stronger rule. Otherwise
            // adding the conductor rule would quietly weaken every
            // puddle.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("CopperPipe"), 13, 13);
            var victim = Creature(zone, "victim", 13, 13);
            int hp = victim.GetStatValue("Hitpoints");

            zone.TileState.WriteCoating(13, 13, "water", 5);
            ZoneTileStateSystem.AddCharge(zone, 13, 13, 2);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            int lost = hp - victim.GetStatValue("Hitpoints");
            Assert.GreaterOrEqual(lost, 5,
                "a wet conductor should shock at least as hard as wet "
                + "ground (took " + lost + ")");
        }

        // ════════════════════════════════════════════════════════
        // The whole loop, as a player would meet it
        // ════════════════════════════════════════════════════════

        [Test]
        public void ChargeAPool_AndSomeoneStandingDownThePipeRunGetsHit()
        {
            // The greenhouse, weaponised: charge the water, the pipe run
            // carries it, and whoever is standing on the far end takes
            // the hit. This is the whole feature in one assertion.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("BrinePool"), 5, 8);
            for (int x = 6; x <= 10; x++)
                zone.AddEntity(_factory.CreateEntity("CopperPipe"), x, 8);

            // Stand within MetalChargeRange (4) of the pool. Charge is
            // deliberately range-limited; asking for reach 5 would be
            // testing a distance the design does not promise.
            var victim = Creature(zone, "victim", 9, 8);
            int hp = victim.GetStatValue("Hitpoints");

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.AddCharge(zone, 5, 8, 2);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.Less(victim.GetStatValue("Hitpoints"), hp,
                "charge into the pool never reached the far end of the pipe run");
        }

        [Test]
        public void ChargeDoesNotReachSomeoneOffTheRun()
        {
            // Counter-check: the pipe is the reason it travelled, not
            // proximity. Someone one row off the run stays dry.
            var zone = new Zone();
            zone.AddEntity(_factory.CreateEntity("BrinePool"), 5, 8);
            for (int x = 6; x <= 10; x++)
                zone.AddEntity(_factory.CreateEntity("CopperPipe"), x, 8);

            var offRun = Creature(zone, "offRun", 10, 3);
            int hp = offRun.GetStatValue("Hitpoints");

            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.AddCharge(zone, 5, 8, 2);
            ZoneTileStateSystem.ResolveAfterAbility(zone);

            Assert.AreEqual(hp, offRun.GetStatValue("Hitpoints"),
                "someone well off the pipe run was shocked anyway");
        }

        [Test]
        public void RepeatedTurnsOnAPipeNetwork_DoNotEscalateDamage()
        {
            // The two bugs together: if charge grew AND the tile shocked,
            // a pipe network would become an ever-worsening death trap.
            // Damage on turn 10 must not exceed damage on turn 1.
            var zone = new Zone();
            for (int x = 4; x <= 12; x++)
                zone.AddEntity(_factory.CreateEntity("CopperPipe"), x, 6);

            var victim = Creature(zone, "victim", 12, 6, hp: 100000);

            int firstLoss = 0, lastLoss = 0;
            for (int turn = 0; turn < 10; turn++)
            {
                int before = victim.GetStatValue("Hitpoints");
                ZoneTileStateSystem.AddCharge(zone, 4, 6, 2);
                ZoneTileStateSystem.OnPlayerTurnEnd(zone);
                int loss = before - victim.GetStatValue("Hitpoints");

                if (turn == 0) firstLoss = loss;
                lastLoss = loss;
            }

            Assert.LessOrEqual(lastLoss, firstLoss + 2,
                "damage escalated from " + firstLoss + " to " + lastLoss
                + " over ten turns — the network is charging itself up");
        }
    }
}
