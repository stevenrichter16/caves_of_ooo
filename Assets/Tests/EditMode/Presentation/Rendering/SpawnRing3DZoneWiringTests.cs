using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class SpawnRing3DZoneWiringTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static FieldInfo Slot()
        {
            var field = typeof(ZoneRenderer).GetField("_spawnRing3DPresenter", Private);
            Assert.NotNull(field, "The ordinary ZoneRenderer has no native ring presentation dependency."); return field;
        }
        static object Invoke(ZoneRenderer renderer, string method, params object[] args)
        {
            try { return typeof(ZoneRenderer).GetMethod(method, Private).Invoke(renderer, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        [Test] public void OrdinaryRendererCreatesTheRingDependencyAlongsideVillageAndDestroysIt()
        {
            Slot(); var before = new HashSet<GameObject>(Resources.FindObjectsOfTypeAll<GameObject>());
            var oldCell = ZoneRenderHooks.CellDirtyCallback; var oldFull = ZoneRenderHooks.FullDirtyCallback;
            var root = new GameObject("owned zone wiring", typeof(Grid));
            try
            {
                var go = new GameObject("native renderer", typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(root.transform, false);
                var renderer = go.AddComponent<ZoneRenderer>();
                // EditMode does not invoke MonoBehaviour.Awake automatically.
                if (typeof(ZoneRenderer).GetField("_worldCursorRenderer", Private).GetValue(renderer) == null) Invoke(renderer, "Awake");
                Assert.NotNull(Slot().GetValue(renderer));
                var property = typeof(ZoneRenderer).GetProperty("SpawnRing3D"); Assert.NotNull(property);
                Assert.AreSame(Slot().GetValue(renderer), property.GetValue(renderer));
            }
            finally
            {
                Object.DestroyImmediate(root);
                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                    if (go != null && go.scene.IsValid() && !before.Contains(go) && go.transform.parent == null) Object.DestroyImmediate(go);
                ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull;
            }
        }
        [TestCase("HasVisibleAuthoredScene")]
        [TestCase("ClaimsAuthoredSceneCell")]
        [TestCase("IsAuthoredSceneEntity")]
        [TestCase("IsSourceSceneBody")]
        public void NativeRingPredicatesFollowReadinessAndReleaseOnPresentationOff(string predicate)
        {
            Slot();
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var go = new GameObject("owned ring predicate host"); go.SetActive(false);
                var renderer = go.AddComponent<ZoneRenderer>(); typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(renderer, f.Zone);
                Slot().SetValue(renderer, f.Presenter);
                try
                {
                    Func<bool> value = () => predicate == "HasVisibleAuthoredScene"
                        ? (bool)typeof(ZoneRenderer).GetProperty(predicate, Private).GetValue(renderer)
                        : (bool)Invoke(renderer, predicate, predicate == "ClaimsAuthoredSceneCell" ? new object[] { 40,12 } : new object[] { f.Player });
                    Assert.IsTrue(value()); f.Call("SetPresentationVisible", false); Assert.IsFalse(value());
                    f.Call("SetPresentationVisible", true); Assert.IsTrue(value());
                    typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(renderer, new Zone("unrelated")); Assert.IsFalse(value(), "A previous-zone presenter must not claim the replacement graph.");
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
        [Test] public void RepresentedFenWaterSuppressesOnlyThePermanentWaterOverlay()
        {
            Slot();
            using (var f = new SpawnRing3DIntegrationFixture(SpawnRing3DIntegrationFixture.Fen,legacyFen:true))
            {
                var go = new GameObject("owned ring water host"); go.SetActive(false); var renderer = go.AddComponent<ZoneRenderer>();
                typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(renderer, f.Zone); Slot().SetValue(renderer, f.Presenter);
                try
                {
                    var p = f.FreeCell(); f.Zone.TileState.RemoveCoating(p.x,p.y,"oil");
                    f.Zone.TileState.WriteCoating(p.x,p.y,"water",ZoneTileState.Permanent); f.Refresh();
                    Func<bool> covered = () => (bool)Invoke(renderer,"IsAuthoredRiverWater",p.x,p.y,f.Zone.GetCell(p.x,p.y),f.Zone.TileState.Get(p.x,p.y));
                    Assert.IsTrue(covered()); f.Zone.TileState.WriteCoating(p.x,p.y,"oil",7); Assert.IsFalse(covered());
                    f.Zone.TileState.RemoveCoating(p.x,p.y,"oil"); f.Call("SetPresentationVisible",false); Assert.IsFalse(covered());
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
    }
}
