using System;
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
        static readonly ProfilerMarker Marker=new ProfilerMarker("COO.WorldAudio.Update");
        sealed class LoopBus
        {
            public readonly AudioSource[] Sources=new AudioSource[2];
            public readonly float[] Weights=new float[2], Starts=new float[2], Gains=new float[2], GainTargets=new float[2];
            public WorldAudioLibrary.Entry Target;
            public int Incoming,StartCount;
            public float Age=FadeSeconds;
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
                Stop(Sources[next]);Weights[next]=0;Gains[next]=GainTargets[next]=target?.Gain??0;
                Incoming=next;Age=0;Starts[0]=Weights[0];Starts[1]=Weights[1];
                if(target!=null)StartTarget();
            }
            void StartTarget()
            {if(Target?.Clip==null)return;Sources[Incoming].clip=Target.Clip;Gains[Incoming]=GainTargets[Incoming]=Target.Gain;StartCount++;if(Application.isPlaying)Sources[Incoming].Play();}
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
                    if(!arriving&&t>=1)Stop(Sources[i]);
                }
            }
            public int Active=>(Sources[0].clip!=null?1:0)+(Sources[1].clip!=null?1:0);
            public void StopAll(){for(int i=0;i<2;i++){Stop(Sources[i]);Weights[i]=Starts[i]=0;}Age=FadeSeconds;}
        }
        readonly WorldAudioLibrary library;readonly LoopBus music=new LoopBus(),ambience=new LoopBus();
        readonly AudioSource[] sounds=new AudioSource[SoundCapacity];readonly float[] remaining=new float[SoundCapacity],gains=new float[SoundCapacity];
        readonly float[] cooldown=new float[13];readonly int[] variants=new int[13];
        bool disposed;public Transform Root{get;private set;}
        public int AllocatedSources=>disposed||Root==null?0:16;
        public int ActiveMusicSources=>disposed?0:music.Active;
        public int ActiveAmbienceSources=>disposed?0:ambience.Active;
        public int ActiveSoundSources{get{int n=0;for(int i=0;i<SoundCapacity;i++)if(remaining[i]>0)n++;return n;}}
        public int AcceptedSoundCount{get;private set;}
        public int MusicStartCount=>music.StartCount;
        public string CurrentMusicId{get;private set;}
        public WorldAudioPlayer(Transform parent,WorldAudioLibrary prepared)
        {
            library=prepared??new WorldAudioLibrary();Root=new GameObject("World audio"){hideFlags=HideFlags.DontSave}.transform;Root.SetParent(parent,false);
            for(int i=0;i<2;i++){music.Sources[i]=Source("Music "+i,true,128);ambience.Sources[i]=Source("Ambience "+i,true,192);}
            for(int i=0;i<SoundCapacity;i++)sounds[i]=Source("Action "+i,false,64);
        }
        AudioSource Source(string name,bool loop,int priority)
        {
            var child=new GameObject(name){hideFlags=HideFlags.DontSave};child.transform.SetParent(Root,false);var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.dopplerLevel=0;source.reverbZoneMix=0;source.pitch=1;source.priority=priority;source.volume=0;return source;
        }
        public void SetContext(WorldAudioContext context)
        {if(!CheckSources())return;CurrentMusicId=context.MusicId;music.Select(library.Music(context.MusicId));ambience.Select(library.Ambience(context.AmbienceId));}
        public bool TryPlay(WorldAudioCue cue,float pan=0)
        {
            int c=(int)cue;if(disposed||Root==null||AudioListener.pause||WorldAudioSettings.SfxVolume<=0||c<0||c>=cooldown.Length||cooldown[c]>0)return false;
            var entry=library.Sound(cue,variants[c]);if(entry?.Clip==null)return false;
            for(int i=0;i<SoundCapacity;i++)if(remaining[i]<=0&&sounds[i]!=null)
            {
                variants[c]++;cooldown[c]=c<=2?.12f:.06f;remaining[i]=entry.Clip.length;gains[i]=entry.Gain;
                sounds[i].clip=entry.Clip;sounds[i].panStereo=float.IsFinite(pan)?Mathf.Clamp(pan,-.65f,.65f):0;
                sounds[i].volume=entry.Gain*WorldAudioSettings.SfxVolume;if(Application.isPlaying)sounds[i].Play();AcceptedSoundCount++;return true;
            }
            return false;
        }
        public void Update(float wallDelta)
        {
            using(Marker.Auto())
            {
                if(!CheckSources())return;float delta=SafeDelta(wallDelta);
                music.Tick(delta,WorldAudioSettings.MusicVolume);ambience.Tick(delta,WorldAudioSettings.AmbienceVolume);
                for(int c=0;c<cooldown.Length;c++)cooldown[c]=Mathf.Max(0,cooldown[c]-delta);
                for(int i=0;i<SoundCapacity;i++)
                {
                    if(remaining[i]<=0)continue;
                    remaining[i]=WorldAudioSettings.SfxVolume<=0||AudioListener.pause?0:Mathf.Max(0,remaining[i]-delta);
                    if(remaining[i]<=0)Stop(sounds[i]);else if(sounds[i]!=null)sounds[i].volume=gains[i]*WorldAudioSettings.SfxVolume;
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
        public void ClearSounds(){for(int i=0;i<SoundCapacity;i++){Stop(sounds[i]);remaining[i]=0;}Array.Clear(cooldown,0,cooldown.Length);}
        static void Stop(AudioSource source){if(source==null)return;if(Application.isPlaying)source.Stop();source.clip=null;source.volume=0;}
        public void Dispose()
        {
            if(disposed)return;ClearSounds();music.StopAll();ambience.StopAll();disposed=true;
            if(Root!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(Root.gameObject);else UnityEngine.Object.DestroyImmediate(Root.gameObject);}Root=null;
        }
    }
}
