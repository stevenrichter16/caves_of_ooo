#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    /// <summary>Explicitly staged native art. Real authored gear and imported
    /// clips; direct canonical death is diagnostic, not a player combat claim.</summary>
    public sealed class SpreadRareMarlbackGalleryTests
    {
        [TestCase("SpreadHurdleCutter")]
        [TestCase("SpreadDitchMate")]
        public void CapturePersistentBodyAuthoredGearAndCanonicalDeath(string blueprint)
        {
            using (var isolation = new GalleryIsolation())
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                CorpsePart.Factory = f.Factory;
                var actor = f.Add(blueprint);
                // Staged art only: relocate the unrelated fixture player away
                // before the observed graph snapshot, so it cannot hide side views.
                var subjectCell = f.Zone.GetEntityCell(actor); Cell playerCell = null;
                for (int y = 1; y < 24 && playerCell == null; y++)
                    for (int x = 1; x < 79 && playerCell == null; x++)
                    {
                        var cell = f.Zone.GetCell(x, y);
                        if (Math.Max(Math.Abs(x - subjectCell.X), Math.Abs(y - subjectCell.Y)) >= 12
                            && f.Zone.CanPlaceFootprint(f.Player, x, y)
                            && !cell.Occupants.Any(e => e != f.Player && e.HasTag("Creature"))) playerCell = cell;
                    }
                Assert.NotNull(playerCell); Assert.True(f.Zone.MoveEntity(f.Player, playerCell.X, playerCell.Y));
                var items = actor.GetPart<InventoryPart>().GetAllEquipped().ToArray();
                CollectionAssert.AreEquivalent(blueprint == "SpreadHurdleCutter" ? new[] { "ShortSword", "LeatherCap" } : new[] { "Cudgel" }, items.Select(i => i.BlueprintName));
                var corpseConfig = actor.GetPart<CorpsePart>(); Assert.NotNull(corpseConfig);
                Assert.AreEqual("MarlbackCorpse", corpseConfig.CorpseBlueprint);
                Assert.AreEqual(70, corpseConfig.CorpseChance); Assert.AreEqual(100, corpseConfig.BuildCorpseChance);
                f.Set("FullReveal", true); f.Refresh();
                var presenter = (SpawnRing3DPresenter)f.Presenter; var root = f.View(actor);
                Assert.True(presenter.TryGetApprovedStyle(actor, out var bodyProof), bodyProof.Failure);
                Assert.AreEqual(blueprint == "SpreadHurdleCutter" ? "spread-rare-hurdle-cutter" : "spread-rare-ditch-mate", bodyProof.ModelId);
                var skin = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x => x.sharedMesh == bodyProof.SubmittedMesh);
                var animator = root.GetComponentInChildren<Animator>(); Assert.NotNull(animator);
                var sourceVertices = skin.sharedMesh.vertices; var sourceBindposes = skin.sharedMesh.bindposes;
                string graph = Graph(f); int bus = EquipmentChangeBus.GlobalVersion; var borrowed = f.Source.targetTexture;
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/SpreadFirstHour/E1Art/NativeGallery", blueprint + "-" + Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(path); var rows = new List<FrameRow>(); var camera = presenter.WorldCamera;
                foreach (var pose in new[] { ("Idle", 0f, 0f), ("Idle", 0f, 90f), ("Idle", 0f, 180f), ("Idle", 0f, 270f),
                    ("Walk", .31f, 35f), ("Attack", .31f, 35f), ("Hit", .31f, 35f), ("Interact", .31f, 35f) })
                {
                    var clip = animator.runtimeAnimatorController.animationClips.Single(x => x.name == pose.Item1);
                    clip.SampleAnimation(animator.gameObject, clip.length * pose.Item2);
                    Assert.True(presenter.TryGetApprovedStyle(actor, out _));
                    var gear = new List<GearRow>();
                    foreach (var item in items)
                    {
                        Assert.AreSame(actor, item.GetPart<PhysicsPart>().Equipped);
                        Assert.True(presenter.TryGetApprovedEquipmentStyle(actor, item, out var proof), item.BlueprintName + ": " + proof.Failure);
                        Assert.True(presenter.TryGetEquipmentView(actor, item, out var view));
                        gear.Add(new GearRow { blueprint = item.BlueprintName, id = item.ID, model = proof.ModelId,
                            pieces = view.GetComponentsInChildren<Renderer>(true).Length, mesh = proof.ExpectedMesh.name,
                            palette = proof.ExpectedMaterial.GetTexture("_BaseMap").name });
                    }
                    var bounds = VisibleBounds(root); string filename = rows.Count.ToString("00") + "-" + pose.Item1 + "-" + pose.Item3 + ".png";
                    var settings = Capture(camera, bounds, root.transform.rotation * Quaternion.Euler(0, pose.Item3, 0), Path.Combine(path, filename));
                    rows.Add(new FrameRow { image = filename, clip = clip.name, normalizedTime = pose.Item2, yaw = pose.Item3,
                        width = bounds.size.x, height = bounds.size.y, depth = bounds.size.z, camera = settings, gear = gear.ToArray() });
                    Assert.AreEqual(graph, Graph(f)); Assert.AreEqual(bus, EquipmentChangeBus.GlobalVersion);
                    Assert.AreSame(borrowed, f.Source.targetTexture); CollectionAssert.AreEqual(sourceVertices, skin.sharedMesh.vertices);
                    CollectionAssert.AreEqual(sourceBindposes, skin.sharedMesh.bindposes);
                }
                var deathCell = f.Zone.GetEntityPosition(actor);
                // Explicit staged canonical death, with no invented corpse or item.
                // This tests render ownership/drop lifecycle, not survivable combat.
                // This art diagnostic chooses the successful edge of the real70% gate.
                // It does not change the authored chance or claim ordinary RNG acquisition.
                corpseConfig.TestRng = new CorpseBoundaryRoll(69);
                CombatSystem.HandleDeath(actor, null, f.Zone);
                Assert.True(CombatSystem.IsDeathHandled(actor)); Assert.IsNull(f.Zone.GetEntityCell(actor));
                var corpse = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "MarlbackCorpse"
                    && e.Properties.TryGetValue("SourceID", out var sourceId) && sourceId == actor.ID);
                Assert.AreEqual(actor.BlueprintName, corpse.GetProperty("SourceBlueprint"));
                Assert.AreEqual(deathCell, f.Zone.GetEntityPosition(corpse));
                foreach (var item in items)
                {
                    Assert.AreEqual(deathCell, f.Zone.GetEntityPosition(item));
                    Assert.IsNull(item.GetPart<PhysicsPart>().InInventory); Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
                }
                f.Refresh(); Assert.False(f.Find(actor, out _, out _));
                Assert.True(presenter.TryGetApprovedStyle(corpse, out var corpseProof), corpseProof.Failure);
                Assert.AreEqual("spread-portable-marlbackcorpse", corpseProof.ModelId);
                var remains = VisibleBounds(f.View(corpse));
                foreach (var item in items)
                { Assert.True(presenter.TryGetApprovedStyle(item, out var dropProof), dropProof.Failure); remains.Encapsulate(VisibleBounds(f.View(item))); }
                string deathImage = "08-canonical-death-corpse-and-original-gear.png";
                Capture(camera, remains, Quaternion.Euler(0, 35, 0), Path.Combine(path, deathImage));
                File.WriteAllText(Path.Combine(path, "report.json"), JsonUtility.ToJson(new Report {
                    blueprint = blueprint, owner = actor.ID, zone = f.Zone.ZoneID, bodyModel = bodyProof.ModelId,
                    corpse = corpse.ID, corpseModel = corpseProof.ModelId, corpseChance = corpseConfig.CorpseChance, diagnosticCorpseRoll = 69, originalGear = items.Select(i => i.ID).ToArray(), deathImage = deathImage, frames = rows.ToArray(),
                    canVerify = "Explicit staged factory owners with exact authored gear; actual persistent mesh/owned palette; original five imported clips sampled; tile state, entity version, existing IDs/positions/HP/equipped IDs unchanged during pose capture; canonical death removes exact owner and produces source-linked corpse plus original gear; actual PNG output.",
                    cannotVerify = "Not encounter placement, ordinary acquisition, scheduling, player attacks or balance. Direct death, sampled poses and explicit instance corpse roll69 below the unchanged70% authored chance are diagnostic. The unrelated fixture player is explicitly repositioned before the unchanged capture snapshot. Camera is a padded close-up; numeric success alone does not prove fit, silhouette/readability, clip quality or lack of world occlusion. Images require review." }, true));
                Assert.AreEqual(8, rows.Count);
            }
        }
        [Test]
        public void ActualSeventyPercentBoundaryCanLeaveOnlyOriginalGear()
        {
            using (var isolation = new GalleryIsolation())
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                CorpsePart.Factory = f.Factory;
                var actor = f.Add("SpreadHurdleCutter"); var config = actor.GetPart<CorpsePart>();
                Assert.AreEqual(70, config.CorpseChance); Assert.AreEqual(100, config.BuildCorpseChance);
                var items = actor.GetPart<InventoryPart>().GetAllEquipped().ToArray();
                CollectionAssert.AreEquivalent(new[] { "ShortSword", "LeatherCap" }, items.Select(i => i.BlueprintName));
                var cell = f.Zone.GetEntityPosition(actor); config.TestRng = new CorpseBoundaryRoll(70);
                CombatSystem.HandleDeath(actor, null, f.Zone);
                Assert.True(CombatSystem.IsDeathHandled(actor)); Assert.IsNull(f.Zone.GetEntityCell(actor));
                Assert.False(f.Zone.GetReadOnlyEntities().Any(e => e.Properties.TryGetValue("SourceID", out var source) && source == actor.ID));
                foreach (var item in items)
                {
                    Assert.AreEqual(cell, f.Zone.GetEntityPosition(item));
                    Assert.IsNull(item.GetPart<PhysicsPart>().InInventory); Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
                }
                Assert.AreEqual(70, config.CorpseChance);
            }
        }
        private sealed class CorpseBoundaryRoll : System.Random
        {
            readonly int roll;
            internal CorpseBoundaryRoll(int roll) { this.roll = roll; }
            public override int Next(int maxValue) { Assert.AreEqual(100, maxValue); return roll; }
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
            Assert.True(any); Assert.Greater(result.size.y, .01f); return result;
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
                camera.orthographicSize = Mathf.Max(bounds.size.y, Mathf.Max(bounds.size.x, bounds.size.z)) * 1.35f;
                var centre = bounds.center; camera.transform.position = centre + facing * new Vector3(0, 2.8f, -6);
                camera.transform.LookAt(centre); camera.ResetProjectionMatrix();
                foreach (var corner in Corners(bounds))
                { var p = camera.WorldToViewportPoint(corner); Assert.That(p.x, Is.InRange(.15f,.85f)); Assert.That(p.y, Is.InRange(.15f,.85f)); Assert.Greater(p.z, 0); }
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
        [Serializable] private sealed class GearRow { public string blueprint,id,model,mesh,palette; public int pieces; }
        [Serializable] private sealed class CameraRow { public float size; public Vector3 position,rotation; public int width,height; }
        [Serializable] private sealed class FrameRow { public string image,clip; public float normalizedTime,yaw,width,height,depth; public CameraRow camera; public GearRow[] gear; }
        [Serializable] private sealed class Report { public string blueprint,owner,zone,bodyModel,corpse,corpseModel,deathImage,canVerify,cannotVerify; public int corpseChance,diagnosticCorpseRoll; public string[] originalGear; public FrameRow[] frames; }
        private sealed class GalleryIsolation : IDisposable
        {
            const BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
            readonly Queue<AsciiFxRequest> pending = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", Private).GetValue(null);
            readonly Stack<AsciiFxRequest> pool = (Stack<AsciiFxRequest>)typeof(AsciiFxBus).GetField("Pool", Private).GetValue(null);
            readonly FieldInfo clearField = typeof(AsciiFxBus).GetField("<ClearVersion>k__BackingField", Private);
            readonly AsciiFxRequest[] priorPending, priorPool;
            readonly int clear, flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly EntityFactory corpseFactory = CorpsePart.Factory;
            public GalleryIsolation()
            { priorPending = pending.ToArray(); priorPool = pool.ToArray(); clear = (int)clearField.GetValue(null); pending.Clear(); pool.Clear(); }
            public void Dispose()
            {
                pending.Clear(); pool.Clear(); foreach (var request in priorPending) pending.Enqueue(request);
                for (int i = priorPool.Length - 1; i >= 0; i--) pool.Push(priorPool[i]); clearField.SetValue(null, clear);
                MessageLog.Restore(messages, announcements, flash, serial); CorpsePart.Factory = corpseFactory;
            }
        }
    }
}
#endif
