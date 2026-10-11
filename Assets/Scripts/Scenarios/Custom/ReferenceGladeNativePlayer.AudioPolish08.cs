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
        static readonly string[] AudioPolish08Checks={"brush_original_generated_dry_source","brush_distant_start_has_no_leaf_voice","brush_real_paid_approach",
            "brush_actual_imported_phrase","brush_imported_voice_outputs_signal","brush_real_paid_departure","brush_out_of_range_stays_quiet",
            "brush_idle_keeps_scan_and_voice_bounds","brush_ordinary_finish","brush_audio_preferences_restored"};
        const string AudioPolish08CanVerify="Approach original generated DryBrush through seven actual paid moves from outside all leaf-source range, observe an imported rustle and positive per-source signal, return through seven real moves, and observe28s leaf silence beyond range. Original material/owner, ordinary actor, bounds and preferences are retained. Eleven checks include shared ordinary boot.";
        const string AudioPolish08CannotVerify="One disclosed travel shortcut reaches an unchanged dry-region route start selected from at most48 actual Beating non-POI surfaces plus cached zones. No brush, material, wildlife, wind, clock, inventory, HP, FOV or sound cue is injected. Existing leaf recordings are a designed brush texture, not a simulation or literal recording of this plant's wind. Source-specific output is not subjective listening, global clipping, physical speaker or Deck proof.";
        sealed class Brush08Site{public Zone Zone;public Entity Brush;public Cell Near,Far;public int Dx,Dy;}
        IEnumerator RunAudioPolish08Brush()
        {
            float old=WorldAudioSettings.AmbienceVolume;
            _audioPolishCleanup=()=>{WorldAudioSettings.AmbienceVolume=old;Check("brush_audio_preferences_restored",WorldAudioSettings.AmbienceVolume==old);};
            WorldAudioSettings.AmbienceVolume=.6f;Require(!AudioListener.pause,"normal isolated audio");
            var actor=_input.PlayerEntity;string id=actor.ID;var site=Brush08FindSite();Require(site!=null,"bounded genuine brush margin with legal seven-step approach and retreat");
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"bootstrap audio owner");var brush=site.Brush;
            Check("brush_original_generated_dry_source",brush.BlueprintName=="DryBrush"&&brush.GetPart<MaterialPart>()?.MaterialID=="DryPlant"
                &&brush.GetPart<PhysicsPart>()?.Takeable==false&&brush.GetPart<DestructiblePart>()?.HP>0&&site.Zone.GetEntityCell(brush)!=null);
            Brush08RecordCorridor(site,"before actual entry");
            yield return River03Travel(site.Zone,site.Far,"actual Beating brush margin, outside all nearby leaf-source radius");
            Brush08RecordCorridor(site,"after actual entry hooks, before any paid movement");
            var library=WorldAudioLibrary.LoadResources();var clips=new[]{library.Detail("leaf-rustle",0)?.Clip,library.Detail("leaf-rustle",1)?.Clip};Require(clips.All(c=>c!=null),"imported existing restrained leaf palette");
            yield return new WaitForSecondsRealtime(2);
            Check("brush_distant_start_has_no_leaf_voice",!Brush08AnyLeafNear(site.Zone,Cell())&&!Brush08LeafPlaying(audio,clips));yield return Capture("brush-01-distant-original-margin");
            for(int step=0;step<7;step++)
            {var next=site.Zone.GetCell(Cell().X-site.Dx,Cell().Y-site.Dy);Brush08RequireStep(site,next,"approach",step);int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);yield return Tap(Direction(-site.Dx,-site.Dy));Require(Cell()==next&&River03OneTurn(tick,energy),"actual paid step reaches unchanged ground");}
            Check("brush_real_paid_approach",Cell()==site.Near&&site.Zone.GetEntityCell(brush)?.IsVisible==true
                &&site.Zone.GetEntityCell(brush).Explored&&site.Zone.GetEntityCell(brush).IsInterior==false);
            Require(!site.Zone.GetReadOnlyEntities().Any(e=>(e.BlueprintName=="Tree"||e.BlueprintName=="Bush")&&SpatialQuery.DistanceToCell(site.Zone,e,Cell().X,Cell().Y)<=6),"leaf palette has no tree/bush alternate source near listener");
            AudioSource voice=null;double until=Time.realtimeSinceStartupAsDouble+45;
            while(voice==null&&Time.realtimeSinceStartupAsDouble<until)
            {voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip));yield return null;}
            Check("brush_actual_imported_phrase",voice!=null&&brush.SpatialZone==site.Zone&&site.Zone.GetEntityCell(brush)?.IsVisible==true);
            var buffer=new float[1024];int count=0;float peak=0;until=Time.realtimeSinceStartupAsDouble+.45;
            while(voice!=null&&voice.isPlaying&&Time.realtimeSinceStartupAsDouble<until)
            {voice.GetOutputData(buffer,0);foreach(float v in buffer){peak=Mathf.Max(peak,Mathf.Abs(v));count++;}yield return null;}
            Check("brush_imported_voice_outputs_signal",voice!=null&&clips.Contains(voice.clip)&&peak>.000001f&&count>0);yield return Capture("brush-02-actual-dry-brush-texture");
            for(int step=0;step<7;step++)
            {var next=site.Zone.GetCell(Cell().X+site.Dx,Cell().Y+site.Dy);Brush08RequireStep(site,next,"retreat",step);int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);yield return Tap(Direction(site.Dx,site.Dy));Require(Cell()==next&&River03OneTurn(tick,energy),"actual paid retreat");}
            Check("brush_real_paid_departure",Cell()==site.Far&&!Brush08AnyLeafNear(site.Zone,Cell()));
            yield return new WaitForEndOfFrame();
            Require((int)Field(audio,"tick")==_input.TurnManager.TickCount
                &&ReferenceEquals(Field(audio,"zone"),site.Zone)&&ReferenceEquals(Field(audio,"actor"),actor),"retreat context sampled before idle baseline");
            int scans=audio.Soundscape.ScanCount;bool heard=false;until=Time.realtimeSinceStartupAsDouble+28;
            while(Time.realtimeSinceStartupAsDouble<until){heard|=Brush08LeafPlaying(audio,clips);yield return null;}
            Check("brush_out_of_range_stays_quiet",!heard&&!Brush08AnyLeafNear(site.Zone,Cell()));
            Check("brush_idle_keeps_scan_and_voice_bounds",audio.Soundscape.ScanCount==scans&&audio.Soundscape.LastVisitedOwners<=512&&audio.Player.AllocatedSources==16);
            Check("brush_ordinary_finish",actor.ID==id&&actor.GetStatValue("Hitpoints")>0&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&State()=="Normal"&&_river03Shortcuts==1);
            _descriptions.Add(new Description{subject="Actual dry brush margin",source=site.Zone.ZoneID+":"+brush.ID,text="Literal DryPlant source,14 actual paid movement steps, imported leaf-rustle peak="+peak+", sampled values="+count+";28s quiet after leaving every leaf source's radius. Other valid ambience remains permitted."});
            yield return Capture("brush-03-real-departure-quiet");
        }
        void Brush08RecordCorridor(Brush08Site site,string phase)
        {
            var text=new System.Text.StringBuilder();
            text.Append("Phase=").Append(phase).Append("; brush=").Append(site.Brush.ID).Append(" @").Append(site.Zone.GetEntityCell(site.Brush)?.X)
                .Append(',').Append(site.Zone.GetEntityCell(site.Brush)?.Y).Append("; near=").Append(site.Near.X).Append(',').Append(site.Near.Y)
                .Append("; far=").Append(site.Far.X).Append(',').Append(site.Far.Y).Append("; direction=").Append(site.Dx).Append(',').Append(site.Dy)
                .Append("; actor=").Append(Cell()?.X).Append(',').Append(Cell()?.Y).Append("; tick=").Append(_input.TurnManager.TickCount)
                .Append("; HP=").Append(_input.PlayerEntity.GetStatValue("Hitpoints"));
            for(int step=0;step<=7;step++)
            {
                var cell=site.Zone.GetCell(site.Far.X-step*site.Dx,site.Far.Y-step*site.Dy);
                text.Append("\nCorridor ").Append(step).Append(": ").Append(Brush08SafetyDescription(site.Zone,cell));
            }
            foreach(var owner in site.Zone.GetReadOnlyEntities())
                if(owner.Properties.ContainsKey(WorldTravellers.OriginProperty)||owner.Properties.ContainsKey(WorldTravellers.DestinationProperty))
                {
                    var cell=site.Zone.GetEntityCell(owner);text.Append("\nActual traveller ").Append(owner.BlueprintName).Append('#').Append(owner.ID)
                        .Append(" @").Append(cell?.X).Append(',').Append(cell?.Y).Append("; hostile=").Append(FactionManager.IsHostile(owner,_input.PlayerEntity));
                }
            text.Append("\nThe player's occupied origin is diagnostic only; safety checks apply to actual destination cells. No encounter or simulation was changed.");
            _descriptions.Add(new Description{subject="Brush route corridor: "+phase,source=site.Zone.ZoneID,text=text.ToString()});
        }
        void Brush08RequireStep(Brush08Site site,Cell next,string phase,int index)
        {
            bool safe=River03Dry(site.Zone,next);string detail=Brush08SafetyDescription(site.Zone,next);
            _descriptions.Add(new Description{subject="Actual brush "+phase+" step "+(index+1),source=site.Zone.ZoneID,text="From="+Cell().X+","+Cell().Y
                +"; tick="+_input.TurnManager.TickCount+"; HP="+_input.PlayerEntity.GetStatValue("Hitpoints")+"; energy="+_input.TurnManager.GetEnergy(_input.PlayerEntity)+"; "+detail});
            Require(safe,"actual safe "+phase+" step "+(index+1)+": "+detail);
        }
        string Brush08SafetyDescription(Zone zone,Cell cell)
        {
            if(cell==null)return "outside zone";
            var text=new System.Text.StringBuilder();text.Append(cell.X).Append(',').Append(cell.Y).Append(" safe=").Append(River03Dry(zone,cell))
                .Append("; interior=").Append(cell.IsInterior).Append("; blocked=").Append(cell.BlocksMovement(_input.PlayerEntity))
                .Append("; activeCoating=").Append(zone.TileState.Get(cell.X,cell.Y)?.Coatings?.Any(c=>c.Turns>0)==true)
                .Append("; heat=").Append(zone.TileState.Heat(cell.X,cell.Y));
            foreach(var owner in cell.Occupants)
                if(owner.HasTag("Creature")||owner.HasTag("Trap")||owner.HasPart<TriggerOnStepPart>()||owner.HasPart<LiquidPoolPart>()||owner.HasPart<GasPoolPart>())
                    text.Append("; rejecting occupant=").Append(owner.BlueprintName).Append('#').Append(owner.ID)
                        .Append(" creature=").Append(owner.HasTag("Creature")).Append(" trap=").Append(owner.HasTag("Trap"))
                        .Append(" trigger=").Append(owner.HasPart<TriggerOnStepPart>()).Append(" liquid=").Append(owner.HasPart<LiquidPoolPart>()).Append(" gas=").Append(owner.HasPart<GasPoolPart>());
            Entity nearest=null;int distance=int.MaxValue;
            foreach(var owner in zone.GetReadOnlyEntities())
                if(owner!=_input.PlayerEntity&&owner.HasTag("Creature")&&FactionManager.IsHostile(owner,_input.PlayerEntity))
                {int d=SpatialQuery.DistanceToCell(zone,owner,cell.X,cell.Y);if(d<distance){distance=d;nearest=owner;}}
            if(nearest!=null){var at=zone.GetEntityCell(nearest);text.Append("; nearest hostile=").Append(nearest.BlueprintName).Append('#').Append(nearest.ID)
                .Append(" @").Append(at?.X).Append(',').Append(at?.Y).Append(" distance=").Append(distance).Append(" rejects<=12=").Append(distance<=12);}
            return text.ToString();
        }
        static bool Brush08LeafPlaying(WorldAudioRuntime audio,AudioClip[] clips)=>audio.Player.Root.GetComponentsInChildren<AudioSource>().Any(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip));
        static bool Brush08AnyLeafNear(Zone zone,Cell at)=>zone.GetReadOnlyEntities().Any(e=>(e.BlueprintName=="DryBrush"||e.BlueprintName=="Tree"||e.BlueprintName=="Bush")&&SpatialQuery.DistanceToCell(zone,e,at.X,at.Y)<=6);
        Brush08Site Brush08FindSite()
        {
            foreach(var zone in BiomeManager.CachedZones.Values.ToArray()){var site=Brush08SiteIn(zone);if(site!=null)return site;}
            int visited=0;for(int y=0;y<WorldMap.Height&&visited<48;y++)for(int x=0;x<WorldMap.Width&&visited<48;x++)
            {
                if(BiomeManager.WorldMap.GetBiome(x,y)!=BiomeType.Beating||BiomeManager.WorldMap.GetPOI(x,y)!=null)continue;
                visited++;var site=Brush08SiteIn(BiomeManager.GetZone(WorldMap.ToZoneID(x,y,0)));if(site!=null)return site;
            }
            return null;
        }
        Brush08Site Brush08SiteIn(Zone zone)
        {
            if(zone==null)return null;var address=WorldMap.FromZoneID(zone.ZoneID);
            if(address.z!=0||BiomeManager.WorldMap.GetBiome(address.x,address.y)!=BiomeType.Beating)return null;
            foreach(var brush in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="DryBrush"&&e.GetPart<MaterialPart>()?.MaterialID=="DryPlant"&&e.GetPart<DestructiblePart>()?.HP>0&&e.GetPart<RenderPart>()?.Visible==true))
            {
                var at=zone.GetEntityCell(brush);if(at==null||at.IsInterior)continue;
                foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {
                    var near=zone.GetCell(at.X+d.Item1,at.Y+d.Item2);var far=zone.GetCell(at.X+8*d.Item1,at.Y+8*d.Item2);
                    if(!River03Dry(zone,near)||!River03Dry(zone,far)||Brush08AnyLeafNear(zone,far))continue;
                    if(zone.GetReadOnlyEntities().Any(e=>(e.BlueprintName=="Tree"||e.BlueprintName=="Bush")&&SpatialQuery.DistanceToCell(zone,e,near.X,near.Y)<=6))continue;
                    bool path=true;for(int step=1;step<7;step++)if(!River03Dry(zone,zone.GetCell(near.X+step*d.Item1,near.Y+step*d.Item2))){path=false;break;}
                    if(path)return new Brush08Site{Zone=zone,Brush=brush,Near=near,Far=far,Dx=d.Item1,Dy=d.Item2};
                }
            }
            return null;
        }
    }
}
