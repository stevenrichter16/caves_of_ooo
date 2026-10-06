using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F=CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondCombatAdversarialTests
    {
        FiftySecondCombatFixture f;
        [SetUp] public void Setup()=>f=new FiftySecondCombatFixture();
        [TearDown] public void Teardown()=>f.Dispose();
        [Test] public void CausticDrawDoesNotConsumeAReplacementPrimerAddedDuringDamage()
        {
            var s=FiftySecondCombatFixture.Skill("Corrosion_CausticDraw");var a=f.Actor(s);var v=f.Target(12,10);
            var old=new AcidicEffect(.5f);v.ApplyEffect(old,a,f.Zone);AcidicEffect replacement=null;
            v.GetPart<MultiCellAbilityProbePart>().OnDamage=()=>{v.RemoveEffect<AcidicEffect>();replacement=new AcidicEffect(.8f);v.ApplyEffect(replacement,a,f.Zone);};
            Assert.True(f.Cast(a,s));Assert.AreSame(replacement,v.GetEffect<AcidicEffect>());Assert.AreEqual(992,v.GetStatValue("Hitpoints"));
        }
        [Test] public void FullyResistedDrawStillSpendsPrimerAndRegisteredCooldown()
        {
            var s=FiftySecondCombatFixture.Skill("Corrosion_CausticDraw");var a=f.Actor(s);var v=f.Target(12,10);
            v.Statistics["AcidResistance"]=new Stat { Owner=v,Name="AcidResistance",BaseValue=100,Min=0,Max=100 };
            v.ApplyEffect(new AcidicEffect(1),a,f.Zone);Assert.True(f.Cast(a,s));Assert.AreEqual(1000,v.GetStatValue("Hitpoints"));Assert.False(v.HasEffect<AcidicEffect>());Assert.AreEqual(20,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [Test] public void CrosswiseWallDoesNotBuryAnOffAnchorBody()
        {
            var s=new Cryomancy_GlacialWall();var a=f.Actor(s);var v=F.Owner("0,0;-1,0",true);F.Place(f.Zone,v,13,11);
            Assert.True(f.Cast(a,s));Assert.False(f.Zone.GetCell(12,11).Occupants.Any(e=>e.BlueprintName=="IceWall"));Assert.AreEqual((13,11),f.Zone.GetEntityPosition(v));
        }
        [Test] public void SavedWallKeepsExactRemainingTimeAndCreatesOnlyOneAftermath()
        {
            var ice=f.Factory.CreateEntity("IceWall");Assert.True(f.Zone.AddEntity(ice,12,10));f.Tick(ice,6);
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(ice);Assert.True(f.Zone.RemoveEntity(ice));Assert.True(f.Zone.AddEntity(loaded,12,10));
            Assert.AreEqual(2,loaded.GetPart<LifespanPart>().TurnsRemaining);f.Tick(loaded);Assert.NotNull(f.Zone.GetEntityCell(loaded));f.Tick(loaded);
            Assert.Null(f.Zone.GetEntityCell(loaded));Assert.AreEqual(4,f.Zone.TileState.CoatingTurns(12,10,"water"));
            f.Zone.TileState.Tick();f.Tick(loaded);Assert.AreEqual(3,f.Zone.TileState.CoatingTurns(12,10,"water"),"Detached expired owner cannot refresh its aftermath.");
        }
        [Test] public void GuardDoesNotSpendOnZeroDamageOrEnvironmentalDamage()
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_EnGarde");var a=f.Actor(s);var enemy=f.Target();Assert.True(f.Cast(a,s));
            var zero=new Damage(0);zero.AddAttribute("Melee");CombatSystem.ApplyDamage(a,zero,enemy,f.Zone);
            CombatSystem.ApplyDamage(a,new Damage(3),null,f.Zone);Assert.NotNull(FiftySecondCombatFixture.Effect(a,"EnGardeEffect"));
            var melee=new Damage(10);melee.AddAttribute("Melee");CombatSystem.ApplyDamage(a,melee,enemy,f.Zone);Assert.AreEqual(992,a.GetStatValue("Hitpoints"));
        }
        [Test] public void FollowThroughUsesSelectedWideBodyContactInsteadOfRemoteAnchor()
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_FollowThrough");var a=f.Actor(s);var v=F.Owner("0,0;-1,0",true);v.GetStat("Hitpoints").BaseValue=1;F.Place(f.Zone,v,12,10);
            Assert.True(f.Cast(a,s,f.Zone.GetCell(11,10)));Assert.AreEqual((11,10),f.Zone.GetEntityPosition(a));Assert.Null(f.Zone.GetEntityCell(v));
        }
        [Test] public void FollowThroughDoesNotSnapBackAfterAnAttackCallbackMovesItsCaster()
        {
            var s=new LongBlades_FollowThrough();var a=f.Actor(s);var v=f.Target(hp:1);
            v.GetPart<MultiCellAbilityProbePart>().OnDamage=()=>Assert.True(f.Zone.MoveEntity(a,20,10));
            Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(v)));Assert.True(CombatSystem.IsDeathHandled(v));
            Assert.AreEqual((20,10),f.Zone.GetEntityPosition(a),"The paid strike does not turn the follow-up into a remote teleport.");
            Assert.AreEqual(12,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [Test] public void SlamDamagesWideCollisionOwnerOnlyOnce()
        {
            var s=new Cudgel_Slam();var a=f.Actor(s,"Bludgeoning Cudgel");var primary=f.Target();var wall=F.Owner("0,0;0,1",true);F.Place(f.Zone,wall,12,10);
            Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(primary)));Assert.AreEqual(1,wall.GetPart<MultiCellAbilityProbePart>().DamageCalls);
        }
        [Test] public void SpellCastPreservesVaultPoiseUntilAnAdjacentMeleeRoll()
        {
            var vault=new Acrobatics_Vault();var a=f.Actor(vault);Assert.True(f.Cast(a,vault));var spell=new Pyromancy_EmberSpit();FiftySecondCombatFixture.Learn(a,spell);var enemy=f.Target(14,10);
            Assert.True(f.Cast(a,spell));Assert.NotNull(FiftySecondCombatFixture.Effect(a,"VaultPoiseEffect"));
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(a);Assert.NotNull(FiftySecondCombatFixture.Effect(saved,"VaultPoiseEffect"));
            Assert.Less(enemy.GetStatValue("Hitpoints"),1000);
        }
        [TestCase("floor")] [TestCase("foreign")]
        public void StatusMedicineRequiresActualCarriedOwnership(string source)
        {
            using(var m=new FieldMedicineFixture())
            {
                var a=m.Actor(hp:20);var enemy=m.Threat(a);var bottle=m.Supply(a,"Antidote");FiftySecondCombatFixture.Set(a.GetPart<FieldMedicinePart>(),"CureBlueprints","Antidote");
                a.ApplyEffect(new PoisonedEffect(6,"1d1"),enemy,m.Zone);a.GetPart<InventoryPart>().RemoveObject(bottle);
                if(source=="floor")m.Zone.AddEntity(bottle,10,10);else enemy.GetPart<InventoryPart>().AddObject(bottle);
                Assert.False(m.Use(a,enemy));Assert.True(a.HasEffect<PoisonedEffect>());Assert.AreEqual(1,bottle.GetPart<StackerPart>().StackCount);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void RecoverySavePreservesTheExactGroundOrCarriedWeapon(bool alreadyPickedUp)
        {
            var disarm=new Cudgel_Disarm();var a=f.Actor(disarm,"Bludgeoning Cudgel");var victim=f.Actor(x:11);
            victim.AddPart(new BrainPart { CurrentZone=f.Zone,Rng=f.Rng,FleeThreshold=0 });victim.AddPart(new WeaponRecoveryPart());
            var weapon=SkillCombatHelpers.FindEquippedWeaponOfClass(victim,"LongBlades").ParentEntity;
            Assert.True(f.Cast(a,disarm,f.Zone.GetEntityCell(victim)));if(alreadyPickedUp)f.Act(victim,a);
            var manager=new OverworldZoneManager(null,64);manager.ReplaceLoadedState(new System.Collections.Generic.Dictionary<string,Zone>{{f.Zone.ZoneID,f.Zone}},f.Zone.ZoneID,new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<ZoneConnection>>());
            var turns=new TurnManager();turns.AddEntity(a);turns.AddEntity(victim);
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("recovery-save","controlled recovery",manager,turns,a));
            var zone=loaded.ZoneManager.ActiveZone;var restored=zone.GetAllEntities().Single(e=>e.ID==victim.ID);
            var remembered=restored.GetPart<WeaponRecoveryPart>().RecoveryWeapon;
            Assert.NotNull(remembered);Assert.AreNotSame(weapon,remembered);Assert.AreEqual(weapon.ID,remembered.ID);
            Assert.AreEqual(alreadyPickedUp,restored.GetPart<InventoryPart>().Contains(remembered));
            Assert.AreEqual(!alreadyPickedUp,zone.GetEntityCell(remembered)!=null);
            var brain=restored.GetPart<BrainPart>();brain.CurrentZone=zone;brain.Rng=f.Rng;
            Assert.True(restored.GetPart<WeaponRecoveryPart>().TryRecover(zone));
            if(!alreadyPickedUp){Assert.False(InventorySystem.IsEquipped(restored,remembered));Assert.True(restored.GetPart<WeaponRecoveryPart>().TryRecover(zone));}
            Assert.True(InventorySystem.IsEquipped(restored,remembered));Assert.Null(restored.GetPart<WeaponRecoveryPart>().RecoveryWeapon);
            Assert.Null(zone.GetEntityCell(remembered));
        }
        [Test] public void OrdinaryPickupStillAutomaticallyEquipsAuthoredWeapons()
        {
            var actor=f.Actor();var old=SkillCombatHelpers.FindEquippedWeaponOfClass(actor,"LongBlades").ParentEntity;
            Assert.True(InventorySystem.UnequipItem(actor,old));Assert.True(actor.GetPart<InventoryPart>().RemoveObject(old));
            var sword=f.Factory.CreateEntity("LongSword");Assert.True(f.Zone.AddEntity(sword,10,10));
            Assert.True(InventorySystem.Pickup(actor,sword,f.Zone));Assert.True(InventorySystem.IsEquipped(actor,sword));
            Assert.Null(f.Zone.GetEntityCell(sword));
        }
        [Test] public void RootedRangeKeeperCannotRetreatThroughItsMovementGate()
        {
            var a=f.Npc();var enemy=f.Hostile(a,11);FiftySecondCombatFixture.Set(a.GetPart<CombatTacticsPart>(),"PreferredRange",3);
            foreach(var ability in a.GetPart<ActivatedAbilitiesPart>().AbilityList)ability.CooldownRemaining=5;
            a.ApplyEffect(new RootedEffect(),enemy,f.Zone);f.Act(a,enemy);Assert.AreEqual((10,10),f.Zone.GetEntityPosition(a));
        }
        [Test] public void StormbinderCannotPrimeThroughItsOwnCompanion()
        {
            var a=f.Factory.CreateEntity("MarlbackStormbinder");Assert.NotNull(a);Assert.True(f.Zone.AddEntity(a,10,10));a.GetPart<BrainPart>().CurrentZone=f.Zone;a.GetPart<BrainPart>().Rng=f.Rng;
            var enemy=f.Hostile(a,13);var ally=f.Npc(11,10,"");Assert.False(a.GetPart<CombatTacticsPart>().TryUseAbility(enemy,f.Zone,f.Rng));
            Assert.False(enemy.HasEffect<WetEffect>());Assert.False(ally.HasEffect<WetEffect>());Assert.True(a.GetPart<ActivatedAbilitiesPart>().AbilityList.All(x=>x.CooldownRemaining==0));
        }
    }
}
