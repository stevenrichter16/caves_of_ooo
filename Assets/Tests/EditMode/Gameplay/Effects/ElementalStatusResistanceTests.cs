using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// A resistance now blunts the status as well as the damage (Docs/MACHINE-PROOFING.md). Until now ColdResistance, HeatResistance, ElectricResistance and AcidResistance only scaled the
    /// damage numbers: Frozen (which does no damage at all), Burning's duration, Electrified's stun and Acidic's corrosion never read them. The rule is one line per status: a positive resistance
    /// shortens or weakens the status in proportion ((100 - resistance) percent), 100 or more is immunity to it, a negative resistance (a vulnerability) changes nothing here (the damage side
    /// already amplifies), and only the matching stat counts. Forced applications (a scripted setup, a test) bypass it, as they bypass every other check.
    /// </summary>
    public class ElementalStatusResistanceTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }

        static Entity Body(params (string stat, int value)[] resistances)
        {
            var e = new Entity { BlueprintName = "TestBody" }; e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100, Owner = e };
            e.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 0, Max = 100, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test body" });
            e.AddPart(new MaterialPart { MaterialID = "Flesh", Combustibility = 0.5f, MaterialTagsRaw = "Organic" });
            e.AddPart(new ThermalPart { Temperature = 25f, HeatCapacity = 1f, FlameTemperature = 400f });
            foreach (var (stat, value) in resistances) e.Statistics[stat] = new Stat { Name = stat, BaseValue = value, Min = -100, Max = 200, Owner = e };
            return e;
        }

        // ── Frozen ──

        [Test] public void NoColdResistanceLeavesTheFreezeAsItWas_Control()
        {
            var e = Body(); Assert.True(e.ApplyEffect(new FrozenEffect(0.6f))); Assert.AreEqual(0.6f, e.GetEffect<FrozenEffect>().Cold, 1e-4f);
        }

        [Test] public void ColdResistanceShortensTheFreezeInProportion()
        {
            var e = Body(("ColdResistance", 35)); Assert.True(e.ApplyEffect(new FrozenEffect(0.6f))); Assert.AreEqual(0.39f, e.GetEffect<FrozenEffect>().Cold, 1e-3f, "0.6 x 65%");
            var half = Body(("ColdResistance", 50)); half.ApplyEffect(new FrozenEffect(0.6f)); Assert.Less(half.GetEffect<FrozenEffect>().TurnsToThaw, 4, "a half-strength freeze thaws in under four turns, not six");
        }

        [Test] public void FullColdResistanceIsImmunityToBeingFrozen()
        {
            var e = Body(("ColdResistance", 100)); Assert.False(e.ApplyEffect(new FrozenEffect(0.6f))); Assert.IsNull(e.GetEffect<FrozenEffect>()); Assert.True(e.GetPart<StatusEffectsPart>() == null || e.GetPart<StatusEffectsPart>().EffectCount == 0);
        }

        [Test] public void AFreezeNeverDropsBelowTheFloorWhileResistanceIsShortOfImmunity()
        {
            var e = Body(("ColdResistance", 95)); Assert.True(e.ApplyEffect(new FrozenEffect(0.6f))); Assert.AreEqual(FrozenEffect.FREEZE_FLOOR, e.GetEffect<FrozenEffect>().Cold, 1e-4f);
        }

        [Test] public void AVulnerabilityDoesNotDeepenTheFreezeHere_Counter()
        {
            var e = Body(("ColdResistance", -50)); e.ApplyEffect(new FrozenEffect(0.6f)); Assert.AreEqual(0.6f, e.GetEffect<FrozenEffect>().Cold, 1e-4f, "the damage side amplifies a vulnerability; the status is left alone");
        }

        [Test] public void OnlyTheMatchingResistanceCounts_Counter()
        {
            var e = Body(("HeatResistance", 100), ("ElectricResistance", 100), ("AcidResistance", 100)); Assert.True(e.ApplyEffect(new FrozenEffect(0.6f))); Assert.AreEqual(0.6f, e.GetEffect<FrozenEffect>().Cold, 1e-4f);
        }

        [Test] public void AStackedSecondFreezeIsDampenedToo()
        {
            var e = Body(("ColdResistance", 50)); e.ApplyEffect(new FrozenEffect(0.8f)); e.ApplyEffect(new FrozenEffect(0.8f));
            Assert.AreEqual(0.4f + 0.4f * 0.5f, e.GetEffect<FrozenEffect>().Cold, 1e-3f, "each dose is halved before it stacks");
        }

        [Test] public void AForcedApplicationBypassesIt_Counter()
        {
            var e = Body(("ColdResistance", 100)); Assert.True(e.ForceApplyEffect(new FrozenEffect(0.6f))); Assert.AreEqual(0.6f, e.GetEffect<FrozenEffect>().Cold, 1e-4f);
        }

        // ── Burning ──

        [Test] public void HeatResistanceShortensABurnOnAThingWithNoFuel()
        {
            var plain = Body(); plain.ApplyEffect(new BurningEffect(1f)); Assert.AreEqual(3, plain.GetEffect<BurningEffect>().Duration, "control: three turns");
            var half = Body(("HeatResistance", 50)); half.ApplyEffect(new BurningEffect(1f)); Assert.AreEqual(2, half.GetEffect<BurningEffect>().Duration, "3 x 50% rounds to 2");
            var most = Body(("HeatResistance", 80)); most.ApplyEffect(new BurningEffect(1f)); Assert.AreEqual(1, most.GetEffect<BurningEffect>().Duration, "never below one turn short of immunity");
        }

        [Test] public void FullHeatResistanceCannotBeSetAlight()
        {
            var e = Body(("HeatResistance", 100)); Assert.False(e.ApplyEffect(new BurningEffect(2f))); Assert.IsNull(e.GetEffect<BurningEffect>());
        }

        [Test] public void AVulnerabilityToHeatDoesNotLengthenTheBurnHere_Counter()
        {
            var e = Body(("HeatResistance", -50)); e.ApplyEffect(new BurningEffect(1f)); Assert.AreEqual(3, e.GetEffect<BurningEffect>().Duration);
        }

        // ── Electrified ──

        [Test] public void ElectricResistanceShortensTheChargeAndTheStun()
        {
            var plain = Body(); plain.ApplyEffect(new ElectrifiedEffect(1f)); Assert.AreEqual(2, plain.GetEffect<ElectrifiedEffect>().Duration, "control"); Assert.AreEqual(2, plain.GetEffect<StunnedEffect>().Duration, "control");
            var half = Body(("ElectricResistance", 50)); half.ApplyEffect(new ElectrifiedEffect(1f)); Assert.AreEqual(1, half.GetEffect<ElectrifiedEffect>().Duration); Assert.AreEqual(1, half.GetEffect<StunnedEffect>().Duration);
        }

        [Test] public void FullElectricResistanceIsNeitherShockedNorStunned()
        {
            var e = Body(("ElectricResistance", 100)); Assert.False(e.ApplyEffect(new ElectrifiedEffect(1f))); Assert.IsNull(e.GetEffect<ElectrifiedEffect>()); Assert.IsNull(e.GetEffect<StunnedEffect>());
        }

        // ── Acidic ──

        [Test] public void AcidResistanceWeakensTheCorrosion()
        {
            var e = Body(("AcidResistance", 50)); e.ApplyEffect(new AcidicEffect(0.8f)); Assert.AreEqual(0.4f, e.GetEffect<AcidicEffect>().Corrosion, 1e-3f);
            var plain = Body(); plain.ApplyEffect(new AcidicEffect(0.8f)); Assert.AreEqual(0.8f, plain.GetEffect<AcidicEffect>().Corrosion, 1e-4f, "control");
        }

        [Test] public void FullAcidResistanceIsImmuneToCorrosion()
        {
            var e = Body(("AcidResistance", 100)); Assert.False(e.ApplyEffect(new AcidicEffect(0.8f))); Assert.IsNull(e.GetEffect<AcidicEffect>());
        }

        // ── the immunity says so ──

        [Test] public void AnImmuneBodySaysItShrugsOffTheStatus_NamingWhichOne()
        {
            foreach (var (stat, effect, word) in new (string, Effect, string)[] { ("ColdResistance", new FrozenEffect(0.6f), "freezing"), ("HeatResistance", new BurningEffect(1f), "burning"), ("ElectricResistance", new ElectrifiedEffect(1f), "shock"), ("AcidResistance", new AcidicEffect(0.8f), "corrosion") })
            {
                MessageLog.Clear(); var e = Body((stat, 100)); e.Tags["Player"] = ""; Assert.False(e.ApplyEffect(effect)); StringAssert.Contains("You shrug off the " + word, string.Join("|", MessageLog.GetRecent(3)), stat);
            }
        }

        [Test] public void ADampenedButNotImmuneStatusSaysNothingOfShruggingItOff_Counter()
        {
            MessageLog.Clear(); var e = Body(("ColdResistance", 50)); e.Tags["Player"] = ""; Assert.True(e.ApplyEffect(new FrozenEffect(0.6f))); StringAssert.DoesNotContain("shrug", string.Join("|", MessageLog.GetRecent(5)));
        }

        // ── the refusal is observable ──

        [Test] public void AnImmuneRefusalSaysWhyInTheDiagStream()
        {
            var e = Body(("ColdResistance", 100)); e.ApplyEffect(new FrozenEffect(0.6f));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "effect", Kind = "ElementalImmunity", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count); StringAssert.Contains("ColdResistance", records[0].PayloadJson);
        }
    }
}
