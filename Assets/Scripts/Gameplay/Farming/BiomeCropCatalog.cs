using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Immutable species identity shared by placement, guidance and art.
    /// Native CropPart owns growth; catalogue rows never carry saved crop state.</summary>
    public sealed class BiomeCropDefinition
    {
        public string Id { get; }
        public BiomeType Biome { get; }
        public string CropBlueprint { get; }
        public string SeedBlueprint { get; }
        public string YieldBlueprint { get; }
        public string DisplayName { get; }
        public string UseText { get; }
        public string UtilityKind { get; }
        public string ModelStem { get; }
        public int TicksPerStage { get; }
        public int YieldCount { get; }
        public string StageGlyphsRaw { get; }
        public string StageColorsRaw { get; }
        internal BiomeCropDefinition(BiomeCropRow row, BiomeType biome)
        {
            Id=row.Id; Biome=biome; CropBlueprint=row.CropBlueprint; SeedBlueprint=row.SeedBlueprint;
            YieldBlueprint=row.YieldBlueprint; DisplayName=row.DisplayName; UseText=row.UseText;
            UtilityKind=row.UtilityKind; ModelStem=row.ModelStem; TicksPerStage=row.TicksPerStage;
            YieldCount=row.YieldCount; StageGlyphsRaw=row.StageGlyphsRaw; StageColorsRaw=row.StageColorsRaw;
        }
    }
    [Serializable] internal sealed class BiomeCropRow
    {
        public string Id, Biome, CropBlueprint, SeedBlueprint, YieldBlueprint, DisplayName, UseText,
            UtilityKind, ModelStem, StageGlyphsRaw, StageColorsRaw;
        public int TicksPerStage, YieldCount;
    }
    [Serializable] internal sealed class BiomeCropFile { public BiomeCropRow[] Species; }

    /// <summary>One validated content catalogue. Malformed input publishes no partial
    /// roster. Returned lists and definitions are immutable; parsing tests need not
    /// replace process-wide content. Historical biome aliases do not create regions.</summary>
    public static class BiomeCropCatalog
    {
        const string ResourcePath="Content/Data/Farming/BiomeCrops";
        static IReadOnlyList<BiomeCropDefinition> all;
        static IReadOnlyList<string> loadErrors;
        static readonly Dictionary<BiomeType,IReadOnlyList<BiomeCropDefinition>> byBiome=new Dictionary<BiomeType,IReadOnlyList<BiomeCropDefinition>>();
        static readonly Dictionary<string,BiomeCropDefinition> byId=new Dictionary<string,BiomeCropDefinition>(StringComparer.Ordinal);
        static readonly Dictionary<string,BiomeCropDefinition> byBlueprint=new Dictionary<string,BiomeCropDefinition>(StringComparer.Ordinal);
        static readonly HashSet<string> Utilities=new HashSet<string>(new[]{"Cure","Reagent","WaterVessel","Torch","Process","Gas","Light","Status","HeadCover","ThrownStatus","ThrownWeapon","Healing","LiquidVessel","Handwear"},StringComparer.Ordinal);
        public static IReadOnlyList<BiomeCropDefinition> All { get { EnsureLoaded(); return all; } }
        static void EnsureLoaded()
        {
            if(all!=null)return;
            var asset=Resources.Load<TextAsset>(ResourcePath);
            all=Parse(asset?.text,out loadErrors);
            foreach(var row in all)
            {byId.Add(row.Id,row);byBlueprint.Add(row.CropBlueprint,row);byBlueprint.Add(row.SeedBlueprint,row);byBlueprint.Add(row.YieldBlueprint,row);}
            foreach(var group in all.GroupBy(row=>row.Biome))byBiome.Add(group.Key,Array.AsReadOnly(group.ToArray()));
        }
        public static BiomeCropDefinition Find(string id)
        {EnsureLoaded();return id!=null&&byId.TryGetValue(id,out var row)?row:null;}
        public static BiomeCropDefinition ByBlueprint(string blueprint)
        {EnsureLoaded();return blueprint!=null&&byBlueprint.TryGetValue(blueprint,out var row)?row:null;}
        public static IReadOnlyList<BiomeCropDefinition> ForBiome(BiomeType biome)
        {
            EnsureLoaded();
            if(biome==BiomeType.Desert)biome=BiomeType.Beating;
            if(biome==BiomeType.Jungle)biome=BiomeType.Grovelands;
            if(biome==BiomeType.Ruins)biome=BiomeType.Spread;
            return byBiome.TryGetValue(biome,out var rows)?rows:Array.Empty<BiomeCropDefinition>();
        }
        public static IReadOnlyList<BiomeCropDefinition> Parse(string json,out IReadOnlyList<string> errors)
        {
            var issues=new List<string>();var rows=new List<BiomeCropDefinition>();BiomeCropFile file=null;
            try{if(!string.IsNullOrWhiteSpace(json))file=JsonUtility.FromJson<BiomeCropFile>(json);}
            catch(Exception){issues.Add("Malformed biome crop catalogue JSON.");}
            var ids=new HashSet<string>(StringComparer.Ordinal);var blueprints=new HashSet<string>(StringComparer.Ordinal);var models=new HashSet<string>(StringComparer.Ordinal);
            if(file?.Species==null||file.Species.Length==0)issues.Add("Biome crop catalogue has no species.");
            else foreach(var row in file.Species)
            {
                if(row==null||!Token(row.Id)||!Enum.TryParse(row.Biome,out BiomeType biome)||!Canonical(biome)
                    ||!Token(row.CropBlueprint)||!Token(row.SeedBlueprint)||!Token(row.YieldBlueprint)||!Token(row.ModelStem)
                    ||string.IsNullOrWhiteSpace(row.DisplayName)||string.IsNullOrWhiteSpace(row.UseText)||!Utilities.Contains(row.UtilityKind??"")
                    ||row.TicksPerStage<1||row.TicksPerStage>10000||row.YieldCount<1||row.YieldCount>31
                    ||!Stages(row.StageGlyphsRaw,false)||!Stages(row.StageColorsRaw,true))
                {issues.Add("Invalid crop row: "+(row?.Id??"<null>"));continue;}
                if(!ids.Add(row.Id)||!models.Add(row.ModelStem)||!blueprints.Add(row.CropBlueprint)||!blueprints.Add(row.SeedBlueprint)||!blueprints.Add(row.YieldBlueprint))
                {issues.Add("Duplicate crop identity: "+row.Id);continue;}
                rows.Add(new BiomeCropDefinition(row,biome));
            }
            errors=Array.AsReadOnly(issues.ToArray());
            return Array.AsReadOnly(issues.Count==0?rows.ToArray():Array.Empty<BiomeCropDefinition>());
        }
        static bool Canonical(BiomeType biome)=>biome==BiomeType.Cave||(biome>=BiomeType.Spread&&biome<=BiomeType.Stump);
        static bool Token(string value)=>!string.IsNullOrWhiteSpace(value)&&value.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_');
        static bool Stages(string value,bool color)
        {var entries=value?.Split(',');return entries?.Length==3&&entries.All(s=>color?s.Length==2&&s[0]=='&'&&"krgwbmcyKRGWBMCY".IndexOf(s[1])>=0:s.Length==1&&!char.IsWhiteSpace(s[0]));}

        /// <summary>Checks baked blueprint links without instantiating callbacks or stock.
        /// Actual utility behavior is covered separately by command integration tests.</summary>
        public static IReadOnlyList<string> Validate(EntityFactory factory)
        {
            EnsureLoaded();var issues=new List<string>(loadErrors);
            if(factory==null){issues.Add("Missing crop content factory.");return issues.AsReadOnly();}
            foreach(var row in all)
            {
                if(!factory.Blueprints.TryGetValue(row.SeedBlueprint,out var seed)||!factory.Blueprints.TryGetValue(row.CropBlueprint,out var crop)
                    ||!factory.Blueprints.TryGetValue(row.YieldBlueprint,out var yield))
                {issues.Add(row.Id+": missing seed/crop/yield blueprint.");continue;}
                bool Part(Blueprint bp,string part,string field,string value)=>bp.Parts.TryGetValue(part,out var p)&&p.TryGetValue(field,out var v)&&v==value;
                if(!Part(seed,"Seed","CropBlueprint",row.CropBlueprint)||!Part(seed,"Seed","RequireCultivatedSoil","true")
                    ||!Part(crop,"Crop","YieldBlueprint",row.YieldBlueprint)||!Part(crop,"Crop","SeedYieldBlueprint",row.SeedBlueprint)
                    ||!Part(crop,"Crop","HarvestAtMaturity","true")||!Part(crop,"Crop","SeedYieldCount","1")
                    ||!Part(crop,"Crop","TicksPerStage",row.TicksPerStage.ToString())||!Part(crop,"Crop","YieldCount",row.YieldCount.ToString())
                    ||!Part(crop,"Crop","StageGlyphsRaw",row.StageGlyphsRaw)||!Part(crop,"Crop","StageColorsRaw",row.StageColorsRaw)
                    ||!crop.Tags.ContainsKey("Crop")||!Part(crop,"Physics","Takeable","false")||!Part(yield,"Physics","Takeable","true")
                    ||yield.Parts.ContainsKey("Food")||yield.Parts.ContainsKey("Cookable")||yield.Tags.ContainsKey("Creature")
                    ||(yield.Parts.TryGetValue("Tonic",out var tonic)&&tonic.TryGetValue("StatBoost",out var boost)&&!string.IsNullOrEmpty(boost)))
                    issues.Add(row.Id+": crop linkage or non-food utility contract differs from catalogue.");
                string partName=row.UtilityKind switch {"Cure"=>"CureTonic","Reagent"=>"Reagent","WaterVessel"=>"Waterskin","Torch"=>"TorchLight",
                    "Process"=>"BotanicalProcessing","Gas"=>"GasGrenade","Light"=>"LightSource","Status"=>"StatusTonic","ThrownStatus"=>"StatusTonic",
                    "HeadCover"=>"Armor","ThrownWeapon"=>"MeleeWeapon","Healing"=>"Tonic","LiquidVessel"=>"LiquidVessel","Handwear"=>"Armor",_=>""};
                if(!yield.Parts.ContainsKey(partName))issues.Add(row.Id+": missing usable harvest part "+partName+".");
            }
            return issues.AsReadOnly();
        }
    }
}
