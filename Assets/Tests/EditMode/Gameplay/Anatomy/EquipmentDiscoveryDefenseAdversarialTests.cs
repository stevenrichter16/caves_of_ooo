using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Hypothesis: forced injury during AfterEquip already removes
    /// equip bonuses, so a later transaction rollback must not subtract them twice.</summary>
    public sealed class EquipmentDiscoveryDefenseAdversarialTests
    {
        public sealed class AfterEquipReaction : Part
        {
            public Action React;
            public int Calls;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="AfterEquip") { Calls++; React?.Invoke(); }
                return true;
            }
        }

        [TestCase(false,false,false)] [TestCase(false,false,true)]
        [TestCase(false,true,false)] [TestCase(false,true,true)]
        [TestCase(true,false,false)] [TestCase(true,false,true)]
        [TestCase(true,true,false)] [TestCase(true,true,true)]
        public void ForcedAfterEquipInjuryAndRollbackDoNotCreateAStatPenalty(bool legacyBonus,bool injury,bool throwAfterEvent)
        {
            using(var f=new EquipmentLifecycleFixture())
            {
                var item=f.Item(legacyBonus?"Buckler":"GroundwireScreen");
                if(legacyBonus)Assert.IsTrue(new DuelistCutTinkerModification().Apply(item,out string reason),reason);
                Assert.IsTrue(f.Inventory.AddObject(item));
                string stat=legacyBonus?"Agility":"ElectricResistance";
                int before=f.Actor.GetStatValue(stat),bonus=legacyBonus?2:50;
                var arm=f.LeftArm;var hand=f.LeftHand;bool dismembered=false;int observedAtHook=int.MinValue;
                var observer=new AfterEquipReaction {React=()=>
                {
                    observedAtHook=f.Actor.GetStatValue(stat);
                    if(injury)dismembered=f.Body.Dismember(arm,f.Zone);
                    if(throwAfterEvent)throw new InvalidOperationException("Deliberate failure after the native AfterEquip operation.");
                }};
                f.Actor.AddPart(observer);
                if(throwAfterEvent)
                {
                    var result=InventorySystem.ExecuteCommand(new EquipCommand(item,hand),f.Actor,f.Zone);
                    Assert.IsFalse(result.Success,"The native executor must roll back the failed equip.");
                }
                else
                {
                    var transaction=new InventoryTransaction();
                    try
                    {
                        var result=new EquipCommand(item,hand).Execute(new InventoryContext(f.Actor,f.Zone),transaction);
                        Assert.IsTrue(result.Success);
                        Assert.AreEqual(injury?before:before+bonus,f.Actor.GetStatValue(stat));
                    }
                    finally { transaction.Rollback(); }
                    Assert.IsTrue(transaction.IsRolledBack);
                }
                Assert.AreEqual(1,observer.Calls);
                Assert.AreEqual(before+bonus,observedAtHook,"The real equip contribution must land before the hook.");
                Assert.AreEqual(injury,dismembered);
                Assert.AreEqual(before,f.Actor.GetStatValue(stat),"Forced cleanup and transaction undo must reverse one contribution exactly once.");
                Assert.IsFalse(InventorySystem.IsEquipped(f.Actor,item));
                if(injury)
                {
                    Assert.IsNull(arm.ParentPart,"Rollback cannot undo the independently completed injury.");
                    f.Ground(item);
                }
                else
                {
                    Assert.NotNull(arm.ParentPart);f.Detached(item);
                    Assert.IsTrue(f.Inventory.Objects.Contains(item));Assert.IsNull(f.Zone.GetEntityCell(item));
                }
            }
        }

        [TestCase(false,false)] [TestCase(false,true)]
        [TestCase(true,false)] [TestCase(true,true)]
        public void SplitScreenDroppedByIndependentInjurySurvivesOuterRollback(bool injury,bool throwAfterEvent)
        {
            using(var f=new EquipmentLifecycleFixture())
            {
                var source=f.Item("GroundwireScreen");var stacker=source.GetPart<StackerPart>();
                Assert.NotNull(stacker);Assert.AreEqual(1,source.Parts.Count(part=>part is StackerPart));
                stacker.StackCount=2;stacker.MaxStack=10;
                Assert.IsTrue(f.Inventory.AddObject(source));var arm=f.LeftArm;var hand=f.LeftHand;
                Entity split=null;bool dismembered=false;int observedAtHook=int.MinValue;
                var observer=new AfterEquipReaction {React=()=>
                {
                    split=hand._Equipped;observedAtHook=f.Actor.GetStatValue("ElectricResistance");
                    if(injury)dismembered=f.Body.Dismember(arm,f.Zone);
                    if(throwAfterEvent)throw new InvalidOperationException("Deliberate failure after equipping the split screen.");
                }};
                f.Actor.AddPart(observer);
                if(throwAfterEvent)
                    Assert.IsFalse(InventorySystem.ExecuteCommand(new EquipCommand(source,hand),f.Actor,f.Zone).Success);
                else
                {
                    var transaction=new InventoryTransaction();
                    try { Assert.IsTrue(new EquipCommand(source,hand).Execute(new InventoryContext(f.Actor,f.Zone),transaction).Success); }
                    finally { transaction.Rollback(); }
                }
                Assert.AreEqual(1,observer.Calls);Assert.AreEqual(50,observedAtHook);Assert.NotNull(split);Assert.AreNotSame(source,split);
                Assert.AreEqual(injury,dismembered);Assert.AreEqual(0,f.Actor.GetStatValue("ElectricResistance"));
                Assert.IsTrue(f.Inventory.Objects.Contains(source));Assert.AreSame(f.Actor,source.GetPart<PhysicsPart>().InInventory);
                Assert.IsFalse(InventorySystem.IsEquipped(f.Actor,source));Assert.IsFalse(InventorySystem.IsEquipped(f.Actor,split));
                if(injury)
                {
                    Assert.AreEqual(1,source.GetPart<StackerPart>().StackCount,"One screen remains in the original carried stack.");
                    Assert.AreEqual(1,split.GetPart<StackerPart>().StackCount,"The independently dropped screen must not be zeroed by stack rollback.");
                    f.Ground(split);Assert.IsNull(arm.ParentPart);
                }
                else
                {
                    Assert.AreEqual(2,source.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,split.GetPart<StackerPart>().StackCount);
                    Assert.IsFalse(f.Inventory.Objects.Contains(split));Assert.IsNull(f.Zone.GetEntityCell(split));Assert.NotNull(arm.ParentPart);
                }
                int count=f.Inventory.Objects.Where(e=>e.BlueprintName=="GroundwireScreen").Sum(e=>e.GetPart<StackerPart>().StackCount)
                    +f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="GroundwireScreen").Sum(e=>e.GetPart<StackerPart>().StackCount);
                Assert.AreEqual(2,count,"Rollback cannot duplicate, discard, or turn a real ground item into an empty shell.");
            }
        }
    }
}
