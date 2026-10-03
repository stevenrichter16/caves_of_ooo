using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedReserveGroundThrowReviewTests : ConnectedReserveTestBase
    {
        // Hypothesis: throwing from the ground is a physical taking. It cannot
        // launder still-owned reserve produce by moving it off its bound bed.
        [TestCase("witnessed", "Suspended")]
        [TestCase("permitted", "Granted")]
        [TestCase("unwitnessed", "Unknown")]
        [TestCase("public", "Unknown")]
        public void GroundThrowThenPickupPreservesLocalConsequences(string mode, string expected)
        {
            Assert.True(Reserve.MoveEntity(Player, 4, 6));
            if (mode == "permitted") Assert.True(Act("pay"));
            Assert.True(Reserve.MoveEntity(Keeper, 25, 5));
            bool isPublic = mode == "public";
            Assert.True(Harvest(Crop(4, isPublic ? 7 : 5)).Success);
            var produce = Reserve.GetCell(4, isPublic ? 7 : 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            Assert.True(HandlingService.CanThrow(Player, produce, out _), "Actual authored produce must support ordinary Throw.");
            Assert.AreEqual(mode == "permitted" ? "Granted" : "Unknown", State());
            if (mode != "unwitnessed") Assert.True(Reserve.MoveEntity(Keeper, 5, 5));
            // Aim at the player's public cell in the public-source counter;
            // the actual soil on the source cell otherwise intercepts a ray.
            int targetY = isPublic ? 6 : 7;
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(produce, 4, targetY, new Random(64)), Player, Reserve).Success);
            Assert.AreEqual((4, targetY), Reserve.GetEntityPosition(produce));
            string afterThrow = State();
            var markerAfterThrow = produce.GetPart<ReserveYieldPart>();
            Assert.True(Reserve.MoveEntity(Player, 4, targetY));
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(produce), Player, Reserve).Success);
            Assert.AreEqual(1, CarriedCount("MarlrootClod"));
            Assert.AreEqual(expected, afterThrow, "The witnessed physical taking must be committed by Throw.");
            Assert.AreEqual(expected, State(), "Picking up off-bed must not bypass or replay the earlier taking.");
            Assert.Null(markerAfterThrow, "A successful physical taking releases only the thrown unit from local ownership.");
            if (mode == "witnessed")
                Assert.AreEqual(1, ((LocalGatheringClaimPart)Claim).Permissions.Single().BreachOrdinal);
        }

        // A valid multi-unit owner may be saved on the bed. Throw takes one,
        // so the still-grounded remainder must retain its exact claim marker.
        [TestCase(false)] [TestCase(true)]
        public void SplitGroundThrowReleasesOnlyThrownUnitAndRemainderStillWitnesses(bool witnessedThrow)
        {
            Assert.True(Reserve.MoveEntity(Player, 4, 6));
            Assert.True(Reserve.MoveEntity(Keeper, 25, 5));
            Assert.True(Harvest(Crop()).Success);
            var source = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            var otherUnit = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod" && e != source);
            Assert.AreEqual(1, source.GetPart<StackerPart>().MergeFrom(otherUnit));
            Assert.True(Reserve.RemoveEntity(otherUnit));
            var marker = source.GetPart<ReserveYieldPart>();
            Assert.NotNull(marker);
            if (witnessedThrow) Assert.True(Reserve.MoveEntity(Keeper, 5, 5));
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(source, 4, 7, new Random(64)), Player, Reserve).Success);
            var thrown = Reserve.GetCell(4, 7).Objects.First(e => e.BlueprintName == "MarlrootClod");
            Assert.AreNotSame(source, thrown);
            var thrownMarker = thrown.GetPart<ReserveYieldPart>();
            string stateAfterThrow = State();
            Assert.AreEqual(1, source.GetPart<StackerPart>().StackCount);
            Assert.AreEqual((4, 5), Reserve.GetEntityPosition(source));
            Assert.AreSame(marker, source.GetPart<ReserveYieldPart>());
            Assert.False(marker.Released);
            Assert.NotNull(LocalGatheringClaims.WarningFor(Player, source, Reserve));
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(thrown), Player, Reserve).Success);
            Assert.True(Reserve.MoveEntity(Keeper, 5, 5));
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(source), Player, Reserve).Success);
            Assert.AreEqual(witnessedThrow ? "Suspended" : "Unknown", stateAfterThrow);
            Assert.Null(thrownMarker, "Only the extracted physical owner is released by Throw.");
            Assert.AreEqual("Suspended", State());
            Assert.AreEqual(1, ((LocalGatheringClaimPart)Claim).Permissions.Single().BreachOrdinal);
            Assert.AreEqual(2, CarriedCount("MarlrootClod"));
        }

        [Test] public void AlreadyWitnessedHarvestAndGroundThrowDoNotReplayBreach()
        {
            Assert.True(Reserve.MoveEntity(Player, 4, 6));
            Assert.True(Harvest(Crop()).Success);
            var produce = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            Assert.AreEqual("Suspended", State());
            Assert.AreSame(Player, produce.GetPart<ReserveYieldPart>().AlreadyWitnessedPlayer);
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(produce, 4, 7, new Random(64)), Player, Reserve).Success);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(produce), Player, Reserve).Success);
            Assert.AreEqual("Suspended", State());
            Assert.AreEqual(1, ((LocalGatheringClaimPart)Claim).Permissions.Single().BreachOrdinal);
            Assert.Null(produce.GetPart<ReserveYieldPart>());
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualWorldThrowActionLabelsReserveBeforeThePlayerCommits(bool permitted)
        {
            Assert.True(Reserve.MoveEntity(Player, 4, 6));
            Assert.True(Reserve.MoveEntity(Keeper, 25, 5));
            Assert.True(Harvest(Crop()).Success);
            Assert.True(Reserve.MoveEntity(Keeper, 5, 5));
            if (permitted) Assert.True(Act("pay"));
            var produce = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            var action = WorldInteractionSystem.GatherActions(produce, Player).Single(a => a.Command == "Throw");
            Assert.AreEqual(!permitted, action.Display.Contains("Nella's tied reserve"));
            Assert.AreEqual(permitted ? "Granted" : "Unknown", State(), "Reading a warning cannot create a breach.");
        }

        // Counter: a refused throw has no physical taking, knowledge, or release.
        [Test] public void RefusedGroundThrowKeepsProduceAndClaimWithoutBreach()
        {
            Assert.True(Reserve.MoveEntity(Player, 4, 6));
            Assert.True(Reserve.MoveEntity(Keeper, 25, 5));
            Assert.True(Harvest(Crop()).Success);
            var produce = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            var marker = produce.GetPart<ReserveYieldPart>();
            Assert.NotNull(marker);
            Assert.True(Reserve.MoveEntity(Keeper, 5, 5));
            Assert.False(InventorySystem.ExecuteCommand(new ThrowItemCommand(produce, -1, 5, new Random(64)), Player, Reserve).Success);
            Assert.AreEqual((4, 5), Reserve.GetEntityPosition(produce));
            Assert.AreSame(marker, produce.GetPart<ReserveYieldPart>());
            Assert.False(marker.Released);
            Assert.AreEqual("Unknown", State());
        }
    }
}
