using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.StarterSpell3DCaptureFixture;

namespace CavesOfOoo.Tests
{
    /// <summary>Real routed casts distinguish physical spell contacts from the
    /// ambient reactions captured by the same synchronous resolution scope.</summary>
    public sealed class StarterSpellAudioGeometryTests
    {
        [SetUp] public void Setup() => F.Setup();
        [TearDown] public void Cleanup() => F.Cleanup();

        static SpellFxSequence Cast(StarterAudioFixture f, Entity caster, BaseSkillPart skill, int dx, int dy)
        {
            var skills = caster.GetPart<SkillsPart>();
            Assert.IsTrue(skills.AddSkill(skill, "audio geometry audit"));
            var spec = skill.DeclareActivatedAbility(caster);
            Assert.IsTrue(skills.TryRouteSkillCommand(spec.Command, f.Zone, new F.FixedRng(0),
                dx, dy, f.Zone.GetEntityCell(caster), null, spec.Range, out bool blocks));
            Assert.IsTrue(blocks, "This must be a consumed real spell, not an invented copied result.");
            return F.Single();
        }

        [Test] public void ActualDiagonalJetWingHitPlaysWetSlapBeyondChebyshevTwo()
        {
            using (var f = new StarterAudioFixture())
            {
                var caster = F.Actor(f.Zone, "caster", 10, 10);
                var target = F.Actor(f.Zone, "diagonal-wing", 11, 13);
                var sequence = Cast(f, caster, new Hydromancy_JetBlast(), 1, 1);
                var hit = sequence.Targets.Single(t => t.TargetId == target.ID);
                Assert.AreEqual(new Point(11, 13), hit.Cell);
                Assert.AreEqual(new Point(11, 13), hit.AnchorCell, "Displacement must not rewrite the copied original anchor.");
                Assert.IsTrue(hit.IsDirectTarget, "The actual selected diagonal cone owner must retain direct provenance.");
                Assert.AreEqual(2, hit.Damage); Assert.IsTrue(hit.Moved);
                Assert.Contains("WetEffect", hit.AppliedEffects.ToArray());
                Assert.Contains(hit.Cell, sequence.AffectedCells.ToArray());
                Assert.AreEqual(new Point(12, 12), sequence.Path.Last());
                Assert.IsTrue(f.Play(sequence)); f.Contact();
                Assert.AreEqual(4, f.Audio.ContactLayerCount);
                Assert.IsTrue(f.Post.Any(s => s.clip.name.Contains("wet_slap")),
                    "Diagonal cone wings lie three cells away on one axis but were really hit.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualCardinalJetDoesNotUseVisibleAmbientReactionAsItsHiddenDirectHit(bool blockedInsideCone)
        {
            using (var f = new StarterAudioFixture())
            {
                var caster = F.Actor(f.Zone, "caster", 10, 10);
                var direct = F.Actor(f.Zone, "hidden-direct", 11, 10);
                int ambientY = blockedInsideCone ? 11 : 12;
                var ambient = F.Actor(f.Zone, "visible-ambient", 12, ambientY);
                if (blockedInsideCone) F.Wall(f.Zone, 12, ambientY);
                f.Zone.GetCell(11, 10).IsVisible = false;
                f.Zone.TileState.WriteCoating(12, ambientY, "water", 6);
                f.Zone.TileState.AddCold(12, ambientY, 1);
                var sequence = Cast(f, caster, new Hydromancy_JetBlast(), 1, 0);
                var directHit = sequence.Targets.Single(t => t.TargetId == direct.ID);
                var ambientHit = sequence.Targets.Single(t => t.TargetId == ambient.ID);
                Assert.IsTrue(directHit.IsDirectTarget);
                Assert.IsFalse(ambientHit.IsDirectTarget, "A resolved material reaction must not acquire selected-target provenance.");
                Assert.AreEqual(new Point(11, 10), directHit.AnchorCell);
                Assert.AreEqual(new Point(12, ambientY), ambientHit.AnchorCell);
                Assert.AreEqual(2, directHit.Damage);
                Assert.Contains("WetEffect", directHit.AppliedEffects.ToArray());
                Assert.Contains("FrozenEffect", ambientHit.AppliedEffects.ToArray());
                Assert.AreEqual(0, ambientHit.Damage);
                Assert.Contains(ambientHit.Cell, sequence.AffectedCells.ToArray());
                Assert.IsTrue(sequence.Reactions.Any(r => r.Kind == "reaction" && r.Value == "freeze_water"
                    && r.Cell.Equals(ambientHit.Cell) && r.Amount > 0));
                Assert.IsTrue(f.Play(sequence)); f.Contact();
                Assert.AreEqual(3, f.Audio.ContactLayerCount);
                Assert.IsFalse(f.Post.Any(s => s.clip.name.Contains("wet_slap")),
                    "A nearby cold reaction outside the cone or inside its blocked cell must not reveal the hidden direct impact.");
            }
        }

        [TestCase(true, true, true, 3)] [TestCase(false, true, true, 1)]
        [TestCase(true, false, true, 1)] [TestCase(true, true, false, 1)] [TestCase(true, false, false, 1)]
        public void ActualOffAnchorRimeFreezingUsesItsOwnVisibleGroundWriteAndRejectsAmbientIce(
            bool waterAtAnchor, bool anchorVisible, bool bodyVisible, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                var caster = F.Actor(f.Zone, "caster", 10, 10);
                var prop = F.Prop(f.Zone, "off-anchor-prop", 13, 9, "0,0;0,1;1,0;1,1");
                var water = waterAtAnchor ? new Point(13, 9) : new Point(20, 15);
                f.Zone.TileState.WriteCoating(water.X, water.Y, "water", 6);
                // The countercase is a genuine ambient reaction during the cast,
                // while Rime itself still writes cold only beneath its own target.
                if (!waterAtAnchor) f.Zone.TileState.AddCold(water.X, water.Y, 1);
                var sequence = Cast(f, caster, new Cryomancy_RimeGrip(), 1, 0);
                var hit = sequence.Targets.Single(t => t.TargetId == prop.ID);
                Assert.AreEqual(new Point(13, 10), hit.Cell);
                Assert.AreEqual(new Point(13, 9), hit.AnchorCell);
                Assert.IsTrue(hit.IsDirectTarget, "Rime selected this physical body before its anchor-water reaction.");
                Assert.AreEqual(hit.Cell, sequence.Path.Last());
                Assert.AreEqual((13, 9), f.Zone.GetEntityPosition(prop));
                Assert.AreEqual(96, prop.GetPart<DestructiblePart>().HP);
                Assert.IsFalse(hit.Died); Assert.IsFalse(hit.Moved);
                Assert.IsFalse(hit.AppliedEffects.Contains("FrozenEffect"),
                    "A durable non-creature prop takes structural damage; only the ground actually freezes.");
                Assert.Greater(f.Zone.TileState.CoatingTurns(water.X, water.Y, "ice"), 0);
                Assert.IsTrue(sequence.Reactions.Any(r => r.Kind == "reaction" && r.Value == "freeze_water"
                    && r.Cell.Equals(water) && r.Amount > 0));
                Assert.IsFalse(sequence.Path.Contains(water), "Both ice sites deliberately lie off the copied line.");
                Assert.IsTrue(f.Play(sequence));
                f.Zone.GetCell(13, 9).IsVisible = anchorVisible;
                f.Zone.GetCell(13, 10).IsVisible = bodyVisible;
                f.Contact();
                Assert.AreEqual(expected, f.Audio.ContactLayerCount);
                bool audibleIce = waterAtAnchor && anchorVisible && bodyVisible;
                Assert.AreEqual(audibleIce, f.Post.Any(s => s.clip.name.Contains("brittle_lock")));
                Assert.AreEqual(audibleIce, f.Post.Any(s => s.clip.name.Contains("settling_fragments")));
            }
        }
    }
}
