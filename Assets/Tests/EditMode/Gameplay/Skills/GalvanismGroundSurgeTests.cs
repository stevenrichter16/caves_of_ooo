using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM3 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.1) — the
    /// Ground Surge family: three Galvanism actives that all PRIME.
    ///
    /// <para>Per the plan's grammar, a skill spell APPLIES a status and
    /// never consumes one — consuming is what grimoire rites do (SM7).
    /// All three powers here therefore leave the target primed for a
    /// later detonation, and none of them reads an existing status to
    /// cash it in.</para>
    ///
    /// <list type="bullet">
    /// <item><b>Ground Surge</b> — line 4, damage, chance of Electrified,
    /// pushes each target 1 cell away. The request's shockwave.</item>
    /// <item><b>Backlash Coil</b> — self-centred ring, damage and a shove
    /// to every adjacent creature, NO Electrified. A panic button, not a
    /// primer.</item>
    /// <item><b>Rail Spike</b> — line 6, pierces everything, Electrified
    /// on the LAST target only. Reaches past a front rank to prime the
    /// caster behind it.</item>
    /// </list>
    /// </summary>
    public class GalvanismGroundSurgeTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        /// <summary>Scripted RNG — each Next(max) pops the next value
        /// mod max. Same idiom as GasWindCouplingTests.cs:18-26. Lets a
        /// chance-gated branch be pinned exactly instead of hunting for
        /// a lucky seed (or, worse, exposing a test-only hook on the
        /// production class).</summary>
        private class ScriptedRng : Random
        {
            private readonly int[] _seq; private int _i;
            public ScriptedRng(params int[] seq) { _seq = seq; }
            public override int Next() => _seq[_i++ % _seq.Length];
            public override int Next(int maxValue) => _seq[_i++ % _seq.Length] % maxValue;
            public override int Next(int minValue, int maxValue)
                => minValue + (_seq[_i++ % _seq.Length] % (maxValue - minValue));
        }

        private static SkillEventContext CtxRng(Entity actor, Zone zone, int dx, int dy, Random rng)
            => new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone,
                Rng = rng, DirectionX = dx, DirectionY = dy,
            };

        private static Entity MakeBodied(string name = "c", int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            // ElectricResistance is the stat ApplyResistances actually
            // reads for Electric/Lightning damage (CombatSystem.cs:1164).
            // "LightningResistance" — which the older Galvanism test
            // fixture sets — is read by NO production code, so setting
            // it looks like immunity and does nothing.
            e.Statistics["ElectricResistance"] = new Stat { Owner = e, Name = "ElectricResistance", BaseValue = 0, Min = -100, Max = 100 };
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

        private static string[] RejectReasons()
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 20 }).Records;
            var outp = new string[recs.Count];
            for (int i = 0; i < recs.Count; i++) outp[i] = recs[i].PayloadJson;
            return outp;
        }

        // ════════════════════════════════════════════════════════
        // Ground Surge — the shockwave
        // ════════════════════════════════════════════════════════

        [Test]
        public void GroundSurge_Spec_IsADirectionalLine()
        {
            var spec = new Galvanism_GroundSurge().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandGroundSurge", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Galvanism_GroundSurge.SURGE_RANGE, spec.Range);
            Assert.Greater(spec.Cooldown, 0, "an active costs a cooldown");
        }

        [Test]
        public void GroundSurge_DamagesEveryCreatureInTheLine()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var near = MakeBodied("near");
            var far = MakeBodied("far");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(near, 6, 5);
            zone.AddEntity(far, 7, 5);

            int nearHp = near.GetStatValue("Hitpoints");
            int farHp = far.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Less(near.GetStatValue("Hitpoints"), nearHp);
            Assert.Less(far.GetStatValue("Hitpoints"), farHp,
                "the surge does not stop at the first body — it is a wave");
        }

        [Test]
        public void GroundSurge_PushesTargetsAwayFromTheCaster()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(7, zone.GetEntityPosition(target).x,
                "knocked one cell further from the caster");
        }

        [Test]
        public void GroundSurge_TargetAgainstAWallIsNotPushed_ButStillTakesDamage()
        {
            // Counter-check to the push test: a blocked shove must not
            // swallow the hit. The wave still lands.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);
            MakeWall(zone, 7, 5);

            int hp = target.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(6, zone.GetEntityPosition(target).x, "the wall holds them");
            Assert.Less(target.GetStatValue("Hitpoints"), hp,
                "but the surge still hurts");
        }

        [Test]
        public void GroundSurge_AppliesElectrified_WhenTheRollSucceeds()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            // Roll 0 is under any positive threshold, so this is the
            // guaranteed-success branch.
            skill.OnCommand(CtxRng(atk, zone, 1, 0, new ScriptedRng(0)));

            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "a successful roll primes the target for a lightning follow-up");
        }

        [Test]
        public void GroundSurge_DoesNotElectrify_WhenTheRollFails()
        {
            // Counter-check (§3.4). Without this, an implementation that
            // ignored the roll and ALWAYS electrified would pass the test
            // above — the precondition would be vacuous.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            // Roll 99 is at or above any threshold below 100.
            skill.OnCommand(CtxRng(atk, zone, 1, 0, new ScriptedRng(99)));

            Assert.Less(Galvanism_GroundSurge.ELECTRIFY_PERCENT, 100,
                "precondition: the electrify is a CHANCE, not a certainty");
            Assert.IsFalse(target.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "a failed roll still damages and shoves, but does not prime");
            Assert.AreEqual(7, zone.GetEntityPosition(target).x,
                "the shove is unconditional — only the prime is a roll");
        }

        [Test]
        public void GroundSurge_DoesNotConsumeExistingStatuses()
        {
            // THE GRAMMAR (plan §4): skills PRIME, they never DETONATE.
            // If a future edit made a skill cash in a status, the
            // prime/detonate division collapses and grimoires lose the
            // only thing that makes them distinct.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);
            target.ApplyEffect(new WetEffect(), atk, zone);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(target.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "the water is still on them — only a rite may spend it");
        }

        [Test]
        public void GroundSurge_StopsAtAWall()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            MakeWall(zone, 6, 5);
            var shielded = MakeBodied("shielded");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(shielded, 7, 5);

            int hp = shielded.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(hp, shielded.GetStatValue("Hitpoints"),
                "a wave does not pass through stone");
        }

        [Test]
        public void GroundSurge_RespectsRange()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var outOfReach = MakeBodied("outOfReach");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(outOfReach, 5 + Galvanism_GroundSurge.SURGE_RANGE + 1, 5);

            int hp = outOfReach.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(hp, outOfReach.GetStatValue("Hitpoints"));
        }

        [Test]
        public void GroundSurge_NeverHitsTheCaster()
        {
            // An earlier draft put ONLY the caster in the zone, so it
            // passed on the empty-line early-out and would have passed
            // even against a power that happily damaged its own caster.
            // The cast must actually FIRE for the exclusion to mean
            // anything, so there are real targets here.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var ahead = MakeBodied("ahead");
            var behind = MakeBodied("behind");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(ahead, 6, 5);
            zone.AddEntity(behind, 4, 5);

            int casterHp = atk.GetStatValue("Hitpoints");
            int behindHp = behind.GetStatValue("Hitpoints");
            int aheadHp = ahead.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Less(ahead.GetStatValue("Hitpoints"), aheadHp,
                "precondition: the surge actually fired");
            Assert.AreEqual(casterHp, atk.GetStatValue("Hitpoints"),
                "the wave does not wash back over its caster");
            Assert.AreEqual(behindHp, behind.GetStatValue("Hitpoints"),
                "nor over anything behind them");
            Assert.IsFalse(atk.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "and never primes its caster");
        }

        [Test]
        public void GroundSurge_EmptyLine_EmitsNoTargetDiag()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.Contains("no_target", string.Join("|", RejectReasons()));
        }

        [Test]
        public void GroundSurge_NoDirection_EmitsDiag_AndHurtsNobody()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var bystander = MakeBodied("bystander");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(bystander, 6, 5);
            Diag.ResetAll();

            int hp = bystander.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 0, 0));

            StringAssert.Contains("no_direction", string.Join("|", RejectReasons()));
            Assert.AreEqual(hp, bystander.GetStatValue("Hitpoints"),
                "a directionless surge is a wasted turn, not a free nova");
        }

        [Test]
        public void GroundSurge_NullContextAndNullRng_DoNotCrash()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

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
        // Backlash Coil — the panic button
        // ════════════════════════════════════════════════════════

        [Test]
        public void BacklashCoil_Spec_IsSelfCentered()
        {
            var spec = new Galvanism_BacklashCoil().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandBacklashCoil", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode,
                "no aiming — it fires around you");
        }

        [Test]
        public void BacklashCoil_ShovesEveryAdjacentCreature()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            var east = MakeBodied("east");
            var west = MakeBodied("west");
            var northeast = MakeBodied("northeast");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(east, 11, 10);
            zone.AddEntity(west, 9, 10);
            zone.AddEntity(northeast, 11, 9);

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            // Asserted against the declared constant rather than a
            // literal: the invariant is "thrown clear by the coil's
            // stated distance", not "moved exactly one cell".
            int d = Galvanism_BacklashCoil.COIL_PUSH_CELLS;
            Assert.AreEqual(11 + d, zone.GetEntityPosition(east).x, "east flies east");
            Assert.AreEqual(9 - d, zone.GetEntityPosition(west).x, "west flies west");
            var ne = zone.GetEntityPosition(northeast);
            Assert.AreEqual(11 + d, ne.x, "and diagonals keep their diagonal");
            Assert.AreEqual(9 - d, ne.y);
        }

        [Test]
        public void BacklashCoil_NeedsNoDirection()
        {
            // Counter-check to GroundSurge_NoDirection: a self-centred
            // power must NOT reject on a zero direction, or the panic
            // button would need aiming in exactly the moment you have no
            // time to aim.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            var adjacent = MakeBodied("adjacent");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(adjacent, 11, 10);
            Diag.ResetAll();

            int hp = adjacent.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.Less(adjacent.GetStatValue("Hitpoints"), hp);
            StringAssert.DoesNotContain("no_direction", string.Join("|", RejectReasons()));
        }

        [Test]
        public void BacklashCoil_ThrowsFurtherThanASingleStep()
        {
            // The design reason Backlash Coil is not just a worse Ground
            // Surge: a one-cell shove leaves the attacker able to step
            // back in and swing on the same turn, so it would not be a
            // disengage at all. Pinning the intent means a future tuning
            // pass has to argue with this test rather than quietly
            // erasing the power's identity.
            Assert.Greater(Galvanism_BacklashCoil.COIL_PUSH_CELLS, 1,
                "a disengage must actually create distance");
        }

        [Test]
        public void BacklashCoil_DoesNotElectrify()
        {
            // The deliberate trade: raw disengage, no prime. If this
            // ever electrified, Backlash Coil would strictly dominate
            // Ground Surge and the family would collapse to one power.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            var adjacent = MakeBodied("adjacent");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(adjacent, 11, 10);

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.IsFalse(adjacent.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "Backlash Coil buys distance, it does not prime");
        }

        [Test]
        public void BacklashCoil_IgnoresNonAdjacentCreatures()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            var twoAway = MakeBodied("twoAway");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(twoAway, 12, 10);

            int hp = twoAway.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.AreEqual(hp, twoAway.GetStatValue("Hitpoints"),
                "the ring is radius 1, not a nova");
        }

        [Test]
        public void BacklashCoil_SurroundedOnAllSides_ShovesAllEight()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);

            var ring = new Entity[8];
            int n = 0;
            for (int ox = -1; ox <= 1; ox++)
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    ring[n] = MakeBodied($"r{ox}_{oy}");
                    zone.AddEntity(ring[n], 10 + ox, 10 + oy);
                    n++;
                }

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            for (int i = 0; i < 8; i++)
            {
                var p = zone.GetEntityPosition(ring[i]);
                int cheb = Math.Max(Math.Abs(p.x - 10), Math.Abs(p.y - 10));
                Assert.AreEqual(1 + Galvanism_BacklashCoil.COIL_PUSH_CELLS, cheb,
                    "every one of the eight is thrown clear — the surround is broken");
            }
        }

        [Test]
        public void BacklashCoil_NoOneAdjacent_EmitsNoTargetDiag()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 10, 10);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            StringAssert.Contains("no_target", string.Join("|", RejectReasons()));
        }

        // ════════════════════════════════════════════════════════
        // Rail Spike — reach past the front rank
        // ════════════════════════════════════════════════════════

        [Test]
        public void RailSpike_Spec_IsALongerLine()
        {
            var spec = new Galvanism_RailSpike().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandRailSpike", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            // Read the DECLARED range. Comparing the two constants to
            // each other is a tautology about two literals and leaves
            // the ability spec itself unpinned — a spec that shipped
            // Range = 1 would have passed.
            Assert.AreEqual(Galvanism_RailSpike.SPIKE_RANGE, spec.Range,
                "the spec must publish the range the power actually walks");
            Assert.Greater(spec.Range,
                new Galvanism_GroundSurge().DeclareActivatedAbility(null).Range,
                "the spike outreaches the surge — that is its whole point");
        }

        [Test]
        public void RailSpike_PiercesEveryCreatureInTheLine()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var front = MakeBodied("front");
            var middle = MakeBodied("middle");
            var back = MakeBodied("back");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(front, 6, 5);
            zone.AddEntity(middle, 7, 5);
            zone.AddEntity(back, 8, 5);

            int f = front.GetStatValue("Hitpoints");
            int m = middle.GetStatValue("Hitpoints");
            int b = back.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.Less(front.GetStatValue("Hitpoints"), f);
            Assert.Less(middle.GetStatValue("Hitpoints"), m);
            Assert.Less(back.GetStatValue("Hitpoints"), b,
                "nothing in a rank stops the spike");
        }

        [Test]
        public void RailSpike_ElectrifiesOnlyTheLastTarget()
        {
            // The signature: reach past the front rank to prime the
            // caster standing behind it.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var front = MakeBodied("front");
            var back = MakeBodied("back");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(front, 6, 5);
            zone.AddEntity(back, 8, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsFalse(front.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "the front rank is hit but not primed");
            Assert.IsTrue(back.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "the charge grounds out in the LAST body it reaches");
        }

        [Test]
        public void RailSpike_SingleTarget_IsAlsoTheLastTarget()
        {
            // Boundary: with exactly one creature in the line, "last"
            // must mean that one. An off-by-one that required a
            // predecessor would silently never electrify a lone target.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var only = MakeBodied("only");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(only, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.IsTrue(only.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "one target is the last target");
        }

        [Test]
        public void RailSpike_DoesNotPush()
        {
            // Counter-check across the family: pushing is Ground Surge
            // and Backlash Coil's identity. If the spike shoved too,
            // it would scatter the very back-line target it exists to
            // reach.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(6, zone.GetEntityPosition(target).x,
                "the spike passes through; it does not shove");
        }

        [Test]
        public void RailSpike_StopsAtAWall()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            MakeWall(zone, 7, 5);
            var front = MakeBodied("front");
            var shielded = MakeBodied("shielded");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(front, 6, 5);
            zone.AddEntity(shielded, 8, 5);

            int s = shielded.GetStatValue("Hitpoints");
            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(s, shielded.GetStatValue("Hitpoints"),
                "piercing bodies is not piercing stone");
            Assert.IsTrue(front.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "and the last target REACHED still takes the charge");
        }

        [Test]
        public void RailSpike_EmptyLine_EmitsNoTargetDiag()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.Contains("no_target", string.Join("|", RejectReasons()));
        }

        [Test]
        public void GroundSurge_ShovesEveryTargetInAPackedRank_NotJustTheFurthest()
        {
            // ADVERSARIAL REVIEW, CRITICAL (converged from 3 independent
            // reviewers). SkillLine.Collect returns targets
            // NEAREST-first, and TryPush refuses a step into a cell that
            // holds another creature. Pushing in collection order means
            // every target except the last shoves into the body behind
            // it and does not move — so in the canonical use of a line
            // AoE (a packed corridor) the shockwave moves exactly one
            // enemy. The JSON blurb promises "knocking each one a cell
            // further away".
            //
            // The fix is to shove FURTHEST-first so each destination is
            // vacated before the target behind it steps into it.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var near = MakeBodied("near");
            var far = MakeBodied("far");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(near, 6, 5);
            zone.AddEntity(far, 7, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(8, zone.GetEntityPosition(far).x,
                "the far target has open floor and must move");
            Assert.AreEqual(7, zone.GetEntityPosition(near).x,
                "and the near target follows into the cell just vacated");
        }

        [Test]
        public void GroundSurge_PushingIntoALiquidPool_FiresCellEntry()
        {
            // ADVERSARIAL REVIEW, CRITICAL. TryPush moved entities with
            // a raw Zone.MoveEntity, bypassing MovementSystem.TryMoveTo
            // — so no AfterMove, no cell-entered events, no render
            // repaint. That directly undercuts this feature's whole
            // thesis: shoving an enemy into water should SOAK them,
            // setting up the lightning follow-up. Pinned via the
            // AfterMove event, which TryMoveTo fires and MoveEntity
            // does not.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            bool afterMoveFired = false;
            target.AddPart(new MoveWatcherPart(() => afterMoveFired = true));

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.AreEqual(7, zone.GetEntityPosition(target).x, "precondition: it moved");
            Assert.IsTrue(afterMoveFired,
                "a shove is a move — it must run the movement pipeline, "
                + "or the shoved creature never enters its destination cell");
        }

        /// <summary>Test-only listener that records an AfterMove event.
        /// AfterMove is fired by MovementSystem.TryMoveTo and NOT by a
        /// raw Zone.MoveEntity, so it distinguishes the two paths.</summary>
        private class MoveWatcherPart : Part
        {
            public override string Name => "MoveWatcher";
            private readonly System.Action _onMove;
            public MoveWatcherPart(System.Action onMove) { _onMove = onMove; }
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterMove") _onMove?.Invoke();
                return true;
            }
        }

        // ════════════════════════════════════════════════════════
        // Charge magnitude — the design distinction between the powers
        // ════════════════════════════════════════════════════════

        [Test]
        public void GroundSurge_LeavesTheDeclaredCharge()
        {
            // Every other prime assertion in this file is a boolean
            // HasEffect, so an implementation that passed charge: 0f
            // would satisfy the whole suite while priming nothing worth
            // detonating. The magnitude IS the payload.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            skill.OnCommand(CtxRng(atk, zone, 1, 0, new ScriptedRng(0)));

            var el = target.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(el);
            Assert.AreEqual(Galvanism_GroundSurge.ELECTRIFY_CHARGE, el.Charge, 0.001f,
                "a dry target carries exactly the declared charge");
        }

        [Test]
        public void RailSpike_GroundsAHeavierChargeThanTheSurge()
        {
            // The spike's charge is guaranteed and lands on one target,
            // so it is deliberately larger than the surge's 40% spray.
            // Without this, the two constants could be swapped and no
            // test would notice.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var target = MakeBodied("target");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(target, 6, 5);

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            var el = target.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(el);
            Assert.AreEqual(Galvanism_RailSpike.SPIKE_CHARGE, el.Charge, 0.001f);
            Assert.Greater(Galvanism_RailSpike.SPIKE_CHARGE,
                Galvanism_GroundSurge.ELECTRIFY_CHARGE,
                "a guaranteed single-target charge outweighs a 40% spray");
        }

        [Test]
        public void SurgingAWetTarget_DoublesTheCharge_TheWholeDesignThesis()
        {
            // The payoff the entire feature exists to serve: soak, then
            // shock. ElectrifiedEffect.OnApply doubles Charge on a
            // target with Moisture > 0.2. This test is the end-to-end
            // proof that a SKILL primer feeds that amplification — not
            // just that the effect class does it in isolation.
            var atk = MakeBodied("atk");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            var dry = MakeBodied("dry");
            var wet = MakeBodied("wet");
            // Caster at (5,5): the east line is (6,5)...; the SE
            // diagonal is (6,6),(7,7)... An earlier draft put the wet
            // target at (6,7), which sits on neither.
            zone.AddEntity(dry, 6, 5);
            zone.AddEntity(wet, 6, 6);
            wet.ApplyEffect(new WetEffect(), atk, zone);

            var skill = Fix<Galvanism_GroundSurge>(atk);
            skill.OnCommand(CtxRng(atk, zone, 1, 0, new ScriptedRng(0)));
            skill.OnCommand(CtxRng(atk, zone, 1, 1, new ScriptedRng(0)));

            var dryCharge = dry.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            var wetCharge = wet.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(dryCharge);
            Assert.IsNotNull(wetCharge, "the wet target was in the second cast's line");
            Assert.Greater(wetCharge.Charge, dryCharge.Charge,
                "water carries the charge — soaking first is rewarded");
        }

        // ════════════════════════════════════════════════════════
        // Lethality — branches the 200-HP fixture could never reach
        // ════════════════════════════════════════════════════════

        [Test]
        public void GroundSurge_DoesNotPrimeOrShoveATargetItKilled()
        {
            // `if (Hitpoints <= 0) continue` was unreachable for the
            // whole suite: every fixture creature has 200 HP against 6
            // damage. A corpse must not be primed (nothing left to
            // detonate) nor dragged across the floor.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_GroundSurge>(atk);
            var zone = new Zone();
            var frail = MakeBodied("frail", hp: 1);
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(frail, 6, 5);

            skill.OnCommand(CtxRng(atk, zone, 1, 0, new ScriptedRng(0)));

            Assert.LessOrEqual(frail.GetStatValue("Hitpoints"), 0,
                "precondition: the surge was lethal");
            Assert.IsFalse(frail.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "a corpse is not a primed target");
        }

        [Test]
        public void RailSpike_KillingItsGroundTarget_EmitsDiag_InsteadOfSilentlyNotPriming()
        {
            // A player who lined the shot up to prime the back rank
            // deserves to know why the charge never appeared. Without
            // the record this is an invisible non-event.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var frail = MakeBodied("frail", hp: 1);
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(frail, 6, 5);
            Diag.ResetAll();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            Assert.LessOrEqual(frail.GetStatValue("Hitpoints"), 0,
                "precondition: the ground target died");
            StringAssert.Contains("ground_target_died", string.Join("|", RejectReasons()));
        }

        [Test]
        public void BacklashCoil_DoesNotShoveATargetItKilled()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            var frail = MakeBodied("frail", hp: 1);
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(frail, 11, 10);

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            Assert.LessOrEqual(frail.GetStatValue("Hitpoints"), 0,
                "precondition: the coil was lethal");
            Assert.IsFalse(frail.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "and the coil never primes regardless");
        }

        // ════════════════════════════════════════════════════════
        // What the player is TOLD — the message log
        // ════════════════════════════════════════════════════════

        private static string LastMessage() => MessageLog.GetLast() ?? "";

        [Test]
        public void RailSpike_MessageDoesNotClaimACharge_WhenItKilledTheGroundTarget()
        {
            // The diag reason was pinned but the PLAYER-FACING text was
            // not, so a revert to the unconditional "grounds in X!" would
            // have passed the whole suite while telling the player their
            // setup worked. The message is the only channel the player
            // actually reads.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var frail = MakeBodied("frail", hp: 1);
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(frail, 6, 5);
            MessageLog.Clear();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.DoesNotContain("grounds in", LastMessage(),
                "it did not ground — the target died first");
            StringAssert.Contains("falls before the charge", LastMessage());
        }

        [Test]
        public void RailSpike_MessageClaimsTheCharge_WhenTheGroundTargetSurvives()
        {
            // Counter-check: the branch above must not fire on the happy
            // path, or the power would always look like it failed.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            var tough = MakeBodied("tough");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(tough, 6, 5);
            MessageLog.Clear();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.Contains("grounds in", LastMessage());
        }

        [Test]
        public void RailSpike_MessagePluralisesBodiesCorrectly()
        {
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_RailSpike>(atk);
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(MakeBodied("a"), 6, 5);
            zone.AddEntity(MakeBodied("b"), 7, 5);
            MessageLog.Clear();

            skill.OnCommand(Ctx(atk, zone, 1, 0));

            StringAssert.Contains("2 bodies", LastMessage());
            StringAssert.DoesNotContain("bodys", LastMessage());
        }

        [Test]
        public void BacklashCoil_CorneredCast_ReportsWhoItHit_NotZeroThrown()
        {
            // The exact situation the power exists for: pinned in a
            // corridor, walls on both sides. Reporting the SHOVE count
            // here printed "hurls 0 attackers clear!" after a landed hit.
            var atk = MakeBodied("atk");
            var skill = Fix<Galvanism_BacklashCoil>(atk);
            var zone = new Zone();
            var pinned = MakeBodied("pinned");
            zone.AddEntity(atk, 10, 10);
            zone.AddEntity(pinned, 11, 10);
            MakeWall(zone, 12, 10);
            MessageLog.Clear();

            skill.OnCommand(Ctx(atk, zone, 0, 0));

            StringAssert.DoesNotContain("hurling 0", LastMessage());
            StringAssert.Contains("1 attacker", LastMessage(),
                "the coil still blasted someone");
            StringAssert.Contains("nothing gives", LastMessage(),
                "and says plainly that they did not move");
        }

        // ════════════════════════════════════════════════════════
        // Family-level invariants
        // ════════════════════════════════════════════════════════

        [Test]
        public void AllThree_HaveDistinctCommands()
        {
            // A duplicate command string would make one power
            // unreachable through SkillsPart.TryRouteSkillCommand, and
            // it would fail silently rather than loudly.
            var a = new Galvanism_GroundSurge().DeclareActivatedAbility(null).Command;
            var b = new Galvanism_BacklashCoil().DeclareActivatedAbility(null).Command;
            var c = new Galvanism_RailSpike().DeclareActivatedAbility(null).Command;

            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(a, c);
        }

        [Test]
        public void AllThree_AreRegisteredInTheGalvanismTreeJson()
        {
            // Content check: a power class with no JSON entry is
            // unbuyable and therefore does not exist for the player —
            // the same reachability trap the loot arc kept hitting.
            // Parsed through the REAL registry, not substring-matched:
            // a grep would pass on a class name that appeared only in a
            // description, or in malformed JSON the loader rejects.
            var json = System.IO.File.ReadAllText(System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "Resources/Content/Data/Skills/Galvanism.json"));
            SkillRegistry.LoadFromJson(json, "test:Galvanism");

            foreach (var cls in new[] { "Galvanism_GroundSurge",
                                        "Galvanism_BacklashCoil",
                                        "Galvanism_RailSpike" })
            {
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(cls, out var power),
                    cls + " must be a purchasable power, or it does not exist "
                    + "for the player at all");
                Assert.Greater(power.Cost, 0, cls + " needs a skill-point cost");
                Assert.IsFalse(string.IsNullOrWhiteSpace(power.Description),
                    cls + " needs a description — it is all the player sees "
                    + "before spending the point");
                Assert.LessOrEqual(power.Description.Length, 55,
                    cls + " description is truncated by SkillsScreenUI "
                    + "(POPUP_W - 4 = 56, cut at 55), hiding the mechanic");
            }
        }

        [Test]
        public void AllThree_DealElectricDamage_SoGalvanismsBonusApplies()
        {
            // The tree's own OnGetSpellDamageModifier only fires on
            // Electric-element damage. A power in the lightning tree
            // that dealt untyped damage would silently miss both the
            // tree bonus and every Electric resistance.
            var atk = MakeBodied("atk");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);

            var immune = MakeBodied("immune");
            immune.Statistics["ElectricResistance"].BaseValue = 100;
            zone.AddEntity(immune, 6, 5);

            // Cast ALL THREE. An earlier draft fired only Ground Surge
            // while the test name and comment claimed to cover the
            // family, so Backlash Coil's and Rail Spike's damage typing
            // were entirely unpinned.
            int hp = immune.GetStatValue("Hitpoints");

            Fix<Galvanism_GroundSurge>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            Assert.AreEqual(hp, immune.GetStatValue("Hitpoints"), "Ground Surge is Electric");

            // Coil shoves, so re-seat the target adjacent before it fires.
            zone.MoveEntity(immune, 6, 5);
            Fix<Galvanism_BacklashCoil>(atk).OnCommand(Ctx(atk, zone, 0, 0));
            Assert.AreEqual(hp, immune.GetStatValue("Hitpoints"), "Backlash Coil is Electric");

            zone.MoveEntity(immune, 6, 5);
            Fix<Galvanism_RailSpike>(atk).OnCommand(Ctx(atk, zone, 1, 0));
            Assert.AreEqual(hp, immune.GetStatValue("Hitpoints"), "Rail Spike is Electric");
        }
    }
}
