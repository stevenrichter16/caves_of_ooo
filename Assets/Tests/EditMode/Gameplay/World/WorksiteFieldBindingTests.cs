using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WorksiteFieldBindingTests : FiftyWorldFixture
    {
        const string Prefix="FieldBinding|";EntityFactory prior;
        [SetUp] public void PrepareBindings(){prior=ForgePart.Factory;ForgePart.Factory=Factory;foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}Carry("Dagger");}
        [TearDown] public void RestoreBindings(){ForgePart.Factory=prior;}
        Entity Weapon()
        {
            var head=Carry("CinderhookAxeHeadComponent");var haft=Carry("FieldHaftComponent");var binding=Carry("GuardLashingComponent");
            Assert.True(WeaponForgingService.TryForge(Actor,Factory,head,haft,binding,out var weapon,out var reason),reason);return weapon;
        }
        bool Offered(Entity w)=>Actions(w).Any(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal));
        [Test] public void BindingCanBeReplacedInTheFieldWhileHeadHaftAndIdentityRemain()
        {
            var weapon=Weapon();var replacement=Carry("PlainCordBindingComponent");var assembly=weapon.GetPart<WeaponAssemblyPart>();
            string id=weapon.ID,head=assembly.BladeBlueprint,haft=assembly.HaftBlueprint;int hit=weapon.GetPart<MeleeWeaponPart>().HitBonus,pen=weapon.GetPart<MeleeWeaponPart>().PenBonus;
            Assert.False(ForgePart.IsNearForge(Actor,Zone));string command=Choice(weapon,Prefix);
            Assert.True(Act(weapon,command));Assert.AreEqual(id,weapon.ID);Assert.AreSame(assembly,weapon.GetPart<WeaponAssemblyPart>());
            Assert.AreEqual(head,assembly.BladeBlueprint);Assert.AreEqual(haft,assembly.HaftBlueprint);Assert.AreEqual(replacement.BlueprintName,assembly.BindingBlueprint);
            Assert.AreEqual(hit-2,weapon.GetPart<MeleeWeaponPart>().HitBonus);Assert.AreEqual(pen+1,weapon.GetPart<MeleeWeaponPart>().PenBonus);
            Assert.AreEqual(0,Count("PlainCordBindingComponent"));Assert.AreEqual(0,Count("GuardLashingComponent"));Assert.False(Act(weapon,command));
        }
        [Test] public void PackRowDeclaresOneActionRatherThanFreeFieldWork()
        {
            var weapon=Weapon();Carry("PlainCordBindingComponent");
            Assert.AreEqual(ActionTime.OneAction,Actions(weapon).Single(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal)).Time);
        }
        [TestCase("no-tool")] [TestCase("blade")] [TestCase("haft")] [TestCase("tempered")] [TestCase("equipped")] [TestCase("stack")]
        public void FieldWorkDoesNotGrantAnOrdinaryForgeRefit(string change)
        {
            var weapon=Weapon();Carry(change=="blade"?"IronSpikeComponent":change=="haft"?"OakHaftComponent":"PlainCordBindingComponent");
            if(change=="no-tool")Pack.RemoveObject(Pack.Objects.Single(e=>e.BlueprintName=="Dagger"));
            if(change=="tempered")weapon.AddPart(new WeaponTemperPart{TemperCount=1});
            if(change=="equipped")Assert.True(InventorySystem.Equip(Actor,weapon));
            if(change=="stack")weapon.GetPart<StackerPart>().StackCount=2;
            Assert.False(Offered(weapon));Assert.AreEqual("GuardLashingComponent",weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);
        }
        [TestCase("tool-drop")] [TestCase("assembly")] [TestCase("replacement-drop")]
        public void AChangedSelectionCannotExecute(string change)
        {
            var weapon=Weapon();var replacement=Carry("PlainCordBindingComponent");string command=Choice(weapon,Prefix);
            if(change=="tool-drop")Pack.RemoveObject(Pack.Objects.Single(e=>e.BlueprintName=="Dagger"));
            if(change=="assembly")weapon.GetPart<WeaponAssemblyPart>().HaftBlueprint="OakHaftComponent";
            if(change=="replacement-drop")Pack.RemoveObject(replacement);
            Assert.False(Act(weapon,command));Assert.AreEqual("GuardLashingComponent",weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);
        }
        [Test] public void FailureRefundsTheBindingAndRestoresWeaponContributions()
        {
            var weapon=Weapon();var replacement=Carry("PlainCordBindingComponent");int pen=weapon.GetPart<MeleeWeaponPart>().PenBonus;
            string command=Choice(weapon,Prefix);FailAfter();Assert.False(Act(weapon,command));
            Assert.AreEqual("GuardLashingComponent",weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);
            Assert.AreEqual(pen,weapon.GetPart<MeleeWeaponPart>().PenBonus);Assert.True(Pack.Objects.Contains(replacement));Assert.AreEqual(0,Count("GuardLashingComponent"));
        }
        [Test] public void SavingTheRefittedWeaponDoesNotProduceAnotherBinding()
        {
            var weapon=Weapon();Carry("PlainCordBindingComponent");Assert.True(Act(weapon,Choice(weapon,Prefix)));
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(weapon);Assert.AreEqual(weapon.ID,saved.ID);
            Assert.AreEqual("PlainCordBindingComponent",saved.GetPart<WeaponAssemblyPart>().BindingBlueprint);Assert.AreEqual(0,Count("GuardLashingComponent"));
        }
        [Test] public void ReadingWeaponCannotInvalidateItsOpenFieldQuote()
        {
            var weapon=Weapon();Carry("PlainCordBindingComponent");string command=Choice(weapon,Prefix);
            for(int i=0;i<4;i++)Assert.IsNotEmpty(UseGuides.SectionText(weapon,Zone,null,Actor));
            Assert.True(Act(weapon,command));Assert.AreEqual("PlainCordBindingComponent",weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);
        }
        public sealed class After : Part
        {public Action Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;}}
        [Test] public void FailureDoesNotHealIndependentPlayerOrWeaponDamage()
        {
            var weapon=Weapon();Carry("PlainCordBindingComponent");string command=Choice(weapon,Prefix);
            int playerHP=Actor.GetStatValue("Hitpoints"),weaponHP=weapon.GetStatValue("Hitpoints");
            Actor.AddPart(new After{Callback=()=>{Actor.GetStat("Hitpoints").BaseValue--;weapon.GetStat("Hitpoints").BaseValue--;throw new InvalidOperationException("independent injuries");}});
            Assert.False(Act(weapon,command));Assert.AreEqual(playerHP-1,Actor.GetStatValue("Hitpoints"));Assert.AreEqual(weaponHP-1,weapon.GetStatValue("Hitpoints"));
            Assert.AreEqual("GuardLashingComponent",weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);Assert.AreEqual(1,Count("PlainCordBindingComponent"));
        }
        [Test] public void PaidSharpContributionSurvivesWithoutAccumulating()
        {
            var weapon=Weapon();weapon.SetTag("ModSharp");weapon.GetPart<MeleeWeaponPart>().PenBonus++;
            int before=weapon.GetPart<MeleeWeaponPart>().PenBonus;Carry("PlainCordBindingComponent");
            Assert.True(Act(weapon,Choice(weapon,Prefix)));Assert.AreEqual(before+1,weapon.GetPart<MeleeWeaponPart>().PenBonus);Assert.True(weapon.HasTag("ModSharp"));
        }
        [Test] public void WeaponCannotSupplyItsOwnCuttingWork()
        {
            var weapon=Weapon();weapon.AddPart(new CuttingToolPart());Carry("PlainCordBindingComponent");
            Pack.RemoveObject(Pack.Objects.Single(e=>e.BlueprintName=="Dagger"));Assert.False(Offered(weapon));
        }
        [Test] public void NonemptyTemperResidueCannotBeSilentlyErased()
        {
            var weapon=Weapon();weapon.AddPart(new WeaponTemperPart{TemperCount=0,AppliedSpecsRaw="Burning,20,,2,1"});Carry("PlainCordBindingComponent");Assert.False(Offered(weapon));
        }
        [Test] public void ChangedAuthoredComponentNumbersInvalidateTheQuote()
        {
            var weapon=Weapon();Carry("PlainCordBindingComponent");string command=Choice(weapon,Prefix);
            Factory.Blueprints["FieldHaftComponent"].Parts["WeaponComponent"]["HitBonus"]="500";
            Assert.False(Act(weapon,command));Assert.AreEqual(1,Count("PlainCordBindingComponent"));Assert.AreEqual("GuardLashingComponent",weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);
        }
        [Test] public void RollbackCannotWriteIntoAMeleePartTransferredToAnotherOwner()
        {
            var weapon=Weapon();Carry("PlainCordBindingComponent");string command=Choice(weapon,Prefix);var melee=weapon.GetPart<MeleeWeaponPart>();var foreign=Factory.CreateEntity("Dagger");
            Actor.AddPart(new After{Callback=()=>{weapon.RemovePart(melee);foreign.RemovePart(foreign.GetPart<MeleeWeaponPart>());foreign.AddPart(melee);melee.PenBonus=71;}});
            Assert.False(Act(weapon,command));Assert.AreEqual(71,melee.PenBonus);Assert.AreSame(foreign,melee.ParentEntity);Assert.AreEqual(1,Count("PlainCordBindingComponent"));
        }
        [Test] public void RefundCannotConvertASpentUnitIntoAnAlteredBinding()
        {
            var weapon=Weapon();var replacement=Carry("PlainCordBindingComponent");replacement.GetPart<StackerPart>().StackCount=2;
            string command=Choice(weapon,Prefix);
            Actor.AddPart(new After{Callback=()=>replacement.GetPart<WeaponComponentPart>().HitBonus=90});
            Assert.False(Act(weapon,command));Assert.AreEqual(1,replacement.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(90,replacement.GetPart<WeaponComponentPart>().HitBonus);Assert.AreEqual("GuardLashingComponent",weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);
        }
    }
}
