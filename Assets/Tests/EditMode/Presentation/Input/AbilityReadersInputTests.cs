using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class AbilityReadersInputTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        InputHandler _input; Entity _actor; TurnManager _turns;
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        [SetUp] public void SetUp()
        {
            AbilityClarityTestSupport.Reset(); _actor = AbilityClarityTestSupport.Actor();
            var zone = new Zone("reader UI"); zone.AddEntity(_actor, 10, 10);
            zone.GetCell(10, 10).IsVisible = zone.GetCell(10, 10).Explored = true;
            _turns = new TurnManager(); _turns.AddEntity(_actor); _turns.ProcessUntilPlayerTurn();
            _input = Make<InputHandler>(); _input.PlayerEntity = _actor; _input.CurrentZone = zone; _input.TurnManager = _turns;
            _input.SkillsScreenUI = Make<SkillsScreenUI>(); _input.AbilityManagerUI = Make<AbilityManagerUI>(); _input.AnnouncementUI = Make<AnnouncementUI>();
        }
        [TearDown] public void TearDown()
        {
            foreach (var obj in _objects) UnityEngine.Object.DestroyImmediate(obj); _objects.Clear();
            SkillRegistry.ResetForTests(); ResonanceSystem.ResetForTests(); MessageLog.Clear();
        }
        T Make<T>() where T : Component { var obj = new GameObject(typeof(T).Name); _objects.Add(obj); return obj.AddComponent<T>(); }
        object Call(string method, params object[] args)
        {
            var info = typeof(InputHandler).GetMethod(method, Private); Assert.NotNull(info, "Missing clarity input: " + method);
            return info.Invoke(_input, args);
        }
        string State => typeof(InputHandler).GetField("_inputState", Private).GetValue(_input).ToString();
        void SetState(string name)
        { var field = typeof(InputHandler).GetField("_inputState", Private); field.SetValue(_input, Enum.Parse(field.FieldType, name)); }
        void Cursor(object ui, int index, int scroll)
        {
            ui.GetType().GetField("_cursorIndex", Private).SetValue(ui, index);
            ui.GetType().GetField("_scrollOffset", Private).SetValue(ui, scroll);
        }
        int Scroll(object ui) => (int)ui.GetType().GetField("_scrollOffset", Private).GetValue(ui);
        void CloseReader() { _input.AnnouncementUI.Close(); Call("CloseAnnouncement"); }
        void AssertNoTurn(int tick, int energy)
        { Assert.AreEqual(tick, _turns.TickCount); Assert.AreEqual(energy, _turns.GetEnergy(_actor)); Assert.True(_turns.WaitingForInput); }

        [Test] public void PurchaseDetailsReaderPaginatesFullTextAndRestoresExactRowScrollWithoutBuying()
        {
            Call("OpenSkillsScreen"); var ui = _input.SkillsScreenUI;
            var snapshot = SkillsScreenStateBuilder.Build(_actor); int index = snapshot.Rows.ToList().FindIndex(r => r.Class == "Rites_HangingBolt");
            Assert.Greater(index, 14); SkillRegistry.TryGetPowerByClass("Rites_HangingBolt", out var power);
            power.Description = string.Join(" ", Enumerable.Repeat("Full purchase description remains available.", 100)) + " END_OF_DETAILS";
            ui.RefreshSnapshot(); Cursor(ui, index, index - 5);
            int tick = _turns.TickCount, energy = _turns.GetEnergy(_actor), scroll = Scroll(ui);
            Call("OpenSkillsDetailsReader"); Assert.AreEqual("AnnouncementOpen", State); Assert.True(_input.AnnouncementUI.IsOpen);
            Assert.Greater(_input.AnnouncementUI.PageCount, 1); var all = new List<string>();
            for (int i = 0; i < _input.AnnouncementUI.PageCount; i++) { _input.AnnouncementUI.GoToPage(i); all.AddRange(_input.AnnouncementUI.VisibleLines); }
            StringAssert.Contains("END_OF_DETAILS", string.Join(" ", all)); CloseReader();
            Assert.AreEqual("SkillsScreenOpen", State); Assert.True(ui.IsOpen); Assert.AreEqual(index, ui.CursorIndex); Assert.AreEqual(scroll, Scroll(ui));
            Assert.AreEqual(100, _actor.GetStatValue("SP")); Assert.False(_actor.GetPart<SkillsPart>().HasSkill(power.Class)); AssertNoTurn(tick, energy);
        }
        [Test] public void AbilityDetailsReaderReturnsWithoutCallingActivationCallbackOrChangingBindings()
        {
            var skills = _actor.GetPart<SkillsPart>(); skills.AddSkill(new Rites_HangingBolt());
            var abilities = _actor.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < 18; i++) abilities.AddAbility("Ability " + i.ToString("00"), "CommandReader" + i, "Rites");
            int calls = 0; _input.AbilityManagerUI.Open(_actor, id => calls++); SetState("AbilityManagerOpen");
            var rows = AbilityManagerStateBuilder.Build(_actor); int index = rows.Rows.ToList().FindIndex(r => r.AbilityID == _actor.GetPart<Rites_HangingBolt>().ActivatedAbilityID);
            Cursor(_input.AbilityManagerUI, index, index - 4);
            var slots = (Guid[])abilities.SlotAssignments.Clone(); int tick = _turns.TickCount, energy = _turns.GetEnergy(_actor);
            Call("OpenAbilityDetailsReader"); Assert.AreEqual("AnnouncementOpen", State); CloseReader();
            Assert.AreEqual("AbilityManagerOpen", State); Assert.AreEqual(index, _input.AbilityManagerUI.CursorIndex);
            Assert.AreEqual(index - 4, Scroll(_input.AbilityManagerUI)); Assert.Zero(calls); CollectionAssert.AreEqual(slots, abilities.SlotAssignments); AssertNoTurn(tick, energy);
        }
        [Test] public void OptionalDirectionPreviewAndCancelNeverEnterCastDirectionState()
        {
            _actor.GetPart<SkillsPart>().AddSkill(new Rites_HangingBolt()); Call("OpenAbilityManager");
            int tick = _turns.TickCount, energy = _turns.GetEnergy(_actor);
            Call("BeginSelectedRitePreview"); Assert.AreEqual("AwaitingRitePreviewDirection", State);
            Call("CancelRitePreview"); Assert.AreEqual("AbilityManagerOpen", State);
            Call("BeginSelectedRitePreview"); Call("ShowRitePreviewDirection", 1, 0);
            Assert.AreEqual("AnnouncementOpen", State); CloseReader(); Assert.AreEqual("AbilityManagerOpen", State);
            AssertNoTurn(tick, energy); Assert.Zero(_actor.GetPart<ActivatedAbilitiesPart>().AbilityList[0].CooldownRemaining);
        }
        [Test] public void RadiusPreviewOpensReaderWithoutMandatoryDirectionOrCast()
        {
            _actor.GetPart<SkillsPart>().AddSkill(new Rites_StormAnvil()); Call("OpenAbilityManager");
            int tick = _turns.TickCount, energy = _turns.GetEnergy(_actor);
            Call("BeginSelectedRitePreview"); Assert.AreEqual("AnnouncementOpen", State); CloseReader();
            Assert.AreEqual("AbilityManagerOpen", State); AssertNoTurn(tick, energy);
        }
        [Test] public void NonRitePreviewLeavesManagerUsableAndDoesNotActivate()
        {
            _actor.GetPart<SkillsPart>().AddSkill(new Spellcraft_Calm()); Call("OpenAbilityManager");
            int tick = _turns.TickCount, energy = _turns.GetEnergy(_actor);
            Call("BeginSelectedRitePreview"); Assert.AreEqual("AbilityManagerOpen", State); Assert.False(_input.AnnouncementUI.IsOpen); AssertNoTurn(tick, energy);
        }
        [Test] public void RemovedSelectedAbilityWhileReadingDoesNotResurrectOrCastIt()
        {
            var rite = new Rites_HangingBolt(); _actor.GetPart<SkillsPart>().AddSkill(rite); Call("OpenAbilityManager");
            Call("OpenAbilityDetailsReader"); _actor.GetPart<SkillsPart>().RemoveSkill(rite); CloseReader();
            Assert.AreEqual("AbilityManagerOpen", State); Assert.Zero(_input.AbilityManagerUI.RowCount);
            Assert.IsNull(_actor.GetPart<ActivatedAbilitiesPart>().GetAbility(rite.ActivatedAbilityID));
        }
        [TestCase("skills", false)] [TestCase("skills", true)]
        [TestCase("ability", false)] [TestCase("ability", true)]
        [TestCase("rite", false)] [TestCase("rite", true)]
        public void OptionalReadersDoNotPolluteCombatLogOrConsumePendingWorldAnnouncements(string kind, bool pending)
        {
            const string marker = "DETAIL_READER_SENTINEL";
            string skillClass = kind == "rite" ? "Rites_StormAnvil" : "Rites_HangingBolt";
            SkillRegistry.TryGetPowerByClass(skillClass, out var power);
            power.Description = marker + " A complete explanation belongs in the optional reader.";
            if (kind == "skills")
            {
                Call("OpenSkillsScreen");
                int row = SkillsScreenStateBuilder.Build(_actor).Rows.ToList().FindIndex(r => r.Class == skillClass);
                Cursor(_input.SkillsScreenUI, row, Math.Max(0, row - 5));
            }
            else
            {
                _actor.GetPart<SkillsPart>().AddSkill(kind == "rite" ? (BaseSkillPart)new Rites_StormAnvil() : new Rites_HangingBolt());
                Call("OpenAbilityManager");
            }
            MessageLog.Add("Existing combat observation.");
            if (pending) MessageLog.AddAnnouncement("Queued world announcement.");
            var messages = MessageLog.GetAllEntries();
            var queue = MessageLog.GetPendingAnnouncementsSnapshot(); int flash = MessageLog.FlashStamp;
            int tick = _turns.TickCount, energy = _turns.GetEnergy(_actor);
            Call(kind == "skills" ? "OpenSkillsDetailsReader" : kind == "ability" ? "OpenAbilityDetailsReader" : "BeginSelectedRitePreview");
            Assert.AreEqual("AnnouncementOpen", State);
            var lines = new List<string>();
            for (int page = 0; page < _input.AnnouncementUI.PageCount; page++)
            { _input.AnnouncementUI.GoToPage(page); lines.AddRange(_input.AnnouncementUI.VisibleLines); }
            StringAssert.Contains(marker, string.Join(" ", lines), "The selected details, not a pre-existing notice, must be opened.");
            CollectionAssert.AreEqual(messages, MessageLog.GetAllEntries(), "Reading must not append its prose to the combat log.");
            CollectionAssert.AreEqual(queue, MessageLog.GetPendingAnnouncementsSnapshot(), "World notices stay queued in their original order.");
            Assert.AreEqual(flash, MessageLog.FlashStamp, "Optional reading is not a new critical event.");
            CloseReader();
            if (pending)
            {
                Assert.AreEqual("AnnouncementOpen", State);
                StringAssert.Contains("Queued world announcement.", string.Join(" ", _input.AnnouncementUI.VisibleLines));
                CloseReader();
            }
            Assert.AreEqual(kind == "skills" ? "SkillsScreenOpen" : "AbilityManagerOpen", State);
            AssertNoTurn(tick, energy);
        }
    }
}
