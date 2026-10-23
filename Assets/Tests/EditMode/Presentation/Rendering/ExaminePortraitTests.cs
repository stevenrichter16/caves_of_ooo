using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class ExaminePortraitTests
    {
        static T Read<T>(ExaminePortraitRenderer renderer, string property)
            => (T)typeof(ExaminePortraitRenderer).GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(renderer);
        static ExaminePortraitRenderer Probe()
            => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        static Entity Glyph(string text = "?", string color = "&C")
        {
            var entity = new Entity { BlueprintName = "UnmodeledLedgerSubject" };
            entity.AddPart(new RenderPart { RenderString = text, ColorString = color });
            return entity;
        }

        [Test] public void NullSubjectAndMissingRenderHaveNoInventedPortraitOrOwnedResources()
        {
            using (var renderer = new ExaminePortraitRenderer())
            {
                Assert.Null(renderer.GetPortrait((Zone)null, null).Texture);
                Assert.Null(renderer.GetPortrait(null, new Entity()).Texture);
                Assert.Null(Read<RenderTexture>(renderer, "OwnedTarget"));
            }
        }

        [Test] public void UnmodeledSubjectBorrowsItsCurrentGlyphAndTintWithoutAllocatingATarget()
        {
            using (var renderer = new ExaminePortraitRenderer())
            {
                var subject = Glyph("!", "&R"); var result = renderer.GetPortrait(null, subject);
                var sprite = CP437TilesetGenerator.GetTile('!').sprite;
                Assert.AreSame(sprite.texture, result.Texture); Assert.False(result.IsModel);
                Assert.AreEqual(QudColorParser.Parse("&R"), result.Tint);
                Assert.AreEqual(new Rect(sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height), result.Uv);
                Assert.Null(Read<RenderTexture>(renderer, "OwnedTarget")); Assert.AreEqual(0, Read<int>(renderer, "CaptureCount"));
                subject.GetPart<RenderPart>().Visible = false;
                Assert.Null(renderer.GetPortrait(null, subject).Texture);
            }
        }

        [Test] public void UnknownUnicodeFallbackDoesNotRebuildTheWorldAtlas()
        {
            using (var renderer = new ExaminePortraitRenderer())
            {
                var before = CP437TilesetGenerator.GetAtlasTexture();
                var result = renderer.GetPortrait(null, Glyph("α"));
                Assert.NotNull(result.Texture); Assert.False(result.IsModel);
                Assert.AreSame(before, CP437TilesetGenerator.GetAtlasTexture());
            }
        }

        [TestCase("RepairLinedWell", "repair-cultivation-lined-well-broken")]
        [TestCase("RepairRopeWell", "repair-cultivation-rope-well-broken")]
        [TestCase("KnotflaxCrop", "repair-cultivation-knotflax-0-dry")]
        public void PortraitUsesExactIndividualWorldPrefabNeverItsCombinedGroundPatch(string blueprint, string model)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            using (var renderer = Probe())
            {
                var subject = f.Add(blueprint); f.Refresh(f.Dirty(subject));
                Assert.True(f.Find(subject, out var groundPatch, out _));
                var result = renderer.GetPortrait(f.Zone, subject);
                Assert.True(result.IsModel); Assert.AreEqual(model, renderer.CurrentModelId);
                Assert.AreSame(f.Library.FindModel(model), Read<GameObject>(renderer, "CurrentSourcePrefab"));
                Assert.AreNotSame(groundPatch, Read<GameObject>(renderer, "CurrentSourcePrefab"));
                Assert.AreEqual(1, Read<GameObject>(renderer, "PreviewRoot").GetComponentsInChildren<Renderer>(true).Length);
                Assert.True(Read<GameObject>(renderer, "PreviewRoot").GetComponentsInChildren<Transform>(true)
                    .All(t => t.gameObject.layer == ExaminePortraitRenderer.PortraitLayer));
                Assert.IsEmpty(Read<GameObject>(renderer, "PreviewRoot").GetComponentsInChildren<Collider>(true));
                Assert.AreEqual(192, result.Texture.width); Assert.AreEqual(192, result.Texture.height);
                Assert.AreEqual(FilterMode.Point, result.Texture.filterMode);
            }
        }

        [Test] public void RepeatedSubjectReusesOneCaptureAndRepairChangeReusesTheSameSmallTarget()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            using (var renderer = Probe())
            {
                var subject = f.Add("RepairLinedWell");
                var first = renderer.GetPortrait(f.Zone, subject);
                var repeated = renderer.GetPortrait(f.Zone, subject);
                Assert.AreSame(first.Texture, repeated.Texture); Assert.AreEqual(1, Read<int>(renderer, "CaptureCount"));
                subject.GetPart<RepairablePart>().Repaired = true;
                var repaired = renderer.GetPortrait(f.Zone, subject);
                Assert.AreSame(first.Texture, repaired.Texture); Assert.AreEqual(2, Read<int>(renderer, "CaptureCount"));
                Assert.AreEqual("repair-cultivation-lined-well-restored", renderer.CurrentModelId);
                Assert.False(Read<Camera>(renderer, "PreviewCamera").enabled);
                Assert.AreEqual(1 << ExaminePortraitRenderer.PortraitLayer, Read<Camera>(renderer, "PreviewCamera").cullingMask);
            }
        }

        [TestCase("hidden")][TestCase("removed")][TestCase("reskinned")]
        public void InvalidCurrentSubjectCannotReusePreviousModel(string condition)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            using (var renderer = Probe())
            {
                var subject = f.Add("RepairLinedWell"); Assert.True(renderer.GetPortrait(f.Zone, subject).IsModel);
                if (condition == "hidden") subject.GetPart<RenderPart>().Visible = false;
                if (condition == "removed") f.Zone.RemoveEntity(subject);
                if (condition == "reskinned") subject.GetPart<RenderPart>().RenderString = "!";
                Assert.False(renderer.GetPortrait(f.Zone, subject).IsModel);
                Assert.Null(renderer.CurrentModelId); Assert.Null(Read<GameObject>(renderer, "PreviewRoot"));
            }
        }

        [TestCase(true)][TestCase(false)]
        public void VisibleFootprintCellCanShowAnAuthorizedPortraitWithoutRevealingHiddenNeighbors(bool footprint)
        {
            var zone = new Zone("LedgerPortraitTest"); var subject = Glyph("!", "&C");
            subject.ID = Guid.NewGuid().ToString(); subject.AddPart(new PhysicsPart());
            if (footprint) subject.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            Assert.True(zone.AddEntity(subject, 10, 10));
            zone.GetCell(10, 10).Explored = true; zone.GetCell(10, 10).IsVisible = false;
            zone.GetCell(11, 10).Explored = zone.GetCell(11, 10).IsVisible = true;
            using (var renderer = Probe())
            {
                Assert.AreEqual(footprint, renderer.GetPortrait(zone, subject).Texture != null);
                zone.GetCell(11, 10).Explored = false;
                Assert.Null(renderer.GetPortrait(zone, subject).Texture);
            }
        }

        [Test] public void PortraitCaptureRestoresThePreviouslyActiveRenderTarget()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var previous = RenderTexture.active;
                var renderer = (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new object[] { (Action<Camera, RenderTexture>)((camera, target) => RenderTexture.active = target) }, null);
                try
                {
                    renderer.GetPortrait(f.Zone, f.Add("RepairLinedWell"));
                    Assert.AreSame(previous, RenderTexture.active, "A one-shot portrait must not redirect later world/UI rendering.");
                }
                finally { RenderTexture.active = previous; renderer.Dispose(); }
            }
        }

        [Test] public void PortraitMaterialsAreOwnedAndClearReleasesOnlyOwnedResources()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var subject = f.Add("RepairRopeWell"); var source = f.Library.FindModel("repair-cultivation-rope-well-broken");
                var sourceMaterial = source.GetComponent<Renderer>().sharedMaterial;
                var sourceMesh = source.GetComponent<MeshFilter>().sharedMesh;
                var originalFog = sourceMaterial.GetTexture("_FogLight"); var originalTransform = source.transform.localToWorldMatrix;
                var renderer = Probe();
                renderer.GetPortrait(f.Zone, subject);
                Assert.AreNotSame(sourceMaterial, Read<GameObject>(renderer, "PreviewRoot").GetComponent<Renderer>().sharedMaterial);
                Assert.AreSame(sourceMesh, Read<GameObject>(renderer, "PreviewRoot").GetComponent<MeshFilter>().sharedMesh);
                var target = Read<RenderTexture>(renderer, "OwnedTarget"); var root = Read<GameObject>(renderer, "PreviewRoot");
                renderer.Clear(); Assert.Null(Read<RenderTexture>(renderer, "OwnedTarget")); Assert.Null(Read<GameObject>(renderer, "PreviewRoot"));
                Assert.True(target == null); Assert.True(root == null); renderer.Dispose(); renderer.Dispose();
                Assert.NotNull(sourceMaterial); Assert.NotNull(sourceMesh);
                Assert.AreSame(originalFog, sourceMaterial.GetTexture("_FogLight"));
                Assert.AreEqual(originalTransform, source.transform.localToWorldMatrix);
            }
        }
    }
}
