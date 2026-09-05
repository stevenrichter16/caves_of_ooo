using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable deterministic circle audit, followed by a native
    /// keyboard waiting workload. Direct stimuli use the real movement,
    /// status and turn systems; each row has an explicit control.</summary>
    [Scenario(name: "Felling-Site Exposure Audit", category: "World",
        description: "Eight exposure and barrenness controls, then 75 seconds of native waiting and CPU capture.")]
    public sealed class FellingSiteBench : IScenario
    {
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public string RunId { get; private set; }
        public int ProfileDV { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            RunId=Guid.NewGuid().ToString("N");Cases=Failures=0;Audit.Clear();Diag.SetChannel("scenario",true);
            foreach(string bp in new[]{"TepuiStone","FellingBarePosition","FellingScar","SeventhPosition","FlowerField"})
                Require(ctx.Factory.Blueprints.ContainsKey(bp),"blueprint:"+bp);
            foreach(var e in ctx.Zone.GetAllEntities().ToArray()){ctx.Turns.RemoveEntity(e);ctx.Zone.RemoveEntity(e);}
            Require(new FellingSiteBuilder().BuildZone(ctx.Zone,ctx.Factory,ctx.Rng),"site authored");
            var player=ctx.PlayerEntity;player.GetPart<StatusEffectsPart>()?.RemoveAllEffects();
            foreach(string stat in new[]{"DV","Agility","Speed"})
                if(player.GetStat(stat)==null)player.Statistics[stat]=new Stat{Name=stat,BaseValue=stat=="Speed"?100:10,Max=200};
            var hp=player.GetStat("Hitpoints");hp.Max=hp.BaseValue=1000000;hp.Penalty=0;
            ctx.Zone.AddEntity(player,40,12);ctx.Turns.AddEntity(player);ctx.Turns.ProcessUntilPlayerTurn();
            int dv=player.GetStatValue("DV");
            Check("six_and_one",ctx.Zone.GetAllEntities().Count(e=>e.BlueprintName=="FellingBarePosition")==6
                &&ctx.Zone.GetAllEntities().Count(e=>e.HasPart<SeventhPositionPart>())==1);
            Wait(ctx);Check("ordinary_wait_control",player.GetStatValue("DV")==dv&&!Confused(player));
            ctx.Zone.MoveEntity(player,FellingSiteBuilder.SeventhX,FellingSiteBuilder.SeventhY);Wait(ctx);
            Check("point_wait_exposes",Confused(player)&&player.GetStatValue("DV")==dv-2);
            for(int i=0;i<5;i++)Wait(ctx);
            Check("repeat_nonstacking",player.GetStatValue("DV")==dv-2&&player.GetPart<StatusEffectsPart>().GetAllEffects().Count(e=>e is ConfusedEffect)==1);
            Require(MovementSystem.TryMove(player,ctx.Zone,1,0),"leave point");Wait(ctx);Wait(ctx);
            Check("movement_recovers",!Confused(player)&&player.GetStatValue("DV")==dv);
            ctx.Zone.MoveEntity(player,40,5);Wait(ctx);Check("bare_position_control",!Confused(player)&&player.GetStatValue("DV")==dv);
            var flower=ctx.Factory.CreateEntity("FlowerField");Require(flower!=null,"flower");
            Check("bare_rejects_flower",!ctx.Zone.AddEntity(flower,40,5)&&ctx.Zone.GetEntityCell(flower)==null);
            Check("ordinary_accepts_flower",ctx.Zone.AddEntity(flower,41,5));ctx.Zone.RemoveEntity(flower);
            ctx.Zone.MoveEntity(player,FellingSiteBuilder.SeventhX,FellingSiteBuilder.SeventhY);
            ProfileDV=player.GetStatValue("DV");ZoneRenderHooks.MarkFullDirty("FellingSiteBench");
            if(Application.isPlaying)new GameObject("Felling Site Native Audit").AddComponent<FellingSiteBenchPlayer>().Initialize(ctx,this);
        }
        public static bool Confused(Entity actor)=>actor.GetPart<StatusEffectsPart>()?.HasEffect<ConfusedEffect>()==true;
        private static void Wait(ScenarioContext ctx){ctx.Turns.EndTurn(ctx.PlayerEntity,ctx.Zone);ctx.Turns.ProcessUntilPlayerTurn();}
        /// <summary>Append one independently auditable result under this run.</summary>
        public void Check(string name,bool passed)
        {
            Cases++;if(!passed)Failures++;Audit.Add(name+":"+(passed?"PASS":"FAIL"));
            Diag.Record("scenario","FellingSiteAudit",payload:new{runId=RunId,name,passed});
        }
        private static void Require(bool value,string condition)
        {if(!value)throw new InvalidOperationException("Felling-site audit precondition failed: "+condition);}
    }
}
