using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        static readonly string[] AudioPolish16Checks={"regional16_distinct_imported_compositions",
            "regional16_sodden_ground_visit","regional16_sodden_streamed_signal",
            "regional16_grovelands_ground_visit","regional16_grovelands_streamed_signal",
            "regional16_native_paid_map_travel","regional16_idle_no_rescan_or_restart",
            "regional16_exact_source_pool","regional16_return_to_original_region",
            "regional16_ordinary_finish","regional16_preferences_restored"};
        const string AudioPolish16CanVerify="Fresh ordinary glade boot, actual paid world-map walking, native ground descent/ascent in real Sodden and Grovelands, distinct full imported regional compositions and sampled source output while on their current map tiles, unchanged two-music/sixteen-total voice budget, idle bounds and native return. Twelve checks include shared ordinary boot.";
        const string AudioPolish16CannotVerify="Script-selected nearest known unmarked region tiles; not natural discovery, long expedition balance, all chunks or subjective musical fit. Ground enemies remain active and may legitimately choose danger; region PCM is measured after actual native re-ascent to its current map tile. No gameplay grants, travel shortcuts, forced context or audio cue. Sampled output is not physical speakers, global clipping or Deck performance. Only temporary in-memory audio levels are restored; saved preferences remain untouched.";
        IEnumerator RunAudioPolish16Regions()
        {
            float oldMusic=WorldAudioSettings.MusicVolume,oldAmbience=WorldAudioSettings.AmbienceVolume,oldSfx=WorldAudioSettings.SfxVolume;
            bool oldPause=AudioListener.pause;string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};
            bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] saved=keys.Select(key=>PlayerPrefs.GetFloat(key)).ToArray();
            _audioPolishCleanup=()=>{
                WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
                bool unchanged=true;for(int i=0;i<keys.Length;i++)unchanged&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==saved[i]);
                Check("regional16_preferences_restored",unchanged&&WorldAudioSettings.MusicVolume==oldMusic&&WorldAudioSettings.AmbienceVolume==oldAmbience&&WorldAudioSettings.SfxVolume==oldSfx&&AudioListener.pause==oldPause);
            };
            WorldAudioSettings.MusicVolume=.55f;WorldAudioSettings.AmbienceVolume=.45f;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null&&BiomeManager?.WorldSeed==64,"ordinary bootstrap audio/world");
            string actorID=_input.PlayerEntity.ID,originID=_input.CurrentZone.ZoneID;var origin=WorldMap.FromZoneID(originID);Require(origin.z==0,"actual surface start");
            var library=WorldAudioLibrary.LoadResources();var wet=library.Music("sodden")?.Clip;var grove=library.Music("grovelands")?.Clip;
            Check("regional16_distinct_imported_compositions",wet!=null&&grove!=null&&wet!=grove&&wet!=library.Music("organic")?.Clip
                &&grove!=library.Music("organic")?.Clip&&wet.length>300&&grove.length>300);
            Require(wet!=null&&grove!=null,"prepared independent regional clips");
            yield return Tap(Key.LeftShift,Key.Comma);Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID),"actual native map ascent");
            int tick=_input.TurnManager.TickCount,steps=0;
            foreach(var biome in new[]{BiomeType.Sodden,BiomeType.Grovelands})
            {
                string id=biome==BiomeType.Sodden?"sodden":"grovelands";var wanted=biome==BiomeType.Sodden?wet:grove;
                var from=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);int tx=-1,ty=-1,best=int.MaxValue;
                for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
                {
                    if(BiomeManager.WorldMap.GetBiome(x,y)!=biome||BiomeManager.WorldMap.GetPOI(x,y)!=null)continue;
                    int distance=Math.Abs(x-from.worldX)+Math.Abs(y-from.worldY);if(distance<best){best=distance;tx=x;ty=y;}
                }
                Require(tx>=0&&best>0,"real different unmarked region destination");
                int before=_audio16MapSteps;yield return AudioPolish16WalkMap(tx,ty);steps+=_audio16MapSteps-before;
                yield return Tap(Key.LeftShift,Key.Period);string zoneID=WorldMap.ToZoneID(tx,ty,0);
                Check("regional16_"+id+"_ground_visit",_input.CurrentZone.ZoneID==zoneID&&BiomeManager.WorldMap.GetBiome(tx,ty)==biome
                    &&_input.PlayerEntity.GetStatValue("Hitpoints")>0&&Cell()!=null);
                Require(_input.CurrentZone.ZoneID==zoneID&&_input.PlayerEntity.GetStatValue("Hitpoints")>0,"surviving actual regional arrival, no enemy suppression");
                yield return Capture("regional16-"+id+"-actual-ground");
                yield return Tap(Key.LeftShift,Key.Comma);yield return new WaitForSecondsRealtime(2.3f);
                var position=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);
                var voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().SingleOrDefault(s=>s.name.StartsWith("Music ")&&s.clip==wanted&&s.isPlaying&&s.volume>0);
                float peak=0;int count=0;var buffer=new float[1024];double until=Time.realtimeSinceStartupAsDouble+4;
                while(Time.realtimeSinceStartupAsDouble<until)
                {
                    if(voice!=null&&voice.clip==wanted&&voice.isPlaying&&voice.volume>0){voice.GetOutputData(buffer,0);foreach(float v in buffer){peak=Mathf.Max(peak,Mathf.Abs(v));count++;}}
                    yield return null;
                }
                Check("regional16_"+id+"_streamed_signal",WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID)&&position.worldX==tx&&position.worldY==ty
                    &&audio.Context.MusicId==id&&!audio.Context.Danger&&wanted.loadType==AudioClipLoadType.Streaming&&wanted.loadState==AudioDataLoadState.Loaded&&count>0&&peak>.000001f);
                _descriptions.Add(new Description{subject="Actual regional score",source=zoneID,text="Biome="+biome+"; context="+audio.Context.MusicId
                    +"; imported clip="+wanted.name+"; full seconds="+wanted.length+"; per-source sampled peak="+peak+"; count="+count+". Ground descent/ascent and all map movement were native input."});
            }
            Check("regional16_native_paid_map_travel",steps>0&&_input.TurnManager.TickCount>=tick+steps*10);
            int starts=audio.Player.MusicStartCount,checks=audio.ContextCheckCount,scans=audio.Soundscape.ScanCount;
            yield return new WaitForSecondsRealtime(.6f);
            Check("regional16_idle_no_rescan_or_restart",starts==audio.Player.MusicStartCount&&checks==audio.ContextCheckCount&&scans==audio.Soundscape.ScanCount);
            Check("regional16_exact_source_pool",audio.Player.AllocatedSources==16&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Length==16&&audio.Player.ActiveMusicSources==1);
            yield return AudioPolish16WalkMap(origin.x,origin.y);yield return new WaitForSecondsRealtime(2.2f);
            starts=audio.Player.MusicStartCount;yield return Tap(Key.LeftShift,Key.Period);yield return new WaitForSecondsRealtime(.3f);
            Check("regional16_return_to_original_region",_input.CurrentZone.ZoneID==originID&&audio.Context.MusicId=="organic"&&audio.Player.MusicStartCount==starts);
            Check("regional16_ordinary_finish",_input.PlayerEntity.ID==actorID&&State()=="Normal"&&_input.PlayerEntity.GetStatValue("Hitpoints")>0
                &&_input.PlayerEntity.GetStat("Hitpoints").Max==40&&!DebugInvincibility.IsEnabled(_input.PlayerEntity)&&!DevMode.Enabled&&!_input.PlayerEntity.HasPart<BitLockerPart>()&&_biomeShortcuts==0);
            yield return Capture("regional16-original-region-return");
        }
        int _audio16MapSteps;
        IEnumerator AudioPolish16WalkMap(int x,int y)
        {
            var at=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);int local=0;
            while(at.worldX!=x||at.worldY!=y)
            {
                Require(local++<50&&WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID)&&State()=="Normal","bounded actual map route");
                int dx=Math.Sign(x-at.worldX),dy=dx==0?Math.Sign(y-at.worldY):0;yield return Tap(Direction(dx,dy));
                var next=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);Require(next.worldX==at.worldX+dx&&next.worldY==at.worldY+dy,"actual paid map step");at=next;_audio16MapSteps++;
            }
        }
    }
}
