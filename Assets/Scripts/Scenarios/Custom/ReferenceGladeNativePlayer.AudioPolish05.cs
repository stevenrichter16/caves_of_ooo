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
        static readonly string[] AudioPolish05Checks={"room_original_enclosing_structure","room_entered_by_actual_input","room_inside_bed_has_no_exterior_habitat",
            "room_actual_structural_phrase","room_imported_structure_outputs_signal","room_idle_keeps_scan_and_voice_bounds","room_ordinary_finish","room_audio_preferences_restored"};
        const string AudioPolish05CanVerify="Enter an unchanged generated stone room through actual paid movement, observe its visible cardinal boundary while the floor is interior and wall is exterior, hear an imported structural phrase through a playing native source with measured signal, retain scan/voice bounds and ordinary actor state. Nine checks include shared ordinary boot.";
        const string AudioPolish05CannotVerify="One recorded travel shortcut reaches a legal exterior approach in a bounded selected existing village. The original room, wall, gap, floor and actor remain unmodified; no furniture, materials, interior tags, clock, FOV or sound cue is injected. Source/cue observations are not subjective listening, physical speaker proof, full-room acoustic modeling or Deck performance. This pass supports immediate cardinal enclosing structure; it does not let distant rooms or outside animals leak through shelter.";
        sealed class Room05Site{public Zone Zone;public Cell Outside,Gap,Inside,Listen;public Entity Wall;}
        IEnumerator RunAudioPolish05Room()
        {
            float old=WorldAudioSettings.AmbienceVolume;
            _audioPolishCleanup=()=>{WorldAudioSettings.AmbienceVolume=old;Check("room_audio_preferences_restored",WorldAudioSettings.AmbienceVolume==old);};
            WorldAudioSettings.AmbienceVolume=.6f;Require(!AudioListener.pause,"normal audio output");
            var actor=_input.PlayerEntity;string id=actor.ID;var site=Room05FindSite();Require(site!=null,"bounded genuine generated stone room with legal exterior entry");
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player!=null,"existing bootstrap audio");
            var at=site.Zone.GetEntityCell(site.Wall);var material=site.Wall.GetPart<MaterialPart>();
            Check("room_original_enclosing_structure",site.Listen.IsInterior&&!at.IsInterior&&site.Wall.HasTag("Wall")
                &&material?.MaterialID=="Stone"&&Math.Abs(at.X-site.Listen.X)+Math.Abs(at.Y-site.Listen.Y)==1);
            yield return River03Travel(site.Zone,site.Outside,"actual exterior approach to generated room boundary");yield return Capture("room-01-outside-original-room");
            int tick=_input.TurnManager.TickCount;
            foreach(var cell in new[]{site.Gap,site.Inside,site.Listen})
            {
                if(Cell()==cell)continue;Require(Room05Safe(site.Zone,cell),"actual room step remains safe");var before=Cell();
                yield return Tap(Direction(cell.X-before.X,cell.Y-before.Y));Require(Cell()==cell,"paid ordinary movement enters exact generated floor");
            }
            Check("room_entered_by_actual_input",Cell()==site.Listen&&Cell().IsInterior&&_input.TurnManager.TickCount>tick&&at.IsVisible&&at.Explored&&site.Wall.GetPart<RenderPart>().Visible);
            yield return null;yield return new WaitForEndOfFrame();
            Check("room_inside_bed_has_no_exterior_habitat",audio.Soundscape.AmbienceId==null&&audio.Soundscape.HabitatDetailCount==0);
            var library=WorldAudioLibrary.LoadResources();var clips=new[]{library.Detail("stone-shift",0)?.Clip,library.Detail("stone-shift",1)?.Clip};Require(clips.All(c=>c!=null),"actual imported structural palette");
            AudioSource voice=null;double until=Time.realtimeSinceStartupAsDouble+45;
            while(Time.realtimeSinceStartupAsDouble<until&&voice==null)
            {voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>!s.loop&&s.priority==200&&s.isPlaying&&s.volume>0&&clips.Contains(s.clip));yield return null;}
            Check("room_actual_structural_phrase",voice!=null&&Cell()==site.Listen&&at.IsVisible&&site.Zone.GetEntityCell(site.Wall)==at&&!at.IsInterior);
            var samples=new float[1024];float peak=0;int count=0;until=Time.realtimeSinceStartupAsDouble+.45;
            while(voice!=null&&voice.isPlaying&&Time.realtimeSinceStartupAsDouble<until)
            {voice.GetOutputData(samples,0);foreach(float sample in samples){peak=Mathf.Max(peak,Mathf.Abs(sample));count++;}yield return null;}
            Check("room_imported_structure_outputs_signal",voice!=null&&clips.Contains(voice.clip)&&count>0&&peak>.000001f);
            yield return Capture("room-02-inside-original-enclosure");int scans=audio.Soundscape.ScanCount;yield return new WaitForSecondsRealtime(2);
            Check("room_idle_keeps_scan_and_voice_bounds",audio.Soundscape.ScanCount==scans&&audio.Soundscape.LastVisitedOwners<=512&&audio.Player.AllocatedSources==16&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
            Check("room_ordinary_finish",actor.ID==id&&actor.GetStatValue("Hitpoints")>0&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&State()=="Normal"&&_river03Shortcuts==1);
            _descriptions.Add(new Description{subject="Immediate generated room boundary",source=site.Zone.ZoneID+":"+site.Wall.ID,text="Original material="+material.MaterialID+"; room floor interior=true, source boundary interior=false; positive imported structural peak="+peak+", sampled values="+count+". No room or wall tags were rewritten."});
        }
        Room05Site Room05FindSite()
        {
            int visited=0;
            for(int y=0;y<WorldMap.Height&&visited<32;y++)for(int x=0;x<WorldMap.Width&&visited<32;x++)
            {
                var poi=BiomeManager.WorldMap.GetPOI(x,y);if(poi?.Type!=POIType.Village)continue;
                visited++;var zone=BiomeManager.GetZone(WorldMap.ToZoneID(x,y,0));
                foreach(var gap in zone.Cells.Cast<Cell>().Where(c=>!c.IsInterior))foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {
                    var outside=zone.GetCell(gap.X-d.Item1,gap.Y-d.Item2);var inside=zone.GetCell(gap.X+d.Item1,gap.Y+d.Item2);
                    if(outside==null||outside.IsInterior||inside?.IsInterior!=true||!River03Dry(zone,outside)||!Room05Safe(zone,gap)||!Room05Safe(zone,inside))continue;
                    foreach(int side in new[]{-1,1})
                    {
                        var listen=zone.GetCell(inside.X+side*d.Item2,inside.Y+side*d.Item1);
                        var boundary=zone.GetCell(gap.X+side*d.Item2,gap.Y+side*d.Item1);
                        var wall=boundary?.Objects.FirstOrDefault(e=>e.HasTag("Wall")&&e.GetPart<MaterialPart>()?.MaterialID=="Stone"&&e.GetPart<RenderPart>()?.Visible==true);
                        if(listen?.IsInterior!=true||wall==null||boundary.IsInterior||!Room05Safe(zone,listen))continue;
                        return new Room05Site{Zone=zone,Outside=outside,Gap=gap,Inside=inside,Listen=listen,Wall=wall};
                    }
                }
            }
            return null;
        }
        bool Room05Safe(Zone zone,Cell cell)=>cell!=null&&!cell.BlocksMovement(_input.PlayerEntity)
            &&!cell.Occupants.Any(e=>e!=_input.PlayerEntity&&(e.HasTag("Creature")||e.HasTag("Trap")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()))
            &&zone.TileState.Get(cell.X,cell.Y)?.Coatings?.Any(c=>c.Turns>0)!=true&&zone.TileState.Heat(cell.X,cell.Y)<=0
            &&!zone.GetReadOnlyEntities().Any(e=>e!=_input.PlayerEntity&&e.HasTag("Creature")&&FactionManager.IsHostile(e,_input.PlayerEntity)&&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<=12);
    }
}
