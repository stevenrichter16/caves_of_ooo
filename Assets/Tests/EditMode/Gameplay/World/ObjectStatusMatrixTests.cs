using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// What a status effect means for a thing that is not alive
    /// (<c>Docs/OBJECT-INTERACTION-PLAN.md</c> §5.2).
    ///
    /// <para>The property worth protecting here is that the table
    /// <b>fails closed</b>. A rule of "effects apply to objects unless
    /// listed" would mean every effect added later silently starts working
    /// on furniture, and the first anyone notices is a bleed timer counting
    /// down on a barrel.</para>
    /// </summary>
    public class ObjectStatusMatrixTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        /// <summary>An object made of the given material tags.</summary>
        private static Entity Thing(string blueprint, params string[] materialTags)
        {
            var e = new Entity { ID = blueprint, BlueprintName = blueprint };
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new DestructiblePart { HP = 10, MaxHP = 10 });
            // AddPart calls Initialize, which is what parses MaterialTagsRaw.
            e.AddPart(new MaterialPart { MaterialTagsRaw = string.Join(",", materialTags) });
            return e;
        }

        private static Entity Hedge() => Thing("Hedge", "Organic", "Plant");
        private static Entity StoneWall() => Thing("StoneWall", "Stone", "Mineral");
        private static Entity CopperPipe() => Thing("CopperPipe", "Metal", "Conductor");

        private static Entity Creature()
        {
            var e = new Entity { ID = "npc", BlueprintName = "Villager" };
            e.Tags["Creature"] = "";
            return e;
        }

        // ════════════════════════════════════════════════════════
        // Fire
        // ════════════════════════════════════════════════════════

        [Test]
        public void FireIgnitesAHedge()
        {
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), Hedge()));
        }

        [Test]
        public void FireDoesNothingToAStoneWall()
        {
            // Counter-check to the above. If the material gate were absent
            // both would return Applies and the first assertion would still
            // pass — this is the one that proves the gate exists.
            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), StoneWall()));
        }

        // ════════════════════════════════════════════════════════
        // Electricity
        // ════════════════════════════════════════════════════════

        [Test]
        public void LightningTakesToACopperPipe()
        {
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), CopperPipe()));
        }

        [Test]
        public void LightningDoesNothingToAHedge()
        {
            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), Hedge()));
        }

        // ════════════════════════════════════════════════════════
        // The fail-closed property
        // ════════════════════════════════════════════════════════

        [Test]
        public void YouCannotMakeABarrelBleed()
        {
            // The row the whole design exists for. Bleeding, Poisoned,
            // Stunned and Confused are all creature-shaped; a silent success
            // would put a countdown timer on furniture.
            var barrel = Thing("WoodenBarrel", "Wood", "Organic");

            Assert.AreEqual(ObjectStatusVerdict.Meaningless,
                ObjectStatusMatrix.Evaluate(new BleedingEffect(), barrel), "bleeding");
            Assert.AreEqual(ObjectStatusVerdict.Meaningless,
                ObjectStatusMatrix.Evaluate(new PoisonedEffect(), barrel), "poisoned");
            Assert.AreEqual(ObjectStatusVerdict.Meaningless,
                ObjectStatusMatrix.Evaluate(new StunnedEffect(), barrel), "stunned");
            Assert.AreEqual(ObjectStatusVerdict.Meaningless,
                ObjectStatusMatrix.Evaluate(new ConfusedEffect(), barrel), "confused");
        }

        [Test]
        public void ACreatureIsNotThisTablesBusiness()
        {
            // The matrix governs scenery only. Creatures keep their own
            // gates — CanBeAppliedTo, resistances, saves — and must not be
            // second-guessed here, or bleeding would stop working on people.
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new BleedingEffect(), Creature()));
        }

        // ════════════════════════════════════════════════════════
        // The door
        // ════════════════════════════════════════════════════════

        [Test]
        public void TryApply_LandsTheEffectWhenTheMatrixAllowsIt()
        {
            var hedge = Hedge();
            Assert.IsTrue(ObjectStatusMatrix.TryApply(new BurningEffect(), hedge, null, null));
            Assert.IsNotNull(hedge.GetPart<StatusEffectsPart>()?.GetEffect<BurningEffect>(),
                "the hedge should actually be on fire, not merely permitted to be");
        }

        [Test]
        public void TryApply_RefusesAndLeavesNoTrace()
        {
            var wall = StoneWall();
            Assert.IsFalse(ObjectStatusMatrix.TryApply(new BurningEffect(), wall, null, null));
            Assert.IsNull(wall.GetPart<StatusEffectsPart>()?.GetEffect<BurningEffect>(),
                "a refused effect must not be half-applied");
        }

        [Test]
        public void ARefusalSaysWhichKindItWas()
        {
            // "Nothing happened" is the hardest bug class to chase. The two
            // refusals have different fixes — author a material tag, or add
            // a matrix row — so the record has to tell them apart.
            ObjectStatusMatrix.TryApply(new BurningEffect(), StoneWall(), null, null);
            ObjectStatusMatrix.TryApply(new BleedingEffect(), Hedge(), null, null);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "ObjectEffectRefused", Limit = 10 }).Records;

            Assert.AreEqual(2, records.Count);
            string both = records[0].PayloadJson + "|" + records[1].PayloadJson;
            StringAssert.Contains("wrong_material", both, "fire on stone");
            StringAssert.Contains("meaningless_on_objects", both, "bleed on a hedge");
        }

        // ════════════════════════════════════════════════════════
        // Degenerate
        // ════════════════════════════════════════════════════════

        [Test]
        public void NullsAreRefusedRatherThanThrowing()
        {
            Assert.AreEqual(ObjectStatusVerdict.Meaningless,
                ObjectStatusMatrix.Evaluate(null, Hedge()));
            Assert.AreEqual(ObjectStatusVerdict.Meaningless,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), null));
            Assert.IsFalse(ObjectStatusMatrix.TryApply(null, Hedge(), null, null));
        }

        [Test]
        public void AnObjectWithNoMaterialAtAll_BurnsOnlyIfItSaysSo()
        {
            // Most scenery carries no MaterialPart. The gate reads entity
            // tags too, so a blueprint can declare "Flammable" directly
            // without being given a whole material profile.
            var plain = new Entity { ID = "x", BlueprintName = "Signpost" };
            plain.AddPart(new PhysicsPart());
            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), plain));

            plain.Tags["Flammable"] = "";
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), plain));
        }
    }
}
