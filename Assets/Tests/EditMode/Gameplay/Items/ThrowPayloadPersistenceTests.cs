using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ThrowPayloadPersistenceTests : FiftyWorldFixture
    {
        static readonly FieldInfo ReactionsField = typeof(TileReactionSystem).GetField("_reactions", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo ReactionsInitialized = typeof(TileReactionSystem).GetField("<IsInitialized>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        List<TileReactionSystem.TileReaction> previousReactions;
        bool previousInitialized;
        [SetUp] public void LoadActualReactions()
        {
            previousReactions = new List<TileReactionSystem.TileReaction>((List<TileReactionSystem.TileReaction>)ReactionsField.GetValue(null));
            previousInitialized = TileReactionSystem.IsInitialized;
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Data/TileReactions/Reactions.json")));
            Assert.True(TileReactionSystem.IsInitialized);
        }
        [TearDown] public void RestoreReactions()
        {
            TileReactionSystem.ResetForTests();
            ((List<TileReactionSystem.TileReaction>)ReactionsField.GetValue(null)).AddRange(previousReactions);
            ReactionsInitialized.SetValue(null, previousInitialized);
        }
        Entity[] Films => Zone.GetReadOnlyEntities().Where(e => e.HasPart<FiniteLiquidFilmPart>()).ToArray();
        int Matter => Films.Sum(e => e.GetPart<LiquidPoolPart>().Volume);
        void Burst(string blueprint, bool focused = false, int x = 15, int y = 10, string serial = "throw-1")
            => Burst(Factory.CreateEntity(blueprint), focused, x, y, serial);
        void Burst(Entity item, bool focused = false, int x = 15, int y = 10, string serial = "throw-1")
            => ThrowPayloadResolver.Resolve(Actor, item, Zone, Zone.GetCell(x, y), null, null, focused, new Random(17), serial);
        Entity Creature(int x, int y, string id = "victim")
        {
            var e = Factory.CreateEntity("Player"); e.ID = id;
            Assert.True(Zone.AddEntity(e, x, y)); return e;
        }
        Entity Wall(int x, int y)
        {
            var e = new Entity { ID = "wall-" + x + "-" + y };
            e.SetTag("Solid"); e.SetTag("Wall"); e.AddPart(new PhysicsPart());
            Assert.True(Zone.AddEntity(e, x, y)); return e;
        }
        [TestCase("WaterTonic")][TestCase("AcidTonic")]
        public void BroadAndFocusedConserveTheSameNineExposureUnits(string blueprint)
        {
            Burst(blueprint); Assert.AreEqual(9, Matter); Assert.AreEqual(9, Films.Length);
            foreach (var e in Films) { Assert.AreEqual(1, e.GetPart<LiquidPoolPart>().Volume); Zone.RemoveEntity(e); }
            Burst(blueprint, true); Assert.AreEqual(9, Matter); Assert.AreEqual(1, Films.Length);
            Assert.AreEqual(6, Films[0].GetPart<FiniteLiquidFilmPart>().RemainingRounds);
            Assert.AreEqual(Actor.ID, Films[0].GetPart<FiniteLiquidFilmPart>().SourceActorId);
        }
        [Test] public void CropAcidDoseCreatesThreeUnitsRatherThanANineUnitFlask()
        { Burst("ScarletSundewDew"); Assert.AreEqual(3, Matter); Assert.AreEqual(3, Films.Length); }
        [Test] public void ClippingLosesAllocatedMatterWithoutRedistributingItThroughWalls()
        {
            Wall(15, 9); Wall(16, 10); Burst("WaterTonic");
            Assert.AreEqual(6, Matter, "two wall cells and their sealed NE corner lose allocations");
            Assert.IsFalse(Films.Any(e => Zone.GetEntityCell(e) == Zone.GetCell(16, 9)));
        }
        [Test] public void DoseRevalidatesCurrentPositionAfterAnEarlierTargetCallbackMovesAnotherOwner()
        {
            var first = Creature(15, 10, "a"); var moved = Creature(15, 9, "b");
            first.AddPart(new PayloadRelocationProbe { Zone = Zone, Other = moved });
            Burst("WaterTonic"); Assert.NotNull(first.GetEffect<WetEffect>());
            Assert.Null(moved.GetEffect<WetEffect>(), "a snapshot owner moved outside the initial footprint is not dosed remotely");
        }
        [Test] public void WideOwnerReceivesOneDirectDoseAndNoImmediateFilmCoating()
        {
            var body = Creature(15, 10); body.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            var counter = new PayloadDoseProbe(); body.AddPart(counter); Burst("WaterTonic");
            Assert.AreEqual(1, counter.Applied); Assert.Null(body.GetEffect<LiquidCoveredEffect>()); Assert.AreEqual(9, Matter);
        }
        [Test] public void FocusedSpareNeighbourAndBroadHealingRetainsFriendlyFire()
        {
            var a = Creature(15, 10, "a"); var b = Creature(16, 10, "b");
            a.GetStat("Hitpoints").BaseValue = b.GetStat("Hitpoints").BaseValue = 1;
            Burst("HealingTonic", true); Assert.Greater(a.GetStatValue("Hitpoints"), 1); Assert.AreEqual(1, b.GetStatValue("Hitpoints"));
            Burst("HealingTonic"); Assert.Greater(b.GetStatValue("Hitpoints"), 1); Assert.Zero(Matter);
        }
        [TestCase("HealingTonic")][TestCase("Antidote")][TestCase("BurnSalve")][TestCase("Panacea")]
        [TestCase("SpeedTonic")][TestCase("StrengthTonic")][TestCase("StoneskinTonic")][TestCase("PoisonTonic")]
        [TestCase("BleedTonic")][TestCase("CharredTonic")][TestCase("KnitmossPad")][TestCase("ShalebeanWax")]
        [TestCase("KnotflaxBandage")][TestCase("ClaspbeanPulp")][TestCase("SumpsievePad")][TestCase("AbsentmintLeaf")]
        [TestCase("MargincressRibbon")][TestCase("SootrootPulp")][TestCase("BrinebuttonPaste")][TestCase("KnucklecapWax")]
        public void DirectOnlyFamiliesNeverInventCollectibleOrContactTerrain(string blueprint)
        { var item = Factory.CreateEntity(blueprint); Assert.True(ThrowPayloadResolver.HasTonicPayload(item)); Burst(item); Assert.Zero(Matter); }
        [TestCase("FireTonic", "heat")][TestCase("CinderpeaOil", "heat")]
        [TestCase("FrostTonic", "cold")][TestCase("LightningTonic", "charge")]
        public void BareFloorEnergyIsOnePerCellWithoutInventingAnEnduringTrap(string blueprint, string channel)
        {
            Burst(blueprint, true); var s = Zone.TileState.Get(15, 10); Assert.NotNull(s);
            Assert.AreEqual(1, channel == "heat" ? s.Heat : channel == "cold" ? s.Cold : s.Charge);
            Assert.Zero(Matter); Assert.False(Zone.TileState.HasCoating(15, 10, "oil"));
        }
        [Test] public void MultiEffectBrewUsesOneAcidMatterBudgetAndDeduplicatedEnergyChannels()
        {
            var brew = Factory.CreateEntity("BrewedTonic");
            brew.AddPart(new BrewItemPart { EffectsRaw = "Wet:2;Acidic:2;Burning:1;Burning:2", Form = "Throwable" });
            Burst(brew, true); Assert.AreEqual(9, Matter); Assert.AreEqual("acid", Films.Single().GetPart<LiquidPoolPart>().LiquidId);
            Assert.AreEqual(1, Zone.TileState.Heat(15, 10));
        }
        [Test] public void FiniteWaterCannotFillEitherVesselAndOrdinaryWaterStillCan()
        {
            Burst("WaterTonic", true, 11, 10); var film = Films.Single(); var flask = Flask(0); var skin = Skin();
            Assert.False(Act(flask, "FillLiquidVessel|" + Id(film) + "|water")); Assert.False(Act(skin, "FillWaterskin"));
            Assert.Zero(Units(flask)); Assert.Zero(Units(skin)); Assert.AreEqual(9, Matter);
            Zone.RemoveEntity(film); Pool(8); Assert.True(Act(skin, "FillWaterskin")); Assert.AreEqual(3, Units(skin));
        }
        [Test] public void PourCannotMergeRecoverableDramsIntoTheNoncollectibleFilm()
        {
            Burst("WaterTonic", true, 11, 10); var flask = Flask(3);
            Assert.True(Act(flask, Pour(flask, false))); Assert.AreEqual(9, Matter);
            Assert.AreEqual(3, Zone.GetCell(11, 10).Objects.Where(e => !e.HasPart<FiniteLiquidFilmPart>()).Sum(e => e.GetPart<LiquidPoolPart>()?.Volume ?? 0));
        }
        [Test] public void WideEntryDebitsOnePerLaunchAndTrueReentryCanSpendAnotherUnit()
        {
            Burst("AcidTonic"); var mover = Creature(13, 9); mover.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,1" });
            Assert.True(MovementSystem.TryMove(mover, Zone, 1, 0));
            Assert.AreEqual(8, Matter); Assert.AreEqual(1, mover.GetEffect<LiquidCoveredEffect>().Amount);
            Assert.True(MovementSystem.TryMove(mover, Zone, -1, 0)); Assert.True(MovementSystem.TryMove(mover, Zone, 1, 0));
            Assert.AreEqual(7, Matter); Assert.AreEqual(2, mover.GetEffect<LiquidCoveredEffect>().Amount);
        }
        [Test] public void ResidualAcidUsesRealDelayedTypedDamageAndLiveThrowerCredit()
        {
            Burst("AcidTonic", true); var mover = Creature(14, 10); var probe = new PayloadSourceProbe(); mover.AddPart(probe);
            Assert.True(MovementSystem.TryMove(mover, Zone, 1, 0)); var coat = mover.GetEffect<LiquidCoveredEffect>();
            Assert.AreEqual(Actor.ID, coat.SourceActorId); Assert.AreEqual(40, mover.GetStatValue("Hitpoints"));
            var ev = GameEvent.New("BeginTakeAction"); ev.SetParameter("Zone", Zone);
            try { coat.OnTurnStart(mover, ev); } finally { ev.Release(); }
            Assert.AreEqual(37, mover.GetStatValue("Hitpoints")); Assert.AreSame(Actor, probe.Source);
        }
        [Test] public void MixedSourceCoatingClearsCreditAndSameSourcePreservesIt()
        {
            var coat = new LiquidCoveredEffect("acid", 1) { SourceActorId = Actor.ID };
            Assert.True(coat.OnStack(new LiquidCoveredEffect("acid", 1) { SourceActorId = Actor.ID })); Assert.AreEqual(Actor.ID, coat.SourceActorId);
            Assert.True(coat.OnStack(new LiquidCoveredEffect("acid", 1) { SourceActorId = "other" })); Assert.AreEqual("", coat.SourceActorId);
            Assert.True(coat.OnStack(new LiquidCoveredEffect("acid", 1) { SourceActorId = Actor.ID })); Assert.AreEqual("", coat.SourceActorId);
        }
        [Test] public void FilmAgesOnlyAtPlayerWorldBoundaryAndNeverDeletesIndependentWater()
        {
            Zone.TileState.WriteCoating(15, 10, "water", 20); Burst("WaterTonic", true);
            ZoneTileStateSystem.ResolveAfterAbility(Zone, Actor); Assert.AreEqual(6, Films.Single().GetPart<FiniteLiquidFilmPart>().RemainingRounds);
            for (int i = 0; i < 6; i++) ZoneTileStateSystem.OnPlayerTurnEnd(Zone);
            Assert.Zero(Matter); Assert.IsEmpty(Films); Assert.AreEqual(14, Zone.TileState.CoatingTurns(15, 10, "water"));
        }
        [TestCase(false)][TestCase(true)] public void ConsumingReactionRetiresActualWaterFilmWithoutResurrection(bool freeze)
        {
            Burst("WaterTonic", true); if (freeze) Zone.TileState.AddCold(15, 10, 1); else Zone.TileState.AddHeat(15, 10, 1);
            ZoneTileStateSystem.ResolveAfterAbility(Zone, Actor); Assert.IsEmpty(Films); Assert.Zero(Matter);
            Assert.False(Zone.TileState.HasCoating(15, 10, "water"));
            Assert.True(freeze ? Zone.TileState.HasCoating(15, 10, "ice") : Zone.TileState.Cloud(15, 10) == "steam");
            ZoneTileStateSystem.OnPlayerTurnEnd(Zone); Assert.False(Zone.TileState.HasCoating(15, 10, "water"));
        }
        [Test] public void ChargeConsumesEnergyButNotTheWaterFilm()
        {
            Burst("WaterTonic", true); Zone.TileState.AddCharge(15, 10, 1); ZoneTileStateSystem.ResolveAfterAbility(Zone, Actor);
            Assert.AreEqual(9, Matter); Assert.Zero(Zone.TileState.Charge(15, 10));
        }
        [Test] public void FiniteAcidNavigationPenaltyDisappearsWhenItsMatterExpires()
        {
            Burst("AcidTonic", true); Assert.Greater(TerrainNavigationWeight.ForCell(Zone.GetCell(15, 10), Actor), 0);
            for (int i = 0; i < 6; i++) ZoneTileStateSystem.OnPlayerTurnEnd(Zone);
            Assert.Zero(TerrainNavigationWeight.ForCell(Zone.GetCell(15, 10), Actor));
        }
        [Test] public void SessionRoundTripPreservesFiniteMatterSourceAndIndependentCoating()
        {
            using (var f = new DecodeIsolationFixture(aura: false))
            {
                var zone = f.Saved.ZoneManager.ActiveZone;
                zone.TileState.WriteCoating(5, 6, "water", 19);
                ThrowPayloadResolver.Resolve(f.Saved.Player, Factory.CreateEntity("WaterTonic"), zone, zone.GetCell(5, 6), null, null, true, new Random(17), "saved-launch");
                ZoneTileStateSystem.OnPlayerTurnEnd(zone);
                var first = DecodeIsolationFixture.Decode(f.EncodeSaved());
                var again = DecodeIsolationFixture.Decode(DecodeIsolationFixture.Serialize(first));
                var loaded = again.ZoneManager.ActiveZone;
                var film = loaded.GetReadOnlyEntities().Single(e => e.HasPart<FiniteLiquidFilmPart>());
                Assert.AreEqual(9, film.GetPart<LiquidPoolPart>().Volume);
                Assert.AreEqual(5, film.GetPart<FiniteLiquidFilmPart>().RemainingRounds);
                Assert.AreEqual(again.Player.ID, film.GetPart<FiniteLiquidFilmPart>().SourceActorId);
                Assert.AreEqual("saved-launch", film.GetPart<FiniteLiquidFilmPart>().ThrowSerial);
                for (int i = 0; i < 5; i++) ZoneTileStateSystem.OnPlayerTurnEnd(loaded);
                Assert.False(loaded.GetReadOnlyEntities().Any(e => e.HasPart<FiniteLiquidFilmPart>()));
                Assert.AreEqual(13, loaded.TileState.CoatingTurns(5, 6, "water"));
            }
        }
        [Test] public void ExplicitGroundClearConsumesFilmAndOverlappingFilmsDoNotMultiplyLayerRefund()
        {
            Burst("WaterTonic", true); Burst("WaterTonic", true, serial: "throw-2");
            Assert.AreEqual(18, Matter); Assert.AreEqual(1, Zone.TileState.CountLayers(15, 10));
            Assert.AreEqual(1, Zone.TileState.Clear(15, 10)); Assert.Zero(Matter); Assert.IsEmpty(Films);
        }
        [Test] public void KnownFootprintDoesNotUseHiddenWallAsAnOracle()
        {
            for (int x = 14; x <= 16; x++) for (int y = 9; y <= 11; y++) { Zone.GetCell(x, y).IsVisible = true; Zone.GetCell(x, y).Explored = true; }
            Wall(15, 9); Zone.GetCell(15, 9).IsVisible = false;
            var known = new List<Cell>(); var actual = new List<Cell>();
            ThrowPayloadResolver.CollectFootprint(Zone, Zone.GetCell(15, 10), false, known, true);
            ThrowPayloadResolver.CollectFootprint(Zone, Zone.GetCell(15, 10), false, actual);
            Assert.AreEqual(9, known.Count); Assert.AreEqual(8, actual.Count);
        }
        [TestCase(false)][TestCase(true)]
        public void FilmContactRequiresItsActualCurrentOccupant(bool touching)
        {
            Burst("AcidTonic", true); var film = Films.Single();
            var mover = Creature(touching ? 15 : 25, 10);
            var ev = GameEvent.New("EntityEnteredCell"); ev.SetParameter("Actor", mover); ev.SetParameter("Zone", Zone);
            try { film.FireEvent(ev); } finally { ev.Release(); }
            Assert.AreEqual(touching ? 8 : 9, Matter);
            Assert.AreEqual(touching, mover.GetEffect<LiquidCoveredEffect>() != null,
                "a stale or forged entry event cannot dose an owner somewhere else");
        }
        [Test] public void DirectOnlyTonicDoesNotClaimOrResolveUnrelatedPrimedGround()
        {
            Zone.TileState.WriteCoating(30, 20, "oil", 8); Zone.TileState.AddHeat(30, 20, 1);
            Burst("HealingTonic", true);
            Assert.True(Zone.TileState.HasCoating(30, 20, "oil"));
            Assert.AreEqual(1, Zone.TileState.Heat(30, 20)); Assert.False(Zone.TileState.HasResidue(30, 20, "embers"));
            ZoneTileStateSystem.ResolveAfterAbility(Zone, Actor);
            Assert.False(Zone.TileState.HasCoating(30, 20, "oil"), "positive control: these are real primed shipped reactions");
        }
        [Test] public void FilmProjectionDoesNotHideMalformedCloudStateFromSessionValidation()
        {
            using (var f = new DecodeIsolationFixture(aura: false))
            {
                var zone = f.Saved.ZoneManager.ActiveZone;
                ThrowPayloadResolver.Resolve(f.Saved.Player, Factory.CreateEntity("WaterTonic"), zone, zone.GetCell(5, 6), null, null, true, new Random(17), "saved-launch");
                var state = zone.TileState.Get(5, 6); state.Cloud = ""; state.CloudTurns = 4;
                Assert.Throws<InvalidDataException>(() => f.EncodeSaved());
                Assert.AreEqual(4, state.CloudTurns, "rejection preserves invalid source for diagnosis instead of silently clamping");
            }
        }
        [Test] public void ReactionRechecksEachVictimsCurrentOccupiedCellAfterEarlierDamageCallback()
        {
            var first = Creature(15, 10, "first"); var moved = Creature(15, 10, "moved");
            first.AddPart(new PayloadDamageRelocationProbe { Zone = Zone, Other = moved });
            Zone.TileState.WriteCoating(15, 10, "water", 6); Zone.TileState.AddCharge(15, 10, 1);
            ZoneTileStateSystem.ResolveAfterAbility(Zone, Actor);
            Assert.Less(first.GetStatValue("Hitpoints"), 40, "real shipped reaction positive control");
            Assert.AreSame(Zone.GetCell(30, 20), Zone.GetEntityCell(moved));
            Assert.AreEqual(40, moved.GetStatValue("Hitpoints")); Assert.Null(moved.GetEffect<ElectrifiedEffect>());
        }
        [TestCase("WaterTonic", "9 contact units", "6 local turns")]
        [TestCase("ScarletSundewDew", "3 contact units", "too little to collect")]
        [TestCase("HealingTonic", "Direct targets only", "no ground residue")]
        [TestCase("FireTonic", "1 heat", "existing ground reactions")]
        [TestCase("FrostTonic", "1 cold", "existing ground reactions")]
        [TestCase("LightningTonic", "1 charge", "existing ground reactions")]
        public void ExamineDisclosesActualGroundDeliveryWithoutInventingPersistentTraps(string blueprint, string first, string second)
        {
            var item = Factory.CreateEntity(blueprint); Assert.True(TonicExamineService.TryDescribeDetails(item, out var text));
            StringAssert.Contains(first, text); StringAssert.Contains(second, text);
            StringAssert.Contains("Broad", text); StringAssert.Contains("Focused", text);
            Assert.Zero(Matter); Assert.AreEqual(0, Zone.TileState.WrittenCount, "description is a pure query");
        }
        [TestCase(false)][TestCase(true)] public void SplashSharesAuthoritativeLowCoverVersusSealedGeometry(bool sealedCover)
        {
            var cover = new Entity { ID = "cover" };
            cover.AddPart(new PhysicsPart { Solid = true, ThrowHeight = sealedCover ? 2.4f : .65f });
            Assert.True(Zone.AddEntity(cover, 15, 9));
            Assert.AreEqual(sealedCover, ThrowProfile.For(cover).Sealed);
            Burst("WaterTonic"); Assert.AreEqual(sealedCover ? 8 : 9, Matter);
        }
        [TestCase(false)][TestCase(true)] public void OpenDoorSplashUsesItsApertureEvenWithAnOldSolidFlag(bool closed)
        {
            var door = new Entity { ID = "door" };
            var physics = new PhysicsPart(); door.AddPart(physics); door.AddPart(new DoorPart { IsOpen = !closed });
            physics.Solid = true; // legacy/custom profile; aperture is still authoritative
            Assert.True(Zone.AddEntity(door, 15, 9));
            Burst("WaterTonic"); Assert.AreEqual(closed ? 8 : 9, Matter);
        }
    }
    public sealed class PayloadDamageRelocationProbe : Part
    {
        public Zone Zone; public Entity Other;
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == "TakeDamage") Zone.MoveEntity(Other, 30, 20); return true; }
    }
    public sealed class PayloadRelocationProbe : Part
    {
        public Zone Zone; public Entity Other;
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == "BeforeApplyEffect") Zone.MoveEntity(Other, 30, 20); return true; }
    }
    public sealed class PayloadDoseProbe : Part
    {
        public int Applied;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeApplyEffect") Applied++; return true; }
    }
    public sealed class PayloadSourceProbe : Part
    {
        public Entity Source;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "TakeDamage") Source = e.GetParameter<Entity>("Source"); return true; }
    }
}
