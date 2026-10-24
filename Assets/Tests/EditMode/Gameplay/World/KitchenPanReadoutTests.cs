using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class KitchenPanReadoutTests
    {
        const string BrokenName = "split clay batch pan", SoundName = "clay batch pan";
        const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic;
        DensityLootTestScope content;
        Zone zone;
        Entity pan, player;
        string authoredName, authoredText;
        IDictionary recipes;
        DictionaryEntry[] oldRecipes;
        IList errors;
        object[] oldErrors;
        bool oldLoaded;

        [SetUp] public void Setup()
        {
            recipes = (IDictionary)typeof(RepairRecipeRegistry).GetField("Recipes", S).GetValue(null);
            oldRecipes = new DictionaryEntry[recipes.Count]; recipes.CopyTo(oldRecipes, 0);
            errors = (IList)typeof(RepairRecipeRegistry).GetField("Errors", S).GetValue(null);
            oldErrors = new object[errors.Count]; errors.CopyTo(oldErrors, 0);
            oldLoaded = (bool)typeof(RepairRecipeRegistry).GetField("_loaded", S).GetValue(null);
            RepairRecipeRegistry.ResetForTests(); RepairRecipeRegistry.LoadDefaults();
            content = new DensityLootTestScope();
            zone = new Zone("pan-readout"); player = content.Factory.CreateEntity("Player");
            pan = content.Factory.CreateEntity("ConnectedBatchPan");
            Assert.True(zone.AddEntity(player, 4, 4)); Assert.True(zone.AddEntity(pan, 5, 4));
            zone.GetCell(5, 4).Explored = zone.GetCell(5, 4).IsVisible = true;
            authoredName = pan.GetPart<RenderPart>().DisplayName; authoredText = pan.GetPart<ExaminablePart>().Text;
            Assert.AreEqual(BrokenName, authoredName); StringAssert.Contains("Repair the split", authoredText);
            Assert.False(pan.GetPart<KitchenBatchPart>().Configured, "Text must not need a generated service binding.");
        }
        [TearDown] public void Cleanup()
        {
            content?.Dispose();
            recipes.Clear(); foreach (var entry in oldRecipes) recipes.Add(entry.Key, entry.Value);
            errors.Clear(); foreach (var entry in oldErrors) errors.Add(entry);
            typeof(RepairRecipeRegistry).GetField("_loaded", S).SetValue(null, oldLoaded);
        }
        AnnouncementDocument Document(Entity owner = null)
        {
            var result = ExamineDocumentBuilder.BuildWorld(owner ?? pan, zone, zone.GetCell(5, 4), player);
            Assert.NotNull(result); return result;
        }
        static string Description(AnnouncementDocument document) => document.Sections.Single(s => s.Heading == "Description").Text;
        void AssertSound(Entity owner = null)
        {
            owner = owner ?? pan;
            Assert.AreEqual(SoundName, owner.GetDisplayName());
            var examine = owner.GetPart<ExaminablePart>();
            foreach (string text in new[] { examine.BuildExamineLine(), examine.BuildWorldExamineLine(zone, zone.GetCell(5, 4), player), Description(Document(owner)) })
            {
                StringAssert.DoesNotContain("Repair the split", text);
                StringAssert.DoesNotContain(BrokenName, text);
                StringAssert.Contains("two emberwheat, one claspbean pulp and two drams", text);
                StringAssert.Contains("120 world ticks", text);
                StringAssert.Contains("fee is kept", text);
            }
            Assert.AreEqual(SoundName, Document(owner).Title);
        }
        void AssertRawUnchanged(Entity owner = null)
        {
            owner = owner ?? pan;
            Assert.AreEqual(authoredName, owner.GetPart<RenderPart>().DisplayName);
            Assert.AreEqual(authoredText, owner.GetPart<ExaminablePart>().Text);
            Assert.AreEqual(authoredText, owner.GetPart<ExaminablePart>().Description);
            Assert.AreEqual("ConnectedBatchPan", owner.BlueprintName);
        }
        [Test] public void RealRepairUpdatesNameFreshFocusAndBothReadersWithoutRewritingAuthoredFields()
        {
            Assert.AreEqual(BrokenName, pan.GetDisplayName());
            Assert.AreEqual("[5,4] " + BrokenName, LookQueryService.BuildSnapshot(player, zone, 5, 4).Header);
            StringAssert.Contains("Repair the split", Description(Document()));
            var clay = content.Factory.CreateEntity("FireClay"); clay.GetPart<StackerPart>().StackCount = 2;
            player.GetPart<InventoryPart>().MaxWeight = 200; Assert.True(player.GetPart<InventoryPart>().AddObject(clay));
            var repair = pan.GetPart<RepairablePart>(); Assert.True(repair.TryRepair(player, zone));
            Assert.True(repair.Repaired); Assert.False(player.GetPart<InventoryPart>().Objects.Contains(clay));
            AssertSound(); AssertRawUnchanged();
            Assert.AreEqual("[5,4] " + SoundName, LookQueryService.BuildSnapshot(player, zone, 5, 4).Header);
            StringAssert.Contains(RepairRecipeRegistry.Get("clay-batch-pan").RepairedText, Document().ToPlainText());
            Assert.False(repair.TryRepair(player, zone)); AssertSound();
        }
        [TestCase("unrepaired")] [TestCase("foreign-fault")] [TestCase("portable")]
        [TestCase("wrong-recipe")] [TestCase("duplicate-fault")] [TestCase("foreign-batch")]
        [TestCase("other-blueprint")]
        public void OnlyTheActualUnambiguousRepairedPanProjectsTheCanonicalCopy(string fault)
        {
            var repair = pan.GetPart<RepairablePart>(); repair.Repaired = true; AssertSound();
            if (fault == "unrepaired") repair.Repaired = false;
            if (fault == "foreign-fault") repair.ParentEntity = new Entity();
            if (fault == "portable") repair.PortableEquipment = true;
            if (fault == "wrong-recipe") repair.RecipeId = "clay-well-lining";
            if (fault == "duplicate-fault") pan.AddPart(new RepairablePart { Repaired = true, RecipeId = "clay-batch-pan" });
            if (fault == "foreign-batch") pan.GetPart<KitchenBatchPart>().ParentEntity = new Entity();
            if (fault == "other-blueprint") pan.BlueprintName = "AnotherPan";
            Assert.AreEqual(BrokenName, pan.GetDisplayName());
            StringAssert.Contains("Repair the split", pan.GetPart<ExaminablePart>().BuildExamineLine());
            Assert.AreEqual(authoredText, Description(Document()));
        }
        [Test] public void CustomNamesAndProseRemainUntouchedAndStackSuffixIsAppliedOnce()
        {
            pan.GetPart<RepairablePart>().Repaired = true; AssertSound();
            var render = pan.GetPart<RenderPart>(); var examine = pan.GetPart<ExaminablePart>();
            render.DisplayName = "Orven's split heirloom"; examine.Text = "Custom note: repair the split only if asked.";
            Assert.AreEqual(render.DisplayName, pan.GetDisplayName()); Assert.AreEqual(examine.Text, Description(Document()));
            StringAssert.Contains(examine.Text, examine.BuildExamineLine());
            render.DisplayName = authoredName; examine.Text = authoredText;
            pan.AddPart(new StackerPart { StackCount = 2 });
            Assert.AreEqual(SoundName + " (x2)", pan.GetDisplayName()); AssertRawUnchanged();
        }
        [Test] public void AlreadyRepairedSaveRoundTripUsesCurrentCopyWithTheOriginalRawFields()
        {
            pan.GetPart<RepairablePart>().Repaired = true;
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(pan); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, null); var restored = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.True(restored.GetPart<RepairablePart>().Repaired); AssertRawUnchanged(restored);
                Assert.True(zone.RemoveEntity(pan)); Assert.True(zone.AddEntity(restored, 5, 4));
                AssertSound(restored); AssertRawUnchanged(restored);
            }
        }
        [Test] public void ReadoutNeverReconcilesOrConfiguresTheUnboundJob()
        {
            pan.GetPart<RepairablePart>().Repaired = true; var batch = pan.GetPart<KitchenBatchPart>();
            batch.State = "Working"; batch.StartTick = 7; batch.DueTick = 127; batch.JobOrdinal = 3;
            var inputs = batch.Inputs; var ids = batch.InputIDs; int tick = WorldClock.CurrentTick;
            for (int i = 0; i < 3; i++) { AssertSound(); AssertRawUnchanged(); }
            Assert.False(batch.Configured); Assert.AreEqual("Working", batch.State);
            Assert.AreEqual(7, batch.StartTick); Assert.AreEqual(127, batch.DueTick); Assert.AreEqual(3, batch.JobOrdinal);
            Assert.AreSame(inputs, batch.Inputs); Assert.AreSame(ids, batch.InputIDs); Assert.IsEmpty(batch.Inputs);
            Assert.IsNull(batch.Output); Assert.AreEqual(tick, WorldClock.CurrentTick);
        }
    }
}
