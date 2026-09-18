using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Native capture exposed the north-row player's head clipping
    /// after the initial81-degree camera change; the final angle is56. Exercise actual imported actor vertices,
    /// not a cell-center or a synthetic tall-box substitute.</summary>
    public sealed class SpawnRing3DTiltHeadroomTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static CameraFollow Follow(SpawnRing3DIntegrationFixture f)
        {
            var follow=f.Source.gameObject.AddComponent<CameraFollow>();
            follow.Player=f.Player;follow.CurrentZone=f.Zone;
            typeof(CameraFollow).GetField("_spawnRing3DPresenter",Private).SetValue(follow,f.Presenter);
            follow.SnapToPlayer();f.Frame();return follow;
        }
        internal static List<Vector3> Vertices(GameObject root)
        {
            var points=new List<Vector3>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                if(filter.GetComponent<Renderer>()?.enabled==true)
                    points.AddRange(filter.sharedMesh.vertices.Select(filter.transform.TransformPoint));
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!skin.enabled)continue;
                var mesh=new Mesh();
                try{skin.BakeMesh(mesh);points.AddRange(mesh.vertices.Select(skin.transform.TransformPoint));}
                finally{Object.DestroyImmediate(mesh);}
            }
            Assert.Greater(points.Count,100,"Actual imported actor geometry is required.");
            return points;
        }
        [TestCase(0,34,false)] [TestCase(24,34,false)]
        [TestCase(0,20,false)] [TestCase(24,20,false)] [TestCase(0,20,true)] [TestCase(24,20,true)]
        public void ImportedPlayerFitsAtNorthAndSouthRowsAfterTilt(int nativeY,int rows,bool lookOverride)
        {
            using(var f=new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                Assert.IsTrue(f.Zone.MoveEntity(f.Player,40,nativeY));f.Refresh();
                var follow=Follow(f);follow.TargetVisibleTileRows=rows;
                if(lookOverride)
                {
                    var tracked=MultiCellSpatialTests.Body(null,false);Assert.IsTrue(f.Zone.AddEntity(tracked,40,12));
                    follow.Player=tracked;follow.SnapToPlayer();f.Frame();Assert.AreEqual(12.5f,f.Source.transform.position.y,.0001f);
                    follow.SetOverrideTargetCell(40,nativeY);Assert.IsTrue(follow.HasOverrideTarget);
                    Assert.AreEqual((40,12),f.Zone.GetEntityPosition(tracked));
                }
                follow.SnapToPlayer();f.Frame();Assert.IsTrue(f.Find(f.Player,out var root,out string model));
                Assert.AreEqual("ring-player",model);var points=Vertices(root);
                var world=f.Get<Camera>("WorldCamera");
                var projected=points.Select(world.WorldToViewportPoint).ToArray();
                Assert.That(projected.Max(p=>p.y),Is.LessThanOrEqualTo(.998f),"Tilted player head exceeds the map viewport.");
                Assert.That(projected.Min(p=>p.y),Is.GreaterThanOrEqualTo(.002f),"Player body exceeds the lower map viewport.");
                Assert.That(projected.Min(p=>p.x),Is.GreaterThan(0));Assert.That(projected.Max(p=>p.x),Is.LessThan(1));
                Assert.AreEqual((40,nativeY),f.Zone.GetEntityPosition(f.Player));
                float halfHeight=Mathf.Min(14f,rows*.5f);
                Assert.AreEqual(halfHeight,f.Source.orthographicSize,.0001f);
                Assert.AreEqual(rows>=28?12.5f:nativeY==0?26.5f-halfHeight:halfHeight-1.5f,f.Source.transform.position.y,.0001f);
                var ground=new Vector3(40.5f,0,24.5f-nativeY);
                Assert.Less(Vector2.Distance(world.WorldToViewportPoint(ground),f.Source.WorldToViewportPoint(new Vector3(ground.x,ground.z,0))),.0001f);
                Assert.AreSame(f.Borrowed,f.Source.targetTexture);
            }
        }
        [Test]
        public void DisabledRingRestoresOrdinaryFramingWithoutMovingPlayer()
        {
            using(var f=new SpawnRing3DIntegrationFixture(MultiCellPilotRuntime.ZoneID))
            {
                Assert.IsTrue(f.Zone.MoveEntity(f.Player,40,0));f.Refresh();var follow=Follow(f);
                var rect=f.Source.rect;Village3DSettings.Enabled=false;f.Frame();follow.SnapToPlayer();
                Assert.IsFalse(f.Get<bool>("PresentationVisible"));Assert.AreEqual(follow.TargetVisibleTileRows*.5f,f.Source.orthographicSize);
                Assert.AreEqual((40,0),f.Zone.GetEntityPosition(f.Player));Assert.AreEqual(rect,f.Source.rect);
                Assert.AreSame(f.Borrowed,f.Source.targetTexture);
            }
        }
    }
}
