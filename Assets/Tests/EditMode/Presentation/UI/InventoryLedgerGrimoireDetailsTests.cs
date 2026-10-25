using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Real registered grimoire candidates and retained modal labels. These
    /// checks exercise the existing F1 decision-prose seam, not OS input or GPU rendering.</summary>
    public sealed class InventoryLedgerGrimoireDetailsTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [TestCase(0)] [TestCase(9)]
        public void CandidateDetailsDescribeEachActualPowerBeforeAssignmentWithoutChangingBindings(int targetSlot)
        {
            using (var r = new Rig())
            {
                var bindings = r.Abilities.SlotAssignments.ToArray();
                var carried = r.Player.GetPart<InventoryPart>().Objects.ToArray();
                r.OpenPicker(targetSlot);
                object picker = Get(r.UI, "_grimoirePicker");
                var candidates = (IList)Get(picker, "Grimoires"); Assert.AreEqual(2, candidates.Count);
                string first = null;
                for (int index = 0; index < candidates.Count; index++)
                {
                    Set(picker, "CursorIndex", index); Call(r.UI, "Render");
                    var candidate = (ActivatedAbility)candidates[index];
                    Assert.True(GrimoireTooltipData.TryGet(candidate.SourcePowerClass, out var expected));
                    var document = r.UI.SelectedDecisionPresentation(out var text);
                    Assert.That(text, Is.Not.Null.And.Not.Empty, "F1 must explain the highlighted candidate before binding it.");
                    Assert.IsNull(document, "A candidate ability is neutral decision prose, not a falsely examined inventory entity.");
                    StringAssert.Contains(expected.DisplayName, text);
                    StringAssert.Contains(expected.Flavor, text);
                    StringAssert.Contains(expected.Mechanics, text);
                    StringAssert.Contains(expected.Signature, text);
                    Assert.AreEqual(text, r.UI.SelectedDecisionDetails());
                    if (index == 0) first = text;
                    else Assert.AreNotEqual(first, text, "Moving the picker must replace the prior candidate's mechanics.");
                    CollectionAssert.AreEqual(bindings, r.Abilities.SlotAssignments);
                    Assert.AreEqual(7, r.Abilities.AbilityList[1].CooldownRemaining);
                    CollectionAssert.AreEqual(carried, r.Player.GetPart<InventoryPart>().Objects);
                    Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
                }
                r.UI.HideForDecisionReader();
                Assert.False(r.UI.GetComponentInChildren<Canvas>(true).isActiveAndEnabled);
                r.UI.RestoreAfterDecisionReader();
                Assert.AreSame(picker, Get(r.UI, "_grimoirePicker"));
                Assert.AreEqual(1, Get(picker, "CursorIndex"));
                Assert.AreEqual(targetSlot, Get(picker, "TargetSlot"));
                Assert.True(r.UI.GetComponentInChildren<Canvas>(true).isActiveAndEnabled);
                CollectionAssert.AreEqual(bindings, r.Abilities.SlotAssignments);
                Call(r.UI, "CloseGrimoirePicker");
                var bound = r.Abilities.GetAbilityBySlot((int)Get(r.UI, "_abilitiesCursorSlot"));
                Assert.NotNull(bound);
                Assert.AreEqual(AbilityDetailsBuilder.BuildForAbility(r.Player, bound.ID), r.UI.SelectedDecisionDetails(),
                    "Closing the picker restores the current bound slot's facts, not the prior candidate's prose.");
                CollectionAssert.AreEqual(bindings, r.Abilities.SlotAssignments);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void CandidateDetailsShortcutIsPaintedInsideTheModalAboveItsScrim(bool gamepad)
        {
            var inputScope = new InputTestFixture(); inputScope.Setup();
            try
            {
                if (gamepad) InputSystem.AddDevice<Gamepad>().MakeCurrent();
                Assert.AreEqual(gamepad, NativeGamepadInput.IsConnected);
                using (var r = new Rig())
                {
                    r.OpenPicker(9); Canvas.ForceUpdateCanvases();
                    var content = (RectTransform)Get(r.UI, "_ledgerContent");
                    string key = gamepad ? "LT+RT" : "F1";
                    var hints = r.UI.GetComponentsInChildren<SkillScreenText>(true).Where(t => t.isActiveAndEnabled
                        && t.Content.IndexOf(key, StringComparison.Ordinal) >= 0
                        && t.Content.IndexOf("candidate", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                    Assert.AreEqual(1, hints.Count, "The candidate picker must advertise its actual details control.");
                    var hint = hints[0]; Rect bounds = LogicalBounds(content, hint.rectTransform);
                    Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(250)); Assert.That(bounds.xMax, Is.LessThanOrEqualTo(1030));
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(142)); Assert.That(bounds.yMax, Is.LessThanOrEqualTo(698));
                    Assert.That(hint.GlyphHeight, Is.GreaterThanOrEqualTo(18));
                    foreach (string line in hint.Content.Split('\n'))
                        Assert.That(line.Length * hint.GlyphHeight * .5f, Is.LessThanOrEqualTo(bounds.width + .1f));
                    foreach (var image in r.UI.GetComponentsInChildren<Image>(true))
                        if (image.isActiveAndEnabled && image.color.a > .5f && image.transform.parent == hint.transform.parent
                            && image.transform.GetSiblingIndex() > hint.transform.GetSiblingIndex())
                            Assert.False(LogicalBounds(content, image.rectTransform).Overlaps(bounds),
                                "A later modal backing must not dim the advertised candidate shortcut.");
                    Call(r.UI, "CloseGrimoirePicker");
                    Assert.False(r.UI.GetComponentsInChildren<SkillScreenText>(true).Any(t => t.isActiveAndEnabled
                        && t.Content.IndexOf("candidate", StringComparison.OrdinalIgnoreCase) >= 0),
                        "Candidate-only guidance must disappear with the picker.");
                }
            }
            finally { inputScope.TearDown(); }
        }

        [TestCase("removed")] [TestCase("foreign-player")] [TestCase("closed")] [TestCase("search")]
        public void CandidateDetailsRefuseAnInvalidOrObscuredOwner(string invalidation)
        {
            using (var r = new Rig())
            {
                r.OpenPicker(9);
                Assert.That(r.UI.SelectedDecisionDetails(), Is.Not.Null.And.Not.Empty, "Positive owner control.");
                if (invalidation == "removed") r.Abilities.RemoveAbility(r.Abilities.AbilityList[0].ID);
                else if (invalidation == "foreign-player") r.UI.PlayerEntity = r.Factory.CreateEntity("Player");
                else if (invalidation == "closed") r.UI.Close();
                else Set(r.UI, "_inventorySearchActive", true);
                var after = r.Abilities.SlotAssignments.ToArray();
                Assert.IsNull(r.UI.SelectedDecisionPresentation(out var text));
                Assert.IsNull(text, "Invalid candidates must not leak stale mechanics into the free reader.");
                Assert.IsNull(r.UI.SelectedDecisionDetails());
                CollectionAssert.AreEqual(after, r.Abilities.SlotAssignments);
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        static Rect LogicalBounds(RectTransform content, RectTransform child)
        {
            var b = RectTransformUtility.CalculateRelativeRectTransformBounds(content, child);
            return new Rect(b.min.x - content.rect.xMin, content.rect.yMax - b.max.y, b.size.x, b.size.y);
        }
        static object Get(object owner, string name)
        { var field = owner.GetType().GetField(name, Members); Assert.NotNull(field, name); return field.GetValue(owner); }
        static void Set(object owner, string name, object value)
        { var field = owner.GetType().GetField(name, Members); Assert.NotNull(field, name); field.SetValue(owner, value); }
        static object Call(object owner, string name, params object[] args)
        { var method = owner.GetType().GetMethod(name, Members); Assert.NotNull(method, name); return method.Invoke(owner, args); }

        sealed class Rig : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly int oldEquipmentVersion = EquipmentChangeBus.GlobalVersion;
            readonly GameObject root;
            public readonly InventoryUI UI;
            public readonly EntityFactory Factory;
            public readonly Entity Player;
            public readonly ActivatedAbilitiesPart Abilities;
            public Rig()
            {
                saved = new HotbarSaveFixture(false, false);
                try
                {
                    root = new GameObject("Grimoire candidate details fixture");
                    var cameraHost = new GameObject("Candidate camera"); cameraHost.transform.SetParent(root.transform, false); cameraHost.tag = "MainCamera";
                    var camera = cameraHost.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 22.5f;
                    camera.transform.position = new Vector3(40, 22.5f, -10);
                    var grid = new GameObject("Candidate grid", typeof(Grid)); grid.transform.SetParent(root.transform, false);
                    var tiles = new GameObject("Candidate tiles", typeof(Tilemap)); tiles.transform.SetParent(grid.transform, false);
                    Factory = new EntityFactory(); Factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                    Player = Factory.CreateEntity("Player"); Assert.NotNull(Player);
                    var previous = Player.GetPart<ActivatedAbilitiesPart>(); if (previous != null) Player.RemovePart(previous);
                    Abilities = new ActivatedAbilitiesPart(); Player.AddPart(Abilities);
                    Abilities.AddAbility("Kindle", "CandidateKindle", "Grimoire", sourcePowerClass: "Pyromancy_Kindle");
                    Abilities.AddAbility("Ice Lance", "CandidateIceLance", "Grimoire", sourcePowerClass: "Cryomancy_IceLance");
                    Abilities.AbilityList[1].CooldownRemaining = 7;
                    var tonic = Factory.CreateEntity("HealingTonic"); Assert.True(Player.GetPart<InventoryPart>().AddObject(tonic));
                    var zone = new Zone("InventoryCandidateDetails"); Assert.True(zone.AddEntity(Player, 10, 10));
                    UI = root.AddComponent<InventoryUI>(); UI.PlayerEntity = Player; UI.CurrentZone = zone; UI.EntityFactory = Factory;
                    UI.Tilemap = tiles.GetComponent<Tilemap>();
                }
                catch { Dispose(); throw; }
            }
            public void OpenPicker(int targetSlot)
            { UI.Open(); Set(UI, "_panel", 3); Call(UI, "OpenGrimoirePicker", targetSlot); Assert.NotNull(Get(UI, "_grimoirePicker")); }
            public void Dispose()
            {
                UI?.Close(); if (root != null) Object.DestroyImmediate(root);
                saved?.Dispose(); typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null, oldEquipmentVersion);
            }
        }
    }
}
