using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Gameplay.Items
{
    /// <summary>
    /// TonicExamineService — the "what exactly does this do to its victim"
    /// text behind the inventory Examine popup. The service CONSTRUCTS each
    /// effect through TonicEffectFactory (the real apply path) and reads the
    /// resulting fields, so the description can never drift from what a
    /// drink/shatter actually does — including the coating-fraction clamps
    /// the first live test run surfaced.
    /// </summary>
    public class TonicExamineServiceTests
    {
        private static Entity MakeItem(string name, params Part[] parts)
        {
            var item = new Entity { ID = name, BlueprintName = name };
            item.AddPart(new RenderPart { DisplayName = name });
            foreach (var p in parts)
                item.AddPart(p);
            return item;
        }

        [Test]
        public void Healing_And_StatBoost_LinesCarryExactValues()
        {
            var item = MakeItem("healing tonic",
                new TonicPart { Healing = "2d4", StatBoost = "Strength:4", Drink = true });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("2d4", text, "exact healing dice");
            StringAssert.Contains("Strength", text);
            StringAssert.Contains("+4", text, "exact boost amount");
            StringAssert.Contains("healing tonic", text, "names the item");
        }

        [Test]
        public void StatusEffect_LineCarriesConstructedDiceAndDuration()
        {
            var item = MakeItem("poison tonic",
                new TonicPart { Drink = true },
                new StatusTonicPart { EffectName = "Poison", EffectDuration = 7, EffectDamageDice = "1d3" });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("Poisoned", text);
            StringAssert.Contains("1d3", text, "the dice the constructed effect will roll");
            StringAssert.Contains("7", text, "the constructed duration");
        }

        [Test]
        public void AcidMagnitude_DescribesTheClampedReality_NotTheRawNumber()
        {
            // The whole point of constructing through the factory: a
            // magnitude-3 acid tonic actually applies Corrosion 1.0 (designed
            // clamp) → 5 damage a turn (1 + floor(1.0*4)). The popup must
            // state the real 5, and must NOT claim a "3".
            var item = MakeItem("acid flask",
                new TonicPart(),
                new StatusTonicPart { EffectName = "Acidic", EffectMagnitude = 3f });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("5 damage", text, "damage derived from the CLAMPED corrosion");
        }

        [Test]
        public void Brew_MultiEffect_ListsEveryEffect()
        {
            var item = MakeItem("galvanic draught",
                new TonicPart { Drink = true },
                new BrewItemPart { Form = "Tonic", EffectsRaw = "Acidic:2;Electrified:1" });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("acid", text.ToLowerInvariant());
            StringAssert.Contains("electrified", text.ToLowerInvariant());
        }

        [Test]
        public void CoatingForm_SaysQuench_NotDrink()
        {
            var item = MakeItem("flame coating",
                new BrewItemPart { Form = "Coating", EffectsRaw = "Burning:2" });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("quench", text.ToLowerInvariant(), "coatings temper blades");
            StringAssert.DoesNotContain("drunk", text.ToLowerInvariant(),
                "a coating is not presented as a drink");
        }

        [Test]
        public void Cure_ListsTheCuredEffect()
        {
            var item = MakeItem("antidote",
                new TonicPart { Drink = true },
                new CureTonicPart { CureEffect = "Poisoned" });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("Cures", text);
            StringAssert.Contains("Poisoned", text);
        }

        [Test]
        public void ThrowablePayload_MentionsShatter()
        {
            var item = MakeItem("poison tonic",
                new TonicPart { Drink = true },
                new StatusTonicPart { EffectName = "Poison" });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("shatter", text.ToLowerInvariant(),
                "throwable payloads advertise the shatter delivery");
        }

        [Test]
        public void UnknownEffectName_IsHonestlyLabeled()
        {
            var item = MakeItem("dud vial",
                new TonicPart(),
                new StatusTonicPart { EffectName = "UltraMegaQuantumBurst" });

            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string text));
            StringAssert.Contains("UltraMegaQuantumBurst", text);
            StringAssert.Contains("nothing", text.ToLowerInvariant(),
                "an undispatchable name must not pretend to do something");
        }

        [Test]
        public void PlainItem_NotDescribable()
        {
            // Counter-check: the Examine row only appears for tonic-ish items.
            var rock = MakeItem("plain rock");
            Assert.IsFalse(TonicExamineService.TryDescribe(rock, out _));
            Assert.IsFalse(TonicExamineService.TryDescribe(null, out _));
        }
    }
}
