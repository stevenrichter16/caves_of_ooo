using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>First G1.3 slice: preference ownership, explicit owner application,
    /// and the real pause-controller row. This does not claim normal bootstrap,
    /// actual keyboard/Gamepad dispatch, complete popup layout or live acceptance.</summary>
    public sealed class WorldPresentationSettingsTests
    {
        const string ModeKey = "CavesOfOoo.WorldPresentation";
        const string SpriteKey = "CavesOfOoo.SpriteEnvironmentEnabled";
        const string FxKey = "CavesOfOoo.Fx.Mode";
        const BindingFlags Static = BindingFlags.Public | BindingFlags.Static;
        const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly string[] LegacyIntKeys = { Village3DSettings.PreferenceKey,
            Village3DSettings.LowDetailPreferenceKey, Village3DSettings.ShadowsPreferenceKey, SpriteKey, FxKey };
        readonly List<GameObject> roots = new List<GameObject>();
        PrefsSnapshot borrowed;
        Action<string> oldFullDirty;
        Action<int, int, string> oldCellDirty;
        bool oldEnabled, oldLow;
        object oldResolution, oldShadows, oldMode, oldSprites;
        SpellFxMode oldFx;

        [SetUp] public void Setup()
        {
            borrowed = Capture(true);
            oldEnabled = Village3DSettings.Enabled; oldLow = Village3DSettings.LowDetail;
            oldResolution = QualityField("resolutionOverride").GetValue(null);
            oldShadows = QualityField("shadowsOverride").GetValue(null); oldFx = SpellFxSettings.Mode;
            var type = FindSettings();
            oldMode = type?.GetProperty("Mode", Static)?.GetValue(null);
            oldSprites = type?.GetProperty("LegacySpritesEnabled", Static)?.GetValue(null);
            oldFullDirty = ZoneRenderHooks.FullDirtyCallback; oldCellDirty = ZoneRenderHooks.CellDirtyCallback;
            ZoneRenderHooks.FullDirtyCallback = null; ZoneRenderHooks.CellDirtyCallback = null;
            PlayerPrefs.DeleteKey(ModeKey);
            foreach (string key in LegacyIntKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(Village3DSettings.WorldResolutionPreferenceKey);
        }

        [TearDown] public void Cleanup()
        {
            try
            {
                for (int i = roots.Count - 1; i >= 0; i--) if (roots[i] != null) Object.DestroyImmediate(roots[i]);
                roots.Clear();
                var type = FindSettings();
                if (oldMode != null) type?.GetProperty("Mode", Static)?.SetValue(null, oldMode);
                if (oldSprites != null) type?.GetProperty("LegacySpritesEnabled", Static)?.SetValue(null, oldSprites);
                Village3DSettings.Enabled = oldEnabled; Village3DSettings.LowDetail = oldLow;
                QualityField("resolutionOverride").SetValue(null, oldResolution);
                QualityField("shadowsOverride").SetValue(null, oldShadows); SpellFxSettings.Mode = oldFx;
            }
            finally
            {
                borrowed?.Restore();
                ZoneRenderHooks.FullDirtyCallback = oldFullDirty; ZoneRenderHooks.CellDirtyCallback = oldCellDirty;
            }
        }

        [Test] public void PersistedContractHasTwoStableModesAndIndependentKeys()
        {
            var mode = ModeType(); Assert.True(mode.IsEnum);
            CollectionAssert.AreEquivalent(new[] { "Flat16x24", "Previous" }, Enum.GetNames(mode));
            Assert.AreEqual(0, Convert.ToInt32(Enum.Parse(mode, "Flat16x24")));
            Assert.AreEqual(1, Convert.ToInt32(Enum.Parse(mode, "Previous")));
            Assert.AreEqual(ModeKey, Constant(SettingsType(), "PreferenceKey"));
            Assert.AreEqual(SpriteKey, Constant(SettingsType(), "LegacySpritesPreferenceKey"));
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingNewChoiceDefaultsFlatWithoutWritingOrInferringAnOldChoice(bool oldChoicesExist)
        {
            if (oldChoicesExist) SeedLegacy();
            var before = Capture(true); SetMode(1); Call("Load");
            Assert.AreEqual(0, Mode()); Assert.AreEqual(!oldChoicesExist, Sprites());
            before.AssertUnchanged(); Assert.False(PlayerPrefs.HasKey(ModeKey));
        }

        [TestCase(0)] [TestCase(1)]
        public void ExplicitChoiceWinsReadOnlyLoadAndRepeatLoad(int selected)
        {
            SeedLegacy(); PlayerPrefs.SetInt(ModeKey, selected); var before = Capture(true);
            SetMode(1 - selected); SetSprites(true); Call("Load");
            Assert.AreEqual(selected, Mode()); Assert.False(Sprites()); before.AssertUnchanged();
            Call("Load"); Assert.AreEqual(selected, Mode()); before.AssertUnchanged();
        }

        [TestCase(-1)] [TestCase(99)]
        public void InvalidNewValueUsesSafeRuntimeDefaultWithoutRewritingRawPreferences(int invalid)
        {
            SeedLegacy(); PlayerPrefs.SetInt(ModeKey, invalid); var before = Capture(true);
            SetMode(1); Call("Load"); Assert.AreEqual(0, Mode()); before.AssertUnchanged();
        }

        [Test] public void LoadingModeDoesNotReloadOrModifyExistingRuntimeQuality()
        {
            SeedLegacy(); Village3DSettings.Enabled = true; Village3DSettings.LowDetail = false;
            Village3DSettings.WorldResolutionScale = .875f; Village3DSettings.ShadowsEnabled = false;
            SpellFxSettings.Mode = SpellFxMode.Full; var before = Capture(true);
            Call("Load");
            Assert.True(Village3DSettings.Enabled); Assert.False(Village3DSettings.LowDetail);
            Assert.AreEqual(.875f, Village3DSettings.WorldResolutionScale); Assert.False(Village3DSettings.ShadowsEnabled);
            Assert.AreEqual(SpellFxMode.Full, SpellFxSettings.Mode); before.AssertUnchanged();
        }

        [TestCase(0)] [TestCase(1)]
        public void SavingModeWritesOnlyNewKeyAndPreservesExactLegacyValuesAndPresence(int selected)
        {
            SeedLegacy(); PlayerPrefs.SetInt(Village3DSettings.PreferenceKey, 9);
            PlayerPrefs.DeleteKey(Village3DSettings.ShadowsPreferenceKey);
            var legacy = Capture(false); SetMode(selected); SetSprites(true); Call("SaveMode");
            Assert.AreEqual(selected, PlayerPrefs.GetInt(ModeKey, -1)); legacy.AssertUnchanged();
            SetMode(1 - selected); Call("Load"); Assert.AreEqual(selected, Mode()); legacy.AssertUnchanged();
        }

        [Test] public void RuntimeAssignmentsDoNotPersistOrChangeLegacyQuality()
        {
            SeedLegacy(); Village3DSettings.Load(); var before = Capture(true);
            SetMode(0); SetSprites(true); SetMode(1); SetSprites(false);
            before.AssertUnchanged(); Assert.False(Village3DSettings.Enabled); Assert.True(Village3DSettings.LowDetail);
            Assert.AreEqual(.625f, Village3DSettings.WorldResolutionScale); Assert.True(Village3DSettings.ShadowsEnabled);
        }

        [TestCase(0)] [TestCase(1)]
        public void LegacySpriteChoiceLoadsItsExistingBooleanWithoutWritingMode(int spriteChoice)
        {
            PlayerPrefs.SetInt(SpriteKey, spriteChoice); var before = Capture(true);
            Call("Load"); Assert.AreEqual(spriteChoice == 1, Sprites()); Assert.AreEqual(0, Mode()); before.AssertUnchanged();
        }

        [Test] public void SavingLegacySpritesCannotPersistOrNormalizeTheModeOrOtherLegacyKeys()
        {
            SeedLegacy(); PlayerPrefs.SetInt(ModeKey, 99); Call("Load");
            var before = Capture(true); SetSprites(true); Call("SaveLegacySprites");
            before.AssertUnchanged(SpriteKey); Assert.AreEqual(1, PlayerPrefs.GetInt(SpriteKey));
            SetSprites(false); Call("SaveLegacySprites");
            before.AssertUnchanged(); Assert.AreEqual(0, Mode());
        }

        [Test] public void OldSettingsLoadCannotOverwriteAnAlreadySelectedPresentation()
        {
            SeedLegacy(); Call("Load"); SetMode(1); Village3DSettings.Load();
            Assert.AreEqual(1, Mode()); Assert.False(Sprites()); Assert.False(PlayerPrefs.HasKey(ModeKey));
            SetMode(0); Village3DSettings.Load(); Assert.AreEqual(0, Mode());
        }

        [TestCase(false)] [TestCase(true)]
        public void OwnerApplyRoundTripRestoresLegacyEnvironmentAndExactBorrowedGrid(bool legacySprites)
        {
            RequireApply(); SeedLegacy(); Village3DSettings.Load(); var before = Capture(true);
            var owner = Rig(); var unrelated = Host("Unrelated inactive renderer").AddComponent<EnvironmentSpriteRenderer>();
            unrelated.RenderingEnabled = false;
            var original = owner.Grid.cellSize;
            for (int visit = 0; visit < 2; visit++)
            {
                Apply(owner.Renderer, 0, legacySprites);
                Assert.True(owner.Renderer.FlatPixelModeEnabled); Assert.True(owner.Renderer.FlatPixelModeActive);
                Assert.True(owner.Environment.RenderingEnabled); Assert.False(unrelated.RenderingEnabled);
                Assert.AreEqual(new Vector3(1, 1.5f, original.z), owner.Grid.cellSize);
                Apply(owner.Renderer, 1, legacySprites);
                Assert.False(owner.Renderer.FlatPixelModeEnabled); Assert.False(owner.Renderer.FlatPixelModeActive);
                Assert.AreEqual(legacySprites, owner.Environment.RenderingEnabled);
                Assert.AreEqual(original, owner.Grid.cellSize); Assert.False(unrelated.RenderingEnabled);
            }
            before.AssertUnchanged(); Assert.False(Village3DSettings.Enabled); Assert.True(Village3DSettings.LowDetail);
            Assert.AreEqual(.625f, Village3DSettings.WorldResolutionScale); Assert.True(Village3DSettings.ShadowsEnabled);
        }

        [Test] public void OwnerApplyWhileFullscreenRetainsSquareCanvasThenRestoresChosenWorld()
        {
            RequireApply(); var owner = Rig(); var original = owner.Grid.cellSize;
            owner.Renderer.Paused = true; Apply(owner.Renderer, 0, false);
            Assert.True(owner.Renderer.FlatPixelModeActive); Assert.AreEqual(original, owner.Grid.cellSize);
            owner.Renderer.Paused = false; Assert.AreEqual(new Vector3(1, 1.5f, original.z), owner.Grid.cellSize);
            owner.Renderer.Paused = true; Apply(owner.Renderer, 1, false);
            Assert.AreEqual(original, owner.Grid.cellSize); Assert.False(owner.Environment.RenderingEnabled);
            owner.Renderer.Paused = false; Assert.AreEqual(original, owner.Grid.cellSize);
            Assert.False(owner.Renderer.FlatPixelModeActive);
        }

        [Test] public void ApplyWithoutWorldCanRememberRequestWithoutCreatingOrSavingAWorld()
        {
            RequireApply(); var owner = Rig();
            typeof(ZoneRenderer).GetProperty("CurrentZone", Instance).SetValue(owner.Renderer, null);
            var before = Capture(true); Apply(owner.Renderer, 0, false);
            Assert.True(owner.Renderer.FlatPixelModeEnabled); Assert.False(owner.Renderer.FlatPixelModeActive);
            Assert.Null(owner.Renderer.CurrentZone); before.AssertUnchanged();
            Apply(owner.Renderer, 1, false); Assert.False(owner.Renderer.FlatPixelModeEnabled);
            Assert.Null(owner.Renderer.CurrentZone); before.AssertUnchanged();
        }

        [Test] public void RealGraphicsModeRowSwitchesBothDirectionsAndAppliesOnlyItsBoundOwner()
        {
            RequireApply(); SeedLegacy(); Call("Load"); var owner = Rig(); Apply(owner.Renderer, Mode(), Sprites());
            var legacy = Capture(false); var service = new CountingSaves(); var menu = GraphicsMenu(service);
            int applies = 0; BindMenu(menu, () => { applies++; Apply(owner.Renderer, Mode(), Sprites()); });
            int row = ModeRow(); Assert.AreEqual(0, row, "World view is the primary graphics choice.");
            Assert.AreEqual("World view: 16x24", menu.GetItemLabel(row));
            menu.ClickSelect(row, service, null);
            Assert.AreEqual(1, Mode()); Assert.AreEqual(1, applies); Assert.False(owner.Renderer.FlatPixelModeActive);
            Assert.False(owner.Environment.RenderingEnabled); Assert.AreEqual(1, PlayerPrefs.GetInt(ModeKey, -1));
            Assert.AreEqual("World view: Previous", menu.GetItemLabel(row));
            menu.ClickSelect(row, service, null);
            Assert.AreEqual(0, Mode()); Assert.AreEqual(2, applies); Assert.True(owner.Renderer.FlatPixelModeActive);
            Assert.True(owner.Environment.RenderingEnabled); Assert.AreEqual(0, PlayerPrefs.GetInt(ModeKey, -1));
            Assert.True(menu.IsOpen && menu.IsGraphicsOpen); Assert.Zero(service.Calls); legacy.AssertUnchanged();
        }

        [Test] public void RealGraphicsModeRowAcceptsBothArrowDirectionsButBackAndUnrelatedKeyDoNotChangeIt()
        {
            SeedLegacy(); Call("Load"); var legacy = Capture(false); var service = new CountingSaves();
            var menu = GraphicsMenu(service); int applies = 0; BindMenu(menu, () => applies++);
            menu.HoverSelect(ModeRow());
            Assert.True(menu.Tick(new KeyProbe(KeyCode.RightArrow), service, null)); Assert.AreEqual(1, Mode());
            Assert.True(menu.Tick(new KeyProbe(KeyCode.LeftArrow), service, null)); Assert.AreEqual(0, Mode());
            Assert.AreEqual(2, applies); var all = Capture(true);
            Assert.False(menu.Tick(new KeyProbe(KeyCode.F10), service, null));
            Assert.True(menu.Tick(new KeyProbe(KeyCode.Escape), service, null));
            Assert.True(menu.IsOpen); Assert.False(menu.IsGraphicsOpen);
            Assert.AreEqual(2, applies); Assert.Zero(service.Calls); all.AssertUnchanged(); legacy.AssertUnchanged();
        }

        [Test] public void InputHandlerActuallyBindsItsBorrowedPauseControllerToTheCurrentRenderer()
        {
            RequireApply(); SeedLegacy(); Call("Load"); var legacy = Capture(false);
            var owner = Rig(); Apply(owner.Renderer, 0, Sprites());
            var input = Host("Bound presentation input").AddComponent<InputHandler>();
            var ui = Host("Bound presentation menu").AddComponent<PauseMenuUI>();
            input.ZoneRenderer = owner.Renderer; input.PauseMenuUI = ui;
            Assert.NotNull(ui.Controller); ui.Controller.Open();
            ui.Controller.ClickSelect(PauseMenuController.GraphicsIndex, null, null);
            ui.Controller.ClickSelect(ModeRow(), null, null);
            Assert.AreEqual(1, Mode()); Assert.False(owner.Renderer.FlatPixelModeActive);
            Assert.False(owner.Environment.RenderingEnabled); Assert.AreEqual(1, PlayerPrefs.GetInt(ModeKey, -1));
            ui.Controller.ClickSelect(ModeRow(), null, null);
            Assert.AreEqual(0, Mode()); Assert.True(owner.Renderer.FlatPixelModeActive);
            Assert.True(owner.Environment.RenderingEnabled); legacy.AssertUnchanged();
        }

        [Test] public void UnboundOrDetachedMenuCannotPersistAnUnappliedPresentation()
        {
            RequireApply(); SeedLegacy(); Call("Load"); var before = Capture(true);
            var input = Host("Unbound presentation input").AddComponent<InputHandler>();
            var ui = Host("Unbound presentation menu").AddComponent<PauseMenuUI>(); input.PauseMenuUI = ui;
            ui.Controller.Open(); ui.Controller.ClickSelect(PauseMenuController.GraphicsIndex, null, null);
            ui.Controller.ClickSelect(ModeRow(), null, null);
            Assert.AreEqual(0, Mode()); before.AssertUnchanged();
            var owner = Rig(); input.ZoneRenderer = owner.Renderer;
            ui.Controller.ClickSelect(ModeRow(), null, null);
            Assert.AreEqual(1, Mode()); Assert.AreEqual(1, PlayerPrefs.GetInt(ModeKey, -1));
            Assert.False(owner.Renderer.FlatPixelModeEnabled);
            var bound = Capture(true); input.ZoneRenderer = null;
            ui.Controller.ClickSelect(ModeRow(), null, null);
            Assert.AreEqual(1, Mode()); bound.AssertUnchanged();
        }

        static Type FindSettings() => typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.WorldPresentationSettings");
        static Type SettingsType() { var type = FindSettings(); Assert.NotNull(type, "G1.3 presentation preference authority is not implemented."); return type; }
        static Type ModeType()
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.WorldPresentationMode");
            Assert.NotNull(type, "Separate stable presentation enum is required."); return type;
        }
        static PropertyInfo Property(string name)
        { var value = SettingsType().GetProperty(name, Static); Assert.NotNull(value, name + " runtime property is required."); return value; }
        static int Mode() => Convert.ToInt32(Property("Mode").GetValue(null));
        static void SetMode(int value) => Property("Mode").SetValue(null, Enum.ToObject(ModeType(), value));
        static bool Sprites() => (bool)Property("LegacySpritesEnabled").GetValue(null);
        static void SetSprites(bool value) => Property("LegacySpritesEnabled").SetValue(null, value);
        static void Call(string name)
        { var method = SettingsType().GetMethod(name, Static, null, Type.EmptyTypes, null); Assert.NotNull(method, name + " explicit operation is required."); method.Invoke(null, null); }
        static object Constant(Type owner, string name)
        { var field = owner.GetField(name, Static); Assert.NotNull(field, name + " public stable constant is required."); return field.GetRawConstantValue(); }
        static FieldInfo QualityField(string name) => typeof(Village3DSettings).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        static MethodInfo RequireApply()
        {
            var method = typeof(ZoneRenderer).GetMethod("ApplyWorldPresentation", BindingFlags.Public | BindingFlags.Instance,
                null, new[] { ModeType(), typeof(bool) }, null);
            Assert.NotNull(method, "Owner-bound apply seam is required; a global scene search is not presentation authority."); return method;
        }
        static void Apply(ZoneRenderer renderer, int mode, bool legacySprites)
            => RequireApply().Invoke(renderer, new[] { Enum.ToObject(ModeType(), mode), (object)legacySprites });
        static int ModeRow() => (int)Constant(typeof(PauseMenuController), "WorldPresentationIndex");
        static void BindMenu(PauseMenuController menu, Action apply)
        {
            var field = typeof(PauseMenuController).GetField("WorldPresentationChanged", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, "The controller must notify its explicit presentation owner.");
            Assert.AreEqual(typeof(Action), field.FieldType); field.SetValue(menu, apply);
        }
        static PauseMenuController GraphicsMenu(ISaveLoadService service)
        { var menu = new PauseMenuController(); menu.Open(); menu.ClickSelect(PauseMenuController.GraphicsIndex, service, null); Assert.True(menu.IsGraphicsOpen); return menu; }
        GameObject Host(string name) { var root = new GameObject(name); root.SetActive(false); roots.Add(root); return root; }
        OwnerRig Rig()
        {
            var root = Host("Presentation settings owner"); var grid = root.AddComponent<Grid>();
            grid.cellSize = new Vector3(.8f, 1.1f, 0);
            var host = new GameObject("Owned world"); host.SetActive(false); host.transform.SetParent(root.transform, false);
            // The renderer stays inactive to avoid Awake; its Grid is a real,
            // discoverable active ancestor just as it is in the normal scene.
            root.SetActive(true);
            var tilemap = host.AddComponent<Tilemap>(); var renderer = host.AddComponent<ZoneRenderer>();
            var environment = host.AddComponent<EnvironmentSpriteRenderer>(); environment.RenderingEnabled = false;
            typeof(ZoneRenderer).GetField("_tilemap", Instance).SetValue(renderer, tilemap);
            typeof(ZoneRenderer).GetField("_envSpriteRenderer", Instance).SetValue(renderer, environment);
            typeof(ZoneRenderer).GetProperty("CurrentZone", Instance).SetValue(renderer, new Zone(ReferenceGladePlan.ZoneID));
            Assert.AreSame(grid, tilemap.GetComponentInParent<Grid>(), "Projection requires a discoverable parent Grid.");
            return new OwnerRig { Grid = grid, Renderer = renderer, Environment = environment };
        }
        static void SeedLegacy()
        {
            PlayerPrefs.SetInt(Village3DSettings.PreferenceKey, 0); PlayerPrefs.SetInt(Village3DSettings.LowDetailPreferenceKey, 1);
            PlayerPrefs.SetFloat(Village3DSettings.WorldResolutionPreferenceKey, .625f);
            PlayerPrefs.SetInt(Village3DSettings.ShadowsPreferenceKey, 1); PlayerPrefs.SetInt(SpriteKey, 0);
            PlayerPrefs.SetInt(FxKey, (int)SpellFxMode.Off);
        }
        static PrefsSnapshot Capture(bool includeMode)
        {
            var snapshot = new PrefsSnapshot();
            foreach (string key in LegacyIntKeys) snapshot.Ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            if (includeMode) snapshot.Ints[ModeKey] = PlayerPrefs.HasKey(ModeKey) ? PlayerPrefs.GetInt(ModeKey) : (int?)null;
            string resolution = Village3DSettings.WorldResolutionPreferenceKey;
            snapshot.Floats[resolution] = PlayerPrefs.HasKey(resolution) ? PlayerPrefs.GetFloat(resolution) : (float?)null;
            return snapshot;
        }
        sealed class OwnerRig { internal Grid Grid; internal ZoneRenderer Renderer; internal EnvironmentSpriteRenderer Environment; }
        sealed class KeyProbe : IInputProbe { readonly KeyCode key; internal KeyProbe(KeyCode value) { key = value; } public bool GetKeyDown(KeyCode value) => value == key; }
        sealed class CountingSaves : ISaveLoadService
        {
            internal int Calls;
            public bool BeginNewGame() { Calls++; return true; } public bool QuickSave() { Calls++; return true; }
            public bool QuickLoad() { Calls++; return true; } public bool HasQuickSave() { Calls++; return true; }
        }
        sealed class PrefsSnapshot
        {
            internal readonly Dictionary<string, int?> Ints = new Dictionary<string, int?>();
            internal readonly Dictionary<string, float?> Floats = new Dictionary<string, float?>();
            internal void AssertUnchanged(string except = null)
            {
                foreach (var pair in Ints)
                {
                    if (pair.Key == except) continue;
                    Assert.AreEqual(pair.Value.HasValue, PlayerPrefs.HasKey(pair.Key), "Key presence: " + pair.Key);
                    if (pair.Value.HasValue) Assert.AreEqual(pair.Value.Value, PlayerPrefs.GetInt(pair.Key), "Raw value: " + pair.Key);
                }
                foreach (var pair in Floats)
                {
                    Assert.AreEqual(pair.Value.HasValue, PlayerPrefs.HasKey(pair.Key), "Key presence: " + pair.Key);
                    if (pair.Value.HasValue) Assert.AreEqual(pair.Value.Value, PlayerPrefs.GetFloat(pair.Key), "Raw value: " + pair.Key);
                }
            }
            internal void Restore()
            {
                foreach (var pair in Ints) if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
                foreach (var pair in Floats) if (pair.Value.HasValue) PlayerPrefs.SetFloat(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
                PlayerPrefs.Save();
            }
        }
    }
}
