using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GleanersDistrictTests
    {
        const string Surface="Overworld.11.10.0", Cellar="Overworld.11.10.1", Role="GleanersDistrict.Role";
        HaulingContentScope scope;
        [SetUp] public void Setup(){scope=new HaulingContentScope();scope.Seed(64);}
        [TearDown] public void Cleanup(){scope.Dispose();}

        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void OrdinaryGladeHasBrokenWellAndNativePairedCellar(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
            var surface=manager.GetZone(Surface);
            var well=surface.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(Role)=="well");
            Assert.NotNull(well,"The ordinary starting district needs a useful material repair.");
            Assert.False(well.GetPart<WellPart>().IsUsable);
            Assert.AreEqual("clay-well-lining",well.GetPart<RepairablePart>().RecipeId);
            var stairs=surface.GetReadOnlyEntities().Single(e=>e.GetProperty(Role)=="stairs");
            Assert.True(stairs.HasPart<StairsDownPart>());
            var at=surface.GetEntityPosition(stairs);
            var connection=manager.GetConnections(Surface).Single(c=>c.TargetZoneID==Cellar&&c.Type=="StairsDown");
            Assert.AreEqual(at,(connection.SourceX,connection.SourceY));
            var cellar=manager.GetZone(Cellar);
            Assert.True(cellar.GetCell(connection.TargetX,connection.TargetY).Objects.Any(e=>e.HasPart<StairsUpPart>()));
            Assert.False(cellar.GetCell(connection.TargetX,connection.TargetY).BlocksMovement());
            var reach=ConnectivityBuilder.FloodFill(surface,40,12);
            Assert.True(reach[at.x,at.y],"Actual spawn can reach the cellar without clearing actors or walls.");
            var w=surface.GetEntityPosition(well);
            Assert.True(new[]{(1,0),(-1,0),(0,1),(0,-1)}.Any(d=>reach[w.x+d.Item1,w.y+d.Item2]));
        }

        [Test] public void ExistingCachedSurfaceIsNotRetrofitted()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            var old=new Zone(Surface);var owner=scope.Factory.CreateEntity("Grass");old.AddEntity(owner,40,12);
            manager.CachedZones[Surface]=old;
            Assert.AreSame(old,manager.GetZone(Surface));Assert.AreEqual(1,old.EntityCount);
            Assert.AreSame(owner,old.GetCell(40,12).Objects.Single());
            Assert.IsEmpty(manager.GetConnections(Surface));
        }

        [Test] public void DestroyedAndEmptiedDistrictDoesNotRegenerateOnUnload()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            var surface=manager.GetZone(Surface);var cellar=manager.GetZone(Cellar);
            Assert.True(surface.GetReadOnlyEntities().Any(e=>e.GetProperty(Role)=="well"));
            foreach(var owner in cellar.GetReadOnlyEntities().ToArray())cellar.RemoveEntity(owner);
            foreach(var owner in surface.GetReadOnlyEntities().Where(e=>e.GetProperty(Role)!=null).ToArray())surface.RemoveEntity(owner);
            manager.UnloadZone(Surface);manager.UnloadZone(Cellar);
            Assert.AreSame(surface,manager.GetZone(Surface));Assert.AreSame(cellar,manager.GetZone(Cellar));
            Assert.AreEqual(0,cellar.EntityCount);
            Assert.False(surface.GetReadOnlyEntities().Any(e=>e.GetProperty(Role)!=null));
        }
    }
}
