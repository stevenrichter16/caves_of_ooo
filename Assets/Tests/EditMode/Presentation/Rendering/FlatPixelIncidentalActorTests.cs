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
    public sealed class FlatPixelIncidentalActorTests
    {
        const string Kitchen = "Overworld.12.11.0";
        const int X = 10, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = new Zone(Kitchen); manager.SetActiveZone(zone);
            for(int y=Y-2;y<=Y+2;y++) for(int x=X-2;x<=X+2;x++) zone.GetCell(x,y).Explored = zone.GetCell(x,y).IsVisible = true;
        }
        [TearDown] public void Cleanup()
        { injected?.Dispose(); adapter?.Dispose(); if(farmerTexture!=null) Object.DestroyImmediate(farmerTexture); if(grazerTexture!=null) Object.DestroyImmediate(grazerTexture); content?.Dispose(); }

        // Existing public world/subject APIs: these fail on the visible glyph,
        // not on a proposed class name or a missing test-only integration seam.
        [TestCase("Farmer", "farmer")]
        [TestCase("ReedbackGrazer", "reedback-grazer")]
        public void RealIncidentalActorHasNativeWorldAndMatchingSubjectInsteadOfGlyph(string blueprint, string resource)
        {
            var owner = content.Factory.CreateEntity(blueprint);
            Assert.True(zone.AddEntity(owner, X, Y));
            using (var world = new FlatPixelGreyboxCatalog())
            {
                Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var body));
                Assert.AreSame(owner, body.Owner);
                Assert.True(body.NativeForeground, blueprint + " still paints the identity-preserving glyph instead of its native body.");
                var expected = Resources.Load<Texture2D>("Sprites/Live16x24/" + resource);
                Assert.NotNull(expected, "The actual optional resource must be imported, not substituted by a fixture.");
                Assert.AreSame(expected, body.ForegroundTile.sprite.texture);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var portrait, out var tint));
                Assert.AreSame(body.ForegroundTile.sprite, portrait); Assert.AreEqual(Color.white, tint);
            }
        }
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Entity owner, out Tile tile);
        Texture2D farmerTexture, grazerTexture;
        IDisposable adapter;
        Query query;
        FlatPixelGreyboxCatalog injected;
        static Type AdapterType()
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelIncidentalActorCatalog");
            Assert.NotNull(type, "No native incidental actor catalog exists yet."); return type;
        }
        static IDisposable Construct(Texture2D farmer, Texture2D grazer)
            => (IDisposable)Activator.CreateInstance(AdapterType(), new object[] { farmer, grazer });
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art,
            AdapterType().GetMethod("TryResolve", new[] { typeof(Entity), typeof(Tile).MakeByRefType() }));
        void Create(bool integrate = false)
        {
            farmerTexture = new Texture2D(16,24); grazerTexture = new Texture2D(16,24);
            adapter = Construct(farmerTexture, grazerTexture); query = Resolver(adapter);
            if (integrate) { injected = new FlatPixelGreyboxCatalog((Texture2D)null); Set(injected,"_incidentalActorArt",adapter); }
        }
        Entity Owner(string blueprint) => content.Factory.CreateEntity(blueprint);
        Texture2D TextureFor(Entity owner) => owner.BlueprintName == "Farmer" ? farmerTexture : grazerTexture;
        static void Expect(Tile tile, Texture2D texture)
        {
            Assert.NotNull(tile); Assert.AreSame(texture,tile.sprite.texture);
            Assert.AreEqual(new Rect(0,0,16,24),tile.sprite.rect);
            Assert.AreEqual(new Vector2(8,12),tile.sprite.pivot); Assert.AreEqual(16,tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
        }
        Tile Positive(Entity owner)
        { Assert.True(query(owner,out var tile),owner.BlueprintName); Expect(tile,TextureFor(owner)); return tile; }

        [Test] public void IndependentCachedNativeBodiesPreserveBorrowedInputsAndGameplay()
        {
            Create(); Tile previous = null;
            foreach(var bp in new[]{"Farmer","ReedbackGrazer"})
            {
                var owner=Owner(bp); int parts=owner.Parts.Count; bool solid=owner.GetPart<PhysicsPart>().Solid;
                string glyph=owner.GetPart<RenderPart>().RenderString; var tile=Positive(owner);
                Assert.AreSame(tile,Positive(owner)); Assert.AreNotSame(previous,tile); previous=tile;
                Assert.AreEqual(parts,owner.Parts.Count); Assert.AreEqual(solid,owner.GetPart<PhysicsPart>().Solid);
                Assert.AreEqual(glyph,owner.GetPart<RenderPart>().RenderString);
                Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(owner),"Static artwork does not manufacture motion frames.");
            }
            Assert.AreEqual(FilterMode.Bilinear,farmerTexture.filterMode); Assert.AreEqual(TextureWrapMode.Repeat,grazerTexture.wrapMode);
        }
        [TestCase("visual-id")] [TestCase("variant")] [TestCase("glyph-variants")] [TestCase("glyph")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("missing-physics")]
        [TestCase("foreign-brain")] [TestCase("foreign-body")] [TestCase("missing-brain")]
        [TestCase("hidden")] [TestCase("creature-tag")] [TestCase("item-tag")] [TestCase("player-tag")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("takeable")] [TestCase("footprint")]
        public void IneligibleKnownOwnerNeverBorrowsTheBody(string fault)
        {
            Create();
            foreach(var bp in new[]{"Farmer","ReedbackGrazer"})
            {
                var owner=Owner(bp); Positive(owner); var render=owner.GetPart<RenderPart>(); var physics=owner.GetPart<PhysicsPart>();
                if(fault=="visual-id") render.VisualID="foreign";
                if(fault=="variant") render.VisualVariant="costume";
                if(fault=="glyph-variants") render.GlyphVariants="@g";
                if(fault=="glyph") render.RenderString="Q";
                if(fault=="foreign-render") render.ParentEntity=new Entity();
                if(fault=="foreign-physics") physics.ParentEntity=new Entity();
                if(fault=="missing-physics") owner.RemovePart(physics);
                if(fault=="foreign-brain") owner.GetPart<BrainPart>().ParentEntity=new Entity();
                if(fault=="foreign-body") owner.GetPart<Body>().ParentEntity=new Entity();
                if(fault=="missing-brain") owner.RemovePart(owner.GetPart<BrainPart>());
                if(fault=="hidden") render.Visible=false;
                if(fault=="creature-tag") owner.Tags.Remove("Creature");
                if(fault=="item-tag") owner.SetTag("Item");
                if(fault=="player-tag") owner.SetTag("Player");
                if(fault=="carried") physics.InInventory=new Entity();
                if(fault=="equipped") physics.Equipped=new Entity();
                if(fault=="takeable") physics.Takeable=true;
                if(fault=="footprint") owner.AddPart(new SpatialFootprintPart { CellsRaw="0,0;1,0" });
                Assert.False(query(owner,out _),bp+" "+fault);
            }
        }
        [Test] public void ExactIdentityAndOwnedGrazerPartCannotBeBorrowedByLookalikesOrRemains()
        {
            Create();
            foreach(var bp in new[]{"Villager","SpreadWaysideCook","MarlbackCindercaller","MarlbackScrabbler","ReedbackGrazerCorpse","Magpie","Viper"})
                Assert.False(query(Owner(bp),out _),bp);
            var owner=Owner("ReedbackGrazer"); Positive(owner); var part=owner.GetPart<SpreadGrazerPart>();
            part.ParentEntity=new Entity(); Assert.False(query(owner,out _)); part.ParentEntity=owner;
            Positive(owner); Assert.True(owner.RemovePart(part)); Assert.False(query(owner,out _));
            var farmer=Owner("Farmer"); Positive(farmer); farmer.BlueprintName="UnknownFarmer"; Assert.False(query(farmer,out _));
        }
        [Test] public void MutableHealthMoodTradeAndForageStateDoNotChangeNeutralBodies()
        {
            Create();
            foreach(var bp in new[]{"Farmer","ReedbackGrazer"})
            {
                var owner=Owner(bp); var tile=Positive(owner); owner.GetStat("Hitpoints").BaseValue=1;
                owner.GetPart<BrainPart>().Passive=!owner.GetPart<BrainPart>().Passive;
                owner.GetPart<RenderPart>().DisplayName="Renamed "+bp;
                if(bp=="Farmer") { owner.GetPart<InventoryPart>().Objects.Clear(); owner.GetPart<TraderPart>().Drams=0; }
                else { var grazer=owner.GetPart<SpreadGrazerPart>(); grazer.Fed=true; grazer.Configured=true; grazer.ApproachAttempts=3; }
                Assert.AreSame(tile,Positive(owner));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedTextureDoesNotDisableItsPeerOrLoadAnImplicitResource(bool malformed)
        {
            Create(); var bad=malformed?new Texture2D(16,16):null;
            try
            {
                using(var left=Construct(bad,grazerTexture))
                { var q=Resolver(left); Assert.False(q(Owner("Farmer"),out _)); Assert.True(q(Owner("ReedbackGrazer"),out var tile)); Expect(tile,grazerTexture); }
                using(var right=Construct(farmerTexture,bad))
                { var q=Resolver(right); Assert.False(q(Owner("ReedbackGrazer"),out _)); Assert.True(q(Owner("Farmer"),out var tile)); Expect(tile,farmerTexture); }
                using(var neither=Construct(null,null)) Assert.False(Resolver(neither)(Owner("Farmer"),out _));
            }
            finally { if(bad!=null) Object.DestroyImmediate(bad); }
        }
        [Test] public void GeneratedObjectsAreDisposedIndependentlyAndBorrowedTexturesSurvive()
        {
            Create(true); var a=Positive(Owner("Farmer")); var b=Positive(Owner("ReedbackGrazer")); var sa=a.sprite; var sb=b.sprite;
            using(var peer=Construct(farmerTexture,grazerTexture))
            {
                var q=Resolver(peer); Assert.True(q(Owner("Farmer"),out var other)); Assert.AreNotSame(a,other);
                injected.Dispose(); injected.Dispose(); Assert.True(a==null); Assert.True(b==null); Assert.True(sa==null); Assert.True(sb==null);
                Assert.False(query(Owner("Farmer"),out _)); Assert.True(q(Owner("Farmer"),out _));
                Assert.True(farmerTexture!=null); Assert.True(grazerTexture!=null);
            }
        }
        [Test] public void WarmSemanticSelectionAllocatesNothingAndNeverAdvancesForage()
        {
            Create(); var owner=Owner("ReedbackGrazer"); var grazer=owner.GetPart<SpreadGrazerPart>(); grazer.ApproachAttempts=4;
            for(int i=0;i<50;i++) query(owner,out _);
            long before=GC.GetAllocatedBytesForCurrentThread(); int found=0;
            for(int i=0;i<1000;i++) if(query(owner,out _)) found++;
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.AreEqual(1000,found); Assert.Zero(allocated); Assert.AreEqual(4,grazer.ApproachAttempts);
        }
        [TestCase("Farmer")] [TestCase("ReedbackGrazer")]
        public void WorldAndAuthorizedReaderUseTheSameNativeFrameThenRetireLostAuthority(string blueprint)
        {
            Create(); using(var f=new PaintFixture(zone)) using(var portraits=PortraitProbe())
            {
                Set(f.Catalog,"_incidentalActorArt",adapter); var owner=Owner(blueprint); var viewer=Owner("Player");
                Assert.True(zone.AddEntity(viewer,X-1,Y)); Assert.True(zone.AddEntity(owner,X,Y));
                f.Renderer.MarkDirty("Initial actor fixture"); f.Frame(); f.Quiet(); Expect(f.ForegroundAt(X,Y),TextureFor(owner));
                var document=ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetEntityCell(owner),viewer);
                Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                var image=portraits.GetPortrait(document,f.Renderer); Assert.AreSame(TextureFor(owner),image.Texture); Assert.False(image.IsModel);
                Assert.AreEqual(new Rect(0,0,1,1),image.Uv);
                zone.GetCell(X,Y).IsVisible=false; f.Renderer.MarkCellDirty(X,Y,"Actual fog transition"); f.Frame();
                Assert.IsNull(f.ForegroundAt(X,Y)); Assert.IsNull(portraits.GetPortrait(document,f.Renderer).Texture);
            }
        }
        [Test] public void RealFarmerMovementRestoresTheCoveredBedWithoutFullRedrawOrChangingPassability()
        {
            Create(); var service=new Texture2D(288,24);
            try { using(var f=new PaintFixture(zone))
            {
                Set(f.Catalog,"_incidentalActorArt",adapter); Set(f.Catalog,"_eastKitchenArt",new FlatPixelEastKitchenCatalog(service,null));
                var bed=Owner("Bed"); var farmer=Owner("Farmer"); Assert.True(zone.AddEntity(bed,X,Y)); Assert.True(zone.AddEntity(farmer,X,Y));
                Assert.False(bed.GetPart<PhysicsPart>().Solid); Assert.True(farmer.GetPart<PhysicsPart>().Solid); Assert.True(zone.GetCell(X,Y).BlocksMovement());
                f.Renderer.MarkDirty("Initial cot fixture"); f.Frame(); f.Quiet(); Expect(f.ForegroundAt(X,Y),farmerTexture);
                long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(MovementSystem.TryMove(farmer,zone,1,0)); Assert.True(f.Dirty.Contains(Y*Zone.Width+X)); Assert.True(f.Dirty.Contains(Y*Zone.Width+X+1));
                f.Frame(); f.Quiet(); Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.AreSame(service,f.ForegroundAt(X,Y).sprite.texture); Assert.AreEqual(new Rect(32,0,16,24),f.ForegroundAt(X,Y).sprite.rect);
                Expect(f.ForegroundAt(X+1,Y),farmerTexture); Assert.False(zone.GetCell(X,Y).BlocksMovement());
                Assert.AreSame(zone.GetCell(X,Y),zone.GetEntityCell(bed)); Assert.False(bed.GetPart<BedPart>().Occupied);
            }} finally { Object.DestroyImmediate(service); }
        }
        [TestCase("Farmer")] [TestCase("ReedbackGrazer")]
        public void OldInjectionAndHiddenOrStaleOwnersStayRejectedWhileOnlyFarmerIsPortable(string blueprint)
        {
            Create(true); var owner=Owner(blueprint); Assert.True(zone.AddEntity(owner,X,Y));
            Assert.True(injected.TryResolvePresentation(zone,X,Y,out var positive)); Expect(positive.ForegroundTile,TextureFor(owner));
            using(var old=new FlatPixelGreyboxCatalog((Texture2D)null))
            { Assert.True(old.TryResolvePresentation(zone,X,Y,out var value)); Assert.False(value.NativeForeground); }
            zone.GetCell(X,Y).IsVisible=false; Assert.True(injected.TryResolvePresentation(zone,X,Y,out var memory)); Assert.IsNull(memory.Owner); Assert.IsNull(memory.ForegroundTile);
            zone.GetCell(X,Y).Explored=false; Assert.False(injected.TryResolvePresentation(zone,X,Y,out _));
            zone.GetCell(X,Y).Explored=zone.GetCell(X,Y).IsVisible=true;
            Assert.True(zone.MoveEntity(owner,X+1,Y)); zone.GetCell(X,Y).Objects.Add(owner);
            Assert.True(injected.TryResolvePresentation(zone,X,Y,out var stale)); Assert.IsNull(stale.Owner);
            Assert.True(injected.TryResolvePresentation(zone,X+1,Y,out var moved)); Expect(moved.ForegroundTile,TextureFor(owner));
            var outside=new Zone("Overworld.13.10.0"); manager.SetActiveZone(outside); outside.GetCell(X,Y).IsVisible=outside.GetCell(X,Y).Explored=true;
            Assert.True(zone.RemoveEntity(owner)); Assert.True(outside.AddEntity(owner,X,Y));
            Assert.True(injected.TryResolvePresentation(outside,X,Y,out var other));
            Assert.True(injected.TryGetPresentationSubjectSprite(outside,owner,X,Y,out var image,out _));
            if (blueprint == "Farmer")
            { Assert.True(other.NativeForeground); Expect(other.ForegroundTile, TextureFor(owner)); Assert.AreSame(TextureFor(owner), image.texture); }
            else { Assert.False(other.NativeForeground); Assert.AreNotSame(TextureFor(owner), image.texture); }
            manager.CachedZones[outside.ZoneID]=new Zone(outside.ZoneID);
            Assert.False(injected.TryResolvePresentation(outside,X,Y,out _)); Assert.False(injected.TryGetPresentationSubjectSprite(outside,owner,X,Y,out _,out _));
        }
        [TestCase("visual-id")] [TestCase("variant")] [TestCase("glyph")]
        [TestCase("carried")] [TestCase("foreign-render")] [TestCase("foreign-physics")]
        public void VisibleCurrentWorldCannotBypassSemanticRejectionThroughAnOlderAlias(string fault)
        {
            Create(true); var owner=Owner("Farmer"); Assert.True(zone.AddEntity(owner,X,Y));
            Assert.True(injected.TryResolvePresentation(zone,X,Y,out var initial)); Expect(initial.ForegroundTile,farmerTexture);
            var r=owner.GetPart<RenderPart>(); var p=owner.GetPart<PhysicsPart>();
            if(fault=="visual-id") r.VisualID="custom"; if(fault=="variant") r.VisualVariant="custom"; if(fault=="glyph") r.RenderString="Q";
            if(fault=="carried") p.InInventory=new Entity(); if(fault=="foreign-render") r.ParentEntity=new Entity(); if(fault=="foreign-physics") p.ParentEntity=new Entity();
            Assert.True(injected.TryResolvePresentation(zone,X,Y,out var rejected)); Assert.False(rejected.NativeForeground);
            Assert.True(rejected.ForegroundTile==null || rejected.ForegroundTile.sprite.texture!=farmerTexture);
            if(fault=="visual-id"||fault=="variant"||fault=="glyph")
            { Assert.True(injected.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var image,out _)); Assert.AreNotSame(farmerTexture,image.texture); }
        }
        [Test] public void DefaultResourceContractPreservesTheWholeApprovedGrazerAndIndependentStaticFrames()
        {
            var farmer=Resources.Load<Texture2D>("Sprites/Live16x24/farmer"); var grazer=Resources.Load<Texture2D>("Sprites/Live16x24/reedback-grazer");
            Assert.NotNull(farmer,"Native Farmer resource has not been imported."); Assert.NotNull(grazer,"Approved Grazer resource has not been connected.");
            foreach(var texture in new[]{farmer,grazer}) { Assert.AreEqual(16,texture.width); Assert.AreEqual(24,texture.height); Assert.AreEqual(FilterMode.Point,texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp,texture.wrapMode); Assert.AreEqual(1,texture.mipmapCount); }
            using(var actual=(IDisposable)Activator.CreateInstance(AdapterType()))
            { var q=Resolver(actual); Assert.True(q(Owner("Farmer"),out var a)); Expect(a,farmer); Assert.True(q(Owner("ReedbackGrazer"),out var b)); Expect(b,grazer); }
            using(var sha=System.Security.Cryptography.SHA256.Create())
            {
                var approved=System.IO.File.ReadAllBytes("Assets/Art/ReedbackGrazer16x24/03-ridge.png");
                var imported=System.IO.File.ReadAllBytes("Assets/Resources/Sprites/Live16x24/reedback-grazer.png");
                CollectionAssert.AreEqual(approved,imported,"Approved pixels/PNG bytes must remain exact.");
                Assert.AreEqual("29db64b90637fdd305a355907c06547c5f46d82f912ec95a88307d92f28b4dad",BitConverter.ToString(sha.ComputeHash(imported)).Replace("-","").ToLowerInvariant());
            }
        }
        static ExaminePortraitRenderer PortraitProbe() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer),I,null,
            new object[]{(Action<Camera,RenderTexture>)((camera,target)=>{})},null);
        static object Get(object owner,string name) => owner.GetType().GetField(name,I).GetValue(owner);
        static void Set(object owner,string name,object value)
        { var field=owner.GetType().GetField(name,I); Assert.NotNull(field,"Missing integration seam: "+name); field.SetValue(owner,value); }

        sealed class PaintFixture : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject grid;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly EntityMovedVisualHandler oldMoved = EntityVisualHooks.MovedCallback;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly Action<string> oldMessage = MessageLog.OnMessage; readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            readonly Tilemap overlay;
            public readonly FlatPixelGreyboxCatalog Catalog; public readonly Zone Zone; public readonly ZoneRenderer Renderer;
            public HashSet<int> Dirty => (HashSet<int>)Get(Renderer, "_dirtyCells");
            public PaintFixture(Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null; saved = new HotbarSaveFixture(); EntityVisualHooks.MovedCallback = null;
                    grid = new GameObject("Incidental actor Grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); Assert.NotNull(main);
                    var bg = new GameObject("Incidental actor background", typeof(Tilemap)); bg.transform.SetParent(saved.Root.transform, false);
                    Renderer = saved.Renderer; Set(Renderer, "_tilemap", main); Set(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>());
                    var camera = new GameObject("Incidental actor camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false);
                    camera.GetComponent<Camera>().orthographic = true; Set(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    Catalog = new FlatPixelGreyboxCatalog((Texture2D)null); Set(env, "_flatPixelGreybox", Catalog); overlay = (Tilemap)Get(env, "_overlayTilemap");
                    Set(Renderer, "_envSpriteRenderer", env); Set(Renderer, "_flatPixelModeEnabled", true); Zone = zone; Renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                    Frame(); Quiet();
                }
                catch { Dispose(); throw; }
            }
            public Tile ForegroundAt(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0));
            public void Frame() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; } }
            public void Quiet() { Assert.False((bool)Get(Renderer, "_fullDirty")); Assert.Zero(Dirty.Count); }
            public void Dispose()
            {
                try { saved?.Dispose(); if (grid != null) Object.DestroyImmediate(grid); }
                finally
                {
                    Catalog?.Dispose(); EntityVisualHooks.MovedCallback = oldMoved; ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                    MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
        }
    }
}
