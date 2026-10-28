// Shared cloth bodies: reflection-only native RED fixture; no shipped art required.
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
    public sealed class FlatPixelSharedCampClothTests
    {
        const int X=10,Y=10;
        const string Wall="TentWall",Pole="GuestClothPole";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static readonly string[] Names={Wall,Pole};
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world; Texture2D atlas;
        readonly List<Texture2D> borrowed=new List<Texture2D>();
        EntityFactory oldDestruction; EntityDamageVisualHandler oldDamage; EntityMovedVisualHandler oldMoved;
        Action<string> oldMessage; Action<WorldAudioCombatReceipt> oldAudio;
        Action<int,int,string> oldDirty; Action<string> oldFull; int oldEquipmentVersion;
        [SetUp] public void Setup()
        {
            oldDestruction=DestructionSystem.EntityFactoryRef;oldDamage=EntityVisualHooks.DamageCallback;oldMoved=EntityVisualHooks.MovedCallback;
            oldMessage=MessageLog.OnMessage;oldAudio=WorldAudioCombat.Callback;oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;oldEquipmentVersion=EquipmentChangeBus.GlobalVersion;
            EntityVisualHooks.DamageCallback=null;EntityVisualHooks.MovedCallback=null;MessageLog.OnMessage=null;WorldAudioCombat.Callback=null;ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            content=new DensityLootTestScope();DestructionSystem.EntityFactoryRef=content.Factory;
            manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);zone=Attach("Overworld.17.13.0");
            atlas=Texture(48,24);Inject(atlas);
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();foreach(var t in borrowed)if(t!=null)Object.DestroyImmediate(t);borrowed.Clear();content?.Dispose();}
            finally{DestructionSystem.EntityFactoryRef=oldDestruction;EntityVisualHooks.DamageCallback=oldDamage;EntityVisualHooks.MovedCallback=oldMoved;MessageLog.OnMessage=oldMessage;WorldAudioCombat.Callback=oldAudio;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null,oldEquipmentVersion);}
        }
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static object Read(object o,string n){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);return f.GetValue(o);}
        static void Write(object o,string n,object v){var f=o.GetType().GetField(n,I);Assert.NotNull(f,n);f.SetValue(o,v);}
        static Type ArtType(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSharedCampClothCatalog");Assert.NotNull(t,"Missing shared camp cloth adapter");return t;}
        delegate bool Query(Entity owner,int x,int y,out Tile tile);
        static IDisposable Construct(Texture2D input){var c=ArtType().GetConstructor(new[]{typeof(Texture2D)});Assert.NotNull(c);return (IDisposable)c.Invoke(new object[]{input});}
        static Query Resolve(IDisposable art){var m=ArtType().GetMethod("TryResolve",new[]{typeof(Entity),typeof(int),typeof(int),typeof(Tile).MakeByRefType()});Assert.NotNull(m);return (Query)Delegate.CreateDelegate(typeof(Query),art,m);}
        IDisposable Art=>(IDisposable)Read(world,"_sharedCampClothArt");
        void Inject(Texture2D input){world?.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Write(world,"_sharedCampClothArt",Construct(input));}
        Texture2D Texture(int w,int h){var t=new Texture2D(w,h);borrowed.Add(t);return t;}
        void Native(Tile tile,string name)
        {
            Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(atlas,tile.sprite.texture);
            var rect=tile.sprite.rect;Assert.AreEqual(new Vector2(16,24),rect.size);Assert.AreEqual(0,rect.y);
            if(name==Pole)Assert.AreEqual(32,rect.x);else Assert.That(rect.x,Is.EqualTo(0).Or.EqualTo(16));
            Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(new Vector2(1,1.5f),(Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
        }
        Tile NativeOwner(Entity e)
        {var c=zone.GetEntityCell(e);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground);Native(v.ForegroundTile,e.BlueprintName);return v.ForegroundTile;}
        AnnouncementDocument Document(Entity e,Entity viewer=null){var d=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));return d;}
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);
        void Refused(Entity e,string why)
        {
            Assert.False(Resolve(Art)(e,X,Y,out var tile),why);Assert.IsNull(tile,why);Assert.False(View().NativeForeground,why);
            if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(atlas,sprite.texture,why);
            if(FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone,X,Y,out var legacy));Assert.AreNotSame(atlas,legacy.ForegroundTile?.sprite?.texture,why);
                if(world.TryGetSubjectSprite(e,X,Y,out sprite,out _))Assert.AreNotSame(atlas,sprite.texture,why);
            }
        }
        [TestCase(Wall)] [TestCase(Pole)]
        public void ExactWorldAndAuthorizedPortraitUseOneUnstretchedNativeCell(string name)
        {
            foreach(var id in new[]{"Overworld.12.11.0","Overworld.17.13.0","StandaloneCampCloth"})
            {
                zone=Attach(id);var e=Add(name);var actor=Add("Player",X-1,Y);var probe=new Probe();e.AddPart(probe);var body=NativeOwner(e);int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {
                    paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Color.white,paint.Tint(X,Y));Assert.AreEqual(Matrix4x4.identity,paint.Transform(X,Y));int count=probe.Renders;
                    var portrait=portraits.GetPortrait(Document(e,actor),paint.Renderer);Assert.AreSame(atlas,portrait.Texture);Assert.False(portrait.IsModel);Assert.AreEqual(new Rect(body.sprite.rect.x/48f,0,16f/48,1),portrait.Uv);
                    NativeOwner(e);Assert.AreEqual(count,probe.Renders);Assert.Zero(probe.ServiceQueries);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
                }
            }
        }
        [Test] public void WallVariantsAreStableAcrossQueriesAndPoleNeverBorrowsThem()
        {
            var wall=content.Factory.CreateEntity(Wall);var pole=content.Factory.CreateEntity(Pole);var query=Resolve(Art);var seen=new HashSet<int>();
            for(int y=2;y<18;y++)for(int x=2;x<18;x++)
            {
                Assert.True(query(wall,x,y,out var a));Native(a,Wall);seen.Add((int)a.sprite.rect.x/16);
                Assert.True(query(pole,x+1,y+1,out var p));Native(p,Pole);Assert.True(query(wall,x,y,out var b));Assert.AreSame(a,b);
            }
            CollectionAssert.AreEquivalent(new[]{0,1},seen);
            Assert.True(query(wall,10,10,out var even));Assert.AreEqual(0,even.sprite.rect.x);
            Assert.True(query(wall,11,10,out var odd));Assert.AreEqual(16,odd.sprite.rect.x);
            Assert.True((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{wall}));
            foreach(var name in new[]{"Wall","Hedge","FrontierClothScreen","Sack","Signpost","Player"})
            {var e=content.Factory.CreateEntity(name);Assert.NotNull(e);e.Properties["SourceBlueprint"]=Wall;Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{e}));Assert.False(query(e,X,Y,out _));}
        }
        [Test] public void ReskinsWrongMaterialAndForeignRolesCannotReachWeakerLocalArt()
        {
            foreach(var name in Names)foreach(var fault in new[]{"glyph","color","background","layer","visual","variant","variants","role","solid"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var r=e.GetPart<RenderPart>();
                switch(fault){case "glyph":r.RenderString="?";break;case "color":r.ColorString="&R";break;case "background":r.BackgroundColor="^r";break;case "layer":r.RenderLayer=8;break;case "visual":r.VisualID="unapproved-cloth";break;case "variant":r.VisualVariant="quest";break;case "variants":r.GlyphVariants="?!";break;case "role":e.AddPart(new WellPart());break;case "solid":e.GetPart<PhysicsPart>().Solid=name!=Wall;break;}
                Refused(e,name+fault);
            }
            foreach(var fault in new[]{"wrong-material","zero-hp","gone","invalid-thermal"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(Wall);NativeOwner(e);
                if(fault=="wrong-material")e.GetPart<MaterialPart>().MaterialID="Stone";
                if(fault=="zero-hp")e.GetPart<DestructiblePart>().HP=0;
                if(fault=="gone")e.GetPart<DestructiblePart>().Gone=true;
                if(fault=="invalid-thermal")e.GetPart<ThermalPart>().Temperature=float.NaN;
                Refused(e,fault);
            }
        }
        [Test] public void RequiredPartsAreUniqueAndOwnedIncludingWallMaterialStructureAndThermal()
        {
            foreach(var name in Names)foreach(var type in name==Wall?new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart),typeof(MaterialPart),typeof(ThermalPart),typeof(DestructiblePart)}:new[]{typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart)})
            foreach(var fault in new[]{"missing","foreign","duplicate"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var part=e.Parts.First(p=>p.GetType()==type);
                if(fault=="missing")Assert.True(e.RemovePart(part));else if(fault=="foreign")part.ParentEntity=new Entity();else e.AddPart((Part)Activator.CreateInstance(type));
                Refused(e,name+type.Name+fault);
            }
        }
        [Test] public void CustodyFootprintAndOptionalOwnedStateRemainAuthoritative()
        {
            foreach(var name in Names)foreach(var fault in new[]{"carried","equipped","takeable","multicell","foreign-footprint","empty-stack","foreign-effects","duplicate-effects"})
            {
                zone=Attach("Overworld.12.11.0");var e=Add(name);NativeOwner(e);var footprint=new SpatialFootprintPart{CellsRaw="0,0"};e.AddPart(footprint);var stack=new StackerPart{StackCount=2,MaxStack=10};e.AddPart(stack);NativeOwner(e);
                var p=e.GetPart<PhysicsPart>();switch(fault){case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;case "takeable":p.Takeable=true;break;case "multicell":Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));break;case "foreign-footprint":footprint.ParentEntity=new Entity();break;case "empty-stack":stack.StackCount=0;break;case "foreign-effects":var fx=new StatusEffectsPart();e.AddPart(fx);fx.ParentEntity=new Entity();break;case "duplicate-effects":e.AddPart(new StatusEffectsPart());e.AddPart(new StatusEffectsPart());break;}
                Refused(e,name+fault);
            }
        }
        [Test] public void MissingOrMalformedAtlasRefusesBothBodiesWithoutResourceReload()
        {
            foreach(var input in new[]{(Texture2D)null,Texture(32,24),Texture(48,16),Texture(49,24)})
            {
                Inject(atlas);foreach(var name in Names){zone=Attach("Overworld.12.11.0");NativeOwner(Add(name));}
                Inject(input);foreach(var name in Names)
                {
                    zone=Attach("Overworld.12.11.0");var e=Add(name);Refused(e,"invalid optional atlas");
                    using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                    {paint.Step();Assert.AreNotSame(atlas,paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(input,paint.At(X,Y)?.sprite?.texture,"Do not create partial slices.");Assert.AreNotSame(atlas,portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
                }
            }
            // Explicit empty legacy injection also cannot consult a new default resource.
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);
            foreach(var name in Names){zone=Attach("Overworld.12.11.0");Add(name);Assert.False(View().NativeForeground);}
        }
        [Test] public void RealWallDamageAndDestructionKeepFloorHazardsAndPlayerPhysiology()
        {
            var floor=Add("Sand");var wall=Add(Wall);var player=Add("Player",X-1,Y);var d=wall.GetPart<DestructiblePart>();var body=NativeOwner(wall);
            Assert.True(wall.HasTag("Terrain"));Assert.AreEqual("Cloth",wall.GetPart<MaterialPart>().MaterialID);Assert.AreEqual(8,d.HP);Assert.AreEqual(8,d.MaxHP);Assert.AreEqual(0,d.Hardness);Assert.IsEmpty(d.WreckageBlueprint);Assert.IsNull(wall.GetStat("Hitpoints"));Assert.True(zone.GetCell(X,Y).BlocksMovement());
            var hp=player.GetStatValue("Hitpoints");var doc=Document(wall,player);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(wall,1,player,zone));Assert.AreEqual(7,d.HP);paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));NativeOwner(wall);Assert.True(zone.GetCell(X,Y).BlocksMovement());
                zone.TileState.AddHeat(X,Y,1);string state=zone.TileState.ToSaveString();Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(wall,99,player,zone));paint.Step();
                Assert.True(d.Gone);Assert.IsNull(zone.GetEntityCell(wall));Assert.False(Resolve(Art)(wall,X,Y,out _));Assert.AreSame(floor,zone.GetCell(X,Y).Objects.Single());Assert.False(zone.GetCell(X,Y).BlocksMovement());
                Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(hp,player.GetStatValue("Hitpoints"));Assert.AreNotSame(body,paint.At(X,Y));Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
            }
        }
        [Test] public void ActualHeatAndBurningCueKeepBodyAndUseOnlyOneRenderEvent()
        {
            var wall=Add(Wall);var t=wall.GetPart<ThermalPart>();var d=wall.GetPart<DestructiblePart>();var probe=new Probe();wall.AddPart(probe);var body=NativeOwner(wall);
            Assert.AreEqual(260,t.FlameTemperature);Assert.AreEqual(.8f,t.HeatCapacity);Assert.False(wall.HasEffect<BurningEffect>());
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();Assert.AreEqual(Color.white,paint.Tint(X,Y));ApplyHeat(wall,40);Assert.AreEqual(75,t.Temperature,.001f);Assert.False(wall.HasEffect<BurningEffect>());Assert.AreSame(body,NativeOwner(wall));
                ApplyHeat(wall,160);Assert.True(wall.HasEffect<BurningEffect>());Assert.GreaterOrEqual(t.Temperature,t.FlameTemperature);Assert.AreEqual(8,d.HP);int count=probe.Renders;
                paint.Dirty(X,Y);paint.Step();Assert.AreEqual(count+1,probe.Renders);Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(QudColorParser.Parse("&*R"),paint.Tint(X,Y));
                float temperature=t.Temperature;var effect=wall.GetEffect<BurningEffect>();int duration=effect.Duration;
                NativeOwner(wall);Assert.AreSame(atlas,portraits.GetPortrait(Document(wall),paint.Renderer).Texture);paint.Dirty(X+1,Y);paint.Step();
                Assert.AreEqual(count+1,probe.Renders);Assert.AreEqual(temperature,t.Temperature);Assert.AreEqual(duration,effect.Duration);Assert.Zero(probe.ServiceQueries);
                ApplyHeat(wall,-160);Assert.False(wall.HasEffect<BurningEffect>());paint.Dirty(X,Y);paint.Step();Assert.AreEqual(Color.white,paint.Tint(X,Y));Assert.AreSame(body,paint.At(X,Y));
            }
        }
        void ApplyHeat(Entity owner,float joules){var e=GameEvent.New("ApplyHeat");e.SetParameter("Joules",joules);e.SetParameter("Zone",zone);owner.FireEventAndRelease(e);}
        [Test] public void GuestPoleStaysPassableUnbreakableAndDoesNotAcquireAService()
        {
            var pole=Add(Pole);var player=Add("Player",X-1,Y);var body=NativeOwner(pole);Assert.True(pole.HasTag("Furniture"));Assert.False(pole.GetPart<PhysicsPart>().Solid);Assert.IsNull(pole.GetPart<MaterialPart>());Assert.IsNull(pole.GetPart<DestructiblePart>());Assert.IsNull(pole.GetPart<ThermalPart>());
            var actions=WorldInteractionSystem.GatherActions(pole,player).Select(a=>a.Command).ToArray();var probe=new Probe();pole.AddPart(probe);Assert.AreEqual(DestroyVerdict.NotBreakable,DestructionSystem.Damage(pole,99,player,zone));
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();NativeOwner(pole);Assert.AreSame(atlas,portraits.GetPortrait(Document(pole,player),paint.Renderer).Texture);Assert.Zero(probe.ServiceQueries);
                CollectionAssert.AreEqual(actions,WorldInteractionSystem.GatherActions(pole,player).Select(a=>a.Command).ToArray());
                Assert.True(MovementSystem.TryMove(player,zone,1,0));paint.Dirty(X,Y);paint.Step();Assert.AreSame(player,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.AreEqual((X,Y),zone.GetEntityPosition(player));
                Assert.True(MovementSystem.TryMove(player,zone,-1,0));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
            }
        }
        [Test] public void DisclosureSelectionMovementRemovalAndCurrentGraphRetireWorldAndReader()
        {
            foreach(var name in Names)
            {
                zone=Attach("Overworld.17.13.0");var e=Add(name);var doc=Document(e);var body=NativeOwner(e);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {
                    paint.Step();zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                    zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;
                    e.GetPart<RenderPart>().Visible=false;Assert.False(View().NativeForeground);e.GetPart<RenderPart>().Visible=true;NativeOwner(e);
                    var player=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(player,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.True(zone.RemoveEntity(player));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));
                    Assert.True(zone.MoveEntity(e,X+1,Y));paint.Dirty(X,Y);paint.Dirty(X+1,Y);paint.Step();Assert.AreNotSame(body,paint.At(X,Y));NativeOwner(e);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                    var moved=Document(e);Assert.True(zone.RemoveEntity(e));paint.Dirty(X+1,Y);paint.Step();Assert.IsNull(portraits.GetPortrait(moved,paint.Renderer).Texture);
                    Assert.True(zone.AddEntity(e,X,Y));NativeOwner(e);var old=zone;Attach(old.ZoneID);Assert.False(world.TryResolvePresentation(old,X,Y,out _));Assert.False(world.TryGetPresentationSubjectSprite(old,e,X,Y,out _,out _));
                }
            }
        }
        [Test] public void CachedViewsDisposeIdempotentlyWithoutDestroyingInputOrPeer()
        {
            using(var peer=Construct(atlas))
            {
                var e=content.Factory.CreateEntity(Wall);var q=Resolve(Art);Assert.True(q(e,X,Y,out var t));Assert.True(q(e,X,Y,out var same));Assert.AreSame(t,same);var sprite=t.sprite;
                var old=Art;old.Dispose();old.Dispose();Assert.True(t==null);Assert.True(sprite==null);Assert.False(q(e,X,Y,out _));Assert.True(Resolve(peer)(e,X,Y,out var live));Native(live,Wall);
                Assert.True(atlas!=null);Assert.AreEqual(FilterMode.Bilinear,atlas.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,atlas.wrapMode);
            }
        }
        sealed class Probe:Part
        {public int Renders,ServiceQueries;public override bool HandleEvent(GameEvent e){if(e.ID=="Render")Renders++;if(e.ID==IdleQueryEvent.ID||e.ID=="GetInventoryActions"||e.ID=="InventoryAction")ServiceQueries++;return true;}}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Shared camp cloth submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Shared camp cloth fixture");
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
