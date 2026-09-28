using System;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class AllocationProbeTests
 {
  [Test]public void WarmCurrentQueryDoesNotAllocate()
  {using(var f=new WorldAffordanceQueryTests.Fixture()){f.Row();for(int i=0;i<30;i++)Assert.Greater(WorldAffordanceQuery.Find(f.Player,f.Zone,false,0,0).Value.Hint.Length,0);long b=GC.GetAllocatedBytesForCurrentThread();var control=new byte[65536];long controlBytes=GC.GetAllocatedBytesForCurrentThread()-b;GC.KeepAlive(control);Assert.GreaterOrEqual(controlBytes,65536);long before=GC.GetAllocatedBytesForCurrentThread();int found=0;for(int i=0;i<10000;i++){var q=WorldAffordanceQuery.Find(f.Player,f.Zone,false,0,0);if(q.HasValue&&q.Value.Hint.Length>0)found++;}long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.AreEqual(10000,found);TestContext.WriteLine("validatedCounter="+controlBytes+"; queries=10000; bytes="+bytes);Assert.AreEqual(0,bytes);}}
 }
}
