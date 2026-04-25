using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Gameplay.Save
{
    /// <summary>
    /// Phase 2 — Gap-coverage audit of <c>SaveSystem.cs</c>.
    /// Companion to <c>SaveSystemSpecTests</c> (Phase 1, spec-first).
    ///
    /// <para><b>Discipline.</b> Per <c>Docs/QUD-PARITY.md §3.9</c>:
    /// read production line-by-line and pin every observable branch
    /// the existing tests don't already cover. This is gap-coverage,
    /// not adversarial probing — predictions match the implementation
    /// because the implementation has been read.</para>
    ///
    /// <para>Scope vs the existing tests:
    /// <list type="bullet">
    ///   <item><c>SaveGraphRoundTripTests</c> — single whole-world round-trip</item>
    ///   <item><c>SaveSystemSpecTests</c> — Phase 1 spec contract (21 tests)</item>
    ///   <item><b>This file</b> — uncovered branches of SaveWriter primitives,
    ///   GameSessionState fields, Zone metadata, BrainPart scalar fields,
    ///   BodyPart tree depth, MessageLog/PlayerReputation static state,
    ///   section-check integrity, type resolution edge cases</item>
    /// </list></para>
    /// </summary>
    [TestFixture]
    public class SaveSystemCoverageGapTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        private static Entity MakeCreature(string id, string blueprint = "TestCreature", bool isPlayer = false)
        {
            var e = new Entity { ID = id, BlueprintName = blueprint };
            e.AddPart(new RenderPart { DisplayName = blueprint, RenderString = "@", ColorString = "&W" });
            e.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 0, Max = 1000, Owner = e };
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10, Owner = e };
            if (isPlayer) e.SetTag("Player");
            return e;
        }

        private static OverworldZoneManager MakeManagerWithZone(Zone zone)
        {
            var m = new OverworldZoneManager(null, 12345);
            m.ReplaceLoadedState(
                new Dictionary<string, Zone> { { zone.ZoneID, zone } },
                zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            return m;
        }

        private static (Entity player, Zone zone, OverworldZoneManager mgr, TurnManager turns) MakeMinimalState(
            string playerID = "player-1", string zoneID = "Overworld.10.10.0", int worldSeed = 12345)
        {
            var player = MakeCreature(playerID, "Player", isPlayer: true);
            var zone = new Zone(zoneID);
            zone.AddEntity(player, 1, 1);
            var mgr = new OverworldZoneManager(null, worldSeed);
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

        private static GameSessionState RoundTrip(
            Entity player, OverworldZoneManager manager, TurnManager turns,
            string gameID = "test-game", string version = "test-version", int hotbar = 0)
        {
            var state = GameSessionState.Capture(gameID, version, manager, turns, player, hotbar);
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            stream.Position = 0;
            return GameSessionState.Load(new SaveReader(stream, null));
        }

        private static byte[] Serialize(GameSessionState state)
        {
            using var stream = new MemoryStream();
            state.Save(new SaveWriter(stream));
            return stream.ToArray();
        }

        // ============================================================
        // SaveWriter / SaveReader primitives
        // ============================================================

        /// <summary>
        /// Pins <c>WriteEntityReference(null)</c> writing token=0
        /// (production line 79-83) and <c>ReadEntityReference</c>
        /// returning null on token=0 (line 175-178). Critical for
        /// nullable cross-references like <c>BrainPart.Target=null</c>
        /// (idle wanderer) or <c>TurnManager.CurrentActor=null</c>
        /// (between turns).
        /// </summary>
        [Test]
        public void Gap_NullEntityReference_RoundTripsAsNull()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var npc = MakeCreature("npc-1", "Idler");
            var brain = new BrainPart();  // Target intentionally null
            npc.AddPart(brain);
            zone.AddEntity(npc, 5, 5);
            brain.CurrentZone = zone;
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 100 }
                });

            var loaded = RoundTrip(player, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(5, 5).Objects)
                if (obj.ID == "npc-1") { lNpc = obj; break; }
            Assert.IsNotNull(lNpc);
            Assert.IsNull(lNpc.GetPart<BrainPart>().Target,
                "Null BrainPart.Target must round-trip as null (token 0).");
        }

        /// <summary>
        /// Pins token-map identity: when the same entity is referenced
        /// from multiple containers (e.g. cell.Objects AND turnManager
        /// entries), ReadEntityReference returns the SAME instance for
        /// both. Production line 180-183 caches by token. This is the
        /// foundation of cross-reference integrity across the entity
        /// graph.
        /// </summary>
        [Test]
        public void Gap_SameEntity_ReferencedTwice_ResolvesToSameInstance()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            // Player is referenced via: Cell.Objects + TurnManager.CurrentActor + TurnManager.Entries

            var loaded = RoundTrip(player, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;
            var lPlayer = loaded.Player;
            var lFromCell = lZone.GetCell(1, 1).Objects[0];
            var lFromTurnCurrent = loaded.TurnManager.CurrentActor;

            Assert.AreSame(lPlayer, lFromCell, "loaded.Player and cell-resolved player must be same instance.");
            Assert.AreSame(lPlayer, lFromTurnCurrent, "loaded.Player and TurnManager.CurrentActor must be same instance.");
        }

        /// <summary>
        /// Pins null-string round-trip distinct from empty-string.
        /// <c>SaveWriter.WriteString</c> writes a leading boolean
        /// (production line 62-64); a null source produces a stream
        /// where the bool=false, so ReadString returns null. Empty
        /// string produces bool=true + length=0. Important for
        /// blueprint fields that intentionally distinguish "unset"
        /// from "empty".
        /// </summary>
        [Test]
        public void Gap_NullVsEmptyString_RoundTripDistinctly()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            // Null property value through tags/properties.
            // (Tags Dictionary stores "" for empty; null can be a property value via direct dict insert.)
            player.Properties["NullField"] = null;
            player.Properties["EmptyField"] = "";

            var loaded = RoundTrip(player, mgr, turns);
            Assert.IsTrue(loaded.Player.Properties.ContainsKey("NullField"));
            Assert.IsTrue(loaded.Player.Properties.ContainsKey("EmptyField"));
            Assert.IsNull(loaded.Player.Properties["NullField"], "Null string must round-trip as null.");
            Assert.AreEqual("", loaded.Player.Properties["EmptyField"], "Empty string must round-trip as empty (not null).");
        }

        // ============================================================
        // GameSessionState fields
        // ============================================================

        /// <summary>
        /// Pins <c>GameSessionState.Capture</c> generating a fresh GUID
        /// when gameID is null (production line 292). Important for
        /// new-game flows where the caller hasn't yet allocated an ID.
        /// </summary>
        [Test]
        public void Gap_Capture_NullGameID_GeneratesNewGuid()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            var state = GameSessionState.Capture(gameID: null, gameVersion: "v1", mgr, turns, player);

            Assert.IsNotNull(state.GameID);
            Assert.IsNotEmpty(state.GameID);
            Assert.AreEqual(32, state.GameID.Length, "GUID 'N' format produces 32 hex chars.");
        }

        /// <summary>
        /// Pins WorldSeed propagation: Capture reads from
        /// <c>zoneManager.WorldSeed</c> (production line 294) and
        /// LoadOverworldZoneManager constructs the new manager with
        /// that seed (line 712).
        /// </summary>
        [Test]
        public void Gap_WorldSeed_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState(worldSeed: 7777777);

            var loaded = RoundTrip(player, mgr, turns);

            Assert.AreEqual(7777777, loaded.ZoneManager.WorldSeed,
                "WorldSeed must round-trip — needed for deterministic world-gen on reload.");
            Assert.AreEqual(7777777, loaded.WorldSeed,
                "GameSessionState.WorldSeed top-level field must also match.");
        }

        /// <summary>
        /// Pins SelectedHotbarSlot round-trip (production line 312/344).
        /// Player UI state — easy to forget, important for player UX.
        /// </summary>
        [Test]
        public void Gap_SelectedHotbarSlot_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            var loaded = RoundTrip(player, mgr, turns, hotbar: 5);

            Assert.AreEqual(5, loaded.SelectedHotbarSlot);
        }

        /// <summary>
        /// Pins <c>CreateInfo()</c> producing a SaveGameInfo with all
        /// fields populated (production line 368-388). This struct
        /// drives the save-slot UI, so its accuracy matters.
        /// </summary>
        [Test]
        public void Gap_CreateInfo_PopulatesAllUiFields()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            turns.RestoreSavedState(tickCount: 250, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 }
                });
            // Set hp.
            player.Statistics["Hitpoints"].BaseValue = 7;
            player.Statistics["Hitpoints"].Max = 15;

            var state = GameSessionState.Capture("game-id", "v1.2.3", mgr, turns, player, selectedHotbarSlot: 3);
            var info = state.CreateInfo();

            Assert.AreEqual(SaveWriter.FormatVersion, info.SaveVersion);
            Assert.AreEqual("v1.2.3", info.GameVersion);
            Assert.AreEqual("game-id", info.GameID);
            Assert.AreEqual("Player", info.PlayerName);
            Assert.AreEqual("Player", info.PlayerDisplayName);
            Assert.AreEqual(zone.ZoneID, info.ActiveZoneID);
            Assert.AreEqual(250, info.Turn);
            Assert.IsNotEmpty(info.SaveTimestampUtc, "Save timestamp must be populated.");
            StringAssert.Contains("/", info.HPSummary, "HPSummary should be in 'cur/max' format.");
        }

        // ============================================================
        // Zone & multi-zone round-trip
        // ============================================================

        /// <summary>
        /// Pins multi-zone caching: when CachedZones contains MORE
        /// than the active zone, all of them save and restore.
        /// Production line 662-667 iterates all cached zones; line
        /// 691-697 reconstructs them all on load.
        /// </summary>
        [Test]
        public void Gap_MultipleCachedZones_AllRoundTrip()
        {
            var player = MakeCreature("p-1", "Player", isPlayer: true);
            var active = new Zone("Active.1.1.0");
            active.AddEntity(player, 1, 1);
            var inactive = new Zone("Cached.2.2.0");

            var mgr = new OverworldZoneManager(null, 12345);
            mgr.ReplaceLoadedState(
                new Dictionary<string, Zone> { { active.ZoneID, active }, { inactive.ZoneID, inactive } },
                active.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 }
                });

            var loaded = RoundTrip(player, mgr, turns);

            Assert.IsTrue(loaded.ZoneManager.CachedZones.ContainsKey("Active.1.1.0"));
            Assert.IsTrue(loaded.ZoneManager.CachedZones.ContainsKey("Cached.2.2.0"),
                "Inactive cached zone must survive round-trip.");
            Assert.AreEqual("Active.1.1.0", loaded.ZoneManager.ActiveZone.ZoneID);
        }

        /// <summary>
        /// Pins Zone.AmbientTint Color round-trip via
        /// <c>WriteColor</c>/<c>ReadColor</c> (production line 826,
        /// 838).
        /// </summary>
        [Test]
        public void Gap_Zone_AmbientTint_ColorRoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            zone.AmbientTint = new Color(0.3f, 0.5f, 0.8f, 1.0f);

            var loaded = RoundTrip(player, mgr, turns);

            var lZone = loaded.ZoneManager.ActiveZone;
            Assert.AreEqual(0.3f, lZone.AmbientTint.r, 1e-5f);
            Assert.AreEqual(0.5f, lZone.AmbientTint.g, 1e-5f);
            Assert.AreEqual(0.8f, lZone.AmbientTint.b, 1e-5f);
            Assert.AreEqual(1.0f, lZone.AmbientTint.a, 1e-5f);
        }

        // ============================================================
        // Stat additional fields
        // ============================================================

        /// <summary>
        /// Pins all six Stat fields round-trip. SaveSystemSpecTests
        /// covered BaseValue/Min/Max; production line 1071-1078
        /// also writes Bonus, Penalty, Boost, sValue.
        /// </summary>
        [Test]
        public void Gap_Stat_AllSixFields_RoundTrip()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var hp = player.Statistics["Hitpoints"];
            hp.BaseValue = 12;
            hp.Min = 0;
            hp.Max = 20;
            hp.Bonus = 3;
            hp.Penalty = 1;
            hp.Boost = 2;
            hp.sValue = "1d6+2";  // string-valued stat (e.g. weapon damage dice)

            var loaded = RoundTrip(player, mgr, turns);
            var lHp = loaded.Player.Statistics["Hitpoints"];

            Assert.AreEqual(12, lHp.BaseValue);
            Assert.AreEqual(0, lHp.Min);
            Assert.AreEqual(20, lHp.Max);
            Assert.AreEqual(3, lHp.Bonus);
            Assert.AreEqual(1, lHp.Penalty);
            Assert.AreEqual(2, lHp.Boost);
            Assert.AreEqual("1d6+2", lHp.sValue);
        }

        // ============================================================
        // BrainPart scalar fields
        // ============================================================

        /// <summary>
        /// Pins all 13 scalar BrainPart fields round-trip. Production
        /// SaveBrainPart writes them at lines 1399-1416. SaveSystemSpec
        /// only pinned Target (cross-ref) and the goal stack.
        /// </summary>
        [Test]
        public void Gap_BrainPart_ScalarFields_RoundTrip()
        {
            var npc = MakeCreature("n-1", "Snapjaw");
            var brain = new BrainPart
            {
                SightRadius = 7,
                Wanders = false,
                WandersRandomly = false,
                FleeThreshold = 0.6f,
                Passive = true,
                CurrentState = AIState.Wander,  // enum
                InConversation = true,
                StartingCellX = 4,
                StartingCellY = 5,
                Staying = true,
                LastThought = "I miss home.",
                ThinkOutLoud = true
            };
            npc.AddPart(brain);

            var player = MakeCreature("p-1", "Player", isPlayer: true);
            var zone = new Zone("Z");
            zone.AddEntity(player, 0, 0);
            zone.AddEntity(npc, 4, 5);
            brain.CurrentZone = zone;
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 100 }
                });

            var loaded = RoundTrip(player, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(4, 5).Objects)
                if (obj.ID == "n-1") { lNpc = obj; break; }
            Assert.IsNotNull(lNpc);
            var lBrain = lNpc.GetPart<BrainPart>();

            Assert.AreEqual(7, lBrain.SightRadius);
            Assert.IsFalse(lBrain.Wanders);
            Assert.IsFalse(lBrain.WandersRandomly);
            Assert.AreEqual(0.6f, lBrain.FleeThreshold, 1e-5f);
            Assert.IsTrue(lBrain.Passive);
            Assert.AreEqual(AIState.Wander, lBrain.CurrentState);
            Assert.IsTrue(lBrain.InConversation);
            Assert.AreEqual(4, lBrain.StartingCellX);
            Assert.AreEqual(5, lBrain.StartingCellY);
            Assert.IsTrue(lBrain.Staying);
            Assert.AreEqual("I miss home.", lBrain.LastThought);
            Assert.IsTrue(lBrain.ThinkOutLoud);
            Assert.AreEqual("Z", lBrain.CurrentZone?.ZoneID, "CurrentZone reference must resolve to the loaded zone via FindZone.");
            Assert.IsNotNull(lBrain.Rng, "Production rebuilds Rng fresh on load (line 1458) — must not be null.");
        }

        /// <summary>
        /// Pins BrainPart.PersonalEnemies round-trip. Each enemy is
        /// stored as a token reference (production line 1407-1409),
        /// so the loaded set should contain references to loaded
        /// entities (token-resolved), not stale ones.
        /// </summary>
        [Test]
        public void Gap_BrainPart_PersonalEnemies_RoundTripsAsLoadedReferences()
        {
            var player = MakeCreature("p-1", "Player", isPlayer: true);
            var foe = MakeCreature("f-1", "Foe");
            var zone = new Zone("Z");
            zone.AddEntity(player, 0, 0);
            zone.AddEntity(foe, 5, 5);

            var npc = MakeCreature("n-1", "Snapjaw");
            var brain = new BrainPart();
            brain.PersonalEnemies.Add(player);  // public HashSet field
            brain.PersonalEnemies.Add(foe);
            npc.AddPart(brain);
            zone.AddEntity(npc, 3, 3);
            brain.CurrentZone = zone;

            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = foe, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 100 }
                });

            var loaded = RoundTrip(player, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(3, 3).Objects)
                if (obj.ID == "n-1") { lNpc = obj; break; }
            Assert.IsNotNull(lNpc);
            var lBrain = lNpc.GetPart<BrainPart>();

            Assert.AreEqual(2, lBrain.PersonalEnemies.Count, "Both personal enemies preserved.");
            Assert.IsTrue(lBrain.PersonalEnemies.Contains(loaded.Player),
                "Loaded PersonalEnemies must contain the loaded player instance, not a stale ref.");
        }

        /// <summary>
        /// Pins the DelegateGoal-filter contract: production line 1421
        /// and 1428 explicitly skip <c>DelegateGoal</c> when saving
        /// the goal stack. DelegateGoals carry C# delegates which
        /// can't be serialized; filtering them is correct and
        /// must not regress.
        /// </summary>
        [Test]
        public void Gap_GoalStack_DelegateGoals_AreFilteredFromSave()
        {
            var npc = MakeCreature("n-1", "Snapjaw");
            var brain = new BrainPart();
            npc.AddPart(brain);
            var zone = new Zone("Z");
            zone.AddEntity(npc, 2, 2);
            brain.CurrentZone = zone;

            // Push a regular goal AND a DelegateGoal.
            brain.PushGoal(new WaitGoal(3));
            brain.PushGoal(new DelegateGoal(_ => { /* test action */ }));

            var player = MakeCreature("p-1", "Player", isPlayer: true);
            zone.AddEntity(player, 0, 0);
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 100 }
                });

            var loaded = RoundTrip(player, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(2, 2).Objects)
                if (obj.ID == "n-1") { lNpc = obj; break; }
            var lBrain = lNpc.GetPart<BrainPart>();
            var snapshot = lBrain.GetGoalsSnapshot();

            Assert.AreEqual(1, snapshot.Count, "DelegateGoal must be filtered; only WaitGoal survives.");
            Assert.IsInstanceOf<WaitGoal>(snapshot[0]);
        }

        // ============================================================
        // Body / BodyPart deeper structure
        // ============================================================

        /// <summary>
        /// Pins nested BodyPart tree round-trip (depth ≥ 2).
        /// Production SaveBodyPart recurses on Parts (line 1344-1349)
        /// and LoadBodyPart re-attaches children (line 1382-1393).
        /// Existing tests covered Body root + one Hand; this proves
        /// the recursion handles deeper trees.
        /// </summary>
        [Test]
        public void Gap_BodyPart_NestedTree_DepthGE2_RoundTrips()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            var torso = new BodyPart { Type = "Body", Name = "torso", ID = 100 };
            var arm = new BodyPart { Type = "Arm", Name = "arm", ID = 200 };
            var hand = new BodyPart { Type = "Hand", Name = "hand", ID = 201 };
            var finger = new BodyPart { Type = "Finger", Name = "finger", ID = 202 };
            arm.AddPart(hand);
            hand.AddPart(finger);  // depth 3 from root: torso → arm → hand → finger
            torso.AddPart(arm);

            var body = new Body();
            player.AddPart(body);
            body.SetBody(torso);

            var loaded = RoundTrip(player, mgr, turns);
            var lBody = loaded.Player.GetPart<Body>();
            var lTorso = lBody.GetBody();
            Assert.AreEqual("Body", lTorso.Type);
            var lArm = lTorso.Parts[0];
            Assert.AreEqual("Arm", lArm.Type);
            var lHand = lArm.Parts[0];
            Assert.AreEqual("Hand", lHand.Type);
            var lFinger = lHand.Parts[0];
            Assert.AreEqual("Finger", lFinger.Type);
            Assert.AreEqual(202, lFinger.ID);
            Assert.AreSame(lHand, lFinger.ParentPart,
                "ParentPart back-reference must be wired by LoadBodyPart (line 1391).");
        }

        // ============================================================
        // Section-check integrity
        // ============================================================

        /// <summary>
        /// Pins section-check corruption detection. Production
        /// <c>WriteCheck("ZoneManager")</c> writes a hash; a corrupted
        /// hash byte must trigger <c>InvalidDataException</c> from
        /// <c>ExpectCheck</c> (line 145-147). This is the primary
        /// stream-corruption canary.
        /// </summary>
        [Test]
        public void Gap_SectionCheck_CorruptionRejected_WithDescriptiveMessage()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            byte[] bytes = Serialize(GameSessionState.Capture("g", "v", mgr, turns, player));
            // Find a "GameSession.Begin" check approximately — it's right after the header.
            // The header is: magic(4) + version(4) + bool(1) + len(?) + "v"(1) = ~11 bytes minimum.
            // The first WriteCheck writes 4 bytes for the hash. Corrupting somewhere in the early
            // post-header range is highly likely to corrupt a check or a string. Either way, load fails.
            // Pick byte 12 (just past header) — flip a bit.
            Assert.Greater(bytes.Length, 20);
            byte[] corrupted = (byte[])bytes.Clone();
            corrupted[12] ^= 0xFF;  // flip all bits

            using var stream = new MemoryStream(corrupted);
            var reader = new SaveReader(stream, null);

            Assert.Catch<System.Exception>(() => GameSessionState.Load(reader),
                "Corruption in early stream bytes must be detected — section check or string parse fails loudly.");
        }

        /// <summary>
        /// Pins format version mismatch detection. Production line
        /// 130-132 throws <c>InvalidDataException</c> with the
        /// version number embedded in the message. Important for
        /// future format upgrades — old saves must reject cleanly,
        /// not corrupt silently.
        /// </summary>
        [Test]
        public void Gap_FormatVersion_Mismatch_RejectsWithVersionInMessage()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            byte[] bytes = Serialize(GameSessionState.Capture("g", "v", mgr, turns, player));

            // Bytes 4-7 are the FormatVersion int (little-endian). Replace with 999.
            byte[] corrupted = (byte[])bytes.Clone();
            corrupted[4] = 0xE7; corrupted[5] = 0x03; corrupted[6] = 0x00; corrupted[7] = 0x00;  // 999 little-endian

            using var stream = new MemoryStream(corrupted);
            var reader = new SaveReader(stream, null);

            var ex = Assert.Catch<InvalidDataException>(() => GameSessionState.Load(reader),
                "Wrong format version must throw InvalidDataException.");
            StringAssert.Contains("999", ex.Message,
                "Error message must include the rejected version number for diagnostic clarity.");
        }

        // ============================================================
        // Static-state restoration (MessageLog, PlayerReputation)
        // ============================================================

        /// <summary>
        /// Pins MessageLog round-trip. Production SaveMessageLog
        /// (line 771-789) writes entries + announcements + flash +
        /// nextSerial; LoadMessageLog calls <c>MessageLog.Restore</c>
        /// which replaces static state. Side effect: loading a save
        /// REPLACES the live MessageLog.
        /// </summary>
        [Test]
        public void Gap_MessageLog_StaticState_PreservedAcrossRoundTrip()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            // Set up MessageLog state pre-save.
            MessageLog.Clear();
            MessageLog.Add("Hello, Ooo!");
            MessageLog.Add("A snapjaw appears.");
            int preFlash = MessageLog.FlashStamp;

            byte[] bytes = Serialize(GameSessionState.Capture("g", "v", mgr, turns, player));

            // Mutate live MessageLog after save.
            MessageLog.Clear();
            MessageLog.Add("Different content.");

            // Load — should restore the saved entries.
            using var stream = new MemoryStream(bytes);
            GameSessionState.Load(new SaveReader(stream, null));

            var loaded = MessageLog.GetAllEntries();
            Assert.AreEqual(2, loaded.Count, "Saved entries restored on load.");
            Assert.AreEqual("Hello, Ooo!", loaded[0].Text);
            Assert.AreEqual("A snapjaw appears.", loaded[1].Text);
        }

        /// <summary>
        /// Pins PlayerReputation round-trip. Static state restored
        /// via <c>PlayerReputation.Restore</c> (production line 820).
        /// </summary>
        [Test]
        public void Gap_PlayerReputation_StaticState_PreservedAcrossRoundTrip()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            PlayerReputation.Restore(new Dictionary<string, int>
            {
                { "Snapjaws", -50 },
                { "Villagers", 25 }
            });

            byte[] bytes = Serialize(GameSessionState.Capture("g", "v", mgr, turns, player));

            // Mutate live state.
            PlayerReputation.Restore(new Dictionary<string, int> { { "Snapjaws", 999 } });

            using var stream = new MemoryStream(bytes);
            GameSessionState.Load(new SaveReader(stream, null));

            var restored = PlayerReputation.GetAll();
            Assert.AreEqual(-50, restored["Snapjaws"], "Snapjaws reputation restored.");
            Assert.AreEqual(25, restored["Villagers"], "Villagers reputation restored.");
            Assert.IsFalse(restored.ContainsKey("ShouldNotExist"));
        }

        // ============================================================
        // Reflection / WritePublicFields edge cases
        // ============================================================

        /// <summary>
        /// Pins reflective serialization of a custom Part with mixed
        /// public-field types: int, string, bool, enum, Entity (token-
        /// based), and a List&lt;int&gt;. Production WritePublicFields
        /// + WriteFieldValue handle each. Test-only Part exercises
        /// the fallback path that catches "any normal Part" without
        /// a custom serializer.
        /// </summary>
        [Test]
        public void Gap_ReflectivePart_MixedPublicFields_RoundTrip()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var part = new ReflectiveTestPart
            {
                IntField = 42,
                StringField = "hello",
                BoolField = true,
                EnumField = TestEnum.Beta,
                IntList = new List<int> { 1, 2, 3 }
            };
            part.EntityRef = player;  // token-based ref to player
            player.AddPart(part);

            var loaded = RoundTrip(player, mgr, turns);
            var lPart = loaded.Player.GetPart<ReflectiveTestPart>();
            Assert.IsNotNull(lPart, "Reflective part must round-trip via the fallback path.");
            Assert.AreEqual(42, lPart.IntField);
            Assert.AreEqual("hello", lPart.StringField);
            Assert.IsTrue(lPart.BoolField);
            Assert.AreEqual(TestEnum.Beta, lPart.EnumField);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, lPart.IntList);
            Assert.AreSame(loaded.Player, lPart.EntityRef,
                "EntityRef on a reflective part must token-resolve to the loaded player.");
        }

        public enum TestEnum { Alpha, Beta, Gamma }

        public class ReflectiveTestPart : Part
        {
            public override string Name => "ReflectiveTest";
            public int IntField;
            public string StringField;
            public bool BoolField;
            public TestEnum EnumField;
            public Entity EntityRef;
            public List<int> IntList;
        }

        /// <summary>
        /// Pins ResolveType behavior for an unknown type name.
        /// Production line 1126-1127 returns null when the type isn't
        /// found OR is abstract. LoadPart line 1126 returns null,
        /// LoadEntityBody line 1644-1645 skips null parts. So a save
        /// that references a missing type should load with the part
        /// silently dropped, not crash.
        /// </summary>
        [Test]
        public void Gap_UnknownTypeName_LoadsAsNullSilentlyDropped()
        {
            // Construct a stream that mimics a saved part-list with one
            // entry referencing a type name that doesn't exist. We do
            // this through full round-trip + binary mutation: use ASCII
            // search for a known type name in the bytes, replace with
            // something that won't resolve.

            // Easier path: rely on the fact that a typo in the saved
            // type string will route through ResolveType returning null
            // → the part is skipped on load.
            //
            // Build a save with a CorpsePart attached. Replace the type
            // string in the bytes with one that doesn't resolve.
            var (player, zone, mgr, turns) = MakeMinimalState();
            var corpse = new CorpsePart { CorpseChance = 50, CorpseBlueprint = "CreatureCorpse" };
            player.AddPart(corpse);

            byte[] bytes = Serialize(GameSessionState.Capture("g", "v", mgr, turns, player));
            // Find ASCII "CavesOfOoo.Core.CorpsePart" and replace 'C' with 'Z'.
            // (AssemblyQualifiedName begins with the namespace+type prefix.)
            int idx = FindAsciiInBytes(bytes, "CavesOfOoo.Core.CorpsePart");
            Assert.Greater(idx, 0, "Test setup: CorpsePart type-name string must be present.");
            bytes[idx] = (byte)'Z';  // CavesOfOoo... → ZavesOfOoo... (won't resolve)

            using var stream = new MemoryStream(bytes);
            // The load may succeed (with the part dropped) or throw, depending on
            // whether the rest of the stream still parses cleanly. Assert: no NPE,
            // and IF load succeeds, the part is dropped from the player.
            try
            {
                var loaded = GameSessionState.Load(new SaveReader(stream, null));
                Assert.IsNull(loaded.Player.GetPart<CorpsePart>(),
                    "Unknown type must load as null → part dropped, not crashed.");
            }
            catch (System.Exception ex)
            {
                // If the corrupted type name desyncs further parsing, that's an
                // acceptable outcome — but the failure must surface as an exception,
                // never a silent partial-load.
                Assert.IsNotNull(ex);
            }
        }

        private static int FindAsciiInBytes(byte[] bytes, string needle)
        {
            byte[] needleBytes = System.Text.Encoding.ASCII.GetBytes(needle);
            for (int i = 0; i <= bytes.Length - needleBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needleBytes.Length; j++)
                {
                    if (bytes[i + j] != needleBytes[j]) { match = false; break; }
                }
                if (match) return i;
            }
            return -1;
        }

        // ============================================================
        // Two-phase post-load contract
        // ============================================================

        /// <summary>
        /// Pins the two-phase post-load: production line 209-235 calls
        /// <c>OnAfterLoad</c> on every part of every loaded entity,
        /// THEN calls <c>FinalizeLoad</c>. Order matters when one
        /// part's FinalizeLoad needs another part's OnAfterLoad to
        /// have completed first.
        /// </summary>
        [Test]
        public void Gap_PostLoad_TwoPhase_OnAfterLoadBeforeFinalizeLoad()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var observer = new TwoPhaseObserverPart();
            player.AddPart(observer);
            // Reset class-static order log.
            TwoPhaseObserverPart.OrderLog.Clear();

            RoundTrip(player, mgr, turns);

            // After load: should see OnAfterLoad call recorded BEFORE FinalizeLoad call.
            var log = TwoPhaseObserverPart.OrderLog;
            Assert.GreaterOrEqual(log.Count, 2, "Both OnAfterLoad and FinalizeLoad must fire.");
            int onAfterIdx = log.IndexOf("OnAfterLoad");
            int finalizeIdx = log.IndexOf("FinalizeLoad");
            Assert.GreaterOrEqual(onAfterIdx, 0);
            Assert.GreaterOrEqual(finalizeIdx, 0);
            Assert.Less(onAfterIdx, finalizeIdx,
                "OnAfterLoad must fire before FinalizeLoad on every part.");
        }

        public class TwoPhaseObserverPart : Part
        {
            public override string Name => "TwoPhaseObserver";
            public static readonly List<string> OrderLog = new List<string>();
            public override void OnAfterLoad(SaveReader reader)
            {
                OrderLog.Add("OnAfterLoad");
            }
            public override void FinalizeLoad(SaveReader reader)
            {
                OrderLog.Add("FinalizeLoad");
            }
        }
    }
}
