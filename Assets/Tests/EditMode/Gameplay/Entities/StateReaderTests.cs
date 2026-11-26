using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Reader contract (Docs/MACHINE-READERS.md, S5): both the quick Examine line and the full Examine document ask one collector for the lines their Parts contribute, so a new Part
    /// appears in both readers without editing either builder. The shipped providers keep their text and order exactly; two of them (the film and the contained-liquid source) stay
    /// quick-reader only, as before, until the owner decides the full reader should gain them.
    /// </summary>
    public sealed class StateReaderTests
    {
        HaulingContentScope scope; Zone zone; Entity viewer;
        [SetUp] public void Setup() { scope = new HaulingContentScope(); scope.Seed(64); Diag.ResetAll(); zone = new Zone("Overworld.1.1.0"); viewer = scope.Factory.CreateEntity("Player"); zone.AddEntity(viewer, 10, 10); }
        [TearDown] public void Cleanup() => scope.Dispose();

        sealed class Line : Part, IDescribeState
        {
            public override string Name => "TestLine" + Id; public string Id = "";
            public string Text = ""; public int OrderValue = 50; public ReaderScope ScopeValue = ReaderScope.Both; public bool Throws; public int Calls;
            public ReaderScope Scope => ScopeValue; public int Order => OrderValue;
            public string DescribeState(DescribeContext context) { Calls++; if (Throws) throw new InvalidOperationException("boom"); return Text; }
        }
        Line Add(Entity owner, string id, string text, int order = 50, ReaderScope scopeValue = ReaderScope.Both) { var l = new Line { Id = id, Text = text, OrderValue = order, ScopeValue = scopeValue }; owner.AddPart(l); return l; }
        List<string> Collect(Entity owner, bool full) { var into = new List<string>(); StateReader.Collect(owner, full, zone, zone.GetEntityCell(owner), viewer, into); return into; }
        Entity Subject(string blueprint = "Crate")  // a neutral object: a forge now describes its own cold work, which these generic collector tests must not depend on
         { var e = scope.Factory.CreateEntity(blueprint); Assert.True(zone.AddEntity(e, 11, 10)); Seen(e); return e; }
        void Seen(Entity e) { var c = zone.GetEntityCell(e); c.IsVisible = true; c.Explored = true; }

        // ── the collector ──

        [Test] public void LinesAreOrderedByPartOrderThenInsertion()
        {
            var e = Subject(); Add(e, "c", "third", 30); Add(e, "a", "first", 10); Add(e, "b1", "second", 20); Add(e, "b2", "also second", 20);
            CollectionAssert.AreEqual(new[] { "first", "second", "also second", "third" }, Collect(e, false));
        }

        [Test] public void ScopeFiltersTheQuickAndFullReaders()
        {
            var e = Subject(); Add(e, "q", "quick only", 10, ReaderScope.Quick); Add(e, "f", "full only", 20, ReaderScope.Full); Add(e, "b", "both", 30);
            CollectionAssert.AreEqual(new[] { "quick only", "both" }, Collect(e, false)); CollectionAssert.AreEqual(new[] { "full only", "both" }, Collect(e, true));
        }

        [Test] public void EmptyNullAndDuplicateTextIsDropped()
        {
            var e = Subject(); Add(e, "a", "same"); Add(e, "b", "same"); Add(e, "c", ""); Add(e, "d", null); Add(e, "e", "   "); Add(e, "f", "different");
            CollectionAssert.AreEqual(new[] { "same", "different" }, Collect(e, false));
        }

        [Test] public void AThrowingDescriberIsIsolatedAndTheOthersStillRender()
        {
            var e = Subject(); var bad = Add(e, "bad", "never", 10); bad.Throws = true; Add(e, "good", "still here", 20);
            CollectionAssert.AreEqual(new[] { "still here" }, Collect(e, false));
            Assert.That(Diag.Snapshot(20).Select(r => r.PayloadJson), Has.Some.Contains("boom"), "the isolated failure is recorded, not swallowed silently");
        }

        [Test] public void DescribingIsReadOnly()
        {
            var e = Subject(); var l = Add(e, "a", "text"); int parts = e.Parts.Count; var effects = e.GetPart<StatusEffectsPart>();
            Collect(e, false); Collect(e, true);
            Assert.AreEqual(parts, e.Parts.Count, "no Part added"); Assert.AreEqual(effects, e.GetPart<StatusEffectsPart>(), "no effects Part created"); Assert.AreEqual(2, l.Calls);
        }

        [Test] public void NullAndEntitiesWithoutDescribersAreSafe()
        {
            var into = new List<string>(); StateReader.Collect(null, false, zone, null, viewer, into); Assert.IsEmpty(into);
            Assert.IsEmpty(Collect(Subject(), false), "a plain forge contributes nothing"); StateReader.Collect(Subject(), false, null, null, null, null);   // a null list is a no-op
        }

        // ── both readers use it ──

        [Test] public void ANewPartAppearsInTheQuickReaderAndTheFullDocumentWithoutEditingEitherBuilder()
        {
            var e = Subject(); Add(e, "x", "A brass tag reads: set here to be used.", 25);
            string quick = e.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, zone.GetEntityCell(e), viewer);
            Assert.That(quick, Does.Contain("A brass tag reads: set here to be used."));
            var doc = ExamineDocumentBuilder.BuildWorld(e, zone, zone.GetEntityCell(e), viewer);
            Assert.That(string.Join("\n", doc.Sections.Select(s => s.Text)), Does.Contain("A brass tag reads: set here to be used."));
        }

        [Test] public void AQuickOnlyLineIsAbsentFromTheFullDocumentAndTheCounterHoldsForBoth()
        {
            var e = Subject(); Add(e, "q", "quick-only marker", 10, ReaderScope.Quick); Add(e, "b", "both-readers marker", 20);
            var doc = ExamineDocumentBuilder.BuildWorld(e, zone, zone.GetEntityCell(e), viewer); string text = string.Join("\n", doc.Sections.Select(s => s.Text));
            Assert.That(text, Does.Not.Contain("quick-only marker")); Assert.That(text, Does.Contain("both-readers marker"));
            Assert.That(e.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, zone.GetEntityCell(e), viewer), Does.Contain("quick-only marker"));
        }

        // ── the shipped providers keep their legacy order and scope ──

        [Test] public void ShippedProvidersKeepTheLegacyOrderAndScope()
        {
            var expected = new (Type type, ReaderScope scope)[]
            {
                (typeof(FiniteLiquidFilmPart), ReaderScope.Quick), (typeof(KitchenBatchPart), ReaderScope.Both), (typeof(SoddenPreparationPart), ReaderScope.Both),
                (typeof(OilStrainingPart), ReaderScope.Both), (typeof(ContainedLiquidSourcePart), ReaderScope.Quick), (typeof(RepairablePart), ReaderScope.Both), (typeof(TrapJammingPart), ReaderScope.Both),
            };
            int last = int.MinValue;
            foreach (var (type, scopeValue) in expected)
            {
                var part = (IDescribeState)Activator.CreateInstance(type);
                Assert.AreEqual(scopeValue, part.Scope, type.Name); Assert.Greater(part.Order, last, type.Name + " keeps the legacy sequence"); last = part.Order;
            }
        }

        [Test] public void ARealRepairLineStillAppearsInBothReaders()
        {
            var rig = scope.Factory.CreateEntity("AbandonedFieldWorkRig"); Assert.True(zone.AddEntity(rig, 11, 10)); Seen(rig);
            var fault = rig.GetPart<RepairablePart>();
            var recipe = RepairRecipeRegistry.Get(fault.RecipeId);
            const string bill = "2 measures of fire clay or prepared lining clay, in any mix";
            string plain = rig.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, zone.GetEntityCell(rig), viewer);
            Assert.That(plain, Does.Contain("NEEDS REPAIR").And.Contain(recipe.Diagnosis).And.Contain(bill));
            var doc = ExamineDocumentBuilder.BuildWorld(rig, zone, zone.GetEntityCell(rig), viewer);
            var warning = doc.Sections.First();
            Assert.AreEqual("Needs repair", warning.Heading);
            Assert.That(warning.Text, Does.Contain(recipe.Diagnosis).And.Contain(bill));
            Assert.That(doc.Facts.Single(fact => fact.Label == "Composition").Value, Does.Contain(recipe.Composition));
        }

        [Test] public void NeitherReaderNamesTheShippedProviderPartsAnyMore()
        {
            string root = UnityEngine.Application.dataPath + "/Scripts/Gameplay/Entities/";
            string quick = System.IO.File.ReadAllText(root + "ExaminablePart.cs"), full = System.IO.File.ReadAllText(root + "ExamineDocumentBuilder.cs");
            foreach (var name in new[] { "KitchenBatchPart", "SoddenPreparationPart", "OilStrainingPart", "ContainedLiquidSourcePart", "FiniteLiquidFilmPart", "RepairablePart", "TrapJammingPart" })
            { string call = "GetPart<" + name + ">()?.Describe"; Assert.That(quick, Does.Not.Contain(call), "ExaminablePart still calls " + name + " directly"); Assert.That(full, Does.Not.Contain(call), "ExamineDocumentBuilder still calls " + name + " directly"); }
            Assert.That(quick, Does.Contain("StateReader.Collect")); Assert.That(full, Does.Contain("StateReader.Collect"));
        }
    }
}
