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
    public sealed class FlatPixelMirrorMucilageTests
    {
        const int X=10,Y=10;
        const string Atlas="Sprites/Live16x24/mirror-mucilage";
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
            zone=new Zone("Overworld.3.0.0");manager.SetActiveZone(zone);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)zone.GetCell(x,y).IsVisible=zone.GetCell(x,y).Explored=true;
            Assert.True(FlatPixelGrovelandsMaterialsCatalog.IsSupportedZone(zone));world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {try{world?.Dispose();content?.Dispose();}finally{liquids.Clear();foreach(var p in previousLiquids)liquids[p.Key]=p.Value;typeof(LiquidRegistry).GetField("_initialized",S).SetValue(null,initialized);}}
        Entity Add(string name){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,X,Y));return e;}
        Entity Pool(){Add("Grass");return Add("MirrorMucilagePool");}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        static object Read(object o,string n)=>o.GetType().GetField(n,I).GetValue(o);
        static void Write(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
        static bool Native(Tile t)=>t?.sprite?.texture==Resources.Load<Texture2D>(Atlas);
        void Body(){var v=View();Assert.True(Native(v.ForegroundTile),"Native mucilage body required");Assert.AreEqual(Color.white,v.ForegroundColor);Assert.True(v.LocalGround);}
        bool Subject(Entity e)=>world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _)&&s!=null&&s.texture==Resources.Load<Texture2D>(Atlas);
        [TestCase(60)][TestCase(0)] public void StockAndExactReaders(int stock)
        {
            var e=Pool();e.GetPart<LiquidPoolPart>().Volume=stock;Body();var v=View();Assert.True(Subject(e));
            Assert.AreEqual(stock==0?2:0,(int)v.ForegroundTile.sprite.rect.x/16 & 2);
            Assert.AreEqual(FlatPixelGreyboxGround.Ground,v.GroundKind);Assert.False(v.RepresentedBrine);Assert.False(v.RepresentedMire);
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelGrovelandsMaterialsCatalog.GroundResourcePath),v.GroundTile.sprite.texture);
            var state=zone.TileState.ToSaveString();int version=zone.EntityVersion;
            for(int i=0;i<20;i++){Body();Assert.AreSame(v.ForegroundTile,View().ForegroundTile);Assert.True(Subject(e));}
            Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(stock,e.GetPart<LiquidPoolPart>().Volume);
        }
        [TestCase("Player")][TestCase("LongSword")]
        public void CoveredSourceKeepsTrueGroundBodyReaderAndSelectedForeground(string cover)
        {
            var e=Pool();Body();var native=View().ForegroundTile;var actor=Add(cover);actor.GetPart<RenderPart>().RenderLayer=100;
            Assert.AreSame(actor,View().Owner);Assert.True(Subject(e));Assert.AreSame(native,View().GroundDecorationTile);
            using(var paint=new Paint(world,zone)){paint.Step();Assert.AreSame(native,paint.Decoration);Assert.IsNull(paint.Mark);
                zone.RemoveEntity(e);paint.Dirty(X,Y);Assert.False(paint.FullDirty);paint.Step();Assert.IsNull(paint.Decoration);Assert.IsNull(paint.Mark);Assert.False(Subject(e));
                // Zone unprojects the removed source. A separately remaining film still needs its glyph.
                zone.TileState.WriteCoating(X,Y,"choir-mirror-mucilage",ZoneTileState.Permanent);
                paint.Dirty(X,Y);paint.Step();Assert.NotNull(paint.Mark);Assert.IsNull(paint.Decoration);}
        }
        [TestCase("none",'\0')][TestCase("heat",'^')][TestCase("cold",'^')][TestCase("charge",'*')][TestCase("embers",'"')][TestCase("veil-mist",'\'')]
        public void SubmittedHazardsRemainIndependent(string hazard,char expected)
        {
            Pool();Body();if(hazard=="heat")zone.TileState.AddHeat(X,Y,1);if(hazard=="cold")zone.TileState.AddCold(X,Y,1);
            if(hazard=="charge")zone.TileState.AddCharge(X,Y,1);if(hazard=="embers")zone.TileState.WriteResidue(X,Y,"embers",3);
            if(hazard=="veil-mist")zone.TileState.WriteCloud(X,Y,"veil-mist",3);
            using(var paint=new Paint(world,zone)){paint.Step();Assert.True(Native(paint.At(X,Y)));
                if(expected=='\0')Assert.IsNull(paint.Mark);else Assert.AreSame(CP437TilesetGenerator.GetTile(expected),paint.Mark);
                zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.IsNull(paint.Mark);Assert.False(Native(paint.At(X,Y)));}
        }
        [TestCase("mixed")][TestCase("expired")][TestCase("finite")][TestCase("projected")][TestCase("missing")]
        [TestCase("wrong-liquid")][TestCase("negative-stock")][TestCase("frozen")][TestCase("vapor")][TestCase("nan")]
        [TestCase("hidden")][TestCase("unexplored")][TestCase("variant")][TestCase("glyph")][TestCase("color")]
        [TestCase("solid")][TestCase("other-pool")][TestCase("duplicate-pool")][TestCase("duplicate-grass")][TestCase("stairs")][TestCase("removed")]
        public void IncompatibleSourceOrStateRetainsFallback(string fault)
        {
            var e=Pool();Body();var state=zone.TileState.Get(X,Y);
            switch(fault){case "mixed":zone.TileState.WriteCoating(X,Y,"oil",3);break;
            case "expired":state.Coatings[0].Turns=0;break;case "finite":state.Coatings[0].Turns=3;break;
            case "projected":state.Coatings[0].ProjectionOwnerId="unknown";break;case "missing":state.Coatings.Clear();break;
            case "wrong-liquid":e.GetPart<LiquidPoolPart>().LiquidId="water";break;case "negative-stock":e.GetPart<LiquidPoolPart>().Volume=-1;break;
            case "frozen":e.GetPart<ThermalPart>().Temperature=-9999;break;case "vapor":e.GetPart<ThermalPart>().Temperature=100;break;
            case "nan":e.GetPart<ThermalPart>().VaporTemperature=float.NaN;break;
            case "hidden":e.GetPart<RenderPart>().Visible=false;break;case "unexplored":zone.GetCell(X,Y).Explored=false;break;
            case "variant":e.GetPart<RenderPart>().VisualVariant="unknown";break;case "glyph":e.GetPart<RenderPart>().RenderString="!";break;
            case "color":e.GetPart<RenderPart>().ColorString="&R";break;case "solid":e.GetPart<PhysicsPart>().Solid=true;break;
            case "other-pool":Add("BrinePool");break;case "duplicate-pool":Add("MirrorMucilagePool");break;
            case "duplicate-grass":Add("Grass");break;case "stairs":Add("StairsDown");break;case "removed":zone.RemoveEntity(e);break;}
            if(fault=="unexplored"){Assert.False(world.TryResolvePresentation(zone,X,Y,out _));Assert.False(Subject(e));return;}
            Assert.False(Native(View().ForegroundTile));Assert.False(Native(View().GroundDecorationTile));Assert.False(Subject(e));
        }
        [Test] public void OldOptionalConstructorCannotSilentlyLoadNewArt()
        {
            var e=Pool();using(var selector=new FlatPixelFallbackCatalog())
            using(var old=new FlatPixelGrovelandsMaterialsCatalog(Resources.Load<Texture2D>(FlatPixelGrovelandsMaterialsCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.ResourcePath),null,null,null,selector,Resources.Load<Texture2D>(FlatPixelGrovelandsMaterialsCatalog.GroundResourcePath),null))
            {Assert.False(old.TryResolveForeground(zone,X,Y,e,out _));Assert.False(old.TryGetSubjectSprite(zone,e,X,Y,out _));}
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedOptionalArtRetainsExactFallback(bool malformed)
        {
            var e=Pool();Texture2D texture=malformed?new Texture2D(16,24):null;
            try {using(var selector=new FlatPixelFallbackCatalog())
                using(var adapter=new FlatPixelGrovelandsMaterialsCatalog(null,Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.ResourcePath),null,null,null,selector,null,null,texture))
                {Assert.False(adapter.TryResolveForeground(zone,X,Y,e,out _));Assert.IsNull(adapter.CoveredMucilage(zone,X,Y));Assert.False(adapter.TryGetSubjectSprite(zone,e,X,Y,out _));Assert.False(adapter.TryResolveGround(zone,X,Y,out _,out _));}}
            finally{if(texture!=null)Object.DestroyImmediate(texture);}
        }
        [Test] public void CurrentGraphAndActualFloorAreRequired()
        {
            var e=Pool();Body();var grass=zone.GetCell(X,Y).Objects.First(o=>o.BlueprintName=="Grass");zone.RemoveEntity(grass);
            Assert.False(Native(View().ForegroundTile));Assert.False(Subject(e));zone.AddEntity(grass,X,Y);Body();
            var replacement=new Zone(zone.ZoneID);manager.SetActiveZone(replacement);
            Assert.False(world.TryResolvePresentation(zone,X,Y,out _));Assert.False(Subject(e));
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Grovelands submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Grovelands current material");
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try { var env=saved?.Root?.GetComponent<EnvironmentSpriteRenderer>();
                    if(env!=null){Write(env,"_flatPixelGreybox",null);typeof(EnvironmentSpriteRenderer).GetMethod("OnDestroy",I).Invoke(env,null);}
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
