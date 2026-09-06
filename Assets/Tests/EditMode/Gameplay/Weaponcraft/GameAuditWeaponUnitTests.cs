using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    /// <summary>A46: real output references and exactly one transformed weapon per payment.</summary>
    public abstract class WeaponUnitFixture : StackIdentityFixture
    {
        protected Entity Crafter() { var actor = Actor(); actor.GetPart<InventoryPart>().MaxWeight = -1; return actor; }
        protected Entity Units(Entity actor, string bp, int count)
        {
            var item = Item(bp); item.GetPart<StackerPart>().StackCount = count;
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item)); Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(item), "fixture source must be resident"); return item;
        }
        protected Entity[] Components(Entity actor, int count) => new[] { Units(actor, "SteelBladeComponent", count), Units(actor, "OakHaftComponent", count), Units(actor, "LeatherBindingComponent", count) };
        protected Entity Stock(Entity actor, int count)
        {
            var c = Components(actor, 1); Assert.IsTrue(WeaponForgingService.TryForge(actor, Factory, c[0], c[1], c[2], out var made, out var why), why);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(made)); made.GetPart<StackerPart>().StackCount = count; return made;
        }
        protected Entity Quench(Entity actor, int count = 1)
        { var item = Brew(actor, false, true); item.GetPart<StackerPart>().StackCount = count; return item; }
        protected static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        protected static Entity[] Weapons(Entity actor) => actor.GetPart<InventoryPart>().Objects.Where(i => i.HasPart<MeleeWeaponPart>()).ToArray();
        protected static int Count(Entity actor, string bp) => actor.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == bp).Sum(Quantity);
        protected static int TemperCount(Entity item) => item.GetPart<WeaponTemperPart>()?.TemperCount ?? 0;
        protected static Zone ForgeZone(Entity actor, Entity forge)
        { var zone = new Zone("UnitForge"); Assert.IsTrue(zone.AddEntity(actor, 10, 10)); Assert.IsTrue(zone.AddEntity(forge, 11, 10)); return zone; }
    }
    public class GameAuditWeaponUnitTests : WeaponUnitFixture
    {
        [TestCase("none")] [TestCase("equivalent")] [TestCase("modified")] [TestCase("full")]
        public void SingleForgeReturnsItsActualPositiveCarriedRecipient(string prior)
        {
            var actor = Crafter(); var inv = actor.GetPart<InventoryPart>(); Entity existing = prior == "none" ? null : Stock(actor, prior == "full" ? 99 : 1);
            if (prior == "modified") { var bits = actor.GetPart<BitLockerPart>(); bits.LearnRecipe("mod_sharp_melee"); bits.AddBits("BC"); Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_sharp_melee", existing, out _)); }
            var c = Components(actor, 1);
            Assert.IsTrue(WeaponForgingService.TryForge(actor, Factory, c[0], c[1], c[2], out var made, out var why), why);
            Assert.IsTrue(inv.Objects.Contains(made), "returned output must resolve to actual inventory, including after merge"); Assert.Greater(Quantity(made), 0);
            Assert.AreSame(actor, made.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(prior == "equivalent", ReferenceEquals(existing, made));
            Assert.AreEqual(prior == "full" ? 100 : prior == "none" ? 1 : 2, Weapons(actor).Sum(Quantity));
            Assert.AreEqual(prior == "equivalent" ? 2 : 1, Quantity(made));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(98)] [TestCase(99)]
        public void BatchReturnsOnlyResidentRecipientsWhileCountingProducedUnits(int existingCount)
        {
            var actor = Crafter(); var existing = existingCount == 0 ? null : Stock(actor, existingCount); var c = Components(actor, 2);
            Assert.IsTrue(WeaponForgingService.TryForgeBatch(actor, Factory, c[0], c[1], c[2], 2, out var outputs, out int made, out var why), why);
            Assert.AreEqual(2, made); Assert.AreEqual(2, outputs.Count); Assert.AreEqual(existingCount + 2, Weapons(actor).Sum(Quantity));
            Assert.IsTrue(outputs.All(i => actor.GetPart<InventoryPart>().Objects.Contains(i) && Quantity(i) > 0));
            if (existingCount == 98) { Assert.AreSame(existing, outputs[0]); Assert.AreNotSame(existing, outputs[1]); Assert.AreEqual(99, Quantity(existing)); Assert.AreEqual(1, Quantity(outputs[1])); }
            else Assert.AreSame(outputs[0], outputs[1], "two units can legitimately have the same receiving stack");
        }
        [TestCase(1, 1)] [TestCase(1, 2)] [TestCase(2, 1)] [TestCase(2, 2)]
        public void TemperPaysOneMediumAndChangesExactlyOneWeapon(int weapons, int media)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", weapons); string original = Name(source); var quench = Quench(actor, media);
            Assert.IsTrue(WeaponTemperingService.TryTemper(actor, source, quench, out var why), why);
            var all = Weapons(actor); Assert.AreEqual(weapons, all.Sum(Quantity));
            var changed = all.Single(i => TemperCount(i) == 1); Assert.AreEqual(1, Quantity(changed));
            Assert.AreEqual(media - 1, Count(actor, "BrewedTonic"));
            if (weapons == 1) Assert.AreSame(source, changed);
            else { Assert.AreNotSame(source, changed); Assert.AreEqual(1, Quantity(source)); Assert.AreEqual(0, TemperCount(source)); Assert.AreEqual(original, Name(source)); }
            Assert.IsNotEmpty(changed.ID); if (weapons > 1) Assert.AreNotEqual(source.ID, changed.ID);
        }
        [TestCase(1, 1)] [TestCase(1, 2)] [TestCase(2, 1)] [TestCase(2, 2)]
        public void ReforgePaysOnePartAndChangesExactlyOneAssembly(int weapons, int components)
        {
            var actor = Crafter(); var source = Stock(actor, weapons); var replacement = Units(actor, "IronSpikeComponent", components);
            Assert.IsTrue(WeaponForgingService.TryReforge(actor, Factory, source, replacement, out var returned, out var why), why);
            var all = Weapons(actor); Assert.AreEqual(weapons, all.Sum(Quantity));
            var changed = all.Single(i => i.GetPart<WeaponAssemblyPart>().BladeBlueprint == "IronSpikeComponent"); Assert.AreEqual(1, Quantity(changed));
            Assert.AreEqual(components - 1, Count(actor, "IronSpikeComponent")); Assert.AreEqual(1, Count(actor, "SteelBladeComponent"));
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(returned));
            if (weapons == 1) Assert.AreSame(source, changed);
            else { Assert.AreNotSame(source, changed); Assert.AreEqual(1, Quantity(source)); Assert.AreEqual("SteelBladeComponent", source.GetPart<WeaponAssemblyPart>().BladeBlueprint); }
            Assert.IsNotEmpty(changed.ID); if (weapons > 1) Assert.AreNotEqual(source.ID, changed.ID);
        }
        [TestCase(0, 1, false)] [TestCase(0, 1, true)] [TestCase(0, 2, false)] [TestCase(0, 2, true)]
        [TestCase(1, 1, false)] [TestCase(1, 1, true)] [TestCase(1, 2, false)] [TestCase(1, 2, true)]
        [TestCase(2, 1, false)] [TestCase(2, 1, true)] [TestCase(2, 2, false)] [TestCase(2, 2, true)]
        public void CurrentForgeThenOptionalQuenchCompositeChangesOneProducedUnit(int existing, int batch, bool quenchPicked)
        {
            var actor = Crafter(); if (existing > 0) Stock(actor, existing); var c = Components(actor, batch); var medium = Quench(actor, 2);
            var zone = ForgeZone(actor, Item("TinkersForge")); var forge = new ForgeWeaponCommand(c[0], c[1], c[2], Factory, batch);
            Assert.IsTrue(InventorySystem.ExecuteCommand(forge, actor, zone).Success);
            if (quenchPicked) Assert.IsTrue(InventorySystem.ExecuteCommand(new TemperWeaponCommand(forge.ForgedWeapons[0], medium), actor, zone).Success);
            var all = Weapons(actor); Assert.AreEqual(existing + batch, all.Sum(Quantity));
            Assert.AreEqual(quenchPicked ? 1 : 0, all.Where(i => TemperCount(i) == 1).Sum(Quantity));
            Assert.AreEqual(existing + batch - (quenchPicked ? 1 : 0), all.Where(i => TemperCount(i) == 0).Sum(Quantity));
            Assert.AreEqual(quenchPicked ? 1 : 2, Count(actor, "BrewedTonic"));
        }
        [TestCase(false, false, 0)] [TestCase(false, false, -1)] [TestCase(false, true, 0)] [TestCase(false, true, -1)]
        [TestCase(true, false, 0)] [TestCase(true, false, -1)] [TestCase(true, true, 0)] [TestCase(true, true, -1)]
        public void EmptyTargetOrSelectedPaymentCannotCreateAChangedUnit(bool reforge, bool emptyTarget, int invalidCount)
        {
            var actor = Crafter(); var source = Stock(actor, 2); var payment = reforge ? Units(actor, "IronSpikeComponent", 1) : Quench(actor);
            (emptyTarget ? source : payment).GetPart<StackerPart>().StackCount = invalidCount;
            var contents = actor.GetPart<InventoryPart>().Objects.ToArray(); string name = Name(source);
            bool ok = reforge ? WeaponForgingService.TryReforge(actor, Factory, source, payment, out _, out _) : WeaponTemperingService.TryTemper(actor, source, payment, out _);
            Assert.IsFalse(ok); CollectionAssert.AreEqual(contents, actor.GetPart<InventoryPart>().Objects); Assert.AreEqual(name, Name(source));
            Assert.AreEqual(emptyTarget ? invalidCount : 2, Quantity(source)); Assert.AreEqual(emptyTarget ? 1 : invalidCount, Quantity(payment));
            Assert.AreSame(actor, payment.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(0, TemperCount(source));
        }
        [TestCase(false, 1)] [TestCase(false, 2)] [TestCase(true, 1)] [TestCase(true, 2)]
        public void EquippedSingletonWorksButMalformedEquippedStackRefuses(bool reforge, int count)
        {
            var actor = Crafter(); var weapon = Stock(actor, 1); Assert.IsTrue(InventorySystem.Equip(actor, weapon)); weapon.GetPart<StackerPart>().StackCount = count;
            var slots = actor.GetPart<Body>().GetParts().Where(p => p._Equipped == weapon).ToArray(); Assert.IsNotEmpty(slots);
            var payment = reforge ? Units(actor, "IronSpikeComponent", 1) : Quench(actor);
            bool ok = reforge ? WeaponForgingService.TryReforge(actor, Factory, weapon, payment, out _, out _) : WeaponTemperingService.TryTemper(actor, weapon, payment, out _);
            Assert.AreEqual(count == 1, ok); CollectionAssert.AreEqual(slots, actor.GetPart<Body>().GetParts().Where(p => p._Equipped == weapon));
            Assert.AreEqual(count, Quantity(weapon)); Assert.AreEqual(count > 1, actor.GetPart<InventoryPart>().Objects.Contains(payment));
            Assert.AreEqual(count == 1 && !reforge ? 1 : 0, TemperCount(weapon));
        }
        [TestCase(false, 1, false)] [TestCase(false, 1, true)]
        [TestCase(false, 2, false)] [TestCase(false, 2, true)]
        [TestCase(true, 1, false)] [TestCase(true, 1, true)]
        [TestCase(true, 2, false)] [TestCase(true, 2, true)]
        public void CommandTransformationJoinsOuterCommitOrExactRollback(bool reforge, int count, bool rollback)
        {
            var actor = Crafter(); var source = Stock(actor, count); var inv = actor.GetPart<InventoryPart>();
            var payment = reforge ? Units(actor, "IronSpikeComponent", 2) : Quench(actor, 2);
            CraftingMarkPart.Toggle(source); var before = inv.Objects.ToArray(); var beforeParts = source.Parts.ToArray();
            string name = Name(source), raw = source.GetPart<MeleeWeaponPart>().OnHitEffectsRaw;
            var tx = new InventoryTransaction();
            IInventoryCommand command = reforge ? (IInventoryCommand)new ReforgeWeaponCommand(source, payment, Factory) : new TemperWeaponCommand(source, payment);
            Assert.IsTrue(command.Execute(new InventoryContext(actor), tx).Success);
            if (rollback) tx.Rollback(); else tx.Commit();
            if (rollback)
            {
                CollectionAssert.AreEqual(before, inv.Objects); CollectionAssert.AreEqual(beforeParts, source.Parts);
                Assert.AreEqual(count, Quantity(source)); Assert.AreEqual(2, Quantity(payment));
                Assert.AreEqual(name, Name(source)); Assert.AreEqual(raw, source.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                Assert.AreEqual("SteelBladeComponent", source.GetPart<WeaponAssemblyPart>().BladeBlueprint);
                Assert.IsTrue(CraftingMarkPart.IsMarked(source));
            }
            else
            {
                Assert.AreEqual(1, Quantity(payment)); Assert.AreEqual(count, Weapons(actor).Sum(Quantity));
                Assert.AreEqual(1, Weapons(actor).Where(i => reforge ? i.GetPart<WeaponAssemblyPart>().BladeBlueprint == "IronSpikeComponent" : TemperCount(i) == 1).Sum(Quantity));
            }
        }
        [TestCase(false, 1)] [TestCase(false, 2)] [TestCase(true, 1)] [TestCase(true, 2)]
        public void MarkFollowsTheOneChangedWeapon(bool reforge, int count)
        {
            var actor = Crafter(); var source = Stock(actor, count); CraftingMarkPart.Toggle(source);
            var payment = reforge ? Units(actor, "IronSpikeComponent", 1) : Quench(actor);
            Assert.IsTrue(reforge ? WeaponForgingService.TryReforge(actor, Factory, source, payment, out _, out _) : WeaponTemperingService.TryTemper(actor, source, payment, out _));
            var marked = CraftingMarkPart.CollectMarked(actor).Weapons;
            Assert.AreEqual(1, marked.Count); Assert.AreEqual(1, Quantity(marked[0]));
            Assert.IsTrue(reforge ? marked[0].GetPart<WeaponAssemblyPart>().BladeBlueprint == "IronSpikeComponent" : TemperCount(marked[0]) == 1);
            Assert.AreEqual(count == 1, CraftingMarkPart.IsMarked(source));
        }
    }
}
