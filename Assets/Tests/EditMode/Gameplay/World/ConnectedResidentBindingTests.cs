using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedResidentBindingTests
    {
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void GeneratedKeeperOwnsExactlyTheTwoPreparedBedsAndTray(int seed)
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(seed);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, seed, true);
                var zone = manager.GetZone(LocalGatheringClaimPart.ReserveZoneID);
                Assert.NotNull(zone);
                var keeper = zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "SpreadSeedKeeper");
                Assert.NotNull(keeper, "The near-spawn reserve must survive ordinary existing landmarks.");
                var claim = keeper.GetPart<LocalGatheringClaimPart>();
                Assert.NotNull(claim, "A sign without actual ownership does not establish gathering rights.");
                Assert.True(claim.Configured);
                Assert.AreEqual(manager.Exploration.WorldKey, claim.WorldKey);
                Assert.AreEqual(seed, claim.WorldSeed);
                Assert.AreSame(zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "ConnectedReserveTray"), claim.Tray);
                Assert.AreNotSame(claim.FirstSoil, claim.SecondSoil);
                foreach (var soil in new[] { claim.FirstSoil, claim.SecondSoil })
                {
                    Assert.NotNull(zone.GetEntityCell(soil));
                    Assert.AreSame(zone, zone.GetEntityCell(soil).ParentZone);
                    Assert.NotNull(soil.GetPart<CultivatedSoilPart>());
                    Assert.AreEqual(keeper.ID, soil.GetProperty("ConnectedSpread.ReserveBed"));
                    Assert.AreEqual(1, zone.GetEntityCell(soil).Objects.Count(e => e.GetProperty("ConnectedSpread.Role") == "reserve-crop"));
                }
                Assert.IsEmpty(claim.Permissions);
            }
        }

        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void GeneratedCookAcknowledgmentAndFiniteRewardsReferToActualPan(int seed)
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(seed);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, seed, true);
                var zone = manager.GetZone("Overworld.12.11.0");
                Assert.NotNull(zone);
                var cook = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadWaysideCook");
                var pan = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "ConnectedBatchPan");
                var introduction = cook.GetPart<CookIntroductionPart>();
                Assert.NotNull(introduction, "The ordinary cook must acknowledge the actual repaired investment.");
                Assert.True(introduction.Configured);
                Assert.AreSame(pan, introduction.Pan);
                Assert.AreEqual(seed, introduction.WorldSeed);
                Assert.AreEqual(manager.Exploration.WorldKey, introduction.WorldKey);
                var source = pan.GetPart<ConnectedSpreadSourcePart>();
                Assert.NotNull(source);
                Assert.AreEqual(manager.Exploration.WorldKey, source.WorldKey);
                Assert.AreEqual(5, source.Kinds, "Repair and first-batch rewards share the same actual pan source.");
            }
        }
    }
}
