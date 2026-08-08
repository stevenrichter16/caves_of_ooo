using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ALPHA-READINESS item 3 — save-lifeline (P0, verified). Saves were
    /// written to a fresh per-boot GUID directory that NOTHING could
    /// rediscover after an app restart (SetActiveGameID had zero
    /// production callers), and a corrupted save file threw an unhandled
    /// exception instead of failing gracefully. These tests pin the
    /// discovery scan and the corruption-safe load path.
    /// </summary>
    [TestFixture]
    public class AlphaSaveLifelineTests
    {
        private string _tempRoot;

        [SetUp]
        public void Setup()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(),
                "coo-savetest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            SaveGameService.SetActiveGameID("Default");
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }

        private void MakeSaveDir(string gameID, DateTime utcStamp, bool withSaveFile = true)
        {
            string dir = Path.Combine(_tempRoot, gameID);
            Directory.CreateDirectory(dir);
            var info = new SaveGameInfo
            {
                GameID = gameID,
                SaveTimestampUtc = utcStamp.ToString("o"),
            };
            File.WriteAllText(Path.Combine(dir, "Quick.json"), JsonUtility.ToJson(info));
            if (withSaveFile)
                File.WriteAllBytes(Path.Combine(dir, "Quick.sav.gz"), new byte[] { 0x1f, 0x8b, 0x08 });
        }

        // ── SM3: boot-time discovery ─────────────────────────────

        [Test]
        public void Discovery_PicksTheNewestQuickSave()
        {
            MakeSaveDir("game-old", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            MakeSaveDir("game-new", new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
            MakeSaveDir("game-mid", new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual("game-new", SaveGameService.DiscoverLatestGameID(_tempRoot),
                "the orphaned-GUID-directory bug: boot must find the newest save");
        }

        [Test]
        public void Discovery_IgnoresDirsWithoutTheActualSaveFile()
        {
            // Metadata without a .sav.gz (crash mid-save, manual deletion)
            // must not win discovery.
            MakeSaveDir("game-real", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            MakeSaveDir("game-ghost", new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                withSaveFile: false);

            Assert.AreEqual("game-real", SaveGameService.DiscoverLatestGameID(_tempRoot));
        }

        [Test]
        public void Discovery_EmptyOrMissingRoot_ReturnsNull()
        {
            Assert.IsNull(SaveGameService.DiscoverLatestGameID(_tempRoot),
                "no save dirs -> null (fresh install)");
            Assert.IsNull(SaveGameService.DiscoverLatestGameID(
                Path.Combine(_tempRoot, "does-not-exist")));
        }

        [Test]
        public void Discovery_ToleratesMalformedMetadata()
        {
            MakeSaveDir("game-good", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
            string bad = Path.Combine(_tempRoot, "game-bad");
            Directory.CreateDirectory(bad);
            File.WriteAllText(Path.Combine(bad, "Quick.json"), "{not valid json!!");
            File.WriteAllBytes(Path.Combine(bad, "Quick.sav.gz"), new byte[] { 0x1f, 0x8b });

            Assert.AreEqual("game-good", SaveGameService.DiscoverLatestGameID(_tempRoot),
                "a malformed metadata file must be skipped, not crash discovery");
        }

        // ── SM1: corruption-safe load ────────────────────────────

        [Test]
        public void QuickLoad_CorruptedSaveFile_ReturnsFalse_NoThrow()
        {
            // Verifier finding: LoadSlot had no try/catch, so real
            // corruption was an unhandled exception inside
            // InputHandler.Update — the "save may be corrupted" message
            // was unreachable. Pin the graceful path.
            string gameID = "coo-corrupt-" + Guid.NewGuid().ToString("N");
            string dir = Path.Combine(Application.persistentDataPath, "Saves", gameID);
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "Quick.sav.gz"),
                    "this is not a gzip stream");
                SaveGameService.RegisterRuntime(
                    captureCurrent: () => null,
                    applyLoaded: _ => { });
                SaveGameService.SetActiveGameID(gameID);

                LogAssert.ignoreFailingMessages = true;
                bool ok = false;
                try
                {
                    Assert.DoesNotThrow(() => ok = SaveGameService.QuickLoad(),
                        "corruption must fail gracefully, not explode in Update()");
                }
                finally
                {
                    LogAssert.ignoreFailingMessages = false;
                }
                Assert.IsFalse(ok, "corrupted save must report failure");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
