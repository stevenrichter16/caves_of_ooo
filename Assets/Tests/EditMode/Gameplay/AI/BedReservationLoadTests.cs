using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BedReservationLoadTests : FiftyWorldFixture
    {
        const string ZoneId = "Overworld.10.11.0";
        const string GuestTag = "Guest";
        OverworldZoneManager manager;

        private Entity InstallGuestBed()
        {
            manager = OverworldZoneManager.CreateDetached(Factory, 64, enableExploration: true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = new Zone(ZoneId);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { ZoneId, Zone } }, ZoneId, null);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, 11, 10));
            Actor.SetTag(GuestTag);
            var bed = Place("Bed", 11, 10);
            bed.GetPart<BedPart>().Owner = GuestTag;
            return bed;
        }

        private Entity Resident(string id = "bed-walking-resident", int x = 14)
        {
            var resident = new Entity { ID = id, BlueprintName = "Villager" };
            resident.SetTag("Creature");
            resident.SetTag("AllowIdleBehavior");
            resident.SetTag(GuestTag);
            resident.AddPart(new PhysicsPart());
            resident.AddPart(new StatusEffectsPart());
            resident.AddPart(new BrainPart { CurrentZone = Zone, Passive = true });
            resident.Statistics["Hitpoints"] = new Stat { Owner = resident, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            Assert.True(Zone.AddEntity(resident, x, 10));
            return resident;
        }

        private IdleOffer ReserveBed(Entity bed, Entity resident)
        {
            var offer = IdleQueryEvent.QueryOffer(bed, resident);
            Assert.NotNull(offer);
            Assert.AreSame(bed, offer.Source);
            Assert.True(bed.GetPart<BedPart>().Occupied);
            return offer;
        }

        private GameSessionState RoundTrip()
        {
            using var bytes = new MemoryStream();
            GameSessionState.Capture("bed-reservation-load", "test", manager, Clock, Actor).Save(new SaveWriter(bytes));
            bytes.Position = 0;
            return GameSessionState.Load(new SaveReader(bytes, Factory));
        }

        private sealed class ConsiderFurniture : System.Random
        {
            public override int Next(int maxValue) => 0;
        }

        [TestCase("Bed")]
        [TestCase("Chair")]
        public void AbandoningAnUnderfootIdleGoalReleasesItsFurniture(string blueprint)
        {
            Assert.True(Zone.MoveEntity(Actor, 7, 10));
            var furniture = Place(blueprint, 11, 10);
            var resident = Resident(x: 11);
            Assert.AreSame(Zone.GetEntityCell(furniture), Zone.GetEntityCell(resident));
            var brain = resident.GetPart<BrainPart>();
            brain.Rng = new ConsiderFurniture();
            var bored = new BoredGoal();
            brain.PushGoal(bored);
            bool Occupied() => furniture.GetPart<BedPart>()?.Occupied ?? furniture.GetPart<ChairPart>().Occupied;

            bored.TakeAction();
            Assert.That(brain.PeekGoal(), Is.TypeOf<DelegateGoal>());
            Assert.True(Occupied());
            Assert.False(resident.HasEffect<SittingEffect>());

            brain.ClearGoals();
            Assert.False(Occupied());
            Assert.False(resident.HasEffect<SittingEffect>());
        }

        [Test]
        public void EnRouteResidentsLostIdleDelegateReleasesTheBedOnBinaryLoad()
        {
            var bed = InstallGuestBed();
            var resident = Resident();
            var offer = ReserveBed(bed, resident);
            var brain = resident.GetPart<BrainPart>();
            brain.PushGoal(new BoredGoal());
            brain.PushGoal(new DelegateGoal(offer.Action, offer.Cleanup, offer.TargetX, offer.TargetY));
            brain.PushGoal(new MoveToGoal(offer.TargetX, offer.TargetY, 50));
            Assert.False(resident.HasEffect<SittingEffect>());
            Assert.AreNotSame(Zone.GetEntityCell(bed), Zone.GetEntityCell(resident));
            Assert.False(PlayerBedService.TryAct(Actor, bed, Zone, PlayerBedService.RestCommand));
            Assert.True(bed.GetPart<BedPart>().Occupied);

            var loaded = RoundTrip();
            var loadedZone = loaded.ZoneManager.ActiveZone;
            var loadedBed = loadedZone.GetReadOnlyEntities().Single(entity => entity.ID == bed.ID);
            var loadedResident = loadedZone.GetReadOnlyEntities().Single(entity => entity.ID == resident.ID);
            Assert.AreEqual(GuestTag, loadedBed.GetPart<BedPart>().Owner);
            Assert.That(loadedResident.GetPart<BrainPart>().PeekGoal(), Is.TypeOf<MoveToGoal>());
            Assert.False(loadedResident.GetPart<BrainPart>().GetGoalsSnapshot().Any(goal => goal is DelegateGoal));
            Assert.False(loadedResident.HasEffect<SittingEffect>());
            Assert.False(loadedBed.GetPart<BedPart>().Occupied);

            Clock = loaded.TurnManager;
            typeof(TurnManager).GetProperty("Active").SetValue(null, Clock);
            SettlementRuntime.ActiveZone = loadedZone;
            int beforeRest = Clock.TickCount;
            Assert.True(PlayerBedService.TryAct(loaded.Player, loadedBed, loadedZone, PlayerBedService.RestCommand));
            Assert.AreEqual(beforeRest + RestSystem.RestClockTurns, Clock.TickCount);
        }

        [Test]
        public void ReplayedIdleOfferCleanupDoesNotReleaseAReplacementReservation()
        {
            var bed = InstallGuestBed();
            var formerResident = Resident();
            var formerOffer = ReserveBed(bed, formerResident);
            formerOffer.Cleanup(null);
            Assert.False(bed.GetPart<BedPart>().Occupied);
            var nextOffer = ReserveBed(bed, Resident("bed-next-resident", 13));

            formerOffer.Cleanup(null);
            Assert.True(bed.GetPart<BedPart>().Occupied);
            Assert.IsNull(IdleQueryEvent.QueryOffer(bed, formerResident));
            nextOffer.Cleanup(null);
            Assert.False(bed.GetPart<BedPart>().Occupied);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SittingEffectReservesTheExactBedOnlyWhileTheResidentIsThere(bool physicallyThere)
        {
            var bed = InstallGuestBed();
            var resident = Resident();
            var offer = ReserveBed(bed, resident);
            Assert.True(Zone.MoveEntity(Actor, 10, 10));
            Assert.True(Zone.MoveEntity(resident, offer.TargetX, offer.TargetY));
            var arrival = new DelegateGoal(offer.Action, offer.Cleanup, offer.TargetX, offer.TargetY);
            resident.GetPart<BrainPart>().PushGoal(arrival);
            arrival.TakeAction();
            Assert.AreSame(bed, resident.GetEffect<SittingEffect>()?.Furniture);
            offer.Cleanup(null);
            Assert.True(bed.GetPart<BedPart>().Occupied, "An arrival cleanup must leave the seated resident's claim alone.");
            if (!physicallyThere) Assert.True(Zone.MoveEntity(resident, 14, 10));
            Assert.True(bed.GetPart<BedPart>().Occupied);

            var loaded = RoundTrip();
            var loadedZone = loaded.ZoneManager.ActiveZone;
            var loadedBed = loadedZone.GetReadOnlyEntities().Single(entity => entity.ID == bed.ID);
            var loadedResident = loadedZone.GetReadOnlyEntities().Single(entity => entity.ID == resident.ID);
            Assert.AreEqual(physicallyThere,
                ReferenceEquals(loadedZone.GetEntityCell(loadedBed), loadedZone.GetEntityCell(loadedResident)));
            Assert.AreEqual(GuestTag, loadedBed.GetPart<BedPart>().Owner);
            Assert.AreEqual(physicallyThere, loadedBed.GetPart<BedPart>().Occupied);
            if (physicallyThere)
            {
                Assert.AreSame(loadedBed, loadedResident.GetEffect<SittingEffect>()?.Furniture);
                Assert.True(loadedResident.RemoveEffect<SittingEffect>());
                Assert.False(loadedBed.GetPart<BedPart>().Occupied);
            }
            else
            {
                Zone = loadedZone;
                var nextOffer = ReserveBed(loadedBed, Resident("bed-next-resident", 13));
                Assert.AreSame(loadedBed, loadedResident.GetEffect<SittingEffect>()?.Furniture);
                Assert.True(loadedResident.RemoveEffect<SittingEffect>());
                Assert.True(loadedBed.GetPart<BedPart>().Occupied);
                nextOffer.Cleanup(null);
                Assert.False(loadedBed.GetPart<BedPart>().Occupied);
            }
        }
    }
}
