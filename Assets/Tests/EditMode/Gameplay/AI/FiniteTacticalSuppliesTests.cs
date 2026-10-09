using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal static class TacticalSupplyContract
    {
        internal static Part Attach(Entity actor)
        {
            var type = typeof(BrainPart).Assembly.GetType("CavesOfOoo.Core.TacticalSupplyPart");
            Assert.NotNull(type, "RED: a saved opt-in tactical water policy is not implemented.");
            var part = (Part)Activator.CreateInstance(type); actor.AddPart(part); return part;
        }
        internal static object Call(Entity actor, string method, params object[] args)
        {
            var part = actor.GetPart("TacticalSupply"); Assert.NotNull(part, "Missing actual opt-in Part.");
            var api = part.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(api, "Missing approved tactical supply contract: " + method);
            try { return api.Invoke(part, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        internal static bool Use(Entity actor, Entity threat, Zone zone)
            => (bool)Call(actor, "TryUseEmergencyWater", threat, zone);
        internal static Entity Water(Entity actor) => (Entity)Call(actor, "FindCarriedWater");
        internal static void Enabled(Entity actor, bool value)
        {
            var part = actor.GetPart("TacticalSupply"); Assert.NotNull(part);
            var field = part.GetType().GetField("SelfDousing"); Assert.NotNull(field); field.SetValue(part, value);
        }
    }

    public sealed class FiniteTacticalSuppliesTests
    {
        FieldMedicineFixture f;
        [SetUp] public void Setup() => f = new FieldMedicineFixture();
        [TearDown] public void Teardown() => f.Dispose();
        Entity Actor(int hp = 20, bool enabled = true)
        {
            var actor = f.Actor(hp, medicine: false);
            actor.GetPart<RenderPart>().DisplayName = "test cindercaller";
            actor.AddPart(new ThermalPart());
            if (enabled) TacticalSupplyContract.Attach(actor);
            return actor;
        }
        Entity Water(Entity actor)
        {
            var shell = f.Factory.CreateEntity("SunbladderShell"); Assert.NotNull(shell);
            Assert.AreEqual(1, shell.GetPart<WaterskinPart>().Charges, "Real authored one-use stock.");
            Assert.True(actor.GetPart<InventoryPart>().AddObject(shell)); return shell;
        }
        BurningEffect Ignite(Entity actor)
        {
            var burn = new BurningEffect(1, null, f.Rng); Assert.True(actor.ApplyEffect(burn)); return burn;
        }
        static int Units(Entity shell) => shell.GetPart<WaterskinPart>().Charges;
        void Unspent(Entity actor, Entity shell, BurningEffect burn)
        {
            Assert.AreEqual(1, Units(shell)); Assert.AreSame(burn, actor.GetEffect<BurningEffect>());
            Assert.False(actor.HasEffect<WetEffect>());
        }

        [TestCase("kill", 20)] [TestCase("kill", 7)] [TestCase("flee", 7)] [TestCase("bored", 7)]
        public void RealWaterUseTakesTheWholeNativeFightOrRetreatOpportunity(string goal, int hp)
        {
            var actor = Actor(hp); var threat = f.Threat(actor); var shell = Water(actor); Ignite(actor);
            var hooks = new TacticalSupplyCommandProbe(); actor.AddPart(hooks);
            int attacks = 0, casts = 0, uses = 0;
            EntityVisualHooks.AttackCallback = (a, b, z) => { if (a == actor) attacks++; };
            EntityVisualHooks.CastCallback = (a, z, s, x, y, tx, ty, d) => { if (a == actor) casts++; };
            EntityVisualHooks.SelfUseCallback = (a, z) =>
            { if (a == actor) { Assert.AreEqual(0, Units(shell)); Assert.False(actor.HasEffect<BurningEffect>()); uses++; } };
            f.Act(actor, threat, goal);
            Assert.AreEqual(0, Units(shell)); Assert.Contains(shell, actor.GetPart<InventoryPart>().Objects);
            Assert.AreSame(actor, shell.GetPart<PhysicsPart>().InInventory); Assert.IsNull(shell.GetPart<PhysicsPart>().Equipped);
            Assert.False(actor.HasEffect<BurningEffect>()); Assert.Greater(actor.GetEffect<WetEffect>().Moisture, .2f);
            Assert.Less(actor.GetPart<ThermalPart>().Temperature, actor.GetPart<ThermalPart>().FlameTemperature);
            Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor)); Assert.AreEqual(20, threat.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, attacks + casts); Assert.AreEqual(1, uses);
            Assert.AreEqual(1, hooks.Before); Assert.AreEqual(1, hooks.After);
            StringAssert.StartsWith("DrenchCreature|", hooks.Command, "Reuse the real inventory command, not a direct status edit.");
        }

        [Test] public void SchedulerStillChargesExactlyOneActionAndOnePreActionEvent()
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor); Ignite(actor);
            var probe = new FieldMedicineTurnProbe(); actor.AddPart(probe);
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(threat));
            var turns = f.ScheduledAction(actor, threat);
            Assert.AreEqual(0, Units(shell)); Assert.AreEqual(1, probe.Begins); Assert.AreEqual(1, probe.Ends);
            Assert.AreEqual(0, turns.GetEnergy(actor)); Assert.AreEqual(20, threat.GetStatValue("Hitpoints"));
        }

        [Test] public void ThrowingSelfUseObserverCannotGrantAFallbackActionAfterTheWaterCommits()
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor); Ignite(actor);
            var probe = new FieldMedicineTurnProbe(); actor.AddPart(probe); int gestures = 0, attacks = 0, casts = 0;
            EntityVisualHooks.SelfUseCallback = (owner, zone) =>
            {
                if (owner != actor) return;
                Assert.AreEqual(0, Units(shell)); Assert.False(actor.HasEffect<BurningEffect>());
                gestures++; throw new InvalidOperationException("independent pose observer");
            };
            EntityVisualHooks.AttackCallback = (owner, target, zone) => { if (owner == actor) attacks++; };
            EntityVisualHooks.CastCallback = (owner, zone, skill, x, y, tx, ty, duration) => { if (owner == actor) casts++; };
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(threat)); var turns = f.ScheduledAction(actor, threat);
            Assert.AreEqual(1, gestures); Assert.AreEqual(0, Units(shell)); Assert.False(actor.HasEffect<BurningEffect>());
            Assert.True(actor.HasEffect<WetEffect>()); Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(0, attacks + casts); Assert.AreEqual(20, threat.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, probe.Begins); Assert.AreEqual(1, probe.Ends); Assert.AreEqual(0, turns.GetEnergy(actor));
        }

        [Test] public void ExtinguishingCreatesTheOrdinaryElectricalVulnerability()
        {
            var actor = Actor(); var threat = f.Threat(actor); Water(actor); Ignite(actor);
            Assert.True(TacticalSupplyContract.Use(actor, threat, f.Zone));
            var shock = new ElectrifiedEffect(1); Assert.True(actor.ApplyEffect(shock)); Assert.AreEqual(2, shock.Charge);
            var dry = f.Actor(20, false, 14, 10); var dryShock = new ElectrifiedEffect(1);
            Assert.True(dry.ApplyEffect(dryShock)); Assert.AreEqual(1, dryShock.Charge);
        }

        [Test] public void TheSameEmptyShellCannotDouseASecondFireOrRefillOnAnotherDecision()
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor); Ignite(actor);
            Assert.True(TacticalSupplyContract.Use(actor, threat, f.Zone)); Assert.IsNull(TacticalSupplyContract.Water(actor));
            actor.RemoveEffect<WetEffect>(); var second = Ignite(actor);
            Assert.False(TacticalSupplyContract.Use(actor, threat, f.Zone));
            Assert.AreEqual(0, Units(shell)); Assert.AreSame(second, actor.GetEffect<BurningEffect>());
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == "SunbladderShell"));
        }

        [TestCase("no-part")] [TestCase("disabled")] [TestCase("not-burning")] [TestCase("empty")]
        public void AnInapplicableSupplyKeepsTheOrdinaryApproachAvailable(string condition)
        {
            var actor = Actor(enabled: condition != "no-part"); var threat = f.Threat(actor, 13); var shell = Water(actor);
            if (condition != "not-burning") Ignite(actor);
            if (condition == "disabled") TacticalSupplyContract.Enabled(actor, false);
            if (condition == "empty") shell.GetPart<WaterskinPart>().Charges = 0;
            f.Act(actor, threat, "kill");
            Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(condition == "empty" ? 0 : 1, Units(shell)); Assert.False(actor.HasEffect<WetEffect>());
        }

        [TestCase("stun")] [TestCase("sleep")] [TestCase("conversation")]
        [TestCase("party-follower")] [TestCase("calm")] [TestCase("nonhostile")]
        public void PolicyRefusesControlledActorsAndInvalidCombatWithoutSpending(string condition)
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor); var burn = Ignite(actor);
            var brain = actor.GetPart<BrainPart>();
            if (condition == "stun") actor.ApplyEffect(new StunnedEffect(5));
            if (condition == "sleep") actor.ApplyEffect(new AsleepByGasEffect(5));
            if (condition == "conversation") brain.InConversation = true;
            if (condition == "party-follower") brain.PartyLeader = threat;
            if (condition == "calm") brain.PushGoal(new NoFightGoal(50, false));
            if (condition == "nonhostile") threat = f.Actor(20, false, 14, 10);
            Assert.False(TacticalSupplyContract.Use(actor, threat, f.Zone)); Unspent(actor, shell, burn);
        }

        [TestCase("floor")] [TestCase("foreign-owner")] [TestCase("equipped-alias")]
        [TestCase("removed-reference")] [TestCase("foreign-vessel")]
        public void OnlyLiteralOwnedCarriedWaterCanBeSpent(string fault)
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor); var burn = Ignite(actor);
            var pack = actor.GetPart<InventoryPart>();
            if (fault == "floor") { Assert.True(pack.RemoveObject(shell)); Assert.True(f.Zone.AddEntity(shell, 10, 10)); }
            if (fault == "foreign-owner") shell.GetPart<PhysicsPart>().InInventory = threat;
            if (fault == "equipped-alias") pack.EquippedItems["forged"] = shell;
            if (fault == "removed-reference") pack.Objects.Remove(shell);
            if (fault == "foreign-vessel") shell.GetPart<WaterskinPart>().ParentEntity = threat;
            Assert.IsNull(TacticalSupplyContract.Water(actor)); Assert.False(TacticalSupplyContract.Use(actor, threat, f.Zone));
            Unspent(actor, shell, burn);
        }

        [TestCase("before-veto")] [TestCase("wet-veto")] [TestCase("after-throw")]
        public void RealCommandFailureRefundsTheExactWaterFlameAndHeatAndDoesNotPublishUse(string failure)
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor); var burn = Ignite(actor);
            float temperature = actor.GetPart<ThermalPart>().Temperature; int uses = 0;
            EntityVisualHooks.SelfUseCallback = (a, z) => uses++;
            actor.AddPart(new TacticalSupplyCommandProbe { VetoBefore = failure == "before-veto", ThrowAfter = failure == "after-throw" });
            if (failure == "wet-veto") actor.AddPart(new CombatSupplyRejectWetPart());
            MessageLog.Clear(); Assert.False(TacticalSupplyContract.Use(actor, threat, f.Zone));
            Unspent(actor, shell, burn); Assert.AreEqual(temperature, actor.GetPart<ThermalPart>().Temperature); Assert.AreEqual(0, uses);
            Assert.False(MessageLog.GetAllEntries().Any(e => e.Text.StartsWith("You ", StringComparison.Ordinal)),
                "NPC refusals must not be reported as failed player commands.");
        }

        [Test] public void ARejectedInventoryCommandFallsBackToMovementOnTheSameGoalOpportunity()
        {
            var actor = Actor(); var threat = f.Threat(actor, 13); var shell = Water(actor); Ignite(actor);
            actor.AddPart(new TacticalSupplyCommandProbe { VetoBefore = true });
            f.Act(actor, threat, "kill");
            Assert.AreEqual(1, Units(shell)); Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor));
            Assert.True(actor.HasEffect<BurningEffect>());
        }

        [TestCase(false)] [TestCase(true)]
        public void PlayerObserverVisibilityChangesFeedbackButNeverSelfTreatment(bool visible)
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor); Ignite(actor);
            f.Zone.GetEntityCell(actor).IsVisible = visible; f.Zone.GetEntityCell(threat).IsVisible = false;
            MessageLog.Clear(); Assert.True(TacticalSupplyContract.Use(actor, threat, f.Zone));
            Assert.AreEqual(0, Units(shell)); Assert.False(actor.HasEffect<BurningEffect>());
            var messages = MessageLog.GetAllEntries().Select(e => e.Text).ToArray();
            Assert.False(messages.Any(m => m.StartsWith("You ", StringComparison.Ordinal)));
            if (visible) Assert.True(messages.Any(m => m.Contains(actor.GetDisplayName()) && m.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0));
            else Assert.IsEmpty(messages, "Wet/Burning lifecycle text must not leak an unseen use either.");
        }

        [Test] public void DescribeAndExamineShowActualFullAndEmptyStockWithoutMutatingIt()
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor);
            var examine = new ExaminablePart { Text = "The old mantle is scorched." }; actor.AddPart(examine);
            string full = examine.BuildWorldExamineLine(f.Zone, f.Zone.GetEntityCell(actor), threat);
            StringAssert.Contains(examine.Text, full); StringAssert.Contains("sunbladder", full.ToLowerInvariant());
            StringAssert.Contains("turn", full.ToLowerInvariant()); Assert.AreEqual(1, Units(shell));
            Ignite(actor); Assert.True(TacticalSupplyContract.Use(actor, threat, f.Zone));
            string empty = examine.BuildWorldExamineLine(f.Zone, f.Zone.GetEntityCell(actor), threat);
            StringAssert.Contains("empty", empty.ToLowerInvariant()); StringAssert.Contains(examine.Text, empty);
            Assert.AreEqual(0, Units(shell)); Assert.AreEqual(0, f.Rng.Calls, "Inspection and dousing do not draw combat randomness.");
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualDeathDropsTheSameFullOrSpentReusableShell(bool used)
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor);
            if (used) { Ignite(actor); Assert.True(TacticalSupplyContract.Use(actor, threat, f.Zone)); }
            CombatSystem.ApplyDamage(actor, new Damage(100), threat, f.Zone);
            Assert.IsNull(f.Zone.GetEntityCell(actor)); Assert.Contains(shell, f.Zone.GetAllEntities());
            Assert.AreEqual(used ? 0 : 1, Units(shell)); Assert.IsNull(shell.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(1, f.Zone.GetAllEntities().Count(e => e.ID == shell.ID));
        }

        [TestCase(false)] [TestCase(true)]
        public void NativeSavePreservesThePartAndActualWaterWithoutManufacturingStock(bool used)
        {
            var actor = Actor(); var threat = f.Threat(actor); var shell = Water(actor);
            if (used) { Ignite(actor); Assert.True(TacticalSupplyContract.Use(actor, threat, f.Zone)); }
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.NotNull(loaded.GetPart("TacticalSupply"));
            var restored = loaded.GetPart<InventoryPart>().Objects.Single(e => e.ID == shell.ID);
            Assert.AreNotSame(shell, restored); Assert.AreEqual(used ? 0 : 1, Units(restored));
            Assert.AreSame(loaded, restored.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(!used, TacticalSupplyContract.Water(loaded) != null);
            Assert.True(f.Zone.RemoveEntity(actor)); Assert.True(f.Zone.AddEntity(loaded, 10, 10));
            loaded.GetPart<BrainPart>().CurrentZone = f.Zone; loaded.GetPart<BrainPart>().SetPersonallyHostile(threat);
            loaded.RemoveEffect<WetEffect>(); Ignite(loaded);
            Assert.AreEqual(!used, TacticalSupplyContract.Use(loaded, threat, f.Zone)); Assert.AreEqual(0, Units(restored));
        }

        [Test] public void DisabledSavedPolicyStaysDisabledWithItsLiteralFullSupply()
        {
            var actor = Actor(); Water(actor); TacticalSupplyContract.Enabled(actor, false);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var part = loaded.GetPart("TacticalSupply"); Assert.NotNull(part);
            Assert.False((bool)part.GetType().GetField("SelfDousing").GetValue(part));
            Assert.AreEqual(1, loaded.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "SunbladderShell").GetPart<WaterskinPart>().Charges);
        }
    }

    public sealed class TacticalSupplyCommandProbe : Part
    {
        public override string Name => "TacticalSupplyCommandProbe";
        public int Before, After;
        public string Command;
        public bool VetoBefore, ThrowAfter;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeInventoryAction") { Before++; Command = e.GetStringParameter("Command"); if (VetoBefore) return false; }
            if (e.ID == "AfterInventoryAction") { After++; if (ThrowAfter) throw new InvalidOperationException("outer tactical supply observer"); }
            return true;
        }
    }

    public sealed class FiniteTacticalSupplySourceTests
    {
        // Native source probe confirms the existing seed64 pair here. The closer
        // 12.10 shelter legitimately has only one source hostile in this cohort.
        const string Shelter = "Overworld.15.10.0";
        [Test] public void OrdinaryFactoryAndLoadedOldActorsDoNotAcquireTheNewSitePolicy()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var ordinary = scope.Factory.CreateEntity("MarlbackCindercaller");
                Assert.IsNull(ordinary.GetPart("TacticalSupply"));
                Assert.False(ordinary.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "SunbladderShell"));
                var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(ordinary);
                Assert.IsNull(restored.GetPart("TacticalSupply"));
                Assert.False(restored.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "SunbladderShell"));
            }
        }

        [Test] public void FixedFreshSiteCensusFindsAnOptedInOriginalOwnerAndDoesNotReplenishCachedStock()
        {
            int witnesses = 0;
            foreach (int seed in new[] { 64, 1729, 729490642 })
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(seed); var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
                var zone = manager.GetZone(Shelter); Assert.NotNull(zone);
                var casters = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "MarlbackCindercaller").ToArray();
                Assert.LessOrEqual(casters.Length, 1, "Enrichment cannot add another encounter.");
                if (casters.Length == 0) continue; // Keep real optional placement absence; aggregate must witness the source.
                var owner = casters[0]; Assert.NotNull(owner.GetPart("TacticalSupply"));
                var inventory = owner.GetPart<InventoryPart>();
                var shell = inventory.Objects.Single(e => e.BlueprintName == "SunbladderShell");
                Assert.AreEqual(1, shell.GetPart<WaterskinPart>().Charges); Assert.AreSame(owner, shell.GetPart<PhysicsPart>().InInventory);
                Assert.True(inventory.Objects.Any(e => e.BlueprintName == "FireMoss"));
                Assert.True(inventory.GetAllEquipped().Any(e => e.BlueprintName == "Cudgel"));
                Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.GetPart("TacticalSupply") != null));
                shell.GetPart<WaterskinPart>().Charges = 0;
                Assert.AreSame(zone, manager.GetZone(Shelter)); Assert.AreEqual(0, shell.GetPart<WaterskinPart>().Charges);
                Assert.AreSame(shell, inventory.Objects.Single(e => e.BlueprintName == "SunbladderShell")); witnesses++;
            }
            Assert.Greater(witnesses, 0, "The predeclared actual seed set must exercise at least one admitted authored cindercaller.");
        }

        [Test] public void OtherAuthoredWorksitesDoNotGainEmergencyWaterAsABiomeWideShortcut()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                foreach (string id in new[] { "Overworld.11.9.0", "Overworld.11.11.0" })
                {
                    var zone = manager.GetZone(id);
                    Assert.False(zone.GetReadOnlyEntities().Any(e => e.GetPart("TacticalSupply") != null), id);
                }
            }
        }
    }
}
