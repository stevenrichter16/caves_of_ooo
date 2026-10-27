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
    // Native fixture only. Ordinary encountered motion and visual acceptance are separate gates.
    public sealed class FlatPixelCindercallerMotionTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string CatalogName = "CavesOfOoo.Rendering.FlatPixelCindercallerMotionCatalog";
        const string IdlePath = "Sprites/Live16x24/cindercaller";
        const string Spell = "Pyromancy_EmberSpit";
        delegate bool AssetQuery(Zone zone, Entity owner, out EntityVisualAsset asset);

        [Test] public void ExistingCompactConstructorStillUsesIdleForUnauthoredCasting()
        {
            var texture = new Texture2D(16, 24); var sprites = new List<Sprite>();
            try
            {
                for (int i = 0; i < 5; i++) sprites.Add(Sprite.Create(texture, new Rect(0, 0, 16, 24), new Vector2(.5f, 0), 16));
                var asset = new EntityVisualAsset(Definition(), sprites[0], sprites[1], sprites[2], sprites[3], sprites[4]);
                Assert.False(asset.HasCastingArt);
                foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
                    Assert.AreSame(sprites[0], asset.GetFrame(EntityVisualState.Cast, facing, 1));
                Assert.True(texture != null);
            }
            finally { foreach (var s in sprites) Object.DestroyImmediate(s); Object.DestroyImmediate(texture); }
        }

        [Test] public void OptionalCompactCastHasDistinctPoseAndBorrowedIdleRecoveryForEveryFacing()
        {
            var texture = new Texture2D(16, 24); var sprites = new List<Sprite>();
            try
            {
                for (int i = 0; i < 6; i++) sprites.Add(Sprite.Create(texture, new Rect(0, 0, 16, 24), new Vector2(.5f, 0), 16));
                var ctor = typeof(EntityVisualAsset).GetConstructor(new[] { typeof(EntityVisualDefinition), typeof(Sprite), typeof(Sprite), typeof(Sprite), typeof(Sprite), typeof(Sprite), typeof(Sprite) });
                Assert.NotNull(ctor, "Compact native actors need an optional cast pose without changing old callers.");
                var args = new object[] { Definition(), sprites[0], sprites[1], sprites[2], sprites[3], sprites[4], sprites[5] };
                var asset = (EntityVisualAsset)ctor.Invoke(args); Assert.True(asset.HasCastingArt);
                foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
                {
                    Assert.AreSame(sprites[5], asset.GetFrame(EntityVisualState.Cast, facing, 0));
                    Assert.AreSame(sprites[0], asset.GetFrame(EntityVisualState.Cast, facing, 1));
                    Assert.AreSame(sprites[3], asset.GetFrame(EntityVisualState.Attack, facing, 0));
                    Assert.AreSame(sprites[4], asset.GetFrame(EntityVisualState.Hurt, facing, 0));
                }
                args[6] = null; var absent = (EntityVisualAsset)ctor.Invoke(args); Assert.False(absent.HasCastingArt);
                Assert.AreSame(sprites[0], absent.GetFrame(EntityVisualState.Cast, EntityVisualFacing.East, 0));
            }
            finally { foreach (var s in sprites) Object.DestroyImmediate(s); Object.DestroyImmediate(texture); }
        }

        [Test] public void LocalCatalogCachesFiveDerivativesAndBorrowsUnchangedApprovedIdle()
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Idle, f.Motion))
            {
                var asset = c.Resolve(f.Zone, f.Actor); Assert.AreSame(asset, c.Resolve(f.Zone, f.Actor));
                Assert.True(asset.HasCastingArt); Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(f.Actor));
                foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
                {
                    var idle = asset.GetFrame(EntityVisualState.Idle, facing, 0); Frame(idle, f.Idle, 0);
                    Frame(asset.GetFrame(EntityVisualState.Walk, facing, 0), f.Motion, 0);
                    Frame(asset.GetFrame(EntityVisualState.Walk, facing, 1), f.Motion, 1);
                    Frame(asset.GetFrame(EntityVisualState.Attack, facing, 0), f.Motion, 2);
                    Frame(asset.GetFrame(EntityVisualState.Hurt, facing, 0), f.Motion, 3);
                    Frame(asset.GetFrame(EntityVisualState.Cast, facing, 0), f.Motion, 4);
                    foreach (var state in new[] { EntityVisualState.Attack, EntityVisualState.Hurt, EntityVisualState.Cast })
                        Assert.AreSame(idle, asset.GetFrame(state, facing, 1));
                }
                Assert.AreEqual(.10f, asset.Definition.MoveDuration); Assert.AreEqual(.16f, asset.Definition.AttackDuration);
                Assert.AreEqual(.14f, asset.Definition.HurtDuration);
            }
        }

        [TestCase("Render")] [TestCase("Physics")] [TestCase("Body")] [TestCase("Brain")]
        public void BodyPartsMustBelongToThisExactActorRatherThanAnEqualID(string name)
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Idle, f.Motion))
            {
                var asset = c.Resolve(f.Zone, f.Actor); var part = f.Actor.GetPart(name); Assert.NotNull(part);
                part.ParentEntity = new Entity { ID = f.Actor.ID }; Assert.False(c.Try(f.Zone, f.Actor, out _));
                part.ParentEntity = f.Actor; Assert.AreSame(asset, c.Resolve(f.Zone, f.Actor));
            }
        }

        [TestCase("reskin")] [TestCase("multicell")] [TestCase("custody")] [TestCase("corpse")]
        [TestCase("covered")] [TestCase("fog")] [TestCase("replaced-cell")] [TestCase("replaced-graph")]
        public void CachedLocalAssetDoesNotBypassCurrentMeaningAndDisclosure(string fault)
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Idle, f.Motion))
            {
                c.Resolve(f.Zone, f.Actor);
                if (fault == "reskin") f.Actor.GetPart<RenderPart>().VisualID = "another-body";
                if (fault == "multicell") f.Actor.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,1" });
                if (fault == "custody") f.Actor.GetPart<PhysicsPart>().InInventory = new Entity();
                if (fault == "corpse") { f.Actor.BlueprintName = "CreatureCorpse"; f.Actor.SetTag("Corpse"); }
                if (fault == "covered") Assert.True(f.Zone.AddEntity(Cover(), 8, 9));
                if (fault == "fog") f.Zone.GetCell(8, 9).IsVisible = false;
                if (fault == "replaced-cell") f.Zone.Cells[8, 9] = new Cell(8, 9) { ParentZone = f.Zone };
                if (fault == "replaced-graph") f.Manager.SetActiveZone(new Zone(f.Zone.ZoneID));
                Assert.False(c.Try(f.Zone, f.Actor, out _), fault);
            }
        }

        [Test] public void SixExistingLocalGraphsAcceptAndUnsupportedOrUnmanagedEastRefuses()
        {
            foreach (string id in new[] { "Overworld.11.10.0", "Overworld.11.10.1", "Overworld.11.9.0", "Overworld.11.8.0", "Overworld.12.10.0", "Overworld.12.11.0" })
                using (var f = new OwnerFixture(id)) using (var c = new Catalog(f.Idle, f.Motion))
                { Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(f.Zone), id); c.Resolve(f.Zone, f.Actor); Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(f.Actor)); }
            using (var f = new OwnerFixture("Overworld.12.12.0")) using (var c = new Catalog(f.Idle, f.Motion))
                Assert.False(c.Try(f.Zone, f.Actor, out _));
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Idle, f.Motion))
            {
                var unmanaged = new Zone(f.Zone.ZoneID); Assert.True(f.Zone.RemoveEntity(f.Actor)); Assert.True(unmanaged.AddEntity(f.Actor, 8, 9));
                unmanaged.GetCell(8, 9).Explored = unmanaged.GetCell(8, 9).IsVisible = true;
                Assert.False(c.Try(unmanaged, f.Actor, out _));
            }
        }

        [Test] public void NeutralBodyDoesNotUseFireMossWeaponCooldownHealthOrMoodAsPoseStock()
        {
            using (var f = new OwnerFixture()) using (var c = new Catalog(f.Idle, f.Motion))
            {
                var asset = c.Resolve(f.Zone, f.Actor); var inventory = f.Actor.GetPart<InventoryPart>();
                var moss = inventory.Objects.Single(e => e.BlueprintName == "FireMoss"); Assert.True(inventory.RemoveObject(moss));
                var cudgel = inventory.GetAllEquipped().Single(e => e.BlueprintName == "Cudgel"); Assert.True(InventorySystem.UnequipItem(f.Actor, cudgel));
                f.Actor.GetStat("Hitpoints").BaseValue = 1; f.Actor.GetPart<BrainPart>().Passive = true;
                f.Actor.GetPart<RenderPart>().DisplayName = "named cindercaller"; f.Ability.CooldownRemaining = 7;
                for (int i = 0; i < 20; i++) Assert.AreSame(asset, c.Resolve(f.Zone, f.Actor));
                Assert.AreEqual(7, f.Ability.CooldownRemaining); Assert.AreEqual(1, f.Actor.GetStatValue("Hitpoints"));
                Assert.False(inventory.Objects.Contains(moss)); Assert.IsNull(cudgel.GetPart<PhysicsPart>().Equipped);
                Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(f.Actor));
            }
        }

        [TestCase("idle")] [TestCase("motion")]
        public void MissingAndMalformedOptionalGroupCannotImplicitlyLoadReplacement(string group)
        {
            using (var f = new OwnerFixture())
            {
                var bad = new Texture2D(group == "idle" ? 15 : 79, 24);
                try
                {
                    using (var c = new Catalog(group == "idle" ? null : f.Idle, group == "motion" ? null : f.Motion)) Assert.False(c.Try(f.Zone, f.Actor, out _));
                    using (var c = new Catalog(group == "idle" ? bad : f.Idle, group == "motion" ? bad : f.Motion)) Assert.False(c.Try(f.Zone, f.Actor, out _));
                    Assert.True(f.Idle != null && f.Motion != null && bad != null);
                    Assert.AreEqual(FilterMode.Bilinear, f.Motion.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, f.Motion.wrapMode);
                }
                finally { Object.DestroyImmediate(bad); }
            }
        }

        [Test] public void DisposalOwnsSixSlicesAndLeavesBorrowedTexturesAlive()
        {
            using (var f = new OwnerFixture())
            {
                var c = new Catalog(f.Idle, f.Motion); var asset = c.Resolve(f.Zone, f.Actor);
                var frames = new[] { asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.East, 0), asset.GetFrame(EntityVisualState.Walk, EntityVisualFacing.East, 0), asset.GetFrame(EntityVisualState.Walk, EntityVisualFacing.East, 1), asset.GetFrame(EntityVisualState.Attack, EntityVisualFacing.East, 0), asset.GetFrame(EntityVisualState.Hurt, EntityVisualFacing.East, 0), asset.GetFrame(EntityVisualState.Cast, EntityVisualFacing.East, 0) };
                Assert.AreEqual(6, frames.Distinct().Count()); c.Dispose(); c.Dispose();
                Assert.True(frames.All(s => s == null)); Assert.True(f.Idle != null && f.Motion != null); Assert.False(c.Try(f.Zone, f.Actor, out _));
            }
        }

        [Test] public void ActualCellObserverUsesOneNativeBodyAndKeepsNeutralReaderAndCoveredBed()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); f.Idle(); Assert.IsNull(f.Foreground(8, 9)); Assert.NotNull(f.Background(8, 9));
                Assert.False(f.Shadow.enabled); Assert.AreEqual(Vector3.zero, f.Body.transform.localPosition); Assert.AreEqual(Color.white, f.Body.color);
                using (var catalog = new FlatPixelGreyboxCatalog())
                { Assert.True(catalog.TryGetPresentationSubjectSprite(f.Zone, f.Actor, 8, 9, out var portrait, out var tint)); Assert.AreSame(f.IdleTexture, portrait.texture); Assert.AreEqual(new Rect(0, 0, 16, 24), portrait.rect); Assert.AreEqual(Color.white, tint); }
                var bed = f.Owner.Content.Create("Bed"); Assert.True(f.Zone.AddEntity(bed, 8, 9));
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0)); f.Frame(); f.Tick(.035f); f.Pose(0);
                f.Tick(.025f); f.Pose(1); f.Tick(.041f); f.Idle(); Assert.AreEqual(9.5f, f.View.position.x);
                var restored = f.Foreground(8, 9); Assert.NotNull(restored); Assert.AreSame(Resources.Load<Texture2D>("Sprites/Live16x24/east-kitchen"), restored.sprite.texture);
                var enemy = f.Target(10, 9); Assert.True(CombatSystem.PerformMeleeAttack(f.Actor, enemy, f.Zone, new LowRandom())); f.Pose(2);
                CombatSystem.ApplyDamage(f.Actor, 1, enemy, f.Zone); f.Tick(.061f); f.Pose(3); f.Tick(.15f); f.Idle();
            }
        }

        [Test] public void MissingMotionRetainsApprovedStaticBodyRatherThanDroppingTheOwner()
        {
            using (var f = new Paint(false))
            { Assert.IsNull(f.Record); var tile = f.Foreground(8, 9); Assert.NotNull(tile); Assert.AreSame(f.IdleTexture, tile.sprite.texture); Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect); }
        }

        [TestCase(1f)] [TestCase(4f)]
        public void RealTacticCaptureIsConsumedOnceByExistingWorldFxWithoutReapplyingSimulation(float speed)
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var target = f.Target(); var moss = f.Owner.Moss; int hpBefore = target.GetStatValue("Hitpoints");
                SpellFxSettings.AnimationSpeed = speed; f.World.Update(0, true, true); int hooks = 0; float seconds = 0;
                EntityCastVisualHandler observed = (a, z, id, sx, sy, tx, ty, duration) => { if (a != f.Actor) return; hooks++; Assert.AreSame(f.Zone, z); Assert.AreEqual(Spell, id); Assert.AreEqual((8, 9), (sx, sy)); Assert.AreEqual((11, 9), (tx, ty)); seconds = duration; };
                EntityVisualHooks.CastCallback += observed;
                try
                {
                    Assert.True(f.Cast(target)); Assert.AreEqual(1, SpellFxBus.PendingCount); Assert.Zero(hooks);
                    int resolvedHP = target.GetStatValue("Hitpoints"); Assert.Less(resolvedHP, hpBefore); Assert.AreEqual(8, f.Owner.Ability.CooldownRemaining);
                    Assert.True(target.HasEffect<BurningEffect>()); Assert.AreSame(f.Actor, moss.GetPart<PhysicsPart>().InInventory);
                    f.World.Update(0, true, true); Assert.AreEqual(1, hooks); Assert.Zero(SpellFxBus.PendingCount); f.Pose(4);
                    Assert.That(seconds, Is.EqualTo(.24f / speed).Within(.00001f)); Assert.AreEqual(EntityVisualState.Cast, f.State);
                    Assert.True(f.World.HasBlockingFx); Assert.Greater(f.World.SpriteRenderer.ActiveCount, 0); Assert.AreEqual(0, f.World.NativeRenderer.ActiveCount);
                    f.Tick(seconds * .5f); f.Pose(4); f.Tick(seconds * .5f + .001f); f.Idle();
                    f.World.Update(4f, true, true); Assert.AreEqual(WorldFxPlaybackState.Completed, f.World.LastPlayback.State); Assert.False(f.World.HasBlockingFx);
                    Assert.AreEqual(1, hooks); Assert.AreEqual(resolvedHP, target.GetStatValue("Hitpoints")); Assert.AreEqual(8, f.Owner.Ability.CooldownRemaining);
                    Assert.AreSame(moss, f.Actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "FireMoss")); Assert.AreSame(f.Actor, moss.GetPart<PhysicsPart>().InInventory);
                }
                finally { EntityVisualHooks.CastCallback -= observed; }
            }
        }

        [Test] public void RealWallAndCooldownRefusalsDoNotCreateASecondSequenceOrPose()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var target = f.Target(); var wall = new Entity { BlueprintName = "Wall" }; wall.AddPart(new PhysicsPart { Solid = true }); wall.SetTag("Solid");
                Assert.True(f.Zone.AddEntity(wall, 9, 9)); Assert.False(f.Cast(target)); Assert.Zero(SpellFxBus.PendingCount); Assert.Zero(f.Owner.Ability.CooldownRemaining); f.Idle();
                Assert.True(f.Zone.RemoveEntity(wall)); Assert.True(f.Cast(target)); Assert.AreEqual(1, SpellFxBus.PendingCount); f.World.Update(0, true, true); f.Pose(4);
                f.Tick(.08f); float elapsed = f.Time; Assert.False(f.Cast(target)); Assert.Zero(SpellFxBus.PendingCount); f.World.Update(0, true, true);
                Assert.AreEqual(elapsed, f.Time); f.Tick(.17f); f.Idle();
            }
        }

        [TestCase("Off")] [TestCase("wrong-zone")] [TestCase("hidden")]
        public void CoordinatorRejectedRealSequenceDoesNotCreateACastingPose(string fault)
        {
            using (var f = new Paint())
            {
                f.RequireBody(); Assert.True(f.Cast(f.Target())); var sequence = SpellFxBus.Drain().Single(); int hooks = 0;
                EntityCastVisualHandler count = (a, z, id, sx, sy, tx, ty, d) => { if (a == f.Actor) hooks++; }; EntityVisualHooks.CastCallback += count;
                try
                {
                    if (fault == "Off") SpellFxSettings.Mode = SpellFxMode.Off;
                    if (fault == "wrong-zone") f.World.SetZone(new Zone("wrong"));
                    if (fault == "hidden") foreach (var cell in f.Zone.Cells) cell.IsVisible = false;
                    var refused = f.World.Play(sequence); Assert.AreEqual(WorldFxPlaybackState.Cancelled, refused.State); Assert.Zero(hooks);
                    Assert.AreNotEqual(EntityVisualState.Cast, f.State);
                    SpellFxSettings.Mode = SpellFxMode.Full; foreach (var cell in f.Zone.Cells) cell.IsVisible = true;
                    f.World.SetZone(f.Zone); f.World.Update(0, true, true);
                    var accepted = f.World.Play(sequence); Assert.AreEqual(WorldFxPlaybackState.Playing, accepted.State); Assert.AreEqual(1, hooks); f.Pose(4);
                }
                finally { EntityVisualHooks.CastCallback -= count; }
            }
        }

        [Test] public void HiddenSourceWithVisibleContactKeepsAcceptedFxWithoutDisclosingActorBody()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var target = f.Target(); Assert.True(f.Cast(target)); var sequence = SpellFxBus.Drain().Single();
                f.Zone.GetCell(8, 9).IsVisible = false; int hooks = 0; EntityCastVisualHandler count = (a, z, id, sx, sy, tx, ty, d) => { if (a == f.Actor) hooks++; };
                EntityVisualHooks.CastCallback += count;
                try
                {
                    Assert.True(f.Zone.GetEntityCell(target).IsVisible); var playback = f.World.Play(sequence);
                    Assert.AreEqual(WorldFxPlaybackState.Playing, playback.State); Assert.AreEqual(1, hooks); Assert.IsNull(f.Record);
                    f.World.Update(0, true, true); Assert.Greater(f.World.SpriteRenderer.ActiveCount, 0);
                    f.Zone.GetCell(8, 9).IsVisible = true; f.Renderer.MarkCellDirty(8, 9, "revealed"); f.Frame(); f.RequireBody(); f.Idle();
                }
                finally { EntityVisualHooks.CastCallback -= count; }
            }
        }

        [Test] public void RealCaptureThenDisplacementCannotWarpOrStartAStaleCast()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var target = f.Target(); Assert.True(f.Cast(target)); var sequence = SpellFxBus.Drain().Single();
                Assert.AreEqual(8, sequence.Source.X); Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 0, 1)); f.Tick(.025f);
                var position = f.View.position; var state = f.State; float elapsed = f.Time; var sprite = f.Body.sprite;
                var playback = f.World.Play(sequence); Assert.AreEqual(WorldFxPlaybackState.Playing, playback.State);
                Assert.AreEqual((8, 10), f.Zone.GetEntityPosition(f.Actor)); Assert.AreEqual(position, f.View.position); Assert.AreEqual(state, f.State); Assert.AreEqual(elapsed, f.Time); Assert.AreSame(sprite, f.Body.sprite);
                f.Tick(.08f); f.Idle();
            }
        }

        [Test] public void CorrectlyCapturedCastInterruptsInterpolationAtTheCurrentCell()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, -1, 0)); f.Tick(.025f); Assert.True(f.Body.flipX);
                var target = f.Target(4, 9); Assert.True(f.Cast(target)); f.World.Update(0, true, true); f.Pose(4);
                Assert.AreEqual((7, 9), f.Zone.GetEntityPosition(f.Actor)); Assert.AreEqual(7.5f, f.View.position.x); Assert.True(f.Body.flipX);
                Assert.False(Get<bool>(f.Record, "HasPendingReaction")); f.Tick(.25f); f.Idle();
            }
        }

        [TestCase("hurt")] [TestCase("melee")]
        public void CastAndImmediateRealReactionHaveBoundedVisibleOrder(string reaction)
        {
            using (var f = new Paint())
            {
                f.RequireBody(); var target = f.Target(9, 9); Assert.True(f.Cast(target)); f.World.Update(0, true, true); f.Pose(4); f.Tick(.02f);
                if (reaction == "hurt") CombatSystem.ApplyDamage(f.Actor, 1, target, f.Zone);
                else Assert.True(CombatSystem.PerformMeleeAttack(f.Actor, target, f.Zone, new LowRandom()));
                f.Pose(4); Assert.True(Get<bool>(f.Record, "HasPendingReaction")); f.Tick(.041f); f.Pose(reaction == "hurt" ? 3 : 2);
                f.Tick(.2f); f.Idle(); Assert.False(Get<bool>(f.Record, "HasPendingReaction"));
            }
        }

        [Test] public void DuplicateCastHookCannotExtendItsWindowAndUnrelatedSpellStaysIdle()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); Assert.True(f.Cast(f.Target())); f.World.Update(0, true, true); f.Tick(.10f); f.Pose(4);
                for (int i = 0; i < 10; i++) EntityVisualHooks.EmitCast(f.Actor, f.Zone, Spell, 8, 9, 11, 9, .24f);
                Assert.That(f.Time, Is.EqualTo(.10f).Within(.00001f)); f.Tick(.141f); f.Idle();
                EntityVisualHooks.EmitCast(f.Actor, f.Zone, "Pyromancy_FlameJet", 8, 9, 11, 9, .24f); f.Idle(); f.Tick(.1f); f.Idle();
            }
        }

        [TestCase("fog")] [TestCase("same-id-replacement")] [TestCase("hide")] [TestCase("zone")]
        public void CastingViewRetiresOnActualAuthorityOrPresentationLoss(string fault)
        {
            using (var f = new Paint())
            {
                f.RequireBody(); Assert.True(f.Cast(f.Target())); f.World.Update(0, true, true); f.Pose(4);
                if (fault == "fog") f.Zone.GetCell(8, 9).IsVisible = false;
                if (fault == "same-id-replacement") { Assert.True(f.Zone.RemoveEntity(f.Actor)); var other = f.Owner.Content.Create("MarlbackCindercaller"); other.ID = f.Actor.ID; Assert.True(f.Zone.AddEntity(other, 8, 9)); }
                if (fault == "hide") f.Actors.SetPresentationVisible(false);
                if (fault == "zone") f.Actors.SetZone(new Zone("other"));
                f.Tick(.01f); Assert.IsNull(f.Record); Assert.False(f.BodyBeforeRemoval.gameObject.activeInHierarchy); Assert.IsNull(f.BodyBeforeRemoval.sprite);
            }
        }

        [Test] public void LethalDamageDuringCastUsesExistingDeathSnapshotAndDoesNotReplayCasting()
        {
            using (var f = new Paint())
            {
                f.RequireBody(); Assert.True(f.Cast(f.Target())); f.World.Update(0, true, true); f.Pose(4);
                CombatSystem.ApplyDamage(f.Actor, 100, null, f.Zone); Assert.IsNull(f.Zone.GetEntityCell(f.Actor));
                Assert.True(Get<bool>(f.Record, "Dying")); f.Pose(3); f.Tick(.29f); Assert.IsNull(f.Record);
            }
        }

        static EntityVisualDefinition Definition() => new EntityVisualDefinition { ID = "test-compact-caster", IdleFrames = 1, WalkFrames = 2, AttackFrames = 2, HurtFrames = 2, CastFrames = 2 };
        static void Frame(Sprite sprite, Texture2D texture, int index)
        { Assert.NotNull(sprite); Assert.AreSame(texture, sprite.texture); Assert.AreEqual(new Rect(index * 16, 0, 16, 24), sprite.rect); Assert.AreEqual(new Vector2(8, 0), sprite.pivot); Assert.AreEqual(16, sprite.pixelsPerUnit); }
        static T Get<T>(object value, string name) => (T)value.GetType().GetField(name, I).GetValue(value);
        static void Call(object value, string name) { try { value.GetType().GetMethod(name, I).Invoke(value, null); } catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; } }
        static Entity Cover() { var e = new Entity { BlueprintName = "cover" }; e.AddPart(new RenderPart { RenderString = "?", RenderLayer = 99 }); e.AddPart(new PhysicsPart()); return e; }
        sealed class LowRandom : System.Random { public override int Next(int maxValue) => 0; public override int Next(int minValue, int maxValue) => minValue; }

        sealed class Catalog : IDisposable
        {
            public readonly object Value; readonly AssetQuery query;
            public Catalog(Texture2D idle, Texture2D motion)
            {
                var type = typeof(AnimatedEntityRenderer).Assembly.GetType(CatalogName); Assert.NotNull(type, "Optional local Cindercaller adapter is missing.");
                var ctor = type.GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D) }); Assert.NotNull(ctor);
                Value = ctor.Invoke(new object[] { idle, motion }); var method = type.GetMethod("TryGetAsset", new[] { typeof(Zone), typeof(Entity), typeof(EntityVisualAsset).MakeByRefType() }); Assert.NotNull(method);
                query = (AssetQuery)Delegate.CreateDelegate(typeof(AssetQuery), Value, method);
            }
            public bool Try(Zone zone, Entity owner, out EntityVisualAsset asset) => query(zone, owner, out asset);
            public EntityVisualAsset Resolve(Zone zone, Entity owner) { Assert.True(Try(zone, owner, out var asset)); Assert.NotNull(asset); return asset; }
            public void Dispose() => ((IDisposable)Value).Dispose();
        }

        sealed class OwnerFixture : IDisposable
        {
            public readonly EntityEquipmentContentFixture Content;
            public readonly Zone Zone; public readonly OverworldZoneManager Manager; public readonly Entity Actor, Moss;
            public readonly Texture2D Idle, Motion;
            public ActivatedAbility Ability => Actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Single(a => a.Command == "CommandEmberSpit");
            public OwnerFixture(string zoneID = "Overworld.12.10.0")
            {
                try
                {
                    Content = new EntityEquipmentContentFixture(); Zone = new Zone(zoneID); Manager = OverworldZoneManager.CreateDetached(Content.Factory, 64, true); Manager.SetActiveZone(Zone);
                    Actor = Content.Create("MarlbackCindercaller"); Assert.NotNull(Actor); Assert.True(Zone.AddEntity(Actor, 8, 9)); Actor.GetPart<BrainPart>().CurrentZone = Zone;
                    Moss = Actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "FireMoss"); Assert.AreEqual(0, Ability.CooldownRemaining);
                    foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true;
                    Idle = new Texture2D(16, 24) { name = "borrowed-caster-idle" }; Motion = new Texture2D(80, 24) { name = "borrowed-caster-motion" };
                }
                catch { Dispose(); throw; }
            }
            public void Dispose() { if (Idle != null) Object.DestroyImmediate(Idle); if (Motion != null) Object.DestroyImmediate(Motion); Content?.Dispose(); }
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
            public readonly Texture2D IdleTexture; public SpriteRenderer BodyBeforeRemoval;
            public Zone Zone => Owner.Zone; public Entity Actor => Owner.Actor; public WorldFxCoordinator World => Renderer.WorldFx;
            public object Record => ((IDictionary)typeof(AnimatedEntityRenderer).GetField("_views", I).GetValue(Actors))[Actor];
            public Transform View => Get<Transform>(Record, "RootTransform"); public SpriteRenderer Body => Get<SpriteRenderer>(Record, "Body"); public SpriteRenderer Shadow => Get<SpriteRenderer>(Record, "Shadow");
            public EntityVisualState State => Get<EntityVisualState>(Record, "State"); public float Time => Get<float>(Record, "StateTime");
            public Paint(bool motion = true)
            {
                var hooks = typeof(EntityVisualHooks).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));
                var cell = ZoneRenderHooks.CellDirtyCallback; var full = ZoneRenderHooks.FullDirtyCallback; float volume = SpellFxSettings.SoundVolume;
                var bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic); var oldBound = bound.GetValue(null);
                Zone previous = null; Action<int, int> oldChanged = null; Action oldSight = null;
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                restore = () => { foreach (var h in hooks) h.Key.SetValue(null, h.Value); ZoneRenderHooks.CellDirtyCallback = cell; ZoneRenderHooks.FullDirtyCallback = full; SpellFxSettings.SoundVolume = volume; bound.SetValue(null, oldBound); if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; } };
                try
                {
                    Owner = new OwnerFixture(); EntityVisualHooks.Reset(); graph = new NativeGraph(); graph.SetupCameraCleanup();
                    Renderer = graph.RendererValue; Actors = graph.Actors; overlay = graph.Overlay; bg = graph.Background;
                    IdleTexture = Resources.Load<Texture2D>(IdlePath); Assert.NotNull(IdleTexture, "Existing approved idle must be imported.");
                    // Old runtime takes the real static path; the integration tests then fail on no caster view.
                    var type = typeof(AnimatedEntityRenderer).Assembly.GetType(CatalogName);
                    if (type != null)
                    {
                        injected = Activator.CreateInstance(type, new object[] { IdleTexture, motion ? Owner.Motion : null });
                        var field = typeof(AnimatedEntityRenderer).GetField("_flatCindercallerMotionCatalog", I); Assert.NotNull(field);
                        if (field.GetValue(Actors) is IDisposable previousCatalog) previousCatalog.Dispose();
                        field.SetValue(Actors, injected);
                    }
                    typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Renderer, true); Renderer.SetZone(Zone);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                    SpellFxSettings.SoundVolume = 0; Frame(); World.Update(0, true, true);
                }
                catch { Dispose(); throw; }
            }
            public void RequireBody() { Assert.NotNull(Record, "Actual cell observer must take over the exact local Cindercaller body."); Assert.AreEqual(1, Actors.ActiveViewCount); BodyBeforeRemoval = Body; }
            public Entity Target(int x = 11, int y = 9)
            {
                var target = Owner.Content.Create("Magpie"); Assert.NotNull(target); Assert.True(Zone.AddEntity(target, x, y));
                Actor.GetPart<BrainPart>().SetPersonallyHostile(target, false); Assert.True(FactionManager.IsHostile(Actor, target)); return target;
            }
            public bool Cast(Entity target) => Actor.GetPart<CombatTacticsPart>().TryUseAbility(target, Zone, new LowRandom());
            public void Frame() => Call(Renderer, "LateUpdate"); public void Tick(float dt) => Actors.AdvancePresentation(dt);
            public void Pose(int index) => FlatPixelCindercallerMotionTests.Frame(Body.sprite, Owner.Motion, index);
            public void Idle() { Assert.AreEqual(EntityVisualState.Idle, State); FlatPixelCindercallerMotionTests.Frame(Body.sprite, IdleTexture, 0); }
            public Tile Foreground(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public TileBase Background(int x, int y) => bg.GetTile(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public void Dispose() { try { graph?.CleanupCameraFixture(); if (injected is IDisposable catalog) catalog.Dispose(); } finally { Owner?.Dispose(); restore?.Invoke(); } }
        }
    }
}
