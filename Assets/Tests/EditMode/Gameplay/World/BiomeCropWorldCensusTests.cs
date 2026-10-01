using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual generated owners reached through native world-map descent
    /// and a surviving ordinary cave staircase. Exact seeds are Unity-authoritative.</summary>
    public sealed class BiomeCropWorldCensusTests
    {
        static HashSet<(int,int)> Flood(Zone zone,int x,int y)
        {
            var seen=new HashSet<(int,int)>();var queue=new Queue<(int x,int y)>();seen.Add((x,y));queue.Enqueue((x,y));
            while(queue.Count>0)
            {
                var at=queue.Dequeue();foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {
                    var next=(at.x+d.Item1,at.y+d.Item2);var cell=zone.GetCell(next.Item1,next.Item2);
                    if(cell!=null&&!cell.BlocksMovement()&&seen.Add(next))queue.Enqueue(next);
                }
            }
            return seen;
        }
        static void Record(Zone zone,HashSet<(int,int)> reached,HashSet<string> found)
        {
            foreach(var owner in zone.GetReadOnlyEntities())
            {
                if(owner.GetPart<CropPart>() is not CropPart crop)continue;
                var definition=BiomeCropCatalog.ByBlueprint(owner.BlueprintName);if(definition==null)continue;
                Assert.True(reached.Contains(zone.GetEntityPosition(owner)),owner.BlueprintName+" cannot be reached in "+zone.ZoneID);
                Assert.True(CultivatedSoilPart.IsCultivated(zone,zone.GetEntityCell(owner)));
                if(crop.GrowthStage==2)found.Add(definition.Id);
            }
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void EveryNewSpeciesHasActualRipeSourceReachedByNormalTravel(int seed)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(seed);var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
                var map=manager.GetZone(WorldMap.WorldMapZoneID);var player=scope.Factory.CreateEntity("Player");
                var found=new HashSet<string>();
                foreach(var site in BiomeCropPlan.SurfaceSites(manager))
                {
                    var at=WorldMap.FromZoneID(site.ZoneID);var point=WorldMap.WorldCellToZoneCell(at.x,at.y);
                    Assert.True(map.AddEntity(player,point.zoneX,point.zoneY));
                    var arrive=WorldMapTraversal.Descend(player,map,manager);Assert.True(arrive.Success,site.ZoneID+": "+arrive.ErrorReason);
                    var zone=arrive.NewZone;Record(zone,Flood(zone,arrive.NewPlayerX,arrive.NewPlayerY),found);
                    Assert.True(zone.RemoveEntity(player));
                }
                bool entered=false;
                foreach(string column in BiomeCropPlan.CaveColumns(manager))
                {
                    var address=WorldMap.FromZoneID(column);var point=WorldMap.WorldCellToZoneCell(address.x,address.y);
                    Assert.True(map.AddEntity(player,point.zoneX,point.zoneY));
                    var surfaceArrival=WorldMapTraversal.Descend(player,map,manager);
                    Assert.True(surfaceArrival.Success,column+": "+surfaceArrival.ErrorReason);
                    var surface=surfaceArrival.NewZone;
                    var surfaceReach=Flood(surface,surfaceArrival.NewPlayerX,surfaceArrival.NewPlayerY);
                    var stair=surface.GetReadOnlyEntities().FirstOrDefault(e=>e.HasPart<StairsDownPart>()
                        &&surfaceReach.Contains(surface.GetEntityPosition(e)));
                    if(stair==null){Assert.True(surface.RemoveEntity(player));continue;}
                    var at=surface.GetEntityPosition(stair);Assert.True(surface.MoveEntity(player,at.x,at.y));
                    var zone=surface;
                    for(int depth=1;depth<=5;depth++)
                    {
                        var currentPosition=zone.GetEntityPosition(player);
                        var walkable=Flood(zone,currentPosition.x,currentPosition.y);
                        var depart=zone.GetReadOnlyEntities().FirstOrDefault(e=>e.HasPart<StairsDownPart>()
                            &&walkable.Contains(zone.GetEntityPosition(e)));
                        Assert.NotNull(depart,zone.ZoneID+": lower stair is not reachable from actual arrival");
                        var p=zone.GetEntityPosition(depart);Assert.True(zone.MoveEntity(player,p.x,p.y));
                        var arrive=ZoneTransitionSystem.TransitionPlayerVertical(player,zone,true,p.x,p.y,manager);
                        Assert.True(arrive.Success,zone.ZoneID+": "+arrive.ErrorReason);zone=arrive.NewZone;
                        Record(zone,Flood(zone,arrive.NewPlayerX,arrive.NewPlayerY),found);
                    }
                    Assert.True(zone.RemoveEntity(player));entered=true;break;
                }
                Assert.True(entered,"At least one selected ordinary column must have an actual surface entrance.");
                CollectionAssert.AreEquivalent(BiomeCropCatalog.All.Select(c=>c.Id),found,
                    "Missing actual ripe sources: "+string.Join(",",BiomeCropCatalog.All.Select(c=>c.Id).Except(found)));
                TestContext.WriteLine("Biome crop native-source census seed="+seed+" reachableRipeSpecies="+found.Count+" ids="+string.Join(",",found.OrderBy(s=>s)));
            }
        }
    }
}
