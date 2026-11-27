using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
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
        EntityFactory previousForgeFactory;

        [SetUp] public void SetupBox()
        {
            PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear();
            previousForgeFactory = ForgePart.Factory;
            ForgePart.Factory = Factory;
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild;
        }
        [TearDown] public void TearDownBox()
        {
            ForgePart.Factory = previousForgeFactory;
            PackableFixtureRegistry.ResetForTests();
        }

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

        (Entity hearth, Entity box, Entity timber) TimberAndHearth(float fuel)
        {
            for (int x = AX - 1; x <= AX + 1; x++)
            {
                var cell = wild.GetCell(x, AY);
                cell.IsVisible = cell.Explored = true;
            }
            var box = Carry("CampBox");
            Assert.True(Act(box, SetDownRow(box)));
            var timber = Carry("SalvagedTimber");
            timber.GetPart<StackerPart>().StackCount = 3;
            Assert.True(InventorySystem.PutInContainer(Actor, box, timber));
            var hearth = Place("CampHearth", AX - 1, AY);
            hearth.GetPart<PhysicsPart>().Takeable = false;
            hearth.GetPart<FuelPart>().FuelMass = fuel;
            hearth.GetPart<ThermalPart>().Temperature = 25f;
            hearth.GetPart<LightSourcePart>().Enabled = false;
            return (hearth, box, timber);
        }

        InventoryAction[] StoredTimberRows() => CellMenu(AX - 1, AY).Where(row => !row.IsDisabled
            && row.Command.StartsWith("FeedStoredTimber|", StringComparison.Ordinal)).ToArray();

        const string StoredOilPrefix = "FeedStoredHearthOil|";

        (Entity hearth, Entity box, Entity flask) OilAndHearth(float fuel = 4, int oil = 3)
        {
            for (int x = AX - 1; x <= AX + 1; x++)
                for (int y = AY - 1; y <= AY + 1; y++)
                    wild.GetCell(x, y).IsVisible = wild.GetCell(x, y).Explored = true;

            var box = Carry("CampBox");
            Assert.True(Act(box, SetDownRow(box)));
            var hearth = Carry("CampHearth");
            Assert.True(Act(hearth, SetDownRow(hearth, "north")));
            hearth.GetPart<FuelPart>().FuelMass = fuel;
            hearth.GetPart<ThermalPart>().Temperature = 25f;
            hearth.GetPart<LightSourcePart>().Enabled = false;
            var flask = Flask(oil, "oil");
            Assert.True(InventorySystem.PutInContainer(Actor, box, flask));
            return (hearth, box, flask);
        }

        InventoryAction[] OilRows(Entity hearth, string prefix = StoredOilPrefix)
        {
            var cell = wild.GetEntityCell(hearth);
            return CellMenu(cell.X, cell.Y).Where(row => !row.IsDisabled
                && row.Command.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        }

        [TestCase(4f, 3, 7f, 0)]
        [TestCase(4f, 7, 7f, 4)]
        [TestCase(28.25f, 7, 29.25f, 6)]
        public void StoredOilCreditsOnlyWholeUnitsAndLeavesItsFlaskInTheBox(float before, int oil, float after, int remaining)
        {
            var (hearth, box, flask) = OilAndHearth(before, oil);
            var packed = Flask(2, "oil");
            var row = OilRows(hearth).Single();
            StringAssert.Contains($"{oil - remaining} oil", row.Display);
            StringAssert.Contains("camp box", row.Display);
            Assert.That(row.Time, Is.EqualTo(ActionTime.OneAction));
            Assert.That(OilRows(hearth, "FeedHearthOil|"), Has.Length.EqualTo(1));
            Assert.True(ChooseRow(row, AX, AY - 1));

            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(after));
            Assert.That(hearth.GetPart<ThermalPart>().Temperature, Is.EqualTo(25f));
            Assert.False(hearth.GetPart<LightSourcePart>().Enabled);
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(flask));
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(Units(flask), Is.EqualTo(remaining));
            Assert.That(flask.GetPart<LiquidVesselPart>().LiquidId, Is.EqualTo(remaining == 0 ? "" : "oil"));
            Assert.That(Units(packed), Is.EqualTo(2));
            Assert.False(ChooseRow(row, AX, AY - 1));
        }

        [TestCase("fill")]
        [TestCase("withdraw")]
        [TestCase("hidden")]
        [TestCase("foreign")]
        [TestCase("far")]
        [TestCase("full")]
        public void AStaleStoredOilQuoteCannotUseACarriedFlaskOrMovedStock(string change)
        {
            var (hearth, box, flask) = OilAndHearth();
            var alternative = Flask(2, "oil");
            var row = OilRows(hearth).Single();
            if (change == "fill") flask.GetPart<LiquidVesselPart>().Volume++;
            if (change == "withdraw") Assert.True(InventorySystem.TakeFromContainer(Actor, box, flask));
            if (change == "hidden") wild.GetEntityCell(box).IsVisible = false;
            if (change == "foreign") box.GetPart<InstallationMarkerPart>().Origin = "Generated";
            if (change == "far") Assert.True(wild.MoveEntity(box, AX + 1, AY + 1));
            if (change == "full") hearth.GetPart<FuelPart>().FuelMass = 29.25f;

            Assert.False(ChooseRow(row, AX, AY - 1));
            Assert.That(Units(flask), Is.EqualTo(change == "fill" ? 4 : 3));
            Assert.That(Units(alternative), Is.EqualTo(2));
            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(change == "full" ? 29.25f : 4));
            if (change != "fill") Assert.That(OilRows(hearth), Is.Empty);
            if (change == "withdraw")
            {
                var carried = OilRows(hearth, "FeedHearthOil|").Single(candidate => candidate.Display.Contains("3 oil"));
                Assert.True(ChooseRow(carried, AX, AY - 1));
                Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(7));
                Assert.That(Units(flask), Is.Zero);
                Assert.That(Units(alternative), Is.EqualTo(2));
            }
        }

        [TestCase("nested")]
        [TestCase("pack-alias")]
        [TestCase("locked")]
        public void UnavailableOrAmbiguousStoredOilOffersNoRoute(string change)
        {
            var (hearth, box, flask) = OilAndHearth();
            var contents = box.GetPart<ContainerPart>();
            if (change == "nested")
            {
                Assert.True(contents.RemoveItem(flask));
                var inner = Factory.CreateEntity("CampBox");
                Assert.True(inner.GetPart<ContainerPart>().AddItem(flask));
                Assert.True(contents.AddItem(inner));
            }
            if (change == "pack-alias") Flask(2, "oil").ID = flask.ID;
            if (change == "locked") contents.Locked = true;

            Assert.That(OilRows(hearth), Is.Empty);
            Assert.That(Units(flask), Is.EqualTo(3));
            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(4));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ReplacingAQuotedFixtureCellCannotSpendStoredOil(bool replaceHearth)
        {
            var (hearth, box, flask) = OilAndHearth();
            var selected = OilRows(hearth).Single();
            var movedOutsideGraph = replaceHearth ? hearth : box;
            var staleCell = wild.GetEntityCell(movedOutsideGraph);
            wild.Cells[staleCell.X, staleCell.Y] = new Cell(staleCell.X, staleCell.Y, wild)
            {
                IsVisible = true,
                Explored = true
            };
            // The stale spatial index still points to the old owner and cell.
            Assert.That(wild.GetEntityCell(movedOutsideGraph), Is.SameAs(staleCell));
            Assert.False(Act(hearth, selected.Command));
            Assert.False(Actions(hearth).Any(row => row.Command.StartsWith(StoredOilPrefix, StringComparison.Ordinal)));
            Assert.That(Units(flask), Is.EqualTo(3));
            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(4));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(flask));
        }

        [Test]
        public void LateStoredOilInterruptionPreservesIndependentChanges()
        {
            var (hearth, box, flask) = OilAndHearth(4, 5);
            var fuel = hearth.GetPart<FuelPart>();
            var vessel = flask.GetPart<LiquidVesselPart>();
            int originalHp = Actor.GetStatValue("Hitpoints");
            bool sawStaged = false;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                sawStaged = fuel.FuelMass == 7 && vessel.Volume == 2;
                fuel.FuelMass += 2;
                vessel.Volume++;
                Actor.GetStat("Hitpoints").Penalty += 2;
            } });

            Assert.False(ChooseRow(OilRows(hearth).Single(), AX, AY - 1));
            Assert.True(sawStaged);
            Assert.That(fuel.FuelMass, Is.EqualTo(6));
            Assert.That(vessel.Volume, Is.EqualTo(6));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(flask));
            Assert.That(Actor.GetStatValue("Hitpoints"), Is.EqualTo(originalHp - 2));
        }

        [Test]
        public void DestroyingTheBoxDuringFeedLeavesTheRefundedFlaskOnTheGround()
        {
            var (hearth, box, flask) = OilAndHearth();
            var boxCell = wild.GetEntityCell(box);
            var selected = OilRows(hearth).Single();
            var fuel = hearth.GetPart<FuelPart>();
            var verdict = DestroyVerdict.NoTarget;
            bool sawStaged = false;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                sawStaged = Units(flask) == 0 && fuel.FuelMass == 7
                    && flask.GetPart<PhysicsPart>().InInventory == box;
                verdict = DestructionSystem.Destroy(box, null, wild, "stored-oil-interruption");
            } });

            Assert.False(ChooseRow(selected, AX, AY - 1));
            Assert.True(sawStaged);
            Assert.That(verdict, Is.EqualTo(DestroyVerdict.Destroyed));
            Assert.That(wild.GetEntityCell(box), Is.Null);
            Assert.That(wild.GetEntityCell(flask), Is.SameAs(boxCell));
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(wild.GetReadOnlyEntities().Single(item => item.ID == flask.ID), Is.SameAs(flask));
            Assert.That(Units(flask), Is.EqualTo(3));
            Assert.That(flask.GetPart<LiquidVesselPart>().LiquidId, Is.EqualTo("oil"));
            Assert.That(fuel.FuelMass, Is.EqualTo(4));
        }

        [Test]
        public void EmptyStoredFlaskAndCreditedFuelKeepTheirExactSavedOwners()
        {
            var (hearth, box, flask) = OilAndHearth();
            Assert.True(ChooseRow(OilRows(hearth).Single(), AX, AY - 1));
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("stored-oil", "test", manager, Clock, Actor));
            var savedZone = loaded.ZoneManager.GetZone(wildId);
            var savedHearth = savedZone.GetReadOnlyEntities().Single(entity => entity.ID == hearth.ID);
            var savedBox = savedZone.GetReadOnlyEntities().Single(entity => entity.ID == box.ID);
            var savedFlask = savedBox.GetPart<ContainerPart>().Contents.Single();

            Assert.That(savedHearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(7));
            Assert.That(savedHearth.GetPart<ThermalPart>().Temperature, Is.EqualTo(25f));
            Assert.That(savedFlask.ID, Is.EqualTo(flask.ID));
            Assert.That(savedFlask.GetPart<PhysicsPart>().InInventory, Is.SameAs(savedBox));
            Assert.That(savedFlask.GetPart<LiquidVesselPart>().LiquidId, Is.Empty);
            Assert.That(Units(savedFlask), Is.Zero);
            Assert.False(loaded.Player.GetPart<InventoryPart>().Objects.Any(entity => entity.ID == flask.ID));
            Assert.False(savedZone.GetReadOnlyEntities().Any(entity => entity.ID == flask.ID));
        }

        [TestCase(5f, 15f, false)]
        [TestCase(27f, 30f, true)]
        public void TheFireTakesOneExactStoredTimberAndLeavesItUnlit(float before, float after, bool packedAlternative)
        {
            var (hearth, box, timber) = TimberAndHearth(before);
            var packed = packedAlternative ? Carry("SalvagedTimber") : null;
            Assert.That(CellMenu(AX - 1, AY).Any(row => row.Command == "FeedCookingFire" && !row.IsDisabled),
                Is.EqualTo(packedAlternative));
            var row = StoredTimberRows().Single();

            Assert.True(ChooseRow(row, AX - 1, AY));
            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(after));
            Assert.That(hearth.GetPart<ThermalPart>().Temperature, Is.EqualTo(25f));
            Assert.False(hearth.GetPart<LightSourcePart>().Enabled);
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(timber));
            Assert.That(timber.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(timber.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(Pack.Objects.Count(item => item.BlueprintName == "SalvagedTimber"), Is.EqualTo(packedAlternative ? 1 : 0));
            if (packed != null)
            {
                Assert.True(Pack.Contains(packed));
                Assert.That(packed.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void AReplacementDepositCannotSatisfyAStaleTimberQuoteOrSpendThePackedAlternative()
        {
            var (hearth, box, timber) = TimberAndHearth(5f);
            var stale = StoredTimberRows().Single();
            var contents = box.GetPart<ContainerPart>();
            Assert.True(contents.RemoveItem(timber));
            Assert.True(wild.AddEntity(timber, AX, AY + 2));
            var replacement = Carry("SalvagedTimber");
            replacement.GetPart<StackerPart>().StackCount = 3;
            Assert.True(InventorySystem.PutInContainer(Actor, box, replacement));
            var packed = Carry("SalvagedTimber");

            Assert.False(ChooseRow(stale, AX - 1, AY));
            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(5f));
            Assert.That(contents.Contents.Single(), Is.SameAs(replacement));
            Assert.That(timber.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(replacement.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(packed.GetPart<StackerPart>().StackCount, Is.EqualTo(1));

            var ordinary = CellMenu(AX - 1, AY).Single(row => row.Command == "FeedCookingFire" && !row.IsDisabled);
            Assert.True(ChooseRow(ordinary, AX - 1, AY));
            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(15f));
            Assert.False(Pack.Contains(packed));
            Assert.That(replacement.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
        }

        [Test]
        public void LateFuelChangeRefundsOnlyTheStagedFeedAndKeepsIndependentInjury()
        {
            var (hearth, box, timber) = TimberAndHearth(5f);
            int hp = Actor.GetStatValue("Hitpoints");
            var fuel = hearth.GetPart<FuelPart>();
            int unitsDuringCallback = 0;
            float fuelDuringCallback = 0;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                unitsDuringCallback = timber.GetPart<StackerPart>().StackCount;
                fuelDuringCallback = fuel.FuelMass;
                fuel.FuelMass += 2;
                Actor.GetStat("Hitpoints").Penalty += 2;
            } });
            var selected = StoredTimberRows().Single();

            Assert.False(ChooseRow(selected, AX - 1, AY));
            Assert.That(unitsDuringCallback, Is.EqualTo(2));
            Assert.That(fuelDuringCallback, Is.EqualTo(15f));
            Assert.That(fuel.FuelMass, Is.EqualTo(7f));
            Assert.That(Actor.GetStatValue("Hitpoints"), Is.EqualTo(hp - 2));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(timber));
            Assert.That(timber.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(timber.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
        }

        [Test]
        public void InterruptedFeedRestoresTheSpentUnitToTheExactStackSpilledByDestruction()
        {
            var (hearth, box, timber) = TimberAndHearth(5f);
            var boxCell = wild.GetEntityCell(box);
            var contents = box.GetPart<ContainerPart>();
            var fuel = hearth.GetPart<FuelPart>();
            int hp = Actor.GetStatValue("Hitpoints");
            var verdict = DestroyVerdict.NoTarget;
            bool sawStagedFeed = false;
            Cell spilledCell = null;
            Actor.AddPart(new RouteWorkAfterProbe { Run = () =>
            {
                sawStagedFeed = contents.Contents.Contains(timber)
                    && timber.GetPart<StackerPart>().StackCount == 2 && fuel.FuelMass == 15f;
                verdict = DestructionSystem.Destroy(box, null, wild, "feed-interruption");
                spilledCell = wild.GetEntityCell(timber);
                Actor.GetStat("Hitpoints").Penalty += 2;
            } });

            Assert.False(ChooseRow(StoredTimberRows().Single(), AX - 1, AY));
            Assert.True(sawStagedFeed);
            Assert.That(verdict, Is.EqualTo(DestroyVerdict.Destroyed));
            Assert.That(spilledCell, Is.SameAs(boxCell));
            Assert.That(wild.GetEntityCell(box), Is.Null);
            Assert.That(contents.Contents, Is.Empty);
            Assert.That(wild.GetEntityCell(timber), Is.SameAs(boxCell));
            Assert.That(wild.GetReadOnlyEntities().Single(item => item.BlueprintName == "SalvagedTimber"), Is.SameAs(timber));
            Assert.That(timber.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(timber.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(fuel.FuelMass, Is.EqualTo(5f));
            Assert.That(Actor.GetStatValue("Hitpoints"), Is.EqualTo(hp - 2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AFullFireOrForeignBoxCannotSpendStoredTimber(bool foreign)
        {
            var (hearth, box, timber) = TimberAndHearth(5f);
            var stale = StoredTimberRows().Single();
            if (foreign) box.GetPart<InstallationMarkerPart>().Origin = "Generated";
            else hearth.GetPart<FuelPart>().FuelMass = 30f;

            Assert.That(StoredTimberRows(), Is.Empty);
            Assert.False(ChooseRow(stale, AX - 1, AY));
            Assert.That(hearth.GetPart<FuelPart>().FuelMass, Is.EqualTo(foreign ? 5f : 30f));
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(timber));
            Assert.That(timber.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
        }

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
        // ── deposit: a solid set-down box cannot be stood on, so the player's own boxes accept deposits from the adjacent cell ──

        [Test] public void AHeldBoxWithinOneCellIsADepositContainer_AndOtherContainersKeepTheAtFeetRule()
        {
            var box = Carry("CampBox"); Assert.True(Act(box, SetDownRow(box)));
            CollectionAssert.Contains(InventorySystem.GetDepositContainers(Actor, wild), box);
            var chest = Place("Chest", AX - 1, AY); CollectionAssert.DoesNotContain(InventorySystem.GetDepositContainers(Actor, wild), chest, "an ordinary adjacent chest is still not a deposit target");
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX - 3, AY)); CollectionAssert.DoesNotContain(InventorySystem.GetDepositContainers(Actor, wild), box, "two cells away");
        }

        [Test] public void AHandedOverOrAuthoredBoxIsNotAnExtendedDepositTarget_Counter()
        {
            var box = Carry("CampBox"); Assert.True(Act(box, SetDownRow(box))); box.GetPart<InstallationMarkerPart>().Origin = "Generated";
            CollectionAssert.DoesNotContain(InventorySystem.GetDepositContainers(Actor, wild), box);
            box.GetPart<InstallationMarkerPart>().Origin = "Player"; CollectionAssert.Contains(InventorySystem.GetDepositContainers(Actor, wild), box);
            Assert.True(Act(box, "HandOver|" + Tail(box))); CollectionAssert.DoesNotContain(InventorySystem.GetDepositContainers(Actor, wild), box, "handing it over ends the player's reach");
        }

        [Test] public void ThePackMenuOffersPutInForAnAdjacentHeldBoxAndTheItemEndsUpInsideAndSurvivesTakeUp()
        {
            var box = Carry("CampBox"); Assert.True(Act(box, SetDownRow(box))); var dagger = Carry("Dagger");
            using (var ui = new HotbarSaveFixture(true, false))
            {
                manager.SetActiveZone(wild); var turns = new TurnManager();
                turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("camp-box", "deposit", manager, turns, Actor));
                const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var inv = ui.Root.AddComponent<InventoryUI>(); inv.PlayerEntity = Actor; inv.CurrentZone = wild; inv.EntityFactory = Factory; ui.Input.InventoryUI = inv; inv.Open();
                var display = InventoryScreenData.Build(Actor).Categories.SelectMany(c => c.Items).Single(i => i.Item == dagger);
                typeof(InventoryUI).GetMethod("OpenItemActionPopup", hidden).Invoke(inv, new object[] { display });
                var popup = typeof(InventoryUI).GetField("_itemActionPopup", hidden).GetValue(inv); var list = (System.Collections.IList)popup.GetType().GetField("Actions").GetValue(popup);
                var labels = Enumerable.Range(0, list.Count).Select(i => (string)list[i].GetType().GetField("Label").GetValue(list[i])).ToArray();
                CollectionAssert.Contains(labels, "Put in camp box");
            }
            Assert.True(InventorySystem.PutInContainer(Actor, box, dagger)); CollectionAssert.Contains(box.GetPart<ContainerPart>().Contents, dagger); Assert.False(Pack.Objects.Contains(dagger));
            Assert.True(Act(box, "TakeUp|" + Tail(box))); CollectionAssert.Contains(box.GetPart<ContainerPart>().Contents, dagger);
        }
    }
}
