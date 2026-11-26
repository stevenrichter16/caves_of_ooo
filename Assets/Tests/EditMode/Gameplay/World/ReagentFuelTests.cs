using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ReagentFuelTests : FiftyWorldFixture
    {
        const string Prefix = "FeedReagentFuel|";
        Entity Fire(float fuel = 0, string blueprint = "CampHearth")
        {
            var fire = Place(blueprint); fire.GetPart<PhysicsPart>().Takeable = false;
            fire.GetPart<CampfirePart>().FiniteCooking = true;
            if (!fire.HasPart<FuelPart>()) fire.AddPart(new FuelPart());
            var f = fire.GetPart<FuelPart>(); f.FuelMass = fuel; f.MaxFuel = 30; f.BurnRate = .25f;
            fire.GetPart<ThermalPart>().Temperature = 25; return fire;
        }
        Entity Resin(int count = 3) { var r = Carry("PitchpodResin"); r.GetPart<StackerPart>().StackCount = count; return r; }
        InventoryAction[] Rows(Entity fire) => Actions(fire).Where(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
        string Quote(Entity fire) { var rows = Rows(fire); Assert.AreEqual(1, rows.Length); return rows[0].Command; }

        [TestCase(1)] [TestCase(3)] public void ResinSpendsOneUnitForFiveFuelWithoutIgniting(int count)
        {
            var f = Fire(); Resin(count); var q = Quote(f); Assert.True(Act(f, q));
            Assert.AreEqual(count - 1, Count("PitchpodResin")); Assert.AreEqual(5, f.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(25, f.GetPart<ThermalPart>().Temperature); Assert.False(f.GetPart<LightSourcePart>().Enabled);
        }
        [Test] public void LampOilHasFifteenFuelAndDoesNotSpendOtherInputs()
        {
            var f = Fire(); Carry("LampOil"); Carry("SalvagedTimber"); Assert.True(Act(f, Quote(f)));
            Assert.AreEqual(15, f.GetPart<FuelPart>().FuelMass); Assert.AreEqual(0, Count("LampOil")); Assert.AreEqual(1, Count("SalvagedTimber"));
        }
        [Test] public void PermissionComesFromTheStableCombustiblePropertyNotAnItemNameList()
        {
            var f = Fire(); var m = Carry("FireMoss"); m.GetPart<ReagentPart>().PropertiesRaw = "combustible:2";
            Assert.True(Act(f, Quote(f))); Assert.AreEqual(10, f.GetPart<FuelPart>().FuelMass);
        }
        [TestCase("CampHearth")] [TestCase("Campfire")] public void TheActualWorldCellMenuDispatchesTheSameFiniteFireRule(string blueprint)
        {
            var f = Fire(0, blueprint); Resin(); var row = CellMenu(11, 10).Single(a => a.Command.StartsWith(Prefix));
            Assert.AreEqual(ActionTime.OneAction, row.Time); Assert.True(ChooseRow(row, 11, 10)); Assert.AreEqual(5, f.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void PreviewShowsActualClampedGainAndLostSurplusAndIsFree()
        {
            var f = Fire(29); Resin(); int tick = Clock.TickCount; var row = Rows(f).Single();
            StringAssert.Contains("1 pitchpod resin", row.Display); StringAssert.Contains("+1 fuel", row.Display); StringAssert.Contains("4", row.Hint);
            Assert.AreEqual(tick, Clock.TickCount); Assert.AreEqual(3, Count("PitchpodResin"));
            Assert.True(Act(f, row.Command)); Assert.AreEqual(30, f.GetPart<FuelPart>().FuelMass);
        }
        [TestCase("full")] [TestCase("free")] [TestCase("carried")] public void UnsupportedFireStatesOfferNoFeed(string state)
        {
            var f = Fire(state == "full" ? 30 : 0); Resin();
            if (state == "free") f.GetPart<CampfirePart>().FiniteCooking = false;
            if (state == "carried") { Zone.RemoveEntity(f); f.GetPart<PhysicsPart>().Takeable = true; Pack.AddObject(f); }
            Assert.IsEmpty(Rows(f));
        }
        [TestCase("heat:1")] [TestCase("combustible:1,volatile:2")] [TestCase("combustible:1,volatile:bad")]
        [TestCase("combustible:1,volatile:0")] [TestCase("combustible:1,,heat:1")] [TestCase("combustible:4")]
        [TestCase("combustible:1:2")] [TestCase("combustible:1,combustible:2")]
        public void CompleteInvalidOrUnstablePropertiesNeverBecomeFuel(string raw)
        {
            var f = Fire(); var r = Resin(); r.GetPart<ReagentPart>().PropertiesRaw = raw; Assert.IsEmpty(Rows(f));
        }
        [TestCase("wet")] [TestCase("frozen")] [TestCase("private")] [TestCase("foreign")] [TestCase("alias")]
        public void UnavailableOrPrivateInputsNeverGetAQuote(string state)
        {
            var f = Fire(); var r = Resin();
            if (state == "wet") r.ApplyEffect(new WetEffect(.5f));
            if (state == "frozen") { r.AddPart(new MaterialPart { MaterialTagsRaw = "Metal" }); Assert.True(r.ApplyEffect(new FrozenEffect(1))); }
            if (state == "private") r.AddPart(new LiquidVesselPart { Capacity = 12, Volume = 2, LiquidId = "oil" });
            if (state == "foreign") r.GetPart<PhysicsPart>().InInventory = Factory.CreateEntity("Player");
            if (state == "alias") Pack.Objects.Add(r);
            Assert.IsEmpty(Rows(f));
        }
        [TestCase("count")] [TestCase("payload")] [TestCase("fuel")] [TestCase("capacity")] [TestCase("move")]
        public void StaleQuotesRefuseWithoutSubstitution(string change)
        {
            var f = Fire(); var r = Resin(); string q = Quote(f);
            if (change == "count") r.GetPart<StackerPart>().StackCount++;
            if (change == "payload") r.GetPart<ReagentPart>().PropertiesRaw = "combustible:2";
            if (change == "fuel") f.GetPart<FuelPart>().FuelMass = 1;
            if (change == "capacity") f.GetPart<FuelPart>().MaxFuel = 31;
            if (change == "move") { Zone.RemoveEntity(Actor); Zone.AddEntity(Actor, 10, 11); }
            Assert.False(Act(f, q)); Assert.AreEqual(change == "count" ? 4 : 3, Count("PitchpodResin"));
            Assert.AreEqual(change == "fuel" ? 1 : 0, f.GetPart<FuelPart>().FuelMass);
        }
        [TestCase(1)] [TestCase(3)] public void OuterFailureRestoresExactlyItsOneUnitAndFuel(int count)
        {
            var f = Fire(7); Resin(count); string q = Quote(f); FailAfter(); Assert.False(Act(f, q));
            Assert.AreEqual(count, Count("PitchpodResin")); Assert.AreEqual(7, f.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void LateFailurePreservesIndependentInjuryAndFuelAddition()
        {
            var f = Fire(7); Resin(); string q = Quote(f); int hp = Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new ReagentFuelCallbackPart { Callback = () => { Actor.GetStat("Hitpoints").Penalty += 2; f.GetPart<FuelPart>().FuelMass += 2; } });
            Assert.False(Act(f, q)); Assert.AreEqual(hp - 2, Actor.GetStatValue("Hitpoints")); Assert.AreEqual(9, f.GetPart<FuelPart>().FuelMass); Assert.AreEqual(3, Count("PitchpodResin"));
        }
        [TestCase("add")] [TestCase("drop")] public void LateFailureRefundsOneUnitWithoutRewindingOtherStockOrCustody(string change)
        {
            var f = Fire(); var r = Resin(); string q = Quote(f);
            Actor.AddPart(new ReagentFuelCallbackPart { Callback = () => { if (change == "add") r.GetPart<StackerPart>().StackCount += 2; else { Pack.RemoveObject(r); Zone.AddEntity(r, 10, 10); } } });
            Assert.False(Act(f, q)); Assert.AreEqual(change == "add" ? 5 : 3, r.GetPart<StackerPart>().StackCount); Assert.AreEqual(0, f.GetPart<FuelPart>().FuelMass);
            if (change == "drop") { Assert.False(Pack.Objects.Contains(r)); Assert.NotNull(Zone.GetEntityCell(r)); }
        }
        [Test] public void TheNewFuelSurvivesTheNativeSavedEntityContract()
        {
            var f = Fire(); Resin(1); Assert.True(Act(f, Quote(f))); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(f);
            Assert.AreEqual(5, copy.GetPart<FuelPart>().FuelMass); Assert.AreEqual(25, copy.GetPart<ThermalPart>().Temperature);
        }
        [TestCase(false)] [TestCase(true)] public void SingletonDetachedChangesAreNotRecapturedOrOverwritten(bool forceFireRefusal)
        {
            var f = Fire(7); var r = Resin(1); string q = Quote(f);
            Actor.AddPart(new ReagentFuelCallbackPart { Callback = () => {
                r.GetPart<StackerPart>().StackCount += 2;
                if (forceFireRefusal) f.GetPart<FuelPart>().FuelMass += 1;
            } });
            Assert.False(Act(f, q)); Assert.False(Pack.Objects.Contains(r));
            Assert.AreEqual(3, r.GetPart<StackerPart>().StackCount, "An independently altered detached payload must not be reset to one and recaptured.");
            Assert.Null(r.GetPart<PhysicsPart>().InInventory); Assert.Null(Zone.GetEntityCell(r));
            Assert.AreEqual(forceFireRefusal ? 8 : 7, f.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ReceiptOwnershipIsAttachedAndCommandSpecificAndGrantsNoPermission()
        {
            var f = Fire(); Resin(); var part = f.GetPart<CampfirePart>(); var owner = (IInventoryActionReceiptOwner)part;
            Assert.True(owner.OwnsInventoryReceipt(Quote(f))); Assert.False(owner.OwnsInventoryReceipt("FeedCookingFire"));
            Assert.False(Act(f, "FeedReagentFuel|forged")); Assert.AreEqual(0, f.GetPart<FuelPart>().FuelMass);
            f.RemovePart(part); Assert.False(owner.OwnsInventoryReceipt("FeedReagentFuel|forged"));
        }
        [Test] public void TorchReceiptOwnerKeepsOnlyItsExistingKindlingFamily()
        {
            var torch = Place("Torch"); var part = torch.GetPart<TorchLightPart>(); var owner = (IInventoryActionReceiptOwner)part;
            Assert.True(owner.OwnsInventoryReceipt("KindleFuel|forged")); Assert.False(owner.OwnsInventoryReceipt("FeedReagentFuel|forged"));
            Assert.False(owner.OwnsInventoryReceipt("LightTorch")); Assert.False(Act(torch, "KindleFuel|forged"));
            torch.RemovePart(part); Assert.False(owner.OwnsInventoryReceipt("KindleFuel|forged"));
        }
        // NUnit's default seven-digit float display gives these distinct inputs
        // the same name. Pin separate names without changing either precision boundary.
        [TestCase(134217728f, TestName = "UnrepresentableFuelGainAt134217728NeverSpendsAReagent")]
        [TestCase(134217720f, TestName = "RoundedUpFuelGainAt134217720NeverSpendsAReagent")]
        public void UnrepresentableOrRoundedUpFuelGainsNeverSpendAReagent(float before)
        {
            var f = Fire(before); f.GetPart<FuelPart>().MaxFuel = 134217760f; Resin(1);
            Assert.IsEmpty(Rows(f), "A precision gap must not consume stock for zero fuel or create more fuel than the item provides.");
            Assert.AreEqual(1, Count("PitchpodResin"));
        }
        [TestCase("source")] [TestCase("fire")] public void LateIdentityChangesRefuseAndPreserveTheIndependentNewIdentity(string which)
        {
            var f = Fire(7); var r = Resin(); string q = Quote(f); string before = which == "source" ? r.ID : f.ID;
            Actor.AddPart(new ReagentFuelCallbackPart { Callback = () => { if (which == "source") r.ID += "_changed"; else f.ID += "_changed"; } });
            Assert.False(Act(f, q)); Assert.AreEqual(before + "_changed", which == "source" ? r.ID : f.ID);
            Assert.AreEqual(7, f.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(which == "source" ? 2 : 3, r.GetPart<StackerPart>().StackCount, "An independently renamed payload has no safely identified refund target.");
        }
        [Test] public void UnstableFuelHasAReasonAndSuccessAndRefusalEmitDiagnostics()
        {
            var f = Fire(); var spore = Carry("BlastcapSpore");
            var disabled = Actions(f).Single(a => a.Name == "FeedReagentFuel"); Assert.True(disabled.IsDisabled);
            StringAssert.Contains("Volatile", disabled.DisabledReason); Assert.True(Pack.RemoveObject(spore));
            Resin(); Diag.ResetAll(); Assert.True(Act(f, Quote(f)));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "ReagentFuelFed", Limit = 10 }).Records.Count);
            Assert.False(Act(f, "FeedReagentFuel|forged"));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "ReagentFuelRefused", Limit = 10 }).Records.Count);
        }
        [TestCase("PitchpodResin", 5)] [TestCase("LampOil", 15)] public void TheMaterialExplainsItsFiniteFuelUseWithoutSpendingAnything(string blueprint, int amount)
        {
            var item = Carry(blueprint); var text = new System.Collections.Generic.List<string>(); int tick = Clock.TickCount;
            UseGuides.Collect(item, Zone, Zone.GetEntityCell(Actor), Actor, text);
            string read = string.Join(" ", text); StringAssert.Contains(amount + " fuel", read); StringAssert.Contains("press C on the fire", read);
            StringAssert.Contains("adds no flame", read); Assert.AreEqual(tick, Clock.TickCount); Assert.True(Pack.Objects.Contains(item));
        }
    }
    public sealed class ReagentFuelCallbackPart : Part
    {
        public Action Callback;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") Callback(); return true; }
    }
}
