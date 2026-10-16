using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    public sealed class ThrowPaymentBoundaryTests : FiftyWorldFixture
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject go; InputHandler input; TurnManager turns; Entity oldWorld;
        [SetUp] public void SetUpPayment()
        {
            oldWorld = TurnManager.World; TurnManager.World = new Entity();
            turns = new TurnManager(); turns.AddEntity(Actor); turns.ProcessUntilPlayerTurn();
            go = new GameObject("throw-payment-boundary"); input = go.AddComponent<InputHandler>();
            input.PlayerEntity = Actor; input.CurrentZone = Zone; input.TurnManager = turns;
            Actor.GetStat("Strength").BaseValue = 20;
        }
        [TearDown] public void CleanPayment()
        { UnityEngine.Object.DestroyImmediate(go); TurnManager.World = oldWorld; AsciiFxBus.Clear(); }
        object Call(string method) => typeof(InputHandler).GetMethod(method, Flags).Invoke(input, null);

        [TestCase(true)] [TestCase(false)]
        public void SelfSplashSettlesCommittedDebtBeforeDeathModalAndAllowsStableLoad(bool lethal)
        {
            var reactionField = typeof(TileReactionSystem).GetField("_reactions", BindingFlags.Static | BindingFlags.NonPublic);
            var initializedField = typeof(TileReactionSystem).GetField("<IsInitialized>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = new List<TileReactionSystem.TileReaction>((List<TileReactionSystem.TileReaction>)reactionField.GetValue(null));
            bool initialized = TileReactionSystem.IsInitialized;
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/TileReactions/Reactions.json")));
            try
            {
                using (var saves = new NewGameSaveFixture())
                {
                    Assert.True(SaveGameService.QuickSave());
                    Actor.GetStat("Hitpoints").BaseValue = lethal ? 1 : 40;
                    var counter = new ThrowPaymentEndCounterPart(); Actor.AddPart(counter);
                    Zone.TileState.WriteCoating(10, 10, "water", 6);
                    var item = Carry("LightningTonic");
                    var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(item,
                        ThrowIntent.Ground(10, 10), new System.Random(123)), Actor, Zone);
                    Assert.True(result.ActionCommitted, "real authored self-splash launched");
                    Assert.AreEqual(lethal, CombatSystem.IsDeathHandled(Actor), "real water/charge reaction distinguishes the pair");
                    Call("HoldThrowAction");
                    var inputState = typeof(InputHandler).GetField("_inputState", Flags);
                    inputState.SetValue(input, Enum.Parse(inputState.FieldType, "WaitingForFxResolution"));
                    Assert.True(ActionResolutionGuard.IsUnstable); Assert.False(SaveGameService.QuickLoad());
                    // A surviving lightning victim legitimately loses one next
                    // action to its authored contact stun. That scheduler closure
                    // is distinct from paying the throw and must remain intact.
                    if (!lethal) Assert.NotNull(Actor.GetEffect<StunnedEffect>());
                    int expectedClosures = lethal ? 1 : 2;
                    Call("UpdateInput");
                    Assert.False(ActionResolutionGuard.IsUnstable, "the death modal must not strand committed action debt");
                    Assert.AreEqual(expectedClosures, counter.Calls, "one throw closure plus the survivor's genuine stun-blocked turn");
                    Assert.AreEqual("Normal", inputState.GetValue(input).ToString());
                    var death = (DeathScreenController)typeof(InputHandler).GetField("_deathScreenController", Flags).GetValue(input);
                    Assert.AreEqual(lethal, death.IsActive);
                    Call("UpdateInput"); Assert.AreEqual(expectedClosures, counter.Calls, "a second modal frame cannot settle again");
                    Assert.True(SaveGameService.QuickLoad(), "the real save service can load after stabilization");
                    saves.ExpectNewLoaded();
                }
            }
            finally
            {
                ActionResolutionGuard.SettlePendingAction(Actor);
                typeof(InputHandler).GetField("_throwDebtActor", Flags).SetValue(input, null);
                typeof(InputHandler).GetField("_throwDebtZone", Flags).SetValue(input, null);
                typeof(InputHandler).GetField("_throwDebtTurns", Flags).SetValue(input, null);
                TileReactionSystem.ResetForTests();
                ((List<TileReactionSystem.TileReaction>)reactionField.GetValue(null)).AddRange(previous);
                initializedField.SetValue(null, initialized);
            }
        }

        [TestCase("EndTurn")] [TestCase("TickEnd")] [TestCase("none")]
        public void LaunchedThrowPaysOnceAndClosesSkillBoundaryEvenIfObserverFails(string phase)
        {
            var item = Carry("Dagger");
            var result = InventorySystem.ExecuteCommand(new ThrowItemCommand(item, 13, 10, new System.Random(123)), Actor, Zone);
            Assert.True(result.ActionCommitted); Assert.NotNull(Zone.GetEntityCell(item));
            var state = ThrowingRules.EnsureState(Actor); state.DrivingCooldown = 6;
            var probe = new ThrowPaymentFaultPart { Event = phase };
            if (phase == "EndTurn") Actor.AddPart(probe); else TurnManager.World.AddPart(probe);
            int tick = turns.TickCount, energy = turns.GetEnergy(Actor), speed = turns.GetSpeed(Actor);
            Call("HoldThrowAction"); Assert.True(ActionResolutionGuard.IsUnstable);
            if (phase != "none") LogAssert.Expect(LogType.Error, new Regex("\\[Turn\\] Exception during turn processing"));
            Call("EndTurnAndProcess");
            long spent = (long)(turns.TickCount - tick) * speed + energy - turns.GetEnergy(Actor);
            Assert.AreEqual(TurnManager.ActionThreshold, spent, "A callback cannot refund a committed projectile, or debit twice after TickEnd.");
            Assert.AreEqual(5, state.DrivingCooldown, "Mandatory owner boundary closes exactly once even if an earlier observer throws.");
            Assert.AreEqual(phase == "none" ? 0 : 1, probe.Calls, "Never retry a partially executed listener.");
            Assert.False(ActionResolutionGuard.IsUnstable); Assert.NotNull(Zone.GetEntityCell(item));
            Assert.AreSame(Actor, turns.CurrentActor, "Recover scheduling before accepting another player input.");
            Assert.GreaterOrEqual(turns.GetEnergy(Actor), TurnManager.ActionThreshold);
            if (phase == "EndTurn") Actor.RemovePart(probe); else TurnManager.World.RemovePart(probe);
            tick = turns.TickCount; energy = turns.GetEnergy(Actor);
            Call("HoldThrowAction"); Call("EndTurnAndProcess");
            spent = (long)(turns.TickCount - tick) * speed + energy - turns.GetEnergy(Actor);
            Assert.AreEqual(TurnManager.ActionThreshold, spent, "The next healthy action must also pay exactly once.");
            Assert.AreEqual(4, state.DrivingCooldown);

        }
    }
    public sealed class ThrowPaymentEndCounterPart : Part
    {
        public int Calls;
        public override bool HandleEvent(GameEvent e) { if (e.ID == "EndTurn") Calls++; return true; }
    }
    public sealed class ThrowPaymentFaultPart : Part
    {
        public string Event; public int Calls;
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == Event) { Calls++; throw new InvalidOperationException("payment boundary fault " + Event); } return true; }
    }
}
