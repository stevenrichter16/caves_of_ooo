using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The element table behind proofing (Docs/MACHINE-PROOFING.md). Element comes from a reagent's own property (heat, cold, conductive, corrosive), so a new reagent that carries one works with no new
    /// recipe, and the strength of the reagent (its property level) is how many coats it makes. The table is data; these tests pin that the data is coherent with the rest of the content.
    /// </summary>
    public sealed class ProofingRegistryTests : FiftyWorldFixture
    {
        [SetUp] public void SetupRegistry() { ProofingRegistry.ResetForTests(); }
        [TearDown] public void TearRegistry() { ProofingRegistry.ResetForTests(); }

        [Test] public void TheFourElementsAreTheFourResistancesTheGameHas()
        {
            CollectionAssert.AreEquivalent(new[] { "heat", "cold", "electric", "acid" }, ProofingRegistry.All.Select(e => e.Id).ToArray());
            CollectionAssert.AreEquivalent(new[] { "HeatResistance", "ColdResistance", "ElectricResistance", "AcidResistance" }, ProofingRegistry.All.Select(e => e.Stat).ToArray());
        }

        [Test] public void EachElementsLiquidIsARealLiquidAndEachPropertyIsOneTheAlchemyVocabularyKnows()
        {
            Assert.True(LiquidRegistry.IsInitialized); Assert.AreEqual(4, ProofingRegistry.All.Count, "the table is not empty, so the loop below is not vacuous");
            foreach (var e in ProofingRegistry.All)
            {
                Assert.NotNull(LiquidRegistry.Get(e.Liquid), e.Id + ": liquid " + e.Liquid);
                CollectionAssert.Contains(new[] { BrewProperties.Heat, BrewProperties.Cold, BrewProperties.Conductive, BrewProperties.Corrosive }, e.Property, e.Id);
                Assert.IsNotEmpty(e.Label, e.Id); Assert.IsNotEmpty(e.Status, e.Id + " names the status it also blunts");
            }
            Assert.AreEqual(ProofingRegistry.All.Count, ProofingRegistry.All.Select(e => e.Liquid).Distinct().Count(), "one liquid per element");
            Assert.AreEqual(ProofingRegistry.All.Count, ProofingRegistry.All.Select(e => e.Property).Distinct().Count(), "one property per element");
        }

        [Test] public void LookupsAreAliasAwareAndCaseInsensitive_AndTheOldFireKindIsHeat()
        {
            Assert.AreEqual("heat", ProofingRegistry.ById("fire").Id, "the clay-wash's saved kind");
            Assert.AreEqual("heat", ProofingRegistry.ById("HEAT").Id); Assert.AreEqual("cold", ProofingRegistry.ByLiquid("cold-proofing").Id); Assert.AreEqual("electric", ProofingRegistry.ByProperty("conductive").Id);
            Assert.AreEqual("HeatResistance", ProofingRegistry.StatFor("fire")); Assert.AreEqual("AcidResistance", ProofingRegistry.StatFor("acid"));
            Assert.IsNull(ProofingRegistry.ById("")); Assert.IsNull(ProofingRegistry.ById("nonsense")); Assert.IsNull(ProofingRegistry.ByLiquid("water"), "an ordinary liquid is not a proofing");
        }

        [Test] public void AnUnknownKindReadsAsAcidLikeEveryEarlierSaveDid_Counter()
        {
            Assert.AreEqual("acid", ProofingRegistry.Canonical("nonsense")); Assert.AreEqual("acid", ProofingRegistry.Canonical(null)); Assert.AreEqual("heat", ProofingRegistry.Canonical("fire"));
            Assert.AreEqual("AcidResistance", ProofingRegistry.StatFor("nonsense"));
        }

        [Test] public void EveryShippedReagentWithAnElementPropertyOffersThatElementAtItsOwnLevel()
        {
            var expected = new (string reagent, string element, int units)[]
            {
                ("FireMoss", "heat", 2), ("EmberFruit", "heat", 1), ("PitchpodResin", "heat", 1), ("FrostLichen", "cold", 2), ("GlacierSalt", "cold", 3), ("ChillcressTip", "cold", 1),
                ("SparkRoot", "electric", 3), ("PrismreedPith", "electric", 1), ("GlimmerBrine", "electric", 2), ("GlimmerBrine", "acid", 2), ("SourmantleFold", "acid", 1),
            };
            foreach (var (reagent, element, units) in expected)
            {
                var item = Factory.CreateEntity(reagent); Assert.NotNull(item, reagent);
                var found = ProofingRegistry.ElementsOf(item).FirstOrDefault(x => x.Id == element); Assert.NotNull(found, reagent + " offers " + element);
                Assert.AreEqual(units, ProofingRegistry.Units(item, found), reagent + " " + element);
            }
        }

        [Test] public void AReagentWithNoElementPropertyOffersNothing_Counter()
        {
            foreach (var name in new[] { "LampOil", "MendleafSprig", "BogSap", "StoneburrSeed" })
            {
                var item = Factory.CreateEntity(name); Assert.NotNull(item, name); Assert.IsEmpty(ProofingRegistry.ElementsOf(item), name);
            }
            Assert.IsEmpty(ProofingRegistry.ElementsOf(null)); Assert.IsEmpty(ProofingRegistry.ElementsOf(Factory.CreateEntity("Dagger")), "a thing that is not a reagent");
            Assert.AreEqual(0, ProofingRegistry.Units(Factory.CreateEntity("LampOil"), ProofingRegistry.ById("heat")));
        }

        [Test] public void TheRulesLoadFromJsonAndABadRowIsDroppedNotFatal()
        {
            ProofingRegistry.InitializeFromJson("{\"Elements\":[{\"Id\":\"heat\",\"Label\":\"heat\",\"Property\":\"heat\",\"Stat\":\"HeatResistance\",\"Liquid\":\"heat-proofing\",\"Status\":\"burns\"},{\"Id\":\"\",\"Property\":\"x\"},{\"Id\":\"dup\",\"Property\":\"heat\",\"Stat\":\"HeatResistance\",\"Liquid\":\"z\"}]}");
            Assert.AreEqual(1, ProofingRegistry.All.Count, "the empty id and the duplicate property are dropped"); Assert.AreEqual("heat", ProofingRegistry.All[0].Id);
            ProofingRegistry.InitializeFromJson("not json"); Assert.AreEqual(0, ProofingRegistry.All.Count, "malformed data leaves an empty table, never an exception");
        }
    }
}
