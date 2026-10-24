using System;
using System.Collections;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual inventory-menu caller checks: a rejected structured snapshot
    /// must not be published as a successful legacy announcement instead.</summary>
    public sealed class ExamineLedgerInventoryTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        HotbarSaveFixture scope;
        EntityFactory factory;
        Entity player;
        InventoryUI ui;
        GameObject host;
        InventoryPart Inventory => player.GetPart<InventoryPart>();

        sealed class DescriptionCallback : IItemEnhancement
        {
            public override string Name => "LedgerInventoryDescriptionCallback";
            public Action Callback;
            public int Calls;
            bool fired;
            public override string GetEffectDescription()
            {
                Calls++;
                if (!fired) { fired = true; Callback?.Invoke(); }
                return "A readable enhancement note.";
            }
        }

        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            factory = new EntityFactory();
            var content = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.NotNull(content);
            factory.LoadBlueprints(content.text);
            player = factory.CreateEntity("Player");
            Assert.NotNull(player);
            Assert.NotNull(Inventory);
            host = new GameObject("Ledger inventory caller");
            host.transform.SetParent(scope.Root.transform, false);
            ui = host.AddComponent<InventoryUI>();
            ui.PlayerEntity = player;
            ui.EntityFactory = factory;
            ui.Open();
            Assert.True(ui.IsOpen);
            MessageLog.Clear();
            Diag.ResetAll();
            Diag.SetChannel("event", true);
        }

        [TearDown] public void Cleanup()
        {
            if (ui != null) ui.Close();
            if (host != null) Object.DestroyImmediate(host);
            scope?.Dispose();
            Diag.ResetAll();
        }

        [TestCase("Dagger")]
        [TestCase("HealingTonic")]
        [TestCase("FireClay")]
        [TestCase("PitchpodSeed")]
        [TestCase("GoldCoin")]
        public void UnchangedOwnerPublishesOneStructuredSnapshotWithoutSpendingTheItem(string blueprint)
        {
            var item = Carry(blueprint);
            int action = OpenExamine(item);
            var callback = new DescriptionCallback();
            item.AddPart(callback);
            Execute(action);
            Assert.Greater(callback.Calls, 0, "The counter-check exercised a real description callback.");
            Assert.True(MessageLog.HasPendingAnnouncement);
            Assert.AreEqual(1, MessageLog.GetPendingAnnouncementsSnapshot().Count);
            var args = new object[] { null, null };
            var take = typeof(MessageLog).GetMethod("ConsumeAnnouncementWithPresentation", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(take);
            Assert.IsNotEmpty((string)take.Invoke(null, args));
            var document = args[1] as AnnouncementDocument;
            Assert.NotNull(document, "A valid item must use its structured subject rather than fallback text.");
            Assert.AreEqual(item.GetDisplayName(), document.Title);
            Assert.False(MessageLog.HasPendingAnnouncement);
            Assert.True(Inventory.Contains(item));
            Assert.AreSame(player, item.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1, Records(Success(blueprint), item));
            Assert.AreEqual(0, Records(Rejection(blueprint), item));
            Assert.IsNull(Field(ui, "_itemActionPopup"));
            Assert.True(ui.IsOpen);
            Assert.False((bool)Field(ui, "_pendingEverydayTurn"));
        }

        [TestCase("Dagger")]
        [TestCase("HealingTonic")]
        [TestCase("FireClay")]
        [TestCase("PitchpodSeed")]
        public void OwnerRemovedDuringDescriptionCannotFallBackToAStaleSuccessfulAnnouncement(string blueprint)
        {
            var item = Carry(blueprint);
            int action = OpenExamine(item);
            object popup = Field(ui, "_itemActionPopup");
            var callback = new DescriptionCallback
            {
                Callback = () => Assert.True(Inventory.RemoveObject(item))
            };
            item.AddPart(callback);
            Execute(action);
            Assert.Greater(callback.Calls, 0, "The owner really changed during description, after menu selection.");
            Assert.False(Inventory.Contains(item));
            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
            AssertRejected(item, blueprint, popup);
        }

        [TestCase("Dagger", "render")]
        [TestCase("Dagger", "physics")]
        [TestCase("HealingTonic", "render")]
        [TestCase("HealingTonic", "physics")]
        [TestCase("FireClay", "render")]
        [TestCase("FireClay", "physics")]
        [TestCase("PitchpodSeed", "render")]
        [TestCase("PitchpodSeed", "physics")]
        public void StalePartOwnershipCannotBypassStructuredValidationThroughLegacyText(string blueprint, string part)
        {
            var item = Carry(blueprint);
            int action = OpenExamine(item);
            object popup = Field(ui, "_itemActionPopup");
            if (part == "render") item.GetPart<RenderPart>().ParentEntity = new Entity();
            else item.GetPart<PhysicsPart>().ParentEntity = new Entity();
            Assert.True(Inventory.Contains(item), "A stale list entry is deliberately insufficient authority.");
            Execute(action);
            AssertRejected(item, blueprint, popup);
        }

        [TestCase("removed-item")]
        [TestCase("empty-stack")]
        [TestCase("foreign-custody")]
        [TestCase("missing-examinable")]
        [TestCase("foreign-examinable")]
        public void OrdinaryExamineRevalidatesTheCapturedItemAndProviderBeforeReading(string fault)
        {
            var item = Carry("PitchpodSeed");
            int action = OpenExamine(item);
            object popup = Field(ui, "_itemActionPopup");
            var description = item.GetPart<ExaminablePart>();
            var custodian = new Entity { ID = "other-reader-owner" };
            if (fault == "removed-item") Assert.True(Inventory.RemoveObject(item));
            if (fault == "empty-stack") item.GetPart<StackerPart>().StackCount = 0;
            if (fault == "foreign-custody") item.GetPart<PhysicsPart>().InInventory = custodian;
            if (fault == "missing-examinable") Assert.True(item.RemovePart(description));
            if (fault == "foreign-examinable") description.ParentEntity = custodian;
            int expectedCount = item.GetPart<StackerPart>().StackCount;
            Assert.DoesNotThrow(() => Execute(action));
            AssertRejected(item, item.BlueprintName, popup, expectedCount);
            Assert.AreEqual(fault != "removed-item", Inventory.Contains(item));
            if (fault == "foreign-custody") Assert.AreSame(custodian, item.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OrdinaryExamineRejectsProviderRemovalOrReplacementDuringItsSingleSnapshot(bool replace)
        {
            var item = Carry("PitchpodSeed");
            int action = OpenExamine(item);
            object popup = Field(ui, "_itemActionPopup");
            var original = item.GetPart<ExaminablePart>();
            var replacement = new ExaminablePart { Text = "A replacement description must not authorize stale prose." };
            var callback = new DescriptionCallback
            {
                Callback = () =>
                {
                    Assert.True(item.RemovePart(original));
                    if (replace) item.AddPart(replacement);
                }
            };
            item.AddPart(callback);
            Assert.DoesNotThrow(() => Execute(action),
                "Losing the description provider must be a visible refusal, not a log-only success or callback exception.");
            Assert.AreEqual(1, callback.Calls,
                "Generic inspection creates one structured snapshot; it does not rerun a legacy description provider.");
            Assert.AreSame(replace ? replacement : null, item.GetPart<ExaminablePart>());
            AssertRejected(item, item.BlueprintName, popup);
        }

        void AssertRejected(Entity item, string blueprint, object popup, int expectedCount = 1)
        {
            Assert.False(MessageLog.HasPendingAnnouncement, "Rejected metadata cannot silently become a plain-text popup.");
            Assert.AreEqual(0, Records(Success(blueprint), item));
            Assert.AreEqual(1, Records(Rejection(blueprint), item));
            Assert.AreSame(popup, Field(ui, "_itemActionPopup"));
            Assert.IsNotEmpty((string)Field(ui, "_actionStatus"));
            Assert.True(ui.IsOpen);
            Assert.False((bool)Field(ui, "_pendingEverydayTurn"));
            Assert.AreEqual(expectedCount, item.GetPart<StackerPart>().StackCount);
        }

        Entity Carry(string blueprint)
        {
            var item = factory.CreateEntity(blueprint);
            Assert.NotNull(item);
            Assert.True(Inventory.AddObject(item));
            Assert.AreSame(player, item.GetPart<PhysicsPart>().InInventory);
            return item;
        }

        int OpenExamine(Entity item)
        {
            Assert.True(ui.ReopenItemActionPopupFor(item));
            var actions = (IList)Field(Field(ui, "_itemActionPopup"), "Actions");
            string command = item.BlueprintName == "FireClay" ? "examine_material"
                : item.BlueprintName == "HealingTonic" ? "examine_tonic"
                : ItemExamineService.TryDescribeDetails(item, out _) ? "examine_item" : "Examine";
            for (int i = 0; i < actions.Count; i++)
                if ((string)Field(actions[i], "Command") == command) return i;
            Assert.Fail("Expected actual inventory action: " + command);
            return -1;
        }

        void Execute(int action)
        {
            var method = typeof(InventoryUI).GetMethod("ExecuteItemAction", Members);
            Assert.NotNull(method);
            method.Invoke(ui, new object[] { action });
        }

        int Records(string kind, Entity item) => DiagQuery.Count(new DiagQuery.Filter
            { Category = "event", Kind = kind, Actor = player.ID, Target = item.ID }).Count;
        static string Success(string blueprint) => blueprint == "FireClay" ? "MaterialExamined" : "ItemExamined";
        static string Rejection(string blueprint) => blueprint == "FireClay" ? "MaterialExamineRejected" : "ItemExamineRejected";
        static object Field(object owner, string name)
        {
            var field = owner.GetType().GetField(name, Members);
            Assert.NotNull(field, owner.GetType().Name + "." + name);
            return field.GetValue(owner);
        }
    }
}
