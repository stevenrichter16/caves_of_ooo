using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// H1 (Docs/PLACE-WORK-AND-PROGRESSION.md §H1): the Tent-Right oath, sworn.
    /// The player swears at a host through the real conversation, extends the cloth
    /// with the real skill, is bound by the host's obligation in real combat, and is
    /// claimed on open Beating ground by a hunted wayfarer.
    /// </summary>
    public sealed class H1OathTests
    {
        internal static EntityFactory factory;
        Entity player;
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
        }
        [TearDown] public void Cleanup()
        {
            ConversationLoader.Reset(); NarrativeStatePart.Current = null; ConversationActions.Factory = null;
            SkillRegistry.ResetForTests();
        }

        internal static void Like(string faction) => PlayerReputation.Modify(faction, 60, silent: true);
        Entity Put(string blueprint, int x, int y) { var e = factory.CreateEntity(blueprint); Assert.True(zone.AddEntity(e, x, y), blueprint); return e; }
        static NodeData Node(string id) => ConversationLoader.Get("TentRightHost_1").Nodes.First(n => n.ID == id);
        List<ChoiceData> Visible(Entity host, string node)
            => Node(node).Choices.Where(c => ConversationPredicates.CheckAll(c.Predicates, host, player)).ToList();
        bool Sees(Entity host, string node, string text) => Visible(host, node).Any(c => c.Text.Contains(text));
        void Swear(Entity host)
        {
            var choice = Node("Oath").Choices.First(c => c.Text == "I swear it.");
            Assert.True(ConversationActions.TryExecuteAll(choice.Actions, host, player), "the oath is sworn");
        }
        bool Extend(Entity target)
        {
            var skill = player.GetPart<SkillsPart>().GetSkill(TentRightOath.PowerClass);
            Assert.NotNull(skill, "precondition: the sworn host knows Extend the Cloth");
            return skill.OnCommand(new SkillEventContext { Attacker = player, Zone = zone, TargetCell = zone.GetEntityCell(target) });
        }
        static void Wound(Entity e, int percent)
        {
            var hp = e.GetStat("Hitpoints");
            hp.BaseValue = hp.Max * percent / 100;
        }
        static bool LastRejected(string reason)
            => DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 20 }).Records
                .Any(r => r.PayloadJson != null && r.PayloadJson.Contains(reason));

        // ════════════════ swearing ════════════════

        [Test] public void ATrustedTravellerIsOfferedTheOathAndSwearsIt()
        {
            var host = Put("TentRightHost", 11, 10);
            Like("TentRight");
            Assert.True(Sees(host, "Start", "swear the oath"), "Liked by Tent-Right: the oath is offered");
            int before = PlayerReputation.Get("TentRight");
            Swear(host);
            Assert.True(TentRightOath.IsSworn());
            Assert.True(player.GetPart<SkillsPart>().HasSkill(TentRightOath.PowerClass));
            Assert.AreEqual(before + TentRightOath.SwearingRepGain, PlayerReputation.Get("TentRight"));
            Assert.False(Sees(host, "Start", "swear the oath"), "sworn once; the offer is gone");
            Assert.True(Sees(host, "Start", "Tell me how it is held"), "the sworn host may ask counsel");
        }

        [Test] public void AStrangerToTheTentsIsNotOfferedTheOath()
        {
            var host = Put("TentRightHost", 11, 10);
            Assert.False(Sees(host, "Start", "swear the oath"), "counter-check: neutral standing hides the offer");
            Assert.AreEqual("not_liked", TentRightOath.Swear(player, host));
            Assert.False(TentRightOath.IsSworn());
            Assert.False(player.GetPart<SkillsPart>().HasSkill(TentRightOath.PowerClass), "a refused oath teaches nothing");
        }

        [Test] public void TheClothCannotBeBought()
        {
            Like("TentRight");
            var result = BuySkillAction.Execute(player, TentRightOath.PowerClass);
            Assert.False(result.Succeeded, "Extend the Cloth is never for sale");
            Assert.False(BuySkillAction.Execute(player, TentRightOath.TreeClass).Succeeded, "nor the tree");
            Assert.True(SkillRegistry.TryGetPowerByClass(TentRightOath.PowerClass, out var power));
            Assert.Less(power.Cost, 0, "listed as a discovery");
        }

        [Test] public void TheDisciplineStaysOutOfTheBrowserUntilSworn()
        {
            var browser = new CavesOfOoo.Rendering.SkillBrowserModel();
            browser.Open(player);
            Assert.False(browser.Disciplines.Any(d => d.Class == TentRightOath.TreeClass), "an unsworn player is not shown the oath's power");
            var host = Put("TentRightHost", 11, 10); Like("TentRight"); Swear(host);
            browser.Open(player);
            Assert.True(browser.Disciplines.Any(d => d.Class == TentRightOath.TreeClass && d.Rows.Any(r => r.Class == TentRightOath.PowerClass)),
                "the sworn host sees what they carry");
        }

        // ════════════════ extending the cloth ════════════════

        [Test] public void AFriendTakesTheClothAndThePersonsWhoWouldHarmThemStandOff()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var friend = Put("Villager", 11, 10);
            var raider = Put("DesertBandit", 15, 10);
            Assume.That(FactionManager.IsHostile(raider, friend), "precondition: raiders would harm the villager");
            Assert.True(Extend(friend));
            var cloth = friend.GetEffect<UnderTheClothEffect>();
            Assert.NotNull(cloth);
            Assert.AreEqual(player.ID, cloth.HostId);
            Assert.True(cloth.HostSworn);
            Assert.False(FactionManager.IsHostile(raider, friend), "no person may harm the guest");
        }

        [Test] public void TheUnsworn_CannotExtendIt()
        {
            var friend = Put("Villager", 11, 10);
            var skill = new Hospitality_ExtendCloth();
            player.GetPart<SkillsPart>().AddSkill(skill);
            Assert.False(skill.OnCommand(new SkillEventContext { Attacker = player, Zone = zone, TargetCell = zone.GetEntityCell(friend) }));
            Assert.True(LastRejected("not_sworn"), "counter-check: the power without the oath is empty");
            Assert.False(friend.HasEffect<UnderTheClothEffect>());
        }

        [Test] public void AnEnemyTakesTheHemOnlyWhenBeaten()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var raider = Put("DesertBandit", 11, 10);
            Assume.That(FactionManager.IsHostile(raider, player), "precondition: the raider wants the host dead");
            Assert.False(Extend(raider), "at full strength they think they can win");
            Assert.True(LastRejected("not_yet_beaten"));
            Wound(raider, 40);
            Assert.True(Extend(raider), "beaten, they reach for the hem");
            Assert.False(FactionManager.IsHostile(raider, player), "the guest keeps the peace");
            Assert.LessOrEqual(FactionManager.GetFeelingUnfloored(raider, player), FactionManager.HOSTILE_THRESHOLD,
                "the truce launders nothing: they still hate you");
        }

        [Test] public void AGuestPutsItsFightDownAndItsHuntersWait()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var raider = Put("DesertBandit", 11, 10);
            var hunter = Put("DesertBandit", 14, 10);
            var quarry = Put("Villager", 12, 11);
            var raiderBrain = raider.GetPart<BrainPart>(); raiderBrain.CurrentZone = zone;
            var hunterBrain = hunter.GetPart<BrainPart>(); hunterBrain.CurrentZone = zone;
            var fight = new KillGoal(player); raiderBrain.PushGoal(fight); raiderBrain.Target = player;
            var hunt = new KillGoal(quarry); hunterBrain.PushGoal(hunt); hunterBrain.Target = quarry;
            Assert.False(fight.Finished()); Assert.False(hunt.Finished());
            Wound(raider, 30);
            Assert.True(Extend(raider));
            Assert.True(fight.Finished(), "the guest stops fighting the host");
            Assert.IsNull(raiderBrain.Target);
            Assert.False(hunt.Finished(), "counter-check: an unrelated hunt goes on");
        }

        [Test] public void AHuntedVillagerUnderTheClothIsLetBe()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var hunter = Put("DesertBandit", 14, 10);
            var quarry = Put("Villager", 11, 10);
            var brain = hunter.GetPart<BrainPart>(); brain.CurrentZone = zone;
            var hunt = new KillGoal(quarry); brain.PushGoal(hunt); brain.Target = quarry;
            Assert.True(Extend(quarry));
            Assert.True(hunt.Finished(), "the pursuers wait outside the cloth");
            Assert.IsNull(brain.Target);
        }

        // ════════════════ the host's obligation ════════════════

        [Test] public void AHostWhoStrikesTheirGuestIsAnOathbreaker()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var guest = Put("Villager", 11, 10);
            var other = Put("Villager", 20, 15);
            Assert.True(Extend(guest));
            other.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            int before = PlayerReputation.Get("TentRight");
            CombatSystem.PerformMeleeAttack(player, guest, zone, new System.Random(3));
            Assert.True(TentRightOath.IsOathbreaker());
            Assert.False(TentRightOath.IsSworn());
            Assert.AreEqual(before + UnderTheClothEffect.OathbreakRepLoss, PlayerReputation.Get("TentRight"));
            Assert.False(player.GetPart<SkillsPart>().HasSkill(TentRightOath.PowerClass), "the oath is withdrawn");
            Assert.False(guest.HasEffect<UnderTheClothEffect>());
            Assert.False(other.HasEffect<UnderTheClothEffect>(), "every guest of this host is set loose");
            StringAssert.Contains("withdrawn by its host", string.Join("\n", MessageLog.GetRecent(20)));
        }

        [Test] public void StrikingSomeoneElsesGuestOrAStrangerIsNoBreakOfYourOwnOath()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var stranger = Put("Villager", 11, 10);
            CombatSystem.PerformMeleeAttack(player, stranger, zone, new System.Random(3));
            Assert.False(TentRightOath.IsOathbreaker(), "counter-check: no cloth between you, no oath to break");
            Assert.True(TentRightOath.IsSworn());
        }

        [Test] public void AnOathbreakerIsNeverNamedGuestOrSwornAgain()
        {
            var host = Put("TentRightHost", 11, 10); Like("TentRight");
            NarrativeStatePart.Current.SetFact(TentRightOath.OathbreakerFact, 1);
            Assert.False(Sees(host, "Start", "I sit, and I am your guest"));
            Assert.True(Sees(host, "Start", "Name me guest"), "the host gives water and says why");
            Assert.False(Sees(host, "Start", "swear the oath"));
            ConversationActions.Execute("ClaimGuestRight", host, player, "");
            Assert.False(player.HasEffect<UnderTheClothEffect>(), "the action refuses as well as the dialogue");
            Assert.AreEqual("oathbreaker", TentRightOath.Swear(player, host));
        }

        [Test] public void AGuestRightClaimedAtATentRecordsItsHost()
        {
            var host = Put("TentRightHost", 11, 10);
            ConversationActions.Execute("ClaimGuestRight", host, player, "");
            var cloth = player.GetEffect<UnderTheClothEffect>();
            Assert.NotNull(cloth);
            Assert.AreEqual(host.ID, cloth.HostId);
            Assert.False(cloth.HostSworn, "a Tent-Right host's cloth, not the player's");
        }

        [Test] public void AGuestElsewhereLosesTheClothOnItsNextTurn()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var away = new Zone("Overworld.8.17.0");
            var guest = factory.CreateEntity("Villager"); away.AddEntity(guest, 5, 5);
            guest.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            TentRightOath.Oathbreak(player, "host");
            var cloth = guest.GetEffect<UnderTheClothEffect>();
            Assert.NotNull(cloth, "precondition: the guest was in another zone");
            cloth.OnTurnEnd(guest, null);
            Assert.AreEqual(0, cloth.Duration);
            Assert.True(cloth.Withdrawn);
        }

        // ════════════════ the claim ════════════════

        [Test] public void DefendingAClaimantIsHonouredOnceAndOnlyWhenThePursuitIsOver()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var guest = Put("Villager", 11, 10);
            var p0 = Put("DesertBandit", 16, 10); p0.ID = "p0";
            var p1 = Put("DesertBandit", 16, 12); p1.ID = "p1";
            var part = new ClothClaimantPart { Pursuers = "p0,p1" }; guest.AddPart(part);
            guest.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            int drams = TradeSystem.GetDrams(player), rep = PlayerReputation.Get("TentRight");
            p0.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(part.TryHonor(), "one pursuer still stands");
            p1.GetStat("Hitpoints").BaseValue = 0;
            Assert.True(part.TryHonor());
            Assert.AreEqual(drams + ClothClaimantPart.Gift, TradeSystem.GetDrams(player));
            Assert.AreEqual(rep + ClothClaimantPart.HonorRep, PlayerReputation.Get("TentRight"));
            Assert.False(part.TryHonor(), "honoured once");
            Assert.AreEqual(drams + ClothClaimantPart.Gift, TradeSystem.GetDrams(player));
        }

        [Test] public void AClaimantWhoseClothWasWithdrawnHonoursNothing()
        {
            var host = Put("TentRightHost", 9, 10); Like("TentRight"); Swear(host);
            var guest = Put("Villager", 11, 10);
            var part = new ClothClaimantPart { Pursuers = "gone" }; guest.AddPart(part);
            guest.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            TentRightOath.Oathbreak(player, "host");
            Assert.False(part.TryHonor(), "counter-check: an oathbreaker is not thanked");
        }

        [Test] public void TheZoneEntryHookRunsBesideTheTravellers()
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Presentation/Input/InputHandler.cs"));
            int travellers = text.IndexOf("WorldTravellers.OnZoneEntered(PlayerEntity, result.NewZone)", System.StringComparison.Ordinal);
            int claims = text.IndexOf("ClothClaims.OnZoneEntered(PlayerEntity, result.NewZone)", System.StringComparison.Ordinal);
            int register = text.IndexOf("var newCreatures =", travellers, System.StringComparison.Ordinal);
            Assert.Greater(claims, travellers); Assert.Less(claims, register, "placed before creatures register for turns");
        }
    }

    /// <summary>H1.4 end to end: a sworn host entering open Beating ground is claimed.</summary>
    public sealed class H1ClothClaimTests
    {
        DensityLootTestScope scope; OverworldZoneManager manager; Entity player; Zone zone;

        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope(); FactionManager.Initialize(); PlayerReputation.Reset(); Diag.ResetAll();
            SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized();
            NarrativeStatePart.Current = new NarrativeStatePart();
            manager = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            player = scope.Factory.CreateEntity("Player");
        }
        [TearDown] public void Cleanup() { NarrativeStatePart.Current = null; SkillRegistry.ResetForTests(); scope.Dispose(); }

        void Attach(Zone z)
        {
            if (zone?.GetEntityCell(player) != null) zone.RemoveEntity(player);
            zone = z; Assert.True(zone.AddEntity(player, 40, 12));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
        }
        void SwearPlayer()
        {
            H1OathTests.Like("TentRight");
            Assert.IsNull(TentRightOath.Swear(player, null));
        }
        IEnumerable<(int x, int y)> OpenBeating()
        {
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
                if (WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Beating && WorldMapAuthoring.TierAt(x, y) <= 3
                    && manager.WorldMap.GetPOI(x, y) == null) yield return (x, y);
        }

        [Test] public void ASwornHostOnOpenBeatingGroundIsClaimedAndCannotRefuse()
        {
            SwearPlayer();
            Entity guest = null;
            foreach (var (x, y) in OpenBeating())
            {
                Attach(new Zone(WorldMap.ToZoneID(x, y, 0)));
                if (ClothClaims.OnZoneEntered(player, zone)) { guest = zone.GetReadOnlyEntities().First(e => e.HasPart<ClothClaimantPart>()); break; }
            }
            Assert.NotNull(guest, "at least one open Beating zone rolls a claim");
            var cloth = guest.GetEffect<UnderTheClothEffect>();
            Assert.NotNull(cloth, "the hem is taken at once");
            Assert.AreEqual(player.ID, cloth.HostId);
            Assert.AreEqual("ClothClaimant_1", guest.GetPart<ConversationPart>().ConversationID);
            var pursuers = zone.GetReadOnlyEntities().Where(e => e.ID != null && e.ID.StartsWith(guest.ID + ":p")).ToList();
            Assert.GreaterOrEqual(pursuers.Count, 2);
            Assert.True(pursuers.All(p => p.GetPart<BrainPart>().IsPersonallyHostileTo(guest)), "they are hunting the guest");
            Assert.True(pursuers.All(p => !FactionManager.IsHostile(p, guest)), "and must wait outside the cloth");
            Assert.AreEqual(1, player.GetIntProperty("ClothClaimCount"));
            Assert.False(ClothClaims.OnZoneEntered(player, zone), "the roll for this zone is spent");
        }

        [Test] public void NobodyClaimsTheUnsworn()
        {
            int claims = 0;
            foreach (var (x, y) in OpenBeating())
            {
                Attach(new Zone(WorldMap.ToZoneID(x, y, 0)));
                if (ClothClaims.OnZoneEntered(player, zone)) claims++;
            }
            Assert.AreEqual(0, claims, "counter-check: no oath, no claim");
            Assert.AreEqual(0, player.GetIntProperty("ClothClaimCount"));
        }

        [Test] public void ClaimsAreBoundedForAGame()
        {
            SwearPlayer();
            int claims = 0;
            foreach (var (x, y) in OpenBeating())
            {
                Attach(new Zone(WorldMap.ToZoneID(x, y, 0)));
                if (ClothClaims.OnZoneEntered(player, zone)) claims++;
            }
            Assert.LessOrEqual(claims, ClothClaims.MaximumClaims);
            Assert.Greater(claims, 0);
        }
    }
}
