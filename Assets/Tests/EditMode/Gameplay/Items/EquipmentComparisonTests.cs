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
    public sealed class EquipmentComparisonTests
    {
        EntityFactory factory;
        Entity actor;
        InventoryPart inventory;
        Body body;

        [SetUp]
        public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            actor = factory.CreateEntity("Player");
            inventory = actor.GetPart<InventoryPart>();
            body = actor.GetPart<Body>();
        }

        Entity Carry(string name)
        {
            var item = factory.CreateEntity(name);
            Assert.That(inventory.AddObject(item), Is.True);
            return item;
        }

        static bool Describe(Entity who, Entity item, out string text, out string reason)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.EquipmentComparisonService");
            Assert.That(type, Is.Not.Null, "Missing factual equipment comparison service.");
            var method = type.GetMethod("TryDescribe", new[] { typeof(Entity), typeof(Entity), typeof(string).MakeByRefType(), typeof(string).MakeByRefType() });
            Assert.That(method, Is.Not.Null);
            object[] args = { who, item, null, null };
            bool result = (bool)method.Invoke(null, args);
            text = (string)args[2]; reason = (string)args[3];
            return result;
        }

        string Text(Entity item)
        {
            Assert.That(Describe(actor, item, out string text, out string reason), Is.True, reason);
            return text;
        }

        [Test]
        public void WeaponUsesCurrentFieldsAndPerUnitWeight()
        {
            var item = Carry("Dagger");
            var weapon = item.GetPart<MeleeWeaponPart>();
            weapon.BaseDamage = "2d3+7"; weapon.HitBonus = -2; weapon.PenBonus = 4;
            weapon.Stat = "Willpower"; weapon.MaxStrengthBonus = 0;
            item.GetPart<StackerPart>().StackCount = 3;
            string text = Text(item);
            StringAssert.Contains("Damage: 2d3+7 per penetration", text);
            StringAssert.Contains("Hit bonus: -2", text);
            StringAssert.Contains("Penetration bonus: +4", text);
            StringAssert.Contains("Willpower modifier (cap 0)", text);
            StringAssert.Contains("Weight per item: " + HandlingService.GetWeight(item), text);
            StringAssert.Contains("Equip slots: Hand", text);
            StringAssert.DoesNotContain("DPS", text);
        }

        [Test]
        public void EmptyWornSlotIsNotANaturalHand()
        {
            string worn = Text(Carry("LeatherCap"));
            StringAssert.Contains("empty", worn);
            StringAssert.DoesNotContain("natural attack:", worn);
            string hand = Text(Carry("Dagger"));
            StringAssert.Contains("natural attack:", hand);
            StringAssert.Contains("fist", hand.ToLowerInvariant());
            StringAssert.Contains("Damage:", hand);
        }

        [Test]
        public void AutoChoiceAndOccupiedHandAlternativeAreBothVisible()
        {
            var current = Carry("Dagger");
            Assert.That(InventorySystem.Equip(actor, current, body.GetPartsByType("Hand")[0]), Is.True);
            string text = Text(Carry("ShortSword"));
            StringAssert.Contains("Auto equip", text);
            StringAssert.Contains("Manual equip", text);
            StringAssert.Contains("Displaced:", text);
            StringAssert.Contains(current.GetDisplayName(), text);
        }

        [Test]
        public void TwoHandCandidateListsBothDifferentDisplacedItems()
        {
            var a = Carry("Dagger"); var b = Carry("Buckler");
            var hands = body.GetPartsByType("Hand");
            Assert.That(InventorySystem.Equip(actor, a, hands[0]), Is.True);
            Assert.That(InventorySystem.Equip(actor, b, hands[1]), Is.True);
            string text = Text(Carry("DissolutionMaul"));
            StringAssert.Contains(a.GetDisplayName(), text);
            StringAssert.Contains(b.GetDisplayName(), text);
            StringAssert.Contains(hands[0].GetDisplayName(), text);
            StringAssert.Contains(hands[1].GetDisplayName(), text);
            StringAssert.Contains("AV: +1", text);
        }

        [Test]
        public void OneHandCandidateShowsBothReleasedSlotsOfCurrentTwoHandWeapon()
        {
            var current = Carry("DissolutionMaul");
            Assert.That(InventorySystem.Equip(actor, current), Is.True);
            string text = Text(Carry("Dagger"));
            var hands = body.GetPartsByType("Hand");
            StringAssert.Contains("Displaced: " + current.GetDisplayName() + " (" + string.Join(", ", hands.Select(p => p.GetDisplayName())) + ")", text);
        }

        [Test]
        public void AlreadyEquippedShowsItsActualCurrentSlots()
        {
            var item = Carry("DissolutionMaul");
            Assert.That(InventorySystem.Equip(actor, item), Is.True);
            string text = Text(item);
            StringAssert.Contains("Currently equipped:", text);
            foreach (var part in body.GetPartsByType("Hand")) StringAssert.Contains(part.GetDisplayName(), text);
            StringAssert.DoesNotContain("Auto equip", text);
        }

        [Test]
        public void IncompatibleCandidateKeepsItsFactsAndPlannerReason()
        {
            var item = Carry("LeatherCap"); item.GetPart<EquippablePart>().UsesSlots = "ImpossibleSlot";
            string text = Text(item);
            StringAssert.Contains("Unavailable: No available ImpossibleSlot slot", text);
            StringAssert.Contains("Armor contribution", text);
        }

        [Test]
        public void BodylessLegacyActorGetsUnavailableReasonWithoutInventedSlots()
        {
            var item = Carry("Dagger"); actor.RemovePart(body);
            StringAssert.Contains("Slot comparison unavailable: Actor has no body", Text(item));
            Assert.That(InventorySystem.Equip(actor, item), Is.True, "Legacy equipment remains usable independently of the comparison.");
        }

        [TestCase("IronshodBoots", "Speed: -5")]
        [TestCase("Buckler", "AV: +1; DV: +1")]
        [TestCase("VenomDagger", "50%")]
        public void EquipmentContributionsReuseExistingItemDescriptions(string blueprint, string expected)
        {
            StringAssert.Contains(expected, Text(Carry(blueprint)));
        }

        [Test]
        public void ConditionalEnhancementAndUnknownOnHitAreNotSummedIntoDamage()
        {
            var item = Carry("Dagger");
            var enhanced = new EnhancementSerrated(); enhanced.ApplyTier(2); item.AddPart(enhanced);
            item.GetPart<MeleeWeaponPart>().OnHitEffectsRaw = "Unrecognized,100,1d4,3,2";
            item.GetPart<EquippablePart>().EquipBonuses = "Agility:2,NoSuchStat:9,bad";
            string text = Text(item);
            StringAssert.Contains("Damage: 1d4 per penetration", text);
            StringAssert.Contains("Conditional enhancement: " + enhanced.GetEffectDescription(), text);
            StringAssert.Contains("Unknown on-hit effect", text);
            StringAssert.Contains("if the stat exists", text);
            StringAssert.Contains("NoSuchStat +9", text);
        }

        [Test]
        public void RuntimeNonWeaponEquippableStillShowsSlotsAndConditionalBonuses()
        {
            var item = Carry("LeatherCap"); item.RemovePart(item.GetPart<ArmorPart>());
            item.GetPart<EquippablePart>().EquipBonuses = "Willpower:2";
            string text = Text(item);
            StringAssert.Contains("Equip slots: Head", text);
            StringAssert.Contains("Willpower +2", text);
            StringAssert.Contains("Other effects are not totaled", text);
        }

        [TestCase("foreign-physics")]
        [TestCase("foreign-equippable")]
        [TestCase("foreign-weapon")]
        [TestCase("cache-only")]
        [TestCase("removed")]
        [TestCase("zero-stack")]
        public void StaleOrForgedCandidateIsRejected(string mutation)
        {
            var item = Carry("Dagger"); var other = factory.CreateEntity("Player");
            if (mutation == "foreign-physics") item.GetPart<PhysicsPart>().InInventory = other;
            if (mutation == "foreign-equippable") item.GetPart<EquippablePart>().ParentEntity = other;
            if (mutation == "foreign-weapon") item.GetPart<MeleeWeaponPart>().ParentEntity = other;
            if (mutation == "cache-only") { inventory.RemoveObject(item); inventory.EquippedItems["fake"] = item; }
            if (mutation == "removed") inventory.RemoveObject(item);
            if (mutation == "zero-stack") item.GetPart<StackerPart>().StackCount = 0;
            Assert.That(Describe(actor, item, out string text, out string reason), Is.False);
            Assert.That(text, Is.Null); Assert.That(reason, Is.Not.Empty);
        }

        [TestCase("foreign-current")]
        [TestCase("missing-current-cache")]
        [TestCase("foreign-body")]
        [TestCase("dead-actor")]
        public void InvalidCurrentEquipmentOrActorCannotProduceTrustworthyComparison(string mutation)
        {
            var current = Carry("Dagger"); Assert.That(InventorySystem.Equip(actor, current), Is.True);
            var item = Carry("ShortSword");
            if (mutation == "foreign-current") current.GetPart<PhysicsPart>().Equipped = factory.CreateEntity("Player");
            if (mutation == "missing-current-cache") inventory.EquippedItems.Clear();
            if (mutation == "foreign-body") body.ParentEntity = factory.CreateEntity("Player");
            if (mutation == "dead-actor") actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.That(Describe(actor, item, out _, out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }

        [Test]
        public void ComparisonDoesNotDispatchEquipCallbacksOrChangeNativeState()
        {
            var current = Carry("Dagger"); Assert.That(InventorySystem.Equip(actor, current), Is.True);
            var item = Carry("ShortSword"); item.GetPart<StackerPart>().StackCount = 4;
            var observer = new ComparisonEventObserver(); actor.AddPart(observer); item.AddPart(new ComparisonEventObserver());
            string before = Snapshot(); int equipmentVersion = EquipmentChangeBus.GlobalVersion;
            string first = Text(item);
            Assert.That(Text(item), Is.EqualTo(first));
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(observer.Events, Is.Zero);
            Assert.That(item.GetPart<ComparisonEventObserver>().Events, Is.Zero);
            Assert.That(EquipmentChangeBus.GlobalVersion, Is.EqualTo(equipmentVersion));
        }

        string Snapshot()
        {
            return string.Join("|", inventory.Objects.Select(e => e.ID + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1)))
                + ";" + string.Join("|", inventory.EquippedItems.Select(p => p.Key + ":" + p.Value.ID))
                + ";" + string.Join("|", body.GetParts().Select(p => p.ID + ":" + p._Equipped?.ID + ":" + p._DefaultBehavior?.ID))
                + ";" + string.Join("|", actor.Statistics.Select(p => p.Key + ":" + p.Value.BaseValue + ":" + p.Value.Bonus + ":" + p.Value.Penalty))
                + ";" + actor.GetStatValue("Energy") + ";" + actor.GetStatValue("Currency");
        }

        public sealed class ComparisonEventObserver : Part
        {
            public int Events;
            public override bool HandleEvent(GameEvent e) { Events++; return true; }
        }
    }
}
