using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W2.5 (Docs/FELLING-W1-W2-PLAN.md §7.6) — the three-day oath.
    /// Canon: "protected for three days, absolutely, regardless of who
    /// they are or what they have done... Breaking it is the only
    /// unforgivable crime" (Lore/Factions/07_TentRight.md:27), and the
    /// break is "the single worst reputation loss in the game" (:152).
    ///
    /// <para>Every clause has its counter: the cloth floors hostile
    /// PEOPLE (beasts don't swear), it overrides even personal vendetta
    /// (the pursuers wait), it breaks on a swing at a person (not at a
    /// scorpion), and when the third day ends the world resumes.</para>
    /// </summary>
    public class OathTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            // The real faction table — the hardcoded no-arg Initialize
            // doesn't know TentRight.
            FactionManager.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json")));
        }

        [TearDown]
        public void TearDown() => FactionManager.Reset();

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
            e.Tags["Faction"] = faction;
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

        private static int DiagCount(string kind)
            => DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = kind, Limit = 20 }).Records.Count;

        // ════════════════════════════════════════════════════════
        // The claim
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheClaim_IsThreeDaysOnTheWorldClock()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);

            Claim(guest);

            var oath = guest.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>();
            Assert.IsNotNull(oath);
            Assert.AreEqual(WorldClock.CurrentTick + UnderTheClothEffect.OathTicks, oath.ExpiryTick);
            Assert.AreEqual(3 * WorldClock.DayLengthTicks, UnderTheClothEffect.OathTicks,
                "three days means three days");
            Assert.AreEqual(1, DiagCount("OathClaimed"));
        }

        [Test]
        public void Reclaiming_RestartsTheThreeDays()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            Claim(guest);
            var oath = guest.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>();
            oath.ExpiryTick = 5;   // nearly spent

            Claim(guest);          // the host offers again

            Assert.AreEqual(WorldClock.CurrentTick + UnderTheClothEffect.OathTicks, oath.ExpiryTick);
        }

        [Test]
        public void TheHostsConversation_ActuallyClaims()
        {
            // The shipped dialogue's action wire, end to end.
            ConversationLoader.Reset();
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json")));
            var conv = ConversationLoader.Get("TentRightHost_1");
            Assert.IsNotNull(conv, "TentRightHost_1 should load");

            var zone = new Zone("Z");
            var guest = Guest(zone);
            var host = Npc(zone, "TentRight", 11, 10, "host");

            ChoiceData sit = null;
            foreach (var c in conv.GetStartNode().Choices)
                if (c.Target == "Named") sit = c;
            Assert.IsNotNull(sit, "the claim choice exists");
            ConversationActions.ExecuteAll(sit.Actions, host, guest);

            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>());
            ConversationLoader.Reset();
        }

        // ════════════════════════════════════════════════════════
        // The protection — and who it does not bind
        // ════════════════════════════════════════════════════════

        [Test]
        public void HostilePeople_AreFlooredWhileTheClothHolds()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var raider = Npc(zone, "Snapjaws", 12, 10, "raider");

            Assert.IsTrue(FactionManager.IsHostile(raider, guest), "hostile before the cloth");

            Claim(guest);
            Assert.IsFalse(FactionManager.IsHostile(raider, guest),
                "even the raider knows what the cloth means");
            Assert.AreEqual(0, FactionManager.GetFeeling(raider, guest));
        }

        [Test]
        public void Beasts_DoNotSwear()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var scorpion = Npc(zone, "Beasts", 12, 10, "scorpion");
            Claim(guest);

            Assert.IsTrue(FactionManager.IsHostile(scorpion, guest),
                "the covenant is between people; the pan is still the pan");
        }

        [Test]
        public void PersonalEnemies_WaitOutside()
        {
            // Canon: "If your enemy comes to my cloth he will find me
            // between you" — the oath outranks vendetta. Oathbreak
            // removes the effect, which removes this floor.
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var pursuer = Npc(zone, "Villagers", 12, 10, "pursuer");
            pursuer.GetPart<BrainPart>().SetPersonallyHostile(guest);
            Assert.IsTrue(FactionManager.IsHostile(pursuer, guest), "vendetta before the cloth");

            Claim(guest);

            Assert.IsFalse(FactionManager.IsHostile(pursuer, guest), "the pursuers wait, politely");
        }

        // ════════════════════════════════════════════════════════
        // The break
        // ════════════════════════════════════════════════════════

        [Test]
        public void ASwingAtAHost_BreaksTheOath_AndLandsTheHammer()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var host = Npc(zone, "TentRight", 11, 10, "host");
            Claim(guest);
            int repBefore = PlayerReputation.Get("TentRight");

            CombatSystem.PerformMeleeAttack(guest, host, zone, new Random(3));

            Assert.IsFalse(guest.HasEffect<UnderTheClothEffect>(), "the cloth is withdrawn");
            Assert.AreEqual(repBefore + UnderTheClothEffect.OathbreakRepLoss,
                PlayerReputation.Get("TentRight"),
                "the single worst reputation loss in the game");
            Assert.AreEqual(1, DiagCount("OathBroken"));
        }

        [Test]
        public void ASwingAtABeast_IsDailyLife()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var scorpion = Npc(zone, "Beasts", 11, 10, "scorpion");
            Claim(guest);

            CombatSystem.PerformMeleeAttack(guest, scorpion, zone, new Random(3));

            Assert.IsTrue(guest.HasEffect<UnderTheClothEffect>(),
                "hunting is not oathbreak — the cloth holds");
            Assert.AreEqual(0, DiagCount("OathBroken"));
        }

        [Test]
        public void ASpellAtAHost_BreaksItToo()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var host = Npc(zone, "TentRight", 11, 10, "host");
            Claim(guest);

            SpellDamageHelpers.ApplySpellDamage(host, 5, "Fire", guest, zone);

            Assert.IsFalse(guest.HasEffect<UnderTheClothEffect>(),
                "a spell at a person is a swing like any other");
        }

        [Test]
        public void AttackingAFellowGuest_IsAlsoOathbreak()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var other = Npc(zone, "Villagers", 11, 10, "fellow");
            Claim(guest);
            other.ApplyEffect(new UnderTheClothEffect(), null, null);

            CombatSystem.PerformMeleeAttack(guest, other, zone, new Random(3));

            Assert.IsFalse(guest.HasEffect<UnderTheClothEffect>(),
                "\"three days I am your peace\" binds guest to guest");
        }

        [Test]
        public void AfterExpiry_AnAttackIsNotOathbreak()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var host = Npc(zone, "TentRight", 11, 10, "host");
            Claim(guest);
            guest.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>().ExpiryTick = 0;
            EndTurn(guest, zone);   // the third day ends
            Assert.IsFalse(guest.HasEffect<UnderTheClothEffect>());
            int repBefore = PlayerReputation.Get("TentRight");

            CombatSystem.PerformMeleeAttack(guest, host, zone, new Random(3));

            Assert.AreEqual(0, DiagCount("OathBroken"),
                "the world resumed; this is an ordinary crime now");
            Assert.Greater(PlayerReputation.Get("TentRight"),
                repBefore + UnderTheClothEffect.OathbreakRepLoss,
                "no oath hammer — whatever ordinary rep effects apply, not the -100");
        }

        // ════════════════════════════════════════════════════════
        // Expiry + persistence
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheThirdDayEnds_AndTheWorldResumes()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            var raider = Npc(zone, "Snapjaws", 12, 10, "raider");
            Claim(guest);
            Assert.IsFalse(FactionManager.IsHostile(raider, guest));

            guest.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>().ExpiryTick = 0;
            EndTurn(guest, zone);

            Assert.IsFalse(guest.HasEffect<UnderTheClothEffect>());
            Assert.IsTrue(FactionManager.IsHostile(raider, guest), "the protection ends cleanly");
            Assert.AreEqual(1, DiagCount("OathExpired"));
            Assert.AreEqual(0, DiagCount("OathBroken"), "an expiry is not a break");
        }

        [Test]
        public void AMidOathSave_KeepsTheClock()
        {
            var zone = new Zone("Z");
            var guest = Guest(zone);
            Claim(guest);
            var oath = guest.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>();
            oath.ExpiryTick = 1234;

            var loaded = PartRoundTripHelper.RoundTripEntity(guest);

            var loadedOath = loaded.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>();
            Assert.IsNotNull(loadedOath, "the oath survives the save");
            Assert.AreEqual(1234, loadedOath.ExpiryTick, "with its clock intact");
            Assert.IsFalse(loadedOath.Broken);
        }
    }
}
