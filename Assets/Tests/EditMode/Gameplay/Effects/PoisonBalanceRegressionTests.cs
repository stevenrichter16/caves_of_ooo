using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// A viper dose lasts four owner actions and deals at most eight poison damage.
    /// Reapplication refreshes ordinary poison instead of banking an additive tail.
    /// These are balance assertions, not a guarantee that an injured player survives.
    /// </summary>
    public class PoisonBalanceRegressionTests
    {
        EntityEquipmentContentFixture _content;

        [SetUp]
        public void SetUp()
        {
            _content = new EntityEquipmentContentFixture();
            new TurnManager(); // No unrelated owner may count as mid-action.
            TurnManager.World = null;
        }

        [TearDown]
        public void TearDown() => _content?.Dispose();

        // Drives one ordinary landed primary bite, declines the 15% offhand,
        // and then rolls the maximum poison die. No exploding penetration roll.
        sealed class BiteRng : Random
        {
            public int ChanceRoll = 74;
            public int DiceRolls;
            bool _firstPenetration = true;
            public override int Next(int maxValue) => maxValue == 100 ? ChanceRoll : 0;
            public override int Next(int minValue, int maxValue)
            {
                if (minValue == 1 && maxValue == 21) return 19;
                if (minValue == 1 && maxValue == 11)
                {
                    bool first = _firstPenetration; _firstPenetration = false;
                    return first ? 9 : 1;
                }
                DiceRolls++;
                return maxValue - 1;
            }
        }

        static Entity Actor(string name, bool player = false)
        {
            var entity = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = name };
            entity.SetTag("Creature");
            if (player) entity.SetTag("Player");
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", Owner = entity, BaseValue = 40, Max = 40 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", Owner = entity, BaseValue = 100 };
            entity.AddPart(new RenderPart { DisplayName = name });
            return entity;
        }

        static MeleeWeaponPart Fangs(Entity snake)
            => snake.GetPart<Body>().GetPartsByType("Hand")[0]._DefaultBehavior.GetPart<MeleeWeaponPart>();

        static int PoisonCount(Entity actor)
            => actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Count(e => e.GetType() == typeof(PoisonedEffect)) ?? 0;

        static void OwnerCycle(Entity actor)
        {
            actor.FireEventAndRelease(GameEvent.New("BeginTakeAction"));
            actor.FireEventAndRelease(GameEvent.New("EndTurn"));
        }

        [TestCase("Viper")]
        [TestCase("SpreadLatchcoil")]
        public void ActualSnakeBiteDealsAtMostEightPoisonDamageAcrossExactlyFourOwnerActions(string blueprint)
        {
            var snake = _content.Create(blueprint);
            var player = _content.Create("Player");
            Assert.AreEqual(40, player.GetStatValue("Hitpoints"));
            var zone = new Zone("poison-bite-regression");
            Assert.IsTrue(zone.AddEntity(snake, 5, 5));
            Assert.IsTrue(zone.AddEntity(player, 6, 5));
            var rng = new BiteRng();
            Assert.IsTrue(CombatSystem.PerformMeleeAttack(snake, player, zone, rng));
            int hpAfterBite = player.GetStatValue("Hitpoints");
            Assert.Less(hpAfterBite, 40, "This must be a damaging real bite, not merely a spec lookup.");
            var poison = player.GetEffect<PoisonedEffect>();
            Assert.NotNull(poison, "The actual stock fangs must dispatch their 75% venom proc.");
            Assert.AreEqual("1d2", poison.DamageDice);
            Assert.AreEqual(4, poison.Duration);
            Assert.AreSame(rng, poison.Rng);
            Assert.IsFalse(poison.JustApplied, "A bite between owner actions does not skip a duration tick.");
            Assert.AreEqual(1, PoisonCount(player));

            var turns = TurnManager.Active;
            turns.AddEntity(player);
            turns.AdvanceClock(500);
            Assert.AreEqual(hpAfterBite, player.GetStatValue("Hitpoints"), "Clock-only travel is not an owner action.");
            Assert.AreEqual(4, poison.Duration);
            for (int action = 1; action <= 4; action++)
            {
                Assert.AreSame(player, turns.ProcessUntilPlayerTurn());
                Assert.AreEqual(hpAfterBite - 2 * action, player.GetStatValue("Hitpoints"), "Exactly one maximum poison die per owner start.");
                turns.EndTurn(player, zone);
                Assert.AreEqual(4 - action, poison.Duration);
                Assert.AreEqual(action < 4, player.HasEffect<PoisonedEffect>());
            }
            Assert.AreEqual(8, hpAfterBite - player.GetStatValue("Hitpoints"));
            Assert.AreSame(player, turns.ProcessUntilPlayerTurn());
            Assert.AreEqual(hpAfterBite - 8, player.GetStatValue("Hitpoints"), "The fifth action must have no poison tail.");
        }

        [TestCase("Viper", 1, 74, true)]
        [TestCase("Viper", 1, 75, false)]
        [TestCase("Viper", 0, 74, false)]
        [TestCase("Viper", -1, 74, false)]
        [TestCase("SpreadLatchcoil", 1, 74, true)]
        [TestCase("SpreadLatchcoil", 1, 75, false)]
        [TestCase("SpreadLatchcoil", 0, 74, false)]
        [TestCase("SpreadLatchcoil", -1, 74, false)]
        public void StockFangsRequireActualDamageAndRollBelowSeventyFive(string blueprint, int damage, int roll, bool applies)
        {
            var snake = _content.Create(blueprint);
            var target = Actor("bitten target");
            OnHitWeaponEffects.Apply(Fangs(snake), new Damage(Math.Max(0, damage)), damage,
                target, snake, null, new BiteRng { ChanceRoll = roll });
            Assert.AreEqual(applies, target.HasEffect<PoisonedEffect>());
            Assert.AreEqual(applies ? 1 : 0, PoisonCount(target));
            Assert.AreEqual(40, target.GetStatValue("Hitpoints"), "Applying a dose must not also tick it immediately.");
        }

        [Test]
        public void NonVenomousNaturalWeaponDoesNotAcquirePoison()
        {
            var bear = _content.Create("CaveBear");
            var target = Actor("clawed target");
            OnHitWeaponEffects.Apply(Fangs(bear), new Damage(1), 1, target, bear, null, new BiteRng { ChanceRoll = 0 });
            Assert.IsFalse(target.HasEffect<PoisonedEffect>());
        }

        [TestCase("Poisoned")]
        [TestCase("poison")]
        public void OnHitFactoryUsesInjectedRngForPoisonDamage(string alias)
        {
            var rng = new BiteRng();
            var spec = OnHitEffectSpec.Parse(alias + ",75,1d2,4,0").Single();
            var poison = (PoisonedEffect)OnHitEffectFactory.Create(spec, Actor("source"), rng);
            Assert.AreSame(rng, poison.Rng, "Combat's deterministic RNG must reach the damage ticks.");
            var target = Actor("target");
            Assert.IsTrue(target.ApplyEffect(poison));
            OwnerCycle(target);
            Assert.AreEqual(38, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, rng.DiceRolls);
        }

        [TestCase(4, 4, 4)]
        [TestCase(1, 4, 4)]
        [TestCase(8, 4, 8)]
        [TestCase(4, 8, 8)]
        [TestCase(int.MaxValue, 4, int.MaxValue)]
        [TestCase(-1, 4, -1)]
        [TestCase(4, -1, -1)]
        [TestCase(-1, -1, -1)]
        public void ReapplicationKeepsLongerDurationAndPreservesEitherIndefiniteDose(int existing, int incoming, int expected)
        {
            var target = Actor("dose target");
            var poison = new PoisonedEffect(existing, "1d2", new BiteRng());
            Assert.IsTrue(target.ApplyEffect(poison));
            Assert.IsTrue(target.ApplyEffect(new PoisonedEffect(incoming, "1d2", new BiteRng())));
            Assert.AreSame(poison, target.GetEffect<PoisonedEffect>());
            Assert.AreEqual(1, PoisonCount(target));
            Assert.AreEqual(expected, poison.Duration);
            OwnerCycle(target);
            Assert.AreEqual(expected == -1 ? -1 : expected - 1, poison.Duration);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MixedStrengthDosesRetainFirstInstancePotencyAndRngInBothOrders(bool strongFirst)
        {
            var target = Actor("mixed-dose target");
            var firstRng = new BiteRng();
            var first = new PoisonedEffect(strongFirst ? 8 : 4, strongFirst ? "1d6" : "1d2", firstRng);
            var later = new PoisonedEffect(strongFirst ? 4 : 8, strongFirst ? "1d2" : "1d6", new BiteRng());
            Assert.IsTrue(target.ApplyEffect(first));
            Assert.IsTrue(target.ApplyEffect(later));
            Assert.AreEqual(8, first.Duration);
            Assert.AreSame(first, target.GetEffect<PoisonedEffect>());
            Assert.AreSame(firstRng, first.Rng);
            Assert.AreEqual(strongFirst ? "1d6" : "1d2", first.DamageDice);
            OwnerCycle(target);
            Assert.AreEqual(strongFirst ? 34 : 38, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, firstRng.DiceRolls);
        }

        [Test]
        public void RepeatedDosesCannotBankMoreThanFourActionsAfterLastExposure()
        {
            var target = Actor("repeated-dose target");
            var poison = new PoisonedEffect(4, "1d2", new BiteRng());
            Assert.IsTrue(target.ApplyEffect(poison));
            for (int exposure = 0; exposure < 12; exposure++)
                Assert.IsTrue(target.ApplyEffect(new PoisonedEffect(4, "1d2", new BiteRng())));
            Assert.AreEqual(4, poison.Duration);
            Assert.AreEqual(1, PoisonCount(target));
            Assert.AreEqual(40, target.GetStatValue("Hitpoints"));
            OwnerCycle(target);
            Assert.AreEqual(3, poison.Duration);
            Assert.IsTrue(target.ApplyEffect(new PoisonedEffect(4, "1d2", new BiteRng())));
            Assert.AreEqual(4, poison.Duration, "A genuinely later bite refreshes the remaining duration.");
            for (int i = 0; i < 4; i++) OwnerCycle(target);
            Assert.IsFalse(target.HasEffect<PoisonedEffect>());
            Assert.AreEqual(30, target.GetStatValue("Hitpoints"));
            OwnerCycle(target);
            Assert.AreEqual(30, target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void NativeNpcBeginAndTakeTurnDispatchTickPoisonOnlyOnce()
        {
            var npc = Actor("poisoned NPC");
            var player = Actor("waiting player", true);
            var rng = new BiteRng();
            var poison = new PoisonedEffect(4, "1d2", rng);
            Assert.IsTrue(npc.ApplyEffect(poison));
            var turns = TurnManager.Active;
            turns.RestoreSavedState(0, false, null, new List<TurnManager.SavedTurnEntry>
            {
                new TurnManager.SavedTurnEntry { Entity = npc, Energy = 1000 },
                new TurnManager.SavedTurnEntry { Entity = player, Energy = 1000 }
            });
            Assert.AreSame(player, turns.ProcessUntilPlayerTurn());
            Assert.AreEqual(38, npc.GetStatValue("Hitpoints"));
            Assert.AreEqual(3, poison.Duration);
            Assert.AreEqual(1, rng.DiceRolls, "NPC TakeTurn follows BeginTakeAction without a second damage tick.");
            Assert.AreEqual(40, player.GetStatValue("Hitpoints"));
        }

        [Test]
        public void LegacyTakeTurnFallbackStillTicksOneDose()
        {
            var target = Actor("legacy turn target");
            var rng = new BiteRng();
            Assert.IsTrue(target.ApplyEffect(new PoisonedEffect(4, "1d2", rng)));
            target.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual(38, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, rng.DiceRolls);
        }

        [TestCase("Antidote", "ApplyTonic", false, true)]
        [TestCase("SoddenFieldDressing", "Apply", true, false)]
        public void ActualTreatmentsConsumeOneAndRetainTheirDistinctCureScopes(string blueprint, string command, bool curesBleeding, bool curesGasPoison)
        {
            var player = _content.Create("Player");
            var zone = new Zone("poison-treatment-regression");
            Assert.IsTrue(zone.AddEntity(player, 5, 5));
            var ordinary = new PoisonedEffect(4, "1d2", new BiteRng());
            var gas = new PoisonedByGasEffect { Duration = 9, DamagePerTurn = 3 };
            var bleed = new BleedingEffect(17);
            Assert.IsTrue(player.ApplyEffect(ordinary));
            Assert.IsTrue(player.ApplyEffect(gas));
            Assert.IsTrue(player.ApplyEffect(bleed));
            var item = _content.Create(blueprint);
            item.GetPart<StackerPart>().StackCount = 2;
            Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(item));
            var result = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(item, command), player, zone);
            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.Contains(item, player.GetPart<InventoryPart>().Objects);
            Assert.IsFalse(player.HasEffect<PoisonedEffect>());
            if (curesGasPoison) Assert.IsNull(player.GetEffect<PoisonedByGasEffect>());
            else
            {
                Assert.AreSame(gas, player.GetEffect<PoisonedByGasEffect>());
                Assert.AreEqual(9, gas.Duration);
                Assert.AreEqual(3, gas.DamagePerTurn);
            }
            Assert.AreEqual(!curesBleeding, player.HasEffect<BleedingEffect>());
            Assert.AreEqual(40, player.GetStatValue("Hitpoints"));
        }

        [Test]
        public void AntidoteTreatsGasPoisonAloneWithOneFiniteDoseWithoutPreventingNewExposure()
        {
            var player = _content.Create("Player");
            var zone = new Zone("gas-antidote-control");
            Assert.IsTrue(zone.AddEntity(player, 5, 5));
            Assert.IsTrue(player.ApplyEffect(new PoisonedByGasEffect { Duration = 9, DamagePerTurn = 3 }));
            var item = _content.Create("Antidote");
            item.GetPart<StackerPart>().StackCount = 2;
            Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(item));
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(item, "ApplyTonic"), player, zone).Success);
            Assert.IsFalse(player.HasEffect<PoisonedByGasEffect>());
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.Contains(item, player.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(40, player.GetStatValue("Hitpoints"));
            var exposure = new PoisonedByGasEffect { Duration = 9, DamagePerTurn = 3 };
            Assert.IsTrue(player.ApplyEffect(exposure));
            Assert.AreSame(exposure, player.GetEffect<PoisonedByGasEffect>());
        }

        [Test]
        public void DressingCannotTreatGasPoisonAloneOrConsumeAUnitForIt()
        {
            var player = _content.Create("Player");
            var zone = new Zone("gas-treatment-control");
            Assert.IsTrue(zone.AddEntity(player, 5, 5));
            var gas = new PoisonedByGasEffect { Duration = 9 };
            Assert.IsTrue(player.ApplyEffect(gas));
            var item = _content.Create("SoddenFieldDressing");
            Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(item));
            Assert.IsFalse(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(item, "Apply"), player, zone).Success);
            Assert.AreSame(gas, player.GetEffect<PoisonedByGasEffect>());
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.Contains(item, player.GetPart<InventoryPart>().Objects);
        }
    }
}
