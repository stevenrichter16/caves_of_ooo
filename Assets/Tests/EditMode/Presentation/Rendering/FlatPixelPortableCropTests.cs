using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Two existing automatic-yield crop bodies, not a new growth mechanic.
    // NorthPlot/PlotProduct/Global/CropTime retain their broader regressions.
    public sealed class FlatPixelPortableCropTests
    {
        const int X=10,Y=10;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        readonly List<Action> restore=new List<Action>();
        readonly List<Texture2D> textures=new List<Texture2D>();
        HotbarSaveFixture globals;EntityFactory factory;OverworldZoneManager manager;Zone zone;
        FlatPixelGreyboxCatalog world;Texture2D states,plot;
        [SetUp] public void Setup()
        {
            foreach(var type in new[]{typeof(MessageLog),typeof(EquipmentChangeBus),typeof(WorldAudioHooks),typeof(WorldAudioUtility),typeof(WorldAudioWork),typeof(EntityVisualHooks)})
                foreach(var field in type.GetFields(S))if(!field.IsLiteral)Snapshot(field);
            Snapshot(typeof(AnatomyFactory).GetField("_types",S));Snapshot(typeof(BodyPart).GetField("_nextID",S));
            foreach(string name in new[]{"_buffer","_writeIndex","_filledCount","_droppedCount"})Snapshot(typeof(Diag).GetField(name,S));
            globals=new HotbarSaveFixture(false,false);MessageLog.OnMessage=null;MessageLog.Clear();WorldAudioHooks.Callback=null;WorldAudioUtility.Callback=null;WorldAudioWork.Callback=null;EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            factory=DensityReferenceGladeTests.Factory();manager=OverworldZoneManager.CreateDetached(factory,65,true);
            Attach("Overworld.18.8.0");world=new FlatPixelGreyboxCatalog();CropSystem.Factory=factory;
            states=Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.StatesResourcePath);plot=Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.PlotResourcePath);
        }
        [TearDown] public void Cleanup()
        {try{world?.Dispose();foreach(var t in textures)if(t!=null)Object.DestroyImmediate(t);textures.Clear();globals?.Dispose();}
         finally{for(int i=restore.Count-1;i>=0;i--)restore[i]();restore.Clear();}}
        void Attach(string id){zone=new Zone(id);manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;}
        Entity Owner(string name){var e=factory.CreateEntity(name);Assert.NotNull(e,name);return e;}
        Entity Add(string name,int x=X,int y=Y){var e=Owner(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);textures.Add(t);return t;}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static void Write(object o,string name,object value){var f=o.GetType().GetField(name,I);Assert.NotNull(f,name);f.SetValue(o,value);}
        static object Read(object o,string name){var f=o.GetType().GetField(name,I);Assert.NotNull(f,name);return f.GetValue(o);}
        static void Frame(Tile tile,Texture2D texture,int index)
        {
            Assert.NotNull(texture);Assert.NotNull(tile);Assert.AreSame(texture,tile.sprite.texture);Assert.AreEqual(new Rect(index*16,0,16,24),tile.sprite.rect);
            Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
        }
        Tile Native(Entity owner,Texture2D texture,int index)
        {
            var c=zone.GetEntityCell(owner);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(owner,v.Owner);
            Assert.True(v.NativeForeground,"Exact planted crop still uses glyph fallback outside the opening.");Frame(v.ForegroundTile,texture,index);Assert.AreEqual(Color.white,v.ForegroundColor);return v.ForegroundTile;
        }
        static void Portrait(Reader reader,AnnouncementDocument document,Texture2D texture,int index)
        {
            Assert.NotNull(document);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document,out var image));Assert.AreSame(texture,image.Texture);
            Assert.AreEqual(new Rect(index*16f/texture.width,0,16f/texture.width,1),image.Uv);Assert.False(image.IsModel);Assert.AreEqual(Color.white,image.Tint);
        }
        static void Retired(Reader r,AnnouncementDocument d)
        {Assert.False(ExamineDocumentBuilder.IsCurrentSubject(d));Assert.True(r.Presentation.TryGetFlatExaminePortrait(d,out var image));Assert.IsNull(image.Texture);}
        FlatPixelGreyboxCatalog Inject(Texture2D planted,Texture2D sprouts)=>new FlatPixelGreyboxCatalog(null,null,null,planted,null,null,null,null,sprouts);
        FlatPixelOpeningArtCatalog Art(Texture2D planted,Texture2D sprouts)=>new FlatPixelOpeningArtCatalog(null,null,null,planted,null,null,null,null,sprouts);
        static void NotCropTexture(Texture2D texture,Texture2D planted,Texture2D sprouts){if(planted!=null)Assert.AreNotSame(planted,texture);if(sprouts!=null)Assert.AreNotSame(sprouts,texture);}
        void Refused(Entity owner,FlatPixelOpeningArtCatalog art)
        {
            Assert.False(art.TryResolve(owner,X,Y,out _));Assert.False(View().NativeForeground);
            if(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var modern,out _))NotCropTexture(modern.texture,states,plot);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone)){Assert.True(world.TryResolve(zone,X,Y,out var legacy));NotCropTexture(legacy.ForegroundTile?.sprite?.texture,states,plot);}
            if(world.TryGetSubjectSprite(owner,X,Y,out var subject,out _))NotCropTexture(subject.texture,states,plot);
        }

        [TestCase("CandyCarrotCrop",0)] [TestCase("EmberwheatCrop",1)]
        public void ExactCurrentWorldAndReaderReuseBothStandingStagesAcrossRegions(string name,int sprout)
        {
            foreach(string id in new[]{"Overworld.18.8.0",ReferenceGladePlan.ZoneID})
            {
                Attach(id);var owner=Add(name);var crop=owner.GetPart<CropPart>();Assert.False(crop.HarvestAtMaturity);
                using(var reader=new Reader(world,zone,Owner("Player")))for(int stage=0;stage<2;stage++)
                {
                    crop.GrowthStage=stage;var texture=stage==0?states:plot;int index=stage==0?6:sprout;
                    var body=Native(owner,texture,index);Assert.AreSame(body,Native(owner,texture,index));
                    Portrait(reader,ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetEntityCell(owner),reader.Viewer),texture,index);
                    Assert.True(world.TryGetSubjectSprite(owner,X,Y,out var sprite,out _));Assert.AreSame(body.sprite,sprite);
                    if(FlatPixelGreyboxCatalog.IsAdmitted(zone)){Assert.True(world.TryResolve(zone,X,Y,out var legacy));Frame(legacy.ForegroundTile,texture,index);}
                    Assert.AreEqual(0,crop.MoistureTicks);Assert.AreEqual(0,crop.TicksInStage);Assert.AreEqual(-1,crop.LastGrowthWorldTick,"Rendering must not initialize or advance the crop clock.");
                }
            }
        }

        [Test] public void ExistingOptionalAtlasInputsStayIndependentWithoutImplicitReload()
        {
            var planted=Texture(192,24);var sprouts=Texture(128,24);var bad=Texture(127,24);var badStates=Texture(191,24);
            foreach(var optional in new[]{sprouts,null,bad})
            {
                world.Dispose();world=Inject(planted,optional);Attach("Overworld.18.8.0");var owner=Add("CandyCarrotCrop");Native(owner,planted,6);
                owner.GetPart<CropPart>().GrowthStage=1;Assert.AreEqual(optional==sprouts,View().NativeForeground);
                if(optional==sprouts)Native(owner,sprouts,0);
            }
            foreach(var optional in new[]{null,badStates})
            {
                world.Dispose();world=Inject(optional,sprouts);Attach("Overworld.18.8.0");var owner=Add("EmberwheatCrop");Assert.False(View().NativeForeground);
                owner.GetPart<CropPart>().GrowthStage=1;Native(owner,sprouts,1);
            }
            world.Dispose();world=new FlatPixelGreyboxCatalog(null,null,null,planted);Attach("Overworld.18.8.0");var old=Add("EmberwheatCrop");Native(old,planted,6);
            old.GetPart<CropPart>().GrowthStage=1;Assert.False(View().NativeForeground,"Old four-input injection must not load a missing plot atlas.");
        }

        [Test] public void SemanticOrForeignOwnerRefusalCannotBorrowTheLocalPlotFallback()
        {
            using(var art=Art(states,plot))foreach(string id in new[]{ReferenceGladePlan.ZoneID,"Overworld.18.8.0"})
            {
                Attach(id);var owner=Add("CandyCarrotCrop");Native(owner,states,6);var crop=owner.GetPart<CropPart>();
                var render=owner.GetPart<RenderPart>();var physical=owner.GetPart<PhysicsPart>();var examine=owner.GetPart<ExaminablePart>();
                foreach(var part in new Part[]{crop,render,physical,examine}){part.ParentEntity=new Entity();Refused(owner,art);part.ParentEntity=owner;Native(owner,states,6);}
                var duplicate=new CropPart();owner.AddPart(duplicate);Refused(owner,art);Assert.True(owner.RemovePart(duplicate));
                foreach(int stage in new[]{-1,2}){crop.GrowthStage=stage;Refused(owner,art);}crop.GrowthStage=0;
                crop.HarvestAtMaturity=true;Refused(owner,art);crop.HarvestAtMaturity=false;
                crop.YieldBlueprint="Emberwheat";Refused(owner,art);crop.YieldBlueprint="CandyCarrot";
                foreach(int count in new[]{0,33}){crop.YieldCount=count;Refused(owner,art);}crop.YieldCount=3;
                render.VisualID="foreign-crop";Refused(owner,art);render.VisualID="";
                physical.Takeable=true;Refused(owner,art);physical.Takeable=false;
                physical.Solid=true;Refused(owner,art);physical.Solid=false;
                physical.InInventory=new Entity();Refused(owner,art);physical.InInventory=null;
                foreach(string tag in new[]{"Item","Creature","Terrain"}){owner.SetTag(tag);Refused(owner,art);owner.Tags.Remove(tag);}
                var container=new ContainerPart();owner.AddPart(container);Refused(owner,art);Assert.True(owner.RemovePart(container));
                owner.BlueprintName="UnrelatedCrop";Refused(owner,art);owner.BlueprintName="CandyCarrotCrop";
                // The picture does not pin mutable compost/yield tuning or fields unused by automatic release.
                crop.TicksPerStage=7;crop.TicksInStage=3;crop.MoistureTicks=4;crop.SeedYieldBlueprint="unused";crop.SeedYieldCount=-1;
                Native(owner,states,6);Assert.True(art.TryResolve(owner,X,Y,out _));
            }
        }

        [TestCase("CandyCarrotCrop",0)] [TestCase("EmberwheatCrop",1)]
        public void RealMoistGrowthAndBlockedFiniteYieldRetireOnlyTheCompletedBody(string name,int sprout)
        {
            Add("Grass");var owner=Add(name);var crop=owner.GetPart<CropPart>();int length=crop.TicksPerStage,unit=CropTime.WorldTicksPerUnit;
            using(var reader=new Reader(world,zone,Owner("Player")))using(var paint=new Paint(world,zone))
            {
                CropSystem.Factory=factory;var planted=Native(owner,states,6);paint.Step();Assert.AreSame(planted,paint.At(X,Y));
                var document=ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetEntityCell(owner),reader.Viewer);
                Assert.True(CropTime.Reconcile(crop,zone,0));int tick=length*unit;Assert.True(CropTime.Reconcile(crop,zone,tick));
                Assert.AreEqual(0,crop.GrowthStage);Assert.AreEqual(0,crop.TicksInStage);Assert.AreSame(planted,Native(owner,states,6),"Dry elapsed time grants no growth.");
                crop.MoistureTicks=length*2+2;tick+=length*unit;Assert.True(CropTime.Reconcile(crop,zone,tick));Assert.AreEqual(1,crop.GrowthStage);
                var body=Native(owner,plot,sprout);paint.Step();Assert.AreSame(body,paint.At(X,Y));Portrait(reader,document,plot,sprout);
                int moisture=crop.MoistureTicks;Assert.True(CropTime.Reconcile(crop,zone,tick));Assert.AreEqual(moisture,crop.MoistureTicks);Assert.AreEqual(0,crop.TicksInStage);
                CropSystem.Factory=null;tick+=length*unit;Assert.True(CropTime.Reconcile(crop,zone,tick));Assert.AreEqual(1,crop.GrowthStage);
                Assert.AreSame(owner,zone.GetCell(X,Y).Objects.Single(e=>e.HasPart<CropPart>()));Assert.AreSame(body,Native(owner,plot,sprout));paint.Step();Assert.AreSame(body,paint.At(X,Y));
                Assert.AreEqual(0,zone.GetCell(X,Y).Objects.Count(e=>e.BlueprintName==crop.YieldBlueprint));
                CropSystem.Factory=factory;tick+=unit;Assert.True(CropTime.Reconcile(crop,zone,tick));Assert.IsNull(zone.GetEntityCell(owner));Retired(reader,document);
                var products=zone.GetCell(X,Y).Objects.Where(e=>e.BlueprintName==crop.YieldBlueprint).ToArray();Assert.AreEqual(crop.YieldCount,products.Length);
                Assert.AreEqual(products.Length,products.Select(e=>e.ID).Distinct().Count());Assert.False(zone.GetCell(X,Y).Objects.Any(e=>e.HasPart<CropPart>()));
                paint.Step();Assert.AreNotSame(planted,paint.At(X,Y));Assert.AreNotSame(body,paint.At(X,Y));Assert.AreNotSame(owner,View().Owner);
                Assert.False(CropTime.Reconcile(crop,zone,tick+unit));CollectionAssert.AreEquivalent(products,zone.GetCell(X,Y).Objects.Where(e=>e.BlueprintName==crop.YieldBlueprint));
            }
        }

        [Test] public void CoveredHiddenAndReplacedGraphOwnersNeverPublishStaleCropBodies()
        {
            var owner=Add("EmberwheatCrop");Native(owner,states,6);
            using(var reader=new Reader(world,zone,Owner("Player")))
            {
                var document=ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetEntityCell(owner),reader.Viewer);Portrait(reader,document,states,6);
                var cover=Add("StoneWall");cover.GetPart<RenderPart>().RenderLayer=100;Assert.AreNotSame(owner,View().Owner);
                zone.RemoveEntity(cover);Native(owner,states,6);zone.GetCell(X,Y).IsVisible=false;
                var remembered=View();Assert.False(remembered.NativeForeground);Retired(reader,document);
                zone.GetCell(X,Y).IsVisible=true;Native(owner,states,6);document=ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetEntityCell(owner),reader.Viewer);
                var prior=zone;Attach(prior.ZoneID);Assert.False(world.TryResolvePresentation(prior,X,Y,out _));
                Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document,out var stale));Assert.IsNull(stale.Texture);
            }
        }

        [Test] public void SharedLegacyCropsDoNotMakeReserveStagesOrTrayPortableAndKeepBorrowedLifetime()
        {
            var planted=Texture(192,24);var sprouts=Texture(128,24);world.Dispose();world=Inject(planted,sprouts);
            var ordinary=Add("CandyCarrotCrop");var body=Native(ordinary,planted,6);var sprite=body.sprite;
            foreach(var name in new[]{"MarlrootCrop","PitchpodCrop","ConnectedReserveTray"})
            {var peer=Add(name,11,Y);if(peer.HasPart<CropPart>())peer.GetPart<CropPart>().GrowthStage=2;Assert.False(View(11,Y).NativeForeground);zone.RemoveEntity(peer);}
            Attach(ReferenceGladePlan.ZoneID);var marl=Add("MarlrootCrop");marl.GetPart<CropPart>().GrowthStage=2;Native(marl,sprouts,3);
            var pitch=Add("PitchpodCrop",11,Y);pitch.GetPart<CropPart>().GrowthStage=2;Native(pitch,sprouts,5);Native(Add("ConnectedReserveTray",12,Y),sprouts,6);
            world.Dispose();Assert.True(body==null&&sprite==null);Assert.True(planted!=null&&sprouts!=null);
            using(var peer=Inject(planted,sprouts)){Assert.True(peer.TryResolvePresentation(zone,X,Y,out var existing));Frame(existing.ForegroundTile,sprouts,3);}
        }

        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            readonly Zone ownedZone;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog,Zone z,Entity viewer)
            {
                Viewer=viewer;ownedZone=z;Assert.True(z.AddEntity(viewer,9,10));root=new GameObject("Portable crop reader");root.SetActive(false);
                Presentation=root.AddComponent<ZoneRenderer>();var environment=root.AddComponent<EnvironmentSpriteRenderer>();
                Write(environment,"_flatPixelGreybox",catalog);environment.FlatPixelGreyboxEnabled=true;
                Write(Presentation,"_envSpriteRenderer",environment);Write(Presentation,"_flatPixelModeEnabled",true);
                typeof(ZoneRenderer).GetProperty("CurrentZone",I).SetValue(Presentation,z);Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose(){if(root!=null){Write(root.GetComponent<EnvironmentSpriteRenderer>(),"_flatPixelGreybox",null);Object.DestroyImmediate(root);}if(ownedZone.GetEntityCell(Viewer)!=null)ownedZone.RemoveEntity(Viewer);}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Portable crop submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Portable crop fixture");
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
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field); object value = field.GetValue(null);
            if (value is Array array) { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary)
            {
                var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry entry in dictionary) copy.Add(entry);
                restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); });
            }
            else if (value is IList list) { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            else if (value is IEnumerable enumerable && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var copy = enumerable.Cast<object>().ToArray(); restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null);
                    foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); });
            }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
