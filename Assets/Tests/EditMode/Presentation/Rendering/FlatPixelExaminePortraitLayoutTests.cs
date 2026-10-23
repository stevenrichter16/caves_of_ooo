using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Pure output-pixel geometry. Must be wired into AnnouncementUI.OnGUI;
    // a passing helper alone does not establish real popup integration.
    public sealed class FlatPixelExaminePortraitLayoutTests
    {
        Texture2D atlas;
        [SetUp] public void Setup() => atlas = new Texture2D(192, 24);
        [TearDown] public void Cleanup() => Object.DestroyImmediate(atlas);

        [TestCase(870, 40, 176, 96, 4)]
        [TestCase(1305, 60, 264, 144, 6)]
        [TestCase(25.25f, 35.75f, 109.5f, 73.25f, 3)]
        public void AtlasSubrectangleKeepsSixteenByTwentyFourPixelsAtIntegerScale(
            float x, float y, float width, float height, int scale)
        {
            var slot = new Rect(x, y, width, height);
            var portrait = new ExaminePortrait(atlas, new Rect(10f / 12f, 0, 1f / 12f, 1), Color.white, false);
            var actual = Measure(slot, portrait);
            Assert.That(actual.width, Is.EqualTo(16 * scale).Within(.001f));
            Assert.That(actual.height, Is.EqualTo(24 * scale).Within(.001f));
            Assert.That(actual.x, Is.EqualTo(Mathf.Round(actual.x)).Within(.001f));
            Assert.That(actual.y, Is.EqualTo(Mathf.Round(actual.y)).Within(.001f));
            Inside(slot, actual);
            Assert.That(Mathf.Abs(actual.center.x - slot.center.x), Is.LessThanOrEqualTo(.51f));
            Assert.That(Mathf.Abs(actual.center.y - slot.center.y), Is.LessThanOrEqualTo(.51f));
        }

        [Test]
        public void UndersizedSlotContainsTheImageWithoutChangingItsAspect()
        {
            var slot = new Rect(4, 5, 5, 8);
            var portrait = new ExaminePortrait(atlas, new Rect(0, 0, 1f / 12f, 1), Color.white, false);
            var actual = Measure(slot, portrait);
            Assert.Greater(actual.width, 0); Assert.That(actual.width / actual.height, Is.EqualTo(2f / 3f).Within(.001f));
            Inside(slot, actual);
        }

        [Test]
        public void ExistingModelCaptureKeepsItsCenteredSquareLayout()
        {
            var portrait = new ExaminePortrait(atlas, new Rect(0, 0, 1, 1), Color.white, true);
            var actual = Measure(new Rect(20, 30, 120, 80), portrait);
            Assert.AreEqual(new Rect(40, 30, 80, 80), actual);
        }

        [Test]
        public void MissingPortraitDoesNotInventAVisibleRectangle()
        { Assert.AreEqual(Rect.zero, Measure(new Rect(20, 30, 120, 80), default)); }

        [TestCase(0, 96)] [TestCase(96, 0)] [TestCase(-1, 96)]
        public void EmptyOrNegativeSlotDoesNotDrawOutsideItsArea(float width, float height)
        {
            var portrait = new ExaminePortrait(atlas, new Rect(0, 0, 1f / 12f, 1), Color.white, false);
            Assert.AreEqual(Rect.zero, Measure(new Rect(20, 30, width, height), portrait));
        }

        [Test]
        public void FractionalTightSlotStillContainsTheWholeImage()
        {
            var slot = new Rect(.3f, .3f, 16.4f, 24.4f);
            var portrait = new ExaminePortrait(atlas, new Rect(0, 0, 1f / 12f, 1), Color.white, false);
            var actual = Measure(slot, portrait);
            Assert.Greater(actual.width, 0);
            Assert.That(actual.width / actual.height, Is.EqualTo(2f / 3f).Within(.001f));
            Inside(slot, actual);
        }

        static Rect Measure(Rect slot, ExaminePortrait portrait)
            => AnnouncementUI.MeasurePortraitRect(slot, portrait);
        static void Inside(Rect slot, Rect image)
        {
            Assert.GreaterOrEqual(image.xMin + .001f, slot.xMin);
            Assert.GreaterOrEqual(image.yMin + .001f, slot.yMin);
            Assert.LessOrEqual(image.xMax - .001f, slot.xMax);
            Assert.LessOrEqual(image.yMax - .001f, slot.yMax);
        }
    }
}
