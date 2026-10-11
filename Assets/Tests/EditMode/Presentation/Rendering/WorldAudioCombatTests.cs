using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioCombatTests
    {
        Action<WorldAudioCombatReceipt> previous;
        readonly List<WorldAudioCombatReceipt> receipts=new List<WorldAudioCombatReceipt>();
        [SetUp] public void Setup(){previous=WorldAudioCombat.Callback;WorldAudioCombat.Callback=receipts.Add;receipts.Clear();MessageLog.Clear();}
        [TearDown] public void Cleanup(){WorldAudioCombat.Callback=previous;MessageLog.Clear();}

        [TestCase("Cutting LongBlades",WorldAudioCombatFamily.Cutting)]
        [TestCase("Piercing",WorldAudioCombatFamily.Piercing)]
        [TestCase("Bludgeoning Cudgel",WorldAudioCombatFamily.Blunt)]
        [TestCase("Cudgel",WorldAudioCombatFamily.Blunt)]
        [TestCase("Piercing Cutting Animal",WorldAudioCombatFamily.Natural)]
        [TestCase("Bludgeoning Unarmed",WorldAudioCombatFamily.Natural)]
        [TestCase("Cutting Piercing",WorldAudioCombatFamily.Cutting)]
        [TestCase("NotCutting Animalistic",WorldAudioCombatFamily.Generic)]
        [TestCase("",WorldAudioCombatFamily.Generic)]
        [TestCase(null,WorldAudioCombatFamily.Generic)]
        public void WeaponFamilyUsesExactPhysicalTokens(string attributes,WorldAudioCombatFamily expected)
        {var actor=Fighter("a");var item=Weapon(attributes);Assert.AreEqual(expected,WorldAudioCombat.Classify(actor,item.GetPart<MeleeWeaponPart>()));}

        [Test] public void NaturalTagAloneDoesNotTurnDefaultBanditBladeIntoAnimalAttack()
        {
            var actor=Fighter("a");var blade=NaturalWeaponFactory.Create("BanditBlade");var bite=NaturalWeaponFactory.Create("DefaultBite");
            Assert.True(blade.HasTag("Natural"));Assert.True(bite.HasTag("Natural"));
            Assert.AreEqual(WorldAudioCombatFamily.Cutting,WorldAudioCombat.Classify(actor,blade.GetPart<MeleeWeaponPart>()));
            Assert.AreEqual(WorldAudioCombatFamily.Natural,WorldAudioCombat.Classify(actor,bite.GetPart<MeleeWeaponPart>()));
        }
        [Test] public void NullAndUntypedInnateAttacksAreNaturalButUnknownCarriedObjectIsGeneric()
        {
            var actor=Fighter("a");var innate=new MeleeWeaponPart();actor.AddPart(innate);
            Assert.AreEqual(WorldAudioCombatFamily.Natural,WorldAudioCombat.Classify(actor,null));
            Assert.AreEqual(WorldAudioCombatFamily.Natural,WorldAudioCombat.Classify(actor,innate));
            Assert.AreEqual(WorldAudioCombatFamily.Generic,WorldAudioCombat.Classify(actor,Weapon("").GetPart<MeleeWeaponPart>()));
        }
        [TestCase("Cutting",WorldAudioCue.SwingBlade,WorldAudioCue.HitCutting)]
        [TestCase("Piercing",WorldAudioCue.SwingBlade,WorldAudioCue.HitPiercing)]
        [TestCase("Bludgeoning",WorldAudioCue.SwingBlunt,WorldAudioCue.HitBlunt)]
        [TestCase("Piercing Animal",WorldAudioCue.SwingNatural,WorldAudioCue.HitNatural)]
        public void ActualMeleePublishesSelectedWeaponSwingAndPositiveDamageReceipt(string attributes,WorldAudioCue swing,WorldAudioCue hit)
        {
            var zone=World(out var actor,out var target);actor.AddPart(new MeleeWeaponPart{Attributes=attributes,BaseDamage="1d1",MaxStrengthBonus=0});
            int hp=target.GetStatValue("Hitpoints");Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));
            Assert.Greater(hp-target.GetStatValue("Hitpoints"),0);Assert.AreEqual(2,receipts.Count);
            Assert.AreEqual(swing,receipts[0].Cue);Assert.False(receipts[0].IsImpact);Assert.AreEqual(0,receipts[0].ActualDamage);
            Assert.AreEqual(hit,receipts[1].Cue);Assert.True(receipts[1].IsImpact);Assert.AreEqual(hp-target.GetStatValue("Hitpoints"),receipts[1].ActualDamage);
            Assert.AreSame(actor,receipts[1].Attempt.Attacker);Assert.AreSame(target,receipts[1].Attempt.Defender);Assert.AreSame(zone,receipts[1].Attempt.Zone);
            Assert.AreEqual(new Point(11,10),receipts[1].Attempt.Contact);
        }
        [TestCase("miss")][TestCase("armor")][TestCase("zero-roll")][TestCase("damage-veto")]
        public void ActualNonDamagingAttemptHasSwingButNoInventedImpact(string refusal)
        {
            var zone=World(out var actor,out var target);var weapon=new MeleeWeaponPart{Attributes="Cutting",BaseDamage=refusal=="zero-roll"?"1d1-1":"1d1",MaxStrengthBonus=0};actor.AddPart(weapon);
            if(refusal=="miss")target.SetStatValue("DV",999);if(refusal=="armor")target.GetPart<ArmorPart>().AV=999;
            if(refusal=="damage-veto")target.AddPart(new EventProbe{Event="BeforeTakeDamage",Veto=true});
            Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));
            Assert.AreEqual(1000,target.GetStatValue("Hitpoints"));Assert.AreEqual(1,receipts.Count);Assert.AreEqual(WorldAudioCue.SwingBlade,receipts[0].Cue);
            Assert.False(receipts[0].IsImpact,"No shield/armor clang or damage hit may be inferred from an unsuccessful penetration or veto.");
        }
        [Test] public void WholeAttackVetoDoesNotPublishEvenASwingAndRemovingVetoRestoresIt()
        {
            var zone=World(out var actor,out var target);var probe=new EventProbe{Event="BeforeMeleeAttack",Veto=true};actor.AddPart(probe);
            Assert.False(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));Assert.IsEmpty(receipts);
            probe.Veto=false;Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));Assert.Greater(receipts.Count,0);
        }
        [Test] public void GenericDamageAndVisualAnimationHookDoNotPublishTypedWeaponReceipts()
        {
            var zone=World(out var actor,out var target);CombatSystem.ApplyDamage(target,7,actor,zone);
            EntityVisualHooks.EmitAttack(actor,target,zone);Assert.AreEqual(993,target.GetStatValue("Hitpoints"));Assert.IsEmpty(receipts);
            Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));Assert.AreEqual(2,receipts.Count);
        }
        [TestCase(false)][TestCase(true)]
        public void ActualOffhandChanceControlsWhichWeaponsEmit(bool offhandAllowed)
        {
            var zone=World(out var actor,out var target);var body=new Body();actor.AddPart(body);body.SetBody(AnatomyFactory.CreateHumanoid());
            var hands=body.GetParts().Where(p=>p.Type=="Hand").ToArray();Assert.AreEqual(2,hands.Length);
            hands[0].Primary=true;hands[1].Primary=false;hands[1].DefaultPrimary=false;
            Assert.True(actor.GetPart<InventoryPart>().EquipToBodyPart(Weapon("Cutting"),hands[0]));
            Assert.True(actor.GetPart<InventoryPart>().EquipToBodyPart(Weapon("Bludgeoning"),hands[1]));
            actor.Statistics["MultiWeaponSkillBonus"]=new Stat{Owner=actor,Name="MultiWeaponSkillBonus",BaseValue=offhandAllowed?1000:-1000,Min=-1000,Max=1000};
            Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));
            var swings=receipts.Where(r=>!r.IsImpact).Select(r=>r.Cue).ToArray();
            CollectionAssert.AreEqual(offhandAllowed?new[]{WorldAudioCue.SwingBlade,WorldAudioCue.SwingBlunt}:new[]{WorldAudioCue.SwingBlade},swings);
        }
        [Test] public void FamilyIsCopiedBeforeDamageCallbacksMutateWeaponMetadata()
        {
            var zone=World(out var actor,out var target);var weapon=new MeleeWeaponPart{Attributes="Cutting",BaseDamage="1d1",MaxStrengthBonus=0};actor.AddPart(weapon);
            target.AddPart(new EventProbe{Event="BeforeTakeDamage",Run=()=>weapon.Attributes="Piercing"});
            Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));Assert.AreEqual("Piercing",weapon.Attributes);
            CollectionAssert.AreEqual(new[]{WorldAudioCue.SwingBlade,WorldAudioCue.HitCutting},receipts.Select(r=>r.Cue).ToArray());
        }
        // HP mutation by another listener is not this weapon's committed damage.
        [TestCase(100)][TestCase(-100)][TestCase(-500)]
        public void ImpactCountsOnlyTheFinalDecrementAfterDamageCallbacks(int callbackDelta)
        {
            var zone=World(out var actor,out var target);target.GetStat("Hitpoints").BaseValue=500;
            actor.AddPart(new MeleeWeaponPart{Attributes="Cutting",BaseDamage="1d1",MaxStrengthBonus=0});
            int atCommit=-1;
            target.AddPart(new EventProbe{Event="TakeDamage",Run=()=>{target.GetStat("Hitpoints").BaseValue+=callbackDelta;atCommit=target.GetStat("Hitpoints").BaseValue;}});
            Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new BranchRandom()));Assert.AreEqual(500+callbackDelta,atCommit);
            int actual=Math.Max(0,atCommit-target.GetStat("Hitpoints").BaseValue);
            var impacts=receipts.Where(r=>r.IsImpact).ToArray();
            Assert.AreEqual(actual>0?1:0,impacts.Length,"Earlier HP mutations must not invent or erase this strike's own impact.");
            if(actual>0)Assert.AreEqual(actual,impacts[0].ActualDamage);
        }
        [Test] public void ThrowingAudioObserverCannotUndoDamageOrChangeSimulationRandomness()
        {
            var a=World(out var aa,out var at);var arng=new BranchRandom();WorldAudioCombat.Callback=null;
            Assert.True(CombatSystem.PerformMeleeAttack(aa,at,a,arng));int hp=at.GetStatValue("Hitpoints");
            var b=World(out var ba,out var bt);var brng=new BranchRandom();WorldAudioCombat.Callback=receipt=>throw new InvalidOperationException("optional audio failed");
            Assert.DoesNotThrow(()=>Assert.True(CombatSystem.PerformMeleeAttack(ba,bt,b,brng)));
            Assert.AreEqual(hp,bt.GetStatValue("Hitpoints"));CollectionAssert.AreEqual(arng.Calls,brng.Calls);
        }
        internal static Zone World(out Entity actor,out Entity target)
        {
            var zone=new Zone("combat-audio");actor=Fighter("actor");target=Fighter("target");
            Assert.True(zone.AddEntity(actor,10,10));Assert.True(zone.AddEntity(target,11,10));
            foreach(var cell in zone.Cells){cell.Explored=true;cell.IsVisible=true;}return zone;
        }
        internal static Entity Fighter(string id)
        {
            var e=new Entity{ID=id,BlueprintName="AudioFighter"};e.SetTag("Creature");e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new RenderPart{DisplayName=id});e.AddPart(new ArmorPart());e.AddPart(new InventoryPart());
            foreach(var pair in new[]{("Hitpoints",1000),("Strength",16),("Agility",16),("DV",0)})e.Statistics[pair.Item1]=new Stat{Owner=e,Name=pair.Item1,BaseValue=pair.Item2,Min=0,Max=1000};return e;
        }
        internal static Entity Weapon(string attributes)
        {
            var e=new Entity{BlueprintName="UnknownWeapon"};e.SetTag("Item");e.AddPart(new PhysicsPart{Takeable=true});e.AddPart(new RenderPart{DisplayName="test weapon"});
            e.AddPart(new MeleeWeaponPart{Attributes=attributes,BaseDamage="1d1",MaxStrengthBonus=0});e.AddPart(new EquippablePart{Slot="Hand"});return e;
        }
        internal sealed class EventProbe:Part
        {
            public string Event;public bool Veto;public Action Run;public override string Name=>"CombatAudioProbe";
            public override bool HandleEvent(GameEvent e){if(e.ID!=Event)return true;Run?.Invoke();return !Veto;}
        }
        internal sealed class BranchRandom:Random
        {
            public readonly List<string> Calls=new List<string>();
            public override int Next(int minValue,int maxValue){Calls.Add(minValue+":"+maxValue);return maxValue==21?10:Math.Min(minValue+4,maxValue-1);}
            public override int Next(int maxValue){Calls.Add("max:"+maxValue);return maxValue-1;}
        }
    }
}
