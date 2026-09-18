using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class MultiCellSpatialTests
    {
        internal static Entity Body(string shape = "0,0;1,0;0,1;1,1", bool solid = true)
        {
            var e = new Entity { ID = System.Guid.NewGuid().ToString(), BlueprintName = "TestBody" };
            e.AddPart(new PhysicsPart { Solid = solid });
            e.AddPart(new RenderPart { RenderLayer = 5, Visible = true });
            if (shape != null) e.AddPart(new SpatialFootprintPart { CellsRaw = shape });
            return e;
        }

        [Test] public void CoveredEdgeResolvesOneOwnerWhileCanonicalStorageRemainsSingle()
        {
            var z = new Zone(); var e = Body();
            Assert.IsTrue(z.AddEntity(e, 10, 10));
            Assert.IsTrue(z.GetOccupants(11, 11).Contains(e));
            Assert.AreEqual(1, z.GetOccupants(11, 11).Count);
            Assert.IsTrue(z.GetCell(11, 11).BlocksMovement());
            Assert.AreSame(e, z.GetCell(11, 11).GetTopVisibleObject());
            Assert.IsEmpty(z.GetCell(11, 11).Objects);
            Assert.AreEqual(1, z.EntityCount);
            Assert.AreEqual(1, z.GetAllEntities().Count);
            Assert.AreEqual((10, 10), z.GetEntityPosition(e));
        }

        [Test] public void SingleCellControlDoesNotOccupyItsNeighbor()
        {
            var z = new Zone(); var e = Body(null); z.AddEntity(e, 10, 10);
            Assert.AreEqual(0, z.GetOccupants(11, 11).Count);
            Assert.IsFalse(z.GetCell(11, 11).BlocksMovement());
            Assert.IsTrue(z.GetOccupants(10, 10).Contains(e));
        }

        [Test] public void IrregularBodyLeavesHoleAndAnchorHoleEmpty()
        {
            var z = new Zone(); var e = Body("1,0;0,1;1,1");
            Assert.IsTrue(z.AddEntity(e, 10, 10));
            Assert.IsFalse(z.GetOccupants(10, 10).Contains(e));
            Assert.IsFalse(z.GetCell(10, 10).BlocksMovement());
            Assert.IsTrue(z.GetCell(11, 10).BlocksMovement());
            Assert.AreEqual(3, z.GetOccupiedCells(e).Count);
        }

        [Test] public void MoveRejectsFarEdgeCollisionWithoutChangingSource()
        {
            var z = new Zone(); var e = Body(); var wall = Body(null);
            z.AddEntity(e, 10, 10); z.AddEntity(wall, 12, 11);
            Assert.IsFalse(z.MoveEntity(e, 11, 10));
            Assert.AreEqual((10, 10), z.GetEntityPosition(e));
            Assert.IsTrue(z.GetOccupants(10, 11).Contains(e));
            Assert.IsFalse(z.GetOccupants(12, 10).Contains(e));
            z.RemoveEntity(wall);
            Assert.IsTrue(z.MoveEntity(e, 11, 10));
            Assert.IsFalse(z.GetOccupants(10, 11).Contains(e));
            Assert.IsTrue(z.GetOccupants(12, 11).Contains(e));
        }

        [Test] public void RemoveClearsAllBodyCellsAndAllowsReplacement()
        {
            var z = new Zone(); var e = Body(); z.AddEntity(e, 10, 10);
            Assert.IsTrue(z.RemoveEntity(e)); Assert.IsFalse(z.RemoveEntity(e));
            for (int x=10;x<=11;x++) for(int y=10;y<=11;y++)
                Assert.IsFalse(z.GetCell(x,y).BlocksMovement());
            Assert.IsTrue(z.AddEntity(Body(),10,10));
        }

        [TestCase("0,0;0,0")][TestCase("bad")][TestCase("")]
        [TestCase("80,0")][TestCase("0,25")][TestCase("2147483648,0")]
        public void MalformedOrOversizedShapeRejectsWithoutPartialPlacement(string raw)
        {
            var z = new Zone(); var e = Body(raw);
            Assert.IsFalse(z.AddEntity(e, 10, 10));
            Assert.AreEqual(0,z.EntityCount);
            Assert.AreEqual(0,z.GetOccupants(10,10).Count);
        }

        [Test] public void BoundsCheckIncludesWholeBodyAndNegativeOffsets()
        {
            var z = new Zone(); var e = Body("0,0;-1,0");
            Assert.IsFalse(z.AddEntity(e,0,10));
            Assert.IsTrue(z.AddEntity(e,1,10));
            Assert.IsFalse(z.MoveEntity(e,0,10));
            Assert.AreEqual((1,10),z.GetEntityPosition(e));
        }

        [Test] public void DerivedIndexRebuildIsIdempotentAndKeepsSingleAnchor()
        {
            var z = new Zone(); var e = Body(); z.GetCell(20,10).AddObject(e);
            z.RebuildEntityCellsFromCells(); z.RebuildEntityCellsFromCells();
            Assert.AreEqual(1,z.EntityCount);
            Assert.AreEqual(1,z.GetOccupants(21,11).Count);
            Assert.AreEqual((20,10),z.GetEntityPosition(e));
        }

        [Test] public void ShapeRoundTripsWithNativePartSerializer()
        {
            var e=Body("-1,0;0,0;1,0;1,1");
            var loaded=PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual(e.GetPart<SpatialFootprintPart>().CellsRaw,loaded.GetPart<SpatialFootprintPart>().CellsRaw);
            var z=new Zone(); Assert.IsTrue(z.AddEntity(loaded,10,10));
            Assert.IsTrue(z.GetOccupants(9,10).Contains(loaded));
            Assert.IsFalse(z.GetOccupants(9,11).Contains(loaded));
        }

        [Test] public void TransferRejectsFarEdgeBlockerAtomicallyThenSucceeds()
        {
            var a=new Zone("a");var b=new Zone("b");var e=Body();var wall=Body(null);
            a.AddEntity(e,10,10); b.AddEntity(wall,21,11);
            Assert.IsFalse(a.TryTransferEntityTo(e,b,20,10));
            Assert.AreEqual((10,10),a.GetEntityPosition(e));
            Assert.AreEqual(1,a.GetOccupants(11,11).Count);
            Assert.AreEqual((-1,-1),b.GetEntityPosition(e));
            b.RemoveEntity(wall);
            Assert.IsTrue(a.TryTransferEntityTo(e,b,20,10));
            Assert.AreEqual(0,a.EntityCount);Assert.AreEqual(1,b.EntityCount);
            Assert.AreEqual(1,b.GetOccupants(21,11).Count);
        }

        [Test] public void ContactDistanceUsesEdgesAndNotAnchorOrEmptyHole()
        {
            var z=new Zone();var e=Body("0,0;1,0;2,0;3,0");var other=Body(null);
            z.AddEntity(e,10,10);z.AddEntity(other,14,10);
            Assert.AreEqual(1,SpatialQuery.Distance(z,e,other));
            Assert.AreEqual(1,SpatialQuery.DistanceToCell(z,e,14,10));
            Assert.AreEqual(2,SpatialQuery.DistanceToCell(z,e,15,10));
        }
    }
}
