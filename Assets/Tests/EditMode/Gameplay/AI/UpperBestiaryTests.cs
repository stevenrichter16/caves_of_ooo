using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P5 (Docs/UPPER-TIER-BESTIARY.md): the upper-tier creatures made distinct, and the
    /// behaviour kit that gives enemies decisions. Grows one section per sub-milestone.
    /// </summary>
    public sealed class UpperBestiaryTests
    {
        DensityCombatFixture f;
        [SetUp] public void Setup() => f = new DensityCombatFixture();
        [TearDown] public void TearDown() => f.Dispose();

        // High rolls that always hit, but a d10 of 9: an unbroken run of tens is an
        // endless penetration chain (the CommittedMeleeTests MeleeRandom convention).
        sealed class FixedRandom : System.Random
        {
            public override int Next(int maxValue) => maxValue - 1;
            public override int Next(int minValue, int maxValue) => maxValue == 11 ? 9 : maxValue - 1;
        }

        int Swing(int percent)
        {
            var attacker = f.Actor(10, 10, skills: "");
            var target = f.Target(11, 10);
            int before = target.GetStatValue("Hitpoints");
            Assert.True(CombatSystem.PerformCommittedMeleeAttack(attacker, target, f.Zone, new FixedRandom(), percent));
            int lost = before - target.GetStatValue("Hitpoints");
            f.Zone.RemoveEntity(attacker); f.Zone.RemoveEntity(target);
            return lost;
        }

        // ════════════════ B1: heavy swings ════════════════

        [Test] public void AHeavySwingLandsAtItsPercentOfAnOrdinaryHit()
        {
            int ordinary = Swing(100), heavy = Swing(200);
            Assert.Greater(ordinary, 0, "precondition: the fixed roll lands and penetrates");
            Assert.AreEqual(ordinary * 2, heavy);
        }

        [Test] public void TheHeavyScopeEndsWithTheSwing()
        {
            int ordinary = Swing(100);
            Swing(250);
            var attacker = f.Actor(10, 10, skills: ""); var target = f.Target(11, 10);
            int before = target.GetStatValue("Hitpoints");
            CombatSystem.PerformMeleeAttack(attacker, target, f.Zone, new FixedRandom());
            Assert.AreEqual(ordinary, before - target.GetStatValue("Hitpoints"), "counter-check: the next ordinary strike is unscaled");
        }

        [Test] public void TheBrutesWindUpHeavySwingsInTheirBlueprints()
        {
            foreach (var (name, percent) in new[] { ("AncientGuardian", 200), ("StoneGolem", 200), ("ObsidianBrute", 250) })
            {
                var e = f.Factory.CreateEntity(name);
                var c = e.GetPart<CommittedMeleePart>();
                Assert.NotNull(c, name);
                Assert.AreEqual(percent, c.DamagePercent, name);
            }
            Assert.AreEqual(100, new CommittedMeleePart().DamagePercent, "counter-check: the shambler's lash keeps the original contract");
        }

        // ════════════════ B1: distinct upper creatures ════════════════

        static readonly string[] Upper =
            { "AncientGuardian", "StoneGolem", "VaultSentinel", "ObsidianBrute", "BrittleHound", "PaleStalker",
              "DuneLurker", "CanopyStrangler", "Mosshulk", "PalimpsestEcho" };

        [Test] public void EveryUpperCreatureCanBeToldApartByLooking()
        {
            foreach (var name in Upper)
            {
                var e = f.Factory.CreateEntity(name);
                var text = e.GetPart<ExaminablePart>()?.Description;
                Assert.False(string.IsNullOrWhiteSpace(text), name + " has a description");
                Assert.Greater(text.Length, 60, name + " says something about how it fights");
            }
        }

        [Test] public void ConstructsAndHulksNeverFleeWhilePredatorsDo()
        {
            foreach (var name in new[] { "AncientGuardian", "StoneGolem", "VaultSentinel", "ObsidianBrute", "Mosshulk" })
                Assert.AreEqual(0f, f.Factory.CreateEntity(name).GetPart<BrainPart>().FleeThreshold, name);
            foreach (var name in new[] { "BrittleHound", "PaleStalker", "DuneLurker", "CanopyStrangler" })
                Assert.Greater(f.Factory.CreateEntity(name).GetPart<BrainPart>().FleeThreshold, 0f, "counter-check: " + name + " still runs");
        }

        [Test] public void CastersAndHulksCarryTheirAbilities()
        {
            foreach (var (name, skill) in new[] { ("VaultSentinel", "Galvanism_ArcBolt"), ("PalimpsestEcho", "Cryomancy_IceLance"), ("Mosshulk", "Cudgel_Slam") })
            {
                var e = f.Factory.CreateEntity(name);
                e.FireEventAndRelease(GameEvent.New("ObjectCreated"));
                Assert.True(e.GetPart<SkillsPart>()?.HasSkill(skill) == true, name + " knows " + skill);
            }
        }

        // ════════════════ B3: the stoop ════════════════

        Entity Bird(int x = 10, int y = 10)
        {
            var bird = f.Factory.CreateEntity("SkySari"); bird.ID = "sky-" + System.Guid.NewGuid().ToString("N");
            f.Zone.AddEntity(bird, x, y);
            var brain = bird.GetPart<BrainPart>(); brain.CurrentZone = f.Zone; brain.Rng = new FixedRandom();
            return bird;
        }

        [Test] public void AShadowCrossesYouTwoTurnsBeforeTheHit()
        {
            var bird = Bird(); var you = f.Target(14, 10); var stoop = bird.GetPart<StoopPart>();
            Assert.NotNull(stoop, "the Sky-Sari stoops");
            int hp = you.GetStatValue("Hitpoints");
            Assert.True(stoop.TryAct(you, f.Zone, new FixedRandom(), true), "it rises and marks you");
            Assert.True(stoop.Shadows(f.Zone, 14, 10));
            StringAssert.Contains("A shadow crosses you", string.Join("\n", MessageLog.GetRecent(3)));
            Assert.True(stoop.AdvancePending(f.Zone, new FixedRandom()));
            Assert.AreEqual(hp, you.GetStatValue("Hitpoints"), "one turn of warning left");
            Assert.True(stoop.AdvancePending(f.Zone, new FixedRandom()));
            Assert.Less(you.GetStatValue("Hitpoints"), hp, "you stayed in the shadow");
            Assert.AreEqual(1, SpatialQuery.Distance(f.Zone, bird, you), "it lands beside its mark");
            Assert.False(stoop.IsAloft);
        }

        [Test] public void SteppingOutOfTheShadowLeavesItsTalonsOnEmptyGround()
        {
            var bird = Bird(); var you = f.Target(14, 10); var stoop = bird.GetPart<StoopPart>();
            int hp = you.GetStatValue("Hitpoints");
            Assert.True(stoop.TryAct(you, f.Zone, new FixedRandom(), true));
            stoop.AdvancePending(f.Zone, new FixedRandom());
            f.Zone.MoveEntity(you, 14, 12);
            stoop.AdvancePending(f.Zone, new FixedRandom());
            Assert.AreEqual(hp, you.GetStatValue("Hitpoints"), "counter-check: the watchful step aside");
            StringAssert.Contains("empty ground", string.Join("\n", MessageLog.GetRecent(3)));
        }

        [Test] public void TheStoopHitsHarderThanItsBeak()
        {
            var bird = Bird(); var you = f.Target(14, 10); var stoop = bird.GetPart<StoopPart>();
            Assert.AreEqual(200, stoop.DamagePercent);
            int hp = you.GetStatValue("Hitpoints");
            stoop.TryAct(you, f.Zone, new FixedRandom(), true); stoop.AdvancePending(f.Zone, new FixedRandom()); stoop.AdvancePending(f.Zone, new FixedRandom());
            int dive = hp - you.GetStatValue("Hitpoints");
            int before = you.GetStatValue("Hitpoints");
            CombatSystem.PerformMeleeAttack(bird, you, f.Zone, new FixedRandom());
            int peck = before - you.GetStatValue("Hitpoints");
            Assert.Greater(peck, 0); Assert.AreEqual(peck * 2, dive, "the dive is the beak, doubled");
        }

        [Test] public void ItDoesNotStoopOnWhatIsBesideItOrOutOfReachOrUnseen()
        {
            var bird = Bird(); var stoop = bird.GetPart<StoopPart>();
            var near = f.Target(11, 10);
            Assert.False(stoop.TryAct(near, f.Zone, new FixedRandom(), true), "adjacent: it simply bites");
            f.Zone.RemoveEntity(near);
            var far = f.Target(10 + stoop.Range + 1, 10);
            Assert.False(stoop.TryAct(far, f.Zone, new FixedRandom(), true), "too far");
            f.Zone.MoveEntity(far, 14, 10);
            Assert.False(stoop.TryAct(far, f.Zone, new FixedRandom(), false), "it must see you to mark you");
            Assert.True(stoop.TryAct(far, f.Zone, new FixedRandom(), true));
        }

        [Test] public void AfterADiveItRestsBeforeRisingAgain()
        {
            var bird = Bird(); var you = f.Target(14, 10); var stoop = bird.GetPart<StoopPart>();
            stoop.TryAct(you, f.Zone, new FixedRandom(), true); stoop.AdvancePending(f.Zone, new FixedRandom()); stoop.AdvancePending(f.Zone, new FixedRandom());
            f.Zone.MoveEntity(you, f.Zone.GetEntityCell(bird).X + 4, 10);
            for (int i = 0; i < stoop.CooldownTurns; i++) Assert.False(stoop.TryAct(you, f.Zone, new FixedRandom(), true), "resting " + i);
            Assert.True(stoop.TryAct(you, f.Zone, new FixedRandom(), true), "then it rises");
        }

        [Test] public void AloftItOwnsItsTurnsThroughTheBrain()
        {
            var bird = Bird(); var you = f.Target(14, 10);
            var brain = bird.GetPart<BrainPart>(); brain.FleeThreshold = 0; brain.PushGoal(new KillGoal(you));
            int hp = you.GetStatValue("Hitpoints");
            for (int i = 0; i < 3; i++) { bird.FireEventAndRelease(GameEvent.New("TakeTurn")); bird.FireEventAndRelease(GameEvent.New("EndTurn")); }
            Assert.Less(you.GetStatValue("Hitpoints"), hp, "mark, wait, drop: three turns through the real brain");
        }

        [Test] public void AShadowSurvivesASaveMidAir()
        {
            var bird = Bird(); var you = f.Target(14, 10); var stoop = bird.GetPart<StoopPart>();
            stoop.TryAct(you, f.Zone, new FixedRandom(), true);
            var loaded = PartRoundTripHelper.RoundTripEntity(bird).GetPart<StoopPart>();
            Assert.AreEqual(stoop.Countdown, loaded.Countdown);
            Assert.AreEqual(14, loaded.MarkX); Assert.AreEqual(10, loaded.MarkY);
            Assert.AreEqual(f.Zone.ZoneID, loaded.MarkZone);
        }

        [Test] public void LeavingTheZoneDropsTheMark()
        {
            var bird = Bird(); var you = f.Target(14, 10); var stoop = bird.GetPart<StoopPart>();
            stoop.TryAct(you, f.Zone, new FixedRandom(), true);
            var elsewhere = new Zone("elsewhere");
            Assert.False(stoop.AdvancePending(elsewhere, new FixedRandom()));
            Assert.False(stoop.IsAloft);
        }

        // ════════════════ B4: grapple and drag ════════════════

        sealed class LowRoll : System.Random
        {
            public override int Next(int maxValue) => 0;
            public override int Next(int minValue, int maxValue) => minValue;
        }

        Entity Water(int x, int y)
        {
            var w = new Entity { ID = "water-" + x + "-" + y }; w.SetTag("Terrain");
            w.AddPart(new LiquidPoolPart { LiquidId = "water", Volume = 20 });
            f.Zone.AddEntity(w, x, y); return w;
        }
        Entity Beast(string blueprint, int x, int y, int chance = 100)
        {
            var e = f.Factory.CreateEntity(blueprint); e.ID = blueprint + "-" + System.Guid.NewGuid().ToString("N");
            f.Zone.AddEntity(e, x, y);
            var brain = e.GetPart<BrainPart>(); brain.CurrentZone = f.Zone; brain.Rng = new FixedRandom(); brain.FleeThreshold = 0;
            var g = e.GetPart<GrapplePart>(); if (g != null) g.Chance = chance;
            return e;
        }

        [Test] public void TheRiverwardenSeizesAndHaulsItsQuarryIntoTheWater()
        {
            var warden = Beast("Riverwarden", 12, 10); var you = f.Target(13, 10); Water(10, 10); Water(9, 10); Water(10, 9); Water(10, 11);
            var grip = warden.GetPart<GrapplePart>();
            Assert.NotNull(grip); Assert.True(grip.Drag);
            Assert.True(grip.TryAct(you, f.Zone, new FixedRandom(), true));
            var held = you.GetEffect<GrappledEffect>();
            Assert.NotNull(held, "a bite that draws blood takes hold");
            Assert.AreEqual(warden.ID, held.GrapplerId);
            for (int i = 0; i < 4 && f.Zone.GetEntityCell(you).X != 10; i++) Assert.True(grip.TryAct(you, f.Zone, new FixedRandom(), true), "drag " + i);
            Assert.AreEqual(10, f.Zone.GetEntityCell(you).X, "into the water");
            Assert.AreEqual(1, SpatialQuery.Distance(f.Zone, warden, you), "and it keeps hold");
            int hp = you.GetStatValue("Hitpoints");
            Assert.False(grip.TryAct(you, f.Zone, new FixedRandom(), true), "held under, then it bites as usual");
            Assert.AreEqual(hp - grip.DrownDamage, you.GetStatValue("Hitpoints"));
        }

        [Test] public void NoBloodNoGrip()
        {
            var warden = Beast("Riverwarden", 12, 10, chance: 0); var you = f.Target(13, 10);
            Assert.False(warden.GetPart<GrapplePart>().TryAct(you, f.Zone, new FixedRandom(), true), "counter-check: at 0% it just fights");
            Assert.False(you.HasEffect<GrappledEffect>());
        }

        [Test] public void AStrongPullTearsYouFreeButOnlyOneTryATick()
        {
            var warden = Beast("Riverwarden", 12, 10); var you = f.Target(13, 10);
            warden.GetPart<GrapplePart>().TryAct(you, f.Zone, new FixedRandom(), true);
            var held = you.GetEffect<GrappledEffect>();
            var old = GrappledEffect.Rng;
            try
            {
                GrappledEffect.Rng = new LowRoll();
                Assert.False(held.AllowMovement(you), "a weak pull holds");
                GrappledEffect.Rng = new FixedRandom();
                Assert.False(held.AllowMovement(you), "and the same tick gives no second try");
                held.LastAttemptTick = -2;
                Assert.True(held.AllowMovement(you), "a natural twenty tears free");
                Assert.True(held.Broken);
                Assert.True(held.AllowMovement(you), "and stays free");
            }
            finally { GrappledEffect.Rng = old; }
        }

        [Test] public void TheGripEndsWhenTheGrapplerIsNoLongerBesideYou()
        {
            var warden = Beast("Riverwarden", 12, 10); var you = f.Target(13, 10);
            warden.GetPart<GrapplePart>().TryAct(you, f.Zone, new FixedRandom(), true);
            var held = you.GetEffect<GrappledEffect>();
            f.Zone.MoveEntity(warden, 5, 5);
            held.OnTurnStart(you);
            Assert.True(held.Broken);
        }

        [Test] public void TheStranglerSqueezesInsteadOfDragging()
        {
            var strangler = Beast("CanopyStrangler", 12, 10); var you = f.Target(13, 10);
            var grip = strangler.GetPart<GrapplePart>();
            Assert.NotNull(grip); Assert.False(grip.Drag); Assert.Greater(grip.SqueezeDamage, 0);
            grip.TryAct(you, f.Zone, new FixedRandom(), true);
            var held = you.GetEffect<GrappledEffect>();
            Assert.NotNull(held);
            int hp = you.GetStatValue("Hitpoints");
            held.OnTurnStart(you);
            Assert.AreEqual(hp - grip.SqueezeDamage, you.GetStatValue("Hitpoints"));
            Assert.False(grip.TryAct(you, f.Zone, new FixedRandom(), true), "held, it strikes as usual");
            Assert.AreEqual(13, f.Zone.GetEntityCell(you).X, "counter-check: a constrictor does not drag");
        }

        [Test] public void AGripSurvivesASave()
        {
            var warden = Beast("Riverwarden", 12, 10); var you = f.Target(13, 10);
            warden.GetPart<GrapplePart>().TryAct(you, f.Zone, new FixedRandom(), true);
            var loaded = PartRoundTripHelper.RoundTripEntity(you).GetEffect<GrappledEffect>();
            Assert.NotNull(loaded);
            Assert.AreEqual(warden.ID, loaded.GrapplerId);
            var strangler = Beast("CanopyStrangler", 15, 15); var other = f.Target(16, 15);
            strangler.GetPart<GrapplePart>().TryAct(other, f.Zone, new FixedRandom(), true);
            Assert.AreEqual(4, PartRoundTripHelper.RoundTripEntity(other).GetEffect<GrappledEffect>().SqueezeDamage);
        }

        // ════════════════ B5: pack hunting and hunting by sound ════════════════

        static void SetTick(int tick)
        {
            var turns = TurnManager.Active ?? new TurnManager();
            typeof(TurnManager).GetProperty("TickCount").SetValue(turns, tick);
            typeof(TurnManager).GetProperty("Active").SetValue(null, turns);
        }

        [Test] public void APaleStalkerHearsWhatMovesAndNotWhatStandsStill()
        {
            var old = TurnManager.Active;
            try
            {
                SetTick(100);
                var stalker = Beast("PaleStalker", 10, 10); var you = f.Target(14, 10);
                f.Wall(12, 9); f.Wall(12, 10); f.Wall(12, 11);
                Assert.True(stalker.HasPart<HuntsBySoundPart>());
                // The fixture's factions leave beasts neutral: make the hunt explicit.
                var brain = stalker.GetPart<BrainPart>(); brain.SetPersonallyHostile(you, alertAllies: false); brain.Target = null;
                Assume.That(FactionManager.IsHostile(stalker, you), "precondition: it wants you");
                Assert.IsNull(AIHelpers.FindNearestHostile(stalker, f.Zone, 10), "you stood still: you are a stone");
                Noise.Mark(you);
                Assert.AreSame(you, AIHelpers.FindNearestHostile(stalker, f.Zone, 10), "you moved: it heard you through the wall");
                SetTick(103);
                Assert.IsNull(AIHelpers.FindNearestHostile(stalker, f.Zone, 10), "and lost you when you stopped");
                f.Zone.MoveEntity(you, 11, 10);
                Assert.IsNull(AIHelpers.FindNearestHostile(stalker, f.Zone, 10), "even beside it, if you hold still");
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null, old); }
        }

        [Test] public void AnEyedHunterStillNeedsToSeeYou()
        {
            var hound = Beast("BrittleHound", 10, 10); var you = f.Target(14, 10);
            f.Wall(12, 9); f.Wall(12, 10); f.Wall(12, 11);
            hound.GetPart<BrainPart>().SetPersonallyHostile(you, alertAllies: false);
            Assume.That(FactionManager.IsHostile(hound, you), "precondition: it wants you");
            Noise.Mark(you);
            Assert.IsNull(AIHelpers.FindNearestHostile(hound, f.Zone, 12), "counter-check: noise does not carry through walls for eyes");
        }

        [Test] public void MovingAndStrikingAreNoise()
        {
            var old = TurnManager.Active;
            try
            {
                SetTick(50);
                var you = f.Target(14, 10); var other = f.Target(15, 10);
                Assert.False(Noise.Recent(you, 1));
                MovementSystem.TryMove(you, f.Zone, 0, 1);
                Assert.True(Noise.Recent(you, 1), "a step");
                SetTick(60);
                CombatSystem.PerformMeleeAttack(you, other, f.Zone, new FixedRandom());
                Assert.True(Noise.Recent(you, 1), "a swing");
                Assert.False(Noise.Recent(other, 1), "counter-check: being struck is not your noise");
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null, old); }
        }

        /// <summary>Each landing path stamps its own noise: TryMove lands through TryMoveTo,
        /// while player input and NPC navigation land through TryMoveDetailed's core, and
        /// shoves and drags through ForceMoveTo (an isolated mutation of the core's stamp
        /// left MovingAndStrikingAreNoise green).</summary>
        [Test] public void EveryWayOfMovingIsNoise()
        {
            var old = TurnManager.Active;
            try
            {
                SetTick(70);
                var walker = f.Target(20, 10);
                Assert.True(MovementSystem.TryMoveDetailed(walker, f.Zone, 0, 1).Moved);
                Assert.True(Noise.Recent(walker, 1), "input and navigation steps are heard");
                SetTick(80);
                var shoved = f.Target(24, 10);
                Assert.True(MovementSystem.ForceMoveTo(shoved, f.Zone, 24, 11));
                Assert.True(Noise.Recent(shoved, 1), "being hauled is heard");
                SetTick(90);
                var still = f.Target(28, 0);
                Assert.False(MovementSystem.TryMoveDetailed(still, f.Zone, 0, -1).Moved, "off the map's edge");
                Assert.False(Noise.Recent(still, 1), "counter-check: a move that does not land is silent");
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null, old); }
        }

        [Test] public void APackMateBesideTheQuarryOpensAFlank()
        {
            var a = Beast("BrittleHound", 13, 10); var b = Beast("BrittleHound", 9, 10); var you = f.Target(12, 10);
            Assert.AreEqual(2, PackHunterPart.FlankHitBonus(b, you, f.Zone), "a mate is beside you");
            Assert.AreEqual(0, PackHunterPart.FlankHitBonus(a, f.Target(20, 20), f.Zone), "counter-check: alone, no flank");
            Assert.True(b.GetPart<PackHunterPart>().TryAct(you, f.Zone, new FixedRandom(), true), "it circles rather than queue");
            Assert.LessOrEqual(SpatialQuery.Distance(f.Zone, b, you), 2);
        }

        [Test] public void AStrangerOfAnotherFactionIsNoPackMate()
        {
            var hound = Beast("BrittleHound", 13, 10); var you = f.Target(12, 10);
            var raider = f.Actor(11, 10, skills: ""); raider.AddPart(new PackHunterPart());
            Assert.AreEqual(0, PackHunterPart.FlankHitBonus(hound, you, f.Zone), "a raider beside you is not the hound's pack");
        }

        // ════════════════ B6: colonies ════════════════

        [Test] public void StrikeOneGeckoAndTheWholeNestRises()
        {
            var you = f.Target(20, 10);
            var nest = new[] { Beast("PrickleBrowGecko", 21, 10), Beast("PrickleBrowGecko", 22, 11), Beast("PrickleBrowGecko", 24, 12) };
            var far = Beast("PrickleBrowGecko", 40, 10);
            foreach (var g in nest) Assert.False(g.GetPart<BrainPart>().IsPersonallyHostileTo(you), "precondition: a quiet nest");
            Assert.False(FactionManager.IsHostile(nest[0], you) && !nest[0].GetPart<BrainPart>().Passive, "walking past wakes nothing");
            CombatSystem.ApplyDamage(nest[0], 1, you, f.Zone);
            foreach (var g in nest) Assert.True(g.GetPart<BrainPart>().IsPersonallyHostileTo(you), "the nest answers");
            Assert.False(far.GetPart<BrainPart>().IsPersonallyHostileTo(you), "counter-check: beyond the call");
            Assert.AreEqual(1, MessageLog.GetRecent(10).Count(l => l.Contains("spines up")), "one call");
        }

        [Test] public void AQuarrelInsideTheColonyRaisesNoAlarm()
        {
            var a = Beast("PrickleBrowGecko", 21, 10); var b = Beast("PrickleBrowGecko", 22, 10);
            Assert.AreEqual(0, a.GetPart<ColonyPart>().Alarm(b));
        }

        [Test] public void TheSimaStewardRoostIsPeacefulUntilStruckAndItsDeathIsTaboo()
        {
            var you = f.Target(20, 10);
            var roost = new[] { Beast("SimaSteward", 22, 10), Beast("SimaSteward", 23, 11) };
            Assert.False(FactionManager.IsHostile(roost[0], you), "seed-bringers leave you be");
            CombatSystem.ApplyDamage(roost[0], 1, you, f.Zone);
            Assert.True(FactionManager.IsHostile(roost[1], you), "strike one and the roost comes for you");
            PlayerReputation.Reset();
            CombatSystem.HandleDeath(roost[0], you, f.Zone);
            Assert.AreEqual(-10, PlayerReputation.Get("CatacombFolk"), "the deep villages hear of it");
            int before = PlayerReputation.Get("CatacombFolk");
            CombatSystem.HandleDeath(roost[1], f.Actor(30, 10, skills: ""), f.Zone);
            Assert.AreEqual(before, PlayerReputation.Get("CatacombFolk"), "counter-check: another's kill is not your taboo");
        }

        // ════════════════ B7: the Bloomed hosts ════════════════

        [Test] public void TheBloomedHostsAreFoundAlreadyWorn()
        {
            foreach (var name in new[] { "BloomedWarden", "BloomedHound" })
            {
                var e = f.Factory.CreateEntity(name);
                Assert.True(e.HasEffect<BloomedEffect>(), name + " arrives worn");
                e.FireEventAndRelease(GameEvent.New("ObjectCreated"));
                Assert.AreEqual(1, e.GetPart<StatusEffectsPart>().GetAllEffects().Count(x => x is BloomedEffect), "worn once");
            }
            Assert.False(f.Factory.CreateEntity("BrittleHound").HasEffect<BloomedEffect>(), "counter-check: an ordinary hound is its own");
        }

        // ════════════════ B8: elders and the roster ════════════════

        static readonly (string name, int tier)[] NewKinds =
        {
            ("Riverwarden", 3), ("SimaSteward", 3), ("BloomedWarden", 4), ("BloomedHound", 4),
            ("OldRiverwarden", 4), ("OldSkySari", 5), ("PaleMatriarch", 5),
        };

        [Test] public void EveryNewKindSitsAtItsTierPaysItsLevelAndCanBeKnownByLooking()
        {
            foreach (var (name, tier) in NewKinds)
            {
                var e = f.Factory.CreateEntity(name);
                Assert.NotNull(e, name);
                Assert.AreEqual(tier, ProgressionCurve.CreatureTier(e), name);
                Assert.AreEqual(ProgressionCurve.XPValueForLevel(e.GetStatValue("Level")), e.GetStatValue("XPValue"), name + " pays its level");
                Assert.Greater(e.GetPart<ExaminablePart>()?.Description?.Length ?? 0, 60, name);
            }
        }

        [Test] public void TheEldersAreTheirKindOlderAndWorse()
        {
            var bird = f.Factory.CreateEntity("OldSkySari").GetPart<StoopPart>();
            Assert.AreEqual(300, bird.DamagePercent); Assert.Greater(bird.Range, f.Factory.CreateEntity("SkySari").GetPart<StoopPart>().Range);
            var river = f.Factory.CreateEntity("OldRiverwarden").GetPart<GrapplePart>();
            Assert.True(river.Drag); Assert.Greater(river.DrownDamage, f.Factory.CreateEntity("Riverwarden").GetPart<GrapplePart>().DrownDamage);
            var mother = f.Factory.CreateEntity("PaleMatriarch");
            Assert.True(mother.HasPart<HuntsBySoundPart>(), "she hears like her line");
            Assert.Greater(mother.GetPart<BrainPart>().SightRadius, f.Factory.CreateEntity("PaleStalker").GetPart<BrainPart>().SightRadius, "farther");
        }

        [Test] public void TheDeepFloorsAndTheStumpCarryTheirNewKinds()
        {
            bool Has(CavesOfOoo.Data.PopulationTable t, string bp) => t.Entries.Any(x => x.BlueprintName == bp);
            var t3 = CavesOfOoo.Data.PopulationTable.UndergroundTier(3, false, 3);
            var t4 = CavesOfOoo.Data.PopulationTable.UndergroundTier(3, false, 4);
            var t5 = CavesOfOoo.Data.PopulationTable.UndergroundTier(3, false, 5);
            Assert.False(Has(t3, "BloomedWarden"), "counter-check: not at tier 3");
            Assert.True(Has(t4, "BloomedWarden") && Has(t4, "BloomedHound"));
            Assert.AreEqual(2, t4.Entries.First(x => x.BlueprintName == "PaleStalker" && x.MinCount >= 2).MinCount, "a pack, not a stray");
            Assert.True(Has(t5, "PaleMatriarch"));
            Assert.True(Has(CavesOfOoo.Data.PopulationTable.StumpSima(), "SimaSteward"));
            Assert.True(Has(CavesOfOoo.Data.PopulationTable.GetStumpTable(StumpBand.Foothills, 3), "Riverwarden"));
            Assert.False(Has(CavesOfOoo.Data.PopulationTable.GetStumpTable(StumpBand.Foothills, 2), "Riverwarden"));
            Assert.True(Has(CavesOfOoo.Data.PopulationTable.GetStumpTable(StumpBand.Slopes, 4), "OldSkySari"), "the high slopes near the Site");
            Assert.False(Has(CavesOfOoo.Data.PopulationTable.GetStumpTable(StumpBand.Foothills, 4), "OldSkySari"), "counter-check: not down by the creeks");
        }

        [Test] public void ARiverwardenLiesOnlyBesideWater()
        {
            var dry = f.Zone.GetCell(30, 15); Assert.False(StumpFaunaHabitat.Allows("Riverwarden", dry));
            Water(31, 15);
            Assert.True(StumpFaunaHabitat.Allows("Riverwarden", dry));
            Assert.True(StumpFaunaHabitat.Allows("SimaSteward", dry), "counter-check: other kinds are not filtered");
        }

        [Test] public void PackAnimalsCallThePack()
        {
            foreach (var name in new[] { "BrittleHound", "PaleStalker" })
                Assert.True(f.Factory.CreateEntity(name).GetPart<CombatTacticsPart>()?.AssistAllies == true, name);
            Assert.IsNull(f.Factory.CreateEntity("StoneGolem").GetPart<CombatTacticsPart>(), "counter-check: a golem fights alone");
        }
    }
}
