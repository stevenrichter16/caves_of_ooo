using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityPlayerChairTests
    {
        Zone zone; Entity actor, chair;
        [SetUp] public void Setup()
        {
            zone=new Zone("ChairTest");actor=new Entity{ID="player"};actor.SetTag("Player");
            actor.AddPart(new PhysicsPart());actor.AddPart(new InventoryPart());actor.AddPart(new StatusEffectsPart());
            actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=20,Max=20};
            zone.AddEntity(actor,10,10);chair=new Entity{ID="chair"};chair.AddPart(new ChairPart());zone.AddEntity(chair,10,10);
            MessageLog.Clear();
        }
        bool Act(string command)=>InventorySystem.PerformAction(actor,chair,command,zone);
        [Test]public void FreeChair_PlayerCanSitThenStand_WithActualReservation()
        {
            Assert.True(InventorySystem.GetActions(actor,chair).Any(a=>a.Command=="SitOnChair"));
            Assert.True(Act("SitOnChair"));Assert.True(chair.GetPart<ChairPart>().Occupied);Assert.AreSame(chair,actor.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>().Furniture);
            Assert.True(InventorySystem.GetActions(actor,chair).Any(a=>a.Command=="StandFromChair"));
            Assert.True(Act("StandFromChair"));Assert.False(chair.GetPart<ChairPart>().Occupied);Assert.Null(actor.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>());
        }
        [Test] public void FirstEffectChairUseSupportsRealPlayerWithoutPreinstalledStatusContainer()
        {
            using (var content = new DensityLootTestScope())
            {
                zone = new Zone("FirstEffectChairTest");
                actor = content.Factory.CreateEntity("Player"); chair = content.Factory.CreateEntity("Chair");
                Assert.NotNull(actor); Assert.NotNull(chair);
                Assert.IsNull(actor.GetPart<StatusEffectsPart>(), "A real factory player starts without an effect container.");
                Assert.True(zone.AddEntity(actor, 10, 10)); Assert.True(zone.AddEntity(chair, 11, 10));
                var seat = chair.GetPart<ChairPart>(); Assert.NotNull(seat);
                int hp = actor.GetStatValue("Hitpoints");
                Assert.True(InventorySystem.GetActions(actor, chair).Any(a => a.Command == "SitOnChair"));
                Assert.False(Act("SitOnChair"), "Adjacent access must not teleport or claim the seat.");
                Assert.IsNull(actor.GetPart<StatusEffectsPart>(), "Rejected eligibility must not eagerly add a status container.");
                Assert.False(seat.Occupied); Assert.IsNull(seat.Occupant);
                Assert.AreEqual((10, 10), zone.GetEntityPosition(actor)); Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));

                Assert.True(zone.MoveEntity(chair, 10, 10));
                Assert.True(Act("SitOnChair"), "Sitting must be allowed to create the player's first effect through ApplyEffect.");
                Assert.AreEqual(1, actor.Parts.Count(p => p is StatusEffectsPart));
                Assert.AreSame(chair, actor.GetEffect<SittingEffect>().Furniture);
                Assert.True(seat.Occupied); Assert.AreSame(actor, seat.Occupant);
                Assert.True(InventorySystem.GetActions(actor, chair).Any(a => a.Command == "StandFromChair"));
                Assert.True(Act("StandFromChair")); Assert.False(actor.HasEffect<SittingEffect>());
                Assert.False(seat.Occupied); Assert.IsNull(seat.Occupant);
                Assert.AreEqual((10, 10), zone.GetEntityPosition(actor)); Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));
            }
        }
        [TestCase("player",true)][TestCase("Stillcord",true)][TestCase("other-owner",false)]
        public void OwnerIdOrTag_IsRespected(string owner,bool allowed)
        {actor.SetTag("Stillcord");chair.GetPart<ChairPart>().Owner=owner;Assert.AreEqual(allowed,Act("SitOnChair"));Assert.AreEqual(allowed,chair.GetPart<ChairPart>().Occupied);}
        [Test]public void OutstandingNpcReservation_CannotBeStolenOrStoodFrom()
        {chair.GetPart<ChairPart>().Occupied=true;Assert.False(Act("SitOnChair"));Assert.False(Act("StandFromChair"));Assert.True(chair.GetPart<ChairPart>().Occupied);}
        [Test]public void AdjacentOrRemoteChair_RequiresStandingAtSeat()
        {zone.MoveEntity(chair,11,10);Assert.False(Act("SitOnChair"));Assert.False(chair.GetPart<ChairPart>().Occupied);zone.MoveEntity(chair,20,10);Assert.False(Act("SitOnChair"));}
        [Test]public void BedInventoryTransaction_DoesNotGrantFreeInnHealing()
        {
            var bed=new Entity();bed.AddPart(new BedPart());bed.AddPart(new PhysicsPart());zone.AddEntity(bed,10,10);
            Assert.True(InventorySystem.GetActions(actor,bed).Any(a=>a.Command=="SleepOnBed"));
            actor.GetStat("Hitpoints").BaseValue=7;
            Assert.False(InventorySystem.PerformAction(actor,bed,"SleepOnBed",zone));
            Assert.AreEqual(7,actor.GetStatValue("Hitpoints"));Assert.False(actor.HasEffect<WellRestedEffect>());
            Assert.False(bed.GetPart<BedPart>().Occupied);
        }
        [Test]public void NonPlayer_CannotUsePlayerSeatCommand()
        {actor.Tags.Remove("Player");Assert.False(Act("SitOnChair"));Assert.False(chair.GetPart<ChairPart>().Occupied);}
        [Test]public void EffectApplicationVeto_LeavesChairUnclaimed()
        {actor.AddPart(new VetoEffectPart());Assert.False(Act("SitOnChair"));Assert.False(chair.GetPart<ChairPart>().Occupied);Assert.Null(actor.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>());}
        [Test]public void SuccessfulMovement_ReleasesOwnSeat_ButBlockedMovementDoesNot()
        {
            Assert.True(Act("SitOnChair"));var wall=new Entity();wall.AddPart(new PhysicsPart{Solid=true});zone.AddEntity(wall,11,10);
            Assert.False(MovementSystem.TryMove(actor,zone,1,0));Assert.True(chair.GetPart<ChairPart>().Occupied);
            Assert.True(MovementSystem.TryMove(actor,zone,0,1));Assert.False(chair.GetPart<ChairPart>().Occupied);Assert.False(actor.HasEffect<SittingEffect>());
        }
        [Test]public void TakingDamage_StandsPlayerWithoutHealing()
        {Assert.True(Act("SitOnChair"));CombatSystem.ApplyDamage(actor,new Damage(1),null,zone);Assert.False(actor.HasEffect<SittingEffect>());Assert.False(chair.GetPart<ChairPart>().Occupied);Assert.AreEqual(19,actor.GetStatValue("Hitpoints"));}
        [Test]public void Death_ReleasesReservation()
        {Assert.True(Act("SitOnChair"));actor.FireEvent("Died");Assert.False(chair.GetPart<ChairPart>().Occupied);Assert.False(actor.HasEffect<SittingEffect>());}
        [Test]public void GenericCommandException_RestoresChairAndEffect()
        {actor.AddPart(new ThrowAfterPart());Assert.False(Act("SitOnChair"));Assert.False(chair.GetPart<ChairPart>().Occupied);Assert.False(actor.HasEffect<SittingEffect>());}
        [Test] public void FirstEffectChairExceptionRollsBackTheActuallyCreatedEffectAndReservation()
        {
            using (var content = new DensityLootTestScope())
            {
                zone = new Zone("FirstEffectChairRollbackTest");
                actor = content.Factory.CreateEntity("Player"); chair = content.Factory.CreateEntity("Chair");
                Assert.NotNull(actor); Assert.NotNull(chair); Assert.IsNull(actor.GetPart<StatusEffectsPart>());
                Assert.True(zone.AddEntity(actor, 10, 10)); Assert.True(zone.AddEntity(chair, 10, 10));
                var seat = chair.GetPart<ChairPart>(); Assert.NotNull(seat);
                int hp = actor.GetStatValue("Hitpoints");
                var observer = new ObserveLazySeatThenThrowPart(); actor.AddPart(observer);
                Assert.False(Act("SitOnChair"));
                Assert.AreEqual(1, observer.AfterCalls, "The action must reach its post-application callback, not merely reject eligibility.");
                Assert.True(observer.SawClaimedSeat, "The callback must observe the lazy container, actual sitting effect and claimed chair before throwing.");
                Assert.False(actor.HasEffect<SittingEffect>()); Assert.False(seat.Occupied); Assert.IsNull(seat.Occupant);
                Assert.AreEqual(hp, actor.GetStatValue("Hitpoints")); Assert.AreEqual((10, 10), zone.GetEntityPosition(actor));
                Assert.True(actor.RemovePart(observer));
                Assert.True(Act("SitOnChair")); Assert.True(Act("StandFromChair"));
                Assert.False(actor.HasEffect<SittingEffect>()); Assert.False(seat.Occupied); Assert.IsNull(seat.Occupant);
                Assert.AreEqual(1, observer.AfterCalls);
            }
        }
        [Test]public void StandFromDifferentChair_DoesNotReleaseOriginalSeat()
        {Assert.True(Act("SitOnChair"));var other=new Entity();other.AddPart(new ChairPart());zone.AddEntity(other,10,10);Assert.False(InventorySystem.PerformAction(actor,other,"StandFromChair",zone));Assert.True(chair.GetPart<ChairPart>().Occupied);Assert.True(actor.HasEffect<SittingEffect>());}
        [Test]public void SaveGraph_PreservesSeatReferenceAndReservation()
        {Assert.True(Act("SitOnChair"));var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);var sitting=loaded.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>();Assert.NotNull(sitting);Assert.AreEqual("chair",sitting.Furniture.ID);Assert.True(sitting.Furniture.GetPart<ChairPart>().Occupied);loaded.RemoveEffect<SittingEffect>();Assert.False(sitting.Furniture.GetPart<ChairPart>().Occupied);}
        [TestCase(false)][TestCase(true)] public void DeadPlayer_CannotReserveSeat(bool handled)
        {if(handled)actor.SetTag("_DeathHandled");else actor.GetStat("Hitpoints").BaseValue=0;Assert.False(Act("SitOnChair"));Assert.False(chair.GetPart<ChairPart>().Occupied);}
        [Test] public void StandingRollback_RestoresExactSeatAndEffect()
        {Assert.True(Act("SitOnChair"));var before=actor.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>();actor.AddPart(new ThrowAfterPart());Assert.False(Act("StandFromChair"));Assert.AreSame(before,actor.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>());Assert.True(chair.GetPart<ChairPart>().Occupied);}
        [Test] public void RemovingStaleEffect_CannotReleaseDifferentPlayerOccupant()
        {
            var other=new Entity{ID="other-player"};other.SetTag("Player");other.AddPart(new InventoryPart());other.AddPart(new StatusEffectsPart());zone.AddEntity(other,10,10);
            Assert.True(Act("SitOnChair"));var stale=new SittingEffect(chair);other.ApplyEffect(stale);
            other.RemoveEffect<SittingEffect>();Assert.True(chair.GetPart<ChairPart>().Occupied);Assert.True(actor.HasEffect<SittingEffect>());
        }
        public class VetoEffectPart:Part{public override string Name=>"ChairVeto";public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeApplyEffect";}
        public class ThrowAfterPart:Part{public override string Name=>"ChairThrow";public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")throw new InvalidOperationException("chair after");return true;}}
        public class ObserveLazySeatThenThrowPart : Part
        {
            public override string Name => "ObserveLazySeatThenThrow";
            public int AfterCalls;
            public bool SawClaimedSeat;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "AfterInventoryAction") return true;
                AfterCalls++;
                var effect = ParentEntity.GetEffect<SittingEffect>();
                var seat = effect?.Furniture?.GetPart<ChairPart>();
                SawClaimedSeat = ParentEntity.GetPart<StatusEffectsPart>() != null
                    && seat?.Occupied == true && seat.Occupant == ParentEntity;
                throw new InvalidOperationException("observed first sitting effect before rollback");
            }
        }
    }
}
