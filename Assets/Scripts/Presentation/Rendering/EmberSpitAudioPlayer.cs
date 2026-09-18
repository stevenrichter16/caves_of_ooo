using System;
using CavesOfOoo.Core;
using UnityEngine;
using Unity.Profiling;

namespace CavesOfOoo.Rendering
{
    /// <summary>Bounded, presentation-only Ember voices. Copied outcomes determine contact;
    /// audio tails never own a gameplay wait. Prepare at zone binding, before casting.</summary>
    public sealed class EmberSpitAudioPlayer : IDisposable
    {
        static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("COO.EmberAudio.Update");
        const int Capacity = 4;
        const double ScheduleLead = .015;
        const float HardTimeout = 5f;
        sealed class Voice
        {
            public readonly AudioSource[] Sources = new AudioSource[3];
            public SpellFxSequence Sequence;
            public Point ContactCell;
            public bool Native, HasHit, Handled, ImpactStarted;
            public float Age, Wall, BodyAge, ImpactAge, Contact;
        }
        readonly Voice[] voices = new Voice[Capacity];
        readonly AudioClip[,] clips = new AudioClip[3,3];
        readonly Transform parent;
        Zone zone;
        bool disposed, loadFailed;
        int mixVoiceCount;
        public Transform Root { get; private set; }
        public int ActiveVoices { get; private set; }
        public int LastVariant { get; private set; }
        public int ImpactCount { get; private set; }
        public int AcceptedCount { get; private set; }
        public int AllocatedSources => Root == null ? 0 : Capacity * 3;
        public bool IsPrepared
        {
            get
            {
                if (disposed || Root == null) return false;
                for (int i=0;i<Capacity;i++)
                    for(int k=0;k<3;k++) if(voices[i]?.Sources[k]==null) return false;
                return true;
            }
        }
        public EmberSpitAudioPlayer(Transform parent=null) { this.parent=parent; }
        public void SetZone(Zone value)
        {
            if(disposed) return;
            if(!ReferenceEquals(zone,value)) ClearAll();
            zone=value;
            if(zone!=null) Prepare();
        }
        /// <summary>Idempotently load all nine clips and allocate twelve reusable sources.</summary>
        public bool Prepare()
        {
            if(IsPrepared) return true;
            if(disposed || loadFailed || zone==null) return false;
            ClearAll(); DestroyRoot();
            string[] kinds={"wind","fire","impact"};
            for(int v=0;v<3;v++) for(int k=0;k<3;k++)
            {
                var clip=Resources.Load<AudioClip>("Audio/EmberSpit/ember_spit_"+kinds[k]+"_"+(v+1).ToString("D2"));
                if(clip==null || clip.channels!=1 || clip.frequency!=48000 || clip.samples!=(k==2?33600:52800)
                    || (!clip.LoadAudioData() && clip.loadState!=AudioDataLoadState.Loaded))
                { loadFailed=true; return false; }
                clips[v,k]=clip;
            }
            var go=new GameObject("Ember Spit Audio") { hideFlags=HideFlags.DontSave };
            Root=go.transform; Root.SetParent(parent,false);
            for(int i=0;i<Capacity;i++)
            {
                voices[i]=new Voice();
                for(int k=0;k<3;k++)
                {
                    var child=new GameObject("Ember "+i+" "+kinds[k]) { hideFlags=HideFlags.DontSave };
                    child.transform.SetParent(Root,false);
                    var source=child.AddComponent<AudioSource>();
                    source.playOnAwake=false; source.loop=false; source.spatialBlend=0;
                    source.dopplerLevel=0; source.reverbZoneMix=0;
                    voices[i].Sources[k]=source;
                }
            }
            return true;
        }
        /// <summary>Contact time/cell come from the selected visual backend, in its playback clock.</summary>
        public bool Play(SpellFxSequence sequence,float contactSeconds,Point contactCell,bool nativeClock)
        {
            if(disposed || sequence==null || sequence.SpellId!="Pyromancy_EmberSpit" || zone==null || sequence.Zone!=zone
                || zone.GetCell(sequence.Source.X,sequence.Source.Y)==null || !AnyVisible(sequence)
                || !float.IsFinite(contactSeconds) || contactSeconds<0 || contactSeconds>=HardTimeout
                || SpellFxSettings.Mode==SpellFxMode.Off || SpellFxSettings.SoundVolume<=0 || AudioListener.pause || !Prepare()) return false;
            Voice free=null;
            for(int i=0;i<Capacity;i++)
            {
                if(ReferenceEquals(voices[i].Sequence,sequence)) return false;
                if(voices[i].Sequence==null && free==null) free=voices[i];
            }
            if(free==null) return false;
            free.Sequence=sequence; free.Native=nativeClock; free.Contact=contactSeconds; free.ContactCell=contactCell;
            free.HasHit=false; free.Handled=false; free.ImpactStarted=false;
            free.Age=free.Wall=free.BodyAge=free.ImpactAge=0;
            foreach(var target in sequence.Targets)
                if(target!=null && target.Cell.Equals(contactCell)) { free.HasHit=true; break; }
            int variant=(int)((uint)sequence.CosmeticSeed%3);
            LastVariant=variant+1; ActiveVoices++; AcceptedCount++;
            for(int k=0;k<3;k++) free.Sources[k].clip=clips[variant,k];
            RefreshMix();
            if(Application.isPlaying)
            {
                double start=AudioSettings.dspTime+ScheduleLead;
                free.Sources[0].PlayScheduled(start); free.Sources[1].PlayScheduled(start);
            }
            return true;
        }
        public void Update(float wallDelta,float normalDelta,float nativeDelta)
        {
            using(UpdateMarker.Auto()) Tick(wallDelta, normalDelta, nativeDelta);
        }
        void Tick(float wallDelta,float normalDelta,float nativeDelta)
        {
            if(disposed) return;
            if(!IsPrepared || SpellFxSettings.Mode==SpellFxMode.Off || SpellFxSettings.SoundVolume<=0 || AudioListener.pause)
            { ClearAll(); return; }
            wallDelta=WorldFxPlayback.SanitizeDelta(wallDelta);
            normalDelta=WorldFxPlayback.SanitizeDelta(normalDelta); nativeDelta=WorldFxPlayback.SanitizeDelta(nativeDelta);
            float pitch=Mathf.Min(3,SpellFxSettings.AnimationSpeed);
            for(int i=0;i<Capacity;i++)
            {
                var voice=voices[i]; if(voice.Sequence==null) continue;
                voice.Wall+=wallDelta;
                if(voice.Wall>=HardTimeout) { Stop(voice); continue; }
                voice.Age+=voice.Native?nativeDelta:normalDelta;
                voice.BodyAge+=wallDelta*pitch;
                if(voice.ImpactStarted) voice.ImpactAge+=wallDelta*pitch;
                if(!voice.Handled && voice.Age>=voice.Contact)
                {
                    voice.Handled=true;
                    if(voice.HasHit && Visible(voice.ContactCell))
                    {
                        voice.ImpactStarted=true; voice.ImpactAge=0; ImpactCount++;
                        if(Application.isPlaying) voice.Sources[2].PlayScheduled(AudioSettings.dspTime+ScheduleLead);
                    }
                }
                if(voice.BodyAge>=1.1f+ScheduleLead*pitch && voice.Handled &&
                    (!voice.ImpactStarted || voice.ImpactAge>=.7f+ScheduleLead*pitch)) Stop(voice);
            }
            RefreshMix();
        }
        /// <summary>Combined active voices supplied by the coordinator; standalone Ember retains its original gain.</summary>
        public void SetMixVoiceCount(int total) { mixVoiceCount=Math.Max(0,total); RefreshMix(); }
        void RefreshMix()
        {
            float volume=SpellFxSettings.SoundVolume/Math.Max(1,Math.Max(ActiveVoices,mixVoiceCount));
            float pitch=Mathf.Min(3,SpellFxSettings.AnimationSpeed);
            for(int i=0;i<Capacity;i++)
                if(voices[i]?.Sequence!=null) for(int k=0;k<3;k++)
                if(voices[i].Sources[k]!=null)
                { voices[i].Sources[k].volume=volume; voices[i].Sources[k].pitch=pitch; }
        }
        bool Visible(Point p) { var cell=zone?.GetCell(p.X,p.Y); return cell!=null && cell.Explored && cell.IsVisible; }
        bool AnyVisible(SpellFxSequence sequence)
        {
            if(Visible(sequence.Source)) return true;
            foreach(var p in sequence.Path) if(Visible(p)) return true;
            foreach(var p in sequence.AffectedCells) if(Visible(p)) return true;
            foreach(var t in sequence.Targets) if(t!=null && Visible(t.Cell)) return true;
            return false;
        }
        void Stop(Voice voice)
        {
            if(voice?.Sequence==null) return;
            for(int k=0;k<3;k++) if(voice.Sources[k]!=null)
            { if(Application.isPlaying) voice.Sources[k].Stop(); voice.Sources[k].clip=null; }
            voice.Sequence=null; ActiveVoices--;
        }
        public void ClearAll() { for(int i=0;i<Capacity;i++) Stop(voices[i]); }
        public void CancelNative()
        { for(int i=0;i<Capacity;i++) if(voices[i]!=null && voices[i].Native) Stop(voices[i]); RefreshMix(); }
        void DestroyRoot()
        {
            if(Root!=null) { if(Application.isPlaying) UnityEngine.Object.Destroy(Root.gameObject); else UnityEngine.Object.DestroyImmediate(Root.gameObject); }
            Root=null;
        }
        public void Dispose() { if(disposed) return; ClearAll(); DestroyRoot(); disposed=true; }
    }
}
