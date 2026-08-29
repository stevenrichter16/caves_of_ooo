using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W5.4 (Docs/FELLING-W5-PLAN.md §3) — the catacomb village: the
    /// Stranded Settlement floor. Canon's authored hearth-chamber
    /// sketch and legend live at FELLING-WORLD-DESIGN.md:1099-1113
    /// (NOT in catacomb_village_design.md, which is anthropology and
    /// carries no floor plan — a false premise this phase's sweep
    /// corrected).
    ///
    /// <para>The reveal order canon fixes: the GLOW before the people,
    /// a threshold you feel underfoot, the hearth-patch at the centre,
    /// niches rising three tiers, and the plaque-wall — oldest at the
    /// floor, with one niche chiselled smooth.</para>
    /// </summary>
    public class CatacombVillageTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static Zone BuildVillage(int seed = 7)
        {
            // Carve a floor the way the sinkhole pipeline does, then run
            // the village builder on it.
            var zone = new Zone("Overworld.4.6.2");
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var f = _factory.CreateEntity("StoneFloor");
                    if (f != null) zone.AddEntity(f, x, y);
                }
            new StrandedSettlementBuilder().BuildZone(zone, _factory, new Random(seed));
            return zone;
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════════
        //   Canon's assignment — the drift W5.3 introduced, corrected
        // ════════════════════════════════════════════════════════════

        [Test]
        public void CanonsCatacombHubs_AreCatacombVillages()
        {
            // W5.3 assigned Lampwell and Spivenor to DrownedSima on my
            // own rationale ("a well holds water"). Canon disagrees:
            // Spivenor is the named instance of the Spore-Wedded village
            // archetype (FELLING-WORLD-DESIGN.md:752) and Lampwell is
            // the bioluminescent catacomb hub (:943). Canon is the
            // authority; the rationale was invention.
            Assert.AreEqual(SinkholeArchetype.StrandedSettlement,
                SinkholeArchetypes.For("Spivenor"),
                "Spivenor is the canon Spore-Wedded village");
            Assert.AreEqual(SinkholeArchetype.StrandedSettlement,
                SinkholeArchetypes.For("Lampwell"),
                "Lampwell is the bioluminescent catacomb hub");
            Assert.AreEqual(SinkholeArchetype.StrandedSettlement,
                SinkholeArchetypes.For("Olderdeep"),
                "and Olderdeep is the founding village (its god is W6's)");
            Assert.AreEqual(SinkholeArchetype.ChoirCathedral,
                SinkholeArchetypes.For("the Deepest Cathedral"));
        }

        // ════════════════════════════════════════════════════════════
        //   The place
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheGlowIsTheTown()
        {
            // "the hearth-patch (glows, radius 4, IS the town)" —
            // canon's own legend. One patch, and it is a light source.
            var zone = BuildVillage();
            Assert.Greater(CountOf(zone, "HearthPatch"), 4,
                "the patch is a body of fungus, not a pixel");

            Entity patch = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "HearthPatch") patch = e;
            var light = patch.GetPart<LightSourcePart>();
            Assert.IsNotNull(light, "you see the glow before you see the people");
            Assert.AreEqual(4, light.Radius, "canon's radius");
        }

        [Test]
        public void TheWallIsTheVillagesHistory()
        {
            var zone = BuildVillage();
            Assert.Greater(CountOf(zone, "PlaqueWall"), 8,
                "the wall runs the chamber");

            // The six authored plaques (Lore/Codex/05_PlaqueWall.md),
            // each its own readable.
            foreach (var name in new[] { "PlaqueRiane", "PlaqueOssu", "PlaqueMerrin",
                                         "PlaqueSmoothed", "PlaqueVashti", "PlaqueOldest" })
                Assert.AreEqual(1, CountOf(zone, name),
                    name + " is on the wall, exactly once");
        }

        [Test]
        public void TheSmoothPlace_SaysNothing_BecauseThatIsThePoint()
        {
            // Refusal-to-inscribe: the worst punishment is that your
            // name is never added, so you cannot be Re-Membered. The
            // niche is chiselled smooth and the plaque carries no name.
            var smoothed = _factory.CreateEntity("PlaqueSmoothed");
            Assert.IsNotNull(smoothed);
            MessageLog.Clear();
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Examine");
            smoothed.FireEvent(ev);
            ev.Release();
            string said = MessageLog.GetLast();
            StringAssert.DoesNotContain("Riane", said);
            StringAssert.DoesNotContain("Ossu", said);
            Assert.IsTrue(said.Contains("—") || said.Contains("smooth"),
                "the wall shows the absence, and names no one");
        }

        [Test]
        public void TheChildsPlaque_SurvivesVerbatim()
        {
            // Canon quotes it; it ships exactly.
            var vashti = _factory.CreateEntity("PlaqueVashti");
            MessageLog.Clear();
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Examine");
            vashti.FireEvent(ev);
            ev.Release();
            StringAssert.Contains("I WILL SLEEP HERE WHEN I AM DONE. SAVE MY PLACE.",
                MessageLog.GetLast());
        }

        [Test]
        public void TheThreshold_AnnouncesYou()
        {
            // "the Pebble-Sundew threshold = announcement". Canon makes
            // the doormat the village's first sense-organ: stepping on
            // it IS the formal greeting.
            var zone = new Zone("Threshold");
            var mat = _factory.CreateEntity("PebbleSundewThreshold");
            Assert.IsNotNull(mat, "the threshold ships");
            zone.AddEntity(mat, 10, 10);
            // (H4 made the greeting player-only and once-ever: the
            // second-person line belongs to the player's own step.)
            var walker = new Entity { ID = "w", BlueprintName = "Walker" };
            walker.Tags["Creature"] = "";
            walker.Tags["Player"] = "";
            walker.AddPart(new RenderPart { DisplayName = "walker" });
            walker.AddPart(new PhysicsPart { Solid = true });
            walker.Statistics["Hitpoints"] = new Stat
            { Owner = walker, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            zone.AddEntity(walker, 9, 10);
            MessageLog.Clear();

            Assert.IsTrue(MovementSystem.TryMove(walker, zone, 1, 0));

            StringAssert.Contains("dewstep", MessageLog.GetLast().ToLowerInvariant(),
                "the village recognises you by the feel of your step");
        }

        [Test]
        public void ThreateningThePatch_IsWar()
        {
            // Canon calls this load-bearing, not flavour: the patch is
            // "the village's physical and political heart; threatening
            // it is war".
            var zone = new Zone("PatchWar");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var patch = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(patch, 10, 10);
                var vandal = new Entity { ID = "v", BlueprintName = "Vandal" };
                vandal.Tags["Creature"] = "";
                vandal.Tags["Player"] = "";
                vandal.Statistics["Hitpoints"] = new Stat
                { Owner = vandal, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
                zone.AddEntity(vandal, 11, 10);
                Diag.ResetAll();

                PlayerReputation.Reset();
                var d = new Damage(3);
                d.AddAttribute("Slashing");
                DestructionSystem.RouteDamage(patch, d, vandal, zone);

                // Verify-pass correction: the first cut charged −40,
                // and 0 − 40 = −40 is not ≤ DISLIKED(−50) — the one
                // act canon calls war left the village NEUTRAL and
                // still chatting. War must be war on the attitude
                // tier, not a ledger nudge the step function eats.
                Assert.AreEqual(PlayerReputation.Attitude.Hated,
                    PlayerReputation.GetAttitude("CatacombFolk"),
                    "threatening the patch IS war — the village hates you, immediately");
                Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
                { Category = "faction", Kind = "HearthThreatened", Limit = 5 }).Records.Count,
                    "and the gate names itself");
            }
            finally { SettlementRuntime.Reset(); PlayerReputation.Reset(); }
        }

        [Test]
        public void TheWarIsDeclaredOnce_NotOncePerBlow()
        {
            // Cold-eye: HearthPatchPart fires on TakeDamage, and
            // DestructionSystem.RouteDamage emits that on EVERY blow —
            // including every tick of a BurningEffect, which passes its
            // IgnitionSource as the source. Setting the patch alight
            // therefore charged -40 per tick (HP 40 at 1-4/tick = ten
            // to forty charges) when the design is ONE break. The war
            // starts once; it cannot be re-declared.
            var zone = new Zone("PatchWarOnce");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var patch = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(patch, 10, 10);
                var vandal = new Entity { ID = "v", BlueprintName = "Vandal" };
                vandal.Tags["Creature"] = "";
                vandal.Tags["Player"] = "";
                vandal.Statistics["Hitpoints"] = new Stat
                { Owner = vandal, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
                zone.AddEntity(vandal, 11, 10);

                PlayerReputation.Reset();
                int before = PlayerReputation.Get("CatacombFolk");
                for (int blow = 0; blow < 5; blow++)
                {
                    var d = new Damage(2);
                    d.AddAttribute("Fire");
                    DestructionSystem.RouteDamage(patch, d, vandal, zone);
                }
                int lost = before - PlayerReputation.Get("CatacombFolk");
                Assert.AreEqual(-PlayerReputation.HATED_THRESHOLD, lost,
                    "five blows, one war — the village does not charge you " +
                    "again for a fire it is already fighting");
            }
            finally { SettlementRuntime.Reset(); PlayerReputation.Reset(); }
        }

        [Test]
        public void TheWarIsOneWar_AcrossEveryTileOfThePatch()
        {
            // Verify-pass RED: the ellipse places ~51 separate
            // HearthPatch ENTITIES, so a per-Part latch was 51
            // independent latches — one Pyroclasm hit nine tiles in a
            // single cast and billed nine wars. The reputation ledger
            // is the latch now: village-scoped, shared by every tile.
            var zone = new Zone("PatchWarTiles");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var tileA = _factory.CreateEntity("HearthPatch");
                var tileB = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(tileA, 10, 10);
                zone.AddEntity(tileB, 11, 10);
                var vandal = new Entity { ID = "v", BlueprintName = "Vandal" };
                vandal.Tags["Creature"] = "";
                vandal.Tags["Player"] = "";
                vandal.Statistics["Hitpoints"] = new Stat
                { Owner = vandal, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
                zone.AddEntity(vandal, 12, 10);

                PlayerReputation.Reset();
                foreach (var tile in new[] { tileA, tileB })
                {
                    var d = new Damage(2);
                    d.AddAttribute("Fire");
                    DestructionSystem.RouteDamage(tile, d, vandal, zone);
                }
                Assert.AreEqual(PlayerReputation.HATED_THRESHOLD,
                    PlayerReputation.Get("CatacombFolk"),
                    "two tiles, one village, one war — not one war per tile");
            }
            finally { SettlementRuntime.Reset(); PlayerReputation.Reset(); }
        }

        [Test]
        public void AFallingRock_IsNotAnEnemyOfTheVillage()
        {
            // Counter-check: only a person can commit a crime. Damage
            // with a non-player source (a collapse, a stray beast)
            // must not start the war.
            var zone = new Zone("PatchRock");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var patch = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(patch, 10, 10);
                var beast = new Entity { ID = "b", BlueprintName = "Snapjaw" };
                beast.Tags["Creature"] = "";
                zone.AddEntity(beast, 11, 10);

                PlayerReputation.Reset();
                var d = new Damage(5);
                d.AddAttribute("Crushing");
                DestructionSystem.RouteDamage(patch, d, beast, zone);

                Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"),
                    "the village does not blame the walker for the ceiling");
            }
            finally { SettlementRuntime.Reset(); PlayerReputation.Reset(); }
        }

        // ════════════════════════════════════════════════════════════
        //   The people, and what the floor must not do to them
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheWardenMeetsYou_AndTheWallIsTended()
        {
            // Canon: "Stranger entering a village is met first by a
            // Warden". The Plaque-Tender speaks the bracketed notes the
            // codex already wrote.
            var zone = BuildVillage();
            Assert.AreEqual(1, CountOf(zone, "CatacombWarden"), "one Warden meets you");
            Assert.AreEqual(1, CountOf(zone, "PlaqueTender"), "one Tender keeps the wall");

            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "CatacombWarden" && e.BlueprintName != "PlaqueTender")
                    continue;
                Assert.AreEqual("CatacombFolk", e.Tags.ContainsKey("Faction")
                    ? e.Tags["Faction"] : null, e.BlueprintName + " is one of the Rooted's people");
                Assert.IsNotNull(e.GetPart<ConversationPart>(), e.BlueprintName + " speaks");
            }
        }

        [Test]
        public void TheVillageFootprint_IsClaimed()
        {
            // HAZARD the sweep caught: the sinkhole floor pipeline still
            // runs PopulationBuilder(UndergroundTier) AFTER this builder,
            // which rolls snapjaws. A village square full of snapjaws is
            // not a village. Claiming the footprint in GenReservedCells
            // is how the surface town keeps its square clear.
            var zone = BuildVillage();
            Assert.Greater(zone.GenReservedCells.Count, 100,
                "the chamber is claimed against later spawners");
        }

        // ════════════════════════════════════════════════════════════
        //   W5.7 close-out — the war's other direction, and the door
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheWarCannotIMPROVEYourStanding()
        {
            // Close-out 🟡 (verify pass) — the set-to arithmetic
            // computes WarRep − current; for a player already BELOW
            // −150 (villager murders), deleting the early-out guard
            // would make patch damage RAISE rep by the difference and
            // print "improves". All the other war tests start at rep 0
            // and cannot see this direction.
            var zone = new Zone("PatchWarWorse");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var patch = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(patch, 10, 10);
                var vandal = new Entity { ID = "v", BlueprintName = "Vandal" };
                vandal.Tags["Creature"] = "";
                vandal.Tags["Player"] = "";
                vandal.Statistics["Hitpoints"] = new Stat
                { Owner = vandal, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
                zone.AddEntity(vandal, 11, 10);

                PlayerReputation.Reset();
                PlayerReputation.Modify("CatacombFolk", -175, silent: true);
                var d = new Damage(2);
                d.AddAttribute("Fire");
                DestructionSystem.RouteDamage(patch, d, vandal, zone);

                Assert.AreEqual(-175, PlayerReputation.Get("CatacombFolk"),
                    "a deeper grievance is not paid off by a fresh crime");
            }
            finally { SettlementRuntime.Reset(); PlayerReputation.Reset(); }
        }

        [Test]
        public void KillingAVillager_IsWarGrade()
        {
            // Close-out hypothesis H8 — murdering Warden Nossik used
            // to cost −10 (the base-Creature GivesRep) while
            // scratching the fungus cost −150: people were 15× cheaper
            // than the crop. Both named villagers now carry the
            // war-grade value.
            foreach (var bp in new[] { "CatacombWarden", "PlaqueTender" })
            {
                var v = _factory.CreateEntity(bp);
                var rep = v.GetPart<GivesRepPart>();
                Assert.IsNotNull(rep, bp + " carries GivesRep");
                Assert.AreEqual(150, rep.Value,
                    bp + ": a person of the village is worth no less than the patch");
            }
        }

        [Test]
        public void TheDewstep_GreetsYouOnce_NotOncePerMat()
        {
            // Close-out hypothesis H4 — the threshold is ~35 separate
            // trigger entities; one walk across the band printed the
            // same second-person line up to five times. Recognition is
            // permanent: greeted once, remembered after.
            var zone = new Zone("DewOnce");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var matA = _factory.CreateEntity("PebbleSundewThreshold");
                var matB = _factory.CreateEntity("PebbleSundewThreshold");
                zone.AddEntity(matA, 10, 10);
                zone.AddEntity(matB, 11, 10);
                var walker = new Entity { ID = "w", BlueprintName = "Walker" };
                walker.Tags["Creature"] = "";
                walker.Tags["Player"] = "";
                walker.AddPart(new RenderPart { DisplayName = "you" });
                walker.AddPart(new PhysicsPart { Solid = true });
                walker.Statistics["Hitpoints"] = new Stat
                { Owner = walker, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
                zone.AddEntity(walker, 9, 10);

                MessageLog.Clear();
                MovementSystem.TryMove(walker, zone, 1, 0);   // onto mat A
                MovementSystem.TryMove(walker, zone, 1, 0);   // onto mat B
                int greetings = 0;
                foreach (var line in MessageLog.GetMessages())
                    if (line.Contains("dewstep")) greetings++;
                Assert.AreEqual(1, greetings,
                    "two mats, one welcome — the village met you already");
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void TheDewstep_DoesNotNarrateOtherPeoplesSteps()
        {
            // Counter-check (H4's second half): a wandering NPC
            // crossing the mats must not print second-person text at
            // the player from across the zone. The village still FEELS
            // the step — the diag record fires — it just says nothing.
            var zone = new Zone("DewNPC");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var mat = _factory.CreateEntity("PebbleSundewThreshold");
                zone.AddEntity(mat, 10, 10);
                var stranger = new Entity { ID = "s", BlueprintName = "Snapjaw" };
                stranger.Tags["Creature"] = "";
                stranger.AddPart(new RenderPart { DisplayName = "snapjaw" });
                stranger.AddPart(new PhysicsPart { Solid = true });
                stranger.Statistics["Hitpoints"] = new Stat
                { Owner = stranger, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
                zone.AddEntity(stranger, 9, 10);

                MessageLog.Clear();
                Diag.ResetAll();
                MovementSystem.TryMove(stranger, zone, 1, 0);
                foreach (var line in MessageLog.GetMessages())
                    StringAssert.DoesNotContain("dewstep", line,
                        "the weight of YOU belongs to the player alone");
                Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
                { Category = "faction", Kind = "Dewstep", Limit = 5 }).Records.Count,
                    "but the village still knows something crossed");
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void TheOldestPlaque_BelongsToTheWall()
        {
            // Close-out 🔵 — it used to sit 17 rows south of the wall
            // on open floor: canon's wall is ONE artifact, oldest at
            // ITS floor, so the oldest lives in the wall's own band.
            var zone = BuildVillage();
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "PlaqueOldest")
                {
                    var pos = zone.GetEntityPosition(e);
                    Assert.LessOrEqual(pos.y, 5,
                        "the oldest plaque anchors the wall run, not the far floor");
                    return;
                }
            Assert.Fail("the oldest plaque exists");
        }

        [Test]
        public void AtWar_TheWardenRefusesToSpeak()
        {
            // Close-out hypothesis H9 — war means war: the shipped
            // refusal gate (ConversationManager.cs:61) fires for a
            // Hated-faction speaker, and nothing pinned it against the
            // war path until now. The Choir's patience (SpeaksToHostiles)
            // is the deliberate exception, pinned in ChoirCathedralTests.
            var zone = new Zone("WarNoChat");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                ConversationLoader.Reset();
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                    Application.dataPath, "Resources/Content/Conversations/Catacomb.json")));
                var warden = _factory.CreateEntity("CatacombWarden");
                zone.AddEntity(warden, 10, 10);
                var player = new Entity { ID = "p", BlueprintName = "Player" };
                player.Tags["Player"] = "";
                player.Tags["Creature"] = "";
                player.Tags["Faction"] = "Player";
                player.AddPart(new RenderPart { DisplayName = "you" });
                zone.AddEntity(player, 11, 10);

                PlayerReputation.Reset();
                PlayerReputation.Modify("CatacombFolk", -150, silent: true);
                bool started = ConversationManager.StartConversation(warden, player);
                Assert.IsFalse(started, "war is war; the door is shut");
            }
            finally
            {
                ConversationLoader.Reset();
                SettlementRuntime.Reset();
                PlayerReputation.Reset();
            }
        }

    }
}
