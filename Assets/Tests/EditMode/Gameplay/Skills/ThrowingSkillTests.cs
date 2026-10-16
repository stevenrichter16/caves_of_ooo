using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class ThrowingSkillTests : FiftyWorldFixture
    {
        const string Measured = "Throwing_MeasuredThrow", Driving = "Throwing_DrivingThrow", Recovery = "Throwing_RecoveringStep";
        SkillsPart Skills => Actor.GetPart<SkillsPart>();
        Entity Target()
        {
            var target = Place("MarlbackScrabbler", 14, 10);
            Zone.GetCell(14, 10).IsVisible = Zone.GetCell(14, 10).Explored = true;
            return target;
        }
        void Grant(string name) => Assert.True(Skills.AddSkill(name));
        void Prepare(Entity item, Entity target)
            => Assert.True(ThrowingRules.Prepare(Actor, item, Zone, target, Zone.GetEntityCell(target), out string reason), reason);
        ThrowSkillLaunch Capture(Entity item, Entity target, ThrowMode mode = ThrowMode.Ordinary)
            => ThrowingRules.CaptureLaunch(Actor, item, Zone, target, Zone.GetEntityCell(target), mode);
        [TearDown] public void ResetRegistry() => SkillRegistry.ResetForTests();

        [Test] public void CatalogContainsFiveOnePointOneRankNodesAndNoSkimmingTax()
        {
            var path = Path.Combine(Application.dataPath, "Resources/Content/Data/Skills/Throwing.json");
            Assert.True(File.Exists(path), "The playable tree must be real authored content.");
            SkillRegistry.InitializeFromJson(File.ReadAllText(path));
            Assert.True(SkillRegistry.TryGetSkillByClass("ThrowingSkill", out var root));
            Assert.AreEqual(1, root.Cost); Assert.AreEqual(4, root.Powers.Count);
            foreach (var node in root.Powers)
            { Assert.AreEqual(1, node.Cost); Assert.AreEqual(1, SkillRanks.Maximum(node.Class)); Assert.True(SkillsPart.IsKnownSkillClass(node.Class)); }
            Assert.False(root.Powers.Any(p => p.Class.Contains("Skimming")));
        }
        [Test] public void ChildGrantOperatesWithoutRootButPurchaseRequiresRoot()
        {
            SkillRegistry.ResetForTests(); Actor.GetStat("SP").BaseValue = 10;
            Assert.False(BuySkillAction.Execute(Actor, Measured).Succeeded);
            Grant(Measured); Prepare(Carry("Dagger"), Target());
            Assert.False(Skills.HasSkill("ThrowingSkill"));
        }
        [Test] public void DeferredSelectionIsFreeAndDoesNotCreatePreparation()
        {
            Grant(Measured); int tick = Clock.TickCount;
            Assert.True(ThrowingRules.TryBeginSelection(Actor, "CommandMeasuredThrow", out var request));
            Assert.AreEqual(ThrowSelectionKind.Measured, request.Kind);
            Assert.AreEqual(tick, Clock.TickCount);
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual(0, ThrowingRules.CooldownRemaining(Actor, Measured));
        }
        [TestCase(false)] [TestCase(true)] public void RetryIsOwnedAndConsumesOnlyOneAdditionalAccuracyDraw(bool owned)
        {
            if (owned) Grant("ThrowingSkill"); var target = Target(); var item = Carry("Dagger");
            var shot = Capture(item, target); ThrowingRules.CommitLaunch(shot);
            var rng = new FixedRandom(3);
            Assert.AreEqual(owned, ThrowingRules.RetryPhysicalAccuracy(shot, Actor, rng));
            Assert.AreEqual(owned ? 1 : 0, rng.Calls);
        }
        [TestCase(1)] [TestCase(2)] public void RetryDoesNotMakeImpossibleAgilityHit(int agility)
        {
            Grant("ThrowingSkill"); Actor.GetStat("Agility").BaseValue = agility;
            var shot = Capture(Carry("Dagger"), Target()); ThrowingRules.CommitLaunch(shot);
            Assert.False(ThrowingRules.RetryPhysicalAccuracy(shot, Actor, new FixedRandom(agility)));
        }
        [Test] public void PreparationPaysCooldownAndSurvivesOnlyItsOwnClosingTurn()
        {
            Grant(Measured); var item = Carry("Dagger"); var target = Target(); Prepare(item, target);
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Measured));
            ThrowingRules.OnOwnerTurnEnded(Actor, Zone);
            Assert.True(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual(5, ThrowingRules.CooldownRemaining(Actor, Measured));
            ThrowingRules.OnOwnerTurnEnded(Actor, Zone);
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
        }
        [Test] public void PurePreviewDoesNotConsumePreparationAndLaunchConsumesItOnce()
        {
            Grant(Measured); var item = Carry("Dagger"); var target = Target(); Prepare(item, target);
            var shot = Capture(item, target); Assert.True(Actor.GetPart<ThrowingStatePart>().Prepared);
            ThrowingRules.CommitLaunch(shot); Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual(4, ThrowingRules.PenetrationBonusFor(shot, target));
            Assert.AreEqual(0, ThrowingRules.PenetrationBonusFor(shot, Actor));
            var second = Capture(item, target); ThrowingRules.CommitLaunch(second);
            Assert.AreEqual(0, ThrowingRules.PenetrationBonusFor(second, target));
        }
        [TestCase(-1,false)] [TestCase(-1,true)]
        [TestCase(0,false)] [TestCase(0,true)]
        [TestCase(2,false)] [TestCase(2,true)]
        public void MeasuredRevisionAddsFourToActualThrowPenetrationAndEffectiveCap(int strengthCap,bool prepared)
        {
            Grant(Measured); var item=Carry("Dagger"); var target=Target();
            Actor.GetStat("Strength").BaseValue=30;
            var weapon=item.GetPart<MeleeWeaponPart>();weapon.Stat="Strength";weapon.PenBonus=1;weapon.MaxStrengthBonus=strengthCap;
            target.GetStat("Hitpoints").Max=1000;target.GetStat("Hitpoints").BaseValue=1000;
            if(prepared){Prepare(item,target);ThrowingRules.OnOwnerTurnEnded(Actor,Zone);}
            Diag.ResetAll();
            var result=InventorySystem.ExecuteCommand(new ThrowItemCommand(item,14,10,new System.Random(123)),Actor,Zone);
            Assert.True(result.ActionCommitted,result.ErrorMessage);Assert.True(result.Success,result.ErrorMessage);
            var records=DiagQuery.Apply(new DiagQuery.Filter{Category="damage",Kind="ThrowPenetration",Limit=5}).Records;
            Assert.AreEqual(1,records.Count,"The real physical throw must reach its one penetration calculation.");
            var values=JsonUtility.FromJson<ThrowPenetrationValues>(records[0].PayloadJson);
            Assert.AreEqual(prepared?12:8,values.totalBonus,"Strength 30 is +7; controlled weapon +1; paid preparation adds exactly four.");
            int ordinaryCap=strengthCap<0?CombatSystem.LEGACY_UNCAPPED_MAX_STR_BONUS+1:strengthCap+1;
            Assert.AreEqual(ordinaryCap+(prepared?4:0),values.maxBonus,"Preparation raises the effective cap too; the legacy uncapped sentinel is retained.");
            Assert.AreEqual(prepared?5:0,ThrowingRules.CooldownRemaining(Actor,Measured),"Tuning does not change the paid preparation cooldown.");
            Assert.False(Actor.GetPart<ThrowingStatePart>()?.Prepared??false);
        }
        [Test] public void MeasuredRevisionRuntimeAndAuthoredDescriptionsMatchThePaidFourPointBonus()
        {
            var text=new Throwing_MeasuredThrow().DescribeRank(1);
            StringAssert.Contains("+4 penetration and cap",text);StringAssert.DoesNotContain("+2",text);
            var path=Path.Combine(Application.dataPath,"Resources/Content/Data/Skills/Throwing.json");
            SkillRegistry.InitializeFromJson(File.ReadAllText(path));
            Assert.True(SkillRegistry.TryGetPowerByClass(Measured,out var node));
            StringAssert.Contains("+4 penetration and cap",node.Description);StringAssert.DoesNotContain("+2",node.Description);
            StringAssert.Contains("cooldown 6",node.Description);
            Assert.AreEqual(1,node.Cost);Assert.AreEqual(1,SkillRanks.Maximum(Measured));
        }
        [Test] public void IncompatibleCommittedThrowConsumesPreparationWithoutBonus()
        {
            Grant(Measured); var item = Carry("Dagger"); var target = Target(); Prepare(item, target);
            var other = Carry("Cudgel"); var shot = Capture(other, target); ThrowingRules.CommitLaunch(shot);
            Assert.Zero(ThrowingRules.PenetrationBonusFor(shot, target)); Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
        }
        [Test] public void ActorMoveOutAndBackInterruptsPreparation()
        {
            Grant(Measured); var item = Carry("Dagger"); var target = Target(); Prepare(item, target);
            Assert.True(MovementSystem.TryMove(Actor, Zone, 0, 1)); Assert.True(MovementSystem.TryMove(Actor, Zone, 0, -1));
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
        }
        [Test] public void SourceOwnershipMutationAndRemovalCannotRevivePreparation()
        {
            Grant(Measured); var item = Carry("Dagger"); var target = Target(); Prepare(item, target);
            ThrowingRules.OnItemCustodyChanged(item);
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
            Skills.RemoveSkill(Skills.GetSkill(Measured)); Grant(Measured);
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Measured));
            Assert.False(ThrowingRules.TryBeginSelection(Actor, "CommandMeasuredThrow", out _));
        }
        [Test] public void HiddenTargetAndPayloadCannotBePrepared()
        {
            Grant(Measured); var target = Target(); var item = Carry("Dagger"); Zone.GetEntityCell(target).IsVisible = false;
            Assert.False(ThrowingRules.Prepare(Actor, item, Zone, target, Zone.GetEntityCell(target), out _));
            Zone.GetEntityCell(target).IsVisible = true;
            Assert.False(ThrowingRules.Prepare(Actor, Carry("HealingTonic"), Zone, target, Zone.GetEntityCell(target), out _));
            Assert.Zero(ThrowingRules.CooldownRemaining(Actor, Measured));
        }
        [Test] public void DrivingRequiresOwnedHeavyPhysicalSourceAndPositiveRetainedContact()
        {
            Grant(Driving); var target = Target(); var item = Carry("Cudgel");
            Assert.False(ThrowingRules.ValidateMode(Actor, Carry("Dagger"), Zone, target, Zone.GetEntityCell(target), ThrowMode.Driving, false, out _));
            Assert.True(ThrowingRules.ValidateMode(Actor, item, Zone, target, Zone.GetEntityCell(target), ThrowMode.Driving, false, out _));
            var contact = Zone.GetEntityCell(target); var shot = Capture(item, target, ThrowMode.Driving); ThrowingRules.CommitLaunch(shot);
            ThrowingRules.ApplyDriving(shot, Actor, target, Zone, contact, 0); Assert.AreEqual(14, Zone.GetEntityCell(target).X);
            ThrowingRules.ApplyDriving(shot, Actor, target, Zone, contact, 1); Assert.AreEqual(15, Zone.GetEntityCell(target).X);
            ThrowingRules.ApplyDriving(shot, Actor, target, Zone, contact, 1); Assert.AreEqual(15, Zone.GetEntityCell(target).X);
            Assert.AreEqual(12, ThrowingRules.CooldownRemaining(Actor, Driving));
        }
        [Test] public void FocusedAuthorizationCannotBeForgedOrUsedWithGas()
        {
            var target = Target(); var tonic = Carry("HealingTonic");
            Assert.False(ThrowingRules.ValidateMode(Actor, tonic, Zone, target, Zone.GetEntityCell(target), ThrowMode.Ordinary, true, out _));
            Grant("Throwing_FocusedSplash");
            Assert.True(ThrowingRules.ValidateMode(Actor, tonic, Zone, target, Zone.GetEntityCell(target), ThrowMode.Ordinary, true, out _));
            Assert.False(ThrowingRules.ValidateMode(Actor, Carry("PoisonGasGrenade"), Zone, target, Zone.GetEntityCell(target), ThrowMode.Ordinary, true, out _));
        }
        Entity MarkedItem()
        {
            var item = Carry("Dagger"); var shot = Capture(item, Target()); ThrowingRules.CommitLaunch(shot);
            Pack.RemoveObject(item); Assert.True(Zone.AddEntity(item, 11, 10)); ThrowingRules.RecordLanded(shot, Actor, item, Zone); return item;
        }
        [Test] public void RecoveryMovesOneCellAndSecuresExactUnitWithoutEquipping()
        {
            Grant(Recovery); var item = MarkedItem(); var result = ThrowingRules.Recover(Actor, item, Zone);
            Assert.True(result.Committed); Assert.True(result.Collected); Assert.AreEqual(11, Zone.GetEntityCell(Actor).X);
            Assert.True(Pack.Objects.Contains(item)); Assert.Null(item.GetPart<PhysicsPart>().Equipped);
            Assert.False(ThrowingRules.CanRecover(Actor, item, Zone, out _));
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Recovery));
        }
        [Test] public void VetoedRecoveryIsFreeButPostMovePickupVetoIsPaid()
        {
            Grant(Recovery); var item = MarkedItem(); var veto = new RecoveryVeto { Event = "BeforeMove" }; Actor.AddPart(veto);
            Assert.False(ThrowingRules.Recover(Actor, item, Zone).Committed); Assert.AreEqual(10, Zone.GetEntityCell(Actor).X);
            Assert.Zero(ThrowingRules.CooldownRemaining(Actor, Recovery)); veto.Event = "BeforePickup";
            var result = ThrowingRules.Recover(Actor, item, Zone);
            Assert.True(result.Committed); Assert.False(result.Collected); Assert.AreEqual(11, Zone.GetEntityCell(Actor).X);
            Assert.AreSame(item, Zone.GetCell(11, 10).Objects.Single(e => e == item));
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Recovery));
        }
        [Test] public void ForeignMarkAndChangedQuantityDoNotPermitRecovery()
        {
            Grant(Recovery); var item = MarkedItem(); var mark = item.GetPart<ThrownRecoveryPart>();
            var id = mark.ThrowerId; mark.ThrowerId = "someone-else"; Assert.False(ThrowingRules.CanRecover(Actor, item, Zone, out _));
            mark.ThrowerId = id; item.GetPart<StackerPart>().StackCount = 2; Assert.False(ThrowingRules.CanRecover(Actor, item, Zone, out _));
            Assert.AreEqual(10, Zone.GetEntityCell(Actor).X);
        }
        [Test] public void RemovedSkillCooldownDebtSurvivesSaveAndRelearning()
        {
            Grant(Measured); Prepare(Carry("Dagger"), Target()); Skills.RemoveSkill(Skills.GetSkill(Measured));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(loaded, Measured));
            Assert.True(loaded.GetPart<SkillsPart>().AddSkill(Measured));
            Assert.False(ThrowingRules.TryBeginSelection(loaded, "CommandMeasuredThrow", out _));
            Assert.False(loaded.GetPart<ThrowingStatePart>().Prepared);
        }
        [TestCase("carried")] [TestCase("equipped")] [TestCase("stack")]
        public void ActualPreparedThrowSurvivesItsOwnExtractionAndLeavesOneMarkedUnit(string source)
        {
            Grant(Measured); var item = Carry("Dagger"); var target = Target();
            if (source == "stack") item.GetPart<StackerPart>().StackCount = 3;
            if (source == "equipped") Assert.True(Pack.Equip(item, "Hand"));
            Prepare(item, target); ThrowingRules.OnOwnerTurnEnded(Actor, Zone);
            var before = target.GetStatValue("Hitpoints");
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(item, 14, 10, new System.Random(123)), Actor, Zone);
            Assert.True(result.ActionCommitted, result.ErrorMessage); Assert.True(result.Success, result.ErrorMessage);
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.Less(target.GetStatValue("Hitpoints"), before);
            // Loose item rest permits ordinary creature co-occupancy; movement
            // blocking by the surviving owner is not structural cover.
            Assert.AreSame(target, Zone.GetCell(14, 10).Objects.Single(e => e == target));
            var landed = Zone.GetCell(14, 10).Objects.Single(e => e.GetPart<ThrownRecoveryPart>()?.ThrowerId == Actor.ID);
            Assert.AreEqual(1, landed.GetPart<StackerPart>().StackCount);
            if (source == "stack") { Assert.AreNotSame(item, landed); Assert.AreEqual(2, item.GetPart<StackerPart>().StackCount); }
            else Assert.AreSame(item, landed);
        }
        [Test] public void ReversibleUnequipFaultKeepsThePaidPreparationAndOriginalSource()
        {
            Grant(Measured); var item = Carry("Dagger"); var target = Target(); Assert.True(Pack.Equip(item, "Hand"));
            Prepare(item, target); ThrowingRules.OnOwnerTurnEnded(Actor, Zone);
            Actor.AddPart(new RecoveryVeto { Event = "BeforeUnequip" });
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(item, 14, 10, new System.Random(123)), Actor, Zone);
            Assert.False(result.ActionCommitted); Assert.False(result.Success);
            Assert.True(Actor.GetPart<ThrowingStatePart>().Prepared); Assert.AreSame(Actor, item.GetPart<PhysicsPart>().Equipped);
        }
        [Test] public void ArbitraryEndTurnBroadcastDoesNotPayDownPersistentDebt()
        {
            Grant(Measured); Prepare(Carry("Dagger"), Target());
            Actor.FireEvent("EndTurn");
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Measured));
            Assert.AreEqual(6, Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(Skills.GetSkill(Measured).ActivatedAbilityID).CooldownRemaining);
            Assert.True(Actor.GetPart<ThrowingStatePart>().Prepared);
        }
        [Test] public void StepEntryExceptionIsPaidAndCannotRestoreTheOriginalPosition()
        {
            Grant(Recovery); var item = MarkedItem(); Actor.AddPart(new RecoveryThrowAfterMove());
            var result = ThrowingRules.Recover(Actor, item, Zone);
            Assert.True(result.Committed); Assert.False(result.Collected);
            Assert.AreEqual(11, Zone.GetEntityCell(Actor).X); Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, Recovery));
            Assert.AreSame(item, Zone.GetCell(11, 10).Objects.Single(e => e == item));
            Assert.False(ActionResolutionGuard.IsUnstable, "Direct gameplay command does not leave native scheduling debt.");
        }
        public sealed class RecoveryThrowAfterMove : Part
        { public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterMove") throw new InvalidOperationException("entry observer"); return true; } }
        public sealed class RecoveryVeto : Part
        { public string Event; public override bool HandleEvent(GameEvent e) => e.ID != Event; }
        [Serializable] sealed class ThrowPenetrationValues
        { public int totalBonus; public int maxBonus; }
        sealed class FixedRandom : System.Random
        { readonly int value; public int Calls; public FixedRandom(int value) { this.value = value; } public override int Next(int min, int max) { Calls++; return Math.Max(min, Math.Min(max - 1, value)); } }
    }
}
