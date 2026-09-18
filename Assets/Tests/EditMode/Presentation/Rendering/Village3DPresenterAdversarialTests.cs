using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Hook coexistence, interruption and ownership hypotheses beyond the
    /// first 40 integration cases. Actual imported library is required. EditMode
    /// observes callback membership and queued view state, not animation smoothness
    /// or GPU output. No unsupported NonParallelizable attribute.</summary>
    public sealed class Village3DPresenterAdversarialTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static Delegate[] Hooks()=>new Delegate[]{EntityVisualHooks.MovedCallback,EntityVisualHooks.AttackCallback,
            EntityVisualHooks.CastCallback,EntityVisualHooks.DamageCallback,EntityVisualHooks.DeathCallback};
        static bool HasTarget(Delegate hook,object target)=>hook!=null&&hook.GetInvocationList().Any(d=>ReferenceEquals(d.Target,target));
        static object ViewState(Village3DIntegrationFixture f,string id)
            =>((IDictionary)typeof(Village3DPresenter).GetField("byId",Private).GetValue(f.Presenter))[id];
        static float Number(object state,string name)=>(float)state.GetType().GetField(name).GetValue(state);
        static void AssertHooksContain(object target)
        {foreach(var hook in Hooks())Assert.IsTrue(HasTarget(hook,target),"Expected subscriber in every visual hook.");}
        static void QueueAttack(Village3DIntegrationFixture f,string id)
        {
            var actor=f.View(id).owner;
            EntityVisualHooks.EmitAttack(actor,f.Player,f.Zone);
            Assert.Greater(Number(ViewState(f,id),"ActionUntil"),Time.unscaledTime,"Live hook must actually queue the action before interruption.");
        }

        [Test] public void LegacyRendererInitializedLaterDoesNotEraseVillageSubscriptions()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                AssertHooksContain(f.Presenter);
                var go=new GameObject("owned late legacy renderer");go.transform.SetParent(f.Root.transform);
                var legacy=go.AddComponent<AnimatedEntityRenderer>();legacy.Init((e,x,y)=>Color.white);
                AssertHooksContain(legacy);AssertHooksContain(f.Presenter);
            }
        }
        [Test] public void LegacyRendererDestroyedFirstRemovesItselfFromMulticastHooks()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var go=new GameObject("owned early legacy renderer");go.transform.SetParent(f.Root.transform);
                var legacy=go.AddComponent<AnimatedEntityRenderer>();legacy.Init((e,x,y)=>Color.white);
                // Rebind so this test pins the ordinary legacy-first subscription order,
                // independently of the separate late-initialization overwrite hypothesis.
                f.Presenter.Bind(new Zone("village3d-hook-order-control"),f.Source);
                f.Presenter.Bind(f.Zone,f.Source);f.Refresh();
                AssertHooksContain(legacy);AssertHooksContain(f.Presenter);
                Object.DestroyImmediate(go);
                foreach(var hook in Hooks())Assert.IsFalse(HasTarget(hook,legacy),"Destroyed legacy renderer retained by a multicast hook.");
                AssertHooksContain(f.Presenter);
            }
        }
        [Test] public void PresenterDestructionRemovesOnlyItsOwnFiveHookSubscriptions()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                // Reuse an ordinary legacy renderer as the live subscriber control.
                var go=new GameObject("owned retained legacy renderer");go.transform.SetParent(f.Root.transform);
                var legacy=go.AddComponent<AnimatedEntityRenderer>();legacy.Init((e,x,y)=>Color.white);
                f.Presenter.Bind(new Zone("village3d-unsubscribe-control"),f.Source);
                f.Presenter.Bind(f.Zone,f.Source);f.Refresh();AssertHooksContain(legacy);AssertHooksContain(f.Presenter);
                var presenter=f.Presenter;Object.DestroyImmediate(presenter);
                foreach(var hook in Hooks())Assert.IsFalse(HasTarget(hook,presenter));
                AssertHooksContain(legacy);
            }
        }
        [Test] public void HidingPresentationDiscardsQueuedActionBeforeShowingAgain()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                const string id="north-guard-west";QueueAttack(f,id);
                f.Presenter.SetPresentationVisible(false);
                Assert.AreEqual(0,Number(ViewState(f,id),"ActionUntil"),"Hidden mode must discard a pending attack, not resume it on return.");
                Assert.AreEqual(0,Number(ViewState(f,id),"MoveDuration"));
                f.Presenter.SetPresentationVisible(true);f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(f.View(id).owner));
            }
        }
        [Test] public void ActualForcedMovementSnapsAndDiscardsQueuedAction()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                const string id="north-guard-west";var view=f.View(id);QueueAttack(f,id);
                Assert.IsTrue(MovementSystem.ForceMoveTo(view.owner,f.Zone,40,23));
                Assert.AreEqual((40,23),f.Zone.GetEntityPosition(view.owner));
                Assert.Less(Vector3.Distance(Village3DProjection.CellCentre(40,23),view.root.transform.position),.001f);
                Assert.AreEqual(0,Number(ViewState(f,id),"MoveDuration"));
                Assert.AreEqual(0,Number(ViewState(f,id),"ActionUntil"),"Knockback/teleport interrupts the previous action pose.");
            }
        }
        [Test] public void StationaryCombatAndCastingFaceTheNativeResolvedDirection()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var view=f.View("north-guard-west");var render=view.owner.GetPart<RenderPart>();Assert.NotNull(render);
                // CombatSystem.FaceToward writes VisualFacing before EmitAttack.
                render.VisualFacing=EntityVisualFacing.East;
                EntityVisualHooks.EmitAttack(view.owner,f.Player,f.Zone);
                bool attackFacesNative=Vector3.Dot(view.root.transform.forward,Vector3.right)>.99f;
                // EmitCast itself writes the captured native aim direction.
                EntityVisualHooks.EmitCast(view.owner,f.Zone,"draft-facing-probe",37,3,37,4);
                Assert.AreEqual(EntityVisualFacing.South,render.VisualFacing);
                bool castFacesNative=Vector3.Dot(view.root.transform.forward,Vector3.back)>.99f;
                Assert.IsTrue(attackFacesNative,"Stationary attack ignored native VisualFacing.");
                Assert.IsTrue(castFacesNative,"Stationary cast ignored captured native aim.");
            }
        }
        [Test] public void OwnerWaterSlotKeepsWaterShaderAfterAllOwnerPreparation()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var library=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);Assert.NotNull(library);
                Assert.AreNotSame(library.WorldMaterial.shader,library.WaterMaterial.shader);
                var prefab=library.FindModel("central-well");Assert.NotNull(prefab);
                Assert.IsTrue(prefab.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==library.WaterMaterial)),
                    "Actual cistern prefab must contain a real authored water slot for this control.");
                var view=f.View("central-cistern");
                Assert.IsTrue(view.root.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m!=null&&m.shader==library.WaterMaterial.shader)),
                    "Second owner preparation must not remap an already-cloned water slot to the palette shader.");
            }
        }
        [Test] public void DistinctPresentersOwnDistinctFogMaterialsAndDisposalIsLocal()
        {
            using(var first=new Village3DIntegrationFixture())
            {
                var material=first.View("north-guard-west").root.GetComponentInChildren<Renderer>(true).sharedMaterial;
                Assert.IsTrue(material.HasProperty("_FogLight"));var texture=material.GetTexture("_FogLight");Assert.NotNull(texture);
                using(var second=new Village3DIntegrationFixture())
                {
                    var other=second.View("north-guard-west").root.GetComponentInChildren<Renderer>(true).sharedMaterial;
                    Assert.AreNotSame(material,other);Assert.AreNotSame(texture,other.GetTexture("_FogLight"));
                    var cell=second.Zone.GetEntityCell(second.View("north-guard-west").owner);cell.IsVisible=false;second.Refresh();
                    Assert.IsTrue(first.Presenter.IsRenderedEntity(first.View("north-guard-west").owner));
                }
                Assert.IsTrue(material!=null);Assert.IsTrue(texture!=null);first.Refresh();
                Assert.AreSame(texture,material.GetTexture("_FogLight"));
            }
        }

        // A — reload/rollback boundaries. Failed art must leave the native world and
        // borrowed resources intact; a cached prior success is not valid new art.
        [Test] public void Adversarial_InvalidatedManifestCannotReuseEarlierValidDefinition()
        {
            var source = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            Assert.NotNull(source); var clone = Object.Instantiate(source); TextAsset malformed = null;
            try
            {
                clone.InvalidateCaches(); clone.Validate(); var before = clone.Definition;
                var changed = JsonUtility.FromJson<Village3DManifest>(source.Manifest.text);
                changed.owners[0].visibleWhen = "always";
                malformed = new TextAsset(JsonUtility.ToJson(changed)); clone.Manifest = malformed; clone.InvalidateCaches();
                Assert.Throws<ArgumentException>(() => clone.Validate(), "Reimport must not reuse a valid cached manifest over invalid new visibility.");
                Assert.Throws<ArgumentException>(() => clone.FindModel(source.PlayerModelId));
                clone.Manifest = source.Manifest; clone.InvalidateCaches(); clone.Validate();
                Assert.AreNotSame(before, clone.Definition); Assert.NotNull(clone.FindModel(source.PlayerModelId));
                Assert.AreEqual(MorrowfastSceneDefinition.Load().owners.Length, clone.Definition.owners.Length);
                Assert.AreSame(source.Manifest, clone.Manifest);
            }
            finally { Object.DestroyImmediate(clone); if (malformed != null) Object.DestroyImmediate(malformed); }
        }

        [Test] public void Adversarial_PartialOwnerBindFailureReleasesOnlyOwnedResourcesAndCanRecover()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                var originalModels = library.Models;
                var indexField = typeof(Village3DLibrary).GetField("index", Private);
                var definitionField = typeof(Village3DLibrary).GetField("definition", Private);
                object originalIndex = indexField.GetValue(library), originalDefinition = definitionField.GetValue(library);
                var empty = new GameObject("owned intentionally empty cistern prefab");
                var members = f.Zone.GetReadOnlyEntities().ToArray();
                var world = f.Presenter.WorldCamera; var target = world.targetTexture;
                try
                {
                    // Replace one binding in memory only; no prefab/asset is changed on disk.
                    library.Models = originalModels.Select(m => new Village3DLibrary.ModelBinding
                        { Id = m.Id, Prefab = m.Id == "central-well" ? empty : m.Prefab }).ToArray();
                    Assert.IsTrue(library.Models.Any(m => m.Prefab == empty)); library.InvalidateCaches();
                    f.Presenter.Bind(new Zone("village3d-failure-transition"), f.Source);
                    LogAssert.Expect(LogType.Warning, "[Village3D] Original presentation retained: Empty owner model central-cistern");
                    f.Presenter.Bind(f.Zone, f.Source);
                    Assert.IsFalse(f.Presenter.IsReady); Assert.IsFalse(f.Presenter.PresentationVisible);
                    StringAssert.Contains("Empty owner model central-cistern", f.Presenter.Failure);
                    Assert.IsFalse(f.Presenter.ClaimsCell(40, 12)); Assert.IsNull(f.Presenter.WorldCamera);
                    Assert.IsTrue(world == null); Assert.IsTrue(target == null || !target.IsCreated());
                    Assert.IsFalse(f.Root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Village 3D native world" || t.name == "Village 3D map composite"));
                    foreach (var hook in Hooks()) Assert.IsFalse(HasTarget(hook, f.Presenter));
                    CollectionAssert.AreEquivalent(members, f.Zone.GetReadOnlyEntities());
                    Assert.AreSame(f.BorrowedTarget, f.Source.targetTexture);
                    // A normal zone transition provides an explicit retry after resources recover.
                    library.Models = originalModels; library.InvalidateCaches();
                    f.Presenter.Bind(new Zone("village3d-recovery-transition"), f.Source);
                    f.Presenter.Bind(f.Zone, f.Source); f.Refresh();
                    Assert.IsTrue(f.Presenter.IsReady, f.Presenter.Failure); Assert.IsTrue(f.PickOwner("central-cistern", out _));
                    AssertHooksContain(f.Presenter);
                }
                finally
                {
                    library.Models = originalModels;
                    indexField.SetValue(library, originalIndex); definitionField.SetValue(library, originalDefinition);
                    Object.DestroyImmediate(empty);
                }
            }
        }

        // B — graph identity, multi-actor events and rapid mode transitions.
        [Test] public void Adversarial_RapidModesAcrossLoadedGraphKeepExactlyOneHookAndLiveOwner()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var go = new GameObject("owned coexisting legacy during reload"); go.transform.SetParent(f.Root.transform);
                var legacy = go.AddComponent<AnimatedEntityRenderer>(); legacy.Init((e, x, y) => Color.white);
                var old = f.View("north-guard-west"); var loaded = f.RoundTrip();
                f.Presenter.SetPresentationVisible(false); f.BindLoaded(loaded);
                var current = f.View("north-guard-west"); Assert.AreNotSame(old.owner, current.owner);
                Assert.AreEqual(old.owner.ID, current.owner.ID); Village3DIntegrationFixture.Hidden(old.root);
                for (int i = 0; i < 4; i++)
                {
                    Village3DSettings.Enabled = false; Village3DIntegrationFixture.TickFrame(f.Presenter);
                    Assert.IsFalse(f.Presenter.ClaimsCell(40, 12));
                    Village3DSettings.Enabled = true;
                    f.Presenter.Bind(new Zone("village3d-toggle-other-" + i), f.Source);
                    f.Presenter.Bind(f.Zone, f.Source); f.Presenter.SetPresentationVisible(true); f.Refresh();
                    Assert.AreSame(current.owner, f.View("north-guard-west").owner);
                    foreach (var hook in Hooks())
                    {
                        Assert.NotNull(hook);
                        Assert.AreEqual(1, hook.GetInvocationList().Count(d => ReferenceEquals(d.Target, f.Presenter)));
                        Assert.AreEqual(1, hook.GetInvocationList().Count(d => ReferenceEquals(d.Target, legacy)));
                    }
                    Assert.IsTrue(f.PickOwner("north-guard-west", out var point));
                    Assert.IsTrue(f.Presenter.TryPickWorld(point, out var hit, out _, out _)); Assert.AreSame(current.owner, hit);
                }
                Assert.IsFalse(f.Presenter.IsRenderedEntity(old.owner));
                Assert.AreSame(f.BorrowedTarget, f.Source.targetTexture);
            }
        }

        [Test] public void Adversarial_StalePreloadActorEventsCannotAnimateOrHideLoadedOwner()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var old = f.View("north-guard-west"); var loaded = f.RoundTrip(); f.BindLoaded(loaded);
                var current = f.View("north-guard-west"); Assert.AreEqual(old.owner.ID, current.owner.ID);
                Assert.AreNotSame(old.owner, current.owner); var position = current.root.transform.position; var rotation = current.root.transform.rotation;
                old.owner.GetPart<RenderPart>().VisualFacing = EntityVisualFacing.East;
                EntityVisualHooks.EmitAttack(old.owner, loaded.Player, f.Zone);
                EntityVisualHooks.EmitDamage(old.owner, loaded.Player, f.Zone, 1, false);
                EntityVisualHooks.EmitMoved(old.owner, f.Zone, 37, 3, 40, 23, true);
                EntityVisualHooks.EmitDeath(old.owner, loaded.Player, f.Zone, 37, 3);
                Assert.AreEqual(position, current.root.transform.position); Assert.AreEqual(rotation, current.root.transform.rotation);
                Assert.IsTrue(f.Presenter.IsRenderedEntity(current.owner)); Assert.IsTrue(Village3DIntegrationFixture.Drawn(current.root));
                current.owner.GetPart<RenderPart>().VisualFacing = EntityVisualFacing.West;
                EntityVisualHooks.EmitAttack(current.owner, loaded.Player, f.Zone);
                Assert.Greater(Vector3.Dot(current.root.transform.forward, Vector3.left), .99f, "Current owner remains a working callback control.");
            }
        }

        [Test] public void Adversarial_SameZoneIdForeignGraphEventsCannotMutateCurrentViews()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var view = f.View("north-guard-west"); var other = f.RoundTrip().ZoneManager.ActiveZone;
                Assert.AreEqual(f.Zone.ZoneID, other.ZoneID); Assert.AreNotSame(f.Zone, other);
                var position = view.root.transform.position; var rotation = view.root.transform.rotation;
                view.owner.GetPart<RenderPart>().VisualFacing = EntityVisualFacing.East;
                EntityVisualHooks.EmitAttack(view.owner, f.Player, other);
                EntityVisualHooks.EmitMoved(view.owner, other, 37, 3, 40, 23, true);
                EntityVisualHooks.EmitDeath(view.owner, f.Player, other, 37, 3);
                Assert.AreEqual(position, view.root.transform.position); Assert.AreEqual(rotation, view.root.transform.rotation);
                Assert.IsTrue(f.Presenter.IsRenderedEntity(view.owner));
                EntityVisualHooks.EmitAttack(view.owner, f.Player, f.Zone);
                Assert.Greater(Vector3.Dot(view.root.transform.forward, Vector3.right), .99f);
            }
        }

        [Test] public void Adversarial_RealDeathImmediatelyHidesOnlyTheKilledNativeGuard()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var west = f.View("north-guard-west"); var east = f.View("north-guard-east");
                Assert.AreNotSame(west.owner, east.owner);
                Assert.IsTrue(f.Presenter.IsRenderedEntity(west.owner)); Assert.IsTrue(f.Presenter.IsRenderedEntity(east.owner));
                int survivingHp = east.owner.GetStatValue("Hitpoints"); Assert.Greater(survivingHp, 0);
                int hp = west.owner.GetStatValue("Hitpoints"); Assert.Greater(hp, 0);
                CombatSystem.ApplyDamage(west.owner, hp + 1000, null, f.Zone);
                Assert.IsNull(f.Zone.GetEntityCell(west.owner), "Real lethal damage must have completed native removal.");
                Village3DIntegrationFixture.Hidden(west.root); // Before Refresh: verifies the death callback, not only a later census.
                Assert.IsFalse(f.Presenter.IsRenderedEntity(west.owner)); Assert.IsTrue(f.Presenter.IsRenderedEntity(east.owner));
                Assert.AreEqual(survivingHp, east.owner.GetStatValue("Hitpoints"));
                f.Refresh(); Assert.IsTrue(f.PickOwner("north-guard-east", out _));
                Assert.IsFalse(f.Presenter.IsRenderedEntity(west.owner));
            }
        }

        // C — memory texture upload, full-reveal exit, large-owner boundaries and real selection.
        static Texture2D ActualFog(Village3DIntegrationFixture f)
        {
            var material = f.View("central-cistern").root.GetComponentsInChildren<Renderer>(true)
                .SelectMany(r => r.sharedMaterials).First(m => m != null && m.HasProperty("_FogLight"));
            var texture = material.GetTexture("_FogLight") as Texture2D;
            Assert.NotNull(texture); Assert.AreEqual(80, texture.width); Assert.AreEqual(25, texture.height); return texture;
        }
        static void SetVisibility(Village3DIntegrationFixture f, bool explored, bool visible)
        { for (int y = 0; y < 25; y++) for (int x = 0; x < 80; x++) { var c = f.Zone.GetCell(x, y); c.Explored = explored; c.IsVisible = visible; } }

        [Test] public void Adversarial_UploadedFogPreservesRememberedAmbientAndCorrectRow()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var texture = ActualFog(f); var c = f.Zone.GetCell(2, 3); var mirrored = f.Zone.GetCell(2, 21);
                c.Explored = true; c.IsVisible = false; mirrored.Explored = mirrored.IsVisible = false;
                f.Zone.AmbientLevel = .06f; f.Refresh();
                Color dark = texture.GetPixel(2, 21); // Upload is (24 - nativeY), not nativeY.
                Assert.AreEqual(.5f, dark.a, .004f); Assert.AreEqual(.03f, dark.r, .004f);
                Assert.AreEqual(dark.r, dark.g, .0001f); Assert.AreEqual(dark.r, dark.b, .0001f);
                Assert.AreEqual(Color.clear, texture.GetPixel(2, 3), "Opposite row is truly unseen.");
                f.Zone.AmbientLevel = .8f; f.Refresh(); Color light = texture.GetPixel(2, 21);
                Assert.AreEqual(.2f, light.r, .004f); Assert.Greater(light.r, dark.r); Assert.AreEqual(.5f, light.a, .004f);
                c.IsVisible = true; f.Refresh(); Assert.AreEqual(1, texture.GetPixel(2, 21).a, .004f);
                Assert.IsFalse(mirrored.Explored); Assert.IsFalse(mirrored.IsVisible);
            }
        }

        [Test] public void Adversarial_ExitingFullRevealRehidesActorAndPickWithoutExploring()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var actor = f.View("north-guard-west"); Assert.IsTrue(f.PickOwner("north-guard-west", out var point));
                SetVisibility(f, false, false); f.Refresh(); Village3DIntegrationFixture.Hidden(actor.root);
                Assert.IsFalse(f.Presenter.TryPickWorld(point, out _, out _, out _));
                f.Presenter.FullReveal = true; f.Refresh();
                Assert.IsTrue(f.Presenter.IsRenderedEntity(actor.owner)); Assert.IsTrue(f.Presenter.TryPickWorld(point, out var hit, out _, out _)); Assert.AreSame(actor.owner, hit);
                f.Presenter.FullReveal = false; f.Refresh(); Village3DIntegrationFixture.Hidden(actor.root);
                Assert.IsFalse(f.Presenter.TryPickWorld(point, out _, out _, out _));
                for (int y = 0; y < 25; y++) for (int x = 0; x < 80; x++)
                { Assert.IsFalse(f.Zone.GetCell(x, y).Explored); Assert.IsFalse(f.Zone.GetCell(x, y).IsVisible); }
                Assert.AreEqual(0, ActualFog(f).GetPixel(37, 21).a);
            }
        }

        [Test] public void Adversarial_LargeOwnerVisibleEdgeCanBePickedWhenItsAnchorIsUnseen()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var well = f.View("central-cistern"); var anchor = f.Zone.GetEntityPosition(well.owner);
                Physics.SyncTransforms(); Vector2 point = default; int vx = -1, vy = -1; bool found = false;
                foreach (var r in well.root.GetComponentsInChildren<Renderer>(true))
                {
                    var b = r.bounds;
                    for (int ix = 0; ix < 9 && !found; ix++) for (int iz = 0; iz < 9 && !found; iz++)
                    {
                        var p = new Vector2(Mathf.Lerp(b.min.x, b.max.x, (ix + .5f) / 9), Mathf.Lerp(b.min.z, b.max.z, (iz + .5f) / 9));
                        if (!Village3DProjection.TryWorldToCell(new Vector3(p.x, 0, p.y), out int x, out int y) || (x, y) == anchor) continue;
                        if (f.Presenter.TryPickWorld(p, out var hit, out _, out _) && ReferenceEquals(hit, well.owner))
                        { found = true; point = p; vx = x; vy = y; }
                    }
                    if (found) break;
                }
                Assert.IsTrue(found, "The real multi-cell well must have a pickable area beyond its anchor.");
                SetVisibility(f, false, false); var edge = f.Zone.GetCell(vx, vy); edge.Explored = edge.IsVisible = true; f.Refresh();
                Assert.IsFalse(f.Zone.GetEntityCell(well.owner).Explored); Assert.IsTrue(Village3DIntegrationFixture.Drawn(well.root));
                Assert.IsTrue(f.Presenter.TryPickWorld(point, out var owner, out int ox, out int oy)); Assert.AreSame(well.owner, owner); Assert.AreEqual(anchor, (ox, oy));
                edge.IsVisible = false; f.Refresh();
                Assert.IsTrue(Village3DIntegrationFixture.Drawn(well.root), "Remembered static geometry remains submitted for the memory shader.");
                Assert.IsFalse(f.Presenter.TryPickWorld(point, out _, out _, out _), "Remembered geometry must not become selectable.");
                edge.Explored = false; f.Refresh(); Village3DIntegrationFixture.Hidden(well.root);
            }
        }

        [Test] public void Adversarial_InvalidWorldPickInputsCannotReuseAPreviousValidHit()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                Assert.IsTrue(f.PickOwner("north-guard-west", out var good));
                Assert.IsTrue(f.Presenter.TryPickWorld(good, out var valid, out _, out _)); Assert.NotNull(valid);
                var bad = new[] { new Vector2(float.NaN, 4), new Vector2(40, float.PositiveInfinity),
                    new Vector2(float.NegativeInfinity, 4), new Vector2(-.01f, 4), new Vector2(80, 4), new Vector2(40, -.01f), new Vector2(40, 125) };
                foreach (var point in bad)
                {
                    Assert.IsFalse(f.Presenter.TryPickWorld(point, out var owner, out int x, out int y), point.ToString());
                    Assert.IsNull(owner); Assert.AreEqual(-1, x); Assert.AreEqual(-1, y);
                }
                Assert.IsFalse(f.Presenter.TryGetOwnerView("", out var missing, out var root)); Assert.IsNull(missing); Assert.IsNull(root);
                Assert.IsTrue(f.Presenter.TryPickWorld(good, out var again, out _, out _)); Assert.AreSame(valid, again);
            }
        }

        [Test] public void Adversarial_RaisedNorthEdgeGeometryCanBePickedBeyondTheFlatGridOnlyWhileVisible()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                // At the tilted camera angle, raised in-bounds geometry projects past the ground's north edge.
                var point = new Vector2(40, 25);
                Assert.IsFalse(Village3DProjection.TryWorldToCell(new Vector3(point.x, 0, point.y), out _, out _));
                Assert.IsTrue(f.Presenter.TryPickWorld(point, out var owner, out int x, out int y));
                Assert.NotNull(owner);
                Assert.That(x, Is.InRange(0, 79)); Assert.That(y, Is.InRange(0, 24));
                Assert.AreSame(f.Zone.GetCell(x, y), f.Zone.GetEntityCell(owner));

                SetVisibility(f, false, false); f.Refresh();
                Assert.IsFalse(f.Presenter.TryPickWorld(point, out var hiddenOwner, out int hiddenX, out int hiddenY));
                Assert.IsNull(hiddenOwner); Assert.AreEqual(-1, hiddenX); Assert.AreEqual(-1, hiddenY);
            }
        }

        // D — native inventory/removal/save and movable room contents crossflows.
        static string CarriedUnits(Entity actor)
            => string.Join("|", actor.GetPart<InventoryPart>().Objects.GroupBy(e => e.BlueprintName).OrderBy(g => g.Key)
                .Select(g => g.Key + ":" + g.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1)));

        [Test] public void Adversarial_ActualLootThenClearSurvivesGraphLoadWithoutRestockOrLostUnits()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                var spec = MorrowfastSceneDefinition.Load().owners.First(s => s.mutable && s.kind == "container"
                    && MorrowfastSceneRuntime.FindOwner(f.Zone, s.id).GetPart<ContainerPart>()?.Contents.Count > 0);
                var view = f.View(spec.id); f.Approach(view.owner); var container = view.owner.GetPart<ContainerPart>();
                var contents = container.Contents.ToArray(); Assert.Greater(contents.Length, 0); Assert.IsFalse(container.Locked);
                f.Player.GetPart<InventoryPart>().MaxWeight = -1;
                Assert.IsFalse(view.owner.GetPart<MorrowfastPropPart>().TryRemove(f.Player, f.Zone), "Nonempty refusal is the initial control.");
                string before = CarriedUnits(f.Player);
                foreach (var item in contents) Assert.IsTrue(InventorySystem.TakeFromContainer(f.Player, view.owner, item), item.BlueprintName);
                Assert.IsEmpty(container.Contents); string acquired = CarriedUnits(f.Player); Assert.AreNotEqual(before, acquired);
                Assert.IsTrue(view.owner.GetPart<MorrowfastPropPart>().TryRemove(f.Player, f.Zone)); f.Refresh(); Village3DIntegrationFixture.Hidden(view.root);
                var loaded = f.RoundTrip(); f.BindLoaded(loaded); f.Presenter.SetPresentationVisible(false); f.Presenter.SetPresentationVisible(true); f.Refresh();
                Assert.IsTrue(MorrowfastSceneRuntime.GetState(f.Zone).WasRemoved(spec.id)); Assert.IsNull(MorrowfastSceneRuntime.FindOwner(f.Zone, spec.id));
                Assert.AreEqual(acquired, CarriedUnits(loaded.Player)); Assert.IsFalse(f.Presenter.IsRenderedEntity(view.owner));
                if (f.Presenter.TryGetOwnerView(spec.id, out var rebound, out var root)) { Assert.IsNull(rebound); Village3DIntegrationFixture.Hidden(root); }
                Assert.IsTrue(f.Presenter.IsReady, f.Presenter.Failure);
            }
        }

        [Test] public void Adversarial_NativeMovedStoolKeepsDisplacementAcrossCutawayAndGraphLoad()
        {
            using (var f = new Village3DIntegrationFixture())
            {
                const string id = "guest-stool-south";
                var room = MorrowfastSceneDefinition.Load().buildings.First(b => b.id == "dry-hem-guesthouse");
                var stool = f.View(id); Assert.IsTrue(f.Zone.MoveEntity(f.Player, 31, 12)); f.Refresh();
                var oldCell = f.Zone.GetEntityPosition(stool.owner); var oldPose = stool.root.transform.position;
                Assert.IsFalse(MorrowfastSceneRuntime.TryMoveFurniture(f.Player, f.Zone, id, 32, 11));
                Assert.AreEqual(oldCell, f.Zone.GetEntityPosition(stool.owner)); Assert.AreEqual(oldPose, stool.root.transform.position);
                Assert.IsTrue(MorrowfastSceneRuntime.TryMoveFurniture(f.Player, f.Zone, id, 30, 10));
                f.Inside(room); f.Refresh(); var movedCell = f.Zone.GetEntityPosition(stool.owner);
                var expected = oldPose + new Vector3(movedCell.x - oldCell.x, 0, oldCell.y - movedCell.y);
                Assert.Less(Vector3.Distance(expected, stool.root.transform.position), .001f); Assert.IsTrue(Village3DIntegrationFixture.Drawn(stool.root));
                f.Outside(); f.Refresh(); Village3DIntegrationFixture.Hidden(stool.root);
                f.Inside(room); f.Refresh(); Assert.IsTrue(Village3DIntegrationFixture.Drawn(stool.root));
                var loaded = f.RoundTrip(); f.BindLoaded(loaded); var next = f.View(id);
                Assert.AreNotSame(stool.owner, next.owner); Assert.AreEqual(stool.owner.ID, next.owner.ID);
                Assert.AreEqual(movedCell, f.Zone.GetEntityPosition(next.owner));
                Assert.Less(Vector3.Distance(expected, next.root.transform.position), .001f); Assert.IsTrue(Village3DIntegrationFixture.Drawn(next.root));
                Village3DIntegrationFixture.Hidden(stool.root);
            }
        }
    }
}
