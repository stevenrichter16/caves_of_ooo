#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    /// <summary>Explicit staged art only. Real assets and native equip commands;
    /// sampled imported poses are not player combat or ordinary acquisition.</summary>
    public sealed class SpreadEquipmentFitGalleryTests
    {
        [TestCase("Player", "LeatherArmor", "LeatherCap", "LeatherBoots", "Cloak", "Greatsword")]
        [TestCase("Farmer", "ChainMail", "IronHelmet", "IronshodBoots", "WardedCloak", "Hatchet")]
        [TestCase("VillageChild", "LeatherArmor", "LeatherCap", "LeatherBoots", "Cloak", "Torch")]
        [TestCase("Scribe", "PlateArmor", "IronHelmet", "IronshodBoots", "Cloak", "Spear")]
        [TestCase("Elder", "FineRingMail", "LeatherCap", "LeatherBoots", "WardedCloak", "CounterweightMaul")]
        [TestCase("MarlbackScrabbler", "PlateArmor", "IronHelmet", "LeatherBoots", "Cloak", "Hatchet")]
        [TestCase("MarlbackBreacher", "RivetedPlate", "IronHelmet", "IronshodBoots", "Cloak", "BreacherCleaver")]
        public void CaptureActualFittedForms(string blueprint, string armor, string hat, string boots, string cloak, string weapon)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor = blueprint == "Player" ? f.Player : f.Add(blueprint);
                f.CleanGear(actor);
                var items = new List<Entity>();
                foreach (string name in new[] { armor, hat, boots, cloak, "LeatherGloves", weapon }) items.Add(EquipObserved(f, actor, name));
                // A second tangible hand item demonstrates glove/tool coexistence.
                if (weapon == "Hatchet" || weapon == "Torch" || weapon == "BreacherCleaver") items.Add(EquipObserved(f, actor, "Buckler"));
                f.Set("FullReveal", true); f.Refresh();
                var presenter = (SpawnRing3DPresenter)f.Presenter; var root = f.View(actor);
                Assert.True(presenter.TryGetApprovedStyle(actor, out var bodyProof), bodyProof.Failure);
                var body = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x => x.sharedMesh == bodyProof.SubmittedMesh);
                Assert.Greater(body.bones.Length, 1); var animator = root.GetComponentInChildren<Animator>(); Assert.NotNull(animator);
                string graph = Graph(f); int bus = EquipmentChangeBus.GlobalVersion; var borrowed = f.Source.targetTexture;
                var sourceVertices = body.sharedMesh.vertices; var sourceBindposes = body.sharedMesh.bindposes;
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/DensityCompletion/SpreadBiome/Equipment/NativeFit", blueprint + "-" + Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(path); var rows = new List<FrameRow>(); var camera = presenter.WorldCamera;
                foreach (var pose in new[] { ("Idle", 0f, 0f), ("Idle", 0f, 90f), ("Idle", 0f, 180f), ("Walk", .25f, 35f), ("Attack", .25f, 35f) })
                {
                    var clip = animator.runtimeAnimatorController.animationClips.Single(x => x.name == pose.Item1 || x.name.EndsWith(pose.Item1, StringComparison.Ordinal));
                    clip.SampleAnimation(animator.gameObject, clip.length * pose.Item2);
                    Assert.True(presenter.TryGetApprovedStyle(actor, out _));
                    var gear = new List<GearRow>();
                    foreach (var item in items)
                    {
                        Assert.True(presenter.TryGetApprovedEquipmentStyle(actor, item, out var proof), item.BlueprintName + ": " + proof.Failure);
                        Assert.True(presenter.TryGetEquipmentView(actor, item, out var view));
                        var pieces = view.GetComponentsInChildren<Renderer>(true);
                        gear.Add(new GearRow { blueprint = item.BlueprintName, id = item.ID, model = proof.ModelId,
                            pieces = pieces.Length, mesh = proof.ExpectedMesh.name, palette = proof.ExpectedMaterial.GetTexture("_BaseMap").name,
                            bones = pieces.OfType<SkinnedMeshRenderer>().SelectMany(x => x.bones).Select(x => x.name).ToArray() });
                    }
                    var bounds = VisibleBounds(root); string filename = rows.Count.ToString("00") + "-" + pose.Item1 + "-" + pose.Item3 + ".png";
                    var settings = Capture(camera, bounds, root.transform.rotation * Quaternion.Euler(0, pose.Item3, 0), path + "/" + filename);
                    rows.Add(new FrameRow { image = filename, clip = clip.name, normalizedTime = pose.Item2, yaw = pose.Item3,
                        width = bounds.size.x, height = bounds.size.y, depth = bounds.size.z, camera = settings, gear = gear.ToArray() });
                    Assert.AreEqual(graph, Graph(f)); Assert.AreEqual(bus, EquipmentChangeBus.GlobalVersion);
                    Assert.AreSame(borrowed, f.Source.targetTexture); CollectionAssert.AreEqual(sourceVertices, body.sharedMesh.vertices);
                    CollectionAssert.AreEqual(sourceBindposes, body.sharedMesh.bindposes);
                }
                File.WriteAllText(path + "/report.json", JsonUtility.ToJson(new Report { blueprint = blueprint, owner = actor.ID, zone = f.Zone.ZoneID,
                    bodyModel = bodyProof.ModelId, frames = rows.ToArray(),
                    canVerify = "Explicit factory gear equipped through native InventorySystem on seven representative real rigged bodies; exact adopted body/equipment mesh and owned palette, paired bone ownership, real imported clip sampling, unchanged tile state, zone entity version, existing IDs/positions/HP/equipped IDs during capture, actual PNG output.",
                    cannotVerify = "Staged EditMode art, not naturally acquired equipment, scheduling, live animation transitions, keyboard combat or balance. Camera is an explicit close-up; passing assertions do not establish visual fit, clipping, readability or appearance; neighboring geometry may still occlude the subject and requires human review." }, true));
                Assert.AreEqual(5, rows.Count);
            }
        }
        private static Entity EquipObserved(SpawnRing3DIntegrationFixture f, Entity actor, string blueprint)
        {
            var inventory = actor.GetPart<InventoryPart>();
            // CleanGear leaves authored loadout items carried. Reuse those exact
            // specimens: adding an identical fresh item would merge its identity.
            var candidate = inventory.Objects.FirstOrDefault(x => x.BlueprintName == blueprint);
            if (candidate == null)
            {
                candidate = f.Factory.CreateEntity(blueprint); Assert.NotNull(candidate);
                Assert.True(inventory.AddObject(candidate)); Assert.True(inventory.Contains(candidate));
            }
            int units = candidate.GetPart<StackerPart>()?.StackCount ?? 1;
            Assert.True(InventorySystem.Equip(actor, candidate), "Native equip must admit the real carried specimen: " + actor.BlueprintName + "/" + blueprint);
            var equipped = inventory.GetAllEquipped().Single(x => x.BlueprintName == blueprint);
            Assert.AreSame(actor, equipped.GetPart<PhysicsPart>().Equipped);
            Assert.AreEqual(1, equipped.GetPart<StackerPart>()?.StackCount ?? 1);
            if (units == 1) Assert.AreSame(candidate, equipped);
            else { Assert.AreNotSame(candidate, equipped); Assert.AreEqual(units - 1, candidate.GetPart<StackerPart>().StackCount); }
            return equipped;
        }
        private static string Graph(SpawnRing3DIntegrationFixture f)
            => f.Zone.TileState.ToSaveString() + "|" + f.Zone.EntityVersion + "|" + string.Join("\n", f.Zone.GetReadOnlyEntities().Select(e => e.ID + "|" + e.BlueprintName + "|" + f.Zone.GetEntityPosition(e) + "|" + e.GetStatValue("Hitpoints") + "|" + string.Join(",", e.GetPart<InventoryPart>()?.GetAllEquipped().Select(i => i.ID).OrderBy(x => x) ?? Enumerable.Empty<string>())).OrderBy(x => x));
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
                    Assert.NotNull(mesh); foreach (var vertex in mesh.vertices)
                    { var p = renderer.transform.TransformPoint(vertex); if (!any) { result = new Bounds(p, Vector3.zero); any = true; } else result.Encapsulate(p); }
                }
                finally { if (owned && mesh != null) Object.DestroyImmediate(mesh); }
            }
            Assert.True(any); Assert.Greater(result.size.y, .4f); return result;
        }
        private static CameraRow Capture(Camera camera, Bounds bounds, Quaternion facing, string path)
        {
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; var position = camera.transform.position;
            var rotation = camera.transform.rotation; float size = camera.orthographicSize, aspect = camera.aspect; var rect = camera.rect;
            RenderTexture target = null; Texture2D pixels = null;
            try
            {
                target = new RenderTexture(720, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
                Assert.True(target.Create()); camera.targetTexture = target; camera.rect = new Rect(0,0,1,1); camera.aspect = 1;
                camera.orthographicSize = Mathf.Max(bounds.size.y, Mathf.Max(bounds.size.x, bounds.size.z)) * .8f;
                var centre = bounds.center; camera.transform.position = centre + facing * new Vector3(0, 2.8f, -6);
                camera.transform.LookAt(centre); camera.ResetProjectionMatrix();
                foreach (var corner in Corners(bounds))
                { var p = camera.WorldToViewportPoint(corner); Assert.That(p.x, Is.InRange(.03f,.97f)); Assert.That(p.y, Is.InRange(.03f,.97f)); Assert.Greater(p.z, 0); }
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                Assert.True(RenderPipeline.SupportsRenderRequest(camera, request)); RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target; pixels = new Texture2D(720,720,TextureFormat.RGBA32,false,false);
                pixels.ReadPixels(new Rect(0,0,720,720),0,0); pixels.Apply();
                Assert.Greater(pixels.GetPixels().Average(x => x.r+x.g+x.b), .03f, "A black frame is not an art inspection.");
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                return new CameraRow { size = camera.orthographicSize, position = camera.transform.position, rotation = camera.transform.eulerAngles, width = 720, height = 720 };
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.rect = rect; camera.aspect = aspect; camera.orthographicSize = size;
                camera.transform.SetPositionAndRotation(position,rotation); camera.ResetProjectionMatrix(); RenderTexture.active = oldActive;
                if (pixels != null) Object.DestroyImmediate(pixels); if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            }
        }
        private static IEnumerable<Vector3> Corners(Bounds b)
        { for (int x=0;x<2;x++) for (int y=0;y<2;y++) for (int z=0;z<2;z++) yield return new Vector3(x==0?b.min.x:b.max.x,y==0?b.min.y:b.max.y,z==0?b.min.z:b.max.z); }
        [Serializable] private sealed class GearRow { public string blueprint,id,model,mesh,palette; public int pieces; public string[] bones; }
        [Serializable] private sealed class CameraRow { public float size; public Vector3 position,rotation; public int width,height; }
        [Serializable] private sealed class FrameRow { public string image,clip; public float normalizedTime,yaw,width,height,depth; public CameraRow camera; public GearRow[] gear; }
        [Serializable] private sealed class Report { public string blueprint,owner,zone,bodyModel,canVerify,cannotVerify; public FrameRow[] frames; }
    }
}
#endif
