using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedReserveInputTests : ConnectedReserveTestBase
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [TestCase("terms", false)][TestCase("pay", false)][TestCase("pay", true)]
        [TestCase("reconcile", false)][TestCase("introduction", false)][TestCase("cook", false)]
        public void ActualWorldMenuKeepsTermsFreeAndChargesOnlyCommittedSocialActions(string action, bool failAfter)
        {
            if (action == "reconcile") { Assert.True(Harvest(Crop()).Success); Give("Emberwheat", 2); }
            if (action == "introduction" || action == "cook") { RepairPan(); if (action == "introduction") { Assert.True(Introduce()); MovePlayer(Reserve); } }
            var zone = action == "cook" ? Kitchen : Reserve;
            var target = action == "cook" ? Cook : Keeper;
            string command = action == "cook" ? CookIntroductionPart.IntroductionCommand : "ReserveAccess:" + action;
            if (failAfter) Player.AddPart(new ConnectedReserveThrowAfterAction());
            using (var ui = new HotbarSaveFixture(true, false))
            {
                var turns = new TurnManager();
                turns.RestoreSavedState(17, true, Player, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Player, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("reserve-social", "native-world-menu", Manager, turns, Player));
                SettlementRuntime.ActiveZone = zone; CropSystem.Factory = SeedPart.Factory = Factory;
                var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", Hidden); state.SetValue(ui.Input, Enum.Parse(state.FieldType, "Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", Hidden).Invoke(ui.Input, new object[] { target, zone.GetEntityCell(target), false });
                var selected = ((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", Hidden).GetValue(menu)).Single(a => a.Command == command);
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", Hidden).Invoke(ui.Input, new object[] { selected, target, zone.GetEntityCell(target), false });
                Assert.AreEqual(failAfter || action == "terms" ? 17 : 27, turns.TickCount);
                Assert.AreEqual(action == "pay" && !failAfter ? 32 : 40, TradeSystem.GetDrams(Player));
                if (action == "cook") Assert.NotNull(Player.GetPart<CookIntroductionKnowledgePart>());
                else Assert.AreEqual(action == "terms" || failAfter ? "Unknown" : "Granted", State());
                if (action == "reconcile") Assert.AreEqual(0, CarriedCount("Emberwheat"));
            }
        }

        [TestCase(false)][TestCase(true)]
        public void WorldExamineWarnsAboutExactReserveOnlyUntilPermissionIsGranted(bool granted)
        {
            var crop = Crop(); if (granted) Assert.True(Act("pay"));
            var method = typeof(ExaminablePart).GetMethods().SingleOrDefault(m => m.Name == "BuildWorldExamineLine" && m.GetParameters().Length == 3);
            Assert.NotNull(method, "World examine needs the actual viewer to explain local rights.");
            var description = (string)method.Invoke(crop.GetPart<ExaminablePart>(), new object[] { Reserve, Reserve.GetEntityCell(crop), Player });
            Assert.AreEqual(!granted, description.Contains("Nella's tied reserve"));
            var publicCrop = Crop(5, 7);
            var publicDescription = (string)method.Invoke(publicCrop.GetPart<ExaminablePart>(), new object[] { Reserve, Reserve.GetEntityCell(publicCrop), Player });
            Assert.False(publicDescription.Contains("Nella's tied reserve"));
            Assert.AreEqual(granted ? "Granted" : "Unknown", State());
        }

        [Test] public void EmptyClaimedBedKeepsItsWarningAfterHarvestWhilePublicSoilDoesNot()
        {
            Reserve.MoveEntity(Keeper, 25, 5); Assert.True(Harvest(Crop()).Success); Reserve.MoveEntity(Keeper, 5, 5);
            Assert.That(LocalGatheringClaims.WarningFor(Player, SoilA, Reserve), Does.Contain("Nella's tied reserve"));
            var publicSoil = Soil(Reserve, 5, 7);
            Assert.Null(LocalGatheringClaims.WarningFor(Player, publicSoil, Reserve));
            Assert.True(Act("pay")); Assert.Null(LocalGatheringClaims.WarningFor(Player, SoilA, Reserve));
        }
    }
}
