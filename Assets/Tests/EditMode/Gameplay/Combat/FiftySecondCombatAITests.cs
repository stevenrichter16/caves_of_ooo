using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondCombatAITests
    {
        FiftySecondCombatFixture f;
        [SetUp] public void Setup()=>f=new FiftySecondCombatFixture();
        [TearDown] public void Teardown()=>f.Dispose();
        static void Cooling(Entity e)
        { foreach(var a in e.GetPart<ActivatedAbilitiesPart>().AbilityList)a.CooldownRemaining=5; }
        [TestCase(true,11,9)] [TestCase(false,11,10)] [TestCase(true,13,10)] [TestCase(false,13,11)]
        public void RangeOptInChangesCoolingCasterPositionWithoutGrantingAnAttack(bool opted,int enemyX,int expectedX)
        {
            var a=f.Npc();var target=f.Hostile(a,enemyX);Cooling(a);
            if(opted)FiftySecondCombatFixture.Set(a.GetPart<CombatTacticsPart>(),"PreferredRange",3);
            f.Act(a,target);Assert.AreEqual((expectedX,10),f.Zone.GetEntityPosition(a));
            if(opted)Assert.AreEqual(1000,target.GetStatValue("Hitpoints"),"Holding or retreat replaces the entire action.");
            Assert.True(a.GetPart<ActivatedAbilitiesPart>().AbilityList.All(x=>x.CooldownRemaining==5));
        }
        [Test] public void ReadyOptedCasterStillFiresItsRealAbility()
        {
            var a=f.Npc();var target=f.Hostile(a);FiftySecondCombatFixture.Set(a.GetPart<CombatTacticsPart>(),"PreferredRange",3);
            f.Act(a,target);Assert.Less(target.GetStatValue("Hitpoints"),1000);Assert.AreEqual((10,10),f.Zone.GetEntityPosition(a));
            Assert.AreEqual(8,a.GetPart<ActivatedAbilitiesPart>().AbilityList.Single().CooldownRemaining);
        }
        [Test] public void OptedCasterStepsAroundFriendlyBlockerThenFiresOnNextAction()
        {
            var a=f.Npc();var target=f.Hostile(a,12);var ally=f.Npc(11,10,"");
            FiftySecondCombatFixture.Set(a.GetPart<CombatTacticsPart>(),"RepositionForShot",true);
            f.Act(a,target);Assert.AreNotEqual((10,10),f.Zone.GetEntityPosition(a));Assert.AreEqual(1000,target.GetStatValue("Hitpoints"));
            Assert.AreEqual(500,ally.GetStatValue("Hitpoints"));Assert.AreEqual(0,a.GetPart<ActivatedAbilitiesPart>().AbilityList.Single().CooldownRemaining);
            f.Act(a,target);Assert.Less(target.GetStatValue("Hitpoints"),1000);Assert.AreEqual(500,ally.GetStatValue("Hitpoints"));
        }
        [Test] public void NoLegalSidestepNeverTeleportsOrShootsThroughAlly()
        {
            var a=f.Npc();var target=f.Hostile(a,12);var ally=f.Npc(11,10,"");
            FiftySecondCombatFixture.Set(a.GetPart<CombatTacticsPart>(),"RepositionForShot",true);
            foreach(var p in new[]{(9,9),(10,9),(11,9),(9,10),(9,11),(10,11),(11,11)})f.Wall(p.Item1,p.Item2);
            f.Act(a,target);Assert.AreEqual((10,10),f.Zone.GetEntityPosition(a));Assert.AreEqual(1000,target.GetStatValue("Hitpoints"));Assert.AreEqual(500,ally.GetStatValue("Hitpoints"));
        }
        [Test] public void DisarmedOptedNpcSpendsSeparateActionsToPickupAndEquipExactOwner()
        {
            var disarm=new Cudgel_Disarm();var a=f.Actor(disarm,"Bludgeoning Cudgel");var victim=f.Actor(x:11);
            victim.AddPart(new BrainPart { CurrentZone=f.Zone,Rng=f.Rng,FleeThreshold=0 });victim.AddPart(FiftySecondCombatFixture.Part("WeaponRecoveryPart"));
            var weapon=SkillCombatHelpers.FindEquippedWeaponOfClass(victim,"LongBlades").ParentEntity;
            Assert.True(f.Cast(a,disarm,f.Zone.GetEntityCell(victim)));Assert.AreSame(f.Zone.GetCell(11,10),f.Zone.GetEntityCell(weapon));
            f.Act(victim,a);Assert.True(victim.GetPart<InventoryPart>().Objects.Contains(weapon));Assert.False(InventorySystem.IsEquipped(victim,weapon));Assert.Null(f.Zone.GetEntityCell(weapon));
            f.Act(victim,a);Assert.True(InventorySystem.IsEquipped(victim,weapon));Assert.AreEqual(1000,a.GetStatValue("Hitpoints"));
        }
        [TestCase(true)] [TestCase(false)] public void StolenOrUnoptedDisarmedWeaponCannotBeRecreated(bool opted)
        {
            var s=new Cudgel_Disarm();var a=f.Actor(s,"Bludgeoning Cudgel");var victim=f.Actor(x:11);
            victim.AddPart(new BrainPart { CurrentZone=f.Zone,Rng=f.Rng,FleeThreshold=0 });if(opted)victim.AddPart(FiftySecondCombatFixture.Part("WeaponRecoveryPart"));
            var w=SkillCombatHelpers.FindEquippedWeaponOfClass(victim,"LongBlades").ParentEntity;Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(victim)));
            if(opted){Assert.True(f.Zone.RemoveEntity(w));Assert.True(a.GetPart<InventoryPart>().AddObject(w));}
            f.Act(victim,a);f.Act(victim,a);Assert.False(InventorySystem.IsEquipped(victim,w));
            Assert.AreEqual(opted?1:0,a.GetPart<InventoryPart>().Objects.Count(e=>ReferenceEquals(e,w)));
            Assert.AreEqual(opted?null:f.Zone.GetCell(11,10),f.Zone.GetEntityCell(w));
        }
        [TestCase(true)] [TestCase(false)] public void MedicUsesAnActualAntidoteOnlyForPoison(bool poisoned)
        {
            using(var m=new FieldMedicineFixture())
            {
                var a=m.Actor(hp:20);var target=m.Threat(a);var bottle=m.Supply(a,"Antidote");
                FiftySecondCombatFixture.Set(a.GetPart<FieldMedicinePart>(),"CureBlueprints","Antidote;BurnSalve");
                if(poisoned)a.ApplyEffect(new PoisonedEffect(6,"1d1"),target,m.Zone);
                Assert.AreEqual(poisoned,m.Use(a,target));Assert.False(a.HasEffect<PoisonedEffect>());
                Assert.AreEqual(!poisoned,a.GetPart<InventoryPart>().Objects.Contains(bottle));Assert.AreEqual(20,a.GetStatValue("Hitpoints"));
            }
        }
        [Test] public void MedicCureReplacesFightActionAndRetainsItsSeparateHealingBottle()
        {
            using(var m=new FieldMedicineFixture())
            {
                var a=m.Actor(hp:7);var target=m.Threat(a);var antidote=m.Supply(a,"Antidote");var heal=m.Supply(a);
                FiftySecondCombatFixture.Set(a.GetPart<FieldMedicinePart>(),"CureBlueprints","Antidote");a.ApplyEffect(new PoisonedByGasEffect { Duration=5,DamagePerTurn=1 },target,m.Zone);
                m.Act(a,target,"kill");Assert.False(a.HasEffect<PoisonedByGasEffect>());Assert.AreEqual(6,a.GetStatValue("Hitpoints"),"The existing start-of-turn poison tick occurs before the paid cure decision.");
                Assert.AreEqual(20,target.GetStatValue("Hitpoints"));Assert.True(a.GetPart<InventoryPart>().Objects.Contains(heal));Assert.False(a.GetPart<InventoryPart>().Objects.Contains(antidote));
            }
        }
        [Test] public void StormbinderUsesTwoRealActionsToSoakThenShock()
        {
            var a=f.Factory.CreateEntity("MarlbackStormbinder");Assert.NotNull(a,"New enemy must be actual authored content.");Assert.True(f.Zone.AddEntity(a,10,10));
            var brain=a.GetPart<BrainPart>();brain.CurrentZone=f.Zone;brain.Rng=f.Rng;brain.FleeThreshold=0;
            var target=f.Hostile(a);Assert.AreEqual(2,a.GetPart<SkillsPart>().SkillList.Count);
            f.Act(a,target);Assert.True(target.HasEffect<WetEffect>());Assert.False(target.HasEffect<ElectrifiedEffect>());
            var quench=a.GetPart<ActivatedAbilitiesPart>().AbilityList.Single(x=>x.Command=="CommandQuench");var arc=a.GetPart<ActivatedAbilitiesPart>().AbilityList.Single(x=>x.Command=="CommandArcBolt");
            Assert.AreEqual(6,quench.CooldownRemaining);Assert.AreEqual(0,arc.CooldownRemaining);int hp=target.GetStatValue("Hitpoints");
            f.Act(a,target);Assert.Less(target.GetStatValue("Hitpoints"),hp);Assert.True(target.HasEffect<ElectrifiedEffect>());Assert.AreEqual(7,arc.CooldownRemaining);
            Assert.AreEqual(1,a.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="SparkRoot"));
        }
        [TestCase(2,false)] [TestCase(3,true)] [TestCase(6,true)]
        public void StormbinderHasAnOrdinaryExclusiveDepthEncounterSource(int depth,bool eligible)
        {
            var table=PopulationTable.UndergroundTier(depth);var rows=table.Entries.Where(e=>e.BlueprintName=="MarlbackStormbinder").ToArray();Assert.AreEqual(eligible?1:0,rows.Length);
            if(!eligible)return;Assert.AreEqual("DepthEncounter",rows[0].EncounterGroup);Assert.AreEqual(1,rows[0].Weight);Assert.AreEqual(1,rows[0].MinCount);Assert.AreEqual(1,rows[0].MaxCount);
            bool found=false;var group=table.Entries.Where(e=>e.EncounterGroup=="DepthEncounter").Select(e=>e.BlueprintName).ToArray();
            for(int seed=0;seed<200;seed++)
            {var roll=table.Roll(new Random(seed));if(!roll.Contains("MarlbackStormbinder"))continue;found=true;Assert.AreEqual(1,roll.Count(group.Contains));break;}
            Assert.True(found,"Actual weighted source must select the singleton, not just declare a row.");
        }
    }
}
