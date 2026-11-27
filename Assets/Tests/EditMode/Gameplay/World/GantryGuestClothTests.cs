using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class GantryGuestClothTests : FiftyWorldFixture
    {
        const string GuestPole = "GuestPole";
        const string Rest = "RestAtRestoredPlaceSite";
        const string Mend = "PlaceSiteRepair|Lasting";
        const string Drying = "DryUnderGuestCloth|";
        static readonly FieldInfo LootTables = typeof(LootTableRegistry).GetField("_byName", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly FieldInfo LootInitialized = typeof(LootTableRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        Dictionary<string, LootTableData> previousLootTables;
        bool previousLootInitialized;
        OverworldZoneManager manager;
        RepairableSiteState Site => manager.SettlementManager.GetSite(GantryCompositionPlan.ZoneID, GuestPole);

        [SetUp]
        public void LoadGantryContent()
        {
            previousLootTables = new Dictionary<string, LootTableData>((Dictionary<string, LootTableData>)LootTables.GetValue(null));
            previousLootInitialized = LootTableRegistry.IsInitialized;
            PlaceSiteCatalog.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/PlaceSites.json")));
            CinderholdCompositionTests.LoadLoot();
        }

        [TearDown]
        public void ResetGantryContent()
        {
            PlaceSiteCatalog.Reset();
            var loot = (Dictionary<string, LootTableData>)LootTables.GetValue(null);
            loot.Clear();
            foreach (var row in previousLootTables) loot.Add(row.Key, row.Value);
            LootInitialized.SetValue(null, previousLootInitialized);
        }

        Entity EnterPlace(string zoneId = GantryCompositionPlan.ZoneID)
        {
            // Generate and enter with the same factory that created the actor and repair materials.
            manager = new OverworldZoneManager(Factory, 64);
            manager.SettlementManager.SetCurrentTurnProvider(() => Clock.TickCount);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(zoneId);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Zone.ForEachCell((cell, _, _) => { cell.IsVisible = true; cell.Explored = true; });
            var poles = Zone.GetReadOnlyEntities().Where(owner => owner.BlueprintName == "GuestClothPole");
            var pole = zoneId == GantryCompositionPlan.ZoneID ? poles.Single() : poles.First();
            var at = Zone.GetEntityCell(pole);
            var beside = new[] { (at.X - 1, at.Y), (at.X + 1, at.Y), (at.X, at.Y - 1), (at.X, at.Y + 1) }
                .Select(p => Zone.GetCell(p.Item1, p.Item2)).First(cell => cell != null && !cell.BlocksMovement());
            Assert.True(Zone.AddEntity(Actor, beside.X, beside.Y));
            if (!Actor.HasPart<StatusEffectsPart>()) Actor.AddPart(new StatusEffectsPart());
            return pole;
        }

        InventoryAction[] PoleMenu(Entity pole)
        {
            var at = Zone.GetEntityCell(pole);
            return CellMenu(at.X, at.Y).ToArray();
        }

        static bool RestRow(InventoryAction row) => !row.IsDisabled && row.Command == Rest;

        bool ChoosePole(Entity pole, InventoryAction row)
        {
            var at = Zone.GetEntityCell(pole);
            return ChooseRow(row, at.X, at.Y);
        }

        InventoryAction[] DryingRows(Entity pole) => PoleMenu(pole)
            .Where(row => !row.IsDisabled && row.Command.StartsWith(Drying, StringComparison.Ordinal)).ToArray();

        void SetShower(bool raining)
        {
            int now = Clock.TickCount;
            int first = now / RegionalWeather.WindowTicks;
            var tick = Enumerable.Range(first, RegionalWeather.CycleWindows)
                .Select(window => Math.Max(now, window * RegionalWeather.WindowTicks))
                .FirstOrDefault(candidate =>
                {
                    var weather = RegionalWeather.Current(Zone, candidate, RegionalWeather.PolicyVersion);
                    return weather.Valid && weather.Raining == raining;
                });
            Clock.AdvanceClock(tick - now);
            var current = RegionalWeather.Current(Zone, Clock.TickCount, RegionalWeather.PolicyVersion);
            Assert.True(current.Valid);
            Assert.AreEqual(raining, current.Raining);
        }

        (Entity striker, WetEffect wet) DampStriker(float moisture = .48f)
        {
            var striker = Carry("FlintStriker");
            var wet = new WetEffect(moisture);
            Assert.True(striker.ApplyEffect(wet));
            wet.Moisture = moisture;
            return (striker, wet);
        }

        [Test]
        public void RepairMakesShelteredDryingAvailableAndTheSameStrikerLightsATorch()
        {
            var pole = EnterPlace();
            SetShower(true);
            var (striker, wet) = DampStriker();
            Assert.IsEmpty(DryingRows(pole), "Torn cloth offers no shelter work.");
            Carry("Cloak");
            Carry("FieldHaftComponent");
            Assert.True(ChoosePole(pole, PoleMenu(pole).Single(row => !row.IsDisabled && row.Command == Mend)));
            Assert.AreEqual(RepairStage.StableRepair, Site.Stage);

            var torch = Carry("Torch");
            Assert.True(InventorySystem.Equip(Actor, torch));
            Assert.True(Act(torch, "ExtinguishTorch"));
            Assert.False(Act(torch, "LightTorch"));
            var mechanism = striker.GetPart<FlintStrikerPart>();
            int originalStrikes = mechanism.Strikes;
            TradeSystem.SetDrams(Actor, 15);

            var row = DryingRows(pole).Single();
            Assert.That(row.Command, Does.Contain(Id(striker)));
            Assert.That(row.Display, Does.Contain("48").And.Contain("33"));
            Assert.AreEqual(ActionTime.OneAction, row.Time);
            var reader = ExamineDocumentBuilder.BuildWorld(pole, Zone, Zone.GetEntityCell(pole), Actor);
            Assert.That(reader.Sections.Single(section => section.Heading == "How to use").Text,
                Does.Contain(row.Display));
            Assert.True(ChoosePole(pole, row));
            Assert.AreSame(wet, striker.GetEffect<WetEffect>());
            Assert.AreEqual(.33f, wet.Moisture, .0001f);
            Assert.AreSame(Actor, striker.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(originalStrikes, mechanism.Strikes);
            Assert.AreEqual(15, TradeSystem.GetDrams(Actor));
            Assert.False(Act(pole, row.Command));

            Assert.True(Act(torch, "LightTorch"));
            Assert.True(torch.GetPart<ThermalPart>().IsAflame);
            Assert.AreEqual(originalStrikes - 1, mechanism.Strikes);
        }

        [Test]
        public void AnInterruptedDryingActionPreservesIndependentWetnessOnTheSameItem()
        {
            var pole = EnterPlace();
            Site.Stage = RepairStage.StableRepair;
            SetShower(true);
            var (striker, wet) = DampStriker();
            var row = DryingRows(pole).Single();
            int calls = 0;
            Actor.AddPart(new FiftyWorldEventProbe { Callback = e =>
            {
                if (e.ID != "AfterInventoryAction") return;
                calls++;
                Assert.AreEqual(.33f, wet.Moisture, .0001f);
                wet.Moisture += .10f;
            } });

            Assert.False(ChoosePole(pole, row));
            Assert.AreEqual(1, calls);
            Assert.AreSame(wet, striker.GetEffect<WetEffect>());
            Assert.AreEqual(.58f, wet.Moisture, .0001f);
        }

        [TestCase("fair")] [TestCase("wet footing")] [TestCase("torn again")] [TestCase("hidden pole")]
        public void ChangingTheShelterContextInvalidatesAnOpenDryingChoice(string change)
        {
            var pole = EnterPlace();
            Site.Stage = RepairStage.StableRepair;
            SetShower(true);
            var (_, wet) = DampStriker();
            var row = DryingRows(pole).Single();
            if (change == "fair") SetShower(false);
            if (change == "wet footing")
            {
                var at = Zone.GetEntityCell(Actor);
                Zone.TileState.WriteCoating(at.X, at.Y, "water", ZoneTileState.Permanent);
            }
            if (change == "torn again") Site.Stage = RepairStage.Fouled;
            if (change == "hidden pole") pole.GetPart<RenderPart>().Visible = false;

            Assert.False(ChoosePole(pole, row));
            Assert.IsEmpty(DryingRows(pole));
            Assert.AreEqual(.48f, wet.Moisture, .0001f);
        }

        [Test]
        public void ASharedPoleElsewhereOffersNoDryingEvenDuringItsShower()
        {
            var pole = EnterPlace(WellmeetCompositionPlan.ZoneID);
            Site.Stage = RepairStage.StableRepair;
            SetShower(true);
            var (_, wet) = DampStriker();
            Assert.IsEmpty(DryingRows(pole));
            Assert.AreEqual(.48f, wet.Moisture, .0001f);
        }

        void Injure(bool bleeding = false)
        {
            Actor.GetStat("Hitpoints").BaseValue = 4;
            if (bleeding) Actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new BleedingEffect());
        }

        void BringDanger(Entity pole)
        {
            var actorCell = Zone.GetEntityCell(Actor);
            var stand = new[] { (actorCell.X - 1, actorCell.Y), (actorCell.X + 1, actorCell.Y),
                    (actorCell.X, actorCell.Y - 1), (actorCell.X, actorCell.Y + 1) }
                .Select(p => Zone.GetCell(p.Item1, p.Item2))
                .First(cell => cell != null && cell != Zone.GetEntityCell(pole) && !cell.BlocksMovement());
            var enemy = new Entity { ID = Guid.NewGuid().ToString("N") };
            enemy.AddPart(new PhysicsPart());
            var brain = new BrainPart();
            enemy.AddPart(brain);
            brain.SetPersonallyHostile(Actor, false);
            Assert.True(Zone.AddEntity(enemy, stand.X, stand.Y));
        }

        [TestCase("OakHaftComponent", false)]
        [TestCase("FieldHaftComponent", false)]
        [TestCase("OakHaftComponent", true)]
        [TestCase("FieldHaftComponent", true)]
        public void CompatiblePegRepairsTheSameClothThroughWorldOrHostAndEnablesFreeRest(string peg, bool throughHost)
        {
            var pole = EnterPlace();
            var sitePart = pole.GetPart<PlaceSiteObjectPart>();
            Assert.NotNull(sitePart, "The existing native pole owns the guest-cloth action.");
            Assert.AreEqual(("Gantry", GuestPole), (sitePart.Place, sitePart.SiteId));
            Assert.AreEqual(RepairStage.Fouled, Site.Stage);
            Injure();
            Assert.False(PoleMenu(pole).Any(RestRow));
            Assert.False(Act(pole, Rest));
            Assert.AreEqual(0, Clock.TickCount);

            var cloak = Carry("Cloak");
            var exactPeg = Carry(peg);
            var other = Carry("IronSpikeComponent");
            if (throughHost)
            {
                var host = Zone.GetReadOnlyEntities().Single(owner => owner.BlueprintName == "TentRightHost");
                Assert.True(ConversationActions.TryExecute("RepairPlaceSite", host, Actor, "Gantry:GuestPole:Lasting"));
            }
            else Assert.True(ChoosePole(pole, PoleMenu(pole).Single(row => !row.IsDisabled && row.Command == Mend)));

            Assert.AreEqual(RepairStage.StableRepair, Site.Stage);
            Assert.AreEqual(RepairMethodId.ManualRepair, Site.ResolvedByMethod);
            Assert.IsNull(Site.RelapseAtTurn);
            Assert.False(Pack.Contains(cloak));
            Assert.False(Pack.Contains(exactPeg));
            Assert.That(Pack.Objects, Does.Contain(other));
            Assert.False(PoleMenu(pole).Any(row => row.Command == Mend));

            TradeSystem.SetDrams(Actor, 17);
            if (throughHost)
                Actor.GetStat("Hitpoints").BaseValue = Actor.GetStat("Hitpoints").Max;
            Actor.GetPart<StatusEffectsPart>().ForceApplyEffect(new BleedingEffect());
            Assert.True(Actor.HasEffect<BleedingEffect>());
            var row = PoleMenu(pole).Single(RestRow);
            Assert.That(row.Display + " " + row.Hint, Does.Contain("60"));
            int tick = Clock.TickCount;
            Assert.True(ChoosePole(pole, row));
            Assert.AreEqual(tick + RestSystem.RestClockTurns, Clock.TickCount);
            Assert.AreEqual(Actor.GetStat("Hitpoints").Max, Actor.GetStatValue("Hitpoints"));
            Assert.False(Actor.HasEffect<BleedingEffect>());
            Assert.AreEqual(17, TradeSystem.GetDrams(Actor));
            Assert.That(Pack.Objects, Does.Contain(other));
        }

        [Test]
        public void BothRepairEntriesRefuseAnAlteredRoughPegAndKeepTheCloak()
        {
            var pole = EnterPlace();
            var cloak = Carry("Cloak");
            var roughPeg = Carry("FieldHaftComponent");
            roughPeg.GetPart<WeaponComponentPart>().NameFragment = "altered fake peg";
            var host = Zone.GetReadOnlyEntities().Single(owner => owner.BlueprintName == "TentRightHost");

            Assert.False(PoleMenu(pole).Any(row => row.Command == Mend && !row.IsDisabled));
            Assert.False(Act(pole, Mend));
            Assert.False(ConversationActions.TryExecute("RepairPlaceSite", host, Actor, "Gantry:GuestPole:Lasting"));
            Assert.AreEqual(RepairStage.Fouled, Site.Stage);
            Assert.That(Pack.Objects, Does.Contain(cloak));
            Assert.That(Pack.Objects, Does.Contain(roughPeg));
            var warning = ExamineDocumentBuilder.BuildWorld(pole, Zone, Zone.GetEntityCell(pole)).Sections
                .Single(section => section.Heading == "Needs repair").Text;
            Assert.That(warning, Does.Contain("cloak").IgnoreCase.And.Contain("oak haft").IgnoreCase
                .And.Contain("field haft").IgnoreCase);
        }

        [TestCase("temporary")]
        [TestCase("healthy")]
        [TestCase("hostile")]
        public void RestReadinessRefusesUnusableClothOrUnsafeAndUnneededRest(string condition)
        {
            var pole = EnterPlace();
            Injure();
            Site.Stage = RepairStage.StableRepair;
            var selected = PoleMenu(pole).Single(RestRow);
            switch (condition)
            {
                case "temporary": Site.Stage = RepairStage.TemporarilyPurified; break;
                case "healthy": Actor.GetStat("Hitpoints").BaseValue = Actor.GetStat("Hitpoints").Max; break;
                case "hostile": BringDanger(pole); break;
            }
            int hp = Actor.GetStatValue("Hitpoints");
            int tick = Clock.TickCount;
            TradeSystem.SetDrams(Actor, 12);

            Assert.False(Act(pole, selected.Command));
            Assert.False(PoleMenu(pole).Any(RestRow));
            if (condition == "healthy")
                Assert.True(PoleMenu(pole).Any(row => row.IsDisabled && row.Display.StartsWith("rest", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual(hp, Actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(tick, Clock.TickCount);
            Assert.AreEqual(12, TradeSystem.GetDrams(Actor));
        }

        [Test]
        public void AStableLegacyGantryPoleRebindsWithoutRegenerationAndCanRest()
        {
            var pole = EnterPlace();
            Site.Stage = RepairStage.StableRepair;
            Injure();
            foreach (string partName in new[] { "RestoredPlaceRest", "PlaceSiteObject" })
            {
                var part = pole.GetPart(partName);
                if (part != null) Assert.True(pole.RemovePart(part));
            }
            var originalCell = Zone.GetEntityPosition(pole);
            using var stream = new MemoryStream();
            GameSessionState.Capture("gantry-guest-cloth", "before-pole-binding", manager, Clock, Actor).Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, Factory));
            manager = loaded.ZoneManager;
            Actor = loaded.Player;
            Zone = manager.ActiveZone;
            SettlementRuntime.ActiveZone = Zone;
            var restored = Zone.GetReadOnlyEntities().Single(owner => owner.BlueprintName == "GuestClothPole");
            Assert.AreEqual(pole.ID, restored.ID);
            Assert.AreEqual(originalCell, Zone.GetEntityPosition(restored));
            Assert.AreEqual(RepairStage.StableRepair, Site.Stage);
            Assert.NotNull(restored.GetPart<PlaceSiteObjectPart>());

            int tick = loaded.TurnManager.TickCount;
            Assert.True(ChoosePole(restored, PoleMenu(restored).Single(RestRow)));
            Assert.AreEqual(tick + RestSystem.RestClockTurns, loaded.TurnManager.TickCount);
            Assert.AreEqual(Actor.GetStat("Hitpoints").Max, Actor.GetStatValue("Hitpoints"));
        }

        [TestCase(FirstTentCompositionPlan.ZoneID)]
        [TestCase(WellmeetCompositionPlan.ZoneID)]
        public void TheSharedPoleBlueprintDoesNotCarryGantrysRestToAnotherSettlement(string zoneId)
        {
            var pole = EnterPlace(zoneId);
            Site.Stage = RepairStage.StableRepair;
            Injure();
            var poles = Zone.GetReadOnlyEntities().Where(owner => owner.BlueprintName == "GuestClothPole").ToArray();
            Assert.That(poles, Is.Not.Empty);
            Assert.That(poles.All(owner => owner.GetPart<PlaceSiteObjectPart>()?.SiteId != GuestPole), Is.True);
            Assert.False(PoleMenu(pole).Any(RestRow));
            Assert.False(Act(pole, Rest));
            Assert.AreEqual(4, Actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, Clock.TickCount);
        }

        [Test]
        public void DangerAtCommitCancelsAnAdmittedRestWithoutHealingOrAdvancing()
        {
            var pole = EnterPlace();
            Site.Stage = RepairStage.StableRepair;
            Injure(bleeding: true);
            var row = PoleMenu(pole).Single(RestRow);
            var transaction = new InventoryTransaction();
            var action = GameEvent.New("InventoryAction");
            try
            {
                action.SetParameter("Actor", (object)Actor);
                action.SetParameter("Zone", (object)Zone);
                action.SetParameter("Command", row.Command);
                action.SetParameter("InventoryTransaction", (object)transaction);
                pole.FireEvent(action);
                Assert.True(action.Handled);
                BringDanger(pole);
                Assert.Throws<InvalidOperationException>(transaction.Commit);
            }
            finally
            {
                action.Release();
                transaction.Rollback();
            }
            Assert.AreEqual(4, Actor.GetStatValue("Hitpoints"));
            Assert.True(Actor.HasEffect<BleedingEffect>());
            Assert.AreEqual(0, Clock.TickCount);
        }
    }
}
