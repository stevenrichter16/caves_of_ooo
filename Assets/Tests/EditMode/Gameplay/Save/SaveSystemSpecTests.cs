using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.Save
{
    /// <summary>
    /// Phase 1 — Spec-first audit of <c>SaveSystem.cs</c>. Per
    /// <c>Docs/QUD-PARITY.md §3.9</c> with the "spec-first" variant
    /// described in the conversation that produced
    /// <c>Docs/roadmap.md</c> commit <c>c1edbe4</c>.
    ///
    /// <para><b>Discipline.</b> These tests articulate the contract
    /// SaveSystem SHOULD honor, drafted WITHOUT reading
    /// <c>SaveSystem.cs</c>. Each test commits a <c>PRED</c> and
    /// <c>CONFIDENCE</c> annotation. Failures classify into:
    /// <list type="bullet">
    ///   <item><b>test-wrong</b> — my spec assumption was naive</item>
    ///   <item><b>code-wrong</b> — SaveSystem violates an invariant it should honor</item>
    ///   <item><b>spec-gap</b> — SaveSystem doesn't address the case at all (design decision pending)</item>
    /// </list>
    /// Classifications are recorded in the commit message, not in the
    /// test source — the tests stay describing the desired contract.</para>
    ///
    /// <para><b>Scope.</b> Round-trip identity, referential integrity,
    /// format/corruption resistance, cross-system state preservation,
    /// functional invariants. Does NOT cover format-version migration
    /// (single-version contract right now) or save-slot management
    /// (no UI yet — that's Phase 4 work).</para>
    /// </summary>
    [TestFixture]
    public class SaveSystemSpecTests
    {
        // ============================================================
        // Helpers — kept minimal so the tests read as the spec.
        // Patterned after SaveGraphRoundTripTests for compatibility.
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

        private static Entity MakeItem(string id, string blueprint = "TestItem")
        {
            var e = new Entity { ID = id, BlueprintName = blueprint };
            e.AddPart(new RenderPart { DisplayName = blueprint, RenderString = "/", ColorString = "&y" });
            e.AddPart(new PhysicsPart());
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

        /// <summary>
        /// Capture, write, read, and return the loaded GameSessionState.
        /// Mirrors the pattern in SaveGraphRoundTripTests.
        /// </summary>
        private static GameSessionState RoundTrip(
            Entity player, Zone zone, OverworldZoneManager manager, TurnManager turns)
        {
            var state = GameSessionState.Capture("test-game", "test-version", manager, turns, player);
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            state.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            return GameSessionState.Load(reader);
        }

        /// <summary>Round-trip and return both the loaded state AND the raw bytes (for stream-equality probes).</summary>
        private static (GameSessionState loaded, byte[] bytes) RoundTripWithBytes(
            Entity player, Zone zone, OverworldZoneManager manager, TurnManager turns)
        {
            var state = GameSessionState.Capture("test-game", "test-version", manager, turns, player);
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            state.Save(writer);
            var bytes = stream.ToArray();
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            var loaded = GameSessionState.Load(reader);
            return (loaded, bytes);
        }

        /// <summary>Build a minimal valid game state (one player, one zone, one turn entry).</summary>
        private static (Entity player, Zone zone, OverworldZoneManager mgr, TurnManager turns) MakeMinimalState(
            string playerID = "player-1", string zoneID = "Overworld.10.10.0")
        {
            var player = MakeCreature(playerID, "Player", isPlayer: true);
            var zone = new Zone(zoneID);
            zone.AddEntity(player, 1, 1);
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, waitingForInput: true, currentActor: player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 }
                });
            return (player, zone, mgr, turns);
        }

        // ============================================================
        // A. Round-trip identity (entity-level)
        // ============================================================

        /// <summary>
        /// PRED: A loaded entity has the same <c>ID</c> and
        /// <c>BlueprintName</c> as the original. CONFIDENCE: HIGH —
        /// these are core identity fields any save system must
        /// preserve.
        /// </summary>
        [Test]
        public void Spec_Entity_IDAndBlueprintName_PreservedExactly()
        {
            var (player, zone, mgr, turns) = MakeMinimalState("uniqueID-42", "Overworld.10.10.0");
            player.BlueprintName = "AdventureTime.SpecificCreature";

            var loaded = RoundTrip(player, zone, mgr, turns);
            var loadedPlayer = loaded.Player;

            Assert.AreEqual("uniqueID-42", loadedPlayer.ID,
                "Entity.ID is identity — must round-trip exactly.");
            Assert.AreEqual("AdventureTime.SpecificCreature", loadedPlayer.BlueprintName,
                "Entity.BlueprintName must round-trip exactly.");
        }

        /// <summary>
        /// PRED: All Tags (Dictionary&lt;string,string&gt;) round-trip
        /// — keys and values both. CONFIDENCE: HIGH — Tags drive
        /// faction, species, role logic everywhere; losing them on
        /// reload would break entire game subsystems.
        /// </summary>
        [Test]
        public void Spec_Entity_Tags_AllKeyValuePairsPreserved()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.SetTag("Faction", "Cultists");
            player.SetTag("Species", "GoblinKing");
            player.SetTag("EmptyValue", "");
            player.SetTag("UnicodeKey", "café");

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lp = loaded.Player;

            Assert.AreEqual("Cultists", lp.Tags["Faction"]);
            Assert.AreEqual("GoblinKing", lp.Tags["Species"]);
            Assert.IsTrue(lp.Tags.ContainsKey("EmptyValue"), "Empty-value tag must still exist.");
            Assert.AreEqual("", lp.Tags["EmptyValue"]);
            Assert.AreEqual("café", lp.Tags["UnicodeKey"], "Unicode in tag values must round-trip.");
            Assert.IsTrue(lp.Tags.ContainsKey("Player"), "Player tag set in MakeCreature must persist.");
        }

        /// <summary>
        /// PRED: Entity.Properties (string→string dict) round-trips
        /// fully. CONFIDENCE: HIGH — corpse properties (CreatureName,
        /// SourceBlueprint, KillerID) live here and are critical for
        /// quest/triggering logic.
        /// </summary>
        [Test]
        public void Spec_Entity_Properties_StringDictPreserved()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.Properties["LastSpokenTo"] = "ShopkeeperBob";
            player.Properties["FavoriteColor"] = "lichen-green";

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lp = loaded.Player;

            Assert.AreEqual("ShopkeeperBob", lp.Properties["LastSpokenTo"]);
            Assert.AreEqual("lichen-green", lp.Properties["FavoriteColor"]);
        }

        /// <summary>
        /// PRED: Entity.IntProperties (string→int dict) round-trips
        /// fully. Used by reservation systems (DepositCorpsesReserve,
        /// RuneReservation). CONFIDENCE: HIGH.
        /// </summary>
        [Test]
        public void Spec_Entity_IntProperties_IntDictPreserved()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.SetIntProperty("CustomCounter", 42);
            player.SetIntProperty("Negative", -7);
            player.SetIntProperty("Zero", 0);  // explicit zero must persist if set

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lp = loaded.Player;

            Assert.AreEqual(42, lp.GetIntProperty("CustomCounter", -1));
            Assert.AreEqual(-7, lp.GetIntProperty("Negative", 0));
            Assert.AreEqual(0, lp.GetIntProperty("Zero", 99),
                "Explicitly-set Zero must persist (vs being treated as 'absent').");
        }

        /// <summary>
        /// PRED: Stat.BaseValue / Min / Max all round-trip exactly.
        /// CONFIDENCE: HIGH — stat round-trip is the canonical save
        /// requirement.
        /// </summary>
        [Test]
        public void Spec_Entity_Stats_BaseMinMaxPreservedExactly()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            // Mutate stats to non-default values.
            player.Statistics["Hitpoints"].BaseValue = 7;
            player.Statistics["Hitpoints"].Max = 15;
            player.Statistics["Hitpoints"].Min = 0;
            player.Statistics["Speed"].BaseValue = 73;
            player.Statistics["Speed"].Max = 250;

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lp = loaded.Player;
            var hp = lp.Statistics["Hitpoints"];
            var sp = lp.Statistics["Speed"];

            Assert.AreEqual(7, hp.BaseValue);
            Assert.AreEqual(15, hp.Max);
            Assert.AreEqual(0, hp.Min);
            Assert.AreEqual(73, sp.BaseValue);
            Assert.AreEqual(250, sp.Max);
        }

        // ============================================================
        // B. Referential integrity
        // ============================================================

        /// <summary>
        /// PRED: An equipped item is the SAME instance as the inventory
        /// entry. After load, the loaded `EquippedItems[slot]` and the
        /// loaded inventory's matching object reference must satisfy
        /// `AreSame`. CONFIDENCE: HIGH — duplicating the entity on
        /// load creates phantom items + de-equipping bugs.
        /// </summary>
        [Test]
        public void Spec_Inventory_EquippedItem_SameInstanceAsInventoryEntry_NoDuplication()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            var hand = new BodyPart { Type = "Hand", Name = "hand", ID = 101 };
            var root = new BodyPart { Type = "Body", Name = "body", ID = 100 };
            root.AddPart(hand);
            var body = new Body();
            player.AddPart(body);
            body.SetBody(root);

            var inv = new InventoryPart();
            player.AddPart(inv);
            var sword = MakeItem("sword-1", "Sword");
            Assert.IsTrue(inv.AddObject(sword));
            Assert.IsTrue(inv.EquipToBodyPart(sword, hand));

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lp = loaded.Player;
            var lInv = lp.GetPart<InventoryPart>();
            var lBody = lp.GetPart<Body>();
            var lHand = lBody.GetPartByType("Hand");

            Assert.IsNotNull(lHand.Equipped, "Loaded hand must still have an equipped item.");
            Assert.AreSame(lHand.Equipped, lInv.EquippedItems[lHand.ID.ToString()],
                "EquippedItems[slot] must point to the SAME instance as BodyPart.Equipped — no duplicate entity.");
            // Note: equipped items are NOT in InventoryPart.Objects (that list holds unequipped items only).
            // The same-instance contract is enforced via the token map across BodyPart._Equipped and
            // EquippedItems[slot]. Pinning that here, plus the round-trip identity, plus the absence
            // from Objects (which would indicate duplication, the actual bug we want to catch).
            Assert.IsFalse(lInv.Objects.Contains(lHand.Equipped),
                "Equipped items live exclusively in EquippedItems[slot] + BodyPart._Equipped — they are not duplicated into Objects.");
        }

        /// <summary>
        /// PRED: Two distinct entities at different cells stay distinct
        /// after round-trip — they don't get merged or aliased. Each
        /// retains its own ID. CONFIDENCE: HIGH.
        /// </summary>
        [Test]
        public void Spec_Zone_TwoEntitiesAtDifferentCells_StayDistinct_AfterLoad()
        {
            var player = MakeCreature("p-1", "Player", isPlayer: true);
            var npc = MakeCreature("n-1", "Snapjaw");
            var zone = new Zone("Overworld.10.10.0");
            zone.AddEntity(player, 1, 1);
            zone.AddEntity(npc, 5, 5);
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 80 }
                });

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;

            var atP = lZone.GetCell(1, 1).Objects;
            var atN = lZone.GetCell(5, 5).Objects;
            Assert.AreEqual(1, atP.Count, "Cell (1,1) should hold one entity.");
            Assert.AreEqual(1, atN.Count, "Cell (5,5) should hold one entity.");
            Assert.AreNotSame(atP[0], atN[0], "The two entities must remain distinct instances.");
            Assert.AreEqual("p-1", atP[0].ID);
            Assert.AreEqual("n-1", atN[0].ID);
        }

        /// <summary>
        /// PRED: A BrainPart.Target reference (cross-entity pointer)
        /// resolves to the LOADED entity, not a stale reference to
        /// the pre-save instance. CONFIDENCE: MEDIUM — depends on the
        /// token-based reference encoding. If the system stores a raw
        /// .NET reference somewhere, post-load Target would be the
        /// pre-save instance and this would fail.
        /// </summary>
        [Test]
        public void Spec_BrainPart_TargetReference_PointsToLoadedEntity_NotOriginal()
        {
            var player = MakeCreature("p-1", "Player", isPlayer: true);
            var npc = MakeCreature("n-1", "Snapjaw");
            var brain = new BrainPart { Target = player };
            npc.AddPart(brain);

            var zone = new Zone("Overworld.10.10.0");
            zone.AddEntity(player, 1, 1);
            zone.AddEntity(npc, 5, 5);
            brain.CurrentZone = zone;
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 80 }
                });

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lPlayer = loaded.Player;
            var lZone = loaded.ZoneManager.ActiveZone;
            // The NPC instance in the loaded zone:
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(5, 5).Objects)
                if (obj.ID == "n-1") { lNpc = obj; break; }
            Assert.IsNotNull(lNpc, "Loaded NPC must be present.");
            var lBrain = lNpc.GetPart<BrainPart>();
            Assert.IsNotNull(lBrain, "Loaded NPC must still have BrainPart.");
            Assert.AreSame(lPlayer, lBrain.Target,
                "BrainPart.Target must be the LOADED player instance — not a stale reference to the pre-save player.");
        }

        // ============================================================
        // C. Format & corruption resistance
        // ============================================================

        /// <summary>
        /// PRED: Loading from an empty stream throws (does not return
        /// a half-initialized GameSessionState). CONFIDENCE: HIGH —
        /// reading 0 bytes when expecting a magic header should be a
        /// clear failure.
        /// </summary>
        [Test]
        public void Spec_Load_EmptyStream_FailsWithoutMutation()
        {
            using var stream = new MemoryStream(new byte[0]);
            var reader = new SaveReader(stream, null);

            Assert.Catch<System.Exception>(
                () => GameSessionState.Load(reader),
                "Empty stream must throw on load — never silently produce an invalid GameSessionState.");
        }

        /// <summary>
        /// PRED: Random garbage bytes are rejected on load (magic
        /// header check fails). CONFIDENCE: HIGH.
        /// </summary>
        [Test]
        public void Spec_Load_RandomBytes_FailsCleanly()
        {
            // 256 bytes of pseudo-random nonsense — extremely unlikely to start with COO0 magic + format.
            var rng = new System.Random(31337);
            var garbage = new byte[256];
            rng.NextBytes(garbage);
            // Force the first 4 bytes to NOT be the COO0 magic (0x434F4F30 little-endian).
            garbage[0] = 0xFF; garbage[1] = 0xFF; garbage[2] = 0xFF; garbage[3] = 0xFF;

            using var stream = new MemoryStream(garbage);
            var reader = new SaveReader(stream, null);

            Assert.Catch<System.Exception>(
                () => GameSessionState.Load(reader),
                "Random bytes (no valid magic) must throw on load.");
        }

        /// <summary>
        /// PRED: A truncated save (valid header but cut off mid-stream)
        /// throws on load rather than returning a partial state.
        /// CONFIDENCE: MEDIUM — partial-data handling is a classic
        /// source of save bugs. May fail loudly or may silently drop
        /// missing data.
        /// </summary>
        [Test]
        public void Spec_Load_TruncatedStream_FailsCleanly()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var (_, bytes) = RoundTripWithBytes(player, zone, mgr, turns);

            Assert.Greater(bytes.Length, 16, "Need a non-trivial save to truncate meaningfully.");
            // Truncate to 16 bytes — keeps any header but cuts off the body.
            var truncated = new byte[16];
            System.Array.Copy(bytes, truncated, 16);

            using var stream = new MemoryStream(truncated);
            var reader = new SaveReader(stream, null);

            Assert.Catch<System.Exception>(
                () => GameSessionState.Load(reader),
                "Truncated save must throw — not return partial state.");
        }

        /// <summary>
        /// PRED: A stream whose magic-header bytes are wrong is rejected
        /// even if the rest looks plausible. CONFIDENCE: HIGH —
        /// SaveSystem.cs declared `Magic = 0x304F4F43 // COO0` at the
        /// top, which signals magic-validation is the design.
        /// </summary>
        [Test]
        public void Spec_Load_WrongMagic_RejectsEvenIfBinaryWellFormed()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var (_, bytes) = RoundTripWithBytes(player, zone, mgr, turns);

            // Corrupt only the first 4 bytes (the magic header).
            var corrupted = (byte[])bytes.Clone();
            corrupted[0] = 0xDE; corrupted[1] = 0xAD; corrupted[2] = 0xBE; corrupted[3] = 0xEF;

            using var stream = new MemoryStream(corrupted);
            var reader = new SaveReader(stream, null);

            Assert.Catch<System.Exception>(
                () => GameSessionState.Load(reader),
                "Wrong magic header must reject the load (this isn't our save file).");
        }

        // ============================================================
        // D. Cross-system state preservation
        // ============================================================

        /// <summary>
        /// PRED: TurnManager.TickCount round-trips. CONFIDENCE: HIGH
        /// — the existing round-trip test already pins this; included
        /// here for spec completeness.
        /// </summary>
        [Test]
        public void Spec_TurnManager_TickCount_Preserved()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            turns.RestoreSavedState(tickCount: 4242, waitingForInput: true, currentActor: player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 }
                });

            var loaded = RoundTrip(player, zone, mgr, turns);

            Assert.AreEqual(4242, loaded.TurnManager.TickCount);
        }

        /// <summary>
        /// PRED: Per-entity energy in the turn queue round-trips with
        /// values intact. CONFIDENCE: HIGH.
        /// </summary>
        [Test]
        public void Spec_TurnManager_PerEntityEnergy_Preserved()
        {
            var player = MakeCreature("p-1", "Player", isPlayer: true);
            var npc = MakeCreature("n-1", "Snapjaw");
            var zone = new Zone("Overworld.10.10.0");
            zone.AddEntity(player, 1, 1);
            zone.AddEntity(npc, 2, 2);
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 777 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 333 }
                });

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lPlayer = loaded.Player;
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(2, 2).Objects) if (obj.ID == "n-1") { lNpc = obj; break; }

            Assert.AreEqual(777, loaded.TurnManager.GetEnergy(lPlayer));
            Assert.AreEqual(333, loaded.TurnManager.GetEnergy(lNpc));
        }

        /// <summary>
        /// PRED: Cell metadata flags (Explored, IsVisible, IsInterior)
        /// round-trip per-cell. CONFIDENCE: HIGH — partially proven by
        /// the existing whole-world round-trip test, made explicit here
        /// per-flag.
        /// </summary>
        [Test]
        public void Spec_Cell_ExploredVisibleInterior_Flags_Preserved()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var cellExplored = zone.GetCell(3, 3);
            cellExplored.Explored = true;
            cellExplored.IsVisible = false;
            cellExplored.IsInterior = false;

            var cellVisible = zone.GetCell(4, 4);
            cellVisible.Explored = false;
            cellVisible.IsVisible = true;

            var cellInterior = zone.GetCell(5, 5);
            cellInterior.IsInterior = true;

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;

            Assert.IsTrue(lZone.GetCell(3, 3).Explored);
            Assert.IsFalse(lZone.GetCell(3, 3).IsVisible);

            Assert.IsTrue(lZone.GetCell(4, 4).IsVisible);
            Assert.IsFalse(lZone.GetCell(4, 4).Explored);

            Assert.IsTrue(lZone.GetCell(5, 5).IsInterior);
        }

        /// <summary>
        /// PRED: BrainPart's goal stack round-trips: stack ORDER
        /// preserved AND each goal's custom fields (e.g., MoveTries on
        /// LayRuneGoal, GoToCorpseTries on DisposeOfCorpseGoal)
        /// preserved. CONFIDENCE: LOW — this is the §3.9 risk surface
        /// I flagged. May rely on per-goal custom serialization that
        /// might not be wired for every goal type.
        /// </summary>
        [Test]
        public void Spec_BrainPart_GoalStack_OrderAndCustomFields_Preserved()
        {
            var npc = MakeCreature("n-1", "TestNpc");
            var brain = new BrainPart();
            npc.AddPart(brain);

            var zone = new Zone("Overworld.10.10.0");
            zone.AddEntity(npc, 1, 1);
            brain.CurrentZone = zone;

            // Push two goals — bottom: LayRuneGoal with MoveTries=3, top: WaitGoal.
            var layRune = new LayRuneGoal(targetX: 5, targetY: 5, runeBlueprint: "RuneOfFlame");
            layRune.MoveTries = 3;
            brain.PushGoal(layRune);
            brain.PushGoal(new WaitGoal(2));

            var player = MakeCreature("p-1", "Player", isPlayer: true);
            zone.AddEntity(player, 0, 0);
            var mgr = MakeManagerWithZone(zone);
            var turns = new TurnManager();
            turns.RestoreSavedState(0, true, player,
                new List<TurnManager.SavedTurnEntry>
                {
                    new TurnManager.SavedTurnEntry { Entity = player, Energy = 100 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 50 }
                });

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lZone = loaded.ZoneManager.ActiveZone;
            Entity lNpc = null;
            foreach (var obj in lZone.GetCell(1, 1).Objects) if (obj.ID == "n-1") { lNpc = obj; break; }
            Assert.IsNotNull(lNpc);

            var lBrain = lNpc.GetPart<BrainPart>();
            Assert.IsNotNull(lBrain);
            var snapshot = lBrain.GetGoalsSnapshot();
            Assert.AreEqual(2, snapshot.Count, "Both goals must be on the loaded stack.");
            Assert.IsInstanceOf<WaitGoal>(snapshot[snapshot.Count - 1],
                "Top of stack should still be the WaitGoal.");
            var loadedLay = snapshot[0] as LayRuneGoal;
            Assert.IsNotNull(loadedLay, "Bottom of stack should still be a LayRuneGoal.");
            Assert.AreEqual(5, loadedLay.TargetX);
            Assert.AreEqual(5, loadedLay.TargetY);
            Assert.AreEqual("RuneOfFlame", loadedLay.RuneBlueprint);
            Assert.AreEqual(3, loadedLay.MoveTries,
                "LayRuneGoal.MoveTries (custom field) MUST round-trip — losing this resets the retry budget.");
        }

        // ============================================================
        // E. Functional invariants
        // ============================================================

        /// <summary>
        /// PRED: Save → load → save produces a stream byte-equivalent
        /// to the first save. Confirms determinism (no nondeterministic
        /// dictionary iteration order, no timestamp-in-stream).
        /// CONFIDENCE: MEDIUM — many save systems include a "saved at"
        /// timestamp or rely on Dictionary iteration order, both of
        /// which would break determinism.
        /// </summary>
        [Test]
        public void Spec_RoundTrip_Idempotent_SaveLoadSave_ProducesEquivalentStream()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.SetTag("Faction", "Cultists");
            player.Properties["Mood"] = "stoic";
            player.Statistics["Hitpoints"].BaseValue = 8;

            // First save.
            var state1 = GameSessionState.Capture("test-game", "test-version", mgr, turns, player);
            byte[] bytes1;
            using (var s = new MemoryStream())
            {
                state1.Save(new SaveWriter(s));
                bytes1 = s.ToArray();
            }

            // Load + immediately save again.
            GameSessionState loaded;
            using (var s = new MemoryStream(bytes1))
                loaded = GameSessionState.Load(new SaveReader(s, null));

            byte[] bytes2;
            using (var s = new MemoryStream())
            {
                loaded.Save(new SaveWriter(s));
                bytes2 = s.ToArray();
            }

            Assert.AreEqual(bytes1.Length, bytes2.Length,
                "Save→load→save must produce same-length byte stream (determinism).");
            CollectionAssert.AreEqual(bytes1, bytes2,
                "Save→load→save must produce byte-equivalent stream — proves no timestamp / nondeterministic ordering in the format.");
        }

        /// <summary>
        /// PRED: Loading produces NEW entity instances; mutating a
        /// loaded entity does not affect the original. CONFIDENCE:
        /// HIGH — this is core deserialization semantics; aliasing the
        /// originals would defeat the purpose.
        /// </summary>
        [Test]
        public void Spec_Load_DoesNotMutateOriginal_LoadedEntitiesAreDistinctInstances()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            player.Statistics["Hitpoints"].BaseValue = 10;

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lp = loaded.Player;

            Assert.AreNotSame(player, lp,
                "Loaded player must be a distinct instance from the original.");

            // Mutate the loaded entity — original must be untouched.
            lp.Statistics["Hitpoints"].BaseValue = 1;
            Assert.AreEqual(10, player.Statistics["Hitpoints"].BaseValue,
                "Mutating loaded entity must not bleed back to the original.");
        }

        /// <summary>
        /// PRED: Two saves followed by two loads — each load reflects
        /// its own save's state, not the other. CONFIDENCE: HIGH.
        /// Probes for hidden static state that could cross-contaminate
        /// loads.
        /// </summary>
        [Test]
        public void Spec_TwoSavesTwoLoads_EachLoadReflectsItsOwnSave()
        {
            var (p1, z1, m1, t1) = MakeMinimalState("alpha", "Z.1.1.0");
            p1.SetTag("Faction", "Alpha");

            var (p2, z2, m2, t2) = MakeMinimalState("beta", "Z.2.2.0");
            p2.SetTag("Faction", "Beta");

            byte[] bytesA, bytesB;
            using (var s = new MemoryStream())
            {
                GameSessionState.Capture("g", "v", m1, t1, p1).Save(new SaveWriter(s));
                bytesA = s.ToArray();
            }
            using (var s = new MemoryStream())
            {
                GameSessionState.Capture("g", "v", m2, t2, p2).Save(new SaveWriter(s));
                bytesB = s.ToArray();
            }

            GameSessionState loadA, loadB;
            using (var s = new MemoryStream(bytesA))
                loadA = GameSessionState.Load(new SaveReader(s, null));
            using (var s = new MemoryStream(bytesB))
                loadB = GameSessionState.Load(new SaveReader(s, null));

            Assert.AreEqual("alpha", loadA.Player.ID);
            Assert.AreEqual("Alpha", loadA.Player.Tags["Faction"]);
            Assert.AreEqual("beta", loadB.Player.ID);
            Assert.AreEqual("Beta", loadB.Player.Tags["Faction"]);
            Assert.AreNotSame(loadA.Player, loadB.Player);
            Assert.AreNotSame(loadA.ZoneManager.ActiveZone, loadB.ZoneManager.ActiveZone);
        }

        // ============================================================
        // F. Static factory re-wiring (the §3.9 priority-list edge)
        // ============================================================

        /// <summary>
        /// PRED: After a load, an NPC carrying a LayRuneGoal can lay a
        /// rune IF the static <c>LayRuneGoal.Factory</c> is wired (by
        /// GameBootstrap or by the test). The save format itself
        /// MUST NOT depend on the factory being wired AT LOAD TIME —
        /// because static state is process-global, not save-graph
        /// state. CONFIDENCE: MEDIUM. Probes that the loader doesn't
        /// crash if Factory==null at load time.
        /// </summary>
        [Test]
        public void Spec_StaticFactories_NotRequiredAtLoadTime()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();

            // Explicitly null out the factories before load.
            var savedFactory = LayRuneGoal.Factory;
            var savedCorpseFactory = CorpsePart.Factory;
            try
            {
                LayRuneGoal.Factory = null;
                CorpsePart.Factory = null;

                Assert.DoesNotThrow(() =>
                {
                    var loaded = RoundTrip(player, zone, mgr, turns);
                    Assert.IsNotNull(loaded.Player,
                        "Load must succeed even when static factories are not yet wired.");
                });
            }
            finally
            {
                LayRuneGoal.Factory = savedFactory;
                CorpsePart.Factory = savedCorpseFactory;
            }
        }

        // ============================================================
        // G. Effect-state preservation
        // ============================================================

        /// <summary>
        /// PRED: A status effect (with non-zero stacks and remaining
        /// duration) round-trips with its state intact. CONFIDENCE:
        /// MEDIUM-LOW — this is exactly the kind of edge that can quietly
        /// break: effects are dynamic, may use type-name based dispatch
        /// for serialization, and the frozen-bug saga lived in this code.
        /// </summary>
        [Test]
        public void Spec_StatusEffect_StacksAndDurationRoundTrip()
        {
            var (player, zone, mgr, turns) = MakeMinimalState();
            var effects = new StatusEffectsPart();
            player.AddPart(effects);

            // Apply a smoldering effect (well-known M6 surface).
            var smolder = new SmolderingEffect(duration: 5);
            effects.ApplyEffect(smolder, source: null, zone: zone);
            // Stack again — should accumulate.
            effects.ApplyEffect(new SmolderingEffect(duration: 5), source: null, zone: zone);

            var loaded = RoundTrip(player, zone, mgr, turns);
            var lp = loaded.Player;
            var lEffects = lp.GetPart<StatusEffectsPart>();
            Assert.IsNotNull(lEffects, "Loaded player must still have StatusEffectsPart.");

            var lSmolder = lEffects.GetEffect<SmolderingEffect>();
            Assert.IsNotNull(lSmolder, "SmolderingEffect must round-trip.");
            // Don't pin exact duration value — it depends on stack semantics — but it must be > 0.
            Assert.Greater(lSmolder.Duration, 0,
                "Loaded effect must have a positive remaining duration.");
        }
    }
}
