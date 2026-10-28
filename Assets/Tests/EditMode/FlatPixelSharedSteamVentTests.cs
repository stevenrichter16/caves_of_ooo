// Proposed SteamVent body-only contract. Reflection keeps absent production behavioral RED.
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
    public sealed class FlatPixelSharedSteamVentTests
    {
        const int X=10,Y=10;
        const string Natural="Sprites/Environment/steam_vent", East="Sprites/Live16x24/east-kitchen";
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;
        readonly List<Texture2D> borrowed=new List<Texture2D>();
        Action<string> oldMessage; Action<int,int,string> oldDirty; Action<string> oldFull;
        [SetUp] public void Setup()
        {
            oldMessage=MessageLog.OnMessage;MessageLog.OnMessage=null;
            oldDirty=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=Attach("Overworld.16.13.9");world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();foreach(var t in borrowed)if(t!=null)Object.DestroyImmediate(t);borrowed.Clear();content?.Dispose();}
            finally{MessageLog.OnMessage=oldMessage;ZoneRenderHooks.CellDirtyCallback=oldDirty;ZoneRenderHooks.FullDirtyCallback=oldFull;}
        }
        Zone Attach(string id){var z=new Zone(id);manager.SetActiveZone(z);foreach(var c in z.Cells)c.IsVisible=c.Explored=true;return z;}
        Entity Add(string name="SteamVent",int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        static object Read(object owner,string field){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);return f.GetValue(owner);}
        static void Write(object owner,string field,object value){var f=owner.GetType().GetField(field,I);Assert.NotNull(f,field);f.SetValue(owner,value);}
        static Texture2D Resource(string path=Natural){var t=Resources.Load<Texture2D>(path);Assert.NotNull(t,path);return t;}
        static void Native(Tile tile,Texture2D texture=null,bool east=false)
        {
            Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(texture??Resource(east?East:Natural),tile.sprite.texture);
            Assert.AreEqual(east?new Rect(256,0,16,24):new Rect(0,0,16,16),tile.sprite.rect);
            Assert.AreEqual(new Vector2(8,east?12:8),tile.sprite.pivot);Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1,east?1.5f:1),(Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);Assert.AreEqual(TileFlags.None,tile.flags);
        }
        Tile NativeOwner(Entity e,Texture2D texture=null,bool east=false)
        {var c=zone.GetEntityCell(e);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground);Assert.False(v.BlocksMovement);Native(v.ForegroundTile,texture,east);return v.ForegroundTile;}
        static Type ArtType(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSharedSteamVentCatalog");Assert.NotNull(t,"Missing shared SteamVent adapter");return t;}
        delegate bool Query(Entity owner,out Tile tile);
        static IDisposable Construct(Texture2D input){var c=ArtType().GetConstructor(new[]{typeof(Texture2D)});Assert.NotNull(c);return (IDisposable)c.Invoke(new object[]{input});}
        static Query Resolve(IDisposable art)=>(Query)Delegate.CreateDelegate(typeof(Query),art,ArtType().GetMethod("TryResolve",new[]{typeof(Entity),typeof(Tile).MakeByRefType()}));
        Texture2D Texture(int w=16,int h=16){var t=new Texture2D(w,h);borrowed.Add(t);return t;}
        static IDisposable EastArt(Texture2D input)=>new FlatPixelEastKitchenCatalog(input,null);
        AnnouncementDocument Document(Entity e,Entity viewer=null){var d=ExamineDocumentBuilder.BuildWorld(e,zone,zone.GetEntityCell(e),viewer);Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d));return d;}
        static ExaminePortraitRenderer Portraits()=>(ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,new object[]{(Action<Camera,RenderTexture>)((c,t)=>{})},null);
        void Refused(Entity e,string label)
        {
            using(var art=Construct(Resource())){Assert.False(Resolve(art)(e,out var tile),label);Assert.IsNull(tile,label);}
            Assert.False(View().NativeForeground,label);
            if(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _))Assert.AreNotSame(Resource(),sprite.texture,label);
        }

        [Test] public void ExactNaturalWorldAndAuthorizedReadersShareOneImmutableBodyAcrossContexts()
        {
            foreach(var id in new[]{"Overworld.16.13.9","Overworld.15.7.0","StandaloneSteam"})
            {
                zone=Attach(id);var e=Add();var player=Add("Player",X-1,Y);NativeOwner(e);
                var m=e.GetPart<MaterialPart>();Assert.AreEqual("Steam",m.MaterialID);Assert.AreEqual(65,m.Conductivity);Assert.AreEqual(0,m.Combustibility);Assert.AreEqual(1,m.Porosity);
                Assert.True(m.HasMaterialTag("Vapor"));Assert.True(m.HasMaterialTag("Water"));Assert.True(e.HasTag("Terrain"));Assert.IsNull(e.GetPart<LiquidPoolPart>());Assert.IsNull(e.GetPart<DestructiblePart>());
                var source=e.GetPart<TileStateSourcePart>();Assert.AreEqual("water",source.Coating);Assert.AreEqual(2,source.CoatingTurns);Assert.AreEqual(1,source.Heat);Assert.False(source.RadiatesEnergy);
                using(var paint=new Paint(world,zone))using(var portraits=Portraits())
                {paint.Step();Native(paint.At(X,Y));var p=portraits.GetPortrait(Document(e,player),paint.Renderer);Assert.AreSame(Resource(),p.Texture);Assert.False(p.IsModel);Assert.AreEqual(new Rect(0,0,1,1),p.Uv);}
            }
        }
        [Test] public void FiniteTemperatureAndRealWetEffectPreserveBodyWithExactlyOneLiveRenderCue()
        {
            var e=Add();var body=NativeOwner(e);var thermal=e.GetPart<ThermalPart>();
            foreach(float value in new[]{-50f,0f,95f,100f,500f}){thermal.Temperature=value;Assert.AreSame(body,NativeOwner(e));}
            thermal.Temperature=95;var effects=new StatusEffectsPart();e.AddPart(effects);var probe=new RenderProbe();e.AddPart(probe);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var wet=new WetEffect();Assert.True(effects.ForceApplyEffect(wet,null,zone));Assert.AreEqual("&B",effects.GetRenderColorOverride());
                int before=probe.Renders;paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(QudColorParser.Parse("&B"),paint.Tint(X,Y));Assert.AreEqual(before+1,probe.Renders);
                NativeOwner(e);portraits.GetPortrait(Document(e),paint.Renderer);paint.Dirty(X+1,Y);paint.Step();Assert.AreEqual(before+1,probe.Renders);
                Assert.True(effects.RemoveEffect(wet));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreEqual(Color.white,paint.Tint(X,Y));
            }
        }
        [TestCase("ownership")] [TestCase("meaning")]
        public void RequiredPartsAndForeignMeaningsCannotBorrowNaturalBody(string group)
        {
            var faults=group=="ownership"?new[]{"missing-source","foreign-source","duplicate-source","foreign-render","duplicate-physics","foreign-examine","foreign-material","foreign-thermal"}:new[]{"solid","pool","service","actor","structure"};
            foreach(var fault in faults)
            {
                zone=Attach("Overworld.16.13.9");var e=Add();NativeOwner(e);
                switch(fault)
                {
                    case "missing-source":Assert.True(e.RemovePart(e.GetPart<TileStateSourcePart>()));break;
                    case "foreign-source":e.GetPart<TileStateSourcePart>().ParentEntity=new Entity();break;
                    case "duplicate-source":e.AddPart(new TileStateSourcePart());break;
                    case "foreign-render":e.GetPart<RenderPart>().ParentEntity=new Entity();break;
                    case "duplicate-physics":e.AddPart(new PhysicsPart());break;
                    case "foreign-examine":e.GetPart<ExaminablePart>().ParentEntity=new Entity();break;
                    case "foreign-material":e.GetPart<MaterialPart>().ParentEntity=new Entity();break;
                    case "foreign-thermal":e.GetPart<ThermalPart>().ParentEntity=new Entity();break;
                    case "solid":e.GetPart<PhysicsPart>().Solid=true;break;case "pool":e.AddPart(new LiquidPoolPart());break;
                    case "service":e.AddPart(new HarvestablePart());break;case "actor":e.AddPart(new BrainPart());break;case "structure":e.AddPart(new DestructiblePart());break;
                }
                Refused(e,fault);
            }
        }
        [TestCase("identity")] [TestCase("custody")]
        public void ReskinsCustodyAndChangedFootprintStayFallback(string group)
        {
            var faults=group=="identity"?new[]{"glyph","color","layer","visual","variant"}:new[]{"carried","equipped","takeable","multicell"};
            foreach(var fault in faults)
            {
                zone=Attach("Overworld.16.13.9");var e=Add();NativeOwner(e);var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
                switch(fault)
                {
                    case "glyph":r.RenderString="!";break;case "color":r.ColorString="&R";break;case "layer":r.RenderLayer=7;break;
                    case "visual":r.VisualID="different-vent";break;case "variant":r.VisualVariant="other";break;
                    case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;case "takeable":p.Takeable=true;break;
                    case "multicell":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});Assert.True(zone.TryChangeFootprint(e,"0,0;1,0"));break;
                }
                Refused(e,fault);
            }
        }
        [Test] public void AlteredMaterialSourceOrNonfiniteThermalMeaningIsRefused()
        {
            foreach(var fault in new[]{"material","tags","conductivity","coating","lease","heat","cold","radiation","residue","temperature"})
            {
                zone=Attach("Overworld.16.13.9");var e=Add();NativeOwner(e);var m=e.GetPart<MaterialPart>();var s=e.GetPart<TileStateSourcePart>();
                switch(fault)
                {
                    case "material":m.MaterialID="Stone";break;case "tags":m.MaterialTags.Clear();break;case "conductivity":m.Conductivity=float.NaN;break;
                    case "coating":s.Coating="oil";break;case "lease":s.CoatingTurns=0;break;case "heat":s.Heat=0;break;case "cold":s.Cold=1;break;
                    case "radiation":s.RadiatesEnergy=true;break;case "residue":s.Residue="embers";s.ResidueTurns=2;break;case "temperature":e.GetPart<ThermalPart>().Temperature=float.NaN;break;
                }
                Refused(e,fault);
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingMalformedNaturalInputStaysMissingWithoutAResourcesReload(int fault)
        {
            var good=Texture();using(var a=Construct(good)){Assert.True(Resolve(a)(content.Factory.CreateEntity("SteamVent"),out var t));Native(t,good);}
            var bad=fault==0?null:fault==1?Texture(15,16):Texture(16,24);
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Write(world,"_sharedSteamVentArt",Construct(bad));
            var e=Add();Assert.False(View().NativeForeground);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {paint.Step();Assert.AreNotSame(Resource(),paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(Resource(),portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
        }
        [TestCase("Overworld.12.10.0")] [TestCase("Overworld.12.11.0")]
        public void ExistingEastGrilleAndItsMissingResourceFallbackTakePrecedence(string id)
        {
            zone=Attach(id);Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));var e=Add();NativeOwner(e,null,true);
            var natural=Texture();var east=Texture(288,24);world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);
            Write(world,"_sharedSteamVentArt",Construct(natural));Write(world,"_eastKitchenArt",EastArt(east));NativeOwner(e,east,true);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {paint.Step();Native(paint.At(X,Y),east,true);var p=portraits.GetPortrait(Document(e),paint.Renderer);Assert.AreSame(east,p.Texture);Assert.AreEqual(new Rect(256f/288,0,16f/288,1),p.Uv);}
            ((IDisposable)Read(world,"_eastKitchenArt")).Dispose();Write(world,"_eastKitchenArt",EastArt(null));
            Assert.False(View().NativeForeground);using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {paint.Step();Assert.AreNotSame(natural,paint.At(X,Y)?.sprite?.texture);Assert.AreNotSame(natural,portraits.GetPortrait(Document(e),paint.Renderer).Texture);}
            zone=Attach("Overworld.16.13.9");e=Add();NativeOwner(e,natural);
        }
        [Test] public void FrostCloudAndSpoofedOwnersAreNotNewNativeSteamBodies()
        {
            using(var a=Construct(Resource()))
            {
                Assert.True(Resolve(a)(content.Factory.CreateEntity("SteamVent"),out _));Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{null}));
                foreach(var name in new[]{"FrostVent","IceSheet","Rubble"})
                {var e=content.Factory.CreateEntity(name);Assert.NotNull(e);e.Properties["SourceBlueprint"]="SteamVent";Assert.False((bool)ArtType().GetMethod("HasKnownRole").Invoke(null,new object[]{e}));Assert.False(Resolve(a)(e,out _));}
            }
            zone.TileState.WriteCloud(X,Y,"steam",2);Assert.IsNull(View().Owner);Assert.False(View().NativeForeground);
        }
        [Test] public void DisclosureAndReplacedCurrentGraphRetireAuthorizedReaders()
        {
            var e=Add();NativeOwner(e);var d=Document(e);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(portraits.GetPortrait(d,paint.Renderer).Texture);
                zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));
                zone.GetCell(X,Y).IsVisible=zone.GetCell(X,Y).Explored=true;e.GetPart<RenderPart>().Visible=false;Assert.False(View().NativeForeground);
                e.GetPart<RenderPart>().Visible=true;NativeOwner(e);
                var old=zone;Attach(old.ZoneID);Assert.False(world.TryResolvePresentation(old,X,Y,out _));Assert.IsNull(portraits.GetPortrait(d,paint.Renderer).Texture);
            }
        }
        [Test] public void DirtyActorCoverAndUncoverPreserveRealVentAndHazard()
        {
            var e=Add();e.GetPart<TileStateSourcePart>().Seed(zone,X,Y);NativeOwner(e);var state=zone.TileState.ToSaveString();
            using(var paint=new Paint(world,zone))
            {
                paint.Step();var body=paint.At(X,Y);Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark(X,Y));var actor=Add("Player");paint.Dirty(X,Y);paint.Step();Assert.AreSame(actor,View().Owner);Assert.AreNotSame(body,paint.At(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark(X,Y));
                Assert.True(zone.RemoveEntity(actor));paint.Dirty(X,Y);paint.Step();Assert.AreSame(body,paint.At(X,Y));Assert.AreSame(e,View().Owner);Assert.AreEqual(state,zone.TileState.ToSaveString());
            }
        }
        [Test] public void RealSourceSeedsOnlySelfRegardlessOfTemperatureAndQueriesNeverRenewIt()
        {
            var e=Add();NativeOwner(e);var source=e.GetPart<TileStateSourcePart>();
            foreach(float temperature in new[]{-50f,95f,150f})
            {
                zone.TileState.Clear(X,Y);e.GetPart<ThermalPart>().Temperature=temperature;source.Seed(zone,X,Y);
                Assert.AreEqual(2,zone.TileState.CoatingTurns(X,Y,"water"));Assert.AreEqual(1,zone.TileState.Heat(X,Y));Assert.AreEqual("",zone.TileState.Cloud(X,Y));
                foreach(var d in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})Assert.False(zone.TileState.Has(X+d.x,Y+d.y));
            }
            zone.TileState.Clear(X,Y);zone.TileState.WriteCloud(X,Y,"steam",1);string state=zone.TileState.ToSaveString();int version=zone.EntityVersion;
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {for(int i=0;i<3;i++){NativeOwner(e);portraits.GetPortrait(Document(e),paint.Renderer);paint.Dirty(X,Y);paint.Step();}Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(version,zone.EntityVersion);}
        }
        [Test] public void SubmittedHazardsKeepExistingPriorityAndDoNotRequireSterileGround()
        {
            var e=Add();NativeOwner(e);Add("QuartziteFloor");
            using(var paint=new Paint(world,zone))
            {
                var kinds=new[]{"water-heat","water-charge","mixed","heat","cold","cloud","residue"};var expected=new[]{'~','~','~','^','^','\'','"'};
                for(int i=0;i<kinds.Length;i++)
                {
                    zone.TileState.Clear(X,Y);var k=kinds[i];
                    if(k.StartsWith("water")||k=="mixed")zone.TileState.WriteCoating(X,Y,"water",2);
                    if(k=="water-heat"||k=="heat")zone.TileState.AddHeat(X,Y,1);if(k=="water-charge")zone.TileState.AddCharge(X,Y,1);
                    if(k=="mixed")zone.TileState.WriteCoating(X,Y,"oil",2);if(k=="cold")zone.TileState.AddCold(X,Y,1);
                    if(k=="cloud")zone.TileState.WriteCloud(X,Y,"steam",2);if(k=="residue")zone.TileState.WriteResidue(X,Y,"embers",2);
                    string state=zone.TileState.ToSaveString();paint.Dirty(X,Y);paint.Step();Native(paint.At(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile(expected[i]),paint.Mark(X,Y),k);Assert.AreEqual(state,zone.TileState.ToSaveString());
                }
            }
        }
        [Test] public void RemovedSourceLeavesOnlyRealResidualCloudNotANativeVentOrNewCloudCue()
        {
            var e=Add();NativeOwner(e);var doc=Document(e);zone.TileState.WriteCloud(X,Y,"steam",2);
            using(var paint=new Paint(world,zone))using(var portraits=Portraits())
            {
                paint.Step();var body=paint.At(X,Y);Assert.True(zone.RemoveEntity(e));paint.Dirty(X,Y);paint.Step();Assert.IsNull(View().Owner);Assert.False(View().NativeForeground);Assert.AreNotSame(body,paint.At(X,Y));
                Assert.AreSame(CP437TilesetGenerator.GetTile('\''),paint.Mark(X,Y));Assert.AreEqual(2,zone.TileState.Get(X,Y).CloudTurns);Assert.IsNull(portraits.GetPortrait(doc,paint.Renderer).Texture);
                zone.TileState.Clear(X,Y);paint.Dirty(X,Y);paint.Step();Assert.IsNull(paint.Mark(X,Y));Assert.False(View().NativeForeground);
            }
        }
        [Test] public void DisposalOwnsCachedViewsOnlyAndAnIndependentPeerSurvives()
        {
            var input=Texture();var e=content.Factory.CreateEntity("SteamVent");var a=Construct(input);
            using(var peer=Construct(input))
            {
                Assert.True(Resolve(a)(e,out var tile));var sprite=tile.sprite;a.Dispose();a.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.False(Resolve(a)(e,out _));
                Assert.True(input!=null);Assert.AreEqual(FilterMode.Bilinear,input.filterMode);Assert.AreEqual(TextureWrapMode.Repeat,input.wrapMode);Assert.True(Resolve(peer)(e,out var other));Native(other,input);
            }
        }
        sealed class RenderProbe:Part{public int Renders;public override bool HandleEvent(GameEvent e){if(e.ID=="Render")Renders++;return true;}}
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Shared SteamVent submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Shared SteamVent fixture");
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
