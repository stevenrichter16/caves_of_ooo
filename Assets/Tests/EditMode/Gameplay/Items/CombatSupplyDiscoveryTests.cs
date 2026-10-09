using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual ordinary allotment generation followed by native harvest
    /// and inventory actions, rather than presence-only blueprint assertions.</summary>
    public sealed class CombatSupplyDiscoveryTests : FiftyWorldFixture
    {
        void InstallAllotment()
        {
            Zone.RemoveEntity(Actor); Zone = new Zone(RepairCultivationSite.ZoneID); SettlementRuntime.ActiveZone = Zone;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) Assert.True(Zone.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Assert.True(Zone.AddEntity(Actor, 10, 10)); Assert.True(RepairCultivationSite.TryInstall(Zone, Factory));
        }
        void Approach(Entity source)
        {
            var cell = Zone.GetEntityCell(source); Assert.True(Zone.MoveEntity(Actor, cell.X, cell.Y));
        }
        [TestCase("RepairCordBundle", "Harvest")] [TestCase("KnotflaxCrop", "GatherCrop")]
        public void OrdinaryAllotmentSourceSuppliesCordThatActuallyLaysASnare(string blueprint, string harvest)
        {
            InstallAllotment(); var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == blueprint
                && (e.GetPart<CropPart>() == null || e.GetPart<CropPart>().GrowthStage == 2));
            Approach(source); Assert.True(Act(source, harvest));
            var cord = Pack.Objects.First(e => e.BlueprintName == "KnotflaxCord");
            var action = Actions(cord).First(a => a.Command.StartsWith("LayCordSnare|", StringComparison.Ordinal));
            int before = Count("KnotflaxCord"); Assert.True(Act(cord, action.Command)); Assert.AreEqual(before - 1, Count("KnotflaxCord"));
            Assert.True(Zone.GetReadOnlyEntities().Any(e => CordSnarePart.IsArmed(e)));
            Assert.False(Act(source, harvest), "The already harvested source cannot mint another free batch.");
        }

        [Test] public void OrdinaryAllotmentClaySeamSuppliesEmergencySmothering()
        {
            InstallAllotment(); var source = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "RepairClayBank");
            Approach(source); Assert.True(Act(source, "Harvest"));
            var clay = Pack.Objects.First(e => e.BlueprintName == "FireClay"); Actor.ApplyEffect(new BurningEffect(1));
            var action = Actions(clay).Single(a => a.Command.StartsWith("SmotherFire|", StringComparison.Ordinal));
            int before = Count("FireClay"); Assert.True(Act(clay, action.Command)); Assert.AreEqual(before - 1, Count("FireClay"));
            Assert.IsNull(Actor.GetEffect<BurningEffect>()); Assert.IsNull(Actor.GetEffect<WetEffect>());
            Assert.False(Act(source, "Harvest"));
        }

        [Test] public void AllotmentIsAnOrdinaryExactWorldDestinationAndDoesNotInstallInArbitraryZones()
        {
            Assert.AreEqual("Overworld.2.6.0", RepairCultivationSite.ZoneID);
            Assert.False(RepairCultivationSite.TryInstall(Zone, Factory));
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "RepairCordBundle" || e.BlueprintName == "RepairClayBank"));
        }
    }
}
