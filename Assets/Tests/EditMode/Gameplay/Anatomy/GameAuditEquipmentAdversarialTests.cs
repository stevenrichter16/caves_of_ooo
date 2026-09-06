using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory;
using System.Reflection;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    // Dedicated A10 taxonomy: multi-slot identity, notification, ownership,
    // forced destinations, reentrant listeners and complete death callers.
    public class GameAuditEquipmentAdversarialTests
    {
        static Entity Glow(EquipmentLifecycleFixture f,string bp="Buckler")
        {var item=f.Item(bp);var glow=new EnhancementGlowQuartz();glow.ApplyTier(2);item.AddPart(glow);Assert.IsTrue(f.Inventory.AddObject(item));Assert.IsTrue(InventorySystem.Equip(f.Actor,item,f.LeftHand));Assert.IsTrue(glow.AppliedBonus);Assert.AreEqual(2,item.GetPart<LightSourcePart>().Radius);return item;}
        [TestCase(false)] [TestCase(true)]
        public void RealTwoHandedEnhancementCleansBothSlotsAndNotifiesOnlyOnce(bool dropAll)
        {using(var f=new EquipmentLifecycleFixture()){var item=Glow(f,"Greatsword");Assert.AreEqual(2,f.Inventory.EquippedItems.Values.Count(e=>ReferenceEquals(e,item)));int version=EquipmentChangeBus.GlobalVersion;var observer=new EquipmentLifecycleObserver();f.Actor.AddPart(observer);if(dropAll)f.Body.DropAllEquipment(f.Zone);else Assert.IsTrue(f.Body.Dismember(f.LeftArm,f.Zone));f.Ground(item);Assert.AreEqual(0,item.GetPart<LightSourcePart>().Radius);Assert.AreEqual(1,observer.AfterCount);Assert.AreEqual(version+1,EquipmentChangeBus.GlobalVersion);}}
        [TestCase(false)] [TestCase(true)]
        public void HardenedShellContributionReversesOnlyOnAcceptedInjury(bool veto)
        {using(var f=new EquipmentLifecycleFixture()){var item=f.Item("Buckler");Assert.IsTrue(new HardenedShellTinkerModification().Apply(item,out string reason),reason);f.Actor.GetStat("Speed").Penalty=7;Assert.IsTrue(f.Inventory.AddObject(item));Assert.IsTrue(InventorySystem.Equip(f.Actor,item,f.LeftHand));Assert.AreEqual(17,f.Actor.GetStat("Speed").Penalty);f.Actor.AddPart(new EquipmentLifecycleObserver{VetoEvent=veto?"BeforeDismember":null});Assert.AreEqual(!veto,f.Body.Dismember(f.LeftArm,f.Zone));Assert.AreEqual(veto?17:7,f.Actor.GetStat("Speed").Penalty);}}
        [TestCase(false)] [TestCase(true)]
        public void ForcedCarryFallbackKeepsDistinctIdenticalItemEvenAtCapacity(bool full)
        {using(var f=new EquipmentLifecycleFixture()){var equipped=f.EquipDuelistBuckler();var carried=f.Item("Buckler");Assert.IsTrue(new DuelistCutTinkerModification().Apply(carried,out string reason),reason);Assert.IsTrue(equipped.GetPart<StackerPart>().CanStackWith(carried));Assert.IsTrue(f.Inventory.AddObject(carried));if(full)f.Inventory.MaxWeight=0;Assert.IsTrue(f.Body.Dismember(f.LeftArm));f.Detached(equipped);Assert.AreEqual(2,f.Inventory.Objects.Count);Assert.AreEqual(1,f.Inventory.Objects.Count(e=>ReferenceEquals(e,equipped)));Assert.AreEqual(1,f.Inventory.Objects.Count(e=>ReferenceEquals(e,carried)));}}
        [TestCase(false)] [TestCase(true)]
        public void MissingInventoryStillRetainsBodyOwnedEquipment(bool ground)
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();Assert.IsTrue(f.Actor.RemovePart(f.Inventory));Assert.IsTrue(f.Body.Dismember(f.LeftArm,ground?f.Zone:null));Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);if(ground){Assert.AreEqual((5,5),f.Zone.GetEntityPosition(item));Assert.IsNull(f.Actor.GetPart<InventoryPart>());}else{Assert.IsTrue(f.Actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.AreSame(f.Actor,item.GetPart<PhysicsPart>().InInventory);}}}
        [Test]
        public void RootHandwearSurvivesArmLossWithItsEnhancement()
        {using(var f=new EquipmentLifecycleFixture()){var gloves=f.Item("LeatherGloves");var glow=new EnhancementGlowQuartz();glow.ApplyTier(2);gloves.AddPart(glow);Assert.IsTrue(f.Inventory.AddObject(gloves));Assert.IsTrue(InventorySystem.Equip(f.Actor,gloves));Assert.IsTrue(f.Body.Dismember(f.LeftArm,f.Zone));Assert.IsTrue(glow.AppliedBonus);Assert.AreEqual(2,gloves.GetPart<LightSourcePart>().Radius);Assert.AreSame(f.Actor,gloves.GetPart<PhysicsPart>().Equipped);Assert.IsNull(f.Zone.GetEntityCell(gloves));}}
        [TestCase(false)] [TestCase(true)]
        public void NoZoneCleanupInvalidatesEquippedLightingWithoutMembershipChanges(bool veto)
        {using(var f=new EquipmentLifecycleFixture()){var item=Glow(f);f.Zone.AmbientLevel=0;var light=new LightMap();light.Compute(f.Zone);Assert.Greater(light.GetBrightness(6,5),0);int zoneVersion=f.Zone.EntityVersion,equipmentVersion=EquipmentChangeBus.GlobalVersion;f.Actor.AddPart(new EquipmentLifecycleObserver{VetoEvent=veto?"BeforeDismember":null});Assert.AreEqual(!veto,f.Body.Dismember(f.LeftArm));Assert.AreEqual(zoneVersion,f.Zone.EntityVersion);light.Compute(f.Zone);if(veto){Assert.Greater(light.GetBrightness(6,5),0);Assert.AreEqual(equipmentVersion,EquipmentChangeBus.GlobalVersion);}else{Assert.AreEqual(0,light.GetBrightness(6,5));Assert.AreEqual(equipmentVersion+1,EquipmentChangeBus.GlobalVersion);}}}
        [TestCase("ground")] [TestCase("inventory")] [TestCase("refused")]
        public void ForcedCleanupDiagnosticHasExactItemAndDestination(string destination)
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();bool old=Diag.IsChannelEnabled("event");Diag.SetChannel("event",true);try{if(destination=="refused")f.Actor.AddPart(new EquipmentLifecycleObserver{VetoEvent="BeforeDismember"});Assert.AreEqual(destination!="refused",f.Body.Dismember(f.LeftArm,destination=="inventory"?null:f.Zone));var result=DiagQuery.Apply(new DiagQuery.Filter{Category="event",Kind="ForcedUnequipped",Actor=f.Actor.ID,Target=item.ID,Limit=10});Assert.AreEqual(destination=="refused"?0:1,result.Records.Count);if(destination!="refused")StringAssert.Contains(destination,result.Records[0].PayloadJson);}finally{Diag.SetChannel("event",old);}}}
        [TestCase(false)] [TestCase(true)]
        public void RealCombatDeathClearsGearUnlessNoDropPolicyExplicitlySuppressesDrops(bool suppress)
        {using(var f=new EquipmentLifecycleFixture()){var item=Glow(f);if(suppress)f.Actor.Tags["NoDropOnDeath"]="";CombatSystem.ApplyDamage(f.Actor,20,null,f.Zone);Assert.LessOrEqual(f.Actor.GetStat("Hitpoints").BaseValue,0);Assert.IsNull(f.Zone.GetEntityCell(f.Actor));if(suppress){Assert.IsNull(f.Zone.GetEntityCell(item));Assert.IsTrue(item.GetPart<EnhancementGlowQuartz>().AppliedBonus);Assert.AreSame(f.Actor,item.GetPart<PhysicsPart>().Equipped);}else{f.Ground(item);Assert.IsFalse(item.GetPart<EnhancementGlowQuartz>().AppliedBonus);Assert.AreEqual(0,item.GetPart<LightSourcePart>().Radius);}}}
        // Hypothesis: publishing AfterUnequip while the doomed arm is attached
        // admits a new item into a slot that will immediately leave the body.
        [TestCase(false)] [TestCase(true)]
        public void CallbackCannotEquipNewItemIntoDoomedHandButSurvivingHandRemainsUsable(bool surviving)
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();var doomed=f.LeftHand;var target=surviving?f.Body.FindFreeSlot("Hand",Laterality.RIGHT):doomed;var spare=f.Item("Dagger");Assert.IsTrue(f.Inventory.AddObject(spare));bool attempted=false,result=false;f.Actor.AddPart(new EquipmentLifecycleObserver{After=e=>{if(!ReferenceEquals(e,item))return;attempted=true;result=InventorySystem.Equip(f.Actor,spare,target);}});Assert.IsTrue(f.Body.Dismember(f.LeftArm,f.Zone));Assert.IsTrue(attempted);Assert.AreEqual(surviving,result);f.Ground(item);if(surviving){Assert.AreSame(spare,target._Equipped);Assert.AreSame(f.Actor,spare.GetPart<PhysicsPart>().Equipped);}else{Assert.IsTrue(f.Inventory.Objects.Contains(spare));Assert.IsNull(spare.GetPart<PhysicsPart>().Equipped);Assert.IsNull(doomed._Equipped);}}}
        // Hypothesis: the outer dismember dereferences ParentPart after a
        // new cleanup listener has already removed the same limb.
        [Test]
        public void SameLimbReentryCannotDuplicateInjuryOrCrashOuterCleanup()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();var arm=f.LeftArm;bool called=false;f.Actor.AddPart(new EquipmentLifecycleObserver{After=e=>{if(called)return;called=true;f.Body.Dismember(arm,f.Zone);}});Assert.DoesNotThrow(()=>Assert.IsTrue(f.Body.Dismember(arm,f.Zone)));Assert.IsTrue(called);Assert.AreEqual(1,f.Body.DismemberedParts.Count(p=>ReferenceEquals(p.Part,arm)));Assert.IsNull(arm.ParentPart);f.Ground(item);Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));}}
        // Hypothesis: forced cleanup needs ordinary command's same-item claim
        // through the post-event/enhancement pair to avoid re-equip drift.
        [TestCase(false)] [TestCase(true)]
        public void SameItemReequipDuringCleanupRefusesAndLeavesEnhancementUnapplied(bool forced)
        {using(var f=new EquipmentLifecycleFixture()){var item=Glow(f);var right=f.Body.FindFreeSlot("Hand",Laterality.RIGHT);bool attempted=false,result=true;f.Actor.AddPart(new EquipmentLifecycleObserver{After=e=>{attempted=true;result=InventorySystem.Equip(f.Actor,item,right);}});Assert.IsTrue(forced?f.Body.Dismember(f.LeftArm):InventorySystem.UnequipItem(f.Actor,item));Assert.IsTrue(attempted);Assert.IsFalse(result);f.Detached(item);Assert.IsFalse(item.GetPart<EnhancementGlowQuartz>().AppliedBonus);Assert.AreEqual(0,item.GetPart<LightSourcePart>().Radius);Assert.IsTrue(f.Inventory.Objects.Contains(item));}}
        [Test]
        public void NestedDropAllKeepsEachDistinctItemAndCleanupExactlyOnce()
        {using(var f=new EquipmentLifecycleFixture()){var first=f.EquipDuelistBuckler();var second=f.Item("IronshodBoots");Assert.IsTrue(f.Inventory.AddObject(second));Assert.IsTrue(InventorySystem.Equip(f.Actor,second));bool nested=false;var observer=new EquipmentLifecycleObserver{After=e=>{if(nested)return;nested=true;f.Body.DropAllEquipment(f.Zone);}};f.Actor.AddPart(observer);f.Body.DropAllEquipment(f.Zone);Assert.IsTrue(nested);Assert.AreEqual(2,observer.AfterCount);f.Ground(first);f.Ground(second);Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));Assert.AreEqual(0,f.Actor.GetStat("Speed").Penalty);}}

        [Test]
        public void ForcedCleanupCannotReleaseAnExistingOuterItemClaim()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = Glow(f);
                var right = f.Body.FindFreeSlot("Hand", Laterality.RIGHT);
                var outer = new InventoryTransaction();
                try
                {
                    var claim = typeof(InventoryTransaction).GetMethod("TryClaim", BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.IsTrue((bool)claim.Invoke(outer, new object[] { item, f.Actor, "audit" }));
                    Assert.IsTrue(f.Body.Dismember(f.LeftArm));
                    Assert.IsFalse(InventorySystem.Equip(f.Actor, item, right));
                    Assert.IsFalse(item.GetPart<EnhancementGlowQuartz>().AppliedBonus);
                    outer.Commit();
                    Assert.IsTrue(InventorySystem.Equip(f.Actor, item, right));
                    Assert.IsTrue(item.GetPart<EnhancementGlowQuartz>().AppliedBonus);
                    Assert.AreEqual(2, item.GetPart<LightSourcePart>().Radius);
                }
                finally { outer.Rollback(); }
            }
        }

        // Only guard lifetime is asserted for throwing observers. General
        // observer exception rollback remains a separate transaction contract.
        [TestCase(false)] [TestCase(true)]
        public void OwnedForcedClaimReleasesAfterSuccessOrObserverException(bool throws)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = f.EquipDuelistBuckler();
                var right = f.Body.FindFreeSlot("Hand", Laterality.RIGHT);
                var observer = new EquipmentLifecycleObserver { After = e => { if (throws) throw new InvalidOperationException("audit observer"); } };
                f.Actor.AddPart(observer);
                if (throws) Assert.Throws<InvalidOperationException>(() => f.Body.Dismember(f.LeftArm));
                else Assert.IsTrue(f.Body.Dismember(f.LeftArm));
                observer.After = null;
                Assert.AreEqual(14, f.Actor.GetStatValue("Agility"));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, item, right));
                Assert.AreEqual(16, f.Actor.GetStatValue("Agility"));
            }
        }

        [Test]
        public void RegenerationRestoresEmptySlotsWithoutDuplicatingCarriedGearOrBonuses()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = f.EquipDuelistBuckler();
                var hand = f.LeftHand;
                Assert.IsTrue(f.Body.Dismember(f.LeftArm));
                Assert.IsTrue(f.Body.RegenerateLimb("Arm"));
                Assert.IsTrue(f.Body.GetParts().Contains(hand));
                Assert.IsNull(hand._Equipped);
                Assert.AreEqual(1, f.Inventory.Objects.Count(e => ReferenceEquals(e, item)));
                Assert.AreEqual(14, f.Actor.GetStatValue("Agility"));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, item, hand));
                Assert.AreEqual(16, f.Actor.GetStatValue("Agility"));
                Assert.IsFalse(f.Body.RegenerateLimb("Arm"));
            }
        }

        [Test]
        public void SavedDetachedSubtreeContainsNoEquipmentAlias()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = f.EquipDuelistBuckler();
                Assert.IsTrue(f.Body.Dismember(f.LeftArm, f.Zone));
                var loaded = f.RoundTrip();
                var body = loaded.Player.GetPart<Body>();
                Assert.AreEqual(1, body.DismemberedParts.Count);
                Assert.IsTrue(body.DismemberedParts.All(d => d.Part.GetParts().All(p => p._Equipped == null)));
                Assert.IsTrue(body.RegenerateLimb("Arm"));
                Assert.IsFalse(body.GetParts().Any(p => p._Equipped?.ID == item.ID));
                Assert.AreEqual(14, loaded.Player.GetStatValue("Agility"));
            }
        }

        [Test]
        public void UnsupportedHandSettlesBeforeCleanupCallbackAndCannotBeRefilled()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = f.EquipDuelistBuckler();
                var hand = f.LeftHand;
                var arm = f.LeftArm;
                var spare = f.Item("Dagger");
                Assert.IsTrue(f.Inventory.AddObject(spare));
                hand.DependsOn = "missing support audit";
                bool attempted = false, equipped = true;
                f.Actor.AddPart(new EquipmentLifecycleObserver { After = e => { attempted = true; equipped = InventorySystem.Equip(f.Actor, spare, hand); } });
                f.Body.CheckUnsupportedPartLoss();
                Assert.IsTrue(attempted);
                Assert.IsFalse(equipped);
                Assert.IsNull(hand.ParentPart);
                Assert.IsTrue(f.Body.GetParts().Contains(arm));
                Assert.AreEqual(1, f.Body.DismemberedParts.Count(d => ReferenceEquals(d.Part, hand)));
                Assert.IsNull(hand._Equipped);
                f.Detached(item);
                Assert.IsTrue(f.Inventory.Objects.Contains(item));
                Assert.IsTrue(f.Inventory.Objects.Contains(spare));
                Assert.AreEqual(14, f.Actor.GetStatValue("Agility"));
            }
        }

        // Hypothesis: a nested public drop clears cache ownership but must also
        // clear every slot on a subtree already detached by the outer injury.
        [TestCase(false, false, false)] [TestCase(true, false, false)]
        [TestCase(false, true, false)] [TestCase(true, true, false)]
        [TestCase(true, false, true)] [TestCase(true, true, true)]
        public void NestedDropDuringInjuryCleansEveryDetachedSlotOnlyOnce(bool nested, bool extrinsic, bool ordinaryUnequip)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var arm = f.LeftArm;
                arm.Extrinsic = extrinsic;
                var firstHand = f.LeftHand;
                var secondHand = f.Body.FindFreeSlot("Hand", Laterality.RIGHT);
                secondHand.ParentPart.RemovePart(secondHand);
                arm.AddPart(secondHand);
                var first = f.EquipDuelistBuckler();
                var second = f.Item("Buckler");
                Assert.IsTrue(new DuelistCutTinkerModification().Apply(second, out string reason), reason);
                Assert.IsTrue(f.Inventory.AddObject(second));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, second, secondHand));
                Assert.AreEqual(18, f.Actor.GetStatValue("Agility"));
                bool called = false, ordinaryResult = false;
                var observer = new EquipmentLifecycleObserver { After = e => {
                    if (!nested || called) return;
                    called = true;
                    if (ordinaryUnequip) ordinaryResult = InventorySystem.UnequipItem(f.Actor, second);
                    else f.Body.DropAllEquipment(f.Zone);
                } };
                f.Actor.AddPart(observer);
                Assert.IsTrue(f.Body.Dismember(arm, f.Zone));
                Assert.AreEqual(nested, called);
                Assert.IsFalse(ordinaryResult, "A pending forced item must refuse an independent ordinary transfer.");
                Assert.AreEqual(2, observer.AfterCount);
                Assert.AreEqual(14, f.Actor.GetStatValue("Agility"));
                Assert.IsNull(firstHand._Equipped);
                Assert.IsNull(secondHand._Equipped);
                f.Ground(first);
                f.Ground(second);
            }
        }
    }
}
