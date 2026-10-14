using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario(name:"Floating Event Text",category:"Combat Stress",
        description:"Real parry and first status onset on a player and NPC; repeats every three seconds for native/ASCII and FX-Off inspection.")]
    public sealed class FloatingEventTextShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.World.RemoveEntitiesWithTag("Creature");
            for(int y=7;y<=17;y++) for(int x=27;x<=53;x++)
            {
                var objects=ctx.Zone.GetCell(x,y).Objects.ToArray();
                foreach(var entity in objects) if(entity!=ctx.PlayerEntity)ctx.Zone.RemoveEntity(entity);
                ctx.Zone.AddEntity(ctx.Factory.CreateEntity("Grass"),x,y);
            }
            ctx.Player.Teleport(35,12).SetHpMax().Equip("LongSword");
            var opponent=ctx.Spawn("MarlbackScrabbler").At(36,12);
            var patient=ctx.Spawn("MarlbackScrabbler").At(46,12);
            if(opponent==null || patient==null)throw new InvalidOperationException("Readout actors could not be placed.");
            foreach(var entity in new[]{opponent,patient})
            {
                var brain=entity.GetPart<BrainPart>(); if(brain!=null)brain.Passive=true;
            }
            if(Application.isPlaying)
            {
                var previous=UnityEngine.Object.FindFirstObjectByType<FloatingEventTextShowcasePlayer>();
                if(previous!=null)UnityEngine.Object.Destroy(previous.gameObject);
                new GameObject("Floating event text showcase").AddComponent<FloatingEventTextShowcasePlayer>()
                    .Initialize(ctx.Zone,ctx.PlayerEntity,opponent,patient);
            }
            ctx.Log("Floating words: parry on the player; stunned, poisoned and burning on the distant scrabbler. Repeats every three seconds. Change graphics or FX mode to compare.");
        }

        /// <summary>Deterministic actual defense/status resolution; presentation
        /// receives ordinary events. Does not tick turns or damage the actors.</summary>
        public static bool Resolve(Zone zone,Entity defender,Entity attacker,Entity patient)
        {
            if(zone==null || !BladeDefense.CanReact(defender,attacker,zone)
                || BladeDefense.FindLongBlade(defender)==null || patient==null
                || patient.SpatialZone!=zone || zone.GetEntityCell(patient)==null
                || patient.GetStatValue("Hitpoints")<=0) return false;
            defender.GetPart<StatusEffectsPart>()?.RemoveEffect<EnGardeEffect>();
            patient.GetPart<StatusEffectsPart>()?.RemoveEffect<StunnedEffect>();
            patient.GetPart<StatusEffectsPart>()?.RemoveEffect<PoisonedEffect>();
            patient.GetPart<StatusEffectsPart>()?.RemoveEffect<BurningEffect>();
            bool guard=defender.ApplyEffect(new EnGardeEffect(),zone:zone);
            bool parry=BladeDefense.TryParry(defender,attacker,zone,new LowestRoll(),out bool guarded);
            bool stun=patient.ApplyEffect(new StunnedEffect(3),zone:zone);
            bool poison=patient.ApplyEffect(new PoisonedEffect(3,"1"),zone:zone);
            bool burning=patient.ApplyEffect(new BurningEffect(),zone:zone);
            return guard && parry && guarded && stun && poison && burning;
        }
        sealed class LowestRoll : System.Random { public override int Next(int maxValue)=>0; }
    }
}
