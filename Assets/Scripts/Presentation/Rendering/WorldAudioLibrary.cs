using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Prepared clip bindings. Resources are resolved once, never from the audio tick or action callback.</summary>
    public sealed class WorldAudioLibrary
    {
        public sealed class Entry
        { public readonly AudioClip Clip;public readonly float Gain;public Entry(AudioClip clip,float gain){Clip=clip;Gain=gain;} }
        readonly Dictionary<string,Entry> music=new Dictionary<string,Entry>(StringComparer.Ordinal);
        readonly Dictionary<string,Entry> ambience=new Dictionary<string,Entry>(StringComparer.Ordinal);
        readonly List<Entry>[] sounds=new List<Entry>[13];
        public void AddMusic(string id,AudioClip clip,float gain){if(Valid(id,clip,gain))music[id]=new Entry(clip,gain);}
        public void AddAmbience(string id,AudioClip clip,float gain){if(Valid(id,clip,gain))ambience[id]=new Entry(clip,gain);}
        public void AddSound(WorldAudioCue cue,AudioClip clip,float gain)
        {
            int index=(int)cue;if(index<0||index>=sounds.Length||!Valid("cue",clip,gain))return;
            if(sounds[index]==null)sounds[index]=new List<Entry>(2);sounds[index].Add(new Entry(clip,gain));
        }
        static bool Valid(string id,AudioClip clip,float gain)=>!string.IsNullOrWhiteSpace(id)&&clip!=null&&clip.length>0&&float.IsFinite(gain)&&gain>0&&gain<=1;
        public Entry Music(string id)=>id!=null&&music.TryGetValue(id,out var entry)?entry:null;
        public Entry Ambience(string id)=>id!=null&&ambience.TryGetValue(id,out var entry)?entry:null;
        public Entry Sound(WorldAudioCue cue,int variant)
        {int i=(int)cue;if(i<0||i>=sounds.Length||sounds[i]==null||sounds[i].Count==0)return null;return sounds[i][(int)((uint)variant%(uint)sounds[i].Count)];}

        [Serializable] sealed class Catalog {public int schemaVersion;public Row[] music,ambience,sounds;}
        [Serializable] sealed class Row {public string id,resource;public float gain;}
        public static WorldAudioLibrary LoadResources()
        {
            var result=new WorldAudioLibrary();var text=Resources.Load<TextAsset>("Audio/World/catalog");
            if(text==null)return result;
            try
            {
                var catalog=JsonUtility.FromJson<Catalog>(text.text);if(catalog==null||catalog.schemaVersion!=1)return result;
                Load(catalog.music,(row,clip)=>result.AddMusic(row.id,clip,row.gain));
                Load(catalog.ambience,(row,clip)=>result.AddAmbience(row.id,clip,row.gain));
                Load(catalog.sounds,(row,clip)=>{if(Enum.TryParse(row.id,false,out WorldAudioCue cue))result.AddSound(cue,clip,row.gain);});
            }
            catch(Exception exception){Debug.LogWarning("[WorldAudio] Catalog unavailable: "+exception.Message);}
            return result;
        }
        static void Load(Row[] rows,Action<Row,AudioClip> accept)
        {
            if(rows==null)return;
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i];if(row==null||row.resource==null||!row.resource.StartsWith("Audio/World/",StringComparison.Ordinal)||row.resource.Contains(".."))continue;
                var clip=Resources.Load<AudioClip>(row.resource);if(clip!=null)accept(row,clip);
            }
        }
    }
}
