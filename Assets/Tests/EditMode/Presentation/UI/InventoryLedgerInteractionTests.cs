using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>CPU checks of the actual retained paint/hit rectangles and production
    /// dispatcher. Camera conversions are real; no OS mouse, GPU or Deck-feel claim.</summary>
    public sealed class InventoryLedgerInteractionTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [TestCase(1280, 800)] [TestCase(1920, 1080)]
        public void ScreenPointUsesThePaintedTabAndExactOwnedItemAtBothViewportShapes(int width, int height)
        {
            using (var r = new Rig(width, height))
            {
                var tonic = r.Carry("HealingTonic"); r.Carry("DriedMeat");
                r.Open();
                int originalCursor = (int)Get(r.UI, "_cursorIndex");
                Assert.AreEqual(-1, r.HitAtScreen(r.ToScreen(new Vector2(8, 8))), "Blank canvas has no semantic action.");
                Assert.AreEqual(0, Get(r.UI, "_panel")); Assert.AreEqual(originalCursor, Get(r.UI, "_cursorIndex"));
                Assert.IsNull(Get(r.UI, "_itemActionPopup"));
                r.ActivatePainted(r.FindHit("Tab", 1));
                Assert.AreEqual(1, Get(r.UI, "_panel"));
                int row = r.InventoryRow(tonic);
                object hit = r.FindHit("Item", row);
                var display = (InventoryScreenData.ItemDisplay)Get(r.Rows("_rows")[row], "Item");
                Assert.That(r.Labels().Any(t => t.Content == display.Name && r.BoundsOf(t).Overlaps(Bounds(hit))),
                    Is.True, "The hit must belong to an actual painted list owner, not only its detail title.");
                r.ActivatePainted(hit);
                var popup = Get(r.UI, "_itemActionPopup");
                Assert.NotNull(popup); Assert.AreSame(tonic, Get(popup, "Item"));
                Assert.True(r.Pack.Contains(tonic)); Assert.AreEqual(1, tonic.GetPart<StackerPart>().StackCount);
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
                Call(r.UI, "CancelLedgerPopup");
                Assert.IsNull(Get(r.UI, "_itemActionPopup")); Assert.AreEqual(row, Get(r.UI, "_cursorIndex"));
                Assert.AreEqual(-1, r.HitAtScreen(r.ToScreen(new Vector2(8, 8))));
                Assert.True(r.UI.IsOpen); Assert.True(r.Pack.Contains(tonic));
                object[] outside = { new Vector2(-width, -height), Vector2.zero };
                Assert.False((bool)CallArgs(r.UI, "LedgerPoint", outside), "Offscreen input must not map into the ledger.");
            }
        }

        [TestCase(1280, 800)] [TestCase(1920, 1080)]
        public void NativeMousePressUsesTheActualPaintedTinkeringTab(int width, int height)
        {
            using (var input = new NativeMouseScope())
            using (var r = new Rig(width, height))
            {
                r.Open();
                Vector2 screen = r.ToScreen(Bounds(r.FindHit("Tab", 2)).center);
                Assert.GreaterOrEqual(r.HitAtScreen(screen), 0, "The native screen point comes from the actually painted tab.");
                input.State(screen);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.AreEqual(0, Get(r.UI, "_panel"), "Hover alone must not activate a tab.");
                input.State(screen, pressed: true);
                Assert.AreSame(input.Mouse, Mouse.current);
                Assert.AreEqual(screen, Mouse.current.position.ReadValue());
                Assert.True(Mouse.current.leftButton.wasPressedThisFrame);
                Assert.True((bool)Call(r.UI, "HandleLedgerPointer"), "Consume the actual Input System mouse press.");
                Assert.AreEqual(2, Get(r.UI, "_panel"), "The native mouse must reach Tinkering even when legacy Input reports another position.");
                Assert.True(r.UI.IsOpen); Assert.False(r.UI.ConsumePendingEverydayTurn());
                Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        [Test]
        public void NativeMouseParkedBeforeOpeningCannotOverrideEquipmentUntilItActuallyMoves()
        {
            using (var input = new NativeMouseScope())
            using (var r = new Rig())
            {
                var tonic = r.Carry("HealingTonic"); r.Open();
                int row = r.InventoryRow(tonic);
                Vector2 parked = r.ToScreen(Bounds(r.FindHit("Item", row)).center);
                // Use the actual painted owner while making the legacy/native
                // mismatch explicit; this is not an injected pointer hit map.
                if (parked == (Vector2)Input.mousePosition) parked += Vector2.right * 4;
                Assert.AreNotEqual((Vector2)Input.mousePosition, parked);
                Assert.AreEqual("Item", Kind(r.Hits()[r.HitAtScreen(parked)]));
                r.UI.Close();
                input.State(parked);
                r.Open();
                Assert.AreEqual(0, Get(r.UI, "_panel"));
                int initialItem = (int)Get(r.UI, "_cursorIndex");
                int initialEquipment = (int)Get(r.UI, "_equipCursorIndex");
                input.State(parked);
                Assert.False(Mouse.current.leftButton.wasPressedThisFrame);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.AreEqual(0, Get(r.UI, "_panel"), "A stationary native pointer must not be mistaken for movement from stale legacy coordinates.");
                Assert.AreEqual(initialItem, Get(r.UI, "_cursorIndex"));
                Assert.AreEqual(initialEquipment, Get(r.UI, "_equipCursorIndex"));

                // Positive counter: the next real movement over that same owner
                // still supports hover selection without executing item actions.
                Vector2 moved = parked + Vector2.right * 4;
                Assert.AreEqual(row, Index(r.Hits()[r.HitAtScreen(moved)]));
                input.State(moved);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.AreEqual(1, Get(r.UI, "_panel")); Assert.AreEqual(row, Get(r.UI, "_cursorIndex"));
                Assert.IsNull(Get(r.UI, "_itemActionPopup")); Assert.True(r.Pack.Contains(tonic));
                Assert.AreEqual(1, tonic.GetPart<StackerPart>().StackCount);
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        [Test]
        public void NativeMouseHeldPressDoesNotRepeatTheActualCraftPick()
        {
            using (var input = new NativeMouseScope())
            using (var r = new Rig())
            {
                var blade = r.Carry("SteelBladeComponent"); r.Open(); r.Tab(4);
                Vector2 screen = r.ToScreen(Bounds(r.FindHit("CraftRow", r.CraftRow(blade))).center);
                input.State(screen);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.False(CraftingMarkPart.IsMarked(blade), "Pointer motion and stationary frames cannot select crafting inputs.");
                input.State(screen, pressed: true);
                Assert.True((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.True(CraftingMarkPart.IsMarked(blade));
                input.State(screen, pressed: true);
                Assert.False(Mouse.current.leftButton.wasPressedThisFrame);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.True(CraftingMarkPart.IsMarked(blade), "Holding the same press cannot toggle the actual input back off.");
                input.State(screen);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.True(CraftingMarkPart.IsMarked(blade));
                input.State(screen, pressed: true);
                Assert.True((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.False(CraftingMarkPart.IsMarked(blade), "A separate release and press toggles the same owned input once.");
                Assert.True(r.Pack.Contains(blade)); Assert.AreEqual(1, r.Pack.Objects.Count);
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        [Test]
        public void NativeMouseWheelMovesSelectionOnceWithoutPickingOrRepeatingOnAStationaryFrame()
        {
            using (var input = new NativeMouseScope())
            using (var r = new Rig())
            {
                var first = r.Carry("SteelBladeComponent", "blade a");
                var second = r.Carry("SteelBladeComponent", "blade b");
                r.Open(); r.Tab(4);
                int firstRow = r.CraftRow(first), secondRow = r.CraftRow(second);
                Assert.Less(firstRow, secondRow);
                Call(r.UI, "SelectLedgerHit", r.FindHit("CraftRow", firstRow)); Call(r.UI, "Render");
                Vector2 screen = r.ToScreen(new Vector2(8, 8));
                Assert.AreEqual(-1, r.HitAtScreen(screen), "Use blank space inside the real canvas so hover cannot reset the wheel selection.");
                input.State(screen);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer")); Assert.AreEqual(firstRow, Get(r.UI, "_craftCursorIndex"));
                input.State(screen, wheel: -120);
                Assert.AreEqual(-120f, Mouse.current.scroll.ReadValue().y);
                Assert.True((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.AreEqual(secondRow, Get(r.UI, "_craftCursorIndex"));
                input.State(screen);
                Assert.False((bool)Call(r.UI, "HandleLedgerPointer"));
                Assert.AreEqual(secondRow, Get(r.UI, "_craftCursorIndex"), "A stationary no-wheel frame must not replay the previous delta.");
                Assert.False(CraftingMarkPart.IsMarked(first)); Assert.False(CraftingMarkPart.IsMarked(second));
                Assert.AreEqual(2, r.Pack.Objects.Count); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void NativeMouseOutsideCanvasDoesNotActivateOrScrollTheCurrentCraftSelection()
        {
            using (var input = new NativeMouseScope())
            using (var r = new Rig())
            {
                var first = r.Carry("SteelBladeComponent", "blade a");
                var second = r.Carry("SteelBladeComponent", "blade b");
                r.Open(); r.Tab(4);
                int row = r.CraftRow(first);
                Call(r.UI, "SelectLedgerHit", r.FindHit("CraftRow", row)); Call(r.UI, "Render");
                var outside = new Vector2(-1280, -800);
                Assert.AreEqual(-1, r.HitAtScreen(outside));
                input.State(outside); Call(r.UI, "HandleLedgerPointer");
                input.State(outside, pressed: true, wheel: -120); Call(r.UI, "HandleLedgerPointer");
                Assert.AreEqual(row, Get(r.UI, "_craftCursorIndex")); Assert.AreEqual(4, Get(r.UI, "_panel"));
                Assert.False(CraftingMarkPart.IsMarked(first)); Assert.False(CraftingMarkPart.IsMarked(second));
                Assert.IsNull(Get(r.UI, "_itemActionPopup")); Assert.True(r.UI.IsOpen);
                Assert.AreEqual(2, r.Pack.Objects.Count); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void LastRealCraftRowStaysPaintedAndClickableAfterNavigationPastTheVisibleWindow()
        {
            using (var r = new Rig())
            {
                var blades = new List<Entity>();
                for (int i = 0; i < 18; i++) blades.Add(r.Carry("SteelBladeComponent", "blade " + i));
                var oak = r.Carry("OakHaftComponent"); var binding = r.Carry("LeatherBindingComponent");
                r.Open(); r.Tab(4);
                var rows = r.Rows("_craftRows"); Assert.Greater(rows.Count, 12);
                int last = r.CraftRow(binding);
                Assert.That(r.Hits().Any(h => Kind(h) == "CraftRow" && Index(h) == last), Is.False,
                    "Precondition: the last actual component starts below the visible window.");
                for (int i = 0; i < rows.Count; i++) { Call(r.UI, "MoveLedgerSelection", 1); Call(r.UI, "Render"); }
                Assert.AreEqual(last, Get(r.UI, "_craftCursorIndex"));
                Assert.Greater((int)Get(r.UI, "_craftScrollOffset"), 0);
                object finalHit = r.FindHit("CraftRow", last);
                Assert.That(r.Labels().Any(t => t.Content.Contains(binding.GetDisplayName()) && r.BoundsOf(t).Overlaps(Bounds(finalHit))), Is.True);
                r.ActivatePainted(finalHit);
                Assert.True(CraftingMarkPart.IsMarked(binding)); Assert.False(CraftingMarkPart.IsMarked(oak));
                Assert.That(blades.All(item => !CraftingMarkPart.IsMarked(item)), Is.True);
                r.ActivatePainted(r.FindHit("CraftRow", r.CraftRow(binding)));
                Assert.False(CraftingMarkPart.IsMarked(binding), "Same row toggles its own pick back off.");
                Assert.AreEqual(20, r.Pack.Objects.Count); Assert.True(r.Pack.Contains(binding));
                Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [Test]
        public void PaintedCraftSectionHeadersAndEmptyNotesNeverOwnItemActions()
        {
            using (var r = new Rig())
            {
                var blade = r.Carry("SteelBladeComponent"); r.Open(); r.Tab(4);
                var rows = r.Rows("_craftRows");
                var header = r.Labels().Single(t => t.Content == "Heads / blades");
                var empty = r.Labels().First(t => t.Content.Trim() == "(none carried)");
                Assert.AreEqual(-1, r.HitAtScreen(r.ToScreen(r.BoundsOf(header).center)));
                Assert.AreEqual(-1, r.HitAtScreen(r.ToScreen(r.BoundsOf(empty).center)));
                foreach (object hit in r.Hits().Where(h => Kind(h) == "CraftRow"))
                    Assert.True((bool)Get(rows[Index(hit)], "IsSelectable"), "Every craft hit must resolve to a selectable actual input.");
                Assert.That(r.Hits().Any(h => Kind(h) == "CraftRow" && Index(h) == r.CraftRow(blade)), Is.True,
                    "Positive control: a real selectable row still has its hit.");
                Assert.False(CraftingMarkPart.IsMarked(blade)); Assert.IsNull(Get(r.UI, "_itemActionPopup"));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void RepaintingCachedCraftingDoesNotNormalizeNewMarksOrRecomputeItsSelection(bool duplicate)
        {
            using (var r = new Rig())
            {
                var steel = r.Carry("SteelBladeComponent"); var iron = r.Carry("IronSpikeComponent");
                var oak = r.Carry("OakHaftComponent"); var binding = r.Carry("LeatherBindingComponent");
                CraftingMarkPart.Toggle(steel); CraftingMarkPart.Toggle(oak); CraftingMarkPart.Toggle(binding);
                r.Open(); r.Tab(4);
                var preview = (ForgePreview)Get(r.UI, "_forgePreview"); Assert.True(preview.IsComplete);
                Assert.AreSame(steel, Get(r.UI, "_pickedBlade"));
                if (duplicate) CraftingMarkPart.Toggle(iron);
                var steelMarker = steel.GetPart<CraftingMarkPart>(); var ironMarker = iron.GetPart<CraftingMarkPart>();
                var originalOrder = r.Pack.Objects.ToArray();
                for (int i = 0; i < 8; i++) Call(r.UI, "Render");
                Assert.AreSame(steelMarker, steel.GetPart<CraftingMarkPart>());
                Assert.AreSame(ironMarker, iron.GetPart<CraftingMarkPart>());
                Assert.AreEqual(duplicate, CraftingMarkPart.IsMarked(iron));
                Assert.AreEqual(preview, Get(r.UI, "_forgePreview")); Assert.AreSame(steel, Get(r.UI, "_pickedBlade"));
                CollectionAssert.AreEqual(originalOrder, r.Pack.Objects);
                Assert.That(originalOrder.All(item => item.GetPart<StackerPart>().StackCount == 1 && item.GetPart<PhysicsPart>().InInventory == r.Player), Is.True);
                Assert.False(r.UI.ConsumePendingEverydayTurn());
                // Counter-control: the existing explicit rebuild still performs its
                // documented normalization. A renderer that calls it would fail above.
                Call(r.UI, "Rebuild");
                Assert.AreEqual(!duplicate, CraftingMarkPart.IsMarked(steel));
                Assert.AreEqual(duplicate, CraftingMarkPart.IsMarked(iron));
                Assert.AreSame(duplicate ? iron : steel, Get(r.UI, "_pickedBlade"));
            }
        }

        [Test]
        public void AllTenActualHotbarOwnersArePaintedAndClearingTheLastLeavesTheOtherNineAlone()
        {
            using (var r = new Rig())
            {
                var previous = r.Player.GetPart<ActivatedAbilitiesPart>(); if (previous != null) r.Player.RemovePart(previous);
                var abilities = new ActivatedAbilitiesPart(); r.Player.AddPart(abilities);
                var ids = new Guid[ActivatedAbilitiesPart.SlotCount];
                for (int slot = 0; slot < ids.Length; slot++)
                    ids[slot] = abilities.AddAbility("Mapped power " + (slot + 1), "LedgerMappedPower" + slot, "Audit");
                abilities.GetAbilityBySlot(9).CooldownRemaining = 7;
                r.Open(); r.Tab(3);
                for (int slot = 0; slot < ids.Length; slot++)
                {
                    string name = abilities.GetAbilityBySlot(slot).DisplayName;
                    object row = r.Hits().First(h => Kind(h) == "AbilitySlot" && Index(h) == slot
                        && r.Labels().Any(t => t.Content == name && r.BoundsOf(t).Overlaps(Bounds(h))));
                    Call(r.UI, "SelectLedgerHit", row); Call(r.UI, "Render");
                    Assert.AreEqual(slot, Get(r.UI, "_abilitiesCursorSlot"));
                    CollectionAssert.AreEqual(ids, abilities.SlotAssignments);
                }
                Assert.AreEqual(7, abilities.GetAbilityBySlot(9).CooldownRemaining);
                r.ActivatePainted(r.FindHit("ClearAbility", 9));
                Assert.AreEqual(Guid.Empty, abilities.SlotAssignments[9]);
                for (int slot = 0; slot < 9; slot++) Assert.AreEqual(ids[slot], abilities.SlotAssignments[slot]);
                Assert.AreEqual(7, abilities.GetAbility(ids[9]).CooldownRemaining, "Clearing a binding does not alter the ability itself.");
                Assert.That(r.Hits().Any(h => Kind(h) == "ClearAbility"), Is.False, "An empty selected slot has no clear action.");
                Assert.That(r.Labels().Any(t => t.Content == "Empty" && r.Hits().Any(h => Kind(h) == "AbilitySlot" && Index(h) == 9 && r.BoundsOf(t).Overlaps(Bounds(h)))), Is.True);
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        [TestCase(0)] [TestCase(1)]
        public void DisplacementPreviewIdentifiesTheExactHandWhenBothWeaponsHaveTheSameName(int handIndex)
        {
            using (var r = new Rig())
            {
                var hands = r.Player.GetPart<Body>().GetPartsByType("Hand");
                Assert.AreEqual(2, hands.Count);
                Assert.AreNotEqual(hands[0].GetDisplayName(), hands[1].GetDisplayName());
                var blades = new[] { r.Carry("Dagger", "matching dagger"), r.Carry("Dagger", "matching dagger") };
                for (int i = 0; i < 2; i++) Assert.True(r.Pack.EquipToBodyPart(blades[i], hands[i]));
                var candidate = r.Carry("LongSword", "candidate long sword");
                var carriedBefore = r.Pack.Objects.ToArray();
                r.Open(); OpenManualDisplacement(r, candidate, hands[handIndex]);
                var popup = Get(r.UI, "_displaceConfirm"); Assert.NotNull(popup);
                var displaced = (List<InventorySystem.Displacement>)Get(popup, "Displacements");
                Assert.AreEqual(1, displaced.Count); Assert.AreSame(blades[handIndex], displaced[0].Item);
                Assert.AreSame(hands[handIndex], displaced[0].BodyPart);
                Assert.True(HasPaintedDisplacement(r, displaced[0]),
                    "The confirmation must pair the identical weapon name with its actual displaced hand, not just show the name.");
                Assert.False(HasPaintedDisplacement(r, new InventorySystem.Displacement
                    { Item = blades[1 - handIndex], BodyPart = hands[1 - handIndex] }), "Do not name the unaffected hand as displaced.");
                CollectionAssert.AreEqual(carriedBefore, r.Pack.Objects);
                for (int i = 0; i < 2; i++) Assert.AreSame(blades[i], hands[i]._Equipped);
                Assert.AreSame(r.Player, candidate.GetPart<PhysicsPart>().InInventory);
                Assert.IsNull(candidate.GetPart<PhysicsPart>().Equipped);
                Assert.False(r.UI.ConsumePendingEverydayTurn());

                r.ActivatePainted(r.FindHit("DisplaceNo", 0));
                Assert.IsNull(Get(r.UI, "_displaceConfirm"));
                CollectionAssert.AreEqual(carriedBefore, r.Pack.Objects);
                for (int i = 0; i < 2; i++) Assert.AreSame(blades[i], hands[i]._Equipped);
                OpenManualDisplacement(r, candidate, hands[handIndex]);
                r.ActivatePainted(r.FindHit("DisplaceYes", 0));
                Assert.IsNull(Get(r.UI, "_displaceConfirm"));
                Assert.AreSame(candidate, hands[handIndex]._Equipped);
                Assert.AreSame(blades[1 - handIndex], hands[1 - handIndex]._Equipped);
                Assert.AreSame(r.Player, blades[handIndex].GetPart<PhysicsPart>().InInventory);
                Assert.IsNull(blades[handIndex].GetPart<PhysicsPart>().Equipped);
                Assert.AreSame(r.Player, candidate.GetPart<PhysicsPart>().Equipped);
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        [TestCase(8)] [TestCase(9)]
        public void ActualDisplacementsExposeEveryItemAndBodyPartBeforeConfirmation(int handCount)
        {
            using (var r = new Rig())
            {
                var body = r.Player.GetPart<Body>();
                for (int i = 0; i < handCount - 2; i++)
                    Assert.NotNull(body.AddPartByManager("ledger-extra-hands", body.GetBody(), "Hand", name: "extra hand " + (i + 1)));
                var hands = body.GetPartsByType("Hand"); Assert.AreEqual(handCount, hands.Count);
                var blades = new List<Entity>();
                for (int i = 0; i < hands.Count; i++)
                {
                    var blade = r.Carry("Dagger", "slot blade " + (i + 1)); blades.Add(blade);
                    Assert.True(r.Pack.EquipToBodyPart(blade, hands[i]));
                }
                var candidate = r.Carry("Dagger", "many-grip candidate");
                candidate.GetPart<EquippablePart>().UsesSlots = string.Join(",", Enumerable.Repeat("Hand", handCount));
                var expected = InventorySystem.PreviewDisplacements(r.Player, candidate);
                Assert.AreEqual(handCount, expected.Count, "Use an actual supported multi-slot equip plan, not invented popup entries.");
                var carriedBefore = r.Pack.Objects.ToArray();
                r.Open(); Assert.True(r.UI.ReopenItemActionPopupFor(candidate));
                r.ActivatePainted(r.FindHit("PopupRow", ActionIndex(r, "equip_auto")));
                var popup = Get(r.UI, "_displaceConfirm"); Assert.NotNull(popup);
                var actual = (List<InventorySystem.Displacement>)Get(popup, "Displacements");
                Assert.AreEqual(handCount, actual.Count);
                foreach (var displacement in actual.Take(8))
                    Assert.True(HasPaintedDisplacement(r, displacement),
                        "The visible summary must identify each shown consequence: " + displacement.Item.GetDisplayName()
                        + " in " + displacement.BodyPart.GetDisplayName());
                if (handCount > 8)
                {
                    Assert.That(r.Labels().Any(label => label.Content.Contains("1")
                        && label.Content.IndexOf("all replacements", StringComparison.OrdinalIgnoreCase) >= 0), Is.True,
                        "The truncated summary must offer the complete consequence reader.");
                    string details = r.UI.SelectedDecisionDetails();
                    Assert.NotNull(details);
                    foreach (var displacement in actual)
                        Assert.True(details.Split('\n').Any(line => line.IndexOf(displacement.Item.GetDisplayName(), StringComparison.OrdinalIgnoreCase) >= 0
                            && line.IndexOf(displacement.BodyPart.GetDisplayName(), StringComparison.OrdinalIgnoreCase) >= 0),
                            "F1 must expose every real item/slot pair, including the ninth: " + displacement.Item.GetDisplayName()
                            + " in " + displacement.BodyPart.GetDisplayName());
                    Assert.IsNull(r.UI.SelectedDecisionPresentation(out string presented), "A multi-owner consequence list has no single item portrait.");
                    Assert.AreEqual(details, presented);
                }
                for (int i = 0; i < hands.Count; i++) Assert.AreSame(blades[i], hands[i]._Equipped);
                CollectionAssert.AreEqual(carriedBefore, r.Pack.Objects);
                r.ActivatePainted(r.FindHit("DisplaceNo", 0));
                Assert.IsNull(Get(r.UI, "_displaceConfirm"));
                for (int i = 0; i < hands.Count; i++) Assert.AreSame(blades[i], hands[i]._Equipped);
                CollectionAssert.AreEqual(carriedBefore, r.Pack.Objects);
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        static int ActionIndex(Rig r, string command)
        {
            var actions = (IList)Get(Get(r.UI, "_itemActionPopup"), "Actions");
            for (int i = 0; i < actions.Count; i++) if ((string)Get(actions[i], "Command") == command) return i;
            Assert.Fail("Expected actual inventory action: " + command); return -1;
        }
        static void OpenManualDisplacement(Rig r, Entity candidate, BodyPart hand)
        {
            Assert.True(r.UI.ReopenItemActionPopupFor(candidate));
            r.ActivatePainted(r.FindHit("PopupRow", ActionIndex(r, "equip_manual")));
            var popup = Get(r.UI, "_itemActionPopup"); Assert.True((bool)Get(popup, "InBodyPartPicker"));
            var parts = (List<BodyPart>)Get(popup, "BodyParts"); int index = parts.IndexOf(hand); Assert.GreaterOrEqual(index, 0);
            r.ActivatePainted(r.FindHit("PopupRow", index));
        }
        static bool HasPaintedDisplacement(Rig r, InventorySystem.Displacement displacement)
        {
            // Read only the actual modal region, excluding the background equipment
            // rail. Accept one line or two aligned columns, but reject clipped text.
            var labels = r.Labels().Where(label =>
            {
                Rect rect = r.BoundsOf(label);
                return rect.xMin >= 250 && rect.xMax <= 1030 && rect.yMin >= 142 && rect.yMax <= 698
                    && label.Content.Split('\n').All(line => line.Length * label.GlyphHeight * .5f <= rect.width + .1f);
            }).ToList();
            foreach (var name in labels)
                if (name.Content.IndexOf(displacement.Item.GetDisplayName(), StringComparison.OrdinalIgnoreCase) >= 0)
                    foreach (var slot in labels)
                        if (Mathf.Abs(r.BoundsOf(name).yMin - r.BoundsOf(slot).yMin) < 3f
                            && slot.Content.IndexOf(displacement.BodyPart.GetDisplayName(), StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
            return false;
        }

        static object Get(object owner, string name)
        { var field = owner.GetType().GetField(name, Members); Assert.NotNull(field, name); return field.GetValue(owner); }
        static object Call(object owner, string name, params object[] args) => CallArgs(owner, name, args);
        static object CallArgs(object owner, string name, object[] args)
        { var method = owner.GetType().GetMethod(name, Members); Assert.NotNull(method, name); return method.Invoke(owner, args); }
        static string Kind(object hit) => Get(hit, "Kind").ToString();
        static int Index(object hit) => (int)Get(hit, "Index");
        static Rect Bounds(object hit) => (Rect)Get(hit, "Bounds");

        sealed class NativeMouseScope : IDisposable
        {
            readonly InputTestFixture input = new InputTestFixture();
            public readonly Mouse Mouse;
            public NativeMouseScope()
            {
                input.Setup();
                try { Mouse = InputSystem.AddDevice<Mouse>(); Mouse.MakeCurrent(); }
                catch { input.TearDown(); throw; }
            }
            public void State(Vector2 position, bool pressed = false, float wheel = 0)
            {
                var state = new MouseState { position = position, scroll = new Vector2(0, wheel) };
                if (pressed) state = state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(Mouse, state); InputSystem.Update();
            }
            public void Dispose() => input.TearDown();
        }

        sealed class Rig : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly int oldEquipmentVersion = EquipmentChangeBus.GlobalVersion;
            readonly GameObject root;
            readonly RenderTexture display;
            public readonly Camera Camera;
            public readonly InventoryUI UI;
            public readonly EntityFactory Factory;
            public readonly Entity Player;
            public InventoryPart Pack => Player.GetPart<InventoryPart>();
            RectTransform Content => (RectTransform)Get(UI, "_ledgerContent");
            public Rig(int width = 1280, int height = 800)
            {
                saved = new HotbarSaveFixture(false, false);
                try
                {
                    root = new GameObject("Ledger interaction fixture");
                    var cameraHost = Child("Ledger camera"); cameraHost.tag = "MainCamera";
                    Camera = cameraHost.AddComponent<Camera>(); Camera.orthographic = true; Camera.orthographicSize = 22.5f;
                    Camera.transform.position = new Vector3(40, 22.5f, -10);
                    display = new RenderTexture(width, height, 0); Camera.targetTexture = display; Camera.aspect = (float)width / height;
                    var grid = Child("Ledger grid"); grid.AddComponent<Grid>();
                    var tiles = new GameObject("Ledger tiles", typeof(Tilemap)); tiles.transform.SetParent(grid.transform, false);
                    Factory = new EntityFactory(); Factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                    Player = Factory.CreateEntity("Player"); Assert.NotNull(Player); Pack.MaxWeight = -1;
                    var zone = new Zone("InventoryLedgerInteraction"); Assert.True(zone.AddEntity(Player, 10, 10));
                    zone.GetCell(10, 10).IsVisible = zone.GetCell(10, 10).Explored = true;
                    UI = root.AddComponent<InventoryUI>(); UI.PlayerEntity = Player; UI.CurrentZone = zone;
                    UI.EntityFactory = Factory; UI.Tilemap = tiles.GetComponent<Tilemap>();
                }
                catch { Dispose(); throw; }
            }
            GameObject Child(string name) { var child = new GameObject(name); child.transform.SetParent(root.transform, false); return child; }
            public Entity Carry(string blueprint, string name = null)
            {
                var item = Factory.CreateEntity(blueprint); Assert.NotNull(item);
                item.GetPart<StackerPart>().MaxStack = 1; item.GetPart<StackerPart>().StackCount = 1;
                if (name != null) item.GetPart<RenderPart>().DisplayName = name;
                Assert.True(Pack.AddObject(item)); Assert.True(Pack.Contains(item)); return item;
            }
            public void Open() { UI.Open(); Canvas.ForceUpdateCanvases(); Assert.True(UI.IsOpen); Assert.AreSame(Camera, UI.GetComponentInChildren<Canvas>().worldCamera); }
            public void Tab(int index) => ActivatePainted(FindHit("Tab", index));
            public IList Rows(string name) => (IList)Get(UI, name);
            public List<object> Hits() => ((IEnumerable)Get(UI, "_ledgerHits")).Cast<object>().ToList();
            public object FindHit(string kind, int index)
            { var matches = Hits().Where(h => Kind(h) == kind && Index(h) == index).ToList(); Assert.AreEqual(1, matches.Count, kind + " index " + index); return matches[0]; }
            public IEnumerable<SkillScreenText> Labels() => UI.GetComponentsInChildren<SkillScreenText>(true).Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.Content));
            public Rect BoundsOf(SkillScreenText label)
            {
                Canvas.ForceUpdateCanvases();
                var b = RectTransformUtility.CalculateRelativeRectTransformBounds(Content, label.rectTransform);
                return new Rect(b.min.x - Content.rect.xMin, Content.rect.yMax - b.max.y, b.size.x, b.size.y);
            }
            public Vector2 ToScreen(Vector2 logical)
            {
                Canvas.ForceUpdateCanvases();
                var local = new Vector3(Content.rect.xMin + logical.x, Content.rect.yMax - logical.y, 0);
                return RectTransformUtility.WorldToScreenPoint(Camera, Content.TransformPoint(local));
            }
            public int HitAtScreen(Vector2 screen)
            {
                object[] args = { screen, Vector2.zero };
                if (!(bool)CallArgs(UI, "LedgerPoint", args)) return -1;
                return (int)Call(UI, "LedgerHitAt", (Vector2)args[1]);
            }
            public void ActivatePainted(object expected)
            {
                int actualIndex = HitAtScreen(ToScreen(Bounds(expected).center)); Assert.GreaterOrEqual(actualIndex, 0);
                var actual = Hits()[actualIndex]; Assert.AreEqual(Kind(expected), Kind(actual)); Assert.AreEqual(Index(expected), Index(actual));
                Call(UI, "ActivateLedgerHit", actual); Canvas.ForceUpdateCanvases();
            }
            public int InventoryRow(Entity item)
            {
                var rows = Rows("_rows");
                for (int i = 0; i < rows.Count; i++)
                    if (Get(rows[i], "Item") is InventoryScreenData.ItemDisplay displayItem && displayItem.Item == item) return i;
                Assert.Fail("Expected actual carried row"); return -1;
            }
            public int CraftRow(Entity item)
            { var rows = Rows("_craftRows"); for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Get(rows[i], "Item"), item)) return i; Assert.Fail("Expected actual craft input row"); return -1; }
            public void Dispose()
            {
                UI?.Close(); if (Camera != null) Camera.targetTexture = null;
                if (root != null) Object.DestroyImmediate(root); if (display != null) Object.DestroyImmediate(display);
                saved?.Dispose(); typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null, oldEquipmentVersion);
            }
        }
    }
}
