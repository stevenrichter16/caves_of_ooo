using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Spurgrass processing (Docs/MACHINE-SPURGRASS-KIT.md). Section 1 characterises how harvested spines stack,
    /// throw and recover, so declaring them a process input cannot take those away. Section 2 pins the bookkeeping
    /// Parts that gameplay attaches permanently (a throw mark, an empty effects Part): they must never make a declared
    /// input unsplittable or unmergeable, and never make an ingredient ambiguous. Section 3 is the recipe.
    /// </summary>
    public sealed class MachineSpurgrassKitTests : FiftyWorldFixture
    {
        const string Spine = "SpurgrassSpine", Cord = "KnotflaxCord", Leather = "LeatherBindingComponent", Kit = "SerratedEdgeComponent";
        const string Prefix = "MaterialProcess|serrated_kit|";
        EntityFactory oldForge;

        [SetUp] public void SetupKit() { oldForge = ForgePart.Factory; ForgePart.Factory = Factory; MaterialProcessRegistry.ResetForTests(); }
        [TearDown] public void RestoreKit() { ForgePart.Factory = oldForge; MaterialProcessRegistry.ResetForTests(); }

        Entity Stack(int count, string blueprint = Spine) { var first = Carry(blueprint); for (int i = 1; i < count; i++) Carry(blueprint); return first; }
        int Stacks() => Pack.Objects.Count(e => e.BlueprintName == Spine);
        Entity Knife() => Carry("Dagger");
        string Cmd(Entity source) => Choice(source, Prefix);
        bool Offered(Entity source) => Actions(source).Any(a => a.Command.StartsWith(Prefix));

        /// <summary>One unit thrown by the real command from <paramref name="stack"/>, with its LIVE recovery mark intact, and the
        /// actor standing on it. Nothing is attached by hand and the unit is not relocated (relocation would invalidate the mark).</summary>
        Entity ThrownSpine(Entity stack)
        {
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(stack, 12, 10, new Random(17)), Actor, Zone);
            Assert.True(result.Success, result.ErrorMessage);
            var unit = Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == Spine);
            var mark = unit.GetPart<ThrownRecoveryPart>(); Assert.NotNull(mark, "the real throw marks the landed unit");
            Assert.AreEqual(Actor.ID, mark.ThrowerId, "the mark is a live recovery claim"); Assert.Greater(mark.Serial, 0);
            var cell = Zone.GetEntityCell(unit); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, cell.X, cell.Y));
            Assert.AreEqual(Actor.ID, unit.GetPart<ThrownRecoveryPart>().ThrowerId, "still live after the actor walked to it");
            return unit;
        }
        /// <summary>A spine stack whose own entity carries a recovery mark: thrown whole, picked up, then added to.</summary>
        Entity MarkedStack(int count)
        {
            var stack = Carry(Spine); var unit = ThrownSpine(stack); Assert.AreSame(stack, unit);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(unit), Actor, Zone).Success);
            for (int i = 1; i < count; i++) Carry(Spine);
            Assert.AreSame(stack, Pack.Objects.Single(e => e.BlueprintName == Spine)); Assert.AreEqual(count, stack.GetPart<StackerPart>().StackCount);
            Assert.NotNull(stack.GetPart<ThrownRecoveryPart>()); return stack;
        }

        // ════════════════ 1. today's behaviour, pinned ════════════════

        [Test] public void FreshSpinesPackIntoOneStack()
        { Stack(2); Assert.AreEqual(1, Stacks()); Assert.AreEqual(2, Count(Spine)); }

        [Test] public void ThrowingOneSpineFromAStackSeparatesExactlyOneUnit()
        {
            var stack = Stack(3); var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(stack, 11, 10, new Random(17)), Actor, Zone);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount); Assert.AreEqual(3, Count(Spine));
        }

        /// <summary>The first throw preparation attaches ThrownRecoveryPart to the stack itself; later throws of one unit
        /// from that stack must still separate it.</summary>
        [Test] public void AStackCarryingThrowTrackingStillSplitsOffOneUnit()
        {
            var stack = Stack(3); stack.AddPart(new ThrownRecoveryPart());
            Assert.NotNull(stack.GetPart<StackerPart>().SplitStack(1));
            Assert.NotNull(stack.GetPart<StackerPart>().RemoveOne());
            Assert.DoesNotThrow(() => stack.CloneForStack());
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(stack, 11, 10, new Random(17)), Actor, Zone);
            Assert.True(result.Success, result.ErrorMessage);
        }

        [Test] public void ARecoveredSpineMergesBackIntoItsStack()
        {
            var stack = Stack(3); var unit = ThrownSpine(stack);
            Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(unit), Actor, Zone).Success);
            Assert.AreEqual(1, Stacks(), "the recovered unit shares one stack with the rest");
            Assert.AreEqual(3, stack.GetPart<StackerPart>().StackCount);
        }

        [Test] public void TwoRecoveredSpinesMergeWithEachOther()
        {
            var stack = Stack(2); var first = ThrownSpine(stack);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(first), Actor, Zone).Success);
            Assert.AreEqual(1, Stacks()); stack = Pack.Objects.Single(e => e.BlueprintName == Spine);
            var second = ThrownSpine(stack);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(second), Actor, Zone).Success);
            var third = ThrownSpine(Pack.Objects.Single(e => e.BlueprintName == Spine));
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(third), Actor, Zone).Success);
            Assert.AreEqual(1, Stacks(), "marked units merge with each other and with unmarked ones");
            Assert.AreEqual(2, Count(Spine));
        }

        /// <summary>Counter-check: a genuinely different weapon payload still never merges.</summary>
        [Test] public void ASpineWithDifferentWeaponStatsDoesNotMerge()
        {
            var plain = Carry(Spine); var changed = Factory.CreateEntity(Spine); changed.GetPart<MeleeWeaponPart>().PenBonus++;
            Assert.False(plain.GetPart<StackerPart>().CanStackWith(changed));
            Assert.True(Pack.AddObject(changed)); Assert.AreEqual(2, Stacks());
        }

        // ════════════════ 2. bookkeeping Parts never strand a declared input ════════════════

        /// <summary>Latent today for the cord and leather binding (declared inputs since M3c): the first effect that ever
        /// touches an item attaches an effects Part that nothing removes, and an empty one makes the stack unsplittable.</summary>
        [TestCase(Spine)] [TestCase(Cord)] [TestCase(Leather)]
        public void AStackThatHasHadAnEffectStillSplitsAndMerges(string blueprint)
        {
            var stack = Stack(3, blueprint); stack.AddPart(new StatusEffectsPart());
            Assert.NotNull(stack.GetPart<StackerPart>().SplitStack(1), "split");
            Assert.NotNull(stack.GetPart<StackerPart>().RemoveOne(), "remove one");
            Assert.DoesNotThrow(() => stack.CloneForStack());
            var plain = Factory.CreateEntity(blueprint);
            Assert.True(stack.GetPart<StackerPart>().CanStackWith(plain), "an empty effects Part is not a payload difference");
            Assert.True(plain.GetPart<StackerPart>().CanStackWith(stack));
        }

        /// <summary>Counter-check: any OTHER attached Part still protects a declared input from losing private state.</summary>
        [TestCase(Spine)] [TestCase(Cord)]
        public void OtherAttachedPartsStillProtectDeclaredInputsFromLosingPrivateState(string blueprint)
        {
            var stack = Stack(2, blueprint); stack.AddPart(new UnknownPart());
            Assert.Null(stack.GetPart<StackerPart>().SplitStack(1)); Assert.Null(stack.GetPart<StackerPart>().RemoveOne());
            Assert.Throws<InvalidOperationException>(() => stack.CloneForStack()); Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount);
        }
        sealed class IdleEffect : Effect { public IdleEffect() { Duration = DURATION_INDEFINITE; } public override string DisplayName => "idle"; }
        sealed class UnknownPart : Part { public override string Name => "UnknownSpurgrassPart"; public string Secret = "kept"; }

        /// <summary>Counter-check to the allowlist: only an INERT bookkeeping Part is tolerated. A live recovery claim or an
        /// active effect is private state and refuses, so a lazy "any ThrownRecoveryPart is fine" implementation fails here.</summary>
        [TestCase("claim")] [TestCase("claim-thrower-only")] [TestCase("claim-serial-only")] [TestCase("claim-zone-only")] [TestCase("effect")]
        public void ALiveBookkeepingPartStillProtectsTheInput(string live)
        {
            Knife(); var cord = Carry(Cord); var spines = Stack(3);
            if (live == "claim") spines.AddPart(new ThrownRecoveryPart { ThrowerId = Actor.ID, ItemId = spines.ID, Blueprint = Spine, ZoneId = Zone.ZoneID, Serial = 3 });
            else if (live == "claim-thrower-only") spines.AddPart(new ThrownRecoveryPart { ThrowerId = Actor.ID });
            else if (live == "claim-serial-only") spines.AddPart(new ThrownRecoveryPart { Serial = 1 });
            else if (live == "claim-zone-only") spines.AddPart(new ThrownRecoveryPart { ZoneId = Zone.ZoneID });
            else { var effects = new StatusEffectsPart(); spines.AddPart(effects); Assert.True(effects.ApplyEffect(new IdleEffect(), Actor)); Assert.AreEqual(1, effects.EffectCount); }
            Assert.False(Offered(cord)); Assert.Null(spines.GetPart<StackerPart>().SplitStack(1)); Assert.Null(spines.GetPart<StackerPart>().RemoveOne());
            Assert.Throws<InvalidOperationException>(() => spines.CloneForStack());
            Assert.AreEqual(3, Count(Spine)); Assert.AreEqual(1, Count(Cord)); Assert.AreEqual(0, Count(Kit));
        }

        /// <summary>The cord is the SOURCE of the process, not an additional input: the shipped guard_lashing source had the same
        /// latent defect, so the source path must tolerate an empty effects Part too.</summary>
        [Test] public void AnEffectTouchedCordSourceStillWorks()
        {
            Knife(); var cord = Carry(Cord); cord.AddPart(new StatusEffectsPart()); Stack(2);
            Assert.True(Offered(cord)); Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(1, Count(Kit)); Assert.AreEqual(0, Count(Cord));
        }

        [Test] public void NoOneCanRegisterAPolicyOverABuiltInBookkeepingPart()
        {
            var impostor = new Impostor();
            Assert.Throws<ArgumentException>(() => MaterialProcessPayloadPolicies.Register(typeof(ThrownRecoveryPart), impostor));
            Assert.Throws<ArgumentException>(() => MaterialProcessPayloadPolicies.Register(typeof(StatusEffectsPart), impostor));
        }
        sealed class Impostor : IMaterialProcessPartPolicy
        {
            public bool ValidTemplate(System.Collections.Generic.IReadOnlyDictionary<string, string> p) => false;
            public bool MatchesTemplate(Part p, System.Collections.Generic.IReadOnlyDictionary<string, string> a) => false;
            public object Capture(Part p) => null;
            public bool Matches(Part p, object s) => true;
        }

        // ════════════════ 3. the kit ════════════════

        [Test] public void ShippedCatalogueCarriesTheKitAndValidatesAgainstRealContent()
        {
            var d = MaterialProcessRegistry.Get("serrated_kit"); Assert.NotNull(d);
            Assert.AreEqual(0, MaterialProcessRegistry.Validate(Factory).Count);
            Assert.AreEqual(Cord, d.SourceBlueprint); Assert.AreEqual("braid_binding", d.SourceRecipeId); Assert.AreEqual("HandCutting", d.WorkKind);
            Assert.AreEqual(1, d.InputCount); Assert.AreEqual(Spine, d.AdditionalInputBlueprints.Single()); Assert.AreEqual(2, d.AdditionalInputCounts.Single());
            Assert.AreEqual(Kit, d.OutputBlueprints.Single()); Assert.AreEqual(1, d.OutputCounts.Single());
        }

        [TestCase("plain-region-a")] [TestCase("plain-region-b")]
        public void TwoSpinesAndACordBecomeOneKitAwayFromAnyStation(string zoneId)
        {
            Zone.RemoveEntity(Actor); Zone = new Zone(zoneId); SettlementRuntime.ActiveZone = Zone; Assert.True(Zone.AddEntity(Actor, 10, 10));
            var knife = Knife(); var cord = Carry(Cord); Stack(2); Assert.False(cord.HasPart<MaterialProcessInputPart>());
            Assert.True(Act(cord, Cmd(cord)));
            Assert.AreEqual(0, Count(Cord)); Assert.AreEqual(0, Count(Spine)); Assert.AreEqual(1, Count(Kit)); Assert.True(Pack.Objects.Contains(knife));
        }

        [Test] public void MenuDisclosesBothInputsWithoutSpendingThemAndNeverSaysTheSpinesAreCut()
        {
            Knife(); var cord = Carry(Cord); Stack(2);
            var action = Actions(cord).Single(a => a.Command.StartsWith(Prefix));
            Assert.That(action.Display, Does.Contain("spurgrass spine")); Assert.That(action.Display, Does.Contain("serrated edge kit"));
            Assert.That(action.Display, Does.Not.Contain("cut spurgrass")); Assert.AreEqual(1, Count(Cord)); Assert.AreEqual(2, Count(Spine));
        }

        [Test] public void ProducedKitStacksWithAnAuthoredKit()
        {
            Knife(); var cord = Carry(Cord); Stack(2); var authored = Carry(Kit); Assert.True(Act(cord, Cmd(cord)));
            Assert.AreEqual(1, Pack.Objects.Count(e => e.BlueprintName == Kit)); Assert.AreEqual(2, authored.GetPart<StackerPart>().StackCount);
        }

        [TestCase(1, false)] [TestCase(2, true)] [TestCase(3, true)]
        public void ExactQuantityOrNothing(int spines, bool offered)
        {
            Knife(); var cord = Carry(Cord); Stack(spines); Assert.AreEqual(offered, Offered(cord));
            if (offered) { Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(spines - 2, Count(Spine)); Assert.AreEqual(1, Count(Kit)); }
        }

        [Test] public void NoKnifeNoKit() { var cord = Carry(Cord); Stack(2); Assert.False(Offered(cord)); Assert.AreEqual(2, Count(Spine)); }

        [TestCase("stat")] [TestCase("temper")] [TestCase("tag")] [TestCase("property")] [TestCase("unknown")]
        public void AModifiedSpineIsRefusedAndNothingIsSpent(string defect)
        {
            Knife(); var cord = Carry(Cord); var spine = Stack(2);
            if (defect == "stat") spine.GetPart<MeleeWeaponPart>().PenBonus++;
            if (defect == "temper") spine.AddPart(new WeaponTemperPart());
            if (defect == "tag") spine.SetTag("Marked");
            if (defect == "property") spine.Properties["odd"] = "x";
            if (defect == "unknown") spine.AddPart(new UnknownPart());
            Assert.False(Offered(cord)); Assert.AreEqual(1, Count(Cord)); Assert.AreEqual(2, Count(Spine)); Assert.AreEqual(0, Count(Kit));
        }

        /// <summary>The spine has no EquippablePart, so it cannot be wielded at all; the structural claim that matters is that
        /// additional inputs resolve only from the pack. A spine on the ground, even in the actor's own cell, is never counted.</summary>
        [Test] public void OnlyPackedSpinesAreIngredients()
        {
            Knife(); var cord = Carry(Cord); Carry(Spine);
            var wield = InventorySystem.ExecuteCommand(new EquipCommand(Pack.Objects.Single(e => e.BlueprintName == Spine)), Actor, Zone);
            Assert.False(wield.Success, "a spurgrass spine is thrown or carried, never wielded");
            var ground = Place(Spine, 10, 10); Assert.NotNull(Zone.GetEntityCell(ground));
            Assert.False(Offered(cord), "one packed spine and one on the ground do not make two");
            Carry(Spine); Assert.True(Offered(cord)); Assert.True(Act(cord, Cmd(cord)));
            Assert.NotNull(Zone.GetEntityCell(ground), "the spine on the ground is untouched"); Assert.AreEqual(1, Count(Kit));
        }

        /// <summary>The ingredient's mark is bumped when its last unit leaves the pack; that must never make the commit refuse.</summary>
        [TestCase(2)] [TestCase(3)]
        public void AStackCarryingARecoveryMarkIsConsumedExactlyOnce(int count)
        {
            Knife(); var cord = Carry(Cord); MarkedStack(count);
            Assert.True(Offered(cord)); Assert.True(Act(cord, Cmd(cord)), "consuming the last unit bumps the mark's revision");
            Assert.AreEqual(count - 2, Count(Spine)); Assert.AreEqual(1, Count(Kit)); Assert.AreEqual(0, Count(Cord));
        }

        [Test] public void AnEffectTouchedStackIsConsumedToo()
        {
            Knife(); var cord = Carry(Cord); var stack = Stack(2); stack.AddPart(new StatusEffectsPart());
            Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(0, Count(Spine)); Assert.AreEqual(1, Count(Kit));
        }

        /// <summary>Two full stacks, one carrying a mark: the lowest identity is chosen, never an ambiguity refusal.</summary>
        [Test] public void AMarkedAndAnUnmarkedStackDoNotMakeTheIngredientAmbiguous()
        {
            Knife(); var cord = Carry(Cord); var marked = MarkedStack(2); marked.GetPart<StackerPart>().MaxStack = 2;
            var other = Carry(Spine); other.GetPart<StackerPart>().StackCount = 2; Assert.AreNotSame(marked, other);
            marked.ID = "b-marked"; other.ID = "a-plain";
            Assert.True(Offered(cord)); Assert.True(Act(cord, Cmd(cord)));
            Assert.True(Pack.Objects.Contains(marked)); Assert.False(Pack.Objects.Contains(other), "the lowest identity is spent");
        }

        [Test] public void ProcessingNeverTouchesAThrownUnitOnTheGround()
        {
            Knife(); var cord = Carry(Cord); var stack = Stack(3); var thrown = ThrownSpine(stack);
            Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount); Assert.True(Act(cord, Cmd(cord)));
            Assert.AreEqual(1, Count(Spine), "only the unit on the ground is left"); Assert.NotNull(Zone.GetEntityCell(thrown), "still on the ground");
        }

        [Test] public void AConsumedStackNeverLeavesAPendingThrowBehind()
        {
            Actor.GetPart<SkillsPart>().AddSkill("Throwing_MeasuredThrow");
            var target = Place("MarlbackScrabbler", 14, 10); Zone.GetCell(14, 10).IsVisible = Zone.GetCell(14, 10).Explored = true;
            Knife(); var cord = Carry(Cord); var stack = Stack(2);
            Assert.True(ThrowingRules.Prepare(Actor, stack, Zone, target, Zone.GetEntityCell(target), out string reason), reason);
            Assert.True(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.True(Act(cord, Cmd(cord)));
            Assert.False(Actor.GetPart<ThrowingStatePart>().Prepared, "the prepared source no longer exists");
            Assert.DoesNotThrow(() => PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor));
        }

        [Test] public void SavedCordAndSavedSpinesWorkWithoutNewPartsOrOldTokens()
        {
            Knife(); var cord = Carry(Cord); var spines = Stack(2); string old = Cmd(cord);
            var cordCopy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(cord); var spineCopy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(spines);
            Pack.RemoveObject(cord); Pack.RemoveObject(spines); Assert.True(Pack.AddObject(cordCopy)); Assert.True(Pack.AddObject(spineCopy));
            Assert.False(cordCopy.HasPart<MaterialProcessInputPart>()); Assert.False(Act(cordCopy, old)); Assert.True(Act(cordCopy, Cmd(cordCopy)));
            Assert.AreEqual(1, Count(Kit));
        }

        [TestCase(false)] [TestCase(true)]
        public void PartialInputsAndOutputMergeHaveExactOuterRollback(bool fail)
        {
            Knife(); var cord = Carry(Cord); cord.GetPart<StackerPart>().StackCount = 3; var spines = Stack(5);
            var kits = Carry(Kit); kits.GetPart<StackerPart>().StackCount = 4; string command = Cmd(cord); if (fail) FailAfter();
            Assert.AreEqual(!fail, Act(cord, command));
            Assert.AreEqual(fail ? 3 : 2, cord.GetPart<StackerPart>().StackCount); Assert.AreEqual(fail ? 5 : 3, spines.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(fail ? 4 : 5, kits.GetPart<StackerPart>().StackCount);
        }

        [Test] public void ARetainedQuoteNeverReselectsAChangedSpineStack()
        {
            Knife(); var cord = Carry(Cord); var spines = Stack(2); string command = Cmd(cord);
            spines.GetPart<StackerPart>().StackCount++;
            Assert.False(Act(cord, command)); Assert.AreEqual(1, Count(Cord)); Assert.AreEqual(3, Count(Spine)); Assert.AreEqual(0, Count(Kit));
        }

        [Test] public void NonPlayerUsesTheSameRules()
        { Actor.Tags.Remove("Player"); Knife(); var cord = Carry(Cord); Stack(2); Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(1, Count(Kit)); }

        [Test] public void TheRecipeNeverMintsValueOrWeight()
        {
            int Value(string bp) => TradeSystem.GetItemValue(Factory.CreateEntity(bp)); int Weight(string bp) => Factory.CreateEntity(bp).GetPart<PhysicsPart>().Weight;
            Assert.LessOrEqual(Value(Kit), Value(Cord) + 2 * Value(Spine)); Assert.LessOrEqual(Weight(Kit), Weight(Cord) + 2 * Weight(Spine));
        }
    }
}
