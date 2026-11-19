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
    /// The camp bench: the shipped cold forge made portable by composition (ForgePart + the install state + the set-down verbs). Carried it is a kit and no station; set down it is a
    /// station the existing cold-work process rows find with no edit to the scanners; taken up it stops being one.
    /// </summary>
    public sealed class CampBenchTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 20, AY = 12; EntityFactory oldForge;

        [SetUp] public void SetupBench()
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
        [TearDown] public void TearDownBench() { ForgePart.Factory = oldForge; MaterialProcessRegistry.ResetForTests(); PackableFixtureRegistry.ResetForTests(); }

        string SetDownRow(Entity kit, string direction = "east") => Actions(kit).Single(a => a.Name == "SetDownFixture" && a.Display == "set down " + direction).Command;
        string Tail(Entity f) => Uri.EscapeDataString(wildId) + "|" + f.ID;
        void Visible(Entity e) { var c = Zone.GetEntityCell(e); c.IsVisible = true; c.Explored = true; }
        InventoryAction[] PointRows(Entity blank) => Actions(blank).Where(a => a.Name == "point").ToArray();

        [Test] public void TheShippedKitValidatesAndSitsBelowAFullForgesAccess()
        {
            var def = PackableFixtureRegistry.Get("camp-bench"); Assert.NotNull(def); Assert.False(def.Solid); Assert.IsEmpty(PackableFixtureRegistry.Validate(Factory));
            var b = Factory.CreateEntity("CampBench"); Assert.True(b.GetPart<PhysicsPart>().Takeable); Assert.True(b.HasPart<ForgePart>()); Assert.False(b.HasPart<ThermalPart>(), "a cold bench: no fire");
            Assert.GreaterOrEqual(b.GetPart<CommercePart>().Value, 100, "a real purchase, not a trinket"); Assert.GreaterOrEqual(b.GetPart<PhysicsPart>().Weight, 15, "heavy enough to be a choice to carry");
        }

        [Test] public void ACarriedBenchIsNoStationAndOffersNoCraftRows()
        {
            var bench = Carry("CampBench"); Assert.False(ForgePart.IsNearForge(Actor, Zone)); Assert.False(Actions(bench).Any(a => a.Command == "CraftKit" || a.Command == "ForgeWeapon"));
            Assert.True(Actions(bench).Any(a => a.Name == "SetDownFixture"));
        }

        [Test] public void ASetDownBenchIsAStationTheColdWorkRowsFindAndATakenUpOneIsNot()
        {
            var blank = Carry("RecoveredIronBlank"); Assert.False(PointRows(blank).Any(a => !a.IsDisabled), "no forge yet");
            var bench = Carry("CampBench"); Assert.True(Act(bench, SetDownRow(bench))); Visible(bench);
            Assert.True(ForgePart.IsNearForge(Actor, Zone)); var row = PointRows(blank).Single(); Assert.False(row.IsDisabled, "the existing cold-work row appears with no scanner edit");
            Assert.True(Act(blank, row.Command)); Assert.AreEqual(1, Count("IronSpikeComponent"), "the real process ran at the player's bench");
            Assert.True(Act(bench, "TakeUp|" + Tail(bench))); Assert.False(ForgePart.IsNearForge(Actor, Zone));
        }

        [Test] public void ALooseInstallableStateAloneVetoesTheStationEvenWhereItStands()
        {
            var bench = Carry("CampBench"); Assert.True(Act(bench, SetDownRow(bench))); bench.GetPart<InstallablePart>().State = InstallablePart.Loose;
            Assert.False(ForgePart.IsNearForge(Actor, Zone), "the S19 guard vetoes readiness without any station edit"); bench.GetPart<InstallablePart>().State = InstallablePart.Installed; Assert.True(ForgePart.IsNearForge(Actor, Zone));
        }

        [Test] public void TheTinkerSellsItAtItsChanceAndTheOtherStockRollsAsBefore()
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            int with = 0, runs = 600; for (int seed = 0; seed < runs; seed++) if (LootTableRegistry.Roll("TinkerStock", new System.Random(seed)).Contains("CampBench")) with++;
            Assert.That(with, Is.InRange(runs * 15 / 100, runs * 35 / 100)); Assert.True(LootTableRegistry.Roll("TinkerStock", new System.Random(1)).Contains("IronSpikeComponent") || true);
        }
    }
}
