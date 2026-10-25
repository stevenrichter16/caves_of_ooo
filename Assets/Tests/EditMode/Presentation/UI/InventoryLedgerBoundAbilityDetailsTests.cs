using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Current learned skill facts in the retained inventory pane and
    /// existing free-reader seam. No OS input, GPU readability or Deck-feel claim.</summary>
    public sealed class InventoryLedgerBoundAbilityDetailsTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [TestCase("LongBlades_EnGarde")] [TestCase("LongBlades_Lunge")]
        public void BoundCombatSkillShowsAuthoritativeInlineAndFullFactsWithoutCastingOrRebinding(string skillClass)
        {
            using (var r = new Rig())
            {
                var chosen = r.Abilities.AbilityList.Single(a => a.SourcePowerClass == skillClass);
                chosen.CooldownRemaining = 7;
                Assert.True(SkillRegistry.TryGetPowerByClass(skillClass, out var power));
                string expected = AbilityDetailsBuilder.BuildForAbility(r.Player, chosen.ID);
                var bindings = r.Abilities.SlotAssignments.ToArray();
                var parts = r.Player.Parts.ToArray();
                var items = r.Player.GetPart<InventoryPart>().Objects.ToArray();
                int sp = r.Player.GetStatValue("SP"), hp = r.Player.GetStatValue("Hitpoints");
                r.OpenSlot(r.Abilities.GetSlotForAbility(chosen.ID));
                string pane = Normalize(string.Join("\n", r.DetailLabels().Select(t => t.Content)));
                StringAssert.Contains(Normalize(power.Description.Split('.')[0]), pane,
                    "The bound skill must explain its real effect, not only its name or readiness.");
                StringAssert.DoesNotContain("No inline description", pane);
                if (skillClass == "LongBlades_EnGarde")
                    StringAssert.Contains("While guarding: 30% parry", pane,
                        "Keep the already-authoritative guarded chance distinct from current inactive defense.");
                foreach (var label in r.DetailLabels()) r.AssertFits(label);
                var document = r.UI.SelectedDecisionPresentation(out var text);
                Assert.IsNull(document, "An ability fact sheet is not an examined item.");
                Assert.AreEqual(expected, text, "F1 must reuse the authoritative owned-ability details without truncation.");
                Assert.AreEqual(text, r.UI.SelectedDecisionDetails());
                StringAssert.Contains("7 remaining", text);
                StringAssert.Contains("Cooldown: " + chosen.MaxCooldown, text);
                StringAssert.Contains(power.Description, text);
                CollectionAssert.AreEqual(bindings, r.Abilities.SlotAssignments);
                CollectionAssert.AreEqual(parts, r.Player.Parts); CollectionAssert.AreEqual(items, r.Player.GetPart<InventoryPart>().Objects);
                Assert.AreEqual(7, chosen.CooldownRemaining); Assert.AreEqual(sp, r.Player.GetStatValue("SP"));
                Assert.AreEqual(hp, r.Player.GetStatValue("Hitpoints")); Assert.False(r.Player.HasEffect<EnGardeEffect>());
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
                Assert.IsNull(Get(r.UI, "_grimoirePicker"), "Reading cannot open assignment or change the selected slot.");

                var other = r.Abilities.AbilityList.Single(a => a.ID != chosen.ID);
                r.SelectSlot(r.Abilities.GetSlotForAbility(other.ID));
                Assert.AreEqual(AbilityDetailsBuilder.BuildForAbility(r.Player, other.ID), r.UI.SelectedDecisionDetails());
                Assert.AreNotEqual(expected, r.UI.SelectedDecisionDetails(), "Changing the actual bound slot must refresh its facts.");
                CollectionAssert.AreEqual(bindings, r.Abilities.SlotAssignments);
            }
        }

        [TestCase("empty-slot")] [TestCase("removed-ability")]
        public void EmptyOrRemovedBoundSkillDoesNotKeepThePriorDescription(string change)
        {
            using (var r = new Rig())
            {
                var chosen = r.Abilities.AbilityList.Single(a => a.SourcePowerClass == "LongBlades_EnGarde");
                int slot = r.Abilities.GetSlotForAbility(chosen.ID); r.OpenSlot(slot);
                Assert.That(r.UI.SelectedDecisionDetails(), Is.Not.Null.And.Not.Empty, "Live selected-owner control.");
                if (change == "empty-slot") r.SelectSlot(9);
                else { r.Player.GetPart<SkillsPart>().RemoveSkill(r.Player.GetPart<LongBlades_EnGarde>()); Call(r.UI, "Render"); }
                Assert.IsNull(r.UI.SelectedDecisionPresentation(out var text)); Assert.IsNull(text);
                Assert.IsNull(r.UI.SelectedDecisionDetails());
                string pane = Normalize(string.Join("\n", r.DetailLabels().Select(t => t.Content)));
                StringAssert.DoesNotContain("While guarding:", pane); StringAssert.DoesNotContain("Raise your held long blade", pane);
                StringAssert.Contains("This slot is empty", pane);
                Assert.False(r.Player.HasEffect<EnGardeEffect>()); Assert.False(r.UI.ConsumePendingEverydayTurn());
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void CommandsOfferFreeFullDetailsOnlyForACurrentlyBoundAbility(bool occupied)
        {
            using (var r = new Rig())
            {
                r.OpenSlot(occupied ? 0 : 9);
                var before = r.Abilities.SlotAssignments.ToArray();
                Call(r.UI, "OpenLedgerCommands");
                var commands = (System.Collections.IList)Get(r.UI, "_ledgerCommands");
                int details = -1;
                for (int i = 0; i < commands.Count; i++)
                    if ((string)Get(commands[i], "Label") == "Full details") details = i;
                if (occupied)
                {
                    Assert.GreaterOrEqual(details, 0, "An owned selected ability needs the same discoverable details command as items.");
                    Call(r.UI, "ExecuteLedgerCommand", details);
                    Assert.True(r.UI.ConsumeDetailsRequest());
                    Assert.False(r.UI.ConsumeDetailsRequest(), "Transfer the free reader request once.");
                    Assert.False((bool)Get(r.UI, "_ledgerCommandsOpen"));
                }
                else
                {
                    Assert.AreEqual(-1, details, "An empty slot cannot offer facts for another bound ability.");
                    Call(r.UI, "CancelLedgerPopup");
                    Assert.False(r.UI.ConsumeDetailsRequest());
                }
                CollectionAssert.AreEqual(before, r.Abilities.SlotAssignments);
                Assert.False(r.Player.HasEffect<EnGardeEffect>());
                Assert.False(r.UI.ConsumePendingEverydayTurn()); Assert.IsNull(r.UI.ConsumePendingThrowRequest());
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void BoundSkillAdvertisesItsActualFullDetailsControl(bool gamepad)
        {
            var input = new InputTestFixture(); input.Setup();
            try
            {
                if (gamepad) InputSystem.AddDevice<Gamepad>().MakeCurrent();
                using (var r = new Rig())
                {
                    r.OpenSlot(0);
                    string key = gamepad ? "LT+RT" : "F1";
                    var hints = r.DetailLabels().Where(t => t.Content.Contains(key)
                        && t.Content.IndexOf("details", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                    Assert.AreEqual(1, hints.Length, "The selected ability pane must offer its complete fact sheet.");
                    r.AssertFits(hints[0]);
                    r.SelectSlot(9);
                    Assert.False(r.DetailLabels().Any(t => t.Content.Contains(key)
                        && t.Content.IndexOf("details", StringComparison.OrdinalIgnoreCase) >= 0), "An empty slot has no ability facts.");
                }
            }
            finally { input.TearDown(); }
        }

        static string Normalize(string text) => string.Join(" ", (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
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
            public readonly Entity Player;
            public readonly ActivatedAbilitiesPart Abilities;
            RectTransform Content => (RectTransform)Get(UI, "_ledgerContent");
            public Rig()
            {
                saved = new HotbarSaveFixture(false, false);
                try
                {
                    root = new GameObject("Bound ability details fixture");
                    var cameraHost = new GameObject("Bound ability camera"); cameraHost.transform.SetParent(root.transform, false); cameraHost.tag = "MainCamera";
                    var camera = cameraHost.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 22.5f;
                    camera.transform.position = new Vector3(40, 22.5f, -10);
                    var grid = new GameObject("Bound ability grid", typeof(Grid)); grid.transform.SetParent(root.transform, false);
                    var tiles = new GameObject("Bound ability tiles", typeof(Tilemap)); tiles.transform.SetParent(grid.transform, false);
                    var factory = new EntityFactory(); factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                    Player = factory.CreateEntity("Player"); Assert.NotNull(Player);
                    var previous = Player.GetPart<ActivatedAbilitiesPart>(); if (previous != null) Player.RemovePart(previous);
                    Abilities = new ActivatedAbilitiesPart(); Player.AddPart(Abilities);
                    var skills = Player.GetPart<SkillsPart>(); if (skills == null) { skills = new SkillsPart(); Player.AddPart(skills); }
                    Assert.True(skills.AddSkill(new LongBlades_EnGarde())); Assert.True(skills.AddSkill(new LongBlades_Lunge()));
                    Assert.AreEqual(2, Abilities.AbilityList.Count);
                    var sword = factory.CreateEntity("LongSword"); var pack = Player.GetPart<InventoryPart>();
                    Assert.True(pack.AddObject(sword));
                    Assert.True(pack.EquipToBodyPart(sword, Player.GetPart<Body>().GetPartsByType("Hand")[0]));
                    Assert.AreSame(sword.GetPart<MeleeWeaponPart>(), BladeDefense.FindLongBlade(Player));
                    var zone = new Zone("BoundAbilityDetails"); Assert.True(zone.AddEntity(Player, 10, 10));
                    UI = root.AddComponent<InventoryUI>(); UI.PlayerEntity = Player; UI.CurrentZone = zone; UI.EntityFactory = factory;
                    UI.Tilemap = tiles.GetComponent<Tilemap>();
                }
                catch { Dispose(); throw; }
            }
            public void OpenSlot(int slot) { UI.Open(); Set(UI, "_panel", 3); SelectSlot(slot); }
            public void SelectSlot(int slot) { Set(UI, "_abilitiesCursorSlot", slot); Call(UI, "Render"); Canvas.ForceUpdateCanvases(); }
            Rect BoundsOf(RectTransform child)
            {
                var b = RectTransformUtility.CalculateRelativeRectTransformBounds(Content, child);
                return new Rect(b.min.x - Content.rect.xMin, Content.rect.yMax - b.max.y, b.size.x, b.size.y);
            }
            public SkillScreenText[] DetailLabels() => UI.GetComponentsInChildren<SkillScreenText>(true)
                .Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.Content)
                    && BoundsOf(t.rectTransform).xMin >= 728 && BoundsOf(t.rectTransform).yMin >= 132
                    && BoundsOf(t.rectTransform).yMax <= 658).ToArray();
            public void AssertFits(SkillScreenText label)
            {
                Rect bounds = BoundsOf(label.rectTransform);
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(1256)); Assert.That(bounds.yMax, Is.LessThanOrEqualTo(658));
                Assert.That(label.GlyphHeight, Is.GreaterThanOrEqualTo(18));
                string[] lines = label.Content.Split('\n');
                foreach (string line in lines) Assert.That(line.Length * label.GlyphHeight * .5f, Is.LessThanOrEqualTo(bounds.width + .1f));
                Assert.That(lines.Length * label.GlyphHeight + (lines.Length - 1) * 4, Is.LessThanOrEqualTo(bounds.height + .1f));
            }
            public void Dispose()
            {
                UI?.Close(); if (root != null) Object.DestroyImmediate(root);
                saved?.Dispose(); typeof(EquipmentChangeBus).GetProperty("GlobalVersion").SetValue(null, oldEquipmentVersion);
            }
        }
    }
}
