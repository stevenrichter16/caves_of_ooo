using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow and malformed/live-content probes through existing Examine events.</summary>
    public sealed class DensityItemExamineAdversarialTests
    {
        private EntityFactory factory;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            MessageLog.Clear();
        }
        private static Entity BareWeapon(string attributes = "", string effects = "")
        {
            var item = new Entity { ID = "probe", BlueprintName = "Probe" };
            item.Tags["Item"] = "";
            item.AddPart(new RenderPart { DisplayName = "probe blade" });
            item.AddPart(new ExaminablePart());
            item.AddPart(new MeleeWeaponPart { Attributes = attributes, OnHitEffectsRaw = effects });
            return item;
        }
        private static string Examine(Entity item)
        {
            MessageLog.Clear();
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Command", "Examine");
            item.FireEventAndRelease(e);
            return MessageLog.GetLast() ?? "";
        }
        private static int Count(string text, string value)
        {
            int n = 0, p = 0;
            while ((p = text.IndexOf(value, p, StringComparison.Ordinal)) >= 0) { n++; p += value.Length; }
            return n;
        }

        [TestCase(null)] [TestCase("")] [TestCase("  ")]
        [TestCase("Burning,0")] [TestCase("Burning,-1")] [TestCase("Burning,no")] [TestCase("Burning")]
        public void AbsentOrInvalidSpecsDoNotInventEffectAttempts(string raw)
        {
            string text = Examine(BareWeapon(effects: raw));
            StringAssert.Contains("Damage: 1d2", text);
            StringAssert.DoesNotContain("On damaging hits", text);
            StringAssert.DoesNotContain("Set ablaze", text);
        }

        [TestCase(101)] [TestCase(int.MaxValue)]
        public void OversizedChanceMatchesTheRealHundredOutcomeRoll(int chance)
        {
            string text = Examine(BareWeapon(effects: "Burning," + chance));
            StringAssert.Contains("100%: Set ablaze", text);
            StringAssert.DoesNotContain(chance + "%", text);
        }

        [Test]
        public void UnknownEffectIsNotAdvertisedAsAWorkingPower()
        {
            string text = Examine(BareWeapon(effects: "UnknownPower,35"));
            StringAssert.Contains("UnknownPower", text);
            StringAssert.Contains("has no effect", text);
            StringAssert.DoesNotContain("35%", text);
        }

        [TestCase("Cudgel", true)] [TestCase("NotCudgel", false)]
        [TestCase("Bludgeoning Cudgel Cudgel", true)]
        public void ClassPredicatesUseCombatAliasesOnceAndRequireAnExactAttribute(string attributes, bool stuns)
        {
            string text = Examine(BareWeapon(attributes));
            Assert.AreEqual(stuns ? 1 : 0, Count(text, "15%: Stunned"));
        }

        [Test]
        public void StatAttributeAlsoParticipatesInTheActualClassDispatcher()
        {
            // Combat always adds the selected stat name to Damage before class
            // hooks. The preview must follow that same construction contract.
            var item = BareWeapon(); item.GetPart<MeleeWeaponPart>().Stat = "Cutting";
            StringAssert.Contains("25%: Bleeding", Examine(item));
            item.GetPart<MeleeWeaponPart>().Stat = "Strength";
            StringAssert.DoesNotContain("25%: Bleeding", Examine(item));
        }

        [Test]
        public void NullLegacySlotUsesTheSameHandFallbackAsEquipment()
        {
            var item = BareWeapon(); item.AddPart(new EquippablePart { Slot = null });
            CollectionAssert.AreEqual(new[] { "Hand" }, item.GetPart<EquippablePart>().GetSlotArray());
            StringAssert.Contains("Equip slots: Hand", Examine(item));
        }

        [Test]
        public void ReexaminingChangedSpecsCannotRetainAStaleCombatCache()
        {
            var item = BareWeapon(effects: "Burning,30");
            var weapon = item.GetPart<MeleeWeaponPart>();
            var priorCache = weapon.OnHitEffectsCachedSpecs;
            StringAssert.Contains("Set ablaze", Examine(item));
            weapon.OnHitEffectsRaw = "Frozen,40";
            string text = Examine(item);
            StringAssert.Contains("40%: Frozen over", text);
            StringAssert.DoesNotContain("Set ablaze", text);
            Assert.AreEqual("Burning", priorCache.Single().EffectName, "inspection does not edit cached specs");
        }

        [Test]
        public void MultipleSpecsRemainIndependentWithoutAddingTheirChances()
        {
            string text = Examine(BareWeapon(effects: "Burning,30;Poisoned,40,1d6,8,0"));
            StringAssert.Contains("independent attempts", text);
            StringAssert.Contains("30%: Set ablaze", text);
            StringAssert.Contains("40%: Poisoned - 1d6 damage a turn for 8 turns", text);
            StringAssert.DoesNotContain("70%", text);
        }

        [Test]
        public void DurationIsNotInventedForPhysicalStateEffects()
        {
            string text = Examine(BareWeapon(effects: "Frozen,30,,777,3;Acidic,40,,888,1.5"));
            // Freeze now thaws by magnitude (Docs/FREEZE-THAW.md): the text states the
            // thaw time derived from Cold (1.0 / 0.10 per turn = ~10 turns), which is a
            // property of the freeze itself. The spec's duration argument (777) is still
            // never shown, which is what this test guards.
            StringAssert.Contains("100% iced; thaws in about 10 turns", text);
            StringAssert.Contains("5 damage a turn", text);
            StringAssert.DoesNotContain("777", text);
            StringAssert.DoesNotContain("888", text);
            StringAssert.DoesNotContain("7 damage a turn", text);
        }

        [TestCase(5, "-5")] [TestCase(-5, "+5")] [TestCase(int.MinValue, "+2147483648")]
        public void ArmorSpeedChangeUsesSignedArithmeticWithoutOverflow(int penalty, string expected)
        {
            var item = factory.CreateEntity("IronshodBoots"); item.GetPart<ArmorPart>().SpeedPenalty = penalty;
            StringAssert.Contains("Speed: " + expected, Examine(item));
        }

        [Test]
        public void ExplicitEquipmentBonusesFollowTheEquipmentParser()
        {
            var item = factory.CreateEntity("Dagger");
            item.GetPart<EquippablePart>().EquipBonuses = "Strength:2, Agility:-1, broken, Toughness:oops";
            string text = Examine(item);
            StringAssert.Contains("Strength +2, Agility -1", text);
            StringAssert.DoesNotContain("broken", text);
            StringAssert.DoesNotContain("Toughness", text);
        }

        [Test]
        public void RenamingOrRemovingItemTagDoesNotTurnNaturalWeaponsIntoEquipment()
        {
            var item = BareWeapon("Piercing");
            StringAssert.Contains("Damage:", Examine(item));
            item.Tags.Remove("Item");
            StringAssert.DoesNotContain("Damage:", Examine(item));
            item.Tags["Item"] = "";
            item.GetPart<RenderPart>().DisplayName = "plain old rock";
            StringAssert.Contains("Damage:", Examine(item), "parts, not guessed item names, drive details");
        }

        [Test]
        public void EmptyRuntimeTonicKeepsAnHonestGenericDescription()
        {
            var item = factory.CreateEntity("BrewedTonic");
            Assert.IsFalse(TonicExamineService.TryDescribe(item, out _));
            StringAssert.DoesNotContain("On whoever", Examine(item));
            item.AddPart(new BrewItemPart { Form = "Tonic", EffectsRaw = "Burning:1" });
            StringAssert.Contains("Set ablaze", Examine(item));
        }

        [Test]
        public void ExaminationDoesNotConsumeOrTickLiveEffectsOrMutateItemState()
        {
            var item = factory.CreateEntity("VenomDagger");
            var stack = item.GetPart<StackerPart>(); stack.StackCount = 3;
            var poison = new PoisonedEffect(9, "1d5", new NoRollRandom()); item.ApplyEffect(poison);
            var beforeParts = item.Parts.ToArray();
            var weapon = item.GetPart<MeleeWeaponPart>(); string raw = weapon.OnHitEffectsRaw;
            int hp = item.GetStatValue("Hitpoints"), tick = WorldClock.CurrentTick;
            string first = Examine(item), second = Examine(item);
            Assert.AreEqual(first, second);
            StringAssert.Contains("50%: Poisoned", first);
            Assert.AreEqual(3, stack.StackCount); Assert.AreEqual(9, poison.Duration);
            Assert.AreEqual(hp, item.GetStatValue("Hitpoints")); Assert.AreEqual(tick, WorldClock.CurrentTick);
            Assert.AreEqual(raw, weapon.OnHitEffectsRaw); CollectionAssert.AreEqual(beforeParts, item.Parts);
        }

        private sealed class NoRollRandom : Random
        {
            public override int Next() => throw new InvalidOperationException("Examine consumed game RNG");
            public override int Next(int max) => throw new InvalidOperationException("Examine consumed game RNG");
            public override int Next(int min, int max) => throw new InvalidOperationException("Examine consumed game RNG");
            public override double NextDouble() => throw new InvalidOperationException("Examine consumed game RNG");
        }
    }
}
