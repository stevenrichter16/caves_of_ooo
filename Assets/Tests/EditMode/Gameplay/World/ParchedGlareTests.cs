using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W2.3 (Docs/FELLING-W1-W2-PLAN.md §7.6) — Parched + the Height-band
    /// glare, WorldClock's first gameplay consumer. The exposure rule in
    /// one sentence: ten consecutive Height-band turns on open Beating
    /// ground with a bare head parches you one stack deeper; shade,
    /// night, cover, or leaving the biome resets the streak; water —
    /// underfoot, drawn at a well, or drunk — takes it back off.
    /// </summary>
    public class ParchedGlareTests
    {
        private static EntityFactory _factory;

        // (16,16) is deep Beating on the authored map; (10,10) is Sill.
        private const string BeatingZoneId = "Overworld.16.16.0";
        private const int HeightTick = 350;   // Height band = 300-599
        private const int DawnTick = 100;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            BeatingGlareSystem.ResetForTests();
        }

        private static Entity Traveller(Zone zone, int x = 10, int y = 10)
        {
            var e = new Entity { ID = "traveller", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 16 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16 };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void EndTurn(Entity e, Zone zone)
        {
            var ev = GameEvent.New("EndTurn");
            ev.SetParameter("Zone", (object)zone);
            e.FireEvent(ev);
            ev.Release();
        }

        // ════════════════════════════════════════════════════════
        // The effect
        // ════════════════════════════════════════════════════════

        [Test]
        public void Parched_StacksToACapOfThree()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);

            for (int i = 0; i < 5; i++)
                t.ApplyEffect(new ParchedEffect(), null, zone);

            var parched = t.GetPart<StatusEffectsPart>().GetEffect<ParchedEffect>();
            Assert.AreEqual(ParchedEffect.MaxStacks, parched.Stacks, "the cap holds");
            Assert.AreEqual(ParchedEffect.MaxStacks, t.GetStat("Strength").Penalty,
                "one point of Strength per stack, no more");
            Assert.AreEqual(ParchedEffect.MaxStacks, t.GetStat("Agility").Penalty);
        }

        [Test]
        public void Parched_PenaltiesReverseOnRemove()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);

            t.GetPart<StatusEffectsPart>().RemoveEffect<ParchedEffect>();

            Assert.AreEqual(0, t.GetStat("Strength").Penalty, "the whole debt reverses at once");
            Assert.AreEqual(0, t.GetStat("Agility").Penalty);
        }

        [Test]
        public void Parched_PersistsUntilCured_NeverExpiresOnItsOwn()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);

            for (int i = 0; i < 50; i++) EndTurn(t, zone);

            Assert.IsTrue(t.HasEffect<ParchedEffect>(), "the sun does not get bored");
        }

        [Test]
        public void StandingOnWater_CuresAtTurnEnd()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone, 10, 10);
            t.ApplyEffect(new ParchedEffect(), null, zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);

            zone.TileState.WriteCoating(10, 10, "water", 6);
            EndTurn(t, zone);

            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "water underfoot ends it");
            Assert.AreEqual(0, t.GetStat("Strength").Penalty,
                "and the removal reversed BOTH stacks' penalties");
        }

        // ════════════════════════════════════════════════════════
        // The glare — every clause of the exposure rule, with its counter
        // ════════════════════════════════════════════════════════

        private static void Expose(Entity player, Zone zone, int turns, int tick = HeightTick)
        {
            for (int i = 0; i < turns; i++)
                BeatingGlareSystem.OnPlayerTurnEnd(player, zone, tick);
        }

        [Test]
        public void TenExposedHeightTurns_Parches()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);

            Expose(t, zone, BeatingGlareSystem.ExposureTurnsPerStack - 1);
            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "nine is not ten");

            Expose(t, zone, 1);
            Assert.IsTrue(t.HasEffect<ParchedEffect>(), "ten consecutive exposed turns parch");
        }

        [Test]
        public void AtDawn_TheSunIsKind()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);

            Expose(t, zone, 30, tick: DawnTick);

            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "only the Height band burns");
        }

        [Test]
        public void InteriorCells_AreShade()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone, 10, 10);
            zone.GetCell(10, 10).IsInterior = true;

            Expose(t, zone, 30);

            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "shade is architecture");
        }

        [Test]
        public void AHatIsShadeYouCarry()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            var body = new Body();
            t.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            var cap = _factory.CreateEntity("LeatherCap");
            Assert.IsNotNull(cap, "LeatherCap should exist");
            body.GetPartByType("Head").SetEquipped(cap);
            Assert.IsTrue(BeatingGlareSystem.HasHeadCover(t), "the helper sees the cap");

            Expose(t, zone, 30);

            Assert.IsFalse(t.HasEffect<ParchedEffect>());
        }

        [Test]
        public void ShadeResetsTheStreak_NotPausesIt()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone, 10, 10);
            var cell = zone.GetCell(10, 10);

            Expose(t, zone, 9);
            cell.IsInterior = true;      // one turn in the tent
            Expose(t, zone, 1);
            cell.IsInterior = false;
            Expose(t, zone, 9);

            Assert.IsFalse(t.HasEffect<ParchedEffect>(),
                "9 + shade + 9 is not 10 consecutive — shade is relief, not a pause button");

            Expose(t, zone, 1);
            Assert.IsTrue(t.HasEffect<ParchedEffect>());
        }

        [Test]
        public void TheSpreadsSun_DoesNotParch()
        {
            var zone = new Zone("Overworld.10.10.0");   // Sill — the Spread
            var t = Traveller(zone);

            Expose(t, zone, 30);

            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "the glare is the Beating's own");
        }

        // ════════════════════════════════════════════════════════
        // The cures with agency
        // ════════════════════════════════════════════════════════

        [Test]
        public void DrawingWaterAtAWell_CuresEverything()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);

            var well = _factory.CreateEntity("Well");
            zone.AddEntity(well, 11, 10);
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "DrawWaterAtWell");
            e.SetParameter("Actor", (object)t);
            well.FireEvent(e);
            e.Release();

            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "the well is the cure the pan is the cause of");
            Assert.AreEqual(0, t.GetStat("Strength").Penalty);
        }

        [Test]
        public void DrawingWater_WhileFine_IsJustADrink()
        {
            // Counter-check: no effect present → no crash, still a message.
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            var well = _factory.CreateEntity("Well");
            zone.AddEntity(well, 11, 10);

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "DrawWaterAtWell");
            e.SetParameter("Actor", (object)t);
            Assert.DoesNotThrow(() => well.FireEvent(e));
            e.Release();

            StringAssert.Contains("cool", MessageLog.GetLast() ?? "");
        }

        [Test]
        public void DrinkingThroughTheActualTonic_PushesTheParchBack()
        {
            // W2 mid-review: the wiring test went straight to
            // ReduceOneStack and never through TonicPart's own dispatch —
            // a broken hook would have passed. This one drinks for real.
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            t.AddPart(new InventoryPart());
            t.ApplyEffect(new ParchedEffect(), null, zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);

            var tonic = _factory.CreateEntity("WaterTonic");
            t.GetPart<InventoryPart>().AddObject(tonic);
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "ApplyTonic");
            e.SetParameter("Actor", (object)t);
            e.SetParameter("Zone", (object)zone);
            tonic.FireEvent(e);
            e.Release();

            var parched = t.GetPart<StatusEffectsPart>().GetEffect<ParchedEffect>();
            Assert.IsNotNull(parched, "two stacks minus one drink = one stack");
            Assert.AreEqual(1, parched.Stacks);
        }

        [Test]
        public void TheBandBoundary_IsExact()
        {
            // The spec'd 299/300 pin (plan §7.6 W2.3): Dawn's last tick
            // does not burn; Height's first does; Height's last does;
            // Dusk's first does not.
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);

            Expose(t, zone, 30, tick: 299);
            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "tick 299 is still Dawn");

            BeatingGlareSystem.ResetForTests();
            Expose(t, zone, BeatingGlareSystem.ExposureTurnsPerStack, tick: 300);
            Assert.IsTrue(t.HasEffect<ParchedEffect>(), "tick 300 is Height");

            t.GetPart<StatusEffectsPart>().RemoveEffect<ParchedEffect>();
            BeatingGlareSystem.ResetForTests();
            Expose(t, zone, BeatingGlareSystem.ExposureTurnsPerStack, tick: 599);
            Assert.IsTrue(t.HasEffect<ParchedEffect>(), "tick 599 is still Height");

            t.GetPart<StatusEffectsPart>().RemoveEffect<ParchedEffect>();
            BeatingGlareSystem.ResetForTests();
            Expose(t, zone, 30, tick: 600);
            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "tick 600 is Dusk");
        }

        [Test]
        public void TheWaterCure_ReportsItsRealCause()
        {
            // Effect.cs contract: self-ending effects overwrite
            // LastRemovalCause before zeroing Duration — a never-expires
            // effect must never report "duration_expired".
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone, 10, 10);
            t.ApplyEffect(new ParchedEffect(), null, zone);
            var parched = t.GetPart<StatusEffectsPart>().GetEffect<ParchedEffect>();

            zone.TileState.WriteCoating(10, 10, "water", 6);
            EndTurn(t, zone);

            Assert.AreEqual("cured_by_water", parched.LastRemovalCause);
        }

        [Test]
        public void TheProductionReset_ClearsAStaleStreak()
        {
            // W2 mid-review: nine exposed turns from before a death must
            // not carry into a loaded game. Reset() is what bootstrap and
            // ApplyLoadedGame call.
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            Expose(t, zone, BeatingGlareSystem.ExposureTurnsPerStack - 1);

            BeatingGlareSystem.Reset();   // the load boundary
            Expose(t, zone, 1);

            Assert.IsFalse(t.HasEffect<ParchedEffect>(),
                "9 pre-load + 1 post-load is not 10 consecutive");
        }

        [Test]
        public void ADrink_PushesTheParchBackOneStack()
        {
            var zone = new Zone(BeatingZoneId);
            var t = Traveller(zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);
            t.ApplyEffect(new ParchedEffect(), null, zone);

            ParchedEffect.ReduceOneStack(t);

            var parched = t.GetPart<StatusEffectsPart>().GetEffect<ParchedEffect>();
            Assert.IsNotNull(parched, "two stacks minus one is still parched");
            Assert.AreEqual(1, parched.Stacks);
            Assert.AreEqual(1, t.GetStat("Strength").Penalty);

            ParchedEffect.ReduceOneStack(t);
            Assert.IsFalse(t.HasEffect<ParchedEffect>(), "the last stack removes the effect whole");
            Assert.AreEqual(0, t.GetStat("Strength").Penalty);
        }
    }
}
