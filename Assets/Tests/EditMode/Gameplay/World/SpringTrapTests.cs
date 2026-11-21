using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The spring trap (Docs/MACHINE-INVENTED-USES.md 3v): a kit loaded with one thing you carry (fire moss, frost lichen, spark root) and laid on a cell. The next thing to step there sets it off: the load is written into the cell
    /// through the tile-state routines every ability uses, so oil ignites, water freezes or carries a shock; without something to catch it the load only marks the cell. It is spent when it springs, and the cartridge is spent loading it.
    /// </summary>
    public sealed class SpringTrapTests : FiftyWorldFixture
    {
        [SetUp] public void SetupTrap()
        {
            Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 1000; TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void TearTrap() { TileReactionSystem.ResetForTests(); }

        InventoryAction[] LoadRows(Entity kit) => Actions(kit).Where(a => a.Name == "LoadSpringTrap").ToArray();
        InventoryAction[] LayRows(Entity kit) => Actions(kit).Where(a => a.Name == "LaySpringTrap").ToArray();
        SpringTrapKitPart Part(Entity kit) => kit.GetPart<SpringTrapKitPart>();
        int Got(string bp) => Pack.Objects.Where(o => o.BlueprintName == bp).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        Entity LoadedKit(string cartridge)
        {
            var kit = Carry("SpringTrap"); Carry(cartridge); var row = LoadRows(kit).Single(r => r.Command.EndsWith(cartridge)); Assert.True(Act(kit, row.Command), string.Join("|", MessageLog.GetRecent(3))); return kit;
        }
        /// <summary>Lay the loaded kit east of the actor and return the device.</summary>
        Entity LayEast(Entity kit)
        {
            var row = LayRows(kit).Single(r => r.Display.EndsWith(" east")); Assert.True(Act(kit, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            var device = Zone.GetCell(11, 10).Objects.FirstOrDefault(e => e.BlueprintName == "SpringTrapLaid"); Assert.NotNull(device); return device;
        }
        void Step() => Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0));
        int Hp() => Actor.GetStatValue("Hitpoints");

        // ── loading ──

        [Test] public void AnUnloadedKitOffersOneRowPerCartridgeYouCarry_AndNoLayRowsYet()
        {
            var kit = Carry("SpringTrap"); Carry("FireMoss"); Carry("SparkRoot"); var rows = LoadRows(kit);
            Assert.AreEqual(2, rows.Length); StringAssert.Contains("fire moss", rows[0].Display + rows[1].Display); Assert.IsEmpty(LayRows(kit), "it must be loaded before it can be laid");
        }

        [Test] public void WithNothingToLoadItOffersOneDisabledRowThatNamesWhatItTakes()
        {
            var kit = Carry("SpringTrap"); var rows = LoadRows(kit); Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual("nothing-to-load", rows[0].DisabledCode); StringAssert.Contains("fire moss, frost lichen or spark root", rows[0].DisabledReason);
        }

        [TestCase("FireMoss", "heat")] [TestCase("FrostLichen", "cold")] [TestCase("SparkRoot", "charge")]
        public void LoadingSpendsOneCartridgeAndSetsTheKindOnce(string cartridge, string kind)
        {
            var kit = Carry("SpringTrap"); Carry(cartridge); Carry(cartridge); Assert.AreEqual(2, Got(cartridge));
            Assert.True(Act(kit, LoadRows(kit).Single().Command)); Assert.AreEqual(kind, Part(kit).Kind); Assert.AreEqual(cartridge, Part(kit).Loaded); Assert.AreEqual(1, Got(cartridge), "one spent, one kept");
            Assert.IsEmpty(LoadRows(kit), "a loaded kit cannot be loaded again"); Assert.AreEqual(8, LayRows(kit).Length, "and now it can be laid in any open direction");
        }

        [Test] public void AnInterruptedLoadingKeepsTheCartridgeAndLeavesTheKitEmpty()
        {
            var kit = Carry("SpringTrap"); Carry("FireMoss"); string command = LoadRows(kit).Single().Command; FailAfter();
            Assert.False(Act(kit, command)); Assert.AreEqual(1, Got("FireMoss")); Assert.AreEqual("", Part(kit).Loaded); Assert.AreEqual("", Part(kit).Kind);
        }

        [Test] public void AStaleLoadCommandIsRefused_Counter()
        {
            var kit = Carry("SpringTrap"); Assert.False(Act(kit, SpringTrapKitPart.LoadPrefix + "FireMoss"), "none carried"); Assert.False(Act(kit, SpringTrapKitPart.LoadPrefix + "Dagger")); Carry("FireMoss"); Assert.False(Act(kit, SpringTrapKitPart.LoadPrefix + "Dagger"), "not a cartridge");
            Assert.AreEqual("", Part(kit).Loaded); Assert.AreEqual(1, Got("FireMoss"));
        }

        // ── laying ──

        [Test] public void LayingSpendsTheKitAndLeavesAnArmedDevice()
        {
            var kit = LoadedKit("FireMoss"); var device = LayEast(kit); Assert.False(Pack.Objects.Contains(kit), "the kit is spent"); var trigger = device.GetPart<SpringTrapPart>(); Assert.AreEqual("heat", trigger.Kind); Assert.True(trigger.Armed);
            Assert.False(device.GetPart<LaidDevicePart>() != null && device.GetPart<LaidDevicePart>().ItemBlueprint != "", "a laid trap cannot be taken up");
        }

        [Test] public void AnInterruptedLayingKeepsTheLoadedKitAndLeavesNoTrap()
        {
            var kit = LoadedKit("FrostLichen"); string command = LayRows(kit).Single(r => r.Display.EndsWith(" east")).Command; FailAfter();
            Assert.False(Act(kit, command)); Assert.True(Pack.Objects.Contains(kit)); Assert.AreEqual("cold", Part(kit).Kind); Assert.IsNull(Zone.GetCell(11, 10).Objects.FirstOrDefault(e => e.BlueprintName == "SpringTrapLaid"));
        }

        [Test] public void AnUnarmedTrapDoesNothingAndIsNotConsumed_Counter()
        {
            var device = Factory.CreateEntity("SpringTrapLaid"); device.GetPart<SpringTrapPart>().Kind = "heat"; device.GetPart<SpringTrapPart>().Armed = false; Assert.True(Zone.AddEntity(device, 11, 10));
            Zone.TileState.WriteCoating(11, 10, "oil", 50); int hp = Hp(); Step(); Assert.AreEqual(hp, Hp()); Assert.NotNull(Zone.GetCell(11, 10).Objects.FirstOrDefault(e => e == device), "still there");
        }

        // ── springing ──

        [Test] public void AFireMossTrapOnOilBurnsTheNextThingToStepThere_AndIsSpent()
        {
            var kit = LoadedKit("FireMoss"); var device = LayEast(kit); Zone.TileState.WriteCoating(11, 10, "oil", 50); int hp = Hp();
            Step(); Assert.Less(Hp(), hp, "the oil went up under you"); Assert.IsNull(Zone.GetCell(11, 10).Objects.FirstOrDefault(e => e.BlueprintName == "SpringTrapLaid"), "the trap is spent");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "SpringTrapSprung", Limit = 5 }).Records.Count);
        }

        [Test] public void AFireMossTrapWithNothingToCatchItMarksTheCellAndIsStillSpent_Counter()
        {
            var kit = LoadedKit("FireMoss"); LayEast(kit); int hp = Hp(); Step(); Assert.AreEqual(hp, Hp(), "no oil, no fire: the load found nothing to catch"); Assert.IsNull(Zone.GetCell(11, 10).Objects.FirstOrDefault(e => e.BlueprintName == "SpringTrapLaid"));
        }

        [Test] public void AFrostLichenTrapOnWaterFreezesIt_AndFreezesWhoeverStands()
        {
            var kit = LoadedKit("FrostLichen"); LayEast(kit); Zone.TileState.WriteCoating(11, 10, "water", 50); Step();
            Assert.True(Zone.TileState.HasCoating(11, 10, "ice"), "the water froze"); Assert.False(Zone.TileState.HasCoating(11, 10, "water")); Assert.True(Actor.HasEffect<FrozenEffect>(), "and whoever stood there is frozen");
        }

        [Test] public void ASparkRootTrapOnWaterShocksTheNextThingToStepThere()
        {
            var kit = LoadedKit("SparkRoot"); LayEast(kit); Zone.TileState.WriteCoating(11, 10, "water", 50); int hp = Hp(); Step(); Assert.Less(Hp(), hp, "the standing water carried the shock"); Assert.True(Actor.HasEffect<ElectrifiedEffect>());
        }

        [Test] public void ATrapIsBlindToWhoLaidIt_ItSpringsOnTheLayerToo_Counter()
        {
            var kit = LoadedKit("FireMoss"); LayEast(kit); Zone.TileState.WriteCoating(11, 10, "oil", 50); int hp = Hp(); Step(); Assert.Less(Hp(), hp, "you stepped on your own trap");
        }

        [Test] public void AHeatTrapIsRefusedOnTheChoirsGround_AndAColdOneIsNot()
        {
            string groveId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => { var (x, y, z) = WorldMap.FromZoneID(id); return WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Grovelands; });
            Zone.RemoveEntity(Actor); Zone = new Zone(groveId); Assert.True(Zone.AddEntity(Actor, 10, 10)); SettlementRuntime.ActiveZone = Zone; Assert.True(GroveLaw.IsChoirGround(Zone), "fixture: grove ground");
            var kit = LoadedKit("FireMoss"); var rows = LayRows(kit); Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual("grove-law", rows[0].DisabledCode);
            Assert.False(Act(kit, SpringTrapKitPart.LayPrefix + System.Uri.EscapeDataString(groveId) + "|10|10|11|10")); Assert.True(Pack.Objects.Contains(kit), "the refused kit is kept");
            var cold = LoadedKit("FrostLichen"); Assert.AreEqual(8, LayRows(cold).Length, "frost and charge are not fire");
        }

        // ── data and saves ──

        [Test] public void TheKitAndTheLaidTrapRideTheSaveAndALoadedKitsDoNotStack_Counter()
        {
            var kit = LoadedKit("SparkRoot"); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(kit).GetPart<SpringTrapKitPart>(); Assert.AreEqual("SparkRoot", copy.Loaded); Assert.AreEqual("charge", copy.Kind);
            var other = Carry("SpringTrap"); Assert.AreEqual(2, Pack.Objects.Count(o => o.BlueprintName == "SpringTrap"), "a loaded and an empty kit are two things, never one stack");
            var device = Factory.CreateEntity("SpringTrapLaid"); device.GetPart<SpringTrapPart>().Kind = "cold"; var back = PartRoundTripHelper.RoundTripEntityViaTokenGraph(device).GetPart<SpringTrapPart>(); Assert.AreEqual("cold", back.Kind); Assert.True(back.Armed);
        }

        [Test] public void TheReaderSaysWhatItNeedsThenWhatItIsLoadedWith()
        {
            var kit = Carry("SpringTrap"); var l = new List<string>(); StateReader.Collect(kit, true, Zone, null, Actor, l); StringAssert.Contains("fire moss, frost lichen or spark root", string.Join(" ", l));
            Carry("FrostLichen"); Assert.True(Act(kit, LoadRows(kit).Single().Command)); l.Clear(); StateReader.Collect(kit, true, Zone, null, Actor, l); StringAssert.Contains("Loaded with frost lichen", string.Join(" ", l));
        }

        [Test] public void TheTrapIsStockedAtTheTinker()
        {
            Assert.True(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")).Contains("\"SpringTrap\"")); Assert.NotNull(Factory.CreateEntity("SpringTrap").GetPart<SpringTrapKitPart>());
        }

        // ── the cell's own interact (C) menu: laying needs no trip through the pack ──

        [Test] public void TheCellsOwnMenuOffersToLayTheLoadedTrapThere_RunByTheKit_AndOnlyBesideYou_AndAnUnloadedKitOffersNone()
        {
            Place("Grass", 11, 10); Place("Grass", 14, 10); var kit = Carry("SpringTrap"); Assert.IsEmpty(CellMenu(11, 10).Where(a => a.Name == "LaySpringTrap"), "unloaded: nothing to lay yet"); Carry("FireMoss");
            Assert.True(Act(kit, LoadRows(kit).Single().Command)); var rows = CellMenu(11, 10).Where(a => a.Name == "LaySpringTrap").ToArray(); Assert.AreEqual(1, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display)));
            Assert.AreSame(kit, rows[0].Via); Assert.AreEqual(LayRows(kit).Single(r => r.Display.EndsWith(" east")).Command, rows[0].Command, "the very command the pack row runs"); Assert.AreEqual(ActionTime.OneAction, rows[0].Time);
            Assert.IsEmpty(CellMenu(14, 10).Where(a => a.Name == "LaySpringTrap"), "out of reach"); Assert.True(ChooseRow(rows[0], 11, 10), string.Join("|", MessageLog.GetRecent(3)));
            Assert.NotNull(Zone.GetCell(11, 10).Objects.FirstOrDefault(e => e.BlueprintName == "SpringTrapLaid"), "the trap lies on the cell you chose"); Assert.IsEmpty(CellMenu(11, 10).Where(a => a.Name == "LaySpringTrap"), "a cell that holds a device is not offered another");
        }

        [Test] public void OnTheChoirsGroundEachOpenCellSaysWhyFireIsNotLaidThere_AndFrostIsOfferedPlainly()
        {
            string groveId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => { var (x, y, z) = WorldMap.FromZoneID(id); return WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Grovelands; });
            Zone.RemoveEntity(Actor); Zone = new Zone(groveId); Assert.True(Zone.AddEntity(Actor, 10, 10)); SettlementRuntime.ActiveZone = Zone; Assert.True(GroveLaw.IsChoirGround(Zone), "fixture: grove ground");
            Place("Grass", 11, 10); Place("Grass", 10, 11); var kit = LoadedKit("FireMoss");
            var east = CellMenu(11, 10).Where(a => a.Name == "LaySpringTrap").ToArray(); var south = CellMenu(10, 11).Where(a => a.Name == "LaySpringTrap").ToArray();
            Assert.AreEqual(1, east.Length); Assert.True(east[0].IsDisabled); Assert.AreEqual("grove-law", east[0].DisabledCode); Assert.AreEqual(1, south.Length); Assert.True(south[0].IsDisabled, "each open cell offers the refusal, not only the first");
            Assert.False(east[0].Display.Contains("east"), "the one refusal row names no direction, so it reads right on every cell"); Assert.AreEqual(1, LayRows(kit).Length, "the pack still shows one refusal row");
            var cold = LoadedKit("FrostLichen"); Assert.AreEqual(1, CellMenu(11, 10).Count(a => a.Name == "LaySpringTrap" && !a.IsDisabled && a.Via == cold), "frost is not fire: offered");
        }

        [Test] public void OnTheChoirsGroundTheRefusalIsListedWithTheOtherLayRows_NotLostWhenThoseGroup()
        {
            string groveId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => { var (x, y, z) = WorldMap.FromZoneID(id); return WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Grovelands; });
            Zone.RemoveEntity(Actor); Zone = new Zone(groveId); Assert.True(Zone.AddEntity(Actor, 10, 10)); SettlementRuntime.ActiveZone = Zone; Assert.True(GroveLaw.IsChoirGround(Zone), "fixture: grove ground");
            Place("Grass", 11, 10); Carry("HushStrip"); Carry("KnotflaxCord"); LoadedKit("FireMoss");
            var group = CellMenu(11, 10).Where(a => a.Name == "ToolGroup" && a.Command.EndsWith(ToolGroups.Lay)).ToArray(); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); StringAssert.Contains("(3)", group[0].Display);
            Assert.True(WorldInteractionSystem.BuildToolGroupActions(Zone.GetCell(11, 10), Actor, ToolGroups.Lay).Any(a => a.DisabledCode == "grove-law"), "the refusal is in the list, where the player will look for the trap");
        }
    }
}
