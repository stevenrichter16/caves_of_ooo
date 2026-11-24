using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PortableKindlingTests : FiftySecondPreparationFixture
    {
        const string Prefix = "KindleFuel|";
        Entity Torch() { var t = Place("Torch"); Assert.True(Act(t, "ExtinguishTorch")); return t; }
        Entity Hearth(int fuel = 10) { var h = Place("CampHearth"); h.GetPart<PhysicsPart>().Takeable = false; h.GetPart<FuelPart>().FuelMass = fuel; return h; }
        Entity Moss(int n = 1) { var m = Carry("FireMoss"); m.GetPart<StackerPart>().StackCount = n; return m; }
        string Quote(Entity target) => Choice(target, Prefix);
        [TestCase(1)] [TestCase(3)] public void TorchSpendsExactlyOneMossAndAddsNoFuel(int count)
        {
            var t = Torch(); var m = Moss(count); float fuel = t.GetPart<FuelPart>().FuelMass;
            Assert.True(Act(t, Quote(t))); Assert.AreEqual(count - 1, PackCount("FireMoss"));
            Assert.True(t.GetPart<LightSourcePart>().Enabled); Assert.True(t.GetPart<TorchLightPart>().ControlledFlameActive);
            Assert.AreEqual(fuel, t.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void HearthUsesTheSameMaterialAndCanCookWithItsOwnFuel()
        {
            var h = Hearth(); Moss(2); var meat = Carry("RawMeat");
            Assert.True(Act(h, Quote(h))); Assert.AreEqual(1, PackCount("FireMoss")); Assert.AreEqual(10, h.GetPart<FuelPart>().FuelMass);
            Assert.True(h.GetPart<ThermalPart>().IsAflame); Assert.True(Actions(h).Any(a => a.Command.Contains("Cook")) || Actions(meat).Any(a => a.Command.Contains("Cook")), "Real existing cooking must become available.");
        }
        [Test] public void FuelLessHearthAndTorchDoNotOfferOrSpendKindling()
        {
            Moss(2); var h = Hearth(0); var t = Torch(); t.GetPart<FuelPart>().FuelMass = 0;
            Assert.False(Actions(h).Any(a => a.Command.StartsWith(Prefix))); Assert.False(Actions(t).Any(a => a.Command.StartsWith(Prefix))); Assert.AreEqual(2, PackCount("FireMoss"));
        }
        [TestCase("flame")] [TestCase("flint")] public void ExistingPreferredSourceKeepsMoss(string source)
        {
            var t = Torch(); Moss(2); if (source == "flame") Place("Campfire", 9, 10); else Carry("FlintStriker");
            Assert.False(Actions(t).Any(a => a.Command.StartsWith(Prefix))); Assert.True(Act(t, "LightTorch")); Assert.AreEqual(2, PackCount("FireMoss"));
        }
        [Test] public void WornFlintDoesNotHideMossFallback()
        { var t = Torch(); Carry("FlintStriker").GetPart<FlintStrikerPart>().Strikes = 0; Moss(); Assert.True(Act(t, Quote(t))); }
        [Test] public void UnquotedLegacyCommandsCannotSpendMoss()
        { var t = Torch(); var h = Hearth(); Moss(2); Assert.False(Act(t, "LightTorch")); Assert.False(Act(h, "RelightCookingFire")); Assert.AreEqual(2, PackCount("FireMoss")); }
        [Test] public void PreviewNamesTheOneItemCostAndReadingIsFree()
        {
            var t = Torch(); Moss(3); int tick = Clock.TickCount;
            for (int i = 0; i < 3; i++) { var row = Actions(t).Single(a => a.Command.StartsWith(Prefix)); StringAssert.Contains("1 fire-moss", row.Display); }
            StringAssert.Contains("torch", ItemTacticalUseDescription.Describe(Packed("FireMoss"))); Assert.AreEqual(3, PackCount("FireMoss")); Assert.AreEqual(tick, Clock.TickCount);
        }
        [TestCase("wet")] [TestCase("frozen")] [TestCase("ground")] [TestCase("foreign")] [TestCase("zero")] [TestCase("duplicate")]
        public void UnavailableSourceOffersNoKindling(string state)
        {
            var t = Torch(); var m = Moss();
            if (state == "wet") m.ApplyEffect(new WetEffect(.36f));
            if (state == "frozen") { m.AddPart(new MaterialPart { MaterialTagsRaw = "Metal" }); Assert.True(m.ApplyEffect(new FrozenEffect(1))); }
            if (state == "ground") { Pack.RemoveObject(m); Assert.True(Zone.AddEntity(m, 10, 10)); }
            if (state == "foreign") m.GetPart<PhysicsPart>().InInventory = Factory.CreateEntity("Player");
            if (state == "zero") m.GetPart<StackerPart>().StackCount = 0;
            if (state == "duplicate") m.AddPart(new StackerPart());
            Assert.False(Actions(t).Any(a => a.Command.StartsWith(Prefix))); Assert.False(t.GetPart<LightSourcePart>().Enabled);
        }
        [TestCase("count")] [TestCase("source")] [TestCase("move")] [TestCase("fuel")] [TestCase("preferred")]
        public void RetainedQuoteRefusesChangedSelectionWithoutSubstitution(string state)
        {
            var t = Torch(); var m = Moss(2); string q = Quote(t);
            if (state == "count") m.GetPart<StackerPart>().StackCount = 3;
            if (state == "source") { Pack.RemoveObject(m); Moss(2); }
            if (state == "move") { Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 10, 11)); }
            if (state == "fuel") t.GetPart<FuelPart>().FuelMass = 0;
            if (state == "preferred") Carry("FlintStriker");
            Assert.False(Act(t, q)); Assert.False(t.GetPart<LightSourcePart>().Enabled); Assert.AreEqual(state == "count" ? 3 : 2, PackCount("FireMoss"));
        }
        [TestCase(1)] [TestCase(3)] public void OuterFailureRestoresOnlyItsMossAndLight(int count)
        {
            var t = Torch(); Moss(count); string q = Quote(t); float temperature = t.GetPart<ThermalPart>().Temperature;
            FailAfter(); Assert.False(Act(t, q)); Assert.AreEqual(count, PackCount("FireMoss")); Assert.False(t.GetPart<LightSourcePart>().Enabled); Assert.AreEqual(temperature, t.GetPart<ThermalPart>().Temperature);
        }
        [Test] public void CommitCallbackCannotDetachTargetAndRefundUnrelatedInjury()
        {
            var t = Torch(); Moss(); string q = Quote(t); var heat = t.GetPart<ThermalPart>(); int hp = Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new KindlingCallbackPart { Callback = () => { Actor.GetStat("Hitpoints").Penalty += 2; t.RemovePart(heat); } });
            Assert.False(Act(t, q)); Assert.AreEqual(1, PackCount("FireMoss")); Assert.AreEqual(hp - 2, Actor.GetStatValue("Hitpoints")); Assert.Null(t.GetPart<ThermalPart>());
        }
        [TestCase("stack")] [TestCase("fuel")] [TestCase("drop")]
        public void LateTargetChangesRefuseAndRetainIndependentState(string change)
        {
            var t = Torch(); Moss(2); string q = Quote(t); int hp = Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new KindlingCallbackPart { Callback = () => {
                Actor.GetStat("Hitpoints").Penalty += 2;
                if (change == "stack") t.GetPart<StackerPart>().StackCount = 2;
                if (change == "fuel") t.GetPart<FuelPart>().FuelMass -= 3;
                if (change == "drop") { Zone.RemoveEntity(t); Assert.True(Zone.AddEntity(t, 15, 15)); }
            } });
            Assert.False(Act(t, q)); Assert.AreEqual(2, PackCount("FireMoss")); Assert.AreEqual(hp - 2, Actor.GetStatValue("Hitpoints"));
            if (change == "stack") Assert.AreEqual(2, t.GetPart<StackerPart>().StackCount);
            if (change == "drop") Assert.AreEqual(15, Zone.GetEntityCell(t).X);
        }
        [Test] public void FailedHearthLightingRestoresMossWithoutChangingExistingFuel()
        {
            var h = Hearth(); Moss(); string q = Quote(h); FailAfter();
            Assert.False(Act(h, q)); Assert.AreEqual(1, PackCount("FireMoss")); Assert.AreEqual(10, h.GetPart<FuelPart>().FuelMass); Assert.AreEqual(25, h.GetPart<ThermalPart>().Temperature);
        }
        [TestCase("add")] [TestCase("drop")]
        public void RollbackRefundsOnlyOneUnitAndPreservesIndependentMossCustody(string change)
        {
            var t = Torch(); var m = Moss(3); string q = Quote(t);
            Actor.AddPart(new KindlingCallbackPart { Callback = () => {
                if (change == "add") m.GetPart<StackerPart>().StackCount += 2;
                else { Assert.True(Pack.RemoveObject(m)); Assert.True(Zone.AddEntity(m, 10, 10)); }
            } });
            Assert.False(Act(t, q));
            Assert.AreEqual(change == "add" ? 5 : 3, m.GetPart<StackerPart>().StackCount);
            if (change == "drop") { Assert.False(Pack.Objects.Contains(m)); Assert.Null(m.GetPart<PhysicsPart>().InInventory); Assert.NotNull(Zone.GetEntityCell(m)); Assert.AreSame(Zone, Zone.GetEntityCell(m).ParentZone); }
        }
        [TestCase(false)] [TestCase(true)]
        public void UnknownRefundDestinationDoesNotRecapturePartialStockOrMutateAnIdAlias(bool alias)
        {
            var t = Torch(); var m = Moss(3); string q = Quote(t); Entity replacement = null;
            Actor.AddPart(new KindlingCallbackPart { Callback = () => {
                Assert.True(Pack.RemoveObject(m));
                if (alias) { replacement = Factory.CreateEntity("FireMoss"); replacement.ID = m.ID; replacement.GetPart<StackerPart>().StackCount = 7; Assert.True(Pack.AddObject(replacement)); m.GetPart<PhysicsPart>().InInventory = Actor; }
            } });
            Assert.False(Act(t, q)); Assert.False(Pack.Objects.Contains(m)); Assert.AreEqual(2, m.GetPart<StackerPart>().StackCount, "An independently detached partial stack has no supported refund destination; preserve it and diagnose the limited refund.");
            if (alias) Assert.AreEqual(7, replacement.GetPart<StackerPart>().StackCount);
        }
        [Test] public void HeldTorchUsesMossButAStowedTorchDoesNot()
        {
            var t = Carry("Torch"); Moss(2); Assert.False(Actions(t).Any(a => a.Command.StartsWith(Prefix)));
            Assert.True(InventorySystem.Equip(Actor, t)); t = Pack.EquippedItems.Values.Single(e => e.BlueprintName == "Torch"); Assert.True(Act(t, "ExtinguishTorch"));
            Assert.True(Act(t, Quote(t))); Assert.True(t.GetPart<TorchLightPart>().ControlledFlameActive); Assert.AreEqual(1, PackCount("FireMoss"));
        }
        [Test] public void BeforeActionCallbackCannotReplaceTheQuotedAmount()
        {
            var t = Torch(); var m = Moss(3); string q = Quote(t);
            Actor.AddPart(new KindlingBeforeCallbackPart { Callback = () => m.GetPart<StackerPart>().StackCount = 5 });
            Assert.False(Act(t, q)); Assert.AreEqual(5, m.GetPart<StackerPart>().StackCount); Assert.False(t.GetPart<LightSourcePart>().Enabled);
        }
        [Test] public void ActualWorldCellMenuOffersAndDispatchesTheHearthKindlingRow()
        {
            var h = Hearth(); Moss(2); var rows = CellMenu(11, 10).Where(a => a.Command.StartsWith(Prefix)).ToArray();
            Assert.AreEqual(1, rows.Length, "The real C menu gathers the target event directly, not the pack adapter.");
            StringAssert.Contains("1 fire-moss", rows[0].Display);
            Assert.True(ChooseRow(rows[0], 11, 10)); Assert.AreEqual(1, PackCount("FireMoss")); Assert.True(h.GetPart<ThermalPart>().IsAflame);
        }
        [Test] public void SpentMossAndLitHearthSurviveGraphReload()
        {
            var h = Hearth(); Moss(2); Assert.True(Act(h, Quote(h))); string id = h.ID;
            var loaded = RoundTrip(); Actor = loaded.Player; Zone = loaded.ZoneManager.ActiveZone; SettlementRuntime.ActiveZone = Zone;
            Assert.AreEqual(1, PackCount("FireMoss")); h = Zone.GetReadOnlyEntities().Single(e => e.ID == id); Assert.True(h.GetPart<ThermalPart>().IsAflame); Assert.AreEqual(10, h.GetPart<FuelPart>().FuelMass);
        }
    }
    public sealed class KindlingBeforeCallbackPart : Part
    {
        public Action Callback;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeInventoryAction") Callback?.Invoke(); return true; }
    }
    public sealed class KindlingCallbackPart : Part
    {
        public Action Callback;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") Callback?.Invoke(); return true; }
    }
}
