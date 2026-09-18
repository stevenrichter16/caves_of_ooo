using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent density, pooled-kind and authored-data counterchecks.
    /// Small synthetic pieces exercise the production renderer; actual imported
    /// art and mixed-fog GPU pixels have separate source/native acceptance gates.</summary>
    public sealed class NativeSpellReadabilityAdversarialTests
    {
        const string Luminous = "CavesOfOoo/Spell3D/Luminous";
        const string Glow = "CavesOfOoo/Spell3D/SoftGlow";
        SpellFxMode oldMode;
        float oldSpeed, oldFlash;

        [SetUp] public void Setup()
        {
            oldMode = SpellFxSettings.Mode; oldSpeed = SpellFxSettings.AnimationSpeed;
            oldFlash = SpellFxSettings.FlashIntensity;
            SpellFxSettings.Mode = SpellFxMode.Full;
            SpellFxSettings.AnimationSpeed = 1; SpellFxSettings.FlashIntensity = 0;
        }
        [TearDown] public void Teardown()
        {
            SpellFxSettings.Mode = oldMode; SpellFxSettings.AnimationSpeed = oldSpeed;
            SpellFxSettings.FlashIntensity = oldFlash;
        }

        // Missing new fields intentionally retain the old behavior during RED,
        // rather than causing a compiler failure before the controls can execute.
        static void SetNew(object owner, string name, object value)
        {
            var field = owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;
            field.SetValue(owner, field.FieldType.IsEnum ? Enum.Parse(field.FieldType, (string)value) : value);
        }
        static Material OptionalMaterial(NativeSpellFixture f, string shaderName)
        {
            // Before the new shader exists, the real baseline Palette is an
            // intentionally incompatible stand-in, never a guessed custom shader.
            return f.Own(new Material(Shader.Find(shaderName) ?? f.Library.Material.shader));
        }
        static void AddMaterials(NativeSpellFixture f)
        {
            SetNew(f.Library, "LuminousMaterial", OptionalMaterial(f, Luminous));
            SetNew(f.Library, "GlowMaterial", OptionalMaterial(f, Glow));
        }
        static Mesh DistinctMesh(NativeSpellFixture f, string name, bool glow = false)
        {
            var mesh = f.Own(UnityEngine.Object.Instantiate(f.Mesh)); mesh.name = name;
            if (glow) mesh.colors = new[] { new Color(1,1,1,0), new Color(1,1,1,1), new Color(1,1,1,.4f) };
            return mesh;
        }
        static NativeSpellFxPiece Contact(NativeSpellFixture f, string id, bool essential = true)
            => f.Piece(id, NativeSpellFxRole.TargetImpact, NativeSpellFxAnchor.Target, essential: essential);
        static NativeSpellFxEntry Entry(NativeSpellFixture f, string id, params NativeSpellFxPiece[] pieces)
            => new NativeSpellFxEntry { SpellId = id, CastClip = f.Entry.CastClip, CastDuration = .66f,
                SampleRate = 100, ReleaseFrame = 22, StudyContactFrame = 32, StudyClearFrame = 70,
                AuthoredDistanceCells = 4, LaunchDistance = .36f, Pieces = pieces };
        static Mesh[] DrawnMeshes(NativeSpellFixture f)
            => f.Drawn().Select(r => r.GetComponent<MeshFilter>().sharedMesh).ToArray();
        static void Pose(NativeSpellFxPiece piece, Vector3 position)
        {
            for (int i = 0; i < piece.Poses.Length; i++)
            { var pose = piece.Poses[i]; pose.Position = position; piece.Poses[i] = pose; }
        }

        [Test] public void NewPresentationFieldsHaveTheAgreedPublicTypes()
        {
            var piece = typeof(NativeSpellFxPiece);
            Assert.AreEqual(typeof(float), piece.GetField("Emission")?.FieldType);
            Assert.AreEqual(typeof(bool), piece.GetField("Glow")?.FieldType);
            var bounds = piece.GetField("VisualBounds")?.FieldType;
            Assert.NotNull(bounds); Assert.IsTrue(bounds.IsEnum);
            CollectionAssert.AreEquivalent(new[] { "CellSurface", "AirborneDecoration" }, Enum.GetNames(bounds));
            Assert.AreEqual(typeof(Material), typeof(NativeSpellFxLibrary).GetField("LuminousMaterial")?.FieldType);
            Assert.AreEqual(typeof(Material), typeof(NativeSpellFxLibrary).GetField("GlowMaterial")?.FieldType);
        }

        [TestCase(SpellFxMode.Full)] [TestCase(SpellFxMode.Reduced)]
        public void LateEssentialSurvivesEarlierOptionalTemplatesBeyondTheScheduleLimit(SpellFxMode mode)
        {
            using (var f = new NativeSpellFixture())
            {
                SpellFxSettings.Mode = mode;
                var core = Contact(f, "last defining core"); core.Mesh = DistinctMesh(f, "essential core");
                f.Entry.Pieces = Enumerable.Range(0, NativeSpellFxRenderer.MaximumScheduledFragments + 9)
                    .Select(i => Contact(f, "optional-" + i, false)).Concat(new[] { core }).ToArray();
                f.Create(); Assert.Greater(f.Fx.Play(f.Sequence()), 0); f.Fx.Update(.33f);
                Assert.Contains(core.Mesh, DrawnMeshes(f), "Optional authored order must not discard the defining mesh.");
                Assert.That(f.Fx.ActiveCount, Is.InRange(1, NativeSpellFxRenderer.MaximumScheduledFragments));
                Assert.That(f.Fx.ActiveMeshCount, Is.InRange(1, mode == SpellFxMode.Full
                    ? NativeSpellFxRenderer.MaximumFullMeshes : NativeSpellFxRenderer.MaximumReducedMeshes));
                if (mode == SpellFxMode.Reduced) Assert.AreEqual(1, f.Fx.ActiveCount);
                f.Fx.ClearAll(); Assert.IsEmpty(f.Drawn()); Assert.IsFalse(f.Fx.HasBlockingFx);
            }
        }

        [Test] public void LaterAcceptedCastCoreDrawsBeforeOlderCastOptionalSaturation()
        {
            using (var f = new NativeSpellFixture("older"))
            {
                var oldCore = Contact(f, "older core"); oldCore.Mesh = DistinctMesh(f, "older core mesh");
                var newCore = Contact(f, "newer core"); newCore.Mesh = DistinctMesh(f, "newer core mesh");
                f.Entry.Pieces = new[] { oldCore }.Concat(Enumerable.Range(0, NativeSpellFxRenderer.MaximumFullMeshes)
                    .Select(i => Contact(f, "old decoration-" + i, false))).ToArray();
                f.Library.Entries = new[] { f.Entry, Entry(f, "newer", newCore) };
                f.Create(); Assert.Greater(f.Fx.Play(f.Sequence()), 0);
                Assert.Greater(f.Fx.Play(f.Sequence(spell: "newer")), 0,
                    "Both casts are admitted below the schedule budget; this is a draw-priority test.");
                f.Fx.Update(.33f);
                Assert.Contains(oldCore.Mesh, DrawnMeshes(f)); Assert.Contains(newCore.Mesh, DrawnMeshes(f));
                Assert.LessOrEqual(f.Fx.ActiveMeshCount, NativeSpellFxRenderer.MaximumFullMeshes);
            }
        }

        [TestCase("reduced-optional")] [TestCase("unmatched-outcome")] [TestCase("empty-recorded-path")]
        public void ValidFilteredEntryRetainsItsCasterGestureWithoutInventingGeometry(string cause)
        {
            using (var f = new NativeSpellFixture())
            {
                var piece = Contact(f, "conditional contact");
                var filtered = f.Sequence(); var matching = f.Sequence();
                if (cause == "reduced-optional") { piece.ReducedEssential = false; SpellFxSettings.Mode = SpellFxMode.Reduced; }
                if (cause == "unmatched-outcome")
                {
                    piece.Condition = NativeSpellFxCondition.FrozenApplied;
                    matching = f.Sequence(targets: new[] { NativeSpellFixture.Hit("frozen",14,10,applied:new[]{"FrozenEffect"}) });
                }
                if (cause == "empty-recorded-path")
                {
                    piece = f.Piece("recorded ground", NativeSpellFxRole.GroundCell, NativeSpellFxAnchor.Cell, forward:1);
                    filtered = f.Sequence(path:Array.Empty<Point>());
                }
                f.Entry.Pieces = new[] { piece };
                var caster = new Entity();
                SpellFxSequence WithCaster(SpellFxSequence sequence) => new SpellFxSequence(sequence.SpellId,
                    sequence.Zone,caster,sequence.Source,sequence.Path,sequence.AffectedCells,sequence.Targets,
                    blocksTurnAdvance:sequence.BlocksTurnAdvance,reactions:sequence.Reactions);
                var tile = new GameObject("Filtered cast legacy",typeof(UnityEngine.Tilemaps.Tilemap),typeof(UnityEngine.Tilemaps.TilemapRenderer));
                tile.transform.SetParent(f.Host.transform);
                var previous = EntityVisualHooks.CastCallback;
                try
                {
                    int gestures = 0; float seconds = 0;
                    EntityVisualHooks.CastCallback = (actor,zone,id,sx,sy,tx,ty,duration) =>
                    { Assert.AreSame(caster,actor); Assert.AreEqual(f.Entry.SpellId,id); gestures++; seconds=duration; };
                    using (var world = new WorldFxCoordinator(new AsciiFxRenderer(tile.GetComponent<UnityEngine.Tilemaps.Tilemap>()),f.Host.transform,f.Library))
                    {
                        f.Fx=world.NativeRenderer; world.SetZone(f.Zone); world.SetNativeSurface(f.Surface); world.Update(0);
                        var playback=world.Play(WithCaster(filtered));
                        Assert.AreSame(f.Entry,f.Fx.LastEntry,"A valid imported caster gesture is still presentation work when its target geometry is filtered.");
                        Assert.AreSame(f.Entry.CastClip,f.Fx.LastEntry.CastClip); Assert.IsNull(f.Fx.Failure);
                        Assert.AreEqual(1,gestures);
                        Assert.That(seconds,Is.EqualTo(f.Entry.CastDuration/SpellFxSettings.AnimationSpeed).Within(.00001f));
                        Assert.AreEqual(WorldFxPlaybackState.Playing,playback.State);
                        world.Update(.33f); Assert.AreEqual(0,f.Fx.ActiveCount); Assert.AreEqual(0,f.Fx.ActiveMeshCount); Assert.IsEmpty(f.Drawn());
                        world.Update(.75f); Assert.AreEqual(WorldFxPlaybackState.Completed,playback.State);
                        Assert.IsFalse(world.HasBlockingFx); Assert.AreSame(f.Entry,f.Fx.LastEntry);
                        if (cause == "reduced-optional") { SpellFxSettings.Mode=SpellFxMode.Full; world.Update(0); }
                        Assert.AreEqual(WorldFxPlaybackState.Playing,world.Play(WithCaster(matching)).State);
                        world.Update(.33f); Assert.IsNotEmpty(f.Drawn(),"Restoring the matching condition adds actual geometry to the same native gesture.");
                        Assert.AreSame(f.Entry,f.Fx.LastEntry); Assert.AreEqual(2,gestures);
                        world.Update(.75f); Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
                    }
                }
                finally { EntityVisualHooks.CastCallback=previous; }
            }
        }

        [Test] public void OnePooledViewResetsPaletteLuminousGlowAndBackWithoutMutatingBorrowedMaterials()
        {
            using (var f = new NativeSpellFixture("plain"))
            {
                AddMaterials(f);
                var bright = Contact(f, "bright core"); SetNew(bright, "Emission", .7f);
                var glow = Contact(f, "feathered shell"); glow.Mesh = DistinctMesh(f, "shell", true);
                SetNew(glow, "Glow", true); SetNew(glow, "Emission", .4f);
                SetNew(glow, "VisualBounds", "AirborneDecoration");
                f.Library.Entries = new[] { f.Entry, Entry(f, "bright", bright), Entry(f, "glow", glow) };
                f.Create(); Transform view = null; var materials = new HashSet<Material>();
                var ids = new[] { "plain", "bright", "glow", "plain" };
                var shaders = new[] { "CavesOfOoo/Village3D/Palette", Luminous, Glow, "CavesOfOoo/Village3D/Palette" };
                var emissions = new[] { 0f, .7f, .4f, 0f };
                for (int i = 0; i < ids.Length; i++)
                {
                    Assert.Greater(f.Fx.Play(f.Sequence(spell:ids[i])), 0); f.Fx.Update(.33f);
                    var rendered = f.Drawn().Single();
                    if (view == null) view = rendered.transform; else Assert.AreSame(view, rendered.transform);
                    Assert.AreEqual(shaders[i], rendered.sharedMaterial.shader.name);
                    Assert.AreNotSame(f.Library.Material, rendered.sharedMaterial);
                    var block = new MaterialPropertyBlock(); rendered.GetPropertyBlock(block);
                    Assert.That(block.GetFloat("_Emission"), Is.EqualTo(emissions[i]).Within(.00001f));
                    Assert.AreSame(f.Surface.FogTexture, rendered.sharedMaterial.GetTexture("_FogLight"));
                    materials.Add(rendered.sharedMaterial); f.Fx.ClearAll(); Assert.IsEmpty(f.Drawn());
                }
                Assert.AreEqual(1, f.Fx.AllocatedViewCount);
                Assert.AreEqual(3, materials.Count, "Each render kind owns one material, reused across casts.");
                var borrowed = new[] { f.Library.Material,
                    (Material)f.Library.GetType().GetField("LuminousMaterial").GetValue(f.Library),
                    (Material)f.Library.GetType().GetField("GlowMaterial").GetValue(f.Library) };
                Assert.IsFalse(borrowed.Any(materials.Contains));
                f.Fx.SetSurface(null); Assert.IsTrue(view == null);
                Assert.IsTrue(materials.All(m => m == null)); Assert.IsTrue(borrowed.All(m => m != null));
                Assert.IsTrue(f.Mesh != null && glow.Mesh != null && f.Surface.FogTexture != null);
                Assert.AreEqual(0, f.Zone.GetAllEntities().Count);
            }
        }

        [TestCase("negative-emission")] [TestCase("excess-emission")] [TestCase("nan-emission")]
        [TestCase("infinite-emission")] [TestCase("zero-glow-emission")]
        [TestCase("no-alpha")] [TestCase("flat-alpha")] [TestCase("zero-alpha")]
        [TestCase("negative-alpha")] [TestCase("excess-alpha")] [TestCase("nan-alpha")]
        [TestCase("infinite-alpha")]
        public void InvalidGlowDataRejectsAndTheSameCorrectedGeometryIsAccepted(string fault)
        {
            using (var f = new NativeSpellFixture())
            {
                AddMaterials(f); var piece = f.Entry.Pieces[0];
                piece.Mesh = DistinctMesh(f, "validated shell", true);
                SetNew(piece, "Glow", true); SetNew(piece, "Emission", .5f);
                SetNew(piece, "VisualBounds", "AirborneDecoration");
                var colors = piece.Mesh.colors;
                if (fault == "negative-emission") SetNew(piece, "Emission", -.01f);
                if (fault == "excess-emission") SetNew(piece, "Emission", 1.01f);
                if (fault == "nan-emission") SetNew(piece, "Emission", float.NaN);
                if (fault == "infinite-emission") SetNew(piece, "Emission", float.PositiveInfinity);
                if (fault == "zero-glow-emission") SetNew(piece, "Emission", 0f);
                if (fault == "no-alpha") colors = Array.Empty<Color>();
                if (fault == "flat-alpha") colors = Enumerable.Repeat(Color.white, 3).ToArray();
                if (fault == "zero-alpha") colors = Enumerable.Repeat(new Color(1,1,1,0), 3).ToArray();
                if (fault == "negative-alpha") colors[2].a = -.01f;
                if (fault == "excess-alpha") colors[2].a = 1.01f;
                if (fault == "nan-alpha") colors[2].a = float.NaN;
                if (fault == "infinite-alpha") colors[2].a = float.PositiveInfinity;
                piece.Mesh.colors = colors;
                Assert.Throws<InvalidOperationException>(() => f.Library.Validate(), fault);
                SetNew(piece, "Emission", .5f);
                piece.Mesh.colors = new[] { new Color(1,1,1,0), new Color(1,1,1,1), new Color(1,1,1,.4f) };
                Assert.DoesNotThrow(() => f.Library.Validate(), "Valid gradient is the necessary positive control.");
                f.Create(); Assert.Greater(f.Fx.Play(f.Sequence()),0); f.Fx.Update(.33f); Assert.IsNotEmpty(f.Drawn());
            }
        }

        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void RequiredSelectedMaterialCannotBeMissingOrUseTheWrongShader(bool glow, bool wrongShader)
        {
            using (var f = new NativeSpellFixture())
            {
                var piece = f.Entry.Pieces[0]; SetNew(piece, "Emission", .5f); SetNew(piece, "Glow", glow);
                if (glow) piece.Mesh = DistinctMesh(f, "selected shell", true);
                string field = glow ? "GlowMaterial" : "LuminousMaterial", shader = glow ? Glow : Luminous;
                SetNew(f.Library, field, wrongShader ? f.Own(new Material(f.Library.Material)) : null);
                Assert.Throws<InvalidOperationException>(() => f.Library.Validate());
                SetNew(f.Library, field, OptionalMaterial(f, shader));
                Assert.DoesNotThrow(() => f.Library.Validate());
                f.Create(); Assert.Greater(f.Fx.Play(f.Sequence()),0); f.Fx.Update(.33f);
                Assert.AreEqual(shader, f.Drawn().Single().sharedMaterial.shader.name);
            }
        }

        [Test] public void BaselineOpaquePieceNeedsNoOptionalMaterialOrVertexColors()
        {
            using (var f = new NativeSpellFixture())
            {
                Assert.IsEmpty(f.Mesh.colors); Assert.DoesNotThrow(() => f.Library.Validate());
                f.Create(); Assert.Greater(f.Fx.Play(f.Sequence()),0); f.Fx.Update(.33f);
                Assert.AreEqual("CavesOfOoo/Village3D/Palette", f.Drawn().Single().sharedMaterial.shader.name);
            }
        }

        [TestCase(NativeSpellFxRole.GroundCell)] [TestCase(NativeSpellFxRole.ConeCell)]
        public void AllEightDirectionsFitSurfaceGeometryAndRetainAirborneSize(NativeSpellFxRole role)
        {
            foreach (var direction in new[] { new Point(1,0), new Point(1,1), new Point(0,1), new Point(-1,1),
                new Point(-1,0), new Point(-1,-1), new Point(0,-1), new Point(1,-1) })
            using (var f = new NativeSpellFixture("direction test"))
            {
                var ground = f.Piece("ground",role,NativeSpellFxAnchor.Cell,forward:1);
                var air = f.Piece("air",role,NativeSpellFxAnchor.Cell,forward:1);
                air.Mesh = DistinctMesh(f,"air mesh"); SetNew(air,"VisualBounds","AirborneDecoration");
                var local = new Vector3(.22f,.9f,.18f); Pose(ground,local); Pose(air,local);
                f.Entry.Pieces = new[] { ground,air }; f.Create();
                var cell = new Point(10+direction.X,10+direction.Y);
                Assert.Greater(f.Fx.Play(f.Sequence(path:new[]{cell},cells:new[]{cell},targets:Array.Empty<SpellFxTargetResult>())),0);
                f.Fx.Update(.3f); Assert.AreEqual(2,f.Drawn().Length);
                var facing = Quaternion.LookRotation(new Vector3(direction.X,0,-direction.Y).normalized,Vector3.up);
                float fit = direction.X != 0 && direction.Y != 0 ? Mathf.Sqrt(.5f) : 1f;
                foreach (var rendered in f.Drawn())
                {
                    bool isAir = rendered.GetComponent<MeshFilter>().sharedMesh == air.Mesh;
                    float expected = isAir ? 1 : fit;
                    Assert.Less(Vector3.Distance(new Vector3(expected,1,expected),rendered.transform.localScale),.00001f,
                        role+" "+direction+" air="+isAir);
                    var expectedPosition = Village3DProjection.CellCentre(cell.X,cell.Y)
                        + facing * new Vector3(local.x*expected,local.y,local.z*expected);
                    Assert.Less(Vector3.Distance(expectedPosition,rendered.transform.position),.00001f);
                }
                Assert.AreEqual(0,f.Zone.GetAllEntities().Count);
            }
        }

        [TestCase(NativeSpellFxRole.GroundCell)] [TestCase(NativeSpellFxRole.ConeCell)]
        public void AirborneDecorationStillNeedsItsRecordedContactCell(NativeSpellFxRole role)
        {
            using (var f = new NativeSpellFixture("bounded contact"))
            {
                var near = f.Piece("recorded air",role,NativeSpellFxAnchor.Cell,forward:1);
                var far = f.Piece("unrecorded air",role,NativeSpellFxAnchor.Cell,forward:2);
                far.Mesh = DistinctMesh(f,"unrecorded mesh");
                SetNew(near,"VisualBounds","AirborneDecoration"); SetNew(far,"VisualBounds","AirborneDecoration");
                f.Entry.Pieces = new[]{near,far}; f.Create();
                var cell = new Point(11,10);
                Assert.Greater(f.Fx.Play(f.Sequence(path:new[]{cell},cells:new[]{cell},targets:Array.Empty<SpellFxTargetResult>())),0);
                f.Fx.Update(.3f); Assert.AreEqual(1,f.Drawn().Length); Assert.AreSame(near.Mesh,DrawnMeshes(f).Single());
                f.Fx.ClearAll(); var second = new Point(12,10);
                Assert.Greater(f.Fx.Play(f.Sequence(path:new[]{cell,second},cells:new[]{cell,second},targets:Array.Empty<SpellFxTargetResult>())),0);
                f.Fx.Update(.3f); Assert.AreEqual(2,f.Drawn().Length); Assert.Contains(far.Mesh,DrawnMeshes(f));
            }
        }

        [TestCase("clear")] [TestCase("off")] [TestCase("surface-hidden")] [TestCase("zone-change")]
        public void GlowCancellationLeavesNoVisiblePooledGeometryOrBorrowedResourceDamage(string boundary)
        {
            using (var f = new NativeSpellFixture())
            {
                AddMaterials(f); var piece=f.Entry.Pieces[0]; piece.Mesh=DistinctMesh(f,"cancelled shell",true);
                SetNew(piece,"Glow",true); SetNew(piece,"Emission",.5f);
                f.Create(); Assert.Greater(f.Fx.Play(f.Sequence()),0); f.Fx.Update(.33f); Assert.AreEqual(1,f.Drawn().Length);
                if(boundary=="clear") f.Fx.ClearAll();
                if(boundary=="off") { SpellFxSettings.Mode=SpellFxMode.Off; f.Fx.Update(0); }
                if(boundary=="surface-hidden") { f.Surface.Sync(f.Source,false,false); f.Fx.Update(0); }
                if(boundary=="zone-change") f.Fx.SetZone(new Zone("other zone"));
                Assert.IsEmpty(f.Drawn()); Assert.AreEqual(0,f.Fx.ActiveCount); Assert.IsFalse(f.Fx.HasBlockingFx);
                Assert.IsTrue(f.Library.Material!=null && piece.Mesh!=null && f.Surface.FogTexture!=null);
                SpellFxSettings.Mode=SpellFxMode.Full; f.Surface.Sync(f.Source,true,false); f.Fx.SetZone(f.Zone);
                Assert.Greater(f.Fx.Play(f.Sequence()),0); f.Fx.Update(.33f);
                Assert.AreEqual(Glow,f.Drawn().Single().sharedMaterial.shader.name);
            }
        }
    }
}
