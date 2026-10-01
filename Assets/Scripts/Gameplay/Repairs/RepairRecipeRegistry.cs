using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Immutable content contract for one Tier 1 structural fault. Future
    /// work stages and tools can extend recipes without changing saved fault IDs.</summary>
    public sealed class RepairRecipe
    {
        public string Id { get; }
        public string Composition { get; }
        public string MaterialBlueprint { get; }
        public string MaterialName { get; }
        public int Quantity { get; }
        public string Diagnosis { get; }
        public string ActionText { get; }
        public string RepairedText { get; }
        internal RepairRecipe(RepairRecipeRow row)
        {
            Id=row.Id; Composition=row.Composition; MaterialBlueprint=row.MaterialBlueprint;
            MaterialName=row.MaterialName; Quantity=row.Quantity; Diagnosis=row.Diagnosis;
            ActionText=row.ActionText; RepairedText=row.RepairedText;
        }
    }
    [Serializable] internal sealed class RepairRecipeRow
    {
        public string Id, Composition, MaterialBlueprint, MaterialName, Diagnosis, ActionText, RepairedText;
        public int Quantity;
    }
    [Serializable] internal sealed class RepairRecipeFile { public RepairRecipeRow[] Recipes; }

    /// <summary>Atomic JSON recipe catalogue. Invalid catalogues install no recipes;
    /// callers fail closed. Native content loads lazily; tests may inject explicit data.</summary>
    public static class RepairRecipeRegistry
    {
        public const int MaximumMaterialQuantity = 64;
        private static readonly Dictionary<string, RepairRecipe> Recipes = new Dictionary<string, RepairRecipe>(StringComparer.Ordinal);
        private static readonly List<string> Errors = new List<string>();
        private static bool _loaded;
        public static int Count { get { LoadDefaults(); return Recipes.Count; } }
        public static void LoadDefaults()
        {
            if (_loaded) return;
            var asset=Resources.Load<TextAsset>("Content/Data/Repairs/Tier1Repairs");
            InitializeFromJson(asset == null ? null : asset.text);
        }
        public static RepairRecipe Get(string id)
        {
            LoadDefaults(); return id != null && Recipes.TryGetValue(id, out var recipe) ? recipe : null;
        }
        public static IReadOnlyList<string> InitializeFromJson(string json)
        {
            Recipes.Clear(); Errors.Clear(); _loaded=true;
            RepairRecipeFile file=null;
            try { if(!string.IsNullOrWhiteSpace(json)) file=JsonUtility.FromJson<RepairRecipeFile>(json); }
            catch(Exception) { Errors.Add("Malformed repair recipe JSON."); }
            if(file?.Recipes==null || file.Recipes.Length==0) Errors.Add("Repair catalogue contains no recipes.");
            else foreach(var row in file.Recipes)
            {
                if(row==null || !Token(row.Id) || !Token(row.Composition) || !Token(row.MaterialBlueprint)
                    || string.IsNullOrWhiteSpace(row.MaterialName) || string.IsNullOrWhiteSpace(row.Diagnosis)
                    || string.IsNullOrWhiteSpace(row.ActionText) || string.IsNullOrWhiteSpace(row.RepairedText)
                    || row.Quantity<1 || row.Quantity>MaximumMaterialQuantity)
                { Errors.Add("Invalid repair recipe: "+(row?.Id??"<null>")); continue; }
                if(Recipes.ContainsKey(row.Id)) { Errors.Add("Duplicate repair recipe: "+row.Id); continue; }
                Recipes.Add(row.Id,new RepairRecipe(row));
            }
            if(Errors.Count>0)Recipes.Clear();
            return Errors.ToArray();
        }
        private static bool Token(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return false;
            foreach(char c in value)if(!char.IsLetterOrDigit(c) && c!='-' && c!='_')return false;
            return true;
        }
        public static List<string> Validate(EntityFactory factory)
        {
            LoadDefaults(); var issues=new List<string>(Errors);
            if(factory==null) { issues.Add("Missing repair content factory."); return issues; }
            foreach(var recipe in Recipes.Values)
            {
                if(!factory.Blueprints.TryGetValue(recipe.MaterialBlueprint,out var blueprint))
                { issues.Add(recipe.Id+": missing supply "+recipe.MaterialBlueprint); continue; }
                if(blueprint.Tags.ContainsKey("Creature") || !blueprint.Parts.TryGetValue("Physics",out var physical)
                    || !physical.TryGetValue("Takeable",out var raw) || !bool.TryParse(raw,out bool takeable) || !takeable)
                    issues.Add(recipe.Id+": supply must be a portable non-creature.");
            }
            return issues;
        }
        public static void ResetForTests() { Recipes.Clear(); Errors.Clear(); _loaded=false; }
    }
}
