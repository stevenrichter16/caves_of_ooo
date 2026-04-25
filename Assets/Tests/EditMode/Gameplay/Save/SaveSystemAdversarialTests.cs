using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.Save
{
    /// <summary>
    /// Phase 3 — Adversarial cold-eye audit of <c>SaveSystem.cs</c>.
    /// Companion to Phase 1 (spec-first) + Phase 2 (gap-coverage).
    /// Per <c>Docs/QUD-PARITY.md §3.9</c>.
    ///
    /// <para><b>Discipline.</b> Predictions made BEFORE re-reading the
    /// production for these specific edges (I have prior context from
    /// P1/P2 reads, but each test below targets a behavior I have NOT
    /// yet pinned). Each test labels PRED + CONFIDENCE; failures
    /// classify into test-wrong / code-wrong / spec-gap.</para>
    ///
    /// <para><b>Targets selected for highest information value</b>:
    /// numerical extremes, cyclic + self-referential entity graphs,
    /// stream/format edges (trailing garbage, double-save, end-check
    /// corruption), polymorphic typed-objects, and the
    /// "round-tripped state is itself resaveable" idempotency
    /// extension.</para>
    /// </summary>
    [TestFixture]
    public class SaveSystemAdversarialTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        private static Entity MakeCreature(string id, string blueprint = "TestCreature")
        {
            var e = new Entity { ID = id, BlueprintName = blueprint };
            e.AddPart(new RenderPart { DisplayName = blueprint, RenderString = "@", ColorString = "&W" });
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 0, Max = 1000, Owner = e };
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10, Owner = e };
            return e;
        }

        private static (Entity player, Zone zone, OverworldZoneManager mgr, TurnManager turns) MakeMinimalState()
        {
            var player = MakeCreature("player-1", "Player");
            player.SetTag("Player");
            var zone = new Zone("Z");
            zone.AddEntity(player, 1, 1);
            var mgr = new OverworldZoneManager(null, 12345);
            mgr.ReplaceLoadedState(
                new Dictionary<string, Zone> { { zone.ZoneID, zone } },
                zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 }
                });
            return (player, zone, mgr, turns);
        }

        private static byte[] Serialize(GameSessionState state)
        {
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            return stream.ToArray();
        }

        private static GameSessionState RoundTrip(GameSessionState state)
        {
            byte[] bytes = Serialize(state);
            using var stream = new MemoryStream(bytes);
            return GameSessionState.Load(new SaveReader(stream, null));
        }

        // ============================================================
        // A. Numerical edges
        // ============================================================

        /// <summary>
        /// PRED: Float NaN and ±Infinity round-trip exactly. Production
        /// SaveStat (line 1071-1078) uses BinaryWriter.Write(float)
        /// which writes raw IEEE-754 bits. ReadFloat reads them back.
        /// CONFIDENCE: HIGH — binary preservation is the floor.
        /// Why probe: stats touched by mods can theoretically end up
        /// with NaN/Inf via division-by-zero or saturation arithmetic.
        /// </summary>
        [Test]
        public void Adv_Stat_FloatNaNAndInfinity_RoundTripExactly()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            // Stat doesn't have a float field (BaseValue/Min/Max are ints), but
            // FleeThreshold on BrainPart is float — exercise that path.
            var npc = MakeCreature("n-1", "Snapjaw");
            var brain = new BrainPart { FleeThreshold = float.NaN };
            npc.AddPart(brain);
            zone.AddEntity(npc, 5, 5);
            brain.CurrentZone = zone;
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 100 }
                });

            var state = GameSessionState.Capture("g", "v", mgr, turns, player);
            var loaded = RoundTrip(state);
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(5, 5).Objects)
                if (obj.ID == "n-1") { lNpc = obj; break; }
            Assert.IsTrue(float.IsNaN(lNpc.GetPart<BrainPart>().FleeThreshold),
                "NaN must round-trip as NaN exactly.");

            // Now re-test with +Infinity.
            brain.FleeThreshold = float.PositiveInfinity;
            var loaded2 = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));
            var lZone2 = loaded2.ZoneManager.ActiveZone;
            Entity lNpc2 = null;
            foreach (var obj in lZone2.GetCell(5, 5).Objects)
                if (obj.ID == "n-1") { lNpc2 = obj; break; }
            Assert.IsTrue(float.IsPositiveInfinity(lNpc2.GetPart<BrainPart>().FleeThreshold),
                "+Infinity must round-trip exactly.");
        }

        /// <summary>
        /// PRED: Stat with negative BaseValue and negative Min/Max
        /// round-trips. SaveStat writes raw ints — no validation.
        /// CONFIDENCE: HIGH. Probes that nothing in production
        /// silently clamps to >= 0.
        /// </summary>
        [Test]
        public void Adv_Stat_NegativeValues_RoundTrip()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            // Custom stat — negative BaseValue, negative Min, negative Max.
            player.Statistics["Charge"] = new Stat
            {
                Name = "Charge",
                BaseValue = -42,
                Min = -100,
                Max = -1,
                Bonus = -5,
                Penalty = -3,
                Boost = -7,
                Owner = player
            };

            var loaded = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));
            var charge = loaded.Player.Statistics["Charge"];

            Assert.AreEqual(-42, charge.BaseValue);
            Assert.AreEqual(-100, charge.Min);
            Assert.AreEqual(-1, charge.Max);
            Assert.AreEqual(-5, charge.Bonus);
            Assert.AreEqual(-3, charge.Penalty);
            Assert.AreEqual(-7, charge.Boost);
        }

        /// <summary>
        /// PRED: Goal with negative Age round-trips as-is. Production
        /// SaveGoal writes <c>Age</c> as raw int (line 1476). No
        /// validation. CONFIDENCE: HIGH.
        /// Why probe: Age is incremented by BrainPart's tick loop;
        /// nothing in production produces negative Age, but the save
        /// system shouldn't add validation that masks a stack-overflow
        /// underflow if it ever happens.
        /// </summary>
        [Test]
        public void Adv_Goal_NegativeAge_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var npc = MakeCreature("n-1", "Snapjaw");
            var brain = new BrainPart();
            npc.AddPart(brain);
            zone.AddEntity(npc, 3, 3);
            brain.CurrentZone = zone;
            var goal = new WaitGoal(5);
            // Force Age negative via reflection (production never does this, but a future bug might).
            typeof(GoalHandler).GetField("Age").SetValue(goal, -100);
            brain.PushGoal(goal);

            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 100 }
                });

            var loaded = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(3, 3).Objects)
                if (obj.ID == "n-1") { lNpc = obj; break; }
            var lGoal = lNpc.GetPart<BrainPart>().GetGoalsSnapshot()[0];
            Assert.AreEqual(-100, lGoal.Age, "Negative Age must round-trip — no silent clamping.");
        }

        // ============================================================
        // B. Reference graph edges
        // ============================================================

        /// <summary>
        /// PRED: Cyclic entity reference round-trips correctly via the
        /// token map. A.Target = B; B.Target = A — both tokens
        /// allocated, both bodies serialized in the queue, references
        /// resolve correctly on load. CONFIDENCE: HIGH (token system
        /// designed for this), but worth pinning since it's the most
        /// classic graph-serialization footgun.
        /// </summary>
        [Test]
        public void Adv_CyclicEntityReference_RoundTrips_NoStackOverflow()
        {
            var player = MakeCreature("p-1", "Player");
            player.SetTag("Player");
            var npcA = MakeCreature("a-1", "Snapjaw");
            var npcB = MakeCreature("b-1", "Snapjaw");
            var brainA = new BrainPart { Target = npcB };  // A → B
            var brainB = new BrainPart { Target = npcA };  // B → A (cycle)
            npcA.AddPart(brainA);
            npcB.AddPart(brainB);

            var zone = new Zone("Z");
            zone.AddEntity(player, 1, 1);
            zone.AddEntity(npcA, 5, 5);
            zone.AddEntity(npcB, 6, 6);
            brainA.CurrentZone = zone;
            brainB.CurrentZone = zone;

            var mgr = new OverworldZoneManager(null, 12345);
            mgr.ReplaceLoadedState(
                new Dictionary<string, Zone> { { zone.ZoneID, zone } },
                zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npcA, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npcB, Energy = 100 }
                });

            GameSessionState loaded = null;
            Assert.DoesNotThrow(() =>
                loaded = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player)),
                "Cyclic entity ref must NOT cause stack overflow / infinite loop.");
            Assert.IsNotNull(loaded);

            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lA = null, lB = null;
            foreach (var obj in lZone.GetCell(5, 5).Objects) if (obj.ID == "a-1") lA = obj;
            foreach (var obj in lZone.GetCell(6, 6).Objects) if (obj.ID == "b-1") lB = obj;
            Assert.IsNotNull(lA);
            Assert.IsNotNull(lB);
            Assert.AreSame(lB, lA.GetPart<BrainPart>().Target,
                "A.Target must point to loaded B (not stale ref).");
            Assert.AreSame(lA, lB.GetPart<BrainPart>().Target,
                "B.Target must point to loaded A (cycle preserved).");
        }

        /// <summary>
        /// PRED: Self-referential entity (E.Target = E) round-trips:
        /// E is added to the token map on first encounter; the
        /// self-reference resolves to the same loaded instance.
        /// CONFIDENCE: HIGH.
        /// </summary>
        [Test]
        public void Adv_SelfReferentialEntity_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var brain = new BrainPart();
            player.AddPart(brain);
            brain.Target = player;  // self-reference
            brain.CurrentZone = zone;

            var loaded = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));
            var lPlayer = loaded.Player;
            Assert.AreSame(lPlayer, lPlayer.GetPart<BrainPart>().Target,
                "Self-reference must resolve to the same loaded instance.");
        }

        /// <summary>
        /// PRED: TurnManager.CurrentActor can legally point to an
        /// entity that is NOT in the entries list (e.g., a quasi-actor
        /// that holds the turn but isn't on the queue). Production
        /// SaveTurnManager (line 736) writes CurrentActor as a token
        /// reference independently of entries. Should round-trip. The
        /// token map ensures the actor's body is serialized regardless.
        /// CONFIDENCE: MEDIUM — depends on whether the actor is
        /// reachable through any other path; if not, the token gets
        /// allocated but the body might not be queued via
        /// WriteQueuedEntityBodies.
        /// </summary>
        [Test]
        public void Adv_TurnManager_CurrentActorNotInEntries_StillRoundTrips()
        {
            var player = MakeCreature("p-1", "Player");
            player.SetTag("Player");
            var ghost = MakeCreature("g-1", "GhostActor");  // current actor but NOT in entries

            var zone = new Zone("Z");
            zone.AddEntity(player, 1, 1);
            // Ghost intentionally NOT in zone — its only reference is via TurnManager.CurrentActor.
            // Wait — without zone presence, the only reference path is through CurrentActor itself.
            // For a meaningful test, add ghost to zone too so its body has a reachable path.
            zone.AddEntity(ghost, 2, 2);

            var mgr = new OverworldZoneManager(null, 12345);
            mgr.ReplaceLoadedState(
                new Dictionary<string, Zone> { { zone.ZoneID, zone } },
                zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());

            var turns = new TurnManager();
            // CurrentActor is the ghost, but entries list contains only the player.
            turns.RestoreSavedState(0, true, ghost,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 }
                });

            var loaded = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));
            Assert.IsNotNull(loaded.TurnManager.CurrentActor,
                "CurrentActor must be non-null after load even when not in entries.");
            Assert.AreEqual("g-1", loaded.TurnManager.CurrentActor.ID);
            // Ghost is also in the loaded zone (because we placed it there).
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lGhost = null;
            foreach (var obj in lZone.GetCell(2, 2).Objects) if (obj.ID == "g-1") lGhost = obj;
            Assert.IsNotNull(lGhost);
            Assert.AreSame(lGhost, loaded.TurnManager.CurrentActor,
                "CurrentActor token must resolve to the same instance as the loaded zone-occupant.");
        }

        // ============================================================
        // C. Stream / format edges
        // ============================================================

        /// <summary>
        /// PRED: A save with an empty cell (zero objects) followed by
        /// a cell with many objects round-trips correctly. Production
        /// SaveCell writes <c>Objects.Count</c> then iterates;
        /// LoadCell reads count then iterates. Boundary: count=0
        /// loop body never executes. CONFIDENCE: HIGH.
        /// </summary>
        [Test]
        public void Adv_Cell_ZeroObjectsThenManyObjects_BothPreserved()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            // Cell (1,1) has player. Cell (5,5) is empty. Cell (10,10) has many entities.
            for (int i = 0; i < 8; i++)
            {
                var item = MakeCreature($"item-{i}", "TestItem");
                zone.AddEntity(item, 10, 10);
            }

            var loaded = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));
            var lZone = loaded.ZoneManager.ActiveZone;

            Assert.AreEqual(0, lZone.GetCell(5, 5).Objects.Count, "Empty cell stays empty.");
            Assert.AreEqual(8, lZone.GetCell(10, 10).Objects.Count, "Cell with 8 objects preserves all 8.");
            Assert.AreEqual(1, lZone.GetCell(1, 1).Objects.Count, "Player cell preserves the player.");
        }

        /// <summary>
        /// PRED: Trailing garbage after a valid save is IGNORED — load
        /// reads sections, hits End check, returns. The MemoryStream
        /// position is past End-check; trailing bytes are unread.
        /// CONFIDENCE: MEDIUM. Plausible alternative: load completes
        /// and the validation just stops at the End marker — any
        /// trailing data is invisible to the load logic.
        /// </summary>
        [Test]
        public void Adv_TrailingGarbageAfterEndCheck_Ignored()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            byte[] valid = Serialize(GameSessionState.Capture("g", "v", mgr, turns, player));

            // Append 64 bytes of garbage after the valid save.
            var concat = new byte[valid.Length + 64];
            System.Array.Copy(valid, concat, valid.Length);
            var rng = new System.Random(42);
            for (int i = valid.Length; i < concat.Length; i++)
                concat[i] = (byte)rng.Next(256);

            using var stream = new MemoryStream(concat);
            GameSessionState loaded = null;
            Assert.DoesNotThrow(() => loaded = GameSessionState.Load(new SaveReader(stream, null)),
                "Trailing garbage past End check must NOT corrupt the load.");
            Assert.IsNotNull(loaded);
            Assert.AreEqual("player-1", loaded.Player.ID, "Loaded state intact despite trailing garbage.");
        }

        /// <summary>
        /// PRED: Two consecutive saves to the same MemoryStream
        /// produce a sequential byte stream where the SECOND save
        /// follows the FIRST. Loading from offset 0 reads the first
        /// save; the stream position after that is at the start of
        /// the second save. CONFIDENCE: LOW — this is a probe; the
        /// SaveWriter doesn't reset state between saves, so the
        /// entity-token counter would carry over and the second save's
        /// header would still be COO0 magic. Genuinely unsure of
        /// outcome.
        /// </summary>
        [Test]
        public void Adv_TwoSavesToSameStream_FirstSaveLoadsCorrectly()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            var state = GameSessionState.Capture("g", "v", mgr, turns, player);

            state.Save(writer);
            int firstSaveEnd = (int)stream.Position;
            // Second save into same writer/stream — simulates user mistake (no reset).
            // Use a fresh writer for the second to avoid token-map carryover assumptions —
            // actually the test point is to use the SAME writer and see what happens.
            state.Save(writer);
            int totalLength = (int)stream.Position;

            Assert.Greater(totalLength, firstSaveEnd, "Second save appended to stream.");

            // Load from offset 0 — should get the first save back.
            stream.Position = 0;
            GameSessionState loaded = null;
            Assert.DoesNotThrow(() => loaded = GameSessionState.Load(new SaveReader(stream, null)),
                "Loading from offset 0 must read the first save cleanly.");
            Assert.AreEqual("player-1", loaded.Player.ID);
        }

        /// <summary>
        /// PRED: The final <c>"GameSession.End"</c> check is the last
        /// thing read in <c>GameSessionState.Load</c> (production line
        /// 364). Corrupting that 4-byte hash must trigger
        /// <c>InvalidDataException</c>. CONFIDENCE: HIGH — this is
        /// the last-line-of-defense check.
        /// </summary>
        [Test]
        public void Adv_EndCheck_Corruption_Rejected()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            byte[] bytes = Serialize(GameSessionState.Capture("g", "v", mgr, turns, player));

            // Corrupt the LAST 4 bytes (the End check is the final write).
            byte[] corrupted = (byte[])bytes.Clone();
            int n = corrupted.Length;
            corrupted[n - 1] ^= 0xFF;
            corrupted[n - 2] ^= 0xFF;
            corrupted[n - 3] ^= 0xFF;
            corrupted[n - 4] ^= 0xFF;

            using var stream = new MemoryStream(corrupted);
            Assert.Catch<InvalidDataException>(
                () => GameSessionState.Load(new SaveReader(stream, null)),
                "Corrupting the GameSession.End check must throw InvalidDataException.");
        }

        // ============================================================
        // D. Polymorphism + idempotency
        // ============================================================

        /// <summary>
        /// PRED: A part with a base-typed reference field (e.g.
        /// <c>Effect</c>) holding a derived instance (e.g.
        /// <c>SmolderingEffect</c>) round-trips with the concrete
        /// type intact. Production WriteTypedObject (line 1762)
        /// writes the concrete type name when the actual type
        /// differs from declared type; ReadTypedObject (line 1780)
        /// resolves it. CONFIDENCE: MEDIUM — the path is implemented
        /// but harder to exercise; this test creates a specific
        /// scenario.
        /// </summary>
        [Test]
        public void Adv_PolymorphicTypedObject_RoundTripPreservesConcreteType()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var holder = new PolymorphicHolderPart();
            holder.PayloadByBaseType = new DerivedPayload { CustomField = "concrete-instance" };
            player.AddPart(holder);

            var loaded = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));
            var lHolder = loaded.Player.GetPart<PolymorphicHolderPart>();
            Assert.IsNotNull(lHolder);
            Assert.IsNotNull(lHolder.PayloadByBaseType, "Payload must round-trip.");
            Assert.IsInstanceOf<DerivedPayload>(lHolder.PayloadByBaseType,
                "Concrete type must be preserved (WriteTypedObject path).");
            Assert.AreEqual("concrete-instance",
                ((DerivedPayload)lHolder.PayloadByBaseType).CustomField);
        }

        public class PolymorphicHolderPart : Part
        {
            public override string Name => "PolymorphicHolder";
            // Field declared as BasePayload, but holds DerivedPayload instances at runtime.
            public BasePayload PayloadByBaseType;
        }
        public class BasePayload { public int BaseField; }
        public class DerivedPayload : BasePayload { public string CustomField; }

        /// <summary>
        /// PRED: A round-tripped GameSessionState is itself fully
        /// serializable — saving the loaded state produces a valid
        /// new save that round-trips again. Strengthens the Phase 1
        /// idempotency test (which compared bytes after exactly one
        /// load) by adding a second cycle. CONFIDENCE: HIGH but
        /// worth pinning — a bug here would be subtle (e.g.
        /// loaded entities missing some setup needed for save).
        /// </summary>
        [Test]
        public void Adv_LoadedState_IsResaveable_AndRoundTripsAgain()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.SetTag("Faction", "TestFaction");
            player.Properties["Mood"] = "stoic";

            var loaded1 = RoundTrip(GameSessionState.Capture("g", "v", mgr, turns, player));

            // Save the loaded state.
            byte[] bytes2 = Serialize(loaded1);

            // Load again.
            using var stream = new MemoryStream(bytes2);
            GameSessionState loaded2 = null;
            Assert.DoesNotThrow(() => loaded2 = GameSessionState.Load(new SaveReader(stream, null)),
                "Saving a loaded state must produce a valid new save.");
            Assert.IsNotNull(loaded2);

            Assert.AreEqual("player-1", loaded2.Player.ID);
            Assert.AreEqual("TestFaction", loaded2.Player.Tags["Faction"]);
            Assert.AreEqual("stoic", loaded2.Player.Properties["Mood"]);
        }
    }
}
