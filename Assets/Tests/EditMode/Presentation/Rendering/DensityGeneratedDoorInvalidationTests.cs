using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    internal sealed class GeneratedDoorInvalidationFixture : IDisposable
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        readonly EntityEquipmentContentFixture scope=new EntityEquipmentContentFixture();
        readonly GameObject root;
        public readonly Zone Zone;
        public readonly Entity Player,Door,Torch;
        public readonly ZoneRenderer Renderer;
        public Cell Probe=>Zone.GetCell(34,4);
        public LightMap CachedLight=>(LightMap)typeof(ZoneRenderer).GetField("_lightMap",Private).GetValue(Renderer);
        public GeneratedDoorInvalidationFixture()
        {
            try
            {
                Zone=new Zone("Overworld.4.6.0");
                for(int x=0;x<Zone.Width;x++)if(x!=34)Assert.True(Zone.AddEntity(scope.Factory.CreateEntity("StoneWall"),x,5));
                Player=scope.Factory.CreateEntity("Player");Assert.True(Zone.AddEntity(Player,34,6));
                Door=scope.Factory.CreateEntity("VillageDoor");Assert.True(Zone.AddEntity(Door,34,5));
                Assert.True(Door.GetPart<DoorPart>().TrySetOpen(Player,Zone,false));
                Zone.AmbientLevel=.05f;
                Torch=scope.Factory.CreateEntity("Torch");Assert.NotNull(Torch.GetPart<LightSourcePart>());
                Assert.IsTrue(Zone.AddEntity(Torch,34,6));
                root=new GameObject("Stationary native door render fixture");root.SetActive(false);
                root.AddComponent<Grid>();var tiles=root.AddComponent<Tilemap>();root.AddComponent<TilemapRenderer>();
                var cameraObject=new GameObject("Owned inactive source camera");cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.aspect=16f/9;camera.orthographicSize=17;camera.transform.position=new Vector3(40,12.5f,-10);
                // Deliberately inactive: do not let Awake create unrelated rendering/UI resources.
                Renderer=root.AddComponent<ZoneRenderer>();
                typeof(ZoneRenderer).GetField("_tilemap",Private).SetValue(Renderer,tiles);
                typeof(ZoneRenderer).GetField("_mainCamera",Private).SetValue(Renderer,camera);
                Renderer.PlayerEntity=Player;Renderer.SetZone(Zone);
                ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty;
                ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;
                Frame();
                Assert.IsFalse(Probe.IsVisible,"Real closed door must occlude this verified interior cell.");
                Assert.IsTrue(Zone.GetCell(34,5).IsWall());
            }
            catch{Dispose();throw;}
        }
        public void Frame()=>typeof(ZoneRenderer).GetMethod("LateUpdate",Private).Invoke(Renderer,null);
        public void SetOpen(bool open)
        {Assert.IsTrue(Door.GetPart<DoorPart>().TrySetOpen(Player,Zone,open));Assert.AreEqual((34,6),Zone.GetEntityPosition(Player));Assert.AreEqual((34,6),Zone.GetEntityPosition(Torch));Frame();}
        public bool FreshFov()
        {FieldOfView.Compute(Zone,34,6,Renderer.FovRadius);return Probe.IsVisible;}
        public float FreshLight()
        {var fresh=new LightMap();fresh.Compute(Zone);return fresh.GetBrightness(34,4);}
        public void Dispose(){if(root!=null)Object.DestroyImmediate(root);scope.Dispose();}
    }
    public sealed class DensityGeneratedDoorInvalidationTests
    {
        [Test] public void StationaryNativeOpenAndCloseRefreshActualRenderedFov()
        {
            using(var f=new GeneratedDoorInvalidationFixture())
            {
                f.SetOpen(true);bool renderedOpen=f.Probe.IsVisible;
                bool freshOpen=f.FreshFov();Assert.IsTrue(freshOpen,"Flipped native door must make this interior visible in fresh control.");
                Assert.AreEqual(freshOpen,renderedOpen,"Incremental render retained closed-door FOV while player stayed still.");
                f.SetOpen(false);bool renderedClosed=f.Probe.IsVisible;
                bool freshClosed=f.FreshFov();Assert.IsFalse(freshClosed,"Closed-door countercheck must occlude again.");
                Assert.AreEqual(freshClosed,renderedClosed,"Closing must restore occlusion without requiring a player move.");
            }
        }
        [Test] public void StationaryNativeOpenAndCloseRefreshExistingGameplayLightCache()
        {
            using(var f=new GeneratedDoorInvalidationFixture())
            {
                float closed=f.CachedLight.GetBrightness(34,4);f.SetOpen(true);
                float observedOpen=f.CachedLight.GetBrightness(34,4),freshOpen=f.FreshLight();
                Assert.Greater(freshOpen,closed+.1f,"Actual Torch must illuminate through the opened door in fresh control.");
                Assert.AreEqual(freshOpen,observedOpen,.0001f,"Compute alone may retain an old cache because native door state is outside its key.");
                f.SetOpen(false);float observedClosed=f.CachedLight.GetBrightness(34,4),freshClosed=f.FreshLight();
                Assert.Less(freshClosed,freshOpen-.1f);
                Assert.AreEqual(freshClosed,observedClosed,.0001f,"Closing must stop existing cached light too.");
            }
        }
    }
}
