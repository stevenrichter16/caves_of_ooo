using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Followers F.3.4 — <see cref="GrantsRepAsFollowerPart"/>
    /// apply/unapply lifecycle.
    ///
    /// <para><b>Contract:</b> a Part on a creature's blueprint that
    /// modifies the player's faction reputation while the creature is
    /// (a) following the player as their <see cref="BrainPart.PartyLeader"/>
    /// AND (b) in the same zone as the player. Each turn-end, the Part
    /// calls <see cref="GrantsRepAsFollowerPart.CheckApplyBonus"/> to
    /// (re)evaluate the apply/unapply state.</para>
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>XRL.World.Parts/GrantsRepAsFollower.cs</c>. CoO F.3 v1 ports:
    /// the comma-delimited <c>Faction</c> syntax with optional
    /// <c>:N</c> per-entry override, idempotent apply/unapply,
    /// same-zone check, leader-is-player check, EndTurn hook.</para>
    ///
    /// <para><b>Deferred from Qud parity:</b></para>
    /// <list type="bullet">
    ///   <item><c>*allvisiblefactions:N</c> wildcard syntax — F.5+ content polish</item>
    ///   <item><c>DeepCopy</c> override that resets <c>AppliedBonus</c>
    ///         — CoO has no in-game cloning yet (⚪ out of scope)</item>
    ///   <item><c>SuspendingEvent</c> / <c>OnDestroyObjectEvent</c>
    ///         unapply — CoO's zone-unload + destroy flow doesn't have
    ///         these specific event hooks yet (🟡 deferred; the leader-
    ///         null check in CheckApplyBonus catches most cases when
    ///         the leader is destroyed first)</item>
    /// </list>
    /// </summary>
    public class GrantsRepAsFollowerPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            PlayerReputation.Reset();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakePlayer(string id = "p")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new BrainPart());
            return e;
        }

        private static Entity MakeNPC(string id, string faction = "Snapjaws", int value = 10)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new BrainPart());
            e.AddPart(new GrantsRepAsFollowerPart(faction, value));
            return e;
        }

        private static (Entity player, Entity npc, Zone zone) MakeFollowerInZone(
            string faction = "Snapjaws", int value = 10)
        {
            var player = MakePlayer();
            var npc = MakeNPC("npc", faction, value);
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(player);
            return (player, npc, zone);
        }

        // ── Apply path ───────────────────────────────────────────

        [Test]
        public void Apply_PlayerLeader_SameZone_AppliesRep()
        {
            var (player, npc, zone) = MakeFollowerInZone(faction: "Snapjaws", value: 10);
            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "Precondition: zero rep before apply.");

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"),
                "After CheckApplyBonus with player as leader + same zone: +10 rep.");
            Assert.IsTrue(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus,
                "AppliedBonus flag set after apply.");
        }

        [Test]
        public void Apply_LeaderNotPlayer_DoesNotApply()
        {
            // Counter-check: NPC recruited by another NPC (not the
            // player). No faction rep flows — the bonus is
            // player-specific, mirroring Qud's IsPlayer() gate.
            var player = MakePlayer();
            var otherLeader = MakeNPC("other-leader", faction: "", value: 0);
            var npc = MakeNPC("npc", "Snapjaws", 10);
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(otherLeader, 7, 5);
            zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            otherLeader.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(otherLeader);

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(otherLeader);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "NPC leader → no player-rep flow.");
            Assert.IsFalse(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus);
        }

        [Test]
        public void Apply_DifferentZone_DoesNotApply()
        {
            // Player and NPC are leader-linked but in different zones.
            // Rep flows only when they're co-located.
            var player = MakePlayer();
            var npc = MakeNPC("npc", "Snapjaws", 10);
            var zoneA = new Zone("A");
            var zoneB = new Zone("B");
            zoneA.AddEntity(player, 5, 5);
            zoneB.AddEntity(npc, 5, 5);
            player.GetPart<BrainPart>().CurrentZone = zoneA;
            npc.GetPart<BrainPart>().CurrentZone = zoneB;
            npc.GetPart<BrainPart>().SetPartyLeader(player);

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "Cross-zone → no rep flow.");
            Assert.IsFalse(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus);
        }

        [Test]
        public void Apply_Idempotent_DoesNotStackOnRepeatCalls()
        {
            // Two CheckApplyBonus calls with the same conditions → only
            // one rep delta applied (AppliedBonus gate).
            var (player, npc, zone) = MakeFollowerInZone("Snapjaws", 10);
            var part = npc.GetPart<GrantsRepAsFollowerPart>();

            part.CheckApplyBonus(player);
            part.CheckApplyBonus(player);
            part.CheckApplyBonus(player);

            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"),
                "Three calls with same conditions → still +10 rep (idempotent).");
        }

        // ── Unapply path ─────────────────────────────────────────

        [Test]
        public void Unapply_LeaderBecomesNull_UndoesRep()
        {
            var (player, npc, zone) = MakeFollowerInZone("Snapjaws", 10);
            var part = npc.GetPart<GrantsRepAsFollowerPart>();
            part.CheckApplyBonus(player);
            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"));

            // Leader-link breaks (dismiss, leader dies, etc.)
            npc.GetPart<BrainPart>().SetPartyLeader(null);
            part.CheckApplyBonus(null);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "Leader gone → unapply restores prior rep.");
            Assert.IsFalse(part.AppliedBonus,
                "AppliedBonus flag cleared after unapply.");
        }

        [Test]
        public void Unapply_LeaderLeavesZone_UndoesRep()
        {
            var (player, npc, zone) = MakeFollowerInZone("Snapjaws", 10);
            var part = npc.GetPart<GrantsRepAsFollowerPart>();
            part.CheckApplyBonus(player);
            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"));

            // Player transits to a new zone WITHOUT this NPC (stranding
            // case from F.2.7 — the follower is left behind).
            var otherZone = new Zone("other");
            player.GetPart<BrainPart>().CurrentZone = otherZone;
            part.CheckApplyBonus(player);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "Player left the zone → unapply.");
            Assert.IsFalse(part.AppliedBonus);
        }

        [Test]
        public void Unapply_Idempotent_DoesNotStackOnRepeatCalls()
        {
            // Calling CheckApplyBonus on already-unapplied state is a no-op.
            var (player, npc, zone) = MakeFollowerInZone("Snapjaws", 10);
            var part = npc.GetPart<GrantsRepAsFollowerPart>();
            // Apply, unapply, then call unapply path again multiple times.
            part.CheckApplyBonus(player);
            npc.GetPart<BrainPart>().SetPartyLeader(null);
            part.CheckApplyBonus(null);
            part.CheckApplyBonus(null);
            part.CheckApplyBonus(null);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "Three unapply calls → still zero (idempotent).");
        }

        // ── Qud-parity comma-delimited Faction syntax ────────────

        [Test]
        public void Parser_SingleFaction_AppliesValue()
        {
            var (player, npc, _) = MakeFollowerInZone("Snapjaws", 10);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"));
        }

        [Test]
        public void Parser_CommaDelimitedFactions_AppliesValueToEach()
        {
            // "Snapjaws,Bandits" with Value=5 → +5 Snapjaws, +5 Bandits
            var (player, npc, _) = MakeFollowerInZone("Snapjaws,Bandits", 5);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(5, PlayerReputation.Get("Snapjaws"));
            Assert.AreEqual(5, PlayerReputation.Get("Bandits"));
        }

        [Test]
        public void Parser_PerFactionOverride_UsesColonValue()
        {
            // "Snapjaws:10,Bandits:-3" — per-faction values override
            // the default Value field.
            var (player, npc, _) = MakeFollowerInZone("Snapjaws:10,Bandits:-3", value: 100);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"),
                "Per-faction colon override used in place of default Value.");
            Assert.AreEqual(-3, PlayerReputation.Get("Bandits"),
                "Negative per-faction value also honored.");
        }

        [Test]
        public void Parser_MixedSyntax_DefaultAndOverride()
        {
            // "FactionA,FactionB:7,FactionC" with Value=2 →
            //   FactionA gets +2 (no colon, default)
            //   FactionB gets +7 (colon override)
            //   FactionC gets +2 (no colon, default)
            var (player, npc, _) = MakeFollowerInZone("FactionA,FactionB:7,FactionC", value: 2);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(2, PlayerReputation.Get("FactionA"));
            Assert.AreEqual(7, PlayerReputation.Get("FactionB"));
            Assert.AreEqual(2, PlayerReputation.Get("FactionC"));
        }

        [Test]
        public void Parser_EmptyFactionString_NoOp()
        {
            var (player, npc, _) = MakeFollowerInZone(faction: "", value: 10);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.IsFalse(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus,
                "Empty Faction string → no apply (avoids modifying the empty-string faction).");
        }

        [Test]
        public void Parser_WhitespaceOnlyFaction_NoOp()
        {
            var (player, npc, _) = MakeFollowerInZone(faction: "   ,  ,", value: 10);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            // All entries are whitespace, none survive trim → no apply.
            Assert.IsFalse(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus,
                "Whitespace-only entries are filtered.");
        }

        // ── Apply/Unapply Symmetry ───────────────────────────────

        [Test]
        public void ApplyThenUnapply_NetZeroChange()
        {
            // Symmetric pair: apply +N then unapply must result in net zero.
            // Comma-delimited + per-faction override case for full coverage.
            var (player, npc, _) = MakeFollowerInZone("Snapjaws:10,Bandits:-3,Forest:5", 2);
            var part = npc.GetPart<GrantsRepAsFollowerPart>();
            part.CheckApplyBonus(player);
            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"));
            Assert.AreEqual(-3, PlayerReputation.Get("Bandits"));
            Assert.AreEqual(5, PlayerReputation.Get("Forest"));

            npc.GetPart<BrainPart>().SetPartyLeader(null);
            part.CheckApplyBonus(null);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"));
            Assert.AreEqual(0, PlayerReputation.Get("Bandits"));
            Assert.AreEqual(0, PlayerReputation.Get("Forest"));
        }

        // ── EndTurn event integration ────────────────────────────

        [Test]
        public void EndTurnEvent_TriggersCheckApplyBonus()
        {
            // The Part listens for "EndTurn" events on the parent
            // entity and calls CheckApplyBonus(PartyLeader). End-to-end
            // test: fire an EndTurn event, verify rep flows.
            var (player, npc, _) = MakeFollowerInZone("Snapjaws", 10);
            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"));

            var endTurn = GameEvent.New("EndTurn");
            npc.FireEventAndRelease(endTurn);

            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"),
                "EndTurn event triggers CheckApplyBonus → +10 rep.");
        }

        // ── Null safety ──────────────────────────────────────────

        [Test]
        public void CheckApplyBonus_NullLeader_NoCrash()
        {
            var npc = MakeNPC("npc");
            var zone = new Zone("z");
            zone.AddEntity(npc, 5, 5);
            npc.GetPart<BrainPart>().CurrentZone = zone;

            Assert.DoesNotThrow(() =>
                npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(null));
            Assert.IsFalse(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus);
        }

        // ── F.3.5 — Save/load round-trip ─────────────────────────

        [Test]
        public void RoundTrip_PreservesFactionAndValue()
        {
            var npc = new Entity { ID = "npc", BlueprintName = "Test" };
            npc.Tags["Creature"] = "";
            npc.AddPart(new RenderPart { DisplayName = "n" });
            npc.AddPart(new BrainPart());
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws:10,Bandits:-3", 5));

            Entity loaded = PartRoundTripHelper.RoundTripEntity(npc);

            var part = loaded.GetPart<GrantsRepAsFollowerPart>();
            Assert.IsNotNull(part, "Part survives the reflection-based save round-trip.");
            Assert.AreEqual("Snapjaws:10,Bandits:-3", part.Faction,
                "Comma-delimited Faction string preserved.");
            Assert.AreEqual(5, part.Value,
                "Value preserved.");
        }

        [Test]
        public void RoundTrip_PreservesAppliedBonusTrue()
        {
            // The Part's apply state (AppliedBonus = true) must survive
            // the save round-trip. Otherwise the player would lose +N
            // rep mid-game on save+load, and the unapply path would
            // never fire (because AppliedBonus is false post-load).
            var (player, npc, _) = MakeFollowerInZone("Snapjaws", 10);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);
            Assert.IsTrue(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus,
                "Precondition: AppliedBonus is true before save.");

            Entity loaded = PartRoundTripHelper.RoundTripEntity(npc);

            Assert.IsTrue(loaded.GetPart<GrantsRepAsFollowerPart>().AppliedBonus,
                "AppliedBonus = true survives round-trip — players don't lose " +
                "applied rep across save/load.");
        }

        [Test]
        public void RoundTrip_PreservesAppliedBonusFalse()
        {
            // Counter-pair: AppliedBonus=false also round-trips.
            var npc = new Entity { ID = "npc", BlueprintName = "Test" };
            npc.Tags["Creature"] = "";
            npc.AddPart(new RenderPart { DisplayName = "n" });
            npc.AddPart(new BrainPart());
            npc.AddPart(new GrantsRepAsFollowerPart("Snapjaws", 10));
            Assert.IsFalse(npc.GetPart<GrantsRepAsFollowerPart>().AppliedBonus);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(npc);

            Assert.IsFalse(loaded.GetPart<GrantsRepAsFollowerPart>().AppliedBonus);
        }

        [Test]
        public void RoundTrip_DoesNotFire_PlayerReputationModify()
        {
            // The save graph must NOT call PlayerReputation.Modify
            // during write or read — that would double-apply the bonus
            // every load. The AppliedBonus flag is the state shadow;
            // it survives without triggering a re-apply.
            var (player, npc, _) = MakeFollowerInZone("Snapjaws", 10);
            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);
            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"),
                "Precondition: applied → +10.");

            Entity loaded = PartRoundTripHelper.RoundTripEntity(npc);

            // After round-trip, the player's rep value is unchanged.
            // The loaded Part still says AppliedBonus=true, but the
            // serialization process itself didn't fire Modify.
            Assert.AreEqual(10, PlayerReputation.Get("Snapjaws"),
                "Save/load is state-shadowing — does NOT re-fire rep modify.");
        }

        // ── Qud-parity *allvisiblefactions wildcard (post-audit Finding #2) ──

        [Test]
        public void Wildcard_AllVisibleFactions_AppliesToEveryTrackedFaction()
        {
            // Seed PlayerReputation with multiple factions (Set, not Modify
            // — Modify short-circuits on delta==0 per PlayerReputation.cs:75).
            // Then apply "*allvisiblefactions:5" → every faction in the
            // dict gets +5.
            PlayerReputation.Set("Snapjaws", 0);
            PlayerReputation.Set("Bandits", 0);
            PlayerReputation.Set("Forest", 0);
            var (player, npc, _) = MakeFollowerInZone("*allvisiblefactions:5", value: 0);

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(5, PlayerReputation.Get("Snapjaws"),
                "Wildcard applies +5 to Snapjaws.");
            Assert.AreEqual(5, PlayerReputation.Get("Bandits"),
                "Wildcard applies +5 to Bandits.");
            Assert.AreEqual(5, PlayerReputation.Get("Forest"),
                "Wildcard applies +5 to Forest.");
        }

        [Test]
        public void Wildcard_AllVisibleFactions_Unapply_ReversesAll()
        {
            // The wildcard reverses correctly on unapply.
            PlayerReputation.Set("Snapjaws", 0);
            PlayerReputation.Set("Bandits", 0);
            var (player, npc, _) = MakeFollowerInZone("*allvisiblefactions:5", value: 0);

            var part = npc.GetPart<GrantsRepAsFollowerPart>();
            part.CheckApplyBonus(player);
            Assert.AreEqual(5, PlayerReputation.Get("Snapjaws"));

            npc.GetPart<BrainPart>().SetPartyLeader(null);
            part.CheckApplyBonus(null);

            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"),
                "Wildcard unapply reverses Snapjaws.");
            Assert.AreEqual(0, PlayerReputation.Get("Bandits"),
                "Wildcard unapply reverses Bandits.");
        }

        [Test]
        public void Wildcard_AllVisibleFactions_NegativeValue_DecreasesAll()
        {
            // Negative wildcard delta (an annoying companion who annoys
            // EVERYONE).
            PlayerReputation.Set("Snapjaws", 50); // starts liked
            PlayerReputation.Set("Bandits", 50);
            var (player, npc, _) = MakeFollowerInZone("*allvisiblefactions:-10", value: 0);

            npc.GetPart<GrantsRepAsFollowerPart>().CheckApplyBonus(player);

            Assert.AreEqual(40, PlayerReputation.Get("Snapjaws"));
            Assert.AreEqual(40, PlayerReputation.Get("Bandits"));
        }

        [Test]
        public void NoGrantsPart_NoRepChange()
        {
            // Counter-check: a follower WITHOUT GrantsRepAsFollowerPart
            // doesn't change the player's rep on EndTurn.
            var player = MakePlayer();
            var npc = new Entity { ID = "plainNpc", BlueprintName = "Test" };
            npc.Tags["Creature"] = "";
            npc.AddPart(new RenderPart { DisplayName = "plain" });
            npc.AddPart(new BrainPart());
            // NOT adding GrantsRepAsFollowerPart.
            var zone = new Zone("z");
            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc, 6, 5);
            player.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().CurrentZone = zone;
            npc.GetPart<BrainPart>().SetPartyLeader(player);

            var endTurn = GameEvent.New("EndTurn");
            npc.FireEventAndRelease(endTurn);

            // No faction modified → all default rep (0).
            Assert.AreEqual(0, PlayerReputation.Get("Snapjaws"));
        }
    }
}
