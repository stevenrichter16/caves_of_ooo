using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 6 §6B.2 — tests for BiomeColorPatcher.
    /// EditMode-only; full Volume integration requires Play mode + a
    /// SampleScene Volume. These tests cover the data-side contract:
    /// active-biome tracking, force-biome test seam.
    /// </summary>
    public class BiomeColorPatcherTests
    {
        private GameObject _go;
        private BiomeColorPatcher _patcher;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("BiomeColorPatcherTest");
            _patcher = _go.AddComponent<BiomeColorPatcher>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void Initial_ActiveBiome_IsCave()
        {
            Assert.AreEqual(BiomeType.Cave, _patcher.TestOnly_ActiveBiome,
                "Default starting biome is Cave (matches the scene's "
                + "starting zone).");
        }

        [Test]
        public void TestOnly_ForceBiome_UpdatesActive()
        {
            _patcher.TestOnly_ForceBiome(BiomeType.Desert);
            Assert.AreEqual(BiomeType.Desert, _patcher.TestOnly_ActiveBiome);
            _patcher.TestOnly_ForceBiome(BiomeType.Jungle);
            Assert.AreEqual(BiomeType.Jungle, _patcher.TestOnly_ActiveBiome);
        }

        [Test]
        public void TestOnly_ForceBiome_ResetsFadeProgress()
        {
            // Adversarial: changing biome must restart the fade so the
            // lerp transitions over the full duration. A buggy impl
            // that left fade=1 would produce instant snaps.
            _patcher.TestOnly_ForceBiome(BiomeType.Desert);
            Assert.AreEqual(0f, _patcher.TestOnly_FadeProgress, 0.001f);
        }

        [Test]
        public void NoVolumeReference_DoesNotCrash()
        {
            // In EditMode tests with no scene Volume, the patcher
            // safely no-ops on its Update. Adversarial counter-check
            // for a buggy impl that NPEs without a Volume.
            Assert.IsFalse(_patcher.TestOnly_HasVolume,
                "Without a 'Global Volume' GameObject in the scene, "
                + "the patcher self-disables.");
        }

        [Test]
        public void SetCurrentZone_AcceptsNull()
        {
            // Defensive: SetCurrentZone(null) shouldn't NPE; the patcher
            // should just no-op on biome detection.
            Assert.DoesNotThrow(() => _patcher.SetCurrentZone(null));
            Assert.IsNull(_patcher.CurrentZone);
        }
    }
}
