using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class EquipmentComparisonAdversarialTests
    {
        EntityFactory factory;
        Entity actor;
        InventoryPart inventory;
        Body body;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            actor = factory.CreateEntity("Player"); inventory = actor.GetPart<InventoryPart>(); body = actor.GetPart<Body>();
        }
        Entity Carry(string id)
        {
            var item = factory.CreateEntity(id); Assert.That(inventory.AddObject(item), Is.True); return item;
        }
        bool Describe(Entity item, out string text)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.EquipmentComparisonService");
            Assert.That(type, Is.Not.Null);
            object[] args = { actor, item, null, null };
            bool result = (bool)type.GetMethod("TryDescribe").Invoke(null, args);
            text = (string)args[2]; return result;
        }
        string Text(Entity item) { Assert.That(Describe(item, out string text), Is.True); return text; }

        [Test]
        public void ShieldHandHasNaturalAttackButSupportingMultiHandSlotDoesNot()
        {
            var shield = Carry("Buckler");
            var hands = body.GetPartsByType("Hand");
            Assert.That(InventorySystem.Equip(actor, shield, hands[0]), Is.True);
            var candidate = Carry("Dagger");
            StringAssert.Contains(hands[0].GetDisplayName() + " now: " + shield.GetDisplayName() + "; natural attack:", Text(candidate));
            Assert.That(InventorySystem.UnequipItem(actor, shield), Is.True);
            var two = Carry("DissolutionMaul"); Assert.That(InventorySystem.Equip(actor, two), Is.True);
            StringAssert.DoesNotContain("natural attack:", Text(candidate));
        }

        [Test]
        public void SharedNaturalWeaponFirstFlagMatchesActualCurrentAttackAdmission()
        {
            var hands = body.GetPartsByType("Hand");
            hands[1]._DefaultBehavior = hands[0]._DefaultBehavior;
            hands[1].FirstSlotForDefaultBehavior = false;
            string text = Text(Carry("Dagger"));
            StringAssert.Contains(hands[0].GetDisplayName() + " now: empty; natural attack:", text);
            StringAssert.Contains(hands[1].GetDisplayName() + " now: empty.", text);
        }

        [TestCase(false)] [TestCase(true)]
        public void ForeignNaturalWeaponOwnerIsRejectedWithoutBorrowingItsFacts(bool equipped)
        {
            var hand = body.GetPartsByType("Hand")[0];
            var natural = factory.CreateEntity("Dagger");
            hand._DefaultBehavior = natural; hand.FirstSlotForDefaultBehavior = true;
            var foreign = factory.CreateEntity("Player");
            if (equipped) natural.GetPart<PhysicsPart>().Equipped = foreign;
            else natural.GetPart<PhysicsPart>().InInventory = foreign;
            Assert.That(Describe(Carry("ShortSword"), out _), Is.False);
        }

        [TestCase("extra-cache")]
        [TestCase("foreign-inventory")]
        [TestCase("foreign-body-part")]
        [TestCase("double-carried-equipped")]
        public void MismatchedGraphRefusesInsteadOfPresentingAnEquipGuarantee(string mutation)
        {
            var item = Carry("Dagger"); Assert.That(InventorySystem.Equip(actor, item), Is.True);
            if (mutation == "extra-cache") inventory.EquippedItems["borrowed-slot"] = item;
            if (mutation == "foreign-inventory") inventory.ParentEntity = factory.CreateEntity("Player");
            if (mutation == "foreign-body-part") body.GetPartsByType("Hand")[0].ParentBody = factory.CreateEntity("Player").GetPart<Body>();
            if (mutation == "double-carried-equipped") inventory.Objects.Add(item);
            Assert.That(Describe(item, out _), Is.False);
        }

        [Test]
        public void ReinspectionUsesNewSlotsAndRefusesTheRemovedExactInstance()
        {
            var item = Carry("Dagger"); string first = Text(item);
            var current = Carry("DissolutionMaul"); Assert.That(InventorySystem.Equip(actor, current), Is.True);
            string second = Text(item); Assert.That(second, Is.Not.EqualTo(first));
            StringAssert.Contains(current.GetDisplayName(), second);
            Assert.That(inventory.RemoveObject(item), Is.True);
            Carry("Dagger"); // same blueprint, different owner cannot satisfy the captured selection
            Assert.That(Describe(item, out _), Is.False);
        }

        [Test]
        public void RuntimeEquippableWithoutItemTagRetainsActualFacts()
        {
            var item = Carry("LeatherCap"); item.Tags.Remove("Item");
            StringAssert.Contains("Armor contribution", Text(item));
        }

        [Test]
        public void ExtremeSpeedAndMalformedBonusFieldsStayFactualAndDoNotChangeStats()
        {
            var item = Carry("LeatherCap"); item.GetPart<ArmorPart>().SpeedPenalty = int.MinValue;
            item.GetPart<EquippablePart>().EquipBonuses = "bad,Agility:no,Strength:2147483648,Willpower:2";
            int bonus = actor.GetStat("Willpower").Bonus;
            string text = Text(item);
            StringAssert.Contains("Speed: +2147483648", text);
            StringAssert.Contains("Willpower +2", text);
            StringAssert.DoesNotContain("Strength +2147483648", text);
            Assert.That(actor.GetStat("Willpower").Bonus, Is.EqualTo(bonus));
        }

        [TestCase(false)] [TestCase(true)]
        public void DescriptionCallbackCannotPublishAStaleOwnerSnapshot(bool currentItem)
        {
            var candidate = Carry("Dagger");
            var observed = currentItem ? Carry("DissolutionMaul") : candidate;
            if (currentItem) Assert.That(InventorySystem.Equip(actor, observed), Is.True);
            observed.AddPart(new MutatingDescription { Change = () =>
            {
                if (currentItem) InventorySystem.UnequipItem(actor, observed);
                inventory.RemoveObject(observed);
            }});
            Assert.That(Describe(candidate, out string text), Is.False);
            Assert.That(text, Is.Null);
        }
        public sealed class MutatingDescription : IItemEnhancement
        {
            public Action Change;
            public override string GetEffectDescription() { Change?.Invoke(); return "stale description"; }
        }

        [Test]
        public void DistinctSameNamedEquippedItemsAreBothRepresented()
        {
            var a = Carry("Dagger"); var b = Carry("ShortSword");
            b.GetPart<RenderPart>().DisplayName = a.GetPart<RenderPart>().DisplayName;
            var hands = body.GetPartsByType("Hand");
            Assert.That(InventorySystem.Equip(actor, a, hands[0]), Is.True);
            Assert.That(InventorySystem.Equip(actor, b, hands[1]), Is.True);
            string text = Text(Carry("DissolutionMaul"));
            StringAssert.Contains("Damage: 1d4 per penetration", text);
            StringAssert.Contains("Damage: 1d6 per penetration", text);
            StringAssert.Contains("Displaced: " + a.GetDisplayName() + " (" + hands[0].GetDisplayName() + ")", text);
            StringAssert.Contains("Displaced: " + b.GetDisplayName() + " (" + hands[1].GetDisplayName() + ")", text);
        }
    }
}
