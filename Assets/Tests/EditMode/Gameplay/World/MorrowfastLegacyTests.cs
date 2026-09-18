using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastLegacyTests
    {
        [Test] public void AVisitedProceduralGroveBecomesAReachableSettlementWithoutLosingForeignItems()
        {
            var f=MorrowfastTestWorld.Factory();var m=new OverworldZoneManager(f,64);m.WorldMap.SetPOI(3,6,null);
            var z=m.GetZone(MorrowfastSceneRuntime.ZoneID);Assert.IsFalse(MorrowfastSceneRuntime.IsActive(z));
            var p=MorrowfastTestWorld.Actor(f,z);var item=f.CreateEntity("Tepuibone");z.AddEntity(item,40,24);
            Assert.IsTrue(MorrowfastSceneRuntime.UpgradeCachedZone(z,f));Assert.IsNotNull(z.GetEntityCell(item));
            var seen=MorrowfastTestWorld.Flood(z,p);Assert.IsTrue(seen.Contains((40,0)),"The migrated main path must really be open. Remaining legacy solids: "
                +string.Join("; ",z.GetAllEntities().Where(e=>!e.HasTag(MorrowfastSceneRuntime.TerrainTag)&&!e.HasPart<MorrowfastPropPart>()&&(e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true)).Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e)+" tags="+string.Join(",",e.Tags.Keys)).Take(45)));
            foreach(var b in MorrowfastSceneDefinition.Load().buildings)MorrowfastTestWorld.Approach(z,p,MorrowfastSceneRuntime.FindOwner(z,b.doorId));
            Assert.AreEqual(2000,z.GetAllEntities().Count(e=>e.HasTag(MorrowfastSceneRuntime.TerrainTag)));
        }
    }
}
