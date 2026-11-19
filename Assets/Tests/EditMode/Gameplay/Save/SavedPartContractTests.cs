using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Saved-Part contract (Docs/MACHINE-ARCHITECTURE-SPEC.md C1/S1). The golden file records every Part and its saved fields at the
    /// time it was generated. A Part or field that disappears, moves namespace, changes type or flips between reflected and custom
    /// serialization is a failing test, because saves persist type names and reflect public fields by name.
    /// Regenerate with the [Explicit] GenerateSavedPartContract test only for an ADDITION; regenerating to hide a removal needs the owner's approval.
    /// </summary>
    public sealed class SavedPartContractTests
    {
        static string GoldenPath => Path.Combine(Application.dataPath, "Tests/EditMode/Gameplay/Save/SavedPartContract.txt");

        [Test] public void GoldenFileExists() => Assert.True(File.Exists(GoldenPath), "run the [Explicit] GenerateSavedPartContract test once");

        [Test] public void EveryGoldenPartAndFieldStillExistsUnchanged()
        {
            var violations = SavedPartContractReader.Compare(File.ReadAllText(GoldenPath), SavedPartContractReader.Snapshot());
            Assert.IsEmpty(violations, string.Join("\n", violations));
        }

        [Test] public void HeaderCountMatchesBlocks()
        {
            var text = File.ReadAllText(GoldenPath); var lines = text.Split('\n');
            Assert.That(lines[0], Does.StartWith("# parts="));
            Assert.AreEqual(int.Parse(lines[0].Substring("# parts=".Length)), lines.Count(l => l.StartsWith("Part ", StringComparison.Ordinal)), "a truncated golden hides removals");
        }

        [Test] public void CustomSerializedPartsAreListedWithoutFields()
        {
            var text = File.ReadAllText(GoldenPath);
            foreach (var name in new[] { "CavesOfOoo.Core.InventoryPart", "CavesOfOoo.Core.StatusEffectsPart", "CavesOfOoo.Core.Body" })
                Assert.That(text, Does.Contain("Part " + name + " | custom"), name);
            Assert.That(text, Does.Contain("Part CavesOfOoo.Core.RenderPart | reflected"));
        }

        [Test] public void EveryPartAddedSinceTheGoldenPassesHygiene()
        {
            var golden = File.ReadAllText(GoldenPath);
            var added = SavedPartContractReader.AllParts().Where(t => !golden.Contains("Part " + t.FullName + " | ")).ToList();
            foreach (var type in added)
                Assert.IsEmpty(SavedPartContractReader.Hygiene(type), "new Part " + type.FullName + " breaks the hygiene rules");
        }

        // ── the checker is not vacuous ──
        const string G = "# parts=2\nPart A.Foo | reflected\n  Count:System.Int32\n  Name:System.String\nPart A.Bar | custom\n";

        [Test] public void Compare_Identical_NoViolation() => Assert.IsEmpty(SavedPartContractReader.Compare(G, G));
        [Test] public void Compare_MissingPart_IsViolation() => Assert.That(SavedPartContractReader.Compare(G, "# parts=1\nPart A.Bar | custom\n"), Has.Some.Contains("missing Part A.Foo"));
        [Test] public void Compare_MissingField_IsViolation() => Assert.That(SavedPartContractReader.Compare(G, G.Replace("  Count:System.Int32\n", "")), Has.Some.Contains("missing field A.Foo.Count"));
        [Test] public void Compare_RetypedField_IsViolation() => Assert.That(SavedPartContractReader.Compare(G, G.Replace("System.Int32", "System.Int64")), Has.Some.Contains("retyped field A.Foo.Count"));
        [Test] public void Compare_MovedNamespace_IsViolation() => Assert.That(SavedPartContractReader.Compare(G, G.Replace("A.Foo", "B.Foo")), Has.Some.Contains("missing Part A.Foo"));
        [Test] public void Compare_ReflectedToCustomFlip_IsViolation() => Assert.That(SavedPartContractReader.Compare(G, G.Replace("Part A.Foo | reflected", "Part A.Foo | custom")), Has.Some.Contains("serialization kind changed"));
        const string N = "# parts=1\nPart A.Foo | reflected\n  #hooks:OnBeforeSave\n  Entries:System.Collections.Generic.List`1[A.Entry]\n  Entries[].Count:System.Int32\n  Entries[].Id:System.String\n  Kind:A.Kind\n  Kind#enum:First=0,Second=1\n";
        [Test] public void Compare_NestedIdentical_NoViolation() => Assert.IsEmpty(SavedPartContractReader.Compare(N, N));
        [Test] public void Compare_NestedFieldRemoved_IsViolation() => Assert.That(SavedPartContractReader.Compare(N, N.Replace("  Entries[].Count:System.Int32\n", "")), Has.Some.Contains("missing field A.Foo.Entries[].Count"));
        [Test] public void Compare_NestedFieldRetyped_IsViolation() => Assert.That(SavedPartContractReader.Compare(N, N.Replace("Entries[].Count:System.Int32", "Entries[].Count:System.Int64")), Has.Some.Contains("retyped field A.Foo.Entries[].Count"));
        [Test] public void Compare_NestedFieldAdded_IsNotViolation() => Assert.IsEmpty(SavedPartContractReader.Compare(N, N + "  Entries[].Extra:System.Int32\n"));
        [Test] public void Compare_EnumMemberRenumbered_IsViolation() => Assert.That(SavedPartContractReader.Compare(N, N.Replace("First=0,Second=1", "First=1,Second=0")), Has.Some.Contains("enum member renumbered"));
        [Test] public void Compare_EnumMemberRemoved_IsViolation() => Assert.That(SavedPartContractReader.Compare(N, N.Replace("First=0,Second=1", "First=0")), Has.Some.Contains("enum member removed"));
        [Test] public void Compare_EnumMemberAppended_IsNotViolation() => Assert.IsEmpty(SavedPartContractReader.Compare(N, N.Replace("First=0,Second=1", "First=0,Second=1,Third=2")));
        [Test] public void Compare_HookAdded_IsViolation() => Assert.That(SavedPartContractReader.Compare(N.Replace("  #hooks:OnBeforeSave\n", ""), N), Has.Some.Contains("save hooks changed"));
        [Test] public void Compare_HookRemoved_IsViolation() => Assert.That(SavedPartContractReader.Compare(N, N.Replace("  #hooks:OnBeforeSave\n", "")), Has.Some.Contains("save hooks changed"));
        [Test] public void Snapshot_ExpandsNestedPayloadsEnumsAndHooks()
        {
            var text = SavedPartContractReader.Snapshot();
            Assert.That(text, Does.Contain("#enum:"), "at least one enum field in the game's Parts"); Assert.That(text, Does.Contain("  #hooks:"), "SpatialFootprintPart and AIAmbushPart override the save hooks");
            Assert.That(text, Does.Contain("[]."), "at least one list of payload classes is expanded");
        }

        [Test] public void Compare_AddedPartAndFieldAreNotViolations() => Assert.IsEmpty(SavedPartContractReader.Compare(G, G + "Part A.New | reflected\n  X:System.Int32\n"));

        // ── hygiene lint, on synthetic types ──
        public sealed class Conforming : Part { public override string Name => "C"; public int Count; public string Id = ""; }
        public sealed class WithZoneId : Part { public override string Name => "Z"; public string ZoneID = ""; }
        public sealed class WithStationX : Part { public override string Name => "X"; public int StationX; }
        public sealed class WithReceiver : Part { public override string Name => "R"; public string OutputReceiverID = ""; }
        public sealed class NoCtor : Part { public override string Name => "N"; public NoCtor(int x) { } }
        public sealed class Unserializable : Part { public override string Name => "U"; public System.Action Callback; }

        [Test] public void Hygiene_ConformingPart_NoViolation() => Assert.IsEmpty(SavedPartContractReader.Hygiene(typeof(Conforming)).Where(p => !p.StartsWith("namespace ")));
        [Test] public void Hygiene_WrongNamespace_Violation() => Assert.That(SavedPartContractReader.Hygiene(typeof(Conforming)), Has.Some.StartsWith("namespace "));
        [Test] public void Hygiene_PositionalAuthorityField_Violation()
        {
            foreach (var t in new[] { typeof(WithZoneId), typeof(WithStationX), typeof(WithReceiver) })
                Assert.That(SavedPartContractReader.Hygiene(t), Has.Some.StartsWith("positional authority"), t.Name);
        }
        [Test] public void Hygiene_NoParameterlessCtor_Violation() => Assert.That(SavedPartContractReader.Hygiene(typeof(NoCtor)), Has.Some.Contains("parameterless"));
        [Test] public void Hygiene_UnserializablePublicField_Violation() => Assert.That(SavedPartContractReader.Hygiene(typeof(Unserializable)), Has.Some.Contains("not serializable"));

        /// <summary>Unity's batch filter selects [Explicit] tests when their class is named, so a plain attribute is not a safe guard:
        /// this only writes when COO_REGENERATE_SAVED_PART_CONTRACT=1 is set in the environment, and is otherwise ignored.</summary>
        [Test, Explicit("Regenerates SavedPartContract.txt; only for additions.")]
        public void GenerateSavedPartContract()
        {
            if (Environment.GetEnvironmentVariable("COO_REGENERATE_SAVED_PART_CONTRACT") != "1") Assert.Ignore("set COO_REGENERATE_SAVED_PART_CONTRACT=1 to regenerate");
            File.WriteAllText(GoldenPath, SavedPartContractReader.Snapshot());
        }
    }
}
