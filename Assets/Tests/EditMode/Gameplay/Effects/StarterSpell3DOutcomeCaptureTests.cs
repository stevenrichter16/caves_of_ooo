using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual starter resolution must tell presentation where contact happened,
    /// without moving simulation-owned coatings or inventing a successful status.</summary>
    public sealed class StarterSpell3DOutcomeCaptureTests
    {
        [SetUp] public void Setup() => StarterSpell3DCaptureFixture.Setup();
        [TearDown] public void Cleanup() => StarterSpell3DCaptureFixture.Cleanup();

        [TestCase(typeof(Pyromancy_EmberSpit), true)]
        [TestCase(typeof(Pyromancy_EmberSpit), false)]
        [TestCase(typeof(Cryomancy_RimeGrip), true)]
        [TestCase(typeof(Cryomancy_RimeGrip), false)]
        [TestCase(typeof(Spellcraft_Calm), true)]
        [TestCase(typeof(Spellcraft_Calm), false)]
        public void FirstBodyContactClipsThePath_AndItsAbsentBodyControlDoesNotHit(Type spell, bool body)
        {
            var zone = new Zone();
            var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            var target = StarterSpell3DCaptureFixture.Actor(zone, "target", 8, 9,
                body ? "0,0;0,1;1,1" : null);
            target.AddPart(new BrainPart());
            var context = StarterSpell3DCaptureFixture.Context(zone, caster);
            bool consumed = StarterSpell3DCaptureFixture.Skill(spell).OnCommand(context);
            Assert.AreEqual(body || spell == typeof(Spellcraft_Calm), consumed);
            var sequences = SpellFxBus.Drain();
            if (!consumed) { Assert.IsEmpty(sequences); Assert.AreEqual(100, target.GetStatValue("Hitpoints")); return; }
            Assert.AreEqual(1, sequences.Count);
            if (!body) { Assert.IsEmpty(sequences[0].Targets); return; }
            var sequence = sequences[0];
            Assert.AreEqual(1, sequence.Targets.Count, "One owner, despite several physical cells.");
            Assert.AreEqual(new Point(8, 10), sequence.Path.Last(), "A line ends at its first physical contact, not beyond its body.");
            Assert.AreEqual(new Point(8, 10), sequence.Targets[0].Cell);
            Assert.AreEqual(new Point(8, 10), sequence.Targets[0].FinalCell);
            Assert.IsFalse(sequence.Targets[0].Moved);
            // Ground state is still the skill's actual canonical-anchor write.
            if (spell == typeof(Pyromancy_EmberSpit))
                Assert.IsTrue(sequence.Reactions.Any(r => r.Kind == "residue" && r.Value == "embers" && r.Cell.Equals(new Point(8, 9))));
        }

        [TestCase(typeof(Hydromancy_JetBlast), false)]
        [TestCase(typeof(Hydromancy_JetBlast), true)]
        [TestCase(typeof(Galvanism_GroundSurge), false)]
        [TestCase(typeof(Galvanism_GroundSurge), true)]
        public void OffAnchorPushKeepsTheContactOffset_AndBlockedControlReportsNoMovement(Type spell, bool blocked)
        {
            var zone = new Zone();
            var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            var target = StarterSpell3DCaptureFixture.Actor(zone, "target", 6, 9, "0,1;1,1");
            if (blocked) StarterSpell3DCaptureFixture.Wall(zone, 7, 9);
            Assert.IsTrue(StarterSpell3DCaptureFixture.Skill(spell).OnCommand(StarterSpell3DCaptureFixture.Context(zone, caster)));
            var sequence = StarterSpell3DCaptureFixture.Single();
            Assert.AreEqual(1, sequence.Targets.Count);
            var result = sequence.Targets[0];
            Assert.AreEqual(spell == typeof(Hydromancy_JetBlast) ? 2 : 6, result.Damage, "One damage resolution per owner.");
            Assert.AreEqual(new Point(6, 10), result.Cell);
            Assert.AreEqual(blocked ? (6, 9) : (7, 8), zone.GetEntityPosition(target), "Preserve the shipped anchor-based shove direction.");
            Assert.AreEqual(blocked ? new Point(6, 10) : new Point(7, 9), result.FinalCell);
            Assert.AreEqual(!blocked, result.Moved);
            if (spell == typeof(Hydromancy_JetBlast))
            {
                Assert.Contains(nameof(WetEffect), result.AppliedEffects.ToArray());
                var anchor = zone.GetEntityPosition(target);
                Assert.IsTrue(sequence.Reactions.Any(r => r.Kind == "coating" && r.Value == "water"
                    && r.Cell.Equals(new Point(anchor.x, anchor.y))), "Capture correction does not move native ground writes.");
            }
        }

        [TestCase(typeof(Pyromancy_EmberSpit))]
        [TestCase(typeof(Cryomancy_RimeGrip))]
        [TestCase(typeof(Hydromancy_JetBlast))]
        [TestCase(typeof(Galvanism_GroundSurge))]
        public void LethalPhysicalContactRetainsOneResultWithoutFalseDisplacementOrLivingStatus(Type spell)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            var target = StarterSpell3DCaptureFixture.Actor(zone, "target", 6, 9, "0,1;1,1", 1);
            Assert.IsTrue(StarterSpell3DCaptureFixture.Skill(spell).OnCommand(StarterSpell3DCaptureFixture.Context(zone, caster)));
            var result = StarterSpell3DCaptureFixture.Single().Targets.Single();
            Assert.AreEqual(new Point(6, 10), result.Cell);
            Assert.AreEqual(1, result.Damage);
            Assert.IsTrue(result.Died); Assert.IsFalse(result.Moved);
            Assert.IsEmpty(result.AppliedEffects);
            Assert.IsNull(zone.GetEntityCell(target));
        }

        [TestCase(true)] [TestCase(false)]
        public void FlamingHandsReportsSelectedBodyCell_AndEmptyControlStillCasts(bool body)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            var target = StarterSpell3DCaptureFixture.Actor(zone, "target", 6, 9, body ? "0,1" : null);
            var context = StarterSpell3DCaptureFixture.Context(zone, caster); context.TargetCell = zone.GetCell(6, 10);
            Assert.IsTrue(new Pyromancy_FlamingHands().OnCommand(context));
            var sequence = StarterSpell3DCaptureFixture.Single();
            Assert.Contains(new Point(6, 10), sequence.AffectedCells.ToArray());
            Assert.AreEqual(body ? 1 : 0, sequence.Targets.Count);
            if (body)
            {
                Assert.AreEqual(new Point(6, 10), sequence.Targets[0].Cell);
                Assert.IsFalse(sequence.Targets[0].Moved);
                Assert.AreEqual(1, sequence.Targets[0].Damage);
            }
            else Assert.AreEqual(100, target.GetStatValue("Hitpoints"));
        }

        [TestCase(true)] [TestCase(false)]
        public void FlamingHandsImmediateOilReactionKeepsItsPhysicalContact(bool oil)
        {
            AssertFlamingHandsReactionContact(oil, false);
        }

        [TestCase(true)] [TestCase(false)]
        public void FlamingHandsLethalOilReactionKeepsContactAfterItsOwnerIsRemoved(bool oil)
        {
            AssertFlamingHandsReactionContact(oil, true);
        }

        private static void AssertFlamingHandsReactionContact(bool oil, bool lethal)
        {
            // Hypothesis: ground reactions resolve before direct spell damage, so their
            // first damage hook can otherwise lock an off-anchor owner to an empty cell.
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            var target = StarterSpell3DCaptureFixture.Actor(zone, "target", 6, 9, "0,1", lethal ? 4 : 20);
            if (oil) zone.TileState.WriteCoating(6, 10, "oil", 6);
            var context = StarterSpell3DCaptureFixture.Context(zone, caster); context.TargetCell = zone.GetCell(6, 10);
            Assert.IsTrue(new Pyromancy_FlamingHands().OnCommand(context));
            var sequence = StarterSpell3DCaptureFixture.Single(); var result = sequence.Targets.Single();
            Assert.AreEqual(oil ? (lethal ? 4 : 9) : 1, result.Damage, "The shipped oil reaction resolves before the 1d4 direct hit.");
            Assert.AreEqual(oil, sequence.Reactions.Any(r => r.Kind == "reaction" && r.Value == "ignite_oil_heat"));
            Assert.AreEqual(oil && lethal, result.Died);
            Assert.AreEqual(new Point(6, 10), result.Cell, "Reaction-first damage must retain the actual reacting body cell.");
            Assert.AreEqual(oil && lethal ? new Point(-1, -1) : new Point(6, 10), result.FinalCell);
            Assert.IsFalse(result.Moved);
        }

        [TestCase(true)] [TestCase(false)]
        public void RainReportsOnlyRealCropContact_WithoutPromotingFallbackSourceToACrop(bool body)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 10, 10);
            var crop = StarterSpell3DCaptureFixture.Prop(zone, "crop", 6, 10, body ? "1,0;2,0;3,0" : null);
            crop.AddPart(new CropPart());
            var bystander = StarterSpell3DCaptureFixture.Actor(zone, "bystander", 11, 10);
            Assert.IsTrue(new Hydromancy_ConjureRain().OnCommand(StarterSpell3DCaptureFixture.Context(zone, caster)));
            var sequence = StarterSpell3DCaptureFixture.Single();
            Assert.AreEqual(body ? 40 : 0, crop.GetPart<CropPart>().MoistureTicks);
            Assert.AreEqual(body ? 1 : 0, sequence.Targets.Count);
            Assert.IsFalse(bystander.HasEffect<WetEffect>()); Assert.IsEmpty(sequence.Reactions);
            if (body)
            {
                Assert.AreEqual(new Point(7, 10), sequence.Targets[0].Cell);
                CollectionAssert.AreEqual(new[] { new Point(7, 10) }, sequence.AffectedCells);
            }
            else CollectionAssert.AreEqual(new[] { new Point(10, 10) }, sequence.AffectedCells,
                "Empty success keeps the existing source fallback; the renderer must use Targets for crop rain.");
        }

        [TestCase(false)] [TestCase(true)]
        public void EmptyGroundSurgeConsumesItsGroundWorkAndCapturesImmediateWaterReaction(bool water)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            if (water) zone.TileState.WriteCoating(6, 10, "water", 6);
            var skill = new Galvanism_GroundSurge();
            Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(skill));
            Assert.IsTrue(StarterSpell3DCaptureFixture.Route(zone, caster, skill, out bool blocks));
            Assert.IsTrue(blocks);
            var sequence = StarterSpell3DCaptureFixture.Single();
            Assert.IsEmpty(sequence.Targets); Assert.AreEqual(4, sequence.Path.Count);
            Assert.IsTrue(sequence.BlocksTurnAdvance);
            Assert.AreEqual(30, caster.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID).CooldownRemaining);
            Assert.IsTrue(sequence.Reactions.Any(r => r.Kind == "energy" && r.Value == "charge"));
            Assert.AreEqual(water, sequence.Reactions.Any(r => r.Kind == "reaction" && r.Value == "electrify_water"));
            Assert.IsFalse(StarterSpell3DCaptureFixture.Route(zone, caster, skill, out _), "The next cast is genuinely gated by its native cooldown.");
            Assert.IsEmpty(SpellFxBus.Drain());
        }

        [TestCase("wall")] [TestCase("edge")] [TestCase("direction")]
        public void GroundSurgeWithoutReachableWorkRemainsAFreeRefusal(string mode)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", mode == "edge" ? 79 : 5, 10);
            if (mode == "wall") StarterSpell3DCaptureFixture.Wall(zone, 6, 10);
            var skill = new Galvanism_GroundSurge(); Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(skill));
            Assert.IsFalse(StarterSpell3DCaptureFixture.Route(zone, caster, skill, out bool blocks, mode == "direction" ? 0 : 1));
            Assert.IsFalse(blocks); Assert.IsEmpty(SpellFxBus.Drain());
            Assert.AreEqual(0, caster.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID).CooldownRemaining);
            Assert.AreEqual(0, zone.TileState.Charge(6, 10));
        }

        [TestCase(false, false)] [TestCase(true, false)] [TestCase(true, true)]
        public void EmberCapturesDamageSeparatelyFromWetIgnitionSuppressionAndResistance(bool wet, bool immune)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            var target = StarterSpell3DCaptureFixture.Actor(zone, "target", 6, 10);
            if (wet) target.ApplyEffect(new WetEffect(.8f), caster, zone);
            if (immune) StarterSpell3DCaptureFixture.Stat(target, "HeatResistance", 100);
            Assert.IsTrue(new Pyromancy_EmberSpit().OnCommand(StarterSpell3DCaptureFixture.Context(zone, caster)));
            var result = StarterSpell3DCaptureFixture.Single().Targets.Single();
            Assert.AreEqual(immune ? 0 : 3, result.Damage); Assert.AreEqual(immune, result.Resisted);
            Assert.AreEqual(!wet, result.AppliedEffects.Contains(nameof(BurningEffect)));
            Assert.AreEqual(!wet, target.HasEffect<BurningEffect>());
        }

        [TestCase(0, true)] [TestCase(99, false)]
        public void GroundSurgeTargetChargeIsTheActualRoll_NotInferredFromGroundCharge(int roll, bool applied)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            var target = StarterSpell3DCaptureFixture.Actor(zone, "target", 6, 10);
            var context = StarterSpell3DCaptureFixture.Context(zone, caster); context.Rng = new StarterSpell3DCaptureFixture.FixedRng(roll);
            Assert.IsTrue(new Galvanism_GroundSurge().OnCommand(context));
            var sequence = StarterSpell3DCaptureFixture.Single(); var result = sequence.Targets.Single();
            Assert.AreEqual(6, result.Damage);
            Assert.AreEqual(applied, result.AppliedEffects.Contains(nameof(ElectrifiedEffect)));
            Assert.AreEqual(applied, target.HasEffect<ElectrifiedEffect>());
            Assert.IsTrue(sequence.Reactions.Any(r => r.Kind == "energy" && r.Value == "charge"));
        }

        [TestCase(true)] [TestCase(false)]
        public void RimeWaterOnlyBranchRecordsActualIce_WhileDryRefusalLeavesNoSequence(bool wet)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            if (wet) zone.TileState.WriteCoating(7, 10, "water", 6);
            Assert.AreEqual(wet, new Cryomancy_RimeGrip().OnCommand(StarterSpell3DCaptureFixture.Context(zone, caster)));
            var sequences = SpellFxBus.Drain(); Assert.AreEqual(wet ? 1 : 0, sequences.Count);
            Assert.AreEqual(wet, zone.TileState.HasCoating(7, 10, "ice"));
            if (wet)
            {
                Assert.IsEmpty(sequences[0].Targets);
                Assert.IsTrue(sequences[0].Reactions.Any(r => r.Kind == "reaction" && r.Value == "freeze_water" && r.Cell.Equals(new Point(7, 10))));
            }
        }

        [TestCase("fresh")] [TestCase("peaceful")] [TestCase("brainless")] [TestCase("miss")]
        public void CalmReportsOnlyNewPacificationAndNeverDamage(string mode)
        {
            var zone = new Zone(); var caster = StarterSpell3DCaptureFixture.Actor(zone, "caster", 5, 10);
            Entity target = mode == "miss" ? null : StarterSpell3DCaptureFixture.Actor(zone, "target", 6, 10);
            if (target != null && mode != "brainless") target.AddPart(new BrainPart());
            if (mode == "peaceful") target.GetPart<BrainPart>().PushGoal(new NoFightGoal(50, false));
            Assert.IsTrue(new Spellcraft_Calm().OnCommand(StarterSpell3DCaptureFixture.Context(zone, caster)));
            var sequence = StarterSpell3DCaptureFixture.Single();
            Assert.AreEqual(target == null ? 0 : 1, sequence.Targets.Count);
            if (target == null) return;
            var result = sequence.Targets.Single();
            Assert.AreEqual(0, result.Damage); Assert.AreEqual(100, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(mode == "fresh", result.AppliedEffects.Contains("Pacified"));
            Assert.AreEqual(mode != "fresh", result.RejectedEffects.Contains("Pacified"));
        }
    }

    internal static class StarterSpell3DCaptureFixture
    {
        public static void Setup()
        {
            SpellFxBus.Clear(); AsciiFxBus.Clear(); MessageLog.Clear(); Diag.ResetAll();
            TileReactionSystem.ResetForTests(); LiquidRegistry.ResetForTests();
            var root = Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Data");
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(root, "LiquidDefinitions"), "*.json").OrderBy(p => p).Select(File.ReadAllText));
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(root, "TileReactions/Reactions.json")));
        }
        public static void Cleanup()
        { SpellFxBus.Clear(); AsciiFxBus.Clear(); TileReactionSystem.ResetForTests(); LiquidRegistry.ResetForTests(); }
        public static Entity Actor(Zone zone, string id, int x, int y, string body = null, int hp = 100)
        {
            var e = new Entity { ID = id, BlueprintName = "CaptureActor" };
            e.SetTag("Creature"); e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = false }); e.AddPart(new StatusEffectsPart());
            e.AddPart(new SkillsPart()); e.AddPart(new ActivatedAbilitiesPart());
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            if (body != null) e.AddPart(new SpatialFootprintPart { CellsRaw = body });
            Assert.IsTrue(zone.AddEntity(e, x, y), "Fixture actor must actually enter its declared footprint.");
            return e;
        }
        public static Entity Prop(Zone zone, string id, int x, int y, string body = null)
        {
            var e = new Entity { ID = id, BlueprintName = "CaptureProp" };
            e.AddPart(new RenderPart { DisplayName = id }); e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new DestructiblePart { HP = 100, MaxHP = 100 });
            if (body != null) e.AddPart(new SpatialFootprintPart { CellsRaw = body });
            Assert.IsTrue(zone.AddEntity(e, x, y)); return e;
        }
        public static Entity Wall(Zone zone, int x, int y)
        {
            var wall = new Entity { ID = "wall", BlueprintName = "Wall" };
            wall.SetTag("Solid"); wall.AddPart(new PhysicsPart { Solid = true });
            Assert.IsTrue(zone.AddEntity(wall, x, y)); return wall;
        }
        public static void Stat(Entity e, string name, int value)
            => e.Statistics[name] = new Stat { Owner = e, Name = name, BaseValue = value, Min = -100, Max = 100 };
        public static BaseSkillPart Skill(Type type) => (BaseSkillPart)Activator.CreateInstance(type);
        public static SkillEventContext Context(Zone zone, Entity caster)
            => new SkillEventContext { Attacker = caster, Zone = zone, SourceCell = zone.GetEntityCell(caster), DirectionX = 1, Rng = new FixedRng(0) };
        public static SpellFxSequence Single()
        { var items = SpellFxBus.Drain(); Assert.AreEqual(1, items.Count); return items[0]; }
        public static bool Route(Zone zone, Entity caster, BaseSkillPart skill, out bool blocks, int dx = 1)
        {
            var spec = skill.DeclareActivatedAbility(caster);
            return caster.GetPart<SkillsPart>().TryRouteSkillCommand(spec.Command, zone, new FixedRng(99),
                dx, 0, zone.GetEntityCell(caster), null, spec.Range, out blocks);
        }
        public sealed class FixedRng : Random
        {
            private readonly int _value;
            public FixedRng(int value) { _value = value; }
            public override int Next(int maxValue) => _value % maxValue;
            public override int Next(int minValue, int maxValue) => minValue + _value % (maxValue - minValue);
        }
    }
}
