using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W5.7 — the dedicated adversarial sweep for the W5 vertical-world
    /// surface (ADVERSARIAL_TESTING.md gate: state atomicity via the
    /// repair loops, cross-actor flows via the war, anti-exploit gates,
    /// save/load reach — four taxonomy surfaces, gate applies).
    ///
    /// <para>These probe bug classes the per-feature tests cannot see:
    /// null boundaries on every new public API, reflection round-trips
    /// of the Parts whose shape this phase changed, clamp boundaries on
    /// the war ledger, and idempotency of the load-time heals. Each
    /// comment says what a buggy implementation would do wrong.</para>
    /// </summary>
    public class W5AdversarialTests
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
            PlayerReputation.Reset();
        }

        [TearDown]
        public void TearDown() => PlayerReputation.Reset();

        // ════════════════════════════════════════════════════════════
        //   Boundary inputs — null safety across the new public APIs
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_StairTravel_NullEverything_NoCrash()
        {
            // A crash in the input path is a crash per frame.
            Assert.IsNull(StairTravel.FindTarget(null, null, true));
            Assert.IsNull(StairTravel.PathTo(null, null, 5, 5));
            Assert.IsNull(StairTravel.PathTo(null, null, 5, 5, ignoreCreatures: false));
            // Fail-SAFE, not fail-open: unknown state stops the walk.
            Assert.IsTrue(StairTravel.ShouldInterrupt(null, null));
        }

        [Test]
        public void Adversarial_GroveLaw_NullAndNonPlayer_NoBillNoCrash()
        {
            Assert.IsFalse(GroveLaw.IsChoirGround(null));
            Assert.IsFalse(GroveLaw.IsChoirGround(new Zone("NotAnOverworldId")));

            // Null source / null zone: silent no-ops.
            GroveLaw.OnIgnite(null, null, null);
            Assert.AreEqual(0, PlayerReputation.Get("RotChoir"));

            // Cross-actor counter-check: an NPC torching the cathedral
            // is the Choir's own problem, never the player's ledger.
            var vault = new Zone("Overworld.5.4.2");
            var npc = new Entity { ID = "npc", BlueprintName = "Snapjaw" };
            npc.Tags["Creature"] = "";
            GroveLaw.OnIgnite(npc, null, vault);
            Assert.AreEqual(0, PlayerReputation.Get("RotChoir"),
                "only the player answers to the law");
        }

        [Test]
        public void Adversarial_SinkholeArchetype_NullName_NoCrash()
        {
            // The hash fallback guards with (name ?? "") — a null-named
            // POI (a corrupted save, a future tool) must roll, not throw.
            var a = SinkholeArchetypes.For(null);
            Assert.IsTrue(System.Enum.IsDefined(typeof(SinkholeArchetype), a));
        }

        [Test]
        public void Adversarial_FixtureVariantIndex_HostileInputs_StayInRange()
        {
            // Public static: negative coords (an off-zone probe), count
            // 0 and 1 (an unregistered or single-face fixture). A buggy
            // modulo goes negative on negative hashes; a buggy count
            // divides by zero.
            Assert.AreEqual(0, EnvironmentSpriteRenderer.FixtureVariantIndex(5, 5, 0));
            Assert.AreEqual(0, EnvironmentSpriteRenderer.FixtureVariantIndex(5, 5, 1));
            for (int x = -3; x <= 3; x++)
                for (int y = -3; y <= 3; y++)
                    Assert.That(EnvironmentSpriteRenderer.FixtureVariantIndex(x, y, 4),
                        Is.InRange(0, 3), $"({x},{y})");
        }

        [Test]
        public void Adversarial_HearthPatch_NullSource_NoWar()
        {
            // TakeDamage with NO source at all (environmental decay, a
            // scripted tick) — the null guard must hold before the
            // Player-tag check ever runs.
            var zone = new Zone("PatchNull");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var patch = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(patch, 10, 10);
                var d = new Damage(3);
                d.AddAttribute("Crushing");
                DestructionSystem.RouteDamage(patch, d, null, zone);
                Assert.AreEqual(0, PlayerReputation.Get("CatacombFolk"));
            }
            finally { SettlementRuntime.Reset(); }
        }

        // ════════════════════════════════════════════════════════════
        //   Save/load reflection — the Parts whose shape W5 changed
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_HearthPatchPart_RoundTrips()
        {
            // The v6→v7 bump exists BECAUSE this part's field shape
            // changed. Pin the new shape so the next change is loud.
            var patch = _factory.CreateEntity("HearthPatch");
            patch.GetPart<HearthPatchPart>().WarRep = -120;
            var loaded = PartRoundTripHelper.RoundTripEntity(patch);
            Assert.AreEqual(-120, loaded.GetPart<HearthPatchPart>().WarRep,
                "WarRep survives the reflection writer");
        }

        [Test]
        public void Adversarial_DewstepLatch_RidesTheSave()
        {
            // The greeting latch is an entity PROPERTY: if Properties
            // ever stop round-tripping, every load re-greets — quiet
            // spam that no per-feature test would notice.
            var walker = new Entity { ID = "w", BlueprintName = "Walker" };
            walker.Properties[PebbleSundewThresholdPart.GreetedProperty] = "";
            var loaded = PartRoundTripHelper.RoundTripEntity(walker);
            Assert.IsTrue(loaded.Properties.ContainsKey(
                PebbleSundewThresholdPart.GreetedProperty),
                "the village still knows you after a load");
        }

        [Test]
        public void Adversarial_ThresholdPart_RoundTrips_NonConsuming()
        {
            // ConsumeOnTrigger=false is set in the CONSTRUCTOR, and the
            // save reader bypasses constructors (uninitialized object):
            // if the field failed to round-trip it would default to
            // false anyway — so pin the meaningful field, TriggerFaction,
            // which comes from the blueprint.
            var mat = _factory.CreateEntity("PebbleSundewThreshold");
            var loaded = PartRoundTripHelper.RoundTripEntity(mat);
            var part = loaded.GetPart<PebbleSundewThresholdPart>();
            Assert.IsNotNull(part);
            Assert.IsFalse(part.ConsumeOnTrigger, "the doormat is permanent");
            Assert.AreEqual("CatacombFolk", part.TriggerFaction,
                "the village does not trip its own doormat");
        }

        // ════════════════════════════════════════════════════════════
        //   Clamp boundaries — the war ledger under pressure
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_War_AtMinRep_StaysClamped()
        {
            // Rep already at the floor: another blow must neither
            // crash, un-clamp, nor IMPROVE anything.
            var zone = new Zone("PatchFloor");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var patch = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(patch, 10, 10);
                var vandal = MakeVandal(zone);
                PlayerReputation.Modify("CatacombFolk", -200, silent: true);
                var d = new Damage(2); d.AddAttribute("Fire");
                DestructionSystem.RouteDamage(patch, d, vandal, zone);
                Assert.AreEqual(-200, PlayerReputation.Get("CatacombFolk"));
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void Adversarial_War_ExactlyAtThreshold_NoReBill()
        {
            // The <= boundary itself: at EXACTLY WarRep the war is
            // already on; a buggy < would re-run the set-to (harmless
            // arithmetic but a fresh "worsens" line every blow).
            var zone = new Zone("PatchEdge");
            SettlementRuntime.ActiveZone = zone;
            FactionManager.Initialize();
            try
            {
                var patch = _factory.CreateEntity("HearthPatch");
                zone.AddEntity(patch, 10, 10);
                var vandal = MakeVandal(zone);
                PlayerReputation.Modify("CatacombFolk",
                    PlayerReputation.HATED_THRESHOLD, silent: true);
                MessageLog.Clear();
                var d = new Damage(2); d.AddAttribute("Fire");
                DestructionSystem.RouteDamage(patch, d, vandal, zone);
                Assert.AreEqual(PlayerReputation.HATED_THRESHOLD,
                    PlayerReputation.Get("CatacombFolk"));
                foreach (var line in MessageLog.GetMessages())
                    StringAssert.DoesNotContain("reputation", line.ToLowerInvariant(),
                        "an ongoing war is not re-announced");
            }
            finally { SettlementRuntime.Reset(); }
        }

        // ════════════════════════════════════════════════════════════
        //   Idempotency — the load-time heals
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SinkholeRehydration_Idempotent()
        {
            // The heal runs on EVERY load. Running it twice (or on a
            // fresh map that already has everything) must change
            // nothing — a non-idempotent heal would churn POI object
            // identity every load.
            var map = WorldGenerator.Generate(42);
            var before = new PointOfInterest[SinkholeSites.All.Length];
            for (int i = 0; i < SinkholeSites.All.Length; i++)
                before[i] = map.GetPOI(SinkholeSites.All[i].X, SinkholeSites.All[i].Y);
            map.RehydrateAuthoredSinkholes();
            map.RehydrateAuthoredSinkholes();
            for (int i = 0; i < SinkholeSites.All.Length; i++)
                Assert.AreSame(before[i],
                    map.GetPOI(SinkholeSites.All[i].X, SinkholeSites.All[i].Y),
                    SinkholeSites.All[i].Name + ": the heal touches nothing that exists");
        }

        [Test]
        public void Adversarial_ConnectionDedup_DifferentCellsAreNotDuplicates()
        {
            // Counter-check on the idempotency fix: two connections
            // between the SAME zones at DIFFERENT cells are two real
            // staircases and must both register. A buggy dedup keyed on
            // zone ids alone would eat the second staircase.
            var mgr = new ZoneManager(_factory, 42);
            mgr.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "A", SourceX = 5, SourceY = 5,
                TargetZoneID = "B", TargetX = 5, TargetY = 5, Type = "StairsDown",
            });
            mgr.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "A", SourceX = 60, SourceY = 20,
                TargetZoneID = "B", TargetX = 60, TargetY = 20, Type = "StairsDown",
            });
            Assert.AreEqual(2, mgr.GetConnectionsTo("B", "StairsDown").Count,
                "two staircases are two connections");
        }

        // ════════════════════════════════════════════════════════════
        //   Cross-actor — patience has edges
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SpeaksToHostiles_DoesNotWeakenTheNormalPath()
        {
            // The tag must be a pure widening: a NON-hostile listener
            // talking to a tagged speaker follows the normal path, and
            // an untagged hostile speaker still refuses. (The refusal
            // half lives in CatacombVillageTests.AtWar_TheWardenRefuses;
            // this is the widening half at neutral rep.)
            var zone = new Zone("PatientNeutral");
            FactionManager.Initialize();
            try
            {
                ConversationLoader.Reset();
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                    Application.dataPath, "Resources/Content/Conversations/RotChoir.json")));
                var elder = _factory.CreateEntity("EncasedElder");
                zone.AddEntity(elder, 10, 10);
                var player = new Entity { ID = "p", BlueprintName = "Player" };
                player.Tags["Player"] = ""; player.Tags["Creature"] = "";
                player.Tags["Faction"] = "Player";
                player.AddPart(new RenderPart { DisplayName = "you" });
                zone.AddEntity(player, 11, 10);

                Assert.IsTrue(ConversationManager.StartConversation(elder, player),
                    "neutral standing talks like it always did");
                ConversationManager.EndConversation();
            }
            finally { ConversationLoader.Reset(); }
        }

        private static Entity MakeVandal(Zone zone)
        {
            var vandal = new Entity { ID = "v", BlueprintName = "Vandal" };
            vandal.Tags["Creature"] = "";
            vandal.Tags["Player"] = "";
            vandal.Statistics["Hitpoints"] = new Stat
            { Owner = vandal, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            zone.AddEntity(vandal, 11, 10);
            return vandal;
        }
    }
}
