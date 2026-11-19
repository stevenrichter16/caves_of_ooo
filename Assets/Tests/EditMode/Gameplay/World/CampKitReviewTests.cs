using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Findings of an independent read of the camp kits (2026-11-19), each confirmed against the code before it was written down: a packable kit that is dropped or taken up
    /// was still a station, a burning hearth that was taken up kept its flame forever, and a toggled hearth light did not reach the light map until something else moved.
    /// </summary>
    public sealed class CampKitReviewTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 20, AY = 12; EntityFactory oldForge;

        [SetUp] public void SetupReview()
        {
            oldForge = ForgePart.Factory; ForgePart.Factory = Factory; MaterialProcessRegistry.ResetForTests(); PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild;
        }
        [TearDown] public void TearDownReview() { ForgePart.Factory = oldForge; MaterialProcessRegistry.ResetForTests(); PackableFixtureRegistry.ResetForTests(); }

        string SetDownRow(Entity kit, string direction = "east") => Actions(kit).Single(a => a.Name == "SetDownFixture" && a.Display == "set down " + direction).Command;
        string Tail(Entity f) => Uri.EscapeDataString(wildId) + "|" + f.ID;
        void Visible(Entity e) { var c = Zone.GetEntityCell(e); c.IsVisible = true; c.Explored = true; }
        void Turn(Entity e) { var ev = GameEvent.New("EndTurn"); e.FireEvent(ev); ev.Release(); }
        void Drop(Entity item) { var r = InventorySystem.ExecuteCommand(new DropCommand(item), Actor, Zone); Assert.True(r.Success, "the ordinary drop works: " + r.ErrorMessage); Assert.NotNull(Zone.GetEntityCell(item), "it lies on the ground"); }

        // ── a kit that is not set down is no station: carried, dropped, or taken up ──

        [Test] public void ADroppedCampBenchIsNoForgeAndOffersNoCraftRows()
        {
            var bench = Carry("CampBench"); Drop(bench); Visible(bench);
            Assert.False(ForgePart.IsNearForge(Actor, Zone), "a bench that was never set down is a kit, not a station");
            Assert.False(Actions(bench).Any(a => a.Command == "CraftKit" || a.Command == "ForgeWeapon"), "and it does not list work it would refuse");
        }

        [Test] public void ADroppedCampStillIsNoStillAndOffersNoBrewRows()
        {
            var still = Carry("CampStill"); Drop(still);
            Assert.False(AlchemyStillPart.IsNearStill(Actor, Zone)); Assert.False(Actions(still).Any(a => a.Command == "BrewMix"));
        }

        [Test] public void ATakenUpThenDroppedCampStillIsNoStillEither()
        {
            var still = Carry("CampStill"); Assert.True(Act(still, SetDownRow(still))); Assert.True(AlchemyStillPart.IsNearStill(Actor, Zone), "set down it is a still");
            Assert.True(Act(still, "TakeUp|" + Tail(still))); Drop(still);
            Assert.False(AlchemyStillPart.IsNearStill(Actor, Zone), "the Loose state it was left in must veto the presence rule too");
        }

        [Test] public void ASetDownAndAHandedOverKitStayStations_Counter()
        {
            var bench = Carry("CampBench"); Assert.True(Act(bench, SetDownRow(bench))); Visible(bench); Assert.True(ForgePart.IsNearForge(Actor, Zone));
            var still = Carry("CampStill"); Assert.True(Act(still, SetDownRow(still, "west"))); Assert.True(AlchemyStillPart.IsNearStill(Actor, Zone));
            Assert.True(Act(still, "HandOver|" + Tail(still))); Assert.True(AlchemyStillPart.IsNearStill(Actor, Zone), "a handed-over still is an ordinary standing still");
        }

        [Test] public void AnAuthoredVillageStillWithNoKitPartsStillWorks_Counter()
        {
            var still = Factory.CreateEntity("AlchemyStill"); Assert.False(still.HasPart<PackableFixturePart>()); Assert.True(Zone.AddEntity(still, AX + 1, AY));
            Assert.True(AlchemyStillPart.IsNearStill(Actor, Zone), "the shipped communal still is untouched by the kit rule");
        }

        // ── a burning hearth that is taken up is put out; its fuel stays ──

        [Test] public void TakingUpABurningHearthPutsTheFireOutAndKeepsTheFuel()
        {
            var h = Carry("CampHearth"); Assert.True(Act(h, SetDownRow(h)));
            var fuel = h.GetPart<FuelPart>(); var heat = h.GetPart<ThermalPart>(); var light = h.GetPart<LightSourcePart>();
            fuel.FuelMass = 10; heat.Temperature = 450; Turn(h); Assert.True(light.Enabled, "burning"); float before = fuel.FuelMass;
            Assert.True(Act(h, "TakeUp|" + Tail(h)));
            Assert.AreEqual(before, fuel.FuelMass, 0.001f, "the timber is still in it"); Assert.Less(heat.Temperature, CookingService.MinimumFiniteCookingTemperature, "the flame is out");
            Assert.False(light.Enabled, "and a carried pan lights nothing");
        }

        [Test] public void ASetDownHearthThatStaysDownKeepsBurning_Counter()
        {
            var h = Carry("CampHearth"); Assert.True(Act(h, SetDownRow(h))); h.GetPart<FuelPart>().FuelMass = 10; h.GetPart<ThermalPart>().Temperature = 450; Turn(h);
            Assert.True(h.GetPart<LightSourcePart>().Enabled); Assert.True(h.GetPart<ThermalPart>().Temperature >= CookingService.MinimumFiniteCookingTemperature);
        }

        // ── a toggled hearth light reaches the light map without anything else moving ──

        [Test] public void LightingAHearthIsSeenByTheLightMapWithNoOtherChange()
        {
            var h = Carry("CampHearth"); Assert.True(Act(h, SetDownRow(h))); wild.AmbientLevel = 0.1f; var cell = Zone.GetEntityCell(h);
            var map = new LightMap(); map.Compute(wild); float dark = map.GetBrightness(cell.X, cell.Y);
            h.GetPart<FuelPart>().FuelMass = 10; h.GetPart<ThermalPart>().Temperature = 450; Turn(h); Assert.True(h.GetPart<LightSourcePart>().Enabled);
            map.Compute(wild); Assert.Greater(map.GetBrightness(cell.X, cell.Y), dark, "the light map must recompute when the hearth lights, not when something next moves");
        }

        [Test] public void AHearthThatBurnsOutIsDarkOnTheLightMapWithNoOtherChange()
        {
            var h = Carry("CampHearth"); Assert.True(Act(h, SetDownRow(h))); wild.AmbientLevel = 0.1f; var cell = Zone.GetEntityCell(h);
            h.GetPart<FuelPart>().FuelMass = 10; h.GetPart<ThermalPart>().Temperature = 450; Turn(h); var map = new LightMap(); map.Compute(wild); float lit = map.GetBrightness(cell.X, cell.Y);
            h.GetPart<FuelPart>().FuelMass = 0; Turn(h); Assert.False(h.GetPart<LightSourcePart>().Enabled);
            map.Compute(wild); Assert.Less(map.GetBrightness(cell.X, cell.Y), lit);
        }
    }
}
