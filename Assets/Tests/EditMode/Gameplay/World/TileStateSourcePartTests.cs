using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/FELLING-W1-W2-PLAN.md SM1 — <see cref="TileStateSourcePart"/>
    /// gains a residue channel, mirroring its shipped <c>Coating</c>
    /// branch exactly. Before this, the part could keep a cell wet or
    /// hot/cold/charged but had no way to leave a non-liquid mark
    /// (petals, ash) — the residue layer existed on
    /// <see cref="ZoneTileState"/> (<c>WriteResidue</c>) with nothing
    /// upstream of it ever calling in.
    /// </summary>
    public class TileStateSourcePartTests
    {
        [Test]
        public void Seed_WithResidueSet_WritesTheResidueLayer()
        {
            var zone = new Zone();
            var part = new TileStateSourcePart { Residue = "petals", ResidueTurns = 50 };

            part.Seed(zone, 5, 5);

            Assert.IsTrue(zone.TileState.HasResidue(5, 5, "petals"));
        }

        [Test]
        public void Seed_WithNoResidue_WritesNothingToTheResidueLayer()
        {
            // Counter-check: a source with Coating set but no Residue
            // must not accidentally write an empty/garbage residue —
            // the existing Coating-only behavior is unchanged.
            var zone = new Zone();
            var part = new TileStateSourcePart { Coating = "water", CoatingTurns = 3 };

            part.Seed(zone, 5, 5);

            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "water"));
            Assert.IsFalse(zone.TileState.HasResidue(5, 5, "petals"));
        }

        [Test]
        public void Seed_WithResidueTurnsZero_WritesNothing()
        {
            // Mirrors the Coating branch's own `CoatingTurns > 0` guard —
            // a residue id with no duration is not a residue.
            var zone = new Zone();
            var part = new TileStateSourcePart { Residue = "petals", ResidueTurns = 0 };

            part.Seed(zone, 5, 5);

            Assert.IsFalse(zone.TileState.HasResidue(5, 5, "petals"));
        }

        [Test]
        public void Seed_ResidueAndCoatingTogether_WritesBoth()
        {
            var zone = new Zone();
            var part = new TileStateSourcePart
            {
                Coating = "water", CoatingTurns = 4,
                Residue = "petals", ResidueTurns = 50,
            };

            part.Seed(zone, 5, 5);

            Assert.IsTrue(zone.TileState.HasCoating(5, 5, "water"));
            Assert.IsTrue(zone.TileState.HasResidue(5, 5, "petals"));
        }

        [Test]
        public void Seed_ResidueDoesNotRadiate_EvenWhenRadiatesEnergyIsSet()
        {
            // Residues never radiate — same rule as Coatings (docstring
            // on RadiatesEnergy: "a tar seep that oiled its neighbours
            // would creep across the zone"). A future author flipping
            // RadiatesEnergy on a flower patch must not spread petals
            // sideways.
            var zone = new Zone();
            var part = new TileStateSourcePart
            {
                Residue = "petals", ResidueTurns = 50,
                RadiatesEnergy = true,
            };

            part.Seed(zone, 5, 5);

            Assert.IsFalse(zone.TileState.HasResidue(6, 5, "petals"));
            Assert.IsFalse(zone.TileState.HasResidue(4, 5, "petals"));
        }
    }
}
