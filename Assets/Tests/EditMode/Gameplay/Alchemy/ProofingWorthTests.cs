using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Is proofing worth the trouble (Docs/MACHINE-PROOFING.md section 3)? A measuring bench, not a balance claim: the same hit is taken at several resistances and the cost of it is read off the game's own
    /// rules (HP lost, turns frozen, turns burning, turns stunned). Each must fall as resistance rises and reach nothing at immunity. The numbers the doc quotes are the ones this prints.
    /// </summary>
    public sealed class ProofingWorthTests
    {
        static readonly int[] Levels = { 0, 25, 35, 60, 85, 100 };

        static Entity Body(string stat, int resistance)
        {
            var e = new Entity { BlueprintName = "TestBody" }; e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1000, Min = 0, Max = 1000, Owner = e };
            e.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 0, Max = 100, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test body" });
            e.AddPart(new MaterialPart { MaterialID = "Flesh", Combustibility = 0.5f, MaterialTagsRaw = "Organic" });
            e.AddPart(new ThermalPart { Temperature = 25f, HeatCapacity = 1f, FlameTemperature = 400f });
            e.Statistics[stat] = new Stat { Name = stat, BaseValue = resistance, Min = -100, Max = 200, Owner = e };
            return e;
        }

        /// <summary>HP lost to one Soursprayer-class spray (1d4 direct is ignored; the corrosion it leaves is the real cost) until the corrosion is gone.</summary>
        static int AcidHpLost(int resistance)
        {
            var e = Body("AcidResistance", resistance); if (!e.ApplyEffect(new AcidicEffect(0.8f))) return 0;
            int before = e.GetStatValue("Hitpoints"); var acid = e.GetEffect<AcidicEffect>();
            for (int turn = 0; turn < 60 && acid.Corrosion > 0f; turn++) { acid.OnTurnStart(e, GameEvent.New("BeginTakeAction")); acid.OnTurnEnd(e); }   // the effect ends when the corrosion does
            return before - e.GetStatValue("Hitpoints");
        }

        static int FrozenTurns(int resistance) { var e = Body("ColdResistance", resistance); return e.ApplyEffect(new FrozenEffect(0.6f)) ? e.GetEffect<FrozenEffect>().TurnsToThaw : 0; }
        static int BurnTurns(int resistance) { var e = Body("HeatResistance", resistance); return e.ApplyEffect(new BurningEffect(1.5f)) ? e.GetEffect<BurningEffect>().Duration : 0; }
        static int StunTurns(int resistance) { var e = Body("ElectricResistance", resistance); return e.ApplyEffect(new ElectrifiedEffect(1f)) ? (e.GetEffect<StunnedEffect>()?.Duration ?? 0) : 0; }

        static void AssertFalls(string what, System.Func<int, int> measure)
        {
            var values = Levels.Select(measure).ToArray(); var table = string.Join("  ", Levels.Zip(values, (r, v) => "+" + r + ":" + v));
            Assert.Greater(values[0], 0, what + " must cost something with no resistance: " + table);
            for (int i = 1; i < values.Length; i++) Assert.LessOrEqual(values[i], values[i - 1], what + " never gets worse as resistance rises: " + table);
            Assert.Less(values[2], values[0], what + ": a coat (+35) must help: " + table); Assert.AreEqual(0, values.Last(), what + ": immunity costs nothing: " + table);
        }

        [Test] public void AcidCostsLessHpAtEveryStep() => AssertFalls("acid corrosion HP", AcidHpLost);
        [Test] public void AFreezeLocksYouForFewerTurnsAtEveryStep() => AssertFalls("frozen turns", FrozenTurns);
        [Test] public void ABurnLastsFewerTurnsAtEveryStep() => AssertFalls("burning turns", BurnTurns);
        [Test] public void AShockStunsForFewerTurnsAtEveryStep_OrNotAtAllAtTheStart() { var values = Levels.Select(StunTurns).ToArray(); Assert.AreEqual(2, values[0], string.Join(",", values)); Assert.AreEqual(0, values.Last()); Assert.LessOrEqual(values[2], values[0]); Assert.Less(values[3], values[0], "a stacked +60 shortens the stun: " + string.Join(",", values)); }

        [Test] public void TheNumbersTheDocQuotesAreTheOnesTheRulesGive()
        {
            CollectionAssert.AreEqual(new[] { 35, 15, 11, 7, 3, 0 }, Levels.Select(AcidHpLost).ToArray(), "acid corrosion HP lost");
            CollectionAssert.AreEqual(new[] { 6, 5, 4, 3, 1, 0 }, Levels.Select(FrozenTurns).ToArray(), "turns frozen");
            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1, 0 }, Levels.Select(BurnTurns).ToArray(), "turns burning");
            CollectionAssert.AreEqual(new[] { 2, 2, 1, 1, 1, 0 }, Levels.Select(StunTurns).ToArray(), "turns stunned");
        }
    }
}
