using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Presentation.Effects;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// 4B.2 — Tests for CrtToggleController. See
    /// <c>Docs/GRAPHICS-PASS4.md</c> §4B.2.
    /// </summary>
    public class CrtToggleControllerTests
    {
        private GameObject _toggleGo;
        private CrtToggleController _toggle;
        private GameObject _fakeVolume;

        [SetUp]
        public void Setup()
        {
            _toggleGo = new GameObject("ToggleTest");
            _toggle = _toggleGo.AddComponent<CrtToggleController>();
            _fakeVolume = new GameObject("FakeCrtVolume");
            _fakeVolume.SetActive(false); // start disabled (matches scene default)
            _toggle.TestOnly_SetCrtVolume(_fakeVolume);
        }

        [TearDown]
        public void TearDown()
        {
            if (_fakeVolume != null) Object.DestroyImmediate(_fakeVolume);
            if (_toggleGo != null) Object.DestroyImmediate(_toggleGo);
        }

        [Test]
        public void Toggle_FromOff_TurnsOn()
        {
            Assert.IsFalse(_toggle.TestOnly_IsCrtActive, "Setup: starts off.");
            bool result = _toggle.TestOnly_Toggle();
            Assert.IsTrue(result);
            Assert.IsTrue(_toggle.TestOnly_IsCrtActive, "After toggle, CRT is on.");
        }

        [Test]
        public void Toggle_FromOn_TurnsOff()
        {
            _fakeVolume.SetActive(true);
            Assert.IsTrue(_toggle.TestOnly_IsCrtActive);
            bool result = _toggle.TestOnly_Toggle();
            Assert.IsFalse(result);
            Assert.IsFalse(_toggle.TestOnly_IsCrtActive);
        }

        [Test]
        public void Toggle_TwoTimes_ReturnsToOriginal()
        {
            bool start = _toggle.TestOnly_IsCrtActive;
            _toggle.TestOnly_Toggle();
            _toggle.TestOnly_Toggle();
            Assert.AreEqual(start, _toggle.TestOnly_IsCrtActive,
                "Two toggles return to original state.");
        }

        [Test]
        public void Toggle_WithoutVolumeReference_DoesNotCrash()
        {
            // Defensive: if the controller never found a CRT volume
            // (e.g., wrong scene), the toggle is a no-op, NOT a crash.
            _toggle.TestOnly_SetCrtVolume(null);
            Assert.DoesNotThrow(() => _toggle.TestOnly_Toggle());
            Assert.IsFalse(_toggle.TestOnly_IsCrtActive,
                "With no volume bound, IsCrtActive returns false defensively.");
        }
    }
}
