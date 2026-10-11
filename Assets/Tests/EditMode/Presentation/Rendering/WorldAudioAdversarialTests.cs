using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioAdversarialTests
    {
        GameObject root;AudioClip clip;WorldAudioPlayer audio;WorldAudioLibrary library;
        float music,ambience,sfx;bool listenerPause;
        [SetUp] public void Setup()
        {
            music=WorldAudioSettings.MusicVolume;ambience=WorldAudioSettings.AmbienceVolume;sfx=WorldAudioSettings.SfxVolume;listenerPause=AudioListener.pause;
            WorldAudioSettings.MusicVolume=WorldAudioSettings.AmbienceVolume=WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            root=new GameObject("world-audio-adverse");clip=AudioClip.Create("valid cue",48000,1,48000,false);library=new WorldAudioLibrary();
            library.AddMusic("organic",clip,.3f);library.AddMusic("danger",clip,.5f);library.AddAmbience("field",clip,.2f);
            foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))library.AddSound(cue,clip,.4f);
            audio=new WorldAudioPlayer(root.transform,library);
        }
        [TearDown] public void Cleanup()
        {
            audio?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);
            WorldAudioSettings.MusicVolume=music;WorldAudioSettings.AmbienceVolume=ambience;WorldAudioSettings.SfxVolume=sfx;AudioListener.pause=listenerPause;
        }
        [TestCase("Music 0")][TestCase("Ambience 0")][TestCase("Action 0")]
        public void DestroyedSourceFailsClosedWithoutExceptionOrUnboundedRepair(string name)
        {
            audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));audio.Update(3);Assert.True(audio.TryPlay(WorldAudioCue.Pickup));
            AudioSource victim=null;foreach(var source in root.GetComponentsInChildren<AudioSource>())if(source.name==name)victim=source;
            Assert.NotNull(victim);UnityEngine.Object.DestroyImmediate(victim.gameObject);
            Assert.DoesNotThrow(()=>audio.Update(.2f));Assert.LessOrEqual(audio.AllocatedSources,16);
            Assert.False(audio.TryPlay(WorldAudioCue.Equip),"a broken pool must not silently keep partially playing");
        }
        [Test] public void DestroyedRootCannotThrowOnNewContextOrDispose()
        {
            audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));audio.Update(3);
            UnityEngine.Object.DestroyImmediate(audio.Root.gameObject);
            Assert.DoesNotThrow(()=>audio.SetContext(WorldAudioContext.Select(BiomeType.Cave,2,false)));
            Assert.DoesNotThrow(()=>audio.Update(1));Assert.DoesNotThrow(()=>audio.Dispose());
        }
        [Test] public void SameClipContextChangesGainSmoothlyWithoutRestartingMusic()
        {
            audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));audio.Update(3);
            AudioSource source=null;foreach(var s in root.GetComponentsInChildren<AudioSource>())if(s.name=="Music 0")source=s;
            Assert.NotNull(source);Assert.AreEqual(.3f,source.volume,.001f);int starts=audio.MusicStartCount;
            audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,true));audio.Update(.01f);
            Assert.Greater(source.volume,.3f);Assert.Less(source.volume,.5f,"danger level should ramp rather than jump on the existing waveform");
            audio.Update(3);Assert.AreEqual(.5f,source.volume,.001f);Assert.AreEqual(starts,audio.MusicStartCount);
        }
        [Test] public void ReversingPlaceFadeReusesOutgoingTrackInsteadOfStartingDuplicateAtZero()
        {
            var second=AudioClip.Create("other place",48000,1,48000,false);
            try
            {
                library.AddMusic("underground",second,.3f);
                audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));audio.Update(3);
                audio.SetContext(WorldAudioContext.Select(BiomeType.Cave,1,false));audio.Update(.3f);
                Assert.AreEqual(2,audio.MusicStartCount);
                audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));audio.Update(.1f);
                Assert.AreEqual(2,audio.MusicStartCount,"the outgoing original track is still playing and must be reused");
                int original=0;foreach(var source in root.GetComponentsInChildren<AudioSource>())if(source.name.StartsWith("Music")&&source.clip==clip)original++;
                Assert.AreEqual(1,original);audio.Update(3);Assert.AreEqual(1,audio.ActiveMusicSources);
            }
            finally{UnityEngine.Object.DestroyImmediate(second);}
        }
        [TestCase(float.NaN)][TestCase(-.1f)][TestCase(1.1f)][TestCase(float.PositiveInfinity)]
        public void MalformedGainCannotEnterPreparedLibrary(float gain)
        {
            var invalid=new WorldAudioLibrary();invalid.AddMusic("organic",clip,gain);invalid.AddAmbience("field",clip,gain);invalid.AddSound(WorldAudioCue.Pickup,clip,gain);
            audio.Dispose();audio=new WorldAudioPlayer(root.transform,invalid);audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));audio.Update(3);
            Assert.AreEqual(0,audio.ActiveMusicSources);Assert.AreEqual(0,audio.ActiveAmbienceSources);Assert.False(audio.TryPlay(WorldAudioCue.Pickup));
        }
        [TestCase(-1)][TestCase(13)][TestCase(int.MaxValue)]
        public void InvalidCueCannotIndexPoolOrConsumeValidCue(int value)
        {Assert.False(audio.TryPlay((WorldAudioCue)value));Assert.True(audio.TryPlay(WorldAudioCue.Pickup));Assert.AreEqual(1,audio.AcceptedSoundCount);}
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-100f)]
        public void PanCannotPoisonSourceProperties(float pan)
        {
            Assert.True(audio.TryPlay(WorldAudioCue.Pickup,pan));
            foreach(var source in root.GetComponentsInChildren<AudioSource>())Assert.True(float.IsFinite(source.panStereo)&&Mathf.Abs(source.panStereo)<=1);
        }
        [Test] public void MutedContextChangeUnmutesCurrentPlaceWithoutOldQueuedMusic()
        {
            WorldAudioSettings.MusicVolume=0;audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));audio.Update(1);
            audio.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,true));audio.Update(1);Assert.AreEqual(0,audio.ActiveMusicSources);
            WorldAudioSettings.MusicVolume=1;audio.Update(3);Assert.AreEqual("danger",audio.CurrentMusicId);Assert.AreEqual(1,audio.ActiveMusicSources);
        }
        [Test] public void SemanticObserverFailureCannotUndoSuccessfulInventoryTransfer()
        {
            var zone=new Zone("audio-receipt");var actor=Actor("player");var item=new Entity{ID="item",BlueprintName="AudioItem"};item.AddPart(new PhysicsPart{Takeable=true});
            Assert.True(zone.AddEntity(actor,10,10));Assert.True(zone.AddEntity(item,10,10));
            Action<WorldAudioCue,Entity,Zone,int,int> observer=(c,a,z,x,y)=>throw new InvalidOperationException("audio observer");WorldAudioHooks.Callback+=observer;
            try{Assert.True(InventorySystem.Pickup(actor,item,zone));Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.Null(zone.GetEntityCell(item));}
            finally{WorldAudioHooks.Callback-=observer;}
        }
        [TestCase(false)][TestCase(true)]
        public void FailedOrCustomReadOnlyCommandsNeverPublishInventorySound(bool success)
        {
            var zone=new Zone("audio-receipt");var actor=Actor("player");Assert.True(zone.AddEntity(actor,10,10));int count=0;
            Action<WorldAudioCue,Entity,Zone,int,int> observer=(c,a,z,x,y)=>count++;WorldAudioHooks.Callback+=observer;
            try{var result=InventorySystem.ExecuteCommand(new CustomCommand(success),actor,zone);Assert.AreEqual(success,result.Success);Assert.AreEqual(0,count);}
            finally{WorldAudioHooks.Callback-=observer;}
        }
        [Test] public void LoadedCatalogBindsEveryRealFamilyAndCreditsAreReadable()
        {
            var real=WorldAudioLibrary.LoadResources();Assert.NotNull(real.Music("organic"));Assert.NotNull(real.Music("dry"));Assert.NotNull(real.Music("underground"));
            Assert.NotNull(real.Ambience("field"));Assert.NotNull(real.Ambience("water"));Assert.NotNull(real.Ambience("cave"));
            foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))Assert.NotNull(real.Sound(cue,0),cue.ToString());
            var credits=Resources.Load<TextAsset>("Audio/World/CREDITS");Assert.NotNull(credits);StringAssert.Contains("Scott Buckley",credits.text);StringAssert.Contains("4.0",credits.text);
        }
        [TestCase("StoneFloor",true)][TestCase("SandstoneFloor",true)][TestCase("Grass",false)]
        public void RealFloorBlueprintSelectsStoneCueWithoutInventedMaterialPart(string blueprint,bool stone)
        {
            using(var isolation=new HotbarSaveFixture(false,false))
            {
                var factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
                var floor=factory.CreateEntity(blueprint);Assert.NotNull(floor);Assert.True(floor.HasTag("Terrain"));
                var zone=new Zone("audio-material");var actor=Actor("player");Assert.True(zone.AddEntity(actor,10,10));Assert.True(zone.AddEntity(floor,10,11));
                zone.GetCell(10,11).IsVisible=zone.GetCell(10,11).Explored=true;
                audio.Dispose();var onlyStone=new WorldAudioLibrary();onlyStone.AddSound(WorldAudioCue.StepStone,clip,.4f);audio=new WorldAudioPlayer(root.transform,onlyStone);
                using(var router=new WorldAudioEventRouter(audio))
                {router.SetWorld(zone,actor,true);Assert.True(MovementSystem.TryMove(actor,zone,0,1));Assert.AreEqual(stone?1:0,audio.AcceptedSoundCount);}
            }
        }
        internal static Entity Actor(string id)
        {
            var e=new Entity{ID=id,BlueprintName="AudioActor"};e.SetTag("Creature");e.SetTag("Player");e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());e.AddPart(new InventoryPart());
            e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Max=20};e.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=16};return e;
        }
        sealed class CustomCommand:IInventoryCommand
        {
            readonly bool success;public CustomCommand(bool value){success=value;}public string Name=>"ReadInventory";
            public InventoryValidationResult Validate(InventoryContext context)=>InventoryValidationResult.Valid();
            public InventoryCommandResult Execute(InventoryContext context,InventoryTransaction transaction)=>success?InventoryCommandResult.Ok():InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,"refused");
        }
    }

    public sealed class WorldAudioRuntimeTests
    {
        GameObject root;InputHandler input;WorldAudioRuntime runtime;Zone zone;Entity player,enemy;TurnManager oldActive;
        readonly Dictionary<string,float?> prefs=new Dictionary<string,float?>();float music,ambience,sfx;
        [SetUp] public void Setup()
        {
            music=WorldAudioSettings.MusicVolume;ambience=WorldAudioSettings.AmbienceVolume;sfx=WorldAudioSettings.SfxVolume;
            foreach(var key in new[]{WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey})
            {prefs[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetFloat(key):(float?)null;PlayerPrefs.SetFloat(key,1);}
            root=new GameObject("world-audio-runtime-test");input=root.AddComponent<InputHandler>();zone=new Zone("Overworld.10.10.0");
            player=WorldAudioAdversarialTests.Actor("player");enemy=WorldAudioAdversarialTests.Actor("enemy");enemy.Tags.Remove("Player");enemy.AddPart(new BrainPart());enemy.GetPart<BrainPart>().PersonalEnemies.Add(player);
            Assert.True(zone.AddEntity(player,10,10));Assert.True(zone.AddEntity(enemy,11,10));
            foreach(var cell in zone.Cells){cell.Explored=true;cell.IsVisible=true;}
            input.PlayerEntity=player;input.CurrentZone=zone;input.WorldMap=new WorldMap(123);input.WorldMap.Tiles[10,10]=BiomeType.Spread;
            oldActive=TurnManager.Active;input.TurnManager=new TurnManager();runtime=root.AddComponent<WorldAudioRuntime>();
        }
        [TearDown] public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(root);typeof(TurnManager).GetProperty("Active").SetValue(null,oldActive);
            foreach(var pair in prefs)if(pair.Value.HasValue)PlayerPrefs.SetFloat(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);
            WorldAudioSettings.MusicVolume=music;WorldAudioSettings.AmbienceVolume=ambience;WorldAudioSettings.SfxVolume=sfx;
        }
        [Test] public void IdleFramesDoNotRescanWorldAndNewTickDoesExactlyOneCheck()
        {
            runtime.Initialize(input);runtime.Tick(0);int checks=runtime.ContextCheckCount;
            for(int i=0;i<100;i++)runtime.Tick(.016f);Assert.AreEqual(checks,runtime.ContextCheckCount);
            input.TurnManager.AdvanceClock(1);runtime.Tick(.016f);Assert.AreEqual(checks+1,runtime.ContextCheckCount);
        }
        [Test] public void HiddenHostileNeverCreatesDangerButSameVisibleOwnerDoes()
        {
            zone.GetCell(11,10).IsVisible=false;runtime.Initialize(input);runtime.Tick(0);Assert.False(runtime.Context.Danger);
            zone.GetCell(11,10).IsVisible=true;input.TurnManager.AdvanceClock(1);runtime.Tick(0);Assert.True(runtime.Context.Danger);
        }
        [TestCase("paused")][TestCase("disabled-input")][TestCase("disabled-renderer")]
        public void HiddenWorldDoesNotAcquireThreatAndResumeResamplesWithoutNewTurn(string hidden)
        {
            var renderer=root.AddComponent<ZoneRenderer>();input.ZoneRenderer=renderer;
            zone.GetCell(11,10).IsVisible=false;runtime.Initialize(input);runtime.Tick(3);
            Assert.False(runtime.Context.Danger);int starts=runtime.Player.MusicStartCount;
            if(hidden=="paused")renderer.Paused=true;else if(hidden=="disabled-input")input.enabled=false;else renderer.enabled=false;
            // Simulate a paid inventory action while FOV is stale behind its modal.
            zone.GetCell(11,10).IsVisible=true;input.TurnManager.AdvanceClock(1);runtime.Tick(0);
            Assert.False(runtime.Context.Danger,"hidden-world stale FOV must not discover an enemy");
            Assert.AreEqual(starts,runtime.Player.MusicStartCount,"opening a modal must keep the exploration track");
            int checks=runtime.ContextCheckCount;renderer.Paused=false;renderer.enabled=true;input.enabled=true;
            runtime.Tick(0);Assert.True(runtime.Context.Danger,"resuming after current FOV must discover the now-visible enemy");
            Assert.AreEqual(checks+1,runtime.ContextCheckCount,"resume needs exactly one fresh context sample even without another turn");
            runtime.Tick(0);Assert.AreEqual(checks+1,runtime.ContextCheckCount,"ordinary idle must remain scan-free");
        }
        [Test] public void FirstRenderPhaseUsesCurrentVisibilityRatherThanInitializeSnapshot()
        {
            zone.GetCell(11,10).IsVisible=false;runtime.Initialize(input);
            zone.GetCell(11,10).IsVisible=true;runtime.Tick(0);Assert.True(runtime.Context.Danger);
        }
        [Test] public void KnownThreatHiddenWithoutNewTickEventuallyReleasesWithoutWorldRescan()
        {
            runtime.Initialize(input);runtime.Tick(0);Assert.True(runtime.Context.Danger);int checks=runtime.ContextCheckCount;
            zone.GetCell(11,10).IsVisible=false;runtime.Tick(1);Assert.True(runtime.Context.Danger,"recently seen threat may linger briefly");
            runtime.Tick(5);Assert.False(runtime.Context.Danger);Assert.AreEqual(checks,runtime.ContextCheckCount);
        }
        [TestCase(true)][TestCase(false)]
        public void MissingLoadGraphDoesNotThrowOrKeepOldSound(bool nullPlayer)
        {
            runtime.Initialize(input);runtime.Tick(0);Assert.True(runtime.Player.TryPlay(WorldAudioCue.Pickup));
            if(nullPlayer)input.PlayerEntity=null;else input.CurrentZone=null;
            Assert.DoesNotThrow(()=>runtime.Tick(1));Assert.AreEqual(0,runtime.Player.ActiveSoundSources);Assert.False(runtime.Context.Danger);
        }
        [Test] public void ReplacementGraphWithSameZoneIdClearsThreatWithoutDuplicatingPool()
        {
            runtime.Initialize(input);runtime.Tick(0);Assert.True(runtime.Context.Danger);
            var replacement=new Zone(zone.ZoneID);var next=WorldAudioAdversarialTests.Actor("player");Assert.True(replacement.AddEntity(next,10,10));
            input.PlayerEntity=next;input.CurrentZone=replacement;runtime.Tick(0);
            Assert.False(runtime.Context.Danger);Assert.AreEqual(16,root.GetComponentsInChildren<AudioSource>().Length);
        }
        [Test] public void RepeatedInitializeRetainsExactlyOneObserverAndFixedPool()
        {
            runtime.Initialize(input);runtime.Initialize(input);runtime.Tick(0);
            Assert.True(MovementSystem.TryMove(player,zone,0,1));Assert.AreEqual(1,runtime.AcceptedPlayerSteps);
            Assert.AreEqual(16,root.GetComponentsInChildren<AudioSource>().Length);
        }
    }
    /// <summary>New shared SFX preference remains independent of the legacy local gain and test-fixture state.</summary>
    public sealed class WorldAudioLegacyBusTests
    {
        float priorSfx,priorMusic,priorAmbience;
        [SetUp] public void Setup()
        {priorSfx=WorldAudioSettings.SfxVolume;priorMusic=WorldAudioSettings.MusicVolume;priorAmbience=WorldAudioSettings.AmbienceVolume;WorldAudioSettings.SfxVolume=1;}
        [TearDown] public void Cleanup()
        {WorldAudioSettings.SfxVolume=priorSfx;WorldAudioSettings.MusicVolume=priorMusic;WorldAudioSettings.AmbienceVolume=priorAmbience;}

        [TestCase("starter")][TestCase("ember")][TestCase("ember-adversarial")]
        public void LegacyFixtureStartsAudibleThenRestoresPreviouslyMutedGlobalBus(string fixture)
        {
            WorldAudioSettings.SfxVolume=0;
            if(fixture=="starter")
            {using(var f=new StarterAudioFixture())Assert.AreEqual(1,WorldAudioSettings.SfxVolume);}
            else if(fixture=="ember")
            {var f=new EmberSpitAudioTests();f.Setup();try{Assert.AreEqual(1,WorldAudioSettings.SfxVolume);}finally{f.Cleanup();}}
            else
            {var f=new EmberSpitAudioAdversarialTests();f.Setup();try{Assert.AreEqual(1,WorldAudioSettings.SfxVolume);}finally{f.Teardown();}}
            Assert.AreEqual(0,WorldAudioSettings.SfxVolume,"the fixture must restore a user's prior mute rather than leave it audible");
        }

        [TestCase(false)][TestCase(true)]
        public void GlobalBusMutesNewLegacyCastsAttenuatesActiveLayersAndClearsTailsWithoutReplay(bool ember)
        {
            using(var f=new StarterAudioFixture())
            {
                EmberSpitAudioPlayer emberAudio=null;
                try
                {
                    if(ember){emberAudio=new EmberSpitAudioPlayer(f.Host.transform);emberAudio.SetZone(f.Zone);}
                    var sequence=ember?f.Sequence("Pyromancy_EmberSpit",targets:new[]{new SpellFxTargetResult("target",new Point(13,10),new Point(13,10),3)}):f.Sequence();
                    Func<bool> play=()=>ember?emberAudio.Play(sequence,.295f,new Point(13,10),true):f.Play(sequence);
                    Action<float> tick=delta=>{if(ember)emberAudio.Update(delta,delta,delta);else f.Audio.Update(delta,delta,delta);};
                    Func<int> active=()=>ember?emberAudio.ActiveVoices:f.Audio.ActiveVoices;
                    WorldAudioSettings.MusicVolume=.37f;WorldAudioSettings.AmbienceVolume=.29f;
                    WorldAudioSettings.SfxVolume=0;Assert.False(play());Assert.AreEqual(0,active());
                    WorldAudioSettings.SfxVolume=1;Assert.True(play());Assert.AreEqual(1,active());
                    var sources=Array.FindAll(f.Host.GetComponentsInChildren<AudioSource>(),source=>source.clip!=null);
                    Assert.Greater(sources.Length,0);var original=new float[sources.Length];
                    for(int i=0;i<sources.Length;i++){original[i]=sources[i].volume;Assert.Greater(original[i],0);}
                    WorldAudioSettings.SfxVolume=.25f;tick(.01f);
                    for(int i=0;i<sources.Length;i++)Assert.AreEqual(original[i]*.25f,sources[i].volume,.00001f);
                    Assert.AreEqual(.37f,WorldAudioSettings.MusicVolume);Assert.AreEqual(.29f,WorldAudioSettings.AmbienceVolume);
                    Assert.AreEqual(1,SpellFxSettings.SoundVolume,"global attenuation must not overwrite the existing local spell setting");
                    WorldAudioSettings.SfxVolume=0;tick(.01f);Assert.AreEqual(0,active());
                    Assert.IsTrue(Array.TrueForAll(sources,source=>source.clip==null));
                    WorldAudioSettings.SfxVolume=1;tick(1);Assert.AreEqual(0,active(),"unmuting cannot replay cancelled spell tails");
                }
                finally{emberAudio?.Dispose();}
            }
        }
    }
}
