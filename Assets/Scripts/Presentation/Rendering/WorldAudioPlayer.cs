using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using Unity.Profiling;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Sixteen reusable 2D sources: two music, two ambience, twelve short cues.
    /// Wall-clock fades and sound tails cannot hold a simulation turn. No world queries occur here.</summary>
    public sealed class WorldAudioPlayer:IDisposable
    {
        public const int SoundCapacity=12;
        const float FadeSeconds=2f;
        const float FocusFloor=.6f,FocusHoldSeconds=.3f,FocusRecoverySeconds=1.5f,FocusAttackRate=4f;
        float focusRemaining;
        readonly WorldAudioGainEnvelope musicVolume,ambienceVolume;
        public float EffectiveMusicVolume=>musicVolume.Value;
        public float EffectiveAmbienceVolume=>ambienceVolume.Value;
        /// <summary>Cosmetic background gain only; accepted combat cues refresh a bounded wall-clock envelope.</summary>
        public float AtmosphereMix{get;private set;}=1;
        public int CombatFocusCount{get;private set;}
        static readonly ProfilerMarker Marker=new ProfilerMarker("COO.WorldAudio.Update");
        sealed class LoopBus
        {
            public readonly AudioSource[] Sources=new AudioSource[2];
            public readonly float[] Weights=new float[2], Starts=new float[2], Gains=new float[2], GainTargets=new float[2];
            public WorldAudioLibrary.Entry Target;
            public int Incoming,StartCount;
            public float Age=FadeSeconds;
            // Music-only, session-local FIFO cache. Sixteen clip identities cover the prepared
            // catalog while bounding retention even if callers repeatedly replace its entries.
            const int BookmarkCapacity=16;
            readonly AudioClip[] bookmarkedClips;
            readonly int[] bookmarkedSamples;
            int nextBookmark;
            readonly WorldAudioPlaybackWatch[] playback={new WorldAudioPlaybackWatch(),new WorldAudioPlaybackWatch()};
            public Action<AudioClip> Completed;
            public LoopBus(bool preservePosition=false)
            {if(preservePosition){bookmarkedClips=new AudioClip[BookmarkCapacity];bookmarkedSamples=new int[BookmarkCapacity];}}
            int RememberedPosition(AudioClip clip)
            {
                if(bookmarkedClips!=null)
                    for(int i=0;i<BookmarkCapacity;i++)if(bookmarkedClips[i]==clip)return bookmarkedSamples[i];
                return 0;
            }
            bool FinishIfNatural(int index)
            {
                var source=Sources[index];
                if(bookmarkedClips==null||!Application.isPlaying||WorldAudioSettings.MusicVolume<=0||AudioListener.pause
                    ||source==null||source.clip==null||!playback[index].Observe(source.clip,source.isPlaying,source.timeSamples,AudioSettings.dspTime))return false;
                var finished=source.clip;
                for(int i=0;i<BookmarkCapacity;i++)if(bookmarkedClips[i]==finished)bookmarkedSamples[i]=0;
                if(Target?.Clip==finished)Target=null;
                Stop(source);Weights[index]=Starts[index]=0;playback[index].Reset();Completed?.Invoke(finished);return true;
            }
            public void ObserveNaturalEnds(){for(int i=0;i<2;i++)FinishIfNatural(i);}
            void Retire(int index)
            {
                if(FinishIfNatural(index))return;
                var source=Sources[index];
                if(bookmarkedClips!=null&&source!=null&&source.clip!=null)
                {
                    var clip=source.clip;int sample=source.timeSamples;
                    if(sample>=0&&sample<clip.samples)
                    {
                        int slot=-1;
                        for(int i=0;i<BookmarkCapacity;i++)if(bookmarkedClips[i]==clip){slot=i;break;}
                        if(slot<0){slot=nextBookmark;nextBookmark=(nextBookmark+1)%BookmarkCapacity;}
                        bookmarkedClips[slot]=clip;bookmarkedSamples[slot]=sample;
                    }
                }
                Stop(source);playback[index].Reset();
            }
            public void ForgetPositions()
            {if(bookmarkedClips!=null){Array.Clear(bookmarkedClips,0,BookmarkCapacity);Array.Clear(bookmarkedSamples,0,BookmarkCapacity);nextBookmark=0;}}
            public void Select(WorldAudioLibrary.Entry target)
            {
                if(Target==target)return;
                Target=target;
                if(target!=null&&Sources[Incoming].clip==target.Clip)
                {GainTargets[Incoming]=target.Gain;return;}
                // Reversing a fade must reuse the outgoing waveform at its current
                // sample position, not start a second copy of the same track at zero.
                if(target!=null)
                    for(int i=0;i<2;i++)if(Sources[i].clip==target.Clip)
                    {Incoming=i;GainTargets[i]=target.Gain;Age=0;Starts[0]=Weights[0];Starts[1]=Weights[1];return;}
                int next=Weights[0]>Weights[1]?1:0;
                Retire(next);Weights[next]=0;Gains[next]=GainTargets[next]=target?.Gain??0;
                Incoming=next;Age=0;Starts[0]=Weights[0];Starts[1]=Weights[1];
                if(target!=null)StartTarget();
            }
            void StartTarget()
            {
                if(Target?.Clip==null)return;
                var source=Sources[Incoming];playback[Incoming].Reset();source.clip=Target.Clip;
                // Assign before Play so a resumed stream never emits its introduction first.
                if(bookmarkedClips!=null)source.timeSamples=RememberedPosition(Target.Clip);
                Gains[Incoming]=GainTargets[Incoming]=Target.Gain;StartCount++;
                if(Application.isPlaying)source.Play();
            }
            public void Tick(float delta,float volume)
            {
                if(volume<=0||AudioListener.pause)
                {StopAll();return;}
                if(Target?.Clip!=null&&Sources[Incoming].clip==null){Age=0;Starts[0]=Starts[1]=Weights[0]=Weights[1]=0;StartTarget();}
                Age=Mathf.Min(FadeSeconds,Age+delta);float t=Age/FadeSeconds;
                for(int i=0;i<2;i++)
                {
                    bool arriving=i==Incoming&&Target?.Clip!=null;
                    float rise=Mathf.Sin(t*Mathf.PI*.5f);
                    Weights[i]=arriving?Mathf.Sqrt(Starts[i]*Starts[i]+(1-Starts[i]*Starts[i])*rise*rise):Starts[i]*Mathf.Cos(t*Mathf.PI*.5f);
                    Gains[i]=Mathf.MoveTowards(Gains[i],GainTargets[i],delta*.2f);
                    Sources[i].volume=Mathf.Clamp01(Weights[i]*Gains[i]*volume);
                    if(!arriving&&t>=1)Retire(i);
                }
            }
            public void Invalidate(WorldAudioLibrary.Entry entry)
            {
                if(entry?.Clip==null)return;
                if(Target?.Clip==entry.Clip)Target=null;
                for(int i=0;i<2;i++)if(Sources[i].clip==entry.Clip){Retire(i);Weights[i]=Starts[i]=0;}
            }
            public int Active=>(Sources[0].clip!=null?1:0)+(Sources[1].clip!=null?1:0);
            public void StopAll(){for(int i=0;i<2;i++){Retire(i);Weights[i]=Starts[i]=0;}Age=FadeSeconds;}
        }
        readonly WorldAudioMusicIntermission intermission=new WorldAudioMusicIntermission();
        public float MusicQuietRemaining=>intermission.Remaining;
        readonly WorldAudioLibrary library;readonly WorldAudioMusicSequence sequence;readonly LoopBus music=new LoopBus(true),ambience=new LoopBus();
        readonly AudioSource[] sounds=new AudioSource[SoundCapacity];readonly float[] remaining=new float[SoundCapacity],gains=new float[SoundCapacity];
        readonly float[] cooldown=new float[Enum.GetValues(typeof(WorldAudioCue)).Length];readonly int[] variants=new int[Enum.GetValues(typeof(WorldAudioCue)).Length];
        readonly bool[] detailVoice=new bool[SoundCapacity];
        readonly Dictionary<string,int> detailVariants=new Dictionary<string,int>(StringComparer.Ordinal);
        bool disposed;public Transform Root{get;private set;}
        public int AllocatedSources=>disposed||Root==null?0:16;
        public int ActiveMusicSources=>disposed?0:music.Active;
        public int ActiveAmbienceSources=>disposed?0:ambience.Active;
        public int ActiveSoundSources{get{int n=0;for(int i=0;i<SoundCapacity;i++)if(remaining[i]>0)n++;return n;}}
        public int ActiveDetailSources{get{int n=0;for(int i=0;i<SoundCapacity;i++)if(remaining[i]>0&&detailVoice[i])n++;return n;}}
        public int AcceptedDetailCount{get;private set;}
        public string LastDetailId{get;private set;}
        public int AcceptedSoundCount{get;private set;}
        public int MusicStartCount=>music.StartCount;
        /// <summary>Observed natural music endings, never mute, loading or context switches.</summary>
        public int MusicCompletionCount{get;private set;}
        public string CurrentMusicId{get;private set;}
        public WorldAudioPlayer(Transform parent,WorldAudioLibrary prepared)
        {
            musicVolume=new WorldAudioGainEnvelope(WorldAudioSettings.MusicVolume);ambienceVolume=new WorldAudioGainEnvelope(WorldAudioSettings.AmbienceVolume);
            library=prepared??new WorldAudioLibrary();sequence=new WorldAudioMusicSequence(library);
            music.Completed=clip=>{bool current=sequence.Select(CurrentMusicId)?.Clip==clip;sequence.Complete(clip);MusicCompletionCount++;if(current)intermission.Complete(CurrentMusicId);};Root=new GameObject("World audio"){hideFlags=HideFlags.DontSave}.transform;Root.SetParent(parent,false);
            for(int i=0;i<2;i++){music.Sources[i]=Source("Music "+i,false,128);ambience.Sources[i]=Source("Ambience "+i,true,192);}
            for(int i=0;i<SoundCapacity;i++)sounds[i]=Source("Action "+i,false,64);
        }
        AudioSource Source(string name,bool loop,int priority)
        {
            var child=new GameObject(name){hideFlags=HideFlags.DontSave};child.transform.SetParent(Root,false);var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.dopplerLevel=0;source.reverbZoneMix=0;source.pitch=1;source.priority=priority;source.volume=0;return source;
        }
        public void SetContext(WorldAudioContext context)
        {if(!CheckSources())return;CurrentMusicId=context.MusicId;music.Select(intermission.WaitFor(context.MusicId)?null:sequence.Select(context.MusicId));ambience.Select(library.Ambience(context.AmbienceId));}
        /// <summary>Retire a source-dependent bed immediately when that source ceases to exist.</summary>
        public void InvalidateAmbience(string id){if(!disposed)ambience.Invalidate(library.Ambience(id));}
        public bool TryPlay(WorldAudioCue cue,float pan=0)
        {
            int c=(int)cue;if(disposed||Root==null||AudioListener.pause||WorldAudioSettings.SfxVolume<=0||c<0||c>=cooldown.Length||cooldown[c]>0)return false;
            var entry=library.Sound(cue,variants[c]);if(entry?.Clip==null)return false;
            int slot=-1;
            for(int i=0;i<SoundCapacity;i++)if(remaining[i]<=0&&sounds[i]!=null){slot=i;break;}
            // Combat/input feedback may reclaim a decorative voice; nature never steals an action.
            if(slot<0)for(int i=0;i<SoundCapacity;i++)if(detailVoice[i]){slot=i;break;}
            if(slot<0)return false;
            variants[c]++;cooldown[c]=c<=2?.12f:.06f;
            StartSound(slot,entry,pan,1,false);AcceptedSoundCount++;return true;
        }
        /// <summary>Only the typed perceived combat route calls this. Refused audio never creates a background dip.</summary>
        public bool TryPlayCombat(WorldAudioCue cue,float pan=0)
        {
            switch(cue)
            {
                case WorldAudioCue.Swing:case WorldAudioCue.Hit:case WorldAudioCue.SwingBlade:
                case WorldAudioCue.Parry:case WorldAudioCue.Guard:
                case WorldAudioCue.SwingBlunt:case WorldAudioCue.SwingNatural:case WorldAudioCue.HitCutting:
                case WorldAudioCue.HitPiercing:case WorldAudioCue.HitBlunt:case WorldAudioCue.HitNatural:break;
                default:return false;
            }
            if(!CheckSources()||!TryPlay(cue,pan))return false;
            focusRemaining=FocusHoldSeconds;CombatFocusCount++;return true;
        }
        void AdvanceFocus(float delta)
        {
            if(AudioListener.pause||WorldAudioSettings.SfxVolume<=0){focusRemaining=0;AtmosphereMix=1;return;}
            // Split at hold expiry so one slow frame and several fast frames recover identically.
            float held=Mathf.Min(delta,focusRemaining);
            if(held>0){AtmosphereMix=Mathf.MoveTowards(AtmosphereMix,FocusFloor,held*FocusAttackRate);focusRemaining=Mathf.Max(0,focusRemaining-held);}
            if(delta>held)AtmosphereMix=Mathf.MoveTowards(AtmosphereMix,1,(delta-held)*(1-FocusFloor)/FocusRecoverySeconds);
        }
        /// <summary>Sparse environmental details share the existing pool, use the ambience bus,
        /// and occupy at most two voices. Cosmetic selection never touches simulation randomness.</summary>
        public bool TryPlayDetail(string id,float pan=0,float gain=1)
        {
            if(!CheckSources()||AudioListener.pause||WorldAudioSettings.AmbienceVolume<=0||!float.IsFinite(gain)||gain<=0||gain>1||ActiveDetailSources>=2)return false;
            int variant=0;if(id!=null)detailVariants.TryGetValue(id,out variant);
            var entry=library.Detail(id,variant);if(entry?.Clip==null)return false;
            // Use the far end of the pool; frequent footsteps usually occupy its beginning.
            for(int i=SoundCapacity-1;i>=0;i--)if(remaining[i]<=0)
            {detailVariants[id]=unchecked(variant+1);StartSound(i,entry,pan,gain,true);AcceptedDetailCount++;LastDetailId=id;return true;}
            return false;
        }
        void StartSound(int slot,WorldAudioLibrary.Entry entry,float pan,float gain,bool detail)
        {
            Stop(sounds[slot]);remaining[slot]=entry.Clip.length;gains[slot]=entry.Gain*gain;detailVoice[slot]=detail;
            sounds[slot].priority=detail?200:64;sounds[slot].clip=entry.Clip;
            sounds[slot].panStereo=float.IsFinite(pan)?Mathf.Clamp(pan,-.65f,.65f):0;
            sounds[slot].volume=gains[slot]*(detail?EffectiveAmbienceVolume*AtmosphereMix:WorldAudioSettings.SfxVolume);
            if(Application.isPlaying)sounds[slot].Play();
        }
        public void ClearDetails()
        {for(int i=0;i<SoundCapacity;i++)if(detailVoice[i]){Stop(sounds[i]);remaining[i]=0;detailVoice[i]=false;}}
        public void Update(float wallDelta)
        {
            using(Marker.Auto())
            {
                if(!CheckSources())return;float delta=SafeDelta(wallDelta);
                musicVolume.Advance(WorldAudioSettings.MusicVolume,delta,!AudioListener.pause);
                ambienceVolume.Advance(WorldAudioSettings.AmbienceVolume,delta,!AudioListener.pause);
                AdvanceFocus(delta);
                intermission.Advance(delta,WorldAudioSettings.MusicVolume>0&&!AudioListener.pause);
                music.ObserveNaturalEnds();music.Select(intermission.WaitFor(CurrentMusicId)?null:sequence.Select(CurrentMusicId));
                music.Tick(delta,EffectiveMusicVolume*AtmosphereMix);ambience.Tick(delta,EffectiveAmbienceVolume*AtmosphereMix);
                for(int c=0;c<cooldown.Length;c++)cooldown[c]=Mathf.Max(0,cooldown[c]-delta);
                for(int i=0;i<SoundCapacity;i++)
                {
                    if(remaining[i]<=0)continue;
                    float volume=detailVoice[i]?EffectiveAmbienceVolume*AtmosphereMix:WorldAudioSettings.SfxVolume;
                    remaining[i]=volume<=0||AudioListener.pause?0:Mathf.Max(0,remaining[i]-delta);
                    if(remaining[i]<=0)Stop(sounds[i]);else if(sounds[i]!=null)sounds[i].volume=gains[i]*volume;
                }
            }
        }
        internal static float SafeDelta(float value)=>float.IsFinite(value)&&value>0?Mathf.Min(value,30):0;
        bool CheckSources()
        {
            if(disposed)return false;bool complete=Root!=null;
            for(int i=0;i<2&&complete;i++)complete=music.Sources[i]!=null&&ambience.Sources[i]!=null;
            for(int i=0;i<SoundCapacity&&complete;i++)complete=sounds[i]!=null;
            if(!complete)Dispose();return complete;
        }
        public void ClearSounds(){focusRemaining=0;AtmosphereMix=1;for(int i=0;i<SoundCapacity;i++){Stop(sounds[i]);remaining[i]=0;detailVoice[i]=false;}Array.Clear(cooldown,0,cooldown.Length);}
        static void Stop(AudioSource source){if(source==null)return;if(Application.isPlaying)source.Stop();source.clip=null;source.volume=0;}
        public void Dispose()
        {
            if(disposed)return;ClearSounds();music.StopAll();music.ForgetPositions();sequence.Clear();intermission.Clear();ambience.StopAll();disposed=true;
            if(Root!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(Root.gameObject);else UnityEngine.Object.DestroyImmediate(Root.gameObject);}Root=null;
        }
    }
}
