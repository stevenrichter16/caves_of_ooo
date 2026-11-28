using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Lossy field replacement of a binding; forge refits keep their refund policy.</summary>
    internal static class FieldBindingActions
    {
        const string Prefix="FieldBinding|";
        internal static bool IsCommand(string command)=>command?.StartsWith(Prefix,StringComparison.Ordinal)==true;
        sealed class Menu { internal readonly Dictionary<string,Quote> Quotes=new Dictionary<string,Quote>(); }
        static readonly ConditionalWeakTable<Entity,Menu> Menus=new ConditionalWeakTable<Entity,Menu>();
        sealed class Payload
        {
            readonly Entity weapon;readonly WeaponAssemblyPart assembly;readonly MeleeWeaponPart melee;readonly RenderPart render;
            readonly string blade,haft,binding,damage,attributes,effects,name;readonly int pen,hit,strength;
            internal Payload(Entity weapon)
            {
                this.weapon=weapon;assembly=weapon.GetPart<WeaponAssemblyPart>();melee=weapon.GetPart<MeleeWeaponPart>();render=weapon.GetPart<RenderPart>();
                blade=assembly.BladeBlueprint;haft=assembly.HaftBlueprint;binding=assembly.BindingBlueprint;
                damage=melee.BaseDamage;attributes=melee.Attributes;effects=melee.OnHitEffectsRaw;pen=melee.PenBonus;hit=melee.HitBonus;strength=melee.MaxStrengthBonus;name=render?.DisplayName;
            }
            internal bool Matches()=>assembly.ParentEntity==weapon && melee.ParentEntity==weapon && (render==null || render.ParentEntity==weapon)
                && assembly.BladeBlueprint==blade && assembly.HaftBlueprint==haft && assembly.BindingBlueprint==binding
                && melee.BaseDamage==damage && melee.Attributes==attributes && melee.OnHitEffectsRaw==effects
                && melee.PenBonus==pen && melee.HitBonus==hit && melee.MaxStrengthBonus==strength && render?.DisplayName==name;
            internal void Restore()
            {
                // Restore only our fields on Parts still owned by this weapon.
                if(assembly.ParentEntity==weapon && weapon.GetPart<WeaponAssemblyPart>()==assembly)assembly.BindingBlueprint=binding;
                if(melee.ParentEntity==weapon && weapon.GetPart<MeleeWeaponPart>()==melee)
                {melee.BaseDamage=damage;melee.Attributes=attributes;melee.OnHitEffectsRaw=effects;melee.PenBonus=pen;melee.HitBonus=hit;melee.MaxStrengthBonus=strength;}
                if(render?.ParentEntity==weapon && weapon.GetPart<RenderPart>()==render)render.DisplayName=name;
            }
        }
        sealed class Quote
        {
            internal readonly Entity Actor,Weapon,Replacement;internal readonly Zone Zone;internal readonly EntityFactory Factory;
            internal readonly MaterialCuttingWorkReceipt Cutting;internal readonly WeaponComponentPart Blade,Haft,Binding;
            internal readonly MaterialProcessItemState ReplacementState;
            internal readonly Payload Before;internal readonly int Count;
            readonly string weaponId,replacementId;readonly Part[] parts;readonly PhysicsPart physics;readonly int weight;readonly Cell cell;
            readonly MaterialProcessTemplate[] definitions;readonly Dictionary<string,string> tags,properties;readonly Dictionary<string,int> ints;
            internal Quote(Entity actor,Entity weapon,Entity replacement,Zone zone,EntityFactory factory,MaterialCuttingWorkReceipt cutting,
                WeaponComponentPart blade,WeaponComponentPart haft,WeaponComponentPart binding,MaterialProcessItemState replacementState,MaterialProcessTemplate[] definitions)
            {
                Actor=actor;Weapon=weapon;Replacement=replacement;Zone=zone;Factory=factory;Cutting=cutting;Blade=blade;Haft=haft;Binding=binding;
                ReplacementState=replacementState;this.definitions=definitions;Before=new Payload(weapon);Count=replacement.GetPart<StackerPart>()?.StackCount??1;
                weaponId=weapon.ID;replacementId=replacement.ID;parts=weapon.Parts.ToArray();physics=weapon.GetPart<PhysicsPart>();weight=physics.Weight;cell=zone.GetEntityCell(actor);
                tags=new Dictionary<string,string>(weapon.Tags);properties=new Dictionary<string,string>(weapon.Properties);ints=new Dictionary<string,int>(weapon.IntProperties);
            }
            static bool Same<K,V>(IDictionary<K,V> a,IDictionary<K,V> b)=>a.Count==b.Count && a.All(p=>b.TryGetValue(p.Key,out var v)&&EqualityComparer<V>.Default.Equals(p.Value,v));
            internal bool Context()=>ForgePart.Factory==Factory && PreparationActions.CurrentActor(Actor,Zone) && Zone.GetEntityCell(Actor)==cell
                && Eligible(Actor,Weapon) && Weapon.ID==weaponId && Weapon.Parts.SequenceEqual(parts) && parts.All(p=>p.ParentEntity==Weapon)
                && Weapon.GetPart<PhysicsPart>()==physics && physics.Weight==weight && Replacement.ID==replacementId
                && Same(Weapon.Tags,tags) && Same(Weapon.Properties,properties) && Same(Weapon.IntProperties,ints)
                && Cutting.Current() && definitions.All(d=>d.Current());
            internal bool Current()=>Context() && Before.Matches() && ReplacementState.Matches()
                && PreparationActions.Carried(Actor,Replacement) && (Replacement.GetPart<StackerPart>()?.StackCount??1)==Count;
        }
        static bool Eligible(Entity actor,Entity weapon)
        {
            if(!PreparationActions.Carried(actor,weapon) || (weapon.GetPart<StackerPart>()?.StackCount??1)!=1
                || weapon.HasEffect<BrokenEffect>() || weapon.GetStat("Hitpoints")!=null && weapon.GetStatValue("Hitpoints")<=0
                || weapon.Parts.Count(p=>p is WeaponAssemblyPart)!=1 || weapon.Parts.Count(p=>p is MeleeWeaponPart)!=1)return false;
            var temper=weapon.GetPart<WeaponTemperPart>();
            return weapon.Parts.Count(p=>p is WeaponTemperPart)<=1 && (temper==null || temper.TemperCount==0 && temper.HpPenaltyTotal==0 && string.IsNullOrEmpty(temper.AppliedSpecsRaw));
        }
        static Quote Create(Entity actor,Entity weapon,Entity replacement,Zone zone)
        {
            var factory=ForgePart.Factory;
            if(factory==null || !PreparationActions.CurrentActor(actor,zone) || !Eligible(actor,weapon)
                || replacement==weapon || !PreparationActions.Carried(actor,replacement)
                || !WeaponForgingService.IsBindingSlot(replacement.GetPart<WeaponComponentPart>()?.Slot))return null;
            var assembly=weapon.GetPart<WeaponAssemblyPart>();if(assembly.BindingBlueprint==replacement.BlueprintName)return null;
            var cutting=MaterialCuttingWorkReceipt.Resolve(actor,weapon,zone);if(cutting==null || cutting.Tool==replacement)return null;
            var owners=new[]{actor,weapon,replacement,cutting.Tool};
            if(owners.Select(e=>e.ID).Distinct(StringComparer.Ordinal).Count()!=owners.Length || owners.Any(e=>string.IsNullOrEmpty(e.ID)))return null;
            var names=new[]{assembly.BladeBlueprint,assembly.HaftBlueprint,assembly.BindingBlueprint,replacement.BlueprintName};
            var definitions=names.Select(n=>MaterialProcessTemplate.Capture(factory,n)).ToArray();if(definitions.Any(d=>d==null))return null;
            var state=definitions[3].Snapshot(replacement,true);if(state==null)return null;
            var blade=MaterialProcessPayloadPolicies.ReadComponentDefinition(factory,names[0]);var haft=MaterialProcessPayloadPolicies.ReadComponentDefinition(factory,names[1]);var binding=MaterialProcessPayloadPolicies.ReadComponentDefinition(factory,names[3]);
            if(blade==null || haft==null || binding==null || !WeaponForgingService.IsBladeSlot(blade.Slot) || !WeaponForgingService.IsHaftSlot(haft.Slot) || !WeaponForgingService.IsBindingSlot(binding.Slot))return null;
            var quote=new Quote(actor,weapon,replacement,zone,factory,cutting,blade,haft,binding,state,definitions);return quote.Current()?quote:null;
        }
        internal static void AddUseRows(Entity weapon,Entity actor,Zone zone,InventoryActionList rows)
        {
            if(actor?.GetPart<InventoryPart>() is not InventoryPart pack)return;
            foreach(var replacement in pack.Objects)
                if(Create(actor,weapon,replacement,zone)!=null)
                    rows.AddAction("describe-binding","replace binding with "+InventoryPart.GetUnitDisplayName(replacement),"description-only",'\0',19);
        }
        internal static bool Handle(Entity weapon,GameEvent e)
        {
            if(e.ID!="GetInventoryActions" && e.ID!="InventoryAction")return true;
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")
            {
                var menu=Menus.GetValue(weapon,_=>new Menu());menu.Quotes.Clear();
                if(actor?.GetPart<InventoryPart>() is not InventoryPart pack)return true;
                foreach(var replacement in pack.Objects.ToArray())
                {
                    var quote=Create(actor,weapon,replacement,zone);if(quote==null)continue;
                    string token=Guid.NewGuid().ToString("N");menu.Quotes[token]=quote;
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction("field-binding","replace binding with "+InventoryPart.GetUnitDisplayName(replacement)+"; old binding destroyed (1 action, cutter kept)",Prefix+token,'\0',19,false,ActionTime.OneAction)
                        .AsRecipe(PackRecipeCategory.EquipmentWork);
                }
                return true;
            }
            string command=e.GetStringParameter("Command");
            if(!IsCommand(command) || !Menus.TryGetValue(weapon,out var current) || !current.Quotes.TryGetValue(command.Substring(Prefix.Length),out var selected))return true;
            current.Quotes.Remove(command.Substring(Prefix.Length));var tx=e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if(selected.Actor!=actor || selected.Zone!=zone || tx==null || !selected.Current()
                || !tx.TryClaimAction(actor) || !PreparationActions.Begin(actor,weapon,zone,tx,command)
                || !PreparationActions.Begin(actor,selected.Replacement,zone,tx,command) || !selected.Cutting.Begin(tx,command)
                || !MaterialProcessService.ReserveInput(weapon,tx) || !MaterialProcessService.ReserveInput(selected.Replacement,tx))return true;
            if(!selected.Current())return true;
            tx.Do(null,selected.Before.Restore);
            if(!MaterialProcessPayment.Spend(actor,selected.Replacement,actor,zone,tx,selected.ReplacementState.Matches))return true;
            weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint=selected.Replacement.BlueprintName;
            WeaponForgingService.ApplyComponentStats(weapon,selected.Blade,selected.Haft,selected.Binding);
            var after=new Payload(weapon);
            bool Final()=>selected.Context() && after.Matches() && selected.ReplacementState.Matches()
                && (selected.Replacement.GetPart<StackerPart>()?.StackCount??1)==Math.Max(1,selected.Count-1)
                && (selected.Count>1?PreparationActions.Carried(actor,selected.Replacement)
                    :selected.Replacement.SpatialZone==null && selected.Replacement.GetPart<PhysicsPart>().InInventory==null && selected.Replacement.GetPart<PhysicsPart>().Equipped==null);
            if(!Final())return true;tx.BeforeCommit(Final);
            PreparationActions.Complete(tx,actor,weapon,command,"You replace the binding. The old binding is cut away and lost; the head and haft are kept.");
            e.Handled=true;return false;
        }
    }
}
