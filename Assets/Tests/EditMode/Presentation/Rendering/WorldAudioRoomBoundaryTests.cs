using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public abstract class WorldAudioRoomBoundaryFixture : FiftyWorldFixture
    {
        protected WorldAudioPlayer Audio;protected WorldAudioSoundscape Scene;
        GameObject root;AudioClip clip;float volume;bool paused;int sequence;
        [SetUp] public void AudioSetup()
        {
            volume=WorldAudioSettings.AmbienceVolume;paused=AudioListener.pause;WorldAudioSettings.AmbienceVolume=1;AudioListener.pause=false;
            root=new GameObject("room boundary audio");clip=AudioClip.Create("structural detail",48000,1,48000,false);var library=new WorldAudioLibrary();
            foreach(var id in new[]{"wood-settle","stone-shift","frog","water-lap","water-drop","fire","leaf-rustle"})library.AddDetail(id,clip,.3f);
            Audio=new WorldAudioPlayer(root.transform,library);Scene=new WorldAudioSoundscape(Audio);
            foreach(var cell in Zone.Cells)cell.Explored=cell.IsVisible=true;Zone.GetEntityCell(Actor).IsInterior=true;
        }
        [TearDown] public void AudioCleanup()
        {Audio.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);WorldAudioSettings.AmbienceVolume=volume;AudioListener.pause=paused;}
        protected Entity Structure(string material="Wood",bool door=false,int x=11,int y=10)
        {
            var owner=new Entity{ID="enclosing-"+sequence++,BlueprintName="fixture boundary"};owner.AddPart(new RenderPart());owner.AddPart(new PhysicsPart());
            owner.AddPart(new MaterialPart{MaterialID=material});if(door)owner.AddPart(new DoorPart());else owner.SetTag("Wall");
            Assert.True(Zone.AddEntity(owner,x,y));return owner;
        }
        protected void Observe()=>Scene.Observe(Zone,Actor,BiomeType.Sodden,0,DayBand.Dawn,true);
        protected void Start(string id)
        {Observe();Assert.AreEqual(1,Scene.SourceCount);Scene.Tick(10,true,false);Assert.AreEqual(id,Audio.LastDetailId);Assert.AreEqual(1,Audio.ActiveDetailSources);}
    }
    public sealed class WorldAudioRoomBoundaryTests : WorldAudioRoomBoundaryFixture
    {
        [TestCase("Wood",false,"wood-settle")][TestCase("Wood",true,"wood-settle")]
        [TestCase("Stone",false,"stone-shift")][TestCase("Masonry",false,"stone-shift")]
        public void ImmediateRealStructureCanEncloseInteriorDespiteExteriorBoundaryFlag(string material,bool door,string cue)
        {var owner=Structure(material,door);Assert.False(Zone.GetEntityCell(owner).IsInterior);Start(cue);Assert.Null(Scene.AmbienceId);}
        [TestCase(9,10)][TestCase(11,10)][TestCase(10,9)][TestCase(10,11)]
        public void EveryCardinalEnclosureFaceIsEligible(int x,int y)
        {Structure(x:x,y:y);Start("wood-settle");}
        [Test] public void ActualVillageBuilderWallAndItsInsideFloorNeedNoFakeInteriorRetagging()
        {
            var built=new Zone("Overworld.10.10.0");var poi=new PointOfInterest(POIType.Village,"audio room","Villagers");
            Assert.True(new VillageBuilder(BiomeType.Ruins,poi).BuildZone(built,Factory,new System.Random(42)));
            foreach(var cell in built.Cells)cell.Explored=cell.IsVisible=true;
            Cell inside=null;Entity wall=null;
            foreach(var cell in built.Cells.Cast<Cell>().Where(c=>c.IsInterior&&!c.BlocksMovement(Actor)))
                foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {
                    var candidate=built.GetCell(cell.X+d.Item1,cell.Y+d.Item2);
                    var stone=candidate?.Objects.FirstOrDefault(e=>e.HasTag("Wall")&&e.GetPart<MaterialPart>()?.MaterialID=="Stone");
                    if(stone!=null&&!candidate.IsInterior){inside=cell;wall=stone;break;}
                }
            Assert.NotNull(wall);Assert.True(Zone.RemoveEntity(Actor));Assert.True(built.AddEntity(Actor,inside.X,inside.Y));Zone=built;
            Assert.True(inside.IsInterior);Assert.False(Zone.GetEntityCell(wall).IsInterior);Start("stone-shift");
        }
        [Test] public void LeavingImmediateEnclosureRetiresTheKnownTailWithoutDiscovery()
        {
            Structure();Start("wood-settle");int scans=Scene.ScanCount;Zone.GetCell(9,10).IsInterior=true;Assert.True(Zone.MoveEntity(Actor,9,10));
            Scene.Tick(.01f,true,false);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(0,Scene.SourceCount);Assert.AreEqual(scans,Scene.ScanCount);
        }
        [Test] public void InteriorOwnedStructurePreservesExistingLongerRangeAdmission()
        {var owner=Structure(x:14);Zone.GetEntityCell(owner).IsInterior=true;Start("wood-settle");}
        [Test] public void KnownBoundaryNeverAddsIdleDiscoveryOrVoices()
        {Structure();Observe();int scans=Scene.ScanCount;for(int i=0;i<1000;i++){Scene.Tick(.1f,true,false);Audio.Update(.1f);}Assert.AreEqual(scans,Scene.ScanCount);Assert.LessOrEqual(Scene.LastVisitedOwners,512);Assert.AreEqual(16,Audio.AllocatedSources);}
    }
}
