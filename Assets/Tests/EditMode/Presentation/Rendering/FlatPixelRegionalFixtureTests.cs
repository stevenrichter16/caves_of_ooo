using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Regional regression slice: real factory owners and submitted painter, no new mechanics.
    public sealed class FlatPixelRegionalFixtureTests
    {
        const string Grove="Overworld.4.7.0", Sodden="Overworld.18.8.0", Corner="Sprites/Live16x24/nella-corner";
        const int X=10,Y=10;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone,oldSettlement;
        FlatPixelGreyboxCatalog world;
        Dictionary<string,LiquidDefinition> liquids,previousLiquids; bool initialized;
        Action<int,int,string> oldDirty; Action<string> oldFull,oldMessage;
        EntityDamageVisualHandler oldDamage; Action<WorldAudioCombatReceipt> oldAudio;
        [SetUp] public void Setup()
        {
            oldSettlement=SettlementRuntime.ActiveZone;oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            oldMessage=MessageLog.OnMessage;oldDamage=EntityVisualHooks.DamageCallback;oldAudio=WorldAudioCombat.Callback;
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;MessageLog.OnMessage=null;
            EntityVisualHooks.DamageCallback=null;WorldAudioCombat.Callback=null;
            liquids=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",S).GetValue(null);
            previousLiquids=new Dictionary<string,LiquidDefinition>(liquids);initialized=LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t=>t.text));
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();content?.Dispose();}
            finally
            {
                liquids.Clear();foreach(var p in previousLiquids)liquids[p.Key]=p.Value;
                typeof(LiquidRegistry).GetField("_initialized",S).SetValue(null,initialized);
                SettlementRuntime.ActiveZone=oldSettlement;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;
                MessageLog.OnMessage=oldMessage;EntityVisualHooks.DamageCallback=oldDamage;WorldAudioCombat.Callback=oldAudio;
            }
        }
        void Attach(string id)
        {
            var p=WorldMap.FromZoneID(id);Assert.IsNull(manager.WorldMap.GetPOI(p.x,p.y));
            Assert.AreEqual(id==Grove?BiomeType.Grovelands:BiomeType.Sodden,manager.WorldMap.GetBiome(p.x,p.y));
            zone=new Zone(id);manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.IsVisible=c.Explored=true;
            Assert.True(Supported(zone));
        }
        bool Supported(Zone graph)=>graph.ZoneID==Grove?FlatPixelGrovelandsMaterialsCatalog.IsSupportedZone(graph):FlatPixelSoddenTravelCatalog.IsSupportedZone(graph);
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        static object Read(object o,string n){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);return f.GetValue(o);}
        static void Write(object o,string n,object v){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);f.SetValue(o,v);}
        static Texture2D Art(string path){var t=Resources.Load<Texture2D>(path);Assert.NotNull(t,path);return t;}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        string GroundPath=>zone.ZoneID==Grove?"Sprites/Live16x24/grovelands-ground":"Sprites/Live16x24/sodden-ground";
        void NativeGround(Tile tile)
        {
            Assert.NotNull(tile);Assert.AreSame(Art(GroundPath),tile.sprite.texture);
            Assert.AreEqual(new Vector2(16,24),tile.sprite.rect.size);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.That(tile.sprite.rect.x,Is.InRange(0,48));Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
        }
        [TestCase(Grove,"CopperPipe")][TestCase(Grove,"PeatBog")][TestCase(Grove,"SteamVent")]
        public void RegionalFixtureRetainsItsBodyGroundAndExactCoveredReader(string id,string role)
        {
            Attach(id);Add("Grass");var owner=Add(role);var v=View();Assert.True(v.NativeForeground,"Fixture remains a glyph");
            Assert.True(v.LocalGround,"Real regional floor is missing beneath fixture");NativeGround(v.GroundTile);
            Assert.AreEqual(FlatPixelGreyboxGround.Ground,v.GroundKind);Assert.False(v.RepresentedBrine||v.RepresentedMire);
            var resource=Art(role=="SteamVent"?"Sprites/Environment/steam_vent":Corner);Assert.AreSame(resource,v.ForegroundTile.sprite.texture);
            if(role!="SteamVent")Assert.AreEqual(role=="PeatBog"?256:0,v.ForegroundTile.sprite.rect.x);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out _));Assert.AreSame(v.ForegroundTile.sprite,sprite);
            var actor=Add("Player");Assert.AreSame(actor,View().Owner);Assert.True(View().LocalGround);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var underfoot,out _));Assert.AreSame(sprite,underfoot);
            var document=ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetCell(X,Y),actor);
            Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document,out var accepted));
                Assert.AreSame(resource,accepted.Texture);
                zone.GetCell(X,Y).IsVisible=false;Assert.False(View().LocalGround);Assert.False(View().NativeForeground);
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document,out var hiddenPortrait));Assert.IsNull(hiddenPortrait.Texture);
                // Shared SteamVent supplies static owner art; the public document
                // boundary above owns disclosure. Nella additionally checks cells.
                if(role!="SteamVent")
                    Assert.True(!world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var hidden,out _)||hidden.texture!=resource);
                zone.GetCell(X,Y).IsVisible=true;owner.GetPart<PhysicsPart>().InInventory=actor;
                Assert.True(!world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var carried,out _)||carried.texture!=resource);
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document,out var carriedPortrait));Assert.IsNull(carriedPortrait.Texture);
                owner.GetPart<PhysicsPart>().InInventory=null;Assert.True(zone.MoveEntity(owner,X+1,Y));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document,out var movedPortrait));Assert.IsNull(movedPortrait.Texture);
                if(role!="SteamVent")
                    Assert.True(!world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var moved,out _)||moved.texture!=resource);
            }
        }
        [TestCase(Grove,"CopperPipe")][TestCase(Grove,"PeatBog")][TestCase(Grove,"SteamVent")]
        public void RegionalFixtureUnderlayPreservesSubmittedHazardsAndResidualFallback(string id,string role)
        {
            Attach(id);Add("Grass");var owner=Add(role);using(var paint=new Paint(world,zone))
            {
                foreach(string coating in new[]{"water","oil","ice"})
                {
                    zone.TileState.Clear(X,Y);zone.TileState.WriteCoating(X,Y,coating,4);paint.Dirty(X,Y);paint.Step();
                    NativeGround(paint.GroundAt(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark(X,Y));
                }
                zone.TileState.Clear(X,Y);zone.TileState.AddCharge(X,Y,2);paint.Dirty(X,Y);paint.Step();
                NativeGround(paint.GroundAt(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile('*'),paint.Mark(X,Y));
                var conflict=Add("WaterPuddle");Assert.False(View().LocalGround);Assert.True(zone.RemoveEntity(conflict));
                zone.TileState.Clear(X,Y);zone.TileState.WriteCoating(X,Y,"water",4);Assert.True(View().LocalGround);
                owner.GetPart<RenderPart>().VisualID="custom fixture";Assert.False(View().LocalGround);
                owner.GetPart<RenderPart>().VisualID=null;Assert.True(View().LocalGround);
                Assert.True(zone.RemoveEntity(owner));zone.TileState.WriteCoating(X,Y,"water",4);paint.Dirty(X,Y);paint.Step();
                Assert.False(View().LocalGround);Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark(X,Y));
            }
        }
        [TestCase(Grove)]
        public void PipeBodyAndFloorCannotEscapeCurrentBiomePoiOrSavedLairAuthority(string id)
        {
            Attach(id);Add("Grass");var pipe=Add("CopperPipe");Assert.True(View().NativeForeground);Assert.True(View().LocalGround);
            var p=WorldMap.FromZoneID(id);var expected=manager.WorldMap.GetBiome(p.x,p.y);
            foreach(string fault in new[]{"poi","biome","cached"})
            {
                if(fault=="poi")manager.WorldMap.SetPOI(p.x,p.y,new PointOfInterest(POIType.MerchantCamp,"counter"));
                if(fault=="biome")manager.WorldMap.Tiles[p.x,p.y]=BiomeType.Cave;
                if(fault=="cached")manager.CachedZones[id]=new Zone(id);
                Assert.False(Supported(zone),fault);
                if(world.TryResolvePresentation(zone,X,Y,out var v))
                {Assert.False(v.NativeForeground,fault);Assert.False(v.LocalGround,fault);}
                Assert.True(!world.TryGetPresentationSubjectSprite(zone,pipe,X,Y,out var image,out _)||image.texture!=Art(Corner),fault);
                manager.CachedZones[id]=zone;manager.WorldMap.Tiles[p.x,p.y]=expected;manager.WorldMap.SetPOI(p.x,p.y,null);
                Assert.True(View().NativeForeground);Assert.True(View().LocalGround);
            }
            manager.CachedZones.Remove(id);manager.WorldMap.SetPOI(p.x,p.y,new PointOfInterest(POIType.Lair,"authority probe",null,1,"MarlbackScrabbler"));
            LairStacks.Restore(manager,null);zone=manager.GetZone(id);manager.SetActiveZone(zone);Assert.NotNull(LairStacks.Inspect(manager,id));
            manager.WorldMap.SetPOI(p.x,p.y,null);Assert.False(Supported(zone));Assert.NotNull(LairStacks.Inspect(manager,id));
            // Use a fresh exact pipe in the actual saved-lair graph, not the detached old owner.
            foreach(var c in zone.Cells)c.IsVisible=c.Explored=true;
            using(var selector=new FlatPixelFallbackCatalog())using(var native=new FlatPixelNellaCornerCatalog(Art(Corner),selector))
            {var source=Add("CopperPipe");Assert.False(native.TryGetSubjectSprite(zone,source,X,Y,out _));}
        }
        [TestCase(Grove)]
        public void MissingCornerArtKeepsGlyphFallbackIndependentOfNativeGroundAndHazards(string id)
        {
            Attach(id);Add("Grass");var pipe=Add("CopperPipe");Assert.True(View().NativeForeground);
            var previous=Read(world,"_nellaCornerArt");
            using(var selector=new FlatPixelFallbackCatalog())using(var missing=new FlatPixelNellaCornerCatalog(null,selector))
            {
                try
                {
                    Write(world,"_nellaCornerArt",missing);Assert.False(View().NativeForeground);Assert.True(View().LocalGround);
                    Assert.True(world.TryGetPresentationSubjectSprite(zone,pipe,X,Y,out var sprite,out _));Assert.AreNotSame(Art(Corner),sprite.texture);
                    zone.TileState.WriteCoating(X,Y,"oil",4);using(var paint=new Paint(world,zone))
                    {paint.Step();NativeGround(paint.GroundAt(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark(X,Y));}
                }
                finally{Write(world,"_nellaCornerArt",previous);}
            }
        }
        // Actual OpenMire audit found no disclosed target fixtures. Keep Sodden
        // outside this admission change; its native MirePool is a different owner.
        [TestCase("CopperPipe")][TestCase("PeatBog")][TestCase("SteamVent")]
        public void UnauditedSoddenFixturesKeepExistingFallbackAndSharedVentBody(string role)
        {
            Attach(Sodden);Add("Grass");var owner=Add(role);zone.TileState.WriteCoating(X,Y,"water",4);
            var v=View();Assert.False(v.LocalGround,"No unobserved Sodden fixture-underlay extension");
            Assert.AreEqual(role=="SteamVent",v.NativeForeground,"Only the existing globally shared vent body is native");
            if(role!="SteamVent")
            {
                Assert.AreNotSame(Art(Corner),v.ForegroundTile.sprite.texture);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out _));
                Assert.AreNotSame(Art(Corner),sprite.texture);
            }
            using(var paint=new Paint(world,zone))
            {paint.Step();Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark(X,Y));}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Regional fixture grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public Tile GroundAt(int x,int y) => ground.GetTile<Tile>(Position(x,y));
            public Matrix4x4 Transform(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetTransformMatrix(at) : main.GetTransformMatrix(at); }
            public Color Tint(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetColor(at) : main.GetColor(at); }
            public Color Lit(Color color, int x, int y) { var light = (LightMap)Read(Renderer, "_lightMap"); return light == null ? color : light.ApplyToColor(color, x, y); }
            public Tile Mark(int x,int y) => ((Tilemap)Read(Renderer,"_tileStateTilemap")).GetTile<Tile>(Position(x,y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Regional fixture fixture");
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
                        if (environment != null) Write(environment, "_flatPixelGreybox", null);
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
