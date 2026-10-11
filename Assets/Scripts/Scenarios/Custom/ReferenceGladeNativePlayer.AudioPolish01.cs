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
        private static readonly string[] AudioPolish01Checks={
            "polish01_fixed_pool","polish01_surface_music","polish01_native_ascend_coordinate",
            "polish01_same_region_ascend_continuity","polish01_paid_world_map_steps",
            "polish01_current_map_region_score","polish01_nonspread_ground_visited","polish01_nonspread_ascent_retains_region","polish01_imported_map_music_signal",
            "polish01_no_local_map_ambience","polish01_idle_map_work_bound",
            "polish01_no_remote_zone_generation","polish01_loaded_map_region",
            "polish01_native_return_position","polish01_descend_same_region_continuity",
            "polish01_ordinary_finish","polish01_preferences_restored"};
        private const string AudioPolish01CanVerify="Fresh ordinary seed64 glade boot; real keyboard world-map ascent, actual paid map steps to the authored Beating and real ground descent/ascent there, current-region streamed music with nonzero per-source PCM windows, no local map ambience/threat discovery, fixed voices and no idle context growth. Actual F5/F6 on the map and native walking back to the original map cell/descending preserve current regional identity. All route movement uses normal input with no travel shortcuts or gameplay grants.";
        private const string AudioPolish01CannotVerify="Script-selected world-map route; no natural discovery or exploration quality claim. One short Beating arrival is not a test of its population balance, natural acquisition or long expeditions. No musical quality, uninterrupted continuous capture, speaker output, global clipping proof or physical Deck performance claim. Only in-memory audio levels are initialized and restored, saved preferences untouched. Imported-source samples prove signal in observed windows, not subjective listening. Other region and invalid-map counterpart coverage is native unit evidence.";

        private IEnumerator RunAudioPolish01WorldMap()
        {
            float oldMusic=WorldAudioSettings.MusicVolume,oldAmbience=WorldAudioSettings.AmbienceVolume,oldSfx=WorldAudioSettings.SfxVolume;
            string[] keys={WorldAudioSettings.MusicKey,WorldAudioSettings.AmbienceKey,WorldAudioSettings.SfxKey};
            bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] saved=keys.Select(key=>PlayerPrefs.GetFloat(key)).ToArray();
            _audioPolishCleanup=()=>{
                WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;
                bool unchanged=true;for(int i=0;i<keys.Length;i++)unchanged&=PlayerPrefs.HasKey(keys[i])==had[i]&&(!had[i]||PlayerPrefs.GetFloat(keys[i])==saved[i]);
                Check("polish01_preferences_restored",unchanged&&WorldAudioSettings.MusicVolume==oldMusic
                    &&WorldAudioSettings.AmbienceVolume==oldAmbience&&WorldAudioSettings.SfxVolume==oldSfx);
            };
            WorldAudioSettings.MusicVolume=.55f;WorldAudioSettings.AmbienceVolume=.45f;WorldAudioSettings.SfxVolume=1;
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"actual bootstrap world audio");
            var actor=_input.PlayerEntity;string actorID=actor.ID,gear=CombatGear(actor),groundID=_input.CurrentZone.ZoneID;
            var origin=WorldMap.FromZoneID(groundID);
            Require(origin.z==0&&BiomeManager!=null&&BiomeManager.WorldSeed==64,"real surface glade and authored seed64 world");
            Require(BiomeManager.WorldMap.GetBiome(origin.x,origin.y)==BiomeType.Spread,"original known Spread context");
            // All row17 tiles are authored Beating; the real map is still checked before choosing the route.
            int destinationX=origin.x,destinationY=17;
            Require(BiomeManager.WorldMap.GetBiome(destinationX,destinationY)==BiomeType.Beating,"real authored Beating destination on current map");
            yield return new WaitForSecondsRealtime(2.2f);
            Check("polish01_fixed_pool",audio.Player.AllocatedSources==16&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
            Check("polish01_surface_music",audio.Context.MusicId=="organic"&&AudioPolish01MusicVoice(audio,"organic")!=null);
            int starts=audio.Player.MusicStartCount;
            yield return Tap(Key.LeftShift,Key.Comma);
            Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID),"actual keyboard ascent reaches map zone");
            yield return new WaitForSecondsRealtime(.3f);
            var at=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);
            Check("polish01_native_ascend_coordinate",at.worldX==origin.x&&at.worldY==origin.y);
            Check("polish01_same_region_ascend_continuity",audio.Context.MusicId=="organic"&&audio.Player.MusicStartCount==starts);
            int tick=_input.TurnManager.TickCount,mapSteps=0;var cached=BiomeManager.CachedZones.Keys.OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            while(at.worldX!=destinationX||at.worldY!=destinationY)
            {
                Require(mapSteps<40&&WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID)&&State()=="Normal","bounded actual world-map walking");
                int dx=Math.Sign(destinationX-at.worldX),dy=dx==0?Math.Sign(destinationY-at.worldY):0;
                yield return Tap(Direction(dx,dy));mapSteps++;
                var next=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);
                Require(next.worldX==at.worldX+dx&&next.worldY==at.worldY+dy,"each keyboard map step actually moves");at=next;
            }
            Check("polish01_paid_world_map_steps",mapSteps>0&&_input.TurnManager.TickCount>=tick+mapSteps*10);
            Check("polish01_no_remote_zone_generation",cached.SequenceEqual(BiomeManager.CachedZones.Keys.OrderBy(id=>id,StringComparer.Ordinal)));
            yield return new WaitForSecondsRealtime(2.2f);
            Check("polish01_current_map_region_score",BiomeManager.WorldMap.GetBiome(at.worldX,at.worldY)==BiomeType.Beating
                &&audio.Context.MusicId=="dry"&&actor.GetPart<WorldMapPart>().LastZoneIDOnSurface==groundID);
            yield return Capture("polish01-02-actual-beating-map-score");
            string beatingID=WorldMap.ToZoneID(destinationX,destinationY,0);
            yield return Tap(Key.LeftShift,Key.Period);
            Require(_input.CurrentZone.ZoneID==beatingID&&_input.PlayerEntity.GetStatValue("Hitpoints")>0,"actual surviving Beating ground arrival; no grants or enemy suppression");
            Check("polish01_nonspread_ground_visited",BiomeManager.WorldMap.GetBiome(destinationX,destinationY)==BiomeType.Beating
                &&_input.CurrentZone.ZoneID==beatingID&&Cell()!=null);
            yield return Capture("polish01-03-actual-beating-ground");
            yield return Tap(Key.LeftShift,Key.Comma);
            Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID),"actual native ascent from Beating");
            yield return new WaitForSecondsRealtime(2.2f);
            at=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);
            Check("polish01_nonspread_ascent_retains_region",at.worldX==destinationX&&at.worldY==destinationY
                &&audio.Context.MusicId=="dry"&&!audio.Context.Danger);
            var voice=AudioPolish01MusicVoice(audio,"dry");float peak=0;var samples=new float[1024];int sampled=0;
            double until=Time.realtimeSinceStartupAsDouble+5;
            while(Time.realtimeSinceStartupAsDouble<until)
            {
                if(voice!=null&&voice.isPlaying&&voice.volume>0)
                {voice.GetOutputData(samples,0);foreach(float v in samples){peak=Mathf.Max(peak,Mathf.Abs(v));sampled++;}}
                yield return null;
            }
            Check("polish01_imported_map_music_signal",voice!=null&&voice.clip.loadType==AudioClipLoadType.Streaming
                &&voice.clip.loadState==AudioDataLoadState.Loaded&&sampled>0&&peak>.000001f);
            Check("polish01_no_local_map_ambience",audio.Context.AmbienceId==null&&!audio.Context.Danger
                &&audio.Player.ActiveAmbienceSources==0&&audio.Player.ActiveDetailSources==0&&audio.Soundscape.SourceCount==0);
            int contextChecks=audio.ContextCheckCount,scans=audio.Soundscape.ScanCount;starts=audio.Player.MusicStartCount;
            yield return new WaitForSecondsRealtime(.6f);
            Check("polish01_idle_map_work_bound",audio.ContextCheckCount==contextChecks&&audio.Soundscape.ScanCount==scans
                &&audio.Player.MusicStartCount==starts&&audio.Player.AllocatedSources==16);

            _descriptions.Add(new Description{subject="Actual map score",source=_input.CurrentZone.ZoneID,text="Map tile="+at.worldX+","+at.worldY
                +"; old surface="+groundID+"; actual context="+audio.Context.MusicId+"; imported clip="+voice?.clip?.name
                +"; per-source sampled peak="+peak+"; sampled floats="+sampled+"; paid map steps="+mapSteps+". No direct audio cue or playhead manipulation."});
            var beforeLoad=_input.PlayerEntity;yield return Tap(Key.F5);yield return Tap(Key.F6);
            until=Time.realtimeSinceStartupAsDouble+10;
            while(ReferenceEquals(beforeLoad,_input.PlayerEntity)&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
            at=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);
            Check("polish01_loaded_map_region",!ReferenceEquals(beforeLoad,_input.PlayerEntity)&&_input.PlayerEntity.ID==actorID
                &&WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID)&&at.worldX==destinationX&&at.worldY==destinationY
                &&audio.Context.MusicId=="dry"&&audio.Player.AllocatedSources==16);
            Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID),"loaded actual map for native return");
            int back=0;
            while(at.worldX!=origin.x||at.worldY!=origin.y)
            {
                Require(back++<40,"bounded native return on world map");int dx=Math.Sign(origin.x-at.worldX),dy=dx==0?Math.Sign(origin.y-at.worldY):0;
                yield return Tap(Direction(dx,dy));var next=WorldMap.ZoneCellToWorldCell(Cell().X,Cell().Y);
                Require(next.worldX==at.worldX+dx&&next.worldY==at.worldY+dy,"actual map return step");at=next;
            }
            yield return new WaitForSecondsRealtime(2.2f);
            Check("polish01_native_return_position",audio.Context.MusicId=="organic"&&at.worldX==origin.x&&at.worldY==origin.y);
            starts=audio.Player.MusicStartCount;yield return Tap(Key.LeftShift,Key.Period);yield return new WaitForSecondsRealtime(.4f);
            Check("polish01_descend_same_region_continuity",_input.CurrentZone.ZoneID==groundID&&Cell()!=null
                &&audio.Context.MusicId=="organic"&&audio.Player.MusicStartCount==starts);
            Check("polish01_ordinary_finish",_input.PlayerEntity.ID==actorID&&State()=="Normal"&&!DevMode.Enabled
                &&!_input.PlayerEntity.HasPart<BitLockerPart>()&&_input.PlayerEntity.GetStat("Hitpoints").Max==40
                &&_input.PlayerEntity.GetStatValue("Hitpoints")>0&&CombatGear(_input.PlayerEntity)==gear&&_biomeShortcuts==0);
            yield return Capture("polish01-04-native-return-to-original-ground");
        }
        private static AudioSource AudioPolish01MusicVoice(WorldAudioRuntime audio,string id)
        {
            var expected=WorldAudioLibrary.LoadResources().Music(id)?.Clip;
            return audio.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(source=>source.name.StartsWith("Music ")
                &&source.clip==expected&&source.clip!=null&&source.isPlaying&&source.volume>0);
        }
    }
}
