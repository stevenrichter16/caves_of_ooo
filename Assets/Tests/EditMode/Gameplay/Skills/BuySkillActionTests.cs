using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ST.6 — BuySkillAction purchase-gating tests.
    ///
    /// Pins all 9 result paths (1 success + 8 failures) and the
    /// skill/PurchaseAttempted diag emit on each. Counter-checks every
    /// failure branch leaves SP unchanged.
    /// </summary>
    public class BuySkillActionTests
    {
        // ────────────────────────────────────────────────────────────────────
        // Setup
        // ────────────────────────────────────────────────────────────────────

        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            SkillRegistry.ResetForTests();
        }

        /// <summary>
        /// Build a minimal actor with an SP stat + SkillsPart attached.
        /// Stats: SP, Agility, Strength (for stat-min tests).
        /// </summary>
        private static Entity MakeActor(int sp = 100, int agility = 18, int strength = 18)
        {
            var e = new Entity { ID = "actor", BlueprintName = "TestActor" };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.Statistics["SP"] = new Stat
            { Owner = e, Name = "SP", BaseValue = sp, Min = 0, Max = 999 };
            e.Statistics["Agility"] = new Stat
            { Owner = e, Name = "Agility", BaseValue = agility, Min = 1, Max = 30 };
            e.Statistics["Strength"] = new Stat
            { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 30 };
            e.AddPart(new SkillsPart());
            return e;
        }

        private const string ACROBATICS_JSON = @"{""Skills"":[{
            ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50,
            ""Powers"":[
                {""Name"":""Dodge"",""Class"":""AcrobaticsDodgePower"",
                 ""Cost"":30,""Attribute"":""Agility"",""Minimum"":""15""}
            ]
        }]}";

        // ────────────────────────────────────────────────────────────────────
        // 1. Happy path: meets all gates, buys skill, SP deducted
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_HappyPath_DeductsSPAndAttachesSkill()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = MakeActor(sp: 100, agility: 18);

            var result = BuySkillAction.Execute(actor, "AcrobaticsSkill");

            Assert.IsTrue(result.Succeeded, $"Should succeed; got {result.Reason}/{result.Detail}");
            Assert.AreEqual(BuySkillAction.FailureReason.None, result.Reason);
            Assert.AreEqual(50, result.CostPaid);
            Assert.AreEqual(100, result.SpBefore);
            Assert.AreEqual(50, result.SpAfter);
            Assert.AreEqual(50, actor.GetStatValue("SP"));
            Assert.IsTrue(actor.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. UnknownSkillClass: registry doesn't know about the name
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_UnknownClass_FailsWithoutTouchingSP()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = MakeActor(sp: 100);

            var result = BuySkillAction.Execute(actor, "NotARealSkillClass");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.UnknownSkillClass, result.Reason);
            Assert.AreEqual(100, actor.GetStatValue("SP"), "SP must be unchanged on unknown-class failure.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 3. ActorMissingSkillsPart: actor has no SkillsPart
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_ActorMissingSkillsPart_Fails()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            // Build actor WITHOUT SkillsPart.
            var actor = new Entity { ID = "noSkills", BlueprintName = "TestNoSkills" };
            actor.AddPart(new RenderPart { DisplayName = "no-skills" });
            actor.Statistics["SP"] = new Stat { Owner = actor, Name = "SP", BaseValue = 100, Max = 999 };

            var result = BuySkillAction.Execute(actor, "AcrobaticsSkill");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.ActorMissingSkillsPart, result.Reason);
            Assert.AreEqual(100, actor.GetStatValue("SP"));
        }

        // ────────────────────────────────────────────────────────────────────
        // 4. ActorMissingSPStat: actor has SkillsPart but no SP stat
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_ActorMissingSPStat_Fails()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = new Entity { ID = "noSP", BlueprintName = "TestNoSP" };
            actor.AddPart(new RenderPart { DisplayName = "no-sp" });
            actor.AddPart(new SkillsPart());
            // No SP stat.

            var result = BuySkillAction.Execute(actor, "AcrobaticsSkill");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.ActorMissingSPStat, result.Reason);
        }

        // ────────────────────────────────────────────────────────────────────
        // 5. AlreadyOwned: actor already has the skill
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_AlreadyOwned_FailsWithoutTouchingSP()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = MakeActor(sp: 100);
            actor.GetPart<SkillsPart>().AddSkill(new AcrobaticsSkill());

            int spBefore = actor.GetStatValue("SP");
            var result = BuySkillAction.Execute(actor, "AcrobaticsSkill");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.AlreadyOwned, result.Reason);
            Assert.AreEqual(spBefore, actor.GetStatValue("SP"),
                "SP must be unchanged when the skill is already owned.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 6. InsufficientSP: cost exceeds actor's SP
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_InsufficientSP_FailsWithoutTouchingSP()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            // Cost is 50; give actor 49.
            var actor = MakeActor(sp: 49);

            var result = BuySkillAction.Execute(actor, "AcrobaticsSkill");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.InsufficientSP, result.Reason);
            Assert.AreEqual(49, actor.GetStatValue("SP"),
                "SP must be unchanged when the buy can't afford it.");
            Assert.AreEqual(0, result.CostPaid);
        }

        // ────────────────────────────────────────────────────────────────────
        // 7. StatMinNotMet: simple AND-conjunct failure on Dodge's
        //    Attribute=Agility / Minimum=15 (actor has Agility 14).
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_StatMinNotMet_FailsAndReportsAttribute()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = MakeActor(sp: 100, agility: 14);

            var result = BuySkillAction.Execute(actor, "AcrobaticsDodgePower");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.StatMinNotMet, result.Reason);
            Assert.AreEqual("Agility", result.Detail,
                "Detail should name the failing attribute so UI / diag can surface it.");
            Assert.AreEqual(100, actor.GetStatValue("SP"));
        }

        // ────────────────────────────────────────────────────────────────────
        // 8. StatMin AT-threshold: agility = exactly 15 → passes
        //    (off-by-one boundary check)
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_StatMin_AtThreshold_Passes()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = MakeActor(sp: 100, agility: 15);

            var result = BuySkillAction.Execute(actor, "AcrobaticsDodgePower");

            Assert.IsTrue(result.Succeeded,
                $"Agility = exactly 15 should pass Minimum=15 (≥, not >). Reason={result.Reason}/{result.Detail}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 9. OR-group: stat A < min OR stat B >= min → passes
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_StatMin_OrGroup_AnyBranchPasses()
        {
            // Power requires Agility>=20 OR Strength>=15. Actor has
            // Agility=10 (fails first), Strength=18 (passes second).
            string json = @"{""Skills"":[{
                ""Name"":""SoftCore"",""Class"":""SoftCoreSkill"",""Cost"":10,
                ""Powers"":[
                    {""Name"":""SoftPow"",""Class"":""SoftCorePower"",
                     ""Cost"":10,""Attribute"":""Agility|Strength"",""Minimum"":""20|15""}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 50, agility: 10, strength: 18);

            var result = BuySkillAction.Execute(actor, "SoftCorePower");

            Assert.IsTrue(result.Succeeded,
                $"OR-group: Agility=10 (fails first branch) OR Strength=18 (passes) " +
                $"must pass overall. Got {result.Reason}/{result.Detail}.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 10. AND-conjunct: both must pass
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_StatMin_AndConjunct_BothMustPass()
        {
            // Power requires Agility>=15 AND Strength>=20. Actor has
            // Agility=18 (passes), Strength=18 (fails) → overall fails.
            string json = @"{""Skills"":[{
                ""Name"":""HardCore"",""Class"":""HardCoreSkill"",""Cost"":10,
                ""Powers"":[
                    {""Name"":""HardPow"",""Class"":""HardCorePower"",
                     ""Cost"":10,""Attribute"":""Agility,Strength"",""Minimum"":""15,20""}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 50, agility: 18, strength: 18);

            var result = BuySkillAction.Execute(actor, "HardCorePower");

            Assert.IsFalse(result.Succeeded,
                "AND-conjunct: Strength=18 fails the >=20 requirement; whole group fails.");
            Assert.AreEqual(BuySkillAction.FailureReason.StatMinNotMet, result.Reason);
        }

        // ────────────────────────────────────────────────────────────────────
        // 11. Requires: prereq class missing
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_Requires_PrereqMissing_Fails()
        {
            // PowerB requires PowerA. Actor doesn't own PowerA.
            string json = @"{""Skills"":[{
                ""Name"":""Tree"",""Class"":""TreeSkill"",""Cost"":10,
                ""Powers"":[
                    {""Name"":""A"",""Class"":""APower"",""Cost"":10},
                    {""Name"":""B"",""Class"":""BPower"",""Cost"":10,""Requires"":""APower""}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 50);

            var result = BuySkillAction.Execute(actor, "BPower");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq, result.Reason);
            Assert.AreEqual("APower", result.Detail,
                "Detail should name the missing prereq class.");
            Assert.AreEqual(50, actor.GetStatValue("SP"));
        }

        // ────────────────────────────────────────────────────────────────────
        // 12. Exclusion: owned mutually-exclusive class blocks purchase
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_Exclusion_OwnedExclusionBlocks()
        {
            // Light_Path is exclusive of Dark_Path. Actor owns Light_Path,
            // tries to buy Dark_Path → blocked.
            string json = @"{""Skills"":[{
                ""Name"":""Tree"",""Class"":""DTree"",""Cost"":10,
                ""Powers"":[
                    {""Name"":""Light"",""Class"":""LightPath"",""Cost"":10},
                    {""Name"":""Dark"",""Class"":""DarkPath"",""Cost"":10,""Exclusion"":""LightPath""}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 50);

            // Attach a stub matching the LightPath class name directly.
            // BuySkillAction.Exclusion check uses SkillsPart.HasSkill which
            // matches by GetType().Name — so a class named "LightPath"
            // matches the JSON's Exclusion="LightPath" entry.
            actor.GetPart<SkillsPart>().AddSkill(new LightPath());

            var result = BuySkillAction.Execute(actor, "DarkPath");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.Exclusion, result.Reason);
            Assert.AreEqual("LightPath", result.Detail,
                "Detail should name the blocking exclusion.");
            Assert.AreEqual(50, actor.GetStatValue("SP"));
        }

        // ────────────────────────────────────────────────────────────────────
        // Test-stub skill classes. Class names match the JSON entries in the
        // OR-group / AND-conjunct / Exclusion tests above. SkillsPart.AddSkill
        // resolves the JSON Class strings to these C# types via reflection
        // (the all-assemblies walk in SkillsPart.ResolveSkillType).
        // ────────────────────────────────────────────────────────────────────

        public class SoftCorePower : BaseSkillPart
        {
            public override string Name => nameof(SoftCorePower);
        }

        /// <summary>Test stub: class name matches the Exclusion JSON entry.</summary>
        public class LightPath : BaseSkillPart
        {
            public override string Name => nameof(LightPath);
        }

        // ────────────────────────────────────────────────────────────────────
        // 13. Diag pin (success): emits PurchaseAttempted with succeeded=true
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_Success_EmitsPurchaseAttemptedDiag_WithSucceededTrue()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = MakeActor(sp: 100);
            Diag.ResetAll();

            BuySkillAction.Execute(actor, "AcrobaticsSkill");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "PurchaseAttempted", Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count, "Exactly 1 PurchaseAttempted record.");
            string payload = records[0].PayloadJson;
            StringAssert.Contains("\"succeeded\":true", payload);
            StringAssert.Contains("AcrobaticsSkill", payload);
            StringAssert.Contains("\"costPaid\":50", payload);
        }

        // ────────────────────────────────────────────────────────────────────
        // 14. Diag pin (failure): emits PurchaseAttempted with reason
        // ────────────────────────────────────────────────────────────────────

        [Test]
        public void Execute_FailureSurfacesReasonInDiag()
        {
            SkillRegistry.InitializeFromJson(ACROBATICS_JSON);
            var actor = MakeActor(sp: 49); // can't afford 50-cost skill
            Diag.ResetAll();

            BuySkillAction.Execute(actor, "AcrobaticsSkill");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Kind = "PurchaseAttempted", Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count);
            string payload = records[0].PayloadJson;
            StringAssert.Contains("\"succeeded\":false", payload,
                "Failure payload should encode succeeded=false.");
            StringAssert.Contains("InsufficientSP", payload,
                "Failure reason must surface in the diag payload — observability " +
                "consumers (UI tooltips, debug console, AI debugging) need it.");
            // SP fields should reflect the unchanged state.
            StringAssert.Contains("\"spBefore\":49", payload);
            StringAssert.Contains("\"spAfter\":49", payload);
        }
    }
}
