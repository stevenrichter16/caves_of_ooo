using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual borrowed native surface plus a deliberately small authored-data
    /// fixture. Integration tests separately pin imported Blender assets and GPU pixels.</summary>
    internal sealed class NativeSpellFixture : IDisposable
    {
        public readonly GameObject Host;
        public readonly Camera Source;
        public readonly RenderTexture Target;
        public readonly NativeZone3DRenderSurface Surface;
        public readonly Zone Zone;
        public readonly NativeSpellFxLibrary Library;
        public readonly NativeSpellFxEntry Entry;
        public readonly Mesh Mesh;
        public NativeSpellFxRenderer Fx;
        readonly List<Object> owned = new List<Object>();

        public NativeSpellFixture(string spell = "Pyromancy_EmberSpit")
        {
            var native = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            Assert.NotNull(native, "The existing native surface is a real fixture prerequisite.");
            Host = new GameObject("Native spell contract fixture");
            var source = new GameObject("Borrowed camera"); source.transform.SetParent(Host.transform);
            Source = source.AddComponent<Camera>(); Source.enabled = false; Source.orthographic = true;
            Source.orthographicSize = 12.75f; Source.transform.position = new Vector3(40, 12.75f, -10);
            Target = Own(new RenderTexture(160, 90, 16)); Assert.IsTrue(Target.Create());
            Source.targetTexture = Target; Source.aspect = 160f / 90f;
            Surface = new NativeZone3DRenderSurface(Host.transform, native.Renderer, native.RendererIndex,
                native.CompositeMaterial, new[] { native.WorldMaterial, native.WaterMaterial }, 2.2f);
            Surface.Sync(Source, true, false);
            Zone = new Zone("NativeSpellContract");
            foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
            Surface.UpdateFog(Zone, null, true);
            Mesh = Own(new Mesh { name = "Borrowed authored test chisel" });
            Mesh.vertices = new[] { new Vector3(-.12f, 0, -.1f), new Vector3(.14f, 0, -.1f), new Vector3(0, .15f, .1f) };
            Mesh.triangles = new[] { 0, 2, 1 }; Mesh.RecalculateNormals(); Mesh.RecalculateBounds();
            Library = Own(ScriptableObject.CreateInstance<NativeSpellFxLibrary>());
            Library.Material = Own(new Material(native.WorldMaterial)); Library.Material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            Entry = new NativeSpellFxEntry { SpellId = spell, CastClip = Own(new AnimationClip()), CastDuration = .66f,
                SampleRate = 100, ReleaseFrame = 22, StudyContactFrame = 32, StudyClearFrame = 70,
                AuthoredDistanceCells = 4, LaunchDistance = .36f,
                Pieces = new[] { Piece("authored-contact", NativeSpellFxRole.TargetImpact, NativeSpellFxAnchor.Target) } };
            Library.Entries = new[] { Entry };
        }
        public T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        public NativeSpellFxPiece Piece(string id, NativeSpellFxRole role, NativeSpellFxAnchor anchor,
            NativeSpellFxCondition condition = NativeSpellFxCondition.Always, int forward = 0, int lateral = 0,
            bool essential = true)
        {
            var poses = new NativeSpellFxPose[111];
            for (int i = 0; i < poses.Length; i++) poses[i] = new NativeSpellFxPose
            { Position = new Vector3(0, .3f, 0), Rotation = Quaternion.identity,
                Scale = i >= 22 && i < 70 ? Vector3.one : Vector3.zero };
            return new NativeSpellFxPiece { Id = id, Mesh = Mesh, Color = Color.red, Role = role, Anchor = anchor,
                Condition = condition, ForwardCell = forward, LateralCell = lateral, Variant = forward,
                ReducedEssential = essential, Poses = poses };
        }
        public NativeSpellFxRenderer Create()
        { Fx = new NativeSpellFxRenderer(Library); Fx.SetZone(Zone); Fx.SetSurface(Surface); return Fx; }
        public SpellFxSequence Sequence(Point? source = null, Point[] path = null, Point[] cells = null,
            SpellFxTargetResult[] targets = null, SpellFxCellResult[] reactions = null, string spell = null,
            bool blocking = true) => new SpellFxSequence(spell ?? Entry.SpellId, Zone, null,
                source ?? new Point(10, 10), path ?? new[] { new Point(11, 10), new Point(12, 10), new Point(13, 10), new Point(14, 10) },
                cells ?? new[] { new Point(14, 10) }, targets ?? new[] { Hit("body", 14, 10) },
                blocksTurnAdvance: blocking, reactions: reactions);
        public static SpellFxTargetResult Hit(string id, int x, int y, bool died = false, bool resisted = false,
            string[] applied = null, string[] rejected = null) => new SpellFxTargetResult(id, new Point(x, y),
                new Point(x, y), damage: 2, died: died, resisted: resisted, appliedEffects: applied, rejectedEffects: rejected);
        public MeshRenderer[] Drawn() => Fx.Root == null ? Array.Empty<MeshRenderer>() :
            Fx.Root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        public void Dispose()
        {
            Fx?.Dispose(); Surface?.Dispose();
            if (Host != null) Object.DestroyImmediate(Host);
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null)
            { if (owned[i] is RenderTexture rt) rt.Release(); Object.DestroyImmediate(owned[i]); }
        }
    }

    public sealed class NativeSpellFxRendererTests
    {
        [SetUp] public void Setup()
        { SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1; SpellFxSettings.FlashIntensity = 0; AsciiFxBus.Clear(); SpellFxBus.Clear(); }
        [TearDown] public void Teardown()
        { SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1; SpellFxSettings.FlashIntensity = .15f; AsciiFxBus.Clear(); SpellFxBus.Clear(); }

        [Test] public void AuthoredMeshAndSampledPoseAppearAtCopiedContactWithoutColliderOrGameplayMutation()
        {
            using (var f = new NativeSpellFixture())
            {
                f.Create(); Assert.Greater(f.Fx.Play(f.Sequence()), 0); f.Fx.Update(.33f);
                var views = f.Drawn(); Assert.AreEqual(1, views.Length);
                Assert.AreSame(f.Mesh, views[0].GetComponent<MeshFilter>().sharedMesh);
                Assert.Less(Vector3.Distance(Village3DProjection.CellCentre(14, 10) + Vector3.up * .3f, views[0].transform.position), .001f);
                Assert.AreEqual(0, f.Fx.Root.GetComponentsInChildren<Collider>(true).Length);
                Assert.AreEqual(0, f.Zone.GetAllEntities().Count);
                Assert.AreEqual(0, SpellFxBus.PendingCount, "A backend must not emit or consume gameplay work.");
                Assert.Greater(f.Fx.ActiveCount, 0);
            }
        }
        [TestCase("Pyromancy_EmberSpit")] [TestCase("Pyromancy_FlamingHands")]
        [TestCase("Hydromancy_JetBlast")] [TestCase("Galvanism_GroundSurge")]
        [TestCase("Cryomancy_RimeGrip")] [TestCase("Spellcraft_Calm")]
        [TestCase("Hydromancy_ConjureRain")]
        public void ImportedLinearSwatchReachesShaderOnceWithoutASecondGammaConversion(string spell)
        {
            Assert.AreEqual(ColorSpace.Linear,QualitySettings.activeColorSpace,"The actual project uses Linear rendering.");
            var library=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);Assert.NotNull(library);
            var imported=library.Find(spell).Pieces.OrderByDescending(p=>((Vector4)p.Color-(Vector4)p.Color.linear).sqrMagnitude).First().Color;
            using(var f=new NativeSpellFixture())
            {
                f.Entry.Pieces[0].Color=imported;f.Create();f.Fx.Play(f.Sequence());f.Fx.Update(.33f);
                var observed=new MaterialPropertyBlock();f.Drawn().Single().GetPropertyBlock(observed);
                int id=Shader.PropertyToID("_BaseColor");
                var control=new MaterialPropertyBlock();control.SetVector(id,(Vector4)imported);
                Assert.Less(Vector4.Distance(control.GetVector(id),imported),.00001f,"Raw-vector reference preserves the library's linear swatch.");
                control.SetVector(id,(Vector4)imported.linear);
                Assert.Greater(Vector4.Distance(control.GetVector(id),imported),.05f,"Deliberate second conversion is a distinguishable counterexample.");
                Assert.Less(Vector4.Distance(observed.GetVector(id),imported),.00001f,
                    "Shader-facing MPB must contain the already-linear imported color, not its second .linear conversion.");
            }
        }
        [Test] public void UnknownSpellUsesExplicitFallbackInsteadOfBorrowingAnotherSpellsMesh()
        {
            using (var f = new NativeSpellFixture())
            { f.Create(); Assert.AreEqual(0, f.Fx.Play(f.Sequence(spell: "UnknownSpell"))); Assert.AreEqual(0, f.Fx.ActiveCount); }
        }
        [TestCase(false)] [TestCase(true)]
        public void HiddenOrUnexploredContactCannotDraw(bool unexplored)
        {
            using (var f = new NativeSpellFixture())
            {
                f.Create(); f.Fx.Play(f.Sequence()); f.Fx.Update(.33f); Assert.AreEqual(1, f.Drawn().Length);
                if (unexplored) f.Zone.GetCell(14, 10).Explored = false; else f.Zone.GetCell(14, 10).IsVisible = false;
                f.Fx.Update(0); Assert.AreEqual(0, f.Drawn().Length);
            }
        }
        [Test] public void FragmentMaterialsUseBorrowedFogWithTransientPolicyAndNoShadow()
        {
            using (var f = new NativeSpellFixture())
            {
                f.Create(); f.Fx.Play(f.Sequence()); f.Fx.Update(.33f); var r = f.Drawn().Single();
                Assert.AreEqual(NativeZone3DRenderSurface.WorldLayer, r.gameObject.layer);
                Assert.AreNotSame(f.Library.Material, r.sharedMaterial);
                Assert.AreSame(f.Surface.FogTexture, r.sharedMaterial.GetTexture("_FogLight"));
                Assert.AreEqual(1, r.sharedMaterial.GetFloat("_Transient"));
                Assert.AreEqual(ShadowCastingMode.Off, r.shadowCastingMode);
                Assert.IsFalse(r.receiveShadows);
            }
        }
        [Test] public void DuplicateOwnerResultsHaveOneImpactButDifferentOwnersInSameCellRetainTwo()
        {
            using (var f = new NativeSpellFixture())
            {
                f.Create(); var same = NativeSpellFixture.Hit("body", 14, 10);
                f.Fx.Play(f.Sequence(targets: new[] { same, NativeSpellFixture.Hit("body", 15, 10), NativeSpellFixture.Hit("other", 14, 10) }));
                f.Fx.Update(.33f); Assert.AreEqual(2, f.Drawn().Length);
                Assert.IsTrue(f.Drawn().All(r => Mathf.Abs(r.transform.position.x - 14.5f) < .001f));
            }
        }
        [TestCase("Cryomancy_RimeGrip", NativeSpellFxCondition.FrozenApplied, "FrozenEffect")]
        [TestCase("Spellcraft_Calm", NativeSpellFxCondition.PacifiedApplied, "Pacified")]
        public void SuccessClampsAndBindingsRequireActualAppliedStatusAndLivingTarget(string spell, NativeSpellFxCondition condition, string effect)
        {
            using (var f = new NativeSpellFixture(spell))
            {
                f.Entry.Pieces[0].Condition = condition; f.Create();
                f.Fx.Play(f.Sequence(targets: new[] { NativeSpellFixture.Hit("accepted", 14, 10, applied: new[] { effect }),
                    NativeSpellFixture.Hit("rejected", 15, 10, rejected: new[] { effect }),
                    NativeSpellFixture.Hit("dead", 16, 10, died: true, applied: new[] { effect }) }));
                f.Fx.Update(.33f); Assert.AreEqual(1, f.Drawn().Length);
                Assert.That(f.Drawn()[0].transform.position.x, Is.EqualTo(14.5f).Within(.001f));
            }
        }
        [Test] public void RainUsesActualWateredTargetsAndNeverFallbackSourceOrUnrelatedAffectedCell()
        {
            using (var f = new NativeSpellFixture("Hydromancy_ConjureRain"))
            {
                f.Entry.Pieces = new[] { f.Piece("actual-crop-rain", NativeSpellFxRole.CropCell, NativeSpellFxAnchor.Cell) }; f.Create();
                f.Fx.Play(f.Sequence(path: Array.Empty<Point>(), cells: new[] { new Point(10, 10), new Point(40, 20) },
                    targets: new[] { NativeSpellFixture.Hit("crop", 12, 9), NativeSpellFixture.Hit("crop", 12, 9) }));
                f.Fx.Update(.25f); Assert.AreEqual(1, f.Drawn().Length);
                Assert.That(f.Drawn()[0].transform.position.x, Is.EqualTo(12.5f).Within(.001f));
                f.Fx.ClearAll(); f.Fx.Play(f.Sequence(path: Array.Empty<Point>(), targets: Array.Empty<SpellFxTargetResult>(), cells: new[] { new Point(10, 10) }));
                f.Fx.Update(.25f); Assert.AreEqual(0, f.Drawn().Length);
            }
        }
        [Test] public void WaterOnlyRimeUsesRecordedFreezeReactionRatherThanEveryObservedPathCell()
        {
            using (var f = new NativeSpellFixture("Cryomancy_RimeGrip"))
            {
                f.Entry.Pieces = new[] { f.Piece("frozen-water", NativeSpellFxRole.ReactionCell, NativeSpellFxAnchor.Cell, NativeSpellFxCondition.FreezeWater) }; f.Create();
                f.Fx.Play(f.Sequence(targets: Array.Empty<SpellFxTargetResult>(), reactions: new[] {
                    new SpellFxCellResult(new Point(12,10), "reaction", "freeze_water", 1),
                    new SpellFxCellResult(new Point(13,10), "energy", "cold", 1),
                    new SpellFxCellResult(new Point(14,10), "reaction", "freeze_water", 0) }));
                f.Fx.Update(.25f); Assert.AreEqual(1, f.Drawn().Length);
                Assert.That(f.Drawn()[0].transform.position.x, Is.EqualTo(12.5f).Within(.001f));
            }
        }
        [Test] public void GroundCellPiecesUseOnlyRecordedUniquePathCells()
        {
            using (var f = new NativeSpellFixture("Galvanism_GroundSurge"))
            {
                f.Entry.Pieces = Enumerable.Range(1, 4).Select(i => f.Piece("stitch-"+i, NativeSpellFxRole.GroundCell, NativeSpellFxAnchor.Cell, forward:i)).ToArray(); f.Create();
                f.Fx.Play(f.Sequence(path: new[] {new Point(11,10),new Point(11,10),new Point(12,10)}, targets:Array.Empty<SpellFxTargetResult>()));
                f.Fx.Update(.3f); Assert.AreEqual(2, f.Drawn().Length);
                CollectionAssert.AreEquivalent(new[] {11.5f,12.5f}, f.Drawn().Select(r => r.transform.position.x).ToArray());
            }
        }
        [TestCase(1,0)] [TestCase(0,-1)] [TestCase(-1,1)]
        public void ConeOnlyUsesRecordedStencilCellsAndRespectsFacing(int dx,int dy)
        {
            using (var f = new NativeSpellFixture("Hydromancy_JetBlast"))
            {
                f.Entry.Pieces = new[] {f.Piece("near",NativeSpellFxRole.ConeCell,NativeSpellFxAnchor.Cell,forward:1),
                    f.Piece("far",NativeSpellFxRole.ConeCell,NativeSpellFxAnchor.Cell,forward:2),
                    f.Piece("left",NativeSpellFxRole.ConeCell,NativeSpellFxAnchor.Cell,forward:2,lateral:-1),
                    f.Piece("right",NativeSpellFxRole.ConeCell,NativeSpellFxAnchor.Cell,forward:2,lateral:1)}; f.Create();
                Point near = new Point(10+dx,10+dy), far = new Point(10+dx*2,10+dy*2), lateral = new Point(10+dx*2-dy,10+dy*2+dx);
                f.Fx.Play(f.Sequence(path:new[]{near,far},cells:new[]{near,far,lateral,new Point(40,20)},targets:Array.Empty<SpellFxTargetResult>()));
                f.Fx.Update(.3f); Assert.AreEqual(3,f.Drawn().Length);
                foreach(var p in new[]{near,far,lateral}) Assert.IsTrue(f.Drawn().Any(r=>Vector3.Distance(r.transform.position,Village3DProjection.CellCentre(p.X,p.Y)+Vector3.up*.3f)<.001f));
            }
        }
        [Test] public void ReducedDropsDecorativePieceButKeepsEssentialShapeAndOffDropsEverything()
        {
            using (var f = new NativeSpellFixture())
            {
                f.Entry.Pieces = new[] {f.Piece("core",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target),f.Piece("accent",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target,essential:false)}; f.Create();
                f.Fx.Play(f.Sequence());f.Fx.Update(.33f);Assert.AreEqual(2,f.Drawn().Length);f.Fx.ClearAll();
                SpellFxSettings.Mode=SpellFxMode.Reduced;f.Fx.Play(f.Sequence());f.Fx.Update(.33f);Assert.AreEqual(1,f.Drawn().Length);f.Fx.ClearAll();
                SpellFxSettings.Mode=SpellFxMode.Off;Assert.AreEqual(0,f.Fx.Play(f.Sequence()));Assert.AreEqual(0,f.Fx.ActiveCount);
            }
        }
        [Test] public void CompletedAndCancelledViewsAreReusedAndBorrowedAssetsSurviveDisposal()
        {
            using(var f=new NativeSpellFixture())
            {
                f.Create();f.Fx.Play(f.Sequence());f.Fx.Update(.33f);int count=f.Fx.AllocatedViewCount;Assert.Greater(count,0);
                f.Fx.Update(2);Assert.AreEqual(0,f.Fx.ActiveCount);Assert.AreEqual(0,f.Drawn().Length);
                f.Fx.Play(f.Sequence());f.Fx.Update(.33f);Assert.AreEqual(count,f.Fx.AllocatedViewCount);
                f.Fx.ClearAll();Assert.AreEqual(0,f.Fx.ActiveCount);Assert.AreEqual(0,f.Drawn().Length);
                f.Fx.Dispose();Assert.IsTrue(f.Mesh!=null);Assert.IsTrue(f.Library.Material!=null);Assert.IsTrue(f.Surface.FogTexture!=null);Assert.IsTrue(f.Target.IsCreated());
            }
        }
        [Test] public void CoordinatorOwnsNativeClockQueueCancellationAndFullCastGestureDuration()
        {
            using(var f=new NativeSpellFixture())
            {
                var tile=new GameObject("Legacy effects",typeof(Tilemap),typeof(TilemapRenderer));tile.transform.SetParent(f.Host.transform);
                var ascii=new AsciiFxRenderer(tile.GetComponent<Tilemap>());
                using(var world=new WorldFxCoordinator(ascii,f.Host.transform,f.Library))
                {
                    world.SetZone(f.Zone);world.SetNativeSurface(f.Surface);SpellFxBus.Emit(f.Sequence());
                    Assert.IsTrue(world.HasBlockingFx);world.Update(0);Assert.AreEqual(0,SpellFxBus.PendingCount);
                    Assert.Greater(world.NativeRenderer.ActiveCount,0);Assert.AreEqual(0,world.SpriteRenderer.ActiveCount);
                    SpellFxSettings.AnimationSpeed=4;world.Update(.09f);Assert.Greater(world.NativeRenderer.ActiveMeshCount,0);
                    world.CancelAll();Assert.AreEqual(0,world.NativeRenderer.ActiveCount);Assert.IsFalse(world.HasBlockingFx);
                }
            }
        }

        [TestCase(1, 0, 1)] [TestCase(0, -1, 6)] [TestCase(-1, 1, 3)]
        public void CarrierMapsActualContactAndPreservesRealMuzzleAndMeshOffsets(int dx, int dy, int distance)
        {
            using (var f = new NativeSpellFixture())
            {
                var head = f.Piece("real-head", NativeSpellFxRole.ProjectileHead, NativeSpellFxAnchor.ProjectileCarrier);
                f.Entry.Pieces = new[] { head };
                f.Entry.ProjectileProgress = new float[111];
                for (int i = 0; i < 111; i++)
                {
                    float progress = i < 32 ? .09f : 1f;
                    f.Entry.ProjectileProgress[i] = progress;
                    head.Poses[i].Position = new Vector3(.1f, .7f, progress * 4 + .04f);
                    head.Poses[i].Scale = i < 70 ? Vector3.one : Vector3.zero;
                }
                var source = new Point(10, 10);
                var path = Enumerable.Range(1, distance).Select(i => new Point(10 + dx * i, 10 + dy * i)).ToArray();
                var target = path.Last();
                var facing = Quaternion.LookRotation(new Vector3(dx, 0, -dy).normalized);
                f.Create(); f.Fx.Play(f.Sequence(path: path, targets: Array.Empty<SpellFxTargetResult>())); f.Fx.Update(0);
                Assert.That(Vector3.Distance(f.Drawn().Single().transform.position,
                    Village3DProjection.CellCentre(source.X, source.Y) + facing * new Vector3(.1f, .7f, .4f)), Is.LessThan(.001f));
                f.Fx.Update(.22f + distance * .025f);
                Assert.That(Vector3.Distance(f.Drawn().Single().transform.position,
                    Village3DProjection.CellCentre(target.X, target.Y) + facing * new Vector3(.1f, .7f, .04f)), Is.LessThan(.001f));
            }
        }
        [Test] public void CarrierFollowsCopiedBendAndClipsItsCurrentCellRatherThanHiddenSource()
        {
            using (var f = new NativeSpellFixture())
            {
                var head = f.Piece("head", NativeSpellFxRole.ProjectileHead, NativeSpellFxAnchor.ProjectileCarrier);
                f.Entry.Pieces = new[] { head }; f.Entry.ProjectileProgress = Enumerable.Repeat(.5f, 111).ToArray();
                foreach (int i in Enumerable.Range(0, 111)) head.Poses[i].Position = new Vector3(0, .7f, 2);
                f.Create(); f.Fx.Play(f.Sequence(path: new[] {new Point(11,10),new Point(11,9),new Point(12,9),new Point(12,8)}));
                f.Zone.GetCell(10,10).IsVisible = false; f.Fx.Update(.3f);
                Assert.That(Vector3.Distance(f.Drawn().Single().transform.position,
                    Village3DProjection.CellCentre(11,9)+Vector3.up*.7f),Is.LessThan(.001f));
                f.Zone.GetCell(11,9).IsVisible = false; f.Fx.Update(0); Assert.AreEqual(0,f.Drawn().Length);
            }
        }
        [Test] public void NativeCastHookUsesWholeGestureButDoesNotExtendTurnWaitAndFallbackCannotBorrowLastEntry()
        {
            using (var f = new NativeSpellFixture())
            {
                var tile = new GameObject("Cast legacy",typeof(Tilemap),typeof(TilemapRenderer)); tile.transform.SetParent(f.Host.transform);
                var ascii = new AsciiFxRenderer(tile.GetComponent<Tilemap>());
                var previous = EntityVisualHooks.CastCallback;
                float duration = 0;
                try
                {
                    EntityVisualHooks.CastCallback = (caster,zone,id,sx,sy,tx,ty,seconds) => duration = seconds;
                    using (var world = new WorldFxCoordinator(ascii,f.Host.transform,f.Library))
                    {
                        world.SetZone(f.Zone); world.SetNativeSurface(f.Surface); SpellFxSettings.AnimationSpeed = 2;
                        var sequence = new SpellFxSequence(f.Entry.SpellId,f.Zone,new Entity(),new Point(10,10),
                            path:new[]{new Point(11,10)},targets:new[]{NativeSpellFixture.Hit("body",11,10)});
                        world.Play(sequence); Assert.That(duration,Is.EqualTo(.33f).Within(.0001f));
                        world.Update(.313f); Assert.IsFalse(world.HasBlockingFx,"The .625s effect clears before the .66s caster gesture.");
                        var unknown = new SpellFxSequence("UnknownNative",f.Zone,new Entity(),new Point(10,10),affectedCells:new[]{new Point(11,10)});
                        world.Play(unknown); Assert.IsNull(world.NativeRenderer.LastEntry);
                        Assert.That(duration,Is.LessThanOrEqualTo(.12f));
                    }
                }
                finally { EntityVisualHooks.CastCallback = previous; }
            }
        }
        [Test] public void LifecycleDiagnosticsIdentifyNativeAcceptanceRefusalAndOneCancellationWithoutFrameSpam()
        {
            bool previous = Diag.IsChannelEnabled("effect"); Diag.SetChannel("effect",true);
            try
            {
                using (var f = new NativeSpellFixture())
                {
                    f.Create(); var before = new HashSet<string>(Diag.Snapshot(8192).Select(e => e.TraceId));
                    f.Fx.Play(f.Sequence()); f.Fx.Update(.33f); for(int i=0;i<20;i++) f.Fx.Update(0);
                    f.Fx.Play(f.Sequence(spell:"UnknownNative")); f.Fx.ClearAll(); f.Fx.ClearAll();
                    var records=Diag.Snapshot(8192).Where(e=>!before.Contains(e.TraceId)&&e.Kind.StartsWith("NativeSpell")).ToArray();
                    Assert.AreEqual(3,records.Length);
                    CollectionAssert.AreEquivalent(new[]{"NativeSpellScheduled","NativeSpellRejected","NativeSpellCancelled"},records.Select(e=>e.Kind));
                    Assert.IsTrue(records.All(e=>e.PayloadJson.Contains("\"backend\":\"native\"")&&e.PayloadJson.Contains("NativeSpellContract")));
                    Assert.IsTrue(records.Single(e=>e.Kind=="NativeSpellScheduled").PayloadJson.Contains("\"pieces\":1"));
                }
            }
            finally { Diag.SetChannel("effect",previous); }
        }

        [Test] public void CastBindingSelectsActualRigPathAndDoesNotBorrowTownClipForUnknownRigs()
        {
            using (var f = new NativeSpellFixture())
            {
                var animator = f.Host.AddComponent<Animator>();
                Assert.IsNull(f.Entry.FindCastClip(animator));
                var model = new GameObject("character-teal"); model.transform.SetParent(f.Host.transform);
                var rig = new GameObject("character-teal__Rig"); rig.transform.SetParent(model.transform);
                Assert.AreSame(f.Entry.CastClip,f.Entry.FindCastClip(animator));
                rig.name = "ring-player__Rig";
                Assert.IsNull(f.Entry.FindCastClip(animator),"A renamed ring rig cannot receive incompatible town binding paths.");
                var ringClip = f.Own(new AnimationClip());
                f.Entry.CastBindings = new[]{new NativeSpellCastBinding {RigPath="character-teal/ring-player__Rig",Clip=ringClip}};
                Assert.AreSame(ringClip,f.Entry.FindCastClip(animator));
                rig.name = "UnrelatedAnimalRig";
                Assert.IsNull(f.Entry.FindCastClip(animator)); Assert.IsNull(f.Entry.FindCastClip(null));
                var obsolete = new GameObject("VillageRig"); obsolete.transform.SetParent(f.Host.transform);
                Assert.IsNull(f.Entry.FindCastClip(animator),"A guessed studio rig name cannot authorize an incompatible native cast clip.");
            }
        }
#if UNITY_EDITOR
        [TestCase("Pyromancy_EmberSpit")] [TestCase("Pyromancy_FlamingHands")]
        [TestCase("Hydromancy_JetBlast")] [TestCase("Galvanism_GroundSurge")]
        [TestCase("Cryomancy_RimeGrip")] [TestCase("Spellcraft_Calm")]
        [TestCase("Hydromancy_ConjureRain")]
        public void ActualImportedCastMovesRenamedRingPlayerRigAndReturnsToItsOwnRest(string spell)
        {
            var library=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath); Assert.NotNull(library);
            var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath); Assert.NotNull(ring);
            var prefab=ring.FindModel("ring-player"); Assert.NotNull(prefab);
            var instance=Object.Instantiate(prefab);
            try
            {
                var animator=instance.GetComponentInChildren<Animator>(true); Assert.NotNull(animator); animator.enabled=false;
                var entry=library.Find(spell); Assert.NotNull(entry);
                var clip=entry.FindCastClip(animator); Assert.NotNull(clip,"Every compatible native ring rig needs its own binding paths.");
                var bindings=AnimationUtility.GetCurveBindings(clip); Assert.IsNotEmpty(bindings);
                foreach(var binding in bindings)
                {
                    Assert.IsNotEmpty(binding.path,"A gesture never keys the actor root.");
                    Assert.NotNull(animator.transform.Find(binding.path),binding.path);
                }
                var bones=instance.GetComponentInChildren<SkinnedMeshRenderer>(true).bones;
                clip.SampleAnimation(animator.gameObject,0); var rest=bones.Select(b=>b.localRotation).ToArray();
                var rootPosition=instance.transform.position; var rootRotation=instance.transform.rotation;
                clip.SampleAnimation(animator.gameObject,.22f);
                Assert.Greater(bones.Select((b,i)=>Quaternion.Angle(rest[i],b.localRotation)).Max(),5f);
                clip.SampleAnimation(animator.gameObject,clip.length);
                Assert.Less(bones.Select((b,i)=>Quaternion.Angle(rest[i],b.localRotation)).Max(),.25f);
                Assert.AreEqual(rootPosition,instance.transform.position); Assert.AreEqual(rootRotation,instance.transform.rotation);
            }
            finally {Object.DestroyImmediate(instance);}
        }
#endif
    }

    /// <summary>Native S3D14 measured the library and first pool loading inside the
    /// first cast. Readiness must perform that bounded work before any cast exists.</summary>
    public sealed class NativeSpellFxReadinessTests
    {
        [SetUp] public void Setup()
        { SpellFxSettings.Mode=SpellFxMode.Full;SpellFxSettings.AnimationSpeed=1;AsciiFxBus.Clear();SpellFxBus.Clear(); }
        [TearDown] public void Teardown()
        { SpellFxSettings.Mode=SpellFxMode.Full;SpellFxSettings.AnimationSpeed=1;AsciiFxBus.Clear();SpellFxBus.Clear(); }
        static WorldFxCoordinator World(NativeSpellFixture f,NativeSpellFxLibrary library=null)
        {
            var tile=new GameObject("Readiness legacy effects",typeof(Tilemap),typeof(TilemapRenderer));tile.transform.SetParent(f.Host.transform);
            var world=new WorldFxCoordinator(new AsciiFxRenderer(tile.GetComponent<Tilemap>()),f.Host.transform,library??f.Library);
            world.SetZone(f.Zone);return world;
        }
        static object Cache(NativeSpellFxLibrary library)=>typeof(NativeSpellFxLibrary).GetField("_entries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(library);
        static void Empty(WorldFxCoordinator world)
        {
            Assert.AreEqual(0,world.NativeRenderer.ActiveCount);Assert.AreEqual(0,world.NativeRenderer.ActiveMeshCount);
            Assert.AreEqual(0,world.ActiveSequenceCount);Assert.IsFalse(world.HasBlockingFx);
            Assert.AreEqual(0,SpellFxBus.PendingCount);Assert.IsNull(world.NativeRenderer.LastEntry);
            Assert.AreEqual(0,world.NativeRenderer.Root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled));
            Assert.AreEqual(0,world.NativeRenderer.Root.GetComponentsInChildren<Collider>(true).Length);
        }
        [TestCase(SpellFxMode.Full,384)] [TestCase(SpellFxMode.Reduced,96)]
        public void VisibleBindingValidatesAndWarmsBoundedPoolBeforeAnyCast(SpellFxMode mode,int views)
        {
            using(var f=new NativeSpellFixture())using(var world=World(f))
            {
                SpellFxSettings.Mode=mode;Assert.IsNull(Cache(f.Library));Assert.AreEqual(0,world.NativeRenderer.AllocatedViewCount);
                world.SetNativeSurface(f.Surface);
                Assert.NotNull(Cache(f.Library),"The complete authored library must be validated before the first command.");
                Assert.AreEqual(views,world.NativeRenderer.AllocatedViewCount);Empty(world);
                Assert.IsTrue(world.NativeRenderer.IsPrepared);var preparation=world.NativeRenderer.LastPreparation;
                Assert.NotNull(preparation);Assert.AreEqual(views,preparation.ViewCount);
                Assert.That(preparation.LoadSeconds,Is.GreaterThanOrEqualTo(0));
                Assert.That(preparation.ValidationSeconds,Is.GreaterThanOrEqualTo(0));
                Assert.That(preparation.PoolSeconds,Is.GreaterThanOrEqualTo(0));
                Assert.AreEqual(0,f.Zone.GetAllEntities().Count);
                var root=world.NativeRenderer.Root;var cache=Cache(f.Library);
                world.SetNativeSurface(f.Surface);world.Update(0);world.Update(0);
                Assert.AreSame(root,world.NativeRenderer.Root);Assert.AreSame(cache,Cache(f.Library));
                Assert.AreSame(preparation,world.NativeRenderer.LastPreparation,"No-op readiness must retain the original load evidence.");
                Assert.AreEqual(views,world.NativeRenderer.AllocatedViewCount);Empty(world);
            }
        }
        [TestCase("off")] [TestCase("hidden")] [TestCase("ascii")] [TestCase("surface-hidden")]
        public void DisabledNativePresentationSkipsColdWorkUntilItActuallyBecomesVisible(string boundary)
        {
            using(var f=new NativeSpellFixture())using(var world=World(f))
            {
                if(boundary=="off")SpellFxSettings.Mode=SpellFxMode.Off;
                if(boundary=="hidden")world.Update(0,presentationVisible:false);
                if(boundary=="ascii")world.Update(0,spritesEnabled:false);
                if(boundary=="surface-hidden")f.Surface.Sync(f.Source,false,false);
                world.SetNativeSurface(f.Surface);
                Assert.IsNull(Cache(f.Library));Assert.IsNull(world.NativeRenderer.Root);Assert.AreEqual(0,world.NativeRenderer.AllocatedViewCount);
                SpellFxSettings.Mode=SpellFxMode.Full;f.Surface.Sync(f.Source,true,false);world.Update(0,true,true);
                Assert.NotNull(Cache(f.Library));Assert.AreEqual(384,world.NativeRenderer.AllocatedViewCount);Empty(world);
            }
        }
        [Test] public void ReducedToFullExpandsOnceBeforeCastingAndRetainsItsBorrowedAssets()
        {
            using(var f=new NativeSpellFixture())using(var world=World(f))
            {
                SpellFxSettings.Mode=SpellFxMode.Reduced;world.SetNativeSurface(f.Surface);
                Assert.AreEqual(96,world.NativeRenderer.AllocatedViewCount);var root=world.NativeRenderer.Root;
                SpellFxSettings.Mode=SpellFxMode.Full;world.Update(0);
                Assert.AreSame(root,world.NativeRenderer.Root);Assert.AreEqual(384,world.NativeRenderer.AllocatedViewCount);Empty(world);
                world.SetNativeSurface(null);Assert.IsTrue(root==null);Assert.IsTrue(f.Library.Material!=null&&f.Mesh!=null&&f.Surface.ContentRoot!=null);
                Assert.AreEqual(0,world.NativeRenderer.AllocatedViewCount);
                Assert.IsFalse(world.NativeRenderer.Prepare());Assert.IsFalse(world.NativeRenderer.IsPrepared);
                world.NativeRenderer.Dispose();Assert.IsFalse(world.NativeRenderer.Prepare());
            }
        }
        [Test] public void InvalidLibraryFailsReadinessWithoutCreatingPartialViewsOrAPlayback()
        {
            using(var f=new NativeSpellFixture())using(var world=World(f))
            {
                f.Entry.Pieces[0].Poses=Array.Empty<NativeSpellFxPose>();
                Assert.DoesNotThrow(()=>world.SetNativeSurface(f.Surface));
                Assert.IsNotEmpty(world.NativeRenderer.Failure,"Invalid readiness must provide a sparse diagnostic before a cast.");
                Assert.IsNull(Cache(f.Library));Assert.IsNull(world.NativeRenderer.Root);Assert.AreEqual(0,world.NativeRenderer.AllocatedViewCount);
                Assert.AreEqual(0,world.NativeRenderer.ActiveCount);Assert.IsFalse(world.HasBlockingFx);
            }
        }
        [Test] public void PreparedActualRainAtMaximumCropCoverageCreatesNoViewsInsideItsFirstCast()
        {
            var library=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);Assert.NotNull(library);
            using(var f=new NativeSpellFixture("Hydromancy_ConjureRain"))using(var world=World(f,library))
            {
                world.SetNativeSurface(f.Surface);int prepared=world.NativeRenderer.AllocatedViewCount;
                Assert.AreEqual(384,prepared,"The worst allowed live budget must be allocated before a first command.");
                var hits=Enumerable.Range(7,7).SelectMany(y=>Enumerable.Range(7,7).Select(x=>NativeSpellFixture.Hit(x+","+y,x,y))).ToArray();
                var result=world.Play(f.Sequence(path:Array.Empty<Point>(),targets:hits));world.Update(.4f);
                Assert.AreEqual(WorldFxPlaybackState.Playing,result.State);Assert.Greater(world.NativeRenderer.ActiveMeshCount,0);
                Assert.AreEqual(prepared,world.NativeRenderer.AllocatedViewCount,"Even the first dense Rain cast must reuse the prepared native pool.");
                world.CancelAll();Empty(world);
            }
        }
    }

}
