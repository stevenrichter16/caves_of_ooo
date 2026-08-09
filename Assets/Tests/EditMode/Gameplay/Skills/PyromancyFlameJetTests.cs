using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM4 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.2) — the
    /// Flame Jet family, and the first consumer of the SM2 cone.
    ///
    /// <list type="bullet">
    /// <item><b>Flame Jet</b> — cone 3, heavy Burning on everything in
    /// it. The request's flamethrower, and the family's mass primer.</item>
    /// <item><b>Ember Spit</b> — single target, short cooldown, light
    /// Burning. The cheap filler you cast between big turns.</item>
    /// <item><b>Backdraft</b> — cone 2, Burning AND a shove. The only
    /// fire power that makes space.</item>
    /// </list>
    ///
    /// <para>All three PRIME and none consume. Note that the tree's
    /// pre-existing <see cref="Pyromancy_Pyroclasm"/> DOES consume
    /// Burning — a skill doing a rite's job, predating this design.
    /// Flagged in the living doc for SM7, deliberately not changed
    /// here.</para>
    /// </summary>
    public class PyromancyFlameJetTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        private static Entity MakeBodied(string name = "c", int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            // HeatResistance is what ApplyResistances reads for Fire/Heat
            // damage (CombatSystem.cs:1162).
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
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

        private static SkillEventContext Ctx(Entity actor, Zone zone, int dx, int dy, int seed = 0)
            => new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone,
                Rng = new Random(seed), DirectionX = dx, DirectionY = dy,
            };

        private static T Fix<T>(Entity actor) where T : BaseSkillPart, new()
        {
            var skill = new T();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            return skill;
        }

        private static bool Burning(Entity e)
            => e.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>();

        private static string Reasons()
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 20 }).Records;
            var sb = new System.Text.StringBuilder();
            foreach (var r in recs) sb.Append(r.PayloadJson).Append('|');
            return sb.ToString();
        }

        private static string LastMessage() => MessageLog.GetLast() ?? "";

        // ════════════════════════════════════════════════════════
        // Flame Jet — the flamethrower
        // ════════════════════════════════════════════════════════

        [Test]
        public void FlameJet_Spec_IsADirectionalCone()
        {
            var spec = new Pyromancy_FlameJet().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandFlameJet", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode,
                "a cone is aimed like a line — the player picks a facing");
            Assert.AreEqual(Pyromancy_FlameJet.JET_LENGTH, spec.Range);
            Assert.Greater(spec.Cooldown, 0);
        }

        [Test]
        public void FlameJet_BurnsEverythingInTheCone_IncludingOffAxisTargets()
        {
            // The whole reason a cone exists: it catches a CLUMP, not a
            // file. If this only hit the centre line it would be a worse
            // Ground Surge.
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            var centre = MakeBodied("centre");
            var offAxis = MakeBodied("offAxis");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(centre, 12, 10);
            zone.AddEntity(offAxis, 12, 11);   // widened band at step 2

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(Burning(centre), "the centre line burns");
            Assert.IsTrue(Burning(offAxis), "and so does the target beside it");
        }

        [Test]
        public void FlameJet_DamagesAndBurns()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(target, 11, 10);

            int hp = target.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Less(target.GetStatValue("Hitpoints"), hp);
            Assert.IsTrue(Burning(target));
        }

        [Test]
        public void FlameJet_SparesAnythingBehindTheCaster()
        {
            // Counter-check: a cone must be directional. If this failed
            // the "cone" would be a nova and Flame Jet would torch the
            // party standing behind you.
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            var behind = MakeBodied("behind");
            var ahead = MakeBodied("ahead");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(behind, 8, 10);
            zone.AddEntity(ahead, 11, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(Burning(ahead), "precondition: the jet fired");
            Assert.IsFalse(Burning(behind), "nothing behind you burns");
            Assert.IsFalse(Burning(atk), "and never the caster");
        }

        [Test]
        public void FlameJet_BurnsHotterThanEmberSpit()
        {
            // The family's damage gradient must be real, not just
            // flavour text: the long-cooldown cone is the heavy primer,
            // the spammable single-target is the light one.
            Assert.Greater(Pyromancy_FlameJet.JET_INTENSITY,
                Pyromancy_EmberSpit.SPIT_INTENSITY,
                "the flamethrower leaves a hotter burn than the filler cantrip");
        }

        [Test]
        public void FlameJet_DoesNotConsumeExistingStatuses()
        {
            // THE GRAMMAR (plan §4). Skills prime; only rites spend.
            // Pyroclasm in this same tree already breaks this rule and
            // is grandfathered — the NEW powers must not add to that.
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(target, 11, 10);
            target.ApplyEffect(new WetEffect(), atk, zone);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "the water is still there — a skill may read it, never spend it");
        }

        [Test]
        public void FlameJet_IsBlockedByAWall()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            MakeWall(zone, 11, 10);
            var shielded = MakeBodied("shielded");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(shielded, 12, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsFalse(Burning(shielded), "flame does not pass through stone");
        }

        [Test]
        public void FlameJet_EmptyCone_EmitsNoTargetDiag()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.Contains("no_target", Reasons());
        }

        [Test]
        public void FlameJet_NoDirection_EmitsDiag_AndBurnsNobody()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            var bystander = MakeBodied("bystander");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(bystander, 11, 10);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            StringAssert.Contains("no_direction", Reasons());
            Assert.IsFalse(Burning(bystander),
                "an unaimed jet is a wasted turn, not a free nova");
        }

        [Test]
        public void FlameJet_NullContextRngAndZone_DoNotCrash()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(MakeBodied("t"), 11, 10);

            Assert.DoesNotThrow(() => skill.OnCommand(null));
            Assert.DoesNotThrow(() => skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = null,
                DirectionX = 1, DirectionY = 0,
            }));
            Assert.DoesNotThrow(() => skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = null, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            }));
        }

        // ════════════════════════════════════════════════════════
        // Ember Spit — the filler
        // ════════════════════════════════════════════════════════

        [Test]
        public void EmberSpit_Spec_IsCheapEnoughToBeFiller()
        {
            var spec = new Pyromancy_EmberSpit().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandEmberSpit", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Pyromancy_EmberSpit.SPIT_RANGE, spec.Range);
            Assert.Less(spec.Cooldown,
                new Pyromancy_FlameJet().DeclareActivatedAbility(null).Cooldown,
                "its entire identity is being available when the jet is not");
        }

        [Test]
        public void EmberSpit_HitsTheFirstTargetOnly()
        {
            // Single-target: it must NOT pierce, or it would be a free
            // Rail Spike and the family's shapes would stop meaning
            // anything.
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_EmberSpit>(atk);
            var zone = new Zone();
            var first = MakeBodied("first");
            var second = MakeBodied("second");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(first, 11, 10);
            zone.AddEntity(second, 12, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(Burning(first));
            Assert.IsFalse(Burning(second),
                "the spit stops at the first body it hits");
        }

        [Test]
        public void EmberSpit_ReachesAcrossItsWholeRange()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_EmberSpit>(atk);
            var zone = new Zone();
            var far = MakeBodied("far");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(far, 10 + Pyromancy_EmberSpit.SPIT_RANGE, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(Burning(far), "the far edge of the range still lands");
        }

        [Test]
        public void EmberSpit_RespectsItsRange()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_EmberSpit>(atk);
            var zone = new Zone();
            var tooFar = MakeBodied("tooFar");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(tooFar, 10 + Pyromancy_EmberSpit.SPIT_RANGE + 1, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsFalse(Burning(tooFar));
            StringAssert.Contains("no_target", Reasons());
        }

        [Test]
        public void EmberSpit_DoesNotPush()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_EmberSpit>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(target, 11, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(11, zone.GetEntityPosition(target).x,
                "only Backdraft makes space in this tree");
        }

        // ════════════════════════════════════════════════════════
        // Backdraft — the one that makes space
        // ════════════════════════════════════════════════════════

        [Test]
        public void Backdraft_Spec_IsAShorterCone()
        {
            var spec = new Pyromancy_Backdraft().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandBackdraft", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Pyromancy_Backdraft.DRAFT_LENGTH, spec.Range);
            Assert.Less(Pyromancy_Backdraft.DRAFT_LENGTH, Pyromancy_FlameJet.JET_LENGTH,
                "it trades reach for the shove");
        }

        [Test]
        public void Backdraft_BurnsAndShovesTheWholeCone()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_Backdraft>(atk);
            var zone = new Zone();
            var near = MakeBodied("near");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(near, 11, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(Burning(near), "it is still a fire power");
            Assert.AreEqual(12, zone.GetEntityPosition(near).x,
                "and it makes space");
        }

        [Test]
        public void Backdraft_ShovesAPackedRank_FurthestFirst()
        {
            // The same ordering trap SM3's audit caught in Ground Surge:
            // shoving nearest-first slams each target into the body
            // behind it and only the last one moves.
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_Backdraft>(atk);
            var zone = new Zone();
            var near = MakeBodied("near");
            var far = MakeBodied("far");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(near, 11, 10);
            zone.AddEntity(far, 12, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(13, zone.GetEntityPosition(far).x);
            Assert.AreEqual(12, zone.GetEntityPosition(near).x,
                "the near target follows into the cell just vacated");
        }

        [Test]
        public void Backdraft_CorneredTarget_StillBurns()
        {
            // Counter-check: a blocked shove must not swallow the burn.
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_Backdraft>(atk);
            var zone = new Zone();
            var pinned = MakeBodied("pinned");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(pinned, 11, 10);
            MakeWall(zone, 12, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(11, zone.GetEntityPosition(pinned).x, "the wall holds them");
            Assert.IsTrue(Burning(pinned), "but they still catch fire");
        }

        // ════════════════════════════════════════════════════════
        // Family-level invariants
        // ════════════════════════════════════════════════════════

        [Test]
        public void AllThree_HaveDistinctCommands()
        {
            var a = new Pyromancy_FlameJet().DeclareActivatedAbility(null).Command;
            var b = new Pyromancy_EmberSpit().DeclareActivatedAbility(null).Command;
            var c = new Pyromancy_Backdraft().DeclareActivatedAbility(null).Command;
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(a, c);
        }

        [Test]
        public void AllThree_DealHeatDamage_SoPyromancysBonusAndResistancesApply()
        {
            var atk = MakeBodied("atk");
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);

            var immune = MakeBodied("immune");
            immune.Statistics["HeatResistance"].BaseValue = 100;
            zone.AddEntity(immune, 11, 10);
            int hp = immune.GetStatValue("Hitpoints");

            Fix<Pyromancy_FlameJet>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            Assert.AreEqual(hp, immune.GetStatValue("Hitpoints"), "Flame Jet is Heat");

            zone.MoveEntity(immune, 11, 10);
            Fix<Pyromancy_EmberSpit>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            Assert.AreEqual(hp, immune.GetStatValue("Hitpoints"), "Ember Spit is Heat");

            zone.MoveEntity(immune, 11, 10);
            Fix<Pyromancy_Backdraft>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            Assert.AreEqual(hp, immune.GetStatValue("Hitpoints"), "Backdraft is Heat");
        }

        [Test]
        public void AllThree_AreRegisteredAndFitTheSkillsScreen()
        {
            var json = System.IO.File.ReadAllText(System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "Resources/Content/Data/Skills/Pyromancy.json"));
            SkillRegistry.LoadFromJson(json, "test:Pyromancy");

            foreach (var cls in new[] { "Pyromancy_FlameJet",
                                        "Pyromancy_EmberSpit",
                                        "Pyromancy_Backdraft" })
            {
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(cls, out var power),
                    cls + " must be purchasable or it does not exist for the player");
                Assert.Greater(power.Cost, 0);
                Assert.LessOrEqual(power.Description.Length, 55,
                    cls + " is truncated by SkillsScreenUI (cut at 55), "
                    + "hiding the mechanic at the moment of purchase");
            }
        }

        [Test]
        public void FlameJet_DoesNotBurnAWetTarget_BecauseMoistureSuppressesIgnition()
        {
            // Cross-system reality check, and a deliberate ANTI-synergy:
            // WetEffect suppresses ignition above 0.35 moisture. Soaking
            // a target sets it up for lightning and protects it from
            // fire — the elemental grammar the player is meant to learn.
            // Pinning it means a future change to either system breaks
            // visibly rather than quietly making water pointless.
            var atk = MakeBodied("atk");
            var skill = Fix<Pyromancy_FlameJet>(atk);
            var zone = new Zone();
            var soaked = MakeBodied("soaked");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(soaked, 11, 10);
            soaked.ApplyEffect(new WetEffect(1.0f), atk, zone);

            int hp = soaked.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Less(soaked.GetStatValue("Hitpoints"), hp,
                "the heat still hurts");
            Assert.IsFalse(Burning(soaked),
                "but a drenched target does not catch — water is fire's counter");
        }
    }
}
