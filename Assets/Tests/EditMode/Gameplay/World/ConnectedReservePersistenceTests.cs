using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedReservePersistenceTests : ConnectedReserveTestBase
    {
        [TestCase(false)][TestCase(true)]
        public void SavedPermissionGraphPreservesActualOwnerReferencesAndState(bool suspended)
        {
            if (suspended) Assert.True(Harvest(Crop()).Success); else Assert.True(Act("pay"));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Keeper);
            var claim = loaded.GetPart<LocalGatheringClaimPart>();
            Assert.AreEqual(Manager.Exploration.WorldKey, claim.WorldKey);
            var actor = claim.Permissions.Single().Player;
            Assert.AreNotSame(Player, actor);
            Assert.AreEqual(Player.ID, actor.ID);
            Assert.AreEqual(suspended ? ReserveAccessState.Suspended : ReserveAccessState.Granted, claim.GetState(actor));
            Assert.AreEqual(ReserveAccessState.Unknown, claim.GetState(Player), "An equal ID does not stand in for the actual loaded player.");
            var zone = new Zone(Reserve.ZoneID); Manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
            Assert.True(zone.AddEntity(loaded, 5, 5)); Assert.True(zone.AddEntity(actor, 5, 6));
            Assert.True(zone.AddEntity(claim.FirstSoil, claim.FirstX, claim.FirstY));
            Assert.True(zone.AddEntity(claim.SecondSoil, claim.SecondX, claim.SecondY));
            Assert.True(zone.AddEntity(claim.Tray, claim.TrayX, claim.TrayY));
            Assert.True(claim.TryAct(actor, zone, "terms"));
            Assert.False(claim.TryAct(actor, zone, "pay"));
            Assert.AreEqual(suspended ? 40 : 32, TradeSystem.GetDrams(actor));
        }

        [Test] public void SavedSpokenIntroductionKeepsRepairCauseAndExplicitRelay()
        {
            RepairPan(); Assert.True(Introduce());
            var actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Player);
            var knowledge = actor.GetPart<CookIntroductionKnowledgePart>();
            Assert.AreSame(actor, knowledge.Learner);
            Assert.AreSame(actor, knowledge.Pan.GetPart<RepairablePart>().RepairedBy);
            Assert.AreSame(knowledge.Pan, knowledge.Speaker.GetPart<CookIntroductionPart>().Pan);
            Assert.AreEqual(knowledge.Pan.GetPart<RepairablePart>().RepairCauseID, knowledge.RepairCauseID);
            Assert.True(CookIntroductionKnowledgePart.ValidFor(actor, Manager.Exploration.WorldKey));
            Kitchen.RemoveEntity(Player); Reserve.RemoveEntity(Player);
            Assert.True(Reserve.AddEntity(actor, 5, 6)); Manager.SetActiveZone(Reserve); SettlementRuntime.ActiveZone = Reserve;
            Assert.AreEqual(ReserveAccessState.Unknown, ((LocalGatheringClaimPart)Claim).GetState(actor));
            Assert.True(((LocalGatheringClaimPart)Claim).TryAct(actor, Reserve, "introduction"));
            Assert.AreEqual(ReserveAccessState.Granted, ((LocalGatheringClaimPart)Claim).GetState(actor));
            Assert.AreEqual(40, TradeSystem.GetDrams(actor));
        }

        [Test] public void SameSeedDifferentActualWorldDoesNotAcceptOldClaimOrKnowledge()
        {
            RepairPan(); Assert.True(Introduce()); MovePlayer(Reserve);
            var other = OverworldZoneManager.CreateDetached(Factory, 1729, true);
            Assert.AreNotEqual(Manager.Exploration.WorldKey, other.Exploration.WorldKey);
            other.SetActiveZone(Reserve);
            Assert.False(CookIntroductionKnowledgePart.ValidFor(Player, other.Exploration.WorldKey));
            Assert.False(Act("pay")); Assert.False(Act("introduction"));
            Assert.AreEqual("Unknown", State()); Assert.AreEqual(40, TradeSystem.GetDrams(Player));
        }

        [TestCase(false)][TestCase(true)]
        public void PickupWitnessesUncollectedReserveYieldOnceAndReleasesItsMarker(bool sawHarvest)
        {
            if (!sawHarvest) Reserve.MoveEntity(Keeper, 25, 5);
            Assert.True(Harvest(Crop()).Success);
            var produce = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            Assert.NotNull(produce.GetPart<ReserveYieldPart>());
            Assert.AreEqual(sawHarvest ? "Suspended" : "Unknown", State());
            Reserve.MoveEntity(Keeper, 5, 5);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(produce), Player, Reserve).Success);
            Assert.AreEqual("Suspended", State());
            Assert.AreEqual(1, ((LocalGatheringClaimPart)Claim).Permissions.Single().BreachOrdinal);
            Assert.Null(produce.GetPart<ReserveYieldPart>());
        }

        [Test] public void FullPackPickupDoesNotPublishWitnessOrDiscardYieldProvenance()
        {
            Reserve.MoveEntity(Keeper, 25, 5); Assert.True(Harvest(Crop()).Success); Reserve.MoveEntity(Keeper, 5, 5);
            var produce = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            Player.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.False(InventorySystem.ExecuteCommand(new PickupCommand(produce), Player, Reserve).Success);
            Assert.AreEqual("Unknown", State()); Assert.NotNull(produce.GetPart<ReserveYieldPart>());
            Assert.AreSame(Reserve.GetCell(4, 5), Reserve.GetEntityCell(produce));
        }

        [Test] public void UnseenHarvestProvenanceKeepsExactBedAndKeeperThroughSave()
        {
            Reserve.MoveEntity(Keeper, 25, 5); Assert.True(Harvest(Crop()).Success);
            var produce = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            var saved = PartRoundTripHelper.RoundTripEntityViaTokenGraph(produce).GetPart<ReserveYieldPart>();
            Assert.AreEqual(Manager.Exploration.WorldKey, saved.WorldKey);
            Assert.AreSame(saved.Soil, saved.Keeper.GetPart<LocalGatheringClaimPart>().FirstSoil);
            Assert.AreEqual(Keeper.ID, saved.KeeperID); Assert.Null(saved.AlreadyWitnessedPlayer); Assert.False(saved.Released);
        }

        [TestCase(false)][TestCase(true)]
        public void AutomaticMaturityAssignsNoCulpritButBoundBedProduceKeepsLocalOwnership(bool boundBed)
        {
            int x = boundBed ? 4 : 5, y = boundBed ? 5 : 7;
            var plant = Factory.CreateEntity("CandyCarrotCrop"); var crop = plant.GetPart<CropPart>();
            Assert.False(crop.HarvestAtMaturity, "This exercises the real legacy automatic-drop path.");
            crop.GrowthStage = 1; crop.TicksInStage = crop.TicksPerStage - 1; crop.MoistureTicks = 1;
            Assert.True(Reserve.AddEntity(plant, x, y));
            CropSystem.OnTickEnd(Reserve);
            Assert.Null(Reserve.GetEntityCell(plant));
            var products = Reserve.GetCell(x, y).Objects.Where(e => e.BlueprintName == "CandyCarrot").ToArray();
            Assert.Greater(products.Length, 0);
            Assert.AreEqual("Unknown", State(), "Growth itself is not an observed taking by a player.");
            Assert.True(products.All(e => e.HasPart<ReserveYieldPart>() == boundBed),
                "Every physical yield keeps the exact bed's ownership until an actual pickup.");
            var produce = products[0];
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(produce), Player, Reserve).Success);
            Assert.AreEqual(boundBed ? "Suspended" : "Unknown", State());
            Assert.Null(produce.GetPart<ReserveYieldPart>());
        }
    }
}
