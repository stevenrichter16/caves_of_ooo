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
    /// <summary>Staged native-cave art capture. Inventory removal and pose sampling
    /// are disclosed diagnostics, not an ordinary-input medicine encounter.</summary>
    public sealed class PatchbearerArtGalleryTests
    {
        [TestCase(true)][TestCase(false)]
        public void CaptureActualHarnessStatesAndOriginalNativeMotion(bool medicineCarried)
        {
            string zoneId;
            using(var content = new EntityEquipmentContentFixture())
            {
                var manager = new OverworldZoneManager(content.Factory, 729490642);
                var column = BiomeCropPlan.CaveColumns(manager).Select(WorldMap.FromZoneID).First(p => manager.WorldMap.GetBiome(p.x,p.y) == BiomeType.Spread);
                zoneId = WorldMap.ToZoneID(column.x,column.y,3);
            }
            using(var f = new SpawnRing3DIntegrationFixture(zoneId))
            {
                var actor = f.Add("MarlbackPatchbearer");
                var inventory = actor.GetPart<InventoryPart>();
                Assert.AreEqual(1,inventory.Objects.Count(e=>e.BlueprintName=="HealingTonic"));
                if(!medicineCarried)Assert.True(inventory.RemoveObject(inventory.Objects.Single(e=>e.BlueprintName=="HealingTonic")));
                // Pick an open interior diagnostic floor. No tile, wall, actor
                // statistics, loadout or encounter success is rewritten for art.
                bool moved=false;
                for(int y=3;y<Zone.Height-3&&!moved;y++)for(int x=3;x<Zone.Width-3&&!moved;x++)
                {
                    bool clear=true;
                    for(int dy=-2;dy<=2&&clear;dy++)for(int dx=-2;dx<=2;dx++)
                        if(f.Zone.GetCell(x+dx,y+dy).Objects.Any(e=>e!=actor&&e.GetPart<PhysicsPart>()?.Solid==true))clear=false;
                    if(clear)moved=f.Zone.MoveEntity(actor,x,y);
                }
                Assert.True(moved,"Native cave lacks an open staging floor.");
                f.Set("FullReveal",true);f.Refresh();
                var presenter=(SpawnRing3DPresenter)f.Presenter;var root=f.View(actor);
                Assert.True(presenter.TryGetApprovedStyle(actor,out var proof),proof.Failure);
                Assert.AreEqual(medicineCarried?"patchbearer-full":"patchbearer-empty",proof.ModelId);
                var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s=>s.sharedMesh==proof.SubmittedMesh);
                var animator=root.GetComponentInChildren<Animator>();Assert.NotNull(animator);
                var vertices=body.sharedMesh.vertices;var bindposes=body.sharedMesh.bindposes;
                string graph=Graph(f),carried=string.Join(",",inventory.Objects.Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1)));
                int bus=EquipmentChangeBus.GlobalVersion;var borrowed=f.Source.targetTexture;
                string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/EnemyFieldMedicine/ArtGallery",proof.ModelId+"-"+Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(path);var rows=new List<FrameRow>();
                foreach(var pose in new[]{("Idle",0f,0f),("Idle",0f,90f),("Idle",0f,180f),("Walk",.31f,35f),("Interact",.31f,35f),("Attack",.31f,35f)})
                {
                    var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==pose.Item1);
                    clip.SampleAnimation(animator.gameObject,clip.length*pose.Item2);
                    Assert.True(presenter.TryGetApprovedStyle(actor,out _));
                    var bounds=VisibleBounds(root);string file=rows.Count.ToString("00")+"-"+pose.Item1+"-"+pose.Item3+".png";
                    var camera=Capture(presenter.WorldCamera,bounds,root.transform.rotation*Quaternion.Euler(0,pose.Item3,0),Path.Combine(path,file));
                    rows.Add(new FrameRow{image=file,clip=clip.name,normalizedTime=pose.Item2,yaw=pose.Item3,width=bounds.size.x,height=bounds.size.y,depth=bounds.size.z,camera=camera,gear=Array.Empty<GearRow>()});
                    Assert.AreEqual(graph,Graph(f));Assert.AreEqual(carried,string.Join(",",inventory.Objects.Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1))));
                    Assert.AreEqual(bus,EquipmentChangeBus.GlobalVersion);Assert.AreSame(borrowed,f.Source.targetTexture);
                    CollectionAssert.AreEqual(vertices,body.sharedMesh.vertices);CollectionAssert.AreEqual(bindposes,body.sharedMesh.bindposes);
                }
                File.WriteAllText(Path.Combine(path,"report.json"),JsonUtility.ToJson(new Report{blueprint=actor.BlueprintName,owner=actor.ID,zone=f.Zone.ZoneID,bodyModel=proof.ModelId,medicineCarried=medicineCarried,frames=rows.ToArray(),
                    canVerify="Actual factory actor/native gear in a generated ordinary Spread cave; exact full/empty adopted mesh and owned palette; five original animation clips preserved, selected poses sampled; graph, inventory identities/counts and source buffers unchanged during capture; actual PNG output.",
                    cannotVerify="Explicit staged art, not natural encounter acquisition, scheduled drinking, live gesture transitions, action cost or combat balance. Empty case removes its inventory tonic before observation and actor is moved onto an existing clear floor. Camera is a close-up with full reveal. Images require independent review for contact, readability, clipping and cave occlusion."},true));
                Assert.AreEqual(6,rows.Count);
            }
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
            Assert.True(any); Assert.Greater(result.size.y, .2f); return result;
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
        [Serializable] private sealed class Report { public string blueprint,owner,zone,bodyModel,canVerify,cannotVerify; public bool medicineCarried; public FrameRow[] frames; }
    }
}
#endif
