using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class ProofingCoatInputTests : FiftyWorldFixture
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static object Get(object owner, string field) => owner.GetType().GetField(field, Flags).GetValue(owner);
        static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Flags).Invoke(owner, args);

        [TestCase("valid")] [TestCase("stale-garment")]
        [TestCase("empty-flask")] [TestCase("disabled")]
        public void CrowdedCoatPopupPaintsAndDispatchesTheExactLateGarment(string condition)
        {
            EnhancementFactory.ForceReinitialize(); ProofingRegistry.ResetForTests(); Pack.MaxWeight = 10000;
            var input = new InputTestFixture(); input.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
                using (var ui = new HotbarSaveFixture(true, false))
                {
                    var manager = OverworldZoneManager.CreateDetached(Factory, 64, true); manager.SetActiveZone(Zone);
                    Actor.GetStat("Speed").BaseValue = 100;
                    Clock.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry>
                        { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                    ui.BindOld(GameSessionState.Capture("coat-input", "inventory", manager, Clock, Actor));
                    ui.Input.WorldMap = manager.WorldMap;
                    var grid = new GameObject("Coat menu grid"); grid.transform.SetParent(ui.Root.transform, false); grid.AddComponent<Grid>();
                    var tiles = new GameObject("Coat menu tiles"); tiles.transform.SetParent(grid.transform, false);
                    var inventory = ui.Root.AddComponent<InventoryUI>(); inventory.Tilemap = tiles.AddComponent<Tilemap>();
                    inventory.PlayerEntity = Actor; inventory.CurrentZone = Zone; inventory.EntityFactory = Factory;
                    ui.Input.InventoryUI = inventory;
                    var flask = Flask(3, "cold-proofing"); var garments = new List<Entity>();
                    for (int n = 0; n < 30; n++)
                    {
                        var g = Factory.CreateEntity("LeatherArmor"); g.RemovePart(g.GetPart<StackerPart>());
                        g.GetPart<RenderPart>().DisplayName = "garment " + n.ToString("D2");
                        Assert.True(Pack.AddObject(g)); garments.Add(g);
                    }
                    var late = garments.Last();
                    if (condition == "disabled")
                    { Assert.True(ItemEnhancing.Apply(late, "EnhancementLacquered")); Assert.True(ItemEnhancing.Apply(late, "EnhancementProofed")); }
                    Call(ui.Input, "OpenInventory"); Assert.True(inventory.ReopenItemActionPopupFor(flask));
                    var popup = Get(inventory, "_itemActionPopup"); var actions = (IList)Get(popup, "Actions");
                    int index = -1;
                    for (int n = 0; n < actions.Count; n++)
                        if (((string)Get(actions[n], "Label")).StartsWith("coat garment 29", StringComparison.Ordinal)) index = n;
                    Assert.Greater(index, 25, "The real late coat must exist beyond alphabet shortcuts.");
                    Action<Key> press = key =>
                    {
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
                        Call(ui.Input, "HandleInventoryInput");
                    };
                    for (int n = 0; n < index; n++) press(Key.DownArrow);
                    Assert.AreEqual(index, Get(popup, "CursorIndex"));
                    Assert.True(inventory.GetComponentsInChildren<SkillScreenText>(true)
                        .Any(t => t.enabled && t.gameObject.activeSelf && t.Content.Contains("coat garment 29")), "The inactive test root must generate the selected command label for rendering.");
                    var hits = ((IEnumerable)Get(inventory, "_ledgerHits")).Cast<object>()
                        .Where(h => Get(h, "Kind").ToString() == "PopupRow").ToArray();
                    Assert.True(hits.Any(h => (int)Get(h, "Index") == index));
                    foreach (var hit in new[] { hits.First(), hits.Last() })
                    {
                        var point = ((Rect)Get(hit, "Bounds")).center;
                        int hitIndex = (int)Call(inventory, "LedgerHitAt", point);
                        var actual = ((IList)Get(inventory, "_ledgerHits"))[hitIndex];
                        Assert.AreEqual(Get(hit, "Index"), Get(actual, "Index"), "Painted row and pointer use the same absolute choice.");
                    }
                    Assert.AreEqual(17, Clock.TickCount, "Browsing is free.");
                    if (condition == "stale-garment") Assert.True(Pack.RemoveObject(late));
                    if (condition == "empty-flask") flask.GetPart<LiquidVesselPart>().Volume = 0;
                    press(Key.Enter);
                    bool success = condition == "valid";
                    Assert.AreEqual(success ? 27 : 17, Clock.TickCount);
                    Assert.AreEqual(condition == "empty-flask" ? 0 : success ? 2 : 3, flask.GetPart<LiquidVesselPart>().Volume);
                    Assert.AreEqual(success, late.Parts.OfType<EnhancementSteeped>().Any());
                    Assert.True(garments.Take(29).All(g => !g.Parts.OfType<EnhancementSteeped>().Any()));
                    if (!success) { Assert.True(inventory.IsOpen); Assert.AreSame(popup, Get(inventory, "_itemActionPopup")); }
                    inventory.Close();
                }
            }
            finally { input.TearDown(); }
        }
    }
}
