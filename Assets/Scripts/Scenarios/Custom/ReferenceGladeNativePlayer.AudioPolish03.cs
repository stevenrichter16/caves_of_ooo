using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    // Draft for the shared AudioPolish dispatcher. No direct cue or context submission.
    public sealed partial class ReferenceGladeNativePlayer
    {
        static readonly string[] AudioPolish03Checks={
            "river_flask_bought_actual_instance","river_original_empty_flask","river_generated_flow_source",
            "river_fill_conserves_units","river_flow_bed_before_pour","river_actual_nearer_puddle",
            "river_pour_conserves_units","river_bed_survives_nearer_puddle","river_imported_bed_outputs_signal",
            "river_idle_is_bounded","river_actual_refill_removes_puddle","river_ordinary_finish","river_audio_preferences_restored"};
        const string AudioPolish03CanVerify="Ordinary isolated starter; actual Sella stock purchase; unchanged generated river water; native fill, two inland steps, pour and refill; exact liquid conservation, current flow owner, imported resumed river bed and sampled source output. Fourteen total checks include shared ordinary boot.";
        const string AudioPolish03CannotVerify="Two explicitly recorded travel shortcuts reach the unchanged merchant and generated river approach. No items, liquid, HP, terrain, flow tags, visibility, clock or cue are injected. Menus intentionally retire source-dependent water sound; the gate proves correct reacquisition on visible-world return, not uninterrupted menu playback. Script-selected route is not natural discovery/travel balance. Noncontiguous per-source samples are not listening, physical speakers, global clipping proof or Deck performance. Existing in-memory ambience preference is restored; saved preferences are untouched.";
        sealed class River03Site {public Zone Zone;public Entity Flow;public Cell Approach,Listen,Pour;public int Dx,Dy;}
        int _river03Shortcuts;

        IEnumerator RunAudioPolish03River()
        {
            float old=WorldAudioSettings.AmbienceVolume;
            _audioPolishCleanup=()=>{WorldAudioSettings.AmbienceVolume=old;Check("river_audio_preferences_restored",WorldAudioSettings.AmbienceVolume==old);};
            WorldAudioSettings.AmbienceVolume=.6f;
            Require(!AudioListener.pause,"listener already active; no override of physical output state");
            var actor=_input.PlayerEntity;string actorId=actor.ID;
            Require(BiomeManager?.WorldSeed==64&&actor.GetStat("Hitpoints").Max==40&&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>(),"unchanged ordinary isolated starter");
            var audio=FindFirstObjectByType<WorldAudioRuntime>();Require(audio?.Player?.Root!=null,"existing bootstrap audio owner");
            var town=BiomeManager.GetZone(MorrowfastSceneRuntime.ZoneID);
            var seller=MorrowfastSceneRuntime.FindOwner(town,"southern-food-vendor");
            Require(seller?.BlueprintName=="MorrowfastSella","actual existing Sella");
            var flask=seller.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="LiquidFlask");
            var sellerCell=town.GetEntityCell(seller);
            var vendorApproach=River03Adjacent(town,sellerCell).FirstOrDefault(c=>River03Dry(town,c));
            Require(vendorApproach!=null,"safe unchanged vendor approach");
            yield return River03Travel(town,vendorApproach,"actual merchant; no generated stock alteration");
            yield return ResidentTrade(seller,"LiquidFlask","river_flask_bought_actual_instance");
            Require(actor.GetPart<InventoryPart>().Objects.Contains(flask),"purchased exact original shelf owner");
            var vessel=flask.GetPart<LiquidVesselPart>();
            Check("river_original_empty_flask",vessel!=null&&vessel.Volume==0&&string.IsNullOrEmpty(vessel.LiquidId));
            yield return Capture("river-01-purchased-flask");

            var site=River03FindSite();Require(site!=null,"bounded existing village/river POI with legal bank/fill/pour geometry");
            yield return River03Travel(site.Zone,site.Approach,"actual generated river approach; no source/visibility changes");
            var flow=site.Flow;var pool=flow.GetPart<LiquidPoolPart>();
            Check("river_generated_flow_source",River03VisibleFlow(flow)&&pool.Volume>=12
                &&(BiomeManager.WorldMap.GetPOI(WorldMap.FromZoneID(site.Zone.ZoneID).x,WorldMap.FromZoneID(site.Zone.ZoneID).y)?.Type==POIType.RiverChunk
                    ||BiomeManager.WorldMap.GetPOI(WorldMap.FromZoneID(site.Zone.ZoneID).x,WorldMap.FromZoneID(site.Zone.ZoneID).y)?.Type==POIType.Village));
            int initialWater=pool.Volume,tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            string fill=InventorySystem.GetActions(actor,flask).Single(a=>a.Command=="FillLiquidVessel|"+Uri.EscapeDataString(flow.ID)+"|water").Command;
            yield return River03ItemAction(flask,fill);yield return ResidentCloseMenus();
            Check("river_fill_conserves_units",vessel.Volume==12&&vessel.LiquidId=="water"&&pool.Volume==initialWater-12&&River03OneTurn(tick,energy));
            for(int i=0;i<2;i++)
            {
                var at=Cell();yield return Tap(Direction(site.Dx,site.Dy));
                Require(Cell().X==at.X+site.Dx&&Cell().Y==at.Y+site.Dy,"actual inland walking step");
            }
            Require(ReferenceEquals(Cell(),site.Listen)&&River03VisibleFlow(flow),"original river stays within actual FOV after walking");
            yield return new WaitForSecondsRealtime(2.3f);
            Check("river_flow_bed_before_pour",audio.Context.AmbienceId=="water");
            yield return Capture("river-02-original-visible-flow");
            Require(River03Dry(site.Zone,site.Pour),"the pour destination remains genuinely dry");
            int held=vessel.Volume;tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);
            string suffix="|"+Uri.EscapeDataString(site.Zone.ZoneID)+"|"+site.Pour.X+"|"+site.Pour.Y;
            string pour=InventorySystem.GetActions(actor,flask).Single(a=>a.Command.StartsWith("PourLiquidVessel|",StringComparison.Ordinal)&&a.Command.EndsWith(suffix,StringComparison.Ordinal)).Command;
            yield return River03ItemAction(flask,pour);yield return ResidentCloseMenus();
            var puddle=site.Pour.Objects.Single(e=>e.BlueprintName==LiquidVesselService.PoolBlueprint&&e.GetPart<LiquidPoolPart>()?.LiquidId=="water");
            int nearestFlow=site.Zone.GetReadOnlyEntities().Where(River03VisibleFlow).Min(e=>SpatialQuery.Distance(site.Zone,actor,e));
            Check("river_actual_nearer_puddle",!puddle.HasTag("FlowsEast")&&!puddle.HasTag("FlowsSouth")
                &&SpatialQuery.Distance(site.Zone,actor,puddle)<nearestFlow&&River03VisibleFlow(flow));
            Check("river_pour_conserves_units",vessel.Volume==0&&string.IsNullOrEmpty(vessel.LiquidId)
                &&puddle.GetPart<LiquidPoolPart>().Volume==held&&pool.Volume==initialWater-12&&River03OneTurn(tick,energy));
            yield return new WaitForSecondsRealtime(2.3f);
            Check("river_bed_survives_nearer_puddle",audio.Context.AmbienceId=="water"&&River03VisibleFlow(flow));
            yield return Capture("river-03-nearer-real-puddle");

            var imported=WorldAudioLibrary.LoadResources().Ambience("water")?.Clip;Require(imported!=null,"actual imported river asset");
            var voice=audio.Player.Root.GetComponentsInChildren<AudioSource>().SingleOrDefault(s=>s.loop&&s.isPlaying&&s.volume>0&&s.clip==imported);
            int scans=audio.Soundscape.ScanCount;float peak=0;int samples=0;var buffer=new float[1024];double end=Time.realtimeSinceStartupAsDouble+.8;
            while(Time.realtimeSinceStartupAsDouble<end)
            {
                if(voice!=null&&voice.isPlaying&&voice.volume>0){voice.GetOutputData(buffer,0);foreach(float sample in buffer){peak=Mathf.Max(peak,Mathf.Abs(sample));samples++;}}
                yield return null;
            }
            Check("river_imported_bed_outputs_signal",voice!=null&&voice.clip==imported&&imported.loadType==AudioClipLoadType.Streaming&&samples>0&&peak>0.000001f);
            Check("river_idle_is_bounded",audio.Soundscape.ScanCount==scans&&audio.Soundscape.LastVisitedOwners<=512
                &&audio.Player.AllocatedSources==16&&audio.Player.Root.GetComponentsInChildren<AudioSource>().Length==16);
            _descriptions.Add(new Description{subject="River and nearer puddle",source=site.Zone.ZoneID,
                text="Original flow="+flow.ID+"; poured owner="+puddle.ID+"; nearest visible flow distance="+nearestFlow
                    +"; exact resumed clip="+imported.name+"; sampled peak="+peak+"; samples="+samples+". Menus may silence source-dependent beds by design."});
            string refill=InventorySystem.GetActions(actor,flask).Single(a=>a.Command=="FillLiquidVessel|"+Uri.EscapeDataString(puddle.ID)+"|water").Command;
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);
            yield return River03ItemAction(flask,refill);yield return ResidentCloseMenus();
            Check("river_actual_refill_removes_puddle",vessel.Volume==held&&vessel.LiquidId=="water"&&site.Zone.GetEntityCell(puddle)==null
                &&pool.Volume==initialWater-12&&River03OneTurn(tick,energy));
            Check("river_ordinary_finish",actor.ID==actorId&&actor.GetStatValue("Hitpoints")>0&&actor.GetStat("Hitpoints").Max==40
                &&!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&State()=="Normal"&&_river03Shortcuts==2);
        }

        River03Site River03FindSite()
        {
            int inspected=0;
            for(int y=0;y<WorldMap.Height&&inspected<32;y++)for(int x=0;x<WorldMap.Width&&inspected<32;x++)
            {
                var poi=BiomeManager.WorldMap.GetPOI(x,y);
                if(poi==null||(poi.Type!=POIType.RiverChunk&&poi.Type!=POIType.Village))continue;
                inspected++;var zone=BiomeManager.GetZone(WorldMap.ToZoneID(x,y,0));
                var flows=zone.GetReadOnlyEntities().Where(e=>e.GetPart<LiquidPoolPart>()?.LiquidId=="water"&&e.GetPart<LiquidPoolPart>().Volume>=24
                    &&(e.HasTag("FlowsEast")||e.HasTag("FlowsSouth"))).ToArray();
                foreach(var flow in flows)
                {
                    var at=zone.GetEntityCell(flow);
                    foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                    {
                        var near=zone.GetCell(at.X+d.Item1,at.Y+d.Item2);var middle=zone.GetCell(at.X+2*d.Item1,at.Y+2*d.Item2);
                        var listen=zone.GetCell(at.X+3*d.Item1,at.Y+3*d.Item2);var pour=listen==null?null:zone.GetCell(listen.X-d.Item2,listen.Y+d.Item1);
                        if(!River03Dry(zone,near)||!River03Dry(zone,middle)||!River03Dry(zone,listen)||!River03Dry(zone,pour))continue;
                        if(flows.Min(e=>SpatialQuery.DistanceToCell(zone,e,listen.X,listen.Y))<=1)continue;
                        return new River03Site{Zone=zone,Flow=flow,Approach=near,Listen=listen,Pour=pour,Dx=d.Item1,Dy=d.Item2};
                    }
                }
            }
            return null;
        }
        bool River03Dry(Zone zone,Cell cell)=>cell!=null&&!cell.IsInterior&&!cell.BlocksMovement(_input.PlayerEntity)
            &&!cell.Occupants.Any(e=>e.HasTag("Creature")||e.HasTag("Trap")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>())
            &&zone.TileState.Get(cell.X,cell.Y)?.Coatings?.Any(c=>c.Turns>0)!=true&&zone.TileState.Heat(cell.X,cell.Y)<=0
            &&!zone.GetReadOnlyEntities().Any(e=>e!=_input.PlayerEntity&&e.HasTag("Creature")&&FactionManager.IsHostile(e,_input.PlayerEntity)
                &&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<=12);
        static IEnumerable<Cell> River03Adjacent(Zone zone,Cell origin)
        {foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)}){var c=zone.GetCell(origin.X+d.Item1,origin.Y+d.Item2);if(c!=null)yield return c;}}
        bool River03VisibleFlow(Entity owner)
        {
            var zone=_input.CurrentZone;var at=zone.GetEntityCell(owner);var from=Cell();var p=owner?.GetPart<LiquidPoolPart>();
            return at!=null&&at.IsVisible&&at.Explored&&!at.IsInterior&&at.Objects.Contains(owner)&&owner.SpatialZone==zone
                &&owner.GetPart<RenderPart>()?.Visible==true&&p?.ParentEntity==owner&&p.Volume>0&&p.LiquidId=="water"
                &&(owner.HasTag("FlowsEast")||owner.HasTag("FlowsSouth"))
                &&Math.Abs(at.X-from.X)<=6&&Math.Abs(at.Y-from.Y)<=6;
        }
        IEnumerator River03Travel(Zone zone,Cell cell,string reason)
        {
            Require(State()=="Normal"&&River03Dry(zone,cell),"unchanged legal travel landing");var actor=_input.PlayerEntity;
            int hp=actor.GetStatValue("Hitpoints"),tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);string gear=CombatGear(actor);
            var old=_input.CurrentZone;Require(old.TryTransferEntityTo(actor,zone,cell.X,cell.Y),"legal exact-actor travel shortcut");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=zone,NewPlayerX=cell.X,NewPlayerY=cell.Y}});
            _river03Shortcuts++;_input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("AudioPolish03Travel");
            yield return null;yield return new WaitForEndOfFrame();
            Require(actor.GetStatValue("Hitpoints")==hp&&CombatGear(actor)==gear&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy,"travel preserves actor state and time");
            _descriptions.Add(new Description{subject="Explicit audio-route travel shortcut",source=zone.ZoneID,text=reason+" @"+cell.X+","+cell.Y});
        }
        IEnumerator River03ItemAction(Entity item,string command)
        {
            yield return Tap(Key.I);Require(State()=="InventoryOpen","ordinary inventory");yield return Tap(Key.Tab);
            Require(InvField<int>("_panel")==1,"ordinary item panel");int row=RowIndex(item);Require(row>=0,"exact carried flask row");
            for(int n=0;InvField<int>("_cursorIndex")!=row;n++){Require(n<80,"bounded item cursor");yield return Tap(InvField<int>("_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);object popup=InvField<object>("_itemActionPopup");Require(popup!=null,"real action popup");
            var actions=((IList)Field(popup,"Actions")).Cast<object>().ToArray();int index=Array.FindIndex(actions,a=>(string)Field(a,"Command")==command);Require(index>=0,"actual current liquid command");
            for(int n=0;(int)Field(popup,"CursorIndex")!=index;n++){Require(n<80,"bounded action cursor");yield return Tap((int)Field(popup,"CursorIndex")<index?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);
        }
        bool River03OneTurn(int tick,int energy)
        {var actor=_input.PlayerEntity;return _input.TurnManager.GetEnergy(actor)==energy-TurnManager.ActionThreshold+(_input.TurnManager.TickCount-tick)*actor.GetStatValue("Speed",TurnManager.DefaultSpeed);}
    }
}
