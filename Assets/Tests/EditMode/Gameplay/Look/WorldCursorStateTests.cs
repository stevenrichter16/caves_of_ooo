using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class WorldCursorStateTests
    {
        [Test]
        public void Activate_StoresState_AndClampsToZoneBounds()
        {
            var zone = new Zone("CursorZone");
            var cursor = new WorldCursorState();

            cursor.Activate(WorldCursorMode.Look, zone, -4, 99, 10, 11, maxRange: 4, followMouse: false);

            Assert.IsTrue(cursor.Active);
            Assert.AreEqual(WorldCursorMode.Look, cursor.Mode);
            Assert.AreSame(zone, cursor.Zone);
            Assert.AreEqual(0, cursor.X);
            Assert.AreEqual(Zone.Height - 1, cursor.Y);
            Assert.AreEqual(10, cursor.AnchorX);
            Assert.AreEqual(11, cursor.AnchorY);
            Assert.AreEqual(4, cursor.MaxRange);
            Assert.IsFalse(cursor.FollowMouse);
        }

        [Test]
        public void MoveBy_AndDistanceFromAnchor_UseChebyshevDistance()
        {
            var zone = new Zone("CursorZone");
            var cursor = new WorldCursorState();
            cursor.Activate(WorldCursorMode.Look, zone, 10, 10, 10, 10);

            cursor.MoveBy(3, -1);

            Assert.AreEqual(13, cursor.X);
            Assert.AreEqual(9, cursor.Y);
            Assert.AreEqual(3, cursor.DistanceFromAnchor());
        }

        [Test]
        public void Deactivate_ClearsActiveZoneReference()
        {
            var zone = new Zone("CursorZone");
            var cursor = new WorldCursorState();
            cursor.Activate(WorldCursorMode.Look, zone, 10, 10, 10, 10);

            cursor.Deactivate();

            Assert.IsFalse(cursor.Active);
            Assert.IsNull(cursor.Zone);
            Assert.IsFalse(cursor.FollowMouse);
        }
    }
}
