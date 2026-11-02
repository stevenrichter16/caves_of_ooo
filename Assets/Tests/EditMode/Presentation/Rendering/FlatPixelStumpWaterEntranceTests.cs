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
    public sealed class FlatPixelStumpWaterEntranceTests
    {
        const int X=10,Y=10;
        const string Atlas="Sprites/Live16x24/opening-anchors";
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
            zone=new Zone("Overworld.4.4.0");manager.SetActiveZone(zone);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)zone.GetCell(x,y).IsVisible=zone.GetCell(x,y).Explored=true;
            Assert.True(FlatPixelStumpMaterialsCatalog.IsSupportedZone(zone));world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {try{world?.Dispose();content?.Dispose();}finally{liquids.Clear();foreach(var p in previousLiquids)liquids[p.Key]=p.Value;typeof(LiquidRegistry).GetField("_initialized",S).SetValue(null,initialized);}}
        Entity Add(string name){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,X,Y));return e;}
        Entity Pool(out Entity stair){Add("TepuiStone");stair=Add("StairsDown");return Add("SprayPool");}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        static object Read(object o,string n)=>o.GetType().GetField(n,I).GetValue(o);
        static void Write(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
        static bool NativeStair(Tile t)=>t?.sprite?.texture==Resources.Load<Texture2D>(Atlas)&&t.sprite.rect.x==64;
        bool Subject(Entity e,string resource)=>world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _)&&s!=null&&s.texture==Resources.Load<Texture2D>(resource);
        void Entrance(Entity pool){var v=View();Assert.True(NativeStair(v.GroundDecorationTile),"Native entrance cue required");Assert.AreSame(pool,v.Owner);Assert.AreEqual(FlatPixelGreyboxGround.Water,v.GroundKind);Assert.IsNull(v.ForegroundTile);}
        [TestCase(40)][TestCase(0)] public void PoolAndEntranceKeepSeparateNativeIdentities(int stock)
        {var p=Pool(out var stair);p.GetPart<LiquidPoolPart>().Volume=stock;Entrance(p);Assert.True(Subject(stair,Atlas));Assert.True(Subject(p,FlatPixelStumpMaterialsCatalog.ResourcePath));
         string state=zone.TileState.ToSaveString();int version=zone.EntityVersion;var tile=View().GroundDecorationTile;
         for(int i=0;i<20;i++){Entrance(p);Assert.AreSame(tile,View().GroundDecorationTile);}Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(version,zone.EntityVersion);}
        [TestCase("Player")][TestCase("LongSword")] public void ActualCoverRetainsEntranceAndBothReaders(string cover)
        {var p=Pool(out var stair);Entrance(p);var actor=Add(cover);actor.GetPart<RenderPart>().RenderLayer=100;Assert.AreSame(actor,View().Owner);Assert.True(NativeStair(View().GroundDecorationTile));
         Assert.True(Subject(stair,Atlas));Assert.True(Subject(p,FlatPixelStumpMaterialsCatalog.ResourcePath));
         using(var paint=new Paint(world,zone)){paint.Step();Assert.True(NativeStair(paint.Decoration));Assert.IsNull(paint.Mark);
           zone.RemoveEntity(stair);paint.Dirty(X,Y);Assert.False(paint.FullDirty);paint.Step();Assert.IsNull(paint.Decoration);Assert.False(Subject(stair,Atlas));}}
        [TestCase("none",'\0')][TestCase("heat",'^')][TestCase("charge",'*')]
        public void SubmittedWaterAndEntranceKeepHazards(string hazard,char expected)
        {var p=Pool(out var stair);Entrance(p);if(hazard=="heat")zone.TileState.AddHeat(X,Y,1);if(hazard=="charge")zone.TileState.AddCharge(X,Y,1);
         using(var paint=new Paint(world,zone)){paint.Step();Assert.True(NativeStair(paint.Decoration));if(expected=='\0')Assert.IsNull(paint.Mark);else Assert.AreSame(CP437TilesetGenerator.GetTile(expected),paint.Mark);
         zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.IsNull(paint.Decoration);Assert.IsNull(paint.Mark);}}
        [TestCase("hidden")][TestCase("glyph")][TestCase("visual")][TestCase("solid")][TestCase("tag")][TestCase("duplicate-stair")][TestCase("duplicate-pool")]
        [TestCase("mixed")][TestCase("finite")][TestCase("projected")][TestCase("missing")][TestCase("wrong-liquid")][TestCase("negative-stock")][TestCase("floor")][TestCase("moved")]
        public void InvalidOwnersOrWaterCannotAdvertiseNativeEntrance(string fault)
        {var p=Pool(out var stair);Entrance(p);var st=zone.TileState.Get(X,Y);
         switch(fault){case "hidden":stair.GetPart<RenderPart>().Visible=false;break;case "glyph":stair.GetPart<RenderPart>().RenderString="!";break;
         case "visual":stair.GetPart<RenderPart>().VisualID="foreign";break;case "solid":stair.GetPart<PhysicsPart>().Solid=true;break;
         case "tag":stair.Tags.Remove("StairsDown");break;case "duplicate-stair":Add("StairsDown");break;case "duplicate-pool":Add("SprayPool");break;
         case "mixed":zone.TileState.WriteCoating(X,Y,"oil",3);break;case "finite":st.Coatings[0].Turns=3;break;
         case "projected":st.Coatings[0].ProjectionOwnerId="other";break;case "missing":st.Coatings.Clear();break;
         case "wrong-liquid":p.GetPart<LiquidPoolPart>().LiquidId="acid";break;case "negative-stock":p.GetPart<LiquidPoolPart>().Volume=-1;break;
         case "floor":zone.RemoveEntity(zone.GetCell(X,Y).Objects.First(e=>e.BlueprintName=="TepuiStone"));break;
         case "moved":zone.MoveEntity(stair,X+1,Y);break;}
         Assert.False(NativeStair(View().GroundDecorationTile));Assert.False(Subject(stair,Atlas));}
        [Test] public void OptionalResourceAndOtherZonesRemainBounded()
        {var p=Pool(out var stair);Entrance(p);using(var selector=new FlatPixelFallbackCatalog())using(var old=new FlatPixelStumpMaterialsCatalog(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),null,null,selector))
         {Assert.False(old.TryResolveWaterGround(zone,X,Y,out _,out _));Assert.False(old.TryGetSubjectSprite(zone,stair,X,Y,out _));}
         var other=new Zone("Overworld.3.4.0");manager.SetActiveZone(other);zone=other;zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;Pool(out stair);Assert.False(NativeStair(View().GroundDecorationTile));}
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
