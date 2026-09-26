using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityWorldActionIntegrationTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        HotbarSaveFixture scope;
        Entity player, chair;
        Zone zone;
        InputHandler input;
        TurnManager turns;
        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(true, false);
            input = scope.Input; player = input.PlayerEntity; zone = input.CurrentZone; turns = input.TurnManager;
            player.AddPart(new StatusEffectsPart());
            chair = new Entity { ID = "density-input-chair", BlueprintName = "Chair" };
            chair.AddPart(new ChairPart()); chair.AddPart(new RenderPart { DisplayName = "chair" });
            Assert.True(zone.AddEntity(chair, 3, 4));
            var field = typeof(InputHandler).GetField("_worldActionMenuReturnState", Private);
            field.SetValue(input, Enum.Parse(field.FieldType, "Normal"));
        }
        [TearDown] public void TearDown() => scope?.Dispose();
        void Select(string command)
        {
            var action = new InventoryAction(command, command, command, 's', 10);
            typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Private)
                .Invoke(input, new object[] { action, chair, zone.GetEntityCell(chair), false });
        }
        [TestCase(false)][TestCase(true)]
        public void SuccessfulSeatActionUsesTransactionAndExactlyOneNormalTurn(bool stand)
        {
            if (stand) Assert.True(PlayerSeatService.TryAct(player, chair, zone, PlayerSeatService.SitCommand));
            var probe = new ActionProbe(); player.AddPart(probe); int before = turns.TickCount;
            Select(stand ? PlayerSeatService.StandCommand : PlayerSeatService.SitCommand);
            Assert.AreEqual(1, probe.Before); Assert.AreEqual(1, probe.After);
            Assert.AreEqual(before + TurnManager.ActionThreshold / player.GetStat("Speed").Value, turns.TickCount);
            Assert.AreEqual(!stand, chair.GetPart<ChairPart>().Occupied);
            Assert.AreEqual(!stand, player.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>() != null);
        }
        [TestCase("owner")][TestCase("occupied")][TestCase("adjacent")][TestCase("dead")][TestCase("stale-stand")]
        public void RefusedSeatActionIsFreeAndDoesNotTakeSeat(string reason)
        {
            if (reason == "owner") chair.GetPart<ChairPart>().Owner = "someone-else";
            if (reason == "occupied") chair.GetPart<ChairPart>().Occupied = true;
            if (reason == "adjacent") Assert.True(zone.MoveEntity(player, 4, 4));
            if (reason == "dead") player.GetStat("Hitpoints").BaseValue = 0;
            int before = turns.TickCount;
            Select(reason == "stale-stand" ? PlayerSeatService.StandCommand : PlayerSeatService.SitCommand);
            Assert.AreEqual(before, turns.TickCount); Assert.IsNull(chair.GetPart<ChairPart>().Occupant);
            Assert.IsNull(player.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>());
        }
        [TestCase(false)][TestCase(true)]
        public void PostActionFailureRollsBackSeatAndRemainsFree(bool stand)
        {
            if (stand) Assert.True(PlayerSeatService.TryAct(player, chair, zone, PlayerSeatService.SitCommand));
            var probe = new ActionProbe { FailAfter = true }; player.AddPart(probe); int before = turns.TickCount;
            Select(stand ? PlayerSeatService.StandCommand : PlayerSeatService.SitCommand);
            Assert.AreEqual(1, probe.Before); Assert.AreEqual(1, probe.After);
            Assert.AreEqual(before, turns.TickCount);
            Assert.AreEqual(stand, chair.GetPart<ChairPart>().Occupied);
            Assert.AreEqual(stand, player.GetPart<StatusEffectsPart>().GetEffect<SittingEffect>() != null);
        }
        [TestCase(5, 0, true)][TestCase(4, 0, false)]
        [TestCase(5, EndingSpine.VesselPath, false)][TestCase(5, EndingSpine.GatheredPath, false)]
        public void CompletedNativeTransitionSoundsFirstHighTierArrivalOnce(int tier, int ending, bool audible)
        {
            Zone destination = null;
            for (int x = 0; x < WorldMap.Width && destination == null; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    if (WorldMapAuthoring.TierAt(x,y) == tier) { destination = new Zone(WorldMap.ToZoneID(x,y,0)); break; }
            Assert.NotNull(destination); player.SetIntProperty(EndingSpine.EndingProperty, ending);
            Assert.True(zone.RemoveEntity(player)); Assert.True(destination.AddEntity(player, 3, 4));
            var result = new ZoneTransitionResult { Success = true, NewZone = destination, NewPlayerX = 3, NewPlayerY = 4 };
            MessageLog.Clear(); int before = turns.TickCount;
            typeof(InputHandler).GetMethod("HandleZoneTransition", Private).Invoke(input, new object[] { result });
            Assert.AreEqual(audible ? 1 : 0, MessageLog.GetMessages().Count(m => m.IndexOf("sari", StringComparison.OrdinalIgnoreCase) >= 0));
            Assert.AreEqual(before, turns.TickCount, "entry notification does not add a second turn");
            Assert.False(SariAmbience.OnZoneEntered(player, destination), "arrival is saved before autosave and cannot replay");
        }
        public sealed class ActionProbe : Part
        {
            public override string Name => "DensitySeatInputProbe";
            public int Before, After; public bool FailAfter;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeInventoryAction") Before++;
                if (e.ID == "AfterInventoryAction") { After++; if (FailAfter) throw new InvalidOperationException("seat transaction test refusal"); }
                return true;
            }
        }
    }
}
