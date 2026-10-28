// HollowLog extension; reflection keeps the absent fifth input a native assertion RED.
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
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelHollowLogTests
    {
        const int X=10,Y=10;
        const string LogResource="Sprites/Environment/cont_hollow_log";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;FlatPixelGreyboxCatalog world;
        readonly List<Texture2D> borrowed=new List<Texture2D>();
        Action<string> oldMessage;Action<int,int,string> oldDirty;Action<string> oldFull;
        EntityDamageVisualHandler oldDamage;EntityMovedVisualHandler oldMoved;
        EntityFactory oldCorpse;Action<WorldAudioCombatReceipt> oldAudio;int oldEquipmentVersion;
        [SetUp] public void Setup()
        {
            oldEquipmentVersion=EquipmentChangeBus.GlobalVersion;oldMessage=MessageLog.OnMessage;oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            oldDamage=EntityVisualHooks.DamageCallback;oldMoved=EntityVisualHooks.MovedCallback;oldCorpse=CorpsePart.Factory;oldAudio=WorldAudioCombat.Callback;
            MessageLog.OnMessage=null;ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            EntityVisualHooks.DamageCallback=null;EntityVisualHooks.MovedCallback=null;WorldAudioCombat.Callback=null;
            content=new DensityLootTestScope();CorpsePart.Factory=content.Factory;
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=Attach("Overworld.18.8.0");world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();foreach(var t in borrowed)if(t!=null)Object.DestroyImmediate(t);borrowed.Clear();content?.Dispose();}
            finally{MessageLog.OnMessage=oldMessage;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;
                EntityVisualHooks.DamageCallback=oldDamage;EntityVisualHooks.MovedCallback=oldMoved;CorpsePart.Factory=oldCorpse;WorldAudioCombat.Callback=oldAudio;typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null,oldEquipmentVersion);}
        }
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static object Read(object o,string n){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);return f.GetValue(o);}
        static void Write(object o,string n,object value){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);f.SetValue(o,value);}
        static Texture2D Resource(){var t=Resources.Load<Texture2D>(LogResource);Assert.NotNull(t);return t;}
        static void Native(Tile tile,Texture2D texture=null)
        {
            const int h=16;Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(texture??Resource(),tile.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,h),tile.sprite.rect);Assert.AreEqual(new Vector2(8,h/2f),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1,h/16f),(Vector2)tile.sprite.bounds.size);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
        }
        Tile NativeOwner(Entity e,Texture2D texture=null)
        {var c=zone.GetEntityCell(e);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground);Native(v.ForegroundTile,texture);return v.ForegroundTile;}
        static FlatPixelSharedStorageCatalog Construct(params Texture2D[] inputs)
        {
            Assert.AreEqual(5,inputs.Length);var ctor=typeof(FlatPixelSharedStorageCatalog).GetConstructor(Enumerable.Repeat(typeof(Texture2D),5).ToArray());
            Assert.NotNull(ctor,"Missing independently optional HollowLog fifth input");return (FlatPixelSharedStorageCatalog)ctor.Invoke(inputs.Cast<object>().ToArray());
        }
        static Texture2D[] Inputs(Texture2D hollow)=>new[]{Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath),Resources.Load<Texture2D>("Sprites/Environment/cont_strongbox"),Resources.Load<Texture2D>("Sprites/Environment/cont_ore_cache"),hollow};
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);borrowed.Add(t);return t;}
        AnnouncementDocument Document(Entity e,Entity viewer=null){var d=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));return d;}
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);
        void Refused(Entity e,string why)
        {
            using(var art=new FlatPixelSharedStorageCatalog()){Assert.False(art.TryResolve(e,out var tile),why);Assert.IsNull(tile,why);}
            Assert.False(View().NativeForeground,why);
            if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Resource(),sprite.texture,why);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(Resource(),old.ForegroundTile?.sprite?.texture,why);if(world.TryGetSubjectSprite(e,X,Y,out sprite,out _))Assert.AreNotSame(Resource(),sprite.texture,why);}
        }
        static void Act(Entity actor,Entity target,Zone z,string command)
        {var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)actor);e.SetParameter("Zone",(object)z);e.SetParameter("Command",command);target.FireEventAndRelease(e);}
        static string Sha(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}

        [Test] public void ApprovedFullSquareSourceAndImporterRemainByteExact()
        {
            string file=System.IO.Path.Combine(Application.dataPath,"Resources/Sprites/Environment/cont_hollow_log.png");
            Assert.AreEqual("0cb5fbd52a7fd91c0f5266254073f2a31b6ab5184a8201129558acf26a4e5783",Sha(file));
            Assert.AreEqual("d42c6a3e6458682054cc26b9cfb795a41bc074d7a47b0c621d5edd015f3c274b",Sha(file+".meta"));
            var t=Resource();Assert.AreEqual(16,t.width);Assert.AreEqual(16,t.height);Assert.AreEqual(FilterMode.Point,t.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,t.wrapMode);
            using(var art=new FlatPixelSharedStorageCatalog()){Assert.True(art.TryResolve(content.Factory.CreateEntity("HollowLog"),out var tile));Native(tile);}
        }
        [Test] public void SelectedBodyAndAuthorizedPortraitUseFullSquareAcrossContexts()
        {
            foreach(var id in new[]{"Overworld.12.11.0","Overworld.18.8.0","Overworld.17.8.0","StandaloneHollowLog"})
            {
                zone=Attach(id);var log=Add("HollowLog");var viewer=Add("Player",X-1,Y);Assert.True(FlatPixelSharedStorageCatalog.HasKnownRole(log));var body=NativeOwner(log);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {
                    paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Color.white,paint.Tint(X,Y));Assert.AreEqual(Matrix4x4.identity,paint.Transform(X,Y));
                    var portrait=portraits.GetPortrait(Document(log,viewer),paint.Renderer);Assert.AreSame(Resource(),portrait.Texture);Assert.AreEqual(new Rect(0,0,1,1),portrait.Uv);Assert.False(portrait.IsModel);
                }
            }
        }
        [Test] public void FifthTextureIsIndependentWhileLegacyFourInputLeavesOnlyHollowLogAbsent()
        {
            var inputs=Inputs(Texture(16,16));var owner=content.Factory.CreateEntity("HollowLog");
            using(var full=Construct(inputs))using(var legacy=new FlatPixelSharedStorageCatalog(inputs[0],inputs[1],inputs[2],inputs[3]))
            {
                Assert.True(full.TryResolve(owner,out var tile));Native(tile,inputs[4]);Assert.False(legacy.TryResolve(owner,out _));
                foreach(var name in new[]{"Chest","LockedChest","Crate","StrongBox","OreCache","MimicChest"})
                {var other=content.Factory.CreateEntity(name);Assert.True(full.TryResolve(other,out var a),name);Assert.True(legacy.TryResolve(other,out var b),name);Assert.AreSame(a.sprite.texture,b.sprite.texture);Assert.AreEqual(a.sprite.rect,b.sprite.rect);}
            }
            using(var only=Construct(null,null,null,null,inputs[4]))
            {Assert.True(only.TryResolve(owner,out var tile));Native(tile,inputs[4]);Assert.False(only.TryResolve(content.Factory.CreateEntity("Chest"),out _));}
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedFifthInputCannotReloadOrBorrowOpeningIdentity(bool malformed)
        {
            var inputs=Inputs(malformed?Texture(16,24):null);world.Dispose();world=new FlatPixelGreyboxCatalog();Write(world,"_sharedStorageArt",Construct(inputs));
            foreach(var id in new[]{"Overworld.12.11.0","Overworld.18.8.0"})
            {
                zone=Attach(id);var log=Add("HollowLog");Assert.False(View().NativeForeground);
                if(FlatPixelGreyboxCatalog.IsAdmitted(zone)){Assert.True(world.TryResolve(zone,X,Y,out var old));Assert.AreNotSame(Resource(),old.ForegroundTile?.sprite?.texture);}
                using(var paint=new Paint(world,zone))using(var portraits=Portraits()){paint.Step();Assert.AreNotSame(Resource(),paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(Resource(),portraits.GetPortrait(Document(log),paint.Renderer).Texture);}
            }
        }
        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void EssentialHollowLogPartsAreUniqueAndOwned(string fault)
        {
            foreach(var type in new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(ContainerPart),typeof(ExaminablePart),typeof(MaterialPart)})
            {
                zone=Attach("Overworld.12.11.0");var log=Add("HollowLog");NativeOwner(log);var part=log.Parts.First(p=>p.GetType()==type);
                if(fault=="missing")Assert.True(log.RemovePart(part));else if(fault=="foreign")part.ParentEntity=new Entity();else log.AddPart((Part)Activator.CreateInstance(type));Refused(log,type.Name+fault);
            }
        }
        [Test] public void WrongMeaningReskinsCustodyAndInvalidSpatialStateStayFallback()
        {
            foreach(var fault in new[]{"glyph","layer","visual","variant","variants","carried","equipped","takeable","material","thermal","structure","brain","well","source","terrain","zeroStack","foreignLock","foreignEffects","multicell","staleFootprint"})
            {
                zone=Attach("Overworld.12.11.0");var log=Add("HollowLog");NativeOwner(log);var render=log.GetPart<RenderPart>();var physics=log.GetPart<PhysicsPart>();
                switch(fault)
                {
                    case "glyph":render.RenderString="*";break;case "layer":render.RenderLayer=9;break;case "visual":render.VisualID="custom-log";break;case "variant":render.VisualVariant="quest";break;case "variants":render.GlyphVariants="?!";break;
                    case "carried":physics.InInventory=new Entity();break;case "equipped":physics.Equipped=new Entity();break;case "takeable":physics.Takeable=true;break;
                    case "material":log.GetPart<MaterialPart>().MaterialID="Stone";break;case "thermal":log.AddPart(new ThermalPart());break;case "structure":log.AddPart(new DestructiblePart());break;
                    case "brain":log.AddPart(new BrainPart());break;case "well":log.AddPart(new WellPart());break;case "source":log.AddPart(new TileStateSourcePart());break;case "terrain":log.SetTag("Terrain");break;
                    case "zeroStack":log.AddPart(new StackerPart{StackCount=0});break;case "foreignLock":var key=new LockPart();log.AddPart(key);key.ParentEntity=new Entity();break;case "foreignEffects":var effects=new StatusEffectsPart();log.AddPart(effects);effects.ParentEntity=new Entity();break;
                    case "multicell":case "staleFootprint":var footprint=new SpatialFootprintPart{CellsRaw="0,0"};log.AddPart(footprint);Assert.True(zone.TryChangeFootprint(log,"0,0;1,0"));if(fault=="staleFootprint")footprint.CellsRaw="0,0";break;
                }
                Refused(log,fault);
            }
        }
        [Test] public void ContainerMutabilityAndPureQueriesDoNotInventStructureOrBodyStates()
        {
            var log=Add("HollowLog");Assert.IsNull(log.GetPart<ThermalPart>());Assert.IsNull(log.GetPart<DestructiblePart>());Assert.IsNull(log.GetStat("Hitpoints"));var body=NativeOwner(log);
            var container=log.GetPart<ContainerPart>();Assert.AreEqual(6,container.MaxItems);container.MaxItems=12;container.Locked=true;Assert.True(container.AddItem(content.Factory.CreateEntity("Dagger")));log.GetPart<RenderPart>().DisplayName="A named cache";
            var probe=new Probe();log.AddPart(probe);var contents=container.Contents.ToArray();
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();int renders=probe.Renders;for(int i=0;i<3;i++){Assert.AreSame(body,NativeOwner(log));Assert.AreSame(Resource(),portraits.GetPortrait(Document(log),paint.Renderer).Texture);}
                Assert.AreEqual(renders,probe.Renders);Assert.Zero(probe.ServiceQueries);CollectionAssert.AreEqual(contents,container.Contents);Assert.True(container.Locked);Assert.AreEqual(12,container.MaxItems);
            }
        }
        [Test] public void ActualOpenTakePutAndLockedRefusalPreserveBodyAndContentsOwnership()
        {
            var log=Add("HollowLog");var body=NativeOwner(log);var actor=Add("Player",X-1,Y);actor.GetPart<InventoryPart>().MaxWeight=-1;var container=log.GetPart<ContainerPart>();var opened=new OpenProbe();actor.AddPart(opened);
            Act(actor,log,zone,"OpenContainer");Assert.AreEqual(1,opened.Opens);var item=content.Factory.CreateEntity("Dagger");Assert.True(container.AddItem(item));
            container.Locked=true;Act(actor,log,zone,"OpenContainer");Assert.AreEqual(1,opened.Opens);Assert.False(InventorySystem.TakeFromContainer(actor,log,item));Assert.AreSame(log,item.GetPart<PhysicsPart>().InInventory);Assert.AreSame(body,NativeOwner(log));
            container.Locked=false;Act(actor,log,zone,"OpenContainer");Assert.AreEqual(2,opened.Opens);Assert.True(InventorySystem.TakeFromContainer(actor,log,item));Assert.AreSame(actor,item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(zone.GetEntityCell(item));
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {paint.Step();var doc=ExamineDocumentBuilder.BuildInventory(item,actor);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc));Assert.AreNotSame(Resource(),portraits.GetPortrait(doc,paint.Renderer).Texture);Assert.AreSame(body,paint.At(X,Y));}
            Assert.True(InventorySystem.PutInContainer(actor,log,item));Assert.AreSame(log,item.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(1,InventorySystem.TakeAllFromContainer(actor,log));Assert.IsEmpty(container.Contents);Assert.AreSame(body,NativeOwner(log));
        }
        [Test] public void DisclosureCoverAndCurrentGraphControlWorldAndStaleReader()
        {
            var log=Add("HollowLog");var body=NativeOwner(log);var doc=Document(log);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));zone.GetCell(X,Y).IsVisible=zone.GetCell(X,Y).Explored=true;
                var player=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(player,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.True(zone.RemoveEntity(player));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                Assert.True(zone.MoveEntity(log,X+2,Y));Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc));Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);NativeOwner(log);
                var movedDoc=Document(log);Assert.AreSame(Resource(),portraits.GetPortrait(movedDoc,paint.Renderer).Texture);var old=zone;Attach(old.ZoneID);Assert.False(world.TryResolvePresentation(old,X+2,Y,out _));Assert.IsNull(portraits.GetPortrait(movedDoc,paint.Renderer).Texture);
            }
        }
        [Test] public void SolidLogBlocksOrdinaryMovementWithoutAddingSpriteCollider()
        {
            var log=Add("HollowLog");var body=NativeOwner(log);var actor=Add("Player",X-1,Y);Assert.True(log.GetPart<PhysicsPart>().Solid);Assert.AreEqual(Tile.ColliderType.None,body.colliderType);
            Assert.False(MovementSystem.TryMove(actor,zone,1,0));Assert.AreEqual((X-1,Y),zone.GetEntityPosition(actor));Assert.IsNull(log.GetPart<DestructiblePart>());Assert.AreSame(body,NativeOwner(log));
        }
        [Test] public void FifthViewsDisposeWithoutDestroyingBorrowedTextureOrPeer()
        {
            var texture=Texture(16,16);var art=Construct(null,null,null,null,texture);var owner=content.Factory.CreateEntity("HollowLog");
            try{using(var peer=Construct(null,null,null,null,texture))
            {Assert.True(art.TryResolve(owner,out var tile));var sprite=tile.sprite;Assert.True(art.TryResolve(owner,out var same));Assert.AreSame(tile,same);art.Dispose();art.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.False(art.TryResolve(owner,out _));Assert.True(peer.TryResolve(owner,out var remaining));Native(remaining,texture);Assert.True(texture!=null);Assert.AreEqual(FilterMode.Bilinear,texture.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,texture.wrapMode);}}
            finally{art.Dispose();}
        }
        sealed class OpenProbe:Part
        {public int Opens;public override bool HandleEvent(GameEvent e){if(e.ID=="OpenContainer")Opens++;return true;}}
        sealed class Probe:Part
        {
            public int Renders,ServiceQueries; public string Cue;
            public override bool HandleEvent(GameEvent e){if(e.ID=="Render"){Renders++;if(Cue!=null)e.SetParameter("ColorString",Cue);}if(e.ID=="GetInventoryActions"||e.ID=="InventoryAction"||e.ID=="ObjectCreated"||e.ID==IdleQueryEvent.ID)ServiceQueries++;return true;}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("HollowLog submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "HollowLog fixture");
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
