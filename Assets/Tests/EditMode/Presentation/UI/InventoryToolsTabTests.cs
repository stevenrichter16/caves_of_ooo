using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        [TestCase("Scraper", "Gathering & processing")]
        [TestCase("HandSieve", "Gathering & processing")]
        [TestCase("CampSettlingTray", "Gathering & processing")]
        [TestCase("Hatchet", "Gathering & processing")]
        [TestCase("CampBench", "Crafting & repair")]
        [TestCase("CampTrough", "Crafting & repair")]
        [TestCase("CampStill", "Brewing & treatment")]
        [TestCase("LiquidFlask", "Water & liquids")]
        [TestCase("HandPump", "Water & liquids")]
        [TestCase("SteamCondenser", "Water & liquids")]
        [TestCase("Torch", "Fire & light")]
        [TestCase("FlintStriker", "Fire & light")]
        [TestCase("CampHearth", "Camp & hauling")]
        [TestCase("CargoSled", "Camp & hauling")]
        [TestCase("SoundingRod", "Exploration & surveying")]
        [TestCase("HandBellows", "Exploration & surveying")]
        [TestCase("HushStrip", "Exploration & surveying")]
        [TestCase("SpringTrap", "Traps & fishing")]
        [TestCase("CampFishTrap", "Traps & fishing")]
        public void ToolsUseSectionsNameTheWorkOfAuthoredTools(string blueprint, string heading)
        {
            using var r = new Rig();
            CountedCarry(r, blueprint);
            r.Open(); r.Tab(6);
            Assert.That(r.Labels().Count(label => label.Content == heading), Is.EqualTo(1));
            Assert.That(r.Hits().Count(hit => Kind(hit) == "ToolRow"), Is.EqualTo(1));
        }

        [Test]
        public void ToolsUseSectionsTogetherKeepPackedAndPlacedTrayAndTheirExactOwners()
        {
            using var input = new NativeNavigationScope();
            using var r = new Rig();
            CountedCarry(r, "CampSettlingTray");
            CountedCarry(r, "CampSettlingTray");
            CountedCarry(r, "LiquidFlask");
            var nearby = PlaceToolsFixture(r, "CampSettlingTray", 11, 10, true);

            r.Open(); r.Tab(6);
            var heading = r.Labels().Single(label => label.Content == "Gathering & processing");
            var nextHeading = r.Labels().Single(label => label.Content == "Water & liquids");
            Assert.That(r.HitAtScreen(r.ToScreen(r.BoundsOf(heading).center)), Is.EqualTo(-1));
            var hits = r.Hits().Where(hit => Kind(hit) == "ToolRow").ToArray();
            Assert.That(hits.Length, Is.EqualTo(3));
            Assert.That(Bounds(hits[0]).y, Is.GreaterThan(r.BoundsOf(heading).y));
            Assert.That(Bounds(hits[1]).yMax, Is.LessThanOrEqualTo(r.BoundsOf(nextHeading).y));
            Assert.That(r.Labels().Any(label => label.Content.Contains("settling tray x2")), Is.True);

            input.Keys(Key.DownArrow);
            r.UI.HandleInput();
            Assert.That(Get(r.UI, "_selectedTool"), Is.SameAs(nearby));
            r.ActivatePainted(r.Hits().Single(hit => Kind(hit) == "PackGrouping"));
            Assert.That(Get(r.UI, "_selectedTool"), Is.SameAs(nearby));
            Assert.That(r.Hits().Count(hit => Kind(hit) == "ToolRow"), Is.EqualTo(4));
            Assert.False(r.UI.ConsumePendingEverydayTurn());
        }

        [Test]
        public void ToolsUseSectionsStayWithVisibleRowsWhenScrollingWithinALongSection()
        {
            using var registry = new PublicStateRegistryScope();
            using var input = new NativeNavigationScope();
            using var r = new Rig();
            CountedCarry(r, "Scraper");
            for (int fill = 0; fill < 12; fill++)
                CountedCarry(r, volume: fill, liquid: fill == 0 ? "" : "water");
            var last = CountedCarry(r, "SpringTrap");

            r.Open(); r.Tab(6);
            input.Keys(Key.End);
            r.UI.HandleInput();
            Assert.That(Get(r.UI, "_selectedTool"), Is.SameAs(last));
            Assert.That(r.Labels().Any(label => label.Content == "Water & liquids"), Is.True);
            Assert.That(r.Labels().Any(label => label.Content == "Traps & fishing"), Is.True);
            r.FindHit("ToolRow", 13);
            Assert.That(r.Hits().Where(hit => Kind(hit) == "ToolRow").All(hit => Bounds(hit).yMax < 677), Is.True);
            input.Keys(Key.Home);
            r.UI.HandleInput();
            Assert.That(r.Labels().Any(label => label.Content == "Gathering & processing"), Is.True);
            r.FindHit("ToolRow", 0);
            Assert.False(r.UI.ConsumePendingEverydayTurn());
        }

        [Test]
        public void ToolsUseSectionsFitAtLeastTenToolsFromSeveralSectionsWithoutOverlapping()
        {
            using var r = new Rig();
            foreach (string blueprint in new[]
            {
                "Scraper", "HandSieve", "CampSettlingTray", "Hatchet",
                "CampBench", "CampTrough", "CampStill", "LiquidFlask",
                "HandPump", "SteamCondenser", "Torch", "FlintStriker", "CargoSled"
            })
                CountedCarry(r, blueprint);
            r.Open(); r.Tab(6);
            var rows = r.Hits().Where(hit => Kind(hit) == "ToolRow").Select(Bounds).OrderBy(rect => rect.y).ToArray();
            Assert.That(rows.Length, Is.GreaterThanOrEqualTo(10));
            Assert.That(rows[^1].yMax, Is.LessThan(677));
            for (int i = 1; i < rows.Length; i++)
                Assert.That(rows[i].y, Is.GreaterThanOrEqualTo(rows[i - 1].yMax));
        }

        [Test]
        public void ToolsTabFindsToolsAcrossExistingCategoriesWithoutReclassifyingThePack()
        {
            using var r = new Rig();
            var carried = new[] { "Scraper", "HandSieve", "CampSettlingTray", "HandBellows", "LiquidFlask" }
                .Select(blueprint => CountedCarry(r, blueprint)).ToArray();
            var food = CountedCarry(r, "DriedMeat");
            var state = InventoryScreenData.Build(r.Player);
            var entries = ToolEntries(r, state);

            foreach (var item in carried)
            {
                var display = state.Categories.SelectMany(group => group.Items).Single(row => row.Item == item);
                var entry = ToolEntry(entries, item);
                Assert.That(ToolValue(entry, "Display"), Is.SameAs(display));
                Assert.That(ToolValue(entry, "Name"), Is.EqualTo(item.GetDisplayName()));
            }
            Assert.False(entries.Any(entry => ReferenceEquals(ToolValue(entry, "Item"), food)));
            Assert.That(ItemCategory.GetCategory(carried[2]), Is.EqualTo(ItemCategory.CampEquipment));
            Assert.That(ItemCategory.GetCategory(carried[4]), Is.EqualTo(ItemCategory.Containers));

            r.Open();
            r.Tab(6);
            Assert.That(Get(r.UI, "_panel"), Is.EqualTo(6));
            Assert.True(r.Labels().Any(label => label.Content == "Tools"));
            foreach (var item in carried)
                Assert.True(r.Labels().Any(label => label.Content.Contains(item.GetDisplayName())), item.BlueprintName);
            Assert.False(r.UI.ConsumePendingEverydayTurn());
        }

        [TestCase("Scraper")]
        [TestCase("HandSieve")]
        [TestCase("HandBellows")]
        [TestCase("HandPump")]
        [TestCase("CampSettlingTray")]
        public void ToolsTabAndPackShareCountedRowsForEquivalentTools(string blueprint)
        {
            using var r = new Rig();
            var first = CountedCarry(r, blueprint);
            var second = CountedCarry(r, blueprint);
            r.Open();
            r.Tab(1);
            Assert.AreEqual(2, CountedUnits(CountedRows(r).Single()));

            r.Tab(6);
            var grouped = ((IEnumerable)Get(r.UI, "_toolEntries")).Cast<object>().Single();
            Assert.AreEqual(2L, Get(grouped, "Units"));
            r.ActivatePainted(r.Hits().Single(hit => Kind(hit) == "PackGrouping"));
            Assert.AreEqual(2, ((IEnumerable)Get(r.UI, "_toolEntries")).Cast<object>().Count());
            CollectionAssert.AreEquivalent(new[] { first, second }, r.Pack.Objects);
            Assert.False(r.UI.ConsumePendingEverydayTurn());
        }

        [Test]
        public void ToolsTabKeepsDifferentChargesAndTrayContentsApart()
        {
            using var r = new Rig();
            CountedCarry(r, "DischargeRod");
            CountedCarry(r, "DischargeRod").GetPart<FieldWorkToolPart>().Charge = 1;
            CountedCarry(r, "CampSettlingTray");
            var tray = CountedCarry(r, "CampSettlingTray").GetPart<SettlingTrayPart>();
            tray.Held = 1;
            tray.Element = "heat";

            r.Open(); r.Tab(1);
            Assert.AreEqual(4, CountedRows(r).Length);
            r.Tab(6);
            Assert.AreEqual(4, ((IEnumerable)Get(r.UI, "_toolEntries")).Cast<object>().Count());
        }

        [Test]
        public void ToolsTabReportsScraperAndTrayWorkAsUnavailableWithoutTheRequiredSetup()
        {
            using var registry = new PublicStateRegistryScope();
            using var r = new Rig();
            var scraper = CountedCarry(r, "Scraper");
            var tray = CountedCarry(r, "CampSettlingTray");
            var state = InventoryScreenData.Build(r.Player);
            var entries = ToolEntries(r, state);
            var scraperEntry = ToolEntry(entries, scraper);
            var trayEntry = ToolEntry(entries, tray);

            Assert.False(ToolOperations(scraperEntry).Any(ToolReady));
            var trayWork = ToolOperations(trayEntry);
            Assert.False(trayWork.Any(operation => ToolReady(operation)
                && (((string)ToolValue(operation, "Title")).StartsWith("gather", StringComparison.OrdinalIgnoreCase)
                    || ((string)ToolValue(operation, "Title")).StartsWith("settle", StringComparison.OrdinalIgnoreCase))));
            string guidance = string.Join(" ", (IEnumerable<string>)ToolValue(trayEntry, "Guidance")) + " "
                + string.Join(" ", trayWork.Select(operation => ToolValue(operation, "Title") + " " + ToolValue(operation, "Detail")));
            Assert.That(guidance, Does.Contain("set down").IgnoreCase);
            Assert.That(guidance, Does.Contain("gather").IgnoreCase);
            Assert.That(r.UI.CurrentZone.GetEntityCell(tray), Is.Null);
            Assert.That(tray.GetPart<PhysicsPart>().InInventory, Is.SameAs(r.Player));
        }

        [Test]
        public void ToolsTabKeepsStowedToolsUnavailableAndListsOnlyVisibleNearbyStations()
        {
            using var registry = new PublicStateRegistryScope();
            using var r = new Rig();
            var nearby = PlaceToolsFixture(r, "CampSettlingTray", 11, 10, true);
            var hidden = PlaceToolsFixture(r, "CampSettlingTray", 10, 11, false);
            var distant = PlaceToolsFixture(r, "CampSettlingTray", 20, 10, true);
            var vent = r.Factory.CreateEntity("SteamVent");
            Assert.True(r.UI.CurrentZone.AddEntity(vent, 12, 10));
            var source = r.UI.CurrentZone.GetEntityCell(vent);
            source.IsVisible = source.Explored = true;

            var carried = CountedCarry(r, "Scraper");
            var satchel = CountedCarry(r, "FieldSatchel");
            var stowed = r.Factory.CreateEntity("Scraper");
            Assert.True(satchel.GetPart<ContainerPart>().AddItem(stowed));
            var entries = ToolEntries(r, InventoryScreenData.Build(r.Player));

            Assert.That(ToolValue(ToolEntry(entries, nearby), "Cell"), Is.SameAs(r.UI.CurrentZone.GetEntityCell(nearby)));
            Assert.False(entries.Any(entry => ReferenceEquals(ToolValue(entry, "Item"), hidden)));
            Assert.False(entries.Any(entry => ReferenceEquals(ToolValue(entry, "Item"), distant)));
            Assert.That(ToolOperations(ToolEntry(entries, carried)).Any(ToolReady), Is.True, "The same visible source is usable by the exact carried scraper.");
            var stowedEntry = ToolEntry(entries, stowed);
            Assert.That(ToolValue(stowedEntry, "StorageOwner"), Is.SameAs(satchel));
            Assert.That(ToolValue(stowedEntry, "Display"), Is.Null);
            Assert.False(ToolOperations(stowedEntry).Any(ToolReady));
            Assert.That(stowed.GetPart<PhysicsPart>().InInventory, Is.SameAs(satchel));
            Assert.That(vent.GetPart<SourceStockPart>(), Is.Null, "Looking at the Tools ledger must not spend or create source stock.");
        }

        [Test]
        public void ToolsTabSeparatesReadySetupFromBlockedStationProcessing()
        {
            using var r = new Rig();
            var tray = PlaceToolsFixture(r, "CampSettlingTray", 11, 10, true);
            var still = PlaceToolsFixture(r, "CampStill", 10, 11, true);
            var entries = ToolEntries(r, InventoryScreenData.Build(r.Player));

            var trayWork = ToolOperations(ToolEntry(entries, tray));
            Assert.False(trayWork.Any(operation => ToolReady(operation) && !(bool)ToolValue(operation, "IsSetup")));
            var stillWork = ToolOperations(ToolEntry(entries, still));
            Assert.True(stillWork.Any(operation => !ToolReady(operation)
                && ((string)ToolValue(operation, "Detail")).Contains("Pick the reagents")));
            Assert.False(stillWork.Any(operation => ToolReady(operation)
                && ((string)ToolValue(operation, "Title")).StartsWith("brew", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public void ToolsTabAttributesScreeningToTheExactSieveSelectedByTheWorkpieceQuote()
        {
            using var r = new Rig();
            var first = CountedCarry(r, "HandSieve");
            var second = CountedCarry(r, "HandSieve");
            var spoil = r.Factory.CreateEntity("DryClaySpoil");
            Assert.True(r.Pack.AddObject(spoil));
            var previousFactory = ForgePart.Factory;
            try
            {
                ForgePart.Factory = r.Factory;
                MaterialProcessRegistry.ResetForTests();
                MaterialProcessRegistry.LoadDefaults();
                var state = InventoryScreenData.Build(r.Player);
                var display = state.Categories.SelectMany(group => group.Items).Single(row => row.Item == spoil);
                var screen = display.Actions.Single(action => action.Command.StartsWith(
                    "MaterialProcess|screen_size_grades|", StringComparison.Ordinal));
                Assert.False(screen.IsDisabled);
                var elected = new[] { first, second }.OrderBy(item => item.ID, StringComparer.Ordinal).First();
                var other = elected == first ? second : first;
                Assert.That(ToolValue(screen, "RecipeTool"), Is.SameAs(elected));

                var entries = ToolEntries(r, state);
                var operations = ToolOperations(ToolEntry(entries, elected));
                Assert.That(operations.Any(operation => ToolReady(operation)
                    && ReferenceEquals(ToolValue(operation, "Workpiece"), spoil)
                    && ((string)ToolValue(operation, "Title")).Contains("fire clay")
                    && ((string)ToolValue(operation, "Title")).Contains("coarse grit")), Is.True);
                Assert.That(ToolOperations(ToolEntry(entries, other)).Any(operation =>
                    ReferenceEquals(ToolValue(operation, "Workpiece"), spoil)), Is.False);
                CollectionAssert.AreEquivalent(new[] { first, second, spoil }, r.Pack.Objects);
            }
            finally
            {
                ForgePart.Factory = previousFactory;
                MaterialProcessRegistry.ResetForTests();
            }
        }

        [Test]
        public void ToolsTabOpensTheNormalActionMenuForTheExactSelectedFlask()
        {
            using var registry = new PublicStateRegistryScope();
            using var r = new Rig();
            var other = CountedCarry(r, "LiquidFlask");
            var selected = CountedCarry(r, "LiquidFlask");
            var pool = r.Factory.CreateEntity("MirePool");
            pool.GetPart<TileStateSourcePart>().Coating = "bog-mire";
            Assert.True(r.UI.CurrentZone.AddEntity(pool, 11, 10));
            var cell = r.UI.CurrentZone.GetEntityCell(pool);
            cell.IsVisible = cell.Explored = true;
            int volumeBefore = pool.GetPart<LiquidPoolPart>().Volume;

            r.Open();
            r.Tab(6);
            Assert.That(((IEnumerable)Get(r.UI, "_toolEntries")).Cast<object>().Count(), Is.EqualTo(1));
            Assert.True(r.Labels().Any(label => label.Content.Contains("liquid flask x2")));
            r.ActivatePainted(r.Hits().Single(hit => Kind(hit) == "PackGrouping"));
            var entries = ((IEnumerable)Get(r.UI, "_toolEntries")).Cast<object>().ToArray();
            int pick = Array.FindIndex(entries, entry => ReferenceEquals(ToolValue(Get(entry, "Entry"), "Item"), selected));
            Assert.That(pick, Is.GreaterThanOrEqualTo(0));
            r.ActivatePainted(r.FindHit("ToolRow", pick));
            Assert.That(Get(r.UI, "_selectedTool"), Is.SameAs(selected));
            r.ActivatePainted(r.Hits().Single(hit => Kind(hit) == "ToolActions"));

            var popup = Get(r.UI, "_itemActionPopup");
            Assert.NotNull(popup);
            Assert.That(Get(popup, "Item"), Is.SameAs(selected));
            var actions = ((IEnumerable)Get(popup, "Actions")).Cast<object>().ToArray();
            int draw = Array.FindIndex(actions, action => ((string)Get(action, "Command")).StartsWith(
                "FillLiquidVessel|" + Uri.EscapeDataString(pool.ID) + "|", StringComparison.Ordinal));
            Assert.That(draw, Is.GreaterThanOrEqualTo(0));
            Assert.That(pool.GetPart<LiquidPoolPart>().Volume, Is.EqualTo(volumeBefore));
            Assert.Zero(selected.GetPart<LiquidVesselPart>().Volume);
            Assert.Zero(other.GetPart<LiquidVesselPart>().Volume);
            Assert.False(r.UI.ConsumePendingEverydayTurn());

            r.ActivatePainted(r.FindHit("PopupRow", draw));
            var vessel = selected.GetPart<LiquidVesselPart>();
            Assert.That(vessel.LiquidId, Is.EqualTo("bog-mire"));
            Assert.That(vessel.Volume, Is.EqualTo(vessel.Capacity));
            Assert.That(pool.GetPart<LiquidPoolPart>().Volume, Is.EqualTo(volumeBefore - vessel.Capacity));
            Assert.Zero(other.GetPart<LiquidVesselPart>().Volume);
            Assert.True(r.UI.ConsumePendingEverydayTurn());
            Assert.False(r.UI.ConsumePendingEverydayTurn());
        }

        private static object[] ToolEntries(Rig r, InventoryScreenData.ScreenState state)
        {
            var assembly = typeof(InventoryUI).Assembly;
            var tools = assembly.GetType("CavesOfOoo.Rendering.InventoryToolsData")
                ?? assembly.GetType("CavesOfOoo.Core.InventoryToolsData");
            Assert.NotNull(tools, "The Tools ledger needs a projection over the existing inventory owners.");
            var build = tools.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(build);
            var entries = (IEnumerable)build.Invoke(null, new object[] { r.Player, r.UI.CurrentZone, state });
            return entries.Cast<object>().ToArray();
        }

        private static object ToolEntry(object[] entries, Entity owner) =>
            entries.Single(entry => ReferenceEquals(ToolValue(entry, "Item"), owner));

        private static object[] ToolOperations(object entry) => ((IEnumerable)ToolValue(entry, "Operations")).Cast<object>().ToArray();

        private static bool ToolReady(object operation) => (bool)ToolValue(operation, "IsReady");

        private static object ToolValue(object owner, string name)
        {
            var property = owner.GetType().GetProperty(name, Members);
            return property != null ? property.GetValue(owner) : Get(owner, name);
        }

        private static Entity PlaceToolsFixture(Rig r, string blueprint, int x, int y, bool visible)
        {
            var fixture = r.Factory.CreateEntity(blueprint);
            if (fixture.GetPart<InstallablePart>() == null) fixture.AddPart(new InstallablePart());
            fixture.GetPart<InstallablePart>().State = InstallablePart.Installed;
            Assert.True(r.UI.CurrentZone.AddEntity(fixture, x, y));
            var cell = r.UI.CurrentZone.GetEntityCell(fixture);
            cell.Explored = true;
            cell.IsVisible = visible;
            return fixture;
        }
    }
}
