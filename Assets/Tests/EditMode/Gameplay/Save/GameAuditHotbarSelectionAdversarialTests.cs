using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditHotbarSelectionAdversarialTests
    {
        private sealed class ActivationProbe : Part
        {
            public int Calls;
            public override bool HandleEvent(GameEvent e)
            {if(e.ID.StartsWith("AuditHotbar",StringComparison.Ordinal))Calls++;return true;}
        }
        [TestCase(0)] [TestCase(5)] [TestCase(9)]
        public void OccupiedSelectionSurvivesAmongSeveralChoices(int selected)
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(0,0,5,9));f.Restore(selected);Assert.AreEqual(selected,f.Capture().SelectedHotbarSlot);Assert.AreEqual(selected,f.Rendered);}}
        [TestCase(-1)] [TestCase(-2)] [TestCase(3)] [TestCase(10)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void InvalidSelectionFallsBackWithoutRebinding(int selected)
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));var part=f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();var slots=part.SlotAssignments.ToArray();f.Restore(selected);Assert.AreEqual(5,f.Selected);Assert.AreEqual(5,f.Rendered);CollectionAssert.AreEqual(slots,part.SlotAssignments);}}
        [TestCase("null")] [TestCase("missing")] [TestCase("empty")]
        public void NoAvailableAbilityClearsInputAndRenderer(string shape)
        {using(var f=new HotbarSaveFixture()){if(shape=="null")f.Input.PlayerEntity=null;else if(shape=="missing")f.Input.PlayerEntity.RemovePart(f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>());else f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().RemoveAbility(f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(0).ID);f.Restore(9);Assert.AreEqual(-1,f.Selected);Assert.AreEqual(-1,f.Rendered);Assert.AreEqual(-1,f.CaptureInput());}}
        [Test]
        public void CooldownDoesNotPreferReadySibling()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));var part=f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();part.GetAbilityBySlot(9).CooldownRemaining=int.MaxValue;f.Restore(9);Assert.AreEqual(9,f.CaptureInput());Assert.AreEqual(int.MaxValue,part.GetAbilityBySlot(9).CooldownRemaining);Assert.AreEqual(0,part.GetAbilityBySlot(5).CooldownRemaining);}}
        [TestCase(0,9)] [TestCase(9,0)]
        public void DifferentActorBindingsAreValidatedAfterReplacement(int oldSlot,int saved)
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(oldSlot,oldSlot));var loaded=HotbarSaveFixture.RoundTrip(HotbarSaveFixture.MakeState(saved,saved,5));f.Bootstrap.ApplyLoadedGame(loaded);Assert.AreEqual(saved,f.Selected);Assert.AreEqual(saved,f.Rendered);Assert.AreSame(loaded.Player,f.Input.PlayerEntity);Assert.AreEqual(saved,f.Capture().SelectedHotbarSlot);}}
        [Test]
        public void RemovedSelectionFallsBackAtCaptureBeforeUpdate()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));var part=f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();Assert.IsTrue(part.RemoveAbility(part.GetAbilityBySlot(9).ID));Assert.AreEqual(5,f.Capture().SelectedHotbarSlot);Assert.IsNull(part.GetAbilityBySlot(9));}}
        [Test]
        public void ChangedAbilityInSameSlotKeepsSlotIdentity()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));var part=f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();Guid old=part.GetAbilityBySlot(9).ID;part.RemoveAbility(old);Guid replacement=part.AddAbility("Replacement","AuditHotbarReplacement","Audit");part.AssignAbilityToSlot(replacement,9);f.Restore(9);Assert.AreEqual(9,f.CaptureInput());Assert.AreEqual(replacement,part.GetAbilityBySlot(9).ID);Assert.IsNull(part.GetAbility(old));}}
        [Test]
        public void ExistingWhollyUnboundMigrationRemainsCompatible()
        {using(var f=new HotbarSaveFixture()){var state=HotbarSaveFixture.MakeState(9,5,9);var part=state.Player.GetPart<ActivatedAbilitiesPart>();Array.Clear(part.SlotAssignments,0,part.SlotAssignments.Length);var loaded=HotbarSaveFixture.RoundTrip(state);f.Bootstrap.ApplyLoadedGame(loaded);Assert.AreEqual(0,f.Selected);Assert.AreEqual("AuditHotbar5",loaded.Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(0).Command);Assert.AreEqual("AuditHotbar9",loaded.Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(1).Command);}}
        [Test]
        public void BootstrapWithoutInputCanCaptureAndApply()
        {using(var f=new HotbarSaveFixture(false)){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));Assert.AreEqual(-1,f.Capture().SelectedHotbarSlot);Assert.DoesNotThrow(()=>f.Bootstrap.ApplyLoadedGame(HotbarSaveFixture.RoundTrip(HotbarSaveFixture.MakeState(9,5,9))));Assert.AreEqual(-1,f.Capture().SelectedHotbarSlot);}}
        [Test]
        public void SparseCaptureKeepsLaterOccupiedSelection()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));var part=f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();var slots=part.SlotAssignments.ToArray();Assert.AreEqual(9,f.Capture().SelectedHotbarSlot);CollectionAssert.AreEqual(slots,part.SlotAssignments);}}
        [Test]
        public void RendererIsOptionalForRestoreAndLoad()
        {using(var f=new HotbarSaveFixture(true,false)){f.BindOld(HotbarSaveFixture.MakeState(5,5,9));f.Restore(9);Assert.AreEqual(9,f.Selected);f.Bootstrap.ApplyLoadedGame(HotbarSaveFixture.RoundTrip(f.Capture()));Assert.AreEqual(9,f.Selected);}}
        [Test]
        public void RepeatedRestoreAndCaptureAreIdempotent()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(5,5,9));var original=f.Input.PlayerEntity;for(int i=0;i<5;i++){f.Restore(9);Assert.AreEqual(9,f.Capture().SelectedHotbarSlot);Assert.AreEqual(9,f.Rendered);}Assert.AreSame(original,f.Input.PlayerEntity);Assert.AreEqual(17,f.Input.TurnManager.TickCount);}}
        [Test]
        public void RestorePreservesPendingTargetAndDoesNotDispatchActivation()
        {
            using(var f=new HotbarSaveFixture())
            {
                f.BindOld(HotbarSaveFixture.MakeState(5,5,9));var player=f.Input.PlayerEntity;var part=player.GetPart<ActivatedAbilitiesPart>();var pending=part.GetAbilityBySlot(5);pending.CooldownRemaining=4;part.GetAbilityBySlot(9).TargetingMode=AbilityTargetingMode.SelfCentered;
                HotbarSaveFixture.Set(f.Input,"_pendingAbility",pending);var mode=HotbarSaveFixture.Get(f.Input,"_inputState");var slots=part.SlotAssignments.ToArray();var probe=new ActivationProbe();player.AddPart(probe);
                // Positive dispatch control proves the listener is reachable.
                player.FireEventAndRelease(GameEvent.New("AuditHotbarProbe"));Assert.AreEqual(1,probe.Calls);probe.Calls=0;
                f.Restore(9);Assert.AreEqual(0,probe.Calls);Assert.AreSame(pending,HotbarSaveFixture.Get(f.Input,"_pendingAbility"));Assert.AreSame(pending,HotbarSaveFixture.Get(f.Renderer,"_pendingHotbarAbility"));Assert.AreEqual(mode,HotbarSaveFixture.Get(f.Input,"_inputState"));Assert.AreEqual(4,pending.CooldownRemaining);Assert.AreEqual(17,f.Input.TurnManager.TickCount);Assert.AreEqual(1000,f.Input.TurnManager.GetEnergy(player));CollectionAssert.AreEqual(slots,part.SlotAssignments);
            }
        }
        // Player-flow hypotheses added after independent cold-eye review.
        [TestCase(9,5)] [TestCase(5,9)]
        public void UnbindingSelectedOrSiblingSlotUsesCurrentOccupancy(int unbind,int expected)
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().AssignAbilityToSlot(Guid.Empty,unbind);var loaded=HotbarSaveFixture.RoundTrip(f.Capture());f.Bootstrap.ApplyLoadedGame(loaded);Assert.AreEqual(expected,f.Selected);Assert.AreEqual(expected,f.Rendered);}}
        [Test]
        public void RemovingUnselectedAbilityPreservesLaterSelection()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));var part=f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();part.RemoveAbility(part.GetAbilityBySlot(5).ID);Assert.AreEqual(9,f.Capture().SelectedHotbarSlot);}}
        [Test]
        public void UnboundOverflowDoesNotReplaceSelectedSlot()
        {using(var f=new HotbarSaveFixture()){var state=HotbarSaveFixture.MakeState(9,0,1,2,3,4,5,6,7,8,9);var part=state.Player.GetPart<ActivatedAbilitiesPart>();Guid overflow=part.AddAbility("Overflow","AuditOverflow","Audit");Assert.AreEqual(-1,part.GetSlotForAbility(overflow));f.BindOld(state);f.Bootstrap.ApplyLoadedGame(HotbarSaveFixture.RoundTrip(f.Capture()));Assert.AreEqual(9,f.Selected);Assert.AreEqual(-1,f.Input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(overflow));}}
        [TestCase(false,5)] [TestCase(true,-1)]
        public void InitialCheckpointUsesValidatedPreUpdateSelection(bool empty,int expected)
        {using(var f=new HotbarSaveFixture()){var state=empty?HotbarSaveFixture.MakeState(-1):HotbarSaveFixture.MakeState(-1,5,9);f.BindOld(state);SaveGameService.RegisterRuntime(f.Capture,f.Bootstrap.ApplyLoadedGame,state.GameID);Assert.IsTrue(SaveGameService.BeginNewGame());f.Restore(9);Assert.IsTrue(SaveGameService.QuickLoad());Assert.AreEqual(expected,f.Selected);Assert.AreEqual(expected,f.Rendered);Assert.AreEqual(17,f.Input.TurnManager.TickCount);}}
        [Test]
        public void RepeatedV7CaptureLoadCaptureRetainsSelectedSlotAndAliases()
        {using(var f=new HotbarSaveFixture()){f.BindOld(HotbarSaveFixture.MakeState(9,5,9));for(int i=0;i<3;i++){var loaded=HotbarSaveFixture.RoundTrip(f.Capture());f.Bootstrap.ApplyLoadedGame(loaded);Assert.AreEqual(7,loaded.SaveVersion);Assert.AreEqual(9,f.Selected);Assert.AreEqual(9,f.Rendered);Assert.AreEqual(9,f.Capture().SelectedHotbarSlot);Assert.AreSame(loaded.Player,f.Input.TurnManager.CurrentActor);}}}
    }
}
