using System;
using System.Collections;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class QuestFreeSpreadStateNativePlayer
    {
        const string PassageBoundary="Seed64, first eight canonical FieldPassage assignments; actual generated committed gate, disclosed original-player setup transfer. Native menu opening, crossing, occupied closure refusal, real exit/return and F5/F6 checkpoint. No source, stock, actor, RNG or state grants. Ten paid inputs maximum;150seconds. Structural bypass is measured, not a claim of ordinary discovery or human route preference. Images require review.";

        IEnumerator Passage()
        {
            if(!Enum.TryParse("FieldPassage",out SpreadExplorationFamily family))
            {Unverified("field-passage","Current build has no FieldPassage family.");yield break;}
            Zone site=null;Entity gate=null;Cell arrival=null,opposite=null;int bypass=0;
            foreach(var entry in Candidates(family))
            {
                var z=Manager.GetZone(entry.ZoneID);
                if(z==null){_observations.Add(new{phase="passage-source-unavailable",zone=entry.ZoneID,reason="generation-refused"});WriteReport();continue;}
                foreach(var e in z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="SpreadFieldGate").OrderBy(e=>e.ID,StringComparer.Ordinal))
                {
                    var d=e.GetPart<DoorPart>();var c=z.GetEntityCell(e);
                    if(Manager.Exploration.DispositionFor(z.ZoneID)!=2||d?.IsClosed!=true||c==null||e.GetPart<DestructiblePart>()?.HP!=10)continue;
                    if(z.GetReadOnlyEntities().Any(v=>v!=Player&&v.HasTag("Creature")&&v.GetStatValue("Hitpoints")>0&&FactionManager.IsHostile(v,Player)&&SpatialQuery.DistanceToCell(z,v,c.X,c.Y)<=8))continue;
                    int dx=d.QuarterTurns%2==0?0:1,dy=1-dx;
                    var a=z.GetCell(c.X-dx,c.Y-dy);var b=z.GetCell(c.X+dx,c.Y+dy);
                    if(!DrySafe(z,a,null)||!DrySafe(z,b,null)||c.Occupants.Any(v=>v!=e&&(v.HasTag("Creature")||v.HasPart<TriggerOnStepPart>()||v.HasPart<LiquidPoolPart>()||v.GetPart<PhysicsPart>()?.Takeable==true)))continue;
                    var route=FindPath.Search(z,a.X,a.Y,b.X,b.Y);
                    if(!route.Usable||route.Steps.Count<=3)continue;
                    site=z;gate=e;arrival=a;opposite=b;bypass=route.Steps.Count;break;
                }
                Selection(entry,z,gate,arrival,"committed closed gate, dry cardinal crossing, actual longer physical bypass, no hostile within eight cells");
                if(gate!=null)break;
            }
            if(gate==null){Unverified("field-passage","No actual admitted safe source in eight fixed seed64 assignments.");yield break;}
            yield return Transfer(site,arrival,"actual generated field passage");
            var anchor=Zone.GetEntityCell(gate);var part=gate.GetPart<DoorPart>();string id=gate.ID,zoneId=Zone.ZoneID;int quarter=part.QuarterTurns;
            Check("actual_closed_gate_and_longer_bypass",part.IsClosed&&Zone.GetEntityCell(gate)==anchor&&Manager.Exploration.DispositionFor(zoneId)==2&&bypass>3);
            _observations.Add(new{phase="actual-passage-geometry",zone=zoneId,owner=id,quarter,anchor=new[]{anchor.X,anchor.Y},arrival=new[]{arrival.X,arrival.Y},opposite=new[]{opposite.X,opposite.Y},closedPhysicalSteps=bypass,openCrossingSteps=2,openActionCost=1,boundary="Measured native physical route; whole bypass not walked by this local observer."});
            Visual(gate,"spread-field-gate-closed",1);yield return Capture("01-generated-closed-field-gate");
            yield return Focus(gate);yield return null;var cue=_input.QueryWorldAffordance(Zone,Player);
            Check("focused_real_open_action_cue",cue.HasValue&&cue.Value.Target==gate&&cue.Value.Command==DoorPart.OpenCommand);
            yield return Capture("02-current-field-gate-open-hint");yield return CloseNormal();
            yield return PassageDoor(gate,DoorPart.OpenCommand,true);
            Check("native_open_stays_at_approach",part.IsOpen&&At==arrival&&!anchor.BlocksMovement(Player));
            Visual(gate,"spread-field-gate-open",1);yield return Capture("03-native-open-clear-passage");
            int moveX=anchor.X-arrival.X,moveY=anchor.Y-arrival.Y;
            yield return Paid(Direction(moveX,moveY),"native-enter-gate");Check("native_step_into_open_gate",At==anchor);
            yield return PassageDoor(gate,DoorPart.CloseCommand,false);
            Check("occupied_gate_refuses_without_closing",part.IsOpen&&At==anchor&&MessageLog.GetRecentEntries(6).Any(e=>e.Text=="The doorway is occupied."));
            yield return Paid(Direction(moveX,moveY),"native-cross-field-boundary");Check("native_step_reaches_other_side",At==opposite);
            yield return PassageDoor(gate,DoorPart.CloseCommand,true);Check("native_close_restores_boundary",part.IsClosed&&anchor.BlocksMovement(Player));
            yield return Capture("04-native-crossing-and-closed-boundary");
            yield return PassageDoor(gate,DoorPart.OpenCommand,true);
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Comma),"local","passage-native-exit");
            Require(WorldMap.IsWorldMapZoneID(Zone.ZoneID),"actual field exit to world map");
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Period),"local","passage-native-return");
            Check("return_keeps_same_open_source",Zone==site&&Zone.GetEntityCell(gate)==anchor&&gate.ID==id&&part.IsOpen&&Manager.Exploration.DispositionFor(zoneId)==2&&Zone.GetReadOnlyEntities().Count(e=>e.ID==id)==1);
            // Normal native surface re-entry restores its departure cell. Do not
            // move the player to compensate if that contract changes.
            Require(At==opposite&&DrySafe(Zone,At,null),"native return restores safe departed approach");
            yield return Capture("05-return-to-opened-field-passage");
            var oldPlayer=Player;string playerFacts=PlayerSignature();int tick=Tick,energy=Energy,worldTick=WorldClock.CurrentTick;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"existing isolated save metadata");
            string path=Path.Combine(SaveGameService.SaveRootOverride,info.GameID,"Quick.sav.gz"),before=PassageHash(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();string saved=PassageHash(path);
            Check("native_checkpoint_saves_open_passage",saved!=before&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==zoneId&&Tick==tick&&Energy==energy);
            yield return PassageDoor(gate,DoorPart.CloseCommand,true);Check("native_close_is_later_checkpoint_mutation",part.IsClosed&&PassageHash(path)==saved);
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(oldPlayer,Player)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native load replaces player graph");yield return null;}
            yield return Settled();var restored=Zone.GetReadOnlyEntities().Single(e=>e.ID==id);var door=restored.GetPart<DoorPart>();
            Check("native_load_restores_open_passage_and_player",Zone.ZoneID==zoneId&&!ReferenceEquals(Zone,site)&&!ReferenceEquals(restored,gate)&&door.IsOpen&&door.QuarterTurns==quarter&&string.IsNullOrEmpty(door.OwnerId)&&restored.GetPart<DestructiblePart>().HP==10&&Zone.GetEntityPosition(restored)==(anchor.X,anchor.Y)&&At.X==opposite.X&&At.Y==opposite.Y&&Manager.Exploration.DispositionFor(zoneId)==2&&PlayerSignature()==playerFacts&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==worldTick&&PassageHash(path)==saved);
            Visual(restored,"spread-field-gate-open",1);yield return Capture("06-native-loaded-open-field-passage");
        }

        IEnumerator PassageDoor(Entity target,string command,bool paid)
        {
            Require(State=="Normal"&&SpatialQuery.Distance(Zone,Player,target)<=1&&_paidInputs<PaidLimit,"current reachable bounded native door action");
            int tick=Tick,energy=Energy,speed=Player.GetStatValue("Speed",100);var standing=At;
            yield return Focus(target);yield return Tap(Key.Enter);Require(State=="WorldActionMenuOpen","real field-door menu");
            yield return SelectCurrentOwner(target);yield return MenuAction(command);yield return CloseNormal();
            long cost=(long)energy+(long)(Tick-tick)*speed-Energy;
            if(paid)_paidInputs++;
            _observations.Add(new{phase="native-passage-door-action",owner=target.ID,command,paid,beforeTick=tick,afterTick=Tick,cost,open=target.GetPart<DoorPart>().IsOpen});
            Require(cost==(paid?TurnManager.ActionThreshold:0)&&At==standing,"actual field-door action cost and stationary player");
        }
        static string PassageHash(string path)
        {using(var hash=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    }
}
