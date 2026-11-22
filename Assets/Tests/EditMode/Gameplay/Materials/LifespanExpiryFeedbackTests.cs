using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class LifespanExpiryFeedbackTests
    {
        Action<int, int, string> oldCell;
        Action<string> oldFull;
        readonly List<(int x, int y)> cells = new List<(int, int)>();
        int full;
        Zone zone;
        Entity owner;
        LifespanPart lifetime;

        [SetUp] public void Setup()
        {
            oldCell = ZoneRenderHooks.CellDirtyCallback;
            oldFull = ZoneRenderHooks.FullDirtyCallback;
            zone = new Zone("lifespan-feedback");
            owner = new Entity { ID = "temporary-owner" };
            lifetime = new LifespanPart { TurnsRemaining = 1 };
            owner.AddPart(lifetime);
            Assert.True(zone.AddEntity(owner, 11, 10));
            cells.Clear(); full = 0;
            ZoneRenderHooks.CellDirtyCallback = (x, y, _) => cells.Add((x, y));
            ZoneRenderHooks.FullDirtyCallback = _ => full++;
        }

        [TearDown] public void Cleanup()
        {
            ZoneRenderHooks.CellDirtyCallback = oldCell;
            ZoneRenderHooks.FullDirtyCallback = oldFull;
        }

        void Tick(Zone eventZone)
        {
            var e = GameEvent.New("EndTurn");
            try { e.SetParameter("Zone", eventZone); lifetime.HandleEvent(e); }
            finally { e.Release(); }
        }

        [Test] public void OrdinaryExpiryRepaintsOnlyItsFormerCell()
        {
            Tick(zone);
            Assert.IsNull(zone.GetEntityCell(owner));
            Assert.AreEqual(new[] { (11, 10) }, cells);
            Assert.AreEqual(0, full);
            Tick(zone);
            Assert.AreEqual(1, cells.Count, "An already expired owner cannot notify again.");
        }

        [TestCase(true)] [TestCase(false)]
        public void LightExpiryRefreshesLightingOnlyForAnEnabledSource(bool enabled)
        {
            owner.AddPart(new LightSourcePart { Enabled = enabled, Radius = 4 });
            Tick(zone);
            Assert.IsNull(zone.GetEntityCell(owner));
            Assert.AreEqual(enabled ? 1 : 0, full);
            Assert.AreEqual(enabled ? 0 : 1, cells.Count);
            if (!enabled) Assert.AreEqual((11, 10), cells[0]);
        }

        [Test] public void ExpiryStillWritesItsFiniteAftermathOnTheFormerCell()
        {
            lifetime.ExpiryCoating = "water";
            lifetime.ExpiryCoatingTurns = 3;
            Tick(zone);
            Assert.IsNull(zone.GetEntityCell(owner));
            var state = zone.TileState.Get(11, 10);
            Assert.NotNull(state);
            Assert.AreEqual("water", state.Coatings[0].Id);
            Assert.AreEqual(3, state.Coatings[0].Turns);
            Assert.IsTrue(cells.Contains((11, 10)));
            Assert.AreEqual(0, full);
        }

        [TestCase(2)] [TestCase(0)] [TestCase(-1)]
        public void ActiveOrIndefiniteOwnerDoesNotRequestExpiryRedraw(int remaining)
        {
            lifetime.TurnsRemaining = remaining;
            owner.AddPart(new LightSourcePart { Enabled = true });
            Tick(zone);
            Assert.NotNull(zone.GetEntityCell(owner));
            Assert.AreEqual(remaining > 0 ? remaining - 1 : remaining, lifetime.TurnsRemaining);
            Assert.AreEqual(0, full); Assert.IsEmpty(cells);
        }

        [TestCase("missing-zone")] [TestCase("other-zone")] [TestCase("removed")]
        public void ExpiryWithoutACurrentWorldCellDoesNotRedrawAnotherGraph(string fault)
        {
            owner.AddPart(new LightSourcePart { Enabled = true });
            Zone eventZone = zone;
            if (fault == "missing-zone") eventZone = null;
            if (fault == "other-zone") eventZone = new Zone(zone.ZoneID);
            if (fault == "removed") Assert.True(zone.RemoveEntity(owner));
            cells.Clear(); full = 0;
            Tick(eventZone);
            Assert.AreEqual(0, full); Assert.IsEmpty(cells);
            Assert.AreEqual(fault == "removed", zone.GetEntityCell(owner) == null);
        }
    }
}
