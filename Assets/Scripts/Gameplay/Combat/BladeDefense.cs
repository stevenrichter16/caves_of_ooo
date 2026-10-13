using System;
using CavesOfOoo.Skills;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>One defense opportunity after accuracy, before location/penetration.
    /// Queries current actor/body state without shared attack scratch or turn events.</summary>
    public static class BladeDefense
    {
        public const int GuardBonus = 30;
        public static int DeflectChance(int rank) => rank == 1 ? 15 : rank == 2 ? 22 : rank >= 3 ? 28 : 0;

        public static bool CanReact(Entity actor, Entity opponent, Zone zone)
            => actor != opponent && Live(actor,zone) && Live(opponent,zone)
            && SpatialQuery.Distance(zone,actor,opponent)==1
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true
            && AIHelpers.TryGetVisibleTargetCell(actor,opponent,zone,actor.GetPart<BrainPart>()?.SightRadius ?? 10,out _);

        static bool Live(Entity actor,Zone zone) => actor!=null && zone!=null && actor.SpatialZone==zone
            && zone.GetEntityCell(actor)!=null && actor.GetStatValue("Hitpoints")>0 && !CombatSystem.IsDeathHandled(actor);

        public static MeleeWeaponPart FindLongBlade(Entity actor)
        {
            var root=actor?.GetPart<Body>()?.GetBody();
            return root==null ? null : Find(actor,root,root);
        }
        static MeleeWeaponPart Find(Entity actor,BodyPart root,BodyPart part)
        {
            var item=part.Equipped;
            if(part.Type=="Hand" && !part.Abstract && !part.IsUnsupported() && item!=null)
            {
                var weapon=item.GetPart<MeleeWeaponPart>(); var physical=item.GetPart<PhysicsPart>();
                var equippable=item.GetPart<EquippablePart>(); var destructible=item.GetPart<DestructiblePart>();
                if(weapon!=null && HasToken(weapon.Attributes,"LongBlades") && physical?.Equipped==actor
                    && weapon.ParentEntity==item && physical.InInventory==null && item.SpatialZone==null
                    && equippable!=null && !(destructible?.Gone??false)
                    && !(destructible?.IsDestroyed??false))
                {
                    int required=RequiredHands(equippable.GetEffectiveSlots());
                    if(required>0 && CountHands(root,item)>=required)return weapon;
                }
            }
            if(part.Parts!=null)foreach(var child in part.Parts)
            { var found=Find(actor,root,child);if(found!=null)return found; }
            return null;
        }
        static int CountHands(BodyPart part,Entity item)
        {
            int count=part.Type=="Hand" && !part.Abstract && !part.IsUnsupported() && part.Equipped==item ? 1 : 0;
            if(part.Parts!=null)foreach(var child in part.Parts)count+=CountHands(child,item);
            return count;
        }
        // Same comma-separated slot contract as EquippablePart, without Split allocations.
        static int RequiredHands(string slots)
        {
            if(string.IsNullOrEmpty(slots))return 1;
            int count=0,start=0;
            for(int i=0;i<=slots.Length;i++)if(i==slots.Length || slots[i]==',')
            {
                int end=i;while(start<end && char.IsWhiteSpace(slots[start]))start++;
                while(end>start && char.IsWhiteSpace(slots[end-1]))end--;
                if(end-start==4 && string.CompareOrdinal(slots,start,"Hand",0,4)==0)count++;
                else return int.MaxValue; // A held blade cannot borrow non-hand support.
                start=i+1;
            }
            return count;
        }
        public static bool HasToken(string attributes,string token)
        {
            if(string.IsNullOrEmpty(attributes))return false;
            int i=0;
            while(i<attributes.Length)
            {
                while(i<attributes.Length && char.IsWhiteSpace(attributes[i]))i++;
                int start=i;while(i<attributes.Length && !char.IsWhiteSpace(attributes[i]))i++;
                if(i-start==token.Length && string.CompareOrdinal(attributes,start,token,0,token.Length)==0)return true;
            }
            return false;
        }
        /// <returns>True only on full parry. Guard fallback belongs solely to this strike.</returns>
        public static bool TryParry(Entity defender,Entity attacker,Zone zone,Random rng,out bool guarded)
        {
            guarded=false;
            int rank=defender?.GetPart<SkillsPart>()?.GetRank(nameof(LongBlades_Deflect))??0;
            var guard=defender?.GetPart<StatusEffectsPart>()?.GetEffect<EnGardeEffect>();
            bool available=guard!=null && !guard.Spent && guard.Duration>0;
            if((rank<=0 && !available) || rng==null || !CanReact(defender,attacker,zone) || FindLongBlade(defender)==null)return false;
            guarded=available;
            if(guarded)
            {
                guard.Spent=true;
                defender.GetPart<StatusEffectsPart>().RemoveEffect(guard);
            }
            int chance=DeflectChance(rank)+(guarded?GuardBonus:0);
            int roll=rng.Next(100);bool parried=roll<chance;
            if(Diag.IsChannelEnabled("damage"))Diag.Record(category:"damage",kind:"BladeDefense",actor:defender,target:attacker,
                payload:new { chance,roll,guarded,parried,rank });
            if(parried)MessageLog.Add(defender.GetDisplayName()+" parries "+attacker.GetDisplayName()+"'s strike.");
            else if(guarded)MessageLog.Add(defender.GetDisplayName()+" catches part of the strike on a measured guard.");
            return parried;
        }
    }
}
