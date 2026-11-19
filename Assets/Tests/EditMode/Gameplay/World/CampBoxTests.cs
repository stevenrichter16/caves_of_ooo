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
    /// The camp box, the first kit that ships with the set-down verbs (Docs/HOME-BASE-IMPLEMENTATION.md, PB2b). Content tests run against the SHIPPED data; the verb tests use the real
    /// blueprint. A glyph-only item: no art ships with it, and that is recorded debt rather than a claim.
    /// </summary>
    public sealed class CampBoxTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 20, AY = 12;

        [SetUp] public void SetupBox()
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
        [TearDown] public void TearDownBox() { PackableFixtureRegistry.ResetForTests(); }

        // ── shipped content ──

        [Test] public void TheShippedFixtureFileHasTheCampBoxAndEveryRowValidates()
        {
            var def = PackableFixtureRegistry.Get("camp-box"); Assert.NotNull(def); Assert.AreEqual("CampBox", def.Blueprint); Assert.AreEqual(PackableFixtureDefinition.Carried, def.Class);
            Assert.True(def.Solid); Assert.AreEqual(PlacementCheck.OpenGround, def.GroundClass);
            Assert.IsEmpty(PackableFixtureRegistry.Validate(Factory), "every shipped row passes the blueprint-level validator (P07 to P13)");
        }

        [Test] public void TheBlueprintIsATakeableContainerThatWeighsItsContentsAndCarriesItsKind()
        {
            var box = Factory.CreateEntity("CampBox"); var p = box.GetPart<PhysicsPart>();
            Assert.True(p.Takeable); Assert.False(p.Solid); Assert.True(box.HasPart<ContainerPart>()); Assert.True(box.HasPart<ContainerLoadPart>(), "its contents must weigh");
            Assert.AreEqual("camp-box", box.GetPart<PackableFixturePart>().Kind); Assert.True(box.HasTag("Item")); Assert.False(box.HasTag("Owned")); Assert.False(box.HasPart<InstallablePart>(), "a new kit is not installed");
            Assert.False(box.HasPart<InstallationMarkerPart>()); Assert.Greater(box.GetPart<CommercePart>().Value, 0); Assert.False(string.IsNullOrWhiteSpace(box.GetPart<ExaminablePart>().Description));
        }

        [Test] public void TheQuartermasterStockRollsTheBoxAboutAsOftenAsItsChance()
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            int withBox = 0, runs = 600;
            for (int seed = 0; seed < runs; seed++) if (LootTableRegistry.Roll("QuartermasterStock", new System.Random(seed)).Contains("CampBox")) withBox++;
            Assert.That(withBox, Is.InRange(runs * 25 / 100, runs * 45 / 100), "a 35% line, never a staple and never absent");
        }

        [Test] public void TheStockLineLeavesTheOtherQuartermasterEntriesRollingExactlyAsBefore()
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            var table = File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json"));
            for (int seed = 0; seed < 50; seed++)
            {
                var rolled = LootTableRegistry.Roll("QuartermasterStock", new System.Random(seed)).Where(b => b != "CampBox").ToList();
                var again = LootTableRegistry.Roll("QuartermasterStock", new System.Random(seed)).Where(b => b != "CampBox").ToList();
                CollectionAssert.AreEqual(rolled, again);
            }
            Assert.That(table, Does.Contain("\"Blueprint\": \"CampBox\""));
        }

        [Test] public void AQuartermasterCanSellItAndAPlayerPaysItsPrice()
        {
            var box = Factory.CreateEntity("CampBox"); int price = TradeSystem.GetBuyPrice(box, 1.0); Assert.Greater(price, 0); Assert.LessOrEqual(price, 120, "a modest purchase, not a base-in-a-box windfall");
        }

        // ── the real kit through every verb ──

        string SetDownRow(Entity kit, string direction = "east") => Actions(kit).Single(a => a.Name == "SetDownFixture" && a.Display == "set down " + direction).Command;
        string Tail(Entity f) => Uri.EscapeDataString(wildId) + "|" + f.ID;

        [Test] public void ABoughtBoxCanBeSetDownFilledTakenUpAndSetDownElsewhereWithItsContents()
        {
            var box = Carry("CampBox"); string id = box.ID; Assert.True(Act(box, SetDownRow(box))); Assert.True(box.GetPart<PhysicsPart>().Solid);
            var dagger = Factory.CreateEntity("Dagger"); Assert.True(box.GetPart<ContainerPart>().AddItem(dagger));
            Assert.True(Act(box, "TakeUp|" + Tail(box))); Assert.AreSame(box, Pack.Objects.Single(e => e.ID == id)); CollectionAssert.Contains(box.GetPart<ContainerPart>().Contents, dagger);
            Assert.True(Act(box, SetDownRow(box, "west"))); Assert.AreSame(box, wild.GetCell(AX - 1, AY).Objects.Single(e => e.ID == id)); CollectionAssert.Contains(box.GetPart<ContainerPart>().Contents, dagger);
            Assert.AreEqual(1, PlacementCheck.CountInZone(wild));
        }

        [Test] public void ALoadedBoxWeighsItsContentsSoItCannotBeCarriedPastTheLimitAndNothingSpills()
        {
            var box = Carry("CampBox"); Assert.True(Act(box, SetDownRow(box)));
            var heavy = Factory.CreateEntity("Dagger"); heavy.GetPart<PhysicsPart>().Weight = 150; Assert.True(box.GetPart<ContainerPart>().AddItem(heavy));
            Assert.Greater(InventoryPart.GetItemWeight(box), 150); Pack.MaxWeight = 100;
            Assert.False(Act(box, "TakeUp|" + Tail(box))); CollectionAssert.Contains(box.GetPart<ContainerPart>().Contents, heavy); Assert.AreSame(box, wild.GetCell(AX + 1, AY).Objects.Single(e => e.ID == box.ID));
        }

        [Test] public void AStandingBoxSurvivesASaveRoundTripWithItsContentsAndIsStillTheSameEntity()
        {
            var box = Carry("CampBox"); Assert.True(Act(box, SetDownRow(box))); var dagger = Factory.CreateEntity("Dagger"); box.GetPart<ContainerPart>().AddItem(dagger);
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(box); Assert.AreEqual(box.ID, copy.ID); Assert.True(InstallationMarkerPart.Retains(copy));
            Assert.AreEqual(1, copy.GetPart<ContainerPart>().Contents.Count); Assert.AreEqual("Dagger", copy.GetPart<ContainerPart>().Contents[0].BlueprintName);
        }

        [Test] public void AnOrdinaryChestIsNeverOfferedTheVerbs_Counter()
        {
            var chest = Place("Chest", AX + 1, AY); Assert.False(Actions(chest).Any(a => a.Name.Contains("Fixture")));
            var plainBox = Carry("CampBox"); Assert.True(Actions(plainBox).Any(a => a.Name == "SetDownFixture"));
        }

        [Test] public void ADestroyedStandingBoxTakesItsMarkerWithIt_NothingIsDestroyedByAbsence()
        {
            var box = Carry("CampBox"); Assert.True(Act(box, SetDownRow(box))); Assert.AreEqual(1, PlacementCheck.CountInZone(wild));
            wild.RemoveEntity(box); Assert.AreEqual(0, PlacementCheck.CountInZone(wild), "the hold is the marker, so it dies with the object; there is no ledger to correct");
        }
    }
}
