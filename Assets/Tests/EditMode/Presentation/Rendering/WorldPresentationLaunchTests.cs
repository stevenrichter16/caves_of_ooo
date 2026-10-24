using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Presentation.Effects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Actual bootstrap binding/load and actual Input System -> component callbacks.
    // DoStart's full scene generation and visible first paint remain ordinary Play gates.
    public sealed class WorldPresentationLaunchTests
    {
        const BindingFlags Hidden = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        const string ModeKey = WorldPresentationSettings.PreferenceKey;
        const string SpriteKey = WorldPresentationSettings.LegacySpritesPreferenceKey;
        readonly Dictionary<string, int?> ints = new Dictionary<string, int?>();
        readonly InputTestFixture devices = new InputTestFixture();
        readonly List<GameObject> roots = new List<GameObject>();
        WorldPresentationMode oldMode; bool oldSprites, old3D, oldLow; object oldRes, oldShadows;
        bool hadResolution; float savedResolution; SpellFxMode oldFx;
        Action<string> oldDirty, oldMessage; Action<int,int,string> oldCell; Func<int> oldTick;
        List<MessageLog.Entry> entries; List<string> announcements; int flash, serial;
        Keyboard keyboard; Gamepad pad; GamepadInputContext oldContext;

        [SetUp] public void Setup()
        {
            foreach (string key in new[] { ModeKey, SpriteKey, Village3DSettings.PreferenceKey,
                Village3DSettings.LowDetailPreferenceKey, Village3DSettings.ShadowsPreferenceKey, "CavesOfOoo.Fx.Mode" })
                ints[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            hadResolution = PlayerPrefs.HasKey(Village3DSettings.WorldResolutionPreferenceKey);
            savedResolution = PlayerPrefs.GetFloat(Village3DSettings.WorldResolutionPreferenceKey);
            oldMode = WorldPresentationSettings.Mode; oldSprites = WorldPresentationSettings.LegacySpritesEnabled;
            old3D = Village3DSettings.Enabled; oldLow = Village3DSettings.LowDetail;
            oldRes = Quality("resolutionOverride").GetValue(null); oldShadows = Quality("shadowsOverride").GetValue(null); oldFx = SpellFxSettings.Mode;
            oldDirty = ZoneRenderHooks.FullDirtyCallback; oldCell = ZoneRenderHooks.CellDirtyCallback;
            ZoneRenderHooks.FullDirtyCallback = null; ZoneRenderHooks.CellDirtyCallback = null;
            entries = MessageLog.GetAllEntries(); announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            flash = MessageLog.FlashStamp; serial = MessageLog.NextSerialValue; oldMessage = MessageLog.OnMessage; oldTick = MessageLog.TickProvider;
            MessageLog.OnMessage = null; MessageLog.TickProvider = null; MessageLog.Clear();
            PlayerPrefs.DeleteKey(ModeKey); PlayerPrefs.SetInt(SpriteKey, 0);
            PlayerPrefs.SetInt(Village3DSettings.PreferenceKey, 0); PlayerPrefs.SetInt(Village3DSettings.LowDetailPreferenceKey, 1);
            PlayerPrefs.SetFloat(Village3DSettings.WorldResolutionPreferenceKey, .625f); PlayerPrefs.DeleteKey(Village3DSettings.ShadowsPreferenceKey);
            WorldPresentationSettings.Mode = WorldPresentationMode.Flat16x24; WorldPresentationSettings.LegacySpritesEnabled = false;
            oldContext = (GamepadInputContext)typeof(NativeGamepadInput).GetField("context",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            devices.Setup(); keyboard = InputSystem.AddDevice<Keyboard>(); pad = InputSystem.AddDevice<Gamepad>(); keyboard.MakeCurrent();
            Keys();
        }
        [TearDown] public void Cleanup()
        {
            for (int i=roots.Count-1;i>=0;i--) if(roots[i]!=null) Object.DestroyImmediate(roots[i]); roots.Clear();
            devices.TearDown(); NativeGamepadInput.SetContext(oldContext);
            WorldPresentationSettings.Mode=oldMode; WorldPresentationSettings.LegacySpritesEnabled=oldSprites;
            Village3DSettings.Enabled=old3D; Village3DSettings.LowDetail=oldLow;
            Quality("resolutionOverride").SetValue(null,oldRes); Quality("shadowsOverride").SetValue(null,oldShadows); SpellFxSettings.Mode=oldFx;
            foreach(var pair in ints) if(pair.Value.HasValue)PlayerPrefs.SetInt(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);
            ints.Clear(); if(hadResolution)PlayerPrefs.SetFloat(Village3DSettings.WorldResolutionPreferenceKey,savedResolution);else PlayerPrefs.DeleteKey(Village3DSettings.WorldResolutionPreferenceKey);
            PlayerPrefs.Save(); ZoneRenderHooks.FullDirtyCallback=oldDirty; ZoneRenderHooks.CellDirtyCallback=oldCell;
            MessageLog.Restore(entries,announcements,flash,serial);MessageLog.OnMessage=oldMessage;MessageLog.TickProvider=oldTick;
        }

        [TestCase(0,false)] [TestCase(0,true)] [TestCase(1,false)] [TestCase(1,true)]
        public void ActualFreshBindingReplacesEitherStaleSceneFlagBeforeFirstZone(int mode,bool staleFlag)
        {
            using(var f=new HotbarSaveFixture())
            {
                var env=AttachEnvironment(f);
                var gridHost=Make("Fresh binding active Grid"); var grid=gridHost.AddComponent<Grid>();
                grid.cellSize=new Vector3(1,1,0); gridHost.SetActive(true);
                f.Root.transform.SetParent(gridHost.transform,false);
                Assert.False(f.Root.activeInHierarchy,"Keep bootstrap/renderer Awake out of this binding test.");
                Assert.AreSame(grid,f.Root.GetComponent<Tilemap>().GetComponentInParent<Grid>(),
                    "Projection requires a discoverable active parent Grid.");
                f.Renderer.FlatPixelModeEnabled=staleFlag; PlayerPrefs.SetInt(ModeKey,mode);
                Invoke(f.Bootstrap,"InitializeWorldPresentation");
                var zone=f.Input.CurrentZone; var player=f.Input.PlayerEntity;
                Invoke(f.Bootstrap,"BindWorldPresentation",zone,player);
                Assert.AreSame(zone,f.Renderer.CurrentZone);Assert.AreSame(player,f.Renderer.PlayerEntity);
                Assert.AreEqual(mode==0,f.Renderer.FlatPixelModeActive);Assert.AreEqual(mode==0,env.RenderingEnabled);
                Assert.AreEqual(new Vector3(1,mode==0?1.5f:1,0),grid.cellSize);
                Assert.AreEqual(0,PlayerPrefs.GetInt(SpriteKey));Assert.AreEqual(.625f,Village3DSettings.WorldResolutionScale);
                Assert.False(PlayerPrefs.HasKey(Village3DSettings.ShadowsPreferenceKey));
            }
        }
        [Test] public void BootstrapLoadsPreferencesOnlyOnceAndKeepsRuntimeChoiceOnRepeatedInitialization()
        {
            using(var f=new HotbarSaveFixture())
            {
                AttachEnvironment(f); Invoke(f.Bootstrap,"InitializeWorldPresentation");
                Assert.AreEqual(WorldPresentationMode.Flat16x24,WorldPresentationSettings.Mode);
                Assert.False(WorldPresentationSettings.LegacySpritesEnabled);Assert.False(Village3DSettings.Enabled);
                WorldPresentationSettings.Mode=WorldPresentationMode.Previous;Village3DSettings.Enabled=true;
                PlayerPrefs.SetInt(ModeKey,0);PlayerPrefs.SetInt(SpriteKey,1);PlayerPrefs.SetInt(Village3DSettings.PreferenceKey,0);
                Invoke(f.Bootstrap,"InitializeWorldPresentation");
                Assert.AreEqual(WorldPresentationMode.Previous,WorldPresentationSettings.Mode);Assert.True(Village3DSettings.Enabled);
                Assert.False(WorldPresentationSettings.LegacySpritesEnabled);Assert.AreEqual(0,PlayerPrefs.GetInt(ModeKey));
            }
        }
        [TestCase(0)] [TestCase(1)]
        public void ActualApplyLoadedGameUsesSessionModeAndRebindsLateSpriteControlWithoutReloadingPreferences(int runtimeMode)
        {
            using(var f=new HotbarSaveFixture())
            {
                var env=AttachEnvironment(f);var toggle=f.Root.AddComponent<SpriteEnvToggleController>();
                var unrelated=Make("Unrelated presentation").AddComponent<ZoneRenderer>();var unrelatedEnv=unrelated.gameObject.AddComponent<EnvironmentSpriteRenderer>();
                Set(unrelated,"_envSpriteRenderer",unrelatedEnv); unrelatedEnv.RenderingEnabled=false;
                Bind(toggle,unrelated); unrelatedEnv.RenderingEnabled=false;
                WorldPresentationSettings.Mode=(WorldPresentationMode)runtimeMode;WorldPresentationSettings.LegacySpritesEnabled=false;
                PlayerPrefs.SetInt(ModeKey,1-runtimeMode); PlayerPrefs.SetInt(SpriteKey,1);
                var state=HotbarSaveFixture.RoundTrip(HotbarSaveFixture.MakeState(0,0));f.Bootstrap.ApplyLoadedGame(state);
                Assert.AreSame(state.ZoneManager.ActiveZone,f.Renderer.CurrentZone);Assert.AreSame(state.Player,f.Renderer.PlayerEntity);
                Assert.AreEqual(runtimeMode==0,f.Renderer.FlatPixelModeActive);Assert.AreEqual(runtimeMode==0,env.RenderingEnabled);
                Assert.False(unrelatedEnv.RenderingEnabled);Assert.AreEqual((WorldPresentationMode)runtimeMode,WorldPresentationSettings.Mode);
                Assert.AreEqual(1-runtimeMode,PlayerPrefs.GetInt(ModeKey));Assert.AreEqual(1,PlayerPrefs.GetInt(SpriteKey));
                Keys();Keys(Key.Backslash);Invoke(toggle,"Update");
                Assert.True(WorldPresentationSettings.LegacySpritesEnabled);Assert.True(env.RenderingEnabled);
                Assert.False(unrelatedEnv.RenderingEnabled);Assert.AreEqual(17,state.TurnManager.TickCount);Assert.AreEqual(1000,state.TurnManager.GetEnergy(state.Player));
            }
        }
        [Test] public void ControlsAwakeNeverReloadsOldPreferencesOrOverwritesRuntimeChoice()
        {
            Village3DSettings.Enabled=true;Village3DSettings.LowDetail=false;WorldPresentationSettings.Mode=WorldPresentationMode.Previous;
            var control=Make("Unbound controls").AddComponent<Village3DControls>();Invoke(control,"Awake");
            Assert.True(Village3DSettings.Enabled);Assert.False(Village3DSettings.LowDetail);
            Assert.AreEqual(WorldPresentationMode.Previous,WorldPresentationSettings.Mode);Assert.Zero(MessageLog.Count);
        }
        [Test] public void ActualF11RoundTripsAndPersistsOnlyPresentationWithoutTakingATurn()
        {
            using(var f=new HotbarSaveFixture())
            {
                var env=AttachEnvironment(f); f.Renderer.SetZone(f.Input.CurrentZone);f.Renderer.ApplyWorldPresentation(WorldPresentationMode.Flat16x24,false);
                var control=f.Root.AddComponent<Village3DControls>();Invoke(control,"Awake");
                int ticks=f.Input.TurnManager.TickCount,energy=f.Input.TurnManager.GetEnergy(f.Input.PlayerEntity);var position=f.Input.CurrentZone.GetEntityPosition(f.Input.PlayerEntity);
                Keys(Key.F11);Invoke(control,"Update");Assert.AreEqual(WorldPresentationMode.Previous,WorldPresentationSettings.Mode);
                Assert.False(f.Renderer.FlatPixelModeActive);Assert.False(env.RenderingEnabled);Assert.AreEqual(1,PlayerPrefs.GetInt(ModeKey,-1));
                Keys(Key.F11);Invoke(control,"Update");Assert.AreEqual(WorldPresentationMode.Previous,WorldPresentationSettings.Mode);
                Keys();Keys(Key.F11);Invoke(control,"Update");Assert.True(f.Renderer.FlatPixelModeActive);Assert.True(env.RenderingEnabled);Assert.AreEqual(0,PlayerPrefs.GetInt(ModeKey,-1));
                Assert.AreEqual(0,PlayerPrefs.GetInt(Village3DSettings.PreferenceKey));Assert.AreEqual(0,PlayerPrefs.GetInt(SpriteKey));Assert.False(PlayerPrefs.HasKey(Village3DSettings.ShadowsPreferenceKey));
                Assert.AreEqual(ticks,f.Input.TurnManager.TickCount);Assert.AreEqual(energy,f.Input.TurnManager.GetEnergy(f.Input.PlayerEntity));Assert.AreEqual(position,f.Input.CurrentZone.GetEntityPosition(f.Input.PlayerEntity));
                Assert.AreEqual(2,MessageLog.Count);StringAssert.Contains("16x24",MessageLog.GetLast());
            }
        }
        [TestCase(Key.LeftShift)] [TestCase(Key.RightShift)]
        public void ActualShiftF11ChangesOnlyPreviousDetailAndNamesThatScope(Key shift)
        {
            var control=Make("Detail shortcut").AddComponent<Village3DControls>();Invoke(control,"Awake");Village3DSettings.LowDetail=false;
            Keys(shift,Key.F11);Invoke(control,"Update");Assert.True(Village3DSettings.LowDetail);
            Assert.AreEqual(WorldPresentationMode.Flat16x24,WorldPresentationSettings.Mode);Assert.False(PlayerPrefs.HasKey(ModeKey));
            StringAssert.Contains("Previous",MessageLog.GetLast());
        }
        [Test] public void UnboundF11CannotPersistAnUnappliedMode()
        {
            var control=Make("No world controls").AddComponent<Village3DControls>();Invoke(control,"Awake");
            Keys(Key.F11);Invoke(control,"Update");Assert.AreEqual(WorldPresentationMode.Flat16x24,WorldPresentationSettings.Mode);
            Assert.False(PlayerPrefs.HasKey(ModeKey));Assert.Zero(MessageLog.Count);
        }
        [Test] public void UnboundSpriteAwakeDoesNotTouchAnUnrelatedEnvironment()
        {
            var env=Make("Unrelated sprite world").AddComponent<EnvironmentSpriteRenderer>();env.RenderingEnabled=true;
            var toggle=Make("Unbound sprite controller").AddComponent<SpriteEnvToggleController>();
            var all=Object.FindObjectsByType<EnvironmentSpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var before=new bool[all.Length];for(int i=0;i<all.Length;i++){before[i]=all[i].RenderingEnabled;all[i].RenderingEnabled=true;}
            try
            {
                Invoke(toggle,"Awake");
                foreach(var renderer in all)Assert.True(renderer.RenderingEnabled,"Unbound Awake must not pick any scene renderer.");
                Assert.False(WorldPresentationSettings.LegacySpritesEnabled);Assert.AreEqual(0,PlayerPrefs.GetInt(SpriteKey));
            }
            finally {for(int i=0;i<all.Length;i++)if(all[i]!=null)all[i].RenderingEnabled=before[i];}
        }
        [TestCase(0)] [TestCase(1)]
        public void ActualBackslashEditsRetainedChoiceWithoutTurningOffFlat(int mode)
        {
            using(var f=new HotbarSaveFixture())
            {
                var env=AttachEnvironment(f);f.Renderer.SetZone(f.Input.CurrentZone);WorldPresentationSettings.Mode=(WorldPresentationMode)mode;
                f.Renderer.ApplyWorldPresentation(WorldPresentationSettings.Mode,false);
                var toggle=f.Root.AddComponent<SpriteEnvToggleController>();Bind(toggle,f.Renderer);
                for(int i=0;i<2;i++)
                {
                    Keys();Keys(Key.Backslash);Invoke(toggle,"Update");bool sprites=i==0;
                    Assert.AreEqual(sprites,WorldPresentationSettings.LegacySpritesEnabled);Assert.AreEqual(mode==0||sprites,env.RenderingEnabled);
                    Assert.AreEqual(sprites?1:0,PlayerPrefs.GetInt(SpriteKey));Assert.False(PlayerPrefs.HasKey(ModeKey));
                }
                Assert.AreEqual(mode==0,f.Renderer.FlatPixelModeActive);
            }
        }

        [Test] public void PreviousChoicesRemainExplicitAndEditableWithoutLeavingFlat()
        {
            using(var f=new HotbarSaveFixture())
            {
                var env=AttachEnvironment(f);f.Renderer.SetZone(f.Input.CurrentZone);f.Renderer.ApplyWorldPresentation(WorldPresentationMode.Flat16x24,false);
                Village3DSettings.Enabled=false;
                var ui=f.Root.AddComponent<PauseMenuUI>();f.Input.PauseMenuUI=ui;ui.Controller.Open();ui.Controller.ClickSelect(PauseMenuController.GraphicsIndex,null,null);
                Assert.AreEqual(10,ui.Controller.VisibleItemCount);
                int model=MenuConstant("Previous3DIndex"),sprites=MenuConstant("PreviousSpritesIndex");
                StringAssert.Contains("Previous 3D",ui.Controller.GetItemLabel(model));StringAssert.Contains("Previous sprites",ui.Controller.GetItemLabel(sprites));
                ui.Controller.ClickSelect(model,null,null);Assert.True(Village3DSettings.Enabled);Assert.AreEqual(1,PlayerPrefs.GetInt(Village3DSettings.PreferenceKey));
                Assert.True(f.Renderer.FlatPixelModeActive);Assert.False(PlayerPrefs.HasKey(ModeKey));
                ui.Controller.ClickSelect(sprites,null,null);Assert.True(WorldPresentationSettings.LegacySpritesEnabled);Assert.AreEqual(1,PlayerPrefs.GetInt(SpriteKey));
                Assert.True(f.Renderer.FlatPixelModeActive);Assert.True(env.RenderingEnabled);Assert.False(PlayerPrefs.HasKey(ModeKey));
                ui.Controller.ClickSelect(PauseMenuController.WorldPresentationIndex,null,null);Assert.False(f.Renderer.FlatPixelModeActive);Assert.True(env.RenderingEnabled);
                ui.Controller.ClickSelect(sprites,null,null);Assert.False(env.RenderingEnabled);Assert.False(WorldPresentationSettings.LegacySpritesEnabled);
                ui.Controller.ClickSelect(PauseMenuController.WorldPresentationIndex,null,null);Assert.True(f.Renderer.FlatPixelModeActive);Assert.True(env.RenderingEnabled);
                Assert.False(WorldPresentationSettings.LegacySpritesEnabled);Assert.True(Village3DSettings.Enabled);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualKeyboardAndNativePadPauseRouteChangesBothDirectionsWithoutWorldAction(bool controller)
        {
            using(var f=new HotbarSaveFixture())
            {
                var env=AttachEnvironment(f);f.Renderer.SetZone(f.Input.CurrentZone);f.Renderer.ApplyWorldPresentation(WorldPresentationMode.Flat16x24,false);
                var ui=f.Root.AddComponent<PauseMenuUI>();f.Input.PauseMenuUI=ui;
                pad.MakeCurrent();Pad(f.Input,new GamepadState());Keys();Invoke(f.Input,"Update");
                int tick=f.Input.TurnManager.TickCount,energy=f.Input.TurnManager.GetEnergy(f.Input.PlayerEntity);var position=f.Input.CurrentZone.GetEntityPosition(f.Input.PlayerEntity);
                void Press(Key key,GamepadButton button)
                {
                    if(controller){Pad(f.Input,new GamepadState());Pad(f.Input,new GamepadState().WithButton(button));}
                    else{Keys();Invoke(f.Input,"Update");Keys(key);Invoke(f.Input,"Update");}
                }
                // World Select/View is Pause; Start is Character, not Pause.
                Press(Key.Escape,GamepadButton.Select);Assert.True(ui.IsOpen);
                for(int i=0;i<PauseMenuController.GraphicsIndex;i++)Press(Key.DownArrow,GamepadButton.DpadDown);
                Press(Key.Enter,GamepadButton.South);Assert.True(ui.Controller.IsGraphicsOpen);
                Press(Key.RightArrow,GamepadButton.DpadRight);Assert.AreEqual(WorldPresentationMode.Previous,WorldPresentationSettings.Mode);Assert.False(env.RenderingEnabled);
                if(controller)Pad(f.Input,new GamepadState().WithButton(GamepadButton.DpadRight));else{Keys(Key.RightArrow);Invoke(f.Input,"Update");}
                Assert.AreEqual(WorldPresentationMode.Previous,WorldPresentationSettings.Mode,"Holding the same press must not switch again.");
                Press(Key.LeftArrow,GamepadButton.DpadLeft);Assert.True(f.Renderer.FlatPixelModeActive);Assert.True(env.RenderingEnabled);
                for(int i=0;i<PauseMenuController.GraphicsBackIndex;i++)Press(Key.DownArrow,GamepadButton.DpadDown);
                Assert.AreEqual(PauseMenuController.GraphicsBackIndex,ui.Controller.SelectedIndex);Assert.AreEqual("Back",ui.Controller.GetItemLabel(ui.Controller.SelectedIndex));
                Press(Key.Enter,GamepadButton.South);Assert.False(ui.Controller.IsGraphicsOpen);Assert.True(ui.IsOpen);
                Press(Key.Escape,GamepadButton.East);Assert.False(ui.IsOpen);
                Assert.AreEqual(tick,f.Input.TurnManager.TickCount);Assert.AreEqual(energy,f.Input.TurnManager.GetEnergy(f.Input.PlayerEntity));Assert.AreEqual(position,f.Input.CurrentZone.GetEntityPosition(f.Input.PlayerEntity));
                Assert.AreEqual(0,PlayerPrefs.GetInt(ModeKey));Assert.AreEqual(0,PlayerPrefs.GetInt(SpriteKey));
            }
        }
        [Test] public void TenRowPopupFitsLabelsAndClearsEveryExtraRowOnBackAndClose()
        {
            var grid=Make("Menu grid");grid.AddComponent<Grid>();
            var fg=Make("Menu text");fg.transform.SetParent(grid.transform);var bg=Make("Menu background");bg.transform.SetParent(grid.transform);
            var ui=Make("Presentation popup").AddComponent<PauseMenuUI>();ui.Tilemap=fg.AddComponent<Tilemap>();ui.BgTilemap=bg.AddComponent<Tilemap>();
            ui.Controller=new PauseMenuController();ui.Controller.Open();ui.Controller.ClickSelect(PauseMenuController.GraphicsIndex,null,null);
            Assert.AreEqual(10,ui.Controller.VisibleItemCount);
            for(int i=0;i<ui.Controller.VisibleItemCount;i++)Assert.LessOrEqual(ui.Controller.GetItemLabel(i).Length,24,"Label must remain inside 28-column popup.");
            Invoke(ui,"Render");Assert.AreEqual(28*14,Count(ui.BgTilemap));
            ui.Controller.Back();Invoke(ui,"Render");Assert.AreEqual(28*(PauseMenuController.ItemCount+4),Count(ui.BgTilemap));
            ui.Controller.Close();Invoke(ui,"ClearAll");Assert.Zero(Count(ui.Tilemap));Assert.Zero(Count(ui.BgTilemap));
        }
        static int MenuConstant(string name){var field=typeof(PauseMenuController).GetField(name,BindingFlags.Public|BindingFlags.Static);Assert.NotNull(field,name+" explicit retained-setting row is missing.");return (int)field.GetRawConstantValue();}
        static int Count(Tilemap map){int count=0;foreach(var cell in map.cellBounds.allPositionsWithin)if(map.HasTile(cell))count++;return count;}
        void Pad(InputHandler input,GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();Invoke(input,"Update");}

        static FieldInfo Quality(string name)=>typeof(Village3DSettings).GetField(name,BindingFlags.Static|BindingFlags.NonPublic);
        GameObject Make(string name){var go=new GameObject(name);go.SetActive(false);roots.Add(go);return go;}
        static object Invoke(object owner,string method,params object[] args){var m=owner.GetType().GetMethod(method,Hidden);Assert.NotNull(m,method+" must be an actual production lifecycle/binding operation.");return m.Invoke(owner,args);}
        static void Set(object owner,string name,object value)=>owner.GetType().GetField(name,Hidden).SetValue(owner,value);
        static EnvironmentSpriteRenderer AttachEnvironment(HotbarSaveFixture f)
        {
            var env=f.Root.AddComponent<EnvironmentSpriteRenderer>();env.RenderingEnabled=false;Set(f.Renderer,"_envSpriteRenderer",env);
            Set(f.Renderer,"_tilemap",f.Root.GetComponent<Tilemap>()??f.Root.AddComponent<Tilemap>());return env;
        }
        static void Bind(SpriteEnvToggleController controller,ZoneRenderer owner)=>Invoke(controller,"Bind",owner);
        void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
    }
}
