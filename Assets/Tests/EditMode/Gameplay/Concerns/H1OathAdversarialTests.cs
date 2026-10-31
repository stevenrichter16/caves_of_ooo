using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// H1 adversarial sweep (ADVERSARIAL_TESTING.md): self-reference, null boundaries,
    /// acceptance boundaries, two-host conflicts, atomic refusals, oathbreak idempotence,
    /// save reach, the beast exemption, conversation integrity and claim eligibility.
    /// </summary>
    public sealed class H1OathAdversarialTests
    {
        EntityFactory factory;
        Entity player, host;
        Zone zone;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            MessageLog.Clear(); Diag.ResetAll(); ConversationActions.Factory = factory; PlayerReputation.Reset();
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized();
            NarrativeStatePart.Current = new NarrativeStatePart();
            ConversationLoader.Reset();
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json")));
            player = factory.CreateEntity("Player");
            zone = new Zone("Overworld.8.16.0");
            zone.AddEntity(player, 10, 10);
            host = Put("TentRightHost", 30, 20); // not adjacent: an unselected command falls back to the first neighbour
        }
        [TearDown] public void Cleanup()
        {
            ConversationLoader.Reset(); NarrativeStatePart.Current = null; ConversationActions.Factory = null;
            SkillRegistry.ResetForTests();
        }

        Entity Put(string blueprint, int x, int y) { var e = factory.CreateEntity(blueprint); Assert.True(zone.AddEntity(e, x, y), blueprint); return e; }
        void Sworn() { H1OathTests.Like("TentRight"); Assert.IsNull(TentRightOath.Swear(player, host)); }
        static void Wound(Entity e, int percent) { var hp = e.GetStat("Hitpoints"); hp.BaseValue = hp.Max * percent / 100; }
        static IEnumerable<string> Rejections()
            => DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 20 }).Records.Select(r => r.PayloadJson ?? "");
        bool Extend(Entity target)
        {
            var skill = player.GetPart<SkillsPart>().GetSkill(TentRightOath.PowerClass);
            return skill.OnCommand(new SkillEventContext { Attacker = player, Zone = zone, TargetCell = target == null ? null : zone.GetEntityCell(target) });
        }

        // ════════════════ self-reference and null boundaries ════════════════

        [Test] public void Adversarial_TheHostCannotBeTheirOwnGuest()
        {
            Sworn();
            Assert.AreEqual("no_target", Hospitality_ExtendCloth.RefusalFor(player, player));
            Assert.AreEqual("no_target", Hospitality_ExtendCloth.RefusalFor(player, null));
        }

        [Test] public void Adversarial_NoNeighbourMeansNoGuest()
        {
            Sworn();
            Assert.False(Extend(null), "an empty cell holds nobody to name");
            Assert.True(Rejections().Any(p => p.Contains("no_target")));
        }

        [Test] public void Adversarial_NullsAreHarmlessAtEverySeam()
        {
            Assert.False(UnderTheClothEffect.Stands(null, player));
            Assert.False(UnderTheClothEffect.Stands(player, null));
            Assert.False(UnderTheClothEffect.KeepsPeace(null, null));
            Assert.AreEqual(0, UnderTheClothEffect.Withdraw(null, zone));
            Assert.AreEqual(0, UnderTheClothEffect.Withdraw(player.ID, null));
            Assert.DoesNotThrow(() => UnderTheClothEffect.Break(null, player));
            Assert.DoesNotThrow(() => UnderTheClothEffect.Break(player, null));
            Assert.DoesNotThrow(() => TentRightOath.Oathbreak(null, "host"));
            Assert.AreEqual("missing_listener", TentRightOath.Swear(null, host));
        }

        [Test] public void Adversarial_StrikingYourselfBreaksNothing()
        {
            Sworn();
            player.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            UnderTheClothEffect.Break(player, player);
            Assert.False(TentRightOath.IsOathbreaker());
        }

        // ════════════════ acceptance boundaries ════════════════

        [Test] public void Adversarial_HalfHealthIsNotYetBeaten_JustBelowIs()
        {
            Sworn();
            var raider = Put("DesertBandit", 11, 10);
            var hp = raider.GetStat("Hitpoints");
            hp.BaseValue = (hp.Max * Hospitality_ExtendCloth.BeatenPercent + 99) / 100;
            Assert.AreEqual("not_yet_beaten", Hospitality_ExtendCloth.RefusalFor(player, raider), "at half, they still think they can win");
            hp.BaseValue -= 1;
            Assert.IsNull(Hospitality_ExtendCloth.RefusalFor(player, raider));
        }

        [Test] public void Adversarial_AFriendAcceptsAtFullHealth()
        {
            Sworn();
            var villager = Put("Villager", 11, 10);
            Assert.IsNull(Hospitality_ExtendCloth.RefusalFor(player, villager), "the beaten rule binds enemies only");
        }

        [Test] public void Adversarial_BeastsAndTheFactionlessAreNotPersons()
        {
            Sworn();
            var beast = factory.CreateEntity("Villager");
            beast.SetTag("Faction", "Beasts");
            zone.AddEntity(beast, 11, 10);
            Assert.AreEqual("not_a_person", Hospitality_ExtendCloth.RefusalFor(player, beast));
            var wild = factory.CreateEntity("Villager");
            wild.Tags.Remove("Faction");
            zone.AddEntity(wild, 11, 11);
            Assert.AreEqual("not_a_person", Hospitality_ExtendCloth.RefusalFor(player, wild));
        }

        [Test] public void Adversarial_AnotherHostsGuestCannotBeTaken_YourOwnIsRenewed()
        {
            Sworn();
            var theirs = Put("Villager", 11, 10);
            theirs.ApplyEffect(new UnderTheClothEffect(host), host, null);
            Assert.False(Extend(theirs));
            Assert.True(Rejections().Any(p => p.Contains("another_host")));
            Assert.AreEqual(host.ID, theirs.GetEffect<UnderTheClothEffect>().HostId, "the first host keeps the guest");

            var mine = Put("Villager", 11, 11);
            Assert.True(Extend(mine));
            var cloth = mine.GetEffect<UnderTheClothEffect>();
            cloth.ExpiryTick = WorldClock.CurrentTick + 5;
            Assert.IsNull(Hospitality_ExtendCloth.RefusalFor(player, mine), "a host may renew their own cloth");
            mine.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            Assert.Greater(mine.GetEffect<UnderTheClothEffect>().ExpiryTick, WorldClock.CurrentTick + 5, "three days from now");
        }

        // ════════════════ atomic refusals ════════════════

        [Test] public void Adversarial_ARefusedOathChangesNothing()
        {
            H1OathTests.Like("TentRight");
            player.RemovePart(player.GetPart<SkillsPart>());
            int rep = PlayerReputation.Get("TentRight");
            Assert.AreEqual("missing_skills", TentRightOath.Swear(player, host));
            Assert.AreEqual(rep, PlayerReputation.Get("TentRight"));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(TentRightOath.SwornFact));
        }

        [Test] public void Adversarial_SwearingTwicePaysOnce()
        {
            Sworn();
            int rep = PlayerReputation.Get("TentRight");
            Assert.AreEqual("already_sworn", TentRightOath.Swear(player, host));
            Assert.AreEqual(rep, PlayerReputation.Get("TentRight"));
        }

        [Test] public void Adversarial_ADiscoveryCannotBeBoughtEvenWithPoints()
        {
            H1OathTests.Like("TentRight");
            player.GetStat("SP").BaseValue = 50;
            Assert.False(BuySkillAction.Execute(player, TentRightOath.PowerClass).Succeeded);
            Assert.AreEqual(50, player.GetStat("SP").BaseValue, "no points are spent on a refusal");
        }

        // ════════════════ oathbreak ════════════════

        [Test] public void Adversarial_StrikingAFormerGuestAfterTheBreakCostsNothingMore()
        {
            Sworn();
            var guest = Put("Villager", 11, 10);
            Assert.True(Extend(guest));
            CombatSystem.PerformMeleeAttack(player, guest, zone, new System.Random(3));
            int rep = PlayerReputation.Get("TentRight");
            CombatSystem.PerformMeleeAttack(player, guest, zone, new System.Random(4));
            Assert.AreEqual(rep, PlayerReputation.Get("TentRight"), "the cloth is already off");
        }

        [Test] public void Adversarial_ASwornPlayerWhoBreaksAsGuestLosesTheirOwnOathToo()
        {
            Sworn();
            player.ApplyEffect(new UnderTheClothEffect(host), host, null);
            var stranger = Put("Villager", 11, 10);
            CombatSystem.PerformMeleeAttack(player, stranger, zone, new System.Random(3));
            Assert.True(TentRightOath.IsOathbreaker(), "guest or host, it is one oath");
            Assert.False(player.GetPart<SkillsPart>().HasSkill(TentRightOath.PowerClass));
        }

        [Test] public void Adversarial_AnNpcGuestWhoStrikesAPersonBreaksOnlyTheirOwnCloth()
        {
            Sworn();
            var guest = Put("Villager", 11, 10);
            var victim = Put("Villager", 12, 10);
            Assert.True(Extend(guest));
            int rep = PlayerReputation.Get("TentRight");
            CombatSystem.PerformMeleeAttack(guest, victim, zone, new System.Random(3));
            Assert.False(guest.HasEffect<UnderTheClothEffect>());
            Assert.AreEqual(rep, PlayerReputation.Get("TentRight"), "the host is not blamed for the guest");
            Assert.True(TentRightOath.IsSworn());
        }

        [Test] public void Adversarial_ANpcHostWhoStrikesTheirGuestLoosesTheirOtherGuests()
        {
            var a = Put("Villager", 11, 10);
            var b = Put("Villager", 12, 11);
            a.ApplyEffect(new UnderTheClothEffect(host), host, null);
            b.ApplyEffect(new UnderTheClothEffect(host), host, null);
            CombatSystem.PerformMeleeAttack(host, a, zone, new System.Random(3));
            Assert.False(b.HasEffect<UnderTheClothEffect>());
            Assert.AreEqual(0, PlayerReputation.Get("TentRight"), "an NPC's oathbreak does not touch the player");
        }

        // ════════════════ the floor's edges ════════════════

        [Test] public void Adversarial_BeastsStillHuntAGuest()
        {
            Sworn();
            var guest = Put("Villager", 11, 10);
            var beast = factory.CreateEntity("DesertBandit"); beast.SetTag("Faction", "Beasts"); zone.AddEntity(beast, 13, 10);
            var brain = beast.GetPart<BrainPart>(); brain.CurrentZone = zone;
            var hunt = new KillGoal(guest); brain.PushGoal(hunt);
            Assert.True(Extend(guest));
            Assert.False(hunt.Finished(), "a scorpion cannot be an oathbreaker, nor bound by the cloth");
        }

        [Test] public void Adversarial_PursuersStillHateTheGuestUnderTheFloor()
        {
            Sworn();
            var guest = Put("Villager", 11, 10);
            var pursuer = Put("DesertBandit", 14, 10);
            pursuer.GetPart<BrainPart>().PersonalEnemies.Add(guest);
            Assert.True(Extend(guest));
            Assert.False(FactionManager.IsHostile(pursuer, guest));
            Assert.LessOrEqual(FactionManager.GetFeelingUnfloored(pursuer, guest), FactionManager.HOSTILE_THRESHOLD);
        }

        [Test] public void Adversarial_WhenTheClothEndsTheHuntResumes()
        {
            Sworn();
            var guest = Put("Villager", 11, 10);
            var pursuer = Put("DesertBandit", 14, 10);
            Assert.True(Extend(guest));
            guest.RemoveEffect(typeof(UnderTheClothEffect));
            var brain = pursuer.GetPart<BrainPart>(); brain.CurrentZone = zone;
            var hunt = new KillGoal(guest); brain.PushGoal(hunt);
            Assert.False(hunt.Finished(), "the third day ends; the world resumes");
            Assert.True(FactionManager.IsHostile(pursuer, guest));
        }

        [Test] public void Adversarial_ThePlayerAsGuestIsNotFlooredTowardOthers()
        {
            player.ApplyEffect(new UnderTheClothEffect(host), host, null);
            var raider = Put("DesertBandit", 11, 10);
            Assert.False(UnderTheClothEffect.KeepsPeace(player, raider), "the player is bound by Break, not by a floor");
        }

        // ════════════════ save reach ════════════════

        [Test] public void Adversarial_TheHostAndTheClaimSurviveASave()
        {
            Sworn();
            var guest = Put("Villager", 11, 10);
            guest.AddPart(new ClothClaimantPart { Pursuers = "a,b", Honored = true });
            guest.ApplyEffect(new UnderTheClothEffect(player, sworn: true) { Withdrawn = true }, player, null);
            var loaded = PartRoundTripHelper.RoundTripEntity(guest);
            var cloth = loaded.GetEffect<UnderTheClothEffect>();
            Assert.NotNull(cloth);
            Assert.AreEqual(player.ID, cloth.HostId);
            Assert.True(cloth.HostSworn);
            Assert.True(cloth.Withdrawn);
            var claim = loaded.GetPart<ClothClaimantPart>();
            Assert.AreEqual("a,b", claim.Pursuers);
            Assert.True(claim.Honored);
        }

        // ════════════════ diag ════════════════

        [Test] public void Adversarial_EveryOutcomeIsRecorded()
        {
            Assert.AreEqual("not_liked", TentRightOath.Swear(player, host));
            Sworn();
            var guest = Put("Villager", 11, 10);
            Assert.True(Extend(guest));
            CombatSystem.PerformMeleeAttack(player, guest, zone, new System.Random(3));
            foreach (var (category, kind) in new[] { ("effect", "OathRefused"), ("effect", "OathSworn"), ("skill", "ClothExtended"),
                         ("effect", "HostStruckGuest"), ("effect", "Oathbreak") })
                Assert.Greater(DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 5 }).Records.Count, 0, kind);
        }

        // ════════════════ conversation integrity ════════════════

        [Test] public void Adversarial_EveryHostAndClaimantChoiceLeadsSomewhere()
        {
            foreach (var id in new[] { "TentRightHost_1", "ClothClaimant_1" })
            {
                var conv = ConversationLoader.Get(id);
                Assert.NotNull(conv, id);
                var nodes = new HashSet<string>(conv.Nodes.Select(n => n.ID)) { "End" };
                foreach (var n in conv.Nodes) foreach (var c in n.Choices)
                {
                    Assert.True(nodes.Contains(c.Target), id + "/" + n.ID + " -> " + c.Target);
                    foreach (var p in c.Predicates ?? new List<ConversationParam>()) Assert.True(ConversationPredicates.IsRegistered(p.Key), p.Key);
                    foreach (var a in c.Actions ?? new List<ConversationParam>()) Assert.True(ConversationActions.IsRegistered(a.Key), a.Key);
                }
            }
        }

        [Test] public void Adversarial_WithoutNarrativeStateGuestRightIsStillOffered()
        {
            NarrativeStatePart.Current = null;
            var start = ConversationLoader.Get("TentRightHost_1").Nodes.First(n => n.ID == "Start");
            var sit = start.Choices.First(c => c.Text.StartsWith("I sit"));
            Assert.True(ConversationPredicates.CheckAll(sit.Predicates, host, player), "no state, no broken cloth: the oathbreaker gate must not fail closed");
            var name = start.Choices.First(c => c.Text == "Name me guest.");
            Assert.False(ConversationPredicates.CheckAll(name.Predicates, host, player));
        }

        // ════════════════ the wells ════════════════

        bool Drink(Zone where)
        {
            var well = factory.CreateEntity("Well"); where.AddEntity(well, 11, 11);
            MessageLog.Clear();
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "DrawWaterAtWell"); e.SetParameter("Actor", (object)player); e.SetParameter("Zone", (object)where);
            well.FireEvent(e); e.Release();
            return string.Join("\n", MessageLog.GetRecent(5)).Contains("drink");
        }

        [Test] public void Adversarial_ATentRightWellRefusesAnOathbreakerAndOnlyThem()
        {
            Assert.True(Drink(zone), "counter-check: a player in good standing drinks at Wellmeet");
            NarrativeStatePart.Current.SetFact(TentRightOath.OathbreakerFact, 1);
            Assert.False(Drink(zone), "exile: no water at a tent");
            StringAssert.Contains("Not for an oathbreaker", string.Join("\n", MessageLog.GetRecent(5)));
            var sill = new Zone("Overworld.10.10.0");
            zone.RemoveEntity(player); sill.AddEntity(player, 10, 10);
            Assert.True(Drink(sill), "a village that is not Tent-Right still gives water");
        }

        // ════════════════ the claim's edges ════════════════

        [Test] public void Adversarial_ADeadOrForeignClaimantHonoursNothing()
        {
            Sworn();
            var dead = Put("Villager", 11, 10);
            var part = new ClothClaimantPart { Pursuers = "none" }; dead.AddPart(part);
            dead.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            dead.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(part.TryHonor());
            var foreign = Put("Villager", 12, 11);
            var other = new ClothClaimantPart { Pursuers = "none" }; foreign.AddPart(other);
            foreign.ApplyEffect(new UnderTheClothEffect(host), host, null);
            Assert.False(other.TryHonor(), "only a sworn host's defence is honoured");
        }
    }
}
