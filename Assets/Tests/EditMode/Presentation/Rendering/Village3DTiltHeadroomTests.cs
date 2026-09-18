using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class Village3DTiltHeadroomTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static CameraFollow Follow(Village3DIntegrationFixture f)
        {
            var follow=f.Source.gameObject.AddComponent<CameraFollow>();follow.Player=f.Player;follow.CurrentZone=f.Zone;
            typeof(CameraFollow).GetField("_village3DPresenter",Private).SetValue(follow,f.Presenter);
            follow.SnapToPlayer();Village3DIntegrationFixture.TickFrame(f.Presenter);return follow;
        }
        [TestCase(0,EntityVisualFacing.North)] [TestCase(0,EntityVisualFacing.East)]
        [TestCase(0,EntityVisualFacing.South)] [TestCase(0,EntityVisualFacing.West)]
        [TestCase(24,EntityVisualFacing.North)] [TestCase(24,EntityVisualFacing.East)]
        [TestCase(24,EntityVisualFacing.South)] [TestCase(24,EntityVisualFacing.West)]
        public void ImportedTownPlayerFitsBothBordersInEveryCardinalFacing(int row,EntityVisualFacing facing)
        {
            using(var f=new Village3DIntegrationFixture())
            {
                Assert.IsTrue(f.Zone.MoveEntity(f.Player,40,row));f.Refresh();var follow=Follow(f);
                f.Player.GetPart<RenderPart>().VisualFacing=facing;EntityVisualHooks.EmitAttack(f.Player,null,f.Zone);
                Assert.IsTrue(f.Presenter.TryGetOwnerView("$player",out var owner,out var root));Assert.AreSame(f.Player,owner);
                var points=SpawnRing3DTiltHeadroomTests.Vertices(root).Select(f.Presenter.WorldCamera.WorldToViewportPoint).ToArray();
                Assert.That(points.Max(p=>p.y),Is.LessThanOrEqualTo(.998f),"Town player head crosses the native viewport at56 degrees.");
                Assert.That(points.Min(p=>p.y),Is.GreaterThanOrEqualTo(.002f));
                Assert.AreEqual(13.75f,f.Source.orthographicSize,.0001f);Assert.AreEqual(12.75f,f.Source.transform.position.y,.0001f);
                Assert.AreEqual((40,row),f.Zone.GetEntityPosition(f.Player));Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);
                var ground=new Vector3(40.5f,0,24.5f-row);
                var worldCamera=f.Presenter.WorldCamera;var a=worldCamera.WorldToViewportPoint(ground);var b=f.Source.WorldToViewportPoint(new Vector3(ground.x,ground.z,0));
                var wp=worldCamera.projectionMatrix;var sp=f.Source.projectionMatrix;
                var wc=wp*worldCamera.worldToCameraMatrix*new Vector4(ground.x,ground.y,ground.z,1);var sc=sp*f.Source.worldToCameraMatrix*new Vector4(ground.x,ground.z,0,1);
                string details=$"worldClip={wc.ToString("R")} sourceClip={sc.ToString("R")} delta={(a-b).ToString("R")} world={a.ToString("R")} source={b.ToString("R")} worldOrtho={worldCamera.orthographicSize:R} sourceOrtho={f.Source.orthographicSize:R} worldAspect={worldCamera.aspect:R} sourceAspect={f.Source.aspect:R} worldPixelRect={worldCamera.pixelRect.ToString("R")} sourcePixelRect={f.Source.pixelRect.ToString("R")} worldMatrix={wp.m00:R},{wp.m11:R},{wp.m03:R},{wp.m13:R} sourceMatrix={sp.m00:R},{sp.m11:R},{sp.m03:R},{sp.m13:R} worldPos={worldCamera.transform.position.ToString("R")} sourcePos={f.Source.transform.position.ToString("R")} worldForward={worldCamera.transform.forward.ToString("R")}";
                // CameraFollow letterboxes to a fractional pixel height, while
                // the owned render target must use integer dimensions. The direct
                // clip-space registration is strict; Unity's viewport helper may
                // normalize by that separately rounded pixel rectangle.
                var worldClip=new Vector2(wc.x/wc.w,wc.y/wc.w);var sourceClip=new Vector2(sc.x/sc.w,sc.y/sc.w);
                Assert.Less(Vector2.Distance(worldClip,sourceClip),.00001f,details);
                var pixelDelta=Vector2.Scale(a-b,new Vector2(worldCamera.pixelWidth,worldCamera.pixelHeight));
                Assert.Less(Mathf.Abs(pixelDelta.x),.5f,details);Assert.Less(Mathf.Abs(pixelDelta.y),.5f,details);
            }
        }
        [Test]
        public void DisablingTown3DRetainsFlatMorrowfastFraming()
        {
            using(var f=new Village3DIntegrationFixture())
            {
                var flat=f.Root.AddComponent<MorrowfastScenePresenter>();flat.Bind(f.Zone);Assert.IsTrue(flat.PresentationVisible);
                var follow=Follow(f);typeof(CameraFollow).GetField("_morrowfastScenePresenter",Private).SetValue(follow,flat);
                Village3DSettings.Enabled=false;Village3DIntegrationFixture.TickFrame(f.Presenter);follow.SnapToPlayer();
                Assert.IsFalse(f.Presenter.PresentationVisible);Assert.IsTrue(flat.PresentationVisible);
                Assert.AreEqual(MorrowfastScenePresenter.CameraHalfHeight(f.Source.aspect),f.Source.orthographicSize,.0001f);
                Assert.AreEqual(12.75f,f.Source.transform.position.y,.0001f);Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);
            }
        }
    }
}
