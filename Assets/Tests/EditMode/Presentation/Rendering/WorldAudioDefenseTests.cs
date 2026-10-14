using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioDefenseTests
    {
        FiftySecondCombatFixture fixture;
        Entity attacker,defender;MeleeWeaponPart weapon;
        GameObject root;WorldAudioPlayer audio;WorldAudioEventRouter router;
        readonly List<AudioClip> clips=new List<AudioClip>();
        readonly List<WorldAudioCombatReceipt> receipts=new List<WorldAudioCombatReceipt>();
        Action<WorldAudioCombatReceipt> prior;float priorSfx;bool priorPause;
        [SetUp] public void Setup()
        {
            prior=WorldAudioCombat.Callback;WorldAudioCombat.Callback=receipts.Add;
            priorSfx=WorldAudioSettings.SfxVolume;priorPause=AudioListener.pause;
            WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            fixture=new FiftySecondCombatFixture();attacker=fixture.Actor(x:11);defender=fixture.Actor();
            weapon=BladeDefense.FindLongBlade(attacker);weapon.Attributes="Cutting LongBlades";
            weapon.BaseDamage="1d1+8";weapon.PenBonus=3;
            foreach(var cell in fixture.Zone.Cells){cell.IsVisible=true;cell.Explored=true;}
            root=new GameObject("defense audio tests");var library=new WorldAudioLibrary();
            foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))
            {var clip=AudioClip.Create(cue.ToString(),48000,1,48000,false);clips.Add(clip);library.AddSound(cue,clip,.3f);}
            audio=new WorldAudioPlayer(root.transform,library);router=new WorldAudioEventRouter(audio);
            router.SetWorld(fixture.Zone,attacker,true);
        }
        [TearDown] public void Cleanup()
        {
            router?.Dispose();audio?.Dispose();UnityEngine.Object.DestroyImmediate(root);
            foreach(var clip in clips)UnityEngine.Object.DestroyImmediate(clip);clips.Clear();receipts.Clear();
            fixture?.Dispose();WorldAudioCombat.Callback=prior;WorldAudioSettings.SfxVolume=priorSfx;AudioListener.pause=priorPause;AsciiFxBus.Clear();
        }
        string[] Playing()=>root.GetComponentsInChildren<AudioSource>().Where(s=>s.clip!=null).Select(s=>s.clip.name).ToArray();
        void Prepare(bool guard=true)
        {
            FiftySecondCombatFixture.Learn(defender,new LongBlades_Deflect());
            if(guard)Assert.True(defender.ApplyEffect(new EnGardeEffect(),defender,fixture.Zone));
        }
        MeleeStrikeResult Strike(bool parry=true)=>CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,fixture.Zone,new ComposableBladeTests.Rolls(parry?0:99));
        [TestCase(true,true)] [TestCase(true,false)] [TestCase(false,true)]
        public void ActualDefensePlaysOnlyMatchingContactAndPreservesDamage(bool parry,bool guard)
        {
            Prepare(guard);var result=Strike(parry);
            Assert.AreEqual(parry?MeleeStrikeOutcome.FullParry:MeleeStrikeOutcome.Hit,result.Outcome);
            Assert.AreEqual(parry?0:5,result.ActualDamage);
            var expected=parry?new[]{"SwingBlade","Parry"}:new[]{"SwingBlade","Guard","HitCutting"};
            CollectionAssert.AreEqual(expected,receipts.Select(r=>r.Cue.ToString()).ToArray());
            CollectionAssert.AreEquivalent(expected,Playing());Assert.AreEqual(expected.Length,audio.CombatFocusCount);
            Assert.AreEqual(16,audio.AllocatedSources);
        }
        [TestCase(true)] [TestCase(false)]
        public void MissAndOrdinaryHitNeverPlayDefense(bool miss)
        {
            if(miss){Prepare();weapon.HitBonus=-100;}
            var result=Strike();Assert.AreEqual(miss?0:9,result.ActualDamage);
            CollectionAssert.AreEqual(miss?new[]{"SwingBlade"}:new[]{"SwingBlade","HitCutting"},Playing());
            Assert.AreEqual(miss,defender.HasEffect<EnGardeEffect>());
        }
        [TestCase("cell")] [TestCase("render")] [TestCase("world")] [TestCase("zone")]
        [TestCase("mute")] [TestCase("pause")] [TestCase("disposed")]
        public void UnperceivedOrDisabledDefenseRemainsSilent(string reason)
        {
            Prepare();
            if(reason=="cell")fixture.Zone.GetEntityCell(defender).IsVisible=false;
            if(reason=="render")defender.GetPart<RenderPart>().Visible=false;
            if(reason=="world")router.SetWorld(fixture.Zone,attacker,false);
            if(reason=="zone")router.SetWorld(new Zone("elsewhere"),attacker,true);
            if(reason=="mute")WorldAudioSettings.SfxVolume=0;
            if(reason=="pause")AudioListener.pause=true;
            if(reason=="disposed")router.Dispose();
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,Strike().Outcome);
            Assert.IsEmpty(Playing());Assert.Zero(audio.CombatFocusCount);
        }
        [TestCase("Parry")] [TestCase("Guard")]
        public void GenericSemanticHooksCannotForgeDefenseSounds(string name)
        {
            var cue=(WorldAudioCue)Enum.Parse(typeof(WorldAudioCue),name);
            WorldAudioHooks.Emit(cue,defender,fixture.Zone,10,10);
            Assert.IsEmpty(Playing());Assert.Zero(audio.CombatFocusCount);
            Prepare();Strike(name=="Parry");Assert.Contains(name,Playing());
        }
        [TestCase("Parry","parry_03_dry_edge",.458f)]
        [TestCase("Guard","guard_04_damped_steel",.578f)]
        public void PreparedCatalogLoadsExactSelectedRecording(string name,string resource,float length)
        {
            var cue=(WorldAudioCue)Enum.Parse(typeof(WorldAudioCue),name);
            var entry=WorldAudioLibrary.LoadResources().Sound(cue,0);Assert.NotNull(entry);
            Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Defense/"+resource),entry.Clip);
            Assert.That(entry.Clip.length,Is.EqualTo(length).Within(.002f));
            Assert.AreEqual(1,entry.Clip.channels);Assert.Greater(entry.Gain,0);Assert.LessOrEqual(entry.Gain,.6f);
        }
        [Test] public void ThrowingDefenseObserverCannotCancelParryOrDrawMoreSimulationRandomness()
        {
            Prepare();var rng=new ComposableBladeTests.Rolls();
            WorldAudioCombat.Callback=r=>{if(r.Cue.ToString()=="Parry")throw new InvalidOperationException("presentation only");};
            var result=CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,fixture.Zone,rng);
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,result.Outcome);Assert.AreEqual(1000,defender.GetStatValue("Hitpoints"));Assert.AreEqual(1,rng.DefenseDraws);
        }
        [Test] public void GenericDamageCannotInventDefenseAudio()
        {
            Prepare();CombatSystem.ApplyDamage(defender,1,attacker,fixture.Zone);
            Assert.AreEqual(999,defender.GetStatValue("Hitpoints"));Assert.IsEmpty(Playing());Assert.IsEmpty(receipts);
        }
        [Test] public void FailedPassiveDeflectDoesNotClaimGuard()
        {
            Prepare(false);Assert.AreEqual(9,Strike(false).ActualDamage);
            CollectionAssert.AreEqual(new[]{"SwingBlade","HitCutting"},Playing());
        }
        [Test] public void GuardFollowedByArmorStopHasNoDamageImpact()
        {
            Prepare();var armor=defender.GetPart<ArmorPart>();
            if(armor==null){armor=new ArmorPart();defender.AddPart(armor);}armor.AV=999;
            Assert.Zero(Strike(false).ActualDamage);
            CollectionAssert.AreEqual(new[]{"SwingBlade","Guard"},Playing());
        }
        [TestCase(true)] [TestCase(false)]
        public void ParryPrecedesRiposteEvenWhenCounterKillsOriginalAttacker(bool lethal)
        {
            Prepare(false);FiftySecondCombatFixture.Learn(defender,new LongBlades_Riposte());
            var counter=BladeDefense.FindLongBlade(defender);counter.PenBonus=3;counter.BaseDamage="1d1+8";
            if(lethal)attacker.GetStat("Hitpoints").BaseValue=1;
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,fixture.Zone,new ComposableRiposteTests.Rolls(0,0)).Outcome);
            CollectionAssert.AreEqual(new[]{"SwingBlade","Parry","SwingBlade","HitCutting"},receipts.Select(r=>r.Cue.ToString()).ToArray());
            Assert.AreSame(defender,receipts[1].Attempt.Defender);Assert.True(receipts[1].IsDefense);
            Assert.True(receipts[3].IsImpact);Assert.AreSame(attacker,receipts[3].Attempt.Defender);
            Assert.AreEqual(lethal,CombatSystem.IsDeathHandled(attacker));Assert.AreEqual(1000,defender.GetStatValue("Hitpoints"));
            Assert.Contains("Parry",Playing());
        }
    }
}
