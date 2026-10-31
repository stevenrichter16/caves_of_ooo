using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P3 (Docs/PLACE-WORK-AND-PROGRESSION.md §3 rule 2): kill XP scales by level band
    /// like Qud (XRL.World.Parts/Experience.cs:33-50), so frogs stop paying a level-15
    /// player, and a companion's kill pays its party leader (Experience.cs:80) instead
    /// of vanishing. Every award or withholding emits a progression diag record.
    /// </summary>
    public sealed class KillXPAwardTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }
        [TearDown] public void Cleanup() { MessageLog.Clear(); }

        static Entity Player(int level)
        {
            var p = new Entity { BlueprintName = "Player", ID = "player" };
            p.SetTag("Player"); p.SetTag("Creature");
            p.Statistics["Level"] = new Stat { Name = "Level", BaseValue = level, Value = level, Min = 1, Max = 99 };
            p.Statistics["Experience"] = new Stat { Name = "Experience", BaseValue = 0, Value = 0, Min = 0, Max = 999999 };
            p.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 40, Value = 40, Min = 0, Max = 40 };
            p.AddPart(new BrainPart());
            return p;
        }
        static Entity Victim(int xp, int? level, string tier = null)
        {
            var v = new Entity { BlueprintName = "Probe", ID = "victim" };
            v.SetTag("Creature");
            v.Statistics["XPValue"] = new Stat { Name = "XPValue", BaseValue = xp, Value = xp, Min = 0, Max = 999999 };
            if (level.HasValue) v.Statistics["Level"] = new Stat { Name = "Level", BaseValue = level.Value, Value = level.Value, Min = 1, Max = 50 };
            if (tier != null) v.SetTag("Tier", tier);
            return v;
        }
        static Entity Follower(Entity leader)
        {
            var f = new Entity { BlueprintName = "Companion", ID = "companion" };
            f.SetTag("Creature");
            var brain = new BrainPart(); f.AddPart(brain);
            if (leader != null) Assert.True(brain.SetPartyLeader(leader), "precondition: recruited");
            return f;
        }
        static int XP(Entity e) => e.GetStatValue("Experience", 0);
        static int Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "progression", Kind = kind, Limit = 50 }).Records.Count;

        // ════════════════ level-band scaling ════════════════

        [Test] public void APeerKillPaysInFull()
        {
            var p = Player(4); LevelingSystem.AwardKillXP(p, Victim(20, 3), null);
            Assert.AreEqual(20, XP(p)); Assert.AreEqual(1, Records("XPAwarded"));
        }

        [Test] public void AKillOneBandBelowPaysHalf()
        {
            var p = Player(5); LevelingSystem.AwardKillXP(p, Victim(20, 4), null);
            Assert.AreEqual(10, XP(p));
        }

        [Test] public void AFarOutclassedKillPaysNothingAndSaysSo()
        {
            var p = Player(15); LevelingSystem.AwardKillXP(p, Victim(9, 2), null);
            Assert.AreEqual(0, XP(p));
            Assert.False(MessageLog.GetRecent(5).Exists(m => m.Contains("You gain")), "no XP line for nothing");
            Assert.AreEqual(1, Records("XPWithheld")); Assert.AreEqual(0, Records("XPAwarded"));
        }

        [Test] public void AStrongerVictimPaysInFullNeverMore()
        {
            var p = Player(1); LevelingSystem.AwardKillXP(p, Victim(311, 8), null);
            Assert.AreEqual(311, XP(p) + Spent(p), "full value, no bonus, carried across the level-up");
        }
        static int Spent(Entity p)
        {
            int total = 0; for (int l = 1; l < p.GetStatValue("Level", 1); l++) total += LevelingSystem.XPToNextLevel(l);
            return total;
        }

        [Test] public void AVictimWithoutALevelIsTreatedAsItsTiersFloor()
        {
            var p = Player(9); LevelingSystem.AwardKillXP(p, Victim(100, null, "3"), null);
            Assert.AreEqual(100, XP(p), "tier 3 floor is L8, same band as L9");
            var q = Player(15); LevelingSystem.AwardKillXP(q, Victim(100, null, "1"), null);
            Assert.AreEqual(0, XP(q), "counter-check: a tier-1 victim without a level is L1, three bands below a L15 killer");
        }

        [Test] public void ANoXPVictimPaysNothingEvenToANewcomer()
        {
            var p = Player(1); var v = Victim(20, 1); v.SetTag("NoXP");
            LevelingSystem.AwardKillXP(p, v, null);
            Assert.AreEqual(0, XP(p)); Assert.AreEqual(1, Records("XPWithheld"));
        }

        // ════════════════ companions ════════════════

        [Test] public void ACompanionsKillPaysItsLeader()
        {
            var p = Player(3); var f = Follower(p);
            LevelingSystem.AwardKillXP(f, Victim(20, 3), null);
            Assert.AreEqual(20, XP(p), "Qud passes the award up to the party leader");
            Assert.AreEqual(0, f.GetStatValue("Experience", 0));
        }

        [Test] public void ACompanionsKillIsScaledAtTheLeadersLevel()
        {
            var p = Player(10); var f = Follower(p);
            LevelingSystem.AwardKillXP(f, Victim(100, 3), null);
            Assert.AreEqual(10, XP(p), "band 2 vs band 0: a tenth");
        }

        [Test] public void AStrangersKillPaysNobody()
        {
            var p = Player(3); var stranger = Follower(null);
            LevelingSystem.AwardKillXP(stranger, Victim(20, 3), null);
            Assert.AreEqual(0, XP(p));
            Assert.AreEqual(0, Records("XPAwarded"));
        }

        [Test] public void AFollowerOfSomeoneElsePaysNobody()
        {
            var p = Player(3); var otherLeader = Follower(null); var f = Follower(otherLeader);
            LevelingSystem.AwardKillXP(f, Victim(20, 3), null);
            Assert.AreEqual(0, XP(p));
        }

        [Test] public void AFollowerOfAFollowerOfThePlayerPaysThePlayer()
        {
            var p = Player(3); var lieutenant = Follower(p); var f = Follower(lieutenant);
            LevelingSystem.AwardKillXP(f, Victim(20, 3), null);
            Assert.AreEqual(20, XP(p), "the award walks up the chain");
        }

        // ════════════════ the death path ════════════════

        [Test] public void DeathByACompanionReachesTheAward()
        {
            var zone = new Zone("KillXP.Death");
            var p = Player(3); var f = Follower(p); var v = Victim(20, 3);
            v.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1, Value = 1, Min = 0, Max = 1 };
            zone.AddEntity(p, 5, 5); zone.AddEntity(f, 6, 5); zone.AddEntity(v, 7, 5);
            CombatSystem.HandleDeath(v, f, zone);
            Assert.AreEqual(20, XP(p), "CombatSystem no longer gates the award on the killer being the player");
        }
    }
}
