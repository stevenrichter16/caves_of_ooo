// Sodden material/edge selection. Synthetic input proves art addresses and real
// submitted incremental geometry; ordinary Play remains the aesthetic gate.
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
    public sealed class FlatPixelSoddenEnvironmentTests
    {
        const string Resource="Sprites/Live16x24/sodden-ground", Travel="Sprites/Live16x24/sodden-travel";
        const int X=10,Y=10;
        const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; FlatPixelFallbackCatalog selector; FlatPixelSoddenTravelCatalog adapter;
        readonly List<Texture2D> textures=new List<Texture2D>();
        Dictionary<string,LiquidDefinition> liquids,previousLiquids; bool previousInitialized;
        [SetUp] public void Setup()
        {
            liquids=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",Static).GetValue(null);
            previousLiquids=new Dictionary<string,LiquidDefinition>(liquids);previousInitialized=LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t=>t.text));
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            Attach();Assert.True(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone));world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try{world?.Dispose();adapter?.Dispose();selector?.Dispose();foreach(var t in textures)if(t!=null)Object.DestroyImmediate(t);textures.Clear();content?.Dispose();}
            finally{liquids.Clear();foreach(var p in previousLiquids)liquids[p.Key]=p.Value;typeof(LiquidRegistry).GetField("_initialized",Static).SetValue(null,previousInitialized);}
        }
        void Attach(){zone=new Zone("Overworld.18.8.0");manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;}
        Entity Add(string name,int x=X,int y=Y){var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,x,y));e.GetPart<TileStateSourcePart>()?.Seed(zone,x,y);return e;}
        Texture2D Texture(int frames){var t=new Texture2D(frames*16,24);textures.Add(t);return t;}
        static object Read(object o,string n)=>o.GetType().GetField(n,I).GetValue(o);
        static void Write(object o,string n,object value)=>o.GetType().GetField(n,I).SetValue(o,value);
        FlatPixelGreyboxCell View(int x=X,int y=Y){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        Texture2D Old(string resource)=>Resources.Load<Texture2D>(resource);
        FlatPixelSoddenTravelCatalog Create(Texture2D input,bool oldGroups=true,bool oldSignature=false)
        {
            var args=new object[]{oldGroups?Old(Travel):null,oldGroups?Old(FlatPixelOpeningArtCatalog.ResourcePath):null,
                oldGroups?Old(FlatPixelSillEnvelopeCatalog.ResourcePath):null,oldGroups?Old(FlatPixelCellarFloorCatalog.ResourcePath):null,
                oldGroups?Old(FlatPixelOpeningArtCatalog.CellarResourcePath):null,selector};
            if(!oldSignature)args=args.Concat(new object[]{input}).ToArray();
            var signature=new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(FlatPixelFallbackCatalog)};
            if(!oldSignature)signature=signature.Concat(new[]{typeof(Texture2D)}).ToArray();
            var ctor=typeof(FlatPixelSoddenTravelCatalog).GetConstructor(signature);
            Assert.NotNull(ctor,"Sodden biome-ground optional atlas constructor is not implemented.");
            return (FlatPixelSoddenTravelCatalog)ctor.Invoke(args);
        }
        void Inject(Texture2D input,bool oldGroups=true,bool oldSignature=false)
        {
            world.Dispose();adapter?.Dispose();selector?.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);selector=new FlatPixelFallbackCatalog();
            adapter=Create(input,oldGroups,oldSignature);Write(world,"_soddenTravelArt",adapter);
        }
        static int Frame(Tile tile,Texture2D input,int low,int high)
        {
            Assert.NotNull(tile);Assert.NotNull(tile.sprite);Assert.AreSame(input,tile.sprite.texture);
            int index=(int)tile.sprite.rect.x/16;Assert.That(index,Is.InRange(low,high));
            Assert.AreEqual(new Rect(index*16,0,16,24),tile.sprite.rect);Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot);
            Assert.AreEqual(16,tile.sprite.pixelsPerUnit);Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);return index;
        }
        void Mire(Entity owner,Texture2D input,int frame)
        {
            var c=zone.GetEntityCell(owner);Assert.NotNull(c);var v=View(c.X,c.Y);Assert.AreSame(owner,v.Owner);Assert.True(v.NativeForeground);
            Assert.True(v.RepresentedMire);Frame(v.ForegroundTile,input,frame,frame);Assert.AreEqual(Color.white,v.ForegroundColor);
        }

        [TestCase("Grass",0,3)] [TestCase("MirePool",4,4)]
        public void DefaultOrdinarySoddenUsesItsOwnMaterialResource(string name,int low,int high)
        {
            var owner=Add(name);var v=View();var tile=name=="Grass"?v.GroundTile:v.ForegroundTile;
            Assert.NotNull(tile);Assert.AreEqual("sodden-ground",tile.sprite.texture.name,"Ordinary Sodden still borrows the previous flat material atlas.");
            var input=Resources.Load<Texture2D>(Resource);Assert.NotNull(input);Assert.AreEqual(576,input.width);Assert.AreEqual(24,input.height);Frame(tile,input,low,high);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out var tint));Assert.AreSame(input,sprite.texture);Assert.AreEqual(Color.white,tint);
        }

        [TestCase(false)] [TestCase(true)]
        public void SixteenCardinalConnectionsJoinWetStocksAndReaderStaysIsolated(bool depleted)
        {
            var input=Texture(36);Inject(input);int[] dx={0,1,0,-1},dy={-1,0,1,0};
            for(int mask=0;mask<16;mask++)
            {
                Attach();var pool=Add("MirePool");pool.GetPart<LiquidPoolPart>().Volume=depleted?0:60;
                for(int bit=0;bit<4;bit++)if((mask&(1<<bit))!=0)Add("MirePool",X+dx[bit],Y+dy[bit]).GetPart<LiquidPoolPart>().Volume=(bit&1)==0?0:60;
                Add("MirePool",X+1,Y+1);string state=zone.TileState.ToSaveString();int version=zone.EntityVersion;
                Mire(pool,input,(depleted?20:4)+mask);var tile=View().ForegroundTile;Assert.AreSame(tile,View().ForegroundTile);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,pool,X,Y,out var portrait,out _));Assert.AreSame(input,portrait.texture);
                Assert.AreEqual(new Rect((depleted?20:4)*16,0,16,24),portrait.rect,"Examine must not reveal adjacent topology.");
                Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(depleted?0:60,pool.GetPart<LiquidPoolPart>().Volume);
            }
        }

        [Test] public void NeighborJoinsRespectDisclosureCustodyAndUniquePhysicalMeaningButNotActorCover()
        {
            var input=Texture(36);Inject(input);var center=Add("MirePool");var neighbor=Add("MirePool",X+1,Y);Mire(center,input,6);
            var actor=Add("Player",X+1,Y);actor.GetPart<RenderPart>().RenderLayer=100;Assert.AreSame(actor,View(X+1,Y).Owner);Mire(center,input,6);
            var cell=zone.GetCell(X+1,Y);cell.IsVisible=false;Mire(center,input,4);cell.IsVisible=true;
            cell.Explored=false;Mire(center,input,4);cell.Explored=true;
            var render=neighbor.GetPart<RenderPart>();render.Visible=false;Mire(center,input,4);render.Visible=true;
            var physical=neighbor.GetPart<PhysicsPart>();physical.InInventory=actor;Mire(center,input,4);physical.InInventory=null;
            physical.Equipped=actor;Mire(center,input,4);physical.Equipped=null;
            var material=neighbor.GetPart<MaterialPart>();material.ParentEntity=actor;Mire(center,input,4);material.ParentEntity=neighbor;
            var duplicate=new LiquidPoolPart();neighbor.AddPart(duplicate);Mire(center,input,4);Assert.True(neighbor.RemovePart(duplicate));
            var other=Add("MirePool",X+1,Y);Mire(center,input,4);Assert.True(zone.RemoveEntity(other));
            zone.TileState.WriteCoating(X+1,Y,"oil",3);Mire(center,input,4);Assert.True(zone.TileState.RemoveCoating(X+1,Y,"oil"));Mire(center,input,6);
        }

        [Test] public void FirmGroundFormsStableQuietPatchesWithoutTouchingStateOrStoneFloor()
        {
            var input=Texture(36);Inject(input);var frames=new int[40,16];var counts=new int[4];int equal=0,pairs=0;
            for(int y=0;y<16;y++)for(int x=0;x<40;x++)Add("Grass",x+2,y+2);
            string state=zone.TileState.ToSaveString();int version=zone.EntityVersion;
            for(int y=0;y<16;y++)for(int x=0;x<40;x++)
            {
                var view=View(x+2,y+2);Assert.True(view.LocalGround);int f=Frame(view.GroundTile,input,0,3);frames[x,y]=f;counts[f]++;
                Assert.AreSame(view.GroundTile,View(x+2,y+2).GroundTile);
                if(x>0){pairs++;if(frames[x-1,y]==f)equal++;}if(y>0){pairs++;if(frames[x,y-1]==f)equal++;}
            }
            Assert.That((double)equal/pairs,Is.GreaterThan(.65),"Details should gather into patches instead of independent per-cell sprinkle noise.");
            Assert.That(counts.Count(n=>n>=10),Is.GreaterThanOrEqualTo(3),"One constant floor cannot satisfy material variation.");
            Assert.That(counts[0]/640.0,Is.InRange(.35,.80),"Quiet ground must remain useful without erasing nearly all material detail.");
            var point=WorldMap.FromZoneID(zone.ZoneID);int brokenDetailPatches=0,mixedDetailPatches=0;
            int firstX=(7-(point.x*Zone.Width+2)%7)%7,firstY=(5-(point.y*Zone.Height+2)%5)%5;
            for(int by=firstY;by+5<=16;by+=5)for(int bx=firstX;bx+7<=40;bx+=7)
            {
                var patch=new HashSet<int>();for(int y=by;y<by+5;y++)for(int x=bx;x<bx+7;x++)patch.Add(frames[x,y]);
                if(patch.Any(f=>f>0)&&patch.Count>1)brokenDetailPatches++;
                if(patch.Count(f=>f>0)>1)mixedDetailPatches++;
            }
            Assert.That(brokenDetailPatches,Is.GreaterThan(0),"Nonquiet patches must break identical35-cell stamp rectangles while retaining coarse material grouping.");
            Assert.That(mixedDetailPatches,Is.GreaterThan(0),"Detailed patches need multiple nonzero native variants, not the same stamp with only quiet holes.");
            Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(version,zone.EntityVersion);
            var stone=Add("StoneFloor",X,Y);Assert.True(View().LocalGround);Frame(View().GroundTile,Old(FlatPixelCellarFloorCatalog.ResourcePath),0,3);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,stone,X,Y,out var portrait,out _));Assert.AreSame(Old(FlatPixelCellarFloorCatalog.ResourcePath),portrait.texture);
        }

        [Test] public void NewGroundNeverMasksIndependentCoatingsOrCurrentOwnerRefusals()
        {
            var input=Texture(36);Inject(input);var grass=Add("Grass");Frame(View().GroundTile,input,0,3);
            zone.TileState.WriteCoating(X,Y,"water",3);Assert.False(View().LocalGround);Assert.True(zone.TileState.RemoveCoating(X,Y,"water"));
            var render=grass.GetPart<RenderPart>();render.VisualVariant="foreign-floor";Assert.False(View().LocalGround);render.VisualVariant="";
            var pool=Add("MirePool");Mire(pool,input,4);zone.TileState.WriteCoating(X,Y,"oil",3);Assert.False(View().NativeForeground);
            Assert.True(zone.TileState.RemoveCoating(X,Y,"oil"));Mire(pool,input,4);
            var old=zone;manager.SetActiveZone(new Zone(zone.ZoneID));Assert.False(adapter.TryResolveForeground(old,X,Y,pool,out _));Assert.False(adapter.TryResolveGround(old,X,Y,out _,out _));
        }

        [Test] public void OptionalNewGroupFailsBackWithoutReloadAndDoesNotRequireOldArt()
        {
            var wrong=Texture(35);foreach(var input in new Texture2D[]{null,wrong})
            {
                Inject(input);Attach();Add("Grass");Frame(View().GroundTile,Old(FlatPixelSillEnvelopeCatalog.ResourcePath),20,23);
                var pool=Add("MirePool");var tile=View().ForegroundTile;Frame(tile,Old(Travel),16,24);Assert.AreSame(pool,View().Owner);
            }
            var own=Texture(36);Inject(own,false);Attach();Add("Grass");Frame(View().GroundTile,own,0,3);Mire(Add("MirePool"),own,4);
            Attach();Add("StoneWall");Assert.False(View().NativeForeground,"A new ground atlas must not fabricate missing wall art.");
        }

        [Test] public void ExistingSixInputInjectionRetainsAcceptedTravelGrassAndReaderSemantics()
        {
            Inject(null,true,true);var grass=Add("Grass");Frame(View().GroundTile,Old(FlatPixelSillEnvelopeCatalog.ResourcePath),20,23);
            var pool=Add("MirePool");Add("MirePool",X+1,Y);var tile=View().ForegroundTile;Frame(tile,Old(Travel),16,24);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,pool,X,Y,out var portrait,out _));Assert.AreSame(tile.sprite,portrait);
        }

        [Test] public void SubmittedNeighborChangesUseExistingIncrementalPassAndMatchFullRepaint()
        {
            var input=Texture(36);Inject(input);var center=Add("MirePool");var neighbor=Add("MirePool",X+1,Y);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Frame(paint.At(X,Y),input,6,6);long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                zone.GetCell(X+1,Y).IsVisible=false;paint.Dirty(X+1,Y);paint.Step();Frame(paint.At(X,Y),input,4,4);
                zone.GetCell(X+1,Y).IsVisible=true;paint.Dirty(X+1,Y);paint.Step();Frame(paint.At(X,Y),input,6,6);
                neighbor.GetPart<LiquidPoolPart>().Volume=0;paint.Dirty(X+1,Y);paint.Step();Frame(paint.At(X,Y),input,6,6);Frame(paint.At(X+1,Y),input,28,28);
                Assert.True(zone.TileState.HasCoating(X+1,Y,"bog-mire"),"A present depleted owner still projects its wet film.");
                Assert.True(zone.RemoveEntity(neighbor));paint.Dirty(X+1,Y);paint.Step();var incremental=paint.At(X,Y);Frame(incremental,input,4,4);
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);long cells=EnvironmentSpriteRenderer.Perf.CellsResolved;
                paint.Step();Assert.AreEqual(cells,EnvironmentSpriteRenderer.Perf.CellsResolved);
                paint.Renderer.MarkDirty("Sodden edge equivalence control");paint.Step();Assert.AreSame(incremental,paint.At(X,Y));
                Assert.False(zone.TileState.HasCoating(X+1,Y,"bog-mire"),"Removing the last pool unprojects its owned coating through Zone.RemoveEntity.");
            }
        }

        [Test] public void SubmittedDepletedWetFilmSuppressesOnlyItsOwnWaveAndRetainsEnergyMarker()
        {
            var input=Texture(36);Inject(input);var pool=Add("MirePool");pool.GetPart<LiquidPoolPart>().Volume=0;zone.TileState.AddCharge(X,Y,1);
            string state=zone.TileState.ToSaveString();using(var paint=new Paint(world,zone))
            {paint.Step();Frame(paint.At(X,Y),input,20,20);Assert.AreSame(CP437TilesetGenerator.GetTile('*'),paint.Mark);Assert.AreEqual(state,zone.TileState.ToSaveString());Assert.AreEqual(0,pool.GetPart<LiquidPoolPart>().Volume);}
        }

        [Test] public void CachedNewFramesAreOwnedButInjectedAtlasAndSelectorAreBorrowed()
        {
            var input=Texture(36);Inject(input);var pool=Add("MirePool");Mire(pool,input,4);var tile=View().ForegroundTile;var sprite=tile.sprite;
            world.Dispose();world.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(input!=null);
            Assert.True(selector.TryResolve(zone,X,Y,out var selected));Assert.AreSame(pool,selected.Owner);
        }

        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, marks, ground;
            public ZoneRenderer Renderer { get; }
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", Static);
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Sodden submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var mark = new GameObject("Hazards", typeof(Tilemap)); mark.transform.SetParent(root.transform, false); marks = mark.GetComponent<Tilemap>();
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>()); Write(Renderer, "_tileStateTilemap", marks);
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true; env.FlatPixelGreyboxEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap");
                    Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            public bool FullDirty => (bool)Read(Renderer, "_fullDirty");
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public TileBase Mark => marks.GetTile(new Vector3Int(X, Zone.Height - 1 - Y, 0));
            public TileBase MarkAt(int x, int y) => marks.GetTile(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Color MarkTint => marks.GetColor(new Vector3Int(X, Zone.Height - 1 - Y, 0));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Sodden current material");
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try
                {
                    if (saved?.Root != null)
                    { var env=saved.Root.GetComponent<EnvironmentSpriteRenderer>(); if(env!=null)Write(env,"_flatPixelGreybox",null); }
                    saved?.Dispose(); if(root!=null)Object.DestroyImmediate(root);
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
