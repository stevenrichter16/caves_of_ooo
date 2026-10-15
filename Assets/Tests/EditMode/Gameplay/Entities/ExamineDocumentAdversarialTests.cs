using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ExamineDocumentAdversarialTests
    {
        static Entity Item()
        {
            var item=new Entity { ID=Guid.NewGuid().ToString("N"),BlueprintName="ledger-probe" };item.SetTag("Item");
            item.AddPart(new RenderPart { DisplayName="field probe",RenderString="!",ColorString="&C" });
            item.AddPart(new PhysicsPart { Takeable=true,Weight=3 });item.AddPart(new ExaminablePart { Text="A complete authored observation." });return item;
        }
        static Entity Carry(Entity item)
        {
            var actor=new Entity();actor.AddPart(new InventoryPart());actor.GetPart<InventoryPart>().Objects.Add(item);
            item.GetPart<PhysicsPart>().InInventory=actor;return actor;
        }
        static Zone Place(Entity item)
        {
            var zone=new Zone("ledger-probe");Assert.True(zone.AddEntity(item,10,10));
            foreach(var cell in zone.Cells)cell.IsVisible=cell.Explored=true;return zone;
        }
        sealed class DescribeHook : IItemEnhancement
        {
            public override string Name=>"LedgerDescribeHook";
            public Action Hook;
            public override string GetEffectDescription() { Hook?.Invoke();return "A faithful enhancement note."; }
        }
        [Test] public void DocumentCopiesCollectionsAndRetainsFullLongText()
        {
            string longText=new string('a',5000)+"last word";
            var sections=new List<AnnouncementSection> { new AnnouncementSection("Notes",longText) };
            var facts=new List<AnnouncementFact> { new AnnouncementFact("Material","Iron") };
            var document=new AnnouncementDocument("Probe","EXAMINE",sections,facts);
            sections.Clear();facts.Clear();
            Assert.AreEqual(1,document.Sections.Count);Assert.AreEqual(1,document.Facts.Count);
            StringAssert.Contains(longText,document.ToPlainText());StringAssert.Contains("Material: Iron",document.ToPlainText());
            Assert.Throws<NotSupportedException>(()=>((IList<AnnouncementSection>)document.Sections).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<AnnouncementFact>)document.Facts).Clear());
        }
        [Test] public void OptionalEmptyRowsCollapseWithoutErasingAnUntitledNotice()
        {
            var document=new AnnouncementDocument(null,null,new[] { null,new AnnouncementSection("", ""),new AnnouncementSection(null,"Actual notice.") },
                new[] { null,new AnnouncementFact("Material",""),new AnnouncementFact("","Unlabeled") });
            Assert.AreEqual("NOTICE",document.Kind);Assert.AreEqual("Actual notice.",document.ToPlainText());
            Assert.AreEqual(1,document.Sections.Count);Assert.IsEmpty(document.Facts);Assert.IsNull(document.Glyph);
        }
        [TestCase(true)] [TestCase(false)]
        public void VisibleFootprintCellIdentifiesOnlyItsRealOccupant(bool footprint)
        {
            var item=Item();if(footprint)item.AddPart(new SpatialFootprintPart { CellsRaw="0,0;1,0" });
            var zone=Place(item);zone.GetCell(10,10).IsVisible=false;
            var document=ExamineDocumentBuilder.BuildWorld(item,zone,zone.GetCell(11,10));
            Assert.AreEqual(footprint,document!=null);
        }
        [TestCase(true)] [TestCase(false)]
        public void DescriptorThatHidesSubjectCannotPublishItsSnapshot(bool mutate)
        {
            var item=Item();var zone=Place(item);
            item.AddPart(new DescribeHook { Hook=()=> { if(mutate)item.GetPart<RenderPart>().Visible=false; } });
            Assert.AreEqual(!mutate,ExamineDocumentBuilder.BuildWorld(item,zone,zone.GetCell(10,10))!=null);
        }
        [TestCase(true)] [TestCase(false)]
        public void DescriptorThatTransfersInventoryCannotPublishOldOwnerDetails(bool mutate)
        {
            var item=Item();var actor=Carry(item);
            item.AddPart(new DescribeHook { Hook=()=> { if(mutate)item.GetPart<PhysicsPart>().InInventory=new Entity(); } });
            Assert.AreEqual(!mutate,ExamineDocumentBuilder.BuildInventory(item,actor)!=null);
        }
        [TestCase(true)] [TestCase(false)]
        public void DescriptorRemovingRenderRefusesSnapshotWithoutThrowing(bool world)
        {
            var item=Item();var zone=world?Place(item):null;var actor=world?null:Carry(item);
            item.AddPart(new DescribeHook { Hook=()=>item.RemovePart(item.GetPart<RenderPart>()) });
            AnnouncementDocument document=null;
            Assert.DoesNotThrow(()=>document=world?ExamineDocumentBuilder.BuildWorld(item,zone,zone.GetCell(10,10))
                :ExamineDocumentBuilder.BuildInventory(item,actor));
            Assert.IsNull(document);
        }
        [TestCase(true)] [TestCase(false)]
        public void CarriedExamineProseMustBelongToTheActualInspectedItem(bool foreign)
        {
            var item=Item();var actor=Carry(item);
            if(foreign)item.GetPart<ExaminablePart>().ParentEntity=new Entity();
            Assert.AreEqual(!foreign,ExamineDocumentBuilder.BuildInventory(item,actor)!=null);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(1)]
        public void ExpiredStatusesAreNotAdvertisedAsCurrentButIndefiniteEffectsRemain(int duration)
        {
            var item=Item();var actor=Carry(item);item.AddPart(new StatusEffectsPart());
            var effect=new EnGardeEffect(3);Assert.True(item.GetPart<StatusEffectsPart>().ForceApplyEffect(effect));effect.Duration=duration;
            var document=ExamineDocumentBuilder.BuildInventory(item,actor);
            Assert.AreEqual(duration!=0,document.ToPlainText().Contains("En Garde"));
            Assert.AreEqual(duration,effect.Duration);Assert.False(effect.Spent);
        }
        [TestCase(true)] [TestCase(false)]
        public void EquippedItemRequiresMatchingCacheAndBacklink(bool cached)
        {
            var item=Item();var actor=new Entity();actor.AddPart(new InventoryPart());item.GetPart<PhysicsPart>().Equipped=actor;
            if(cached)actor.GetPart<InventoryPart>().EquippedItems["Hand"]=item;
            Assert.AreEqual(cached,ExamineDocumentBuilder.BuildInventory(item,actor)!=null);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(2)]
        public void EmptyStacksCannotMasqueradeAsCurrentOwnedSubjects(int count)
        {
            var item=Item();var actor=Carry(item);item.AddPart(new StackerPart { StackCount=count });
            var document=ExamineDocumentBuilder.BuildInventory(item,actor);
            Assert.AreEqual(count>0,document!=null);
            if(count>0)Assert.AreEqual("2; 6 lb total",document.Facts.Single(f=>f.Label=="Stack").Value);
        }
        [TestCase("render")] [TestCase("physics")] [TestCase("examine")]
        public void ForeignBacklinksCannotAuthorizeAWorldSubject(string part)
        {
            var item=Item();var zone=Place(item);var other=new Entity();
            Assert.NotNull(ExamineDocumentBuilder.BuildWorld(item,zone,zone.GetCell(10,10)));
            if(part=="render")item.GetPart<RenderPart>().ParentEntity=other;
            if(part=="physics")item.GetPart<PhysicsPart>().ParentEntity=other;
            if(part=="examine")item.GetPart<ExaminablePart>().ParentEntity=other;
            Assert.IsNull(ExamineDocumentBuilder.BuildWorld(item,zone,zone.GetCell(10,10)));
        }
        [Test] public void ReopeningUsesCurrentIdentityWhileOldDocumentRemainsASnapshot()
        {
            var item=Item();var actor=Carry(item);var original=ExamineDocumentBuilder.BuildInventory(item,actor);
            item.GetPart<RenderPart>().DisplayName="renamed probe";item.GetPart<ExaminablePart>().Text="New field observation.";
            var current=ExamineDocumentBuilder.BuildInventory(item,actor);
            Assert.AreEqual("field probe",original.Title);Assert.AreEqual("renamed probe",current.Title);
            StringAssert.Contains("complete authored",original.ToPlainText());StringAssert.DoesNotContain("New field",original.ToPlainText());
            StringAssert.Contains("New field observation.",current.ToPlainText());
        }
    }
}
