using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class ItemUtility35InventoryTests : FiftyWorldFixture
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static object Field(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
        static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, Flags).Invoke(target, args);

        [TestCase("HealingTonic", "TreatCompanion|")]
        [TestCase("GlimmerBrine", "MaterialField|")]
        [TestCase("GlowQuartz", "CrackQuartz|")]
        public void RealSupplyActionClosesInventoryAndQueuesExactlyOneTurn(string blueprint, string prefix)
        { InventoryAction(blueprint, prefix, false); }

        [TestCase("HealingTonic", "TreatCompanion|")]
        [TestCase("GlimmerBrine", "MaterialField|")]
        [TestCase("GlowQuartz", "CrackQuartz|")]
        public void StaleSupplyActionKeepsMenuAndQueuesNoTurn(string blueprint, string prefix)
        { InventoryAction(blueprint, prefix, true); }

        [Test]
        public void RoastedPerchDecisionReadsSelectedCompanionThenClearsWhenTheyMove()
        {
            var fixture = GetType().Assembly.GetType("CavesOfOoo.Tests.HotbarSaveFixture");
            using (var scope = (IDisposable)Activator.CreateInstance(fixture, Flags, null, new object[] { false, false }, null))
            {
                var host = new GameObject("roasted perch decision reader");
                try
                {
                    host.transform.SetParent(((GameObject)Field(scope, "Root")).transform, false);
                    var grid = new GameObject("Grid"); grid.transform.SetParent(host.transform, false); grid.AddComponent<Grid>();
                    var tiles = new GameObject("Tiles"); tiles.transform.SetParent(grid.transform, false);
                    var tilemap = tiles.AddComponent<Tilemap>(); tiles.AddComponent<TilemapRenderer>();
                    foreach (var cell in Zone.Cells) { cell.IsVisible = true; cell.Explored = true; }

                    var perch = Carry("RoastedLakePerch");
                    var friend = Place("MarlbackScrabbler");
                    Assert.True(friend.GetPart<BrainPart>().SetPartyLeader(Actor));
                    friend.GetPart<RenderPart>().DisplayName = "Injured trail companion";
                    friend.GetStat("Hitpoints").BaseValue = 1;
                    int companionHp = friend.GetStatValue("Hitpoints");
                    int playerHp = Actor.GetStatValue("Hitpoints");
                    int tick = Clock.TickCount;
                    int quantity = perch.GetPart<StackerPart>()?.StackCount ?? 1;

                    var ui = host.AddComponent<InventoryUI>();
                    ui.Tilemap = tilemap;
                    ui.PlayerEntity = Actor;
                    ui.CurrentZone = Zone;
                    ui.Open();
                    Assert.True(ui.ReopenItemActionPopupFor(perch));
                    var popup = Field(ui, "_itemActionPopup");
                    var commands = ((IList)Field(popup, "Actions")).Cast<object>()
                        .Select(row => (string)Field(row, "Command")).ToArray();
                    int share = Array.FindIndex(commands, command => command.StartsWith("ShareMeal|", StringComparison.Ordinal));
                    int eat = Array.IndexOf(commands, "Eat");
                    Assert.That(share, Is.GreaterThanOrEqualTo(0), "The actual pack popup must offer sharing.");
                    Assert.That(eat, Is.GreaterThanOrEqualTo(0), "The same pack popup must offer eating.");
                    var cursor = popup.GetType().GetField("CursorIndex", Flags);

                    cursor.SetValue(popup, share);
                    string sharedDetails = ui.SelectedDecisionDetails();
                    ui.SelectedDecisionPresentation(out string sharedPresentation);
                    Assert.True(Zone.MoveEntity(friend, 13, 10));
                    string staleDetails = ui.SelectedDecisionDetails();
                    var staleDocument = ui.SelectedDecisionPresentation(out string stalePresentation);
                    cursor.SetValue(popup, eat);
                    string eatDetails = ui.SelectedDecisionDetails();
                    ui.SelectedDecisionPresentation(out string eatPresentation);

                    foreach (string preview in new[] { sharedDetails, sharedPresentation })
                    {
                        Assert.That(preview, Does.Contain(friend.GetDisplayName()));
                        Assert.That(preview, Does.Match(@"(?i)\b(?:one|1) (?:unit|item|meal)\b"));
                        Assert.That(preview, Does.Contain("4 to 10 HP"));
                        Assert.That(preview, Does.Not.Contain("Eat one unit."));
                    }
                    Assert.IsNull(staleDetails);
                    Assert.IsNull(staleDocument);
                    Assert.IsNull(stalePresentation);
                    Assert.That(eatDetails, Does.Contain("Eat one unit.").And.Contain("HP missing:"));
                    Assert.AreEqual(eatDetails, eatPresentation);
                    Assert.True(Pack.Objects.Contains(perch));
                    Assert.AreEqual(quantity, perch.GetPart<StackerPart>()?.StackCount ?? 1);
                    Assert.AreEqual(companionHp, friend.GetStatValue("Hitpoints"));
                    Assert.AreEqual(playerHp, Actor.GetStatValue("Hitpoints"));
                    Assert.AreEqual(tick, Clock.TickCount);
                    Assert.False(ui.ConsumePendingEverydayTurn());
                    ui.Close();
                }
                finally { Object.DestroyImmediate(host); }
            }
        }

        void InventoryAction(string blueprint, string prefix, bool stale)
        {
            var fixture = GetType().Assembly.GetType("CavesOfOoo.Tests.HotbarSaveFixture");
            using (var scope = (IDisposable)Activator.CreateInstance(fixture, Flags, null, new object[] { false, false }, null))
            {
                var host = new GameObject("combat utility inventory");
                try
                {
                    host.transform.SetParent(((GameObject)Field(scope, "Root")).transform, false);
                    var grid = new GameObject("Grid"); grid.transform.SetParent(host.transform, false); grid.AddComponent<Grid>();
                    var tiles = new GameObject("Tiles"); tiles.transform.SetParent(grid.transform, false);
                    var tilemap = tiles.AddComponent<Tilemap>(); tiles.AddComponent<TilemapRenderer>();
                    foreach (var cell in Zone.Cells) { cell.IsVisible = true; cell.Explored = true; }
                    var item = Carry(blueprint);
                    if (blueprint == "HealingTonic")
                    {
                        var friend=Place("MarlbackScrabbler");Assert.True(friend.GetPart<BrainPart>().SetPartyLeader(Actor));
                        friend.GetStat("Hitpoints").BaseValue=1;
                    }
                    var ui = host.AddComponent<InventoryUI>(); ui.Tilemap = tilemap; ui.PlayerEntity = Actor; ui.CurrentZone = Zone; ui.Open();
                    Assert.True(ui.ReopenItemActionPopupFor(item));
                    var actions = (IList)Field(Field(ui, "_itemActionPopup"), "Actions");
                    int selected = -1;
                    for (int i = 0; i < actions.Count; i++)
                        if (((string)Field(actions[i], "Command")).StartsWith(prefix, StringComparison.Ordinal)) { selected = i; break; }
                    Assert.That(selected, Is.GreaterThanOrEqualTo(0), "the normal inventory must offer the real utility action");
                    if (stale) Assert.True(Zone.MoveEntity(Actor, 9, 10));
                    Call(ui, "ExecuteItemAction", selected);
                    Assert.AreEqual(stale, ui.IsOpen);
                    Assert.AreEqual(!stale, Call(ui, "ConsumePendingEverydayTurn"));
                    Assert.False((bool)Call(ui, "ConsumePendingEverydayTurn"));
                    Assert.AreEqual(stale, Pack.Objects.Contains(item));
                    if (stale) Assert.NotNull(Field(ui, "_itemActionPopup"));
                    ui.Close();
                }
                finally { Object.DestroyImmediate(host); }
            }
        }

    }
}
