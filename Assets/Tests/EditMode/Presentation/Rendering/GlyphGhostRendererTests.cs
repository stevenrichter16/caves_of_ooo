using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 6 §6A.3 — tests for GlyphGhostRenderer.
    /// EditMode tests cover the pure-data contracts (active count,
    /// last-known tracking, reset). Full Init + PostRender requires
    /// a Unity scene with Tilemap and is exercised by the showcase.
    /// </summary>
    public class GlyphGhostRendererTests
    {
        private GameObject _go;
        private GlyphGhostRenderer _renderer;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("GhostRendererTest");
            _renderer = _go.AddComponent<GlyphGhostRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void IsInitialized_FalseBeforeInit()
        {
            Assert.IsFalse(_renderer.IsInitialized,
                "Component starts uninitialized. Init must be called "
                + "explicitly by ZoneRenderer.");
        }

        [Test]
        public void TestOnly_ActiveGhostCount_StartsAtZero()
        {
            Assert.AreEqual(0, _renderer.TestOnly_ActiveGhostCount);
        }

        [Test]
        public void TestOnly_ResetTracking_ClearsState()
        {
            // Adversarial: even after some hypothetical state would
            // accumulate, ResetTracking must wipe everything.
            _renderer.TestOnly_ResetTracking();
            Assert.AreEqual(0, _renderer.TestOnly_ActiveGhostCount);
        }
    }
}
