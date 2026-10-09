using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CombatSupplyDousingAdversarialTests : FiftyWorldFixture
    {
        string Select(Entity item, Entity target, bool clay = false)
            => Choice(item, clay ? "SmotherFire|" : "DrenchCreature|", a => a.Command.Split('|')[4] == Id(target));
        static BurningEffect Burn(Entity entity)
        { var burn = new BurningEffect(); Assert.True(entity.ApplyEffect(burn)); return burn; }
        static int Amount(Entity entity) => entity.GetPart<StackerPart>()?.StackCount ?? 1;

        [TestCase("zone")] [TestCase("verb")] [TestCase("amount")] [TestCase("overflow")] [TestCase("target")]
        public void ForgedCommandsCannotSoakOrSpend(string fault)
        {
            var water = Skin(3); var fields = Select(water, Actor).Split('|');
            if (fault == "zone") fields[1] = "other-zone";
            if (fault == "verb") fields[0] = "SmotherFire";
            if (fault == "amount") fields[7] = "-1";
            if (fault == "overflow") fields[5] = "2147483648";
            if (fault == "target") fields[4] = "missing";
            Assert.False(Act(water, string.Join("|", fields))); Assert.AreEqual(3, Units(water)); Assert.False(Actor.HasEffect<WetEffect>());
        }

        [TestCase("duplicate-id")] [TestCase("foreign-owner")] [TestCase("equipped-alias")] [TestCase("ground-alias")]
        public void CorruptWaterOwnershipCannotSpend(string fault)
        {
            var water = Skin(2); string command = Select(water, Actor);
            if (fault == "duplicate-id") Carry("FireClay").ID = water.ID;
            if (fault == "foreign-owner") water.GetPart<PhysicsPart>().InInventory = new Entity();
            if (fault == "equipped-alias") Pack.EquippedItems["forged"] = water;
            if (fault == "ground-alias") Zone.AddEntity(water, 10, 10);
            Assert.False(Act(water, command)); Assert.AreEqual(2, Units(water)); Assert.False(Actor.HasEffect<WetEffect>());
        }

        [TestCase(false)] [TestCase(true)]
        public void BeforeActionVetoKeepsBothFlameAndPayment(bool clay)
        {
            var item = clay ? Carry("FireClay") : Skin(2); var burn = Burn(Actor); string command = Select(item, Actor, clay);
            Actor.AddPart(new Hook("BeforeInventoryAction", () => false));
            Assert.False(Act(item, command)); Assert.AreSame(burn, Actor.GetEffect<BurningEffect>()); Assert.False(Actor.HasEffect<WetEffect>());
            Assert.Contains(item, Pack.Objects); if (!clay) Assert.AreEqual(2, Units(item));
        }

        [TestCase(false)] [TestCase(true)]
        public void FlameRemovalCallbackExceptionRestoresTheExactFlameAndSupply(bool clay)
        {
            var target = Place("MarlbackScrabbler"); var burn = Burn(target); float temperature = target.GetPart<ThermalPart>().Temperature;
            var item = clay ? Carry("FireClay") : Skin(2); string command = Select(item, target, clay);
            target.AddPart(new Hook("EffectRemoved", () => throw new InvalidOperationException("removal observer")));
            Assert.False(Act(item, command)); Assert.AreSame(burn, target.GetEffect<BurningEffect>());
            Assert.False(target.HasEffect<WetEffect>()); Assert.AreEqual(temperature, target.GetPart<ThermalPart>().Temperature);
            Assert.Contains(item, Pack.Objects); if (!clay) Assert.AreEqual(2, Units(item));
        }

        [Test] public void WetApplicationCallbackExceptionRemovesOnlyTheNewWetEffect()
        {
            var target = Place("MarlbackScrabbler"); var burn = Burn(target); var water = Skin(2); string command = Select(water, target);
            target.AddPart(new Hook("EffectApplied", () => throw new InvalidOperationException("application observer")));
            Assert.False(Act(water, command)); Assert.AreSame(burn, target.GetEffect<BurningEffect>());
            Assert.False(target.HasEffect<WetEffect>()); Assert.AreEqual(2, Units(water));
        }

        [Test] public void FailurePreservesAnIndependentConditionAddedByTheOuterObserver()
        {
            var target = Place("MarlbackScrabbler"); var burn = Burn(target); var water = Skin(2); string command = Select(water, target);
            var acid = new AcidicEffect(.3f);
            Actor.AddPart(new Hook("AfterInventoryAction", () => { target.ApplyEffect(acid); throw new InvalidOperationException("outer observer"); }));
            Assert.False(Act(water, command)); Assert.AreSame(burn, target.GetEffect<BurningEffect>());
            Assert.AreSame(acid, target.GetEffect<AcidicEffect>()); Assert.False(target.HasEffect<WetEffect>()); Assert.AreEqual(2, Units(water));
        }

        [Test] public void RolledBackNonBurningDrenchDoesNotProvoke()
        {
            var target = Place("MarlbackScrabbler"); var water = Skin(2); string command = Select(water, target); FailAfter();
            Assert.False(Act(water, command)); Assert.False(target.HasEffect<WetEffect>()); Assert.AreEqual(2, Units(water));
            Assert.False(target.GetPart<BrainPart>().IsPersonallyHostileTo(Actor));
        }

        [TestCase(false)] [TestCase(true)]
        public void ReentrantUseCannotConsumeTwice(bool clay)
        {
            var item = clay ? Carry("FireClay") : Skin(3); if (clay) { item.GetPart<StackerPart>().StackCount = 3; Burn(Actor); }
            string command = Select(item, Actor, clay); bool nested = true;
            Actor.AddPart(new Hook("AfterInventoryAction", () => { nested = Act(item, command); return true; }));
            Assert.True(Act(item, command)); Assert.False(nested); Assert.AreEqual(2, clay ? Amount(item) : Units(item));
        }

        [Test] public void WideCreatureIsReachableByItsNearBodyContact()
        {
            var target = Factory.CreateEntity("MarlbackScrabbler"); target.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-1,0" });
            Assert.True(Zone.AddEntity(target, 12, 10)); var water = Skin(1);
            Assert.True(Act(water, Select(water, target))); Assert.True(target.HasEffect<WetEffect>()); Assert.AreEqual(0, Units(water));
        }

        [Test] public void ClayDoesNotExtinguishFireOnTheFloorOrConsumeForAnUnburningBody()
        {
            Zone.TileState.WriteResidue(10, 10, "embers", 8); var clay = Carry("FireClay");
            Assert.False(Actions(clay).Any(a => a.Command.StartsWith("SmotherFire|")));
            Assert.True(Zone.TileState.HasResidue(10, 10, "embers")); Assert.Contains(clay, Pack.Objects);
            Burn(Actor); Assert.True(Act(clay, Select(clay, Actor, true))); Assert.True(Zone.TileState.HasResidue(10, 10, "embers"));
        }

        [Test] public void ExpiringWetRecordIsRenewedWithoutDuplicates()
        {
            var wet = new WetEffect(1); Actor.ApplyEffect(wet); wet.Duration = 0; var water = Skin(2);
            Assert.True(Act(water, Select(water, Actor))); Assert.AreSame(wet, Actor.GetEffect<WetEffect>());
            Assert.AreEqual(Effect.DURATION_INDEFINITE, wet.Duration); Assert.AreEqual(1, Actor.GetPart<StatusEffectsPart>().GetAllEffects().OfType<WetEffect>().Count());
        }

        [Test] public void DuplicateRecipientIdIsRejectedWithoutGuessing()
        {
            var target = Place("MarlbackScrabbler"); var water = Skin(2); string command = Select(water, target);
            Place("MarlbackScrabbler", 10, 11).ID = target.ID;
            Assert.False(Act(water, command)); Assert.AreEqual(2, Units(water)); Assert.False(target.HasEffect<WetEffect>());
        }

        [TestCase("dead")] [TestCase("temperature")]
        public void InvalidRecipientDoesNotReceiveDousing(string fault)
        {
            var target = Place("MarlbackScrabbler"); var water = Skin(2); string command = Select(water, target);
            if (fault == "dead") target.GetStat("Hitpoints").BaseValue = 0;
            else target.GetPart<ThermalPart>().Temperature = float.NaN;
            Assert.False(Act(water, command)); Assert.AreEqual(2, Units(water)); Assert.False(target.HasEffect<WetEffect>());
        }

        [Test] public void MoisturePersistsThroughActualEntitySaveWithoutAddingProtectionFlags()
        {
            var water = Skin(2); Assert.True(Act(water, Select(water, Actor)));
            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Assert.AreEqual(1, restored.GetEffect<WetEffect>().Moisture); Assert.False(restored.HasEffect<BurningEffect>());
            Assert.AreEqual(1, restored.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "Waterskin").GetPart<WaterskinPart>().Charges);
        }

        [TestCase(false)] [TestCase(true)]
        public void ExtinguishedObserverFailureCannotHideCommittedDousingReceiptOrVisualUpdate(bool throws)
        {
            var target = Place("MarlbackScrabbler"); Burn(target); var water = Skin(2);
            string command = Select(water, target); int dirty = 0; var previous = ZoneRenderHooks.CellDirtyCallback;
            Diag.ResetAll();
            target.AddPart(new Hook("Extinguished", () => { if (throws) throw new InvalidOperationException("extinguished observer"); return true; }));
            ZoneRenderHooks.CellDirtyCallback = (x, y, reason) => { if (x == 11 && y == 10 && reason == "Drenched") dirty++; };
            try
            {
                Assert.True(Act(water, command)); Assert.AreEqual(1, Units(water));
                Assert.False(target.HasEffect<BurningEffect>()); Assert.AreEqual(1f, target.GetEffect<WetEffect>().Moisture);
                Assert.AreEqual(1, dirty, "A notification failure must not hide an already-paid visual change.");
                Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "EmergencyDousingUsed" }).Records.Count);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = previous; }
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void RollbackRestartsTheRestoredFireAura(bool clay, bool rollback)
        {
            AsciiFxBus.Clear();
            try
            {
                var target = Place("MarlbackScrabbler"); var burn = new BurningEffect();
                Assert.True(target.ApplyEffect(burn, zone: Zone));
                var initial = AsciiFxBus.Drain();
                Assert.True(initial.Any(r => r.Type == AsciiFxRequestType.AuraStart && r.Anchor == target && r.Theme == AsciiFxTheme.Fire));
                foreach (var request in initial) AsciiFxBus.Release(request);
                var item = clay ? Carry("FireClay") : Skin(2); string command = Select(item, target, clay);
                if (rollback) FailAfter();
                Assert.AreEqual(!rollback, Act(item, command));
                Assert.AreEqual(rollback, target.HasEffect<BurningEffect>());
                if (rollback) Assert.AreSame(burn, target.GetEffect<BurningEffect>());
                Assert.AreEqual(rollback ? 1 : 0, clay ? Pack.Objects.Count(e => e == item) : Units(item) - 1);
                var requests = AsciiFxBus.Drain();
                try
                {
                    CollectionAssert.AreEqual(rollback
                            ? new[] { AsciiFxRequestType.AuraStop, AsciiFxRequestType.AuraStart }
                            : new[] { AsciiFxRequestType.AuraStop },
                        requests.Where(r => r.Anchor == target && r.Theme == AsciiFxTheme.Fire).Select(r => r.Type).ToArray());
                }
                finally { foreach (var request in requests) AsciiFxBus.Release(request); }
            }
            finally { AsciiFxBus.Clear(); }
        }

        sealed class Hook : Part
        {
            readonly string eventId; readonly Func<bool> callback;
            public Hook(string id, Func<bool> action) { eventId = id; callback = action; }
            public override bool HandleEvent(GameEvent e) => e.ID != eventId || callback();
        }
    }
}
