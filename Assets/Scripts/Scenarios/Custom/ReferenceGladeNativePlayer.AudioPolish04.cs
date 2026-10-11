using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        static readonly string[] AudioPolish04Checks={"freeze_original_forage_harvested","freeze_actual_pool_visible","freeze_actual_water_phrase",
            "freeze_imported_water_outputs_signal","freeze_real_item_commits_ice","freeze_pool_volume_preserved","freeze_no_wet_phrase_under_ice",
            "freeze_idle_keeps_scan_and_voice_bounds","freeze_ordinary_finish","freeze_audio_preferences_restored"};
        const string AudioPolish04CanVerify="Actual generated FrostLichenPatch harvest through ordinary input; a real isolated visible natural pool, imported water phrase and sampled source output before freezing; real carried lichen consumption and ice from native inventory action; no further wet phrase while current ice remains. Eleven checks include shared ordinary boot.";
        const string AudioPolish04CannotVerify="Two explicitly recorded travel shortcuts reach unchanged generated forage and pool. Forage uses the existing hazard-free clearance-two specialist landing; nearby enemies remain active during the one paid harvest. The pool observation keeps strict twelve-cell hostile clearance. No healing is granted or used. Source selection is bounded and script-directed, not natural discovery. No inventory, liquid, ice, temperature, HP, clock, FOV or audio cue is injected. No live thaw, frozen whole river, all-biome or subjective listening claim. Per-source noncontiguous samples do not prove physical speakers, global clipping or Deck performance. Only in-memory ambience level is temporarily changed and restored.";
        sealed class Freeze04Site{public Zone Zone;public Entity Pool;public Cell Approach;}
        IEnumerator RunAudioPolish04Frozen()
        {
            float old=WorldAudioSettings.AmbienceVolume;
            _audioPolishCleanup=()=>{WorldAudioSettings.AmbienceVolume=old;Check("freeze_audio_preferences_restored",WorldAudioSettings.AmbienceVolume==old);};
            WorldAudioSettings.AmbienceVolume=.6f;
            Require(!AudioListener.pause&&BiomeManager?.WorldSeed==64,"normal isolated audio and world");
            var actor=_input.PlayerEntity;string actorId=actor.ID;var pack=actor.GetPart<InventoryPart>();
            Require(actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>(),"ordinary starting actor");
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"existing bootstrap audio owner");
            var field=Freeze04FindForage(out var patch,out var near);
            Require(field!=null&&patch!=null&&near!=null,"bounded existing generated alembic with a real hazard-free clearance-two forage approach");
            yield return BiomeTravel(field,near,"actual finite FrostLichenPatch; existing specialist clearance-two policy, nearby enemies remain active");
            int count=pack.Objects.Where(e=>e.BlueprintName=="FrostLichen").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            yield return BiomeWorldAction(patch,"Harvest");yield return ResidentCloseMenus();
            int earned=pack.Objects.Where(e=>e.BlueprintName=="FrostLichen").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            Check("freeze_original_forage_harvested",patch.GetPart<HarvestablePart>().Harvested&&field.GetEntityCell(patch)==null
                &&earned>count&&earned-count<=2&&River03OneTurn(tick,energy));
            var lichen=pack.Objects.First(e=>e.BlueprintName=="FrostLichen");yield return Capture("freeze-01-actual-earned-lichen");
            var site=Freeze04FindSite();Require(site!=null,"bounded actual isolated aqueous source; no authored fallback hidden in this route");
            yield return River03Travel(site.Zone,site.Approach,"actual isolated finite water-family pool");
            var at=site.Zone.GetEntityCell(site.Pool);var liquid=site.Pool.GetPart<LiquidPoolPart>();int volume=liquid.Volume;string liquidId=liquid.LiquidId;
            Check("freeze_actual_pool_visible",at.IsVisible&&at.Explored&&site.Pool.GetPart<RenderPart>()?.Visible==true
                &&LiquidSourcePhase.CanDrawWater(site.Zone,site.Pool)&&volume>0&&site.Zone.TileState.HasCoating(at.X,at.Y,liquidId));
            var library=WorldAudioLibrary.LoadResources();var clips=new[]{library.Detail("water-lap",0)?.Clip,library.Detail("water-lap",1)?.Clip};
            Require(clips.All(c=>c!=null),"actual imported water details");AudioSource voice=null;double until=Time.realtimeSinceStartupAsDouble+45;
            while(Time.realtimeSinceStartupAsDouble<until&&voice==null)
            {
                voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip));
                yield return null;
            }
            Check("freeze_actual_water_phrase",voice!=null&&at.IsVisible&&site.Zone.GetEntityCell(site.Pool)==at);
            float peak=0;int samples=0;var buffer=new float[1024];until=Time.realtimeSinceStartupAsDouble+.6;
            while(Time.realtimeSinceStartupAsDouble<until&&voice!=null&&voice.isPlaying)
            {voice.GetOutputData(buffer,0);foreach(float sample in buffer){peak=Mathf.Max(peak,Mathf.Abs(sample));samples++;}yield return null;}
            Check("freeze_imported_water_outputs_signal",voice!=null&&clips.Contains(voice.clip)&&samples>0&&peak>0.000001f);
            yield return Capture("freeze-02-visible-wet-source");
            string command=InventorySystem.GetActions(actor,lichen).Single(a=>a.Command.StartsWith("MaterialField|Freeze|",StringComparison.Ordinal)
                &&a.Command.Split('|')[5]==at.X.ToString()&&a.Command.Split('|')[6]==at.Y.ToString()).Command;
            int held=pack.Objects.Where(e=>e.BlueprintName=="FrostLichen").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);
            yield return River03ItemAction(lichen,command);yield return ResidentCloseMenus();
            Check("freeze_real_item_commits_ice",site.Zone.TileState.HasCoating(at.X,at.Y,"ice")
                &&pack.Objects.Where(e=>e.BlueprintName=="FrostLichen").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==held-1&&River03OneTurn(tick,energy));
            Check("freeze_pool_volume_preserved",site.Zone.GetEntityCell(site.Pool)==at&&liquid.Volume==volume&&liquid.LiquidId==liquidId);
            Require(site.Zone.TileState.HasCoating(at.X,at.Y,"ice")&&!LiquidSourcePhase.CanDrawWater(site.Zone,site.Pool),"actual ice phase must still exist for quiet gate");
            int scans=audio.Soundscape.ScanCount;bool wet=false;until=Time.realtimeSinceStartupAsDouble+28;
            while(Time.realtimeSinceStartupAsDouble<until)
            {
                wet|=audio.Player.Root.GetComponentsInChildren<AudioSource>().Any(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip));
                Require(site.Zone.TileState.HasCoating(at.X,at.Y,"ice")&&at.IsVisible,"unchanged visible ice throughout no-input quiet window");yield return null;
            }
            Check("freeze_no_wet_phrase_under_ice",!wet);
            Check("freeze_idle_keeps_scan_and_voice_bounds",audio.Soundscape.ScanCount==scans&&audio.Soundscape.LastVisitedOwners<=512
                &&audio.Player.AllocatedSources==16&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
            _descriptions.Add(new Description{subject="Frozen source audio",source=site.Zone.ZoneID+":"+site.Pool.ID,
                text="Original literal liquid="+liquidId+", volume="+volume+"; wet sample peak="+peak+", count="+samples
                    +"; actual finite lichen consumed;28s no-input frozen-source observation. Other valid environmental and music sounds remain permitted."});
            yield return Capture("freeze-03-actual-ice-quiet");
            Check("freeze_ordinary_finish",actor.ID==actorId&&actor.GetStatValue("Hitpoints")>0&&actor.GetStat("Hitpoints").Max==40
                &&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&State()=="Normal"&&_river03Shortcuts==1&&_biomeShortcuts==1);
        }
        Zone Freeze04FindForage(out Entity patch,out Cell approach)
        {
            patch=null;approach=null;
            foreach(var entry in BiomeManager.Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family==SpreadExplorationFamily.FieldAlembic)
                .OrderBy(e=>e.ZoneID,StringComparer.Ordinal).Take(48))
            {
                var zone=BiomeManager.GetZone(entry.ZoneID);if(zone==null)continue;
                if(!zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="AlchemyStill"&&e.Properties.ContainsKey("SpreadWorksite.Role")))continue;
                foreach(var owner in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="FrostLichenPatch"&&e.Properties.ContainsKey("SpreadWorksite.Role")
                    &&e.GetPart<HarvestablePart>()?.Harvested==false))
                {
                    var at=zone.GetEntityCell(owner);if(at==null)continue;
                    var near=River03Adjacent(zone,at).FirstOrDefault(c=>BiomeSafe(zone,c,2));
                    _biomeSteps.Add("freeze-forage-preflight zone="+zone.ZoneID+" source="+owner.ID+" specialistClearanceTwoApproach="+(near!=null));
                    if(near==null)continue;patch=owner;approach=near;return zone;
                }
            }
            return null;
        }
        Freeze04Site Freeze04FindSite()
        {
            foreach(var zone in BiomeManager.CachedZones.Values.ToArray()){var site=Freeze04SiteIn(zone);if(site!=null)return site;}
            int visited=0;
            for(int y=0;y<WorldMap.Height&&visited<48;y++)for(int x=0;x<WorldMap.Width&&visited<48;x++)
            {
                var biome=BiomeManager.WorldMap.GetBiome(x,y);if((biome!=BiomeType.Spread&&biome!=BiomeType.Sodden)||BiomeManager.WorldMap.GetPOI(x,y)!=null)continue;
                visited++;var site=Freeze04SiteIn(BiomeManager.GetZone(WorldMap.ToZoneID(x,y,0)));if(site!=null)return site;
            }
            return null;
        }
        Freeze04Site Freeze04SiteIn(Zone zone)
        {
            if(zone==null||WorldMap.FromZoneID(zone.ZoneID).z!=0)return null;
            var pools=zone.GetReadOnlyEntities().Where(e=>e.GetPart<LiquidPoolPart>() is LiquidPoolPart p&&p.Volume>0
                &&(p.LiquidId=="water"||p.LiquidId=="brine"||p.LiquidId=="bog-mire")).ToArray();
            foreach(var pool in pools)
            {
                var at=zone.GetEntityCell(pool);if(at==null||at.IsInterior||!at.IsPassable()||pool.GetPart<RenderPart>()?.Visible!=true||!LiquidSourcePhase.CanDrawWater(zone,pool))continue;
                foreach(var near in River03Adjacent(zone,at))
                {
                    if(!River03Dry(zone,near)||pools.Any(other=>other!=pool&&SpatialQuery.DistanceToCell(zone,other,near.X,near.Y)<=6))continue;
                    return new Freeze04Site{Zone=zone,Pool=pool,Approach=near};
                }
            }
            return null;
        }
    }
}
