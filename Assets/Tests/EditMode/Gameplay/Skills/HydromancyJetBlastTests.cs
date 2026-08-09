using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM5 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.3) —
    /// Hydromancy, the fifth elemental tree, and the milestone where the
    /// soak-then-shock loop becomes self-serve.
    ///
    /// <list type="bullet">
    /// <item><b>Jet Blast</b> — cone 2, soaks and shoves. The request's
    /// jet blast.</item>
    /// <item><b>Drench Lob</b> — flies to range 6, bursts radius 2. Primes
    /// a clump BEFORE it closes.</item>
    /// <item><b>Undertow</b> — line 3, soaks and DRAGS toward the caster.
    /// The only pull in the game.</item>
    /// </list>
    ///
    /// <para>The tree's job is priming, so these tests care much more
    /// about the moisture that lands (and the thresholds it clears) than
    /// about damage.</para>
    /// </summary>
    public class HydromancyJetBlastTests
    {
        /// <summary>Moisture above which ElectrifiedEffect doubles its
        /// charge (ElectrifiedEffect.OnApply).</summary>
        private const float ConductionThreshold = 0.2f;
        /// <summary>Moisture above which ignition is suppressed
        /// (ThermalPart.cs:117, mirrored by PyroIgnition).</summary>
        private const float FireSuppressionThreshold = 0.35f;

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
            e.Statistics["ElectricResistance"] = new Stat { Owner = e, Name = "ElectricResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["ColdResistance"] = new Stat { Owner = e, Name = "ColdResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["AcidResistance"] = new Stat { Owner = e, Name = "AcidResistance", BaseValue = 0, Min = -100, Max = 100 };
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

        private static float Moisture(Entity e)
        {
            var wet = e.GetPart<StatusEffectsPart>().GetEffect<WetEffect>();
            return wet == null ? 0f : wet.Moisture;
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
        // Jet Blast — the request's jet blast
        // ════════════════════════════════════════════════════════

        [Test]
        public void JetBlast_Spec_IsAShortCone()
        {
            var spec = new Hydromancy_JetBlast().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandJetBlast", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Hydromancy_JetBlast.BLAST_LENGTH, spec.Range);
        }

        [Test]
        public void JetBlast_SoaksEveryTargetInTheCone()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            var centre = MakeBodied("centre");
            var offAxis = MakeBodied("offAxis");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(centre, 12, 10);
            zone.AddEntity(offAxis, 12, 11);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Greater(Moisture(centre), 0f);
            Assert.Greater(Moisture(offAxis), 0f, "the cone catches a clump, not a file");
        }

        [Test]
        public void JetBlast_SoaksPastBothInteractionThresholds()
        {
            // THE POINT OF THE WHOLE TREE. A soaking that does not clear
            // 0.2 buys no lightning amplification, and one that does not
            // clear 0.35 buys no fire protection. If a future tuning pass
            // drops BLAST_MOISTURE below either, the power silently stops
            // doing its job and this test says so.
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(target, 11, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Greater(Moisture(target), ConductionThreshold,
                "below this, lightning does not double its charge");
            Assert.Greater(Moisture(target), FireSuppressionThreshold,
                "below this, the target still catches fire");
        }

        [Test]
        public void JetBlast_SoakingThenElectrifying_DoublesTheCharge_EndToEnd()
        {
            // The loop the whole feature exists for, exercised across
            // three systems: a Hydromancy skill soaks, a Galvanism skill
            // shocks, and ElectrifiedEffect's amplification pays out.
            var atk = MakeBodied("atk");
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);

            var dry = MakeBodied("dry");
            var soaked = MakeBodied("soaked");
            zone.AddEntity(dry, 11, 10);
            zone.AddEntity(soaked, 10, 11);

            // Soak only the second one.
            Fix<Hydromancy_JetBlast>(atk).OnCommand(Ctx(atk, zone, 0, 1));
            Assert.Greater(Moisture(soaked), ConductionThreshold, "precondition");

            // Jet Blast shoves; put it back in range for the shock.
            zone.MoveEntity(soaked, 10, 11);

            var surge = Fix<Galvanism_GroundSurge>(atk);
            surge.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone,
                Rng = new AlwaysZeroRng(), DirectionX = 1, DirectionY = 0,
            });
            surge.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone,
                Rng = new AlwaysZeroRng(), DirectionX = 0, DirectionY = 1,
            });

            var dryCharge = dry.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            var wetCharge = soaked.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(dryCharge);
            Assert.IsNotNull(wetCharge);
            Assert.Greater(wetCharge.Charge, dryCharge.Charge,
                "soak, then shock — the payoff the player is meant to discover");
        }

        [Test]
        public void JetBlast_SoakingProtectsAgainstFire_TheAntiSynergy()
        {
            // The other half of the grammar: water is fire's counter.
            var atk = MakeBodied("atk");
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            var target = MakeBodied("target");
            zone.AddEntity(target, 11, 10);

            Fix<Hydromancy_JetBlast>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            zone.MoveEntity(target, 11, 10);   // undo the shove
            Fix<Pyromancy_FlameJet>(atk).OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsFalse(target.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "a soaked target does not catch — soaking is a real defence");
        }

        [Test]
        public void JetBlast_ShovesTargetsAway()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(target, 11, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(12, zone.GetEntityPosition(target).x);
        }

        [Test]
        public void JetBlast_DoesNotConsumeExistingStatuses()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(target, 11, 10);
            target.ApplyEffect(new ElectrifiedEffect(1.0f), atk, zone);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "skills prime; only rites spend");
        }

        [Test]
        public void WaterDamage_IsUntyped_AndNoElementalResistanceStopsIt()
        {
            // "Water" maps to DamageAttributeFlags.None (Damage.cs:144-173),
            // so it is a descriptive tag carrying no flag. Pinned so that
            // nobody later assumes water is a resisted element — and so
            // that if "Water" ever BECOMES a flag, this test forces the
            // decision to be deliberate.
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            var warded = MakeBodied("warded");
            foreach (var stat in new[] { "ElectricResistance", "HeatResistance",
                                         "ColdResistance", "AcidResistance" })
                warded.Statistics[stat].BaseValue = 100;
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(warded, 11, 10);

            int hp = warded.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Less(warded.GetStatValue("Hitpoints"), hp,
                "no elemental ward stops water — it is untyped by design");
        }

        [Test]
        public void JetBlast_NoDirectionAndEmptyCone_EmitDiags()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);

            Diag.ResetAll();
            skill.OnCommand(Ctx(atk, zone, 1, 0));
            StringAssert.Contains("no_target", Reasons());

            Diag.ResetAll();
            skill.OnCommand(Ctx(atk, zone, 0, 0));
            StringAssert.Contains("no_direction", Reasons());
        }

        [Test]
        public void JetBlast_NullArguments_DoNotCrash()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_JetBlast>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            Assert.DoesNotThrow(() => skill.OnCommand(null));
            Assert.DoesNotThrow(() => skill.OnCommand(new SkillEventContext
            { Attacker = atk, Defender = atk, Zone = null, Rng = new Random(0), DirectionX = 1 }));
        }

        // ════════════════════════════════════════════════════════
        // The tree passive
        // ════════════════════════════════════════════════════════

        [Test]
        public void HydromancyRoot_DeepensTheSoaking()
        {
            var withTree = MakeBodied("withTree");
            withTree.GetPart<SkillsPart>().AddSkill(new HydromancySkill(), source: "test");
            var without = MakeBodied("without");

            Assert.AreEqual(0.5f + HydromancySkill.MOISTURE_BONUS,
                HydromancySkill.ApplyMoistureBonus(withTree, 0.5f), 0.001f);
            Assert.AreEqual(0.5f,
                HydromancySkill.ApplyMoistureBonus(without, 0.5f), 0.001f,
                "counter-check: no tree, no bonus");
        }

        [Test]
        public void HydromancyRoot_ClampsAtFullSaturation()
        {
            var withTree = MakeBodied("withTree");
            withTree.GetPart<SkillsPart>().AddSkill(new HydromancySkill(), source: "test");

            Assert.AreEqual(1.0f,
                HydromancySkill.ApplyMoistureBonus(withTree, 0.9f), 0.001f,
                "you cannot be more than soaked");
        }

        [Test]
        public void HydromancyRoot_NullActorIsGraceful()
        {
            Assert.AreEqual(0.5f, HydromancySkill.ApplyMoistureBonus(null, 0.5f), 0.001f);
        }

        [Test]
        public void JetBlast_SoaksDeeperForAHydromancer()
        {
            // The passive reaching the actual power, end to end.
            var plain = MakeBodied("plain");
            var adept = MakeBodied("adept");
            adept.GetPart<SkillsPart>().AddSkill(new HydromancySkill(), source: "test");

            var zoneA = new Zone();
            var targetA = MakeBodied("targetA");
            zoneA.AddEntity(plain, 10, 10); zoneA.AddEntity(targetA, 11, 10);
            Fix<Hydromancy_JetBlast>(plain).OnCommand(Ctx(plain, zoneA, 1, 0));

            var zoneB = new Zone();
            var targetB = MakeBodied("targetB");
            zoneB.AddEntity(adept, 10, 10); zoneB.AddEntity(targetB, 11, 10);
            Fix<Hydromancy_JetBlast>(adept).OnCommand(Ctx(adept, zoneB, 1, 0));

            Assert.Greater(Moisture(targetB), Moisture(targetA),
                "a Hydromancer's water runs deeper");
        }

        // ════════════════════════════════════════════════════════
        // Drench Lob — prime at range
        // ════════════════════════════════════════════════════════

        [Test]
        public void DrenchLob_BurstsOnTheFirstBodyAndSoaksAroundIt()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_DrenchLob>(atk);
            var zone = new Zone();
            var struck = MakeBodied("struck");
            var nearby = MakeBodied("nearby");
            zone.AddEntity(atk, 5, 10);
            zone.AddEntity(struck, 10, 10);
            zone.AddEntity(nearby, 11, 11);   // within radius 2 of impact

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Greater(Moisture(struck), 0f);
            Assert.Greater(Moisture(nearby), 0f, "the burst catches the clump");
        }

        [Test]
        public void DrenchLob_DoesNotSoakTheCaster()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_DrenchLob>(atk);
            var zone = new Zone();
            var close = MakeBodied("close");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(close, 11, 10);   // bursts within radius of the caster

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Greater(Moisture(close), 0f, "precondition: it burst nearby");
            Assert.AreEqual(0f, Moisture(atk), "you do not soak yourself");
        }

        [Test]
        public void DrenchLob_DealsNoDamage()
        {
            // Deliberate: it is pure setup, and that is the trade for
            // its reach.
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_DrenchLob>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 10);
            zone.AddEntity(target, 9, 10);

            int hp = target.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(hp, target.GetStatValue("Hitpoints"));
            Assert.Greater(Moisture(target), 0f, "but it absolutely soaks");
        }

        [Test]
        public void DrenchLob_DoesNotPush()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_DrenchLob>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 10);
            zone.AddEntity(target, 9, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(9, zone.GetEntityPosition(target).x,
                "shoving would scatter the cluster it just soaked");
        }

        [Test]
        public void DrenchLob_OutReachesJetBlast()
        {
            Assert.Greater(Hydromancy_DrenchLob.LOB_RANGE,
                Hydromancy_JetBlast.BLAST_LENGTH,
                "priming before they close is its entire reason to exist");
        }

        [Test]
        public void DrenchLob_BurstsAtMaxRangeWhenItHitsNothing()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_DrenchLob>(atk);
            var zone = new Zone();
            // Nothing in the flight path; someone near the far end.
            var farOut = MakeBodied("farOut");
            zone.AddEntity(atk, 5, 10);
            zone.AddEntity(farOut, 5 + Hydromancy_DrenchLob.LOB_RANGE, 11);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Greater(Moisture(farOut), 0f,
                "an unobstructed lob travels its full range and bursts there");
        }

        [Test]
        public void DrenchLob_WalledInImmediately_EmitsLineBlocked()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_DrenchLob>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            MakeWall(zone, 11, 10);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.Contains("line_blocked", Reasons());
        }

        // ════════════════════════════════════════════════════════
        // Undertow — the only pull in the game
        // ════════════════════════════════════════════════════════

        [Test]
        public void Undertow_DragsTargetsTowardTheCaster()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_Undertow>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 10);
            zone.AddEntity(target, 8, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(7, zone.GetEntityPosition(target).x,
                "hauled one cell closer");
            Assert.Greater(Moisture(target), 0f, "and soaked on the way in");
        }

        [Test]
        public void Undertow_NeverDragsATargetOntoTheCaster()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_Undertow>(atk);
            var zone = new Zone();
            var adjacent = MakeBodied("adjacent");
            zone.AddEntity(atk, 5, 10);
            zone.AddEntity(adjacent, 6, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(6, zone.GetEntityPosition(adjacent).x,
                "already adjacent — there is nowhere closer to go");
            Assert.AreEqual(5, zone.GetEntityPosition(atk).x,
                "and the caster stays put");
        }

        [Test]
        public void Undertow_DragsARankNearestFirst_SoTheWholeLineCloses()
        {
            // The mirror of the shove rule. Pulling the FAR target first
            // would jam it against the body in front; nearest-first
            // vacates each cell just before the one behind needs it.
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_Undertow>(atk);
            var zone = new Zone();
            var near = MakeBodied("near");
            var far = MakeBodied("far");
            zone.AddEntity(atk, 5, 10);
            zone.AddEntity(near, 7, 10);
            zone.AddEntity(far, 8, 10);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(6, zone.GetEntityPosition(near).x);
            Assert.AreEqual(7, zone.GetEntityPosition(far).x,
                "the far one follows into the cell just vacated");
        }

        [Test]
        public void Undertow_IsTheOnlyPowerInTheFamilyThatPulls()
        {
            // Counter-check across the tree: Jet Blast pushes, Undertow
            // pulls. If they ever agreed, one of them would be pointless.
            var atk = MakeBodied("atk");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 10);
            var pushed = MakeBodied("pushed");
            zone.AddEntity(pushed, 6, 10);
            Fix<Hydromancy_JetBlast>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            Assert.AreEqual(7, zone.GetEntityPosition(pushed).x, "Jet Blast shoves away");

            var pulled = MakeBodied("pulled");
            zone.AddEntity(pulled, 8, 10);
            Fix<Hydromancy_Undertow>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            Assert.AreEqual(7, zone.GetEntityPosition(pulled).x, "Undertow hauls in");
        }

        [Test]
        public void Undertow_EmptyLine_EmitsNoTargetDiag()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Hydromancy_Undertow>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 10);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.Contains("no_target", Reasons());
        }

        // ════════════════════════════════════════════════════════
        // Tree-level invariants
        // ════════════════════════════════════════════════════════

        [Test]
        public void AllThree_HaveDistinctCommands()
        {
            var a = new Hydromancy_JetBlast().DeclareActivatedAbility(null).Command;
            var b = new Hydromancy_DrenchLob().DeclareActivatedAbility(null).Command;
            var c = new Hydromancy_Undertow().DeclareActivatedAbility(null).Command;
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(a, c);
        }

        [Test]
        public void TheTreeAndItsPowers_AreRegisteredAndFitTheSkillsScreen()
        {
            var json = System.IO.File.ReadAllText(System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "Resources/Content/Data/Skills/Hydromancy.json"));
            SkillRegistry.LoadFromJson(json, "test:Hydromancy");

            Assert.IsTrue(SkillRegistry.TryGetSkillByClass("HydromancySkill", out var root),
                "the tree root must be purchasable or the whole tree is unreachable");
            Assert.Greater(root.Cost, 0);

            foreach (var cls in new[] { "Hydromancy_JetBlast",
                                        "Hydromancy_DrenchLob",
                                        "Hydromancy_Undertow" })
            {
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(cls, out var power), cls);
                Assert.Greater(power.Cost, 0);
                Assert.LessOrEqual(power.Description.Length, 55,
                    cls + " is truncated by SkillsScreenUI, hiding the mechanic");
            }
        }

        /// <summary>Always rolls 0, so any chance gate succeeds. Used to
        /// force Ground Surge's 40% electrify in the cross-tree test.</summary>
        private class AlwaysZeroRng : Random
        {
            public override int Next() => 0;
            public override int Next(int maxValue) => 0;
            public override int Next(int minValue, int maxValue) => minValue;
        }
    }
}
