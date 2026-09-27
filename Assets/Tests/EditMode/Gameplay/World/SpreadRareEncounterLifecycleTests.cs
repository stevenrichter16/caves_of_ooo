using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Exact authored kit and saved graph witnesses; not an ordinary-play balance claim.
    public sealed class SpreadRareEncounterLifecycleTests
    {
        DensityLootTestScope scope; Zone zone; Entity leader, mate, player;
        [SetUp] public void Setup()
        {
            scope=new DensityLootTestScope(); FactionManager.Initialize();
            zone=new Zone("Overworld.5.15.0");
            leader=scope.Factory.CreateEntity(SpreadRareEncounterPlan.PairLeader);
            mate=scope.Factory.CreateEntity(SpreadRareEncounterPlan.PairMate);
            player=scope.Factory.CreateEntity("Player");
            zone.AddEntity(leader,10,10);zone.AddEntity(mate,10,12);zone.AddEntity(player,12,10);
            leader.GetPart<BrainPart>().CurrentZone=mate.GetPart<BrainPart>().CurrentZone=zone;
        }
        [TearDown] public void Teardown(){scope.Dispose();}
        ActivatedAbility Ability => leader.GetPart<ActivatedAbilitiesPart>().AbilityList.Single();
        bool Lunge()=>leader.GetPart<CombatTacticsPart>().TryUseAbility(player,zone,new DensityCombatFixture.LowRandom());
        [Test] public void ExactAuthoredSwordExecutesReachWithoutMovementAndKeepsRealCooldown()
        {
            Assert.AreEqual(35,leader.GetPart<CombatTacticsPart>().AbilityChance);
            Assert.IsTrue(Lunge());Assert.AreEqual((10,10),zone.GetEntityPosition(leader));
            Assert.AreEqual(25,Ability.CooldownRemaining);Assert.IsFalse(Lunge());
            Assert.IsTrue(MessageLog.GetAllEntries().Any(e=>e.Text.Contains("(Lunge)")),"A real swing is recorded even when its ordinary hit roll misses.");
        }
        [TestCase("disarm")] [TestCase("wall")] [TestCase("ally")] [TestCase("range")]
        public void ActualChangedKitOrRayCannotSpendItsReach(string change)
        {
            if(change=="disarm")Assert.IsTrue(InventorySystem.UnequipItem(leader,DensityLootTestScope.Gear(leader).Single(e=>e.BlueprintName=="ShortSword")));
            if(change=="wall")zone.AddEntity(scope.Factory.CreateEntity("Hedge"),11,10);
            if(change=="ally")zone.MoveEntity(mate,11,10);
            if(change=="range")zone.MoveEntity(player,13,10);
            Assert.IsFalse(Lunge());Assert.AreEqual(0,Ability.CooldownRemaining);
            Assert.AreEqual(40,player.GetStatValue("Hitpoints"));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualPairAssistsOnlyAcrossVisibleSpace(bool blocked)
        {
            if(blocked)zone.AddEntity(scope.Factory.CreateEntity("StoneWall"),10,11);
            Assert.AreEqual(!blocked,AIHelpers.HasLineOfSight(zone,10,12,10,10));
            leader.GetPart<BrainPart>().SetPersonallyHostile(player);
            Assert.AreEqual(!blocked,mate.GetPart<BrainPart>().IsPersonallyHostileTo(player));
        }
        [Test] public void RealDeathSpillsSameGearAndPickedCapSurvivesChangedSaveAndReturn()
        {
            var cap=DensityLootTestScope.Gear(leader).Single(e=>e.BlueprintName=="LeatherCap");
            var sword=DensityLootTestScope.Gear(leader).Single(e=>e.BlueprintName=="ShortSword");
            string capID=cap.ID,swordID=sword.ID;
            CombatSystem.ApplyDamage(leader,leader.GetStatValue("Hitpoints"),player,zone);
            Assert.IsTrue(CombatSystem.IsDeathHandled(leader));
            Assert.AreSame(cap,zone.GetReadOnlyEntities().Single(e=>e.ID==capID));
            Assert.AreSame(sword,zone.GetReadOnlyEntities().Single(e=>e.ID==swordID));
            Assert.IsNull(cap.GetPart<PhysicsPart>().Equipped);
            zone.MoveEntity(player,10,10);
            Assert.IsTrue(InventorySystem.Pickup(player,cap,zone));
            Assert.IsTrue(EquipmentComparisonService.TryDescribe(player,cap,out var description,out _));
            StringAssert.Contains("AV: +1",description);
            Assert.IsTrue(InventorySystem.Equip(player,cap));
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,1);manager.SetActiveZone(zone);
            var turns=new TurnManager();turns.RestoreSavedState(3,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
            var state=GameSessionState.Capture("rare-life","rare-life",manager,turns,player);
            var saved=HotbarSaveFixture.RoundTrip(state);
            Assert.IsTrue(InventorySystem.Drop(player,cap,zone)); // current state diverges after saved snapshot
            Assert.IsNull(cap.GetPart<PhysicsPart>().Equipped);
            var restoredCap=DensityLootTestScope.Gear(saved.Player).Single(e=>e.ID==capID);
            Assert.AreNotSame(cap,restoredCap);Assert.AreSame(saved.Player,restoredCap.GetPart<PhysicsPart>().Equipped);
            var restoredZone=saved.ZoneManager.GetZone(zone.ZoneID);
            Assert.IsFalse(restoredZone.GetReadOnlyEntities().Any(e=>e.BlueprintName==SpreadRareEncounterPlan.PairLeader));
            Assert.AreEqual(1,restoredZone.GetReadOnlyEntities().Count(e=>e.ID==swordID));
            Assert.AreSame(restoredZone,saved.ZoneManager.GetZone(zone.ZoneID));
            var again=HotbarSaveFixture.RoundTrip(saved);
            Assert.AreEqual(capID,DensityLootTestScope.Gear(again.Player).Single(e=>e.BlueprintName=="LeatherCap").ID);
            Assert.IsFalse(again.ZoneManager.GetZone(zone.ZoneID).GetReadOnlyEntities().Any(e=>e.BlueprintName==SpreadRareEncounterPlan.PairLeader));
        }
        [Test] public void UsedLungeCooldownAndSkillIdentitySurviveActualSave()
        {
            Assert.IsTrue(Lunge());var skillID=leader.GetPart<SkillsPart>().SkillList.Single().ActivatedAbilityID;
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,1);manager.SetActiveZone(zone);
            var turns=new TurnManager();turns.RestoreSavedState(3,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
            var saved=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("rare-cooldown","rare",manager,turns,player));
            var owner=saved.ZoneManager.GetZone(zone.ZoneID).GetReadOnlyEntities().Single(e=>e.ID==leader.ID);
            Assert.AreEqual(skillID,owner.GetPart<SkillsPart>().SkillList.Single().ActivatedAbilityID);
            Assert.AreEqual(25,owner.GetPart<ActivatedAbilitiesPart>().AbilityList.Single().CooldownRemaining);
            Assert.IsFalse(owner.GetPart<CombatTacticsPart>().TryUseAbility(saved.Player,saved.ZoneManager.GetZone(zone.ZoneID),new DensityCombatFixture.LowRandom()));
        }
    }
}
