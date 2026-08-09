using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
// System.Random and UnityEngine.Random collide once both
// namespaces are imported; skills take the System one.
using Random = System.Random;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM6 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.4) — the Rime
    /// Grip family, closing the skill-primer half of the plan.
    ///
    /// <list type="bullet">
    /// <item><b>Rime Grip</b> — single target, Frozen. Wet targets freeze
    /// harder, the cold branch of the same rule that makes wet targets
    /// conduct.</item>
    /// <item><b>Glacial Wall</b> — a line of temporary ice. Zoning, not
    /// damage: the only power in any tree that changes the map.</item>
    /// <item><b>Cold Snap</b> — nova 2, Hobbled on everything, no damage.
    /// Buys the turns a slow setup needs.</item>
    /// </list>
    /// </summary>
    public class CryomancyRimeGripTests
    {
        private const float ConductionThreshold = 0.2f;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
            Cryomancy_GlacialWall.Factory = null;
        }

        [TearDown]
        public void TearDown() => Cryomancy_GlacialWall.Factory = null;

        private static Entity MakeBodied(string name = "c", int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["ColdResistance"] = new Stat { Owner = e, Name = "ColdResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        private static Entity MakeWall(Zone zone, int x, int y)
        {
            var w = new Entity { ID = $"wall{x}_{y}", BlueprintName = "Wall" };
            w.Tags["Solid"] = "";
            w.AddPart(new RenderPart { DisplayName = "wall" });
            zone.AddEntity(w, x, y);
            return w;
        }

        private static SkillEventContext Ctx(Entity actor, Zone zone, int dx, int dy)
            => new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone,
                Rng = new Random(0), DirectionX = dx, DirectionY = dy,
            };

        private static T Fix<T>(Entity actor) where T : BaseSkillPart, new()
        {
            var skill = new T();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            return skill;
        }

        private static float ColdOn(Entity e)
        {
            var f = e.GetPart<StatusEffectsPart>().GetEffect<FrozenEffect>();
            return f == null ? 0f : f.Cold;
        }

        private static string Reasons()
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 20 }).Records;
            var sb = new System.Text.StringBuilder();
            foreach (var r in recs) sb.Append(r.PayloadJson).Append('|');
            return sb.ToString();
        }

        // ════════════════════════════════════════════════════════
        // FrozenEffect symmetry — wet freezes harder
        // ════════════════════════════════════════════════════════

        [Test]
        public void FrozenEffect_OnAWetTarget_BitesDeeper()
        {
            // SYMMETRY (cold-eye Q1). ElectrifiedEffect.OnApply doubles
            // its Charge on a target with Moisture > 0.2, and
            // CryomancySkill's own bonus already keys on Wet. Frozen NOT
            // amplifying was the odd one out: soaking made a target
            // better to shock but no better to freeze, for no reason a
            // player could infer. Water freezes.
            var dry = MakeBodied("dry");
            var wet = MakeBodied("wet");
            var zone = new Zone();
            zone.AddEntity(dry, 5, 5);
            zone.AddEntity(wet, 6, 5);
            wet.ApplyEffect(new WetEffect(1.0f), null, zone);

            dry.ApplyEffect(new FrozenEffect(0.5f), null, zone);
            wet.ApplyEffect(new FrozenEffect(0.5f), null, zone);

            Assert.Greater(ColdOn(wet), ColdOn(dry),
                "a soaked target freezes deeper and thaws slower");
        }

        [Test]
        public void FrozenEffect_OnABarelyDampTarget_IsNotAmplified()
        {
            // Counter-check: the threshold must be real, mirroring
            // ElectrifiedEffect's 0.2. A trace of damp is not a bath.
            var damp = MakeBodied("damp");
            var dry = MakeBodied("dry");
            var zone = new Zone();
            zone.AddEntity(damp, 5, 5);
            zone.AddEntity(dry, 6, 5);
            damp.ApplyEffect(new WetEffect(0.1f), null, zone);

            damp.ApplyEffect(new FrozenEffect(0.5f), null, zone);
            dry.ApplyEffect(new FrozenEffect(0.5f), null, zone);

            Assert.AreEqual(ColdOn(dry), ColdOn(damp), 0.001f,
                "below the conduction threshold, water does not deepen the freeze");
        }

        [Test]
        public void FrozenEffect_AmplificationStillClampsAtOne()
        {
            var wet = MakeBodied("wet");
            var zone = new Zone();
            zone.AddEntity(wet, 5, 5);
            wet.ApplyEffect(new WetEffect(1.0f), null, zone);

            wet.ApplyEffect(new FrozenEffect(1.0f), null, zone);

            Assert.LessOrEqual(ColdOn(wet), 1.0f,
                "Cold is a 0..1 field — amplification must not break the invariant");
        }

        // ════════════════════════════════════════════════════════
        // Rime Grip
        // ════════════════════════════════════════════════════════

        [Test]
        public void RimeGrip_Spec_IsALongSingleTargetLine()
        {
            var spec = new Cryomancy_RimeGrip().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandRimeGrip", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Cryomancy_RimeGrip.GRIP_RANGE, spec.Range);
        }

        [Test]
        public void RimeGrip_FreezesAndDamagesTheFirstTarget()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_RimeGrip>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            int hp = target.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Less(target.GetStatValue("Hitpoints"), hp);
            Assert.Greater(ColdOn(target), 0f);
        }

        [Test]
        public void RimeGrip_DoesNotPierce()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_RimeGrip>(atk);
            var zone = new Zone();
            var first = MakeBodied("first");
            var second = MakeBodied("second");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(first, 6, 5);
            zone.AddEntity(second, 7, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Greater(ColdOn(first), 0f);
            Assert.AreEqual(0f, ColdOn(second), "the grip takes hold of one target");
        }

        [Test]
        public void RimeGrip_OnASoakedTarget_FreezesHarder_EndToEnd()
        {
            // The wet→cold branch through an actual power, parallel to
            // the wet→shock test in SM5.
            var atk = MakeBodied("atk");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            var dry = MakeBodied("dry");
            var soaked = MakeBodied("soaked");
            zone.AddEntity(dry, 6, 5);
            zone.AddEntity(soaked, 5, 6);
            soaked.ApplyEffect(new WetEffect(0.8f), atk, zone);
            Assert.Greater(0.8f, ConductionThreshold, "precondition: past the threshold");

            var skill = Fix<Cryomancy_RimeGrip>(atk);
            skill.OnCommand(Ctx(atk, zone, 1, 0));
            skill.OnCommand(Ctx(atk, zone, 0, 1));

            Assert.Greater(ColdOn(soaked), ColdOn(dry),
                "soak, then freeze — the cold half of the grammar");
        }

        [Test]
        public void RimeGrip_DoesNotConsumeTheWater()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_RimeGrip>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);
            target.ApplyEffect(new WetEffect(0.8f), atk, zone);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "skills prime; only rites spend");
        }

        [Test]
        public void RimeGrip_IsColdTyped()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_RimeGrip>(atk);
            var zone = new Zone();
            var immune = MakeBodied("immune");
            immune.Statistics["ColdResistance"].BaseValue = 100;
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(immune, 6, 5);

            int hp = immune.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(hp, immune.GetStatValue("Hitpoints"),
                "full ColdResistance means the grip is typed Cold");
        }

        [Test]
        public void RimeGrip_EmptyLineAndNoDirection_EmitDiags()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_RimeGrip>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            Diag.ResetAll();
            skill.OnCommand(Ctx(atk, zone, 1, 0));
            StringAssert.Contains("no_target", Reasons());

            Diag.ResetAll();
            skill.OnCommand(Ctx(atk, zone, 0, 0));
            StringAssert.Contains("no_direction", Reasons());
        }

        // ════════════════════════════════════════════════════════
        // Glacial Wall — the only power that edits the map
        // ════════════════════════════════════════════════════════

        private static EntityFactory RealFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            return f;
        }

        [Test]
        public void GlacialWall_RaisesSolidIceAcrossTheLine()
        {
            Cryomancy_GlacialWall.Factory = RealFactory();
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_GlacialWall>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            for (int i = 1; i <= Cryomancy_GlacialWall.WALL_LENGTH; i++)
            {
                var cell = zone.GetCell(5 + i, 5);
                Assert.IsTrue(cell.IsSolid(),
                    $"cell {5 + i},5 should be blocked by ice");
            }
        }

        [Test]
        public void GlacialWall_IceMelts()
        {
            // A permanent wall would let a player brick a corridor
            // forever. The whole power is a few turns of tempo.
            Cryomancy_GlacialWall.Factory = RealFactory();
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_GlacialWall>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));
            var ice = zone.GetCell(6, 5).Objects.Find(o => o.BlueprintName == "IceWall");
            Assert.IsNotNull(ice, "precondition: ice was raised");

            var lifespan = ice.GetPart<LifespanPart>();
            Assert.IsNotNull(lifespan, "the ice must be temporary");
            Assert.Greater(lifespan.TurnsRemaining, 0);
        }

        [Test]
        public void GlacialWall_DoesNotBuryCreatures()
        {
            // Raising ice on top of a creature would either delete it
            // from play or stack a solid on an occupied cell. Neither is
            // acceptable; the wall forms around what is standing there.
            Cryomancy_GlacialWall.Factory = RealFactory();
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_GlacialWall>(atk);
            var zone = new Zone();
            var bystander = MakeBodied("bystander");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(bystander, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsFalse(zone.GetCell(6, 5).IsSolid(),
                "an occupied cell is skipped, not sealed over");
            Assert.AreEqual(6, zone.GetEntityPosition(bystander).x,
                "and the creature is still standing where it was");
        }

        [Test]
        public void GlacialWall_DoesNotStackOnExistingWalls()
        {
            Cryomancy_GlacialWall.Factory = RealFactory();
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_GlacialWall>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            MakeWall(zone, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            int iceHere = zone.GetCell(6, 5).Objects.FindAll(
                o => o.BlueprintName == "IceWall").Count;
            Assert.AreEqual(0, iceHere, "no ice on a cell that is already stone");
        }

        [Test]
        public void GlacialWall_NullFactoryIsAGracefulNoOp()
        {
            // Headless contexts have no factory. The power must decline
            // loudly in the diag stream rather than throw.
            Cryomancy_GlacialWall.Factory = null;
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_GlacialWall>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();

            Assert.DoesNotThrow(() => skill.OnCommand(Ctx(atk, zone, 1, 0)));
            StringAssert.Contains("no_factory", Reasons());
        }

        [Test]
        public void GlacialWall_DealsNoDamage()
        {
            Cryomancy_GlacialWall.Factory = RealFactory();
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_GlacialWall>(atk);
            var zone = new Zone();
            var bystander = MakeBodied("bystander");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(bystander, 6, 5);

            int hp = bystander.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(hp, bystander.GetStatValue("Hitpoints"),
                "it is zoning, not an attack");
        }

        // ════════════════════════════════════════════════════════
        // Cold Snap
        // ════════════════════════════════════════════════════════

        [Test]
        public void ColdSnap_Spec_IsSelfCentered()
        {
            var spec = new Cryomancy_ColdSnap().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandColdSnap", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode);
        }

        [Test]
        public void ColdSnap_HobblesEveryoneInRadius()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_ColdSnap>(atk);
            var zone = new Zone();
            var near = MakeBodied("near");
            var edge = MakeBodied("edge");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(near, 11, 10);
            zone.AddEntity(edge, 10 + Cryomancy_ColdSnap.SNAP_RADIUS, 10);

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.IsTrue(near.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>());
            Assert.IsTrue(edge.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>(),
                "the far edge of the radius is still inside it");
        }

        [Test]
        public void ColdSnap_SparesTheCaster()
        {
            // Counter-check: a self-centred slow that also slowed YOU
            // would be strictly bad to cast.
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_ColdSnap>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(MakeBodied("other"), 11, 10);

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.IsFalse(atk.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>(),
                "you do not snap yourself");
        }

        [Test]
        public void ColdSnap_SparesTargetsOutsideTheRadius()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_ColdSnap>(atk);
            var zone = new Zone();
            var outside = MakeBodied("outside");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(outside, 10 + Cryomancy_ColdSnap.SNAP_RADIUS + 1, 10);

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.IsFalse(outside.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>());
        }

        [Test]
        public void ColdSnap_DealsNoDamage()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_ColdSnap>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(target, 11, 10);

            int hp = target.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.AreEqual(hp, target.GetStatValue("Hitpoints"),
                "it buys turns, it does not kill");
        }

        [Test]
        public void ColdSnap_NeedsNoDirection()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_ColdSnap>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(MakeBodied("t"), 11, 10);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            StringAssert.DoesNotContain("no_direction", Reasons());
        }

        [Test]
        public void ColdSnap_NobodyAround_EmitsNoTargetDiag()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Cryomancy_ColdSnap>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            StringAssert.Contains("no_target", Reasons());
        }

        // ════════════════════════════════════════════════════════
        // Family invariants
        // ════════════════════════════════════════════════════════

        [Test]
        public void AllThree_HaveDistinctCommands()
        {
            var a = new Cryomancy_RimeGrip().DeclareActivatedAbility(null).Command;
            var b = new Cryomancy_GlacialWall().DeclareActivatedAbility(null).Command;
            var c = new Cryomancy_ColdSnap().DeclareActivatedAbility(null).Command;
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(a, c);
        }

        [Test]
        public void AllThree_AreRegisteredAndFitTheSkillsScreen()
        {
            var json = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Skills/Cryomancy.json"));
            SkillRegistry.LoadFromJson(json, "test:Cryomancy");

            foreach (var cls in new[] { "Cryomancy_RimeGrip",
                                        "Cryomancy_GlacialWall",
                                        "Cryomancy_ColdSnap" })
            {
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(cls, out var power), cls);
                Assert.Greater(power.Cost, 0);
                Assert.LessOrEqual(power.Description.Length, 55,
                    cls + " is truncated by SkillsScreenUI, hiding the mechanic");
            }
        }

        [Test]
        public void IceWall_BlueprintExists_AndIsSolidAndTemporary()
        {
            // Content reachability: a power that spawns a blueprint which
            // does not exist fails silently at runtime.
            var factory = RealFactory();
            var ice = factory.CreateEntity("IceWall");

            Assert.IsNotNull(ice, "IceWall blueprint must exist");
            Assert.IsTrue(ice.HasTag("Solid"), "it has to actually block movement");
            Assert.IsNotNull(ice.GetPart<LifespanPart>(), "and it has to melt");
        }
    }
}
