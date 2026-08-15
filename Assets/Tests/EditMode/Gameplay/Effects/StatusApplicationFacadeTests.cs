using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Step 3 of the status-system plan (Docs/STATUS-EFFECTS-STUDY-2026-08.md
    /// §6 Option 2): ONE application door. <c>Entity.ApplyEffect</c> is
    /// itself the façade — every one of the 91 production call sites
    /// becomes correct without being touched — internalizing the
    /// ObjectStatusMatrix verdict for non-creature targets.
    ///
    /// <para><b>The rule (behavior-preserving by construction):</b>
    /// creatures pass through unchanged; a non-creature receiving an
    /// effect the matrix HAS A ROW FOR gets the matrix verdict
    /// (WrongMaterial → refused + diag); a non-creature receiving an
    /// effect the matrix has NO ROW FOR passes through exactly as
    /// today — 20 of the 28 effect types in production are creature/item
    /// statuses (Stunned, Sitting, Recruited, WellRested…) that
    /// legitimately land on items, followers and statted props, and a
    /// fail-closed door would silently kill every one of them.</para>
    ///
    /// <para>The enforcement mechanism is a SOURCE-SCANNING test (the
    /// review's correction: reflection cannot see call sites, and
    /// `internal` is useless in a single-asmdef project): the substrate
    /// bypass — reaching <c>StatusEffectsPart.ApplyEffect</c> directly
    /// from production code — must have zero occurrences outside the
    /// façade itself, so an agent pattern-matching the wrong idiom turns
    /// into a RED test, not a silent bug.</para>
    /// </summary>
    public class StatusApplicationFacadeTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixtures ─────────────────────────────────────────────────

        private static Entity Creature(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "c" + x + "_" + y, BlueprintName = "Snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "snapjaw" });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity Prop(Zone zone, int x, int y, string tags)
        {
            var e = new Entity { ID = "p" + x + "_" + y, BlueprintName = "Prop" };
            e.AddPart(new RenderPart { DisplayName = "prop" });
            e.AddPart(new MaterialPart { MaterialTagsRaw = tags });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static int Refusals()
            => DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "ObjectEffectRefused", Limit = 20 }).Records.Count;

        // ── The three branches of the rule ───────────────────────────

        [Test]
        public void Creature_PassesThrough_MatrixNeverConsulted()
        {
            var zone = new Zone("Z");
            var c = Creature(zone, 5, 5);

            Assert.IsTrue(c.ApplyEffect(new BurningEffect(1.0f), null, zone));
            Assert.IsTrue(c.ApplyEffect(new StunnedEffect(duration: 2), null, zone));

            Assert.IsTrue(c.HasEffect<BurningEffect>());
            Assert.IsTrue(c.HasEffect<StunnedEffect>());
            Assert.AreEqual(0, Refusals(), "creatures never trip the object matrix");
        }

        [Test]
        public void Object_MatrixRow_WrongMaterial_IsRefusedAtTheDoor()
        {
            // The whole point: a stone wall cannot be set ablaze THROUGH
            // Entity.ApplyEffect anymore — the door the study found had
            // no wall now has one, for the effects the matrix knows.
            var zone = new Zone("Z");
            var wall = Prop(zone, 5, 5, "Stone");

            bool landed = wall.ApplyEffect(new BurningEffect(1.0f), null, zone);

            Assert.IsFalse(landed);
            Assert.IsFalse(wall.HasEffect<BurningEffect>());
            Assert.AreEqual(1, Refusals(), "a gate that rejects emits a record");
        }

        [Test]
        public void Object_MatrixRow_RightMaterial_Lands()
        {
            var zone = new Zone("Z");
            var hedge = Prop(zone, 5, 5, "Organic,Plant");

            Assert.IsTrue(hedge.ApplyEffect(new BurningEffect(1.0f), null, zone));
            Assert.IsTrue(hedge.HasEffect<BurningEffect>());
            Assert.AreEqual(0, Refusals());
        }

        [Test]
        public void Object_NoMatrixRow_PassesThroughUnchanged()
        {
            // The behavior-preservation clause: 20 shipped effect types
            // land on non-creatures today with no matrix row (Broken on
            // equipment, Recruited on followers, gas statuses on statted
            // props...). Those must keep landing exactly as before —
            // fail-closed here would break shipped mechanics.
            var zone = new Zone("Z");
            var sword = Prop(zone, 5, 5, "Metal");

            Assert.IsTrue(sword.ApplyEffect(new StunnedEffect(duration: 2), null, zone),
                "no row == today's ungated behavior, preserved");
            Assert.IsTrue(sword.HasEffect<StunnedEffect>());
            Assert.AreEqual(0, Refusals(), "and it is NOT a refusal");
        }

        // ── The study's named violators are closed by construction ───

        [Test]
        public void Oilmark_CanNoLongerCoatAStoneWall_ButCoatsWood()
        {
            // Study §4 violator #1: Pyromancy_Oilmark applied
            // LiquidCoveredEffect straight through Entity.ApplyEffect to
            // scenery. The effect now has a matrix row (Flammable —
            // coating something is prep for igniting it, so it follows
            // the fire rule) and the door enforces it, WITHOUT touching
            // Oilmark's code.
            var zone = new Zone("Z");
            var wall = Prop(zone, 5, 5, "Stone");
            var crate = Prop(zone, 6, 5, "Wood");

            Assert.IsFalse(wall.ApplyEffect(new LiquidCoveredEffect("oil", 1), null, zone),
                "stone refuses the oil coat");
            Assert.IsTrue(crate.ApplyEffect(new LiquidCoveredEffect("oil", 1), null, zone),
                "wood takes it");
        }

        [Test]
        public void Matrix_HasARowForLiquidCovered()
        {
            var zone = new Zone("Z");
            var wall = Prop(zone, 5, 5, "Stone");
            var crate = Prop(zone, 6, 5, "Wood");
            var probe = new LiquidCoveredEffect("oil", 1);
            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(probe, wall));
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(probe, crate));
        }

        // ── Two strictness levels, ONE table ─────────────────────────

        [Test]
        public void TryApply_AndApplyEffect_AgreeOnEveryRowVerdict()
        {
            // Both doors read the same table: a WrongMaterial refusal and a
            // right-material landing come out identical from either.
            var zone = new Zone("Z");
            var stone = Prop(zone, 5, 5, "Stone");
            var stone2 = Prop(zone, 6, 5, "Stone");
            var wood = Prop(zone, 7, 5, "Wood");
            var wood2 = Prop(zone, 8, 5, "Wood");

            Assert.AreEqual(
                ObjectStatusMatrix.TryApply(new BurningEffect(1f), stone, null, zone),
                stone2.ApplyEffect(new BurningEffect(1f), null, zone), "wrong material: both refuse");
            Assert.AreEqual(
                ObjectStatusMatrix.TryApply(new BurningEffect(1f), wood, null, zone),
                wood2.ApplyEffect(new BurningEffect(1f), null, zone), "right material: both land");
        }

        [Test]
        public void TryApply_IsTheStrictDoor_NoRowMeansRefused()
        {
            // The one documented divergence: the universal door lets a
            // no-row effect through (preservation); the strict door a
            // caller names on purpose refuses it as Meaningless — the
            // original TryApply contract, unchanged.
            var zone = new Zone("Z");
            var barrel = Prop(zone, 5, 5, "Wood");
            var barrel2 = Prop(zone, 6, 5, "Wood");

            Assert.IsFalse(ObjectStatusMatrix.TryApply(new BleedingEffect(saveTarget: 10, damageDice: "1d4"), barrel, null, zone),
                "strict door: a barrel cannot bleed");
            Assert.IsTrue(barrel2.ApplyEffect(new BleedingEffect(saveTarget: 10, damageDice: "1d4"), null, zone),
                "universal door: no row passes, exactly as before");
            Assert.AreEqual(1, Refusals(), "and only the strict refusal is recorded");
        }

        // ── ENFORCEMENT: the substrate bypass has zero production users ─

        [Test]
        public void NoProductionCode_BypassesTheFacade()
        {
            // Source-scanning enforcement (reflection cannot see call
            // sites). The ONLY legal way to put an effect on an entity is
            // Entity.ApplyEffect / ForceApplyEffect (the façade) or
            // ObjectStatusMatrix.TryApply (the same door). Reaching
            // StatusEffectsPart.ApplyEffect directly skips the matrix.
            // If this test goes RED, an agent pattern-matched the wrong
            // idiom — fix the call site, do not whitelist it.
            string root = Path.Combine(Application.dataPath, "Scripts");
            var offenders = new System.Collections.Generic.List<string>();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                // The façade and the substrate are the two legal homes.
                if (name == "Entity.cs" || name == "StatusEffectsPart.cs") continue;

                string src = File.ReadAllText(file);
                // GetPart<StatusEffectsPart>()... .ApplyEffect( / .ForceApplyEffect(
                if (System.Text.RegularExpressions.Regex.IsMatch(src,
                        @"StatusEffectsPart>\(\)\s*\??\.\s*(Force)?ApplyEffect\(")
                    || System.Text.RegularExpressions.Regex.IsMatch(src,
                        @"\beffects\s*\??\.\s*(Force)?ApplyEffect\("))
                    offenders.Add(file.Substring(root.Length + 1));
            }
            Assert.IsEmpty(offenders,
                "these files reach past the façade to StatusEffectsPart directly:\n  "
                + string.Join("\n  ", offenders));
        }
    }
}
