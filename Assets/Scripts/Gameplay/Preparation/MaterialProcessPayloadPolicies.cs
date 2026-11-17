using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>A Part's explicit processing contract. Implementations must be
    /// pure: validate before creation, then retain immutable live-state evidence.
    /// Register a policy for a new Part instead of editing the transaction engine.</summary>
    public interface IMaterialProcessPartPolicy
    {
        bool ValidTemplate(IReadOnlyDictionary<string,string> parameters);
        bool MatchesTemplate(Part part,IReadOnlyDictionary<string,string> parameters);
        object Capture(Part part);
        bool Matches(Part part,object state);
    }
    /// <summary>An explicit contract for an extra Part attached to one input.
    /// Output creation still requires the stricter authored-template admission.</summary>
    public interface IMaterialProcessInstancePartPolicy : IMaterialProcessPartPolicy
    { bool AllowsInstance(Part part); }
    /// <summary>Separate opt-in proof that construction/Initialize/ObjectCreated
    /// create no external inventory, currency or world side effects.</summary>
    public interface IMaterialProcessCreationPartPolicy : IMaterialProcessPartPolicy
    { bool AllowsCreation { get; } }
    public static class MaterialProcessPayloadPolicies
    {
        static readonly Dictionary<Type,IMaterialProcessPartPolicy> Policies=new Dictionary<Type,IMaterialProcessPartPolicy>();
        static MaterialProcessPayloadPolicies()
        {
            foreach(var type in new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart),typeof(CommercePart),
                typeof(StackerPart),typeof(WeaponComponentPart),typeof(MaterialPart),typeof(CompositionPart),
                typeof(PreparationRecipePart),typeof(MaterialProcessInputPart)})Policies[type]=new ScalarPolicy(type);
        }
        public static void Register(Type partType,IMaterialProcessPartPolicy policy)
        {if(partType==null || !typeof(Part).IsAssignableFrom(partType) || policy==null)throw new ArgumentException("A Part and explicit policy are required.");Policies[partType]=policy;}
        public static bool Unregister(Type type,IMaterialProcessPartPolicy expected)
        {return type!=null && Policies.TryGetValue(type,out var current) && current==expected && Policies.Remove(type);}
        internal static IMaterialProcessPartPolicy Get(Type type)=>Policies.TryGetValue(type,out var policy)?policy:null;

        internal static bool CanSplitProcessingInput(Entity owner)
            => !owner.HasPart<MaterialProcessInputPart>() || owner.Statistics?.Count==0
                && owner.Parts.All(p=>p!=null && Get(p.GetType()) is ScalarPolicy);
        internal static bool CanMergeProcessingInputs(Entity first,Entity second)
        {
            if(!first.HasPart<MaterialProcessInputPart>() && !second.HasPart<MaterialProcessInputPart>())return true;
            if(!first.HasPart<MaterialProcessInputPart>() || !second.HasPart<MaterialProcessInputPart>()
                || !CanSplitProcessingInput(first) || !CanSplitProcessingInput(second) || first.Parts.Count!=second.Parts.Count)return false;
            bool Same<K,V>(IDictionary<K,V> a,IDictionary<K,V> b)=>a!=null&&b!=null&&a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var v)&&EqualityComparer<V>.Default.Equals(p.Value,v));
            if(!Same(first.Tags,second.Tags)||!Same(first.Properties,second.Properties)||!Same(first.IntProperties,second.IntProperties))return false;
            for(int i=0;i<first.Parts.Count;i++)
            {var left=first.Parts[i];var right=second.Parts[i];var policy=Get(left.GetType());if(left.GetType()!=right.GetType()||!policy.Matches(right,policy.Capture(left)))return false;}
            return true;
        }

        // A bounded adapter for the existing simple data Parts. This is not a
        // general object serializer: only scalar authoring fields are supported.
        sealed class ScalarPolicy : IMaterialProcessCreationPartPolicy
        {
            public bool AllowsCreation => true;
            readonly Type type; readonly FieldInfo[] fields;
            public ScalarPolicy(Type type)
            {
                this.type=type;
                fields=type.GetFields(BindingFlags.Public|BindingFlags.Instance).Where(f=>f.Name!="ParentEntity"
                    && !(type==typeof(PhysicsPart)&&(f.Name=="InInventory"||f.Name=="Equipped"))
                    && !(type==typeof(StackerPart)&&f.Name=="StackCount")
                    && f.Name!="MaterialTags").OrderBy(f=>f.Name,StringComparer.Ordinal).ToArray();
            }
            Part Template(IReadOnlyDictionary<string,string> parameters)
            {
                var part=(Part)Activator.CreateInstance(type);
                foreach(var pair in parameters)
                {
                    var field=type.GetField(pair.Key,BindingFlags.Public|BindingFlags.Instance);
                    var property=type.GetProperty(pair.Key,BindingFlags.Public|BindingFlags.Instance);
                    var target=field?.FieldType??(property?.CanWrite==true?property.PropertyType:null);
                    if(target==null || pair.Key=="ParentEntity")return null;
                    object value;
                    if(target==typeof(string))value=pair.Value;
                    else if(target.IsEnum)value=Enum.Parse(target,pair.Value,true);
                    else if(target==typeof(int)||target==typeof(float)||target==typeof(double)||target==typeof(bool))value=Convert.ChangeType(pair.Value,target,CultureInfo.InvariantCulture);
                    else return null;
                    if(value is float f && (float.IsNaN(f)||float.IsInfinity(f)) || value is double d && (double.IsNaN(d)||double.IsInfinity(d)))return null;
                    if(field!=null)field.SetValue(part,value);else property.SetValue(part,value);
                }
                return part;
            }
            public bool ValidTemplate(IReadOnlyDictionary<string,string> parameters)
            {try{return parameters!=null && Template(parameters)!=null;}catch{return false;}}
            public bool MatchesTemplate(Part part,IReadOnlyDictionary<string,string> parameters)
            {
                try
                {
                    var expected=Template(parameters);if(expected==null || part?.GetType()!=type)return false;
                    if(!fields.All(f=>Equals(f.GetValue(part),f.GetValue(expected))))return false;
                    if(part is MaterialPart material)
                        return material.MaterialTags.SetEquals((material.MaterialTagsRaw??"").Split(',').Select(s=>s.Trim()).Where(s=>s.Length>0));
                    return true;
                }catch{return false;}
            }
            public object Capture(Part part)=>new ScalarState(fields.Select(f=>f.GetValue(part)).ToArray(),
                part is MaterialPart material?material.MaterialTags.OrderBy(s=>s,StringComparer.Ordinal).ToArray():null);
            public bool Matches(Part part,object state)
            {
                if(part?.GetType()!=type || state is not ScalarState saved)return false;
                return fields.Select(f=>f.GetValue(part)).SequenceEqual(saved.Values)
                    && (saved.MaterialTags==null || part is MaterialPart material && material.MaterialTags.SetEquals(saved.MaterialTags));
            }
            sealed class ScalarState
            {internal readonly object[] Values;internal readonly string[] MaterialTags;internal ScalarState(object[] values,string[] tags){Values=values;MaterialTags=tags;}}
        }
    }

    /// <summary>One captured, admitted blueprint and its Part-owned policies.
    /// No outputs are created while validating this template.</summary>
    internal sealed class MaterialProcessTemplate
    {
        readonly EntityFactory factory;readonly Blueprint blueprint;readonly string name;readonly bool requireCreationSafe;
        readonly Dictionary<string,string> tags,props;readonly Dictionary<string,int> ints;
        readonly Dictionary<string,(string Name,string Text,int Value,int Min,int Max,int Boost)> stats;
        static Dictionary<string,(string,string,int,int,int,int)> StatValues(Blueprint bp)=>bp.Stats.ToDictionary(p=>p.Key,p=>(p.Value.Name,p.Value.sValue,p.Value.Value,p.Value.Min,p.Value.Max,p.Value.Boost));
        static bool Same<K,V>(IDictionary<K,V> a,IDictionary<K,V> b)=>a!=null && b!=null && a.Count==b.Count && a.All(p=>b.TryGetValue(p.Key,out var v)&&EqualityComparer<V>.Default.Equals(p.Value,v));
        readonly Dictionary<string,Dictionary<string,string>> parameters;
        readonly Dictionary<string,(Type type,IMaterialProcessPartPolicy policy)> policies;
        MaterialProcessTemplate(EntityFactory factory,Blueprint blueprint,
            Dictionary<string,(Type,IMaterialProcessPartPolicy)> policies,bool requireCreationSafe)
        {
            this.requireCreationSafe=requireCreationSafe;
            this.factory=factory;this.blueprint=blueprint;name=blueprint.Name;this.policies=policies;
            parameters=blueprint.Parts.ToDictionary(p=>p.Key,p=>new Dictionary<string,string>(p.Value));
            tags=new Dictionary<string,string>(blueprint.Tags);props=new Dictionary<string,string>(blueprint.Props);ints=new Dictionary<string,int>(blueprint.IntProps);stats=StatValues(blueprint);
        }
        internal static MaterialProcessTemplate Capture(EntityFactory factory,string name,bool requireCreationSafe=false)
        {
            if(factory?.Blueprints==null || !factory.Blueprints.TryGetValue(name,out var bp) || bp?.Parts==null || bp.Name!=name || bp.Tags==null || bp.Props==null || bp.IntProps==null || bp.Stats==null || bp.Stats.Values.Any(v=>v==null))return null;
            var policies=new Dictionary<string,(Type,IMaterialProcessPartPolicy)>();
            foreach(var row in bp.Parts)
            {
                if(row.Value==null || !factory.TryGetRegisteredPartType(row.Key,out var type))return null;
                var policy=MaterialProcessPayloadPolicies.Get(type);
                if(policy==null || !policy.ValidTemplate(row.Value) || requireCreationSafe && (policy is not IMaterialProcessCreationPartPolicy creation || !creation.AllowsCreation))return null;
                policies.Add(row.Key,(type,policy));
            }
            return new MaterialProcessTemplate(factory,bp,policies,requireCreationSafe);
        }
        internal bool Current()
        {
            if(factory.Blueprints==null || !factory.Blueprints.TryGetValue(name,out var now) || now!=blueprint || now.Name!=name || now.Parts?.Count!=parameters.Count
                || !Same(now.Tags,tags) || !Same(now.Props,props) || !Same(now.IntProps,ints) || now.Stats==null || now.Stats.Values.Any(v=>v==null) || !Same(StatValues(now),stats))return false;
            foreach(var row in parameters)
                if(!now.Parts.TryGetValue(row.Key,out var p) || p==null || p.Count!=row.Value.Count || !row.Value.All(k=>p.TryGetValue(k.Key,out var v)&&v==k.Value)
                    || !factory.TryGetRegisteredPartType(row.Key,out var type) || type!=policies[row.Key].type
                    || MaterialProcessPayloadPolicies.Get(type)!=policies[row.Key].policy
                    || requireCreationSafe && (policies[row.Key].policy is not IMaterialProcessCreationPartPolicy creation || !creation.AllowsCreation))return false;
            return true;
        }
        internal bool Matches(Entity owner,bool allowInstances=false)
        {
            if(!Current() || owner?.BlueprintName!=name || owner.Parts.Count<parameters.Count
                || owner.Parts.Any(p=>p==null||p.ParentEntity!=owner) || owner.GetPart<PhysicsPart>()?.Weight<0
                || !Same(owner.Properties,props) || !Same(owner.IntProperties,ints) || owner.Statistics==null || owner.Statistics.Count!=stats.Count)return false;
            foreach(var row in stats)
            {
                var expected=row.Value;
                if(!owner.Statistics.TryGetValue(row.Key,out var stat) || stat==null || stat.Owner!=owner || stat.Name!=expected.Name || stat.sValue!=expected.Text
                    || stat.BaseValue!=expected.Value || stat.Min!=expected.Min || stat.Max!=expected.Max || stat.Boost!=expected.Boost || stat.Bonus!=0 || stat.Penalty!=0)return false;
            }
            var expectedTags=new Dictionary<string,string>(tags);
            foreach(var material in owner.Parts.OfType<MaterialPart>())foreach(var tag in material.MaterialTags)expectedTags[tag]="";
            if(!Same(owner.Tags,expectedTags))return false;
            foreach(var extra in owner.Parts.Where(p=>!parameters.ContainsKey(p.Name)))
                if(!allowInstances || MaterialProcessPayloadPolicies.Get(extra.GetType()) is not IMaterialProcessInstancePartPolicy policy || !policy.AllowsInstance(extra))return false;
            foreach(var row in parameters)
            {
                var parts=owner.Parts.Where(p=>p.Name==row.Key).ToArray();var expected=policies[row.Key];
                if(parts.Length!=1 || parts[0].GetType()!=expected.type || !expected.policy.MatchesTemplate(parts[0],row.Value))return false;
            }
            return true;
        }
        internal MaterialProcessItemState Snapshot(Entity owner,bool allowInstances=false)=>Matches(owner,allowInstances)?new MaterialProcessItemState(owner,this,allowInstances):null;
    }
    internal sealed class MaterialProcessItemState
    {
        internal readonly Entity Owner;
        readonly MaterialProcessTemplate template;readonly bool allowInstances;readonly string id;readonly Part[] parts;
        readonly object[] values;readonly IMaterialProcessPartPolicy[] policies;readonly Dictionary<string,string> tags;
        internal MaterialProcessItemState(Entity owner,MaterialProcessTemplate template,bool allowInstances)
        {
            Owner=owner;this.template=template;this.allowInstances=allowInstances;id=owner.ID;parts=owner.Parts.ToArray();tags=new Dictionary<string,string>(owner.Tags);
            policies=parts.Select(p=>MaterialProcessPayloadPolicies.Get(p.GetType())).ToArray();
            values=parts.Select((p,i)=>policies[i].Capture(p)).ToArray();
        }
        internal bool Matches()=>Owner.ID==id && Owner.Parts.SequenceEqual(parts) && template.Matches(Owner,allowInstances)
            && Owner.Tags.Count==tags.Count && tags.All(p=>Owner.Tags.TryGetValue(p.Key,out var v)&&v==p.Value)
            && parts.Select((p,i)=>MaterialProcessPayloadPolicies.Get(p.GetType())==policies[i] && policies[i].Matches(p,values[i])).All(v=>v);
    }
}
