using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ST.7a — SkillsScreenStateBuilder tests. Pins the Build contract:
    /// snapshot reflects current actor state (SP, owned skills, stat
    /// values), correctly classifies each row (Owned / Buyable /
    /// InsufficientSP / RequirementsNotMet), populates parent back-refs
    /// for power rows, and renders <c>"???"</c> for FLAG_OBFUSCATED
    /// entries with unmet requirements.
    /// </summary>
    public class SkillsScreenStateBuilderTests
    {
        [SetUp]
        public void Setup()
        {
            SkillRegistry.ResetForTests();
        }

        private static Entity MakeActor(int sp = 100, int agility = 18)
        {
            var e = new Entity { ID = "actor", BlueprintName = "TestActor" };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.Statistics["SP"] = new Stat
            { Owner = e, Name = "SP", BaseValue = sp, Min = 0, Max = 999 };
            e.Statistics["Agility"] = new Stat
            { Owner = e, Name = "Agility", BaseValue = agility, Min = 1, Max = 30 };
            e.AddPart(new SkillsPart());
            return e;
        }

        // ====================================================================
        // 1. Empty registry → empty snapshot (no skills loaded)
        // ====================================================================

        [Test]
        public void Build_EmptyRegistry_ReturnsEmptySnapshot()
        {
            var actor = MakeActor(sp: 50);
            // Mark registry as initialized-but-empty. Without this,
            // SkillRegistry.GetAllSkills() triggers EnsureInitialized which
            // lazy-loads Acrobatics.json from Resources (production content),
            // so the test would see 2 rows instead of 0. Calling
            // InitializeFromJson("") clears state + sets _initialized=true,
            // and LoadFromJson short-circuits on empty input.
            SkillRegistry.InitializeFromJson("");

            var snapshot = SkillsScreenStateBuilder.Build(actor);

            Assert.AreEqual(0, snapshot.RowCount, "Empty registry → no rows.");
            Assert.AreEqual(50, snapshot.CurrentSP, "Snapshot still tracks current SP.");
        }

        // ====================================================================
        // 2. One tree, one power → 2 rows (tree-root + power), correctly
        //    structured
        // ====================================================================

        [Test]
        public void Build_OneTreeOnePower_EmitsTreeRootAndPowerRows()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50,
                ""Description"":""Athletic finesse."",
                ""Powers"":[
                    {""Name"":""Dodge"",""Class"":""AcrobaticsDodgePower"",
                     ""Cost"":30,""Attribute"":""Agility"",""Minimum"":""15""}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 100);

            var snapshot = SkillsScreenStateBuilder.Build(actor);

            Assert.AreEqual(2, snapshot.RowCount, "One tree + one power = 2 rows.");

            var treeRow = snapshot.Rows[0];
            Assert.IsTrue(treeRow.IsTreeRoot, "First row should be the tree-root.");
            Assert.AreEqual("Acrobatics", treeRow.DisplayName);
            Assert.AreEqual("AcrobaticsSkill", treeRow.Class);
            Assert.AreEqual(string.Empty, treeRow.ParentSkillName,
                "Tree-root rows have empty ParentSkillName.");

            var powerRow = snapshot.Rows[1];
            Assert.IsFalse(powerRow.IsTreeRoot, "Second row should be a power.");
            Assert.AreEqual("Dodge", powerRow.DisplayName);
            Assert.AreEqual("AcrobaticsDodgePower", powerRow.Class);
            Assert.AreEqual("Acrobatics", powerRow.ParentSkillName,
                "Power rows carry the parent tree's display name for UI grouping.");
        }

        // ====================================================================
        // 3. Owned skill → State == Owned
        // ====================================================================

        [Test]
        public void Build_OwnedSkill_RowStateOwned()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 100);
            actor.GetPart<SkillsPart>().AddSkill(new AcrobaticsSkill());

            var snapshot = SkillsScreenStateBuilder.Build(actor);

            Assert.AreEqual(1, snapshot.RowCount);
            Assert.AreEqual(SkillsScreenRowState.Owned, snapshot.Rows[0].State,
                "Owned skill must report State=Owned regardless of cost / SP.");
            Assert.IsFalse(snapshot.Rows[0].IsObfuscated,
                "Owned skills must never render obfuscated — Obfuscated is " +
                "the not-yet-met-requirements display, mutually exclusive " +
                "with Owned. Counter-check guards against an EvaluateRowState " +
                "bug that returned (Owned, true) for FLAG_OBFUSCATED skills.");
        }

        // ====================================================================
        // 4. All gates pass + cost <= SP → State == Buyable
        // ====================================================================

        [Test]
        public void Build_AffordableUnowned_RowStateBuyable()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 100);

            var snapshot = SkillsScreenStateBuilder.Build(actor);

            Assert.AreEqual(SkillsScreenRowState.Buyable, snapshot.Rows[0].State,
                "Affordable + unowned + no requirements = Buyable.");
        }

        // ====================================================================
        // 5. Requirements met but cost > SP → State == InsufficientSP
        // ====================================================================

        [Test]
        public void Build_InsufficientSP_RowStateInsufficientSP()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 49);

            var snapshot = SkillsScreenStateBuilder.Build(actor);

            Assert.AreEqual(SkillsScreenRowState.InsufficientSP, snapshot.Rows[0].State,
                "Cost 50 > SP 49 → InsufficientSP. Distinguishes from RequirementsNotMet " +
                "so the UI can show the player they qualify but need more SP.");
        }

        // ====================================================================
        // 6. Stat min not met → State == RequirementsNotMet
        // ====================================================================

        [Test]
        public void Build_StatMinNotMet_RowStateRequirementsNotMet()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50,
                ""Powers"":[
                    {""Name"":""Dodge"",""Class"":""AcrobaticsDodgePower"",
                     ""Cost"":30,""Attribute"":""Agility"",""Minimum"":""15""}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);
            var actor = MakeActor(sp: 100, agility: 14);  // below threshold

            var snapshot = SkillsScreenStateBuilder.Build(actor);

            Assert.AreEqual(2, snapshot.RowCount);
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet, snapshot.Rows[1].State,
                "Power's Agility >= 15 not met (actor has 14) → RequirementsNotMet, " +
                "even though SP is plenty.");
            Assert.IsFalse(snapshot.Rows[1].IsObfuscated,
                "RequirementsNotMet WITHOUT FLAG_OBFUSCATED must keep " +
                "IsObfuscated false. Counter-check guards against an " +
                "EvaluateRowState bug where the obfuscatedFlag is " +
                "miscomputed for non-flagged entries.");
        }

        // ====================================================================
        // 7. FLAG_OBFUSCATED + requirements not met → name renders as "???"
        //    + counter-check: same skill with requirements MET shows real name
        // ====================================================================

        [Test]
        public void Build_FlagObfuscated_RendersQuestionMarksWhenUnmet_RealNameWhenMet()
        {
            // Flags = 2 = FLAG_OBFUSCATED. Power requires Agility >= 18.
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50,
                ""Powers"":[
                    {""Name"":""SecretMove"",""Class"":""SecretMovePower"",
                     ""Cost"":30,""Attribute"":""Agility"",""Minimum"":""18"",
                     ""Flags"":2}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);

            // Case 1: requirements not met (Agility 14 < 18). DisplayName
            // hides the secret-move name behind ???.
            var actorBelow = MakeActor(sp: 100, agility: 14);
            var snapshotBelow = SkillsScreenStateBuilder.Build(actorBelow);
            Assert.AreEqual("???", snapshotBelow.Rows[1].DisplayName,
                "FLAG_OBFUSCATED + unmet requirements → DisplayName == '???'");
            Assert.IsTrue(snapshotBelow.Rows[1].IsObfuscated,
                "IsObfuscated flag must be true when name is hidden.");

            // Case 2: counter-check — requirements met (Agility 18 >= 18).
            // Real name now visible. (InitializeFromJson resets internally
            // so an explicit ResetForTests() here would be redundant.)
            SkillRegistry.InitializeFromJson(json);
            var actorAtThreshold = MakeActor(sp: 100, agility: 18);
            var snapshotAt = SkillsScreenStateBuilder.Build(actorAtThreshold);
            Assert.AreEqual("SecretMove", snapshotAt.Rows[1].DisplayName,
                "Once requirements are met, real name shows even with FLAG_OBFUSCATED set.");
            Assert.IsFalse(snapshotAt.Rows[1].IsObfuscated,
                "IsObfuscated flag flips back to false once requirements are met.");
        }

        // ====================================================================
        // 8. Power's Requires not owned → State == RequirementsNotMet
        //    + counter-check: same setup with prereq added → State == Buyable
        //
        // Pins the Requires gating-helper behavior. Without these tests, a
        // bug that hard-coded MeetsRequires => true would pass tests 1-7
        // and silently desync the UI from BuySkillAction's purchase
        // semantics (UI shows "Buyable", purchase fails with MissingPrereq).
        // ====================================================================

        [Test]
        public void Build_PrereqMissing_RowStateRequirementsNotMet()
        {
            // Acrobatics (Cost=50) + a power that requires "AcrobaticsSkill".
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50,
                ""Powers"":[
                    {""Name"":""GatedDodge"",""Class"":""AcrobaticsDodgePower"",
                     ""Cost"":30,""Requires"":""AcrobaticsSkill""}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);

            // Case 1: prereq NOT owned. SP plenty, no stat min, no exclusion —
            // the only possible blocker is the Requires gate.
            var actorMissing = MakeActor(sp: 100);
            var snapshotMissing = SkillsScreenStateBuilder.Build(actorMissing);
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet,
                snapshotMissing.Rows[1].State,
                "Power requires AcrobaticsSkill, actor doesn't own it → RequirementsNotMet.");

            // Case 2 (counter-check): prereq owned. Same actor + same registry,
            // skill manually added → Requires gate now passes → Buyable.
            var actorOwning = MakeActor(sp: 100);
            actorOwning.GetPart<SkillsPart>().AddSkill(new AcrobaticsSkill());
            var snapshotOwning = SkillsScreenStateBuilder.Build(actorOwning);
            Assert.AreEqual(SkillsScreenRowState.Buyable,
                snapshotOwning.Rows[1].State,
                "Same setup with the required AcrobaticsSkill owned → Buyable. " +
                "Counter-check pins that the gate is the Requires field, not " +
                "an unrelated state.");
        }

        // ====================================================================
        // 9. Power's Exclusion is owned → State == RequirementsNotMet
        //    + counter-check: same setup without exclusion owned → State == Buyable
        // ====================================================================

        [Test]
        public void Build_ExclusionOwned_RowStateRequirementsNotMet()
        {
            // Power has Exclusion="AcrobaticsSkill". Actor owning Acrobatics
            // makes the exclusion fire. (Pretend "GatedDodge" is mutually
            // exclusive with the Acrobatics tree — semantically odd but
            // exercises the field correctly.)
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":50,
                ""Powers"":[
                    {""Name"":""GatedDodge"",""Class"":""AcrobaticsDodgePower"",
                     ""Cost"":30,""Exclusion"":""AcrobaticsSkill""}
                ]
            }]}";

            // Case 1: exclusion-skill IS owned → blocked.
            SkillRegistry.InitializeFromJson(json);
            var actorOwningBlocker = MakeActor(sp: 100);
            actorOwningBlocker.GetPart<SkillsPart>().AddSkill(new AcrobaticsSkill());
            var snapshotBlocked = SkillsScreenStateBuilder.Build(actorOwningBlocker);
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet,
                snapshotBlocked.Rows[1].State,
                "Power's Exclusion is owned → RequirementsNotMet. " +
                "Even though SP is plenty and no other gate blocks, the " +
                "presence of the exclusion-skill in the actor's inventory " +
                "fires the !HasAnyExclusion check.");

            // Case 2 (counter-check): exclusion-skill NOT owned → unblocked.
            SkillRegistry.InitializeFromJson(json);
            var actorClean = MakeActor(sp: 100);
            var snapshotUnblocked = SkillsScreenStateBuilder.Build(actorClean);
            Assert.AreEqual(SkillsScreenRowState.Buyable,
                snapshotUnblocked.Rows[1].State,
                "Same setup with the exclusion-skill NOT owned → Buyable. " +
                "Counter-check pins that the gate is the Exclusion field; a " +
                "bug that hard-coded HasAnyExclusion => true would fail this.");
        }

        // ====================================================================
        // 10. Null actor → empty snapshot (defensive)
        // ====================================================================

        [Test]
        public void Build_NullActor_ReturnsEmptySnapshot()
        {
            // Counter-check: no NRE on null. Loaders / save-load might
            // pass null transiently.
            var snapshot = SkillsScreenStateBuilder.Build(null);
            Assert.AreEqual(0, snapshot.RowCount);
            Assert.AreEqual(0, snapshot.CurrentSP);
        }
    }
}
