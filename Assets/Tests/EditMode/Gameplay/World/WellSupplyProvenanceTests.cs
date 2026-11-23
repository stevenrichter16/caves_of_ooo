using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WellSupplyProvenanceTests : FiftyWorldFixture
    {
        static IEnumerable<TestCaseData> BaseSpills()
        {
            foreach (string consumer in new[] { "drink", "skin", "flask" })
                foreach (string supply in new[] { "finite", "endless", "pool" })
                    foreach (string spill in new[] { "acid-pool", "oil-coating" })
                        yield return new TestCaseData(consumer, supply, spill);
        }

        Entity Source(string supply)
        {
            var source = Place(supply == "pool" ? "GroveSeep" : "Cistern");
            source.GetPart<WellPart>().Stock = supply == "endless" ? -1 : 3;
            if (supply == "pool") source.GetPart<LiquidPoolPart>().Volume = 3;
            return source;
        }
        Entity Recipient(string consumer) => consumer == "drink" ? null : consumer == "skin" ? Skin() : Flask(0);
        string Command(Entity source, Entity recipient, string consumer) => consumer == "drink" ? "DrawWaterAtWell"
            : consumer == "skin" ? WaterVesselService.FillSourcePrefix + Id(source) : Choice(recipient, "FillWellFlask|");
        bool Draw(Entity source, Entity recipient, string command) => Act(recipient ?? source, command);
        void Thirst() => Assert.True(Actor.ApplyEffect(new ParchedEffect { Stacks = 3 }));
        int Water(Entity source) => source.GetPart<LiquidPoolPart>()?.Volume ?? source.GetPart<WellPart>().Stock;
        int Drawn(string consumer, string supply) => consumer == "drink" ? 1 : supply == "endless" && consumer == "flask" ? 12 : 3;
        void AssertDebt(string consumer, bool success)
        {
            int debt = consumer == "drink" && success ? 0 : 3;
            Assert.AreEqual(debt, Actor.GetEffect<ParchedEffect>()?.Stacks ?? 0);
            Assert.AreEqual(debt, Actor.GetStat("Strength").Penalty);
            Assert.AreEqual(debt, Actor.GetStat("Agility").Penalty);
        }
        void AssertMenu(Entity source, Entity recipient, string consumer, bool available)
        {
            if (consumer == "drink") Assert.AreEqual(available, Actions(source).Any(a => a.Command == "DrawWaterAtWell"));
            if (consumer == "flask") Assert.AreEqual(available, Actions(recipient).Any(a => a.Command.StartsWith("FillWellFlask|", StringComparison.Ordinal)));
        }

        [TestCaseSource(nameof(BaseSpills))]
        public void StockSupplyIsSeparateButExposedPoolRetainsMixedCellSafety(string consumer, string supply, string spill)
        {
            var source = Source(supply); var recipient = Recipient(consumer); Thirst();
            string command = Command(source, recipient, consumer);
            Entity hazard = null;
            if (spill == "acid-pool") hazard = Pool(5, "acid");
            else Zone.TileState.WriteCoating(11, 10, "oil", 5);
            bool success = supply != "pool";
            AssertMenu(source, recipient, consumer, success);
            Assert.AreEqual(success, Draw(source, recipient, command));
            int spent = success ? Drawn(consumer, supply) : 0;
            Assert.AreEqual(supply == "endless" ? -1 : 3 - spent, Water(source));
            if (supply == "pool") Assert.AreEqual(3, source.GetPart<WellPart>().Stock, "pool never falls back to stock");
            if (recipient != null) Assert.AreEqual(spent, Units(recipient));
            AssertDebt(consumer, success);
            if (hazard != null)
            {
                Assert.AreEqual(5, hazard.GetPart<LiquidPoolPart>().Volume);
                Assert.AreEqual("acid", hazard.GetPart<LiquidPoolPart>().LiquidId);
                Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(hazard));
            }
            else Assert.AreEqual(5, Zone.TileState.CoatingTurns(11, 10, "oil"));
        }

        [TestCase("drink", false)] [TestCase("skin", false)] [TestCase("flask", false)]
        [TestCase("drink", true)] [TestCase("skin", true)] [TestCase("flask", true)]
        public void ActorCellHazardDoesNotBecomeWaterFromADifferentSource(string consumer, bool pool)
        {
            string supply = pool ? "pool" : "finite";
            var source = Source(supply); var recipient = Recipient(consumer); Thirst();
            var hazard = Pool(5, "acid", 10, 10); int hp = Actor.GetStatValue("Hitpoints");
            Assert.True(Actor.ApplyEffect(new LiquidCoveredEffect("acid", 30)));
            var exposure = Actor.GetEffect<LiquidCoveredEffect>();
            Assert.NotNull(exposure);
            Assert.True(Draw(source, recipient, Command(source, recipient, consumer)));
            int spent = Drawn(consumer, supply);
            Assert.AreEqual(3 - spent, Water(source));
            if (recipient != null) Assert.AreEqual(spent, Units(recipient));
            Assert.AreEqual(5, hazard.GetPart<LiquidPoolPart>().Volume);
            Assert.AreSame(Zone.GetCell(10, 10), Zone.GetEntityCell(hazard));
            Assert.AreEqual("acid", hazard.GetPart<LiquidPoolPart>().LiquidId);
            Assert.AreEqual(hp, Actor.GetStatValue("Hitpoints"));
            Assert.AreSame(exposure, Actor.GetEffect<LiquidCoveredEffect>(), "draw does not cure or replace exposure");
            Assert.AreEqual("acid", exposure.LiquidId); Assert.AreEqual(30, exposure.Amount);
            AssertDebt(consumer, true);
        }

        [TestCase("drink")] [TestCase("skin")] [TestCase("flask")]
        public void MeteredLastStockUnitBesideSpillRetainsItsExistingTariff(string consumer)
        {
            var source = Source("finite"); var recipient = Recipient(consumer); Thirst();
            var well = source.GetPart<WellPart>(); well.Stock = 1; well.MeteredHydration = true;
            var hazard = Pool(5, "acid");
            string command = consumer == "drink" ? Choice(source, "DrawMeteredWaterAtWell|") : Command(source, recipient, consumer);
            Assert.True(Draw(source, recipient, command));
            Assert.AreEqual(0, well.Stock);
            if (recipient != null) Assert.AreEqual(1, Units(recipient));
            int debt = consumer == "drink" ? 2 : 3;
            Assert.AreEqual(debt, Actor.GetEffect<ParchedEffect>().Stacks);
            Assert.AreEqual(debt, Actor.GetStat("Strength").Penalty);
            Assert.AreEqual(debt, Actor.GetStat("Agility").Penalty);
            Assert.AreEqual(5, hazard.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual("acid", hazard.GetPart<LiquidPoolPart>().LiquidId);
        }

        [TestCase("drink", false)] [TestCase("skin", false)] [TestCase("flask", false)]
        [TestCase("drink", true)] [TestCase("skin", true)] [TestCase("flask", true)]
        public void LateSpillPreservesItsOwnStateAndRevalidatesTheActualSupply(string consumer, bool pool)
        {
            string supply = pool ? "pool" : "finite";
            var source = Source(supply); var recipient = Recipient(consumer); Thirst();
            string command = Command(source, recipient, consumer); Entity hazard = null;
            int hp = Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new FiftyWorldEventProbe { Callback = e =>
            {
                if (e.ID != "AfterInventoryAction" || hazard != null) return;
                hazard = Pool(5, "acid"); Actor.GetStat("Hitpoints").BaseValue--;
            } });
            Assert.AreEqual(!pool, Draw(source, recipient, command));
            Assert.NotNull(hazard, "callback actually introduced a new source-cell hazard");
            Assert.AreEqual(5, hazard.GetPart<LiquidPoolPart>().Volume);
            Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(hazard));
            Assert.AreEqual("acid", hazard.GetPart<LiquidPoolPart>().LiquidId);
            Assert.AreEqual(hp - 1, Actor.GetStatValue("Hitpoints"), "independent injury survives both outcomes");
            int spent = pool ? 0 : Drawn(consumer, supply);
            Assert.AreEqual(3 - spent, Water(source));
            if (recipient != null) Assert.AreEqual(spent, Units(recipient));
            AssertDebt(consumer, !pool);
        }

        [TestCase("drink")] [TestCase("skin")] [TestCase("flask")]
        public void EmptyExposedPoolCannotDrawAbstractStockBesideASpill(string consumer)
        {
            var source = Source("pool"); var recipient = Recipient(consumer); Thirst();
            string command = Command(source, recipient, consumer);
            source.GetPart<LiquidPoolPart>().Volume = 0; source.GetPart<WellPart>().Stock = 7;
            var hazard = Pool(5, "acid");
            AssertMenu(source, recipient, consumer, false);
            Assert.False(Draw(source, recipient, command));
            Assert.AreEqual(0, Water(source)); Assert.AreEqual(7, source.GetPart<WellPart>().Stock);
            Assert.AreEqual(5, hazard.GetPart<LiquidPoolPart>().Volume);
            if (recipient != null) Assert.AreEqual(0, Units(recipient));
            AssertDebt(consumer, false);
        }

        [TestCase("drink", "oil")] [TestCase("skin", "oil")] [TestCase("flask", "oil")]
        [TestCase("drink", "water")] [TestCase("skin", "water")] [TestCase("flask", "water")]
        public void WellWithAnExposedEmitterCannotBypassMixedSourceSafety(string consumer, string emitted)
        {
            var source = Source("finite"); var recipient = Recipient(consumer); Thirst();
            string command = Command(source, recipient, consumer);
            source.AddPart(new TileStateSourcePart { Coating = emitted, CoatingTurns = 3 });
            var hazard = emitted == "water" ? Pool(5, "acid") : null;
            AssertMenu(source, recipient, consumer, false);
            Assert.False(Draw(source, recipient, command));
            Assert.AreEqual(3, Water(source));
            if (recipient != null) Assert.AreEqual(0, Units(recipient));
            if (hazard != null) Assert.AreEqual(5, hazard.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(emitted, source.GetPart<TileStateSourcePart>().Coating);
            AssertDebt(consumer, false);
        }
    }
}
