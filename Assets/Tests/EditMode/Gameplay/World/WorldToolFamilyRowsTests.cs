using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The water, fire and planting tools on the interact (C) menu of the thing they act on (Docs/WORLD-TOOL-ROWS.md): a flask fills from a pool and pours on a cell, a waterskin fills from a well or pool and douses a burning thing,
    /// a seed is planted in a prepared bed. Each row is the pack row's own command run by the carried item, offered only on the cell it acts on and only within reach, and each has the counter that proves the gate.
    /// </summary>
    public sealed class WorldToolFamilyRowsTests : FiftyWorldFixture
    {
        [SetUp] public void SetupFamilies() { Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 1000; }

        InventoryAction[] Rows(int x, int y, string name) => CellMenu(x, y).Where(a => a.Name == name).ToArray();
        Entity BurningProp(int x = 11, int y = 10)
        {
            var prop = Place("PhysicalObject", x, y); prop.AddPart(new ThermalPart { Temperature = 450, FlameTemperature = 300 });
            prop.AddPart(new FuelPart { FuelMass = 20, MaxFuel = 20 }); prop.AddPart(new MaterialPart { MaterialTagsRaw = "Wood,Flammable" }); Assert.True(prop.ApplyEffect(new BurningEffect(1)), "fixture: it burns"); return prop;
        }

        // ── liquid vessels ──

        [Test] public void APoolsOwnMenuOffersToFillTheFlask_RunByTheFlask_AndNoOtherCellDoes()
        {
            var pool = Pool(5, "water", 11, 10); Place("Grass", 12, 10); Assert.IsEmpty(Rows(11, 10, "FillLiquid"), "no vessel carried"); var flask = Flask(0, "water", 12);
            var rows = Rows(11, 10, "FillLiquid"); Assert.AreEqual(1, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); Assert.AreSame(flask, rows[0].Via);
            Assert.AreEqual(Choice(flask, "FillLiquidVessel|"), rows[0].Command, "the very command the pack row runs"); Assert.AreEqual(ActionTime.OneAction, rows[0].Time); Assert.IsEmpty(Rows(12, 10, "FillLiquid"), "a cell with no pool");
            Assert.True(ChooseRow(rows[0], 11, 10), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(5, Units(flask)); Assert.AreEqual(0, PoolUnits(11, 10));
        }

        Entity Tray()
        {
            var tray = Factory.CreateEntity("PhysicalObject"); tray.GetPart<PhysicsPart>().Solid = true; tray.GetPart<PhysicsPart>().Takeable = false;
            tray.AddPart(new ContainerPart()); tray.AddPart(new ContainedLiquidSourcePart()); Assert.True(tray.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("CinderhookAxeHeadComponent")));
            Assert.True(Zone.AddEntity(tray, 11, 10)); Zone.GetCell(11, 10).IsVisible = Zone.GetCell(11, 10).Explored = true; return tray;
        }

        [Test] public void ATraysOwnMenuOffersToDrawItsLiquidIntoTheFlask_RunByTheFlask()
        {
            var tray = Tray(); Assert.IsEmpty(Rows(11, 10, "FillLiquid"), "no vessel carried"); var flask = Flask(0, "water", 12);
            var rows = Rows(11, 10, "FillLiquid"); Assert.AreEqual(1, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); Assert.AreSame(flask, rows[0].Via);
            Assert.AreEqual("FillLiquidVessel|" + Id(tray) + "|oil", rows[0].Command, "the very command the pack row runs"); StringAssert.Contains("tray", rows[0].Display); Assert.IsEmpty(Rows(12, 10, "FillLiquid"));
            Assert.True(ChooseRow(rows[0], 11, 10), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(12, Units(flask));
        }

        [Test] public void TwoFlasksThatHoldDifferentlyEachOfferToFillFromThePool_AndSayWhichIsWhich()
        {
            Pool(9, "water", 11, 10); var empty = Flask(0, "water", 12); var part = Flask(5, "water", 12); var other = Flask(5, "water", 12);
            var rows = Rows(11, 10, "FillLiquid"); Assert.AreEqual(2, rows.Length, string.Join(" / ", rows.Select(r => r.Display)));
            Assert.AreEqual(1, rows.Count(r => r.Via == empty && r.Display.Contains("(empty)"))); Assert.AreEqual(1, rows.Count(r => (r.Via == part || r.Via == other) && r.Display.Contains("(5/12 water)")), "two flasks that hold the same are one choice");
        }

        [Test] public void ACellOffersToPourTheFlaskOnIt_BothWays_AndTheCellYouStandInOffersItToo()
        {
            var flask = Flask(3, "oil"); Place("Grass", 11, 10); Place("Grass", 10, 10);
            var rows = CellMenu(11, 10).Where(a => a.Name == "PourLiquid" || a.Name == "PourOneLiquid").ToArray(); Assert.AreEqual(2, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display)));
            Assert.True(rows.All(r => r.Via == flask)); Assert.AreEqual(Pour(flask, false), rows.Single(r => r.Name == "PourLiquid").Command); Assert.AreEqual(Pour(flask, true), rows.Single(r => r.Name == "PourOneLiquid").Command);
            Assert.AreEqual(2, CellMenu(10, 10).Count(a => a.Name == "PourLiquid" || a.Name == "PourOneLiquid"), "the 'here' pair belongs to the cell you stand in");
            Assert.IsEmpty(CellMenu(14, 10).Where(a => a.Name == "PourLiquid" || a.Name == "PourOneLiquid"), "a cell out of reach");
            Assert.True(ChooseRow(rows.Single(r => r.Name == "PourOneLiquid"), 11, 10), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(2, Units(flask));
        }

        [Test] public void AnEmptyFlaskOffersNothingToPour_Counter() { Flask(0); Place("Grass", 11, 10); Assert.IsEmpty(CellMenu(11, 10).Where(a => a.Name.StartsWith("Pour"))); }

        // ── a burning thing ──

        [Test] public void ABurningThingsOwnMenuOffersToDouseItWithTheSkin_RunBySkin()
        {
            var prop = BurningProp(); Assert.IsEmpty(Rows(11, 10, "DouseWorld"), "no water carried"); var water = Skin(3);
            var rows = Rows(11, 10, "DouseWorld"); Assert.AreEqual(1, rows.Length); Assert.AreSame(water, rows[0].Via); Assert.AreEqual("DouseWorld|" + Id(prop), rows[0].Command); Assert.AreEqual(ActionTime.OneAction, rows[0].Time);
            var calm = Place("PhysicalObject", 10, 11); Assert.IsEmpty(Rows(10, 11, "DouseWorld"), "a thing that is not burning is not offered");
            Assert.True(ChooseRow(rows[0], 11, 10)); Assert.AreEqual(2, Units(water)); Assert.False(prop.HasEffect<BurningEffect>());
        }

        [Test] public void ABurningThingsOwnMenuOffersToSmotherItWithFireClay_ButABurningCreatureIsNotTheEnvironment_Counter()
        {
            var prop = BurningProp(); var clay = Carry("FireClay"); var rows = Rows(11, 10, "SmotherFire"); Assert.AreEqual(1, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display)));
            Assert.AreSame(clay, rows[0].Via); Assert.AreEqual(Actions(clay).Single(a => a.Name == "SmotherFire" && a.Command.Contains(Id(prop))).Command, rows[0].Command, "the very command the pack row runs");
            var creature = Place("MarlbackScrabbler", 10, 11); var heat = creature.GetPart<ThermalPart>(); heat.FlameTemperature = 400; heat.HeatCapacity = 1.6f; Assert.True(creature.ApplyEffect(new BurningEffect(1))); heat.Temperature = 500;
            Assert.True(Actions(clay).Any(a => a.Name == "SmotherFire" && a.Command.Contains(Id(creature))), "fixture: the pack offers to smother the creature"); Assert.IsEmpty(Rows(10, 11, "SmotherFire"), "a creature is care, not work on the environment");
            Assert.True(ChooseRow(rows[0], 11, 10), string.Join("|", MessageLog.GetRecent(3))); Assert.False(prop.HasEffect<BurningEffect>());
        }

        // ── cooking at a fire ──

        [Test] public void ACampfiresOwnMenuOffersToCookTheRawStack_RunByTheFood_AndNoOtherCellDoes()
        {
            Place("Campfire", 11, 10); Place("Grass", 10, 11); Assert.IsEmpty(Rows(11, 10, "Cook"), "nothing raw carried"); var raw = Carry("Emberwheat");
            var rows = Rows(11, 10, "Cook"); Assert.AreEqual(1, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); Assert.AreSame(raw, rows[0].Via); Assert.AreEqual("Cook", rows[0].Command); Assert.AreEqual(ActionTime.OneAction, rows[0].Time);
            Assert.IsEmpty(Rows(10, 11, "Cook"), "a cell with no fire"); Place("Campfire", 14, 10); Assert.IsEmpty(Rows(14, 10, "Cook"), "a fire out of reach");
            int raws = Count("Emberwheat"); Assert.True(ChooseRow(rows[0], 11, 10), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(0, Count("Emberwheat")); Assert.AreEqual(raws, Count("ToastedEmberwheat"), "the stack is cooked");
        }

        // ── the waterskin's fill, from the source you chose ──

        [Test] public void APoolOfWaterOffersToFillTheSkinFromIt_NamingThatSource_AndFillsIt()
        {
            var skin = Skin(0); var pool = Pool(5, "water", 11, 10); var rows = Rows(11, 10, "FillWater"); Assert.AreEqual(1, rows.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display)));
            Assert.AreSame(skin, rows[0].Via); Assert.AreEqual("FillWaterskin|" + Id(pool), rows[0].Command); Assert.AreEqual(ActionTime.OneAction, rows[0].Time); StringAssert.Contains("fill the waterskin", rows[0].Display);
            Assert.True(ChooseRow(rows[0], 11, 10), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(3, Units(skin)); Assert.AreEqual(2, PoolUnits(11, 10));
        }

        [Test] public void TheFillTakesFromTheSourceYouChose_NotTheFirstBesideYou()
        {
            var skin = Skin(0); Pool(5, "water", 11, 10); Pool(5, "water", 10, 11);
            foreach (var (x, y, other) in new[] { (11, 10, (10, 11)), (10, 11, (11, 10)) })
            {
                skin.GetPart<WaterskinPart>().Charges = 0; PoolUnitsReset(); var row = Rows(x, y, "FillWater").Single();
                Assert.True(ChooseRow(row, x, y)); Assert.AreEqual(2, PoolUnits(x, y), "drawn from the pool you chose"); Assert.AreEqual(5, PoolUnits(other.Item1, other.Item2), "the other was never touched");
            }
        }
        void PoolUnitsReset() { foreach (var p in Zone.GetReadOnlyEntities().Where(e => e.HasPart<LiquidPoolPart>())) p.GetPart<LiquidPoolPart>().Volume = 5; }

        [Test] public void NoFillRowForAFullSkin_ABrinePool_OrWaterTwoCellsOff_Counter()
        {
            var skin = Skin(3); Pool(5, "water", 11, 10); Assert.IsEmpty(Rows(11, 10, "FillWater"), "the skin is full");
            skin.GetPart<WaterskinPart>().Charges = 0; Assert.AreEqual(1, Rows(11, 10, "FillWater").Length, "fixture: offered once there is room");
            Pool(5, "brine", 10, 11); Assert.IsEmpty(Rows(10, 11, "FillWater"), "brine is not fresh water"); Pool(5, "water", 12, 10); Assert.IsEmpty(Rows(12, 10, "FillWater"), "two cells off");
        }

        [Test] public void AFillRowGoneStaleIsRefusedAndChangesNothing_AndAMalformedFillIsRefused()
        {
            var skin = Skin(0); var pool = Pool(5, "water", 11, 10); var row = Rows(11, 10, "FillWater").Single();
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 18, 10)); Assert.False(ChooseRow(row, 11, 10), "you have walked away since the menu was drawn"); Assert.AreEqual(0, Units(skin)); Assert.AreEqual(5, PoolUnits(11, 10));
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 10, 10)); Zone.RemoveEntity(pool); Assert.False(ChooseRow(row, 11, 10), "the water is gone"); Assert.AreEqual(0, Units(skin));
            Assert.False(Act(skin, "FillWaterskin|")); Assert.False(Act(skin, "FillWaterskin|%zz")); Assert.False(Act(skin, "FillWaterskin|" + Id(Actor))); Assert.AreEqual(0, Units(skin));
        }

        [Test] public void AStunnedPlayerIsOfferedNoFillRow_Counter()
        {
            Skin(0); Pool(5, "water", 11, 10); Assert.AreEqual(1, Rows(11, 10, "FillWater").Length, "fixture: offered while able");
            Assert.True(Actor.ApplyEffect(new StunnedEffect(duration: 5)), "fixture: the stun lands"); Assert.True(Actor.GetPart<StatusEffectsPart>().IsActionBlocked(), "fixture: stunned"); Assert.IsEmpty(Rows(11, 10, "FillWater"));
        }

        [Test] public void ThePlainFillStillTakesTheFirstFreshSourceBesideYou_TheOldCommandIsUnchanged()
        {
            var skin = Skin(0); Pool(5, "water", 11, 10); Assert.True(Act(skin, "FillWaterskin")); Assert.AreEqual(3, Units(skin));
        }

        // ── a seed in a prepared bed ──

        [Test] public void APreparedBedOffersToPlantTheSeed_AndYourOwnBedToo_ButBareGroundDoesNot_Counter()
        {
            Bed(11, 10); Bed(10, 10); Place("Grass", 12, 10); Assert.IsEmpty(Rows(11, 10, "PlantAdjacent"), "no seed carried"); var seed = Seed();
            var adjacent = Rows(11, 10, "PlantAdjacent"); Assert.AreEqual(1, adjacent.Length); Assert.AreSame(seed, adjacent[0].Via); Assert.AreEqual(ActionTime.OneAction, adjacent[0].Time);
            Assert.AreEqual(Actions(seed).Single(a => a.Name == "PlantAdjacent" && a.Display.Contains("(11,10)")).Command, adjacent[0].Command, "the very command the pack row runs");
            var here = Rows(10, 10, "Plant"); Assert.AreEqual(1, here.Length, "the bed you stand on"); Assert.AreEqual("PlantSeed", here[0].Command);
            Assert.AreEqual(ActionTime.Free, here[0].Time, "the plain plant costs no turn in the pack, so none from the menu either"); Assert.AreEqual(ActionTime.Free, Actions(seed).Single(a => a.Name == "Plant").Time);
            Assert.IsEmpty(CellMenu(12, 10).Where(a => a.Name == "Plant" || a.Name == "PlantAdjacent"), "bare ground is not a prepared bed");
            Assert.True(ChooseRow(adjacent[0], 11, 10), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(1, Zone.GetCell(11, 10).Objects.Count(o => o.HasPart<CropPart>()));
            Assert.IsEmpty(Rows(11, 10, "PlantAdjacent"), "a bed that holds a crop is not offered again");
        }

        [Test] public void YourOwnCellOffersToPlantOnlyWhereTheSeedCouldTake_Counter()
        {
            var seed = Seed(); Place("Grass", 10, 10); Assert.IsEmpty(Rows(10, 10, "Plant"), "plantable but untilled: this seed needs a prepared bed");
            Assert.True(Actions(seed).Any(a => a.Name == "Plant"), "fixture: the pack still lists it"); Zone.GetCell(10, 10).Objects.First(o => o.HasTag("Plantable")).AddPart(new CultivatedSoilPart()); Assert.AreEqual(1, Rows(10, 10, "Plant").Length, "tilled now");
        }

        [Test] public void YourOwnCellOffersNoPlantingWhereACropAlreadyGrowsOrTheGroundIsBarren_Counter()
        {
            var seed = Seed(); Bed(10, 10); Assert.AreEqual(1, Rows(10, 10, "Plant").Length, "fixture: a tilled bed you stand on");
            var crop = Place("CandyCarrotCrop", 10, 10); Assert.IsEmpty(Rows(10, 10, "Plant"), "something already grows here"); Assert.True(Actions(seed).Any(a => a.Name == "Plant"), "the pack still lists it");
            Zone.RemoveEntity(crop); Assert.AreEqual(1, Rows(10, 10, "Plant").Length, "fixture: offered again once the crop is gone");
            Zone.GetCell(10, 10).Objects.First(o => o.HasTag("Plantable")).SetTag("Barren"); Assert.IsEmpty(Rows(10, 10, "Plant"), "barren ground takes nothing");
        }

        [Test] public void APlainSeedOffersToPlantOnOrdinaryGroundButNotOnBarrenGround_Counter()
        {
            var seed = Carry("CandyCarrotSeed"); var grass = Place("Grass", 10, 10); Assert.False(seed.GetPart<SeedPart>().RequireCultivatedSoil, "fixture: a seed that needs no tilled bed");
            Assert.AreEqual(1, Rows(10, 10, "Plant").Length, "plain plantable ground takes it"); grass.SetTag("Barren"); Assert.IsEmpty(Rows(10, 10, "Plant"), "barren ground takes nothing");
            Assert.True(Actions(seed).Any(a => a.Name == "Plant"), "the pack still lists it");
        }

        // ── each heading groups rows from different tools that are the same kind of work ──

        InventoryAction[] Groups(int x, int y, string heading) => CellMenu(x, y).Where(a => a.Name == "ToolGroup" && a.Command == WorldInteractionSystem.ToolGroupPrefix + heading).ToArray();
        List<InventoryAction> Listed(int x, int y, string heading) => WorldInteractionSystem.BuildToolGroupActions(Zone.GetCell(x, y), Actor, heading).Where(a => a.Via != null).ToList();

        [Test] public void AStripACordAndALoadedTrapAreOneLayRowThatOpensTheirList()
        {
            Place("Grass", 11, 10); var strip = Carry("HushStrip"); var cord = Carry("KnotflaxCord"); var trap = Carry("SpringTrap"); Carry("FireMoss"); Assert.True(Act(trap, Actions(trap).Single(a => a.Name == "LoadSpringTrap").Command), "fixture: a loaded trap");
            var group = Groups(11, 10, ToolGroups.Lay); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); StringAssert.Contains("(3)", group[0].Display);
            Assert.IsFalse(CellMenu(11, 10).Any(a => a.Name == "LayDevice" || a.Name == "LayCordSnare" || a.Name == "LaySpringTrap"), "not also inline"); CollectionAssert.AreEquivalent(new[] { strip, cord, trap }, Listed(11, 10, ToolGroups.Lay).Select(a => a.Via).ToArray());
        }

        [Test] public void ThreeVesselsAtAPoolAreOneFillRowThatOpensTheirList()
        {
            Pool(20, "water", 11, 10); Flask(0, "water", 12); Flask(5, "water", 12); Flask(3, "water", 12);
            var group = Groups(11, 10, ToolGroups.Fill); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); StringAssert.Contains("(3)", group[0].Display); Assert.AreEqual(3, Listed(11, 10, ToolGroups.Fill).Count(a => a.Name == "FillLiquid"));
        }

        [Test] public void ThreeSeedsAtAPreparedBedAreOnePlantRowThatOpensTheirList()
        {
            Bed(11, 10); Carry("CandyCarrotSeed"); Carry("EmberwheatSeed"); Carry("KnotflaxSeed");
            var group = Groups(11, 10, ToolGroups.Plant); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); StringAssert.Contains("(3)", group[0].Display); Assert.AreEqual(3, Listed(11, 10, ToolGroups.Plant).Count(a => a.Name == "PlantAdjacent"));
        }

        [Test] public void ThreeRawFoodsAtACampfireAreOneCookRowThatOpensTheirList()
        {
            Place("Campfire", 11, 10); Carry("Emberwheat"); Carry("Mushroom"); Carry("RawMeat");
            var group = Groups(11, 10, ToolGroups.Cook); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); StringAssert.Contains("(3)", group[0].Display); Assert.AreEqual(3, Listed(11, 10, ToolGroups.Cook).Count(a => a.Name == "Cook"));
        }

        [Test] public void GreaseGritAndABrineFilmAreOneSpreadRowThatOpensTheirList()
        {
            Place("Grass", 11, 10); foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true; Carry("FrogOil"); Carry("SilverSand"); Carry("GlimmerBrine");
            var group = Groups(11, 10, ToolGroups.Spread); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); StringAssert.Contains("(3)", group[0].Display);
            CollectionAssert.AreEquivalent(new[] { "SpreadGrease", "ScatterGrit", "MaterialField" }, Listed(11, 10, ToolGroups.Spread).Select(a => a.Name).ToArray());
        }

        [Test] public void ThreeSeedsOnTheBedYouStandOnAreOnePlantRowThatOpensTheirList()
        {
            Bed(10, 10); Carry("CandyCarrotSeed"); Carry("EmberwheatSeed"); Carry("KnotflaxSeed");
            var group = Groups(10, 10, ToolGroups.Plant); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(10, 10).Select(a => a.Display))); Assert.AreEqual(3, Listed(10, 10, ToolGroups.Plant).Count(a => a.Name == "Plant"));
        }

        [Test] public void ThreeVesselsAtATrayAreOneFillRowThatOpensTheirList()
        {
            Tray(); Flask(0, "water", 12); Flask(5, "oil", 12); Flask(3, "oil", 12);
            var group = Groups(11, 10, ToolGroups.Fill); Assert.AreEqual(1, group.Length, string.Join(" / ", CellMenu(11, 10).Select(a => a.Display))); StringAssert.Contains("(3)", group[0].Display); Assert.AreEqual(3, Listed(11, 10, ToolGroups.Fill).Count(a => a.Name == "FillLiquid"));
        }
    }
}
