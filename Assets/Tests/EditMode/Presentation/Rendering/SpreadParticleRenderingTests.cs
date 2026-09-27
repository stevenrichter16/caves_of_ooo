using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadParticleRenderingTests
 {
  FxGlobals prior;
  [SetUp]public void Setup(){prior=new FxGlobals();SpellFxSettings.Mode=SpellFxMode.Full;SpellFxSettings.AnimationSpeed=1;SpellFxSettings.SoundVolume=0;}
  [TearDown]public void Cleanup(){prior?.Dispose();prior=null;}
  static void NearColor(Color expected,Color actual)
  {Assert.That(actual.r,Is.EqualTo(expected.r).Within(.000001f));Assert.That(actual.g,Is.EqualTo(expected.g).Within(.000001f));Assert.That(actual.b,Is.EqualTo(expected.b).Within(.000001f));Assert.That(actual.a,Is.EqualTo(expected.a).Within(.000001f));}
  sealed class Fixture:IDisposable
  {
   public readonly SpawnRing3DIntegrationFixture Map;public readonly GameObject Root;public readonly Tilemap Tiles;public readonly AsciiFxRenderer Ascii;public readonly WorldFxCoordinator World;
   public Fixture(){Map=new SpawnRing3DIntegrationFixture("Overworld.12.10.0");Root=new GameObject("Particle frame fixture",typeof(Grid));var tile=new GameObject("Legacy FX",typeof(Tilemap),typeof(TilemapRenderer));tile.transform.SetParent(Root.transform);Tiles=tile.GetComponent<Tilemap>();Ascii=new AsciiFxRenderer(Tiles);World=new WorldFxCoordinator(Ascii,Root.transform);World.SetZone(Map.Zone);World.SetNativeSurface(((SpawnRing3DPresenter)Map.Presenter).ActiveSurface);World.Update(0);}
   public bool View(int x,int y,out GameObject root){var m=typeof(WorldFxCoordinator).GetMethod("TryGetNativeParticle",BindingFlags.Instance|BindingFlags.Public);Assert.NotNull(m,"The scoped decorative draw-frame hook must exist.");object[] a={x,y,null};bool found=(bool)m.Invoke(World,a);root=a[2]as GameObject;return found;}
   public bool Any(){for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(View(x,y,out _))return true;return false;}
   public void Emit(char glyph='*',float life=1)=>AsciiFxBus.EmitParticle(Map.Zone,20,10,glyph,"&M",life);
   public TileBase Tile(int x=20,int y=10)=>Tiles.GetTile(new Vector3Int(x,Zone.Height-1-y,0));
   public void Dispose(){World.Dispose();UnityEngine.Object.DestroyImmediate(Root);Map.Dispose();}
  }
  [TestCase("particle")][TestCase("burst")][TestCase("projectile")][TestCase("beam")][TestCase("ring")]
  [TestCase("orbit")][TestCase("chain")][TestCase("column")][TestCase("aura")][TestCase("dust")]
  public void ActualLegacyDrawFormsUseNativeGeometryWithoutAnotherClock(string kind)
  {using(var f=new Fixture()){var z=f.Map.Zone;int version=z.EntityVersion;string state=z.TileState.ToSaveString();var path=new[]{new Point(20,10),new Point(21,10)};
   switch(kind){case "particle":f.Emit();break;case "burst":AsciiFxBus.EmitBurst(z,20,10,AsciiFxTheme.Fire,false);break;case "projectile":AsciiFxBus.EmitProjectile(z,path,AsciiFxTheme.Fire,true,true);break;case "beam":AsciiFxBus.EmitBeam(z,path,1,0,AsciiFxTheme.Fire,1,true);break;case "ring":AsciiFxBus.EmitRingWave(z,20,10,2,.2f,AsciiFxTheme.Fire,true);break;case "orbit":AsciiFxBus.EmitChargeOrbit(z,f.Map.Player,1,1,AsciiFxTheme.Fire,true);break;case "chain":AsciiFxBus.EmitChainArc(z,path,AsciiFxTheme.Fire,.2f,true);break;case "column":AsciiFxBus.EmitColumnRise(z,20,10,3,.2f,.2f,AsciiFxTheme.Earth,true);break;case "aura":AsciiFxBus.StartAura(z,f.Map.Player,AsciiFxTheme.Fire);break;case "dust":f.Ascii.SpawnDustMote(20,10);break;}
   f.World.Update(0);if(kind=="aura"){Assert.AreEqual(1,f.Ascii.ActiveAuraCount);for(int i=0;i<12&&!f.Any();i++)f.World.Update(.02f);}Assert.True(f.Any(),kind);Assert.AreEqual(version,z.EntityVersion);Assert.AreEqual(state,z.TileState.ToSaveString());Assert.Zero(AsciiFxBus.PendingCount);f.World.CancelAll();Assert.False(f.Any());Assert.False(f.World.HasBlockingFx);}}
  [Test]public void NativeMarkHasExactSemanticMaterialFogAndNoPhysics()
  {using(var f=new Fixture()){f.Emit();f.World.Update(0);Assert.True(f.View(20,10,out var root));var surface=((SpawnRing3DPresenter)f.Map.Presenter).ActiveSurface;var r=root.GetComponent<MeshRenderer>();var mesh=root.GetComponent<MeshFilter>().sharedMesh;Assert.NotNull(mesh);Assert.Greater(mesh.vertexCount,0);Assert.Greater(mesh.triangles.Length,0);Assert.AreSame(surface.ContentRoot,root.transform.parent);Assert.AreEqual(NativeZone3DRenderSurface.WorldLayer,root.layer);Assert.AreSame(surface.MaterialFor(f.Map.Library.WorldMaterial),r.sharedMaterial);var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);NearColor(QudColorParser.Parse("&M").linear,b.GetColor("_BaseColor"));Assert.AreEqual(1,b.GetFloat("_Transient"));Assert.AreSame(surface.FogTexture,r.sharedMaterial.GetTexture("_FogLight"));Assert.Zero(root.GetComponents<Collider>().Length);Assert.Null(f.Tile());}}
  [TestCase(false)][TestCase(true)]public void NumericReadoutRemainsAUiGlyphAndOverwritesDecoration(bool number)
  {using(var f=new Fixture()){f.Emit();if(number)f.Emit('7');f.World.Update(0);Assert.AreEqual(!number,f.View(20,10,out _));Assert.AreEqual(number,f.Tile()!=null);Assert.AreEqual(number?2:1,f.Ascii.ActiveParticleCount);}}
  [Test]public void ExistingLifetimeMotionAndBlockingRemainOwnedByAsciiRenderer()
  {using(var f=new Fixture()){AsciiFxBus.EmitParticle(f.Map.Zone,20,10,'*',"&M",.5f,-1,.1f);f.World.Update(0);Assert.True(f.View(20,10,out var first));f.World.Update(.11f);Assert.False(f.View(20,10,out _));Assert.True(f.View(20,9,out _));SpawnRing3DIntegrationFixture.Hidden(first);Assert.False(f.World.HasBlockingFx);f.World.Update(.5f);Assert.False(f.Any());Assert.Zero(f.Ascii.ActiveParticleCount);}}
  [TestCase("hidden")][TestCase("unexplored")][TestCase("foreign")][TestCase("surface")][TestCase("sprite")][TestCase("ui")][TestCase("off")]
  public void LostAuthorityOrPresentationNeverSuppressesFallbackWithAnOldView(string fault)
  {using(var f=new Fixture()){f.Emit();f.World.Update(0);Assert.True(f.View(20,10,out var old));if(fault=="hidden")f.Map.Zone.GetCell(20,10).IsVisible=false;else if(fault=="unexplored")f.Map.Zone.GetCell(20,10).Explored=false;else if(fault=="foreign")f.Map.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;else if(fault=="surface")f.World.SetNativeSurface(null);else if(fault=="off")SpellFxSettings.Mode=SpellFxMode.Off;f.World.Update(0,fault!="sprite",fault!="ui");Assert.False(f.View(20,10,out _));SpawnRing3DIntegrationFixture.Hidden(old);}}
  [TestCase("parent")][TestCase("layer")][TestCase("material")][TestCase("renderer")]
  public void LiveSubmissionMutationMustRefuseBeforeRefreshAndRepairOwnedView(string fault)
  {using(var f=new Fixture()){f.Emit();f.World.Update(0);Assert.True(f.View(20,10,out var root));var r=root.GetComponent<MeshRenderer>();if(fault=="parent")root.transform.SetParent(f.Root.transform,false);else if(fault=="layer")root.layer=0;else if(fault=="material")r.sharedMaterials=new[]{r.sharedMaterial,f.Map.Library.WaterMaterial};else r.forceRenderingOff=true;Assert.False(f.View(20,10,out _));f.World.Update(0);Assert.True(f.View(20,10,out _));}}
  [TestCase(false)][TestCase(true)]public void OwnedIndexedOverrideCannotSubmitAlongsideFallback(bool mutate)
  {using(var f=new Fixture()){f.Emit();f.World.Update(0);Assert.True(f.View(20,10,out var root));var r=root.GetComponent<MeshRenderer>();if(mutate){var b=new MaterialPropertyBlock();b.SetFloat("_Transient",0);r.SetPropertyBlock(b,0);}Assert.AreEqual(!mutate,f.View(20,10,out _));f.World.Update(0);Assert.True(f.View(20,10,out root));var after=new MaterialPropertyBlock();root.GetComponent<MeshRenderer>().GetPropertyBlock(after,0);Assert.True(after.isEmpty);Assert.Null(f.Tile());}}
  [TestCase(false)][TestCase(true)]public void WrongParticleColorCannotBorrowTheCurrentMarkProof(bool mutate)
  {using(var f=new Fixture()){f.Emit();f.World.Update(0);Assert.True(f.View(20,10,out var root));var r=root.GetComponent<MeshRenderer>();if(mutate){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetColor("_BaseColor",Color.green);r.SetPropertyBlock(b);}Assert.AreEqual(!mutate,f.View(20,10,out _));f.World.Update(0);Assert.True(f.View(20,10,out root));var b2=new MaterialPropertyBlock();root.GetComponent<MeshRenderer>().GetPropertyBlock(b2);NearColor(QudColorParser.Parse("&M").linear,b2.GetColor("_BaseColor"));}}
  [Test]public void RepeatedDrawReusesGeometryAndDisposeReleasesOnlyOwnedResources()
  {GameObject root;Mesh mesh;Material borrowed;using(var f=new Fixture()){borrowed=f.Map.Library.WorldMaterial;f.Emit();f.World.Update(0);Assert.True(f.View(20,10,out root));mesh=root.GetComponent<MeshFilter>().sharedMesh;f.World.Update(0);Assert.True(f.View(20,10,out var same));Assert.AreSame(root,same);Assert.AreSame(mesh,same.GetComponent<MeshFilter>().sharedMesh);}Assert.True(root==null);Assert.True(mesh==null);Assert.True(borrowed!=null);}
  [Test]public void UnknownDecorativeColorRetainsExistingFallback()
  {using(var f=new Fixture()){AsciiFxBus.EmitParticle(f.Map.Zone,20,10,'*',"invalid",1);f.World.Update(0);Assert.False(f.View(20,10,out _));Assert.NotNull(f.Tile());}}
  [Test]public void NativeAndNumericLastDrawOrderingMatchesSingleLegacyCell()
  {using(var f=new Fixture()){f.Emit('7');f.Emit('*');f.World.Update(0);Assert.True(f.View(20,10,out _));Assert.Null(f.Tile());}}
        sealed class FxGlobals : IDisposable
        {
            const BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
            readonly SpellFxMode mode = SpellFxSettings.Mode; readonly float speed = SpellFxSettings.AnimationSpeed, sound = SpellFxSettings.SoundVolume, flash = SpellFxSettings.FlashIntensity;
            readonly Queue<AsciiFxRequest> pending = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", Private).GetValue(null);
            readonly Stack<AsciiFxRequest> pool = (Stack<AsciiFxRequest>)typeof(AsciiFxBus).GetField("Pool", Private).GetValue(null);
            readonly List<SpellFxSequence> spells = (List<SpellFxSequence>)typeof(SpellFxBus).GetField("Pending", Private).GetValue(null);
            readonly FieldInfo clearField = typeof(AsciiFxBus).GetField("<ClearVersion>k__BackingField", Private);
            readonly Dictionary<string, SpellFxDefinition> definitions = (Dictionary<string, SpellFxDefinition>)typeof(SpellFxCatalog).GetField("Definitions", Private).GetValue(null);
            readonly Dictionary<string, SpellFxAsset> assets = (Dictionary<string, SpellFxAsset>)typeof(SpellFxCatalog).GetField("Assets", Private).GetValue(null);
            readonly List<string> issues = (List<string>)typeof(SpellFxCatalog).GetField("Issues", Private).GetValue(null);
            readonly FieldInfo loadedField = typeof(SpellFxCatalog).GetField("_loaded", Private);
            readonly AsciiFxRequest[] priorPending, priorPool; readonly SpellFxSequence[] priorSpells; readonly string[] priorIssues;
            readonly Dictionary<string, SpellFxDefinition> priorDefinitions; readonly Dictionary<string, SpellFxAsset> priorAssets;
            readonly int clear; readonly bool loaded; bool disposed;
            public FxGlobals()
            {
                priorPending = pending.ToArray(); priorPool = pool.ToArray(); priorSpells = spells.ToArray(); clear = (int)clearField.GetValue(null);
                priorDefinitions = new Dictionary<string, SpellFxDefinition>(definitions); priorAssets = new Dictionary<string, SpellFxAsset>(assets); priorIssues = issues.ToArray(); loaded = (bool)loadedField.GetValue(null);
                pending.Clear(); pool.Clear(); spells.Clear();
                definitions.Clear(); assets.Clear(); issues.Clear(); loadedField.SetValue(null, false);
            }
            public void Dispose()
            {
                if (disposed) return; disposed = true;
                pending.Clear(); pool.Clear(); spells.Clear(); foreach (var request in priorPending) pending.Enqueue(request);
                for (int i = priorPool.Length - 1; i >= 0; i--) pool.Push(priorPool[i]); spells.AddRange(priorSpells); clearField.SetValue(null, clear);
                foreach (var value in assets.Values.Distinct()) if (value != null && !priorAssets.Values.Contains(value)) value.Dispose();
                definitions.Clear(); foreach (var row in priorDefinitions) definitions.Add(row.Key, row.Value);
                assets.Clear(); foreach (var row in priorAssets) assets.Add(row.Key, row.Value); issues.Clear(); issues.AddRange(priorIssues); loadedField.SetValue(null, loaded);
                SpellFxSettings.Mode = mode; SpellFxSettings.AnimationSpeed = speed; SpellFxSettings.SoundVolume = sound; SpellFxSettings.FlashIntensity = flash;
            }
        }
 }
}
