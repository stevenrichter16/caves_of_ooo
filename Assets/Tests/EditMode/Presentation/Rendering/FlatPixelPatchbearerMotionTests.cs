using System;
using System.Collections;
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
    public sealed class FlatPixelPatchbearerMotionTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string CatalogName = "CavesOfOoo.Rendering.FlatPixelPatchbearerMotionCatalog";
        delegate bool AssetQuery(Zone zone, Entity owner, out EntityVisualAsset asset);

        [TestCase(false)] [TestCase(true)]
        public void ActualStockSelectsCachedDerivativesAndBorrowsApprovedIdlePixels(bool empty)
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Works, f.Motion))
            {
                if (empty) Assert.True(f.Inventory.RemoveObject(f.Tonic));
                var asset = c.Resolve(f.Zone, f.Actor); int offset = empty ? 4 : 0;
                Assert.AreSame(asset, c.Resolve(f.Zone, f.Actor));
                foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
                {
                    var idle = asset.GetFrame(EntityVisualState.Idle, facing, 0);
                    Frame(idle, f.Works, empty ? 8 : 7);
                    Frame(asset.GetFrame(EntityVisualState.Walk, facing, 0), f.Motion, offset);
                    Frame(asset.GetFrame(EntityVisualState.Walk, facing, 1), f.Motion, offset + 1);
                    Frame(asset.GetFrame(EntityVisualState.Attack, facing, 0), f.Motion, offset + 2);
                    Frame(asset.GetFrame(EntityVisualState.Hurt, facing, 0), f.Motion, offset + 3);
                    Assert.AreSame(idle, asset.GetFrame(EntityVisualState.Attack, facing, 1));
                    Assert.AreSame(idle, asset.GetFrame(EntityVisualState.Hurt, facing, 1));
                    Assert.AreSame(idle, asset.GetFrame(EntityVisualState.Cast, facing, 0));
                }
                Assert.AreEqual(.10f, asset.Definition.MoveDuration);
                Assert.AreEqual(.16f, asset.Definition.AttackDuration);
                Assert.AreEqual(.14f, asset.Definition.HurtDuration);
                Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(f.Actor));
                Assert.AreEqual(empty ? null : f.Tonic, f.Actor.GetPart<FieldMedicinePart>().FindCarriedMedicine());
            }
        }

        [TestCase("Render")] [TestCase("Physics")] [TestCase("Body")] [TestCase("Brain")]
        [TestCase("FieldMedicine")] [TestCase("Inventory")]
        public void RequiredPartsMustBelongToTheExactActorEvenWhenIDsMatch(string name)
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Works, f.Motion))
            {
                c.Resolve(f.Zone, f.Actor); var part = f.Actor.GetPart(name); Assert.NotNull(part);
                part.ParentEntity = new Entity { ID = f.Actor.ID };
                Assert.False(c.Try(f.Zone, f.Actor, out _)); part.ParentEntity = f.Actor;
                c.Resolve(f.Zone, f.Actor); Assert.True(f.Actor.RemovePart(part));
                Assert.False(c.Try(f.Zone, f.Actor, out _));
            }
        }

        [TestCase("corpse")] [TestCase("reskin")] [TestCase("inventory")] [TestCase("takeable")]
        public void ACurrentPlacedActorCannotBorrowMotionAfterItsMeaningChanges(string fault)
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Works, f.Motion))
            {
                c.Resolve(f.Zone, f.Actor);
                if (fault == "corpse") { f.Actor.BlueprintName = "CreatureCorpse"; f.Actor.SetTag("Corpse"); }
                if (fault == "reskin") f.Actor.GetPart<RenderPart>().VisualVariant = "foreign-harness";
                if (fault == "inventory") f.Actor.GetPart<PhysicsPart>().InInventory = new Entity();
                if (fault == "takeable") f.Actor.GetPart<PhysicsPart>().Takeable = true;
                Assert.False(c.Try(f.Zone, f.Actor, out _));
            }
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("covered")]
        [TestCase("replaced-cell")] [TestCase("foreign-cell")] [TestCase("replaced-graph")]
        public void StateResolutionRequiresActualCurrentDisclosedTopOwner(string fault)
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Works, f.Motion))
            {
                c.Resolve(f.Zone, f.Actor); var cell = f.Zone.GetCell(8, 9);
                if (fault == "hidden") cell.IsVisible = false;
                if (fault == "unexplored") cell.Explored = false;
                if (fault == "covered") Assert.True(f.Zone.AddEntity(Cover(), 8, 9));
                if (fault == "replaced-cell") f.Zone.Cells[8, 9] = new Cell(8, 9) { ParentZone = f.Zone };
                if (fault == "foreign-cell") cell.ParentZone = new Zone(f.Zone.ZoneID);
                if (fault == "replaced-graph") f.Manager.SetActiveZone(new Zone(f.Zone.ZoneID));
                Assert.False(c.Try(f.Zone, f.Actor, out _));
            }
        }

        [TestCase("Overworld.11.9.0", true)] [TestCase("Overworld.12.10.0", true)]
        [TestCase("Overworld.12.12.0", false)]
        public void ExistingLocalScenePolicyDoesNotCreateAGlobalFifthActor(string zoneID, bool expected)
        {
            using (var f = new OwnerFixture(zoneID)) using (var c = new Catalog(f.Works, f.Motion))
            {
                Assert.AreEqual(expected, c.Try(f.Zone, f.Actor, out _));
                Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(f.Actor));
            }
        }

        [TestCase("zero-stack")] [TestCase("foreign-custody")] [TestCase("equipped")]
        [TestCase("ground")]
        public void InvalidHealingSupplyUsesEmptyFramesWhileSeparateAntidoteRemains(string fault)
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Works, f.Motion))
            {
                var full = c.Resolve(f.Zone, f.Actor);
                if (fault == "zero-stack") f.Tonic.GetPart<StackerPart>().StackCount = 0;
                if (fault == "foreign-custody") f.Tonic.GetPart<PhysicsPart>().InInventory = new Entity { ID = f.Actor.ID };
                if (fault == "equipped") f.Tonic.GetPart<PhysicsPart>().Equipped = f.Actor;
                if (fault == "ground") { Assert.True(f.Inventory.RemoveObject(f.Tonic)); Assert.True(f.Zone.AddEntity(f.Tonic, 9, 9)); }
                var empty = c.Resolve(f.Zone, f.Actor); Assert.AreNotSame(full, empty);
                Frame(empty.GetFrame(EntityVisualState.Idle, EntityVisualFacing.East, 0), f.Works, 8);
                Assert.True(f.Inventory.Objects.Any(e => e.BlueprintName == "Antidote"));
            }
        }

        [TestCase("missing-works")] [TestCase("missing-motion")] [TestCase("wrong-width")]
        public void MissingOrMalformedArtRefusesWithoutChangingBorrowedTextures(string fault)
        {
            using (var f = new OwnerFixture())
            {
                var malformed = new Texture2D(127, 24);
                try
                {
                    var works = fault == "missing-works" ? null : f.Works;
                    var motion = fault == "missing-motion" ? null : fault == "wrong-width" ? malformed : f.Motion;
                    using (var c = new Catalog(works, motion)) Assert.False(c.Try(f.Zone, f.Actor, out _));
                    Assert.True(f.Works != null && f.Motion != null && malformed != null);
                }
                finally { Object.DestroyImmediate(malformed); }
            }
        }

        [Test] public void DisposalOwnsOnlyItsCachedSpritesAndIsIdempotent()
        {
            using (var f = new OwnerFixture())
            {
                var c = new Catalog(f.Works, f.Motion); var full = c.Resolve(f.Zone, f.Actor);
                Assert.True(f.Inventory.RemoveObject(f.Tonic)); var empty = c.Resolve(f.Zone, f.Actor);
                var a = full.GetFrame(EntityVisualState.Idle, EntityVisualFacing.East, 0);
                var b = empty.GetFrame(EntityVisualState.Hurt, EntityVisualFacing.East, 0);
                c.Dispose(); c.Dispose(); Assert.True(a == null && b == null);
                Assert.True(f.Works != null && f.Motion != null); Assert.False(c.Try(f.Zone, f.Actor, out _));
            }
        }

        [Test] public void ActualCellPassTakesOverOnlyTheBodyAndKeepsTheApprovedIdleAndGround()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); f.Idle(false); Assert.IsNull(f.Foreground(8, 9)); Assert.NotNull(f.Background(8, 9));
                Assert.AreEqual(Color.white, f.Body.color); Assert.False(f.Shadow.enabled);
                Assert.AreEqual(Vector3.zero, f.Body.transform.localPosition);
                Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(f.Owner.Actor));
                var view = f.Record; var sprite = f.Body.sprite;
                f.Renderer.MarkCellDirty(20, 20, "unrelated"); f.Frame();
                Assert.AreSame(view, f.Record); Assert.AreSame(sprite, f.Body.sprite);
            }
        }

        [Test] public void MissingMotionRetainsExistingStaticNorthBody()
        {
            using (var f = new Paint(false))
            {
                Assert.Zero(f.Actors.ActiveViewCount); var tile = f.Foreground(8, 9); Assert.NotNull(tile);
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath), tile.sprite.texture);
                Assert.AreEqual(new Rect(112, 0, 16, 24), tile.sprite.rect); Assert.NotNull(f.Background(8, 9));
            }
        }

        [Test] public void ExistingPortableIdentityChangeStillRetiresItsPreviousView()
        {
            using (var f = new Paint(blueprint: "MarlbackGleaner"))
            {
                f.RequireBody(); var oldBody = f.Body; var oldAsset = Get<EntityVisualAsset>(f.Record, "Asset");
                Assert.AreEqual(new Rect(48, 0, 16, 24), oldBody.sprite.rect);
                EntityVisualHooks.EmitAttack(f.Owner.Actor, null, f.Owner.Zone); f.Tick(.02f);
                f.Owner.Actor.BlueprintName = "MarlbackScrabbler";
                f.Tick(0);
                Assert.Zero(f.Actors.ActiveViewCount, "An unrelated portable-identity change must retain the prior retirement policy.");
                f.Renderer.MarkCellDirty(8, 9, "new portable identity"); f.Frame(); f.RequireBody();
                Assert.AreNotSame(oldAsset, Get<EntityVisualAsset>(f.Record, "Asset"));
                Assert.AreEqual(EntityVisualState.Idle, Get<EntityVisualState>(f.Record, "State"));
                Assert.AreEqual(new Rect(32, 0, 16, 24), f.Body.sprite.rect);
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath), f.Body.sprite.texture);
            }
        }

        [Test] public void RealMovementAndReversibleMedicineTransferKeepElapsedPositionAndFacing()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var actor = f.Owner.Actor; var view = f.Record;
                Assert.True(MovementSystem.TryMove(actor, f.Owner.Zone, -1, 0)); f.Frame(); f.Tick(.035f);
                Assert.AreEqual((7, 9), f.Owner.Zone.GetEntityPosition(actor)); var position = f.View.position;
                var recipient = f.Owner.Content.Create("MarlbackPatchbearer"); Assert.True(f.Owner.Zone.AddEntity(recipient, 12, 10));
                foreach (var item in recipient.GetPart<InventoryPart>().Objects.ToArray())
                    Assert.True(recipient.GetPart<InventoryPart>().RemoveObject(item));
                Transfer(f.Owner.Inventory, recipient.GetPart<InventoryPart>(), f.Owner.Tonic); f.Frame();
                Assert.AreSame(view, f.Record); Assert.AreEqual(position, f.View.position); Assert.True(f.Body.flipX);
                Assert.That(f.Time, Is.EqualTo(.035f).Within(.00001f)); f.Pose(4);
                f.Tick(.025f); f.Pose(5);
                Transfer(recipient.GetPart<InventoryPart>(), f.Owner.Inventory, f.Owner.Tonic); f.Frame();
                Assert.AreSame(view, f.Record); Assert.That(f.Time, Is.EqualTo(.06f).Within(.00001f)); f.Pose(1);
                f.Tick(.041f); f.Idle(false); Assert.AreEqual(7.5f, f.View.position.x);
                var wall = new Entity { BlueprintName = "Wall" }; wall.AddPart(new PhysicsPart { Solid = true });
                Assert.True(f.Owner.Zone.AddEntity(wall, 6, 9)); Assert.False(MovementSystem.TryMove(actor, f.Owner.Zone, -1, 0)); f.Idle(false);
            }
        }

        [Test] public void RealHealingDuringAttackAndQueuedHurtRebindsWithoutRestartOrFakeSelfUsePose()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var actor = f.Owner.Actor; var target = f.Threat(); var view = f.Record;
                actor.GetStat("Hitpoints").BaseValue = 7; target.GetStat("DV").BaseValue = 10000;
                int targetHP = target.GetStatValue("Hitpoints");
                Assert.True(CombatSystem.PerformMeleeAttack(actor, target, f.Owner.Zone, new System.Random(7)));
                Assert.AreEqual(targetHP, target.GetStatValue("Hitpoints"));
                CombatSystem.ApplyDamage(actor, 1, target, f.Owner.Zone); f.Tick(.02f); f.Pose(2);
                Assert.True(Get<bool>(f.Record, "HasPendingReaction"));
                var rng = new FieldMedicineFixture.MedicineRandom();
                Assert.True(actor.GetPart<FieldMedicinePart>().TryUseMedicine(target, f.Owner.Zone, rng));
                Assert.AreEqual(14, actor.GetStatValue("Hitpoints")); Assert.AreEqual(4, rng.Calls);
                Assert.False(f.Owner.Inventory.Objects.Contains(f.Owner.Tonic)); f.Frame();
                Assert.AreSame(view, f.Record); Assert.That(f.Time, Is.EqualTo(.02f).Within(.00001f));
                Assert.True(Get<bool>(f.Record, "HasPendingReaction")); f.Pose(6);
                f.Tick(.041f); f.Pose(7); f.Tick(.100f); f.Idle(true); f.Tick(.05f); f.Idle(true);
                Assert.AreEqual(14, actor.GetStatValue("Hitpoints")); Assert.AreEqual(4, rng.Calls);
                Assert.False(actor.GetPart<FieldMedicinePart>().TryUseMedicine(target, f.Owner.Zone, rng)); f.Idle(true);
            }
        }

        [Test] public void RealCureKeepsHealingBottleAndDoesNotReplaceAnOngoingHurt()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var actor = f.Owner.Actor; var threat = f.Threat();
                Assert.True(actor.ApplyEffect(new PoisonedEffect(6, "1d1"), threat, f.Owner.Zone));
                CombatSystem.ApplyDamage(actor, 1, threat, f.Owner.Zone); f.Tick(.03f); f.Pose(3); var view = f.Record;
                Assert.True(actor.GetPart<FieldMedicinePart>().TryUseMedicine(threat, f.Owner.Zone, new FieldMedicineFixture.MedicineRandom()));
                f.Frame(); Assert.AreSame(view, f.Record); Assert.That(f.Time, Is.EqualTo(.03f).Within(.00001f)); f.Pose(3);
                Assert.AreSame(f.Owner.Tonic, actor.GetPart<FieldMedicinePart>().FindCarriedMedicine());
                Assert.False(f.Owner.Inventory.Objects.Any(e => e.BlueprintName == "Antidote"));
                f.Tick(.12f); f.Idle(false);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void HiddenOrCoveredBodyRetiresAndUncoverResolvesCurrentMedicine(bool covered)
        {
            using (var f = new Paint())
            {
                f.RequireBody(); Entity cover = null;
                if (covered) { cover = Cover(); Assert.True(f.Owner.Zone.AddEntity(cover, 8, 9)); }
                else f.Owner.Zone.GetCell(8, 9).IsVisible = false;
                f.Tick(0); Assert.Zero(f.Actors.ActiveViewCount);
                Assert.True(f.Owner.Inventory.RemoveObject(f.Owner.Tonic));
                if (covered) Assert.True(f.Owner.Zone.RemoveEntity(cover)); else f.Owner.Zone.GetCell(8, 9).IsVisible = true;
                f.Renderer.MarkCellDirty(8, 9, "uncovered"); f.Frame(); f.RequireBody(); f.Idle(true);
            }
        }

        [Test] public void RealLethalDamageCapturesEmptyHarnessAfterStockDropsThenRetires()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var view = f.Record; int hp = f.Owner.Actor.GetStatValue("Hitpoints");
                CombatSystem.ApplyDamage(f.Owner.Actor, hp + 100, null, f.Owner.Zone);
                Assert.IsNull(f.Owner.Zone.GetEntityCell(f.Owner.Actor));
                Assert.NotNull(f.Owner.Zone.GetEntityCell(f.Owner.Tonic)); Assert.IsNull(f.Owner.Tonic.GetPart<PhysicsPart>().InInventory);
                Assert.AreSame(view, f.Record); Assert.True(Get<bool>(view, "Dying")); f.Pose(7);
                f.Tick(.281f); Assert.Zero(f.Actors.ActiveViewCount);
            }
        }

        [Test] public void RealDamageAndAcceptedMissKeepTheirExistingPositiveAndNegativeMeanings()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); CombatSystem.ApplyDamage(f.Owner.Actor, 0, null, f.Owner.Zone); f.Idle(false);
                CombatSystem.ApplyDamage(f.Owner.Actor, 1, null, f.Owner.Zone); f.Pose(3); f.Tick(.15f); f.Idle(false);
                var target = f.Threat(); target.GetStat("DV").BaseValue = 10000;
                Assert.True(CombatSystem.PerformMeleeAttack(f.Owner.Actor, target, f.Owner.Zone, new System.Random(7))); f.Pose(2); f.Tick(.17f);
                f.Owner.Actor.AddPart(new VetoAttack());
                Assert.False(CombatSystem.PerformMeleeAttack(f.Owner.Actor, target, f.Owner.Zone, new System.Random(7))); f.Idle(false);
            }
        }

        static void Transfer(InventoryPart from, InventoryPart to, Entity item)
        { Assert.True(from.RemoveObject(item)); Assert.True(to.AddObject(item)); }
        static Entity Cover()
        { var e = new Entity { BlueprintName = "UnillustratedCover" }; e.AddPart(new RenderPart { RenderString = "x", RenderLayer = 999 }); e.AddPart(new PhysicsPart()); return e; }
        sealed class VetoAttack : Part { public override bool HandleEvent(GameEvent e) => e.ID != "BeforeMeleeAttack"; }
        static T Get<T>(object owner, string name) => (T)owner.GetType().GetField(name, I).GetValue(owner);
        static object Call(object owner, string name, params object[] args)
        { try { return owner.GetType().GetMethod(name, I).Invoke(owner, args); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
        static void Frame(Sprite sprite, Texture2D texture, int frame)
        { Assert.NotNull(sprite); Assert.AreSame(texture, sprite.texture); Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), sprite.rect); Assert.AreEqual(new Vector2(8, 0), sprite.pivot); Assert.AreEqual(16, sprite.pixelsPerUnit); }

        sealed class Catalog : IDisposable
        {
            public readonly object Value; readonly AssetQuery query;
            public Catalog(Texture2D works, Texture2D motion)
            {
                var type = typeof(AnimatedEntityRenderer).Assembly.GetType(CatalogName);
                Assert.NotNull(type, "Missing local Patchbearer motion adapter.");
                Value = Activator.CreateInstance(type, new object[] { works, motion });
                var method = type.GetMethod("TryGetAsset", I); Assert.NotNull(method);
                query = (AssetQuery)Delegate.CreateDelegate(typeof(AssetQuery), Value, method);
            }
            public bool Try(Zone zone, Entity owner, out EntityVisualAsset asset) => query(zone, owner, out asset);
            public EntityVisualAsset Resolve(Zone zone, Entity owner) { Assert.True(Try(zone, owner, out var asset)); Assert.NotNull(asset); return asset; }
            public void Dispose() => ((IDisposable)Value).Dispose();
        }

        sealed class OwnerFixture : IDisposable
        {
            public readonly EntityEquipmentContentFixture Content;
            public readonly Zone Zone;
            public readonly OverworldZoneManager Manager;
            public readonly Entity Actor, Tonic;
            public readonly Texture2D Works, Motion;
            public InventoryPart Inventory => Actor.GetPart<InventoryPart>();
            public OwnerFixture(string zoneID = "Overworld.11.9.0")
            {
                try
                {
                    Content = new EntityEquipmentContentFixture(); Zone = new Zone(zoneID);
                    Manager = OverworldZoneManager.CreateDetached(Content.Factory, 64, true); Manager.SetActiveZone(Zone);
                    Actor = Content.Create("MarlbackPatchbearer"); Assert.NotNull(Actor);
                    Assert.True(Zone.AddEntity(Actor, 8, 9)); Actor.GetPart<BrainPart>().CurrentZone = Zone;
                    Tonic = Actor.GetPart<FieldMedicinePart>().FindCarriedMedicine(); Assert.NotNull(Tonic);
                    Assert.AreEqual(1, Tonic.GetPart<StackerPart>().StackCount);
                    foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true;
                    Works = new Texture2D(144, 24); Motion = new Texture2D(128, 24);
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            { if (Works != null) Object.DestroyImmediate(Works); if (Motion != null) Object.DestroyImmediate(Motion); Content?.Dispose(); }
        }

        sealed class NativeGraph : CameraCleanupFixture
        {
            public ZoneRenderer RendererValue => Renderer;
            public AnimatedEntityRenderer Actors => Read<AnimatedEntityRenderer>(Renderer, "_entityVisualRenderer");
            public Tilemap Overlay => Read<Tilemap>(Read<EnvironmentSpriteRenderer>(Renderer, "_envSpriteRenderer"), "_overlayTilemap");
            public Tilemap Background => Read<Tilemap>(Renderer, "_bgTilemap");
        }

        sealed class Paint : IDisposable
        {
            readonly NativeGraph graph; readonly Action restore; readonly Tilemap overlay, bg; readonly object injected;
            public readonly OwnerFixture Owner; public readonly ZoneRenderer Renderer; public readonly AnimatedEntityRenderer Actors;
            public object Record => ((IDictionary)typeof(AnimatedEntityRenderer).GetField("_views", I).GetValue(Actors))[Owner.Actor];
            public Transform View => Get<Transform>(Record, "RootTransform");
            public SpriteRenderer Body => Get<SpriteRenderer>(Record, "Body");
            public SpriteRenderer Shadow => Get<SpriteRenderer>(Record, "Shadow");
            public float Time => Get<float>(Record, "StateTime");
            public Paint(bool motion = true, string blueprint = "MarlbackPatchbearer")
            {
                var hooks = typeof(EntityVisualHooks).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));
                var cell = ZoneRenderHooks.CellDirtyCallback; var full = ZoneRenderHooks.FullDirtyCallback;
                var bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic); var oldBound = bound.GetValue(null);
                Zone previous = null; Action<int, int> oldChanged = null; Action oldSight = null;
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                restore = () => { foreach (var h in hooks) h.Key.SetValue(null, h.Value); ZoneRenderHooks.CellDirtyCallback = cell; ZoneRenderHooks.FullDirtyCallback = full; bound.SetValue(null, oldBound); if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; } };
                try
                {
                    Owner = new OwnerFixture(); Owner.Actor.BlueprintName = blueprint;
                    EntityVisualHooks.Reset(); graph = new NativeGraph(); graph.SetupCameraCleanup();
                    Renderer = graph.RendererValue; Actors = graph.Actors; overlay = graph.Overlay; bg = graph.Background;
                    // On old production this branch is absent: cell takeover fails on behavior, not a missing-API assertion.
                    var type = typeof(AnimatedEntityRenderer).Assembly.GetType(CatalogName);
                    if (type != null)
                    {
                        injected = Activator.CreateInstance(type, new object[] { Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath), motion ? Owner.Motion : null });
                        var field = typeof(AnimatedEntityRenderer).GetField("_flatPatchbearerMotionCatalog", I); Assert.NotNull(field); field.SetValue(Actors, injected);
                    }
                    typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Renderer, true);
                    Renderer.SetZone(Owner.Zone); ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty; Frame();
                    Assert.True(Renderer.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            public void RequireBody() { Assert.AreEqual(1, Actors.ActiveViewCount, "Actual EnvironmentSpriteRenderer cell path must admit the local Patchbearer."); Assert.NotNull(Record); }
            public void Frame() => Call(Renderer, "LateUpdate");
            public void Tick(float dt) => Actors.AdvancePresentation(dt);
            public void Pose(int index) => FlatPixelPatchbearerMotionTests.Frame(Body.sprite, Owner.Motion, index);
            public void Idle(bool empty) => FlatPixelPatchbearerMotionTests.Frame(Body.sprite, Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath), empty ? 8 : 7);
            public Tile Foreground(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public TileBase Background(int x, int y) => bg.GetTile(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Entity Threat()
            {
                var target = Owner.Content.Create("Viper"); Assert.True(Owner.Zone.AddEntity(target, 9, 9));
                Owner.Actor.GetPart<BrainPart>().SetPersonallyHostile(target); return target;
            }
            public void Dispose()
            { try { graph?.CleanupCameraFixture(); if (injected is IDisposable catalog) catalog.Dispose(); } finally { Owner?.Dispose(); restore?.Invoke(); } }
        }
    }
}
