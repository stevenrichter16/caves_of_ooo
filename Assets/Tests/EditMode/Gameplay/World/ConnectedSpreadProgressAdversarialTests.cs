using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedSpreadProgressAdversarialTests:ConnectedProgressFixture
    {
        [Test] public void OrdinaryClayCannotBorrowTheOriginalCacheReward()
        {var clay=Factory.CreateEntity("FireClay");clay.GetPart<StackerPart>().StackCount=2;Cache.GetPart<ContainerPart>().AddItem(clay);Assert.True(Take(clay));Assert.AreEqual(0,XP);Assert.False(Actor.HasPart<ConnectedSpreadProgressPart>());}
        [Test] public void SameSeedIsNotTheSameWorld()
        {
            var foreign=OverworldZoneManager.CreateDetached(Factory,64,true);string key=foreign.Exploration.WorldKey;Assert.AreNotEqual(Key,key);
            var clay=Factory.CreateEntity("FireClay");Assert.True(ConnectedSpreadProgress.BindClay(clay,key,0));Cache.GetPart<ContainerPart>().AddItem(clay);Assert.True(Take(clay));Assert.AreEqual(0,XP);Assert.False(Actor.HasPart<ConnectedSpreadProgressPart>());
        }
        [Test] public void RawStructuralCloneDoesNotMintASecondOriginalUnit()
        {
            var source=Clay(0);var clone=source.CloneForStack();Assert.AreNotEqual(source.ID,clone.ID);
            Zone.AddEntity(clone,4,4);Assert.True(Pick(clone));Assert.AreEqual(0,XP);Assert.False(Actor.HasPart<ConnectedSpreadProgressPart>());
        }
        [Test] public void UnknownWorldOrNonplayerDoesNotAcquireExplorationAwards()
        {
            var clay=Stores();Actor.Tags.Remove("Player");Assert.True(Take(clay));Assert.AreEqual(0,XP);
            Actor.SetTag("Player");Pack.RemoveObject(clay);var unbound=new Zone("unbound");Zone.RemoveEntity(Actor);unbound.AddEntity(Actor,4,4);unbound.AddEntity(clay,4,4);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(clay),Actor,unbound).Success);Assert.AreEqual(0,XP);
        }
        [Test] public void CapacityRefusalAndRetryDoNotSpendOriginalProvenance()
        {
            var clay=Stores();Pack.MaxWeight=0;Assert.False(Take(clay));Assert.AreEqual(0,XP);Assert.AreEqual(3,clay.GetPart<ConnectedClayOriginPart>().Units);
            Pack.MaxWeight=-1;Assert.True(Take(clay));Assert.AreEqual(30,XP);
        }
        [Test] public void FailedTakenCallbackRestoresMergedQuantitiesAndOriginMarkers()
        {
            var clay=Stores();Supply("FireClay",4);var existing=Pack.Objects.Single();clay.AddPart(new ThrowTaken());
            Assert.False(Take(clay));Assert.AreEqual(0,XP);Assert.AreEqual(4,existing.GetPart<StackerPart>().StackCount);Assert.False(existing.HasPart<ConnectedClayOriginPart>());
            Assert.AreEqual(2,clay.GetPart<StackerPart>().StackCount);Assert.AreEqual(3,clay.GetPart<ConnectedClayOriginPart>().Units);Assert.Contains(clay,Cache.GetPart<ContainerPart>().Contents);
            clay.RemovePart(clay.GetPart<ThrowTaken>());Assert.True(Take(clay));Assert.AreEqual(30,XP);
        }
        [Test] public void ExplicitRollbackRestoresLedgerAndDoesNotAnnounceOrGrantXP()
        {
            var clay=Stores();var tx=new InventoryTransaction();MessageLog.Clear();Assert.True(new TakeFromContainerCommand(Cache,clay).Execute(new InventoryContext(Actor,Zone),tx).Success);
            Assert.AreEqual(0,XP,"Experience waits for the physical command's commit.");tx.Rollback();Assert.AreEqual(0,XP);Assert.False(Actor.HasPart<ConnectedSpreadProgressPart>());
            Assert.AreEqual(3,clay.GetPart<ConnectedClayOriginPart>().Units);Assert.True(Take(clay));Assert.AreEqual(30,XP);
        }
        [Test] public void SeparateOneRollbackConservesTheTwoOriginalBits()
        {
            var clay=Stores();Assert.True(Take(clay));var tx=new InventoryTransaction();var separate=new SeparateOneCommand(clay);
            Assert.True(separate.Execute(new InventoryContext(Actor,Zone),tx).Success);var piece=separate.SeparatedItem;
            Assert.AreEqual(0,clay.GetPart<ConnectedClayOriginPart>().Units&piece.GetPart<ConnectedClayOriginPart>().Units);
            Assert.AreEqual(3,clay.GetPart<ConnectedClayOriginPart>().Units|piece.GetPart<ConnectedClayOriginPart>().Units);
            tx.Rollback();Assert.IsNull(separate.SeparatedItem);Assert.AreEqual(2,clay.GetPart<StackerPart>().StackCount);Assert.AreEqual(3,clay.GetPart<ConnectedClayOriginPart>().Units);Assert.AreEqual(30,XP);
        }
        [Test] public void PartialMergeMovesOnlyTheOriginalUnitThatActuallyFit()
        {
            var clay=Stores();var ordinary=Factory.CreateEntity("FireClay");ordinary.GetPart<StackerPart>().MaxStack=1;ordinary.GetPart<StackerPart>().StackCount=0;
            Assert.AreEqual(1,ordinary.GetPart<StackerPart>().MergeFrom(clay));Assert.AreEqual(1,ordinary.GetPart<ConnectedClayOriginPart>().Units);Assert.AreEqual(2,clay.GetPart<ConnectedClayOriginPart>().Units);
            Zone.AddEntity(ordinary,4,4);Assert.True(Pick(ordinary));Assert.AreEqual(0,XP);Assert.True(Take(clay));Assert.AreEqual(30,XP);
        }
        [Test] public void ConsumptionCannotLeaveTheSpentOriginalBitOnAnOrdinaryRemainder()
        {
            var first=Clay(0);var second=Clay(1);Pack.AddObject(first);Pack.AddObject(second);Assert.True(Pack.TryConsumeOne(first));
            Assert.AreEqual(2,first.GetPart<ConnectedClayOriginPart>().Units);Pack.RemoveObject(first);Zone.AddEntity(first,4,4);Assert.True(Pick(first));Assert.AreEqual(0,XP);
        }
        [Test] public void FailedRepairDoesNotKeepItsAwardReservation()
        {
            var well=RepairTarget();Supply("FireClay",2);var failure=new ThrowAfter();Actor.AddPart(failure);
            Assert.False(well.GetPart<RepairablePart>().TryRepair(Actor,Zone));Assert.AreEqual(0,XP);Assert.False(well.GetPart<RepairablePart>().Repaired);Assert.False(Actor.HasPart<ConnectedSpreadProgressPart>());
            Actor.RemovePart(failure);Assert.True(well.GetPart<RepairablePart>().TryRepair(Actor,Zone));Assert.AreEqual(30,XP);
        }
        [Test] public void OrdinaryRepairsAndAlreadyRepairedVisitsDoNotCount()
        {
            Move(GleanersDistrict.SurfaceID);var well=Factory.CreateEntity("RepairLinedWell");Zone.AddEntity(well,5,4);Supply("FireClay",2);
            Assert.True(well.GetPart<RepairablePart>().TryRepair(Actor,Zone));Assert.AreEqual(0,XP);Assert.False(ConnectedSpreadProgress.BindRepair(well,Key));
        }
        [Test] public void ReplacementRipeRootCannotBorrowTheInitiallyDryPlantsIdentity()
        {
            var initial=DryRoot();Zone.RemoveEntity(initial);var replacement=Factory.CreateEntity("SootrootCrop");replacement.GetPart<CropPart>().GrowthStage=2;Zone.AddEntity(replacement,5,4);
            Assert.False(ConnectedSpreadProgress.BindDryCrop(replacement,Key));Assert.True(Harvest(replacement));Assert.AreEqual(0,XP);
        }
        [Test] public void FailedHarvestRestoresExactSourceAndAwardsNothing()
        {
            var root=DryRoot();root.GetPart<CropPart>().Water(4);for(int i=0;i<4;i++)CropSystem.OnTickEnd(Zone);var fail=new ThrowAfter();Actor.AddPart(fail);
            Assert.False(Harvest(root));Assert.AreEqual(0,XP);Assert.NotNull(Zone.GetEntityCell(root));Assert.False(Actor.HasPart<ConnectedSpreadProgressPart>());
            Actor.RemovePart(fail);Assert.True(Harvest(root));Assert.AreEqual(30,XP);
        }
        [Test] public void SavedPartialCustodyStillRequiresTheOtherOriginalUnit()
        {
            var clay=Stores();var piece=clay.GetPart<StackerPart>().SplitStack(1);Zone.AddEntity(piece,4,4);Assert.True(Pick(piece));Assert.AreEqual(0,XP);
            using(var stream=new MemoryStream())
            {
                var writer=new SaveWriter(stream);writer.WriteEntityReference(Actor);writer.WriteEntityReference(Cache);writer.WriteQueuedEntityBodies();stream.Position=0;
                var reader=new SaveReader(stream,null);var player=reader.ReadEntityReference();var cache=reader.ReadEntityReference();reader.ReadEntityBodies();
                Assert.AreNotSame(Actor,player);Actor=player;Cache=cache;Zone=new Zone(GleanersCellarBuilder.ZoneID);Manager.SetActiveZone(Zone);Zone.AddEntity(Actor,4,4);Zone.AddEntity(Cache,5,4);
                Assert.AreEqual(1,Actor.GetPart<ConnectedSpreadProgressPart>().ClayUnits);Assert.AreEqual(Key,Actor.GetPart<ConnectedSpreadProgressPart>().WorldKey);
                Assert.True(Take(Cache.GetPart<ContainerPart>().Contents.Single()));Assert.AreEqual(30,XP);
            }
        }
        [Test] public void AnotherWorldsPlayerLedgerIsNotSilentlyResetForMoreAwards()
        {
            var clay=Stores();Actor.AddPart(new ConnectedSpreadProgressPart{WorldKey=Guid.NewGuid().ToString("N"),PlayerID=Actor.ID,Awards=15,ClayUnits=3});
            Assert.True(Take(clay));Assert.AreEqual(0,XP);Assert.AreEqual(15,Actor.GetPart<ConnectedSpreadProgressPart>().Awards);
        }
    }
}
