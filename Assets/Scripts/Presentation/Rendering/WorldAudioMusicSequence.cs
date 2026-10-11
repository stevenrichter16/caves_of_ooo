using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Session-local ordered score cursors. Context changes preserve an unfinished
    /// selection; only a naturally completed waveform advances contexts that selected it.</summary>
    public sealed class WorldAudioMusicSequence
    {
        sealed class Cursor{public int Index;public AudioClip Selected;}
        readonly WorldAudioLibrary library;
        readonly Dictionary<string,Cursor> cursors=new Dictionary<string,Cursor>(StringComparer.Ordinal);
        // Authored WorldAudioContext values form a small fixed set; additionally bound
        // public callers so malformed dynamic IDs cannot create unbounded session state.
        const int Capacity=16;
        public WorldAudioMusicSequence(WorldAudioLibrary prepared){library=prepared??new WorldAudioLibrary();}
        public WorldAudioLibrary.Entry Select(string id)
        {
            if(id==null||library.MusicCount(id)==0)return null;
            if(!cursors.TryGetValue(id,out var cursor))
            {
                if(cursors.Count>=Capacity)return library.Music(id);
                cursor=new Cursor();cursors.Add(id,cursor);
            }
            int count=library.MusicCount(id);cursor.Index%=count;
            var selected=library.Music(id,cursor.Index);cursor.Selected=selected?.Clip;return selected;
        }
        /// <summary>Called once by a playback watch for an observed natural end, including
        /// a still-fading outgoing source. It never advances unrelated current regions.</summary>
        public bool Complete(AudioClip clip)
        {
            if(clip==null)return false;bool changed=false;
            foreach(var pair in cursors)
            {
                var cursor=pair.Value;if(cursor.Selected!=clip)continue;
                int count=library.MusicCount(pair.Key);cursor.Index=count>0?(cursor.Index+1)%count:0;
                cursor.Selected=library.Music(pair.Key,cursor.Index)?.Clip;changed=true;
            }
            return changed;
        }
        public void Clear()=>cursors.Clear();
    }

    /// <summary>Corroborate actual playback completion without confusing loading or an early
    /// stop with an ending. The owning player resets this watch before deliberate retirement.</summary>
    public sealed class WorldAudioPlaybackWatch
    {
        AudioClip clip;bool played,completed;double expectedEnd;
        const double DspTolerance=.08;
        public void Reset(){clip=null;played=completed=false;expectedEnd=0;}
        public bool Observe(AudioClip current,bool playing,int sample,double dspTime)
        {
            if(current==null||current.samples<=0||current.frequency<=0||sample<0||sample>=current.samples
                ||!double.IsFinite(dspTime)||dspTime<0)return false;
            if(clip!=current){Reset();clip=current;}
            if(completed)return false;
            if(playing)
            {
                played=true;expectedEnd=dspTime+(current.samples-sample)/(double)current.frequency;return false;
            }
            if(!played)return false;
            // A premature explicit/external stop cannot become an ending merely
            // because silence later outlasts the composition's expected duration.
            if(dspTime+DspTolerance<expectedEnd){played=false;expectedEnd=0;return false;}
            completed=true;return true;
        }
    }
}
