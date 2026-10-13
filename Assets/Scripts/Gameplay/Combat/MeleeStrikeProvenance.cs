using System;
namespace CavesOfOoo.Core
{
    public enum MeleeStrikeOrigin { Ordinary, ActiveAbility, ExtraAttack, ReactiveCounter, BonusDamage, GuaranteedDamage }

    /// <summary>Runtime causal identity, independent of display text. Reactive ancestry
    /// survives extra-attack descendants; suppression only disables attack-generating hooks.</summary>
    public readonly struct MeleeStrikeProvenance
    {
        public readonly string ActionId, RootStrikeId, ParentStrikeId, StrikeId;
        public readonly MeleeStrikeOrigin Origin;
        public readonly int Depth, CounterDepth;
        public readonly bool SuppressFollowups;
        public bool IsEmpty => ActionId==null;
        MeleeStrikeProvenance(string action,string root,string parent,string strike,MeleeStrikeOrigin origin,int depth,int counters,bool suppress)
        {ActionId=action;RootStrikeId=root;ParentStrikeId=parent;StrikeId=strike;Origin=origin;Depth=depth;CounterDepth=counters;SuppressFollowups=suppress;}
        public static MeleeStrikeProvenance Action(MeleeStrikeOrigin origin)
            => new MeleeStrikeProvenance(Guid.NewGuid().ToString("N"),null,null,null,origin,0,0,false);
        public MeleeStrikeProvenance Child(MeleeStrikeOrigin origin,bool suppress=false)
        {
            if(IsEmpty)return Action(origin).Child(origin,suppress);
            string id=Guid.NewGuid().ToString("N");
            return new MeleeStrikeProvenance(ActionId,RootStrikeId??id,StrikeId,id,origin,
                StrikeId==null?0:Depth+1,CounterDepth+(origin==MeleeStrikeOrigin.ReactiveCounter?1:0),SuppressFollowups||suppress);
        }
    }

    /// <summary>Stack-scoped context, restored on exceptions. Saves must wait until resolved.</summary>
    public readonly struct MeleeStrikeScope : IDisposable
    {
        [ThreadStatic] static MeleeStrikeProvenance current;
        public static MeleeStrikeProvenance Current => current;
        public static bool IsResolving => !current.IsEmpty;
        readonly MeleeStrikeProvenance previous;
        public MeleeStrikeScope(MeleeStrikeProvenance value) {previous=current;current=value;}
        public static MeleeStrikeScope BeginAction(MeleeStrikeOrigin origin)
            => new MeleeStrikeScope(current.IsEmpty?MeleeStrikeProvenance.Action(origin):current.Child(origin));
        public void Dispose() {current=previous;}
    }
}
