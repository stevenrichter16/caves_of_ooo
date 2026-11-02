using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class OverwritRimGatheringTests
    {
        [TestCase(64)][TestCase(65)][TestCase(704860946)][TestCase(int.MinValue)][TestCase(int.MaxValue)]
        public void FreshPilotHasAnOpenConnectedGatheringPlace(int seed)
        {
            var zone = new Zone("Overworld.4.12.0");
            var builder = new OverwritCompositionBuilder(seed);
            Assert.True(builder.BuildZone(zone, GrovelandsCompositionTests.Factory(), new Random(seed)));
            var p = builder.Plan;
            var fixtures = zone.GetAllEntities().Where(e => e.BlueprintName == "OverwritPilgrimBench" || e.BlueprintName == "OverwritWaymarker").ToList();
            Assert.AreEqual(2, fixtures.Count(e => e.BlueprintName == "OverwritPilgrimBench"));
            Assert.AreEqual(1, fixtures.Count(e => e.BlueprintName == "OverwritWaymarker"));
            foreach (var owner in fixtures)
            {
                var (x,y) = zone.GetEntityPosition(owner);
                Assert.True(p.IsRim(x,y)); Assert.False(p.IsApproach(x,y));
                for (int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
                {
                    if (dx==0 && dy==0) continue;
                    var c=zone.GetCell(x+dx,y+dy);
                    Assert.False(c.BlocksMovement(), "Keep the real fixture apron walkable.");
                    Assert.False(c.Objects.Any(e => e.BlueprintName == "OverwritNewGrowth"), "A clear apron makes the fixture legible.");
                }
            }
            Assert.True(FormationReachability.FullyReached(zone, FormationReachability.FloodFromWest(zone)));
            for (int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++)
            {
                if (p.IsApproach(x,y)) Assert.False(zone.GetCell(x,y).BlocksMovement());
                if (!p.IsRim(x,y)) Assert.IsNull(p.ObjectAt(x,y), "Preserve the intentionally quiet interior.");
            }
            Assert.AreEqual(p.Signature(), OverwritCompositionPlan.Create(zone.ZoneID, seed).Signature());
            var before = p.Signature(); int entities=zone.EntityCount;
            Assert.False(new OverwritCompositionBuilder(seed+1).BuildZone(zone, GrovelandsCompositionTests.Factory(), new Random(2)));
            Assert.AreEqual(entities,zone.EntityCount); Assert.AreEqual(before,p.Signature(),"Never retrofit cached ownership.");
        }

        [TestCase("Overworld.2.13.0")][TestCase("Overworld.4.11.0")]
        [TestCase("Overworld.0.9.0")][TestCase("Overworld.0.13.0")]
        public void OtherFormationsKeepExistingFurnitureBounds(string id)
        {
            for (int seed=0;seed<32;seed++)
            {
                var p=OverwritCompositionPlan.Create(id,seed);int furniture=0,growth=0;
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    string owner=p.ObjectAt(x,y);
                    if(owner=="OverwritPilgrimBench"||owner=="OverwritWaymarker")furniture++;
                    if(owner=="OverwritNewGrowth")growth++;
                }
                Assert.LessOrEqual(furniture,p.Formation==OverwritFormation.PilgrimageRim?2:0);
                if(p.Role==OverwritRole.Blank)Assert.That(growth,Is.InRange(5,10));
            }
        }
    }
}
