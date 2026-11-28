using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public abstract class CraftingFlowFixture : StackIdentityFixture
    {
        protected InventoryUI UI; protected Entity Player, Steel, Iron, Oak, Leather, Station; protected Zone Zone;
        EntityFactory _oldForge, _oldStill; Camera _camera; readonly List<Camera> _oldMainCameras = new List<Camera>();
        protected InventoryPart Inventory => Player.GetPart<InventoryPart>();
        [SetUp] public void SetupUI()
        {
            _oldForge = ForgePart.Factory; _oldStill = AlchemyStillPart.Factory; ForgePart.Factory = AlchemyStillPart.Factory = Factory;
            Player = Actor(); Inventory.MaxWeight = -1; Zone = new Zone("CraftingFlow"); Assert.IsTrue(Zone.AddEntity(Player, 10, 10));
            Station = Item("TinkersForge"); Assert.IsTrue(Zone.AddEntity(Station, 11, 10));
            Steel = Carry("SteelBladeComponent"); Iron = Carry("IronSpikeComponent"); Oak = Carry("OakHaftComponent"); Leather = Carry("LeatherBindingComponent");
            UI = new GameObject("Crafting flow test").AddComponent<InventoryUI>(); UI.PlayerEntity = Player; UI.CurrentZone = Zone; UI.EntityFactory = Factory; UI.Open(); Set("_panel", 4); Call("Rebuild");
        }
        [TearDown] public void CleanupUI()
        {
            if (UI != null) { UI.Close(); UnityEngine.Object.DestroyImmediate(UI.gameObject); }
            if (_camera != null) UnityEngine.Object.DestroyImmediate(_camera.gameObject);
            foreach (var camera in _oldMainCameras) if (camera != null) camera.tag = "MainCamera"; _oldMainCameras.Clear();
            ForgePart.Factory = _oldForge; AlchemyStillPart.Factory = _oldStill;
        }
        protected Entity Carry(string bp, int quantity = 2)
        { var item = Item(bp); item.GetPart<StackerPart>().StackCount = quantity; Assert.IsTrue(Inventory.AddObject(item)); Assert.IsTrue(Inventory.Contains(item)); return item; }
        protected object Get(string field) => typeof(InventoryUI).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(UI);
        protected void Set(string field, object value) => typeof(InventoryUI).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(UI, value);
        protected object Call(string name, params object[] args)
        { var method = typeof(InventoryUI).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic); Assert.NotNull(method, name); return method.Invoke(UI, args); }
        protected static object Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(owner);
        protected int Row(Entity item)
        { var rows = (IList)Get("_craftRows"); for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Field(rows[i], "Item"), item)) return i; Assert.Fail("Actual carried item must have a crafting row."); return -1; }
        protected void Pick(Entity item) { Set("_craftCursorIndex", Row(item)); Call("ToggleCraftPickUnderCursor"); }
        protected void BrewMode()
        { var f = typeof(InventoryUI).GetField("_craftingMode", BindingFlags.Instance | BindingFlags.NonPublic); f.SetValue(UI, Enum.Parse(f.FieldType, "Brew")); Call("Rebuild"); }
        protected int Hit(int x, int y) => (int)Call("GetCraftingRowAtGrid", new Vector2Int(x, y));
        protected void PointerAt(int x, int y)
        {
            if (_camera == null)
            {
                foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (camera.CompareTag("MainCamera")) { _oldMainCameras.Add(camera); camera.tag = "Untagged"; }
                _camera = new GameObject("Crafting pointer test").AddComponent<Camera>(); _camera.tag = "MainCamera"; _camera.orthographic = true; _camera.orthographicSize = 22.5f; _camera.transform.position = new Vector3(40, 22.5f, -10);
            }
            Vector3 actual = _camera.ScreenToWorldPoint(Input.mousePosition); Vector3 desired = new Vector3(x + .5f, 44 - y + .5f, actual.z);
            _camera.transform.position += desired - actual;
            Assert.AreEqual(new Vector2Int(x, y), (Vector2Int)Call("MouseToGrid"), "Existing legacy pointer must land on the test cell.");
        }
    }
    public class GameAuditCraftingFlowTests : CraftingFlowFixture
    {
        [TestCase(false)] [TestCase(true)] public void SelectingAnotherBladeUnifiesPreviewAndPackOrStationOutput(bool station)
        {
            Pick(Steel); Pick(Iron); Pick(Oak); Pick(Leather);
            Assert.IsFalse(CraftingMarkPart.IsMarked(Steel)); Assert.IsTrue(CraftingMarkPart.IsMarked(Iron)); Assert.AreSame(Iron, Get("_pickedBlade"));
            var preview = (ForgePreview)Get("_forgePreview"); Assert.IsTrue(preview.IsComplete);
            if (station)
            {
                var e = GameEvent.New("InventoryAction"); e.SetParameter("Actor", (object)Player); e.SetParameter("Zone", (object)Zone); e.SetParameter("Command", "CraftKit");
                try { Station.FireEvent(e); } finally { e.Release(); }
            }
            else Call("ExecuteCraft", false);
            var weapon = Inventory.Objects.Single(i => i.GetPart<WeaponAssemblyPart>() != null);
            Assert.AreEqual("IronSpikeComponent", weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint); Assert.AreEqual(preview.BaseDamage, weapon.GetPart<MeleeWeaponPart>().BaseDamage);
            Assert.AreEqual(2, Steel.GetPart<StackerPart>().StackCount); Assert.AreEqual(1, Iron.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1, Oak.GetPart<StackerPart>().StackCount); Assert.AreEqual(1, Leather.GetPart<StackerPart>().StackCount);
        }
        [Test] public void PickingSelectedRowAgainClearsOnlyThatGroup()
        { Pick(Steel); Pick(Oak); Pick(Leather); Pick(Steel); Assert.IsFalse(CraftingMarkPart.IsMarked(Steel)); Assert.IsNull(Get("_pickedBlade")); Assert.IsTrue(CraftingMarkPart.IsMarked(Oak)); Assert.IsTrue(CraftingMarkPart.IsMarked(Leather)); }
        [Test] public void AlternativeHaftSelectionPreservesBladeAndBinding()
        { var willow = Carry("WillowHaftComponent"); Call("Rebuild"); Pick(Steel); Pick(Leather); Pick(Oak); Pick(willow); Assert.IsFalse(CraftingMarkPart.IsMarked(Oak)); Assert.AreSame(willow, Get("_pickedHaft")); Assert.IsTrue(CraftingMarkPart.IsMarked(Steel)); Assert.IsTrue(CraftingMarkPart.IsMarked(Leather)); }
        [Test] public void ActualQuenchMediaAreExclusiveButReagentsRemainMultiple()
        {
            var weak = Brew(Player, false, false); var strong = Brew(Player, false, true); Call("Rebuild"); Pick(weak); Pick(strong);
            Assert.IsFalse(CraftingMarkPart.IsMarked(weak)); Assert.IsTrue(CraftingMarkPart.IsMarked(strong)); Assert.AreSame(strong, Get("_pickedQuench"));
            var a = Carry("GlimmerBrine"); var b = Carry("SparkRoot"); BrewMode(); Pick(a); Pick(b);
            Assert.IsTrue(CraftingMarkPart.IsMarked(a)); Assert.IsTrue(CraftingMarkPart.IsMarked(b)); CollectionAssert.AreEquivalent(new[] { a, b }, (IEnumerable)Get("_pickedReagents"));
            Assert.IsTrue(CraftingMarkPart.IsMarked(strong));
        }
        [TestCase(false)] [TestCase(true)] public void OpeningAnOldDuplicateSelectionKeepsTheStationWinner(bool quench)
        {
            var first = quench ? Brew(Player,false,false) : Steel; var last = quench ? Brew(Player,false,true) : Iron;
            CraftingMarkPart.Toggle(first); CraftingMarkPart.Toggle(last); var survivor = last.GetPart<CraftingMarkPart>();
            Assert.IsTrue(CraftingMarkPart.IsMarked(first)); Assert.IsTrue(CraftingMarkPart.IsMarked(last));
            Call("Rebuild"); Assert.IsFalse(CraftingMarkPart.IsMarked(first)); Assert.AreSame(survivor,last.GetPart<CraftingMarkPart>());
            Assert.AreSame(last,Get(quench?"_pickedQuench":"_pickedBlade"));
        }
        [TestCase(false)] [TestCase(true)] public void StaleRemovedRowCannotMarkUnownedItem(bool removed)
        { int row = Row(Steel); if (removed) Assert.IsTrue(Inventory.RemoveObject(Steel)); Set("_craftCursorIndex", row); Call("ToggleCraftPickUnderCursor"); Assert.AreEqual(!removed, CraftingMarkPart.IsMarked(Steel)); Assert.AreEqual(!removed, Inventory.Contains(Steel)); }
        [TestCase(false)] [TestCase(true)] public void HoverOverVisibleOakDoesNotSelectHiddenHeadEquipment(bool equipment)
        {
            Set("_panel", equipment ? 0 : 4); PointerAt(13, 9); Call("UpdateMouseHover");
            Assert.AreEqual(equipment ? 0 : 4, Get("_panel"));
            if (equipment) { var slots = (IList)Get("_equipSlots"); Assert.AreEqual("Head", Field(slots[(int)Get("_equipCursorIndex")], "ShortLabel")); }
            else Assert.AreEqual(Row(Oak), Get("_craftCursorIndex"));
            Assert.IsFalse(CraftingMarkPart.IsMarked(Oak)); Assert.IsNull(Get("_equipPopup"));
        }
        [TestCase(13,4)] [TestCase(13,7)] [TestCase(13,8)] [TestCase(13,10)] [TestCase(13,15)] [TestCase(47,9)] [TestCase(49,9)] [TestCase(52,9)] [TestCase(0,9)]
        public void HeadersSpacersNotesAndRightSideHaveNoCraftingHit(int x, int y)
        { Assert.AreEqual(-1, Hit(x, y)); }
        [Test] public void HitTestingUsesSectionSpacingAndScrolledVisibleRows()
        {
            Assert.AreEqual(Row(Steel), Hit(13,5)); Assert.AreEqual(Row(Iron), Hit(13,6)); Assert.AreEqual(Row(Oak), Hit(13,9)); Assert.AreEqual(Row(Leather), Hit(13,12));
            Set("_craftScrollOffset", 3); Call("BuildCraftingLayout"); Assert.AreEqual(Row(Oak), Hit(13,5)); Assert.AreEqual(-1, Hit(13,6)); Assert.AreEqual(Row(Leather), Hit(13,8));
        }
        [Test] public void ParkedPointerDoesNotUndoKeyboardSelection()
        {
            PointerAt(13,5); Call("UpdateMouseHover"); Assert.AreEqual(Row(Steel), Get("_craftCursorIndex"));
            Call("MoveCraftCursor", 1); Assert.AreEqual(Row(Iron), Get("_craftCursorIndex")); Call("UpdateMouseHover"); Assert.AreEqual(Row(Iron), Get("_craftCursorIndex"));
            PointerAt(13,9); Call("UpdateMouseHover"); Assert.AreEqual(Row(Oak), Get("_craftCursorIndex")); Assert.IsFalse(CraftingMarkPart.IsMarked(Oak));
        }
        [TestCase(false)] [TestCase(true)] public void ExplicitPointerClickSelectsAndTogglesOnlyVisibleItem(bool header)
        {
            int y = header ? 8 : 9; Call("HandleCraftingClick", new Vector2Int(13,y)); Assert.AreEqual(!header, CraftingMarkPart.IsMarked(Oak)); Assert.IsNull(Get("_equipPopup")); Assert.AreEqual(4, Get("_panel"));
            Call("HandleCraftingClick", new Vector2Int(13,y)); Assert.IsFalse(CraftingMarkPart.IsMarked(Oak));
        }
    }

    public class InventoryPackRecipesTests : CraftingFlowFixture
    {
        const string Haft = "PrepareRecipe|shape_haft";
        const string Binding = "PrepareRecipe|braid_binding";
        const string Bandage = "PrepareRecipe|bandage";

        [Test]
        public void PackRecipesGroupTheSameActionsOfferedOnTimberAndCord()
        {
            var timber = Carry("SalvagedTimber");
            var cord = Carry("KnotflaxCord");
            OpenRecipes();

            var haft = FindRecipe(timber, Haft);
            var binding = FindRecipe(cord, Binding);
            var bandage = FindRecipe(cord, Bandage);
            Assert.AreEqual(haft.Category, binding.Category);
            Assert.AreNotEqual(binding.Category, bandage.Category);
            Assert.False(UI.ConsumePendingEverydayTurn());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MakingAHaftSpendsOneSelectedUnitOrRefusesAStaleSource(bool sourceRemoved)
        {
            // Shaping a haft is an ordinary hand recipe. Move the fixture's
            // forge out of the scene so it cannot accidentally satisfy it.
            Zone.RemoveEntity(Station);
            var timber = Carry("SalvagedTimber");
            var alternate = Carry("KnotflaxCord");
            OpenRecipes();
            var recipe = FindRecipe(timber, Haft);
            Set("_craftCursorIndex", recipe.Index);

            Entity replacement = null;
            if (sourceRemoved)
            {
                Assert.True(Inventory.RemoveObject(timber));
                replacement = Carry("SalvagedTimber", 3);
            }

            var oldSeedFactory = SeedPart.Factory;
            try
            {
                SeedPart.Factory = Factory;
                Call("ExecuteCraft", false);
                if (sourceRemoved) Call("ExecuteCraft", false);
            }
            finally { SeedPart.Factory = oldSeedFactory; }

            int hafts = Inventory.Objects.Where(item => item.BlueprintName == "FieldHaftComponent")
                .Sum(item => item.GetPart<StackerPart>()?.StackCount ?? 1);
            Assert.AreEqual(sourceRemoved ? 0 : 1, hafts);
            Assert.AreEqual(sourceRemoved ? 2 : 1, timber.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(2, alternate.GetPart<StackerPart>().StackCount);
            if (sourceRemoved) Assert.AreEqual(3, replacement.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(sourceRemoved, UI.IsOpen);
            Assert.AreEqual(!sourceRemoved, UI.ConsumePendingEverydayTurn());
            Assert.False(UI.ConsumePendingEverydayTurn(), "The action receipt is single-use.");
        }

        [Test]
        public void AStillRecipeExplainsTheMissingStationWithoutSpendingAnything()
        {
            var mendleaf = Carry("MendleafSprig");
            OpenRecipes();
            var rows = (IList)Get("_craftRows");
            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if (!ReferenceEquals(mendleaf, Field(row, "Item"))
                    || Field(row, "RecipeAction") is not InventoryAction action || action.Name != "concentrate_mendleaf") continue;
                Assert.AreEqual("still-required", action.DisabledCode);
                Set("_craftCursorIndex", index);
                Assert.That((string)Call("SelectedPackRecipeDetails"), Does.Contain("alchemy still"));
                Call("ExecuteCraft", false);
                Assert.AreEqual(2, mendleaf.GetPart<StackerPart>().StackCount);
                Assert.True(UI.IsOpen);
                Assert.False(UI.ConsumePendingEverydayTurn());
                return;
            }
            Assert.Fail("The unavailable recipe must still explain the station requirement.");
        }

        void OpenRecipes()
        {
            var mode = typeof(InventoryUI).GetNestedType("CraftingMode", BindingFlags.NonPublic);
            Assert.NotNull(mode);
            Call("SwitchCraftingMode", Enum.Parse(mode, "Recipes"));
        }

        (int Index, string Category) FindRecipe(Entity source, string command)
        {
            var original = InventorySystem.GetActions(Player, source).Single(action => action.Command == command);
            var rows = (IList)Get("_craftRows");
            string category = null;
            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                if ((bool)Field(row, "IsHeader")) category = (string)Field(row, "Text");
                if (!ReferenceEquals(source, Field(row, "Item"))
                    || !(Field(row, "RecipeAction") is InventoryAction action) || action.Command != command) continue;
                Assert.That(category, Is.Not.Null.And.Not.Empty);
                Assert.True((bool)Field(row, "IsSelectable"));
                Assert.AreEqual(original.Display, action.Display);
                Assert.AreEqual(original.Time, action.Time);
                return (index, category);
            }
            Assert.Fail($"Missing pack recipe for {source.BlueprintName}: {command}");
            return (-1, null);
        }
    }
}
