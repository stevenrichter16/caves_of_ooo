using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class BeatingCompositionRefinementTests
    {
        [Test] public void DuneBeltsHaveBroadBodiesInsteadOfOnlySingleCellFences()
        {
            for(int seed=0;seed<24;seed++)
            {
                var plan=BeatingCompositionPlan.Create(BeatingCompositionTests.Id,seed,Formation.DuneBelt);
                int cores=0;
                for(int y=1;y<Zone.Height-1;y++)for(int x=1;x<Zone.Width-1;x++)
                {
                    bool filled=true;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                        if(plan.ObjectAt(x+dx,y+dy)!="DuneCrest")filled=false;
                    if(filled)cores++;
                }
                Assert.Greater(cores,5,"Dunes need substantial bodies, seed "+seed);
                var control=BeatingCompositionPlan.Create(BeatingCompositionTests.Id,seed,Formation.WindBarrens);
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                    Assert.AreNotEqual("DuneCrest",control.ObjectAt(x,y));
            }
        }

        [Test] public void DuneHeightFollowsCurrentNativeNeighboursAndNeverReconstructsRemovedTerrain()
        {
            var f=GrovelandsCompositionTests.Factory();var zone=new Zone(BeatingCompositionTests.Id);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var centre=f.CreateEntity("DuneCrest");zone.AddEntity(centre,15,10);
            var left=f.CreateEntity("DuneCrest");zone.AddEntity(left,14,10);
            var right=f.CreateEntity("DuneCrest");zone.AddEntity(right,16,10);
            var up=f.CreateEntity("DuneCrest");zone.AddEntity(up,15,9);
            var down=f.CreateEntity("DuneCrest");zone.AddEntity(down,15,11);
            Assert.AreEqual("beating-dune-3",SpawnRing3DRecipes.Resolve(zone,centre,catalog).ModelId);
            zone.RemoveEntity(right);
            Assert.AreEqual("beating-dune-2",SpawnRing3DRecipes.Resolve(zone,centre,catalog).ModelId);
            up.GetPart<RenderPart>().Visible=false;
            Assert.AreEqual("beating-dune-1",SpawnRing3DRecipes.Resolve(zone,centre,catalog).ModelId);
            zone.RemoveEntity(left);zone.RemoveEntity(down);
            var control=f.CreateEntity("SandstoneWall");zone.AddEntity(control,14,10);
            Assert.AreEqual("beating-dune-0",SpawnRing3DRecipes.Resolve(zone,centre,catalog).ModelId);
            Assert.IsNull(zone.GetEntityCell(right));
            Assert.AreEqual("beating-ruin-",SpawnRing3DRecipes.Resolve(zone,control,catalog).ModelId.Substring(0,13));
        }
    }
}
