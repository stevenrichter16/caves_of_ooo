using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class BiomeCropCatalogAdversarialTests : CultivatedCropTestBase
    {
        static string Json=>File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Farming/BiomeCrops.json"));
        [Test] public void ShippedCatalogueAndFactoryAgree_AndParsingDoesNotReplaceSharedContent()
        {
            var shared=BiomeCropCatalog.All;Assert.IsEmpty(BiomeCropCatalog.Validate(Factory));
            var parsed=BiomeCropCatalog.Parse(Json,out var errors);Assert.IsEmpty(errors);Assert.AreEqual(35,parsed.Count);
            Assert.AreSame(shared,BiomeCropCatalog.All);
            foreach(var row in shared){Assert.AreSame(row,BiomeCropCatalog.Find(row.Id));Assert.AreSame(row,BiomeCropCatalog.ByBlueprint(row.CropBlueprint));Assert.AreSame(row,BiomeCropCatalog.ByBlueprint(row.SeedBlueprint));Assert.AreSame(row,BiomeCropCatalog.ByBlueprint(row.YieldBlueprint));}
        }
        [TestCase("null")][TestCase("malformed")][TestCase("empty")][TestCase("id")][TestCase("blueprint")][TestCase("biome")]
        [TestCase("ticks")][TestCase("yield")][TestCase("yield-cap")][TestCase("glyph")][TestCase("color")][TestCase("unsupported-color")]
        [TestCase("use")][TestCase("utility")][TestCase("model")]
        public void MalformedRosterFailsClosedWithoutPartialRowsOrGlobalMutation(string fault)
        {
            var shared=BiomeCropCatalog.All;string json=Json;
            if(fault=="null")json=null;if(fault=="malformed")json="{";if(fault=="empty")json="{\"Species\":[]}";
            if(fault=="id")json=json.Replace("\"Id\": \"Pitchpod\"","\"Id\": \"Claspbean\"");
            if(fault=="blueprint")json=json.Replace("\"PitchpodSeed\"","\"ClaspbeanSeed\"");
            if(fault=="biome")json=json.Replace("\"Biome\": \"Spread\"","\"Biome\": \"Desert\"");
            if(fault=="ticks")json=json.Replace("\"TicksPerStage\": 20","\"TicksPerStage\": 0");
            if(fault=="yield")json=json.Replace("\"YieldCount\": 2","\"YieldCount\": 0");
            if(fault=="yield-cap")json=json.Replace("\"YieldCount\": 2","\"YieldCount\": 32");
            if(fault=="glyph")json=json.Replace(".,;,T",".,;");
            if(fault=="color")json=json.Replace("&g,&g,","&g,");
            if(fault=="unsupported-color")json=json.Replace("&g,&g,","&Z,&g,");
            if(fault=="use")json=json.Replace("\"UseText\":", "\"IgnoredText\":");
            if(fault=="utility")json=json.Replace("\"UtilityKind\": \"Cure\"","\"UtilityKind\": \"PermanentStatFarm\"");
            if(fault=="model")json=json.Replace("\"ModelStem\": \"pitchpod\"","\"ModelStem\": \"claspbean\"");
            var parsed=BiomeCropCatalog.Parse(json,out var errors);Assert.IsEmpty(parsed);Assert.IsNotEmpty(errors);Assert.AreSame(shared,BiomeCropCatalog.All);Assert.AreEqual(35,shared.Count);
        }
        [TestCase("missing")][TestCase("wrong-seed")][TestCase("uncultivated")][TestCase("food")][TestCase("no-utility")][TestCase("stat-farm")][TestCase("not-portable")]
        public void ActualContentValidationRejectsAChangedSpeciesInsteadOfAdvertisingIt(string fault)
        {
            Assert.IsEmpty(BiomeCropCatalog.Validate(Factory));var row=BiomeCropCatalog.Find("Claspbean");
            var seed=Factory.Blueprints[row.SeedBlueprint];var output=Factory.Blueprints[row.YieldBlueprint];
            if(fault=="missing")Factory.Blueprints.Remove(row.SeedBlueprint);
            if(fault=="wrong-seed")seed.Parts["Seed"]["CropBlueprint"]="CandyCarrotCrop";
            if(fault=="uncultivated")seed.Parts["Seed"]["RequireCultivatedSoil"]="false";
            if(fault=="food")output.Parts["Food"]=new Dictionary<string,string>();
            if(fault=="no-utility")output.Parts.Remove("CureTonic");
            if(fault=="stat-farm")output.Parts["Tonic"]["StatBoost"]="Strength:1";
            if(fault=="not-portable")output.Parts["Physics"]["Takeable"]="false";
            Assert.IsNotEmpty(BiomeCropCatalog.Validate(Factory));
        }
        [Test] public void LegacyAliasesDoNotAddExtraRegions_AndExposedListsCannotBeMutated()
        {
            Assert.AreSame(BiomeCropCatalog.ForBiome(BiomeType.Beating),BiomeCropCatalog.ForBiome(BiomeType.Desert));
            Assert.AreSame(BiomeCropCatalog.ForBiome(BiomeType.Grovelands),BiomeCropCatalog.ForBiome(BiomeType.Jungle));
            Assert.AreSame(BiomeCropCatalog.ForBiome(BiomeType.Spread),BiomeCropCatalog.ForBiome(BiomeType.Ruins));
            Assert.IsEmpty(BiomeCropCatalog.ForBiome((BiomeType)999));Assert.Null(BiomeCropCatalog.Find("missing"));Assert.Null(BiomeCropCatalog.ByBlueprint("Player"));
            Assert.Throws<NotSupportedException>(()=>((IList)BiomeCropCatalog.All).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList)BiomeCropCatalog.ForBiome(BiomeType.Spread)).Clear());
        }
    }
}
