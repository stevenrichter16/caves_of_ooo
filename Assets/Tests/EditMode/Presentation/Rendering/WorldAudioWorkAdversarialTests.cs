using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioWorkAdversarialTests : WorldAudioWorkFixture
    {
        [TestCase("repair")][TestCase("field")][TestCase("crop")]
        public void LaterActionFailureRollsBackRealWorkAndNeverPlaysACompletion(string kind)
        {
            Entity target;
            if (kind == "repair") { target = RepairTarget(); PayRepair(target); }
            else target = kind == "field" ? Place("RipeCropRow") : Crop(2);
            FailAfter(); Assert.False(Act(target, kind == "repair" ? RepairablePart.RepairCommand : kind == "field" ? "Harvest" : "HarvestCultivatedCrop"));
            Assert.NotNull(Zone.GetEntityCell(target)); Assert.IsEmpty(Receipts); Assert.IsEmpty(Assigned());
            if (kind == "repair") Assert.False(target.GetPart<RepairablePart>().Repaired);
            if (kind == "field") Assert.False(target.GetPart<FieldHarvestPart>().Harvested);
            if (kind == "crop") { Assert.AreEqual(0, Count("CandyCarrot")); Assert.AreEqual(0, Count("CandyCarrotSeed")); }
        }
        [TestCase("unexplored")][TestCase("hidden-cell")][TestCase("hidden-render")][TestCase("moved-actor")][TestCase("other-zone")][TestCase("muted")]
        public void SuccessfulPhysicalWorkDoesNotGrantUnperceivedSoundAuthority(string hidden)
        {
            var target = RepairTarget(); PayRepair(target);
            if (hidden == "unexplored") Zone.GetCell(11,10).Explored = false;
            if (hidden == "hidden-cell") Zone.GetCell(11,10).IsVisible = false;
            if (hidden == "hidden-render") target.GetPart<RenderPart>().Visible = false;
            if (hidden == "moved-actor") Actor.AddPart(new AfterAction { Run = () => Zone.MoveEntity(Actor,20,10) });
            if (hidden == "other-zone") Router.SetWorld(new Zone("replacement-work-world"), Actor, true);
            if (hidden == "muted") CavesOfOoo.Rendering.WorldAudioSettings.SfxVolume = 0;
            Assert.True(Repair(target)); Assert.True(target.GetPart<RepairablePart>().Repaired);
            Assert.IsEmpty(Assigned()); Assert.AreEqual(0, Audio.AcceptedSoundCount);
        }
        [Test] public void OwnActionMenuMayFinishFeedbackWhileTheWorldIsTemporarilyHidden()
        {
            var target = RepairTarget(); PayRepair(target); Router.SetWorld(Zone, Actor, false);
            Assert.True(Repair(target)); Assert.AreEqual(1, Audio.AcceptedSoundCount);
        }
        [TestCase("hidden")][TestCase("destroyed")][TestCase("portable")][TestCase("removed-after-stage")]
        public void ForgeFurnitureMustBeTheSameLiveVisiblePhysicalWitness(string fault)
        {
            var forge = Place("TinkersForge");
            if (fault == "hidden") forge.GetPart<RenderPart>().Visible = false;
            if (fault == "destroyed")
            { var d = forge.GetPart<DestructiblePart>(); if (d == null) forge.AddPart(new DestructiblePart { HP = 0, Gone = true }); else { d.HP = 0; d.Gone = true; } }
            if (fault == "portable") forge.GetPart<PhysicsPart>().Takeable = true;
            var command = ForgeCommand(); var context = new InventoryContext(Actor, Zone); var tx = new InventoryTransaction();
            Assert.True(command.Validate(context).IsValid); Assert.True(command.Execute(context, tx).Success);
            Assert.IsEmpty(Receipts); if (fault == "removed-after-stage") Assert.True(Zone.RemoveEntity(forge)); tx.Commit();
            Assert.AreEqual(1, command.ForgedWeapons.Count, "audio policy must not change legitimate one-unit crafting");
            Assert.IsEmpty(Assigned()); Assert.IsEmpty(Receipts);
        }
        [TestCase(false)][TestCase(true)]
        public void CultivatedYieldUsesOnlyItsActualTransactionRemoval(bool commit)
        {
            var crop = Crop(2); var tx = new InventoryTransaction(); Stage(Actor,crop,Zone,"HarvestCultivatedCrop",tx);
            Assert.IsNull(Zone.GetEntityCell(crop)); Assert.IsEmpty(Receipts);
            if (commit) tx.Commit(); else tx.Rollback();
            Assert.AreEqual(commit ? 1 : 0, Audio.AcceptedSoundCount);
            Assert.AreEqual(commit, Zone.GetEntityCell(crop) == null);
        }
        [TestCase("reinserted-source")][TestCase("hidden-contact")]
        public void ConsumedCropExceptionCannotReplayAReinsertedOrNewlyHiddenSource(string fault)
        {
            var crop = Crop(2); var tx = new InventoryTransaction(); Stage(Actor,crop,Zone,"HarvestCultivatedCrop",tx);
            Assert.IsNull(Zone.GetEntityCell(crop)); Assert.IsEmpty(Receipts);
            if (fault == "reinserted-source") Zone.AddEntity(crop,11,10);
            else Zone.GetCell(11,10).IsVisible = false;
            tx.Commit();
            Assert.AreEqual(2,Count("CandyCarrot"), "sound refusal does not revoke committed yields");
            Assert.AreEqual(1,Count("CandyCarrotSeed"));
            Assert.IsEmpty(Receipts); Assert.IsEmpty(Assigned());
        }
        [Test] public void MissingSuppliesThenRestoredSuppliesProveThePositiveRepairIsNotVacuous()
        {
            var target=RepairTarget(); Assert.False(Repair(target)); Assert.IsEmpty(Assigned());
            PayRepair(target); Assert.True(Repair(target)); Assert.AreEqual(1,Audio.AcceptedSoundCount);
        }
        [Test] public void GrazingDoesNotBorrowThePlayerHarvestCue()
        {
            var row=Place("RipeCropRow",20,10); var reserve=Place("RipeCropRow",22,10);
            var grazer=Place("ReedbackGrazer",19,10); var brain=grazer.GetPart<BrainPart>(); brain.CurrentZone=Zone;
            Assert.True(grazer.GetPart<SpreadGrazerPart>().ConfigureForage(Zone,row,reserve));
            grazer.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.True(grazer.GetPart<SpreadGrazerPart>().Fed); Assert.False(reserve.GetPart<FieldHarvestPart>().Harvested);
            Assert.True(row.GetPart<FieldHarvestPart>().Harvested); Assert.AreEqual(0,Count("Emberwheat")); Assert.IsEmpty(Receipts); Assert.IsEmpty(Assigned());
        }
        [Test] public void ThrowingWorkObserverCannotUndoPaidRepair()
        {
            var target=RepairTarget(); PayRepair(target); WorldAudioWork.Callback=receipt=>throw new InvalidOperationException("optional work audio failed");
            Assert.True(Repair(target)); Assert.True(target.GetPart<RepairablePart>().Repaired); Assert.AreEqual(0,Count("FireClay"));
        }
        [Test] public void WorkRouterDisposalDoesNotConsumeAnotherObserver()
        {
            Router.Dispose(); var target=RepairTarget(); PayRepair(target); Assert.True(Repair(target));
            Assert.AreEqual(1,Receipts.Count); Assert.IsEmpty(Assigned());
        }
        public sealed class AfterAction : Part
        { public Action Run; public override bool HandleEvent(GameEvent e) { if(e.ID=="AfterInventoryAction") Run?.Invoke(); return true; } }
    }
}
