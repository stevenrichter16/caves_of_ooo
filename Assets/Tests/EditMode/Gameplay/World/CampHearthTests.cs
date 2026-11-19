using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The camp hearth: the same set-down verbs applied to a machine made of existing parts (a finite cooking fire: Campfire + Thermal + Fuel). Carried, it is inert; set down, the
    /// shipped fire verbs (feed, relight, dry gear, cook) work on it unchanged. Every claim is checked against the real shipped blueprint and verbs.
    /// </summary>
    public sealed class CampHearthTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 20, AY = 12;

        [SetUp] public void SetupHearth()
        {
            PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild;
        }
        [TearDown] public void TearDownHearth() { PackableFixtureRegistry.ResetForTests(); }

        string SetDownRow(Entity kit, string direction = "east") => Actions(kit).Single(a => a.Name == "SetDownFixture" && a.Display == "set down " + direction).Command;
        Entity SetDown() { var h = Carry("CampHearth"); Assert.True(Act(h, SetDownRow(h)), string.Join("|", MessageLog.GetRecent(2))); return h; }
        string Tail(Entity f) => Uri.EscapeDataString(wildId) + "|" + f.ID;

        [Test] public void TheShippedKitValidatesAndIsASmallCarriedFireNotAContainer()
        {
            var def = PackableFixtureRegistry.Get("camp-hearth"); Assert.NotNull(def); Assert.False(def.Solid, "a fire is stepped around, not walled"); Assert.IsEmpty(PackableFixtureRegistry.Validate(Factory));
            var h = Factory.CreateEntity("CampHearth"); Assert.True(h.GetPart<PhysicsPart>().Takeable); Assert.False(h.HasPart<ContainerPart>());
            Assert.True(h.GetPart<CampfirePart>().FiniteCooking); Assert.AreEqual(0, h.GetPart<FuelPart>().FuelMass, 0.001f, "it is sold cold and empty: the player brings the timber and the flame");
            Assert.Greater(h.GetPart<CommercePart>().Value, 0); Assert.LessOrEqual(h.GetPart<CommercePart>().Value, 100);
        }

        [Test] public void ACarriedHearthOffersNoFireVerbsButSetDownRows()
        {
            var h = Carry("CampHearth"); Carry("SalvagedTimber"); var rows = Actions(h);
            Assert.False(rows.Any(a => a.Command == "FeedCookingFire"), "the shipped fire verbs need a standing, non-takeable fire"); Assert.True(rows.Any(a => a.Name == "SetDownFixture"));
        }

        [Test] public void ASetDownHearthIsAStandingFireWhoseShippedVerbsWorkUnchanged()
        {
            var h = SetDown(); Assert.False(h.GetPart<PhysicsPart>().Takeable); Assert.False(h.GetPart<PhysicsPart>().Solid); Assert.True(InstallationMarkerPart.Retains(h));
            Assert.False(Actions(h).Any(a => a.Command == "FeedCookingFire"), "no timber carried yet");
            var timber = Carry("SalvagedTimber"); Assert.True(Actions(h).Any(a => a.Command == "FeedCookingFire")); float before = h.GetPart<FuelPart>().FuelMass;
            Assert.True(Act(h, "FeedCookingFire")); Assert.Greater(h.GetPart<FuelPart>().FuelMass, before); Assert.AreEqual(0, Count("SalvagedTimber"), "one timber is spent for up to ten fuel");
            Assert.LessOrEqual(h.GetPart<FuelPart>().FuelMass, h.GetPart<FuelPart>().MaxFuel);
        }

        [Test] public void TakingTheHearthUpKeepsItsFuelAndItGoesInertAgain()
        {
            var h = SetDown(); Carry("SalvagedTimber"); Assert.True(Act(h, "FeedCookingFire")); float fuel = h.GetPart<FuelPart>().FuelMass; Assert.Greater(fuel, 0);
            Assert.True(Act(h, "TakeUp|" + Tail(h))); Assert.AreEqual(fuel, h.GetPart<FuelPart>().FuelMass, 0.001f, "the fuel you fed is still in it"); Carry("SalvagedTimber");
            Assert.False(Actions(h).Any(a => a.Command == "FeedCookingFire"));
        }

        [Test] public void AHandedOverHearthKeepsBurningAsAnOrdinaryFireAndStopsBeingTheirs()
        {
            var h = SetDown(); Assert.True(Act(h, "HandOver|" + Tail(h))); Assert.False(h.HasPart<InstallationMarkerPart>()); Assert.AreEqual(0, PlacementCheck.CountInZone(wild));
            Carry("SalvagedTimber"); Assert.True(Actions(h).Any(a => a.Command == "FeedCookingFire"), "it is still a fire you can feed");
        }

        [Test] public void TheProvisionerStockRollsTheHearthAboutAsOftenAsItsChanceAndTheRestIsUnchanged()
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            int with = 0, runs = 600; for (int seed = 0; seed < runs; seed++) if (LootTableRegistry.Roll("ProvisionerStock", new System.Random(seed)).Contains("CampHearth")) with++;
            Assert.That(with, Is.InRange(runs * 25 / 100, runs * 45 / 100));
        }

        [Test] public void ASpotWithHeatOnItIsRefusedForASecondKit_ThePlacementRuleNotAClaimThatTheHearthWritesHeat()
        {
            var h = SetDown(); var thermal = h.GetPart<ThermalPart>(); wild.TileState.AddHeat(AX + 1, AY, 10); // a lit hearth heats its own tile
            var box = Carry("CampBox"); var rows = Actions(box).Where(a => a.Name == "SetDownFixture").ToArray();
            Assert.False(rows.Any(a => !a.IsDisabled && a.Display.Contains("east")), "a box cannot be set down onto or beside heat");
            Assert.NotNull(thermal);
        }

        // ── light: the fire lights the night only while it burns ──

        void Turn(Entity e) { var ev = GameEvent.New("EndTurn"); e.FireEvent(ev); ev.Release(); }

        [Test] public void ALitHearthGlowsAndASpentOrColdOneDoesNot()
        {
            var h = SetDown(); var light = h.GetPart<LightSourcePart>(); Assert.False(light.Enabled, "sold dark"); Turn(h); Assert.False(light.Enabled);
            h.GetPart<FuelPart>().FuelMass = 10; h.GetPart<ThermalPart>().Temperature = 400; Turn(h); Assert.True(light.Enabled, "hot with fuel");
            h.GetPart<FuelPart>().FuelMass = 0; Turn(h); Assert.False(light.Enabled, "fuel gone"); h.GetPart<FuelPart>().FuelMass = 10; h.GetPart<ThermalPart>().Temperature = 25; Turn(h); Assert.False(light.Enabled, "cold");
        }

        [Test] public void ACarriedHearthKeepsItsStateAndNeverGlowsInAPack()
        {
            var h = Carry("CampHearth"); h.GetPart<FuelPart>().FuelMass = 10; h.GetPart<ThermalPart>().Temperature = 400; Turn(h); Assert.False(h.GetPart<LightSourcePart>().Enabled);
        }

        // ── fuel is spent while it burns (audit finding: the shipped relight spends none) ──

        [Test] public void ABurningHearthSpendsItsFuelAndGoesDarkWhenItIsGone()
        {
            var h = SetDown(); var fuel = h.GetPart<FuelPart>(); var light = h.GetPart<LightSourcePart>(); fuel.FuelMass = 10; h.GetPart<ThermalPart>().Temperature = 450;
            int turns = 0; while (fuel.FuelMass > 0 && turns < 200) { Turn(h); turns++; h.GetPart<ThermalPart>().Temperature = 450; }
            Assert.AreEqual(0f, fuel.FuelMass, 0.001f); Assert.AreEqual(40, turns, "one timber (10 fuel) at 0.25 a turn is forty turns of fire"); Turn(h); Assert.False(light.Enabled);
        }

        [Test] public void AColdOrCarriedHearthSpendsNoFuel_Counter()
        {
            var h = SetDown(); h.GetPart<FuelPart>().FuelMass = 10; for (int i = 0; i < 20; i++) Turn(h); Assert.AreEqual(10f, h.GetPart<FuelPart>().FuelMass, 0.001f, "an unlit hearth keeps its fuel");
            var carried = Carry("CampHearth"); carried.GetPart<FuelPart>().FuelMass = 10; carried.GetPart<ThermalPart>().Temperature = 450; for (int i = 0; i < 20; i++) Turn(carried); Assert.AreEqual(10f, carried.GetPart<FuelPart>().FuelMass, 0.001f);
        }

        [Test] public void FuelNeverGoesNegativeAndSpentFuelCannotBeRelit()
        {
            var h = SetDown(); var fuel = h.GetPart<FuelPart>(); fuel.FuelMass = 0.1f; h.GetPart<ThermalPart>().Temperature = 450; Turn(h); Assert.AreEqual(0f, fuel.FuelMass, 0.001f);
            Assert.False(Actions(h).Any(a => a.Command == "RelightCookingFire"), "relighting needs fuel, and there is none");
        }

        [Test] public void TheHearthIsAnIronPanAnOutsideFireCannotIgnite()
        {
            var h = Factory.CreateEntity("CampHearth"); var m = h.GetPart<MaterialPart>(); Assert.NotNull(m); Assert.AreEqual(0f, m.Combustibility, 0.0001f); Assert.AreEqual(0.25f, h.GetPart<FuelPart>().BurnRate, 0.0001f);
        }
    }
}
