using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Shared surface vines reuse approved Sill pixels, preserving real
    /// wall/disclosure semantics. Initial native run precedes the new adapter.</summary>
    public sealed class FlatPixelSharedVineWallTests
    {
        const string Home = "Overworld.18.8.0", Resource = "Sprites/Live16x24/sill-envelope";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Zone zone, int x, int y, out Tile tile);
        delegate bool Portrait(Zone zone, Entity owner, int x, int y, out Sprite sprite);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        FlatPixelFallbackCatalog selector;
        IDisposable adapter;
        Query query;
        Portrait portrait;
        Texture2D atlas;
        Zone zone;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = Attach(Home); world = new FlatPixelGreyboxCatalog();
            atlas = Resources.Load<Texture2D>(Resource); Assert.NotNull(atlas);
        }
        [TearDown] public void Cleanup()
        { world?.Dispose(); adapter?.Dispose(); selector?.Dispose(); content?.Dispose(); }
        Zone Attach(string id)
        {
            var graph = new Zone(id); manager.SetActiveZone(graph);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                graph.GetCell(x,y).Explored = graph.GetCell(x,y).IsVisible = true;
            return graph;
        }
        Entity Add(string name = "VineWall", int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e,x,y)); return e; }
        static void Write(object target, string name, object value)
        { var f = target.GetType().GetField(name,I); Assert.NotNull(f,name); f.SetValue(target,value); }
        static object Read(object target, string name) => target.GetType().GetField(name,I).GetValue(target);
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone,x,y,out var value)); return value; }
        void Frame(Tile tile, int mask)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(atlas,tile.sprite.texture);
            Assert.AreEqual(new Rect(mask*16,0,16,24),tile.sprite.rect);
            Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot); Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None,tile.flags); Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
        }
        Tile Native(int mask = 0)
        { var value = View(); Assert.True(value.NativeForeground,"A disclosed surface VineWall still renders as a glyph."); Frame(value.ForegroundTile,mask); Assert.AreEqual(Color.white,value.ForegroundColor); return value.ForegroundTile; }
        void Fallback(int x = X, int y = Y)
        {
            Assert.True(world.TryResolveFallback(zone,x,y,out var expected)); var value=View(x,y);
            Assert.False(value.NativeForeground); Assert.AreSame(expected.Owner,value.Owner);
            Assert.AreSame(expected.ForegroundTile,value.ForegroundTile);
        }
        void Create(Texture2D source, bool inject = false)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSharedVineWallCatalog");
            Assert.NotNull(type,"The shared exact-owner VineWall adapter does not exist.");
            selector = new FlatPixelFallbackCatalog();
            adapter = (IDisposable)Activator.CreateInstance(type,new object[]{source,selector});
            query = (Query)Delegate.CreateDelegate(typeof(Query),adapter,type.GetMethod("TryResolve",I));
            portrait = (Portrait)Delegate.CreateDelegate(typeof(Portrait),adapter,type.GetMethod("TryGetSubjectSprite",I));
            if (inject) { world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); Write(world,"_sharedVineWallArt",adapter); }
        }
        void Neighbors(int mask)
        {
            if((mask&1)!=0)Add(x:X,y:Y-1); if((mask&2)!=0)Add(x:X+1);
            if((mask&4)!=0)Add(x:X,y:Y+1); if((mask&8)!=0)Add(x:X-1);
        }

        [TestCase(Home)] [TestCase("Overworld.12.12.0")] [TestCase("Overworld.11.10.0")]
        public void OrdinaryWorldAndReaderReuseExactApprovedVinesWithoutChangingWallMeaning(string id)
        {
            zone=Attach(id);var owner=Add();Neighbors(10);Native(10);
            Assert.AreSame(owner,View().Owner); Assert.True(View().BlocksMovement); Assert.True(zone.GetCell(X,Y).IsWall());
            Assert.AreEqual(14,owner.GetPart<DestructiblePart>().HP);Assert.AreEqual("Plant",owner.GetPart<MaterialPart>().MaterialID);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out var tint));
            Assert.AreSame(atlas,sprite.texture);Assert.AreEqual(new Rect(0,0,16,24),sprite.rect);Assert.AreEqual(Color.white,tint);
            if(id=="Overworld.11.10.0") { Assert.True(world.TryResolve(zone,X,Y,out var legacy));Assert.True(legacy.NativeForeground);Frame(legacy.ForegroundTile,10); }
            else Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone),"Shared scenery cannot admit the opening kit.");
        }

        [Test] public void AllMasksUseOnlyCardinalsAndCacheFramesWithoutMutatingTheWorld()
        {
            Create(atlas);var tiles=new HashSet<Tile>();
            for(int mask=0;mask<16;mask++)
            {
                zone=Attach(Home);var center=Add();Neighbors(mask);Add(x:X-1,y:Y-1);Add(x:X+2);
                int version=zone.EntityVersion;Assert.True(query(zone,X,Y,out var tile));Frame(tile,mask);Assert.True(tiles.Add(tile));
                for(int i=0;i<16;i++){Assert.True(query(zone,X,Y,out var warm));Assert.AreSame(tile,warm);}
                Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(14,center.GetPart<DestructiblePart>().HP);
                Assert.True(portrait(zone,center,X,Y,out var sprite));Assert.AreEqual(new Rect(0,0,16,24),sprite.rect);
            }
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("covered")]
        public void NeighborDisclosureAndSelectionDetermineConnections(string state)
        {
            Add();Add(x:X+1);Native(2);var east=zone.GetCell(X+1,Y);Entity cover=null;
            if(state=="hidden")east.IsVisible=false;
            if(state=="unexplored")east.Explored=false;
            if(state=="covered")cover=Add("Dagger",X+1);
            Native(0);east.Explored=east.IsVisible=true;if(cover!=null)zone.RemoveEntity(cover);Native(2);
            zone.GetCell(X,Y).IsVisible=false;Fallback();Assert.True(View().Remembered);
            zone.GetCell(X,Y).IsVisible=true;Native(2);
        }

        [Test] public void HedgeStoneTreeAndSameGlyphDisguisesNeverMakeVineConnections()
        {
            Create(atlas);
            foreach(string name in new[]{"Hedge","StoneWall","Tree"})
            {
                zone=Attach(Home);Add();var east=Add(x:X+1);Assert.True(query(zone,X,Y,out var tile));Frame(tile,2);
                zone.RemoveEntity(east);var other=Add(name,X+1);other.GetPart<RenderPart>().RenderString="#";
                Assert.True(query(zone,X,Y,out tile));Frame(tile,0);Assert.False(query(zone,X+1,Y,out _));
                Assert.False(portrait(zone,other,X+1,Y,out _));
            }
        }

        static readonly string[] VisualFaults={"visual-id","variant","glyphs","glyph","layer","color","background","foreign-render","duplicate-render"};
        static readonly string[] BodyFaults={"gone","dead","missing-damage","foreign-damage","non-solid","foreign-physics","material","foreign-material","missing-thermal","foreign-thermal"};
        static readonly string[] CustodyFaults={"carried","equipped","takeable","item","creature","wall-tag","vegetation-tag","footprint","zero-stack","container","repair"};
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void InvalidCenterAndNeighborCannotBorrowCanonicalWallArt(int group)
        {
            Create(atlas);
            foreach(string fault in group==0?VisualFaults:group==1?BodyFaults:CustodyFaults)
            {
                zone=Attach(Home);var center=Add();var east=Add(x:X+1);
                Assert.True(query(zone,X,Y,out var tile),fault);Frame(tile,2);
                Fault(east,fault);Assert.True(query(zone,X,Y,out tile),fault);Frame(tile,0);
                Fault(center,fault);Assert.False(query(zone,X,Y,out _),fault);Assert.False(portrait(zone,center,X,Y,out _),fault);
            }
        }

        [TestCase("burning")] [TestCase("frozen")] [TestCase("nan")]
        public void NonNeutralThermalBodyFallsBackThenRestoresWithoutChangingDamage(string state)
        {
            var center=Add();var east=Add(x:X+1);Native(2);
            var thermal=east.GetPart<ThermalPart>();float previous=thermal.Temperature;
            thermal.Temperature=state=="burning"?thermal.FlameTemperature:state=="frozen"?thermal.FreezeTemperature:float.NaN;
            Native(0);Fallback(X+1,Y);thermal.Temperature=previous;Native(2);
            Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(center,1,null,zone));
            Assert.Less(center.GetPart<DestructiblePart>().HP,14);Native(2);
        }

        [Test] public void RemovedStaleForeignGraphAndFogNeverProduceLiveWallOrNeighborArt()
        {
            Create(atlas);var center=Add();var east=Add(x:X+1);Assert.True(query(zone,X,Y,out var tile));Frame(tile,2);
            Assert.True(zone.MoveEntity(east,X+2,Y));zone.GetCell(X+1,Y).Objects.Add(east);
            var phantom=content.Factory.CreateEntity("VineWall");phantom.ID=east.ID;zone.GetCell(X+1,Y).Objects.Add(phantom);
            Assert.True(query(zone,X,Y,out tile));Frame(tile,0);Assert.False(portrait(zone,east,X+1,Y,out _));
            manager.CachedZones[Home]=new Zone(Home);Assert.False(query(zone,X,Y,out _));Assert.False(portrait(zone,center,X,Y,out _));
            manager.CachedZones[Home]=zone;Assert.True(query(zone,X,Y,out _));
            zone.GetCell(X,Y).IsVisible=false;Assert.False(query(zone,X,Y,out _));Assert.False(portrait(zone,center,X,Y,out _));
            zone.GetCell(X,Y).IsVisible=true;zone.GetCell(X,Y).Explored=false;Assert.False(query(zone,X,Y,out _));
            zone.GetCell(X,Y).Explored=true;Assert.True(zone.RemoveEntity(center));Assert.False(portrait(zone,center,X,Y,out _));
        }

        [TestCase("Overworld.18.8.1")] [TestCase("VineStudy")]
        public void UndergroundAndUnaddressedGraphsRetainExistingPresentation(string id)
        { Create(atlas);zone=Attach(Home);Add();Assert.True(query(zone,X,Y,out _));zone=Attach(id);var wall=Add();Assert.False(query(zone,X,Y,out _));Assert.False(portrait(zone,wall,X,Y,out _));Fallback(); }

        [TestCase("Overworld.10.10.0","_sillEnvelopeArt")]
        [TestCase("Overworld.4.7.0","_grovelandsMaterialsArt")]
        public void ExistingRegionalVinesRetainTheirOriginalAdapterAndPixels(string id,string field)
        {
            zone=Attach(id);var owner=Add();Neighbors(10);Native(10);Assert.NotNull(Read(world,field));
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out _));Assert.AreSame(atlas,sprite.texture);Assert.AreEqual(new Rect(0,0,16,24),sprite.rect);
            var render=owner.GetPart<RenderPart>();render.VisualID="other-wall";Fallback();
        }

        [TestCase("Overworld.10.10.0",10,10)] [TestCase("Overworld.4.7.0",4,7)]
        public void RevokedRegionalAuthorityCannotFallThroughToTheSharedSurfaceRoute(string id,int wx,int wy)
        {
            zone=Attach(id);var wall=Add();Neighbors(2);Native(2);
            var map=manager.WorldMap;var tiles=map.Tiles;var pois=map.POIs;var biome=tiles[wx,wy];var poi=pois[wx,wy];
            foreach(string fault in new[]{"biome","poi","map-shape","poi-shape"})
            {
                try
                {
                    if(fault=="biome")tiles[wx,wy]=BiomeType.Sodden;
                    if(fault=="poi")map.SetPOI(wx,wy,new PointOfInterest(POIType.MerchantCamp,"changed saved site"));
                    if(fault=="map-shape")map.Tiles=new BiomeType[1,1];
                    if(fault=="poi-shape")map.POIs=new PointOfInterest[1,1];
                    Assert.False(id=="Overworld.10.10.0"?FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone):FlatPixelGrovelandsMaterialsCatalog.IsSupportedZone(zone),fault);
                    Fallback();
                    if(world.TryGetPresentationSubjectSprite(zone,wall,X,Y,out var sprite,out _))Assert.AreNotSame(atlas,sprite.texture,fault);
                }
                finally{map.Tiles=tiles;map.POIs=pois;tiles[wx,wy]=biome;pois[wx,wy]=poi;}
                Native(2);
            }
        }

        [Test] public void SharedSurfaceNeedsIntactMapArraysWithoutRepairingOrChangingThem()
        {
            var wall=Add();Native();var map=manager.WorldMap;var tiles=map.Tiles;var pois=map.POIs;
            foreach(string fault in new[]{"map-null","map-shape","poi-null","poi-shape"})
            {
                try
                {
                    if(fault=="map-null")map.Tiles=null;if(fault=="map-shape")map.Tiles=new BiomeType[1,1];
                    if(fault=="poi-null")map.POIs=null;if(fault=="poi-shape")map.POIs=new PointOfInterest[1,1];
                    var observedTiles=map.Tiles;var observedPois=map.POIs;Fallback();
                    if(world.TryGetPresentationSubjectSprite(zone,wall,X,Y,out var sprite,out _))Assert.AreNotSame(atlas,sprite.texture,fault);
                    Assert.AreSame(observedTiles,map.Tiles);Assert.AreSame(observedPois,map.POIs);
                }
                finally{map.Tiles=tiles;map.POIs=pois;}
                Native();
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingMalformedAndDisposedBorrowedAtlasDoNotReloadOrClaimNative(bool malformed)
        {
            Texture2D bad=malformed?new Texture2D(256,24):null;
            try
            {
                Create(bad,true);Add();Add(x:X+1);Assert.False(query(zone,X,Y,out _));Fallback();
                adapter.Dispose();adapter.Dispose();Assert.True(selector.TryResolve(zone,X,Y,out _));
                if(bad!=null)Assert.True(bad!=null);
                Assert.True(atlas!=null);
            }
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [Test] public void BorrowedApprovedPixelsOutliveCachedFramesAndLegacyInjectionDoesNotLoadThem()
        {
            Create(atlas);Add();Assert.True(query(zone,X,Y,out var tile));var sprite=tile.sprite;
            adapter.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(atlas!=null);
            Assert.True(selector.TryResolve(zone,X,Y,out _));Assert.False(query(zone,X,Y,out _));
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Fallback();
        }

        [Test] public void RealDestructionClearsGapAndCapsSubmittedNeighborsThroughIncrementalPainter()
        {
            Add(x:X-1);var center=Add();Add(x:X+1);Native(10);
            using(var paint=new Paint(world,zone))
            {
                Frame(paint.At(X-1,Y),2);Frame(paint.At(X,Y),10);Frame(paint.At(X+1,Y),8);
                long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(center,999,null,zone));
                Assert.True(center.GetPart<DestructiblePart>().Gone);Assert.IsNull(zone.GetEntityCell(center));
                Assert.False(paint.FullDirty);paint.Step();Frame(paint.At(X-1,Y),0);Frame(paint.At(X+1,Y),0);
                var gap=paint.At(X,Y);Assert.True(gap==null||gap.sprite.texture!=atlas,"Real rubble is allowed; stale intact vine is not.");
                Assert.False(zone.GetCell(X,Y).BlocksMovement());Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
                long cells=EnvironmentSpriteRenderer.Perf.CellsResolved;paint.Step();Assert.AreEqual(cells,EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        static void Fault(Entity owner,string fault)
        {
            var r=owner.GetPart<RenderPart>();var p=owner.GetPart<PhysicsPart>();var d=owner.GetPart<DestructiblePart>();var foreign=new Entity();
            switch(fault)
            {
                case "visual-id":r.VisualID="other-vine";break;case "variant":r.VisualVariant="other-vine";break;
                case "glyphs":r.GlyphVariants="#%";break;case "glyph":r.RenderString="?";break;case "layer":r.RenderLayer=3;break;
                case "color":r.ColorString="&R";break;case "background":r.BackgroundColor="^b";break;
                case "foreign-render":r.ParentEntity=foreign;break;case "duplicate-render":owner.Parts.Add(new RenderPart{ParentEntity=owner});break;
                case "gone":d.Gone=true;break;case "dead":d.HP=0;break;case "missing-damage":owner.RemovePart(d);break;case "foreign-damage":d.ParentEntity=foreign;break;
                case "non-solid":p.Solid=false;break;case "foreign-physics":p.ParentEntity=foreign;break;
                case "material":owner.GetPart<MaterialPart>().MaterialID="Stone";break;case "foreign-material":owner.GetPart<MaterialPart>().ParentEntity=foreign;break;
                case "missing-thermal":owner.RemovePart(owner.GetPart<ThermalPart>());break;case "foreign-thermal":owner.GetPart<ThermalPart>().ParentEntity=foreign;break;
                case "carried":p.InInventory=foreign;break;case "equipped":p.Equipped=foreign;break;case "takeable":p.Takeable=true;break;
                case "item":owner.SetTag("Item");break;case "creature":owner.SetTag("Creature");break;case "wall-tag":owner.Tags.Remove("Wall");break;
                case "vegetation-tag":owner.Tags.Remove("Vegetation");break;case "footprint":owner.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});break;
                case "zero-stack":owner.AddPart(new StackerPart{StackCount=0});break;case "container":owner.AddPart(new ContainerPart());break;case "repair":owner.AddPart(new RepairablePart());break;
            }
        }

        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject grid;
            readonly ZoneRenderer renderer;
            readonly Tilemap overlay;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public bool FullDirty => (bool)Read(renderer, "_fullDirty");
            public Paint(FlatPixelGreyboxCatalog catalog, Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); grid = new GameObject("Shared vine cadence grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var background = new GameObject("Shared vine background", typeof(Tilemap)); background.transform.SetParent(saved.Root.transform, false);
                    renderer = saved.Renderer; Write(renderer, "_tilemap", main); Write(renderer, "_bgTilemap", background.GetComponent<Tilemap>());
                    var camera = new GameObject("Shared vine camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, background.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap");
                    Write(renderer, "_envSpriteRenderer", env); Write(renderer, "_flatPixelModeEnabled", true); renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = renderer.MarkDirty; Step();
                }
                catch { Dispose(); throw; }
            }
            public void Dirty(int x, int y) => renderer.MarkCellDirty(x, y, "shared vine disclosure");
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public void Dispose()
            {
                try { saved?.Dispose(); if (grid != null) Object.DestroyImmediate(grid); }
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
