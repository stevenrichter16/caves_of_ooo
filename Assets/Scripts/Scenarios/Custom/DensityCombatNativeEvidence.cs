using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Read-only native audit witnesses. These parse existing actor-scoped
    /// diagnostics and current ownership; they never invoke gameplay or advance time.</summary>
    public static class DensityCombatNativeEvidence
    {
        public const string MarkerKind = "DensityCombatNativeKeyStart";
        public const string MoveKind = "DensityCombatNativeMoveObserved";
        public struct TurnEvidence
        {
            public string BeginTrace, EndTrace, ActionTrace, Kind;
            public int Cooldown;
        }
        struct Slice { public int Start, End; }

        public static bool TryTacticTurn(IReadOnlyList<Diag.Entry> rows,string marker,
            string player,string enemy,string command,out TurnEvidence result)
        {
            result=default;
            if(string.IsNullOrEmpty(command)||!Window(rows,marker,player,enemy,out var window)
                ||!EnemyTurns(rows,window,enemy,out var turns))return false;
            foreach(var turn in turns)
            {
                int used=-1,routed=-1,uses=0,routes=0;
                for(int i=turn.Start+1;i<turn.End;i++)
                {
                    var row=rows[i];if(row.ActorId!=enemy)continue;
                    if(row.Category=="ai"&&row.Kind=="TacticUsed"){used=i;uses++;}
                    if(row.Category=="skill"&&row.Kind=="CommandRouted")
                    {
                        if(row.TargetId!=enemy||!Text(row,"command",out var routedCommand)||routedCommand!=command)continue;
                        routed=i;routes++;
                    }
                }
                if(uses!=1||routes!=1||routed>=used)continue;
                var use=rows[used];
                if(use.TargetId!=player||!Text(use,"command",out var actual)||actual!=command
                    ||!Integer(use,"cooldown",out int cooldown)||cooldown<=0)continue;
                result=new TurnEvidence{BeginTrace=rows[turn.Start].TraceId,EndTrace=rows[turn.End].TraceId,
                    ActionTrace=use.TraceId,Kind="tactic",Cooldown=cooldown};return true;
            }
            return false;
        }

        /// <summary>A cooldown refusal alone is insufficient. One completed enemy
        /// turn must also contain an exact melee attempt or the native observer's
        /// nonforced movement while that same actor owns the scheduler turn.</summary>
        public static bool TryCooldownFallback(IReadOnlyList<Diag.Entry> rows,string marker,
            string player,string enemy,string command,int beforeCooldown,int afterCooldown,out TurnEvidence result)
        {
            result=default;
            if(string.IsNullOrEmpty(command)||beforeCooldown<=0||afterCooldown!=beforeCooldown-1
                ||!Window(rows,marker,player,enemy,out var window)
                ||!EnemyTurns(rows,window,enemy,out var turns)||turns.Count!=1)return false;
            var turn=turns[0];int reject=-1,action=-1;string kind=null;
            for(int i=turn.Start+1;i<turn.End;i++)
            {
                var row=rows[i];if(row.ActorId!=enemy)continue;
                if(row.Category=="ai"&&row.Kind=="TacticUsed")return false;
                if(row.Category=="ai"&&row.Kind=="TacticRejected"&&row.TargetId==player
                    &&Text(row,"command",out string actual)&&actual==command
                    &&Text(row,"reason",out string reason)&&reason=="cooldown"
                    &&Integer(row,"cooldown",out int cooldown)&&cooldown==beforeCooldown)reject=i;
                if(row.Category=="damage"&&row.Kind=="HitRoll"&&row.TargetId==player)
                {action=i;kind="melee";}
                if(row.Category=="scenario"&&row.Kind==MoveKind
                    &&Text(row,"scheduledActor",out string scheduled)&&scheduled==enemy
                    &&Boolean(row,"forced",out bool forced)&&!forced
                    &&Integer(row,"oldX",out int oldX)&&Integer(row,"oldY",out int oldY)
                    &&Integer(row,"newX",out int newX)&&Integer(row,"newY",out int newY)
                    &&oldX>=0&&oldX<Zone.Width&&oldY>=0&&oldY<Zone.Height
                    &&newX>=0&&newX<Zone.Width&&newY>=0&&newY<Zone.Height
                    &&Math.Max(Math.Abs(newX-oldX),Math.Abs(newY-oldY))==1)
                {action=i;kind="move";}
            }
            if(reject<0||action<=reject)return false;
            result=new TurnEvidence{BeginTrace=rows[turn.Start].TraceId,EndTrace=rows[turn.End].TraceId,
                ActionTrace=rows[action].TraceId,Kind=kind,Cooldown=afterCooldown};return true;
        }

        public static int CountEnds(IReadOnlyList<Diag.Entry> rows,string marker,string player,string enemy,string actor)
        {
            if((actor!=player&&actor!=enemy)||!Window(rows,marker,player,enemy,out var window))return -1;
            string channel=actor==player?"turn":"turn-verbose";int count=0;
            for(int i=window.Start+1;i<window.End;i++)
                if(rows[i].Category==channel&&rows[i].Kind=="End"&&rows[i].ActorId==actor)count++;
            return count;
        }
        public static bool EnergyMatches(int before,int after,int elapsedTicks,int speed,int completedTurns)
        {
            if(before<0||after<0||elapsedTicks<0||speed<=0||completedTurns<0)return false;
            long expected=(long)before+(long)elapsedTicks*speed-(long)completedTurns*TurnManager.ActionThreshold;
            return expected==after;
        }

        /// <summary>One input can also spend one automatic blocked player turn.
        /// The native player has no Brain zone: its automatic End lacks the zone
        /// needed by cosmetic ambience, although energy/cooldowns still advance.</summary>
        public static bool TryPaidPlayerInput(IReadOnlyList<Diag.Entry> rows,string marker,string player,string enemy,
            int before,int after,int ticks,int speed,int ambientDelta,bool scheduledPlayerHasZone,out int turns)
        {
            turns=0;if(!Window(rows,marker,player,enemy,out var window)||!rows[window.Start].Turn.HasValue)return false;
            bool began=false;int beginTick=-1,lastTick=rows[window.Start].Turn.Value;
            for(int i=window.Start+1;i<window.End;i++)
            {
                var row=rows[i];if(row.ActorId!=player||row.Category!="turn"||(row.Kind!="Begin"&&row.Kind!="End"))continue;
                if(!row.Turn.HasValue||row.Turn.Value<lastTick||row.Turn.Value>(long)rows[window.Start].Turn.Value+ticks)return false;
                lastTick=row.Turn.Value;
                if(row.Kind=="Begin")
                {if(turns==0||began)return false;began=true;beginTick=row.Turn.Value;}
                else
                {
                    if(turns==0){if(began||row.Turn!=rows[window.Start].Turn)return false;}
                    else if(!began||row.Turn!=beginTick)return false;
                    if(++turns>2)return false;began=false;
                }
            }
            // A second blocked turn ends without another Begin before yielding.
            if(turns<1||(turns==2&&began))return false;
            int expectedAmbient=scheduledPlayerHasZone?turns:1;
            return ambientDelta==expectedAmbient&&EnergyMatches(before,after,ticks,speed,turns);
        }

        /// <summary>The native freeze window can contain the tactic and its next
        /// cooldown fallback. Require both exact completed turns and the final
        /// twice-ticked cooldown; never infer a fallback from rejection alone.</summary>
        public static bool TryTacticThenCooldownFallback(IReadOnlyList<Diag.Entry> rows,string marker,string player,string enemy,
            string command,int afterCooldown,out TurnEvidence result)
        {
            result=default;
            if(!Window(rows,marker,player,enemy,out var window)||!EnemyTurns(rows,window,enemy,out var turns)||turns.Count!=2
                ||!TryTacticTurn(rows,marker,player,enemy,command,out var used)
                ||used.BeginTrace!=rows[turns[0].Start].TraceId||used.Cooldown<2||afterCooldown!=used.Cooldown-2)return false;
            var second=new List<Diag.Entry>{rows[window.Start]};
            for(int i=turns[1].Start;i<=turns[1].End;i++)second.Add(rows[i]);
            return TryCooldownFallback(second,marker,player,enemy,command,used.Cooldown-1,afterCooldown,out result);
        }

        public static bool CurrentCalm(Entity owner,Zone zone,NoFightGoal exactGoal)
        {
            if(owner==null||zone==null||exactGoal==null||owner.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(owner))return false;
            var cell=zone.GetEntityCell(owner);var brain=owner.GetPart<BrainPart>();
            return cell!=null&&cell.Objects.Contains(owner)&&brain!=null&&ReferenceEquals(brain.ParentEntity,owner)
                &&ReferenceEquals(brain.CurrentZone,zone)&&ReferenceEquals(brain.PeekGoal(),exactGoal)
                &&ReferenceEquals(exactGoal.ParentBrain,brain)&&!exactGoal.Wander
                &&exactGoal.Duration==Spellcraft_Calm.CALM_DURATION&&exactGoal.Age>=0&&!exactGoal.Finished();
        }

        public static bool OwnsAbility(Entity owner,Guid originalID,string command,out ActivatedAbility ability)
        {
            ability=null;
            if(owner==null||originalID==Guid.Empty||string.IsNullOrEmpty(command))return false;
            var skills=owner.GetPart<SkillsPart>();var abilities=owner.GetPart<ActivatedAbilitiesPart>();
            if(skills==null||abilities==null||skills.ParentEntity!=owner||abilities.ParentEntity!=owner)return false;
            var matched=skills.SkillList.Where(s=>s!=null&&s.ActivatedAbilityID==originalID).ToArray();
            var entries=abilities.AbilityList.Where(a=>a!=null&&(a.ID==originalID||a.Command==command)).ToArray();
            if(matched.Length!=1||entries.Length!=1||matched[0].ParentEntity!=owner
                ||entries[0].ID!=originalID||entries[0].Command!=command)return false;
            var spec=matched[0].DeclareActivatedAbility(owner);
            if(spec==null||spec.Command!=command||entries[0].Range!=spec.Range||entries[0].MaxCooldown!=spec.Cooldown)return false;
            ability=entries[0];return true;
        }

        static bool Window(IReadOnlyList<Diag.Entry> rows,string marker,string player,string enemy,out Slice result)
        {
            result=default;
            if(rows==null||string.IsNullOrEmpty(marker)||string.IsNullOrEmpty(player)||string.IsNullOrEmpty(enemy)||player==enemy)return false;
            int start=-1;
            for(int i=0;i<rows.Count;i++)if(rows[i].TraceId==marker)
            {
                if(start>=0||rows[i].Category!="scenario"||rows[i].Kind!=MarkerKind
                    ||rows[i].ActorId!=player||rows[i].TargetId!=enemy)return false;
                start=i;
            }
            if(start<0)return false;int end=rows.Count;
            for(int i=start+1;i<rows.Count;i++)if(rows[i].Category=="scenario"&&rows[i].Kind==MarkerKind){end=i;break;}
            var traces=new HashSet<string>(StringComparer.Ordinal);
            for(int i=start;i<end;i++)if(string.IsNullOrEmpty(rows[i].TraceId)||!traces.Add(rows[i].TraceId))return false;
            result=new Slice{Start=start,End=end};return true;
        }
        static bool EnemyTurns(IReadOnlyList<Diag.Entry> rows,Slice window,string enemy,out List<Slice> turns)
        {
            turns=new List<Slice>();int begin=-1;
            for(int i=window.Start+1;i<window.End;i++)
            {
                var row=rows[i];if(row.ActorId!=enemy||row.Category!="turn-verbose")continue;
                if(row.Kind=="Begin"){if(begin>=0)return false;begin=i;}
                if(row.Kind=="End"){if(begin<0)return false;turns.Add(new Slice{Start=begin,End=i});begin=-1;}
            }
            return begin<0;
        }
        static JToken Value(Diag.Entry row,string name)
        {
            try{return string.IsNullOrEmpty(row.PayloadJson)?null:JObject.Parse(row.PayloadJson)[name];}
            catch(Newtonsoft.Json.JsonException){return null;}
        }
        static bool Text(Diag.Entry row,string name,out string value)
        {var token=Value(row,name);value=token?.Type==JTokenType.String?(string)token:null;return value!=null;}
        static bool Integer(Diag.Entry row,string name,out int value)
        {
            value=0;var token=Value(row,name);if(token?.Type!=JTokenType.Integer)return false;
            try{value=token.Value<int>();return true;}catch(Exception e)when(e is OverflowException||e is FormatException){return false;}
        }
        static bool Boolean(Diag.Entry row,string name,out bool value)
        {var token=Value(row,name);value=token?.Type==JTokenType.Boolean&&(bool)token;return token?.Type==JTokenType.Boolean;}
    }
}
