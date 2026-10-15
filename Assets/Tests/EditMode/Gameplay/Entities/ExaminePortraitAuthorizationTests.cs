using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A captured subject is not permanent permission to show its current image.</summary>
    public sealed class ExaminePortraitAuthorizationTests
    {
        [SetUp] public void Setup()=>RepairRecipeRegistry.ResetForTests();
        [TearDown] public void Cleanup()=>RepairRecipeRegistry.ResetForTests();
        static Entity Subject()
        {
            var item=new Entity { ID=Guid.NewGuid().ToString("N"),BlueprintName="ledger-subject" };item.SetTag("Item");
            item.AddPart(new RenderPart { DisplayName="field object",RenderString="!" });
            item.AddPart(new PhysicsPart { Takeable=true });item.AddPart(new ExaminablePart());return item;
        }
        static Entity Carry(Entity item)
        {
            var viewer=Subject();viewer.AddPart(new InventoryPart());viewer.GetPart<InventoryPart>().Objects.Add(item);
            item.GetPart<PhysicsPart>().InInventory=viewer;return viewer;
        }
        static Zone Place(Entity item)
        {
            var zone=new Zone("portrait-authority");Assert.True(zone.AddEntity(item,10,10));
            foreach(var cell in zone.Cells)cell.IsVisible=cell.Explored=true;return zone;
        }
        static bool Authorized(AnnouncementDocument document)
        {
            var method=typeof(ExamineDocumentBuilder).GetMethod("IsCurrentSubject",BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method,"Portraits need a read-only current-subject guard, retaining original cell and viewer authority.");
            return (bool)method.Invoke(null,new object[] { document });
        }
        [TestCase("transfer")] [TestCase("removed")] [TestCase("hidden")] [TestCase("world")]
        public void InventoryPortraitAuthorityEndsWhenOriginalOwnershipOrVisibilityEnds(string change)
        {
            var item=Subject();var viewer=Carry(item);var document=ExamineDocumentBuilder.BuildInventory(item,viewer);
            Assert.True(Authorized(document));
            if(change=="transfer") { viewer.GetPart<InventoryPart>().Objects.Remove(item);Carry(item); }
            if(change=="removed")viewer.GetPart<InventoryPart>().Objects.Remove(item);
            if(change=="hidden")item.GetPart<RenderPart>().Visible=false;
            if(change=="world")Place(item);
            Assert.False(Authorized(document));
        }
        [TestCase("moved")] [TestCase("fog")] [TestCase("unexplored")]
        [TestCase("removed")] [TestCase("viewer-left")] [TestCase("replaced-cell")]
        public void WorldPortraitAuthorityRetainsTheOriginalVisibleCellAndViewer(string change)
        {
            var item=Subject();var zone=Place(item);var viewer=Subject();Assert.True(zone.AddEntity(viewer,9,10));
            var cell=zone.GetCell(10,10);var document=ExamineDocumentBuilder.BuildWorld(item,zone,cell,viewer);
            Assert.True(Authorized(document));
            if(change=="moved")Assert.True(zone.MoveEntity(item,12,10));
            if(change=="fog")cell.IsVisible=false;
            if(change=="unexplored")cell.Explored=false;
            if(change=="removed")zone.RemoveEntity(item);
            if(change=="viewer-left") { zone.RemoveEntity(viewer);Place(viewer); }
            if(change=="replaced-cell")zone.Cells[10,10]=new Cell(10,10,zone) { IsVisible=true,Explored=true };
            Assert.False(Authorized(document));
        }
        [TestCase(true)] [TestCase(false)]
        public void FootprintAuthorizationUsesCapturedVisibleBodyCell(bool keepVisible)
        {
            var item=Subject();item.AddPart(new SpatialFootprintPart { CellsRaw="0,0;1,0" });var zone=Place(item);
            zone.GetCell(10,10).IsVisible=false;var visible=zone.GetCell(11,10);
            var document=ExamineDocumentBuilder.BuildWorld(item,zone,visible);
            Assert.NotNull(document);Assert.True(Authorized(document));
            visible.IsVisible=keepVisible;Assert.AreEqual(keepVisible,Authorized(document));
        }
        [Test] public void PlainTextAndBareSubjectDocumentsHaveNoPortraitAuthority()
        {
            Assert.False(Authorized(null));Assert.False(Authorized(new AnnouncementDocument("Notice")));
            Assert.False(Authorized(new AnnouncementDocument("Probe",subject:Subject())));
        }
        sealed class CountingDescription : IItemEnhancement
        {
            public int Reads;
            public override string Name=>"LedgerCountingDescription";
            public override string GetEffectDescription() { Reads++;return "An observed enhancement."; }
        }
        [Test] public void PortraitRevalidationDoesNotInvokeDescriptionProvidersOrChangeTheSnapshot()
        {
            var item=Subject();var viewer=Carry(item);var hook=new CountingDescription();item.AddPart(hook);
            var document=ExamineDocumentBuilder.BuildInventory(item,viewer);int reads=hook.Reads;string text=document.ToPlainText();
            Assert.Greater(reads,0);Assert.True(Authorized(document));Assert.True(Authorized(document));
            Assert.AreEqual(reads,hook.Reads);Assert.AreEqual(text,document.ToPlainText());
        }
        [TestCase(false)] [TestCase(true)]
        public void RepairSupplyFactShowsOnlyAnActualUnrepairedMatchingFault(bool repaired)
        {
            var item=Subject();item.GetPart<PhysicsPart>().Takeable=false;
            item.AddPart(new CompositionPart { MaterialsRaw="Masonry" });
            item.AddPart(new RepairablePart { RecipeId="clay-well-lining",Repaired=repaired });var zone=Place(item);
            var document=ExamineDocumentBuilder.BuildWorld(item,zone,zone.GetCell(10,10));
            Assert.AreEqual(repaired?null:"2 fire clay",document.Facts.FirstOrDefault(f=>f.Label=="Repair")?.Value);
        }
        [TestCase("unknown")] [TestCase("wrong-composition")] [TestCase("duplicate")]
        public void MalformedRepairCannotAdvertiseAUsableSupplyRecipe(string fault)
        {
            var item=Subject();item.GetPart<PhysicsPart>().Takeable=false;
            item.AddPart(new CompositionPart { MaterialsRaw=fault=="wrong-composition"?"Iron":"Masonry" });
            item.AddPart(new RepairablePart { RecipeId=fault=="unknown"?"missing-recipe":"clay-well-lining" });
            if(fault=="duplicate")item.AddPart(new RepairablePart { RecipeId="clay-well-lining" });
            var zone=Place(item);var document=ExamineDocumentBuilder.BuildWorld(item,zone,zone.GetCell(10,10));
            Assert.False(document.Facts.Any(f=>f.Label=="Repair"));
        }
    }
}
