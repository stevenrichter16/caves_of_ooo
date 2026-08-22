using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.4 SM-A (Docs/FELLING-W4-PLAN.md §6.2) — BloomedEffect + the
    /// compulsion goal. The Bloom is terrain wearing a body: the bearer
    /// attacks whatever stands adjacent — its own faction included —
    /// and otherwise leaves the others for open ground. Cured ONLY by
    /// the Choir's ritual (conversation CureEffect); the cure-all tonic
    /// does not understand it (R4).
    /// </summary>
    public class BloomedEffectTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            Diag.ResetAll();
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            SettlementRuntime.Reset();
        }

        // --- Fixture (CombatPathfindingTests shape + StatusEffectsPart) ---

        private static Entity MakeCreature(string faction, int hp = 30)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N").Substring(0, 8), BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction)) e.Tags["Faction"] = faction;
            void S(string n, int v, int max) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = 0, Max = max };
            S("Hitpoints", hp, hp);
            S("Strength", 16, 50); S("Agility", 16, 50);
            S("Toughness", 16, 50); S("Speed", 100, 200);
            e.Statistics["DV"] = new Stat { Owner = e, Name = "DV", BaseValue = 0, Min = -20, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = faction ?? "creature" });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            return e;
        }

        private static BrainPart GiveBrain(Entity e, Zone zone)
        {
            var brain = new BrainPart { SightRadius = 20, CurrentZone = zone, Rng = new Random(42) };
            e.AddPart(brain);
            return brain;
        }

        /// <summary>One bearer turn, in TurnManager's order: effects see
        /// BeginTakeAction (goal push) before the brain sees TakeTurn
        /// (goal execution) — the same-tick contract the sweep verified.</summary>
        private static void Cycle(Entity e)
        {
            var begin = GameEvent.New("BeginTakeAction");
            e.FireEventAndRelease(begin);
            var turn = GameEvent.New("TakeTurn");
            e.FireEventAndRelease(turn);
        }

        // ════════════════════════════════════════════════════════════
        //   The compulsion
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BloomedGuard_AttacksItsOwnAdjacentAlly()
        {
            var zone = new Zone("BloomAttack");
            var guard = MakeCreature("GroveWardens");
            var ally = MakeCreature("GroveWardens");
            GiveBrain(guard, zone);
            zone.AddEntity(guard, 10, 10);
            zone.AddEntity(ally, 11, 10);

            guard.ApplyEffect(new BloomedEffect());
            int hpBefore = ally.GetStatValue("Hitpoints", -1);
            for (int i = 0; i < 5; i++) Cycle(guard);

            Assert.Less(ally.GetStatValue("Hitpoints", -1), hpBefore,
                "the Bloom does not consult the faction table — whatever " +
                "stands adjacent gets attacked, its own ward included");
        }

        [Test]
        public void UnbloomedGuard_LeavesItsAllyAlone()
        {
            // Counter-check: identical setup, no effect — same-faction
            // neighbors are not a fight.
            var zone = new Zone("BloomCounter");
            var guard = MakeCreature("GroveWardens");
            var ally = MakeCreature("GroveWardens");
            GiveBrain(guard, zone);
            zone.AddEntity(guard, 10, 10);
            zone.AddEntity(ally, 11, 10);

            int hpBefore = ally.GetStatValue("Hitpoints", -1);
            for (int i = 0; i < 5; i++) Cycle(guard);

            Assert.AreEqual(hpBefore, ally.GetStatValue("Hitpoints", -1),
                "no Bloom, no compulsion");
        }

        [Test]
        public void BloomedHost_StepsAwayFromNearbyCompany()
        {
            // No one adjacent, an ally in notice range: the drive is
            // AWAY — open ground, not company.
            var zone = new Zone("BloomAway");
            var host = MakeCreature("GroveWardens");
            var ally = MakeCreature("GroveWardens");
            GiveBrain(host, zone);
            zone.AddEntity(host, 10, 10);
            zone.AddEntity(ally, 13, 10);

            host.ApplyEffect(new BloomedEffect());
            Cycle(host);

            var pos = zone.GetEntityPosition(host);
            int dist = Math.Max(Math.Abs(pos.x - 13), Math.Abs(pos.y - 10));
            Assert.Greater(dist, 3, "the bearer leaves the others");
        }

        [Test]
        public void Bloom_PushesExactlyOneGoal_AndRemovalPopsIt()
        {
            // The HasGoal guard: three turns must not stack three goals.
            // Observable proof: removing the effect pops THE goal — if
            // duplicates had stacked, one would survive the pop.
            var zone = new Zone("BloomOnce");
            var host = MakeCreature("GroveWardens");
            var brain = GiveBrain(host, zone);
            zone.AddEntity(host, 10, 10);

            host.ApplyEffect(new BloomedEffect());
            for (int i = 0; i < 3; i++) Cycle(host);
            Assert.IsTrue(brain.HasGoal<BloomGoal>(), "the compulsion is on the stack");

            host.GetPart<StatusEffectsPart>().RemoveEffect<BloomedEffect>();
            Assert.IsFalse(brain.HasGoal<BloomGoal>(),
                "one push, one pop — no duplicate goals left behind");
        }

        [Test]
        public void Bloom_EmitsCompelledDiag_OncePerPush()
        {
            var zone = new Zone("BloomDiag");
            var host = MakeCreature("GroveWardens");
            GiveBrain(host, zone);
            zone.AddEntity(host, 10, 10);

            host.ApplyEffect(new BloomedEffect());
            for (int i = 0; i < 3; i++) Cycle(host);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "BloomCompelled", Limit = 10 }).Records;
            Assert.AreEqual(1, recs.Count,
                "one record per actual push — the HasGoal guard keeps re-pushes silent");
        }

        // ════════════════════════════════════════════════════════════
        //   R4 — the cure taxonomy
        // ════════════════════════════════════════════════════════════

        [Test]
        public void CureAllTonic_CuresTheAilment_ButNotTheBloom()
        {
            // The oath pattern (OathAdversarialTests): the tonic proves
            // it works on a real ailment in the same swallow.
            var zone = new Zone("BloomTonic");
            var host = MakeCreature("GroveWardens");
            zone.AddEntity(host, 10, 10);
            host.ApplyEffect(new BloomedEffect());
            host.ApplyEffect(new PoisonedEffect { Duration = 10 });

            var tonic = new Entity { ID = "tonic", BlueprintName = "Panacea" };
            tonic.AddPart(new CureTonicPart { CureEffect = "All" });
            var ev = GameEvent.New("ApplyTonic");
            ev.SetParameter("Actor", (object)host);
            tonic.FireEvent(ev);
            ev.Release();

            Assert.IsFalse(host.HasEffect<PoisonedEffect>(),
                "the ailment is cured — the tonic still works");
            Assert.IsTrue(host.HasEffect<BloomedEffect>(),
                "the Bloom is not an ailment a tonic understands (R4)");
        }

        [Test]
        public void ChoirRitual_RemovesTheBloom_AndTheCompulsion()
        {
            // The one cure: the conversation CureEffect action, matched
            // by name — the exact JSON the shrine choices will carry.
            var zone = new Zone("BloomRitual");
            var host = MakeCreature("GroveWardens");
            var brain = GiveBrain(host, zone);
            zone.AddEntity(host, 10, 10);
            var choir = MakeCreature("RotChoir");
            zone.AddEntity(choir, 12, 10);

            host.ApplyEffect(new BloomedEffect());
            Cycle(host);
            Assert.IsTrue(brain.HasGoal<BloomGoal>(), "precondition: compelled");

            ConversationActions.Execute("CureEffect", choir, host, "Bloomed");

            Assert.IsFalse(host.HasEffect<BloomedEffect>(), "the Choir takes it back");
            Assert.IsFalse(brain.HasGoal<BloomGoal>(), "and the legs are the bearer's again");
        }

        // ════════════════════════════════════════════════════════════
        //   SM-C — eruption on death (OnRemove + CAUSE_OWNER_DIED)
        // ════════════════════════════════════════════════════════════

        private static void InitBloomGas() => GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""bloom-spores"", ""GasType"":""BloomSpores"",
                ""Glyph"":""°"", ""Color"":""&m"",
                ""DefaultDensity"":60, ""DefaultLevel"":1,
                ""BehaviorKind"":""BloomSpores"" } ] }");

        private static int BloomGasDensityAt(Zone zone, int x, int y)
        {
            int total = 0;
            foreach (var e in zone.GetEntitiesWithTag("Gas"))
            {
                var p = zone.GetEntityPosition(e);
                var pool = e.GetPart<GasPoolPart>();
                if (p.x == x && p.y == y && pool != null && pool.GasId == "bloom-spores")
                    total += pool.Density;
            }
            return total;
        }

        [Test]
        public void Death_Erupts_BloomSpores_AtTheBody()
        {
            // Effects never see the Died event — StatusEffectsPart
            // translates death into RemoveAllEffects(CAUSE_OWNER_DIED),
            // and the eruption rides OnRemove gated on that cause
            // (sweep row 4). The cell is still resolvable during
            // dispatch: removal runs before zone.RemoveEntity.
            InitBloomGas();
            var zone = new Zone("BloomErupt");
            SettlementRuntime.ActiveZone = zone;
            var host = MakeCreature("GroveWardens", hp: 5);
            zone.AddEntity(host, 10, 10);
            host.ApplyEffect(new BloomedEffect());

            var killer = MakeCreature("Concord");
            zone.AddEntity(killer, 11, 10);
            var d = new Damage(50);
            d.AddAttribute("Slashing");
            CombatSystem.ApplyDamage(host, d, killer, zone);

            Assert.Greater(BloomGasDensityAt(zone, 10, 10), 0,
                "the body opens — bloom spores at the death cell");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "BloomErupted", Limit = 5 }).Records.Count,
                "the eruption records itself");
        }

        [Test]
        public void TheCure_DoesNotErupt()
        {
            // Counter (the CAUSE gate): a Choir cure arrives as
            // CAUSE_EXTERNAL — being healed is not being opened.
            InitBloomGas();
            var zone = new Zone("BloomNoErupt");
            SettlementRuntime.ActiveZone = zone;
            var host = MakeCreature("GroveWardens");
            zone.AddEntity(host, 10, 10);
            host.ApplyEffect(new BloomedEffect());

            ConversationActions.Execute("CureEffect", null, host, "Bloomed");

            Assert.IsFalse(host.HasEffect<BloomedEffect>(), "precondition: cured");
            Assert.AreEqual(0, BloomGasDensityAt(zone, 10, 10),
                "no spores — the cure is a cure");
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "BloomErupted", Limit = 5 }).Records.Count);
        }

        // ════════════════════════════════════════════════════════════
        //   SM-F — the player's compulsion: sometimes the legs decide
        // ════════════════════════════════════════════════════════════

        private static Entity MakePlayer(Zone zone, int x, int y)
        {
            // PRODUCTION-FAITHFUL fixture (close-out 🔴 #1): the real
            // player inherits Creature and therefore HAS a BrainPart —
            // which TurnManager/BrainPart never drive (Player-tag
            // skips). The first fixture omitted the brain and masked
            // that the stride gated on brain-null was dead code in
            // live play. The stride must fire for a Player-tagged
            // bearer WITH a brain.
            var e = MakeCreature(null);
            e.Tags["Player"] = "";
            e.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(11) });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void PlayerCycle(Entity e)
        {
            var begin = GameEvent.New("BeginTakeAction");
            e.FireEventAndRelease(begin);
        }

        [Test]
        public void PlayerBearer_IsWalked_OnTheStride()
        {
            var zone = new Zone("BloomStride");
            SettlementRuntime.ActiveZone = zone;
            var player = MakePlayer(zone, 10, 10);
            var other = MakeCreature("GroveWardens");
            zone.AddEntity(other, 13, 10);

            player.ApplyEffect(new BloomedEffect());

            for (int i = 0; i < BloomedEffect.BLOOM_STRIDE - 1; i++)
                PlayerCycle(player);
            var before = zone.GetEntityPosition(player);
            Assert.AreEqual((10, 10), (before.x, before.y),
                "between strides, the legs are still yours");

            PlayerCycle(player);
            var after = zone.GetEntityPosition(player);
            int dist = Math.Max(Math.Abs(after.x - 13), Math.Abs(after.y - 10));
            Assert.Greater(dist, 3,
                "on the stride, the Bloom walks you away from the others");

            // Counter (🔴 #1's second half): the player's brain must
            // NEVER receive a BloomGoal — TakeTurn never fires on
            // players, so a pushed goal would be a lie the
            // BloomCompelled diag repeats.
            Assert.IsFalse(player.GetPart<BrainPart>().HasGoal<BloomGoal>(),
                "players are walked, never goal-driven");

            // And the stride records itself, once, with its own shape.
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "BloomCompelled", Limit = 10 }).Records;
            Assert.AreEqual(1, recs.Count,
                "one stride, one record — the four quiet turns emitted none");
            StringAssert.Contains("\"stride\":true", recs[0].PayloadJson);
        }

        [Test]
        public void Eruption_IsConcentrated_LevelTwo()
        {
            // Close-out 🔴 #3: every shipped bloom-spores source was
            // level 1, and chance(1, Toughness 18 — the shipped player
            // statline) is exactly 0: the plan's player infection path
            // was arithmetic-dead. A corpse burst is concentrated —
            // level 2 — and chance(2, 18) = 9%: rare, real.
            InitBloomGas();
            var zone = new Zone("BloomEruptLevel");
            SettlementRuntime.ActiveZone = zone;
            var host = MakeCreature("GroveWardens", hp: 5);
            zone.AddEntity(host, 10, 10);
            host.ApplyEffect(new BloomedEffect());
            var killer = MakeCreature("Concord");
            zone.AddEntity(killer, 11, 10);
            var d = new Damage(50);
            d.AddAttribute("Slashing");
            CombatSystem.ApplyDamage(host, d, killer, zone);

            GasPoolPart pool = null;
            foreach (var e in zone.GetEntitiesWithTag("Gas"))
                if (e.GetPart<GasPoolPart>()?.GasId == "bloom-spores")
                    pool = e.GetPart<GasPoolPart>();
            Assert.IsNotNull(pool, "precondition: erupted");
            Assert.AreEqual(2, pool.Level, "the burst is concentrated");
            Assert.Greater(GasBloomSporesPart.ComputeTakeChance(pool.Level, 18), 0,
                "a shipped body CAN be taken by a shipped source");
        }

        [Test]
        public void AloneBearer_DriftsTowardOpenGround()
        {
            // Close-out 🧪: the goal's third branch was untested. A
            // lone bearer beside a wall cluster walks off it, toward
            // ground with a full open ring.
            var zone = new Zone("BloomDrift");
            var host = MakeCreature("GroveWardens");
            GiveBrain(host, zone);
            zone.AddEntity(host, 3, 10);
            for (int y = 8; y <= 12; y++)
            {
                var wall = new Entity { ID = "w" + y, BlueprintName = "Wall" };
                wall.Tags["Solid"] = "";
                wall.AddPart(new RenderPart());
                wall.AddPart(new PhysicsPart { Solid = true });
                zone.AddEntity(wall, 2, y);
            }

            host.ApplyEffect(new BloomedEffect());
            var before = zone.GetEntityPosition(host);
            for (int i = 0; i < 3; i++) Cycle(host);
            var after = zone.GetEntityPosition(host);

            Assert.IsTrue(after.x != before.x || after.y != before.y,
                "alone, the Bloom still walks its bearer toward the open");
            Assert.Greater(after.x, before.x - 1,
                "and not INTO the wall line");
        }

        [Test]
        public void UncompelledPlayer_IsNeverWalked()
        {
            // Counter-check: no Bloom, no stride.
            var zone = new Zone("BloomStrideCounter");
            SettlementRuntime.ActiveZone = zone;
            var player = MakePlayer(zone, 10, 10);
            var other = MakeCreature("GroveWardens");
            zone.AddEntity(other, 13, 10);

            for (int i = 0; i < 12; i++) PlayerCycle(player);

            var pos = zone.GetEntityPosition(player);
            Assert.AreEqual((10, 10), (pos.x, pos.y));
        }

        [Test]
        public void TheStrideClock_SurvivesSaveLoad()
        {
            var host = MakeCreature(null);
            host.Tags["Player"] = "";
            var fx = new BloomedEffect();
            host.ApplyEffect(fx);
            fx.TurnsWorn = 3;

            var loaded = PartRoundTripHelper.RoundTripEntity(host);

            Assert.AreEqual(3,
                loaded.GetPart<StatusEffectsPart>().GetEffect<BloomedEffect>().TurnsWorn,
                "the stride phase rides the save — no free reset by reloading");
        }

        // ════════════════════════════════════════════════════════════
        //   Persistence + scars
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Bloom_SurvivesSaveLoad_Indefinite()
        {
            var host = MakeCreature("GroveWardens");
            host.ApplyEffect(new BloomedEffect());

            var loaded = PartRoundTripHelper.RoundTripEntity(host);

            var fx = loaded.GetPart<StatusEffectsPart>()?.GetEffect<BloomedEffect>();
            Assert.IsNotNull(fx, "the Bloom rides the save");
            Assert.AreEqual(-1, fx.Duration,
                "and the clock still does not apply — the Choir or death ends it");
        }

        [Test]
        public void TheVictim_Remembers_EvenAfterTheCure()
        {
            // Documented behavior, pinned on purpose: personal hostility
            // from a Bloomed attack is PERMANENT (BrainPart.PersonalEnemies
            // has no forgiveness path outside party joins). The Bloom
            // leaves scars — a cured guard's victims keep the vendetta.
            var zone = new Zone("BloomScar");
            var guard = MakeCreature("GroveWardens");
            var victim = MakeCreature("GroveWardens");
            GiveBrain(guard, zone);
            var victimBrain = GiveBrain(victim, zone);
            zone.AddEntity(guard, 10, 10);
            zone.AddEntity(victim, 11, 10);

            guard.ApplyEffect(new BloomedEffect());
            for (int i = 0; i < 5; i++) Cycle(guard);
            Assert.Less(victim.GetStatValue("Hitpoints", -1), 30, "precondition: struck");

            guard.GetPart<StatusEffectsPart>().RemoveEffect<BloomedEffect>();
            Assert.IsTrue(victimBrain.IsPersonallyHostileTo(guard),
                "the scar outlives the cure — documented, not accidental");
        }
    }
}
