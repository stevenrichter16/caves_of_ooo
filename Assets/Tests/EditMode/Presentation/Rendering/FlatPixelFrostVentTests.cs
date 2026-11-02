using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelFrostVentTests
    {
        const int X=10,Y=10;
        const string Atlas="Sprites/Live16x24/frost-vent";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;
        Dictionary<string,LiquidDefinition> liquids,previousLiquids;bool initialized;
        [SetUp] public void Setup()
        {
            liquids=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",S).GetValue(null);
            previousLiquids=new Dictionary<string,LiquidDefinition>(liquids);initialized=LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t=>t.text));
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone("Overworld.2.7.1");manager.SetActiveZone(zone);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)zone.GetCell(x,y).IsVisible=zone.GetCell(x,y).Explored=true;
            world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {try{world?.Dispose();content?.Dispose();}finally{liquids.Clear();foreach(var p in previousLiquids)liquids[p.Key]=p.Value;typeof(LiquidRegistry).GetField("_initialized",S).SetValue(null,initialized);}}
        Entity Add(string name){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,X,Y));return e;}
        Entity Pool(){Add("SandstoneFloor");var e=Add("FrostVent");e.GetPart<TileStateSourcePart>().Seed(zone,X,Y);return e;}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        static object Read(object o,string n)=>o.GetType().GetField(n,I).GetValue(o);
        static void Write(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
        static bool Native(Tile t)=>t?.sprite?.texture==Resources.Load<Texture2D>(Atlas);
        void Body(){var v=View();Assert.True(Native(v.ForegroundTile),"Native frost vent required");Assert.AreEqual(Color.white,v.ForegroundColor);Assert.True(v.LocalGround);}
        bool Subject(Entity e)=>world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _)&&s!=null&&s.texture==Resources.Load<Texture2D>(Atlas);
        [TestCase(-25f)][TestCase(8.870154f)] public void WarmOrColdEmitterHasNativeBodyReaderAndIndependentCold(float temperature)
        {
            var e=Pool();e.GetPart<ThermalPart>().Temperature=temperature;Body();Assert.True(Subject(e));
            var v=View();Assert.AreEqual(new Vector2(16,24),v.ForegroundTile.sprite.rect.size);Assert.False(v.BlocksMovement);
            Assert.AreEqual(FlatPixelGreyboxGround.StoneFloor,v.GroundKind);var before=zone.TileState.ToSaveString();
            using(var paint=new Paint(world,zone)){paint.Step();Assert.True(Native(paint.At(X,Y)));Assert.True(Native(paint.Mark as Tile));
                Assert.AreEqual(32,(paint.Mark as Tile).sprite.rect.x);}
            Assert.AreEqual(before,zone.TileState.ToSaveString());Assert.AreEqual(temperature,e.GetPart<ThermalPart>().Temperature);
        }
        [Test] public void SourceFreeColdFloorGetsRimeWithoutInventingVent()
        {
            var floor=Add("SandstoneFloor");zone.TileState.AddCold(X,Y,1);
            Assert.AreSame(floor,View().Owner);Assert.False(Native(View().ForegroundTile));Assert.True(View().LocalGround);
            using(var paint=new Paint(world,zone)){paint.Step();Assert.True(Native(paint.Mark as Tile));
                zone.TileState.Get(X,Y).Cold=0;paint.Dirty(X,Y);Assert.False(paint.FullDirty);paint.Step();Assert.IsNull(paint.Mark);}
        }
        [TestCase("Player")][TestCase("LongSword")] public void CoveredVentKeepsBodyAndExactReader(string cover)
        {
            var e=Pool();Body();var tile=View().ForegroundTile;var a=Add(cover);a.GetPart<RenderPart>().RenderLayer=100;
            Assert.AreSame(a,View().Owner);Assert.AreSame(tile,View().GroundDecorationTile);Assert.True(Subject(e));
            using(var paint=new Paint(world,zone)){paint.Step();Assert.AreSame(tile,paint.Decoration);zone.RemoveEntity(e);
                zone.TileState.AddCold(X,Y,1);paint.Dirty(X,Y);paint.Step();Assert.IsNull(paint.Decoration);Assert.True(Native(paint.Mark as Tile));Assert.False(Subject(e));}
        }
        [TestCase("hidden")][TestCase("memory")][TestCase("no-floor")][TestCase("duplicate-floor")][TestCase("duplicate-vent")]
        [TestCase("material")][TestCase("glyph")][TestCase("variant")][TestCase("source")][TestCase("nan")][TestCase("solid")][TestCase("stairs")]
        public void UnsupportedOwnersRetainFallback(string fault)
        {
            var e=Pool();switch(fault){case "hidden":e.GetPart<RenderPart>().Visible=false;break;case "memory":zone.GetCell(X,Y).IsVisible=false;break;
            case "no-floor":zone.RemoveEntity(zone.GetCell(X,Y).Objects.First(o=>o.BlueprintName=="SandstoneFloor"));break;
            case "duplicate-floor":Add("SandstoneFloor");break;case "duplicate-vent":Add("FrostVent");break;
            case "material":e.GetPart<MaterialPart>().MaterialID="Steam";break;case "glyph":e.GetPart<RenderPart>().RenderString="!";break;
            case "variant":e.GetPart<RenderPart>().VisualVariant="other";break;case "source":e.GetPart<TileStateSourcePart>().Cold=0;break;
            case "nan":e.GetPart<ThermalPart>().Temperature=float.NaN;break;case "solid":e.GetPart<PhysicsPart>().Solid=true;break;case "stairs":Add("StairsDown");break;}
            Assert.False(Native(View().ForegroundTile));Assert.False(Native(View().GroundDecorationTile));Assert.False(Subject(e));
        }
        [TestCase("heat",'^')][TestCase("charge",'*')][TestCase("oil",'~')][TestCase("embers",'"')][TestCase("veil-mist",'\'')]
        public void HigherPriorityHazardsRetainTheirMarks(string hazard,char glyph)
        {
            Pool();if(hazard=="heat")zone.TileState.AddHeat(X,Y,1);if(hazard=="charge")zone.TileState.AddCharge(X,Y,1);
            if(hazard=="oil")zone.TileState.WriteCoating(X,Y,"oil",3);if(hazard=="embers")zone.TileState.WriteResidue(X,Y,"embers",3);
            if(hazard=="veil-mist")zone.TileState.WriteCloud(X,Y,"veil-mist",3);
            using(var paint=new Paint(world,zone)){paint.Step();Assert.AreSame(CP437TilesetGenerator.GetTile(glyph),paint.Mark);Assert.False(Native(paint.Mark as Tile));}
        }
        [Test] public void HiddenCellClearsBodyAndColdAndCurrentGraphIsRequired()
        {
            var e=Pool();Body();using(var paint=new Paint(world,zone)){paint.Step();zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.IsNull(paint.Mark);Assert.False(Native(paint.At(X,Y)));}
            zone.GetCell(X,Y).IsVisible=true;Assert.True(Subject(e));manager.SetActiveZone(new Zone(zone.ZoneID));Assert.False(Subject(e));
        }
        [TestCase(false)][TestCase(true)] public void OptionalBodyAndColdGroupsAreIndependent(bool malformed)
        {
            var e=Pool();var good=Resources.Load<Texture2D>(Atlas);var bad=malformed?new Texture2D(16,24):null;
            try{using(var selector=new FlatPixelFallbackCatalog())
                using(var missingBody=new FlatPixelFrostVentCatalog(bad,good,selector))
                using(var missingCold=new FlatPixelFrostVentCatalog(good,bad,selector))
                {Assert.False(missingBody.TryResolve(zone,X,Y,e,out _));Assert.True(missingBody.TryCold(zone,X,Y,out _));
                Assert.True(missingCold.TryResolve(zone,X,Y,e,out _));Assert.False(missingCold.TryCold(zone,X,Y,out _));
                missingCold.Dispose();Assert.False(missingCold.TryResolve(zone,X,Y,e,out _));Assert.NotNull(good);}}
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [Test] public void LegacyReturnsColdGlyphAndReenterRestoresNativeMark()
        {
            Pool();using(var paint=new Paint(world,zone)){paint.Step();Assert.True(Native(paint.Mark as Tile));
                Write(paint.Renderer,"_flatPixelModeEnabled",false);paint.Dirty(X,Y);paint.Step();Assert.AreSame(CP437TilesetGenerator.GetTile('^'),paint.Mark);
                Write(paint.Renderer,"_flatPixelModeEnabled",true);paint.Dirty(X,Y);paint.Step();Assert.True(Native(paint.Mark as Tile));}
        }
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, marks, ground, decoration;
            public ZoneRenderer Renderer { get; }
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone z)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Frost vent submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var mark = new GameObject("Hazards", typeof(Tilemap)); mark.transform.SetParent(root.transform, false); marks = mark.GetComponent<Tilemap>();
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>()); Write(Renderer, "_tileStateTilemap", marks);
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true; env.FlatPixelGreyboxEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap"); decoration = (Tilemap)Read(env, "_flatGroundDecorationTilemap");
                    Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            public bool FullDirty => (bool)Read(Renderer, "_fullDirty");
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Tile Decoration => decoration.GetTile<Tile>(new Vector3Int(X, Zone.Height - 1 - Y, 0));
            public TileBase Mark => marks.GetTile(new Vector3Int(X, Zone.Height - 1 - Y, 0));
            public TileBase MarkAt(int x, int y) => marks.GetTile(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Color MarkTint => marks.GetColor(new Vector3Int(X, Zone.Height - 1 - Y, 0));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Frost vent current material");
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try { var env=saved?.Root?.GetComponent<EnvironmentSpriteRenderer>();
                    if(env!=null){Write(env,"_flatPixelGreybox",null);typeof(EnvironmentSpriteRenderer).GetMethod("OnDestroy",I).Invoke(env,null);}
                    if(Renderer!=null)typeof(ZoneRenderer).GetMethod("OnDestroy",I).Invoke(Renderer,null);
                    saved?.Dispose(); if (root != null) Object.DestroyImmediate(root); }
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                }
            }
        }
    }
}
