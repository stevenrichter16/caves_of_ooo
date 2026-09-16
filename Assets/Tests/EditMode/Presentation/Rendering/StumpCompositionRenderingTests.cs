using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class StumpCompositionRenderingTests
    {
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void EveryVisibleOwnerAcrossAllEighteenNativeChunksHasAnExplicitRecipe(int seed)
        {
            var factory=GrovelandsCompositionTests.Factory();var manager=new OverworldZoneManager(factory,seed);
            var catalog=UnityEngine.Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            int zones=0;
            for(int y=0;y<20;y++)for(int x=0;x<20;x++)
            {
                string id=WorldMap.ToZoneID(x,y);if(!StumpCompositionPlan.IsWildernessZone(id))continue;
                var zone=manager.GetZone(id);int version=zone.EntityVersion;zones++;
                foreach(var owner in zone.GetAllEntities())
                {
                    var recipe=SpawnRing3DRecipes.Resolve(zone,owner,catalog);var render=owner.GetPart<RenderPart>();
                    if(render==null||!render.Visible){Assert.IsNull(recipe.ModelId);continue;}
                    Assert.NotNull(recipe.ModelId,id+" "+owner.BlueprintName+": "+recipe.Failure);
                    Assert.NotNull(catalog.FindModel(recipe.ModelId));Assert.AreSame(owner,recipe.Owner);
                }
                Assert.AreEqual(version,zone.EntityVersion,"Art lookup must not repair native objects.");
            }
            Assert.AreEqual(18,zones);
        }

        [TestCase("Overworld.2.1.0")] [TestCase("Overworld.4.6.0")]
        public void LockedCampChestKeepsItsNativeLockAndMembershipWhileSharingScopedChestArt(string id)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone(id);
            var catalog=UnityEngine.Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var chest=factory.CreateEntity("LockedChest");zone.AddEntity(chest,10,10);
            var lockPart=chest.GetPart<LockPart>();Assert.NotNull(lockPart);Assert.IsTrue(lockPart.IsLocked);
            var recipe=SpawnRing3DRecipes.Resolve(zone,chest,catalog);
            Assert.AreEqual("ring-chest",recipe.ModelId);Assert.AreSame(chest,recipe.Owner);Assert.IsTrue(lockPart.IsLocked);
            Assert.IsFalse(recipe.Transient);Assert.IsTrue(recipe.Batched);
            zone.RemoveEntity(chest);Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,chest,catalog).ModelId);
            var control=new Zone("Overworld.4.6.3");control.AddEntity(chest,10,10);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(control,chest,catalog).ModelId,"Unsupported depths do not gain the shared chest visual alias.");
            Assert.IsTrue(lockPart.IsLocked);
        }

        [Test] public void DestroyingNativeTepuiWallLeavesVisibleVoxelRubbleAndKeepsTheNeighbor()
        {
            var old=DestructionSystem.EntityFactoryRef;
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.2.1.0"))
            {
                try
                {
                    DestructionSystem.EntityFactoryRef=GrovelandsCompositionTests.Factory();f.Set("FullReveal",true);
                    var wall=f.Add("TepuiWall",20,10);var control=f.Add("TepuiWall",60,20);f.Refresh();
                    Assert.IsTrue(f.Rendered(wall));Assert.IsTrue(f.Rendered(control));
                    Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(wall,1000,null,f.Zone));
                    var rubble=f.Zone.GetCell(20,10).Objects.Single(e=>e.BlueprintName=="Rubble");f.Refresh();
                    Assert.IsFalse(f.Rendered(wall));Assert.IsTrue(f.Rendered(control));Assert.IsTrue(f.Rendered(rubble));
                    Assert.IsTrue(f.Find(rubble,out _,out string model));StringAssert.Contains("rubble",model);
                    Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
                }
                finally{DestructionSystem.EntityFactoryRef=old;}
            }
        }

        [TestCase("GrainRidge","grain")] [TestCase("StoneDome","dome")]
        public void NativeNeighborsControlHeightAndRemovalUpdatesAcrossPatchBoundaries(string bp,string family)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.3.2.0"))
            {
                f.Set("FullReveal",true);
                foreach(var c in new[]{(19,12),(18,12),(20,12),(19,11),(19,13)})
                    foreach(var old in f.Zone.GetCell(c.Item1,c.Item2).Objects.ToArray())
                        if(old.BlueprintName==bp)f.Zone.RemoveEntity(old);
                var center=f.Add(bp,19,12);f.Add(bp,18,12);var right=f.Add(bp,20,12);f.Add(bp,19,11);f.Add(bp,19,13);
                f.Refresh();Assert.IsTrue(f.Find(center,out var patch,out string first));
                Assert.AreEqual("stump-"+family+"-3",first);
                Assert.IsTrue(f.Find(right,out var other,out _));Assert.AreNotSame(patch,other);
                int revision=f.Revision(19,12);f.Zone.RemoveEntity(right);
                f.Call("Refresh",null,new HashSet<int>());f.Frame();
                Assert.IsTrue(f.Find(center,out var same,out string next));Assert.AreSame(patch,same);
                Assert.AreEqual("stump-"+family+"-2",next);Assert.Greater(f.Revision(19,12),revision);
                Assert.IsFalse(f.Find(right,out _,out _));Assert.IsTrue(f.Get<bool>("IsReady"),f.Get<string>("Failure"));
                Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
            }
        }
        [TestCase("SummitSinger","singer")] [TestCase("BrocchiniaSentinel","sentinel")] [TestCase("Tepuibone","bone")] [TestCase("IronKey","key")]
        public void ActorsAndPortableMineralsStayTransientAndFollowNativeMembership(string bp,string family)
        {
            var factory=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.3.2.0");var e=factory.CreateEntity(bp);z.AddEntity(e,20,10);
            var catalog=UnityEngine.Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var recipe=SpawnRing3DRecipes.Resolve(z,e,catalog);
            StringAssert.StartsWith("stump-"+family+"-",recipe.ModelId);Assert.IsTrue(recipe.Transient);Assert.IsFalse(recipe.Batched);
            if(bp=="SummitSinger"||bp=="BrocchiniaSentinel")
            {
                string glyph=e.GetPart<RenderPart>().RenderString;e.GetPart<RenderPart>().RenderString="?";
                Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,catalog).ModelId);e.GetPart<RenderPart>().RenderString=glyph;
            }
            z.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,catalog).ModelId);
        }
    }
}
