// No TacticalSupplyPart dependency: this exercises the existing actual command and effects.
using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class EmergencyDousingObserverTests : FiftyWorldFixture
    {
        List<MessageLog.Entry> previousEntries;
        List<string> previousAnnouncements;
        int previousFlash, previousSerial;
        Action<string> previousObserver;
        [SetUp] public void CaptureMessages()
        {
            previousEntries = MessageLog.GetAllEntries(); previousAnnouncements = MessageLog.GetPendingAnnouncementsSnapshot();
            previousFlash = MessageLog.FlashStamp; previousSerial = MessageLog.NextSerialValue;
            previousObserver = MessageLog.OnMessage; MessageLog.OnMessage = null; MessageLog.Clear();
        }
        [TearDown] public void RestoreMessages()
        {
            MessageLog.Restore(previousEntries, previousAnnouncements, previousFlash, previousSerial);
            MessageLog.OnMessage = previousObserver;
        }
        Entity Npc(bool visible, bool rendered = true)
        {
            Actor = Place("MarlbackScrabbler"); Actor.GetPart<RenderPart>().DisplayName = "test water carrier";
            Actor.GetPart<RenderPart>().Visible = rendered; Zone.GetEntityCell(Actor).IsVisible = visible;
            Assert.False(Actor.HasTag("Player")); return Actor;
        }
        static BurningEffect Ignite(Entity owner)
        { var burn = new BurningEffect(1); Assert.True(owner.ApplyEffect(burn)); return burn; }
        string SelfChoice(Entity water)
            => Choice(water, "DrenchCreature|", action => action.Command.Split('|')[4] == Id(Actor));
        bool Execute(Entity water, string command)
            => new InventoryCommandExecutor().Execute(new PerformInventoryActionCommand(water, command),
                new InventoryContext(Actor, Zone)).Success;

        [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void ObserverVisibilityControlsOnlyTextAndCannotChangeNativeDousing(bool visible, bool rendered)
        {
            var npc = Npc(visible, rendered); var water = Skin(1); Ignite(npc); string command = SelfChoice(water);
            MessageLog.Clear(); Assert.True(Execute(water, command));
            Assert.AreEqual(0, Units(water)); Assert.False(npc.HasEffect<BurningEffect>());
            Assert.AreEqual(1f, npc.GetEffect<WetEffect>().Moisture);
            Assert.Less(npc.GetPart<ThermalPart>().Temperature, npc.GetPart<ThermalPart>().FlameTemperature);
            var messages = MessageLog.GetAllEntries().Select(e => e.Text).ToArray();
            Assert.False(messages.Any(m => m.StartsWith("You ", StringComparison.Ordinal)));
            if (visible && rendered)
                Assert.True(messages.Any(m => m.Contains(npc.GetDisplayName()) && m.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0));
            else Assert.IsEmpty(messages, "Wet application and fire removal may not leak an unseen rescue.");
        }

        [Test] public void ThePlayerRetainsNormalSelfDousingFeedbackEvenWithoutAVisibleCellFlag()
        {
            Assert.True(Actor.HasTag("Player")); Zone.GetEntityCell(Actor).IsVisible = false;
            var water = Skin(1); Ignite(Actor); string command = SelfChoice(water); MessageLog.Clear();
            Assert.True(Execute(water, command)); Assert.AreEqual(0, Units(water));
            Assert.True(MessageLog.GetAllEntries().Any(e => e.Text.StartsWith("You soak yourself", StringComparison.Ordinal)));
        }

        [Test] public void ARejectedNpcDousingCommandDoesNotClaimThePlayerFailed()
        {
            Npc(true); var water = Skin(1); var burn = Ignite(Actor); string command = SelfChoice(water);
            Actor.AddPart(new CombatSupplyRejectWetPart()); MessageLog.Clear();
            Assert.False(Execute(water, command)); Assert.AreEqual(1, Units(water)); Assert.AreSame(burn, Actor.GetEffect<BurningEffect>());
            Assert.False(MessageLog.GetAllEntries().Any(e => e.Text.StartsWith("You ", StringComparison.Ordinal)));
        }

        [Test] public void HidingDousingFeedbackDoesNotEraseAnIndependentInventoryObserverMessage()
        {
            Npc(false); var water = Skin(1); Ignite(Actor); string command = SelfChoice(water);
            const string Independent = "The outer observer recorded a separate event.";
            Actor.AddPart(new DousingIndependentMessageProbe(Independent)); MessageLog.Clear();
            Assert.True(Execute(water, command)); Assert.AreEqual(0, Units(water));
            CollectionAssert.AreEqual(new[] { Independent }, MessageLog.GetAllEntries().Select(e => e.Text).ToArray());
        }

        [Test] public void SuppressingHiddenWetMessagesKeepsActualPorosityAndRemoval()
        {
            Npc(false); var material = Actor.GetPart<MaterialPart>(); Assert.NotNull(material);
            material.Porosity = .5f; var wet = new WetEffect(.2f); MessageLog.Clear();
            Assert.True(Actor.ApplyEffect(wet)); Assert.AreEqual(.3f, wet.Moisture, .00001f);
            Assert.True(Actor.RemoveEffect<WetEffect>()); Assert.False(Actor.HasEffect<WetEffect>());
            Assert.IsEmpty(MessageLog.GetAllEntries());
        }

        [Test] public void HiddenNaturalFlameExpiryStillAppliesTheRealCharredConsequence()
        {
            Npc(false); Assert.IsNull(Actor.GetPart<FuelPart>()); var material = Actor.GetPart<MaterialPart>(); Assert.NotNull(material);
            float combustibility = material.Combustibility; var burn = Ignite(Actor); burn.Duration = 1; burn.JustApplied = false;
            Actor.FireEvent("EndTurn");
            Assert.False(Actor.HasEffect<BurningEffect>()); Assert.True(Actor.HasEffect<CharredEffect>());
            Assert.AreEqual(combustibility * .3f, material.Combustibility, .00001f,
                "A message visibility guard must never early-return out of the Burning lifecycle.");
            // Charred's own message is outside this bounded dousing feedback slice.
        }

        sealed class DousingIndependentMessageProbe : Part
        {
            readonly string message;
            public DousingIndependentMessageProbe(string message) { this.message = message; }
            public override bool HandleEvent(GameEvent e)
            { if (e.ID == "AfterInventoryAction") MessageLog.Add(message); return true; }
        }
    }
}
