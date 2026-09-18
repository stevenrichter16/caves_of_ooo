using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>A stationary player watches another native actor walk with a
    /// glowing weapon. The next ring frame must use the actor's current light
    /// position, while a nonluminous weapon is the identical-movement control.
    /// Actual native movement/equip and imported ring resources; no GPU claim.</summary>
    public sealed class SpawnRing3DMovingLightTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [TestCase(true,"cached-map")][TestCase(true,"uploaded-fog")]
        [TestCase(false,"cached-map")][TestCase(false,"uploaded-fog")]
        public void NativeNonplayerMoveUpdatesRingIlluminationWithoutPlayerMovement(bool luminous,string oracle)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.2.5.0"))
            {
                f.Zone.AmbientLevel=.05f;
                var lane=FindNativeLane(f);var actor=f.Add("CaveHermit",lane.x,lane.y);
                f.CleanGear(actor);var weapon=f.Equip(actor,luminous?"FlamingSword":"Dagger");
                Assert.AreEqual(luminous,weapon.GetPart<LightSourcePart>()!=null);
                Assert.IsNull(actor.GetPart<LightSourcePart>(),"The actual weapon must be the only fixture light.");
                var equipped=actor.GetPart<InventoryPart>().GetAllEquipped().ToArray();
                Assert.IsTrue(equipped.Contains(weapon));
                Assert.IsTrue(actor.GetPart<InventoryPart>().EquippedItems.Values.Contains(weapon),
                    "The equipped-light scan must contain the exact native weapon in its shipped equipment mirror.");
                using(var host=new LightHost(f))
                {
                    host.Frame();Assert.IsTrue(f.Get<bool>("PresentationVisible"));
                    var playerAt=f.Zone.GetEntityPosition(f.Player);int tick=TurnManager.Active?.TickCount??0;
                    int builds=f.Get<int>("GroundBuildCount");
                    var before=new LightMap();before.Compute(f.Zone);
                    var beforeMask=NativeMask(f.Zone,before);var uploadedBefore=host.FogPixels(f);
                    Assert.IsFalse(host.FullDirty);
                    var moved=MovementSystem.TryMoveEx(actor,f.Zone,1,0);
                    Assert.IsTrue(moved.moved,"Real voluntary native actor movement must succeed without fixture teleport or synthetic dirty calls.");
                    Assert.IsNull(moved.blockedBy);
                    Assert.AreEqual((lane.x+1,lane.y),f.Zone.GetEntityPosition(actor));
                    Assert.IsFalse(host.FullDirty,"A nonplayer step must enter the existing incremental branch.");
                    Assert.AreEqual(2,host.DirtyCount,"The movement system dirties only the old and new cells.");
                    var fresh=new LightMap();fresh.Compute(f.Zone);
                    var freshMask=NativeMask(f.Zone,fresh);
                    // Generated lamps may saturate the carrier's original cell.
                    // Pick the strongest real light change across the complete
                    // native mask, requiring an actual 8-bit upload difference.
                    var probe=lane;float changedBrightness=0f;float maximumBrightnessDelta=0f;
                    for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                    {
                        float delta=Mathf.Abs(before.GetBrightness(x,y)-fresh.GetBrightness(x,y));
                        maximumBrightnessDelta=Mathf.Max(maximumBrightnessDelta,delta);
                        int index=(Zone.Height-1-y)*Zone.Width+x;
                        if(delta>changedBrightness&&!beforeMask[index].Equals(freshMask[index]))
                        {changedBrightness=delta;probe=(x,y);}
                    }
                    int probeIndex=(Zone.Height-1-probe.y)*Zone.Width+probe.x;
                    Color32 beforePixel=beforeMask[probeIndex],expected=freshMask[probeIndex];
                    Assert.AreEqual(before.GetBrightness(probe.x,probe.y),host.Light.GetBrightness(probe.x,probe.y),.0001f,
                        "The current renderer cache must match the captured native before state at the selected probe.");
                    Assert.AreEqual(beforePixel,uploadedBefore[probeIndex],"Actual mask upload positive control captured before movement.");
                    if(luminous)
                    {
                        var source=weapon.GetPart<LightSourcePart>();
                        Assert.Greater(changedBrightness,.05f,
                            "Actual FlamingSword movement must change native light and its quantized mask somewhere. "
                            +"Maximum brightness delta="+maximumBrightnessDelta+", selected="+probe
                            +", ambient="+f.Zone.AmbientLevel+", radius="+source.Radius+", intensity="+source.Intensity);
                        Assert.AreNotEqual(beforePixel,expected,"8-bit fog upload must have a nonvacuous changed-light expectation.");
                    }
                    else
                    {
                        Assert.LessOrEqual(maximumBrightnessDelta,.0001f,"Identical Dagger movement must not change light anywhere in the native zone.");
                        CollectionAssert.AreEqual(beforeMask,freshMask,"Identical Dagger movement must not invent any native mask change.");
                    }
                    host.Frame();
                    if(oracle=="cached-map")
                        Assert.AreEqual(fresh.GetBrightness(probe.x,probe.y),host.Light.GetBrightness(probe.x,probe.y),.0001f,
                            "The ring incremental frame passed a stale gameplay LightMap after actual NPC movement.");
                    else
                        Assert.AreEqual(expected,host.FogPixel(f,probe.x,probe.y),
                            "The actual ring material fog-light texture retained illumination from the carrier's old cell.");
                    Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"),"Lighting and actor movement must not rebuild static ground.");
                    Assert.AreEqual(playerAt,f.Zone.GetEntityPosition(f.Player));Assert.AreEqual(tick,TurnManager.Active?.TickCount??0);
                    Assert.IsTrue(InventorySystem.IsEquipped(actor,weapon));Assert.IsNull(f.Zone.GetEntityCell(weapon));
                }
            }
        }
        static Color32[] NativeMask(Zone zone,LightMap light)
        {
            var pixels=new Color32[Zone.Width*Zone.Height];
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                pixels[(Zone.Height-1-y)*Zone.Width+x]=Village3DVisibility.SampleCell(zone.GetCell(x,y),light,false);
            return pixels;
        }
        static (int x,int y) FindNativeLane(SpawnRing3DIntegrationFixture f)
        {
            for(int y=2;y<23;y++)for(int x=2;x<77;x++)
            {
                bool okay=true;
                for(int dx=0;dx<=1;dx++)
                {
                    var cell=f.Zone.GetCell(x+dx,y);var tile=f.Zone.TileState.Get(x+dx,y);
                    if(cell.BlocksMovement(f.Player)||cell.Objects.Any(e=>e.HasTag("Creature"))||(tile!=null&&!tile.IsEmpty))okay=false;
                }
                if(okay)return(x,y);
            }
            Assert.Fail("Actual generated northwest ring requires two adjacent unblocked uncoated native cells.");return(-1,-1);
        }
        sealed class LightHost:IDisposable
        {
            readonly GameObject root;
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            readonly ZoneRenderer renderer;
            public LightMap Light=>(LightMap)Field("_lightMap").GetValue(renderer);
            public bool FullDirty=>(bool)Field("_fullDirty").GetValue(renderer);
            public int DirtyCount=>((HashSet<int>)Field("_dirtyCells").GetValue(renderer)).Count;
            public LightHost(SpawnRing3DIntegrationFixture f)
            {
                try
                {
                    root=new GameObject("Owned native moving-light host");root.SetActive(false);root.AddComponent<Grid>();
                    var go=new GameObject("Native body map",typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(root.transform,false);
                    renderer=go.AddComponent<ZoneRenderer>();Field("_tilemap").SetValue(renderer,go.GetComponent<Tilemap>());
                    Field("_mainCamera").SetValue(renderer,f.Source);Field("_spawnRing3DPresenter").SetValue(renderer,f.Presenter);
                    // Only the active native sprite-mode flag is needed here. No
                    // synthetic pixels or fake fog/material uploads are provided.
                    Field("_envSpriteRenderer").SetValue(renderer,root.AddComponent<EnvironmentSpriteRenderer>());
                    renderer.SetZone(f.Zone);
                    ZoneRenderHooks.CellDirtyCallback=renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=renderer.MarkDirty;
                }
                catch{Dispose();throw;}
            }
            public void Frame()
            {try{typeof(ZoneRenderer).GetMethod("LateUpdate",Private).Invoke(renderer,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}
            public Color32 FogPixel(SpawnRing3DIntegrationFixture f,int x,int y)
                =>FogPixels(f)[(Zone.Height-1-y)*Zone.Width+x];
            public Color32[] FogPixels(SpawnRing3DIntegrationFixture f)
            {
                var surface=(NativeZone3DRenderSurface)typeof(SpawnRing3DPresenter).GetField("surface",Private).GetValue(f.Presenter);
                Assert.NotNull(surface);Assert.NotNull(surface.FogTexture);
                return surface.FogTexture.GetPixels32();
            }
            static FieldInfo Field(string name)=>typeof(ZoneRenderer).GetField(name,Private);
            public void Dispose(){if(root!=null)Object.DestroyImmediate(root);ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;}
        }
    }
}
