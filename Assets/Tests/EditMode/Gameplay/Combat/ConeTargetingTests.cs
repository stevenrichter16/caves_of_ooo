using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM2 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §5, P2) — the
    /// cone shape.
    ///
    /// <para>Every spell in the game today is a bolt, a line, a nova or
    /// a chain (<c>SpellTargeting.TraceBeam</c> /
    /// <c>GetCreaturesInRadius</c> / <c>FindChainTargets</c>). The
    /// flamethrower (Flame Jet) and jet blast the request names are
    /// unbuildable without a cone, so it is a genuinely new primitive
    /// rather than a reskin of an existing one.</para>
    ///
    /// <para><b>Shape contract.</b> A cone opens from the cell in front
    /// of the caster and widens by one cell per step, so length 3 covers
    /// 1 + 3 + 5 = 9 cells at full width. Walls occlude: a solid cell
    /// stops the cone from spreading past it along that ray, which keeps
    /// a flamethrower from licking around a corner. The caster is never
    /// in their own cone.</para>
    /// </summary>
    [TestFixture]
    public class ConeTargetingTests
    {
        private Zone _zone;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _zone = new Zone();
        }

        private Entity Creature(string name, int x, int y)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Value = 20 };
            e.AddPart(new RenderPart { DisplayName = name, RenderString = "@" });
            _zone.AddEntity(e, x, y);
            return e;
        }

        private Entity Wall(int x, int y)
        {
            var w = new Entity { ID = $"wall{x}_{y}", BlueprintName = "Wall" };
            w.Tags["Solid"] = "";
            w.AddPart(new RenderPart { DisplayName = "wall", RenderString = "#" });
            _zone.AddEntity(w, x, y);
            return w;
        }

        private static bool Has(List<Entity> hits, Entity e) => hits.Contains(e);

        // ── Shape ────────────────────────────────────────────────

        [Test]
        public void Cone_HitsTargetsDirectlyAhead()
        {
            var caster = Creature("caster", 10, 10);
            var near = Creature("near", 11, 10);
            var far = Creature("far", 13, 10);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsTrue(Has(hits, near), "the cell in front is in the cone");
            Assert.IsTrue(Has(hits, far), "and so is the far end at length 3");
        }

        [Test]
        public void Cone_WidensWithDistance()
        {
            var caster = Creature("caster", 10, 10);
            // Directly beside the caster's facing cell — NOT in a cone
            // that has only just opened.
            var beside = Creature("beside", 11, 11);
            // Two steps out, the cone is 3 wide, so this offset is in.
            var wide = Creature("wide", 12, 11);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsFalse(Has(hits, beside), "the cone is 1 wide at step 1");
            Assert.IsTrue(Has(hits, wide), "and 3 wide at step 2");
        }

        [Test]
        public void Cone_ExcludesTheCaster()
        {
            var caster = Creature("caster", 10, 10);
            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsFalse(Has(hits, caster), "you are never in your own flamethrower");
        }

        [Test]
        public void Cone_ExcludesTargetsBehindTheCaster()
        {
            // Counter-check to the shape tests: a cone must be
            // directional. If this failed the "cone" would be a nova.
            var caster = Creature("caster", 10, 10);
            var behind = Creature("behind", 8, 10);
            var beside = Creature("flank", 10, 13);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsFalse(Has(hits, behind), "nothing behind you burns");
            Assert.IsFalse(Has(hits, beside), "nor directly to the side");
        }

        [Test]
        public void Cone_RespectsLength()
        {
            var caster = Creature("caster", 10, 10);
            var justOutside = Creature("justOutside", 14, 10);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsFalse(Has(hits, justOutside),
                "length 3 reaches x+3, not x+4");
        }

        [Test]
        public void Cone_WorksInAllFourCardinalDirections()
        {
            var caster = Creature("caster", 10, 10);
            var north = Creature("north", 10, 8);
            var south = Creature("south", 10, 12);
            var east = Creature("east", 12, 10);
            var west = Creature("west", 8, 10);

            Assert.IsTrue(Has(SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, 0, -1, 3), north));
            Assert.IsTrue(Has(SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, 0, 1, 3), south));
            Assert.IsTrue(Has(SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, 1, 0, 3), east));
            Assert.IsTrue(Has(SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, -1, 0, 3), west));
        }

        [Test]
        public void Cone_WorksDiagonally()
        {
            var caster = Creature("caster", 10, 10);
            var diag = Creature("diag", 12, 12);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 1, length: 3);

            Assert.IsTrue(Has(hits, diag), "diagonal facings spray too");
        }

        // ── Occlusion ────────────────────────────────────────────

        [Test]
        public void Cone_IsBlockedByAWall()
        {
            var caster = Creature("caster", 10, 10);
            Wall(11, 10);
            var shielded = Creature("shielded", 12, 10);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsFalse(Has(hits, shielded),
                "a wall between you and the target stops the spray");
        }

        [Test]
        public void Cone_WallOccludesOnlyItsOwnRay()
        {
            // Counter-check to the occlusion test: one wall must not
            // cancel the whole cone. If it did, a flamethrower would be
            // useless in any room with a pillar.
            //
            // The pillar sits OFF the centre line, at step 2. (An
            // earlier draft of this test put it at (11,10) — the cone's
            // one-cell apex — and rightly failed: everything the spray
            // emits must pass through that cell. That case is now pinned
            // separately as Cone_WallAtTheApexBlocksEverything.)
            var caster = Creature("caster", 10, 10);
            Wall(12, 11);
            var centreLine = Creature("centreLine", 12, 10);
            var behindPillar = Creature("behindPillar", 13, 12);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsTrue(Has(hits, centreLine),
                "the rest of the cone still lands past a pillar");
            Assert.IsFalse(Has(hits, behindPillar),
                "but the pillar's own shadow stays covered");
        }

        [Test]
        public void Cone_WallAtTheApexBlocksEverything()
        {
            // The apex is one cell wide, so a wall there is the whole
            // cone's only exit. Being jammed against a pillar disabling
            // your flamethrower is intended, teachable positioning —
            // step aside and it works.
            var caster = Creature("caster", 10, 10);
            Wall(11, 10);
            var anyone = Creature("anyone", 12, 11);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsEmpty(hits,
                "a wall in the cone's mouth stops the whole spray");
            Assert.IsFalse(Has(hits, anyone));
        }

        // ── Guards ───────────────────────────────────────────────

        [Test]
        public void Cone_NullZoneOrZeroDirectionReturnsEmpty()
        {
            var caster = Creature("caster", 10, 10);
            Creature("bystander", 11, 10);

            Assert.IsEmpty(SpellTargeting.GetCreaturesInCone(
                null, caster, 10, 10, 1, 0, 3));
            Assert.IsEmpty(SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, 0, 0, 3),
                "no facing means no cone, not a nova");
            Assert.IsEmpty(SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, 1, 0, 0),
                "zero length is a no-op");
        }

        [Test]
        public void Cone_ClipsAtTheZoneEdgeWithoutThrowing()
        {
            // Zone is 80x25 (Zone.cs:13-14). Fire off the east edge.
            var caster = Creature("caster", 78, 12);
            var edge = Creature("edge", 79, 12);

            List<Entity> hits = null;
            Assert.DoesNotThrow(() => hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 78, 12, dx: 1, dy: 0, length: 5));
            Assert.IsTrue(Has(hits, edge), "what is on the map still burns");
        }

        [Test]
        public void Cone_ReportsEachCreatureOnce()
        {
            // Overlapping rays must not double-report a target, or a
            // per-hit effect would apply twice from one cast.
            var caster = Creature("caster", 10, 10);
            var target = Creature("target", 12, 10);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 4);

            int count = 0;
            foreach (var h in hits) if (h == target) count++;
            Assert.AreEqual(1, count, "one creature, one entry");
        }

        [Test]
        public void Cone_IgnoresNonCreatures()
        {
            var caster = Creature("caster", 10, 10);
            var item = new Entity { ID = "loot", BlueprintName = "Dagger" };
            item.AddPart(new RenderPart { DisplayName = "dagger", RenderString = "/" });
            _zone.AddEntity(item, 11, 10);

            var hits = SpellTargeting.GetCreaturesInCone(
                _zone, caster, 10, 10, dx: 1, dy: 0, length: 3);

            Assert.IsFalse(Has(hits, item), "dropped loot is not a target");
        }
    }
}
