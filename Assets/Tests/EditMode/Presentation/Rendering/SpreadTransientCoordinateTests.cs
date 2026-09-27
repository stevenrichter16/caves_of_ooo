using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadTransientCoordinateTests
    {
        [TestCase(80, 0, 0, 1, false)]
        [TestCase(-1, 1, 79, 0, false)]
        [TestCase(17, 53687091, 1, 0, false)]
        [TestCase(80, 0, 0, 1, true)]
        [TestCase(-1, 1, 79, 0, true)]
        [TestCase(17, 53687091, 1, 0, true)]
        public void InvalidCoordinateCannotClaimOrEraseAnotherValidCell(
            int invalidX, int invalidY, int validX, int validY, bool draw)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            using (var frame = new SpreadParticleFrame(
                ((SpawnRing3DPresenter)f.Presenter).ActiveSurface, f.Library.WorldMaterial))
            {
                Assert.False(f.Zone.InBounds(invalidX, invalidY));
                Assert.True(f.Zone.InBounds(validX, validY));
                // Proves the tested alias rather than relying on a chosen large number.
                Assert.AreEqual(validY * Zone.Width + validX, unchecked(invalidY * Zone.Width + invalidX));
                f.Zone.GetCell(validX, validY).IsVisible = f.Zone.GetCell(validX, validY).Explored = true;
                frame.BeginFrame(f.Zone);
                Assert.True(frame.TryDraw(validX, validY, '!', "&Y"));
                Assert.True(frame.TryGet(validX, validY, out var original));
                if (draw)
                    Assert.False(frame.TryDraw(invalidX, invalidY, '*', "&Y"));
                else
                    Assert.False(frame.TryGet(invalidX, invalidY, out _), "An invalid address cannot claim the valid cell's root.");
                Assert.True(frame.TryGet(validX, validY, out var retained), "An invalid address cannot retire the valid cell's root.");
                Assert.AreSame(original, retained);
                frame.EndFrame();
                Assert.True(frame.TryGet(validX, validY, out retained));
                Assert.AreSame(original, retained);
                Assert.AreEqual(Village3DProjection.CellCentre(validX, validY), retained.transform.position);
            }
        }
    }
}
