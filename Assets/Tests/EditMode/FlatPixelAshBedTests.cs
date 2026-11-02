using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelAshBedTests
    {
        const int X=10,Y=10;
        const string Ash="Sprites/Live16x24/campfire-scorch", Stone="Sprites/Live16x24/stump-materials";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;
        Action<string> oldMessage,oldFull; Action<int,int,string> oldDirty;
        [SetUp] public void Setup(){oldMessage=MessageLog.OnMessage;MessageLog.OnMessage=null;oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=Attach("Overworld.2.3.0");world=new FlatPixelGreyboxCatalog();}
        [TearDown] public void Cleanup(){try{world?.Dispose();content?.Dispose();}finally{MessageLog.OnMessage=oldMessage;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;}}
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        static object Read(object o,string n)=>o.GetType().GetField(n,I).GetValue(o);
        static void Write(object o,string n,object v)=>o.GetType().GetField(n,I).SetValue(o,v);
        static Texture2D Texture(string path)=>Resources.Load<Texture2D>(path);
        static void Native(Tile t,string path=Ash){Assert.NotNull(t);Assert.NotNull(t.sprite);Assert.AreSame(Texture(path),t.sprite.texture);Assert.AreEqual(new Vector2(16,24),t.sprite.rect.size);Assert.AreEqual(Tile.ColliderType.None,t.colliderType);}
        void Body(Entity e){var v=View();Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground);Assert.False(v.BlocksMovement);Native(v.ForegroundTile);}
        AnnouncementDocument Document(Entity e,Entity viewer=null)=>ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);
        static FlatPixelSharedSteamVentCatalog Inject(Texture2D vent,Texture2D ash){var c=typeof(FlatPixelSharedSteamVentCatalog).GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D)});Assert.NotNull(c,"Optional ash input missing");return (FlatPixelSharedSteamVentCatalog)c.Invoke(new object[]{vent,ash});}
        [TestCase("Overworld.2.3.0")][TestCase("Overworld.19.14.0")][TestCase("Overworld.16.13.9")][TestCase("Overworld.12.10.0")][TestCase("StandaloneAsh")]
        public void ExactAshBodyAndAuthorizedReaderReuseApprovedScorch(string id)
        {
            zone=Attach(id);var e=Add("AshBed");Body(e);var player=Add("Player",X-1,Y);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits()){paint.Step();Native(paint.At(X,Y));var portrait=portraits.GetPortrait(Document(e,player),paint.Renderer);Assert.AreSame(Texture(Ash),portrait.Texture);Assert.False(portrait.IsModel);}
        }
        [Test] public void StumpBackingRequiresTheExactDisclosedSourceAndRealStone()
        {
            Add("TepuiStone");var e=Add("AshBed");e.GetPart<TileStateSourcePart>().Seed(zone,X,Y);Body(e);Native(View().GroundTile,Stone);string state=zone.TileState.ToSaveString();
            var actor=Add("Player");Assert.AreSame(actor,View().Owner);Native(View().GroundTile,Stone);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits()){paint.Step();Assert.AreSame(Texture(Ash),portraits.GetPortrait(Document(e,actor),paint.Renderer).Texture);}
            Assert.True(zone.RemoveEntity(actor));Assert.True(zone.RemoveEntity(e));Assert.AreNotSame(Texture(Stone),View().GroundTile.sprite.texture,"Residual heat alone cannot authorize the source exception");Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [TestCase("source")][TestCase("duplicate-source")][TestCase("foreign-source")][TestCase("render")][TestCase("hidden")][TestCase("material")][TestCase("heat")][TestCase("radiation")][TestCase("coating")][TestCase("charge")][TestCase("capacity")][TestCase("nonfinite")][TestCase("solid")][TestCase("carried")][TestCase("service")][TestCase("duplicate-physics")]
        public void ChangedMeaningRefusesBothNativeBodyAndStumpBacking(string fault)
        {
            Add("TepuiStone");var e=Add("AshBed");Body(e);
            switch(fault){case "source":e.RemovePart(e.GetPart<TileStateSourcePart>());break;case "duplicate-source":e.AddPart(new TileStateSourcePart());break;case "foreign-source":e.GetPart<TileStateSourcePart>().ParentEntity=new Entity();break;case "render":e.GetPart<RenderPart>().RenderString="*";break;case "hidden":e.GetPart<RenderPart>().Visible=false;break;case "material":e.GetPart<MaterialPart>().MaterialID="Stone";break;case "heat":e.GetPart<TileStateSourcePart>().Heat=2;break;case "radiation":e.GetPart<TileStateSourcePart>().RadiatesEnergy=false;break;case "coating":e.GetPart<TileStateSourcePart>().Coating="oil";break;case "charge":e.GetPart<TileStateSourcePart>().Charge=1;break;case "capacity":e.GetPart<ThermalPart>().HeatCapacity=1;break;case "nonfinite":e.GetPart<ThermalPart>().Temperature=float.NaN;break;case "solid":e.GetPart<PhysicsPart>().Solid=true;break;case "carried":e.GetPart<PhysicsPart>().InInventory=new Entity();break;case "service":e.AddPart(new WellPart());break;case "duplicate-physics":e.AddPart(new PhysicsPart());break;}
            using(var catalog=new FlatPixelSharedSteamVentCatalog()){Assert.False(catalog.TryResolve(e,out _));}
            zone.TileState.AddHeat(X,Y,1);Assert.AreNotSame(Texture(Ash),View().ForegroundTile.sprite.texture);Assert.AreNotSame(Texture(Stone),View().GroundTile.sprite.texture);
        }
        [Test] public void TemperatureEvolutionDoesNotInventOrRenewHazards()
        {
            Add("TepuiStone");var e=Add("AshBed");Body(e);zone.TileState.WriteCloud(X,Y,"steam",2);string state=zone.TileState.ToSaveString();int version=zone.EntityVersion;
            foreach(float t in new[]{-20f,0f,44.347f,180f,500f}){e.GetPart<ThermalPart>().Temperature=t;Body(e);Native(View().GroundTile,Stone);}
            Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(version,zone.EntityVersion);
        }
        [TestCase("heat",'^')][TestCase("charge",'*')][TestCase("water",'~')][TestCase("steam",'\'')]
        public void ActualPainterKeepsIndependentHazardsAndCoverPriority(string effect,char glyph)
        {
            Add("TepuiStone");var e=Add("AshBed");Body(e);if(effect=="heat")e.GetPart<TileStateSourcePart>().Seed(zone,X,Y);if(effect=="charge")zone.TileState.AddCharge(X,Y,1);if(effect=="water")zone.TileState.WriteCoating(X,Y,"water",2);if(effect=="steam")zone.TileState.WriteCloud(X,Y,"steam",2);string state=zone.TileState.ToSaveString();
            using(var paint=new Paint(world,zone)){paint.Step();Native(paint.At(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile(glyph),paint.Mark(X,Y));var actor=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(actor,View().Owner);Assert.AreNotSame(Texture(Ash),paint.At(X,Y).sprite.texture);Assert.AreSame(CP437TilesetGenerator.GetTile(glyph),paint.Mark(X,Y));Assert.True(zone.RemoveEntity(actor));paint.Dirty(X,Y);paint.Step();Native(paint.At(X,Y));Assert.AreEqual(state,zone.TileState.ToSaveString());}
        }
        [Test] public void HiddenRememberedMovedAndReplacedOwnersLoseCurrentReaderAuthority()
        {
            var e=Add("AshBed");Body(e);var d=Document(e);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();Assert.AreSame(Texture(Ash),portraits.GetPortrait(d,paint.Renderer).Texture);
                zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(d,paint.Renderer).Texture);
                zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));zone.GetCell(X,Y).IsVisible=zone.GetCell(X,Y).Explored=true;
                Assert.True(zone.RemoveEntity(e));Assert.True(zone.AddEntity(e,X+1,Y));Assert.IsNull(portraits.GetPortrait(d,paint.Renderer).Texture);
                var old=zone;zone=Attach(old.ZoneID);Assert.IsNull(portraits.GetPortrait(d,paint.Renderer).Texture);
            }
        }
        [TestCase(false)][TestCase(true)] public void AshDoesNotAuthorizeHotBackingInSitesOrFelling(bool felling)
        {
            int x=X,y=Y;
            if(felling)
            {
                manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);
                zone=manager.GetZone(FellingSiteBuilder.ZoneID);manager.SetActiveZone(zone);
                foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
                var cell=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="TepuiStone"&&!c.Objects[0].GetPart<PhysicsPart>().Solid);
                x=cell.X;y=cell.Y;
            }
            else { Add("TepuiStone");manager.WorldMap.SetPOI(2,3,new PointOfInterest(POIType.Lair,"foreign site")); }
            var ash=Add("AshBed",x,y);ash.GetPart<TileStateSourcePart>().Seed(zone,x,y);
            Assert.True(world.TryResolvePresentation(zone,x,y,out var view));Native(view.ForegroundTile);
            Assert.AreNotSame(Texture(Stone),view.GroundTile.sprite.texture);
        }
        [Test] public void DuplicateOrForeignGroundCannotBorrowStumpBacking()
        {
            Add("TepuiStone");var e=Add("AshBed");Body(e);Native(View().GroundTile,Stone);var other=Add("TepuiStone");Assert.AreNotSame(Texture(Stone),View().GroundTile.sprite.texture);zone.RemoveEntity(other);Add("Sand");Assert.AreNotSame(Texture(Stone),View().GroundTile.sprite.texture);
        }
        [Test] public void OptionalAshResourceIsIndependentAndBorrowedTextureSurvivesDisposal()
        {
            var ash=Add("AshBed");var vent=content.Factory.CreateEntity("SteamVent");var resource=Texture(Ash);var natural=Texture("Sprites/Environment/steam_vent");
            using(var absent=Inject(natural,null)){Assert.False(absent.TryResolve(ash,out _));Assert.True(absent.TryResolve(vent,out _));}
            var malformed=new Texture2D(16,16);try{using(var bad=Inject(natural,malformed))Assert.False(bad.TryResolve(ash,out _));}finally{Object.DestroyImmediate(malformed);}
            var a=Inject(null,resource);Assert.True(a.TryResolve(ash,out var tile));var sprite=tile.sprite;a.Dispose();a.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(resource!=null);Assert.False(a.TryResolve(ash,out _));
            using(var legacy=new FlatPixelSharedSteamVentCatalog(natural)){Assert.False(legacy.TryResolve(ash,out _));Assert.True(legacy.TryResolve(vent,out _));}
        }
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, main, ground;
            public ZoneRenderer Renderer { get; }
            public Transform RootTransform => root.transform;
            public Tilemap Main => main;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone z)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Shared AshBed submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var marks = new GameObject("Hazards", typeof(Tilemap)); marks.transform.SetParent(root.transform, false);
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", ground); Write(Renderer, "_tileStateTilemap", marks.GetComponent<Tilemap>());
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, ground); env.RenderingEnabled = true; env.FlatPixelGreyboxEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap"); Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            static Vector3Int Position(int x, int y) => new Vector3Int(x, Zone.Height - 1 - y, 0);
            public Tile At(int x, int y) { var at = Position(x, y); return overlay.GetTile<Tile>(at) ?? main.GetTile<Tile>(at); }
            public Matrix4x4 Transform(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetTransformMatrix(at) : main.GetTransformMatrix(at); }
            public Color Tint(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetColor(at) : main.GetColor(at); }
            public Color Lit(Color color, int x, int y) { var light = (LightMap)Read(Renderer, "_lightMap"); return light == null ? color : light.ApplyToColor(color, x, y); }
            public Tile Mark(int x,int y) => ((Tilemap)Read(Renderer,"_tileStateTilemap")).GetTile<Tile>(Position(x,y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Shared AshBed fixture");
            public void Step() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try
                {
                    // The fixture owns this catalog across regions/save checks.
                    // EnvironmentSpriteRenderer owns its field in production,
                    // so detach this borrowed test injection before teardown.
                    if (saved?.Root != null)
                    {
                        var environment = saved.Root.GetComponent<EnvironmentSpriteRenderer>();
                        if (environment != null)
                        {
                            Write(environment, "_flatPixelGreybox", null);
                            // Inactive fixture roots do not receive Unity OnDestroy.
                            typeof(EnvironmentSpriteRenderer).GetMethod("OnDestroy", I).Invoke(environment, null);
                        }
                    }
                    saved?.Dispose(); if (root != null) Object.DestroyImmediate(root);
                }
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
