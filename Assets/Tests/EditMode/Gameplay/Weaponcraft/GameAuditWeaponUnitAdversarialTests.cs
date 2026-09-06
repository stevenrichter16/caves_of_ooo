using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Tests.TestSupport;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditWeaponUnitAdversarialTests : WeaponUnitFixture
    {
        [TearDown] public void ResetCallbacks() { MessageLog.OnMessage = null; CloneProbe.Callback = null; }
        [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
        public void SameComponentRemergesBothOutputsAndRollsBackWithoutAliasLoss(int count, bool rollback)
        {
            var actor = Crafter(); var source = Stock(actor, count); var spare = Units(actor, "SteelBladeComponent", 2);
            CraftingMarkPart.Toggle(source); var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); var parts = source.Parts.ToArray();
            var tx = new InventoryTransaction(); var command = new ReforgeWeaponCommand(source, spare, Factory);
            Assert.IsTrue(command.Execute(new InventoryContext(actor), tx).Success); Assert.AreSame(source, command.ReforgedWeapon);
            Assert.AreEqual(count, Quantity(source)); Assert.AreEqual(2, Quantity(spare)); Assert.IsTrue(CraftingMarkPart.IsMarked(source));
            if (rollback) tx.Rollback(); else tx.Commit();
            CollectionAssert.AreEqual(before, inv.Objects); CollectionAssert.AreEqual(parts, source.Parts);
            Assert.AreEqual(count, Quantity(source)); Assert.AreEqual(2, Quantity(spare));
        }
        [TestCase(1, false)] [TestCase(1, true)] [TestCase(99, false)] [TestCase(99, true)]
        public void TemperReturnsActualReceivingStackAndMovesOnlyExistingSelection(int receiverCount, bool marked)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 2); var medium = Quench(actor, 2);
            var existing = Tempered(true); existing.GetPart<StackerPart>().StackCount = receiverCount;
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(existing));
            if (marked) CraftingMarkPart.Toggle(source); else CraftingMarkPart.Toggle(existing);
            Assert.IsTrue(WeaponTemperingService.TryTemper(actor, source, medium, out var changed, out var why), why);
            Assert.AreEqual(receiverCount < 99, ReferenceEquals(existing, changed));
            Assert.AreEqual(receiverCount + 2, Weapons(actor).Sum(Quantity)); Assert.AreEqual(1, Quantity(source)); Assert.AreEqual(1, Quantity(medium));
            Assert.IsFalse(CraftingMarkPart.IsMarked(source));
            Assert.AreEqual(marked || receiverCount < 99, CraftingMarkPart.IsMarked(changed));
            Assert.IsTrue(CraftingMarkPart.IsMarked(existing) || marked && receiverCount == 99);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void FinalWeightUsesActualRecipientAndAllowsNonIncreasingOverweightTransform(bool heavyRecipient, bool alreadyOverweight)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 2); source.GetPart<PhysicsPart>().Weight = 1;
            var medium = Quench(actor, 2); medium.GetPart<PhysicsPart>().Weight = 0;
            var existing = Tempered(true); existing.GetPart<PhysicsPart>().Weight = heavyRecipient ? 10 : 1;
            var inv = actor.GetPart<InventoryPart>(); Assert.IsTrue(inv.AddObject(existing)); CraftingMarkPart.Toggle(source);
            var before = inv.Objects.ToArray(); int weight = inv.GetCarriedWeight(); inv.MaxWeight = weight - (alreadyOverweight ? 1 : 0);
            bool ok = WeaponTemperingService.TryTemper(actor, source, medium, out var changed, out _);
            Assert.AreEqual(!heavyRecipient, ok); Assert.AreEqual(weight, inv.GetCarriedWeight());
            if (ok) { Assert.AreSame(existing, changed); Assert.AreEqual(2, Quantity(existing)); Assert.AreEqual(1, Quantity(medium)); }
            else { Assert.IsNull(changed); CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(2, Quantity(source)); Assert.AreEqual(2, Quantity(medium)); Assert.AreEqual(1, Quantity(existing)); Assert.IsTrue(CraftingMarkPart.IsMarked(source)); }
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void ReforgeCapacityRestoresExactDisplacedMergeRecipient(bool heavyRecipient, bool unlimited)
        {
            var actor = Crafter(); var source = Stock(actor, 2); var replacement = Units(actor, "IronSpikeComponent", 2); replacement.GetPart<PhysicsPart>().Weight = 0;
            var existing = Units(actor, "SteelBladeComponent", 2); existing.GetPart<PhysicsPart>().Weight = heavyRecipient ? 10 : 0;
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); int weight = inv.GetCarriedWeight(); inv.MaxWeight = unlimited ? -1 : weight;
            bool ok = WeaponForgingService.TryReforge(actor, Factory, source, replacement, out var changed, out var returned, out _);
            Assert.AreEqual(unlimited || !heavyRecipient, ok);
            if (ok) { Assert.AreSame(existing, returned); Assert.IsTrue(inv.Objects.Contains(changed)); Assert.AreEqual(3, Quantity(existing)); Assert.AreEqual(1, Quantity(source)); }
            else { CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(2, Quantity(source)); Assert.AreEqual(2, Quantity(replacement)); Assert.AreEqual(2, Quantity(existing)); Assert.AreEqual(weight, inv.GetCarriedWeight()); }
        }
        [TestCase(false, 1)] [TestCase(false, 2)] [TestCase(true, 1)] [TestCase(true, 2)]
        public void ThrowingPublicationRestoresPaymentPayloadAndExactMark(bool reforge, int count)
        {
            Diag.SetChannel("event", true);
            var actor = Crafter(); var source = Stock(actor, count); var medium = reforge ? Units(actor, "IronSpikeComponent", 2) : Quench(actor, 2);
            CraftingMarkPart.Toggle(source); var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); var parts = source.Parts.ToArray(); string name = Name(source);
            MessageLog.OnMessage = _ => throw new InvalidOperationException("publication probe");
            try
            {
                Assert.Throws<InvalidOperationException>(() => { if (reforge) WeaponForgingService.TryReforge(actor, Factory, source, medium, out _, out _); else WeaponTemperingService.TryTemper(actor, source, medium, out _); });
            }
            finally { MessageLog.OnMessage = null; }
            CollectionAssert.AreEqual(before, inv.Objects); CollectionAssert.AreEqual(parts, source.Parts);
            Assert.AreEqual(name, Name(source)); Assert.AreEqual(count, Quantity(source)); Assert.AreEqual(2, Quantity(medium));
            Assert.AreEqual(0, DiagQuery.Count(new DiagQuery.Filter { Kind = "WeaponCraftingRollbackFailed" }).Count);
            Assert.AreEqual(0, TemperCount(source)); Assert.AreEqual("SteelBladeComponent", source.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            Assert.IsTrue(WeaponTemperingService.TryTemper(actor, source, QuenchAfterFailure(actor, reforge, medium), out _), "claims released after exception");
        }
        private Entity QuenchAfterFailure(Entity actor, bool reforge, Entity payment) => reforge ? Quench(actor) : payment;
        [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
        public void NestedSameTransactionPublicationUsesTrueReverseRollback(int count, bool throws)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", count); var medium = Quench(actor, 2);
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); var tx = new InventoryTransaction(); bool entered = false;
            MessageLog.OnMessage = _ =>
            {
                if (entered) return; entered = true;
                var first = Weapons(actor).Single(w => TemperCount(w) == 1);
                Assert.IsTrue(new TemperWeaponCommand(first, medium).Execute(new InventoryContext(actor), tx).Success);
                if (throws) throw new InvalidOperationException("nested publication probe");
            };
            try
            {
                if (throws) Assert.Throws<InvalidOperationException>(() => new TemperWeaponCommand(source, medium).Execute(new InventoryContext(actor), tx));
                else Assert.IsTrue(new TemperWeaponCommand(source, medium).Execute(new InventoryContext(actor), tx).Success);
            }
            finally { MessageLog.OnMessage = null; }
            Assert.IsTrue(entered);
            if (throws)
            { tx.Rollback(); CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(count, Quantity(source)); Assert.AreEqual(2, Quantity(medium)); Assert.AreEqual(0, TemperCount(source)); }
            else { tx.Commit(); Assert.AreEqual(count, Weapons(actor).Sum(Quantity)); Assert.AreEqual(1, Weapons(actor).Where(w => TemperCount(w) == 2).Sum(Quantity)); Assert.IsFalse(inv.Objects.Contains(medium)); }
        }
        [TestCase(false)] [TestCase(true)]
        public void SavedSplitRetainsDistinctAddressableIdentityAndSelection(bool reforge)
        {
            var actor = Crafter(); var source = Stock(actor, 2); CraftingMarkPart.Toggle(source);
            var medium = reforge ? Units(actor, "IronSpikeComponent", 1) : Quench(actor);
            Assert.IsTrue(reforge ? WeaponForgingService.TryReforge(actor, Factory, source, medium, out _, out _) : WeaponTemperingService.TryTemper(actor, source, medium, out _));
            actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor); var weapons = Weapons(actor);
            Assert.AreEqual(2, weapons.Length); Assert.AreEqual(2, weapons.Select(w => w.ID).Distinct().Count()); Assert.IsTrue(weapons.All(w => !string.IsNullOrEmpty(w.ID)));
            var marked = CraftingMarkPart.CollectMarked(actor).Weapons.Single(); Assert.AreEqual(1, Quantity(marked));
            Assert.IsTrue(reforge ? marked.GetPart<WeaponAssemblyPart>().BladeBlueprint == "IronSpikeComponent" : TemperCount(marked) == 1);
            var actions = WorldInteractionSystem.GatherActions(Item("TinkersForge"), actor);
            Assert.IsTrue(actions.Any(a => a.Command == "CraftToggle:" + marked.ID));
        }
        [TestCase(false)] [TestCase(true)]
        public void SplitPreservesPaidModificationsWithoutSharingEnhancementParts(bool reforge)
        {
            Entity Paid(Entity owner)
            {
                var weapon = Stock(owner, 1); var locker = owner.GetPart<BitLockerPart>(); locker.LearnRecipe("mod_sharp_melee"); locker.AddBits("BC");
                Assert.IsTrue(TinkeringService.TryApplyModification(owner, "mod_sharp_melee", weapon, out var why), why); Give(owner, "PaleSalt");
                Assert.IsTrue(TinkeringService.TryApplyModification(owner, "mod_palesalt_infuse", weapon, out why), why); return weapon;
            }
            var actor = Crafter(); var source = Paid(actor); var bits = actor.GetPart<BitLockerPart>();
            var other = Crafter(); var duplicate = Paid(other); Assert.IsTrue(other.GetPart<InventoryPart>().RemoveObject(duplicate));
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(duplicate)); Assert.AreEqual(2, Quantity(source));
            var originalPart = source.GetPart<EnhancementPaleSalt>(); var medium = reforge ? Units(actor, "IronSpikeComponent", 1) : Quench(actor);
            Entity changed;
            Assert.IsTrue(reforge ? WeaponForgingService.TryReforge(actor, Factory, source, medium, out changed, out _, out _) : WeaponTemperingService.TryTemper(actor, source, medium, out changed, out _));
            Assert.AreNotSame(source, changed); Assert.AreSame(originalPart, source.GetPart<EnhancementPaleSalt>());
            Assert.AreNotSame(originalPart, changed.GetPart<EnhancementPaleSalt>()); Assert.AreEqual(originalPart.BonusDamage, changed.GetPart<EnhancementPaleSalt>().BonusDamage);
            Assert.IsTrue(changed.HasTag("ModSharp")); Assert.AreEqual(2, changed.GetIntProperty("ModificationCount")); Assert.AreEqual(0, bits.GetBitCount('B')); Assert.AreEqual(0, bits.GetBitCount('C'));
        }
        [TestCase(false)] [TestCase(true)]
        public void PartialBatchCanCommitEarlierUnitsOrUndoTheWholeCommand(bool rollback)
        {
            var actor = Crafter(); var c = Components(actor, 2); var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray();
            var tx = new InventoryTransaction(); var command = new ForgeWeaponCommand(c[0], c[1], c[2], Factory, 3);
            Assert.IsTrue(command.Execute(new InventoryContext(actor), tx).Success); Assert.AreEqual(2, command.ForgedWeapons.Count);
            Assert.AreSame(command.ForgedWeapons[0], command.ForgedWeapons[1]);
            if (rollback) { tx.Rollback(); CollectionAssert.AreEqual(before, inv.Objects); Assert.IsTrue(c.All(i => Quantity(i) == 2)); }
            else { tx.Commit(); Assert.AreEqual(2, Weapons(actor).Sum(Quantity)); Assert.IsTrue(c.All(i => !inv.Objects.Contains(i))); }
        }
        public class CloneProbe : Part { public static Action Callback; public override string Name => "CloneProbe"; public override void Initialize() { Callback?.Invoke(); } }
        [TestCase(false)] [TestCase(true)]
        public void ClonePreparationKeepsIndependentCompletedWorkOnRefusal(bool throwDuringClone)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 2); var medium = Quench(actor, 2); var torch = Give(actor, "Torch");
            var zone = ForgeZone(actor, Item("TinkersForge")); source.AddPart(new CloneProbe());
            CloneProbe.Callback = () =>
            {
                CloneProbe.Callback = null;
                Assert.IsTrue(InventorySystem.ExecuteCommand(new DropCommand(torch), actor, zone).Success);
                if (throwDuringClone) throw new InvalidOperationException("clone preparation probe");
            };
            if (!throwDuringClone) MessageLog.OnMessage = message => { if (message.Contains(" quenches ")) throw new InvalidOperationException("publication probe"); };
            try { Assert.Throws<InvalidOperationException>(() => WeaponTemperingService.TryTemper(actor, source, medium, out _)); }
            finally { CloneProbe.Callback = null; MessageLog.OnMessage = null; }
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(torch)); Assert.IsTrue(zone.GetAllEntities().Contains(torch));
            Assert.AreEqual(2, Quantity(source)); Assert.AreEqual(2, Quantity(medium)); Assert.AreEqual(0, TemperCount(source));
        }
        [TestCase(false, 0)] [TestCase(false, -1)] [TestCase(true, 0)] [TestCase(true, -1)]
        public void CraftedUnitNeverMergesIntoEmptyMalformedDestination(bool temper, int count)
        {
            var actor = Crafter(); var invalid = temper ? Tempered(true) : Stock(actor, 1);
            if (temper) Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(invalid));
            invalid.GetPart<StackerPart>().StackCount = count;
            Entity made;
            if (temper)
            { var source = Units(actor, "Dagger", 2); var medium = Quench(actor); Assert.IsTrue(WeaponTemperingService.TryTemper(actor, source, medium, out made, out _)); }
            else
            { var c = Components(actor, 1); Assert.IsTrue(WeaponForgingService.TryForge(actor, Factory, c[0], c[1], c[2], out made, out _)); }
            Assert.AreNotSame(invalid, made); Assert.AreEqual(1, Quantity(made)); Assert.AreEqual(count, Quantity(invalid));
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(made));
        }
        public class ThrowingMark : CraftingMarkPart
        {
            public static bool Throw;
            public override void Initialize() { if (Throw) throw new InvalidOperationException("marker rollback probe"); }
        }
        [TestCase(false)] [TestCase(true)]
        public void OneThrowingRollbackActionCannotPreventQuantityRestoration(bool reforge)
        {
            Diag.SetChannel("event", true);
            var actor = Crafter(); var source = Stock(actor, 2); var payment = reforge ? Units(actor, "IronSpikeComponent", 2) : Quench(actor, 2);
            source.AddPart(new ThrowingMark()); var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray();
            MessageLog.OnMessage = _ => { ThrowingMark.Throw = true; throw new InvalidOperationException("publication probe"); };
            try { Assert.Throws<InvalidOperationException>(() => { if (reforge) WeaponForgingService.TryReforge(actor, Factory, source, payment, out _, out _); else WeaponTemperingService.TryTemper(actor, source, payment, out _); }); }
            finally { ThrowingMark.Throw = false; MessageLog.OnMessage = null; }
            CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(2, Quantity(source)); Assert.AreEqual(2, Quantity(payment));
            var failure = DiagQuery.Apply(new DiagQuery.Filter { Kind = "WeaponCraftingRollbackFailed", Actor = actor.ID }).Records.Single();
            StringAssert.Contains(reforge ? "ReforgeWeapon" : "TemperWeapon", failure.PayloadJson);
            StringAssert.Contains("InvalidOperationException", failure.PayloadJson); StringAssert.Contains("marker rollback probe", failure.PayloadJson);
        }
        [TestCase(false)] [TestCase(true)]
        public void SameTransactionClonePreparationIsUndoneInMutationOrder(bool rollback)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 2); var medium = Quench(actor, 2);
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); source.AddPart(new CloneProbe());
            var tx = new InventoryTransaction(); bool nested = false;
            CloneProbe.Callback = () =>
            {
                CloneProbe.Callback = null; nested = true;
                Assert.IsTrue(new TemperWeaponCommand(source, medium).Execute(new InventoryContext(actor), tx).Success);
            };
            try { Assert.IsTrue(new TemperWeaponCommand(source, medium).Execute(new InventoryContext(actor), tx).Success); }
            finally { CloneProbe.Callback = null; }
            Assert.IsTrue(nested);
            if (rollback) { tx.Rollback(); CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(2, Quantity(source)); Assert.AreEqual(2, Quantity(medium)); Assert.AreEqual(0, TemperCount(source)); }
            else { tx.Commit(); Assert.AreEqual(2, Weapons(actor).Sum(Quantity)); Assert.AreEqual(2, Weapons(actor).Where(w => TemperCount(w) == 1).Sum(Quantity)); Assert.IsFalse(inv.Objects.Contains(medium)); }
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void EquippedGlowSingletonKeepsExactPartsSlotsAndWoundedHealth(bool reforge, bool rollback)
        {
            var actor = Crafter(); var source = Stock(actor, 1); Assert.IsTrue(InventorySystem.Equip(actor, source));
            var bits = actor.GetPart<BitLockerPart>(); bits.LearnRecipe("mod_glowquartz_infuse"); Give(actor, "GlowQuartz");
            Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_glowquartz_infuse", source, out var why), why);
            var medium = Quench(actor, 3); Assert.IsTrue(WeaponTemperingService.TryTemper(actor, source, medium, out why), why);
            var hp = source.GetStat("Hitpoints"); hp.BaseValue = Math.Min(hp.BaseValue, 3);
            int max = hp.Max, current = hp.BaseValue; var parts = source.Parts.ToArray(); var light = source.GetPart<LightSourcePart>();
            var slots = actor.GetPart<Body>().GetParts().Where(b => b._Equipped == source).ToArray();
            var payment = reforge ? Units(actor, "IronSpikeComponent", 1) : medium;
            var tx = new InventoryTransaction(); IInventoryCommand command = reforge ? (IInventoryCommand)new ReforgeWeaponCommand(source, payment, Factory) : new TemperWeaponCommand(source, payment);
            Assert.IsTrue(command.Execute(new InventoryContext(actor), tx).Success);
            if (rollback) tx.Rollback(); else tx.Commit();
            CollectionAssert.AreEqual(parts, source.Parts); CollectionAssert.AreEqual(slots, actor.GetPart<Body>().GetParts().Where(b => b._Equipped == source));
            Assert.AreSame(hp, source.GetStat("Hitpoints")); Assert.AreSame(light, source.GetPart<LightSourcePart>()); Assert.AreEqual(2, light.Radius);
            Assert.IsTrue(source.GetPart<EnhancementGlowQuartz>().AppliedBonus); Assert.AreEqual(current, hp.BaseValue);
            Assert.AreEqual(rollback ? max : reforge ? max + 2 : max - 2, hp.Max);
            Assert.AreEqual(rollback ? 1 : reforge ? 0 : 2, TemperCount(source));
        }
        [TestCase("source", false)] [TestCase("source", true)] [TestCase("payment", false)] [TestCase("payment", true)]
        [TestCase("recipient", false)] [TestCase("recipient", true)]
        public void IndependentPublicationTransferCannotTakeAnyParticipant(string target, bool throws)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 2); var payment = Quench(actor, 2);
            var recipient = Tempered(true); var inv = actor.GetPart<InventoryPart>(); Assert.IsTrue(inv.AddObject(recipient));
            var zone = ForgeZone(actor, Item("TinkersForge")); var before = inv.Objects.ToArray(); bool called = false;
            MessageLog.OnMessage = _ =>
            {
                if (called) return; called = true;
                var item = target == "source" ? source : target == "payment" ? payment : recipient;
                Assert.IsFalse(InventorySystem.ExecuteCommand(new DropCommand(item), actor, zone).Success);
                if (throws) throw new InvalidOperationException("publication probe");
            };
            try
            {
                if (throws) Assert.Throws<InvalidOperationException>(() => WeaponTemperingService.TryTemper(actor, source, payment, out _));
                else Assert.IsTrue(WeaponTemperingService.TryTemper(actor, source, payment, out _));
            }
            finally { MessageLog.OnMessage = null; }
            Assert.IsTrue(called); CollectionAssert.AreEqual(before, inv.Objects); Assert.IsFalse(zone.GetAllEntities().Contains(source));
            Assert.AreEqual(throws ? 2 : 1, Quantity(source)); Assert.AreEqual(throws ? 2 : 1, Quantity(payment)); Assert.AreEqual(throws ? 1 : 2, Quantity(recipient));
        }
        [TestCase(false)] [TestCase(true)]
        public void ForgeFindsLaterRecipientOnlyWhenFirstCompatibleStackIsFull(bool firstFull)
        {
            var actor = Crafter(); var first = Stock(actor, 99); var other = Crafter(); var second = Stock(other, 2);
            Assert.IsTrue(other.GetPart<InventoryPart>().RemoveObject(second)); Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(second));
            if (!firstFull) first.GetPart<StackerPart>().StackCount = 98;
            var c = Components(actor, 1); Assert.IsTrue(WeaponForgingService.TryForge(actor, Factory, c[0], c[1], c[2], out var made, out _));
            Assert.AreSame(firstFull ? second : first, made); Assert.AreEqual(99, Quantity(first)); Assert.AreEqual(firstFull ? 3 : 2, Quantity(second));
        }
        [TestCase(0, 0)] [TestCase(0, -1)] [TestCase(1, 0)] [TestCase(1, -1)] [TestCase(2, 0)] [TestCase(2, -1)]
        public void ExplicitEmptyComponentCannotProduceWeaponOrMisleadBatchPreview(int slot, int count)
        {
            var actor = Crafter(); var c = Components(actor, 2); c[slot].GetPart<StackerPart>().StackCount = count;
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray();
            Assert.AreEqual(0, WeaponForgingService.GetMaxBatchCount(c[0], c[1], c[2]));
            Assert.IsFalse(WeaponForgingService.TryForge(actor, Factory, c[0], c[1], c[2], out var made, out _));
            Assert.IsNull(made); CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(count, Quantity(c[slot])); Assert.IsEmpty(Weapons(actor));
            for (int i = 0; i < c.Length; i++) if (i != slot) Assert.AreEqual(2, Quantity(c[i]));
        }
        [TestCase(false)] [TestCase(true)]
        public void LaterCapacityRefusalDisarmsOnlyThatIteration(bool rollback)
        {
            var actor = Crafter(); var existing = Stock(actor, 1); existing.GetPart<PhysicsPart>().Weight = 12;
            var c = Components(actor, 2); foreach (var component in c) component.GetPart<PhysicsPart>().Weight = 3;
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); int weight = inv.GetCarriedWeight(); inv.MaxWeight = weight + 3;
            var tx = new InventoryTransaction(); var command = new ForgeWeaponCommand(c[0], c[1], c[2], Factory, 2);
            Assert.IsTrue(command.Execute(new InventoryContext(actor), tx).Success);
            Assert.AreEqual(1, command.ForgedWeapons.Count); Assert.AreSame(existing, command.ForgedWeapons[0]); Assert.AreEqual(2, Quantity(existing));
            Assert.IsTrue(c.All(i => Quantity(i) == 1)); Assert.AreEqual(weight + 3, inv.GetCarriedWeight());
            if (rollback) { tx.Rollback(); Assert.AreEqual(1, Quantity(existing)); Assert.IsTrue(c.All(i => Quantity(i) == 2)); Assert.AreEqual(weight, inv.GetCarriedWeight()); }
            else tx.Commit();
            CollectionAssert.AreEqual(before, inv.Objects);
        }
    }
}
