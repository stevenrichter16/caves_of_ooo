using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class PouredFuelCombustionTests : FiftyWorldFixture
    {
        static readonly FieldInfo Rules = typeof(TileReactionSystem).GetField("_reactions", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo Initialized = typeof(TileReactionSystem).GetField("<IsInitialized>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        List<TileReactionSystem.TileReaction> oldRules;
        bool oldInitialized;

        [SetUp] public void LoadReactions()
        {
            oldRules = new List<TileReactionSystem.TileReaction>((List<TileReactionSystem.TileReaction>)Rules.GetValue(null));
            oldInitialized = TileReactionSystem.IsInitialized;
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void RestoreReactions()
        {
            var rules = (List<TileReactionSystem.TileReaction>)Rules.GetValue(null);
            rules.Clear(); rules.AddRange(oldRules); Initialized.SetValue(null, oldInitialized);
        }
        Entity Poured(int volume = 1, string liquid = "oil", int x = 11, int y = 10)
        {
            var e = Factory.CreateEntity("PouredLiquidPool");
            e.GetPart<LiquidPoolPart>().LiquidId = liquid;
            e.GetPart<LiquidPoolPart>().Volume = volume;
            Assert.True(Zone.AddEntity(e, x, y)); return e;
        }
        void Ignite(bool embers = false)
        {
            if (embers) Zone.TileState.WriteResidue(11, 10, "embers", 4);
            else Zone.TileState.AddHeat(11, 10, 1);
            TileReactionSystem.ResolveZone(Zone);
        }
        string PourRow(Entity flask) => Actions(flask).Single(a => a.Name == "PourLiquid" && a.Command.EndsWith("|11|10")).Command;
        string FillRow(Entity flask, Entity pool) => Actions(flask).Single(a => a.Name == "FillLiquid" && a.Command.Split('|')[1] == Uri.EscapeDataString(pool.ID)).Command;

        [TestCase(false, 1)] [TestCase(true, 1)]
        [TestCase(false, 3)] [TestCase(true, 3)] [TestCase(false, int.MaxValue)]
        public void IgnitionExhaustsTheRepresentedPouredFuel(bool embers, int volume)
        {
            var pool = Poured(volume); Ignite(embers);
            Assert.Zero(pool.GetPart<LiquidPoolPart>().Volume, "burnt fuel must not remain collectible");
            Assert.Null(Zone.GetEntityCell(pool));
            Assert.False(Zone.TileState.HasCoating(11, 10, "oil"));
            Assert.True(Zone.TileState.HasResidue(11, 10, "embers"));
            Assert.AreEqual("smoke", Zone.TileState.Cloud(11, 10));
        }
        [Test] public void ActualPourBurnAndStaleRecoveryCannotReuseTheSameOil()
        {
            var flask = Flask(1, "oil"); Assert.True(Act(flask, PourRow(flask)));
            var pool = Zone.GetCell(11, 10).Objects.Single(e => e.BlueprintName == "PouredLiquidPool");
            string recovery = FillRow(flask, pool); Ignite();
            Assert.False(Act(flask, recovery)); Assert.Zero(Units(flask));
            Assert.False(Actions(flask).Any(a => a.Name == "FillLiquid"));
        }
        [Test] public void CollectionBeforeIgnitionRemainsAnAlternative()
        {
            var flask = Flask(0); var pool = Poured(3);
            Assert.True(Act(flask, FillRow(flask, pool)));
            Assert.AreEqual(3, Units(flask)); Assert.Null(Zone.GetEntityCell(pool));
            Ignite(); Assert.AreEqual(3, Units(flask));
        }
        [Test] public void AllMatchingPouredOwnersBurnButUnrelatedLiquidsAndOtherCellsRemain()
        {
            var a = Poured(2); var b = Poured(3); var water = Poured(7, "water");
            var other = Poured(5, "oil", 12, 10);
            Ignite();
            Assert.Zero(a.GetPart<LiquidPoolPart>().Volume); Assert.Zero(b.GetPart<LiquidPoolPart>().Volume);
            Assert.Null(Zone.GetEntityCell(a)); Assert.Null(Zone.GetEntityCell(b));
            Assert.AreEqual(7, water.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(5, other.GetPart<LiquidPoolPart>().Volume);
        }
        [TestCase("freeze")] [TestCase("charge")] [TestCase("steam")]
        public void PhysicalWaterIsNotFuel(string kind)
        {
            var water = Poured(3, "water");
            if (kind == "freeze") Zone.TileState.AddCold(11, 10, 1);
            else if (kind == "charge") Zone.TileState.AddCharge(11, 10, 1);
            else Zone.TileState.AddHeat(11, 10, 1);
            TileReactionSystem.ResolveZone(Zone);
            Assert.AreEqual(3, water.GetPart<LiquidPoolPart>().Volume); Assert.NotNull(Zone.GetEntityCell(water));
        }
        [Test] public void TileOnlyOilStillReactsWithoutInventingPhysicalFuel()
        {
            Zone.TileState.WriteCoating(11, 10, "oil", 6); Ignite();
            Assert.False(Zone.TileState.HasCoating(11, 10, "oil"));
            Assert.True(Zone.TileState.HasResidue(11, 10, "embers"));
        }
        [Test] public void OtherConsumingRulesDoNotSilentlyAcquireAFuelDebit()
        {
            TileReactionSystem.Initialize("{\"Reactions\":[{\"ID\":\"custom\",\"InputCoating\":\"oil\",\"InputEnergy\":\"heat\",\"ConsumeCoating\":true}]}");
            var pool = Poured(3); Ignite(); Assert.AreEqual(3, pool.GetPart<LiquidPoolPart>().Volume);
        }
        [Test] public void ExistingThinFilmConsumptionKeepsItsOwnLifetime()
        {
            var film = Factory.CreateEntity("PouredLiquidPool");
            film.AddPart(new FiniteLiquidFilmPart { SourceActorId = Actor.ID, ThrowSerial = "probe" });
            film.GetPart<LiquidPoolPart>().LiquidId = "oil"; film.GetPart<LiquidPoolPart>().Volume = 3;
            Assert.True(Zone.AddEntity(film, 11, 10));
            var pool = Poured(2); Ignite();
            Assert.Null(Zone.GetEntityCell(film)); Assert.Zero(film.GetPart<LiquidPoolPart>().Volume);
            Assert.Null(Zone.GetEntityCell(pool)); Assert.Zero(pool.GetPart<LiquidPoolPart>().Volume);
        }
        [TestCase("authored")] [TestCase("source")] [TestCase("well")]
        [TestCase("takeable")] [TestCase("creature")] [TestCase("body")]
        public void UnsupportedSourceOrCustodyIsNotSilentlyDestroyed(string kind)
        {
            var pool = Factory.CreateEntity("PouredLiquidPool");
            pool.GetPart<LiquidPoolPart>().LiquidId = "oil"; pool.GetPart<LiquidPoolPart>().Volume = 8;
            if (kind == "authored") pool.BlueprintName = "AuthoredNaturalPool";
            if (kind == "source") pool.AddPart(new TileStateSourcePart { Coating = "oil" });
            if (kind == "well") pool.AddPart(new WellPart());
            if (kind == "takeable") pool.GetPart<PhysicsPart>().Takeable = true;
            if (kind == "creature") pool.SetTag("Creature");
            if (kind == "body") pool.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            Assert.True(Zone.AddEntity(pool, 11, 10)); Ignite();
            Assert.AreEqual(8, pool.GetPart<LiquidPoolPart>().Volume); Assert.NotNull(Zone.GetEntityCell(pool));
        }
        [Test] public void LegacyPoolRoundTripRequiresNoNewSavedFlag()
        {
            var pool = Poured(3); Zone.RemoveEntity(pool);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(pool);
            Assert.True(Zone.AddEntity(loaded, 11, 10)); Ignite();
            Assert.Zero(loaded.GetPart<LiquidPoolPart>().Volume); Assert.Null(Zone.GetEntityCell(loaded));
        }
        [Test] public void SpentPoolDoesNotReappearInAFullSessionRoundTrip()
        {
            using (var f = new DecodeIsolationFixture(aura: false))
            {
                var zone = f.Saved.ZoneManager.ActiveZone;
                var pool = Factory.CreateEntity("PouredLiquidPool");
                pool.GetPart<LiquidPoolPart>().LiquidId = "oil"; pool.GetPart<LiquidPoolPart>().Volume = 3;
                Assert.True(zone.AddEntity(pool, 5, 6)); string id = pool.ID;
                zone.TileState.AddHeat(5, 6, 1); TileReactionSystem.ResolveZone(zone);
                var decoded = DecodeIsolationFixture.Decode(f.EncodeSaved());
                var loaded = decoded.ZoneManager.ActiveZone;
                Assert.False(loaded.GetReadOnlyEntities().Any(e => e.ID == id));
                Assert.False(loaded.TileState.HasCoating(5, 6, "oil"));
            }
        }
        [Test] public void FuelIsSpentBeforeDamageCallbacksAndReentryCannotRecoverIt()
        {
            var previousSlipRng = LiquidSlipSystem.TestRng;
            try
            {
                // This test measures callback ordering, not a random slide away
                // from the ignition cell. Keep ordinary entry/contact behavior.
                LiquidSlipSystem.TestRng = new NoSlipRoll();
                var pool = Poured(3); Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0));
                Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(Actor));
                bool called = false;
                Actor.AddPart(new DamageProbe { Callback = () =>
                {
                    called = true; Assert.Zero(pool.GetPart<LiquidPoolPart>().Volume);
                    Assert.Null(Zone.GetEntityCell(pool)); TileReactionSystem.ResolveZone(Zone);
                }});
                Ignite(); Assert.True(called);
            }
            finally { LiquidSlipSystem.TestRng = previousSlipRng; }
        }
        sealed class NoSlipRoll : System.Random
        {
            public override int Next(int maxValue) => Math.Max(0, maxValue - 1);
        }
        sealed class DamageProbe : Part
        {
            internal Action Callback;
            public override string Name => "PouredFuelDamageProbe";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeTakeDamage") Callback(); return true; }
        }
    }
}
