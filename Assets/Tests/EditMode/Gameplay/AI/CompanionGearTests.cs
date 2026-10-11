using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class CompanionGearTests
    {
        Zone zone; Entity leader, follower; Body body;
        InventoryPart Pack => follower.GetPart<InventoryPart>();
        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); MessageLog.Clear(); zone=new Zone("companion-gear");
            leader=Creature("leader",10,10);leader.SetTag("Player");follower=Creature("follower",11,10);
            body=new Body();body.SetBody(AnatomyFactory.CreateHumanoid());follower.AddPart(body);
            Assert.True(follower.ApplyEffect(new RecruitedEffect(leader),leader,zone));
        }
        Entity Creature(string id,int x,int y)
        {
            var e=new Entity{ID=id,BlueprintName=id};e.SetTag("Creature");e.SetTag("Faction","Villagers");
            e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new RenderPart{DisplayName=id});e.AddPart(new InventoryPart());e.AddPart(new StatusEffectsPart());e.AddPart(new BrainPart{CurrentZone=zone,Rng=new Random(4)});
            e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Max=20};Assert.True(zone.AddEntity(e,x,y));zone.GetCell(x,y).IsVisible=zone.GetCell(x,y).Explored=true;return e;
        }
        Entity Gear(string id="sword",string slot="Hand")
        {var e=new Entity{ID=id,BlueprintName=id};e.AddPart(new PhysicsPart{Takeable=true,Weight=2});e.AddPart(new RenderPart{DisplayName=id});e.AddPart(new EquippablePart{Slot=slot});Assert.True(Pack.AddObject(e));return e;}
        string Choice(string prefix, Entity item=null) => WorldInteractionSystem.GatherActions(follower,leader).FirstOrDefault(a=>a.Command.StartsWith(prefix+"|"+(item==null?"":Uri.EscapeDataString(item.ID)+"|"),StringComparison.Ordinal))?.Command;
        bool Run(string command)=>InventorySystem.PerformAction(leader,follower,command,zone);
        [Test] public void MenuDeliberatelyReplacesChosenHandAndLeavesOldGearInCompanionPack()
        {
            var old=Gear("old");var hand=body.GetPartsByType("Hand")[0];Assert.True(InventorySystem.Equip(follower,old,hand));var item=Gear();
            string command=Choice("CompanionEquip",item);Assert.NotNull(command);Assert.True(Run(command));
            Assert.AreSame(item,hand._Equipped);Assert.Contains(old,Pack.Objects);Assert.AreSame(follower,old.GetPart<PhysicsPart>().InInventory);Assert.False(Pack.Objects.Contains(item));
            Assert.False(Run(command),"Already worn item cannot be equipped again through stale choice.");
        }
        [Test] public void ComparisonUsesActualFollowerGearAndDoesNotEquip()
        {var item=Gear();string command=Choice("CompanionCompare",item);Assert.NotNull(command);Assert.True(Run(command));StringAssert.Contains("Equipment comparison",MessageLog.ConsumeAnnouncement());Assert.Contains(item,Pack.Objects);Assert.False(InventorySystem.IsEquipped(follower,item));}
        [Test] public void UnequipIsExplicitAndDoesNotRetrieveIntoPlayerPack()
        {var item=Gear();Assert.True(InventorySystem.Equip(follower,item));string command=Choice("CompanionUnequip",item);Assert.NotNull(command);Assert.True(Run(command));Assert.Contains(item,Pack.Objects);Assert.False(leader.GetPart<InventoryPart>().Objects.Contains(item));Assert.False(InventorySystem.IsEquipped(follower,item));Assert.False(Run(command));}
        [TestCase("incompatible")] [TestCase("bound")] [TestCase("foreign")] [TestCase("missing-limb")]
        public void InvalidGearOrAnatomyCannotBeEquipped(string change)
        {var item=Gear();string command=Choice("CompanionEquip",item);Assert.NotNull(command);if(change=="incompatible")item.GetPart<EquippablePart>().Slot="Tentacle";if(change=="bound")item.SetTag("NoTrade");if(change=="foreign")item.GetPart<PhysicsPart>().InInventory=leader;if(change=="missing-limb")body.SetBody(new BodyPart{Type="Body"});Assert.False(Run(command));Assert.False(InventorySystem.IsEquipped(follower,item));}
        public sealed class Callback:Part
        {public string Event;public Action Change;public Action<GameEvent> Edit;public bool Veto;public override string Name=>"GearCallback";public override bool HandleEvent(GameEvent e){if(e.ID==Event){Change?.Invoke();Edit?.Invoke(e);return !Veto;}return true;}}
        [TestCase("BeforeEquip")] [TestCase("BeforeUnequip")] public void EquipmentVetoPreservesOldAndNewOwners(string veto)
        {var old=Gear("old");Assert.True(InventorySystem.Equip(follower,old,body.GetPartsByType("Hand")[0]));var item=Gear();string command=Choice("CompanionEquip",item);Assert.NotNull(command);follower.AddPart(new Callback{Event=veto,Veto=true});Assert.False(Run(command));Assert.True(InventorySystem.IsEquipped(follower,old));Assert.Contains(item,Pack.Objects);}
        [Test] public void ChangedDisplacementAfterMenuDoesNotReplaceUnpreviewedGear()
        {var item=Gear();string command=Choice("CompanionEquip",item);Assert.NotNull(command);var other=Gear("other");var hand=body.GetPartsByType("Hand")[0];Assert.True(InventorySystem.Equip(follower,other,hand));Assert.False(Run(command));Assert.AreSame(other,hand._Equipped);Assert.Contains(item,Pack.Objects);}
        [Test] public void BeforeEquipCallbackCannotIntroduceAnUnpreviewedReplacement()
        {var item=Gear();var other=Gear("other");string command=Choice("CompanionEquip",item);Assert.NotNull(command);var hand=body.GetPartsByType("Hand")[0];bool entered=false;follower.AddPart(new Callback{Event="BeforeEquip",Change=()=>{if(entered)return;entered=true;Assert.True(InventorySystem.Equip(follower,other,hand));}});Assert.False(Run(command));Assert.AreSame(other,hand._Equipped);Assert.Contains(item,Pack.Objects);}
        [Test] public void OuterFailureRestoresEquipmentAndCarriedItem()
        {var old=Gear("old");var hand=body.GetPartsByType("Hand")[0];Assert.True(InventorySystem.Equip(follower,old,hand));var item=Gear();string command=Choice("CompanionEquip",item);Assert.NotNull(command);leader.AddPart(new Callback{Event="AfterInventoryAction",Change=()=>throw new InvalidOperationException("rollback")});Assert.False(Run(command));Assert.AreSame(old,hand._Equipped);Assert.Contains(item,Pack.Objects);Assert.False(Pack.Objects.Contains(old));}
        [TestCase(false)] [TestCase(true)] public void StackedGearEquipsOneUnitAndOuterFailureRestoresAllUnits(bool fail)
        {
            var item=Gear();item.AddPart(new StackerPart{StackCount=3});string command=Choice("CompanionEquip",item);Assert.NotNull(command);
            if(fail)leader.AddPart(new Callback{Event="AfterInventoryAction",Change=()=>throw new InvalidOperationException("stack rollback")});
            Assert.AreEqual(!fail,Run(command));Assert.AreEqual(fail?3:2,item.GetPart<StackerPart>().StackCount);Assert.Contains(item,Pack.Objects);
            var equipped=body.GetPartsByType("Hand")[0]._Equipped;
            if(fail)Assert.Null(equipped);else{Assert.NotNull(equipped);Assert.AreNotSame(item,equipped);Assert.AreEqual(1,equipped.GetPart<StackerPart>().StackCount);Assert.AreSame(follower,equipped.GetPart<PhysicsPart>().Equipped);}
        }
        [Test] public void SaveGraphRetainsChosenCompanionEquipmentAndDisplacedPackItem()
        {
            var old=Gear("old");Assert.True(InventorySystem.Equip(follower,old,body.GetPartsByType("Hand")[0]));var item=Gear();Assert.True(Run(Choice("CompanionEquip",item)));
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(follower);var loadedPack=loaded.GetPart<InventoryPart>();
            Assert.AreEqual("sword",loaded.GetPart<Body>().GetPartsByType("Hand")[0]._Equipped.ID);Assert.AreSame(loaded,loaded.GetPart<Body>().GetPartsByType("Hand")[0]._Equipped.GetPart<PhysicsPart>().Equipped);
            Assert.True(loadedPack.Objects.Any(e=>e.ID=="old"&&e.GetPart<PhysicsPart>().InInventory==loaded));Assert.AreSame(loaded.GetEffect<RecruitedEffect>().Recruiter,loaded.GetPart<BrainPart>().PartyLeader);
        }
        [TestCase("source")] [TestCase("body")] [TestCase("dismiss")]
        public void BeforeEquipInvalidationCannotStealTheChangedSourceOrUseDetachedBody(string change)
        {
            var item=Gear();string command=Choice("CompanionEquip",item);Assert.NotNull(command);
            follower.AddPart(new Callback{Event="BeforeEquip",Change=()=>{
                if(change=="source"){Assert.True(Pack.RemoveObject(item));Assert.True(leader.GetPart<InventoryPart>().AddObject(item));}
                if(change=="body"){follower.RemovePart(body);var replacement=new Body();replacement.SetBody(AnatomyFactory.CreateHumanoid());follower.AddPart(replacement);}
                if(change=="dismiss")follower.GetEffect<RecruitedEffect>().Dismiss(leader);
            }});
            Assert.False(Run(command));Assert.False(InventorySystem.IsEquipped(follower,item));
            if(change=="source"){Assert.Contains(item,leader.GetPart<InventoryPart>().Objects);Assert.AreSame(leader,item.GetPart<PhysicsPart>().InInventory);}else Assert.Contains(item,Pack.Objects);
        }
        [TestCase(false)] [TestCase(true)] public void BoundWornGearCannotBeRemovedDirectlyOrByReplacement(bool replace)
        {
            var old=Gear("old");Assert.True(InventorySystem.Equip(follower,old,body.GetPartsByType("Hand")[0]));var item=Gear();string command=Choice(replace?"CompanionEquip":"CompanionUnequip",replace?item:old);Assert.NotNull(command);old.SetTag("NoDrop");
            Assert.False(Run(command));Assert.True(InventorySystem.IsEquipped(follower,old));Assert.Contains(item,Pack.Objects);
        }
        [Test] public void AfterInventoryDismissalRollsBackEquipmentWithoutReRecruiting()
        {
            var item=Gear();string command=Choice("CompanionEquip",item);Assert.NotNull(command);leader.AddPart(new Callback{Event="AfterInventoryAction",Change=()=>follower.GetEffect<RecruitedEffect>().Dismiss(leader)});
            Assert.False(Run(command));Assert.Contains(item,Pack.Objects);Assert.False(InventorySystem.IsEquipped(follower,item));Assert.Null(follower.GetEffect<RecruitedEffect>());
        }
        [Test] public void UnequipVetoRetainsWornItem()
        {var item=Gear();Assert.True(InventorySystem.Equip(follower,item));string command=Choice("CompanionUnequip",item);Assert.NotNull(command);follower.AddPart(new Callback{Event="BeforeUnequip",Veto=true});Assert.False(Run(command));Assert.True(InventorySystem.IsEquipped(follower,item));Assert.False(Pack.Objects.Contains(item));}
        [Test] public void ComparisonChangedByOuterCallbackIsNotPublishedAsCurrent()
        {var item=Gear();string command=Choice("CompanionCompare",item);Assert.NotNull(command);leader.AddPart(new Callback{Event="AfterInventoryAction",Change=()=>Assert.True(InventorySystem.Equip(follower,item))});Assert.False(Run(command));Assert.True(InventorySystem.IsEquipped(follower,item));Assert.False(MessageLog.HasPendingAnnouncement);}
        [TestCase(false)] [TestCase(true)] public void SplitPreparedGearCannotChangeItsRequirementsOrBecomeBound(bool bind)
        {
            var old=Gear("protected");var hands=body.GetPartsByType("Hand");Assert.True(InventorySystem.Equip(follower,old,hands[1]));old.SetTag("NoDrop");
            var item=Gear();item.AddPart(new StackerPart{StackCount=3});string command=Choice("CompanionEquip",item);Assert.NotNull(command);
            var callback=new Callback{Event="BeforeEquip",Edit=e=>{var prepared=e.GetParameter<Entity>("Item");Assert.AreNotSame(item,prepared);if(bind)prepared.SetTag("NoDrop");else prepared.GetPart<EquippablePart>().UsesSlots="Hand,Hand";}};follower.AddPart(callback);
            Assert.False(Run(command));Assert.AreSame(old,hands[1]._Equipped);Assert.Null(hands[0]._Equipped);Assert.AreEqual(3,item.GetPart<StackerPart>().StackCount);
            follower.RemovePart(callback);Assert.True(Run(command));Assert.AreSame(old,hands[1]._Equipped);Assert.AreEqual(2,item.GetPart<StackerPart>().StackCount);
        }
    }
}
