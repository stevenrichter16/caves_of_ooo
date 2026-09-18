using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Imported Ember art must flow along the recorded shot and finish
    /// quietly beside unchanged Flaming Hands. These are transformed geometry and
    /// visibility contracts; the source/native image gates judge the actual image.</summary>
    public sealed class NativeEmberProjectileRevisionTests
    {
        const string Ember = "Pyromancy_EmberSpit", Hands = "Pyromancy_FlamingHands";
        const float VertexTolerance = .025f, TailSeamTolerance = .08f;
        static readonly Point Source = new Point(10, 10);
        SpellFxMode oldMode;
        float oldSpeed, oldFlash;

        [SetUp] public void Setup()
        {
            oldMode = SpellFxSettings.Mode; oldSpeed = SpellFxSettings.AnimationSpeed; oldFlash = SpellFxSettings.FlashIntensity;
            SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1; SpellFxSettings.FlashIntensity = 0;
            AsciiFxBus.Clear(); SpellFxBus.Clear();
        }
        [TearDown] public void Cleanup()
        {
            AsciiFxBus.Clear(); SpellFxBus.Clear();
            SpellFxSettings.Mode = oldMode; SpellFxSettings.AnimationSpeed = oldSpeed; SpellFxSettings.FlashIntensity = oldFlash;
        }

        static NativeSpellFxLibrary Imported(NativeSpellFixture f)
        {
            var library = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
            Assert.NotNull(library, "The real imported library is a prerequisite, not a synthetic substitute.");
            Assert.NotNull(library.Find(Ember)); Assert.NotNull(library.Find(Hands));
            f.Fx = new NativeSpellFxRenderer(library); f.Fx.SetZone(f.Zone); f.Fx.SetSurface(f.Surface);
            return library;
        }
        static SpellFxSequence Shot(NativeSpellFixture f, int distance, int dx, int dy, string spell = Ember)
        {
            var path = Enumerable.Range(1, distance).Select(i => new Point(Source.X + dx * i, Source.Y + dy * i)).ToArray();
            var end = path.Last();
            return f.Sequence(source: Source, path: path, cells: new[] { end },
                targets: new[] { NativeSpellFixture.Hit("actual-first-contact", end.X, end.Y) }, spell: spell);
        }
        static float Contact(NativeSpellFxEntry entry, int distance) => entry.ReleaseFrame / entry.SampleRate
            + (entry.StudyContactFrame > entry.ReleaseFrame ? distance * .025f : 0);
        static float AtStudyFrame(NativeSpellFxEntry entry, int distance, float frame)
        {
            if (frame <= entry.ReleaseFrame) return frame / entry.SampleRate;
            float contact = Contact(entry, distance), release = entry.ReleaseFrame / entry.SampleRate;
            return frame < entry.StudyContactFrame
                ? Mathf.Lerp(release, contact, (frame - entry.ReleaseFrame) / (entry.StudyContactFrame - entry.ReleaseFrame))
                : contact + (frame - entry.StudyContactFrame) / entry.SampleRate;
        }
        static MeshRenderer[] Drawn(NativeSpellFixture f, NativeSpellFxEntry entry, Func<NativeSpellFxPiece, bool> predicate)
        {
            var meshes = new HashSet<Mesh>(entry.Pieces.Where(predicate).Select(p => p.Mesh));
            return f.Drawn().Where(r => meshes.Contains(r.GetComponent<MeshFilter>().sharedMesh)).ToArray();
        }
        static bool Projectile(NativeSpellFxPiece piece) => piece.Role == NativeSpellFxRole.ProjectileHead
            || piece.Role == NativeSpellFxRole.ProjectileTrail;
        static Vector3[] Vertices(MeshRenderer view) => view.GetComponent<MeshFilter>().sharedMesh.vertices
            .Select(view.transform.TransformPoint).ToArray();
        static Bounds BoundsOf(MeshRenderer[] views)
        {
            var vertices = views.SelectMany(Vertices).ToArray(); Assert.IsNotEmpty(vertices);
            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (var vertex in vertices) bounds.Encapsulate(vertex);
            return bounds;
        }

        [TestCase(1, 1, 0, SpellFxMode.Full)]
        [TestCase(1, 1, -1, SpellFxMode.Reduced)]
        [TestCase(2, 0, -1, SpellFxMode.Reduced)]
        [TestCase(2, -1, 1, SpellFxMode.Full)]
        [TestCase(4, 1, 0, SpellFxMode.Full)]
        [TestCase(4, 1, -1, SpellFxMode.Reduced)]
        public void ShortAndDiagonalShotsKeepEveryDrawnProjectileVertexBetweenCasterAndContactCell(
            int distance, int dx, int dy, SpellFxMode mode)
        {
            SpellFxSettings.Mode = mode;
            using (var f = new NativeSpellFixture())
            {
                var entry = Imported(f).Find(Ember);
                var sequence = Shot(f, distance, dx, dy);
                Assert.Greater(f.Fx.Play(sequence), 0); Assert.AreSame(entry, f.Fx.LastEntry);
                var origin = Village3DProjection.CellCentre(Source.X, Source.Y);
                var forward = new Vector3(dx, 0, -dy).normalized;
                float step = Mathf.Sqrt(dx * dx + dy * dy);
                float endCap = distance * step + .5f * (Mathf.Abs(dx) + Mathf.Abs(dy)) / step;
                float age = 0; int drawnSamples = 0;
                // Include anticipation, launch, late travel, fractional contact,
                // the authored follow-through overshoot and its final contraction.
                foreach (float frame in new[] { 14f, 18f, 22f, 24f, 26f, 28f, 29.5f, 33f, 39f, 48f, 60f })
                {
                    float next = AtStudyFrame(entry, distance, frame); f.Fx.Update(next - age); age = next;
                    var views = Drawn(f, entry, Projectile);
                    if (views.Length == 0) continue;
                    drawnSamples++;
                    foreach (var view in views)
                    foreach (var vertex in Vertices(view))
                    {
                        float along = Vector3.Dot(vertex - origin, forward);
                        Assert.That(along, Is.GreaterThanOrEqualTo(-VertexTolerance),
                            view.GetComponent<MeshFilter>().sharedMesh.name + " extends behind the caster at study frame " + frame);
                        Assert.That(along, Is.LessThanOrEqualTo(endCap + VertexTolerance),
                            "The actual first hit caps the tail/head, including short casts and follow-through: "
                            + view.GetComponent<MeshFilter>().sharedMesh.name + " at frame " + frame);
                    }
                }
                Assert.Greater(drawnSamples, 2, "No-projectile art must not vacuously satisfy the bounds.");
                f.Fx.Update(1); Assert.IsEmpty(f.Drawn()); Assert.IsFalse(f.Fx.HasBlockingFx);
                CollectionAssert.AreEqual(sequence.Path, Enumerable.Range(1, distance)
                    .Select(i => new Point(Source.X + dx * i, Source.Y + dy * i)).ToArray());
                Assert.AreEqual(0, f.Zone.GetAllEntities().Count, "Drawing cannot manufacture gameplay objects.");
            }
        }

        [TestCase(1, 0, SpellFxMode.Full)]
        [TestCase(1, -1, SpellFxMode.Reduced)]
        public void FourCellShotRetainsLongContinuousTailBeforeItsSmallImpact(int dx, int dy, SpellFxMode mode)
        {
            SpellFxSettings.Mode = mode;
            using (var f = new NativeSpellFixture())
            {
                var entry = Imported(f).Find(Ember); Assert.Greater(f.Fx.Play(Shot(f, 4, dx, dy)), 0);
                f.Fx.Update(AtStudyFrame(entry, 4, entry.StudyContactFrame - .3f));
                Assert.IsNotEmpty(Drawn(f, entry, p => p.Role == NativeSpellFxRole.ProjectileHead), "There must be a real head at late travel.");
                Assert.IsEmpty(Drawn(f, entry, p => p.Role == NativeSpellFxRole.TargetImpact), "Impact cannot impersonate the travelling cone.");
                var tail = Drawn(f, entry, p => p.Role == NativeSpellFxRole.ProjectileTrail && p.ReducedEssential && !p.Glow);
                Assert.IsNotEmpty(tail, "Reduced mode must retain actual tail solids, not only a head or a wide transparent halo.");
                var origin = Village3DProjection.CellCentre(Source.X, Source.Y);
                var forward = new Vector3(dx, 0, -dy).normalized;
                var intervals = new List<Vector2>();
                foreach (var view in tail)
                {
                    var vertices = Vertices(view); var triangles = view.GetComponent<MeshFilter>().sharedMesh.triangles;
                    // Triangle intervals retain real longitudinal holes between
                    // disconnected motes; a batch's overall AABB would hide them.
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        float a = Vector3.Dot(vertices[triangles[i]] - origin, forward);
                        float b = Vector3.Dot(vertices[triangles[i + 1]] - origin, forward);
                        float c = Vector3.Dot(vertices[triangles[i + 2]] - origin, forward);
                        intervals.Add(new Vector2(Mathf.Min(a, b, c), Mathf.Max(a, b, c)));
                    }
                }
                Assert.IsNotEmpty(intervals); intervals.Sort((a, b) => a.x.CompareTo(b.x));
                float start = intervals[0].x, end = intervals[0].y, longest = 0;
                foreach (var interval in intervals.Skip(1))
                {
                    if (interval.x > end + TailSeamTolerance)
                    { longest = Mathf.Max(longest, end - start); start = interval.x; end = interval.y; }
                    else end = Mathf.Max(end, interval.y);
                }
                longest = Mathf.Max(longest, end - start);
                Assert.GreaterOrEqual(longest, 2.25f * Mathf.Sqrt(dx * dx + dy * dy),
                    "A long filled wake must follow the recorded route; separated soot specks do not satisfy this longitudinal coverage gate.");
            }
        }

        static Vector3 PeakImpact(NativeSpellFixture f, NativeSpellFxEntry entry, string spell, bool glow, bool expectedPresent = true)
        {
            f.Fx.ClearAll(); int distance = spell == Ember ? 4 : 1;
            Assert.Greater(f.Fx.Play(Shot(f, distance, 1, 0, spell)), 0);
            float age = 0; int samples = 0; var peak = Vector3.zero;
            foreach (float offset in new[] { .03f, .08f, .16f, .25f })
            {
                float next = Contact(entry, distance) + offset; f.Fx.Update(next - age); age = next;
                var views = Drawn(f, entry, p => p.Glow == glow && p.Role ==
                    (spell == Ember ? NativeSpellFxRole.TargetImpact : NativeSpellFxRole.ConeCell));
                if (views.Length == 0) continue;
                samples++; peak = Vector3.Max(peak, BoundsOf(views).size);
            }
            if (expectedPresent)
                Assert.Greater(samples, 0, spell + " must have a real " + (glow ? "halo" : "solid impact") + " to compare.");
            else Assert.AreEqual(0, samples, "Reduced mode omits the optional impact halo while retaining the solid hit.");
            return peak;
        }
        [TestCase(SpellFxMode.Full)] [TestCase(SpellFxMode.Reduced)]
        public void EmberImpactIsCompactAndClearlySmallerThanTheUnchangedHandsCrown(SpellFxMode mode)
        {
            SpellFxSettings.Mode = mode;
            using (var f = new NativeSpellFixture())
            {
                var library = Imported(f);
                var hands = PeakImpact(f, library.Find(Hands), Hands, false);
                var ember = PeakImpact(f, library.Find(Ember), Ember, false);
                var halo = PeakImpact(f, library.Find(Ember), Ember, true, expectedPresent: mode == SpellFxMode.Full);
                var haloPieces = library.Find(Ember).Pieces.Where(p => p.Role == NativeSpellFxRole.TargetImpact && p.Glow).ToArray();
                Assert.IsNotEmpty(haloPieces, "The Full-mode halo is a real imported prerequisite for the Reduced filtering countercheck.");
                Assert.IsTrue(haloPieces.All(p => !p.ReducedEssential), "Ember's subtle halo is optional decoration.");
                float handsWidth = Mathf.Max(hands.x, hands.z), emberWidth = Mathf.Max(ember.x, ember.z);
                Assert.Greater(handsWidth, 2.3f, "The approved large Hands crown is the real comparison control.");
                Assert.Greater(hands.y, .8f);
                Assert.LessOrEqual(emberWidth, 1.15f, "Ember's small hit must not retain its former radial fire crown.");
                Assert.LessOrEqual(emberWidth, handsWidth * .45f);
                Assert.LessOrEqual(ember.y, hands.y * .55f);
                if (mode == SpellFxMode.Full)
                    Assert.LessOrEqual(Mathf.Max(halo.x, halo.z), 1.4f, "A large halo must not secretly restore the removed crown footprint.");
            }
        }

        [TestCase("source-hidden", SpellFxMode.Full)]
        [TestCase("rear-hidden", SpellFxMode.Reduced)]
        [TestCase("fully-hidden", SpellFxMode.Full)]
        public void PartialFogKeepsTheVisibleProjectileWhileFullFogHidesAndRestoresIt(string fog, SpellFxMode mode)
        {
            SpellFxSettings.Mode = mode;
            using (var f = new NativeSpellFixture())
            {
                var entry = Imported(f).Find(Ember); Assert.Greater(f.Fx.Play(Shot(f, 4, 1, 0)), 0);
                f.Fx.Update(AtStudyFrame(entry, 4, entry.StudyContactFrame - .3f));
                var baseline = Drawn(f, entry, Projectile);
                Assert.IsNotEmpty(Drawn(f, entry, p => p.Role == NativeSpellFxRole.ProjectileHead));
                Assert.IsNotEmpty(Drawn(f, entry, p => p.Role == NativeSpellFxRole.ProjectileTrail));
                var originalMeshes = baseline.Select(r => r.GetComponent<MeshFilter>().sharedMesh).ToArray();
                foreach (var cell in f.Zone.Cells)
                    cell.IsVisible = fog != "fully-hidden" && (fog != "rear-hidden" || cell.X >= 13);
                if (fog == "source-hidden") f.Zone.GetCell(Source.X, Source.Y).IsVisible = false;
                f.Surface.UpdateFog(f.Zone, null, true); f.Fx.Update(0);
                var visible = Drawn(f, entry, Projectile);
                if (fog == "fully-hidden") Assert.IsEmpty(visible);
                else
                {
                    Assert.IsNotEmpty(Drawn(f, entry, p => p.Role == NativeSpellFxRole.ProjectileHead));
                    Assert.IsNotEmpty(Drawn(f, entry, p => p.Role == NativeSpellFxRole.ProjectileTrail),
                        "A hidden source/rear segment must not suppress the still-visible travelling wake.");
                    if (fog == "rear-hidden") Assert.Less(visible.Length, baseline.Length, "The rear visibility change must be meaningful.");
                    foreach (var view in visible)
                    {
                        Assert.IsTrue(Village3DProjection.TryWorldToCell(view.transform.position, out int x, out int y));
                        Assert.IsTrue(f.Zone.GetCell(x, y).IsVisible, "Centre clipping must use the current physical fragment cell.");
                        Assert.AreSame(f.Surface.FogTexture, view.sharedMaterial.GetTexture("_FogLight"));
                        Assert.AreEqual(1, view.sharedMaterial.GetFloat("_Transient"));
                    }
                }
                // Pixel clipping of overhanging triangles remains the independent
                // GPU shader gate. Here restore the identical pose without advancing.
                foreach (var cell in f.Zone.Cells) cell.IsVisible = true;
                f.Surface.UpdateFog(f.Zone, null, true); f.Fx.Update(0);
                CollectionAssert.AreEquivalent(originalMeshes,
                    Drawn(f, entry, Projectile).Select(r => r.GetComponent<MeshFilter>().sharedMesh).ToArray());
                f.Fx.Update(1); Assert.IsEmpty(f.Drawn()); Assert.IsFalse(f.Fx.HasBlockingFx);
            }
        }
    }
}
