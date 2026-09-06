using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class CraftOutputProbePart : Part
    {
        public static Action<Entity> Created;
        public override string Name => "CraftOutputProbe";
        public override bool HandleEvent(GameEvent e) { if (e.ID == "ObjectCreated") Created?.Invoke(ParentEntity); return true; }
    }
    public abstract class CraftReceiptFixture : CraftAvailabilityFixture
    {
        private readonly List<string> _probeBlueprints = new List<string>();
        [TearDown] public void ClearOutputProbe()
        { CraftOutputProbePart.Created = null; foreach (var bp in _probeBlueprints) Factory.Blueprints[bp].Parts.Remove("CraftOutputProbe"); _probeBlueprints.Clear(); }
        protected void Probe(string bp, Action<Entity> callback)
        { Factory.RegisterPartType<CraftOutputProbePart>("CraftOutputProbe"); Factory.Blueprints[bp].Parts.Add("CraftOutputProbe", new Dictionary<string, string>()); _probeBlueprints.Add(bp); CraftOutputProbePart.Created = callback; }
        protected Entity BrewUnit(Entity actor)
        { var reagent = CarryUnit(actor, "GlimmerBrine"); Assert.IsTrue(BrewingService.TryBrew(actor, Factory, new[] { reagent }, out var made, out _, out var why), why); Assert.NotNull(made); return made; }
        protected void BuildSetup(Entity actor, int made = 1, string ingredient = null)
        { Fund(actor, "craft_dagger"); Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("craft_dagger", out var recipe)); recipe.NumberMade = made; if (ingredient != null) recipe.Ingredient = ingredient; }
        protected sealed class PackState
        {
            readonly InventoryPart _inventory; readonly Entity[] _items; readonly int[] _counts; readonly Entity[] _owners; readonly string[] _ids; readonly int _weight;
            public PackState(InventoryPart inventory)
            { _inventory = inventory; _items = inventory.Objects.ToArray(); _counts = _items.Select(Quantity).ToArray(); _owners = _items.Select(i => i.GetPart<PhysicsPart>()?.InInventory).ToArray(); _ids = _items.Select(i => i.ID).ToArray(); _weight = inventory.GetCarriedWeight(); }
            public void AssertRestored()
            {
                CollectionAssert.AreEqual(_items, _inventory.Objects); Assert.AreEqual(_weight, _inventory.GetCarriedWeight());
                for (int i = 0; i < _items.Length; i++) { Assert.AreEqual(_counts[i], Quantity(_items[i])); Assert.AreSame(_owners[i], _items[i].GetPart<PhysicsPart>()?.InInventory); Assert.AreEqual(_ids[i], _items[i].ID); }
            }
        }
    }
    public class GameAuditCraftReceiptTests : CraftReceiptFixture
    {
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void SameActorPreparationRefusesNestedCraftAcrossServices(bool outerBrew, bool innerBrew)
        {
            var actor = Crafter(); BuildSetup(actor); var reagent = CarryUnit(actor, "GlimmerBrine", 3);
            bool? nested = null;
            Probe(outerBrew ? "BrewedTonic" : "Dagger", createdProbe => {
                CraftOutputProbePart.Created = null; // Bounded one-shot callback, even without the guard.
                nested = innerBrew ? BrewingService.TryBrew(actor, Factory, new[] { reagent }, out _, out _, out _)
                    : TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _);
            });
            bool ok = outerBrew ? BrewingService.TryBrew(actor, Factory, new[] { reagent }, out _, out _, out _)
                : TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _);
            Assert.IsTrue(ok); Assert.AreEqual(false, nested); Assert.AreEqual(outerBrew ? 2 : 3, Quantity(reagent));
            Assert.IsTrue(TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _), "Guard releases after success.");
        }
        [TestCase(false)] [TestCase(true)] public void DifferentActorMayCraftDuringOutputPreparation(bool brew)
        {
            var actor = Crafter(); var other = Crafter(); BuildSetup(actor); BuildSetup(other); var reagent = CarryUnit(actor, "GlimmerBrine");
            bool? nested = null;
            Probe(brew ? "BrewedTonic" : "Dagger", createdProbe => { CraftOutputProbePart.Created = null; nested = TinkeringService.TryCraft(other, Factory, "craft_dagger", out _, out _); });
            Assert.IsTrue(brew ? BrewingService.TryBrew(actor, Factory, new[] { reagent }, out _, out _, out _) : TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _));
            Assert.AreEqual(true, nested); Assert.AreEqual(1, other.GetPart<InventoryPart>().Objects.Count(i => i.BlueprintName == "Dagger"));
        }
        [TestCase(false)] [TestCase(true)] public void PreparationExceptionReleasesActorForRetry(bool brew)
        {
            var actor = Crafter(); BuildSetup(actor); var reagent = CarryUnit(actor, "GlimmerBrine", 2);
            Probe(brew ? "BrewedTonic" : "Dagger", _ => throw new InvalidOperationException("guard release"));
            Assert.Throws<InvalidOperationException>(() => { if (brew) BrewingService.TryBrew(actor, Factory, new[] { reagent }, out _, out _, out _); else TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _); });
            CraftOutputProbePart.Created = null;
            Assert.IsTrue(brew ? BrewingService.TryBrew(actor, Factory, new[] { reagent }, out _, out _, out _) : TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _));
        }
        [TestCase(false)] [TestCase(true)] public void OrdinaryBrewReturnsTheActualResidentAfterAppendOrMerge(bool existing)
        {
            var actor = Crafter(); var first = existing ? BrewUnit(actor) : null; var made = BrewUnit(actor); var inv = actor.GetPart<InventoryPart>();
            Assert.Contains(made, inv.Objects); Assert.AreSame(actor, made.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(existing ? 2 : 1, Quantity(made));
            if (existing) Assert.AreSame(first, made); Assert.AreEqual(1, inv.Objects.Count(i => i.BlueprintName == "BrewedTonic"));
        }
        [TestCase(1)] [TestCase(3)] [TestCase(5)] public void BrewBatchCountsUnitsWithRepeatedResidentReferences(int request)
        {
            var actor = Crafter(); var reagent = CarryUnit(actor, "GlimmerBrine", 3);
            Assert.IsTrue(BrewingService.TryBrewBatch(actor, Factory, new[] { reagent }, request, out var outputs, out var results, out int made, out var why), why);
            Assert.AreEqual(Math.Min(3, request), made); Assert.AreEqual(made, outputs.Count); Assert.AreEqual(made, results.Count);
            Assert.IsTrue(outputs.All(e => ReferenceEquals(e, outputs[0]) && actor.GetPart<InventoryPart>().Objects.Contains(e))); Assert.AreEqual(made, Quantity(outputs[0]));
        }
        [TestCase("none")] [TestCase("compatible")] [TestCase("modified")] [TestCase("full")] [TestCase("empty")] [TestCase("negative")]
        public void TinkerOutputReturnsItsPositiveRecipientWithoutMergingIntoEmptyStacks(string prior)
        {
            var actor = Crafter(); Entity first = prior == "none" ? null : CarryUnit(actor, "Dagger", prior == "full" ? 99 : 1);
            if (prior == "modified") { Fund(actor, "mod_sharp_melee"); Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_sharp_melee", first, out _)); }
            if (prior == "empty" || prior == "negative") first.GetPart<StackerPart>().StackCount = prior == "empty" ? 0 : -1;
            BuildSetup(actor); Assert.IsTrue(TinkeringService.TryCraft(actor, Factory, "craft_dagger", out var outputs, out var why), why);
            Assert.AreEqual(1, outputs.Count); Assert.Contains(outputs[0], actor.GetPart<InventoryPart>().Objects); Assert.Greater(Quantity(outputs[0]), 0);
            Assert.AreSame(actor, outputs[0].GetPart<PhysicsPart>().InInventory); Assert.AreEqual(prior == "compatible", ReferenceEquals(first, outputs[0]));
            if (prior == "empty" || prior == "negative") Assert.AreEqual(prior == "empty" ? 0 : -1, Quantity(first));
        }
        // NumberMade2 is supported configuration; all authored V1 recipes currently make1.
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void MultioutputRefusalRestoresExactPlainSiblingInsteadOfEnhancedOne(bool reverse, bool enoughCapacity)
        {
            var actor = Crafter(); var inv = actor.GetPart<InventoryPart>(); var modified = CarryUnit(actor, "Dagger"); Fund(actor, "mod_sharp_melee");
            Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_sharp_melee", modified, out _)); var plain = CarryUnit(actor, "Dagger");
            if (reverse) inv.Objects.Reverse(); BuildSetup(actor, 2); int b = actor.GetPart<BitLockerPart>().GetBitCount('B'), c = actor.GetPart<BitLockerPart>().GetBitCount('C');
            inv.MaxWeight = inv.GetCarriedWeight() + InventoryPart.GetItemWeight(plain) * (enoughCapacity ? 2 : 1); var before = new PackState(inv); int pen = modified.GetPart<MeleeWeaponPart>().PenBonus;
            Assert.AreEqual(enoughCapacity, TinkeringService.TryCraft(actor, Factory, "craft_dagger", out var outputs, out var why), why);
            if (!enoughCapacity) { Assert.IsEmpty(outputs); before.AssertRestored(); }
            else { Assert.AreEqual(2, outputs.Count); Assert.IsTrue(outputs.All(e => ReferenceEquals(e, plain))); Assert.AreEqual(3, Quantity(plain)); }
            Assert.IsTrue(modified.HasTag("ModSharp")); Assert.AreEqual(pen, modified.GetPart<MeleeWeaponPart>().PenBonus); Assert.AreEqual(1, Quantity(modified));
            Assert.AreEqual(b - (enoughCapacity ? 1 : 0), actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.AreEqual(c - (enoughCapacity ? 1 : 0), actor.GetPart<BitLockerPart>().GetBitCount('C'));
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void FailedIngredientCraftRestoresExactSeparatedSingletonWithoutRemerging(bool separated, bool succeeds)
        {
            var actor = Crafter(); var inv = actor.GetPart<InventoryPart>(); var source = CarryUnit(actor, "PaleSalt", 2);
            if (separated) Assert.IsTrue(InventorySystem.ExecuteCommand(new SeparateOneCommand(source), actor).Success);
            BuildSetup(actor, ingredient: "PaleSalt"); inv.MaxWeight = succeeds ? 100 : inv.GetCarriedWeight(); var before = new PackState(inv); int bits = actor.GetPart<BitLockerPart>().GetBitCount('B');
            Assert.AreEqual(succeeds, TinkeringService.TryCraft(actor, Factory, "craft_dagger", out var outputs, out _));
            if (!succeeds) { before.AssertRestored(); Assert.IsEmpty(outputs); Assert.AreEqual(bits, actor.GetPart<BitLockerPart>().GetBitCount('B')); }
            else { Assert.AreEqual(1, outputs.Count); Assert.AreEqual(bits - 1, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.AreEqual(1, inv.Objects.Where(e => e.BlueprintName == "PaleSalt").Sum(Quantity)); }
        }
        [TestCase(false)] [TestCase(true)] public void OutputCreationExceptionCannotSpendPreparedInputs(bool brew)
        {
            var actor = Crafter(); var input = CarryUnit(actor, brew ? "GlimmerBrine" : "PaleSalt", 2); BuildSetup(actor, ingredient: "PaleSalt"); var before = new PackState(actor.GetPart<InventoryPart>()); int bits = actor.GetPart<BitLockerPart>().GetBitCount('B');
            Probe(brew ? "BrewedTonic" : "Dagger", _ => throw new InvalidOperationException("creation probe"));
            Assert.Throws<InvalidOperationException>(() => { if (brew) BrewingService.TryBrew(actor, Factory, new[] { input }, out _, out _, out _); else TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _); });
            before.AssertRestored(); Assert.AreEqual(bits, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.IsNull(actor.GetPart<BrewKnowledgePart>());
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)]
        public void FactoryReplacementOfPaymentPartRefusesBeforeChargingDetachedState(bool brew, bool bits)
        {
            var actor = Crafter(); var inv = actor.GetPart<InventoryPart>(); var locker = actor.GetPart<BitLockerPart>(); var input = CarryUnit(actor, brew ? "GlimmerBrine" : "PaleSalt", 2); BuildSetup(actor, ingredient: "PaleSalt"); var before = new PackState(inv); int b = locker.GetBitCount('B');
            Probe(brew ? "BrewedTonic" : "Dagger", _ => { if (bits) { actor.RemovePart(locker); actor.AddPart(new BitLockerPart()); } else { actor.RemovePart(inv); actor.AddPart(new InventoryPart()); } });
            bool ok = brew ? BrewingService.TryBrew(actor, Factory, new[] { input }, out _, out _, out _) : TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _);
            Assert.IsFalse(ok); before.AssertRestored(); Assert.AreEqual(b, locker.GetBitCount('B')); Assert.AreEqual(bits, ReferenceEquals(inv, actor.GetPart<InventoryPart>()));
        }
        [TestCase(false)] [TestCase(true)] public void ClaimedDestinationRefusesThenRetryReachesItsActualRecipient(bool brew)
        {
            var actor = Crafter(); var first = brew ? BrewUnit(actor) : CarryUnit(actor, "Dagger"); var reagent = brew ? CarryUnit(actor, "GlimmerBrine") : null; BuildSetup(actor);
            var before = new PackState(actor.GetPart<InventoryPart>()); int bits = actor.GetPart<BitLockerPart>().GetBitCount('B'); var held = new InventoryTransaction(); Assert.IsTrue((bool)typeof(InventoryTransaction).GetMethod("TryClaim", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(held, new object[] { first, actor, "held test transfer" }));
            try { bool ok = brew ? BrewingService.TryBrew(actor, Factory, new[] { reagent }, out _, out _, out _) : TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _); Assert.IsFalse(ok); before.AssertRestored(); Assert.AreEqual(bits, actor.GetPart<BitLockerPart>().GetBitCount('B')); }
            finally { held.Rollback(); }
            if (brew) { Assert.IsTrue(BrewingService.TryBrew(actor, Factory, new[] { reagent }, out var made, out _, out var why), why); Assert.AreSame(first, made); }
            else { Assert.IsTrue(TinkeringService.TryCraft(actor, Factory, "craft_dagger", out var made, out var why), why); Assert.AreSame(first, made.Single()); }
            Assert.AreEqual(2, Quantity(first));
        }
    }
}
