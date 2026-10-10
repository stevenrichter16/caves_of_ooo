using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SteamDeckSimulationAdversarialTests
    {
        [TestCase("attach", false)] [TestCase("attach", true)]
        [TestCase("remove", false)] [TestCase("remove", true)]
        [TestCase("transfer", false)] [TestCase("transfer", true)]
        [TestCase("duplicate", false)] [TestCase("duplicate", true)]
        [TestCase("rebuild", false)] [TestCase("rebuild", true)]
        public void MembershipMutationIsIndependentOfQueryTiming(string operation, bool warm)
        {
            var zone = new Zone(); var other = new Zone(); var e = new Entity(); var part = new ThermalPart();
            if (operation != "attach") e.AddPart(part);
            zone.AddEntity(e, 2, 2);
            if (warm) { SteamDeckSimulationWorkTests.Indexed<ThermalPart>(zone); SteamDeckSimulationWorkTests.Indexed<ThermalPart>(other); }
            if (operation == "attach") e.AddPart(part);
            if (operation == "remove") e.RemovePart(part);
            if (operation == "transfer") { Assert.True(zone.RemoveEntity(e)); Assert.True(other.AddEntity(e, 3, 3)); }
            if (operation == "duplicate") { e.AddPart(new ThermalPart()); e.RemovePart(part); }
            if (operation == "rebuild") { e.Parts.Clear(); zone.RebuildEntityCellsFromCells(); }
            bool retained = operation == "attach" || operation == "duplicate";
            Assert.AreEqual(retained ? 1 : 0, SteamDeckSimulationWorkTests.Indexed<ThermalPart>(zone).Count);
            Assert.AreEqual(operation == "transfer" ? 1 : 0, SteamDeckSimulationWorkTests.Indexed<ThermalPart>(other).Count);
        }
        [TestCase(100f)] [TestCase(-30f)] [TestCase(25f)]
        public void DirectTemperatureMutationsAreReadAfterAnIdleTurn(float temperature)
        {
            var zone = new Zone(); var e = new Entity(); var thermal = new ThermalPart();
            var probe = new SteamDeckSimulationWorkTests.TurnProbe(); e.AddPart(thermal); e.AddPart(probe); zone.AddEntity(e, 3, 3);
            MaterialSimSystem.TickMaterialEntities(zone); Assert.Zero(probe.Turns);
            thermal.Temperature = temperature; MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(temperature == 25 ? 0 : 1, probe.Turns);
        }
        [TestCase(false)] [TestCase(true)]
        public void LateWetAttachmentTicksOnlyNonCreatures(bool creature)
        {
            var zone = new Zone(); var e = new Entity(); if (creature) e.SetTag("Creature"); zone.AddEntity(e, 3, 3);
            MaterialSimSystem.TickMaterialEntities(zone);
            var wet = new WetEffect(.8f); Assert.True(e.ApplyEffect(wet, null, zone));
            MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(creature ? .8f : .79f, wet.Moisture, .0001f);
        }
        [TestCase(false)] [TestCase(true)]
        public void MultiCellLifespanIsCountedOnceAndRemovalDoesNotLeaveMembership(bool multi)
        {
            var zone = new Zone(); var e = new Entity(); var life = new LifespanPart { TurnsRemaining = 2 };
            e.AddPart(life); if (multi) e.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0;0,1" });
            Assert.True(zone.AddEntity(e, 5, 5)); MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(1, life.TurnsRemaining); Assert.NotNull(zone.GetEntityCell(e));
            MaterialSimSystem.TickMaterialEntities(zone); Assert.IsNull(zone.GetEntityCell(e));
            Assert.Zero(SteamDeckSimulationWorkTests.Indexed<LifespanPart>(zone).Count);
        }
        [TestCase("remove")] [TestCase("new")] [TestCase("throw")]
        public void CallbackMutationPreservesSnapshotAndReturnsScratch(string mutation)
        {
            var zone = new Zone(); var first = new Entity(); var second = new Entity();
            var secondProbe = new SteamDeckSimulationWorkTests.TurnProbe();
            first.AddPart(new LifespanPart { TurnsRemaining = 20 }); second.AddPart(new LifespanPart { TurnsRemaining = 20 });
            second.AddPart(secondProbe); zone.AddEntity(first, 1, 1); zone.AddEntity(second, 2, 2);
            var callback = new SteamDeckSimulationWorkTests.TurnProbe { OnTurn = () =>
            {
                if (mutation == "remove") zone.RemoveEntity(second);
                if (mutation == "new") { var e = new Entity(); e.AddPart(new LifespanPart { TurnsRemaining = 9 }); zone.AddEntity(e, 3, 3); }
                if (mutation == "throw") throw new InvalidOperationException("test callback");
            }};
            first.AddPart(callback);
            if (mutation == "throw") Assert.Throws<InvalidOperationException>(() => MaterialSimSystem.TickMaterialEntities(zone));
            else MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(mutation == "new" ? 1 : 0, secondProbe.Turns);
            first.RemovePart(callback); MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(mutation == "remove" ? 0 : mutation == "new" ? 2 : 1, secondProbe.Turns);
            if (mutation == "new") Assert.AreEqual(8, zone.GetCell(3, 3).Objects[0].GetPart<LifespanPart>().TurnsRemaining);
        }
        [Test] public void ReentrantOtherZoneTickUsesIndependentSnapshots()
        {
            var a = new Zone(); var b = new Zone(); var first = new Entity(); var second = new Entity();
            first.AddPart(new LifespanPart { TurnsRemaining = 3 }); second.AddPart(new LifespanPart { TurnsRemaining = 3 });
            first.AddPart(new SteamDeckSimulationWorkTests.TurnProbe { OnTurn = () => MaterialSimSystem.TickMaterialEntities(b) });
            a.AddEntity(first, 1, 1); b.AddEntity(second, 1, 1); MaterialSimSystem.TickMaterialEntities(a);
            Assert.AreEqual(2, first.GetPart<LifespanPart>().TurnsRemaining); Assert.AreEqual(2, second.GetPart<LifespanPart>().TurnsRemaining);
        }
    }
}
