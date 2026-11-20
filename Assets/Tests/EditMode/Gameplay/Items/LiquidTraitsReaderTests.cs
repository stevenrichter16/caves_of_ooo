using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// What a flask of liquid is good for, said plainly when you examine it (Docs/MACHINE-INVENTED-USES.md, 3p quality of life). The still turns mire into water or oil, and nothing in the
    /// game said that oil burns or that water carries a charge; the numbers were in the liquid data and in no reader. The line is derived from that data (no list of names), never shows
    /// a number, never appears on an empty flask, and changes nothing.
    /// </summary>
    public sealed class LiquidTraitsReaderTests : FiftyWorldFixture
    {
        string Details(Entity flask) { Assert.True(ItemExamineService.TryDescribeDetails(flask, out string text)); return text; }

        [Test] public void OilSaysItBurnsReadilyAndMakesGroundSlippery() => StringAssert.Contains("It burns readily and makes ground slippery.", Details(Flask(7, "oil")));

        [Test] public void WaterSaysItCarriesAChargeAndDampsFire() => StringAssert.Contains("It carries a charge and damps fire.", Details(Flask(5, "water")));

        [Test] public void BogMireSaysItDampsFire_WhichIsWhyPouringItOnYourHearthPutsItOut() => StringAssert.Contains("It damps fire.", Details(Flask(12, "bog-mire")));

        [Test] public void BrineSaysItCarriesAChargeAndNothingElse() { string text = Details(Flask(4, "brine")); StringAssert.Contains("It carries a charge.", text); StringAssert.DoesNotContain("damps", text); StringAssert.DoesNotContain("burns", text); }

        [Test] public void AStickyThingSaysItClings() => StringAssert.Contains("It burns readily and clings.", Details(Flask(3, "pitch")));

        [Test] public void ALiquidWithNothingToSayGetsNoLine_Counter()
        {
            string text = Details(Flask(2, "acid")); StringAssert.Contains("Contents: acid", text);
            foreach (var word in new[] { "It burns", "It carries", "It damps", "It makes", "It clings" }) StringAssert.DoesNotContain(word, text);
        }

        [Test] public void AnEmptyFlaskSaysNothingOfTheKind_Counter()
        {
            string text = Details(Flask(0, "oil")); StringAssert.Contains("Contents: empty", text); StringAssert.DoesNotContain("It burns", text);
        }

        [Test] public void ReadingChangesNothing()
        {
            var flask = Flask(7, "oil"); var vessel = flask.GetPart<LiquidVesselPart>(); Details(flask); Details(flask);
            Assert.AreEqual(7, vessel.Volume); Assert.AreEqual("oil", vessel.LiquidId);
        }

        [Test] public void ThresholdsAreTheDefinitionsOwnNumbersNotAListOfNames()
        {
            var def = new LiquidDefinition { Id = "x", DisplayName = "x", Combustibility = LiquidDefinition.BurnsReadilyAt - 1, Conductivity = LiquidDefinition.CarriesChargeAt - 1, FireDampen = LiquidDefinition.DampsFireAt - 1 };
            Assert.AreEqual("", def.DescribeTraits(), "one short of every threshold says nothing");
            def.Combustibility++; def.Conductivity++; def.FireDampen++;
            Assert.AreEqual("It burns readily, carries a charge and damps fire.", def.DescribeTraits(), "at every threshold, in a fixed order");
            Assert.AreEqual("", new LiquidDefinition { Id = "y", Combustibility = -50 }.DescribeTraits(), "water's negative combustibility is not 'burns'");
        }
    }
}
