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
            return root==null ? null : Find(actor,root,root,true) ?? Find(actor,root,root,false);
        }
        static MeleeWeaponPart Find(Entity actor,BodyPart root,BodyPart part,bool primaryOnly)
        {
            if(!primaryOnly || part.Primary || part.DefaultPrimary)
            {
                var weapon=part.Equipped?.GetPart<MeleeWeaponPart>();
                if(weapon!=null && HasToken(weapon.Attributes,"LongBlades") && IsHeldWeapon(actor,weapon))return weapon;
            }
            if(part.Parts!=null)foreach(var child in part.Parts)
            {var found=Find(actor,root,child,primaryOnly);if(found!=null)return found;}
            return null;
        }
        /// <summary>Validate the same selected physical weapon without selecting a replacement.</summary>
        public static bool IsHeldWeapon(Entity actor,MeleeWeaponPart weapon)
        {
            var root=actor?.GetPart<Body>()?.GetBody();var item=weapon?.ParentEntity;
            if(root==null || item==null || item.GetPart<MeleeWeaponPart>()!=weapon || item.SpatialZone!=null)return false;
            var physical=item.GetPart<PhysicsPart>();var equippable=item.GetPart<EquippablePart>();var destructible=item.GetPart<DestructiblePart>();
            if(physical?.Equipped!=actor || physical.InInventory!=null || equippable==null
                || (destructible?.Gone??false) || (destructible?.IsDestroyed??false))return false;
            int required=RequiredHands(equippable.GetEffectiveSlots());
            return required>0 && CountHands(root,item)>=required;
        }
        /// <summary>Resumed melee sequences revalidate contact/action and their original weapon.
        /// Attached natural/default weapons retain their distinct ownership contract.</summary>
        public static bool CanContinueStrike(Entity actor,Entity target,Zone zone,MeleeWeaponPart weapon)
        {
            if(!Live(actor,zone) || !Live(target,zone) || SpatialQuery.Distance(zone,actor,target)!=1
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true)return false;
            if(weapon==null)return true;
            var item=weapon.ParentEntity;
            if(item==actor)return actor.GetPart<MeleeWeaponPart>()==weapon;
            if(IsHeldWeapon(actor,weapon))return true;
            var root=actor.GetPart<Body>()?.GetBody();
            return root!=null && HasNatural(root,item);
        }
        static bool HasNatural(BodyPart part,Entity item)
        {
            if(item==null)return false;
            // Match GatherMeleeWeapons: a non-melee item still permits this hand's
            // natural fallback; supporting slots of a multi-slot item do not.
            if(part._DefaultBehavior==item && part.FirstSlotForDefaultBehavior
                && (part.Equipped==null || (part.FirstSlotForEquipped && part.Equipped.GetPart<MeleeWeaponPart>()==null))
                && !part.Abstract && !part.IsUnsupported())return true;
            if(part.Parts!=null)foreach(var child in part.Parts)if(HasNatural(child,item))return true;
            return false;
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
            if(parried || guarded)
            {
                string defenderName=defender.GetDisplayName();
                bool playerDefender=string.Equals(defenderName,"you",StringComparison.OrdinalIgnoreCase);
                if(parried)
                {
                    string attackerName=attacker.GetDisplayName();
                    string possessive=string.Equals(attackerName,"you",StringComparison.OrdinalIgnoreCase)?"your":attackerName+"'s";
                    MessageLog.Add(defenderName+(playerDefender?" parry ":" parries ")+possessive+" strike.");
                }
                else MessageLog.Add(defenderName+(playerDefender?" catch ":" catches ")+"part of the strike on a measured guard.");
                AsciiFxBus.EmitEventText(zone,defender,parried?"Parried":"Guarded",parried?"&C":"&Y");
            }
            return parried;
        }
    }
}
