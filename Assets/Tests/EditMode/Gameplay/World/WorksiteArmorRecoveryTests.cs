using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorksiteArmorRecoveryTests : FiftyWorldFixture
    {
        const string Process="reclaim_leather_armor", Prefix="MaterialProcess|reclaim_leather_armor|";
        EntityFactory priorForge;
        [SetUp] public void PrepareRecovery()
        {
            priorForge=ForgePart.Factory;ForgePart.Factory=Factory;
            MaterialProcessRegistry.ResetForTests();
            Assert.IsEmpty(MaterialProcessRegistry.InitializeFromJson(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialProcesses.json"))));
            foreach(var cell in Zone.Cells){cell.IsVisible=true;cell.Explored=true;}
            Carry("Dagger");
        }
        [TearDown] public void RestoreRecovery(){ForgePart.Factory=priorForge;MaterialProcessRegistry.ResetForTests();}
        bool Offered(Entity armor)=>Actions(armor).Any(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal));
        [Test] public void SpareArmorBecomesOneFiniteOffcutWithoutAForge()
        {
            var armor=Carry("LeatherArmor");int tick=Clock.TickCount;
            string action=Choice(armor,Prefix);Assert.AreEqual(tick,Clock.TickCount);
            Assert.True(Act(armor,action));Assert.AreEqual(0,Count("LeatherArmor"));Assert.AreEqual(1,Count("CuredLeatherOffcut"));
            Assert.False(Act(armor,action));Assert.AreEqual(1,Count("CuredLeatherOffcut"));Assert.AreEqual(1,Count("Dagger"));
        }
        [Test] public void ReadingArmorCannotInvalidateItsOpenMenuQuote()
        {
            var armor=Carry("LeatherArmor");string command=Choice(armor,Prefix);
            for(int i=0;i<4;i++)Assert.IsNotEmpty(UseGuides.SectionText(armor,Zone,null,Actor));
            Assert.True(Act(armor,command));Assert.AreEqual(1,Count("CuredLeatherOffcut"));
        }
        [Test] public void OneArmorInAStackIsSpentAndTheOthersRemainUsable()
        {
            var armor=Carry("LeatherArmor");armor.GetPart<StackerPart>().StackCount=3;
            Assert.True(Act(armor,Choice(armor,Prefix)));Assert.AreEqual(2,armor.GetPart<StackerPart>().StackCount);
            Assert.NotNull(armor.GetPart<StackerPart>().SplitStack(1));Assert.AreEqual(1,Count("CuredLeatherOffcut"));
        }
        [Test] public void EmptyUnequipReceiptsDoNotPreventRecoveryOrLikeStateStacking()
        {
            var armor=Carry("LeatherArmor");var spare=Factory.CreateEntity("LeatherArmor");
            armor.GetPart<EquippablePart>().AppliedEquipBonuses="";armor.GetPart<EquippablePart>().AppliedEquipSkillBonuses="";
            Assert.True(armor.GetPart<StackerPart>().CanStackWith(spare));
            Assert.True(Act(armor,Choice(armor,Prefix)));Assert.AreEqual(1,Count("CuredLeatherOffcut"));
        }
        [Test] public void NormalEquipUnequipThenReclaimWorks()
        {
            var armor=Carry("LeatherArmor");Assert.True(InventorySystem.Equip(Actor,armor));Assert.False(Offered(armor));
            Assert.True(InventorySystem.UnequipItem(Actor,armor));Assert.True(Act(armor,Choice(armor,Prefix)));
        }
        [Test] public void EquipOneOfTwoThenReclaimTheSparePreservesTheWornUnit()
        {
            var armor=Carry("LeatherArmor");armor.GetPart<StackerPart>().StackCount=2;
            Assert.True(InventorySystem.Equip(Actor,armor));var worn=Pack.EquippedItems.Values.Single(e=>e.BlueprintName=="LeatherArmor");
            var spare=Pack.Objects.Single(e=>e.BlueprintName=="LeatherArmor");Assert.AreNotSame(worn,spare);
            Assert.True(Act(spare,Choice(spare,Prefix)));Assert.AreSame(Actor,worn.GetPart<PhysicsPart>().Equipped);Assert.AreEqual(1,Count("CuredLeatherOffcut"));
        }
        [TestCase("no-tool")] [TestCase("foreign-part")] [TestCase("damaged")] [TestCase("repaired-reference")]
        [TestCase("receipt")] [TestCase("other-armor")] [TestCase("explicit-denial")]
        public void UnsupportedOrUnpermittedStockIsNeverSilentlyReduced(string kind)
        {
            var armor=Carry(kind=="other-armor"?"ChainMail":"LeatherArmor");
            if(kind=="no-tool")Pack.RemoveObject(Pack.Objects.Single(e=>e.BlueprintName=="Dagger"));
            if(kind=="foreign-part")armor.AddPart(new ContainerPart());
            if(kind=="damaged")armor.GetStat("Hitpoints").BaseValue--;
            if(kind=="repaired-reference")armor.GetPart<RepairablePart>().RepairedBy=Actor;
            if(kind=="receipt")armor.GetPart<EquippablePart>().AppliedEquipBonuses="Strength:3";
            if(kind=="explicit-denial")armor.AddPart(new MaterialProcessInputPart{ProcessIds=""});
            Assert.False(Offered(armor));Assert.AreEqual(0,Count("CuredLeatherOffcut"));Assert.True(Pack.Objects.Contains(armor));
        }
        [Test] public void RecoveryRollsBackWithAnOuterFailure()
        {
            var armor=Carry("LeatherArmor");armor.GetPart<StackerPart>().StackCount=3;string action=Choice(armor,Prefix);FailAfter();
            Assert.False(Act(armor,action));Assert.AreEqual(3,Count("LeatherArmor"));Assert.AreEqual(0,Count("CuredLeatherOffcut"));
        }
        public sealed class After : Part
        {public Action Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;}}
        [Test] public void FailedArmorRecoveryDoesNotHealAnIndependentInjury()
        {
            var armor=Carry("LeatherArmor");string action=Choice(armor,Prefix);int hp=Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new After{Callback=()=>{Actor.GetStat("Hitpoints").BaseValue--;throw new InvalidOperationException("independent injury");}});
            Assert.False(Act(armor,action));Assert.AreEqual(hp-1,Actor.GetStatValue("Hitpoints"));Assert.AreEqual(1,Count("LeatherArmor"));Assert.AreEqual(0,Count("CuredLeatherOffcut"));
        }
        [Test] public void SavedOldArmorGetsOnlyTheManifestBackedAction()
        {
            var armor=Carry("LeatherArmor");var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(armor);
            Pack.RemoveObject(armor);Assert.True(Pack.AddObject(saved));Assert.True(Act(saved,Choice(saved,Prefix)));
        }
        [Test] public void DamagedArmorDoesNotMergeIntoSoundStock()
        {
            var armor=Carry("LeatherArmor");var other=Factory.CreateEntity("LeatherArmor");other.GetStat("Hitpoints").BaseValue--;
            Assert.False(armor.GetPart<StackerPart>().CanStackWith(other));
        }
        [TestCase("stat")] [TestCase("skill")] [TestCase("repair")]
        public void SplittingCannotEraseUnsupportedContributionOrOwnerReferences(string receipt)
        {
            var armor=Carry("LeatherArmor");armor.GetPart<StackerPart>().StackCount=2;
            if(receipt=="stat")armor.GetPart<EquippablePart>().AppliedEquipBonuses="Strength:3";
            if(receipt=="skill")armor.GetPart<EquippablePart>().AppliedEquipSkillBonuses="CudgelSkill:1";
            if(receipt=="repair")armor.GetPart<RepairablePart>().RepairedBy=Actor;
            Assert.Null(armor.GetPart<StackerPart>().SplitStack(1));Assert.AreEqual(2,armor.GetPart<StackerPart>().StackCount);
            Assert.False(Offered(armor));Assert.AreEqual(0,Count("CuredLeatherOffcut"));
        }
        [Test] public void TheYieldIsAnExplicitDestructiveManifest()
        {
            var definition=MaterialProcessRegistry.Get(Process);Assert.NotNull(definition);
            Assert.AreEqual("LeatherArmor",definition.SourceBlueprint);Assert.AreEqual("Reclaim",definition.Mode);
            Assert.AreEqual("HandCutting",definition.WorkKind);Assert.AreEqual(1,definition.InputCount);
            CollectionAssert.AreEqual(new[]{"CuredLeatherOffcut"},definition.OutputBlueprints);
            CollectionAssert.AreEqual(new[]{1},definition.OutputCounts);Assert.IsNotEmpty(definition.LossText);
        }
    }
}
