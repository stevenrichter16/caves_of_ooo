using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class LiquidAmbientColorTests
 {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  const int X=20,Y=10;
  static readonly Vector3Int At=new Vector3Int(X,Zone.Height-1-Y,0);
  EntityEquipmentContentFixture content;
  Dictionary<string,LiquidDefinition> registry,prior;bool initialized;
  GameObject root;ZoneRenderer renderer;Tilemap tiles,fine,background;Zone zone;
  [SetUp]public void Setup()
  {
   registry=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
   prior=new Dictionary<string,LiquidDefinition>(registry);initialized=LiquidRegistry.IsInitialized;
   try
   {
   LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").OrderBy(p=>p,StringComparer.Ordinal).Select(File.ReadAllText));
   content=new EntityEquipmentContentFixture();zone=new Zone("liquid-ambient-render-probe");
   foreach(var cell in zone.Cells){cell.Explored=true;cell.IsVisible=true;}
   root=new GameObject("Owned liquid ambient color probe");root.SetActive(false);root.AddComponent<Grid>();
   tiles=Map("main");fine=Map("fine");background=Map("background");
   // Inactive host avoids Awake/global UI resources. The production painters
   // operate on these real owned Tilemaps with no simulated implementation.
   renderer=tiles.gameObject.AddComponent<ZoneRenderer>();
   typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(renderer,zone);
   Set("_tilemap",tiles);Set("_fineWaterTilemap",fine);Set("_bgTilemap",background);
   }catch{Cleanup();throw;}
  }
  [TearDown]public void Cleanup()
  {
   if(root!=null)Object.DestroyImmediate(root);root=null;content?.Dispose();content=null;
   if(registry!=null&&prior!=null){registry.Clear();foreach(var p in prior)registry[p.Key]=p.Value;typeof(LiquidRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,initialized);}
  }
  Tilemap Map(string name){var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(root.transform,false);return go.GetComponent<Tilemap>();}
  void Set(string name,object value)=>typeof(ZoneRenderer).GetField(name,Private).SetValue(renderer,value);
  void Invoke(string name,params object[] args){try{typeof(ZoneRenderer).GetMethod(name,Private).Invoke(renderer,args);}catch(TargetInvocationException e){throw e.InnerException??e;}}
  Entity Add(string blueprint,string liquid=null)
  {
   var owner=content.Create(blueprint);Assert.NotNull(owner);
   if(liquid!=null){var pool=owner.GetPart<LiquidPoolPart>();Assert.NotNull(pool);pool.LiquidId=liquid;pool.Volume=3;pool.Initialize();}
   Assert.True(zone.AddEntity(owner,X,Y));Assert.AreSame(owner,zone.GetCell(X,Y).GetTopVisibleObject());return owner;
  }
  void Paint(){Invoke("RenderCellCore",X,Y);Assert.NotNull(tiles.GetTile(At));}
  void Cache()=>Invoke("RefreshWaterCache");
  void Animate()=>Invoke("UpdateAmbientAnimations",.6f);
  static void ColorEqual(Color expected,Color actual,string because=null)
  {Assert.AreEqual(expected.r,actual.r,1e-6f,because);Assert.AreEqual(expected.g,actual.g,1e-6f,because);Assert.AreEqual(expected.b,actual.b,1e-6f,because);Assert.AreEqual(expected.a,actual.a,1e-6f,because);}
  void KeepsPaintedColor(Entity owner)
  {
   Paint();var before=tiles.GetColor(At);ColorEqual(QudColorParser.Parse(owner.GetPart<RenderPart>().ColorString),before,"Base native renderer must first paint the correct authored color.");
   var tile=tiles.GetTile(At);Cache();for(int i=0;i<4;i++){Animate();ColorEqual(before,tiles.GetColor(At),"Water animation must not recolor a different liquid or an unrelated tilde.");Assert.AreSame(tile,tiles.GetTile(At));}
   Assert.AreEqual(0,fine.GetUsedTilesCount());
  }
  [TestCase("AcidPool")][TestCase("OilSlick")]
  public void ActualNaturalNonwaterPoolsRetainTheirPaintedColors(string blueprint)=>KeepsPaintedColor(Add(blueprint));
  [TestCase("acid")][TestCase("oil")][TestCase("honey")][TestCase("lava")][TestCase("gel")]
  public void ActualConfiguredPoursRetainExactRegistryColor(string liquid)
  {
   var owner=Add("PouredLiquidPool",liquid);var entry=PouredLiquid3DLibrary.Load().Find(PouredLiquid3DLibrary.ResolveOwner(owner))?.Prefab;
   Assert.NotNull(entry);var material=entry.GetComponent<MeshRenderer>().sharedMaterial;var original=material.GetColor("_BaseColor");
   KeepsPaintedColor(owner);ColorEqual(original,material.GetColor("_BaseColor"),"Borrowed native 3D liquid palette stays unchanged.");
  }
  [TestCase(false)][TestCase(true)]public void ActualWaterKeepsItsExistingStationaryShimmer(bool poured)
  {
   var owner=poured?Add("PouredLiquidPool","water"):Add("WaterPuddle");Paint();Cache();var before=tiles.GetColor(At);bool changed=false;
   var palette=new[]{QudColorParser.DarkBlue,QudColorParser.DarkCyan,QudColorParser.BrightBlue};
   for(int i=0;i<4;i++){Animate();var actual=tiles.GetColor(At);Assert.True(palette.Any(c=>c==actual));changed|=actual!=before;}
   Assert.True(changed,"Positive water must still visibly shimmer.");Assert.AreEqual("water",owner.GetPart<LiquidPoolPart>().LiquidId);
  }
  [TestCase("FlowsSouth")][TestCase("FlowsEast")]
  public void ActualWaterKeepsFineFlowAndBackground(string tag)
  {
   var owner=Add("WaterPuddle");owner.SetTag(tag,tag=="FlowsSouth"?"core":"0.250");Paint();Cache();
   // Decorative leaf timing is unrelated to this paired fine-water control.
   var debris=typeof(ZoneRenderer).GetField("_debris",Private);debris.SetValue(renderer,Array.CreateInstance(debris.FieldType.GetElementType(),0));Animate();
   Assert.Greater(fine.GetUsedTilesCount(),0);Assert.NotNull(background.GetTile(At));Assert.AreEqual("water",owner.GetPart<LiquidPoolPart>().LiquidId);
   if(tag=="FlowsSouth")Assert.NotNull(background.GetTile(new Vector3Int(X-1,Zone.Height-1-Y,0)),"The actual still-flowing water source keeps its cached flank reflection.");
  }
  [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)][TestCase(true,true)]
  public void CachedWaterChangedToAcidCannotRetainBlueShimmer(bool rebuild,bool flowing)
  {
   var owner=Add("PouredLiquidPool","water");if(flowing)owner.SetTag("FlowsSouth","core");Paint();Cache();Animate();
   var pool=owner.GetPart<LiquidPoolPart>();pool.LiquidId="acid";pool.Initialize();
   // A base/dirty repaint may clear background while retaining the ambient cache.
   // The old flowing source may no longer repaint its cached reflection flank.
   background.ClearAllTiles();Paint();var expected=tiles.GetColor(At);var tile=tiles.GetTile(At);
   if(rebuild)Cache();for(int i=0;i<4;i++){Animate();ColorEqual(expected,tiles.GetColor(At));Assert.AreSame(tile,tiles.GetTile(At));}
   Assert.AreEqual(0,fine.GetUsedTilesCount());Assert.AreEqual(0,background.GetUsedTilesCount());
   Assert.AreEqual("acid",pool.LiquidId);Assert.AreEqual(3,pool.Volume);
  }
  [TestCase("no-pool")][TestCase("unknown-liquid")][TestCase("empty-water")][TestCase("foreign-pool")]
  public void ASharedTildeIsNotWaterAuthority(string state)
  {
   Entity owner;
   if(state=="no-pool"){owner=new Entity{BlueprintName="CustomTilde"};owner.AddPart(new RenderPart{RenderString="~",ColorString="&M",RenderLayer=5});Assert.True(zone.AddEntity(owner,X,Y));}
   else{owner=Add("PouredLiquidPool","water");var pool=owner.GetPart<LiquidPoolPart>();if(state=="unknown-liquid")pool.LiquidId="unregistered";else if(state=="empty-water")pool.Volume=0;else pool.ParentEntity=new Entity();owner.GetPart<RenderPart>().ColorString="&M";}
   owner.SetTag("FlowsSouth","core");KeepsPaintedColor(owner);Assert.AreEqual(0,background.GetUsedTilesCount(),"False flow tags must not paint a water background/reflection.");
  }
  [TestCase("acid",false)][TestCase("acid",true)]
  [TestCase("empty",false)][TestCase("empty",true)]
  [TestCase("removed",false)][TestCase("removed",true)]
  [TestCase("live",false)][TestCase("live",true)]
  public void RetiredFlowSourceRestoresItsActuallyPaintedFlankOnTheDirtyPath(string state,bool authoredBackground)
  {
   var source=Add("PouredLiquidPool","water");source.SetTag("FlowsSouth","core");
   var flank=new Vector3Int(X-1,Zone.Height-1-Y,0);
   if(authoredBackground)
   {
    var land=new Entity{BlueprintName="ActualColoredFlank"};
    land.AddPart(new RenderPart{RenderString=".",ColorString="&W",BackgroundColor="^R",RenderLayer=1});
    Assert.True(zone.AddEntity(land,X-1,Y));
   }
   Set("_fullDirty",false);renderer.MarkCellDirty(X-1,Y,"LiquidColorFlankBaseline");
   Invoke("RenderDirtyCells");ClearDirty();
   var originalTile=background.GetTile(flank);var originalColor=background.GetColor(flank);
   Assert.AreEqual(authoredBackground,originalTile!=null);
   Paint();Cache();Animate();Assert.NotNull(background.GetTile(flank),"Real flow first paints a visible adjacent reflection.");
   if(authoredBackground)Assert.AreNotEqual(originalColor,background.GetColor(flank),"The reflection visibly replaced the actual authored background.");
   if(state=="acid"){var pool=source.GetPart<LiquidPoolPart>();pool.LiquidId="acid";pool.Initialize();}
   else if(state=="empty")source.GetPart<LiquidPoolPart>().Volume=0;
   else if(state=="removed")Assert.True(zone.RemoveEntity(source));
   // Reproduce a stationary player's cell-only source repaint, without erasing
   // the flank up front. Ambient invalidation must arrange its own bounded
   // baseline restoration through the normal following dirty pass.
   renderer.MarkCellDirty(X,Y,"LiquidSourceChanged");Invoke("RenderDirtyCells");ClearDirty();
   for(int frame=0;frame<3;frame++){Animate();Invoke("RenderDirtyCells");ClearDirty();}
   if(state=="live")
   {Assert.NotNull(background.GetTile(flank));if(authoredBackground)Assert.AreNotEqual(originalColor,background.GetColor(flank));}
   else
   {Assert.AreSame(originalTile,background.GetTile(flank),"Retired water cannot leave its previous blue footprint.");if(authoredBackground)ColorEqual(originalColor,background.GetColor(flank));}
  }
  void ClearDirty()=>((HashSet<int>)typeof(ZoneRenderer).GetField("_dirtyCells",Private).GetValue(renderer)).Clear();
  [TestCase("fog")][TestCase("cover")]
  public void CachedWaterCannotOverwriteFogOrAnotherTopOwner(string change)
  {
   Add("WaterPuddle");Paint();Cache();
   if(change=="fog")zone.GetCell(X,Y).IsVisible=false;
   else{var actor=new Entity{BlueprintName="CoveringActor"};actor.AddPart(new RenderPart{RenderString="@",ColorString="&R",RenderLayer=20});Assert.True(zone.AddEntity(actor,X,Y));}
   Paint();var expected=tiles.GetColor(At);var tile=tiles.GetTile(At);Animate();ColorEqual(expected,tiles.GetColor(At));Assert.AreSame(tile,tiles.GetTile(At));Assert.AreEqual(0,fine.GetUsedTilesCount());
  }
 }
}
