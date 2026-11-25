using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Explicit bounded conversions. Material/Composition tags never imply yields.</summary>
    public sealed class MaterialProcessDefinition
    {
        public string Id { get; }
        public string SourceBlueprint { get; }
        public string Mode { get; }
        public string WorkKind { get; }
        public string SourceRecipeId { get; }
        public string ActionText { get; }
        public string LossText { get; }
        public int InputCount { get; }
        public IReadOnlyList<string> OutputBlueprints { get; }
        public IReadOnlyList<int> OutputCounts { get; }
        public IReadOnlyList<string> AdditionalInputBlueprints { get; }
        public IReadOnlyList<int> AdditionalInputCounts { get; }
        internal MaterialProcessDefinition(MaterialProcessRow row)
        {
            Id=row.Id;SourceBlueprint=row.SourceBlueprint;Mode=row.Mode;WorkKind=row.WorkKind;InputCount=row.InputCount;ActionText=row.ActionText??row.Mode.ToLowerInvariant();LossText=row.LossText??"";
            SourceRecipeId=row.SourceRecipeId??"";
            AdditionalInputBlueprints=Array.AsReadOnly((string[])(row.AdditionalInputBlueprints??Array.Empty<string>()).Clone());
            AdditionalInputCounts=Array.AsReadOnly((int[])(row.AdditionalInputCounts??Array.Empty<int>()).Clone());
            OutputBlueprints=Array.AsReadOnly((string[])row.OutputBlueprints.Clone());
            OutputCounts=Array.AsReadOnly((int[])row.OutputCounts.Clone());
        }
    }
    [Serializable] internal sealed class MaterialProcessRow
    {
        public string Id,SourceBlueprint,Mode,WorkKind,ActionText,LossText,SourceRecipeId;
        public int InputCount;
        public string[] OutputBlueprints;
        public int[] OutputCounts;
        public string[] AdditionalInputBlueprints;
        public int[] AdditionalInputCounts;
    }
    [Serializable] internal sealed class MaterialProcessFile { public MaterialProcessRow[] Processes; }

    /// <summary>Atomic catalogue only; not a running machine or an inventory mutation API.</summary>
    public static class MaterialProcessRegistry
    {
        const int MaximumUnits=64,MaximumOutputs=2,MaximumProcesses=64;
        static readonly Dictionary<string,MaterialProcessDefinition> Definitions=new Dictionary<string,MaterialProcessDefinition>(StringComparer.Ordinal);
        // Revoking a recipe cannot grant permission to erase a known input's
        // private state by splitting it before a later catalogue reload.
        static readonly HashSet<string> KnownPayloads=new HashSet<string>(StringComparer.Ordinal);
        static bool loaded;
        public static void LoadDefaults()
        {if(loaded)return;var asset=Resources.Load<TextAsset>("Content/Data/MaterialProcesses");InitializeFromJson(asset==null?null:asset.text);}
        public static void ResetForTests(){Definitions.Clear();KnownPayloads.Clear();loaded=false;}
        /// <summary>The loaded definitions of one work kind, in id order (readers and menus that describe what a station or tool can do).</summary>
        public static System.Collections.Generic.IReadOnlyList<MaterialProcessDefinition> ForWorkKind(string kind)
        {LoadDefaults();return Definitions.Values.Where(d=>d.WorkKind==kind).OrderBy(d=>d.Id,StringComparer.Ordinal).ToArray();}
        public static MaterialProcessDefinition Get(string id)
        {LoadDefaults();return id!=null && Definitions.TryGetValue(id,out var found)?found:null;}
        internal static string[] ForPreparation(Entity source)
        {LoadDefaults();return source==null?Array.Empty<string>():Definitions.Values.Where(d=>d.SourceBlueprint==source.BlueprintName && d.SourceRecipeId.Length>0).Select(d=>d.Id).ToArray();}
        internal static string[] ForReclamation(Entity source)
        {LoadDefaults();return source==null?Array.Empty<string>():Definitions.Values.Where(d=>d.SourceBlueprint==source.BlueprintName && d.SourceRecipeId.Length==0 && d.Mode=="Reclaim" && d.WorkKind=="HandCutting").Select(d=>d.Id).ToArray();}
        internal static MaterialProcessDefinition[] ForSource(Entity source)
        {LoadDefaults();return source==null?Array.Empty<MaterialProcessDefinition>():Definitions.Values.Where(d=>d.SourceBlueprint==source.BlueprintName).OrderBy(d=>d.Id,StringComparer.Ordinal).ToArray();}
        internal static bool IsPayload(Entity owner)
        {
            LoadDefaults();if(owner==null)return false;
            if(owner.HasPart<SiftableStockPart>() || HarvestablePart.Factory?.Blueprints.TryGetValue(owner.BlueprintName,out var bp)==true
                && bp?.Parts?.ContainsKey("SiftableStock")==true)RememberPayload(owner.BlueprintName);
            return owner.HasPart<MaterialProcessInputPart>() || KnownPayloads.Contains(owner.BlueprintName);
        }
        // Classification is sticky within this runtime, including permission
        // removal/revocation. It is never an executable process registration.
        internal static void RememberPayload(string blueprint)
        {if(!string.IsNullOrEmpty(blueprint))KnownPayloads.Add(blueprint);}
        public static IReadOnlyList<string> InitializeFromJson(string json)
        {
            loaded=true;var errors=new List<string>();var staged=new Dictionary<string,MaterialProcessDefinition>(StringComparer.Ordinal);
            MaterialProcessFile file=null;
            try { if(!string.IsNullOrWhiteSpace(json))file=JsonUtility.FromJson<MaterialProcessFile>(json); }
            catch(Exception){errors.Add("Malformed process catalogue.");}
            if(file?.Processes==null || file.Processes.Length<1 || file.Processes.Length>MaximumProcesses)errors.Add("Invalid process catalogue size.");
            else foreach(var row in file.Processes)
            {
                if(!Valid(row)){errors.Add("Invalid finite process: "+(row?.Id??"<null>"));continue;}
                if(staged.ContainsKey(row.Id)){errors.Add("Duplicate process: "+row.Id);continue;}
                staged.Add(row.Id,new MaterialProcessDefinition(row));
            }
            Definitions.Clear();
            if(errors.Count==0)foreach(var pair in staged)
            {
                Definitions.Add(pair.Key,pair.Value);KnownPayloads.Add(pair.Value.SourceBlueprint);
                foreach(var input in pair.Value.AdditionalInputBlueprints)KnownPayloads.Add(input);
            }
            return errors.AsReadOnly();
        }
        // Syntax loading is separate from factory admission. A planner must call
        // this against its current factory before exposing any executable choice.
        public static IReadOnlyList<string> Validate(EntityFactory factory)
        {
            LoadDefaults();var issues=new List<string>();
            if(Definitions.Count==0)issues.Add("No valid process catalogue.");
            if(factory?.Blueprints==null){issues.Add("Missing process content factory.");return issues.AsReadOnly();}
            foreach(var definition in Definitions.Values)
            {
                if(!Portable(factory,definition.SourceBlueprint))issues.Add(definition.Id+": invalid source "+definition.SourceBlueprint);
                if(definition.SourceRecipeId.Length>0)
                {
                    var recipe=PreparationRecipeService.Recipes.FirstOrDefault(r=>r.Id==definition.SourceRecipeId);
                    if(recipe==null || recipe.Input!=definition.SourceBlueprint
                        || !factory.Blueprints.TryGetValue(definition.SourceBlueprint,out var bp)
                        || bp?.Parts==null || !bp.Parts.TryGetValue("PreparationRecipe",out var parameters) || parameters==null
                        || !parameters.TryGetValue("RecipeIds",out var raw) || raw==null || !raw.Split(',').Contains(definition.SourceRecipeId))
                        issues.Add(definition.Id+": invalid source recipe permission");
                }
                foreach(var ingredient in definition.AdditionalInputBlueprints)
                    if(!Portable(factory,ingredient))issues.Add(definition.Id+": invalid ingredient "+ingredient);
                foreach(var output in definition.OutputBlueprints)
                    if(!Portable(factory,output))issues.Add(definition.Id+": invalid output "+output);
            }
            return issues.AsReadOnly();
        }
        internal static bool Portable(EntityFactory factory,string name)
        {
            if(factory?.Blueprints==null || !factory.Blueprints.TryGetValue(name,out var blueprint) || blueprint?.Tags==null || blueprint.Parts==null
                || !blueprint.Tags.ContainsKey("Item") || blueprint.Tags.ContainsKey("Creature") || blueprint.Tags.ContainsKey("Terrain") || blueprint.Tags.ContainsKey("Solid")
                || blueprint.Parts.ContainsKey("SpatialFootprint") || !blueprint.Parts.TryGetValue("Physics",out var physics) || physics==null)return false;
            bool True(string key)=>physics.TryGetValue(key,out var raw)&&bool.TryParse(raw,out bool value)&&value;
            return True("Takeable")&&!True("Solid");
        }
        internal static bool Valid(MaterialProcessRow row)
        {
            if(row==null || (row.ActionText?.Length??0)>120 || (row.LossText?.Length??0)>200 || !Token(row.Id) || !Token(row.SourceBlueprint)
                || row.Mode!="Dismantle" && row.Mode!="Reclaim" && row.Mode!="Process"
                || row.WorkKind!="ForgeCold" && row.WorkKind!="HandCutting" && row.WorkKind!="Screening" || row.InputCount<1 || row.InputCount>MaximumUnits
                || row.OutputBlueprints==null || row.OutputCounts==null
                || row.OutputBlueprints.Length<1 || row.OutputBlueprints.Length>MaximumOutputs
                || row.OutputBlueprints.Length!=row.OutputCounts.Length)return false;
            if(!string.IsNullOrEmpty(row.SourceRecipeId) && !Token(row.SourceRecipeId))return false;
            var inputs=row.AdditionalInputBlueprints??Array.Empty<string>();var counts=row.AdditionalInputCounts??Array.Empty<int>();
            if(inputs.Length!=counts.Length || inputs.Length>2)return false;
            var ingredientNames=new HashSet<string>(StringComparer.Ordinal){row.SourceBlueprint};int total=row.InputCount;
            for(int i=0;i<inputs.Length;i++)
                if(!Token(inputs[i]) || !ingredientNames.Add(inputs[i]) || counts[i]<1 || counts[i]>MaximumUnits || (total+=counts[i])>MaximumUnits)return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<row.OutputBlueprints.Length;i++)
                if(!Token(row.OutputBlueprints[i]) || ingredientNames.Contains(row.OutputBlueprints[i])
                    || !seen.Add(row.OutputBlueprints[i]) || row.OutputCounts[i]<1 || row.OutputCounts[i]>MaximumUnits)return false;
            return true;
        }
        static bool Token(string text)
        {
            if(string.IsNullOrWhiteSpace(text))return false;
            foreach(char c in text)if(!char.IsLetterOrDigit(c) && c!='-' && c!='_')return false;
            return true;
        }
    }
}
