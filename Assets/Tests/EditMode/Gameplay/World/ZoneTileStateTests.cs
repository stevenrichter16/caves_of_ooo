using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// PALIMPSEST P1 (Docs/PALIMPSEST-PROTOTYPE-PLAN.md §4) — the tile
    /// state layer, the bridge the rest of the prototype stands on.
    ///
    /// <para>The shipped combat system is actor-centric: statuses live
    /// on creatures. The Palimpsest design is tile-centric: coatings,
    /// residues, energy and clouds live on the ground and persist after
    /// the ability that wrote them. This layer is what makes the second
    /// thing possible without disturbing the first.</para>
    ///
    /// <para><b>Sparse on purpose.</b> A zone is 80×25 = 2000 cells and
    /// the overwhelming majority never carry state. A dense per-cell
    /// array would cost memory and save size for nothing, and would make
    /// per-turn decay a 2000-cell scan instead of an iteration over the
    /// handful of tiles actually written.</para>
    ///
    /// <para>P1 deliberately ships NO gameplay. It proves the layer can
    /// be written, read, decayed and round-tripped — nothing reacts yet.</para>
    /// </summary>
    public class ZoneTileStateTests
    {
        private ZoneTileState _tiles;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _tiles = new ZoneTileState();
        }

        // ── Writing and reading ──────────────────────────────────

        [Test]
        public void ABlankTile_HasNothingOnIt()
        {
            Assert.IsFalse(_tiles.Has(5, 5), "nothing has been written");
            Assert.IsNull(_tiles.Get(5, 5), "and a blank tile allocates nothing");
            Assert.AreEqual(0, _tiles.WrittenCount);
        }

        [Test]
        public void WritingACoating_MakesItReadable()
        {
            _tiles.WriteCoating(5, 5, "water", 6);

            Assert.IsTrue(_tiles.Has(5, 5));
            Assert.IsTrue(_tiles.HasCoating(5, 5, "water"));
            Assert.AreEqual(6, _tiles.CoatingTurns(5, 5, "water"));
        }

        [Test]
        public void CoatingsCoexist_TheyDoNotOverwrite()
        {
            // Spec §6: a tile can genuinely be Wet AND Oiled. Forcing one
            // to replace the other would delete the single most
            // interesting setup in the system.
            _tiles.WriteCoating(5, 5, "water", 6);
            _tiles.WriteCoating(5, 5, "oil", 8);

            Assert.IsTrue(_tiles.HasCoating(5, 5, "water"));
            Assert.IsTrue(_tiles.HasCoating(5, 5, "oil"));
        }

        [Test]
        public void RewritingTheSameCoating_TakesTheLongerDuration()
        {
            _tiles.WriteCoating(5, 5, "water", 3);
            _tiles.WriteCoating(5, 5, "water", 9);
            Assert.AreEqual(9, _tiles.CoatingTurns(5, 5, "water"),
                "refreshing a coating extends it");

            _tiles.WriteCoating(5, 5, "water", 2);
            Assert.AreEqual(9, _tiles.CoatingTurns(5, 5, "water"),
                "but a weaker write must not shorten it");
        }

        [Test]
        public void ResiduesAreSeparateFromCoatings()
        {
            // Different layer, different rules — embers are not a liquid.
            _tiles.WriteCoating(5, 5, "water", 6);
            _tiles.WriteResidue(5, 5, "embers", 4);

            Assert.IsTrue(_tiles.HasCoating(5, 5, "water"));
            Assert.IsTrue(_tiles.HasResidue(5, 5, "embers"));
            Assert.IsFalse(_tiles.HasCoating(5, 5, "embers"),
                "a residue must not answer a coating query");
        }

        // ── Energy: the coarse, legible channels ────────────────

        [Test]
        public void EnergyClampsToTheSpecRange()
        {
            // Spec §8: Heat/Cold/Charge are 0..2 on tiles. Deliberately
            // coarser than ThermalPart's continuous simulation, because
            // the player has to be able to reason about it.
            _tiles.AddHeat(5, 5, 5);
            Assert.AreEqual(2, _tiles.Heat(5, 5), "clamped at 2");

            _tiles.AddCharge(5, 5, -3);
            Assert.AreEqual(0, _tiles.Charge(5, 5), "and floored at 0");
        }

        [Test]
        public void HeatAndColdCancel_ChargeIsUnaffected()
        {
            // Spec §12. Prevents nonsense states like "extremely hot and
            // extremely frozen", while leaving Cold+Charged available —
            // which is a genuinely useful combination.
            _tiles.AddHeat(5, 5, 2);
            _tiles.AddCharge(5, 5, 2);
            _tiles.AddCold(5, 5, 1);

            _tiles.CancelOpposedEnergy(5, 5);

            Assert.AreEqual(1, _tiles.Heat(5, 5), "2 heat - 1 cold = 1 heat");
            Assert.AreEqual(0, _tiles.Cold(5, 5));
            Assert.AreEqual(2, _tiles.Charge(5, 5), "charge is independent");
        }

        [Test]
        public void EqualHeatAndCold_LeaveNeither()
        {
            _tiles.AddHeat(5, 5, 1);
            _tiles.AddCold(5, 5, 1);
            _tiles.CancelOpposedEnergy(5, 5);

            Assert.AreEqual(0, _tiles.Heat(5, 5));
            Assert.AreEqual(0, _tiles.Cold(5, 5));
        }

        // ── Clouds ───────────────────────────────────────────────

        [Test]
        public void ATileHoldsOneCloud_AndTheNewestWins()
        {
            _tiles.WriteCloud(5, 5, "steam", 2);
            Assert.AreEqual("steam", _tiles.Cloud(5, 5));

            _tiles.WriteCloud(5, 5, "smoke", 3);
            Assert.AreEqual("smoke", _tiles.Cloud(5, 5),
                "one cloud per tile — the newest replaces");
        }

        // ── Decay: the reason this is sparse ─────────────────────

        [Test]
        public void LayersDecayAndThenVanish()
        {
            _tiles.WriteCoating(5, 5, "water", 2);

            _tiles.Tick();
            Assert.IsTrue(_tiles.HasCoating(5, 5, "water"), "1 turn left");

            _tiles.Tick();
            Assert.IsFalse(_tiles.HasCoating(5, 5, "water"), "expired");
        }

        [Test]
        public void ATileWithNothingLeft_IsReclaimed()
        {
            // The sparse store's whole justification: written tiles must
            // return to costing nothing, or a long fight leaks entries
            // for every puddle that ever existed.
            _tiles.WriteCoating(5, 5, "water", 1);
            Assert.AreEqual(1, _tiles.WrittenCount);

            _tiles.Tick();

            Assert.AreEqual(0, _tiles.WrittenCount,
                "an emptied tile is dropped from the store, not kept blank");
            Assert.IsFalse(_tiles.Has(5, 5));
        }

        [Test]
        public void EnergyDecaysToo_SoAChargeDoesNotSitForever()
        {
            _tiles.AddCharge(5, 5, 2);
            _tiles.Tick();
            Assert.AreEqual(1, _tiles.Charge(5, 5), "energy bleeds off a turn at a time");
            _tiles.Tick();
            Assert.AreEqual(0, _tiles.Charge(5, 5));
            Assert.AreEqual(0, _tiles.WrittenCount, "and the tile is reclaimed");
        }

        [Test]
        public void DecayTouchesOnlyWrittenTiles()
        {
            // Counter-check on the performance claim: Tick must be
            // proportional to what was written, not to the 2000 cells in
            // a zone.
            _tiles.WriteCoating(1, 1, "water", 5);
            _tiles.WriteCoating(70, 20, "oil", 5);
            Assert.AreEqual(2, _tiles.WrittenCount);

            int visited = _tiles.Tick();

            Assert.AreEqual(2, visited,
                "exactly the written tiles were visited, not the whole grid");
        }

        // ── Clearing — the substrate Scrape (P7) will stand on ───

        [Test]
        public void ClearRemovesEveryLayer_AndReportsHowMany()
        {
            // P7's Scrape refunds ink by layer count, so the count has to
            // be trustworthy before that economy is built on it.
            _tiles.WriteCoating(5, 5, "water", 6);
            _tiles.WriteCoating(5, 5, "oil", 8);
            _tiles.WriteResidue(5, 5, "embers", 4);
            _tiles.AddCharge(5, 5, 2);
            _tiles.WriteCloud(5, 5, "steam", 2);

            int removed = _tiles.Clear(5, 5);

            Assert.AreEqual(5, removed,
                "two coatings, one residue, one energy channel, one cloud");
            Assert.IsFalse(_tiles.Has(5, 5));
            Assert.AreEqual(0, _tiles.WrittenCount);
        }

        [Test]
        public void ClearingABlankTile_RemovesNothing()
        {
            Assert.AreEqual(0, _tiles.Clear(5, 5),
                "so Scrape can refuse to refund for scraping bare ground");
        }

        [Test]
        public void CountLayers_MatchesWhatClearWouldRemove()
        {
            // Scrape needs to preview its refund before committing.
            _tiles.WriteCoating(5, 5, "water", 6);
            _tiles.WriteResidue(5, 5, "ash", 8);
            _tiles.AddHeat(5, 5, 1);

            Assert.AreEqual(3, _tiles.CountLayers(5, 5));
            Assert.AreEqual(_tiles.CountLayers(5, 5), _tiles.Clear(5, 5),
                "preview and commit must agree");
        }

        // ── Bounds and guards ────────────────────────────────────

        [Test]
        public void OutOfBoundsWrites_AreIgnoredNotCrashes()
        {
            Assert.DoesNotThrow(() => _tiles.WriteCoating(-1, 5, "water", 6));
            Assert.DoesNotThrow(() => _tiles.WriteCoating(5, -1, "water", 6));
            Assert.DoesNotThrow(() => _tiles.WriteCoating(9999, 5, "water", 6));
            Assert.AreEqual(0, _tiles.WrittenCount, "nothing was stored");
        }

        [Test]
        public void EmptyOrZeroDurationWrites_AreNoOps()
        {
            _tiles.WriteCoating(5, 5, "", 6);
            _tiles.WriteCoating(5, 5, "water", 0);
            _tiles.WriteCoating(5, 5, null, 6);

            Assert.AreEqual(0, _tiles.WrittenCount);
        }

        [Test]
        public void DistinctTilesDoNotShareState()
        {
            // Guards the key packing: y*Width+x must not collide.
            _tiles.WriteCoating(1, 2, "water", 6);
            _tiles.WriteCoating(2, 1, "oil", 6);

            Assert.IsTrue(_tiles.HasCoating(1, 2, "water"));
            Assert.IsFalse(_tiles.HasCoating(1, 2, "oil"));
            Assert.IsTrue(_tiles.HasCoating(2, 1, "oil"));
            Assert.IsFalse(_tiles.HasCoating(2, 1, "water"));
        }

        // ── Save / load ──────────────────────────────────────────

        [Test]
        public void StateRoundTripsThroughSave()
        {
            _tiles.WriteCoating(5, 5, "water", 6);
            _tiles.WriteResidue(7, 3, "embers", 4);
            _tiles.AddCharge(5, 5, 2);
            _tiles.WriteCloud(7, 3, "steam", 2);

            string saved = _tiles.ToSaveString();
            var loaded = new ZoneTileState();
            loaded.LoadFromString(saved);

            Assert.AreEqual(2, loaded.WrittenCount);
            Assert.IsTrue(loaded.HasCoating(5, 5, "water"));
            Assert.AreEqual(6, loaded.CoatingTurns(5, 5, "water"));
            Assert.AreEqual(2, loaded.Charge(5, 5));
            Assert.IsTrue(loaded.HasResidue(7, 3, "embers"));
            Assert.AreEqual("steam", loaded.Cloud(7, 3));
        }

        [Test]
        public void LoadingGarbage_IsGracefulNotACrash()
        {
            var loaded = new ZoneTileState();
            Assert.DoesNotThrow(() => loaded.LoadFromString(null));
            Assert.DoesNotThrow(() => loaded.LoadFromString(""));
            Assert.DoesNotThrow(() => loaded.LoadFromString("{ not json"));
            Assert.AreEqual(0, loaded.WrittenCount);
        }
    }
}
