using System;
using System.Collections.Generic;
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
        public string ActionText { get; }
        public string LossText { get; }
        public int InputCount { get; }
        public IReadOnlyList<string> OutputBlueprints { get; }
        public IReadOnlyList<int> OutputCounts { get; }
        internal MaterialProcessDefinition(MaterialProcessRow row)
        {
            Id=row.Id;SourceBlueprint=row.SourceBlueprint;Mode=row.Mode;WorkKind=row.WorkKind;InputCount=row.InputCount;ActionText=row.ActionText??row.Mode.ToLowerInvariant();LossText=row.LossText??"";
            OutputBlueprints=Array.AsReadOnly((string[])row.OutputBlueprints.Clone());
            OutputCounts=Array.AsReadOnly((int[])row.OutputCounts.Clone());
        }
    }
    [Serializable] internal sealed class MaterialProcessRow
    {
        public string Id,SourceBlueprint,Mode,WorkKind,ActionText,LossText;
        public int InputCount;
        public string[] OutputBlueprints;
        public int[] OutputCounts;
    }
    [Serializable] internal sealed class MaterialProcessFile { public MaterialProcessRow[] Processes; }

    /// <summary>Atomic catalogue only; not a running machine or an inventory mutation API.</summary>
    public static class MaterialProcessRegistry
    {
        const int MaximumUnits=64,MaximumOutputs=2,MaximumProcesses=64;
        static readonly Dictionary<string,MaterialProcessDefinition> Definitions=new Dictionary<string,MaterialProcessDefinition>(StringComparer.Ordinal);
        static bool loaded;
        public static void LoadDefaults()
        {if(loaded)return;var asset=Resources.Load<TextAsset>("Content/Data/MaterialProcesses");InitializeFromJson(asset==null?null:asset.text);}
        public static void ResetForTests(){Definitions.Clear();loaded=false;}
        public static MaterialProcessDefinition Get(string id)
        {LoadDefaults();return id!=null && Definitions.TryGetValue(id,out var found)?found:null;}
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
            if(errors.Count==0)foreach(var pair in staged)Definitions.Add(pair.Key,pair.Value);
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
                foreach(var output in definition.OutputBlueprints)
                    if(!Portable(factory,output))issues.Add(definition.Id+": invalid output "+output);
            }
            return issues.AsReadOnly();
        }
        static bool Portable(EntityFactory factory,string name)
        {
            if(factory?.Blueprints==null || !factory.Blueprints.TryGetValue(name,out var blueprint) || blueprint?.Tags==null || blueprint.Parts==null
                || !blueprint.Tags.ContainsKey("Item") || blueprint.Tags.ContainsKey("Creature") || blueprint.Tags.ContainsKey("Terrain") || blueprint.Tags.ContainsKey("Solid")
                || blueprint.Parts.ContainsKey("SpatialFootprint") || !blueprint.Parts.TryGetValue("Physics",out var physics) || physics==null)return false;
            bool True(string key)=>physics.TryGetValue(key,out var raw)&&bool.TryParse(raw,out bool value)&&value;
            return True("Takeable")&&!True("Solid");
        }
        static bool Valid(MaterialProcessRow row)
        {
            if(row==null || (row.ActionText?.Length??0)>120 || (row.LossText?.Length??0)>200 || !Token(row.Id) || !Token(row.SourceBlueprint)
                || row.Mode!="Dismantle" && row.Mode!="Reclaim" && row.Mode!="Process"
                || row.WorkKind!="ForgeCold" || row.InputCount<1 || row.InputCount>MaximumUnits
                || row.OutputBlueprints==null || row.OutputCounts==null
                || row.OutputBlueprints.Length<1 || row.OutputBlueprints.Length>MaximumOutputs
                || row.OutputBlueprints.Length!=row.OutputCounts.Length)return false;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<row.OutputBlueprints.Length;i++)
                if(!Token(row.OutputBlueprints[i]) || row.OutputBlueprints[i]==row.SourceBlueprint
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
