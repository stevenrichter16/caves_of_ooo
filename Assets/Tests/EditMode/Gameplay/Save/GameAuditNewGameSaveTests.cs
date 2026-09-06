using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    // Real serialized A/B sessions with production save adapter. The RED fallback
    // only permits owned unique slots before the new root API exists.
    internal sealed class NewGameSaveFixture : IDisposable
    {
        internal const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Dictionary<FieldInfo, object> _fields = new Dictionary<FieldInfo, object>();
        private readonly List<MessageLog.Entry> _log = MessageLog.GetAllEntries();
        private readonly List<string> _announcements = MessageLog.GetPendingAnnouncementsSnapshot();
        private readonly int _flash = MessageLog.FlashStamp, _serial = MessageLog.NextSerialValue;
        private readonly Dictionary<string,int> _rep = new Dictionary<string,int>(PlayerReputation.GetAll());
        private readonly TurnManager _turns = TurnManager.Active;
        private readonly bool _hadPref = PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey);
        private readonly string _pref = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
        private readonly PropertyInfo _rootProperty = typeof(SaveGameService).GetProperty("SaveRootOverride", Static);
        private readonly Queue<AsciiFxRequest> _ascii = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", Static).GetValue(null);
        private readonly List<SpellFxSequence> _spell = (List<SpellFxSequence>)typeof(SpellFxBus).GetField("Pending", Static).GetValue(null);
        private readonly AsciiFxRequest[] _oldAscii;
        private readonly SpellFxSequence[] _oldSpell;
        private readonly int _clearVersion = AsciiFxBus.ClearVersion;
        private readonly string _ownedRoot;
        public readonly string Root, OldID = "GA03b-old-" + Guid.NewGuid().ToString("N"), NewID = "GA03b-new-" + Guid.NewGuid().ToString("N");
        public readonly GameSessionState Old, Fresh;
        public GameSessionState Loaded;
        public int Captures;
        public readonly List<string> Messages = new List<string>();
        public readonly ISaveLoadService Service;
        private readonly Dictionary<string, byte[]> _oldBytes = new Dictionary<string, byte[]>();
        public string ActiveID => (string)typeof(SaveGameService).GetField("_activeGameID", Static).GetValue(null);
        public NewGameSaveFixture()
        {
            foreach (var field in typeof(SaveGameService).GetFields(Static)) if (!field.IsLiteral && !field.IsInitOnly) _fields[field] = field.GetValue(null);
            _oldAscii = _ascii.ToArray(); _ascii.Clear(); _oldSpell = _spell.ToArray(); _spell.Clear();
            _ownedRoot = Path.Combine(Path.GetTempPath(), "coo-newgame-tests-" + Guid.NewGuid().ToString("N"));
            Root = _rootProperty == null ? Path.Combine(Application.persistentDataPath, "Saves") : _ownedRoot;
            try
            {
            _rootProperty?.SetValue(null, Root);
            Old = State(OldID,111); Fresh = State(NewID,222);
            SaveGameService.RegisterRuntime(() => Old, state => Loaded = state);
            SaveGameService.SetActiveGameID(OldID);
            Assert.IsTrue(SaveGameService.QuickSave()); Assert.IsTrue(SaveGameService.SavePrimary());
            Assert.IsTrue(SaveGameService.QuickSave()); Assert.IsTrue(SaveGameService.SavePrimary());
            foreach (string path in Directory.GetFiles(Path.Combine(Root,OldID))) _oldBytes[path] = File.ReadAllBytes(path);
            Register(() => { Captures++; return Fresh; }, NewID);
            Service = (ISaveLoadService)Activator.CreateInstance(typeof(BootMenuController).Assembly.GetType("CavesOfOoo.Rendering.SaveGameServiceAdapter"), true);
            }
            catch { Dispose(); throw; }
        }
        public static GameSessionState State(string id,int seed) => new GameSessionState
        { GameID = id, GameVersion = "audit", WorldSeed = seed, Player = new Entity { ID = id + "-actor", BlueprintName = "SaveProbe" } };
        public void Register(Func<GameSessionState> capture,string id)
        {
            var method = typeof(SaveGameService).GetMethods(Static).Single(m => m.Name == "RegisterRuntime");
            object[] args = method.GetParameters().Length == 3 ? new object[] { capture, (Action<GameSessionState>)(s => Loaded = s), id }
                : new object[] { capture, (Action<GameSessionState>)(s => Loaded = s) };
            method.Invoke(null,args);
        }
        public bool Begin()
        {
            var method = typeof(SaveGameService).GetMethod("BeginNewGame",Static);
            Assert.NotNull(method,"A new-game operation must bind independently of capture.");
            return (bool)method.Invoke(null,null);
        }
        public BootMenuController Choose(params KeyCode[] keys)
        {
            var boot = new BootMenuController(); Assert.IsTrue(boot.TryActivate(SaveGameService.HasQuickSave(),Messages.Add));
            boot.Tick(new Keys(keys),Service,Messages.Add); return boot;
        }
        public void Load(string route)
        {
            if (route == "F6") new SaveLoadInputController().Tick(new Keys(KeyCode.F6),Service,Messages.Add);
            else if (route == "pause") { var pause = new PauseMenuController(); pause.Open(); pause.ClickSelect(PauseMenuController.LoadIndex,Service,Messages.Add); Assert.IsFalse(pause.IsOpen); }
            else { var death = new DeathScreenController(); death.Activate(Messages.Add); death.Tick(new Keys(KeyCode.L),Service,new NoRestart(),Messages.Add); Assert.IsFalse(death.IsActive); }
        }
        public void OldUnchanged()
        {
            CollectionAssert.AreEquivalent(_oldBytes.Keys,Directory.GetFiles(Path.Combine(Root,OldID)));
            foreach (var entry in _oldBytes) CollectionAssert.AreEqual(entry.Value,File.ReadAllBytes(entry.Key),entry.Key);
        }
        public void ExpectNewLoaded()
        { Assert.NotNull(Loaded); Assert.AreEqual(NewID,Loaded.GameID); Assert.AreEqual(222,Loaded.WorldSeed); Assert.AreEqual(Fresh.Player.ID,Loaded.Player.ID); OldUnchanged(); }
        public void Dispose()
        {
            foreach (var field in _fields) field.Key.SetValue(null,field.Value);
            if (_hadPref) PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,_pref); else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey); PlayerPrefs.Save();
            MessageLog.Restore(_log,_announcements,_flash,_serial); PlayerReputation.Restore(_rep);
            typeof(TurnManager).GetProperty("Active",Static).SetValue(null,_turns);
            _ascii.Clear(); foreach (var item in _oldAscii) _ascii.Enqueue(item); _spell.Clear(); _spell.AddRange(_oldSpell);
            typeof(AsciiFxBus).GetField("<ClearVersion>k__BackingField",Static).SetValue(null,_clearVersion);
            foreach (string id in new[] { OldID,NewID }) { string path = Path.Combine(Root,id); if (Directory.Exists(path)) Directory.Delete(path,true); else if (File.Exists(path)) File.Delete(path); }
            if (Directory.Exists(_ownedRoot)) Directory.Delete(_ownedRoot,true);
        }
        internal sealed class Keys : IInputProbe
        { private readonly HashSet<KeyCode> _keys; public Keys(params KeyCode[] keys) { _keys = new HashSet<KeyCode>(keys); } public bool GetKeyDown(KeyCode key) => _keys.Contains(key); }
        private sealed class NoRestart : ISceneRestarter { public void Restart() { Assert.Fail("Load must not restart."); } }
    }

    public class GameAuditNewGameSaveTests
    {
        [TestCase("F6")] [TestCase("pause")] [TestCase("death")]
        public void NewGame_ImmediatelyLoadsFreshCharacterThroughEveryControl(string route)
        {
            using (var f = new NewGameSaveFixture())
            { Assert.IsFalse(f.Choose(KeyCode.N).IsActive); Assert.AreEqual(f.NewID,f.ActiveID); Assert.AreEqual(1,f.Captures); f.Load(route); f.ExpectNewLoaded(); }
        }
        [TestCase(false)] [TestCase(true)]
        public void Continue_PreservesPreviousCharacterAndWinsCollision(bool collide)
        {
            using (var f = new NewGameSaveFixture())
            { var menu = f.Choose(collide ? new[] {KeyCode.C,KeyCode.N} : new[] {KeyCode.C}); Assert.IsFalse(menu.IsActive); Assert.AreEqual(f.OldID,f.Loaded.GameID); Assert.AreEqual(111,f.Loaded.WorldSeed); Assert.AreEqual(f.OldID,f.ActiveID); Assert.IsFalse(Directory.Exists(Path.Combine(f.Root,f.NewID))); f.OldUnchanged(); }
        }
        [TestCase("missing")] [TestCase("null")] [TestCase("throw")] [TestCase("write")]
        public void InitialCheckpointFailure_RetainsFreshBindingAndAllowsLaterSave(string failure)
        {
            using (var f = new NewGameSaveFixture())
            {
                if (failure == "missing") f.Register(null,f.NewID);
                if (failure == "null") f.Register(() => null,f.NewID);
                if (failure == "throw") f.Register(() => throw new InvalidOperationException("capture probe"),f.NewID);
                string blocker = Path.Combine(f.Root,f.NewID);
                if (failure == "write") File.WriteAllText(blocker,"block directory");
                if (failure == "throw" || failure == "write") LogAssert.Expect(LogType.Error,new Regex("\\[Save\\].*failed"));
                Assert.IsFalse(f.Begin()); Assert.AreEqual(f.NewID,f.ActiveID); Assert.IsFalse(SaveGameService.HasQuickSave()); Assert.IsFalse(SaveGameService.QuickLoad()); Assert.IsNull(f.Loaded); f.OldUnchanged();
                if (File.Exists(blocker)) File.Delete(blocker);
                f.Register(() => f.Fresh,f.NewID); Assert.IsTrue(SaveGameService.QuickSave()); f.Load("F6"); f.ExpectNewLoaded();
            }
        }
        [Test]
        public void NewGameCheckpointFailure_EntersPlayWithTruthfulRetryMessage()
        {
            using (var f = new NewGameSaveFixture())
            { f.Register(() => null,f.NewID); Assert.IsFalse(f.Choose(KeyCode.N).IsActive); Assert.AreEqual(f.NewID,f.ActiveID); StringAssert.Contains("F5",string.Join(" ",f.Messages)); StringAssert.Contains("failed",string.Join(" ",f.Messages).ToLowerInvariant()); f.OldUnchanged(); }
        }
        [Test]
        public void InactiveOrRepeatedNewKey_DoesNotRecaptureOrCreateAnotherSave()
        {
            using (var f = new NewGameSaveFixture())
            {
                var inactive = new BootMenuController(); inactive.Tick(new NewGameSaveFixture.Keys(KeyCode.N),f.Service,f.Messages.Add); Assert.AreEqual(0,f.Captures); Assert.AreEqual(f.OldID,f.ActiveID);
                var chosen = f.Choose(KeyCode.N); Assert.AreEqual(1,f.Captures); var bytes = File.ReadAllBytes(Path.Combine(f.Root,f.NewID,"Quick.sav.gz")); chosen.Tick(new NewGameSaveFixture.Keys(KeyCode.N),f.Service,f.Messages.Add);
                Assert.AreEqual(1,f.Captures); CollectionAssert.AreEqual(bytes,File.ReadAllBytes(Path.Combine(f.Root,f.NewID,"Quick.sav.gz"))); f.OldUnchanged();
            }
        }
        [TestCase(null)] [TestCase("")]
        public void EmptyCapturedIdentity_UsesIndependentlyRegisteredFreshIdentity(string missing)
        { using (var f = new NewGameSaveFixture()) { f.Fresh.GameID = missing; Assert.IsTrue(f.Begin()); Assert.AreEqual(f.NewID,f.ActiveID); Assert.AreEqual(f.NewID,f.Fresh.GameID); f.Load("F6"); f.ExpectNewLoaded(); } }
        [Test]
        public void MismatchedCapturedIdentity_RefusesWithoutRebindingPreviousCharacter()
        { using (var f = new NewGameSaveFixture()) { f.Register(() => f.Old,f.NewID); Assert.IsFalse(f.Begin()); Assert.AreEqual(f.NewID,f.ActiveID); Assert.IsFalse(SaveGameService.HasQuickSave()); f.OldUnchanged(); } }
        [TestCase(false)] [TestCase(true)]
        public void CaptureMutationThenFailure_CannotRebindThePreviousCharacter(bool throws)
        {
            using (var f = new NewGameSaveFixture())
            {
                f.Register(() => { SaveGameService.SetActiveGameID(f.OldID); if (throws) throw new InvalidOperationException("mutated capture"); return null; },f.NewID);
                if (throws) LogAssert.Expect(LogType.Error,new Regex("\\[Save\\].*failed"));
                Assert.IsFalse(f.Begin()); Assert.AreEqual(f.NewID,f.ActiveID); f.OldUnchanged();
            }
        }
        [Test]
        public void NewSessionWithoutPriorSave_ClearsStaleStaticLoadBinding()
        { using (var f = new NewGameSaveFixture()) { SaveGameService.SetActiveGameID("stale-"+Guid.NewGuid().ToString("N")); Assert.IsFalse(SaveGameService.HasQuickSave()); Assert.IsTrue(f.Begin()); Assert.AreEqual(f.NewID,f.ActiveID); f.Load("F6"); f.ExpectNewLoaded(); } }
        [Test]
        public void EmptyIsolatedRoot_CannotDiscoverOutsideSaveNamedByPreference()
        {
            using (var f = new NewGameSaveFixture())
            {
                var root = typeof(SaveGameService).GetProperty("SaveRootOverride",NewGameSaveFixture.Static); Assert.NotNull(root);
                string empty = Path.Combine(f.Root,"empty-root"); Directory.CreateDirectory(empty); root.SetValue(null,empty);
                SaveGameService.SetActiveGameID(f.NewID); SaveGameService.ResolveActiveGameIDOnBoot(); Assert.AreEqual(f.NewID,f.ActiveID); Assert.IsFalse(SaveGameService.HasQuickSave()); f.OldUnchanged();
            }
        }
    }
}
