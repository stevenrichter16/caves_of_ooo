using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CavesOfOoo.Tests
{
    public sealed class DialogueLedgerPresentationTests : ShortcutFixture
    {
        private const string Reply = "I came about the northern crossing.";
        private const string LongReply = "The ordinary crossing will still be open for the rest of the day. "
            + "I can also wait here for the returned porter with the dispatch and tell the next travelers.";

        private DialogueUI OpenLedger(int lines = 1, int choices = 2, bool allowEscape = true, Camera popupCamera = null)
        {
            var conversation = new ConversationData { ID = "LedgerDialogueFixture" };
            conversation.Nodes.Add(new NodeData
            {
                ID = "Start",
                Text = string.Join("\n", Enumerable.Range(1, lines).Select(i => $"Report {i:D2}. The post is beyond the well.")),
                AllowEscape = allowEscape,
                Choices = Enumerable.Range(0, choices).Select(i => new ChoiceData
                {
                    Text = i == 0 ? Reply : i == 1 ? LongReply : $"Destination {i:D2}",
                    Target = $"Choice{i}"
                }).ToList()
            });
            for (int i = 0; i < choices; i++)
                conversation.Nodes.Add(new NodeData
                {
                    ID = $"Choice{i}", Text = "Here is the short answer.",
                    Choices = new List<ChoiceData> { new ChoiceData { Text = "Back to the report.", Target = "Start" } }
                });
            ConversationLoader.Register(conversation);
            var speaker = new Entity { BlueprintName = "LedgerSpeaker" };
            speaker.AddPart(new ConversationPart { ConversationID = conversation.ID });
            speaker.AddPart(new RenderPart { DisplayName = "Ivrin of the North Post", RenderString = "I" });
            Assert.True(ConversationManager.StartConversation(speaker, Player));
            var ui = Component<DialogueUI>();
            ui.PlayerEntity = Player;
            ui.CurrentZone = Zone;
            ui.PopupCamera = popupCamera;
            typeof(DialogueUI).GetField("UseLedgerView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(ui, true);
            ui.Open();
            return ui;
        }

        private static Text[] Labels(DialogueUI dialogue) => dialogue.GetComponentsInChildren<Text>(false)
            .Where(label => label.isActiveAndEnabled && !string.IsNullOrWhiteSpace(label.text)).ToArray();

        private static string VisibleText(DialogueUI dialogue) => string.Join("\n", Labels(dialogue).Select(label => label.text));

        private static Vector2 ScreenCenter(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private static RectTransform Card(DialogueUI ui) => ui.GetComponentsInChildren<RectTransform>(false)
            .Single(rect => rect.name == "DialogueCard");

        [Test]
        public void CardFollowsTheGameplayViewportCenterOnBothAxes()
        {
            var camera = OutsideCamera();
            camera.rect = new Rect(.07f, .18f, .68f, .73f);
            var ui = OpenLedger(popupCamera: camera);
            Canvas.ForceUpdateCanvases();
            var card = Card(ui);
            Assert.That(card.anchorMin, Is.EqualTo(camera.rect.center));
            Assert.That(card.anchorMax, Is.EqualTo(camera.rect.center));

            camera.rect = new Rect(.12f, .26f, .60f, .61f);
            Call(ui, "LateUpdate");
            Assert.That(card.anchorMin, Is.EqualTo(camera.rect.center));
            Assert.That(card.anchorMax, Is.EqualTo(camera.rect.center));
            Assert.That(VisibleText(ui), Does.Contain(Reply));
        }

        [Test]
        public void CardAndReplyHitboxFitInsideANarrowGameplayViewport()
        {
            var camera = OutsideCamera();
            camera.rect = new Rect(.18f, .28f, .40f, .52f);
            var ui = OpenLedger(lines: 30, choices: 12, popupCamera: camera);
            Canvas.ForceUpdateCanvases();
            Call(ui, "LateUpdate");
            var card = Card(ui);
            var canvas = card.GetComponentInParent<Canvas>();
            Rect display = canvas.pixelRect;
            Vector3[] corners = new Vector3[4];
            card.GetWorldCorners(corners);
            Vector2 lower = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 upper = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            Assert.That(lower.x, Is.GreaterThan(display.xMin + camera.rect.xMin * display.width));
            Assert.That(lower.y, Is.GreaterThan(display.yMin + camera.rect.yMin * display.height));
            Assert.That(upper.x, Is.LessThan(display.xMin + camera.rect.xMax * display.width));
            Assert.That(upper.y, Is.LessThan(display.yMin + camera.rect.yMax * display.height));
            var reply = Labels(ui).Single(label => label.text == Reply);
            Assert.That(Call(ui, "GetLedgerChoiceRowAtScreen", ScreenCenter(reply.rectTransform)), Is.EqualTo(0));
        }

        [Test]
        public void SpeechAndCompleteChoicesUseProportionalScreenTextWithoutTheTilemap()
        {
            var ui = OpenLedger();
            Press(Key.Space, ui.HandleInput);
            var labels = Labels(ui);
            Assert.That(labels.Any(t => t.text.Contains("Ivrin of the North Post") && t.font != null && t.font.dynamic));
            Assert.That(VisibleText(ui), Does.Contain("Report 01").And.Contain(Reply).And.Contain("tell the next travelers."));
            Assert.That(ui.GetComponentsInChildren<Canvas>(false).Single().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        }

        [Test]
        public void RevealThenSelectAndClosePreserveTheConversationLifecycle()
        {
            var ui = OpenLedger();
            Press(Key.A, ui.HandleInput);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Start"));
            Assert.That(VisibleText(ui), Does.Contain("Report 01"));
            Press(Key.A, ui.HandleInput);
            Press(Key.Space, ui.HandleInput);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Choice0"));
            Assert.That(VisibleText(ui), Does.Contain("Here is the short answer.").And.Not.Contain(LongReply));
            Press(Key.Escape, ui.HandleInput);
            Assert.That(ui.IsOpen, Is.False);
            Assert.That(ConversationManager.IsActive, Is.False);
            Assert.That(Labels(ui), Is.Empty);
        }

        [Test]
        public void LongSpeechIsReachableThroughPageControlsWithoutChoosingAReply()
        {
            var ui = OpenLedger(lines: 50, choices: 12);
            Press(Key.Space, ui.HandleInput);
            Assert.That(VisibleText(ui), Does.Contain("Report 01").And.Not.Contain("Report 50"));
            for (int i = 0; i < 55 && !VisibleText(ui).Contains("Report 50"); i++)
            {
                Press(Key.PageDown, ui.HandleInput);
                if ((bool)Get(ui, "_revealing")) Press(Key.Space, ui.HandleInput);
            }
            Assert.That(VisibleText(ui), Does.Contain("Report 50"));
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Start"));
            Assert.That(ConversationManager.PendingAttackTarget, Is.Null);
            Assert.That(ConversationManager.PendingTradePartner, Is.Null);
        }

        [Test]
        public void ScrolledChoiceAndScreenHitMapUseTheSameVisibleAbsoluteIndex()
        {
            var ui = OpenLedger(choices: 12);
            Press(Key.Space, ui.HandleInput);
            const Key lastKey = Key.N;
            Press(lastKey, ui.HandleInput);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Start"));
            for (int i = 0; i < 11; i++) Press(Key.DownArrow, ui.HandleInput);
            var target = Labels(ui).Single(t => t.text.Contains("Destination 11"));
            var point = ScreenCenter(target.rectTransform);
            Assert.That(Call(ui, "GetLedgerChoiceRowAtScreen", point), Is.EqualTo(11));
            Assert.That(Call(ui, "GetLedgerChoiceRowAtScreen", new Vector2(-30, -30)), Is.EqualTo(-1));
            Press(lastKey, ui.HandleInput);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Choice11"));
        }

        [Test]
        public void MouseWheelAndClickCanChooseAReplyBeyondTheInitialWindow()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            var ui = OpenLedger(choices: 12);
            Press(Key.Space, ui.HandleInput);
            Release();

            var firstReply = Labels(ui).Single(t => t.text == Reply);
            var pointer = ScreenCenter(firstReply.rectTransform);
            for (int i = 0; i < 4; i++) MouseFrame(mouse, ui, pointer, -120);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Start"));
            Assert.That(VisibleText(ui), Does.Not.Contain(Reply));

            var lastReply = Labels(ui).Single(t => t.text == "Destination 11");
            pointer = ScreenCenter(lastReply.rectTransform);
            MouseFrame(mouse, ui, pointer, 0, click: true);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Choice11"));
        }

        private static void MouseFrame(Mouse mouse, DialogueUI ui, Vector2 pointer, float wheel, bool click = false)
        {
            var state = new MouseState { position = pointer, scroll = new Vector2(0, wheel) };
            InputSystem.QueueStateEvent(mouse, click ? state.WithButton(MouseButton.Left) : state);
            InputSystem.Update();
            ui.HandleInput();
        }

        [Test]
        public void ControllerNavigationAndConfirmSelectTheHighlightedReply()
        {
            var ui = OpenLedger();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            gamepad.MakeCurrent();
            NativeGamepadInput.SetContext(GamepadInputContext.Menu);
            Press(Key.Space, ui.HandleInput);
            Assert.That(VisibleText(ui), Does.Contain(Reply));
            Release();

            GamepadFrame(gamepad, ui, new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(Get(ui, "_cursorIndex"), Is.EqualTo(1));
            GamepadFrame(gamepad, ui, new GamepadState());
            GamepadFrame(gamepad, ui, new GamepadState().WithButton(GamepadButton.South));
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Choice1"));
        }

        private static void GamepadFrame(Gamepad gamepad, DialogueUI ui, GamepadState state)
        {
            InputSystem.QueueStateEvent(gamepad, state);
            InputSystem.Update();
            ui.HandleInput();
        }

        [Test]
        public void UnescapableNodeDoesNotAdvertiseAnEscapeControl()
        {
            var ui = OpenLedger(allowEscape: false);
            Press(Key.Space, ui.HandleInput);
            Assert.That(VisibleText(ui), Does.Contain(Reply).And.Not.Contain("Esc"));
            Press(Key.Escape, ui.HandleInput);
            Assert.That(ui.IsOpen, Is.True);
            Assert.That(ConversationManager.CurrentNode.ID, Is.EqualTo("Start"));
        }
    }
}
