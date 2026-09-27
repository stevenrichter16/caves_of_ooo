#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Explicit factory-staged art, with real receiving-zone ownership,
    /// exact adopted meshes and actual imported poses. Not gameplay acquisition.</summary>
    public sealed class SpreadVisitorCreatureGalleryTests
    {
        public static IEnumerable<string> Blueprints => SpreadVisitorCreatureSource.Specs.Select(s => s.Blueprint);

        [TestCaseSource(nameof(Blueprints))]
        public void CaptureCurrentAdoptedVisitorAndItsActualAuthoredGear(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var cell = GalleryCell(f);
                var actor = f.Add(blueprint, cell.x, cell.y);
                f.Set("FullReveal", true);
                f.Refresh();
                var presenter = (SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(actor, out var bodyProof), bodyProof.Failure);
                var entry = SpreadVisitorCreatureLibrary.Load().Find(SpreadVisitorCreatureSource.ForBlueprint(blueprint).Id);
                Assert.AreSame(entry.Mesh, bodyProof.SubmittedMesh);
                var root = f.View(actor);
                var skin = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s => s.sharedMesh == entry.Mesh);
                var animator = root.GetComponentInChildren<Animator>();
                Assert.NotNull(animator);
                var items = actor.GetPart<InventoryPart>()?.GetAllEquipped().ToArray() ?? Array.Empty<Entity>();
                if (blueprint == "SkeletalSentry")
                {
                    var helmet = items.Single(i => i.BlueprintName == "IronHelmet");
                    Assert.AreSame(actor, helmet.GetPart<PhysicsPart>().Equipped);
                }
                var vertices = skin.sharedMesh.vertices;
                var bindposes = skin.sharedMesh.bindposes;
                string graph = Graph(f);
                int equipmentVersion = EquipmentChangeBus.GlobalVersion;
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Docs/Verification/DensityCompletion/SpreadBiome/Art/Visitors/NativeGallery", blueprint + "-" + Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(directory);
                var frames = new List<Frame>();
                foreach (var pose in new[] { ("Idle", 0f, 0f), ("Walk", .31f, 35f), ("Attack", .31f, -35f) })
                {
                    var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name == pose.Item1);
                    clip.SampleAnimation(animator.gameObject, clip.length * pose.Item2);
                    Assert.True(presenter.TryGetApprovedStyle(actor, out var current), current.Failure);
                    Assert.AreSame(entry.Mesh, current.SubmittedMesh);
                    var gear = new List<string>();
                    foreach (var item in items)
                    {
                        Assert.True(presenter.TryGetApprovedEquipmentStyle(actor, item, out var evidence), item.BlueprintName + ": " + evidence.Failure);
                        Assert.True(presenter.TryGetEquipmentView(actor, item, out var view));
                        Assert.True(view.GetComponentsInChildren<SkinnedMeshRenderer>().All(s => s.bones.All(b => b.IsChildOf(root.transform))));
                        gear.Add(item.ID + "|" + item.BlueprintName + "|" + evidence.ModelId + "|" + evidence.ExpectedMesh.name);
                    }
                    var bounds = VisibleBounds(root);
                    string file = frames.Count.ToString("00") + "-" + pose.Item1 + ".png";
                    // Reuse the existing native camera capture/restoration path. It
                    // owns only temporary render targets/textures, never scene data.
                    var capture = typeof(SpreadEquipmentFitGalleryTests).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
                    Assert.NotNull(capture);
                    object camera;
                    try { camera = capture.Invoke(null, new object[] { presenter.WorldCamera, bounds,
                        root.transform.rotation * Quaternion.Euler(0, pose.Item3, 0), Path.Combine(directory, file) }); }
                    catch (TargetInvocationException e) { throw e.InnerException ?? e; }
                    frames.Add(new Frame { image = file, clip = clip.name, normalizedTime = pose.Item2,
                        width = bounds.size.x, height = bounds.size.y, depth = bounds.size.z,
                        camera = JsonUtility.ToJson(camera), gear = gear.ToArray() });
                    Assert.AreEqual(graph, Graph(f));
                    Assert.AreEqual(equipmentVersion, EquipmentChangeBus.GlobalVersion);
                    CollectionAssert.AreEqual(vertices, skin.sharedMesh.vertices);
                    CollectionAssert.AreEqual(bindposes, skin.sharedMesh.bindposes);
                }
                File.WriteAllText(Path.Combine(directory, "report.json"), JsonUtility.ToJson(new Report
                {
                    blueprint = blueprint, owner = actor.ID, zone = f.Zone.ZoneID, bodyModel = bodyProof.ModelId, frames = frames.ToArray(),
                    canVerify = "Explicit factory-staged original visitor in a real managed Spread graph, actual unchanged authored gear, exact adopted body and all equipment style identity, real imported Idle/Walk/Attack samples, PNG close-ups. Tile state, zone entity version, IDs/positions/HP/equipped IDs and source vertices/bindposes remain unchanged during capture.",
                    cannotVerify = "Not ordinary acquisition, party transit, scheduler behavior, animation transitions, keyboard combat or balance. Full reveal, factory staging and close-up cameras are explicit art fixtures. Nonblack frames and bounded assertions do not establish final visual fit or remove surrounding-geometry occlusion; actual images require review."
                }, true));
                Assert.AreEqual(3, frames.Count);
            }
        }

        private static (int x, int y) GalleryCell(SpawnRing3DIntegrationFixture f)
        {
            var player = f.Zone.GetEntityPosition(f.Player);
            for (int y = 4; y < 21; y++) for (int x = 5; x < 75; x++)
            {
                if (Math.Abs(x - player.x) + Math.Abs(y - player.y) < 8) continue;
                bool clear = true;
                for (int dy = -2; dy <= 2 && clear; dy++) for (int dx = -2; dx <= 2; dx++)
                {
                    var at = f.Zone.GetCell(x + dx, y + dy);
                    if (at == null || at.BlocksMovement(f.Player) || at.Objects.Any(e => e.HasPart<BrainPart>() || e.HasTag("Player"))) { clear = false; break; }
                }
                if (clear) return (x, y);
            }
            Assert.Fail("Actual generated fixture lacks an open art-staging area; do not delete scenery to manufacture a gallery.");
            return (-1, -1);
        }
        private static string Graph(SpawnRing3DIntegrationFixture f)
            => f.Zone.TileState.ToSaveString() + "|" + f.Zone.EntityVersion + "|" + string.Join("\n", f.Zone.GetReadOnlyEntities()
                .Select(e => e.ID + "|" + e.BlueprintName + "|" + f.Zone.GetEntityPosition(e) + "|" + e.GetStatValue("Hitpoints") + "|"
                    + string.Join(",", e.GetPart<InventoryPart>()?.GetAllEquipped().Select(i => i.ID).OrderBy(x => x) ?? Enumerable.Empty<string>())).OrderBy(x => x));
        private static Bounds VisibleBounds(GameObject root)
        {
            bool any = false; var result = new Bounds();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) continue;
                Mesh mesh = null; bool owned = false;
                try
                {
                    if (renderer is SkinnedMeshRenderer skin) { mesh = new Mesh(); owned = true; skin.BakeMesh(mesh); }
                    else mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    Assert.NotNull(mesh, "Actual submitted equipment must contribute to camera bounds.");
                    foreach (var vertex in mesh.vertices)
                    {
                        var point = renderer.transform.TransformPoint(vertex);
                        if (!any) { result = new Bounds(point, Vector3.zero); any = true; }
                        else result.Encapsulate(point);
                    }
                }
                finally { if (owned && mesh != null) Object.DestroyImmediate(mesh); }
            }
            Assert.True(any); Assert.Greater(result.size.y, .02f); return result;
        }
        [Serializable] private sealed class Frame { public string image, clip, camera; public float normalizedTime, width, height, depth; public string[] gear; }
        [Serializable] private sealed class Report { public string blueprint, owner, zone, bodyModel, canVerify, cannotVerify; public Frame[] frames; }
    }
}
#endif
