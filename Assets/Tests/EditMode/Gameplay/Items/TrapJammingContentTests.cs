using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class TrapJammingContentTests
    {
        HaulingContentScope scope;
        [SetUp] public void Setup() => scope = new HaulingContentScope();
        [TearDown] public void Cleanup() => scope.Dispose();
        static Part Jam(Entity owner) => owner.Parts.SingleOrDefault(p => p.Name == "TrapJamming");

        [TestCase("SpikeTrap")][TestCase("BearTrap")][TestCase("PressurePlate")][TestCase("FireTrap")]
        public void ActualMechanicalBlueprintOptsIntoOnePersistentTimberIntervention(string blueprint)
        {
            var trap = scope.Factory.CreateEntity(blueprint);
            var part = Jam(trap);
            Assert.NotNull(part, "The real factory blueprint must admit the player action.");
            Assert.AreSame(trap, part.ParentEntity);
            Assert.AreEqual("JamTrap", part.GetType().GetField("JamCommand").GetRawConstantValue());
            Assert.False((bool)part.GetType().GetField("Jammed").GetValue(part));
            Assert.AreEqual(1, trap.Parts.Count(p => p is TriggerOnStepPart));
            Assert.False(trap.GetPart<PhysicsPart>().Solid);
            Assert.False(trap.GetPart<PhysicsPart>().Takeable);
        }

        [TestCase("RuneOfFlame")][TestCase("RuneOfFrost")][TestCase("RuneOfPoison")][TestCase("Greatdew")]
        public void UnrelatedTriggerBlueprintsRemainOutsideTheTimberContract(string blueprint)
        {
            Assert.True(scope.Factory.Blueprints.ContainsKey(blueprint));
            var trap = scope.Factory.CreateEntity(blueprint);
            Assert.NotNull(trap.Parts.FirstOrDefault(p => p is TriggerOnStepPart));
            Assert.IsNull(Jam(trap));
        }

        [Test] public void CarriedTimberNamesBothCompetingUsesAndActualTrapExamples()
        {
            var player = scope.Factory.CreateEntity("Player");
            var timber = scope.Factory.CreateEntity("SalvagedTimber");
            Assert.True(player.GetPart<InventoryPart>().AddObject(timber));
            Assert.True(MaterialUseDescription.TryDescribe(player, timber, scope.Factory, out string text));
            text = text.ToLowerInvariant();
            StringAssert.Contains("two lengths", text);
            StringAssert.Contains("one length", text);
            StringAssert.Contains("jam", text);
            StringAssert.Contains("spike", text);
            StringAssert.Contains("pressure", text);
            StringAssert.Contains("permanent", text);
            Assert.True(player.GetPart<InventoryPart>().CanConsumeOne(timber), "Inspection must not spend the material.");
        }

        [Test] public void CordGuidanceDoesNotPromiseTimberJamming()
        {
            var player = scope.Factory.CreateEntity("Player");
            var cord = scope.Factory.CreateEntity("KnotflaxCord");
            Assert.True(player.GetPart<InventoryPart>().AddObject(cord));
            Assert.True(MaterialUseDescription.TryDescribe(player, cord, scope.Factory, out string text));
            StringAssert.Contains("well line", text);
            StringAssert.DoesNotContain("trap", text.ToLowerInvariant());
        }

        [Test] public void MissingOptInOnAnOldTrapDoesNotAcquireJamAction()
        {
            var trap = scope.Factory.CreateEntity("SpikeTrap"); var part = Jam(trap);
            if (part != null) trap.RemovePart(part);
            var player = scope.Factory.CreateEntity("Player");
            var zone = new Zone("LegacyTrap"); Assert.True(zone.AddEntity(player, 4, 4)); Assert.True(zone.AddEntity(trap, 5, 4));
            Assert.True(player.GetPart<InventoryPart>().AddObject(scope.Factory.CreateEntity("SalvagedTimber")));
            Assert.False(WorldInteractionSystem.GatherActions(trap, player).Any(a => a.Command == "JamTrap"));
            Assert.NotNull(trap.GetPart<SpikeTrapTriggerPart>());
        }

        [TestCase(64)][TestCase(1729)]
        public void ActualOneSouthStoreCarriesOptInAndGladeCellarOffersOnlyTwoTimber(int seed)
        {
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex("Overworld.11.11.0", int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var store = manager.GetZone("Overworld.11.11.0");
            var trap = store.GetReadOnlyEntities().Single(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == "trap");
            Assert.AreEqual("SpikeTrap", trap.BlueprintName); Assert.NotNull(Jam(trap));
            var cellar = manager.GetZone(GleanersCellarBuilder.ZoneID);
            var pallet = cellar.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "timber-pallet");
            var harvest = pallet.GetPart<HarvestablePart>(); Assert.AreEqual("SalvagedTimber", harvest.YieldBlueprint);
            Assert.AreEqual(2, harvest.YieldMin); Assert.AreEqual(2, harvest.YieldMax); Assert.False(harvest.Harvested);
            Assert.AreSame(store, manager.GetZone(store.ZoneID)); Assert.AreSame(cellar, manager.GetZone(cellar.ZoneID));
        }

        [Test] public void JammedGeneratedStoreReadoutKeepsBypassWithoutClaimingItStillFires()
        {
            const string id = "Overworld.11.11.0";
            scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            var zone = manager.GetZone(id);
            var trap = zone.GetReadOnlyEntities().Single(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == "trap");
            string armed = trap.GetPart<ExaminablePart>().BuildExamineLine();
            StringAssert.Contains("An open gap leads around", armed);
            StringAssert.Contains("While armed, anything stepping", armed);
            trap.GetPart<TrapJammingPart>().Jammed = true; // Controlled readout state; ordinary payment has separate coverage.
            string jammed = trap.GetPart<ExaminablePart>().BuildExamineLine();
            StringAssert.Contains("jammed permanently", jammed);
            StringAssert.Contains("An open gap leads around", jammed);
            StringAssert.Contains("While armed, anything stepping", jammed);
            StringAssert.DoesNotContain("Anything stepping on the teeth", jammed);
        }
    }
}
