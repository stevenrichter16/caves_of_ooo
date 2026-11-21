using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The proofing workshop scenario (Docs/MACHINE-PROOFING.md, hand-testing): the player starts with every machine, every reagent, flasks, fire clay, ten full sets of armor, status tonics and spell books,
    /// a proving post that calls enemies that burn, freeze, shock or corrode, and an environmental range of hazards for each element to test the resistances on.
    /// </summary>
    public sealed class ProofingWorkshopTests : FiftyWorldFixture
    {
        static readonly string[] Reagents = { "FireMoss", "EmberFruit", "PitchpodResin", "FrostLichen", "GlacierSalt", "ChillcressTip", "SparkRoot", "PrismreedPith", "GlimmerBrine", "SourmantleFold" };

        [SetUp] public void SetupWorkshop() { ProofingRegistry.ResetForTests(); EnhancementFactory.ForceReinitialize(); MessageLog.Clear(); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 40, 12)); }

        ScenarioContext Run() { var ctx = new ScenarioContext(Zone, Factory, Actor, Clock); new ProofingWorkshopShowcase().Apply(ctx); return ctx; }
        int Held(string blueprint) => Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        int Placed(string blueprint) => Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == blueprint);
        static string SlotOf(EntityFactory f, string blueprint) => f.CreateEntity(blueprint).GetPart<EquippablePart>()?.Slot ?? "";

        [Test] public void TenSetsOfFivePiecesEachInFiveDifferentSlots_AllRealBlueprints()
        {
            Assert.AreEqual(10, ProofingWorkshopShowcase.ArmorSets.Length);
            foreach (var set in ProofingWorkshopShowcase.ArmorSets)
            {
                Assert.AreEqual(5, set.Length, string.Join(",", set)); var slots = set.Select(b => SlotOf(Factory, b)).ToArray();
                Assert.AreEqual(5, slots.Distinct().Count(), "a set is worn together: " + string.Join(",", set) + " -> " + string.Join(",", slots));
                foreach (var b in set) Assert.NotNull(Factory.CreateEntity(b)?.GetPart<ArmorPart>(), b);
            }
            CollectionAssert.IsSupersetOf(ProofingWorkshopShowcase.ArmorSets.SelectMany(s => s).Select(b => SlotOf(Factory, b)).Distinct().ToArray(), new[] { "Body", "Head", "Feet", "Back" });
        }

        [Test] public void ThePlayerCarriesEveryMachineEveryReagentAndTheFixingsForALastingProofing()
        {
            Run(); foreach (var m in new[] { "CampTrough", "CampSettlingTray", "CampHearth", "CampStill", "CampBench", "CampBox" }) Assert.GreaterOrEqual(Held(m), 1, m);
            foreach (var r in Reagents) Assert.GreaterOrEqual(Held(r), 6, r); Assert.GreaterOrEqual(Held("LiquidFlask"), 12); Assert.GreaterOrEqual(Held("FireClay"), 20); Assert.GreaterOrEqual(Held("SalvagedTimber"), 20);
            Assert.GreaterOrEqual(Held("FlintStriker"), 1); foreach (var t in new[] { "FrostTonic", "LightningTonic" }) Assert.GreaterOrEqual(Held(t), 4, t);
            foreach (var g in new[] { "ArcBoltGrimoire", "IceLanceGrimoire", "EmberVeinGrimoire", "AcidSprayGrimoire" }) Assert.GreaterOrEqual(Held(g), 1, g);
            foreach (var element in ProofingRegistry.All) Assert.True(Reagents.Any(r => ProofingRegistry.ElementsOf(Factory.CreateEntity(r)).Contains(element)), element.Id);
        }

        [Test] public void EveryPieceOfEveryOfTheTenSetsIsInThePack_InTheQuantitiesTheSetsAddUpTo()
        {
            Run(); var want = ProofingWorkshopShowcase.ArmorSets.SelectMany(s => s).GroupBy(b => b).ToDictionary(g => g.Key, g => g.Count());
            foreach (var pair in want) Assert.GreaterOrEqual(Held(pair.Key), pair.Value, pair.Key + " (" + pair.Value + " sets use it)");
        }

        [Test] public void TheFirstFourFlasksHoldMireAndTheRestAreEmpty()
        {
            Run(); var flasks = Pack.Objects.Where(o => o.BlueprintName == "LiquidFlask").Select(o => o.GetPart<LiquidVesselPart>()).ToList();
            Assert.AreEqual(4, flasks.Count(f => f.LiquidId == "bog-mire" && f.Volume == 12)); Assert.GreaterOrEqual(flasks.Count(f => f.Volume == 0), 8);
        }

        [Test] public void EveryElementHasEnvironmentalHazardsToTestOn_AndAProvingPostToHand()
        {
            Run();
            foreach (var group in new[] { ("heat", new[] { "SteamVent", "AshBed", "FireTrap" }), ("cold", new[] { "FrostVent", "IceSheet", "RuneOfFrost" }), ("acid", new[] { "AcidPool", "MirePool" }), ("shock", new[] { "BrinePool" }) })
                foreach (var bp in group.Item2) Assert.GreaterOrEqual(Placed(bp), 2, group.Item1 + ": " + bp);
            var post = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "ElementalProvingPost"); var at = Zone.GetEntityPosition(post); var me = Zone.GetEntityPosition(Actor);
            Assert.LessOrEqual(Math.Max(Math.Abs(at.x - me.x), Math.Abs(at.y - me.y)), 1, "the post is right beside the start");
        }

        [Test] public void EveryElementHasAHazardTheSettlingTrayCanCatchFrom_SoTheWorkshopShowsAllFourCatches()
        {
            Run(); var kinds = Zone.GetReadOnlyEntities().Select(ProofingSources.ElementOf).Where(k => k != null).Select(k => k.Id).Distinct().OrderBy(k => k).ToList();
            CollectionAssert.AreEqual(new[] { "acid", "cold", "electric", "heat" }, kinds, "heat, cold, shock and acid each have a source on the range");
            Assert.IsNull(ProofingSources.ElementOf(Zone.GetReadOnlyEntities().First(e => e.BlueprintName == "FireTrap")), "counter: a fire trap is a hazard but not a steady source");
        }

        [Test] public void TheHazardGroupsAreApartSoOneElementIsTestedAtATime_Counter()
        {
            Run(); var heat = Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "SteamVent" || e.BlueprintName == "AshBed" || e.BlueprintName == "FireTrap").Select(e => Zone.GetEntityPosition(e)).ToList();
            var cold = Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "FrostVent" || e.BlueprintName == "IceSheet" || e.BlueprintName == "RuneOfFrost").Select(e => Zone.GetEntityPosition(e)).ToList();
            int closest = heat.SelectMany(h => cold.Select(c => Math.Max(Math.Abs(h.x - c.x), Math.Abs(h.y - c.y)))).Min(); Assert.GreaterOrEqual(closest, 12, "heat and cold are at opposite corners of the range");
        }

        [Test] public void ThePlayerCanSurviveTheRangeAndCarryTheLot()
        {
            Run(); Assert.GreaterOrEqual(Actor.GetStatValue("Hitpoints"), 300); int weight = Pack.GetCarriedWeight(); Assert.Greater(weight, 500, "it is a lot"); Assert.LessOrEqual(weight, Actor.GetStatValue("Strength") * 15, "and within what the player can carry");
        }

        [Test] public void TheWholeLoopWorksOnTheShippedLoadout_PressProofAStackedLeatherArmorAndMeetAnIceWight()
        {
            var ctx = Run(); var flask = Pack.Objects.First(o => o.BlueprintName == "LiquidFlask" && o.GetPart<LiquidVesselPart>().Volume == 0);
            for (int i = 0; i < 2; i++) { var lichen = Pack.Objects.First(o => o.BlueprintName == "FrostLichen"); Assert.True(Act(lichen, Actions(lichen).First(a => a.Name == "PressProofing" && !a.IsDisabled && a.Command.Contains(Id(flask))).Command)); }
            Assert.AreEqual(4, flask.GetPart<LiquidVesselPart>().Volume);
            // the trough, set up the way a set-down leaves it, beside the player
            var trough = Pack.Objects.First(o => o.BlueprintName == "CampTrough"); Pack.RemoveObject(trough);
            trough.AddPart(new InstallationMarkerPart { InstallationId = "player:" + trough.ID, Origin = HeldFixtureActions.PlayerOrigin, RetainZone = true }); var inst = trough.GetPart<InstallablePart>() ?? new InstallablePart(); inst.Mode = InstallablePart.Portable; inst.State = InstallablePart.Installed; if (trough.GetPart<InstallablePart>() == null) trough.AddPart(inst);
            var me = Zone.GetEntityPosition(Actor); Assert.True(Zone.AddEntity(trough, me.x - 1, me.y)); Zone.GetCell(me.x - 1, me.y).IsVisible = true; Zone.GetCell(me.x - 1, me.y).Explored = true;
            var leather = Pack.Objects.First(o => o.BlueprintName == "LeatherArmor"); Assert.Greater(leather.GetPart<StackerPart>().StackCount, 1, "fixture: the sets give a stack of leather armor");
            var row = WorldInteractionSystem.GatherActions(trough, Actor).First(a => a.Name == "ProofGarment" && !a.IsDisabled && a.Command.EndsWith("|cold") && a.Display.Contains("leather armor")); Assert.True(Act(trough, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            var proofed = Pack.Objects.Single(o => o.Parts.OfType<EnhancementProofed>().Any()); Assert.True(InventorySystem.Equip(Actor, proofed)); Assert.AreEqual(25, Actor.GetStatValue("ColdResistance"));
            var post = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "ElementalProvingPost"); var call = WorldInteractionSystem.GatherActions(post, Actor).Single(a => a.Command == "CallProvingFoe|IceWight"); Assert.True(Act(post, call.Command));
            Assert.NotNull(Zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "IceWight"));
        }

        [Test] public void TheWholeWorkshopCarriedKeepsAnOrdinaryCellsInteractMenuShort_NoRowRepeatsAndIdenticalFlasksAreOneChoice()
        {
            Run(); foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true;
            var origin = Zone.GetEntityCell(Actor); int worst = 0;
            foreach (var (dx, dy) in new[] { (0, 1), (-1, 0), (-1, 1), (1, 1) })
            {
                var tools = CellMenu(origin.X + dx, origin.Y + dy).Where(a => a.Via != null).ToList(); worst = System.Math.Max(worst, tools.Count);
                Assert.AreEqual(tools.Count, tools.Select(a => a.Display).Distinct().Count(), "no row reads the same twice: " + string.Join(" | ", tools.Select(a => a.Display)));
            }
            Assert.LessOrEqual(worst, 6, "four identical mire flasks, a brine, a hush strip and the rest of the pack must not turn a plain cell's menu into a wall");
            Assert.Greater(worst, 0, "fixture: the workshop does carry tools that act on an open cell");
        }
    }
}
