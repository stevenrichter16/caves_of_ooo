using System;

namespace CavesOfOoo.Core
{
    public enum WorldAudioCombatFamily { Generic, Cutting, Piercing, Blunt, Natural }

    /// <summary>Copied selected weapon and contact for one actual strike. Not a save record or a gameplay permission.</summary>
    public readonly struct WorldAudioCombatAttempt
    {
        public readonly Entity Attacker,Defender;
        public readonly Zone Zone;
        public readonly Point Contact;
        public readonly WorldAudioCombatFamily Family;
        public readonly bool TargetWasVisible;
        public bool IsValid=>Attacker!=null&&Defender!=null&&Zone!=null;
        internal WorldAudioCombatAttempt(Entity attacker,Entity defender,Zone zone,Point contact,WorldAudioCombatFamily family,bool visible)
        {Attacker=attacker;Defender=defender;Zone=zone;Contact=contact;Family=family;TargetWasVisible=visible;}
    }

    /// <summary>Swings and defenses have zero damage. An impact is emitted at the HP decrement, before death removes the owner.</summary>
    public readonly struct WorldAudioCombatReceipt
    {
        public readonly WorldAudioCombatAttempt Attempt;
        public readonly WorldAudioCue Cue;
        public readonly int ActualDamage;
        public bool IsImpact=>ActualDamage>0;
        public bool IsDefense=>Cue==WorldAudioCue.Parry||Cue==WorldAudioCue.Guard;
        internal WorldAudioCombatReceipt(WorldAudioCombatAttempt attempt,WorldAudioCue cue,int damage)
        {Attempt=attempt;Cue=cue;ActualDamage=damage;}
    }

    /// <summary>Presentation-only per-weapon receipts. Uses no simulation RNG; observer failures cannot undo combat.</summary>
    public static class WorldAudioCombat
    {
        public static Action<WorldAudioCombatReceipt> Callback {get;set;}
        public static WorldAudioCombatFamily Classify(Entity attacker,MeleeWeaponPart weapon)
        {
            if(weapon==null)return WorldAudioCombatFamily.Natural;
            var attributes=weapon.Attributes;
            if(Token(attributes,"Animal")||Token(attributes,"Unarmed"))return WorldAudioCombatFamily.Natural;
            if(Token(attributes,"Cutting"))return WorldAudioCombatFamily.Cutting;
            if(Token(attributes,"Piercing"))return WorldAudioCombatFamily.Piercing;
            if(Token(attributes,"Bludgeoning")||Token(attributes,"Cudgel"))return WorldAudioCombatFamily.Blunt;
            return weapon.ParentEntity==attacker?WorldAudioCombatFamily.Natural:WorldAudioCombatFamily.Generic;
        }
        // Match Damage.AddAttributes' ordinal, space-separated tokens without allocating a split array.
        static bool Token(string text,string token)
        {
            if(string.IsNullOrEmpty(text))return false;
            int start=0;
            while((start=text.IndexOf(token,start,StringComparison.Ordinal))>=0)
            {
                int end=start+token.Length;
                if((start==0||text[start-1]==' ')&&(end==text.Length||text[end]==' '))return true;
                start=end;
            }
            return false;
        }
        public static WorldAudioCombatAttempt Begin(Entity attacker,Entity defender,MeleeWeaponPart weapon,Zone zone)
        {
            if(attacker==null||defender==null||zone==null)return default;
            var source=zone.GetEntityCell(attacker);var contact=zone.GetEntityCell(defender);
            if(source==null||contact==null||!source.Objects.Contains(attacker)||!contact.Objects.Contains(defender))return default;
            var attempt=new WorldAudioCombatAttempt(attacker,defender,zone,new Point(contact.X,contact.Y),Classify(attacker,weapon),
                contact.Explored&&contact.IsVisible&&defender.GetPart<RenderPart>()?.Visible==true);
            Emit(new WorldAudioCombatReceipt(attempt,Swing(attempt.Family),0));return attempt;
        }
        /// <summary>Called only by the typed melee damage core after a positive actual HP decrement. Generic damage never supplies an attempt.</summary>
        public static void Impact(WorldAudioCombatAttempt attempt,int actualDamage)
        {if(attempt.IsValid&&actualDamage>0)Emit(new WorldAudioCombatReceipt(attempt,Hit(attempt.Family),actualDamage));}
        /// <summary>One actual resolved defense, before riposte or guarded damage.
        /// A successful guarded parry is only a parry; no fallback sound is layered on it.</summary>
        public static void Defense(WorldAudioCombatAttempt attempt,bool parried,bool guarded)
        {
            if(attempt.IsValid&&(parried||guarded))
                Emit(new WorldAudioCombatReceipt(attempt,parried?WorldAudioCue.Parry:WorldAudioCue.Guard,0));
        }
        static void Emit(WorldAudioCombatReceipt receipt)
        {try{Callback?.Invoke(receipt);}catch(Exception){/* Optional audio cannot alter an already committed action. */}}
        static WorldAudioCue Swing(WorldAudioCombatFamily family)
        {
            switch(family)
            {
                case WorldAudioCombatFamily.Cutting:case WorldAudioCombatFamily.Piercing:return WorldAudioCue.SwingBlade;
                case WorldAudioCombatFamily.Blunt:return WorldAudioCue.SwingBlunt;
                case WorldAudioCombatFamily.Natural:return WorldAudioCue.SwingNatural;
                default:return WorldAudioCue.Swing;
            }
        }
        static WorldAudioCue Hit(WorldAudioCombatFamily family)
        {
            switch(family)
            {
                case WorldAudioCombatFamily.Cutting:return WorldAudioCue.HitCutting;
                case WorldAudioCombatFamily.Piercing:return WorldAudioCue.HitPiercing;
                case WorldAudioCombatFamily.Blunt:return WorldAudioCue.HitBlunt;
                case WorldAudioCombatFamily.Natural:return WorldAudioCue.HitNatural;
                default:return WorldAudioCue.Hit;
            }
        }
    }
}
