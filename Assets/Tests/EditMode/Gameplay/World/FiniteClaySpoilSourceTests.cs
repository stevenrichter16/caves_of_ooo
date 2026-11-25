using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiniteClaySpoilSourceTests : FiftyWorldFixture
    {
        [SetUp] public void ReadySite()
        {
            Assert.True(Zone.RemoveEntity(Actor));Zone=new Zone(RepairCultivationSite.ZoneID);SettlementRuntime.ActiveZone=Zone;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)Assert.True(Zone.AddEntity(Factory.CreateEntity("Grass"),x,y));
            Assert.True(Zone.AddEntity(Actor,10,10));foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
        }
        [TearDown] public void RestoreSite(){RouteWorkCreationProbe.Run=null;}
        Entity Heap()=>Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName=="DryClaySpoilHeap");
        Entity Role(string role)=>Zone.GetReadOnlyEntities().Single(e=>e.GetProperty(RepairCultivationSite.RoleKey)==role);
        void Beside(Entity e)
        {var at=Zone.GetEntityCell(e);var c=new[]{(0,-1),(1,0),(0,1),(-1,0)}.Select(d=>Zone.GetCell(at.X+d.Item1,at.Y+d.Item2)).First(x=>x!=null&&!x.BlocksMovement(Actor));Assert.True(Zone.MoveEntity(Actor,c.X,c.Y));}
        [Test] public void FreshOptionalHeapPreservesAllThirteenOriginalRolesAndFourClay()
        {
            Assert.True(RepairCultivationSite.TryInstall(Zone,Factory));Assert.NotNull(Heap());Assert.AreEqual(13,Zone.GetReadOnlyEntities().Count(e=>e.GetProperty(RepairCultivationSite.RoleKey)!=null));
            var bank=Role("clay").GetPart<HarvestablePart>();Assert.AreEqual("FireClay",bank.YieldBlueprint);Assert.AreEqual(4,bank.YieldMin);Assert.AreEqual(4,bank.YieldMax);
        }
        [Test] public void RealHarvestYieldsOnePacketAndRepeatCannotRefillIt()
        {
            Assert.True(RepairCultivationSite.TryInstall(Zone,Factory));var heap=Heap();Assert.NotNull(heap);Beside(heap);Assert.True(Act(heap,"Harvest"));
            Assert.IsNull(Zone.GetEntityCell(heap));Assert.AreEqual(1,Count("DryClaySpoil"));Assert.False(RepairCultivationSite.TryInstall(Zone,Factory));Assert.IsNull(Heap());Assert.AreEqual(1,Count("DryClaySpoil"));
        }
        [Test] public void OrdinaryBankHarvestStillProvidesFourConvenientClay()
        {Assert.True(RepairCultivationSite.TryInstall(Zone,Factory));var bank=Role("clay");Beside(bank);Assert.True(Act(bank,"Harvest"));Assert.AreEqual(4,Count("FireClay"));}
        [Test] public void MissingOptionalHeapCannotPreventTheOriginalSite()
        {Factory.Blueprints.Remove("DryClaySpoilHeap");Assert.True(RepairCultivationSite.TryInstall(Zone,Factory));Assert.AreEqual(13,Zone.GetReadOnlyEntities().Count(e=>e.GetProperty(RepairCultivationSite.RoleKey)!=null));Assert.NotNull(Role("lining"));Assert.IsNull(Heap());}
        [Test] public void AnOldInstalledSiteDoesNotAcquireNewOptionalStock()
        {
            Factory.Blueprints.TryGetValue("DryClaySpoilHeap",out var blueprint);Factory.Blueprints.Remove("DryClaySpoilHeap");Assert.True(RepairCultivationSite.TryInstall(Zone,Factory));
            if(blueprint!=null)Factory.Blueprints["DryClaySpoilHeap"]=blueprint;
            Assert.False(RepairCultivationSite.TryInstall(Zone,Factory));Assert.IsNull(Heap());
        }
        [TestCase(false)][TestCase(true)]
        public void InvalidOptionalIdentityCannotDamageTheOriginalSite(bool empty)
        {
            Factory.RegisterPartType<RouteWorkCreationProbe>("Examinable");bool reached=false;string bankID=null;
            RouteWorkCreationProbe.Run=e=>{if(e.BlueprintName!="DryClaySpoilHeap")return;reached=true;bankID=Role("clay").ID;e.ID=empty?"":bankID;};
            Assert.True(RepairCultivationSite.TryInstall(Zone,Factory));Assert.True(reached);Assert.IsNull(Heap());
            Assert.AreEqual(13,Zone.GetReadOnlyEntities().Count(e=>e.GetProperty(RepairCultivationSite.RoleKey)!=null));
            Assert.AreEqual(1,Zone.GetReadOnlyEntities().Count(e=>e.ID==bankID));Assert.NotNull(Role("lining"));
        }
        [Test] public void LateIndependentOccupationOfOptionalGroundKeepsTheExistingSite()
        {
            Factory.RegisterPartType<RouteWorkCreationProbe>("Examinable");bool reached=false;string[] originalRoles=null;
            RouteWorkCreationProbe.Run=e=>
            {
                if(e.BlueprintName!="DryClaySpoilHeap")return;reached=true;originalRoles=Zone.GetReadOnlyEntities().Where(o=>o.GetProperty(RepairCultivationSite.RoleKey)!=null).Select(o=>o.ID).ToArray();
                var bank=Zone.GetEntityCell(Role("clay"));
                for(int y=bank.Y-3;y<=bank.Y+3;y++)for(int x=bank.X-3;x<=bank.X+3;x++)
                {var c=Zone.GetCell(x,y);if(c!=null&&c.Objects.Count==1&&c.Objects[0].BlueprintName=="Grass")Assert.True(Zone.AddEntity(Factory.CreateEntity("FireClay"),x,y));}
            };
            Assert.True(RepairCultivationSite.TryInstall(Zone,Factory));Assert.True(reached,"must reach the optional creation callback");Assert.AreEqual(13,originalRoles.Length);
            Assert.True(originalRoles.All(id=>Zone.GetReadOnlyEntities().Any(e=>e.ID==id)));Assert.IsNull(Heap());Assert.NotNull(Role("lining"));
        }
    }
}
