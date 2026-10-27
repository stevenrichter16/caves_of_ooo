// Ordinary MawToad native body contract. Reflection keeps the optional ninth
// texture overload testable as genuine RED before production is extended.
using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelMawToadTests
    {
        const int X = 10, Y = 10;
        const string ResourcePath = "Sprites/Live16x24/maw-toad";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Existing = { "CascadeFather", "GlasspaneFrog", "YellowfootWayfarer", "Wardline", "SariSnake", "SummitSinger", "BrocchiniaSentinel", "SkySari" };
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        IDisposable adapter;
        readonly List<Texture2D> borrowed = new List<Texture2D>();
        EntityDamageVisualHandler oldDamage;
        EntityMovedVisualHandler oldMoved;
        EntityFactory oldCorpseFactory;
        Action<string> oldMessage;

        [SetUp] public void Setup()
        {
            oldDamage = EntityVisualHooks.DamageCallback; oldMoved = EntityVisualHooks.MovedCallback; oldMessage = MessageLog.OnMessage;
            oldCorpseFactory = CorpsePart.Factory;
            EntityVisualHooks.DamageCallback = null; EntityVisualHooks.MovedCallback = null; MessageLog.OnMessage = null;
            content = new DensityLootTestScope(); CorpsePart.Factory = content.Factory;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = Attach("Overworld.2.2.0"); world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); adapter?.Dispose(); foreach (var texture in borrowed) if (texture != null) Object.DestroyImmediate(texture); borrowed.Clear(); content?.Dispose(); }
            finally { CorpsePart.Factory = oldCorpseFactory; EntityVisualHooks.DamageCallback = oldDamage; EntityVisualHooks.MovedCallback = oldMoved; MessageLog.OnMessage = oldMessage; }
        }
        Zone Attach(string id)
        { var graph = new Zone(id); manager.SetActiveZone(graph); foreach (var cell in graph.Cells) cell.Explored = cell.IsVisible = true; return graph; }
        Entity Owner(string name = "MawToad") { var value = content.Factory.CreateEntity(name); Assert.NotNull(value, name); return value; }
        Entity Add(string name = "MawToad", int x = X, int y = Y)
        { var value = Owner(name); Assert.True(zone.AddEntity(value, x, y)); return value; }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var value)); return value; }
        static Texture2D Resource()
        { var texture = Resources.Load<Texture2D>(ResourcePath); Assert.NotNull(texture, "The dedicated native MawToad resource is missing."); return texture; }
        static void Native(Tile tile, Texture2D texture)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1, 1.5f), (Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelStumpFaunaCatalog"); Assert.NotNull(type); return type; }
        delegate bool Query(Entity owner, out Tile tile);
        static IDisposable Construct(params Texture2D[] textures) => (IDisposable)Activator.CreateInstance(AdapterType(), Array.ConvertAll(textures, value => (object)value));
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art, AdapterType().GetMethod("TryResolve", new[] { typeof(Entity), typeof(Tile).MakeByRefType() }));
        Texture2D[] Textures()
        { var result = new Texture2D[9]; for (int i = 0; i < result.Length; i++) { result[i] = i < 5 ? new Texture2D(64, 384) : new Texture2D(16, i == 8 ? 24 : 16); borrowed.Add(result[i]); } return result; }
        static object Read(object target, string name) => target.GetType().GetField(name, I).GetValue(target);
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        static ExaminePortraitRenderer Portraits() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I, null,
            new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        AnnouncementDocument Document(Entity owner, Entity viewer = null)
        { var result = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetEntityCell(owner), viewer); Assert.NotNull(result); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(result)); return result; }

        [Test] public void ExactLiveBodyUsesNativeGeometryInSubmittedWorldAndAuthorizedReader()
        {
            var owner = Add(); var viewer = Add("Player", X - 1, Y); var probe = new RenderProbe(); owner.AddPart(probe);
            Assert.AreSame(owner, owner.GetPart<Body>().ParentEntity, "Existing anatomy is authoritative; no Frog anatomy is assumed.");
            var texture = Resource(); Assert.AreEqual(16, texture.width); Assert.AreEqual(24, texture.height);
            var view = View(); Assert.AreSame(owner, view.Owner); Assert.True(view.NativeForeground); Native(view.ForegroundTile, texture);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); Native(paint.At(X, Y), texture); Assert.AreEqual(Color.white, paint.Tint(X, Y)); Assert.AreEqual(Matrix4x4.identity, paint.Transform(X, Y));
                int count = probe.Renders; var document = Document(owner, viewer); var image = portraits.GetPortrait(document, paint.Renderer);
                Assert.AreSame(texture, image.Texture); Assert.AreEqual(new Rect(0, 0, 1, 1), image.Uv); Assert.False(image.IsModel);
                var rect = AnnouncementUI.MeasurePortraitRect(new Rect(3, 5, 79, 95), image);
                Assert.Greater(rect.width, 0); Assert.AreEqual(0, rect.width % 16); Assert.AreEqual(rect.width * 1.5f, rect.height);
                Assert.AreEqual(count, probe.Renders, "Reading must not dispatch Render.");
                zone.GetCell(X, Y).IsVisible = false; paint.Dirty(X, Y); paint.Step();
                Assert.IsNull(paint.At(X, Y)); Assert.IsNull(portraits.GetPortrait(document, paint.Renderer).Texture);
            }
        }

        [Test] public void RealNonlethalDamageProducesThreeSingleEventHurtPaintsThenNeutralRecovery()
        {
            var owner = Add(); var probe = new RenderProbe(); owner.AddPart(probe); var hp = owner.GetStat("Hitpoints"); int initialHP = hp.BaseValue;
            Assert.NotNull(owner.GetPart<DamageFlashPart>());
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); int initial = probe.Renders;
                CombatSystem.ApplyDamage(owner, 1, null, zone); Assert.AreEqual(initialHP - 1, hp.BaseValue); Assert.Greater(hp.BaseValue, 0);
                Assert.AreEqual(1, probe.Damages); Assert.AreEqual(initial, probe.Renders);
                paint.Step(); Assert.AreEqual(initial + 1, probe.Renders); Native(paint.At(X, Y), Resource()); Assert.AreEqual(QudColorParser.Parse("&R"), paint.Tint(X, Y));
                Assert.AreSame(Resource(), portraits.GetPortrait(Document(owner), paint.Renderer).Texture);
                View(); paint.Dirty(X + 1, Y); paint.Step(); paint.Step(); Assert.AreEqual(initial + 1, probe.Renders, "Neither neighbor nor reader consumes a hurt frame.");
                for (int frame = 2; frame <= 4; frame++)
                {
                    paint.Dirty(X, Y); paint.Step(); Assert.AreEqual(initial + frame, probe.Renders);
                    Native(paint.At(X, Y), Resource()); Assert.AreEqual(frame < 4 ? QudColorParser.Parse("&R") : Color.white, paint.Tint(X, Y));
                }
                Assert.AreEqual(initialHP - 1, hp.BaseValue); Assert.AreEqual(1, probe.Damages);
            }
        }

        [Test] public void SuccessfullyAppliedWetKeepsTheBodyAndRemovalRestoresNeutralColor()
        {
            var owner = Add(); var probe = new RenderProbe(); owner.AddPart(probe); int hp = owner.GetStat("Hitpoints").BaseValue;
            var effects = owner.GetPart<StatusEffectsPart>(); if (effects == null) { effects = new StatusEffectsPart(); owner.AddPart(effects); }
            var wet = new WetEffect();
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); int before = probe.Renders;
                Assert.True(effects.ForceApplyEffect(wet, null, zone), "This must exercise an applied effect, not an immune no-op.");
                Assert.AreEqual("&B", effects.GetRenderColorOverride()); paint.Dirty(X, Y); paint.Step();
                Assert.AreEqual(before + 1, probe.Renders); Native(paint.At(X, Y), Resource()); Assert.AreEqual(QudColorParser.Parse("&B"), paint.Tint(X, Y));
                Assert.True(effects.RemoveEffect(wet)); paint.Dirty(X, Y); paint.Step();
                Assert.AreEqual(before + 2, probe.Renders); Native(paint.At(X, Y), Resource()); Assert.AreEqual(Color.white, paint.Tint(X, Y));
                Assert.AreEqual(hp, owner.GetStat("Hitpoints").BaseValue);
            }
        }

        [TestCase("wrong-glyph")] [TestCase("variant")] [TestCase("visual-id")]
        [TestCase("foreign-body")] [TestCase("dead")] [TestCase("carried")] [TestCase("equipped")]
        public void IdenticalOwnerWithUnsupportedBodyOrCustodyDoesNotKeepNativeArt(string fault)
        {
            var owner = Add(); Native(View().ForegroundTile, Resource());
            switch (fault)
            {
                case "wrong-glyph": owner.GetPart<RenderPart>().RenderString = "u"; break;
                case "variant": owner.GetPart<RenderPart>().VisualVariant = "other body"; break;
                case "visual-id": owner.GetPart<RenderPart>().VisualID = "other body"; break;
                case "foreign-body": owner.GetPart<Body>().ParentEntity = new Entity(); break;
                case "dead": owner.GetStat("Hitpoints").BaseValue = 0; break;
                case "carried": owner.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                case "equipped": owner.GetPart<PhysicsPart>().Equipped = new Entity(); break;
                default: Assert.Fail(fault); break;
            }
            using (var art = new FlatPixelStumpFaunaCatalog()) Assert.False(art.TryResolve(owner, out _));
            var view = View(); Assert.False(view.NativeForeground);
            if (fault == "carried" || fault == "equipped")
            { Assert.IsNull(view.Owner); Assert.IsNull(view.ForegroundTile, "Carried/equipped owners must disappear from world claims."); }
            else { Assert.NotNull(view.ForegroundTile); Assert.AreNotSame(Resource(), view.ForegroundTile.sprite.texture); }
        }

        [Test] public void RealDeathSpawnsSeparateCorpseWithoutInheritingTheLivingSprite()
        {
            var owner = Add(); var spawner = owner.GetPart<CorpsePart>(); Assert.NotNull(spawner);
            Assert.AreEqual(100, spawner.CorpseChance); Assert.AreEqual("FrogOil", spawner.HarvestBlueprint);
            spawner.TestRng = new System.Random(65);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); Native(paint.At(X, Y), Resource()); var document = Document(owner);
                CombatSystem.ApplyDamage(owner, owner.GetStat("Hitpoints").BaseValue, null, zone); paint.Step();
                Assert.IsNull(zone.GetEntityCell(owner)); var corpse = spawner.CreatedCorpse; Assert.NotNull(corpse);
                Assert.AreNotSame(owner, corpse); Assert.AreEqual("CreatureCorpse", corpse.BlueprintName); Assert.AreEqual("MawToad", corpse.GetProperty("SourceBlueprint"));
                Assert.AreSame(zone.GetCell(X, Y), zone.GetEntityCell(corpse)); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.IsNull(portraits.GetPortrait(document, paint.Renderer).Texture);
                Assert.AreSame(corpse, View().Owner); Assert.AreNotSame(Resource(), paint.At(X, Y).sprite.texture);
                using (var art = Construct(Textures())) Assert.False(Resolver(art)(corpse, out _));
            }
        }

        [Test] public void RegisteredTwoByTwoFootprintRefusesOneCellArtAndRestoringItRecovers()
        {
            var owner = Add(); Native(View().ForegroundTile, Resource());
            owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" });
            Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0;0,1;1,1"));
            Assert.AreEqual(4, zone.GetOccupiedCells(owner).Count);
            Assert.False(View().NativeForeground); Assert.False(View(X + 1, Y + 1).NativeForeground);
            Assert.True(zone.TryChangeFootprint(owner, "0,0")); Native(View().ForegroundTile, Resource());
        }

        [Test] public void StaleMembershipAndReplacementGraphCannotGrantCurrentWorldOrReaderAuthority()
        {
            var owner = Add(); Native(View().ForegroundTile, Resource()); var document = Document(owner);
            Assert.True(zone.MoveEntity(owner, X + 1, Y)); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
            zone.GetCell(X, Y).Objects.Add(owner); Assert.IsNull(View().Owner); Native(View(X + 1, Y).ForegroundTile, Resource());
            manager.CachedZones[zone.ZoneID] = new Zone(zone.ZoneID);
            Assert.False(world.TryResolvePresentation(zone, X + 1, Y, out _)); Assert.False(world.TryGetPresentationSubjectSprite(zone, owner, X + 1, Y, out _, out _));
        }

        [Test] public void ActualMovementAndRemovalClearOldClaimsWithoutRepaintingGroundOrRetainingPortrait()
        {
            Add("TepuiStone"); Add("TepuiStone", X + 1, Y); var owner = Add();
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); var oldBody = paint.At(X, Y); Native(oldBody, Resource());
                var ground = paint.GroundAt(X, Y); var nextGround = paint.GroundAt(X + 1, Y); long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.NotNull(ground, "The movement countercheck must preserve an actual ground tile.");
                Assert.NotNull(nextGround, "The destination ground must exist before movement.");
                var document = Document(owner); Assert.True(MovementSystem.TryMove(owner, zone, 1, 0)); paint.Step();
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses); Assert.AreNotSame(oldBody, paint.At(X, Y)); Native(paint.At(X + 1, Y), Resource());
                Assert.AreSame(ground, paint.GroundAt(X, Y)); Assert.AreSame(nextGround, paint.GroundAt(X + 1, Y)); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                document = Document(owner); Assert.True(zone.RemoveEntity(owner)); paint.Dirty(X + 1, Y); paint.Step();
                Assert.AreNotSame(oldBody, paint.At(X + 1, Y)); Assert.AreSame(nextGround, paint.GroundAt(X + 1, Y)); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); Assert.IsNull(portraits.GetPortrait(document, paint.Renderer).Texture);
            }
        }

        [Test] public void LiveMawToadRemainsPortableOutsideStumpWithoutHabitatOrAttitudeGate()
        {
            zone = Attach("Overworld.13.10.0"); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var owner = Add(); owner.GetStat("Hitpoints").BaseValue = 1; owner.GetPart<BrainPart>().Passive = true; owner.GetPart<RenderPart>().DisplayName = "A traveling toad";
            var view = View(); Assert.AreSame(owner, view.Owner); Assert.True(view.NativeForeground); Native(view.ForegroundTile, Resource());
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            { paint.Step(); Native(paint.At(X, Y), Resource()); Assert.AreSame(Resource(), portraits.GetPortrait(Document(owner), paint.Renderer).Texture); }
        }

        [TestCase(false)] [TestCase(true)]
        public void OptionalNinthInputFailsIndependentlyAndSubmittedFallbackKeepsGlyph(bool malformed)
        {
            var textures = Textures(); using (var valid = Construct(textures)) { Assert.True(Resolver(valid)(Owner(), out var native)); Native(native, textures[8]); }
            var invalid = malformed ? new Texture2D(16, 16) : null; if (invalid != null) borrowed.Add(invalid); textures[8] = invalid;
            adapter = Construct(textures); var query = Resolver(adapter); Assert.False(query(Owner(), out var missing)); Assert.IsNull(missing);
            for (int i = 0; i < Existing.Length; i++) { Assert.True(query(Owner(Existing[i]), out var tile)); Assert.AreSame(textures[i], tile.sprite.texture); }
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); Write(world, "_stumpFaunaArt", adapter); var owner = Add();
            var view = View(); Assert.AreSame(owner, view.Owner); Assert.False(view.NativeForeground); Assert.AreEqual("t", owner.GetPart<RenderPart>().RenderString);
            using (var paint = new Paint(world, zone)) { paint.Step(); Assert.NotNull(paint.At(X, Y)); Assert.AreNotSame(invalid, paint.At(X, Y).sprite.texture); }
        }

        [Test] public void FiveAndEightArgumentInjectionStillOmitMawWhileNinthCanStandAlone()
        {
            var textures = Textures(); foreach (int count in new[] { 5, 8 })
            {
                var inputs = new Texture2D[count]; Array.Copy(textures, inputs, count);
                using (var legacy = Construct(inputs)) { var query = Resolver(legacy); for (int i = 0; i < count; i++) Assert.True(query(Owner(Existing[i]), out _)); Assert.False(query(Owner(), out _), "Explicit old injection must not load hidden Maw resources."); }
            }
            var onlyMaw = new Texture2D[9]; onlyMaw[8] = textures[8];
            using (var art = Construct(onlyMaw)) { var query = Resolver(art); Assert.True(query(Owner(), out var tile)); Native(tile, textures[8]); foreach (var name in Existing) Assert.False(query(Owner(name), out _)); }
        }

        [Test] public void SharedGlyphNameAndCorpseProvenanceNeverSelectTheMawBody()
        {
            var inputs = new Texture2D[9]; inputs[8] = Textures()[8]; using (var art = Construct(inputs))
            {
                var query = Resolver(art); Assert.True(query(Owner(), out _));
                foreach (var name in new[] { "YellowfootWayfarer", "HelmwoodFrog", "CreatureCorpse" })
                { var owner = Owner(name); owner.GetPart<RenderPart>().DisplayName = "maw-toad"; owner.Properties["SourceBlueprint"] = "MawToad"; Assert.False(query(owner, out _)); }
            }
        }

        [Test] public void DisposalDestroysOnlyOwnedMawPairAndLeavesBorrowedTexturesAndPeerAlive()
        {
            var textures = Textures(); adapter = Construct(textures); var query = Resolver(adapter);
            Assert.True(query(Owner(), out var tile)); var sprite = tile.sprite; Native(tile, textures[8]);
            using (var peer = Construct(textures))
            {
                adapter.Dispose(); adapter.Dispose(); Assert.True(tile == null); Assert.True(sprite == null); Assert.False(query(Owner(), out _));
                Assert.True(Resolver(peer)(Owner(), out var other)); Native(other, textures[8]);
                foreach (var texture in textures) { Assert.True(texture != null); Assert.AreEqual(FilterMode.Bilinear, texture.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, texture.wrapMode); }
            }
        }

        sealed class RenderProbe : Part
        {
            public int Renders, Damages;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "Render") Renders++; if (e.ID == "TakeDamage") Damages++; return true; }
        }
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, main, ground;
            public ZoneRenderer Renderer { get; }
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
                    saved = new HotbarSaveFixture(); root = new GameObject("MawToad submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(Position(x, y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "MawToad fixture");
            public void Step() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try { saved?.Dispose(); if (root != null) Object.DestroyImmediate(root); }
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
