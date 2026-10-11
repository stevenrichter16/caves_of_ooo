using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioCombatAdversarialTests
    {
        GameObject root;WorldAudioPlayer audio;WorldAudioEventRouter router;Zone zone;Entity actor,target;
        readonly List<AudioClip> clips=new List<AudioClip>();
        Action<WorldAudioCombatReceipt> previousCombat;Action<WorldAudioCue,Entity,Zone,int,int> previousSemantic;
        EntityAttackVisualHandler previousVisual;float priorSfx;bool priorPause;
        [SetUp] public void Setup()
        {
            priorSfx=WorldAudioSettings.SfxVolume;priorPause=AudioListener.pause;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            previousCombat=WorldAudioCombat.Callback;previousSemantic=WorldAudioHooks.Callback;previousVisual=EntityVisualHooks.AttackCallback;
            WorldAudioCombat.Callback=null;WorldAudioHooks.Callback=null;EntityVisualHooks.AttackCallback=null;
            root=new GameObject("typed combat audio test");var library=new WorldAudioLibrary();
            foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))
                for(int i=0;i<2;i++){var clip=AudioClip.Create(cue+" recording "+i,48000,1,48000,false);clips.Add(clip);library.AddSound(cue,clip,.3f);}
            audio=new WorldAudioPlayer(root.transform,library);router=new WorldAudioEventRouter(audio);
            zone=WorldAudioCombatTests.World(out actor,out target);router.SetWorld(zone,actor,true);
            actor.AddPart(new MeleeWeaponPart{Attributes="Cutting",BaseDamage="1d1",MaxStrengthBonus=0});
        }
        [TearDown] public void Cleanup()
        {
            router?.Dispose();audio?.Dispose();UnityEngine.Object.DestroyImmediate(root);foreach(var clip in clips)UnityEngine.Object.DestroyImmediate(clip);clips.Clear();
            WorldAudioCombat.Callback=previousCombat;WorldAudioHooks.Callback=previousSemantic;EntityVisualHooks.AttackCallback=previousVisual;
            WorldAudioSettings.SfxVolume=priorSfx;AudioListener.pause=priorPause;
        }
        string[] Playing()=>root.GetComponentsInChildren<AudioSource>().Where(s=>s.clip!=null).Select(s=>s.clip.name).ToArray();
        void Attack()=>Assert.True(CombatSystem.PerformMeleeAttack(actor,target,zone,new WorldAudioCombatTests.BranchRandom()));

        [Test] public void RealStrikeChoosesTwoDistinctFamilyRecordingsWithoutOldGenericDuplication()
        {
            Attack();CollectionAssert.AreEquivalent(new[]{"SwingBlade recording 0","HitCutting recording 0"},Playing());
            Assert.AreEqual(2,audio.AcceptedSoundCount);Assert.AreEqual(16,audio.AllocatedSources);
        }
        [TestCase("miss")][TestCase("armor")][TestCase("zero")][TestCase("veto")]
        public void VisibleNonDamageNeverPlaysARecordedHit(string refusal)
        {
            if(refusal=="miss")target.SetStatValue("DV",999);if(refusal=="armor")target.GetPart<ArmorPart>().AV=999;
            if(refusal=="zero")actor.GetPart<MeleeWeaponPart>().BaseDamage="1d1-1";
            if(refusal=="veto")target.AddPart(new WorldAudioCombatTests.EventProbe{Event="BeforeTakeDamage",Veto=true});
            Attack();Assert.AreEqual(1000,target.GetStatValue("Hitpoints"));CollectionAssert.AreEqual(new[]{"SwingBlade recording 0"},Playing());
        }
        [TestCase("cell-hidden")][TestCase("render-hidden")][TestCase("world-hidden")][TestCase("off-zone")][TestCase("muted")]
        public void ARealButUnperceivedStrikeDoesNotLeakCombat(string hidden)
        {
            if(hidden=="cell-hidden")zone.GetCell(11,10).IsVisible=false;
            if(hidden=="render-hidden")target.GetPart<RenderPart>().Visible=false;
            if(hidden=="world-hidden")router.SetWorld(zone,actor,false);
            if(hidden=="off-zone")router.SetWorld(new Zone("other"),actor,true);
            if(hidden=="muted")WorldAudioSettings.SfxVolume=0;
            Attack();Assert.Less(target.GetStatValue("Hitpoints"),1000);Assert.IsEmpty(Playing());Assert.AreEqual(0,audio.AcceptedSoundCount);
        }
        [Test] public void LethalVisibleHitRetainsItsCopiedContactAfterTargetRemoval()
        {
            target.GetStat("Hitpoints").BaseValue=1;Attack();Assert.True(CombatSystem.IsDeathHandled(target));Assert.IsNull(zone.GetEntityCell(target));
            CollectionAssert.AreEquivalent(new[]{"SwingBlade recording 0","HitCutting recording 0"},Playing());
        }
        [TestCase("move",false)][TestCase("hide",false)][TestCase("remove",false)]
        [TestCase("move",true)][TestCase("hide",true)][TestCase("remove",true)]
        public void ATargetChangedByDamageCallbackCannotBorrowOldContactEvenIfKilled(string change,bool lethal)
        {
            if(lethal)target.GetStat("Hitpoints").BaseValue=1;
            target.AddPart(new WorldAudioCombatTests.EventProbe{Event="BeforeTakeDamage",Run=()=>
            {if(change=="move")zone.MoveEntity(target,12,10);else if(change=="hide")target.GetPart<RenderPart>().Visible=false;else zone.RemoveEntity(target);}});
            Attack();Assert.Less(target.GetStatValue("Hitpoints"),1000);Assert.AreEqual(lethal,CombatSystem.IsDeathHandled(target));
            CollectionAssert.AreEqual(new[]{"SwingBlade recording 0"},Playing());
        }
        [TestCase("hide")][TestCase("remove")][TestCase("die")]
        public void DamageCallbackCannotGiveAHiddenDetachedOrDeadAttackerCurrentSoundAuthority(string change)
        {
            target.AddPart(new WorldAudioCombatTests.EventProbe{Event="BeforeTakeDamage",Run=()=>
            {if(change=="hide")actor.GetPart<RenderPart>().Visible=false;else if(change=="remove")zone.RemoveEntity(actor);else CombatSystem.HandleDeath(actor,target,zone);}});
            Attack();Assert.Less(target.GetStatValue("Hitpoints"),1000);
            CollectionAssert.AreEqual(new[]{"SwingBlade recording 0"},Playing());
        }
        [Test] public void GenericDamageAndVisualAttackAreSilentWhileTheNextActualWeaponStrikeIsAudible()
        {
            CombatSystem.ApplyDamage(target,3,actor,zone);EntityVisualHooks.EmitAttack(actor,target,zone);Assert.IsEmpty(Playing());
            Attack();Assert.AreEqual(2,audio.AcceptedSoundCount);
        }
        [Test] public void DistinctAcceptedStrikesActuallyAssignBothPreparedRecordings()
        {
            Attack();audio.Update(.2f);Attack();var playing=Playing();
            Assert.Contains("HitCutting recording 0",playing);Assert.Contains("HitCutting recording 1",playing);
            Assert.Contains("SwingBlade recording 0",playing);Assert.Contains("SwingBlade recording 1",playing);Assert.AreEqual(4,audio.AcceptedSoundCount);
        }
        [Test] public void DisposingTypedRouterLeavesAnotherReceiptObserverInstalled()
        {
            int calls=0;Action<WorldAudioCombatReceipt> observer=receipt=>calls++;WorldAudioCombat.Callback+=observer;router.Dispose();
            Attack();Assert.AreEqual(2,calls);Assert.IsEmpty(Playing());WorldAudioCombat.Callback-=observer;
        }
    }
}
