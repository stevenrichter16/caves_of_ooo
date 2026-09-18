using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class MultiCellSpatialPerformanceTests
    {
        [Test] public void WarmPhysicalQueriesDoNotAllocatePerNpcOrPerCell()
        {
            var z=new Zone();var e=MultiCellSpatialTests.Body();z.AddEntity(e,10,10);
            int observations=0;
            for(int i=0;i<200;i++)
            {observations+=z.GetOccupants(11,11).Count;SpatialQuery.DistanceToCell(z,e,12,12);z.CanPlaceFootprint(e,11,10);}
            long before=System.GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<5000;i++)
            {
                var occupants=z.GetOccupants(11,11);if(occupants.Count==1&&occupants[0]==e) observations++;
                if(z.CanPlaceFootprint(e,11,10)) observations++;
                observations+=SpatialQuery.DistanceToCell(z,e,12,12);
            }
            long bytes=System.GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.AreEqual(15200,observations);Assert.AreEqual(0,bytes,"body queries cannot allocate per rendered cell or AI candidate");
        }
    }
}
