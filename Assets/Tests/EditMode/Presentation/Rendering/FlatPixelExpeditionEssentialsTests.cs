using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Real world/authorized reader assertions precede resource checks. Reflection
    // keeps the absent small adapter an assertion RED, not a compile dependency.
    public sealed class FlatPixelExpeditionEssentialsTests
    {
        const int X=10,Y=10;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static readonly string[] Names={"HealingTonic","DriedMeat","WateringGrimoire"};
        readonly List<Action> restore=new List<Action>();
        readonly List<Texture2D> textures=new List<Texture2D>();
        HotbarSaveFixture globals;
        EntityFactory factory;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        delegate bool Query(Entity owner,out Tile tile);
        [SetUp] public void Setup()
        {
            foreach(var type in new[]{typeof(MessageLog),typeof(EquipmentChangeBus),typeof(WorldAudioHooks),typeof(WorldAudioUtility),typeof(EntityVisualHooks)})
                foreach(var field in type.GetFields(S))if(!field.IsLiteral)Snapshot(field);
            Snapshot(typeof(AnatomyFactory).GetField("_types",S));Snapshot(typeof(BodyPart).GetField("_nextID",S));
            foreach(string name in new[]{"_buffer","_writeIndex","_filledCount","_droppedCount"})Snapshot(typeof(Diag).GetField(name,S));
            globals=new HotbarSaveFixture(false,false);MessageLog.OnMessage=null;MessageLog.Clear();WorldAudioHooks.Callback=null;WorldAudioUtility.Callback=null;EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            factory=DensityReferenceGladeTests.Factory();manager=OverworldZoneManager.CreateDetached(factory,65,true);Attach("Overworld.18.8.0");world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {try{world?.Dispose();foreach(var t in textures)if(t!=null)Object.DestroyImmediate(t);textures.Clear();globals?.Dispose();}
         finally{for(int i=restore.Count-1;i>=0;i--)restore[i]();restore.Clear();}}
        void Attach(string id){zone=new Zone(id);manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;}
        Entity Owner(string name){var e=factory.CreateEntity(name);Assert.NotNull(e,name);return e;}
        Entity Add(string name,int x=X,int y=Y){var e=Owner(name);Assert.True(zone.AddEntity(e,x,y));return e;}
        static string PathFor(string name)=>name=="WateringGrimoire"?"Sprites/Environment/item_book":"Sprites/Live16x24/opening-items";
        static Rect RectFor(string name)=>name=="WateringGrimoire"?new Rect(0,0,16,16):new Rect(name=="HealingTonic"?112:128,0,16,24);
        static Texture2D Imported(string name){var t=Resources.Load<Texture2D>(PathFor(name));Assert.NotNull(t);Assert.AreEqual(name=="WateringGrimoire"?16:192,t.width);Assert.AreEqual(name=="WateringGrimoire"?16:24,t.height);Assert.AreEqual(FilterMode.Point,t.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,t.wrapMode);Assert.AreEqual(1,t.mipmapCount);return t;}
        static void Frame(Tile tile,string name,Texture2D texture=null)
        {Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(texture??Imported(name),tile.sprite.texture);var r=RectFor(name);Assert.AreEqual(r,tile.sprite.rect);Assert.AreEqual(new Vector2(8,r.height/2),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(new Vector2(1,r.height/16),(Vector2)tile.sprite.bounds.size);Assert.AreEqual(TileFlags.None,tile.flags);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        Tile Native(Entity e)
        {var c=zone.GetEntityCell(e);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground,"Exact expedition supply still uses glyph fallback.");Frame(v.ForegroundTile,e.BlueprintName);Assert.AreEqual(Color.white,v.ForegroundColor);return v.ForegroundTile;}
        static Type ArtType(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelExpeditionEssentialsCatalog");Assert.NotNull(t,"The exact expedition essentials adapter is absent.");return t;}
        static IDisposable Construct(Texture2D items,Texture2D book){var c=ArtType().GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D)});Assert.NotNull(c);return(IDisposable)c.Invoke(new object[]{items,book});}
        static Query Resolve(IDisposable a)=>(Query)Delegate.CreateDelegate(typeof(Query),a,ArtType().GetMethod("TryResolve",new[]{typeof(Entity),typeof(Tile).MakeByRefType()}));
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);textures.Add(t);return t;}
        static void Write(object o,string name,object value){var f=o.GetType().GetField(name,I);Assert.NotNull(f,name);f.SetValue(o,value);}
        static object Read(object o,string name){var f=o.GetType().GetField(name,I);Assert.NotNull(f,name);return f.GetValue(o);}
        AnnouncementDocument Document(Entity e,Entity viewer)=>ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);
        static void Portrait(Reader r,AnnouncementDocument d,string name)
        {Assert.NotNull(d);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));Assert.True(r.Presentation.TryGetFlatExaminePortrait(d,out var image));Assert.AreSame(Imported(name),image.Texture);var rect=RectFor(name);Assert.AreEqual(new Rect(rect.x/image.Texture.width,0,16f/image.Texture.width,1),image.Uv);Assert.False(image.IsModel);Assert.AreEqual(Color.white,image.Tint);}
        static void Rejected(Reader r,AnnouncementDocument d){Assert.NotNull(d);Assert.True(r.Presentation.TryGetFlatExaminePortrait(d,out var image));Assert.IsNull(image.Texture);}
        void Refused(Entity e,string reason)
        {
            using(var a=Construct(Imported("HealingTonic"),Imported("WateringGrimoire"))){Assert.False(Resolve(a)(e,out var tile),reason);Assert.IsNull(tile);}
            Assert.False(View().NativeForeground,reason);
            if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Imported(e.BlueprintName),sprite.texture,reason);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(Imported(e.BlueprintName),old.ForegroundTile?.sprite?.texture,reason);if(world.TryGetSubjectSprite(e,X,Y,out sprite,out _))Assert.AreNotSame(Imported(e.BlueprintName),sprite.texture,reason);}
        }

        [TestCase("HealingTonic")][TestCase("DriedMeat")][TestCase("WateringGrimoire")]
        public void SameCompleteApprovedBodyPaintsAndReadsOutsideAndInsideOpening(string name)
        {
            foreach(string id in new[]{"Overworld.18.8.0",ReferenceGladePlan.ZoneID})
            {
                Attach(id);var e=Add(name);var body=Native(e);
                using(var paint=new Paint(world,zone)){paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Matrix4x4.identity,paint.Transform(X,Y));}
                using(var reader=new Reader(world,zone,Owner("Player")))Portrait(reader,Document(e,reader.Viewer),name);
                if(FlatPixelGreyboxCatalog.IsAdmitted(zone)){Assert.True(world.TryResolve(zone,X,Y,out var legacy));Frame(legacy.ForegroundTile,name);Assert.True(world.TryGetSubjectSprite(e,X,Y,out var sprite,out var tint));Assert.AreEqual(RectFor(name),sprite.rect);Assert.AreSame(Imported(name),sprite.texture);Assert.AreEqual(Color.white,tint);}
            }
        }
        [TestCase("HealingTonic")][TestCase("DriedMeat")][TestCase("WateringGrimoire")]
        public void PausedCarriedReaderUsesItsOwnNativeBodyAndRevokesChangedCustody(string name)
        {
            using(var reader=new Reader(world,zone,Owner("Player")))
            {
                var e=Owner(name);var pack=reader.Viewer.GetPart<InventoryPart>();Assert.True(pack.AddObject(e));reader.Presentation.Paused=true;
                var d=ExamineDocumentBuilder.BuildInventory(e,reader.Viewer);Assert.True(world.TryGetFallbackSubjectSprite(e,X,Y,out var glyph,out _));
                Assert.True(reader.Presentation.TryGetFlatExaminePortrait(d,out var image));Assert.AreNotSame(glyph.texture,image.Texture,"Carried supply still uses glyph art.");Portrait(reader,d,name);
                int count=e.GetPart<StackerPart>().StackCount;for(int i=0;i<3;i++)Portrait(reader,d,name);Assert.AreEqual(count,e.GetPart<StackerPart>().StackCount);
                pack.Objects.Remove(e);Rejected(reader,d);
            }
        }
        [TestCase(false)][TestCase(true)]
        public void OptionalResourcesFailIndependentlyWithoutReloadOrOlderLocalBypass(bool malformed)
        {
            Attach(ReferenceGladePlan.ZoneID);
            for(int missing=0;missing<2;missing++)
            {
                world.Dispose();world=new FlatPixelGreyboxCatalog();
                var bad=malformed?Texture(17,24):null;var a=Construct(missing==0?bad:Imported("HealingTonic"),missing==1?bad:Imported("WateringGrimoire"));Write(world,"_expeditionEssentialsArt",a);
                foreach(string name in Names){var e=Add(name);bool ok=(name=="WateringGrimoire"?1:0)!=missing;Assert.AreEqual(ok,View().NativeForeground,name);
                    Assert.True(world.TryResolve(zone,X,Y,out var legacy));Assert.AreEqual(ok,legacy.NativeForeground,name+" legacy");
                    using(var r=new Reader(world,zone,Owner("Player"))){var d=Document(e,r.Viewer);Assert.True(r.Presentation.TryGetFlatExaminePortrait(d,out var image));if(ok)Portrait(r,d,name);else Assert.AreNotSame(Imported(name),image.Texture);}
                    Assert.True(zone.RemoveEntity(e));}
            }
        }
        [TestCase("HealingTonic","Tonic")][TestCase("DriedMeat","Food")][TestCase("WateringGrimoire","Grimoire")]
        public void EssentialPartsMustBelongUniquelyToTheExactItem(string name,string semantic)
        {
            foreach(string part in new[]{"Render","Physics","Examinable",semantic})foreach(string fault in new[]{"missing","foreign","duplicate"})
            {Attach(ReferenceGladePlan.ZoneID);var e=Add(name);Native(e);var p=e.GetPart(part);Assert.NotNull(p);
             if(fault=="missing")Assert.True(e.RemovePart(p));else if(fault=="foreign")p.ParentEntity=new Entity();else{var copy=(Part)Activator.CreateInstance(p.GetType());copy.ParentEntity=e;e.Parts.Add(copy);}Refused(e,part+" "+fault);}
        }
        [Test] public void ReskinsInvalidStacksAndForeignSemanticOwnersCannotMasqueradeAsSupplies()
        {
            foreach(string name in Names)foreach(string fault in new[]{"visual","variant","glyphs","hidden","no-item","player","solid","immovable","empty","overflow","foreign-stack","container","crop","equipment"})
            {Attach(ReferenceGladePlan.ZoneID);var e=Add(name);Native(e);var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();var s=e.GetPart<StackerPart>();
                switch(fault){case"visual":r.VisualID="other";break;case"variant":r.VisualVariant="other";break;case"glyphs":r.GlyphVariants="!?";break;case"hidden":r.Visible=false;break;case"no-item":e.Tags.Remove("Item");break;case"player":e.SetTag("Player");break;case"solid":p.Solid=true;break;case"immovable":p.Takeable=false;break;case"empty":s.StackCount=0;break;case"overflow":s.StackCount=s.MaxStack+1;break;case"foreign-stack":s.ParentEntity=new Entity();break;case"container":e.AddPart(new ContainerPart());break;case"crop":e.AddPart(new CropPart());break;case"equipment":e.AddPart(new EquippablePart());break;}
                Refused(e,name+" "+fault);
            }
        }
        [Test] public void MutableNeutralStateIsCachedButOtherPayloadIdentitiesAreNotInvented()
        {
            var items=Texture(192,24);var book=Texture(16,16);using(var a=Construct(items,book))
            {var q=Resolve(a);foreach(string name in Names){var e=Owner(name);Assert.True(q(e,out var t));e.GetPart<RenderPart>().DisplayName="travel-worn supply";e.GetPart<CommercePart>().Value+=7;e.GetPart<StackerPart>().StackCount=2;
                if(name=="HealingTonic")e.GetPart<TonicPart>().Healing="1d4";if(name=="DriedMeat")e.GetPart<FoodPart>().Healing="1d3";
                for(int i=0;i<4;i++)Assert.True(q(e,out var same)&&same==t);Frame(t,name,name=="WateringGrimoire"?book:items);
                if(name=="HealingTonic")e.GetPart<TonicPart>().Healing="";else if(name=="WateringGrimoire")e.GetPart<GrimoirePart>().SkillClassName="Pyromancy_Fireball";else continue;
                Assert.False(q(e,out _),"Different function cannot borrow exact identity.");}
             Assert.AreEqual(FilterMode.Bilinear,items.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,book.wrapMode);}
        }
        [Test] public void AlternateBlueprintsAndSourceProvenanceDoNotAcquireAnEssentialsBody()
        {
            using(var a=Construct(Imported("HealingTonic"),Imported("WateringGrimoire"))){var q=Resolve(a);Assert.True(q(Owner("HealingTonic"),out _));
                foreach(string name in new[]{"BrewedTonic","RawMeat","CookedMeat","PurifyWaterGrimoire","CandyCarrotSeed","LongSword","Player"})
                {var e=Owner(name);e.Properties["SourceBlueprint"]="HealingTonic";Assert.False(q(e,out _),name);}}
        }
        [Test] public void HiddenCoveredAndReplacedGraphNeverLeakGroundOwnersIntoReaderOrPainter()
        {
            var e=Add("HealingTonic");var body=Native(e);using(var r=new Reader(world,zone,Owner("Player")))
            {var d=Document(e,r.Viewer);Portrait(r,d,e.BlueprintName);zone.GetCell(X,Y).IsVisible=false;Assert.False(View().NativeForeground);Rejected(r,d);
             zone.GetCell(X,Y).IsVisible=true;var cover=Add("LongSword");cover.GetPart<RenderPart>().RenderLayer=100;Assert.AreSame(cover,View().Owner);Assert.AreNotSame(body,View().ForegroundTile);
             Portrait(r,Document(e,r.Viewer),e.BlueprintName);Assert.True(zone.RemoveEntity(cover));Assert.AreSame(body,Native(e));
             var old=zone;Attach(old.ZoneID);Assert.False(world.TryResolvePresentation(old,X,Y,out _));Rejected(r,d);}
        }
        [Test] public void MultiCellForeignAndStaleFootprintsCannotBorrowCompactBody()
        {
            var e=Add("DriedMeat");var f=new SpatialFootprintPart{CellsRaw="0,0"};e.AddPart(f);var body=Native(e);
            f.ParentEntity=new Entity();Refused(e,"foreign footprint");f.ParentEntity=e;Assert.AreSame(body,Native(e));
            Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));Refused(e,"multiple cells");f.CellsRaw="0,0";Refused(e,"stale index");Assert.True(zone.TryChangeFootprint(e,"0,0"));Assert.AreSame(body,Native(e));
        }
        [Test] public void ActualDropAndPickupPaintAndRemoveBodyWithoutFullRebuild()
        {
            var actor=Add("Player",X-1,Y);var item=Owner("DriedMeat");Assert.True(actor.GetPart<InventoryPart>().AddObject(item));
            using(var paint=new Paint(world,zone))
            {paint.Step();long full=EnvironmentSpriteRenderer.Perf.FullPasses;Assert.True(InventorySystem.Drop(actor,item,zone));var c=zone.GetEntityCell(item);Assert.NotNull(c);Assert.True(zone.MoveEntity(actor,X-2,Y));
             var body=Native(item);paint.Dirty(c.X,c.Y);paint.Step();Assert.AreSame(body,paint.At(c.X,c.Y));Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
             Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item,false),actor,zone).Success);paint.Dirty(c.X,c.Y);paint.Step();Assert.IsNull(paint.At(c.X,c.Y));Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
             long cells=EnvironmentSpriteRenderer.Perf.CellsResolved;paint.Step();Assert.AreEqual(cells,EnvironmentSpriteRenderer.Perf.CellsResolved);}
        }
        [Test] public void NativeInspectionDoesNotConsumeSuppliesAndRealUseEndsTheirCurrentReader()
        {
            foreach(string name in new[]{"HealingTonic","DriedMeat"})
            {Attach("Overworld.18.8.0");using(var r=new Reader(world,zone,Owner("Player"))){var e=Owner(name);Assert.True(r.Viewer.GetPart<InventoryPart>().AddObject(e));r.Viewer.GetStat("Hitpoints").BaseValue=20;
                var d=ExamineDocumentBuilder.BuildInventory(e,r.Viewer);Portrait(r,d,name);Assert.AreEqual(20,r.Viewer.GetStat("Hitpoints").Value);Assert.AreEqual(1,e.GetPart<StackerPart>().StackCount);
                var result=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(e,name=="HealingTonic"?"ApplyTonic":"Eat"),r.Viewer,zone);Assert.True(result.Success,result.ErrorMessage);Assert.Greater(r.Viewer.GetStat("Hitpoints").Value,20);
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(d));Rejected(r,d);Assert.False(r.Viewer.GetPart<InventoryPart>().Objects.Contains(e));}}
        }
        [Test] public void BookInspectionDoesNotTeachAndRealReadRetainsTheSameOwnedBook()
        {
            using(var r=new Reader(world,zone,Owner("Player"))){var skills=r.Viewer.GetPart<SkillsPart>();if(skills==null){skills=new SkillsPart();r.Viewer.AddPart(skills);}Assert.False(skills.HasSkill("Hydromancy_ConjureRain"));
                var e=Owner("WateringGrimoire");Assert.True(r.Viewer.GetPart<InventoryPart>().AddObject(e));var d=ExamineDocumentBuilder.BuildInventory(e,r.Viewer);Portrait(r,d,e.BlueprintName);Assert.False(skills.HasSkill("Hydromancy_ConjureRain"));
                var result=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(e,"ReadGrimoire"),r.Viewer,zone);Assert.True(result.Success,result.ErrorMessage);Assert.True(skills.HasSkill("Hydromancy_ConjureRain"));Portrait(r,d,e.BlueprintName);Assert.AreEqual(1,e.GetPart<StackerPart>().StackCount);}
        }
        [Test] public void CachedFramesAreOwnedButPixelsAreBorrowedAndOldInjectionDoesNotLoadThem()
        {
            var items=Texture(192,24);var book=Texture(16,16);var a=Construct(items,book);var q=Resolve(a);Assert.True(q(Owner("DriedMeat"),out var tile));var sprite=tile.sprite;
            using(var peer=Construct(items,book)){a.Dispose();a.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(items!=null&&book!=null);Assert.False(q(Owner("DriedMeat"),out _));Assert.True(Resolve(peer)(Owner("DriedMeat"),out _));}
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Add("HealingTonic");Assert.False(View().NativeForeground);
        }
        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            readonly Zone ownedZone;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog, Zone z, Entity viewer)
            {
                Viewer = viewer; ownedZone = z; Assert.True(z.AddEntity(viewer, X - 1, Y));
                root = new GameObject("Exact expedition reader"); root.SetActive(false);
                Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                Write(environment, "_flatPixelGreybox", catalog); environment.FlatPixelGreyboxEnabled = true;
                Write(Presentation, "_envSpriteRenderer", environment); Write(Presentation, "_flatPixelModeEnabled", true);
                typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, z);
                Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose() { if (root != null) { Write(root.GetComponent<EnvironmentSpriteRenderer>(), "_flatPixelGreybox", null); Object.DestroyImmediate(root); } if (ownedZone.GetEntityCell(Viewer) != null) ownedZone.RemoveEntity(Viewer); }
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Expedition submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Expedition fixture");
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
        void SnapshotStatics(Type type) { foreach (var field in type.GetFields(S)) if (!field.IsLiteral) Snapshot(field); }
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
