using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Current native state and explicit presenter refresh; this is not a paid-input or camera-pixel witness.
    public sealed class PlantedCropArtTests
    {
        static string Prefix(string bp)=>"spread-environment-"+(bp=="CandyCarrotCrop"?"candy-carrot-crop":"emberwheat-crop")+"-";
        static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f,Entity e)=>SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
        static SpreadEnvironment3DLibrary.Entry Exact(SpawnRing3DIntegrationFixture f,Entity e,int stage,bool wet)
        {
            var native=(SpawnRing3DRecipe)typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{f.Zone,e,f.Library.Definition,null});
            Assert.Null(native.Failure);Assert.NotNull(native.ModelId,"Initial native admission must not strand the real crop in the legacy glyph route.");
            var r=Recipe(f,e);Assert.AreEqual(Prefix(e.BlueprintName)+(stage*2+(wet?1:0)),r.ModelId);Assert.AreSame(e,r.Owner);Assert.True(r.Batched);Assert.False(r.Transient);
            f.Refresh(f.Dirty(e));Assert.True(f.Rendered(e));var entry=SpreadEnvironment3DLibrary.Load().Find(r.ModelId);Assert.NotNull(entry);Assert.Greater(entry.Mesh.vertexCount,24);Assert.GreaterOrEqual(entry.Mesh.uv.Distinct().Count(),2);
            Assert.AreEqual(1,entry.Mesh.subMeshCount);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,entry.Prefab.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.That(entry.Mesh.bounds.size.y,stage==0?Is.InRange(.025f,.15f):Is.InRange(.18f,.65f));return entry;
        }
        [TestCase("CandyCarrotCrop",0,false)][TestCase("CandyCarrotCrop",0,true)][TestCase("CandyCarrotCrop",1,false)][TestCase("CandyCarrotCrop",1,true)]
        [TestCase("EmberwheatCrop",0,false)][TestCase("EmberwheatCrop",0,true)][TestCase("EmberwheatCrop",1,false)][TestCase("EmberwheatCrop",1,true)]
        public void ActualSeedAndSproutUseCurrentDryOrWetGeometry(string blueprint,int stage,bool wet)
        {
            var oldZone=SettlementRuntime.ActiveZone;
            try{using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                SettlementRuntime.ActiveZone=f.Zone;var e=f.Add(blueprint);var c=e.GetPart<CropPart>();Assert.NotNull(c);c.GrowthStage=stage;e.GetPart<RenderPart>().RenderString=c.GlyphForStage(stage).ToString();e.GetPart<RenderPart>().ColorString=c.ColorForStage(stage);if(wet)c.Water(1);
                string id=e.ID;var at=f.Zone.GetEntityPosition(e);Exact(f,e,stage,wet);Assert.AreEqual(id,e.ID);Assert.AreEqual(at,f.Zone.GetEntityPosition(e));Assert.AreEqual(stage,c.GrowthStage);Assert.AreEqual(wet,c.MoistureTicks>0);
            }}finally{SettlementRuntime.ActiveZone=oldZone;}
        }
        [TestCase("CandyCarrotCrop","CandyCarrot")][TestCase("EmberwheatCrop","Emberwheat")]
        public void RealWaterDryGrowthAndMaturityChangeOnlyTheActualCropView(string blueprint,string produce)
        {
            var oldZone=SettlementRuntime.ActiveZone;var oldFactory=CropSystem.Factory;var oldDirty=ZoneRenderHooks.CellDirtyCallback;
            try{using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                SettlementRuntime.ActiveZone=f.Zone;CropSystem.Factory=f.Factory;var e=f.Add(blueprint);var c=e.GetPart<CropPart>();Assert.AreEqual(produce,c.YieldBlueprint);var cell=f.Zone.GetEntityCell(e);var under=cell.Objects.Where(x=>x!=e).ToArray();var notices=new List<string>();ZoneRenderHooks.CellDirtyCallback=(x,y,why)=>{if(x==cell.X&&y==cell.Y)notices.Add(why);};
                var dry=Exact(f,e,0,false).Mesh;c.Water(1);Assert.Contains("CropWatered",notices);var wet=Exact(f,e,0,true).Mesh;CollectionAssert.AreEqual(dry.vertices,wet.vertices);Assert.False(dry.uv.SequenceEqual(wet.uv),"Moisture has a real soil-color difference.");
                CropSystem.OnTickEnd(f.Zone);Assert.Contains("SoilDried",notices);Exact(f,e,0,false);int paused=c.TicksInStage;CropSystem.OnTickEnd(f.Zone);Assert.AreEqual(paused,c.TicksInStage,"Dry crop pauses; no visual growth or death is invented.");
                c.Water(c.TicksPerStage*2+2);for(int i=c.TicksInStage;i<c.TicksPerStage;i++)CropSystem.OnTickEnd(f.Zone);
                Assert.AreEqual(1,c.GrowthStage);Assert.Contains("CropStageAdvanced",notices);var sprout=Exact(f,e,1,true).Mesh;Assert.Greater(sprout.bounds.size.y,dry.bounds.size.y+.05f);
                for(int i=0;i<c.TicksPerStage;i++)CropSystem.OnTickEnd(f.Zone);
                Assert.Contains("CropMatured",notices);Assert.Null(f.Zone.GetEntityCell(e));f.Refresh(SpawnRing3DIntegrationFixture.Dirty(cell.X,cell.Y));Assert.False(f.Rendered(e));Assert.Null(Recipe(f,e).ModelId);
                Assert.AreEqual(c.YieldCount,cell.Objects.Where(x=>!under.Contains(x)&&x.BlueprintName==produce).Sum(x=>x.GetPart<StackerPart>()?.StackCount??1));foreach(var owner in under)Assert.True(cell.Objects.Contains(owner));
            }}finally{ZoneRenderHooks.CellDirtyCallback=oldDirty;CropSystem.Factory=oldFactory;SettlementRuntime.ActiveZone=oldZone;}
        }
        [TestCase("foreign-crop")][TestCase("foreign-spatial")][TestCase("foreign-cell")][TestCase("foreign-zone")]
        [TestCase("glyph")][TestCase("invalid-stage")][TestCase("hidden")][TestCase("removed")]
        public void RefusedCurrentSourcesNeverAcquirePlantedCropGeometry(string fault)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var e=f.Add("CandyCarrotCrop");var c=e.GetPart<CropPart>();var cell=f.Zone.GetEntityCell(e);var zone=f.Zone;var spatial=typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic);var oldSpatial=spatial.GetValue(e);var oldCell=cell.ParentZone;
                try
                {
                    if(fault=="foreign-crop")c.ParentEntity=f.Player;if(fault=="foreign-spatial")spatial.SetValue(e,new Zone(zone.ZoneID));if(fault=="foreign-cell")cell.ParentZone=new Zone(zone.ZoneID);if(fault=="foreign-zone")zone=new Zone(zone.ZoneID);
                    if(fault=="glyph")e.GetPart<RenderPart>().RenderString="?";if(fault=="invalid-stage")c.GrowthStage=2;if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;if(fault=="removed")Assert.True(f.Zone.RemoveEntity(e));
                    Assert.False(SpawnRing3DRecipes.Resolve(zone,e,f.Library.Definition).ModelId?.StartsWith(Prefix(e.BlueprintName),StringComparison.Ordinal)==true);
                }
                finally{c.ParentEntity=e;if(fault=="foreign-spatial")spatial.SetValue(e,oldSpatial);cell.ParentZone=oldCell;}
            }
        }
        [Test]public void ExistingRipeFieldRemainsItsSeparateHarvestableRow()
        {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var row=f.Add("RipeCropRow");Assert.NotNull(row.GetPart<FieldHarvestPart>());Assert.Null(row.GetPart<CropPart>());Assert.That(Recipe(f,row).ModelId,Does.StartWith("spread-environment-grain-"));}}
    }
}
