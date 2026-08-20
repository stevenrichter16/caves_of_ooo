using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W2.5 adversarial sweep (Docs/FELLING-W1-W2-PLAN.md §7.6 named it;
    /// the W2 close-out review confirmed it missing). Taxonomy surfaces:
    /// cross-actor flows, anti-exploit gates, save/load reach, boundary
    /// inputs, cross-system aggregation (cure tonics, recruitment, AI
    /// target selection all read the same oath state).
    ///
    /// <para><b>The headline bug this file was written to catch</b>
    /// (three independent review lenses raised it): the shipped floor
    /// protected the guest from ALL people, but Break() only fired when
    /// the victim was Tent-Right or a fellow guest — a three-day window
    /// of one-sided combat against anyone else. Canon D1 says the oath
    /// binds BOTH ways: "guest attacks anyone while under the cloth →
    /// effect removed". The tests here state the symmetric contract:
    /// whoever's hostility the cloth floors, swinging at them forfeits
    /// the cloth.</para>
    ///
    /// <para><b>Honesty bound (CLAUDE.md):</b> 0 bugs found by a sweep
    /// never proves the system bug-free; the classes probed are bounded
    /// by what the author imagined. This file began life with six RED
    /// tests — each one a confirmed shipped bug.</para>
    /// </summary>
    public class OathAdversarialTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            SkillRegistry.ResetForTests();
            PlayerReputation.Reset();
            FactionManager.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json")));
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
            PlayerReputation.Reset();
        }

        // ── Fixture helpers (mirror OathTests) ───────────────────

        private static Entity Guest(Zone zone, int x = 10, int y = 10)
        {
            var e = new Entity { ID = "guest", BlueprintName = "Player" };
            e.Tags["Player"] = "";
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 16 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16 };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity Npc(Zone zone, string faction, int x, int y, string name = "npc")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            if (faction != null) e.Tags["Faction"] = faction;
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(1) });
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 12 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 12 };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void Claim(Entity guest)
            => guest.ApplyEffect(new UnderTheClothEffect(), null, null);

        private static void EndTurn(Entity e, Zone zone)
        {
            var ev = GameEvent.New("EndTurn");
            ev.SetParameter("Zone", (object)zone);
            e.FireEvent(ev);
            ev.Release();
        }

        private static int SkillRejections(string reason)
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "skill", Kind = "SkillRejected", Limit = 100 }).Records;
            int n = 0;
            for (int i = 0; i < recs.Count; i++)
                if (recs[i].PayloadJson != null && recs[i].PayloadJson.Contains(reason)) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════
        // ANTI-EXPLOIT — the one-way shield (confirmed shipped bug)
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AttackingAFlooredRaider_IsOathbreak()
        {
            // Hypothesis (confirmed RED pre-fix): the floor stops the
            // raider's hostility, but swinging at the raider was NOT
            // oathbreak — three days of one-sided killing. The symmetric
            // contract: whoever the cloth floors, attacking them
            // forfeits it.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var raider = Npc(zone, "Snapjaws", 11, 10, "raider");
            Claim(guest);
            Assert.IsFalse(FactionManager.IsHostile(raider, guest), "floored — cannot fight back");
            int repBefore = PlayerReputation.Get("TentRight");

            CombatSystem.PerformMeleeAttack(guest, raider, zone, new Random(3));

            Assert.IsFalse(guest.HasEffect<UnderTheClothEffect>(),
                "the cloth binds the guest toward everyone it silences");
            Assert.AreEqual(repBefore + UnderTheClothEffect.OathbreakRepLoss,
                PlayerReputation.Get("TentRight"));
            Assert.IsTrue(FactionManager.IsHostile(raider, guest),
                "the floor lifts with the cloth — the raider answers");
        }

        [Test]
        public void Adversarial_AttackingAnyStrangerPerson_IsOathbreak()
        {
            // D1's own words: "guest attacks anyone while under the
            // cloth → effect removed". A Villager stranger is not
            // Tent-Right and wears no cloth — pre-fix, swinging at them
            // was free.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var stranger = Npc(zone, "Villagers", 11, 10, "stranger");
            Claim(guest);

            CombatSystem.PerformMeleeAttack(guest, stranger, zone, new Random(3));

            Assert.IsFalse(guest.HasEffect<UnderTheClothEffect>(),
                "a person is a person; the oath does not parse factions");
        }

        [Test]
        public void Adversarial_AttackingABeast_IsStillDailyLife()
        {
            // Counter-check for the widened Break: the person test must
            // not swallow the hunt. (OathTests pins this too; this copy
            // guards the NEW code path's carve-out specifically.)
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var scorpion = Npc(zone, "Beasts", 11, 10, "scorpion");
            Claim(guest);

            CombatSystem.PerformMeleeAttack(guest, scorpion, zone, new Random(3));

            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>());
        }

        [Test]
        public void Adversarial_AttackingAFactionlessCritter_IsNotOathbreak()
        {
            // Boundary input: GetFaction returns null for an unfactioned
            // creature. Pre-fix, null != "Beasts" made every wild critter
            // a "person" — swinging at one would have been oathbreak
            // under the widened Break. The person test is "factioned and
            // not Beasts", so the wilds stay daily life.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var critter = Npc(zone, null, 11, 10, "critter");
            Claim(guest);

            CombatSystem.PerformMeleeAttack(guest, critter, zone, new Random(3));

            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>(),
                "no faction, no covenant — a wild thing is a beast in law");
        }

        [Test]
        public void Adversarial_AWoundedWildCritter_IsNotBoundByTheOath()
        {
            // The veto-side mirror (confirmed RED pre-fix): a factionless
            // critter with a personal grudge (you wounded it) was floored
            // by the cloth — a scorpion cannot be an oathbreaker, and a
            // wild dog cannot swear. Its revenge stands.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var critter = Npc(zone, null, 12, 10, "critter");
            critter.GetPart<BrainPart>().SetPersonallyHostile(guest);
            Assert.IsTrue(FactionManager.IsHostile(critter, guest), "wounded and angry");

            Claim(guest);

            Assert.IsTrue(FactionManager.IsHostile(critter, guest),
                "the covenant is between people; the wilds never signed");
        }

        [Test]
        public void Adversarial_BreakWithNullVictim_DoesNotBreak()
        {
            // Boundary input (confirmed RED pre-fix): the victim guard
            // fell through on null and landed the -100 hammer for a
            // swing at nobody.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            Claim(guest);
            int repBefore = PlayerReputation.Get("TentRight");

            Assert.DoesNotThrow(() => UnderTheClothEffect.Break(guest, null));

            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>(),
                "no victim, no crime");
            Assert.AreEqual(repBefore, PlayerReputation.Get("TentRight"));
        }

        // ════════════════════════════════════════════════════════
        // CROSS-SYSTEM — cures and recruitment read the same state
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_APanacea_DoesNotStripTheOath()
        {
            // Cross-system aggregation (confirmed RED pre-fix):
            // CureEffect="All" called RemoveAllEffects, which stripped
            // the oath and printed the calm expiry line mid-oath. The
            // cloth is a covenant, not an ailment; a cure-all removes
            // TYPE_NEGATIVE effects only.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            guest.ApplyEffect(new PoisonedEffect(duration: 5), null, null);
            Claim(guest);

            var tonic = new Entity { ID = "tonic", BlueprintName = "Panacea" };
            tonic.AddPart(new CureTonicPart { CureEffect = "All" });
            var ev = GameEvent.New("ApplyTonic");
            ev.SetParameter("Actor", (object)guest);
            tonic.FireEvent(ev);
            ev.Release();

            Assert.IsFalse(guest.HasEffect<PoisonedEffect>(),
                "the ailment is cured — the tonic still works");
            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>(),
                "the covenant is not a disease");
        }

        [Test]
        public void Adversarial_TheClothDoesNotMakeARaiderRecruitable()
        {
            // Anti-exploit (confirmed RED pre-fix): Persuasion_Recruit's
            // hostility veto read the floored feeling, so a raider under
            // truce could be recruited — and STAYED recruited after the
            // third day. Truce is not friendship: the veto reads the
            // unfloored feeling.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            guest.AddPart(new StatusEffectsPart());
            guest.AddPart(new SkillsPart());
            guest.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            var raider = Npc(zone, "Snapjaws", 11, 10, "raider");
            Claim(guest);
            Assert.IsFalse(FactionManager.IsHostile(raider, guest), "truce holds");

            new Persuasion_Recruit().OnCommand(new SkillEventContext
            { Attacker = guest, Defender = guest, Zone = zone, Rng = new Random(0) });

            Assert.AreEqual(1, SkillRejections("target_hostile"),
                "the raider tolerates you for three days; that is not consent");
            Assert.IsNull(raider.GetEffect<RecruitedEffect>());
        }

        [Test]
        public void Adversarial_RecruitingAGenuineNeutral_IsNotVetoedByTheOathPath()
        {
            // Counter-check: the unfloored reading must not invent
            // hostility where none exists. A neutral Villager while the
            // player wears the cloth sails past veto #7.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            guest.AddPart(new StatusEffectsPart());
            guest.AddPart(new SkillsPart());
            guest.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit(), source: "test");
            Npc(zone, "Villagers", 11, 10, "neighbor");
            Claim(guest);

            new Persuasion_Recruit().OnCommand(new SkillEventContext
            { Attacker = guest, Defender = guest, Zone = zone, Rng = new Random(0) });

            Assert.AreEqual(0, SkillRejections("target_hostile"),
                "neutral before the cloth, neutral under it");
        }

        // ════════════════════════════════════════════════════════
        // THE PLAN'S NAMED SCENARIOS — travel + rock-bottom rep
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TheOathTravels_ClaimHereWalkThere()
        {
            // Plan §7.6 named scenario, pinned as designed: the oath
            // binds the person, not the ground. Claim in one camp, walk
            // to another zone — the cloth is still over you.
            var camp = new Zone("Overworld.5.17.0");
            var guest = Guest(camp);
            Claim(guest);

            camp.RemoveEntity(guest);
            var wilds = new Zone("Overworld.9.9.0");
            wilds.AddEntity(guest, 10, 10);
            var raider = Npc(wilds, "Snapjaws", 12, 10, "raider");

            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>(), "the effect rode along");
            Assert.IsFalse(FactionManager.IsHostile(raider, guest),
                "a raider two zones from the tent still knows the cloth");
        }

        [Test]
        public void Adversarial_RockBottomRep_TheClothStillCovers()
        {
            // Plan §7.6 named scenario: canon says protection is
            // absolute, "regardless of who they are or what they have
            // done". At rep so low the tents themselves would attack,
            // the claim still floors them.
            PlayerReputation.Modify("TentRight", -1000, silent: true);
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var host = Npc(zone, "TentRight", 12, 10, "host");
            Assert.IsTrue(FactionManager.IsHostile(host, guest),
                "rock bottom — even the hosts have had enough");

            Claim(guest);

            Assert.IsFalse(FactionManager.IsHostile(host, guest),
                "absolutely, regardless — that is the whole covenant");
        }

        // ════════════════════════════════════════════════════════
        // AI + persistence + boundaries
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_APursuerMidHunt_LosesTheTarget()
        {
            // Raised by review, refuted, pinned: AI target selection
            // re-validates hostility every evaluation (IsValidHostile
            // Target → IsHostile → the floored feeling), so an in-flight
            // pursuit drops the guest the turn the cloth goes on.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var raider = Npc(zone, "Snapjaws", 13, 10, "raider");

            Assert.AreSame(guest, AIHelpers.FindNearestHostile(raider, zone, 10),
                "mid-hunt before the cloth");
            Claim(guest);
            Assert.IsNull(AIHelpers.FindNearestHostile(raider, zone, 10),
                "the pursuers wait, politely");
        }

        [Test]
        public void Adversarial_ABrokenOath_RoundTripsItsShame()
        {
            // Save/load reach for the Broken flag specifically (OathTests
            // pins ExpiryTick): quit-and-reload between the swing and the
            // removal message must not launder an oathbreak into a calm
            // expiry.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            Claim(guest);
            guest.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>().Broken = true;

            var loaded = PartRoundTripHelper.RoundTripEntity(guest);

            Assert.IsTrue(loaded.GetPart<StatusEffectsPart>()
                .GetEffect<UnderTheClothEffect>().Broken);
        }

        [Test]
        public void Adversarial_OneTickBeforeExpiry_TheClothStillHolds()
        {
            // Boundary counter for OathTests' expiry tests: expiry fires
            // at CurrentTick >= ExpiryTick, so one tick shy of the third
            // day the oath must survive the turn.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            Claim(guest);
            guest.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>()
                .ExpiryTick = WorldClock.CurrentTick + 1;

            EndTurn(guest, zone);

            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>(),
                "the third day has not ended yet");
        }
    }
}
