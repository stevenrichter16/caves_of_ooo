using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MirePithCleanupTests : BodyCoatingCleanupFixture
    {
        [TestCase(false, 1)] [TestCase(false, 20)] [TestCase(false, 21)] [TestCase(false, 272)]
        [TestCase(true, 1)] [TestCase(true, 20)] [TestCase(true, 21)] [TestCase(true, 272)]
        public void MireCleanupSpendsOnePithAndRestoresAgilityOnlyAtTheFinalUnit(bool party, int amount)
        {
            var target = party ? Friend() : Actor; int agility = target.GetStatValue("Agility");
            target.GetStat("Hitpoints").BaseValue -= 3;
            var coat = Coat(target, "bog-mire", amount); var pith = Carry("PrismreedPith");
            pith.AddPart(new StackerPart()); pith.GetPart<StackerPart>().StackCount = 3;
            int hp = target.GetStatValue("Hitpoints");
            Assert.AreEqual(agility - 2, target.GetStatValue("Agility"));
            Assert.True(Act(pith, Pick(pith, target)));
            Assert.AreEqual(2, pith.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(Math.Max(0, amount - 20), coat.Amount);
            Assert.AreEqual(amount > 20, target.HasEffect<LiquidCoveredEffect>());
            Assert.AreEqual(amount > 20 ? agility - 2 : agility, target.GetStatValue("Agility"));
            Assert.AreEqual(hp, target.GetStatValue("Hitpoints"), "cleaning never heals existing injury");
        }

        [Test] public void MireWipePreservesOtherAilmentsWaterAndRealGroundLiquid()
        {
            var wet = new WetEffect(1); var poison = new PoisonedEffect(); var buff = new BerserkEffect();
            Assert.True(Actor.ApplyEffect(wet)); Assert.True(Actor.ApplyEffect(poison)); Assert.True(Actor.ApplyEffect(buff));
            int strength = Actor.GetStatValue("Strength"), dodge = Actor.GetStatValue("DV");
            var pool = Pool(7, "bog-mire"); Zone.TileState.WriteCoating(11, 10, "bog-mire", 8);
            var flask = Flask(2); var coat = Coat(Actor, "bog-mire", 20); var pith = Carry("PrismreedPith");
            Assert.True(Act(pith, Pick(pith, Actor)));
            Assert.AreSame(wet, Actor.GetEffect<WetEffect>()); Assert.AreSame(poison, Actor.GetEffect<PoisonedEffect>());
            Assert.AreSame(buff, Actor.GetEffect<BerserkEffect>()); Assert.AreEqual(strength, Actor.GetStatValue("Strength")); Assert.AreEqual(dodge, Actor.GetStatValue("DV"));
            Assert.AreEqual(7, pool.GetPart<LiquidPoolPart>().Volume); Assert.AreEqual(2, Units(flask));
            Assert.AreEqual(8, Zone.TileState.CoatingTurns(11, 10, "bog-mire"));
        }

        [TestCase(false)] [TestCase(true)]
        public void ChangedOrReplacedMireSelectionNeverCleansOrSpends(bool replacement)
        {
            var coat = Coat(Actor, "bog-mire"); var pith = Carry("PrismreedPith"); string command = Pick(pith, Actor);
            if (replacement) { Actor.GetPart<StatusEffectsPart>().RemoveEffect(coat); coat = Coat(Actor, "bog-mire"); }
            else coat.Amount++;
            int amount = coat.Amount;
            Assert.False(Act(pith, command)); Assert.AreEqual(amount, coat.Amount); Assert.Contains(pith, Pack.Objects);
        }

        [Test] public void OuterRefusalKeepsMireSupplyAndPenalty()
        {
            var coat = Coat(Actor, "bog-mire"); var pith = Carry("PrismreedPith"); string command = Pick(pith, Actor);
            int agility = Actor.GetStatValue("Agility");
            Actor.AddPart(new Hook("BeforeInventoryAction", e => false));
            Assert.False(Act(pith, command)); Assert.AreEqual(35, coat.Amount);
            Assert.AreEqual(agility, Actor.GetStatValue("Agility")); Assert.Contains(pith, Pack.Objects);
        }

        [TestCase(35)] [TestCase(20)]
        public void BinaryReloadKeepsSpentPithAndExactRemainingModifier(int amount)
        {
            var coat = Coat(Actor, "bog-mire", amount); var pith = Carry("PrismreedPith");
            pith.AddPart(new StackerPart()); pith.GetPart<StackerPart>().StackCount = 3;
            Assert.True(Act(pith, Pick(pith, Actor)));
            using (var stream = new MemoryStream()) {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(Actor); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, Factory); var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.AreEqual(Actor.GetStatValue("Agility"), loaded.GetStatValue("Agility"));
                Assert.AreEqual(amount > 20, loaded.HasEffect<LiquidCoveredEffect>());
                if (amount > 20) { Assert.AreEqual(15, loaded.GetEffect<LiquidCoveredEffect>().Amount); Assert.AreEqual("Agility:-2", loaded.GetEffect<LiquidCoveredEffect>().AppliedModsRaw); }
                Assert.AreEqual(2, loaded.GetPart<InventoryPart>().Objects.Single(e => e.ID == pith.ID).GetPart<StackerPart>().StackCount);
            }
        }

        [Test] public void PithGuidanceExplainsFiniteBodyUseAndRetainsBrewingAndGroundLimits()
        {
            var pith = Carry("PrismreedPith");
            Assert.True(MaterialUseDescription.TryDescribe(Actor, pith, Factory, out string text));
            foreach (string fact in new[] { "bog mire", "20", "party", "discarded", "remaining coating", "brewing", "thin film", "pools" })
                StringAssert.Contains(fact, text);
            Assert.Contains(pith, Pack.Objects); Assert.False(Actor.HasEffect<LiquidCoveredEffect>());
        }

        [Test] public void ExistingTacticalReaderAlsoNamesMireWithoutChangingOtherLimits()
        {
            var pith = Carry("PrismreedPith"); string text = ItemTacticalUseDescription.Describe(pith);
            foreach (string fact in new[] { "bog mire", "20", "Wet", "poison", "burning", "Pools", "permanent", "ice" })
                StringAssert.Contains(fact, text);
        }
    }
}
