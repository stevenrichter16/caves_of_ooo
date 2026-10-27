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
    public sealed class FlatPixelKitchenBrineTests
    {
        const string Kitchen = "Overworld.12.11.0";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Zone z, int x, int y, out Tile tile);
        delegate bool Portrait(Zone z, Entity e, int x, int y, out Sprite sprite);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelFallbackCatalog selector;
        FlatPixelGreyboxCatalog world;
        Texture2D atlas;
        IDisposable adapter;
        Query query;
        Portrait portrait;
        Dictionary<string, LiquidDefinition> liquids, previousLiquids;
        bool liquidsInitialized;
        [SetUp] public void Setup()
        {
            liquids = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry)
                .GetField("_byId", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            previousLiquids = new Dictionary<string, LiquidDefinition>(liquids);
            liquidsInitialized = LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(source => source.text));
            Assert.AreEqual("water", LiquidRegistry.Get("brine")?.GroundReactionFamily,
                "The real brine source and freeze reaction require the shipped liquid-family definition.");
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            selector = new FlatPixelFallbackCatalog(); atlas = new Texture2D(16,24); zone = Attach(Kitchen);
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); adapter?.Dispose(); selector?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); content?.Dispose(); }
            finally
            {
                if (liquids != null && previousLiquids != null)
                {
                    liquids.Clear(); foreach (var entry in previousLiquids) liquids[entry.Key] = entry.Value;
                    typeof(LiquidRegistry).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, liquidsInitialized);
                }
            }
        }
        Zone Attach(string id)
        {
            var z = new Zone(id); manager.SetActiveZone(z);
            for (int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++) z.GetCell(x,y).Explored=z.GetCell(x,y).IsVisible=true;
            return z;
        }
        Entity Add(string name="BrinePool", int x=X, int y=Y)
        { var e=content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e,x,y)); return e; }
        void Create(Texture2D texture)
        {
            var type=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelKitchenBrineCatalog");
            Assert.NotNull(type,"Kitchen brine still lacks its scoped native material identity.");
            adapter=(IDisposable)Activator.CreateInstance(type,new object[]{texture,selector});
            query=(Query)Delegate.CreateDelegate(typeof(Query),adapter,type.GetMethod("TryResolve",I));
            portrait=(Portrait)Delegate.CreateDelegate(typeof(Portrait),adapter,type.GetMethod("TryGetSubjectSprite",I));
            world=new FlatPixelGreyboxCatalog((Texture2D)null); Write(world,"_kitchenBrineArt",adapter);
        }
        static void Write(object o,string field,object v)
        { var f=o.GetType().GetField(field,I);Assert.NotNull(f,field);f.SetValue(o,v); }
        FlatPixelGreyboxCell Visible()
        { Assert.True(world.TryResolvePresentation(zone,X,Y,out var v)); return v; }
        void Native(Entity e)
        {
            Assert.True(query(zone,X,Y,out var t));Assert.NotNull(t);Assert.AreSame(atlas,t.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,24),t.sprite.rect);Assert.AreEqual(new Vector2(8,12),t.sprite.pivot);
            Assert.AreEqual(16,t.sprite.pixelsPerUnit);Assert.AreEqual(Tile.ColliderType.None,t.colliderType);
            var v=Visible();Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground);Assert.AreSame(t,v.ForegroundTile);
            Assert.AreEqual(Color.white,v.ForegroundColor);Assert.False(v.BlocksMovement);
            Assert.True(portrait(zone,e,X,Y,out var s));Assert.AreSame(t.sprite,s);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var ws,out var tint));Assert.AreSame(s,ws);Assert.AreEqual(Color.white,tint);
        }
        void Refused(Entity e)
        { Assert.False(query(zone,X,Y,out _));Assert.False(portrait(zone,e,X,Y,out _)); }
        [TestCase(1)] [TestCase(90)]
        public void PositiveBrineStockUsesExactWorldAndReaderFrameWithoutChangingState(int volume)
        {
            var e=Add();e.GetPart<LiquidPoolPart>().Volume=volume;Create(atlas);
            string before=zone.TileState.ToSaveString();var heat=e.GetPart<ThermalPart>().Temperature;
            for(int n=0;n<3;n++)Native(e);
            Assert.AreEqual(before,zone.TileState.ToSaveString());Assert.AreEqual(volume,e.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(heat,e.GetPart<ThermalPart>().Temperature);Assert.AreEqual("brine",e.GetPart<TileStateSourcePart>().EffectiveCoating);
        }
        [Test] public void OrdinaryResourcesConstructionLoadsTheShippedBrineForWorldAndExactReader()
        {
            var e = Add(); var shipped = Resources.Load<Texture2D>("Sprites/Live16x24/kitchen-brine");
            Assert.NotNull(shipped); Assert.AreEqual(16, shipped.width); Assert.AreEqual(24, shipped.height);
            world = new FlatPixelGreyboxCatalog();
            var view = Visible(); Assert.AreSame(e, view.Owner); Assert.True(view.NativeForeground);
            Assert.NotNull(view.ForegroundTile); Assert.AreSame(shipped, view.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 24), view.ForegroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, e, X, Y, out var sprite, out var tint));
            Assert.AreSame(view.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            Assert.AreEqual(90, e.GetPart<LiquidPoolPart>().Volume);
            zone.TileState.WriteCoating(X, Y, "oil", 3);
            Assert.False(Visible().NativeForeground, "Ordinary resource routing must retain the same mixed-liquid refusal as injected routing.");
        }
        [TestCase("Overworld.12.10.0")] [TestCase("Overworld.11.9.0")] [TestCase("Overworld.11.8.0")]
        [TestCase("Overworld.11.10.0")] [TestCase("Overworld.12.11.1")]
        public void ExactKitchenScopeDoesNotSpreadToOtherSites(string id)
        { zone=Attach(id);var e=Add();Create(atlas);Refused(e); }
        [TestCase("zero")] [TestCase("negative")] [TestCase("water")] [TestCase("material")]
        [TestCase("frozen-temperature")] [TestCase("nan-temperature")] [TestCase("nan-threshold")]
        [TestCase("frozen-effect")] [TestCase("electrified-effect")]
        [TestCase("source")] [TestCase("visual")] [TestCase("variant")] [TestCase("glyph-variants")]
        [TestCase("invisible")] [TestCase("carried")] [TestCase("equipped")] [TestCase("takeable")]
        [TestCase("solid")] [TestCase("creature")] [TestCase("item")] [TestCase("player")]
        [TestCase("terrain")] [TestCase("footprint")] [TestCase("foreign-pool")] [TestCase("foreign-thermal")]
        [TestCase("foreign-source")] [TestCase("foreign-render")] [TestCase("foreign-physics")]
        [TestCase("foreign-material")] [TestCase("zero-stack")]
        public void NonNeutralOrMalformedOwnerKeepsTruthfulFallback(string fault)
        {
            var e=Add();Create(atlas);Native(e);var p=e.GetPart<LiquidPoolPart>();var t=e.GetPart<ThermalPart>();
            var r=e.GetPart<RenderPart>();var ph=e.GetPart<PhysicsPart>();var other=new Entity();
            switch(fault)
            {
                case "zero":p.Volume=0;break;case "negative":p.Volume=-1;break;case "water":p.LiquidId="water";break;
                case "material":e.GetPart<MaterialPart>().MaterialID="Water";break;
                case "frozen-temperature":t.Temperature=t.FreezeTemperature;break;
                case "nan-temperature":t.Temperature=float.NaN;break;case "nan-threshold":t.FreezeTemperature=float.NaN;break;
                case "frozen-effect":e.ForceApplyEffect(new FrozenEffect(1),null,zone);break;
                case "electrified-effect":e.ForceApplyEffect(new ElectrifiedEffect(1),null,zone);break;
                case "source":e.GetPart<TileStateSourcePart>().Coating="oil";break;
                case "visual":r.VisualID="custom";break;case "variant":r.VisualVariant="custom";break;
                case "glyph-variants":r.GlyphVariants="~=";break;
                case "invisible":Add("Grass");r.Visible=false;break;
                case "carried":ph.InInventory=other;break;case "equipped":ph.Equipped=other;break;
                case "takeable":ph.Takeable=true;break;case "solid":ph.Solid=true;break;
                case "creature":e.SetTag("Creature");break;case "item":e.SetTag("Item");break;case "player":e.SetTag("Player");break;
                case "terrain":e.Tags.Remove("Terrain");break;case "footprint":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});break;
                case "foreign-pool":p.ParentEntity=other;break;case "foreign-thermal":t.ParentEntity=other;break;
                case "foreign-source":e.GetPart<TileStateSourcePart>().ParentEntity=other;break;
                case "foreign-render":r.ParentEntity=other;break;case "foreign-physics":ph.ParentEntity=other;break;
                case "foreign-material":e.GetPart<MaterialPart>().ParentEntity=other;break;
                case "zero-stack":e.AddPart(new StackerPart{StackCount=0});break;
            }
            Refused(e);
            var fallback = Visible();
            if (fault == "invisible")
            {
                Assert.AreNotSame(e, fallback.Owner, "An invisible pool cannot remain the selected foreground owner.");
                Assert.AreEqual("Grass", fallback.Owner?.BlueprintName,
                    "The real visible substrate remains selectable; invisibility does not require an empty cell.");
            }
            else Assert.False(fallback.NativeForeground);
        }
        [TestCase("ice")] [TestCase("water")] [TestCase("oil")] [TestCase("acid")]
        public void MixedCoatingNeverPromisesNeutralBrine(string coating)
        {var e=Add();Create(atlas);Native(e);zone.TileState.WriteCoating(X,Y,coating,3);Refused(e);}
        [TestCase(false)] [TestCase(true)]
        public void HiddenOrRememberedCellDoesNotReadLiveBrine(bool explored)
        {var e=Add();Create(atlas);Native(e);zone.GetCell(X,Y).IsVisible=false;zone.GetCell(X,Y).Explored=explored;Refused(e);}
        [Test] public void ExactLowerReaderAndWorldPriorityStayIndependent()
        {
            var e=Add();Create(atlas);Native(e);var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100;
            Assert.False(query(zone,X,Y,out _));Assert.True(portrait(zone,e,X,Y,out _));Assert.AreSame(actor,Visible().Owner);
            Assert.True(zone.MoveEntity(actor,X-1,Y));Native(e);
        }
        [Test] public void ExpiredBrineAndForeignGraphRefuseWithoutCreatingState()
        {
            var e=Add();Create(atlas);Native(e);var coat=zone.TileState.Get(X,Y).Coatings[0];coat.Turns=0;Refused(e);
            coat.Turns=ZoneTileState.Permanent;Native(e);manager.SetActiveZone(new Zone(Kitchen));Refused(e);
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedOptionalResourceCannotSuppressFallback(bool malformed)
        {
            Texture2D bad=malformed?new Texture2D(32,24):null;var e=Add();
            try {Create(bad);Refused(e);Assert.False(Visible().NativeForeground);}
            finally{if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [Test] public void DisposeOwnsOnlyCachedSpriteAndTileAndNeverLoadsMissingInjection()
        {
            var e=Add();Create(atlas);Assert.True(query(zone,X,Y,out var t));var s=t.sprite;adapter.Dispose();adapter.Dispose();
            Assert.True(t==null);Assert.True(s==null);Assert.True(atlas!=null);Assert.True(selector.TryResolve(zone,X,Y,out _));Refused(e);
        }
        [TestCase("none",'\0')] [TestCase("charge",'*')] [TestCase("heat",'^')] [TestCase("cold",'^')]
        [TestCase("cloud",'\'')] [TestCase("veil-mist",'\'')] [TestCase("embers",'"')]
        public void SubmittedNeutralBrineRemovesOnlyDuplicateWaveAndRetainsHazards(string hazard,char glyph)
        {
            var e=Add();Create(atlas);Native(e);
            if(hazard=="charge")zone.TileState.AddCharge(X,Y,3);
            if(hazard=="heat")zone.TileState.AddHeat(X,Y,3);
            if(hazard=="cold")zone.TileState.AddCold(X,Y,3);
            if(hazard=="cloud"||hazard=="veil-mist")zone.TileState.WriteCloud(X,Y,hazard=="cloud"?"steam":"veil-mist",3);
            if(hazard=="embers")zone.TileState.WriteResidue(X,Y,"embers",3);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.AreSame(atlas,paint.Submitted.sprite.texture);
                if(glyph=='\0')Assert.IsNull(paint.Mark);else Assert.AreSame(CP437TilesetGenerator.GetTile(glyph),paint.Mark);
                long resolved=EnvironmentSpriteRenderer.Perf.CellsResolved;paint.Step();Assert.AreEqual(resolved,EnvironmentSpriteRenderer.Perf.CellsResolved);
                zone.GetCell(X,Y).IsVisible=false;paint.Renderer.MarkCellDirty(X,Y,"test disclosure loss");paint.Step();Assert.IsNull(paint.Mark);
            }
        }
        [Test] public void CoveredAndMissingBrineArtRetainWaveWhileUncoverRepaintsItAway()
        {
            var e=Add();Create(atlas);using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.IsNull(paint.Mark);var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100;
                paint.Renderer.MarkCellDirty(X,Y,"test actor added");paint.Step();Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark);
                Assert.True(zone.MoveEntity(actor,X-1,Y));paint.Renderer.MarkCellDirty(X,Y,"test actor moved");paint.Step();Assert.IsNull(paint.Mark);
                e.GetPart<LiquidPoolPart>().Volume=0;paint.Renderer.MarkCellDirty(X,Y,"test stock edge");paint.Step();Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark);
            }
        }
        [Test] public void AuthoredFreezeMeltAndSourceRenewalPreserveTheMixedPhaseUntilWaterExpires()
        {
            var e = Add(); Create(atlas); Native(e);
            int volume = e.GetPart<LiquidPoolPart>().Volume;
            float temperature = e.GetPart<ThermalPart>().Temperature;
            using (var reactions = new ReactionScope())
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); Assert.IsNull(paint.Mark); Assert.AreSame(atlas, paint.Submitted.sprite.texture);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                zone.TileState.AddCold(X, Y, 1);
                Assert.Greater(TileReactionSystem.ResolveZone(zone), 0, "Use the actual water-family reaction, not an injected ice record.");
                Assert.True(zone.TileState.HasCoating(X, Y, "ice")); Assert.False(zone.TileState.HasCoating(X, Y, "brine"));
                Refused(e); paint.Step();
                Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark);
                Assert.AreEqual(new Color(.85f, .95f, 1), paint.MarkTint);
                Assert.True(paint.Submitted == null || paint.Submitted.sprite.texture != atlas);

                zone.TileState.AddHeat(X, Y, 1);
                Assert.Greater(TileReactionSystem.ResolveZone(zone), 0);
                Assert.False(zone.TileState.HasCoating(X, Y, "ice")); Assert.True(zone.TileState.HasCoating(X, Y, "water"));
                Refused(e); paint.Step();
                var thawed = Visible();
                Assert.True(thawed.LocalGround); Assert.AreEqual(FlatPixelGreyboxGround.Water, thawed.GroundKind);
                Assert.AreEqual((Color)new Color32(12, 67, 68, 255), thawed.GroundColor);
                Assert.False(thawed.NativeForeground);
                Assert.True(paint.Submitted == null || paint.Submitted.sprite.texture != atlas);
                Assert.IsNull(paint.Mark, "Existing colored water ground already represents sole meltwater; it needs no duplicate wave.");
                ZoneTileStateSystem.SeedTerrainSources(zone);
                Assert.True(zone.TileState.HasCoating(X, Y, "water")); Assert.True(zone.TileState.HasCoating(X, Y, "brine"));
                Assert.AreEqual(2, zone.TileState.Get(X, Y).Coatings.Count);
                Refused(e); paint.Step(); Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark);

                // Actual melt output lasts six tile ticks; source renewal retains
                // brine without changing its liquid stock or erasing meltwater.
                for (int tick = 0; tick < 6; tick++)
                { zone.TileState.Tick(); ZoneTileStateSystem.SeedTerrainSources(zone); }
                Assert.False(zone.TileState.HasCoating(X, Y, "water")); Assert.True(zone.TileState.HasCoating(X, Y, "brine"));
                Native(e); paint.Step(); Assert.IsNull(paint.Mark); Assert.AreSame(atlas, paint.Submitted.sprite.texture);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses, "Tile-state hooks should repaint without a full pass.");
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step();
                Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
            Assert.AreEqual(volume, e.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(temperature, e.GetPart<ThermalPart>().Temperature);
            Assert.AreEqual("water", e.GetPart<TileStateSourcePart>().Coating);
            Assert.AreEqual("brine", e.GetPart<TileStateSourcePart>().EffectiveCoating);
        }

        sealed class ReactionScope : IDisposable
        {
            const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            readonly List<TileReactionSystem.TileReaction> list;
            readonly TileReactionSystem.TileReaction[] previous;
            readonly bool initialized = TileReactionSystem.IsInitialized;
            public ReactionScope()
            {
                list = (List<TileReactionSystem.TileReaction>)typeof(TileReactionSystem).GetField("_reactions", S).GetValue(null);
                previous = list.ToArray();
                try
                {
                    var source = Resources.Load<TextAsset>("Content/Data/TileReactions/Reactions"); Assert.NotNull(source);
                    TileReactionSystem.Initialize(source.text); Assert.True(TileReactionSystem.IsInitialized);
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            {
                list.Clear(); list.AddRange(previous);
                typeof(TileReactionSystem).GetProperty("IsInitialized", S).GetSetMethod(true).Invoke(null, new object[] { initialized });
            }
        }
        sealed class Paint:IDisposable
        {
            readonly HotbarSaveFixture saved;readonly GameObject root;readonly Tilemap overlay,marks;
            public ZoneRenderer Renderer {get;}
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",BindingFlags.Static|BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int,int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo,object> perf=new Dictionary<FieldInfo,object>();
            public Paint(FlatPixelGreyboxCatalog catalog,Zone z)
            {
                oldBound=bound.GetValue(null);
                if(oldBound is WeakReference<Zone> weak&&weak.TryGetTarget(out previous))
                {oldChanged=previous.TileState.OnCellChanged;oldSight=previous.TileState.OnSightChanged;}
                try
                {
                    foreach(var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public|BindingFlags.Static))perf[field]=field.GetValue(null);
                    saved=new HotbarSaveFixture();root=new GameObject("Brine submitted grid",typeof(Grid));saved.Root.transform.SetParent(root.transform,false);
                    var main=saved.Root.GetComponent<Tilemap>();var bg=new GameObject("Background",typeof(Tilemap));bg.transform.SetParent(root.transform,false);
                    var mark=new GameObject("Hazards",typeof(Tilemap));mark.transform.SetParent(root.transform,false);marks=mark.GetComponent<Tilemap>();
                    Renderer=saved.Renderer;Write(Renderer,"_tilemap",main);Write(Renderer,"_bgTilemap",bg.GetComponent<Tilemap>());Write(Renderer,"_tileStateTilemap",marks);
                    var camera=new GameObject("Camera",typeof(Camera));camera.transform.SetParent(root.transform,false);camera.GetComponent<Camera>().orthographic=true;Write(Renderer,"_mainCamera",camera.GetComponent<Camera>());
                    var env=saved.Root.AddComponent<EnvironmentSpriteRenderer>();env.Init(root.transform,main,bg.GetComponent<Tilemap>());env.RenderingEnabled=true;env.FlatPixelGreyboxEnabled=true;
                    Write(env,"_flatPixelGreybox",catalog);overlay=(Tilemap)typeof(EnvironmentSpriteRenderer).GetField("_overlayTilemap",I).GetValue(env);
                    Write(Renderer,"_envSpriteRenderer",env);Write(Renderer,"_flatPixelModeEnabled",true);Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;
                }
                catch{Dispose();throw;}
            }
            public Tile Submitted=>overlay.GetTile<Tile>(new Vector3Int(X,Zone.Height-1-Y,0));
            public TileBase Mark=>marks.GetTile(new Vector3Int(X,Zone.Height-1-Y,0));
            public Color MarkTint=>marks.GetColor(new Vector3Int(X,Zone.Height-1-Y,0));
            public void Step()
            {try{typeof(ZoneRenderer).GetMethod("LateUpdate",I).Invoke(Renderer,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}
            public void Dispose()
            {
                try{saved?.Dispose();if(root!=null)Object.DestroyImmediate(root);}
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;bound.SetValue(null,oldBound);
                    if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}
                    foreach(var entry in perf)entry.Key.SetValue(null,entry.Value);
                }
            }
        }
    }
}
